# ASSalesManager GBridge parity review

Date: 2026-11-04

## Scope

This is a read-only comparison of the GBridge POS-payment flow in ASFuelControl with the implementation in:

`E:\Exedron\2026\ASSalesManager_Legacy\ASSalesManager`

It covers iTimologia and direct Ilyda provider payments, persistence, recovery, retry and cancellation policies, and the related operator UI. The separate ASSalesManager Arbitrans POS integration is not part of the GBridge comparison.

## Executive summary

ASFuelControl already implements most of the normal GBridge flow, often through its newer generic fiscal-workflow layer. Both applications can:

- Generate and persist provider signatures.
- Submit card and IRIS transactions to GBridge.
- Persist attempts and HTTP evidence.
- Recover the same transaction using its original `TransUid`.
- Resume provider submission after payment approval.
- Protect against duplicate payments.
- Void or refund an approved payment.

The largest parity gaps concern uncertain or rejected payments:

1. ASSalesManager classifies more GBridge outcomes.
2. ASSalesManager supports more controlled replacement-payment scenarios.
3. ASSalesManager has explicit cancellation policies for uncertain and no-card transactions.
4. iTimologia ePay Soft POS signature format (`acq_id=2`) is missing from ASFuelControl.
5. ASFuelControl does not provide the same detailed POS status and catalog integration.

## Step-by-step comparison

| Flow step | ASFuelControl status | Finding |
| --- | --- | --- |
| GBridge endpoint and TID | Equivalent | Configurable through `PaymentRoute` and application settings. |
| Provider/GBridge mapping | Equivalent | iTimologia provider ID `34` and Ilyda provider ID `8` are centrally registered. |
| Card/IRIS request creation | Equivalent | The same transaction type, payment type, provider ID, provider input, and provider signature fields are sent. |
| Request persistence before transmission | Equivalent or stronger | ASFuelControl uses `FiscalAttempt`, `FiscalPaymentEvent`, and `FiscalHttpExchange` with revisions and invoice locking. |
| Provider signature persistence | Equivalent | The signature is persisted before POS authorization. |
| Recovery after restart | Equivalent or stronger | Pending and Unknown attempts are reopened as lookup-only operations with their saved route and `TransUid`. |
| Approval restoration from evidence | Equivalent or stronger | `ReadRecordedOutcome` restores approval and detects incomplete or mismatched replies. |
| GBridge response validation | Stronger | ASFuelControl also verifies that amount, transaction type, and payment type were actually present in the response. |
| Void/refund | Stronger architecture | ASFuelControl has a dedicated compensation journal and guarded void/refund recovery actions. |
| F4 operator retry | Implemented differently | ASFuelControl supports an audited new UID for its specific F4 and F4/F2/0E policies. |
| Input rejection, terminal busy, and database-not-found | Missing | ASSalesManager classifies these outcomes explicitly; ASFuelControl does not. |
| No-card confirmation cancellation | Missing | ASSalesManager supports an audited confirmation that no card interaction occurred. |
| Central payment-outcome resolver | Missing | ASFuelControl uses several independent checks rather than one authoritative policy. |
| ePay Soft POS signature format | Missing | ASSalesManager supports iTimologia `acq_id=2`; ASFuelControl accepts only formats 0 and 1. |
| POS status in the invoice catalog | Partial | ASFuelControl has a general fiscal-failure icon but not the dedicated POS state, action, and catalog found in ASSalesManager. |

## Genuine missing functionality

### 1. Central GBridge outcome classification

ASSalesManager has one resolver that classifies an attempt as:

- `Uncertain`
- `Unsent`
- `SignatureRejected`
- `CredentialsRejected`
- `InputRejected`
- `DatabaseNotFound`
- `ManualOverrideEligible`
- `NoCardConfirmationRequired`
- `TransactionRecorded`

Reference:

`ASSalesManager.InvoiceProviders/GBridgePaymentOutcomeResolver.cs`

ASFuelControl currently combines:

- `ReadRecordedOutcome`
- `HasOnlyRejectedAuthorizations`
- `CanRetryAfterF4`
- Payment-store state checks

These checks cover the currently implemented cases, but payment retry, payment change, invoice deletion, cancellation, and UI status do not share one authoritative evidence classification.

For parity, port the resolver decisions so they consume ASFuelControl's `FiscalPaymentEvent` and fiscal-journal records. The old ASSalesManager SQL tables do not need to be copied.

