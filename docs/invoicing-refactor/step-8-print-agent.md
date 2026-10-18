# Step 8 — PrintAgent workflow integration

The configuration-file interface described here remains the version-1 fallback. [Step 9](step-9-ui-configuration.md) adds database-backed global settings, reviewed per-type transmission/signing policies, local unsigned printing and native status/recovery screens.

PrintAgent can now use the shared provider, POS and fiscal-device services through an explicitly configured rollout. Fiscal acceptance and printer handoff have separate durable records. The switch is **off by default**; no production options, credentials, database or devices were changed during implementation.

## Execution and recovery

`InvoiceWorkflowService` holds one SQL document lease across preparation, signing/payment, submission, compatibility projection and print-job dispatch. `SignedDocumentWorkflowService` does the equivalent for supported balances and tank fillings. Nested stores share that lease, including non-invoice document kinds.

First issuance captures a versioned snapshot containing the detached mapped invoice, provider/payment/device route, rendered invoice text and explicit printer settings. SQL stores its SHA-256 and verifies it on every read. A later call does not remap edited entities. Before any fiscal attempt exists, the common account/subscription and credential profile may refresh from current settings within the same provider/environment/issuer/endpoint; prior connections remain in the JSON audit history. Once an attempt exists the connection is fixed. See [step 9](step-9-ui-configuration.md) for the guarded refresh after authentication failures. Existing fiscal history without a workflow snapshot requires migration/reconciliation; it is not automatically adopted.

For direct myDATA following Samtec, the orchestrator verifies the original Signed attempt, payload and matching issuer/branch/environment before submission. It does not enable arbitrary provider-after-signing combinations. Card/IRIS invoices enter the step-7 payment service; payment approval and invoice acceptance remain separate. Unknown/rejected states retain the existing recovery rules and do not trigger another authorization or signature.

The first printable result contains the frozen text plus accepted signature, MARK, UID, provider verification details and QR. The Windows renderer saves PNG pages before printer dispatch. Printer name, paper dimensions, font size and copies are frozen too. This is an explicit new print format, not a re-execution of Telerik reports or raw/TCP commands. Reprints reuse the first saved pages and preferred destination; a changed customer, total or report template cannot change that document. If the requested printer is missing or undefined, printing selects an installed installation default, then the Windows default. A later fallback preserves the original destination record, pages, geometry and copies; the handoff receipt records the printer actually used. Unknown print outcomes still require reconciliation before any retry.

Print jobs follow `Prepared → Dispatching → HandedOff`. A local validation failure is `FailedBeforeDispatch`. Exceptions during dispatch, missing handoff evidence, or an interrupted Dispatching state become `Unknown`; polling and even an explicit reprint cannot dispatch another copy until reconciliation. A failure saving the handoff leaves Dispatching. `HandedOff` means the Windows print API accepted the job, not verified physical output. `FiscalPrintService.Reconcile` requires an explicit evidence string to confirm printing or establish rejection.

Once a print job exists, subsequent print processing invokes no provider, POS or fiscal signer, and performs no rendering. An explicit reprint creates another attempt with the original payload and a reason. An ordinary repeated processing call returns the existing handoff. Projection failures cannot cause duplicate issuance or printing.

## Windows entry points

- Background polling, `SignInvoice`, direct/internal printing, `SendMyDataToPrint`, the legacy myDATA submission callback and the TCP entry point route owned invoices through the shared workflow.
- `InvoiceFormEx` uses an explicit manual action. Already-owned documents bypass saving/recalculation of edited screen values. A new document saves pending edits before first issuance. A repeated manual print records an operator reprint reason; automatic callers do not imply a reprint.
- The original form Print button stays available when issued fields are locked. It runs the same `SharedInvoiceRuntime.Process` path for new and unfinished invoices, including numbering before submission. The action runs off the UI thread with editing, closing and duplicate clicks blocked until completion; the form then reloads the saved number and status. Completed print jobs require a copy reason, while failed or interrupted processing uses the existing recovery rules without silently becoming a reprint. An unknown printer outcome remains blocked until reconciliation.
- Legacy signature callbacks and exception-recovery lists cannot overwrite a shared document. The older `PrintHandler.SignInvoice` entry point is guarded too; no active production caller of that helper was found.
- Configured Balance and TankFilling routes bypass legacy report export/device dispatch and use the shared signing and saved-page flow. Balance PrintDate is set after handoff. TankFilling SignSignature remains its legacy queue-completion projection and is set after handoff; the actual fiscal signature is already durable in Signing history before then.
- Disabling `SharedInvoicingEnabled` does not return an owned document to legacy issuance. Ownership is checked in SQL, and its saved workflow still requires historical profiles.

