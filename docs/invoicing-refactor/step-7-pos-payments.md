# Step 7 — Durable POS payment integration

The host now has an explicit GBridge payment workflow for iTimologia card/IRIS and direct Ilyda card payments. It retains payment approval independently of invoice issuance, supports lookup-only recovery, and journals explicit voids and full refunds against their original payment. PrintAgent activation remains step 8; the existing Windows worker has not been switched to this path.

## Components and entry points

| Component | Responsibility |
|---|---|
| Core `ProviderPaymentCoordinator` | Signature preparation, persist-before-authorization, explicit decline retry, approved reuse and lookup-only payment recovery. |
| Host `PaymentRoute` | Immutable provider route, explicit GBridge endpoint/TID, iTimologia signature format and Ilyda protocol. No credentials in the snapshot. |
| Host `ProviderPaymentService` | Holds the SQL invoice lease across mapping, signature, authorization and issuance. Reads the original model/route for all subsequent calls. |
| Host `ProviderPaymentCompensation` | Explicit void/recovery and one full refund per original accepted payment, with original/credit leases acquired in stable order. |
| Persistence `SqlProviderPaymentStore` | Stable transaction UID, signature, payment outcome and immutable versioned workflow snapshot. |
| Persistence `SqlPaymentCompensationStore` | Separate Void/Refund records linked to the original accepted payment. An unknown reversal remains blocking. |

Use the following host entry points:

- `Send(invoiceId, route, source)`: first issuance or continuation using the saved model. `retryDeclined` explicitly permits a new attempt after definitive payment rejection. `retryRejectedInvoice` permits another provider submission using the **same approved payment** and original invoice snapshot.
- `Recover(invoiceId)`: looks up the original payment and any existing invoice submission. It never starts invoice issuance if no submission exists, creates a replacement payment, requests a new signature, cancels a signature, or dispatches a Prepared/SignatureReady payment. This includes replacements saved by older automatic-retry builds.
- `GetRetryablePaymentAttemptId(invoiceId)`: reads persisted eligibility without HTTP or mutation. It returns the attempt the UI must name when requesting a retry.
- `RetryPayment(invoiceId, expectedAttemptId)`: explicit operator action for qualifying **single F4 / F4 → F2 → 0E** evidence. Revalidates the latest attempt under the invoice lease, creates a linked replacement with a fresh UID/signature, and sends exactly one payment request. A stale or duplicate click cannot retry a newer attempt. It performs payment only; issuance continues separately using the durable approval.
- `VoidAcceptedPayment(invoiceId, reason)`: explicit full void of an approved payment when no invoice dispatch exists, or every existing invoice attempt is definitively rejected. Accepted/unknown invoice issuance blocks it.
- `RecoverVoid(invoiceId)`: lookup only. A Prepared void remains Prepared; explicit `VoidAcceptedPayment` resumes those saved parameters. Processing/Unknown never sends another void command.
- `Refund(creditInvoiceId, originalInvoiceId, source, ..., recoveryOnly: true/false)`: explicit full refund against the original accepted payment/invoice. Use this entry point for refund recovery too; ordinary Send/Recover reject a saved refund without the original reference.

The host must use one shared `SqlFiscalDatabase` instance for the workflow's nested stores and keep these synchronous leases on their owning thread. Do not run old and new workers on the same documents; the old worker does not participate in these locks.

## Ordering and durable states

Before external signing, the service validates the real host mapping, provider serializer, issuer/branch, exact cents, EUR, one full card/IRIS payment and supported provider. Legacy print/signature/submission history and conflicting fiscal stages block a fresh payment.

The saved `provider-payment.v2.json` request contains the original mapped invoice, provider credential-profile identity, endpoint, terminal, signature configuration, payment amount/type and optional refund source. Credentials are resolved using that historical provider route. Later calls do not consult current source entities. Pending/Unknown/Approved payments retain their original route. An explicit continuation of an unsent or definitively failed attempt may use corrected credentials/signature mode for the same provider, environment, issuer, endpoint and terminal. It preserves the old request and creates a linked replacement; an unused Ilyda signature is cancelled with the historical credentials first.

