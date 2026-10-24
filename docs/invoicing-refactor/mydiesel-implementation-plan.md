# myDiesel integration implementation plan

Status: implementation in progress; production activation remains gated
API baseline: AADE myDiesel ERP Guide v1.0, 19 October 2026  
Application baseline: shared invoicing schema through database version 61

Implemented through 24 October 2026:

- versioned Core contracts, strict QR parsing, all six ERP adapter calls, sanitized HTTP evidence, and contract tests;
- database migration 62, coupon projection, immutable operation history, reference cache, historical credential profiles, lease/CAS recovery, and SQL-100 tests;
- reservation, release, redemption, unknown-outcome reconciliation, administrator MARK/station corrections, and safe verification polling;
- invoice workflow snapshot v2 with coupon scope, beneficiary/environment/litre checks, accepted-MARK redemption before printing, and restart-safe recovery;
- native settings, invoice coupon panel, catalogue columns/filtering, and a separate resizable evidence form with a minimum size of 1100 x 650; and
- UAT/production activation checks that keep production disabled without the reviewed OpenAPI fingerprint.

Still gated or pending acceptance evidence: the official OpenAPI/examples bundle, live UAT credentials/coupons, the final fuel-code-to-host-product mapping, and the business-approved EFK/VAT/accounting representation. These are rollout inputs rather than safe values to infer in code.

## 1. Objective

Integrate AADE myDiesel into ASFuelControl so a station can:

1. scan or enter an agricultural-diesel coupon;
2. validate the beneficiary and reserve the coupon before issuing the invoice;
3. issue an eligible diesel invoice through the existing shared invoice workflow;
4. redeem the coupon with the accepted 15-digit myDATA MARK;
5. release a reservation when a sale is abandoned;
6. monitor AADE's later invoice verification;
7. correct the submitted MARK or station when the API permits it; and
8. recover safely after process termination, timeout, response loss, or an operator retry.

The implementation must retain the existing SQL Server 2008 R2 compatibility target, .NET Framework 4.6 target, immutable fiscal evidence, document leases, and controlled rollout behavior.

## 2. Contract and rollout gates

Implementation may start against the guide, but production activation is blocked until all of the following are available and reviewed:

- the official `openapi-erp-v1.0.json` file is stored with a recorded SHA-256 and compared with the handwritten DTO/client contract;
- the AADE examples package and UAT API key are available;
- a UAT tester/company allocation and test coupons are available;
- the business confirms how the EFK discount, VAT, rounding, invoice text, and accounting entries must be represented;
- the business approves the policy for litres above the coupon amount;
- AADE clarifies or UAT proves the recovery behavior where `CouponInfo` does not return the beneficiary AFM, linked MARK, or current branch; and
- the installation has registered its production company and participating stations in myDiesel.

The OpenAPI contract has precedence over the prose guide. Contract differences found during implementation must be recorded in this document and covered by tests before a production build.

## 3. Supported scope

The first production scope is deliberately narrow:

- one myDiesel coupon per invoice;
- one invoice MARK per coupon;
- invoice documents only (`invoiceType = INVOICE`);
- fuel codes returned by `/fuel-types`, initially AADE codes 20 and 21;
- a Greek beneficiary VAT number obtained from the QR payload or entered separately;
- synchronous invoice acceptance with a usable 15-digit MARK before coupon redemption;
- direct myDATA, iTimologia, or Ilyda routes only when they return a final MARK;
- the existing payment workflow, including card/IRIS, without authorizing a second payment during myDiesel recovery; and
- station correction and MARK correction as explicit administrator actions.

The first version excludes receipts, multiple coupons on one invoice, automatic cancellation/reissue, offline or queued invoice issuance without a MARK, coupon issuance, business registration, and coupon returns. Those exclusions must be shown in settings validation rather than failing after external work has begun.

## 4. API behavior that drives the design

| Operation | Endpoint | Local meaning |
|---|---|---|
| Company lookup | `GET /secured/erp/company?vat=...` | Validate registration and select an accepted `branchNum`. |
| Fuel types | `GET /secured/erp/fuel-types` | Refresh eligible fuel reference data. |
| Query/reserve | `POST /secured/erp/coupon/query` | A state-changing operation: an `ACTIVE` coupon becomes `RESERVED`. |
| Redeem | `PUT /secured/erp/coupon/redeem` | Link the reserved coupon to the accepted invoice MARK. |
| Release | `PUT /secured/erp/coupon/release` | Return a reserved coupon to `ACTIVE`, or receive `EXPIRED` if its validity elapsed. |
| Station correction | `PUT /secured/erp/coupon/branch` | Change only the station on a qualifying redeemed coupon. |

