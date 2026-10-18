# Host mapper verification

Run from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingMapping.ps1
```

Requires Visual Studio MSBuild, .NET Framework 4.6 targeting assemblies and the repository's existing Telerik packages/enhancer. The script supplies `SolutionDir` when building the project outside the solution. It uses an incremental build of the real production projects, including `ASFuelControl.Data`.

The console suite creates detached real Data entities without opening a `DatabaseModel`. It reuses the reviewed step 1 case data, checks the new shared output and exercises all three offline payload mappers. It also checks the intentional differences and rejection paths described in [step 4](../../docs/invoicing-refactor/step-4-host-mapper.md).

Expected result: **80 passed, 0 failed**. No database, provider, fiscal device or printer is called. Generated binaries/objects remain ignored. This suite does not validate SQL lazy loading, transaction isolation or provider acceptance; those require the later integration steps.
