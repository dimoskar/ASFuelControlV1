# SQL persistence integration tests

From the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/Verify-InvoicingPersistence.ps1
```

Requires Visual Studio MSBuild, production .NET Framework 4.6 targeting assemblies, .NET Framework 4.7.2 for the test executable, SQL Server LocalDB, and Microsoft ScriptDom (installed with Visual Studio SQL tooling). Override locations with `-MSBuild`, `-LocalDB`, and `-ScriptDomPath`.

The script creates a private, uniquely named LocalDB instance and an empty test database. It sets compatibility level 100, exercises the real SQL stores, and deletes only the resources it created. The executable refuses instance/database names outside its test naming pattern. It never reads application settings or connects to an existing application database.

SQL syntax checks use `TSql100Parser` and guards for later features. The suite checks all active application SQL scripts and every runtime batch in `SqlStatements.cs`. A negative control ensures a later-only construct is rejected. LocalDB remains a modern engine; passing these checks does not claim testing on a native SQL Server 2008 R2 installation.

Concurrency tests use independent SQL sessions, another thread and child processes. One child exits after recording Signing, simulating external acceptance whose reply was never persisted. Subsequent attempts must remain blocked. Other cases verify stale fencing/revisions, immutable request history, provider/payment scopes, print-only retry, cancellation and adapter execution with fake HTTP transport. All payloads are synthetic.

See [step 3 implementation details](../../docs/invoicing-refactor/step-3-persistence.md) for tables, workflow lease usage and activation boundaries. Nonzero exit status indicates failure; build or SQL prerequisites are not silently skipped.
