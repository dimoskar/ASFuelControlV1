# Invoicing refactor step 3: SQL persistence

This step adds the host-owned `ASFuelControl.Invoicing.Persistence` project, migration 57, SQL implementations of the shared store interfaces, and database integration tests. Core and Adapters remain independent of SQL/Telerik. The three invoicing projects are now included in the solution; the Windows application still uses its existing issuance/PrintAgent implementation. Connecting the new stores to production issuance is a later integration step.

**All scripts and statements in this refactor target SQL Server 2008 R2 (10.50), compatibility level 100. Keep that constraint for subsequent steps.**

## Database tables

[UpdateDatabase57.sql](../../ASFuelControl.Windows/DBScripts/UpdateDatabase57.sql) creates six additive tables. No existing invoice, MyDataInvoice, Option, trader or generated ORM entity is rewritten.

| Table | Purpose |
|---|---|
| `FiscalSchemaVersion` | Installation marker for this journal schema, separate from the application's `DBVersion`. |
| `FiscalDocumentLease` | Document kind/ID and monotonically increasing fencing token. A SQL session application lock owns the document while a workflow executes. |
| `FiscalAttempt` | Immutable attempt identity, original request bytes/format/SHA-256, historical scope, document and operation, creation time, optional original-attempt link; current revision and duplicate-blocking projection. An identity sequence orders attempts independently of wall-clock timestamps. |
| `FiscalAttemptRevision` | Append-only revisions containing state, UTC time, complete result/evidence snapshot and fencing token. Earlier responses survive reconciliation and PDF/payment updates. |
| `FiscalHttpExchange` | Adapter-sanitized request evidence saved before HTTP transport and a single completion response. Completion cannot replace earlier evidence. |
| `FiscalPaymentEvent` | Separate append-only gateway exchanges keyed for lookup by transaction UID. |

Signing, invoice submission, provider POS sessions, payment, printing and cancellation are separate `Family` values and separate attempt records. They share the journal schema, not a single combined status. Each family/operation has independent history. A unique filtered index prevents two blocking attempts for the same document, family and operation, including attempts under different providers/devices/environments. A definitive rejection/local pre-dispatch failure can release that blocker; acceptance and uncertainty retain it. Print handoff/confirmation permits an explicit new print job; an unknown print result does not.

The application database version is raised to 57 and the script is registered in the existing resources/project. The next deployed application startup will apply it through the normal updater. Migration 57 is one transactional batch, can be rerun, and checks for incomplete/incompatible installations. Failures in migration 57 and later are rethrown instead of being swallowed; worker startup requires successful database initialization. Existing migrations retain their previous error policy. Connections are disposed even when a migration fails.

No application database was opened or migrated during development. Tests create and remove only uniquely named private test resources.

## Store implementations

| Class | Contract / behavior |
|---|---|
| `SqlFiscalDatabase` | Explicit SQL connection string, document leases, nested synchronous invoice workflow scope, transactions, immutable requests and revision checks. |
| `SqlFiscalSigningStore` | `IFiscalSigningStore`: begin before dispatch, Core transition validation, exact request/results, current projections and complete history. Supports all Core document kinds without claiming device capability. |
| `SqlMyDataSubmissionStore` | `IMyDataSubmissionStore`: retained XML and responses, original scope, rejection/uncertainty/acceptance and stale-object protection. |
| `SqlIlydaSubmissionStore` | `IIlydaSubmissionStore`: original JSON, issuer/environment, queued recovery and later PDF-upload flag without replacing accepted fiscal evidence. |
| `SqlITimologiaSubmissionStore` | `IITimologiaSubmissionStore`: Begin/Complete, retained HTTP outcome and original endpoint/issuer/environment. An HTTP 401 can be recorded as definitive authentication rejection even with an empty body. |
| `SqlIlydaPosStore` | `IIlydaPosStore`: original signature session, response, expiry, cancellation and deferred payment-submission evidence. Expired/unknown sessions block replacement. |
| `SqlProviderPaymentStore` | `IProviderPaymentAttemptControl`: stable transaction UID, original amount/terminal/provider scope, signature, authorization, void and recovery history. Requires an outer invoice lease across the whole payment workflow. |
| `SqlFiscalOperationStore` | Host cancellation and print jobs using the Core transitions. Cancellation references an accepted submission and retains its original scope. Print jobs reference accepted signing/submission evidence, retain rendered bytes, and distinguish handoff from confirmation. |
| `SqlEvidenceLogger` | `IRequestResponseLogger` and `IPaymentGatewayEventSink`, using separate connections so asynchronous HTTP logging does not depend on the synchronous workflow lease. |
| `SqlFiscalHistoryStore` | Read-only projections/history for reconciliation and later UI; request bytes are defensively copied. |

Existing provider/payment string states are preserved at their interfaces; they are not cast to the new Core enums. In particular, Ilyda `Queued`, POS `Ready`/`Expired`, and payment `Invoiced`/`VoidFailed` have distinct meanings. The older payment coordinator calls `MarkPending` during recovery: the store preserves `Unknown` instead of converting it into new-authorization permission. A legacy `Failed` report after dispatch is retained as `Unknown`, preventing a new charge on ambiguous failure.

Provider submission stores stamp returned/begun DTO instances with their database revision. Save/Complete requires that same instance. Reload through the store after restarting; do not construct an accepted DTO from UI fields. Accepted results cannot be overwritten, except the explicitly permitted Ilyda PDF-upload flag. An authoritative accepted/rejected result needs retained response evidence.

## Lease and transaction boundaries