Production uses `https://mydatapi.aade.gr/myDiesel` with `aade-user-id` and `ocp-apim-subscription-key`. UAT uses `https://acc-mydiesel.aade.gr/prv` with `X-myDiesel-PRV-API-Key` and `company-vat`. `companyVat` remains mandatory in coupon request bodies in both environments.

The client must keep the operation result separate from the remote coupon status. HTTP 200 can return a non-redeemable coupon. HTTP 500, HTTP 504, transport loss, cancellation, and a malformed response after dispatch are `Unknown`, not rejection.

## 5. Target architecture

### 5.1 Separate coupon-session resource

Add `MyDieselCoupon = 6` to `FiscalDocumentKind` and use the coupon case ID as its document ID. Add `MyDiesel` to `FiscalJournalFamily`.

The coupon is not journalled directly against `Invoice` because reservation occurs before invoice quantities and totals are final. Journal ownership of an invoice currently freezes invoice editing. A separate coupon-session resource preserves exact external-call evidence while allowing the operator to finish the saved draft under restricted validation.

`MyDieselCouponCase` links the coupon-session resource to the host invoice. It is the current projection; `FiscalAttempt`, `FiscalAttemptRevision`, and `FiscalHttpExchange` remain the immutable audit history.

### 5.2 Lock ordering

All code that needs both resources must acquire them in this order:

1. invoice lease (`DocumentKind = Invoice`, `DocumentId = InvoiceId`);
2. coupon-session lease (`DocumentKind = MyDieselCoupon`, `DocumentId = CouponCaseId`).

Reservation and read-only coupon UI operations may acquire only the coupon-session lease. No code may acquire the invoice lease while already holding a coupon-session lease. Tests must prove that concurrent PrintAgent, UI, and recovery operations do not deadlock.

### 5.3 Component placement

| Project | Additions |
|---|---|
| `Exedron.Invoicing.Core` | myDiesel DTOs, normalized coupon identity, remote statuses, operation states, transition rules, client and credential contracts. |
| `Exedron.Invoicing.Adapters` | `MyDieselClient`, environment-specific headers, JSON serialization, error parsing, QR payload parser, and sanitized HTTP logging. |
| `ASFuelControl.Invoicing.Persistence` | `SqlMyDieselStore`, reference-cache store, SQL statements, migration support, concurrency and immutable-attempt handling. |
| `ASFuelControl.Invoicing` | `MyDieselCouponService`, invoice acceptance/redemption coordinator, recovery rules, polling, and invoice eligibility validation. |
| `ASFuelControl.Windows` | settings, credential profiles, reservation UI, invoice/status actions, catalog columns, background verification poller, and activation checks. |
| `tests` | contract, client, SQL, orchestration, recovery, UI binding, UAT checklist, and release-suite integration. |

## 6. Database migration 62

Create `ASFuelControl.Windows/DBScripts/UpdateDatabase62.sql`. Do not amend version 61, which owns the heating-correction journal. Register version 62 in `Program.CurrentDBVersion`, Windows resources/project content, and the persistence project's embedded schema installer.

Migration 62 must be transactional, safe to rerun, and valid under SQL Server compatibility level 100.

### 6.1 Extend the journal contract

- Replace the existing `FiscalDocumentLease.DocumentKind BETWEEN 1 AND 5` check with a named check allowing 1 through 6. The original constraint is system-named, so migration code must discover and drop only the check attached to `FiscalDocumentLease.DocumentKind` before adding the new named constraint.
- Replace `CK_FiscalAttempt_Family` with the existing values plus `MyDiesel`.
- Add nullable `DocumentKind` and `DocumentId` columns to `FiscalHttpExchange`; retain `InvoiceId` for existing readers.
- Add an index on `(DocumentKind, DocumentId, StartedUtc)`.
- Update request logging so a myDiesel exchange contains both coupon-session correlation and linked invoice correlation, without logging authentication headers.