For invoices, `InvoiceSignature`, QR and accepted `MyDataInvoice` fields are compatibility projections. `IsPrinted` is updated from the print outcome, never from signing or transmission success. The shared workflow does not encode print commands in `MyDataInvoice.Errors` or use legacy status fields as its authoritative state. Full status/reconciliation UI and migration of existing records remain step 9.

## Numbering

The new path allocates numbers before opening the mapping/rendering ORM context. It holds an update/serializable lock on the invoice-type counter and assigns earlier unnumbered invoices with saved lines in transaction-date/GUID order, through the requested invoice. Only records within the configured cutover period and without legacy print/signature/QR/myDATA history participate. Counter and invoice updates commit together. Repeated calls preserve allocated identities; a counter behind issued history is rejected. Other legacy application versions must be stopped during cutover because they do not participate in the new workflow locks.

## Configuration and supported scope

Set host Option `SharedInvoicingConfiguration` to an **absolute path** to the reviewed JSON configuration and enable `SharedInvoicingEnabled` only during controlled cutover. Settings are captured once per process; restart workers after configuration changes. The [example configuration](step-8-configuration.example.json) contains synthetic values, an example invoice-type GUID and a placeholder printer, and is not deployable as supplied. Retain old credential/device profiles for recovery.

Configuration has:

- `Version: 1`, `FirstInvoiceDate`, and an explicit `InvoiceTypes` dictionary keyed by real invoice-type GUID.
- Per-type Provider and optional Payment or Signing route, plus Printer settings. Provider and Samtec may be combined only for direct myDATA with matching scope. Payment is selected only for card/IRIS invoices.
- `Mapping` wholesale codes, country catalog and any required item/correlation policy. Company identity/address fields are captured from the same host options used for invoice text. Parent MARK evidence comes from accepted shared submissions.
- `Credentials` entries matching the complete historical ProviderRoute. Values such as `SubscriptionKeyOption` name host Option keys; configuration and workflow snapshots do not contain the actual secret.
- `Devices` containing versioned Samtec scope/endpoint profiles. Optional `Documents` entries select Balance/TankFilling routes; absent entries retain their legacy path until explicitly configured.

Supported invoice transports are direct myDATA, iTimologia, direct Ilyda, the supported GBridge payment combinations from step 7, and Samtec cash/credit signing. Shared printing uses the requested installed Windows printer, otherwise the installation default, otherwise the Windows default. Old folder/TCP strings do not execute commands; they fall back when they do not name an installed printer. Missing printer profiles use the standard page defaults. The settings check reports one printer error only if a required route has neither a valid selected printer nor a usable default. Page layout, paper and QR output must be checked on the target printer before rollout.

Unsupported paths fail before new external dispatch: non-fiscal/no-route invoice policy, Sign A, direct Mellon/Viva migration, Samtec combined POS, Ilyda IRIS, partial/deferred refunds, tank-control invoices (`ForcesDelivery`) and special cancellation documents (official code `215`). The legacy `IsCancelation` flag alone does not exclude financial credit notes; these use Core's credit mapping and original-invoice/MARK validation. Credits paid by card/IRIS require the dedicated refund workflow and cannot start a fresh POS sale. Full refunds/voids retain their dedicated step-7 service entry points and are not inferred by PrintAgent. Legacy cancellation of shared invoices is blocked pending the journalled cancellation UI/workflow. Alerts, titrimetry and daily closing retain their legacy paths; their placeholder signs are not promoted into accepted shared fiscal evidence. Balance email/report-export side effects are not part of the new print transport.

