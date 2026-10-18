# Step 10 validation and controlled rollout

Step 10 adds release verification, durable rollback protection, safer invoice editing/deletion and a replacement for the destructive MyDataCorrection utility. Production cutover and final removal of the installation's old transports remain conditional on a successful full build and installation acceptance. Shared invoicing has not been enabled on an application database.

## Rollback behavior

`SqlFiscalOwnershipStore` treats a frozen invoice/document workflow **or any fiscal journal attempt** as shared ownership, irrespective of the attempt's state. This includes attempts created before PrintAgent integration. They remain visible as shared records in the catalog; a missing workflow requires reconciliation instead of legacy sending.

At startup, the runtime reads the append-only configuration history and journalled document kinds. Once shared invoice processing has been activated, clearing the enable checkbox pauses new invoices. It does not restore legacy numbering, provider initialization or transmission. Existing snapshots retain their original channel, environment, POS mode, signing payload and saved pages. Account/subscription and credential references follow the latest saved settings only before any fiscal attempt exists, as described in [step 9](step-9-ui-configuration.md); all started attempts retain their original connection. Balance/tank-filling protection likewise survives disabling their previously activated routes.

Routing, policy and device settings remain fixed for a running process. Stop **all** workers before changing these settings and restart them together. Account/key corrections for invoices without any fiscal attempt are read from the latest saved configuration without a restart. Old binaries do not implement this protection and must not run alongside the new version. Do not delete configuration revisions, workflow snapshots, journals or historical credential profiles to undo activation.

## Host actions

- The invoice editor holds the same SQL document lease as issuance through its ordinary save/commit. Shared documents cannot be changed or deleted through this path.
- Catalog deletion rechecks shared ownership under that lease and checks current legacy fiscal evidence before deleting. An accepted invoice whose printer failed remains protected even when `IsPrinted` is false.
- Discarding related invoices holds leases through deletion and no longer rewinds invoice counters. Issued numbers are not made available for reuse.
- Legacy cancellation transformations are blocked for shared processing before they create replacement/cancellation relations. The dedicated cancellation workflow is still required.

These are host application guards; they do not authorize manual SQL edits or make an older executable safe against the new database.

## Historical evidence utility

`MyDataCorrection` now uses `SqlFiscalMigrationStore` to show a read-only, date-filtered report with stable pagination and JSON export of the displayed page. It labels shared workflows, shared attempts needing reconciliation, legacy MARK/cancellation evidence, other legacy outcomes and invoices with no recorded evidence. Multiple legacy rows, the saved print flag and an original zero-AA payload are visible separately.

The utility does not infer provider/environment, fabricate acceptance, import missing attempts, call cancellation APIs, display credentials or delete `MyDataInvoice` records. Recovery of shared records remains in the application's invoicing catalog. Legacy fiscal evidence requires a reviewed reconciliation procedure before adoption; a legacy MARK alone is insufficient. The Invoice form now separately permits explicit transmission of printed-only records with no fiscal evidence and a saved positive number, subject to the restrictions documented in steps 8 and 9. Existing historical records do not block installation activation.

Removed from this utility: the old `Exedron.MyData` binary reference, cancellation/delete implementations and its unused credential-decryption helper. Database migration 59 is required; no step-10 schema migration is needed. All new SQL statements use SQL Server 2008 R2 syntax and participate in the SQL-100 parser checks.

## Repeatable verification

The solution must contain all five shared invoicing projects, including `ASFuelControl.Invoicing` and `ASFuelControl.Invoicing.Mapping`. Referencing their project files from Windows without adding them to the classic Visual Studio solution can leave the IDE compiling against old DLLs. All five now have matching Debug/Release build mappings for every solution platform, and their production project references include the target project GUIDs. Reopen/reload the solution after this project-list change.

