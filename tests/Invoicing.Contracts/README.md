# Offline invoicing contract tests

Run from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingContracts.ps1
```

Requires Visual Studio MSBuild, .NET Framework 4.6 targeting assemblies and the existing `packages/Newtonsoft.Json.13.0.3` dependency. Pass `-MSBuild` to select an installation. The executable exits nonzero for any failure.

This console suite references the real Core and Adapters projects. It tests fiscal request/result invariants, every pair of states for signing/transmission/payment/printing, signage routing compatibility, and invoice mapping/serialization in English and Greek cultures. All data is synthetic. It creates no provider/device/payment clients and uses no database or printer.

See the [step 2 contract specification](../../docs/invoicing-refactor/step-2-contracts.md) for supported inputs, historical scope and SQL implementation requirements. Store concurrency and physical-device recovery are requirements for later integration tests, not simulated guarantees here. The separate step 1 characterization suite continues to describe legacy PrintAgent behavior, including known defects.