### 6.2 `MyDieselCouponCase`

Create this host-owned projection table without modifying generated OpenAccess entities:

| Column | SQL type | Purpose |
|---|---|---|
| `CouponCaseId` | `uniqueidentifier` PK | Coupon-session/document identity. |
| `InvoiceId` | `uniqueidentifier` | Saved draft or issued host invoice. |
| `CouponCode` | `varchar(24)` | Normalized uppercase coupon code. |
| `BeneficiaryVat` | `char(9)` | AFM parsed from QR or explicitly entered. |
| `CompanyVat` | `char(9)` | Historical calling company. |
| `Environment` | `tinyint` | Test or production. |
| `CredentialProfileId` | `varchar(128)` | Immutable reference to the historical non-secret credential profile. |
| `ReservedBranchNum` | `int` | Branch used by the successful reservation. |
| `ExciseDiscountPerLitre` | `decimal(18,4)` | Configured EFK discount frozen when the coupon case is captured; defaults to €0.41/L. |
| `CurrentBranchNum` | `int` NULL | Latest known redemption/correction branch. |
| `LocalStage` | `varchar(32)` | Captured, ReserveUnknown, Reserved, Released, InvoiceAccepted, RedeemUnknown, Redeemed, VerificationPending, Final, InterventionRequired, or Dismissed. |
| `RemoteStatus` | `varchar(32)` NULL | Exact AADE `couponStatus`. |
| `Redeemable` | `bit` NULL | Latest AADE value. |
| `IssuedLitres` | `decimal(18,2)` NULL | Coupon limit returned by AADE. |
| `CouponCreatedUtc` | `datetime2(0)` NULL | Converted from API Unix timestamp. |
| `ValidUntilUtc` | `datetime2(0)` NULL | Converted from API Unix timestamp. |
| `FuelType` | `varchar(32)` NULL | Verified AADE fuel type. |
| `FuelCode` | `int` NULL | Verified AADE fuel code. |
| `LowBalance` | `bit` NOT NULL | Latest low-balance flag. |
| `LowBalanceMessageEl` | `nvarchar(1000)` NULL | Exact Greek operator warning. |
| `InvoiceMark` | `varchar(15)` NULL | MARK used in the latest redeem request. Store as text. |
| `MarkSourceInvoiceId` | `uniqueidentifier` NULL | Local invoice supplying that MARK. |
| `BlocksInvoice` | `bit` NOT NULL | Prevents attaching another coupon to the invoice. |
| `BlocksCouponReuse` | `bit` NOT NULL | Prevents local reuse while reserved/redeemed/uncertain. |
| `LastCheckedUtc` | `datetime2(7)` NULL | Last authoritative coupon response. |
| `Revision` | `bigint` NOT NULL | Optimistic-concurrency projection revision. |
| `FencingToken` | `bigint` NOT NULL | Coupon-session lease fencing token. |
| `CreatedUtc`, `UpdatedUtc` | `datetime2(7)` | Audit timestamps. |

Add:

- a filtered unique index on `InvoiceId WHERE BlocksInvoice = 1`;
- a filtered unique index on `CouponCode WHERE BlocksCouponReuse = 1`;
- an index on `(RemoteStatus, LastCheckedUtc)` for verification polling; and
- checks for environment, non-negative branch numbers, positive litres when present, valid fuel codes when present, and a 15-digit MARK when present.

`Released`, `EXPIRED`, and `CANCELLED` cases clear both blocking flags after authoritative evidence. Redeemed, verification, and unknown outcomes continue to block reuse. Historical rows are never deleted to permit a retry.

### 6.3 `MyDieselReferenceCache`

Create a small cache keyed by `(Environment, CompanyVat)` containing:

- the last successful company/station JSON and SHA-256;
- the last successful fuel-types JSON and SHA-256;
- refresh timestamps; and
- the endpoint/API contract version used.

This cache supports startup validation and offline display only. It cannot authorize a reservation when AADE is unavailable.

## 7. Core contracts and adapter

### 7.1 Core models

Add versioned contracts for:

- `MyDieselScope`: environment, base URL, company VAT, selected branch, credential-profile ID;
- `MyDieselCouponIdentity`: normalized code and beneficiary VAT;
- `MyDieselLookupRequest`, `MyDieselRedeemRequest`, and `MyDieselCouponInfo`;
- company, station, and fuel-type responses;
- `MyDieselError`: HTTP status, stable `code`, path, timestamp, and diagnostic text;
- remote coupon statuses, retaining an `UnknownValue` path for future values; and
- operation states `Prepared`, `Sending`, `Succeeded`, `Rejected`, `Unknown`, and `FailedBeforeDispatch`.

Do not deserialize `couponStatus` directly into a closed enum that rejects future AADE values. Retain the original text and map known values for UI/actions.

### 7.2 QR parsing and normalization

The QR parser accepts only a JSON object with `code`, `afm`, `validUntil`, and `issuedLitres`. It must:

- normalize the code to the AADE format;
- validate the beneficiary AFM as exactly nine digits;
- parse Unix seconds as UTC;
- parse litres using invariant JSON number rules;
- reject duplicate/conflicting JSON properties; and
- retain only parsed fields, not the raw QR payload.

Manual coupon-code entry is allowed only with a separately entered beneficiary AFM because `CouponInfo` does not contain it. The invoice trader VAT must equal the captured beneficiary VAT before reservation and again before issuance.

### 7.3 HTTP client

`MyDieselClient` must:

- construct all six endpoints from a fixed environment catalog;
- send the exact authentication headers required for the selected environment;
- omit `company-vat` from production ERP requests and include it in UAT;
- send `companyVat` in every coupon body;
- use UTF-8 JSON and a finite timeout longer than AADE's documented server timeout;
- parse both `ErrorDTO` and the special 500/504 `{ success:false }` envelope;
- treat omitted optional fields and explicit JSON `null` identically;
- classify stable error codes rather than matching the `error` text;
- never log subscription keys, API keys, authorization headers, or arbitrary query strings; and
- save request evidence before transport through the existing logger boundary.

The client remains transport-only. It does not decide whether an operation may be retried.

## 8. Configuration and credentials

Raise `SharedInvoiceConfiguration.Version` to 3 and add a `MyDieselProfile`:

- enabled flag;
- environment;
- fixed base URL/API version;
- production company VAT from the application settings, or one of the official synthetic company VATs in UAT;
- selected `branchNum` and station display data;
- EFK discount per litre, configurable with a default of `0.41` euros;
- production AADE user ID plus immutable subscription-key option reference;
- the official shared UAT ERP API key from the AADE examples v0.1 (never myDATA test credentials);
- last successful connection/reference check; and
- polling policy.

Production secrets remain in encrypted immutable `Option` keys. Configuration JSON stores only option references. Production may explicitly reference the same AADE credential option used by direct myDATA, but reuse must be selected and scope-validated; it must not be inferred from a provider route. The shared UAT ERP key is a public test fixture distributed by AADE and is versioned in the adapter alongside the supplied synthetic company and beneficiary VAT catalogues.

Add a **myDiesel** group to General Options:

- enable checkbox;
- environment inherited from the common fiscal environment;
- read-only company VAT from `CompanyTIN` in production;
- official synthetic company-VAT selector in UAT, preferring `CompanyTIN` when it already contains one of those values;
- production AADE user ID and subscription key;
- no UAT credential entry: the official shared ERP key from examples v0.1 is used automatically;
- `Έλεγχος σύνδεσης` button;
- registered-station selector populated by company lookup; and
- EFK discount per litre; each captured coupon stores the current value so later settings changes cannot alter an issued workflow; and
- reference-data status showing last company/fuel refresh.

The settings check must fail activation when company VAT, credentials, accepted station, fuel codes, or environment alignment are missing. Saving settings must not itself reserve a coupon.

## 9. Invoice eligibility rules

Add a single validator used by the invoice form and workflow preparation. For an attached myDiesel case it must require:

- an accepted `RESERVED` case or a recoverable reservation-unknown case;
- invoice trader VAT equal to `BeneficiaryVat`;
- an invoice type that produces an invoice and an accepted MARK;
- every exempted fuel line to use a currently eligible AADE fuel code;
- quantities expressed in litres;
- no receipt/local-only/offline-queue route;
- company VAT and fiscal environment equal to the frozen coupon scope; and
- exactly one blocking coupon case for the invoice.