Run from the repository with Visual Studio MSBuild, LocalDB and Python with lxml available:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingRelease.ps1 -Python "path\to\python.exe"
```

The script runs all eight regression suites sequentially, validates generated XML against the supplied AADE 2.0.0 schemas now stored in the repository, builds the evidence utility and full solution, compiles the Windows diagnostic target, and checks/renders the native forms. It writes individual logs and `tests/Invoicing.Release/output/verification.json`. Any failed check produces a failing command; the diagnostic build cannot substitute for a full release build.

Additional workflow tests run separate processes that terminate during submission to each channel, after POS approval and during printer dispatch. A restarted workflow must preserve the durable attempts, avoid another POST/charge/print and require print reconciliation. Other checks cover edit/issuance exclusion, journal-only ownership, activation history after disabling, historical classification and pagination at equal timestamps. Provider/device/printer transports are synthetic; SQL tests create disposable private LocalDB instances at compatibility level 100.

Installation checks remain listed as pending in the generated report even when automated checks pass. The report is verification evidence, not a deployment command.

Verified on 2026-10-16: **478 regression checks** passed (37 contracts, 102 mapping, 44 providers, 57 payments, 26 persistence, 55 signing, 59 workflow/rollout and 98 characterization), plus **25 XML schema checks**. The evidence utility build, native UI checks/renders and Windows diagnostic compilation passed. The full solution build failed at the existing `ASFuelControl.Unimep/Controller.cs:38` reference to missing `ControllerTypeEnum.Unimep`. Consequently the release script correctly reported failure and left production cutover blocked. Native SQL Server 2008 R2 and live installation acceptance have not been performed.

## Installation sequence

1. Resolve full-build failures and produce the reviewed release package. Record its build/version and selected installation, provider/environment and POS/device/printer inventory.
2. Stop all application instances, PrintAgent workers and old correction/resend utilities. Take a consistent backup of the application database, binaries and retained configuration/credential storage. Rehearse restoration on an isolated machine.
3. Restore a copy on **native SQL Server 2008 R2** and apply migrations 57–59. Verify the existing OpenAccess application against that copy. SQL grammar checks and modern LocalDB compatibility level 100 do not replace this check.
4. Use the evidence report and existing history to account for accepted, cancelled and uncertain historical documents/payments. Export the reviewed pages with their date range; the report is a bounded view, not an atomic backup. Never clear a MARK/signature to make a record resendable. Choose the cutover date only after the historical boundary is understood.
5. Save global settings while disabled, select Ilyda/iTimologia as applicable, configure direct myDATA where used, select the documented POS mode, and review every active printable invoice type. Verify printers and retained credential profiles. Provider codes and software licences are supplied automatically by `InvoiceProviderCatalog`; the settings form shows only the selected provider's credentials. Run the transition check. Special cancellation/tank-control routes and historical issuance are reported without blocking global activation; their individual processing restrictions remain enforced.
6. Validate the selected channel in its approved test setup: cash, supported card/IRIS, fuel SupplyNumber, fuel-code normalization, exemptions/credits where applicable, response loss, payment recovery, app restart, concurrent manual/background requests and printer failure. Provider issuance must return its own signature with no fiscal-device call. Record provider, terminal and print evidence for the exact configured modes.
7. Enable one installation/channel only after those checks pass, with all workers stopped; restart the reviewed binaries together. Observe a small controlled pilot and reconcile every fiscal/payment/printing outcome before expanding. Do not use synthetic test identities in production.
8. If a pilot fails, stop workers, save a disabled configuration revision and restart the same compatible binaries for recorded-workflow recovery. New issuance stays paused. Preserve all post-cutover evidence; restoring a pre-cutover database after external acceptance would lose the evidence needed to prevent duplicates. Resolve uncertain outcomes through recorded-route recovery before resuming.

## Remaining legacy removal

The Windows application's old `Exedron.MyData`, `Exedron.ProviderInvoicing` and Arbitrans dependencies are still used by installations which have never activated shared processing and by unsupported legacy routes. Removing them now would break those routes. The historical characterization harness also deliberately exercises the old implementation.

After the selected installation's pilot is accepted, inventory the remaining active routes and utilities, migrate or explicitly retire them, then remove their PrintAgent branches, legacy settings, project references and packages together. Rebuild the **whole solution** and rerun release verification. Preserve journal readers, original credential profiles and stored print formats needed by historical documents. Shared Core/Adapters/host invoicing projects have no Arbitrans dependency.

PDF upload remains deferred. Shared cancellation, unsupported signing/POS combinations and unverified historical adoption are not enabled by this step. Live provider/device/POS/printer and native SQL Server 2008 R2 acceptance are outstanding deployment work.