The payment progresses through:

`Prepared → SignaturePending → SignatureReady → Pending → Approved → Invoiced`

`SignaturePending` is saved before requesting a provider signature. If that request or its persistence is interrupted, another signature is not automatically requested. Uncertain results still require reconciliation; this step does not invent a provider-signature lookup. Definitive signature rejections become Failed and permit explicit continuation. Old SignaturePending records with retained definitive rejection evidence are reconciled without sending a signature request. Unsent SignatureReady attempts revisit signature preparation, so Ilyda renews expired unused signatures and iTimologia regenerates them before authorization. Ilyda also retains its own separate provider-session request/response history. Expired Ilyda signatures cannot begin authorization; the original provider signature remains unchanged for invoice submission, while GBridge receives the required encoding.

`Pending` is saved before the gateway call. An existing Pending/Unknown attempt is queried with InputType 4/3, never re-authorized under that UID. A correlated, explicitly reported decline from either lookup becomes Declined, as do exact signature and NSP credential rejections. Other malformed/ambiguous replies and transport failures remain Unknown. Explicitly mismatched values remain blocked. Missing response fields become Unknown/IncompleteResponse with a controlled missing-field message, allowing lookup-only recovery under the same UID. An incomplete database result falls through to the terminal lookup; explicit conflicts still stop recovery. Old Mismatch records are repaired only when their retained evidence proves missing fields without contradictory values. Approval requires the original transaction UID, terminal, amount and explicitly reported transaction/payment types, plus a transaction ID. An absent zero-valued payment/type field is not treated as a reported value.

Recovery first replays original stored authorization/lookup evidence through the same strict validator. It can restore approval after a failed approval-state write without contacting the gateway. A single database result addressed by the full UID may contain an eight-character CustomerReference prefix, provided the remaining identity, amount and explicitly reported type fields match; terminal lookups and declines do not use this exception. Reversal evidence, multiple authorizations and contradictory results block replay.

Recovery also evaluates original stored rejection evidence. An old Pending/Unknown attempt may be reclassified Declined only when every authorization proves an exact signature/NSP rejection and no lookup contains a transaction. This adds a journal revision; original responses are never rewritten. The invoice form's explicit continuation enables retry of Declined/Failed payments, independently of retrying a rejected invoice submission. Payment errors use controlled, actionable UI messages; raw bodies remain in the evidence journal.

### Operator retry after single F4 or F4 → F2 → 0E

The invoice form offers **Επανάληψη πληρωμής** for a single correlated F4 authorization response, or for the exact retained sequence below. Clicking the button initiates one replacement without an additional confirmation dialog. Ordinary processing, PrintAgent polling and recovery never initiate a replacement. The eligibility rule follows the installation's assumption that a new attempt is permitted; it is not a gateway-reported decline:

- One authorization: HTTP 200, Status 200, ReplyCode F4, EftposReplyCode 200.
- Database lookup: HTTP 200/400, Status 400, ReplyCode F2, empty EftposReplyCode.
- Terminal lookup: HTTP 200, Status 200, ReplyCode 0E, EftposReplyCode 200.

For the complete sequence, every response must contain an empty EFTReplyData array and match the request's InputType and original UID. Request TIDs must match, and the authorization must retain the saved provider input and provider/transaction/payment types. No conflicting, incomplete or additional authorization evidence is allowed. The last two events must be the database and terminal lookups. Refunds and any existing invoice submission are excluded.

For a lone F4, exactly one non-signature event must exist: authorization InputType 1, HTTP 200, Status 200, ReplyCode F4, EftposReplyCode 200, the original UID/TID and signed request values, and absent/null/empty EFTReplyData. Any transaction data or other gateway event excludes the single-F4 rule. A returned HTTP-200 F4 waits for the operator. Explicit recovery may add lookups; all retained evidence must still qualify before a later retry. Approval, conflicting values or incomplete transaction data disable the retry action.

