# Invoicing refactor step 2: shared contracts

Implemented on 2026-10-15. This step changes the new libraries and adds offline contract tests. The running ASFuelControl application, PrintAgent, solution membership, database and UI have not been switched. The [step 1 baseline](step-1-baseline.md) remains the reference for existing behavior.

## Ownership

| Component | Responsibility |
|---|---|
| Core | Fiscal document identity, historical signing scope, immutable signing request/result, optional recovery contract, store/lease contracts, state-transition rules, signage routing policy and invoice inputs/validation. |
| Adapters | Provider/device payload preparation, transport, response parsing and supported recovery protocols. Existing public signage entry points delegate to Core. Samtec transport extraction is step 6. |
| Host | Number allocation, entity-to-input mapping, SQL stores and leases, configuration/credentials, orchestration, permissions, print rendering/jobs and tank-control side effects. |

The Core assembly still targets .NET Framework 4.6 and has no dependency on ASFuelControl, an ORM, SQL, WinForms, Samtec or a provider SDK. Existing provider/payment interfaces and their persisted string states remain unchanged; migration code must explicitly translate them into the new stage states.

## Fiscal signing contract

[FiscalSigningContracts.cs](../../Exedron.Invoicing.Core/FiscalSigningContracts.cs) defines:

- `FiscalDocumentReference`: non-empty document ID and explicit invoice/balance/tank-filling/alert/titrimetry kind. Zero is invalid. Supporting a kind in the identity enum does not mean a device can sign it.
- `FiscalSigningScope`: saved signer key, device identity, test/production environment, issuer VAT and branch. Credentials stay in host configuration; historical recovery must resolve this scope instead of using the newly selected device.
- `FiscalSigningRequest`: non-empty attempt ID, identity, scope, versioned payload format and exact payload bytes. Bytes are defensively copied and carry a SHA-256 fingerprint. The format identifier must describe encoding and protocol version; a hash alone is not the recoverable request. The host prepares and saves this request before external I/O, after allocating the document number.
- `IFiscalSigner`: explicit supported document kinds and a signing operation. The adapter validates its configured scope and capabilities before dispatch. It must send the saved payload, without rebuilding it from mutable entities. Credentials/transport headers are separate from the journalled fiscal payload.
- `IFiscalSignerRecovery`: an optional, read-only lookup capability. Lookup must identify the original request. Unsupported lookup, an empty result or “not found” leaves the outcome unknown; none authorizes another signing call.
- `FiscalSigningResult`: signed, definitively rejected or unknown, with attempt identity and retained response evidence. Signed requires a real signature and acceptance evidence; the legacy `-` and `Sign Device A` placeholders are rejected. Optional QR and device reference are separate values. Provider invoice signatures and POS payment signatures continue through their existing contracts.
- `FiscalSigningAttempt`: immutable request plus state, revision, UTC audit times and a matching result. The invoice issue/dispatch date remains fiscal wall-clock time, separate from UTC audit timestamps.

There is deliberately no concrete signer or store in this step. Interface comments define implementation requirements; they do not themselves provide cross-process exclusion or recovery.

## Separate processing stages

[FiscalProcessingStates.cs](../../Exedron.Invoicing.Core/FiscalProcessingStates.cs) supplies separate signing, transmission, payment and print enums. Values are explicit persistence contracts. `Unspecified` is invalid operational state; `NotRequired` is an explicit policy decision and cannot be inferred from missing credentials.

| Stage | Allowed transitions for one attempt |
|---|---|
| Signing | Prepared → Signing or FailedBeforeDispatch. Signing → Signed, Rejected or Unknown. Unknown → Signed or Rejected only with authoritative evidence; Unknown → Unknown records another inconclusive lookup. |
| Transmission | Prepared → Sending or FailedBeforeDispatch. Sending → Accepted, Rejected or Unknown. Unknown → Accepted/Rejected with evidence, or Unknown for another inconclusive lookup. |
| Payment operation | Prepared → Processing or FailedBeforeDispatch. Processing → Accepted, Rejected or Unknown. Unknown → Accepted/Rejected with evidence, or Unknown for another inconclusive lookup. |
| Print job | Prepared → Dispatching or FailedBeforeDispatch. Dispatching → HandedOff, Confirmed, Rejected or Unknown. HandedOff → Confirmed or Unknown. Unknown → Confirmed/Rejected with evidence, or Unknown for another inconclusive check. |

Accepted/signed/confirmed, rejected and failed-before-dispatch attempts are terminal. A corrected retry creates a new journalled attempt under an explicit host policy; there is no transition from unknown to prepared/sending/signing/processing/dispatching. Do not use `FailedBeforeDispatch` after a request might have left the process. A definitive rejection must establish that the operation did not happen, rather than merely indicate a communication error.

Cancellation is a separate `FiscalTransmissionOperation.Cancel` entry and preserves the original accepted issuance. Authorization, void and refund are separate `FiscalPaymentOperation` entries: an accepted void is not an accepted authorization. Step 7 must link these entries to the original payment reference and prevent a new authorization while the original outcome is uncertain.

Printer/spooler acceptance means `HandedOff`; it is not proof of physical output. Only a printer path with actual confirmation evidence may record `Confirmed`. Reprinting is an explicit new print job against the saved accepted document, without resubmission or signing.