Recommended first-version quantity policy: block issuance when eligible litres exceed `IssuedLitres`, and direct the operator to split excess litres into a non-exempt transaction. This is safer than silently consuming a future-year entitlement. Make this policy a documented acceptance decision before coding; do not add an unrestricted override.

`lowBalanceMessage.el` must be shown prominently before the operator continues. It is a warning in the AADE API and is stored as evidence even if the operator proceeds.

## 10. UI workflow

### 10.1 Invoice form coupon panel

Add a `MyDieselCouponPanel` above the existing fiscal status panel in `InvoiceFormEx` with:

- QR scanner/paste input;
- manual coupon code and beneficiary AFM fields;
- selected station;
- coupon state, expiry, issued litres, verified fuel, and MARK;
- a visually prominent low-balance message;
- `Έλεγχος και δέσμευση`, `Αποδέσμευση`, and `Ανανέωση κατάστασης` actions; and
- an evidence-preserving `Αφαίρεση άκυρου` action only for a definitively rejected coupon that was never reserved; and
- administrator-only `Διόρθωση ΜΑΡΚ` and `Αλλαγή πρατηρίου` actions when permitted.

Reservation first saves the invoice draft, then creates the coupon case and calls `query`. A successful reservation does not allocate the invoice number and does not create `FiscalInvoiceWorkflow`. A new coupon is rejected when the invoice is already printed or has fiscal issuance evidence (a shared workflow/attempt, invoice signature, QR data, or myDATA row).

While reserved, the operator may edit quantity, price, payment method, and permitted invoice data. The following are blocked unless the coupon is first released: deleting the invoice, changing the trader to another AFM, changing to an ineligible document type, removing all eligible diesel lines, or attaching a different coupon.

Closing a form with a reserved coupon and no issued invoice prompts the operator to release it. If release is unknown, the form closes only after recording the unresolved case in the intervention queue; it must not claim the coupon is available.

### 10.2 Status and recovery UI

Extend `FiscalDocumentPanel` with a myDiesel summary/tab showing:

- coupon code, beneficiary VAT, company/station, and coupon litres;
- local stage and exact remote status;
- reservation, redeem, release, poll, and correction attempts;
- last check and verification status;
- stored MARK and its source invoice; and
- an actionable explanation for unknown or invalid states.

The existing embedded invoice panel remains compact. Full myDiesel attempt history and HTTP evidence open in a new resizable `MyDieselEvidenceForm` (minimum 1100 x 650) from the panel or catalogue. The detailed form uses a full-width attempt grid plus a separate HTTP-evidence tab; it is not squeezed into the invoice form's bottom tab area.

Extend `SqlFiscalCatalogStore` and `MyDataCatalog` with columns/filtering for coupon code, myDiesel status, and `Χρειάζεται παρέμβαση`. The invoice catalog remains a business-document list; detailed request/response evidence stays in the fiscal status screen.

### 10.3 Corrections

MARK correction is administrator-only and requires:

- a qualifying remote state (`REDEEMED`, `VERIFICATION_FAILED`, `VERIFICATION_EXHAUSTED`, or `VERIFICATION_INVALID`);
- a new 15-digit MARK;
- selection of the local accepted invoice that produced it, where available;
- a mandatory reason/evidence note; and
- a new immutable redeem attempt linked to the original attempt.

Station correction uses `coupon/branch`, requires an accepted company station, and records a new immutable attempt. The UI disables both corrections after a verified/final state.

## 11. Invoice workflow integration

Add `MyDieselCouponCaseId` to `InvoiceWorkflowSnapshot` version 2. The snapshot stores the case ID and non-secret coupon scope, never credentials. Snapshot validation rejects myDiesel when the invoice route cannot provide a final MARK.

Inject `IMyDieselRedemptionCoordinator` into `InvoiceWorkflowService`. The workflow becomes:

```mermaid
sequenceDiagram
    participant UI as Invoice UI
    participant Coupon as myDiesel service
    participant WF as Invoice workflow
    participant Fiscal as Provider/myDATA/POS
    participant Print as Print service

    UI->>Coupon: query and reserve saved draft
    Coupon-->>UI: RESERVED + litres/warning
    UI->>WF: issue invoice
    WF->>WF: acquire invoice then coupon lease
    WF->>WF: validate and freeze snapshot
    WF->>Fiscal: payment/signing/submission
    Fiscal-->>WF: accepted invoice + 15-digit MARK
    WF->>Coupon: redeem exact coupon/branch/MARK
    Coupon-->>WF: REDEEMED or known/unknown failure
    alt redemption known successful
        WF->>Print: render/dispatch saved invoice
    else rejected or unknown
        WF-->>UI: issued invoice; coupon requires action
    end
```