Use one `SqlFiscalDatabase` instance for the cooperating stores. Hold an outer scope across the full synchronous invoice workflow:

```csharp
var database = new SqlFiscalDatabase(applicationSqlConnectionString);
var myDataStore = new SqlMyDataSubmissionStore(database);
var signingStore = new SqlFiscalSigningStore(database);
var logger = new SqlEvidenceLogger(database);

using (var workflow = database.EnterInvoice(invoiceId))
{
    // Future host orchestration: allocate/map, sign, authorize, submit, schedule printing.
    // Nested Acquire calls made by stores share this lease and fencing token.
    // Persist each dispatch state before calling any external system.
}
```

The scope is thread-affine and must be disposed on its owning thread. Do not carry it across `await` continuations. Existing invoicing entry points are synchronous. Other threads/processes acquire separate SQL sessions and contend on the same document resource. Non-invoice documents use `Acquire(FiscalDocumentReference)` directly.

Session locks are held on dedicated, unpooled connections, with automatic connection retry disabled. A process exit/connection loss releases the SQL lock; the saved Signing/Sending/Pending state still blocks a fresh external operation. A later owner receives a higher token. Every journal transaction checks session lock ownership and the persisted token. Every update checks expected revision, immutable scope/request/timestamp and allowed transition, then appends evidence and updates the current projection atomically.

Do not confuse acquiring a new lease with permission to resend. An interrupted operation must be reconciled against its original scope. New scope settings, a different adapter or a new attempt GUID cannot bypass the journal blocker.

## SQL Server 2008 R2 compatibility

- SQL uses `RAISERROR`, `TRY/CATCH`, `IDENTITY`, `datetime2`, application locks and filtered indexes available to this target. There is no `THROW`, SQL sequence object, native JSON operation, `OFFSET/FETCH`, `CREATE OR ALTER`, or `DROP IF EXISTS` dependency.
- JSON is serialized/deserialized in .NET and stored as `nvarchar(max)`. SHA-256 is computed in .NET; the database does not need a later `HASHBYTES` algorithm.
- Migration and runtime sessions explicitly set the options needed for filtered indexes. See Microsoft's [CREATE INDEX requirements](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-index-transact-sql#filtered-indexes).
- Every runtime SQL batch is centralized in [SqlStatements.cs](../../ASFuelControl.Invoicing.Persistence/SqlStatements.cs). Tests parse these actual statements, migration 57 and all active `ASFuelControl.Windows/DBScripts/*.sql` files with `TSql100Parser`, plus checks for newer constructs that might parse as identifiers/functions.
- Database integration tests explicitly set compatibility level **100**. They run on the installed modern LocalDB engine. This is grammar/compatibility validation, **not a native SQL Server 2008 R2 runtime certification**. A native server smoke test remains a deployment validation requirement.

Production projects still target .NET Framework 4.6. Only the persistence test executable targets 4.7.2 because the installed Microsoft ScriptDom assembly requires it.

## Integration boundaries and remaining work

This step supplies persistence, not the full orchestration. Step 4 must normalize host entities to shared inputs. Steps 5–8 must attach these stores/logger, use one outer lease across the external workflow, pass saved historical scope on recovery, and drive print/cancellation jobs. The UI changes remain in step 9.

The workflow must check outstanding attempts across families before changing payment/signing routes. A submission-family blocker does not, by itself, reconcile an unknown payment or provider-signature session. Keep that orchestration gate inside the outer document lease.

Before activating the new path for an existing installation, import/reconcile legacy accepted and uncertain invoices. An empty new journal cannot identify a historical accepted `MyDataInvoice` by itself. Do not enable the new sender without a legacy-history gate. Preserve existing signed/printed fields as compatibility projections; they are not proof of actual printer completion.

Number allocation still belongs to host transactions and the mapper/orchestrator steps. This migration does not add a unique constraint to potentially duplicated legacy `MyDataInvoice` data or guess its historical provider/environment. Fiscal payloads contain business/customer data; existing host permissions and retention policy must cover journal/history access. The logger receives payloads already sanitized by the adapter; it does not reinterpret arbitrary strings as safe credentials-free data.

Print jobs currently require an accepted signing/submission record. Policies for explicitly non-fiscal documents and migration of historical print evidence belong to step 8/9. Payment refund source-document mapping and external void confirmation remain responsibilities of the payment workflow; the store validates saved state/identity and retains the supplied evidence.

## Verification

Run [Verify-InvoicingPersistence.ps1](../../tests/Verify-InvoicingPersistence.ps1). It creates a fresh `ASFC_InvoicingStep3_<guid>` LocalDB instance and `ASFC_Step3_<guid>` database, then removes only those resources. It never discovers the application's connection settings or reads existing databases.

The suite covers transactional migration failure/rerun, SQL-100 parsing, cross-thread/process exclusion, stale fencing/revisions, immutable request protection, interrupted processes, preserved response history, changed-provider/environment blocking, provider/payment store contracts, nested workflow leases, printing and cancellation, and real myDATA adapter control flow against fake HTTP transport. No real fiscal, payment, device or printer requests occur.

The step 1 and step 2 suites remain separate checks. Full Windows application/device integration and a native SQL Server 2008 R2 run remain outside these tests.

Verification on 2026-10-15: **26 persistence tests, 37 shared-contract tests and 98 legacy characterization tests passed**. The new projects built without warnings; the baseline retained its three existing unused-variable warnings. All generated test databases and LocalDB instances were removed. No production database migration or live provider/payment/device/printer operation was performed.