An existing print flag alone no longer prevents shared processing. The Invoice form offers **Διαβίβαση** for a saved, numbered, printed invoice with an electronic route and no legacy submission/signature/QR evidence. This explicit action also permits a printed invoice before the cutover date, preserves its number/series/date, and runs the normal durable submission workflow with printing disabled. It does not require an installed printer. Automatic processing retains the cutover boundary, and printed number-zero records cannot be renumbered. Previously printed card/IRIS or fiscal-device routes require reconciliation instead of another charge/signature. Existing legacy submission rows (including failed/empty summaries) still require outcome review; they are never silently resubmitted or treated as accepted.

## Database

Migration **58**, wired into Resources and `Program.CurrentDBVersion`, adds:

| Table | Purpose |
|---|---|
| `FiscalInvoiceWorkflow` | Frozen invoice and print input, provider connection with pre-attempt refresh history, hash, UTC timestamp and fencing token. |
| `FiscalDocumentWorkflow` | Equivalent snapshots keyed by document kind and ID for balances/tank fillings. |

Existing FiscalAttempt/FiscalAttemptRevision tables retain signing, submission, payment and print-job history. No legacy table/column is removed. Numbering uses existing Invoice/InvoiceType columns. All migration and runtime batches use SQL Server 2008 R2 syntax; SQL-100 parser and isolated compatibility-level-100 tests cover the added statements. Native SQL Server 2008 R2 runtime validation remains a deployment check.

## Verification

Run `tests/Verify-InvoicingWorkflow.ps1`. The suite uses real host/Core/adapters/SQL stores, detached real Data entities, fake HTTP/printer transports and a newly created disposable LocalDB database. It covers persist-before-dispatch boundaries, unknown printer outcomes, explicit reconciliation and reprints, immutable pages/routes, projection failures, signature-to-myDATA composition, POS approval followed by print failure, concurrent callers, and transactional chronological numbering. It also exercises the production Windows PNG renderer with Greek text and QR without opening a printer.

Manual-print follow-up verified on 2026-10-16: **62 workflow checks** passed, including unfinished saved workflows for all three channels. Native UI checks verified the original Print button remains enabled for owned invoices, invoice editing remains locked, new/unfinished invoices call normal processing, duplicate clicks are blocked, and status-read failures disable actions. The diagnostic Windows compile passed; the native invoice-form rendering was visually inspected. No application database or live provider/printer was used.

Printer-fallback follow-up verified on 2026-10-16: **78 workflow/configuration checks**, native UI checks and the diagnostic Windows compile passed. Synthetic printer inventories cover installed per-type priority, missing/blank/folder names, installation and Windows defaults, absent profiles, no usable default, and preservation of historical geometry/copies/font. The printer-settings screen was visually inspected. No physical print or application database access occurred.

Credit/printed-invoice follow-up verified on 2026-10-16: **88 workflow/configuration checks** and **104 mapping checks** passed. All three channels submit a printed-only invoice once without rendering, printing or signing, preserve its saved identity, and reuse accepted evidence. Tests cover individual special-flow restrictions, pre-cutover explicit submission, zero-number rejection, legacy evidence, inactive configuration and duplicate-payment/signing prevention. Native UI checks passed with eleven renders; the printed-invoice summary and enabled transmission action were visually inspected. The diagnostic Windows compile passed. No SQL/schema changes, application database access or live dispatch occurred.

The ordinary Windows solution build is blocked by the pre-existing `ControllerTypeEnum.Unimep` error in ASFuelControl.Unimep. A separate diagnostic Windows compile uses `tests/Invoicing.Workflow/WindowsCompile.targets` to omit that unused broken reference only for the compile check. This does not change the production project dependency or establish a deployable full-solution build. Existing COM registration/reference warnings also remain. Provider/device/printer certification and a deployed ORM/database smoke test are still required before enabling the switch.

Verified on 2026-10-15: **28 workflow checks**, plus **37 Core, 80 mapper, 98 legacy characterization, 26 persistence/SQL-100, 32 provider, 55 signing and 44 payment checks** passed: **400 total**. The production Windows page renderer's Greek text/QR preview was visually inspected. The diagnostic Windows source build passed; the ordinary solution build retains the Unimep blocker described above. All private test databases/instances were removed. No live provider, gateway, signer, physical printer or application database was used. Whitespace checks passed.
