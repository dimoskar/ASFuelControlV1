# GBridge deployment certification

Use this checklist per provider account, POS mode, terminal and environment before enabling the shared workflow in production. This scope intentionally excludes `acq_id=2`, split or mixed payments, partial/multiple refunds, deferred Ilyda payments and cashier payment-account changes.

## Configuration and evidence

- Record application build, provider (`iTimologia` or `Ilyda.Direct`), test/production environment, provider account/subscription, POS mode, terminal ID and terminal firmware/version.
- Confirm the configured provider issuer VAT/branch and the POS route belong to the same environment.
- Confirm the diagnostics screen can find attempts by date, state, invoice, transaction UID, transaction ID, RRN and TID.
- Confirm `SIGNATURE` and input types `1`, `2`, `3` and `4` appear when exercised, and that displayed request/response evidence redacts credentials, tokens, signatures, receipts, PAN/card data and authorization values.

## Sale and issuance

- Approve a card sale and verify one authorization, one accepted invoice, the same amount/TID/payment type throughout, and a printed fiscal document.
- For iTimologia Mellon, approve an IRIS payment and verify payment type `1`; verify IRIS is blocked for unsupported modes and for Ilyda.
- Decline a sale and verify a new authorization is possible only after the explicit retry action and uses a new transaction UID.
- Interrupt each boundary (signature request, authorization, provider submission and print handoff), restart, and verify recovery never creates a second authorization or a second invoice submission.
- Exercise incomplete/mismatched terminal replies (UID, TID, amount, transaction type and payment type) and verify issuance remains blocked until lookup evidence resolves the original transaction.

## Void

- Produce an approved payment followed by a definitive invoice rejection, then use the administrator-only void action with a reason.
- Verify the void record points to the original payment attempt and that the terminal confirms the original transaction ID and amount.
- Interrupt the void after dispatch and verify **Ανάκτηση ακύρωσης** performs lookup-only recovery with the saved transaction identity.
- Verify void is unavailable after accepted/uncertain issuance, cancellation history or print history.

## Full refund

- Create one related credit for an accepted, fully paid original invoice with the same customer, payment method and full amount.
- Use the administrator-only full refund action and verify one refund authorization, accepted credit submission, print handoff, and links to the original invoice/payment attempt.
- Interrupt refund authorization and submission separately; verify **Ανάκτηση επιστροφής** is lookup-only and **Πλήρης επιστροφή** continues the saved operation without a second refund.
- Verify a second credit, unrelated credit, different customer, different payment method, different amount and partial refund are all rejected before transport.

## Sign-off

- Export or capture the redacted attempt/event evidence for every scenario above.
- Record the operator, reviewer, date, provider support reference and terminal/acquirer confirmation.
- Enable production only after every configured provider/POS-mode/terminal combination passes; a pass for one terminal or mode does not certify another.
