# ASFuelControlV1
ASFuelControl Version1

## Invoicing refactor

- [Step 1 workflow baseline and migration acceptance checklist](docs/invoicing-refactor/step-1-baseline.md)
- [Offline characterization tests and fixture documentation](tests/Invoicing.Characterization/README.md)
- [Step 2 shared fiscal contracts and database handoff](docs/invoicing-refactor/step-2-contracts.md)
- [Offline contract and mapping tests](tests/Invoicing.Contracts/README.md)
- [Step 3 SQL stores, migration and integration boundaries](docs/invoicing-refactor/step-3-persistence.md)
- [Isolated database integration tests](tests/Invoicing.Persistence/README.md)
- [Step 4 host invoice mapper and integration contract](docs/invoicing-refactor/step-4-host-mapper.md)
- [Offline mapper tests using real Data entities](tests/Invoicing.Mapping/README.md)
- [Step 5 provider integration, recovery and host boundaries](docs/invoicing-refactor/step-5-provider-integration.md)
- [Provider integration tests with isolated SQL and fake HTTP](tests/Invoicing.Providers/README.md)
- [Step 6 fiscal signing extraction and integration boundaries](docs/invoicing-refactor/step-6-fiscal-signing.md)
- [Fiscal signing tests with legacy payload comparisons and isolated SQL](tests/Invoicing.Signing/README.md)
- [Step 7 POS payment integration, voids and refunds](docs/invoicing-refactor/step-7-pos-payments.md)
- [POS integration tests with isolated SQL and fake provider/gateway HTTP](tests/Invoicing.Payments/README.md)
- [Step 8 PrintAgent integration, immutable printing and controlled activation](docs/invoicing-refactor/step-8-print-agent.md)
- [PrintAgent workflow and Windows-renderer tests](tests/Invoicing.Workflow/README.md)
- [Step 9 native settings, per-type transmission/signing and status/recovery UI](docs/invoicing-refactor/step-9-ui-configuration.md)
- [Native form rendering and UI binding checks](tests/Invoicing.UI/README.md)
- [Step 10 release verification, rollback protection and installation rollout](docs/invoicing-refactor/step-10-rollout.md)
- [Proposed myDiesel integration implementation plan](docs/invoicing-refactor/mydiesel-implementation-plan.md)

Database scripts and statements for this refactor must remain compatible with **SQL Server 2008 R2 (compatibility level 100)**.

The one-off heating allowance correction is available to administrators under **Κατάλογος Παραστατικών → Ενέργειες → Διόρθωση επιδόματος θέρμανσης…** after database migration 61. Select the date range and retail/wholesale credit types, run **Έλεγχος παραστατικών**, review the eligible selection and exclusions, then choose **Έναρξη παρτίδας**. **Συνέχιση εκκρεμοτήτων** resumes saved work across restarts, including documents outside the current date filter. Pause finishes the current provider operation first. CSV export records the results; printing is a separate explicit action and repeated clicks do not generate copies.

The batch accepts verified, accepted iTimologia/direct Ilyda sales missing category 11, including already printed sales. It freezes the original provider account/environment, amounts, recipient, supply number and lines; issues a full correlated credit; waits for confirmed acceptance; then reissues with category 11. Both new documents use credit/offset settlement and their ledger postings cancel out. Original documents/payments remain intact; no POS charge, refund, fiscal-device signature or physical fuel movement is created. POS/IRIS/fleet payments, movement documents, previous correlations/credits, changed source data and missing/unverifiable submission history are excluded with a reason. The immutable plan and reserved child identities in `FiscalHeatingCorrection` prevent duplicate pairs; provider journals retain request/response evidence. Uncertain submissions are looked up, never blindly resubmitted.

Batch regression tests are included in `tests/Verify-InvoicingWorkflow.ps1`; `tests/Verify-HeatingBatchUI.ps1` checks the actual native form, administrator authorization and credit/offset ledger behavior in a disposable database after the Windows binary has been built. Test databases use compatibility level 100; actual SQL Server 2008 R2 and provider sandbox acceptance remain installation checks.

Direct Ilyda can recover its documented offline queue. A normal Ilyda submission whose response was lost remains pending for provider reconciliation because its API has no documented general invoice lookup; resuming the batch never treats that uncertainty as permission for another POST.

Run the baseline without accessing a database, provider, fiscal device or printer:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingCharacterization.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingContracts.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingMapping.ps1
```

Run the SQL persistence tests in a newly created, disposable LocalDB instance:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingPersistence.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingProviders.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingSigning.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingPayments.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingWorkflow.ps1
```
