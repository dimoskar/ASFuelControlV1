# Invoice fiscal workflow status/action decision matrices

Status: implementation-aligned draft, 2026-11-05

## Purpose and decision rule

These matrices cover the shared invoice workflow actions that can affect POS payment, fiscal-device signing, invoice-provider submission, printing, or the related recovery paths. Configuration editing, myDiesel case management, and the internal steps of the heating/type-correction batch engines are outside the action list. The controlled provider-MARK cancellation used by correction batches is included because it changes an accepted provider document.

Every matrix cell contains exactly one of these values:

- **Allow**: this status dimension permits the action. Other matrices and blockers must still be checked.
- **Disallow**: this status dimension blocks the action.
- **Does not matter**: this status dimension is not an input to the decision. This is neutral, not permission.

Evaluate an action as follows:

1. Resolve the effective POS, fiscal-device, and invoice-provider statuses.
2. Find the action in all three matrices.
3. If any applicable cell is **Disallow**, block the action.
4. Require **Allow** from every status dimension that participates in the action. Ignore **Does not matter** cells.
5. Apply the blocker catalogue. A blocker always wins over an **Allow** cell.

This is intentionally fail-closed. It describes when an action is safe to perform, not merely when the current UI happens to enable a button or when a service call would return without changing anything.

## Effective status resolution

| Dimension | Effective status rule |
|---|---|
| POS payment attempt | `Not required` when the saved workflow has no POS route; `Not started` when it has a POS route but no Payment/Payment journal entry; otherwise the latest Payment/Payment attempt state. |
| POS reversal | `None` when no Payment/Void or Payment/Refund record exists; otherwise the latest reversal state. When a reversal exists, Matrix 1B is an additional mandatory gate. |
| Fiscal device | `Not required` when the saved workflow has no signing request; `Not started` when it has a signing request but no Signing entry; otherwise the latest Signing state. |
| Invoice provider | `Not required` when the saved workflow has no provider route; `Not started` when it has a provider route but no Submission entry; otherwise the latest Submission state. |

`Unspecified` is a contract sentinel, not an operational state, and must always fail validation rather than enter a matrix.

## Actions

| ID | Application action |
|---|---|
| A01 | Read status, evidence, or diagnostics |
| A02 | Start or continue normal invoice issuance |
| A03 | Recover POS payment state without a new charge |
| A04 | Explicitly retry a qualifying F4 POS payment |
| A05 | Change the invoice payment method |
| A06 | Start or resume void of an approved POS payment |
| A07 | Recover an uncertain POS void |
| A08 | Start or continue a full POS refund |
| A09 | Recover an uncertain POS refund |
| A10 | Start fiscal-device signing |
| A11 | Resume a persisted `Prepared` fiscal-signing request |
| A12 | Recover fiscal-device signing state without a new sign request |
| A13 | Retry a definitive fiscal-device failure with corrected input |
| A14 | Run an adapter-authorized automatic/deferred frozen-request signing retry |
| A15 | Send the initial invoice to the invoice provider |
| A16 | Retry a definitively rejected provider submission |
| A17 | Recover provider submission state without a new invoice send |
| A18 | Authorize a risky resend after an uncertain provider outcome |
| A19 | Execute an already authorized provider resend |
| A20 | Mark an approved POS payment as attached to an accepted invoice |
| A21 | Print the first copy |
| A22 | Reprint the saved document |
| A23 | Reconcile an uncertain print job |
| A24 | Submit an already-printed legacy invoice without reprinting or charging |
| A25 | Print a historical copy outside the shared fiscal workflow |
| A26 | Upload the accepted invoice PDF to Ilyda |
| A27 | Cancel an accepted provider invoice/MARK through a controlled correction workflow |

## Matrix 1A — POS payment-attempt status

The columns are the persisted `ProviderPaymentAttempt` states plus the two effective pre-attempt states. `VoidPending`, `Voided`, and `VoidFailed` are retained compatibility states on the payment attempt. New explicit void/refund work is also represented by the separate reversal record in Matrix 1B.