On the explicit click, the host atomically records an `OperatorRetry` decision with policy `GBridge.F4.v1` or `GBridge.F4-F2-0E.v1`, the predecessor's Unknown outcome, evidence count and old/new identities together with the new Prepared payment. The original attempt stays Unknown in history. If the replacement also returns F4, another deliberate click is required. Interrupted Prepared/SignatureReady replacements are resumed under their existing UID only through this button. Historical `AutomaticRetry` decisions remain readable and are never rewritten as operator decisions.

During an explicit retry, Ilyda cancels the unused signature before preparing a fresh signature. Cancellation must be confirmed before another authorization, unless the unused signature has already expired. Expired sessions are retained and released so a fresh signature can be prepared. The new attempt retains a renewal flag while Prepared, so interrupted cancellation resumes its own saved provider session on the next explicit click. Recovery does not cancel or renew signatures. Approval/issuance and uncertain outcomes outside these qualifying responses keep their existing safeguards. No database migration is needed; runtime queries remain SQL Server 2008 R2 compatible.

Exact HTTP bodies are saved by the existing SQL HTTP logger before transport, including signature and gateway requests. Responses and diagnostic gateway events are retained separately. Request-evidence failure prevents the call. Failure saving approval propagates and leaves Pending for stored-evidence replay or lookup; it does not report success or authorize again.

Ilyda POS payment details are supplied in its outer EG-5 payment data only. The embedded myDATA XML omits paymentMethods to avoid X-0213; direct myDATA serialization remains independent.

The invoice is submitted only after payment approval is durable. An accepted provider result marks the payment Invoiced only after the submission journal confirms Accepted. A provider timeout, queue, malformed reply or result-write failure retains Approved; no automatic void/refund occurs. Subsequent calls recover the original submission. The compatibility `GBridgePOSSupport` also no longer performs automatic compensating voids or repeats unresolved void commands; use the host service for the complete workflow.

## Payment enum correction and historical data

These are separate contracts and must be translated explicitly:

| Meaning | Host PaymentType | Shared invoice model | GBridge / payment journal v2 |
|---|---:|---:|---:|
| Card | 2 | 7 | 0 |
| IRIS | 4 | 8 | 1 |

The previous SQL store incorrectly expected invoice-model values 7/8 while GBridge supplied 0/1. It now validates 0/1 and writes payload format v2. Existing v1 or metadata-less payment records are held for reconciliation by the host rather than reinterpreted or rerouted. The original step-3 persistence fixture was updated to the actual gateway contract.

## Voids and refunds

Voids are separate Payment-family operations with `OriginalPaymentAttemptId`, immutable request parameters/reason and Prepared/Processing/Accepted/Unknown revisions. The original approval remains in history. A void reply must identify that original gateway transaction, terminal, amount and payment/transaction types. Unknown voids remain blocked and use lookup only. A completed void blocks invoice retry from charging again.

Refunds require an accepted original payment attached to an accepted invoice. The credit must match the original customer VAT, payment method and full amount. The original payment is reserved for that credit before any refund dispatch; a second credit or competing void cannot claim it. The reservation stores the validated credit input, so a crash before creating the child payment cannot cause remapping of changed entities. The credit's payment request retains the original invoice/payment IDs, and attempts link through the original-payment/retry chain. GBridge receives its existing signed-credit refund operation (TxnType 1); no unsupported wire field is invented for the original transaction reference.

Both original and credit invoices are locked in stable GUID order. Unknown refunds are looked up using their original refund transaction UID. An approved refund followed by uncertain credit issuance cannot refund again. A final reservation-save failure likewise cannot reset the child payment. Partial/multiple refunds and deferred payments are outside this implementation and fail closed. Original-invoice cancellation history requires reconciliation before a refund.

## Supported scope and later integration

- The new service supports GBridge with iTimologia card/IRIS and Ilyda Direct card. Ilyda IRIS is rejected before HTTP because the supplied contract does not establish support.
- Direct myDATA cannot supply the provider signature needed by this path. Samtec's combined signing/POS call remains blocked in the step-6 adapter: the supplied protocol does not expose sufficient independent payment/recovery evidence to claim support.
- Existing PrintAgent settings also expose direct Mellon/VivaWallet paths. The new libraries supply a GBridge adapter; this step does not silently reinterpret a Mellon API key as a terminal ID or migrate those installations to GBridge. Confirm their supported replacement/configuration before rollout.
- Numbering remains the host's responsibility. Split/mixed payments, cash/credit through this POS service, non-EUR amounts and unspecified payment methods are rejected. Plain non-POS submission continues through the step-5 service.
- PrintAgent routing, physical printing and compatibility projections remain step 8. Payment approval does not set printed flags, signatures, MARK or QR fields on legacy entities.

