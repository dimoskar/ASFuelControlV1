# Step 6 — Fiscal device extraction

The shared signing path now supports Samtec invoices, balances and tank fillings with durable requests and outcomes. This is a staged integration: production PrintAgent still uses its existing path until step 8 connects signing, submission, payment and printing together. Do not run the old worker and the new service for the same documents; the old worker does not acquire the new lease.

## Ownership

| Component | Responsibility |
|---|---|
| `Exedron.Invoicing.Core/FiscalSigningCoordinator.cs` | Portable execution, document lease, persist-before-dispatch, explicit retry/resume and optional read-only recovery. No Data, SQL, Newtonsoft, Samtec SDK or UI dependency. |
| `Exedron.Invoicing.Adapters/SamtecFiscalSigner.cs` | Device profile validation, versioned UTF-8 JSON payload, Samtec HTTP transport and response parsing. Implements the Core contracts. |
| `Exedron.Invoicing.Adapters/SamtecWireModels.cs` | Extracted protocol fields, independent of the legacy Samtec assembly. |
| `ASFuelControl.Invoicing/HostFiscalSigningMapper.cs` | Maps real host entities to the wire contract without mutating entities. Numbering and rendered text are supplied by the host. |
| `ASFuelControl.Invoicing/FiscalSigningService.cs` | SQL store integration, cross-stage guards and a database-backed source with explicit issuer/device scope and rendering callbacks. |

Fiscal orchestration belongs in Core; device-specific JSON and HTTP belong in Adapters. There is no reference from Core to the hardware adapter or host database.

## Durable behavior

1. Acquire the document kind/ID lease across all scopes. Reuse an existing signed result without reading current settings or remapping the entity.
2. For a new attempt, check other fiscal journals and legacy evidence under the lease. Require an already numbered invoice, matching scope, explicit cash/credit payment and supported document kind.
3. Build and save `Prepared`, including the exact rendered text, payload bytes, SHA-256 and historical device/issuer scope.
4. Resolve the historical device profile and perform local validation. A local failure becomes `FailedBeforeDispatch`; it cannot contact the device.
5. Save `Signing` before HTTP. Send exactly the saved bytes once, without automatic redirects or adapter retries.
6. Save a real signature, optional independent QR value and raw response as `Signed`. A timeout, malformed response, non-success HTTP status, generic device `Error`, mismatched document/operation or placeholder becomes `Unknown`.

A final database-write failure propagates; the caller never receives an undurable successful signature. A saved `Signing` found after interruption becomes `Unknown`. Samtec does not implement recovery because this repository does not establish a verified lookup returning a signature bound to the original uncertain request. `GetInvoiceInfo` was used for cancellation daily numbers; that alone is insufficient evidence for signing recovery. Unknown never calls Sign again, even after settings change or an explicit retry flag.

A `Prepared` attempt is returned unchanged by an ordinary Sign call. `ResumePrepared` is an explicit workflow action that dispatches its saved bytes and historical scope. Only `Rejected` or `FailedBeforeDispatch` permit an explicitly requested new attempt; scope must remain unchanged. The Samtec adapter currently emits no definitive rejection because authoritative rejection codes have not been established. Core supports other adapters with verified rejection/recovery capabilities.

## Preserved protocol and deliberate boundaries

