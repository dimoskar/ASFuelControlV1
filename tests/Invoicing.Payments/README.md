# POS payment integration tests

Run from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingPayments.ps1
```

Requires Visual Studio MSBuild, .NET Framework 4.6 targeting assemblies and LocalDB. The runner creates a private `ASFC_InvoicingStep7_<guid>` instance and `ASFC_Step7_<guid>` database, sets compatibility level 100 and removes only those resources in `finally`. It never reads the application's connection string or sends live provider/POS requests.

The tests use real Core/adapter/host/persistence code, detached enhanced Data entities and injected HTTP handlers. Fake iTimologia and Ilyda signatures follow the parser contracts in the supplied adapters. Gateway handlers inspect the SQL state and saved exact HTTP request before returning a result. This validates local orchestration and the supplied wire contract; it does not certify a remote provider or terminal.

Coverage includes card and IRIS mappings; original route/model reuse; pending/unknown lookup-only recovery; mismatched or missing result fields; explicit decline retry; provider failure after approval; signature expiry/uncertainty; concurrent callers; legacy gates; failures saving signature, authorization, invoice linkage, HTTP evidence, void and refund state; and separate void/refund records linked to the accepted original payment.

F4 coverage verifies operator-only retries for single F4 and F4/F2/0E, repeated polling without new charges, lookup-only recovery, stale and concurrent clicks, atomic retry history, and explicit resumption of interrupted or historical automatic replacements. Strict response checks must revoke retry eligibility when recovery finds approval, conflicting values or incomplete transaction data.

Refund fixtures change the detached Core request's invoice mode to Credit after the real host mapper runs, so the suite can exercise full-refund orchestration independently of host credit-entry UI and parent selection. Existing mapping suites cover those host mapping rules. Full refund retry uses the persisted original credit input. No printable legacy state is changed by this workflow.

See [step 7](../../docs/invoicing-refactor/step-7-pos-payments.md) for entry-point contracts, supported channels and rollout boundaries.

Verification on 2026-10-15: **44 passed, 0 failed**. All earlier refactor suites also passed, giving **372 checks** in total. The private LocalDB instance/database was removed after the run.

For a focused rerun, set `ASFC_PAYMENT_TEST_FILTER` to a case-insensitive regular expression matching test names before invoking the runner. Leave it unset for the complete suite. All selected tests still use the private SQL-100 database and mocked transports.
