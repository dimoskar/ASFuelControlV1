# Invoicing characterization tests

This standalone .NET Framework 4.6 console harness records the current ASFuelControl behavior before the invoicing refactor. It does not switch the application to the new libraries or fix production behavior.

Run from the solution directory:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingCharacterization.ps1
```

Requirements: Windows, Visual Studio MSBuild with a C# 7.3-capable compiler, and .NET Framework 4.6 targeting assemblies. The runner discovers MSBuild using `vswhere`; `-MSBuild 'C:\path\MSBuild.exe'` overrides discovery. No NuGet restore, Telerik installation, database, credentials, network access, POS or printer is required. Some restricted execution environments require MSBuild permission to read the installed SDK registry entries.

The runner builds this project only, runs the executable, and fails on any build/test error. The main solution and production project references are unchanged. Output and generated sources are under ignored `bin` and `obj` directories.

## What runs

Every build extracts these method sections verbatim from `ASFuelControl.Windows/Threads/PrintAgent.cs`:

- `InvoiceHelper.CreateInvoice` and `CreateProviderInvoice`.
- `MyDataSendInvoice`, both `SendInvoice` overloads and `CancelInvoice`.
- `SignInvoice`, `SignAlert`, `SignBalance`, `SignTitrimetry`, `SignTankFilling`.
- `ApplySign`, `IsRememberedSign`, `RememberAppliedSign`.

The executable also compiles the production legacy model classes, XML resources/serializers, provider type/classification resolvers, response contracts and application payment enum. Extraction fails if its named boundaries cannot be found; there is no checked-in copy of the production algorithm to go stale.

`TestDoubles.cs` supplies only infrastructure and data shapes: lists in place of OpenAccess, synthetic company/options/country data, scripted API/device responses and printer handoff callbacks. It contains no SQL driver, HTTP transport, fiscal SDK or printer implementation. Production `InvoiceHandler`, `InvoiceSender`, `InvoiceCancelation`, Samtec HTTP client and `PrintAgent` constructor are deliberately not compiled. Device request builders and report rendering are also substituted.

The suite currently has 98 cases:

- 18 invoice fixtures through both legacy channels in `en-US` and `el-GR`: 72 cases.
- Submission, cancellation and malformed input outcomes: 19 cases.
- Signing and printer handoff behavior: 7 cases.

Assertions check important values independently of the snapshots, including type, payment mapping, correlations, exemptions, net amounts and persisted MARK/status.

## Fixtures and snapshots

`Fixtures/cases.json` defines synthetic inputs and independent expected values. Defaults are in `CaseFixture` in `Program.cs`. All invoice IDs, VAT numbers, names, addresses, credentials and responses are synthetic. URLs use `.invalid` and are never resolved.

`Fixtures/Expected/*.mydata.xml` is the XML passed to the substituted legacy `InvoiceSender`, including the string replacements performed by `PrintAgent`.

`Fixtures/Expected/*.provider-model.xml` is the XML model archive saved by `PrintAgent` before invoking the legacy provider handler. **It is not the final Arbitrans/Ilyda HTTP payload.** That translation belongs to the excluded SDK/handler. Provider responses represent the handler's normalized result contract, not captured wire responses.

Comparison ignores XML formatting and declaration encoding, while preserving element order, namespaces, values and presence/absence. A normal run never updates expected output. Actual differing output is written beside the executable for inspection.

To deliberately recapture a baseline after reviewing a behavior change:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingCharacterization.ps1 -RecordSnapshots
```

Recording writes the English-culture snapshot once; Greek-culture runs must match that same snapshot. Review the resulting fixture diffs and independent value assertions, then run normally. Do not regenerate snapshots just to make an unexplained difference pass.

## Known defects are not desired behavior

Cases prefixed `KNOWN-DEFECT` pass when they reproduce the current defect. Some invoice snapshots also intentionally capture discrepancies, including direct card/IRIS as cash, cancelled-parent correlation, and same-VAT differences. A green run means the baseline is reproduced; it does not certify correct issuance.

When a defect is fixed in later steps, replace its characterization expectation with an explicit acceptance test and update the decision record. Do not preserve an unsafe behavior merely to retain a green baseline.

## Coverage boundaries

This harness does not simulate OpenAccess identity maps, SQL isolation/constraints, rollback, real cross-process contention, application startup/settings refresh, physical printers, provider activation, POS authorizations, device request payload construction, or fiscal-schema validation. The in-memory `SaveChanges` records invocation order; it does not prove durability. A duplicate-summary test observes the attempted duplicate insert; a real database constraint could reject that write.

One signing test models the `PrintToPrinter` early-return boundary with a callback into the actual extracted send method. It does not execute Telerik rendering. Signing request payloads are placeholders; only control flow and result handling are exercised.

The workflow inventory, known defects, decisions and future integration acceptance checklist are in [the step 1 baseline](../../docs/invoicing-refactor/step-1-baseline.md).
