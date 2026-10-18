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

Database scripts and statements for this refactor must remain compatible with **SQL Server 2008 R2 (compatibility level 100)**.

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
