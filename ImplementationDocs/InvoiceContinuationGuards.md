# Invoice continuation guards

## Scope

The continuation policy is provider-neutral and lives in
`Exedron.Invoicing.Core`. Provider-specific identity, payload serialization,
recovery, and cancellation behavior remain in the adapters and host workflow.

The common contract consists of:

- `IInvoicePayloadComparer`, which classifies two payloads as `Equivalent`,
  `Changed`, `IdentityChanged`, or `Unknown`.
- `IInvoiceContinuationGuard`, which returns `Block`, `Recover`,
  `ContinueExisting`, `Resend`, or `CancelAndReissue`.
- `InvoiceCancellationStrategy`, which distinguishes direct MARK cancellation
  from a linked cancellation document.
- `IInvoiceCancellationHandler`, which is the common boundary for executing a
  cancellation after the guard has authorized it.

## Provider identity and cancellation

| Route | Duplicate identity | Cancellation strategy |
| --- | --- | --- |
| Direct myDATA | Issuer VAT, issue date, issuer branch, invoice type, series, AA, and `invoiceVariationType` when applicable | Cancel the accepted MARK before reissue |
| iTimologia | Stable application `InvoiceId` (`ext_system_id`) | Issue a linked cancellation document; the user must select one of the configured cancellation invoice types |
| ILYDA | Submission is tracked by the stable application `InvoiceId`; a resend can return an already-existing-invoice result, while the adapter also verifies its provider invoice number | Issue a linked cancellation document; the user must select one of the configured cancellation invoice types |

The provider catalogue exposes this distinction through
`InvoiceProviderDefinition.CancellationStrategy`. The existing invoice catalog
cancellation command already limits the selection dialog to configured
`Cancelation` transformations, so a provider cancellation cannot silently pick
an invoice type.

## Decision rules

1. A first submission, a failure before dispatch, a confirmed provider
   not-found result, or an already-cancelled invoice can be submitted again.
2. A definitive rejection can be retried with corrected values. Direct myDATA
   retains its seven-field fiscal identity unless an audited correction exists.
   Provider routes retain their stable application `InvoiceId`; their own
   adapter restrictions still apply.
3. `ReconciliationRequired`, or any state whose payload cannot be compared
   safely, requires recovery and cannot be bypassed by consent.
4. An unresolved `Unknown` or `Sending` attempt can be resent only after user
   consent and only when the semantic payload is equivalent. iTimologia offers
   this consent only after its lookup has returned HTTP 404. ILYDA can safely
   resend the same provider identity and handle an already-existing response.
5. Changed values with an unresolved outcome are blocked until recovery.
   Authorizing a resend does not consume the recovery path: if the final
   preflight detects a change, the authorization is revoked back to the prior
   `Unknown` or `Sending` state and the revocation is audited.
6. If recovery confirms an active invoice with different values, explicit user
   consent is required. Direct myDATA must cancel its MARK. iTimologia and
   ILYDA must also receive the user-selected cancellation invoice type before
   the guard returns `CancelAndReissue`.
7. An accepted invoice with equivalent values is continued from its stored
   evidence and is never submitted again.

## Semantic payload comparison

`SemanticInvoicePayloadComparer` compares JSON and XML values instead of raw
document text:

- object property names and XML namespace prefixes do not depend on ordering;
- collection ordering is ignored, but collection cardinality and values must
  match;
- numeric, boolean, ISO date, and ISO date/time representations are normalized;
- missing, null, and empty optional values are equivalent;
- fields present in only one document are ignored, so transport additions do
  not make an old persisted request incompatible;
- two non-empty documents with no common fields are not considered equivalent;
- base64-encoded XML embedded in an ILYDA JSON value is compared semantically;
- identity is evaluated separately, so an optional myDATA deviation type that
  appears or disappears is still an identity change even though one-sided
  optional fields are otherwise ignored.

The comparer reports up to 50 changed paths for diagnostics. The guard consumes
the classification; adapters continue to own their exact provider lookup and
response validation.

## Enforcement points

- The Windows resend action regenerates the current model and runs the guard
  before saving operator authorization.
- `InvoiceWorkflowService` repeats the check under the document lease before a
  regenerated snapshot replaces the frozen workflow.
- `ProviderSubmissionService` compares the persisted provider request with the
  newly mapped request immediately before dispatch. This protects non-UI
  callers as well as the desktop workflow.
- The myDATA, iTimologia, and ILYDA adapters use semantic comparison when they
  inspect their own persisted attempts and recovery results.
- myDATA reconciliation includes `invoiceVariationType` in its seven-field
  identity match.

## Verification

The provider suite covers semantic JSON/XML comparison, myDATA deviation
identity, provider cancellation-type selection, unresolved consent, recovery,
and legacy retry protections. The workflow suite covers final preflight,
authorization revocation, immutable recovery evidence, and provider-specific
rejected retry behavior.