Detailed ordering:

1. Enter the invoice lease.
2. Load exactly one blocking coupon case linked to the invoice.
3. Enter the coupon-session lease in the prescribed order.
4. Validate the saved draft against the coupon.
5. Allocate invoice identity and freeze `InvoiceWorkflowSnapshot` as today, including the coupon case reference.
6. Run existing payment, signing, and submission behavior without changing its retry rules.
7. Require an accepted response containing a 15-digit MARK and persist normal invoice acceptance.
8. Create and persist a myDiesel redeem attempt before sending its exact request.
9. Send `coupon/redeem` using the frozen company, branch, coupon, and accepted MARK.
10. Persist the reply and update the coupon-case projection.
11. Permit automatic printing only when redemption is known successful or the coupon is already in a later verified state.

An accepted invoice followed by rejected/unknown redemption is still an issued invoice. The software must not delete it, create another invoice, repeat payment, repeat signing, or repeat provider submission. It returns an intervention result and exposes the legal invoice for an explicit administrator print/correction path.

Provider queue/offline acceptance without a MARK is incompatible with myDiesel. The workflow remains pending before redemption and does not claim coupon success.

## 12. Recovery and retry rules

| Situation | Required behavior |
|---|---|
| Query timeout/connection loss | Repeat the same query for the same company/station. It is outcome-idempotent and confirms an existing reservation. |
| Query returns 200/non-redeemable | Record the returned status as authoritative; do not issue the myDiesel invoice. |
| Redeem timeout/504 | Record `RedeemUnknown`; query current status before any operator decision. Never resubmit invoice/payment/signing. |
| Query after unknown redeem returns `REDEEMED` or later | Retain unknown MARK linkage unless the same MARK can be confirmed; require reconciliation or a deliberate same-MARK redeem based on UAT evidence. |
| Release timeout/504 | Do not use a plain query as a harmless read: it can reserve an `ACTIVE` coupon again. Run a controlled recovery sequence or require web/UAT evidence. |
| Release retry returns `COUPON_STATUS_INVALID` | Treat as ambiguous until the current status is established; it can mean the first release succeeded or the coupon moved to another state. |
| 400/401/403/404/409 with a stable code | Record a definitive rejection for that request; enable only the actions allowed for that specific code/state. |
| HTTP 500, malformed reply after dispatch, process termination | Keep the attempt blocking and unknown. |
| `VERIFICATION_FAILED` | Poll; AADE performs its own retry schedule. Do not automatically submit a new MARK. |
| `VERIFICATION_EXHAUSTED` or `VERIFICATION_INVALID` | Show intervention; allow an explicit MARK/station correction only where documented. |
| `VERIFIED`, `VERIFIED_PAID`, `VERIFIED_UNPAYABLE` | Final for ERP actions; block correction and coupon reuse. |

Background polling calls `query` only for already redeemed/verification states, where the call does not reserve an active coupon. It must not poll `ACTIVE`, `Released`, or an unresolved release case automatically. Use bounded backoff and surface stale cases rather than polling forever.

## 13. Logging, privacy, and support evidence

Persist exact request bytes and sanitized response bodies through the existing evidence model. Do not store raw QR JSON when parsed fields suffice. Never log subscription/API keys or authentication headers.

The support view/export should include:

- company VAT, station number, local timestamp, endpoint operation, coupon code, stable error code, correlation/case ID, invoice ID, and MARK where applicable;
- request/response SHA-256 and attempt IDs; and
- no TaxisNet credentials, subscription key, API key, or unrelated customer data.

## 14. Tests

### 14.1 Contract tests

- QR parsing, Greek/Latin coupon normalization, spaces/hyphens, invalid AFM/timestamp/litres;
- known and future coupon statuses;
- request validation and 15-digit MARK preservation as string;
- allowed operation transitions and final-state action blocking; and
- OpenAPI schema/property/required-field comparison.