`FiscalSignagePolicy` now owns the existing pure routing rules. Compatibility wrappers in Adapters retain their public APIs. This routing policy does not perform issuance, validate credentials or authorize retries. Its `hasSignature` argument must come from verified saved evidence, not `IsPrinted` or a placeholder.

## Invoice input changes

| Input | Behavior and limits |
|---|---|
| `InvoiceInput.VATPaymentSuspension` | Explicitly maps to the shared header and direct XML (after invoice type, before currency). iTimologia already supports this field in its mapper. Ilyda Direct still rejects it explicitly; no claim of Ilyda support is made. Default false leaves existing direct XML unchanged. |
| Trader email/phone/job; company phone/job | Map independently to the shared parties; issuer email never substitutes for customer email. Channel-specific serializers retain their own field policies. |
| `InvoiceBuildRequest.Delivery` | Ordinary delivery vehicle, departure and loading/delivery addresses. Requires a delivery document or invoice with delivery, complete addresses and dispatch details. Mutually exclusive with tanker `Movement`. It supports multiple ordinary lines without imposing tanker fuel-code/allocation restrictions. Output copies delivery addresses. |
| `InvoiceBuildRequest.SaleCorrelations` | Explicit opt-in for normal domestic wholesale goods (1.1), separate from credit `Parents` and tanker parent MARKs. Requires a different saved original sale/ordinary delivery, matching customer VAT, normal wholesale classification, accepted positive MARK and no cancellation evidence. Duplicate MARKs are collapsed. Serializers receive the resulting shared correlations. |
| Net/discount comments | Invoice `NettoAmount` and line `NetAmount` are before discount. The builder subtracts the discount exactly once and validates totals. The step 4 host mapper must normalize legacy saved amounts to this contract. |

The host must obtain parent evidence from authoritative saved data and verify the issuer/environment of correlations. The existing `TransmissionEvidence` is a mapping snapshot, not an independent verification service. Step 3 must retain historical scope so step 4 can make that check.

Existing `Build(InvoiceInput, ...)` callers keep their public signature and default behavior. New optional inputs are available through `InvoiceBuildRequest`. Delivery address validation now returns the intended validation error for missing addresses instead of a null-reference exception.

The same-VAT discrepancy (legacy direct 6.1 versus provider 1.1), FleetCard translation, service/mixed-item classification, and quantity at 15 degrees still require explicit host mapping decisions in step 4. This step does not silently infer 6.1 from a VAT match or change the tanker quantity convention. The new libraries still do not implement a self-supply 6.1 path; such documents must be held out of rollout until that path and its provider support are decided and tested. Do not cast the host payment enum to `InvoicePaymentType`.

## Step 3 database/store handoff

`IFiscalSigningStore` requires these behaviors from its SQL implementation:

1. Acquire a durable lease on `(DocumentKind, DocumentId)` across all routes/devices/environments, not just an instance lock. Issue a fencing token; reject stale owners. Persist `Signing` before calling the device. On restart, reconcile interrupted signing instead of dispatching it again.
2. `Begin` saves the immutable request as Prepared/revision 0 and checks the whole document history. Duplicate attempt IDs and existing Prepared, Signing, Unknown or Signed attempts block a new attempt even after device/settings changes.
3. `Save` checks the lease, expected revision, unchanged request/creation timestamp, next revision and allowed transition atomically. Append each revision, including response evidence and diagnostics, and update the current projection in the same transaction. Never erase an uncertain request or earlier lookup response.
4. `Load` returns the current attempt projections across historical scopes; `LoadHistory` returns every revision of a selected attempt. Preserve terminal records. No new attempt follows a rejection/local failure without an explicit host retry decision.
5. Persist document identity, attempt ID, route/device/environment/issuer scope, payload format/bytes/hash, state/revision, UTC timestamps, signature/QR/device reference and response/diagnostics. Apply a unique attempt ID, unique revision per attempt and database-enforced concurrency protection. Exact table/migration names are step 3 implementation work.
6. Preserve separate transmission, cancellation, payment and print records. A saved fiscal signature must not update print completion. Existing `MyDataInvoice`/invoice fields can remain compatibility projections during rollout; they cannot replace the attempt journal.

Database tests in step 3 must cover two workers, a stale lease holder, changed settings, crashes before/after external acceptance, revision conflicts, and preservation of all request/response evidence. Transition tests alone do not establish these properties.

## Verification

Run [Verify-InvoicingContracts.ps1](../../tests/Verify-InvoicingContracts.ps1) and [Verify-InvoicingCharacterization.ps1](../../tests/Verify-InvoicingCharacterization.ps1).

The contract suite builds both new libraries against .NET Framework 4.6 and exercises immutable payloads, identity/outcome invariants, the complete state-transition matrices, routing compatibility, new builder inputs and public offline serializers under `en-US` and `el-GR`. No HTTP client, SQL store, fiscal device or printer is invoked. It does not claim schema certification or external provider acceptance.

Verification: **37 contract tests passed** and **98 step 1 characterization tests passed**. The contract build emitted no warnings. The baseline retains its three existing unused-variable warnings. Physical device recovery, SQL locking, full application build/activation and UI behavior are for the corresponding integration steps.
