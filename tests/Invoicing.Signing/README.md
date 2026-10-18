# Fiscal signing integration tests

Run from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingSigning.ps1
```

Requires Visual Studio MSBuild, the .NET Framework 4.6 targeting assemblies and SQL Server LocalDB. The script builds the real shared/host projects and creates a fresh private `ASFC_InvoicingStep6_<guid>` instance and `ASFC_Step6_<guid>` database at compatibility level 100. Its `finally` block removes only those created resources. It never uses the application connection string or a live endpoint.

The 55 checks exercise real SQL leases/revisions and fake Samtec HTTP, including exact persisted bytes before transport, invoice/balance/filling outcomes, malformed replies, Unknown blocking, prepared resume, legacy gates, failed persistence, concurrent workers and optional read-only recovery. Detached enhanced Data entities are used without a production Data connection. SQL failure injection uses SQL Server 2008-compatible triggers and RAISERROR.

`Extract-LegacySamtec.ps1` reads the actual PrintAgent payload builders into ignored `obj/LegacySamtec.g.cs`. The legacy wire-model files are linked directly. The only mapping substitution is the former Article27 enum reference with its reviewed value 11; legacy device transport is replaced by a throwing stub. The parity checks therefore detect changes to production builders rather than compare two handwritten expected payloads. Greek and English culture cases cover types 100, 169, 175, 316, 158 and 501; additional cases cover credit, VAT exemption and five-decimal unit price. Cancellation's legacy lookup is deliberately not executed.

This suite does not certify a Samtec protocol/version, exercise the Data loader against a deployed schema, activate PrintAgent or replace native SQL Server 2008 R2 acceptance testing. See [step 6](../../docs/invoicing-refactor/step-6-fiscal-signing.md) for integration limits and follow-up steps.