| Action | Not required | Not started | Prepared | SignaturePending | SignatureReady | Pending | Unknown | Declined | Approved | Invoiced | Mismatch | VoidPending | Voided | VoidFailed | Failed |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| A01 Read status/diagnostics | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A02 Continue issuance | Allow | Allow | Allow | Disallow | Allow | Allow | Allow | Allow | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Allow |
| A03 Recover POS payment | Disallow | Disallow | Allow | Allow | Allow | Allow | Allow | Allow | Allow | Allow | Allow | Disallow | Disallow | Disallow | Allow |
| A04 Retry qualifying F4 | Disallow | Disallow | Allow | Disallow | Allow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A05 Change payment method | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow |
| A06 Void approved payment | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A07 Recover POS void | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A08 Full POS refund | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A09 Recover POS refund | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A10 Start fiscal signing | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A11 Resume prepared signing | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A12 Recover fiscal signing | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A13 Retry fiscal failure | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A14 Automatic signing retry | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A15 Initial provider send | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A16 Retry provider rejection | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A17 Recover provider submission | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A18 Authorize provider resend | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A19 Execute authorized resend | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A20 Mark payment invoiced | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A21 Print first copy | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A22 Reprint saved document | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A23 Reconcile print | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A24 Submit printed legacy invoice | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A25 Print historical copy | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A26 Upload Ilyda PDF | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A27 Cancel provider invoice/MARK | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |

### Matrix 1B — POS reversal override

The reversal state is not a replacement for the original payment state; both records exist. Therefore this table is evaluated in addition to Matrix 1A whenever a Payment/Void or Payment/Refund record exists. `Rejected` and `FailedBeforeDispatch` are contract states but are not currently produced by the implemented void/refund services; they remain fail-closed.

| Action | None | Prepared | Processing | Unknown | Accepted | Rejected | FailedBeforeDispatch |
|---|---|---|---|---|---|---|---|
| A01 Read status/diagnostics | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A02 Continue issuance | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A03 Recover POS payment | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A04 Retry qualifying F4 | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A05 Change payment method | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A06 Void approved payment | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A07 Recover POS void | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow |
| A08 Full POS refund | Allow | Allow | Disallow | Disallow | Allow | Disallow | Disallow |
| A09 Recover POS refund | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow |
| A10 Start fiscal signing | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A11 Resume prepared signing | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A12 Recover fiscal signing | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A13 Retry fiscal failure | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A14 Automatic signing retry | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A15 Initial provider send | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A16 Retry provider rejection | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A17 Recover provider submission | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A18 Authorize provider resend | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A19 Execute authorized resend | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A20 Mark payment invoiced | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A21 Print first copy | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A22 Reprint saved document | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A23 Reconcile print | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A24 Submit printed legacy invoice | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A25 Print historical copy | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A26 Upload Ilyda PDF | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A27 Cancel provider invoice/MARK | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |

## Matrix 2 — Fiscal-device signing status

| Action | Not required | Not started | Prepared | Signing | Signed | Rejected | Unknown | FailedBeforeDispatch |
|---|---|---|---|---|---|---|---|---|
| A01 Read status/diagnostics | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A02 Continue issuance | Allow | Allow | Allow | Disallow | Allow | Allow | Disallow | Allow |
| A03 Recover POS payment | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A04 Retry qualifying F4 | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A05 Change payment method | Allow | Allow | Allow | Disallow | Disallow | Allow | Disallow | Allow |
| A06 Void approved payment | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A07 Recover POS void | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A08 Full POS refund | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A09 Recover POS refund | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A10 Start fiscal signing | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A11 Resume prepared signing | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A12 Recover fiscal signing | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Allow | Disallow |
| A13 Retry fiscal failure | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Allow |
| A14 Automatic signing retry | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Allow | Disallow |
| A15 Initial provider send | Allow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow |
| A16 Retry provider rejection | Allow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow |
| A17 Recover provider submission | Allow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow |
| A18 Authorize provider resend | Allow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow |
| A19 Execute authorized resend | Allow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow |
| A20 Mark payment invoiced | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A21 Print first copy | Allow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow |
| A22 Reprint saved document | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A23 Reconcile print | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A24 Submit printed legacy invoice | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A25 Print historical copy | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A26 Upload Ilyda PDF | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A27 Cancel provider invoice/MARK | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |

## Matrix 3 — Invoice-provider submission status

| Action | Not required | Not started | Sending | Unknown | Rejected | RetryAuthorized | Queued | QueueFailed | Accepted |
|---|---|---|---|---|---|---|---|---|---|
| A01 Read status/diagnostics | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A02 Continue issuance | Allow | Allow | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Allow |
| A03 Recover POS payment | Disallow | Allow | Allow | Allow | Allow | Allow | Allow | Allow | Allow |
| A04 Retry qualifying F4 | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A05 Change payment method | Allow | Allow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow |
| A06 Void approved payment | Disallow | Allow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow |
| A07 Recover POS void | Disallow | Allow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow |
| A08 Full POS refund | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow |
| A09 Recover POS refund | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow |
| A10 Start fiscal signing | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A11 Resume prepared signing | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A12 Recover fiscal signing | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A13 Retry fiscal failure | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A14 Automatic signing retry | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A15 Initial provider send | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A16 Retry provider rejection | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow | Disallow |
| A17 Recover provider submission | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Allow | Allow | Allow |
| A18 Authorize provider resend | Disallow | Disallow | Allow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A19 Execute authorized resend | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Disallow | Disallow |
| A20 Mark payment invoiced | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow |
| A21 Print first copy | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow | Disallow | Allow |
| A22 Reprint saved document | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A23 Reconcile print | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A24 Submit printed legacy invoice | Disallow | Allow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow |
| A25 Print historical copy | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter | Does not matter |
| A26 Upload Ilyda PDF | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow |
| A27 Cancel provider invoice/MARK | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Disallow | Allow |

