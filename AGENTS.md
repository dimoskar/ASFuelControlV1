# Repository instructions

## Deployment packaging

- For every Normal and Full deployment, overwrite the packaged Tatsuno
  protocol files with the canonical files from
  `Deployments/tatsuno protocol`.
- Apply the overwrite after copying the Release artifacts, then verify that
  the packaged Tatsuno files have the same SHA-256 hashes as the canonical
  files.

## Invoice submission idempotency and recoverability

- A duplicate invoice must never be created. Every invoice provider and
  myDATA must use its stable invoice identity and destination guards so a
  repeated request resolves to the same invoice rather than creating another
  invoice.
- An invoice must never be left in a state where the user has no safe action
  available.
- Every submission outcome must provide an explicit operator path:
  - accepted: continue the workflow and allow printing;
  - rejected or authoritatively confirmed as not stored: allow correction and
    controlled resend with the same stable identity;
  - unknown: provide manual recovery/status lookup; when lookup confirms the
    invoice is not stored, allow resend with the same stable identity; when a
    send returns an error, query or recover that invoice again;
  - confirmed as already stored: adopt the recovered result and continue.
- Limits on automatic retry or recovery may stop background processing, but
  they must never disable the corresponding manual recovery action.
- A timeout, transport error, empty response, or failed recovery attempt is
  not evidence that the invoice was not stored. Preserve the invoice and keep
  an operator-visible resolution action available.