### 2. Input-rejected and terminal-busy handling

ASSalesManager recognizes:

- An exact correlated `FF` input-validation rejection.
- An exact terminal-busy rejection.
- Complete attempt sequences containing only input rejection and empty recovery responses.

Reference:

`ASSalesManager.InvoiceProviders/GBridgeClient.cs`

ASFuelControl does not have an `InputRejected` member in:

`Exedron.Invoicing.Core/GBridgeException.cs`

These cases can therefore remain `Unknown` or generic `Failed`, blocking a safe corrected attempt.

Parity requires:

- Adding the outcome classification.
- Preserving the original attempt and all evidence.
- Permitting a new attempt only when the complete evidence sequence proves that no transaction reached the terminal.

### 3. Database-not-found and controlled replacement payment

ASSalesManager distinguishes a correlated F2 database response from a generic unknown result. It can create a new attempt with a new `TransUid` while retaining the previous attempt as `Unknown`.

It supports:

- Automatic database-not-found retry.
- Explicit user-confirmed unconfirmed retry.
- Audited linkage between old and new attempts.

ASFuelControl currently offers only its specific F4/F2/0E operator policy through:

- `ASFuelControl.Invoicing/ProviderPaymentService.cs`
- `ASFuelControl.Invoicing.Persistence/SqlProviderPaymentStore.cs`

Parity requires equivalents of:

- `TryCreateDatabaseNotFoundRetry`
- A separately named user-confirmed retry policy
- Durable reason, operator, old UID, new UID, and evidence count

There should be no broad automatic retry. A replacement should be authorized only for the exact evidence patterns accepted by the policy.

### 4. Recovery-sequence semantics

ASSalesManager immediately performs a lookup when a received Type-1 response is structurally inconclusive. This performs only lookup; it does not send another payment.

ASFuelControl leaves a returned inconclusive or F4 response as `Unknown` and waits for worker or operator recovery.

There is also a difference after a correlated database F2:

- ASSalesManager stops because it treats F2 as proof that the transaction was not persisted under that UID.
- ASFuelControl continues to terminal lookup Type 3.

Matching ASSalesManager requires both the immediate lookup and its F2 stopping rule.

### 5. No-card local cancellation

ASSalesManager allows local cancellation only after exact safe evidence or after an explicit operator confirmation that the customer never inserted or tapped a card. It retains the POS history and does not misclassify the payment as declined.

Reference:

`ASSalesManager/InvoiceProviders/ProviderInvoiceCancellationResolver.cs`

ASFuelControl provides recovery, approved-payment void, refund, and payment-method correction. It does not have the equivalent cancellation resolver for uncertain, terminal-free, or confirmed-no-card cases.

### 6. HTTP 401 provider-signature rejection

ASSalesManager recognizes two exact provider-signature rejection forms:

- Gateway validation rejection.
- Bank-side HTTP 401 invalid-provider-signature rejection.

ASFuelControl recognizes only the first form. The second form can remain uncertain instead of becoming a correctable signature rejection.

### 7. iTimologia ePay Soft POS

ASSalesManager supports three iTimologia signature formats:

- Format 0: Mellon/ePay/Cardlink/Nexi, `acq_id=1`
- Format 1: EDPS/Viva, `acq_id=0`
- Format 2: ePay Soft POS, `acq_id=2`

ASFuelControl restricts the setting to formats 0 and 1 in:

- `ASFuelControl.Invoicing/PaymentRoute.cs`
- `Exedron.Invoicing.Adapters/ITimologiaInvoiceProvider.cs`

This is a clear missing integration.

### 8. POS monitoring UI

ASSalesManager provides:

- POS status directly in the invoice catalog.
- A dedicated provider-payment catalog.
- Search by invoice type, series, number, UID, transaction, and RRN.
- Retry/recovery actions.
- Explicit cancellation of an unused Ilyda signature.
- Attempt-specific request/response events.

ASFuelControl provides:

- A general fiscal-failure icon.
- Detailed payment information in the opened invoice.
- Recovery, F4 retry, payment change, void, and refund buttons.
- A redacted diagnostics form with UID, transaction, RRN, and TID filters.

Relevant files:

- `ASFuelControl.Windows/UI/Forms/GBridgeDiagnosticsForm.cs`
- `ASFuelControl.Windows/UI/Controls/FiscalDocumentPanel.cs`

Missing for parity:

- A dedicated POS status column in the invoice catalog.
- A dedicated POS catalog or action entry.
- Search by visible invoice series and number rather than only invoice GUID.
- A manual **Cancel unused Ilyda signature** action.
- Exact attempt-scoped HTTP evidence. The current diagnostics form loads all HTTP exchanges for the invoice, which can mix multiple retries.

## Functionality implemented through another ASFuelControl layer

The following ASSalesManager behavior is already present in ASFuelControl and should not be reimplemented using the old design.

### Durable attempts and evidence

ASSalesManager uses `ProviderPaymentAttempt` and `ProviderPaymentEvent` tables.

ASFuelControl instead uses:

- `FiscalAttempt`
- `FiscalAttemptRevision`
- `FiscalPaymentEvent`
- `FiscalHttpExchange`
- `SqlEvidenceLogger`

The ASFuelControl approach provides immutable request identity, revisions, invoice leases, and durable evidence.

### Frozen payment route and provider protocol

ASSalesManager preserves the original Ilyda protocol in its POS session.

ASFuelControl preserves the complete `PaymentRoute` inside the frozen `PaymentSnapshot` in `ProviderPaymentService`. Recovery uses that saved route instead of current settings.

### Ilyda unused-signature lifecycle

ASSalesManager handles protocol changes directly inside `IlydaPos`.

ASFuelControl handles configuration replacement through `ProviderPaymentService`, which cancels an unused lifecycle signature before changing the saved route. A separate manual cancellation action is still missing from the UI.

### Restart recovery

`ProviderPaymentCoordinator` turns Pending or Unknown attempts into lookup-only authorization. The same transaction UID and saved signature are reused; a new payment is not created automatically.

### Duplicate-action protection

The ASFuelControl UI sends the attempt ID that it displayed, and the store verifies that the attempt is still current. A stale or double-clicked action cannot retry a newer attempt.

### Approved-payment void and refund

ASFuelControl's `ProviderPaymentCompensation` and compensation store provide a more explicit implementation than the corresponding ASSalesManager behavior.

### Payment-method correction

ASFuelControl already supports controlled payment-method correction after a failed issued invoice and invalidates the prior failed submission payload when eligible. This is newer functionality and is not missing from the comparison.

## Conditional differences

These ASSalesManager behaviors should be added only if they are required operationally.

### Split or partial payments

ASSalesManager's coordinator permits exactly one POS or IRIS payment among other payment lines.

ASFuelControl requires exactly one payment covering the complete gross amount. Supporting split tender requires invoice-UI, mapping, provider-signature, and payload testing.

### IRIS with non-Mellon iTimologia modes

ASSalesManager does not explicitly restrict iTimologia IRIS to Mellon mode.

ASFuelControl restricts IRIS to iTimologia with Mellon mode. The legacy permissiveness should not be copied without verified provider examples for EDPS, Viva, or ePay Soft IRIS.

## Recommended implementation order

1. Add a central GBridge outcome resolver with parity tests.
2. Add exact input-rejected, terminal-busy, F2, and HTTP-401 classifications.
3. Extend the existing journal store with database-not-found and confirmed-retry decisions.
4. Add the cancellation and no-card confirmation policy.
5. Add catalog POS status and the missing operator actions.
6. Add iTimologia ePay Soft `acq_id=2` support.
7. Consider split payments and broader IRIS support only after confirming the operational requirement and provider behavior.

## Likely ASFuelControl implementation surface

The parity work should primarily affect:

- `Exedron.Invoicing.Adapters/GBridgeClient.cs`
- A new or shared GBridge outcome-resolver class
- `Exedron.Invoicing.Core/GBridgeException.cs`
- `ASFuelControl.Invoicing.Persistence/SqlProviderPaymentStore.cs`
- `ASFuelControl.Invoicing/ProviderPaymentService.cs`
- `ASFuelControl.Windows/Threads/SharedInvoiceRuntime.Status.cs`
- `ASFuelControl.Windows/UI/Controls/FiscalDocumentPanel.cs`
- `ASFuelControl.Windows/UI/Forms/GBridgeDiagnosticsForm.cs`
- `ASFuelControl.Windows/UI/Catalogs/InvoiceCatalog.cs`
- `Exedron.Invoicing.Adapters/ITimologiaInvoiceProvider.cs`
- `ASFuelControl.Invoicing/PaymentRoute.cs`
- `Exedron.Invoicing.Core/PosModeCatalog.cs`
- Payment, persistence, workflow, and UI tests