- Invoice InputType selection, invoice/header fields, receiver text, price precision, ToEven rounding, VAT exemption 11, cash/credit mappings and code 169/175 sign conventions match the extracted legacy builders in tested cases. Lines are ordered by saved line ID for deterministic snapshots.
- Code 316 retains truncated normalized volume to three decimals and now formats values below one litre with a leading zero. It requires exactly one fuel line rather than silently selecting one from several.
- Balances use type 501. Tank fillings accept 40 or 181 and retain returned QR evidence. Text rendering remains a host concern, including tank-filling context generation.
- Card/IRIS, fleet and missing/unknown payment selections are blocked before dispatch. Samtec payment type 1 can initiate POS, so it must wait for step 7. Raw requests cannot enable POS, printer dispatch or token requests through those fields.
- Cancellation code 215 is blocked pending verified original-device daily-number evidence and cancellation journal integration. No daily number is guessed and payload construction performs no hidden lookup.
- Alerts and titrimetry have no implemented signer capability; the old `-` placeholder is never treated as a signature. The incomplete legacy file-signing path has no new adapter and requires deployment inventory before replacement.
- Response acceptance requires the exact first result field `ΔΦΣΣ` and a non-placeholder signature. Optional InvoiceUID and JobType must match when present. Responses without those fields are bound to this single synchronous HTTP request, matching the existing protocol usage. This is not a claim of device protocol certification; other response formats remain Unknown until verified.
- Neither signing nor recovery sets `IsPrinted`, a print date, legacy signature/QR fields or any transmission/payment result. Printing and compatibility projections belong to step 8.

## Host integration contract

Construct `FiscalSigningService` with the shared `SqlFiscalDatabase` and a resolver returning a new owned signer for the **supplied historical scope**. Use immutable, versioned `DeviceId` profiles that retain the original endpoint, issuer, branch and environment. Do not resolve an old ID through the current global Samtec settings. Missing historical configuration must fail closed. `SamtecDeviceProfile` accepts explicit HTTP(S) endpoints because existing installations use local HTTP; credentials, query strings and fragments are rejected.

`DatabaseFiscalSigningSource` requires a database factory, a scope reader and a side-effect-free renderer. These callbacks run synchronously while the lease is held; they must not number, sign, print, call a provider or save changes. Supply the tank-filling official code explicitly when 181 is needed. The source reads actual entities and blocks existing legacy signing/print/submission evidence. The mapper is also exposed for detached entities and other host integrations.

For now, any submission, provider session, payment, cancellation or print journal blocks a new standalone signing attempt. Step 8 must replace this conservative guard with verified stage prerequisites under the same outer invoice lease. In particular, a legitimate signing-then-myDATA workflow must be composed there; the independent step-5 and step-6 services are not yet that orchestration. Recovery of an existing attempt remains available.

## Database and UI

No table, column, migration or production SQL statement is added or changed. This step uses the existing `FiscalDocumentLease`, `FiscalAttempt` and `FiscalAttemptRevision` tables from migration 57. All additional serialization is in .NET. SQL Server 2008 R2 compatibility remains required.

No UI is activated in this step. Step 9 needs versioned device-profile settings, separate signing/printing status, signature and QR display, and reconciliation visibility for Unknown/legacy history. A retry button must not turn Unknown into a new signing attempt. The future prepared-resume action must use `ResumePrepared`, and accepted reprints must use stored evidence.

## Verification

Run `tests/Verify-InvoicingSigning.ps1`: **55 checks passed**, using detached real Data entities, the real SQL signing store, Core coordinator and Samtec adapter with fake HTTP. The runner extracts the actual PrintAgent builders into ignored test output and compares structural JSON under Greek and English cultures. It also verifies all three document kinds, raw response retention, QR independence, legacy gates, POS/cancellation/unsupported capability guards, prepared resume, interrupted signing, concurrent workers, SQL failures before/after device dispatch and optional recovery persistence failures.

The test creates and removes a uniquely named LocalDB instance/database at compatibility level 100. No application database, fiscal device, provider, POS or printer is contacted. Physical-device verification, real Data-loader execution against a deployed schema, endpoint/profile rollout and native SQL Server 2008 R2 runtime verification remain deployment work.

Regression verification also passed 37 Core contract, 80 host mapper, 98 legacy characterization, 26 persistence/SQL-100 and 32 provider checks: **328 passing checks across six suites**. The host project builds through the solution's Release configuration. Existing legacy unused-variable warnings remain in the characterization build; no new build errors or whitespace errors were found.
