# Provider integration verification

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingProviders.ps1
```

Requires Visual Studio MSBuild, .NET Framework 4.6 targeting assemblies, the existing Telerik packages/enhancer, and SQL LocalDB. The script creates a fresh `ASFC_InvoicingStep5_<guid>` instance and the executable creates a fresh `ASFC_Step5_<guid>` database at compatibility level 100. Only these generated resources are removed afterward. Application connection settings are never read.

The suite uses the real host mapper, provider adapters, logger and durable SQL stores, with detached Data entities and fake HTTP. The fake transport checks that `Sending` and the exact request bytes are present in SQL before returning a provider response. No external network request is made.

Expected result: **32 passed, 0 failed**. See [step 5](../../docs/invoicing-refactor/step-5-provider-integration.md) for behavior and remaining integration boundaries. Run the persistence suite separately for SQL Server 2008 grammar checks and the other journal contracts. Compatibility level 100 on modern LocalDB does not replace native SQL Server 2008 R2 runtime validation.