## Database and UI

No migration, table, column or production SQL batch is added or changed. Existing fiscal attempt/revision, HTTP exchange and payment event tables hold the new states and versioned .NET JSON snapshots. Compensation uses separate Operation values in the existing Payment family. All SQL remains SQL Server 2008 R2-compatible; new test triggers use RAISERROR and existing SQL-100 syntax.

Step 9 should expose explicit gateway endpoint/TID and provider-signature settings, separate payment and invoice states, original payment/refund links, and approved transaction references. Payment retry, invoice retry, void and refund must be separate actions. Unknown must offer recovery/reconciliation rather than a new charge. Existing v1 records must not be silently upgraded through UI settings changes.

## Verification

Run `tests/Verify-InvoicingPayments.ps1`. It builds the real host, Core, adapters and SQL stores, uses detached real Data entities and fake HTTP, and creates/removes a private LocalDB instance/database at compatibility level 100. It never accesses an application database or live terminal/provider. Checks cover both providers, IRIS translation, historical snapshots, before-transport evidence, declines, timeouts, mismatches/missing fields, concurrent callers, signature expiry, failed writes at each dispatch/outcome boundary, separate void recovery and linked refund recovery.

Physical POS/provider certification, actual Data-loader execution against a deployed schema, direct Mellon/Viva migration and native SQL Server 2008 R2 runtime validation remain deployment/integration requirements. SQL-100 grammar and compatibility-level tests do not replace a native server smoke test.

Verified on 2026-10-15: **44 POS checks passed**, including both providers' full-refund flows and the crash between original-payment reservation and child creation. Regression suites passed **37 Core contract, 80 mapper, 98 legacy characterization, 26 persistence/SQL-100, 32 provider and 55 signing checks**: **372 passing checks across seven suites**. All private test databases/instances were removed. The host project builds through the solution's Release configuration; only the existing legacy characterization warnings remain. `git diff --check` reported no whitespace errors.

Recovery update verified on 2026-10-16: **87 payment, 102 workflow and 26 persistence/SQL-100 checks passed**. Native-form checks passed with 16 renders, including automatic-retry explanation and declined-payment continuation. Debug and Release host builds and isolated Windows diagnostic builds passed using the existing Unimep exclusion. Separate Windows output folders were used because the running application/Visual Studio locked the normal Debug DLLs. Test transports were fake and every private LocalDB instance was removed.

Strict recovery and single-F4 update verified on 2026-10-18: the full suite passed **137 payment checks**, followed by **46 targeted recovery checks** after adding the incomplete database-to-terminal fallback. Regression suites passed **102 workflow and 45 provider checks**. Native-form checks passed with **18 renders**, including the single-F4 explanation and incomplete-response recovery controls. The isolated Windows Debug diagnostic build passed using the existing unrelated Unimep exclusion. All provider/POS transports were fake, all private LocalDB instances were removed, and the test databases used SQL Server compatibility level 100. No database migration is required. Logs are under `tests/gbridge-recovery-20261018-*.log`.

Operator-only retry update verified on 2026-10-18: automatic replacements have been removed. The full suite passed **140 payment checks**, followed by **9 targeted checks** covering added interruption/legacy/concurrency cases and refund eligibility. The workflow suite passed **102 checks**. Native UI tests passed with **20 renders**, including real Telerik button dispatch without confirmation, duplicate-click blocking, and disabled retry for incomplete responses. The isolated Windows Debug diagnostic build passed with the existing Unimep exclusion and the existing AutoPay dependency DLL supplied from the local build output because its project hint path is stale. No live transports or application database were used; private LocalDB instances were removed. SQL compatibility level 100 remains in use and no migration is required. Logs are under `tests/gbridge-manual-retry-20261018-*.log`.