## Blocker catalogue

| ID | Applies to | Blocking rule |
|---|---|---|
| B01 | All mutating actions | A failed status/history read blocks every mutation. Diagnostics may remain available from already-read evidence. |
| B02 | All mutating actions | Failure to acquire the durable document lease, stale revisions, a stale expected POS-attempt ID, or changed workflow ownership blocks the action. |
| B03 | Normal issuance, signing, provider submission, POS authorization, printing | Existing Cancellation history requires explicit reconciliation or its owning correction workflow. It is never ignored by a normal retry. |
| B04 | Continue issuance, first print, reprint | Print `Dispatching` or `Unknown` permits only print reconciliation. `HandedOff`/`Confirmed` permits another print only as an explicit reprint with an audit reason. |
| B05 | A04 | F4 retry requires a sale, the exact retained single-F4 or F4/F2/0E evidence policy, the expected latest attempt ID, no contradictory event, and no Signing, Submission, Cancellation, or Print history. `Prepared`/`SignatureReady` is allowed only for the already-linked replacement created by that policy. |
| B06 | A02, A03, A04 | POS `Unknown` permits lookup/recovery, not another authorization. `Mismatch` remains blocked unless retained evidence can be reclassified as an incomplete response. |
| B07 | A06, A07 | Void requires the original payment to remain `Approved`, no non-rejected provider submission, and no Cancellation or Print history. A saved `Processing`/`Unknown` void is lookup-only. A `Prepared` void resumes only through the explicit void action with its saved request. |
| B08 | A08, A09 | Full refund requires one original `Invoiced` sale, original provider `Accepted`, no original Cancellation history, the same provider route, customer, payment type, and full amount, and no competing void/refund. Partial and multiple refunds are unsupported. |
| B09 | A05 | Payment-method change requires a saved positive invoice number, not printed, no saved signature/QR/MARK, and only terminal-negative history: Signing `Prepared`/`Rejected`/`FailedBeforeDispatch`, Submission `Rejected`, ProviderSession `Rejected`/`Cancelled`/`Expired`, and Payment `Declined`/`Failed`. |
| B10 | A10–A14 | Signing `Signing`/`Unknown` never authorizes a newly rendered sign request. Recovery uses the original request. Automatic/deferred resend is allowed only when the historical adapter explicitly accepts the exact frozen request and its bounded timing/count policy. |
| B11 | A18, A19 | Risky resend is provider-specific: iTimologia requires `Unknown` plus retained HTTP 404 lookup evidence; direct Ilyda permits `Unknown` or interrupted `Sending`. myDATA has no operator-authorized resend path. Operator identity, reason, and duplicate-issuance warning are mandatory. |
| B12 | POS, signing, provider actions | Historical provider, environment, issuer, branch, terminal, endpoint, device, request bytes, and attempt identity remain immutable except for the explicit same-issuer correction rules. Missing historical credentials/profile blocks external I/O. |
| B13 | A17, A21 | Provider `Queued` is printable only for the supported direct-Ilyda TF2 offline path with the required saved metadata. `QueueFailed` permits recovery/reconciliation, never first print or a fresh submission. |
| B14 | A15–A19, A21 | Accepted issuance with incomplete print metadata stays accepted. Recover metadata; do not send the invoice again merely to obtain printable fields. |
| B15 | A24 | Legacy printed submission requires an active shared runtime, a positive saved number, `IsPrinted = true`, no legacy MARK/signature/QR/shared history, no POS payment method, and no required fiscal-device signing. It never prints or charges. |
| B16 | A25 | Historical-copy printing is allowed only when no shared fiscal ownership/history exists and the host invoice is already numbered and printed. It creates no signing, payment, or provider attempt. |
| B17 | A26 | PDF upload requires direct Ilyda, a durably `Accepted` submission, the immutable accepted route, and a valid PDF payload. It must not alter accepted fiscal evidence. |
| B18 | A27 | Provider/MARK cancellation is not a generic retry action. It is allowed only inside the correction workflow that owns the original accepted evidence and records the cancellation outcome. POS reversal is a separate explicit decision. |
| B19 | A02, A21 | myDiesel redemption, correction-batch ownership, or another workflow-specific pending stage may block continuation/printing even when all three matrices allow it. |
| B20 | A02, A15–A21 | A connection/profile problem before the first external attempt blocks dispatch. After an attempt exists, the saved historical route and its resolvable credential profile control recovery. |
| B21 | A02, A15–A20 | Provider rejection/uncertainty never causes automatic POS void/refund. Payment approval and provider issuance are independent durable outcomes. |
| B22 | POS signature preparation | Direct-Ilyda ProviderSession `Requesting`, `Unknown`, or `CancelPending` blocks a new signature/payment dispatch. `Ready` can feed authorization; `Rejected`, `Cancelled`, or `Expired` permits replacement only when the main payment workflow and explicit retry rules also allow it. |

