# PrintAgent workflow integration checks

Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingWorkflow.ps1` from the repository root. Requires the .NET Framework 4.6 targeting pack, Visual Studio MSBuild, the restored repository dependencies and LocalDB. The script creates/removes only its uniquely named private LocalDB instance/database and sets compatibility level 100. No application database, live provider, POS, fiscal device or physical printer is accessed.

Tests compile the production orchestrators, SQL stores and Windows page renderer. Fake transports assert durable Dispatching state before handoff. Failure-injection triggers exercise snapshot, dispatch and outcome write failures. The numbering fixture uses the actual runtime SQL against minimal Invoice/InvoiceType/InvoiceLine tables, including concurrent callers and rollback. A synthetic Greek invoice/QR preview is written under ignored `bin/Release/render-preview.png` for inspection.

The legacy characterization suite deliberately substitutes `SharedInvoiceRuntime` with a disabled implementation; its 98 checks remain a baseline for the retained legacy branch. It does not claim to test the shared path. This suite covers the shared path separately.

`WindowsCompile.targets` is a diagnostic-only omission of the unrelated broken Unimep reference. Use it only with a Windows project compile check after building dependencies; do not use that output for deployment. Full solution build, physical printer layout, live device/provider integration and native SQL Server 2008 R2 remain separate rollout checks. See the [step-8 report](../../docs/invoicing-refactor/step-8-print-agent.md).