### 14.2 Adapter tests with fake HTTP

- UAT and production headers;
- company, fuel, query, redeem, release, and branch payloads;
- optional fields omitted versus explicit `null`;
- success envelope, `ErrorDTO`, gateway authentication response, 500, 504, malformed JSON, timeout, and connection loss;
- persist-before-dispatch evidence; and
- secret/header redaction.

### 14.3 Persistence tests

- migration 62 new install, rerun, partial-install rejection, and SQL-100 parsing;
- discovery/replacement of the old system-named document-kind check;
- coupon and invoice filtered uniqueness;
- revision/fencing protection and concurrent terminals;
- deterministic dual-lease ordering;
- immutable correction attempts;
- blocking behavior for unknown calls; and
- cache fingerprint validation.

### 14.4 Workflow tests

- standard reserve -> issue -> MARK -> redeem -> print;
- low-balance display and retained evidence;
- abandoned sale and release;
- expired/cancelled/already-redeemed coupon;
- beneficiary mismatch, ineligible fuel, quantity over policy, wrong branch, and duplicate coupon;
- payment approval followed by redemption rejection/unknown without a second charge;
- signing/submission acceptance followed by process termination before redeem;
- restart recovery without a second invoice submission;
- queued/offline provider rejection before work starts;
- MARK and station correction;
- verification polling to every final/exception state;
- projection/UI failure after successful redemption; and
- concurrent UI and PrintAgent processing.

### 14.5 UI and release tests

- native form bindings and Greek warning/status rendering;
- fields/actions enabled for each coupon state;
- invoice close/delete/edit guards;
- settings validation and station selection;
- catalog filtering/intervention queue; and
- inclusion in `Verify-InvoicingRelease.ps1` without live AADE traffic.

UAT is a separate opt-in suite using credentials supplied at execution time. It must never run from the ordinary offline regression scripts.

## 15. File-level change map

### New production files

- `Exedron.Invoicing.Core/MyDieselContracts.cs`: scopes, DTOs, statuses, operation results, interfaces, and transition rules.
- `Exedron.Invoicing.Adapters/MyDieselClient.cs`: REST client, QR parser, environment/header handling, and error parsing.
- `ASFuelControl.Invoicing.Persistence/SqlMyDieselStore.cs`: coupon-case and reference-cache persistence.
- `ASFuelControl.Invoicing/MyDieselCouponService.cs`: reservation, release, redemption, correction, polling, and recovery orchestration.
- `ASFuelControl.Windows/Threads/SharedMyDieselRuntime.cs`: Windows host composition, credentials, configuration, and background polling.
- `ASFuelControl.Windows/Threads/SharedMyDieselRuntime.Status.cs`: invoice-linked status/read/recovery operations.
- `ASFuelControl.Windows/UI/Controls/MyDieselCouponPanel.cs` plus designer/resources: invoice coupon capture and actions.
- `ASFuelControl.Windows/UI/Forms/MyDieselEvidenceForm.cs` plus designer/resources: resizable detailed attempt and HTTP-evidence view.
- `ASFuelControl.Windows/DBScripts/UpdateDatabase62.sql`: schema migration described above.

### Existing production files to modify