## Supporting states that are not primary matrix axes

These states exist, but treating them as additional columns in one of the three primary matrices would be incorrect because they can coexist with the primary status.

| State owner | States | How it participates |
|---|---|---|
| POS reversal record | `Prepared`, `Processing`, `Accepted`, `Rejected`, `Unknown`, `FailedBeforeDispatch` | Evaluated through Matrix 1B in addition to the main payment attempt. `Unspecified`/`NotRequired` are contract values, not persisted reversal attempts. |
| Direct-Ilyda provider POS-signature session | `Requesting`, `Ready`, `Rejected`, `Unknown`, `CancelPending`, `Cancelled`, `Expired` | Internal blocker B22; it is not the invoice-provider submission result. |
| Direct-Ilyda deferred payment submission | `Sending`, `Unknown`, `Accepted` | A nested field on the provider POS session, not the invoice Submission journal state. |
| Print | `Prepared`, `Dispatching`, `HandedOff`, `Confirmed`, `Unknown`, `FailedBeforeDispatch`, `Rejected` | Governed by blockers B04/B13 and the print action rows. A fourth print matrix would be appropriate if print becomes a first-class requested dimension. |
| Cancellation | The cancellation operation has its own journal/history | Any unresolved or completed cancellation history is a global blocker for ordinary issuance; correction workflows own its resolution. |

## Is the three-matrix approach usable?

Yes, if it is used as a two-layer decision model:

1. the three matrices provide fast, deterministic status gates; and
2. the blocker catalogue handles history, route identity, evidence quality, ownership, and cross-document conditions.

The three matrices alone are not sufficient. A single latest status cannot represent overlapping records such as an `Approved` payment plus an `Unknown` void, and it cannot express evidence-sensitive rules such as the exact F4 retry sequence or iTimologia 404 resend authorization. Without Matrix 1B and the blockers, the result would look complete while permitting duplicate charges, duplicate invoices, or repeated fiscal signing.

This structure is suitable for:

- a single central `CanExecute(action, workflowState)` policy;
- UI button enablement and consistent service-side guards;
- table-driven unit tests generated from every non-neutral cell; and
- auditing differences between UI availability and backend enforcement.

Before converting it into production code, review the deliberately strict cells for `SignaturePending`, `Mismatch`, legacy print submission, PDF upload, and controlled MARK cancellation with the product owner. Some of these are backend capabilities or fail-closed states rather than currently exposed general UI actions.

## Implementation sources

- `Exedron.Invoicing.Core/PaymentAttemptPersistence.cs`
- `Exedron.Invoicing.Core/FiscalProcessingStates.cs`
- `Exedron.Invoicing.Core/FiscalSigningCoordinator.cs`
- `Exedron.Invoicing.Core/ProviderPaymentCoordinator.cs`
- `ASFuelControl.Invoicing/ProviderPaymentService.cs`
- `ASFuelControl.Invoicing/ProviderPaymentCompensation.cs`
- `ASFuelControl.Invoicing/ProviderSubmissionService.cs`
- `ASFuelControl.Invoicing/InvoiceWorkflowService.cs`
- `ASFuelControl.Invoicing.Persistence/SqlProviderPaymentStore.cs`
- `ASFuelControl.Invoicing.Persistence/SqlPaymentCompensationStore.cs`
- `ASFuelControl.Invoicing.Persistence/SqlSubmissionStores.cs`
- `ASFuelControl.Invoicing.Persistence/SqlIlydaPosStore.cs`
- `ASFuelControl.Windows/Threads/SharedInvoiceRuntime.Status.cs`
- `ASFuelControl.Windows/UI/Controls/FiscalDocumentPanel.cs`