- `Exedron.Invoicing.Core/FiscalSigningContracts.cs`: add document kind 6.
- `Exedron.Invoicing.Core/RequestResponseLogging.cs`: add generic document correlation while retaining invoice correlation.
- `Exedron.Invoicing.Adapters/LoggedHttpClient.cs`: pass coupon correlation and keep header/query redaction.
- all four affected classic `.csproj` files: explicitly include new source files; embed migration 62 in persistence.
- `ASFuelControl.Invoicing.Persistence/SqlStatements.cs`: migration/runtime statements for cases, attempts, cache, and catalog projection.
- `ASFuelControl.Invoicing.Persistence/SqlFiscalDatabase.cs`: install the fifth schema resource.
- `ASFuelControl.Invoicing.Persistence/SqlFiscalHistoryStore.cs`: add the `MyDiesel` family.
- `ASFuelControl.Invoicing.Persistence/SqlFiscalCatalogStore.cs`: add coupon/status/intervention projections.
- `ASFuelControl.Invoicing/InvoiceWorkflowService.cs`: snapshot v2, redemption coordinator, result fields, and print gate.
- `ASFuelControl.Windows/Threads/SharedInvoiceConfiguration.cs`: configuration v3 and myDiesel profile.
- `ASFuelControl.Windows/Threads/SharedInvoicingSettings.cs`: validation, credential references, upgrade from v2, and activation checks.
- `ASFuelControl.Windows/Threads/SharedInvoiceRuntime.cs`: attach the reserved coupon to preparation and inject the coordinator.
- `ASFuelControl.Windows/Threads/SharedInvoiceRuntime.Status.cs`: combine fiscal and coupon state/recovery.
- `ASFuelControl.Windows/UI/SettingForms/MainSettingsForm.Invoicing.cs` plus designer/resources: myDiesel settings and connection test.
- `ASFuelControl.Windows/UI/Forms/InvoiceFormEx.Invoicing.cs`: host the coupon panel and apply edit/close/delete guards.
- `ASFuelControl.Windows/UI/Controls/FiscalDocumentPanel.cs`: show redemption and verification evidence/actions.
- `ASFuelControl.Windows/UI/Catalogs/MyDataCatalog.cs` plus designer/resources: coupon/status/intervention columns and filters.
- `ASFuelControl.Windows/Program.cs`, `Properties/Resources.resx`, `Properties/Resources.Designer.cs`, and `ASFuelControl.Windows.csproj`: register migration 62 and new UI/runtime sources.

### New and modified test files

- add `tests/Invoicing.MyDiesel/` with a standalone executable and README;
- add `tests/Verify-InvoicingMyDiesel.ps1` using fake HTTP and a uniquely named disposable LocalDB database;
- extend persistence SQL-100 parsing and migration tests;
- extend workflow/Windows diagnostic tests for form actions and print gating; and
- add the new offline suite to `tests/Verify-InvoicingRelease.ps1`.

## 16. Delivery phases

### Phase 0 - contract pinning

Store and hash the OpenAPI/examples artifacts, resolve DTO discrepancies, document invoice tax treatment and quantity policy, and define UAT acceptance data.

### Phase 1 - contracts and client

Implement Core models, QR parser, HTTP client, stable error classification, and fake-HTTP tests. No database/UI activation.

### Phase 2 - migration and durable coupon service

Implement migration 62, journal extensions, coupon/reference stores, reservation/release/recovery service, SQL-100 tests, and concurrency tests.

### Phase 3 - settings and reservation UI

Implement configuration version 3, protected credentials, station/fuel checks, coupon panel, invoice edit guards, and offline UI tests. Keep feature disabled.

### Phase 4 - invoice acceptance and redemption

Add snapshot version 2 and the redemption coordinator, gate printing, preserve existing payment/submission rules, and add crash/restart workflow tests.

### Phase 5 - status, correction, and polling

Extend the fiscal panel/catalog, implement controlled corrections and redeemed-state polling, and test intervention flows.

### Phase 6 - UAT and pilot

Run all seven documented scenarios plus timeout, restart, concurrent-terminal, payment, printer-failure, and correction cases. Then enable one installation/station for a controlled pilot and reconcile every invoice/coupon outcome before expansion.

## 17. Rollback behavior

Disabling myDiesel prevents new reservations but does not disable recovery, release, redemption, status polling, or correction of already-owned cases. Historical credential profiles and attempts remain available.

If a pilot fails:

1. stop all workers;
2. save a disabled configuration revision;
3. restart the same compatible binaries;
4. finish or reconcile all reserved and issued cases;
5. preserve external and database evidence; and
6. do not restore a pre-pilot database after external reservation, invoice acceptance, payment, or redemption.

## 18. Completion criteria

The feature is implementation-complete only when:

- the official OpenAPI contract test passes;
- all offline suites and the full release suite pass;
- a native SQL Server 2008 R2 migration/runtime smoke test passes;
- all documented UAT scenarios and failure-injection cases are reconciled;
- no retry can issue a second invoice, payment, signature, print, or coupon mutation after an unknown result;
- every accepted invoice with a coupon has a durable MARK and known or explicitly unresolved redemption state;
- every reserved but abandoned coupon is released or visible in the intervention queue;
- settings remain disabled by default; and
- the pilot checklist is signed off with exact application build, configuration revision, station, provider route, and test evidence.

