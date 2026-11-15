# Sale retention during application operation

This change covers controller sales and sales generated through the existing
correction-sale path. Manual invoice replacement and cancellation workflows are
outside this scope. Retention is in memory; application shutdown, power loss and
restart recovery are explicitly deferred. There is no sale journal in this change.

## Ownership and invoicing

`ASFuelControl.Common.Sales.PendingSaleRegistry` owns the single shared dictionary
for pending sale-driven invoicing. Its key is `SaleData.SalesId`. Totalizers are data, not an
identity: different IDs with identical nozzle/start/end totalizers remain separate
sales. Delivering the same ID again while it is pending reuses the retained entry.

The common controller base retains a sale directly before raising
`SaleRetentionRequired` or adding it to its value queue. The event only enriches
the existing entry with application metadata; no subscriber is required for
retention. `ControllerThread` consumes that same registry, without a second copy.
Controller identity, channel, address and nozzle number are captured at source.
Controllers implementing `IController` independently are
captured immediately after `GetDispenserValues` returns their sale. The value queue
is synchronized and status-update coalescing never removes completed sales.

## Transaction-completion receipt

Before interpreting a received totals/completion event or direct-sale event, the
common controller puts it in a static pending-completion dictionary and captures
its source values. These event records are separate from the sale registry:
they protect the earlier stage before a sale object can be constructed.

Processing records its progress. The sale is retained before clearing
`NeedsInvoice`, resetting the work flag, fleet communication or subscriber
notification. Exceptions leave the completion pending; controller polling retries
after two seconds. Retry reuses the same sale ID and captured amount, volume and
totalizers, and does not publish a successful handoff a second time. Total readings
awaiting confirmation stay pending until a later reading completes that stage.
Existing decisions that a totals reading represents no sale are still respected.

The base controller's normal and preset authorization methods reject new
authorization for a fuel point with pending completion/sale data. Different fuel
points can continue processing if one completion fails. `PendingCompletionCount`
reports pending receipts for a controller; failures are written to diagnostic
trace with the retained event ID. Replacing a controller object does not erase
the static receipts, but automatic reassignment to a different controller instance
is not performed: its source identity and context must be preserved.

This protects events received by the common controller base. It does not prove
delivery from hardware or add this receipt stage to independent `IController`
implementations that bypass the base class.

Receipt retains the sale before nozzle/context preparation and database work.
Preparation captures customer/invoice context once; a retry reuses that context.
Workflow completion only marks the retained entry ready. Correction sales use the
same `AddSale` entry point. `ProcessNextRetainedSale` is the only active dispatcher
of `SaleAvaliable`; the persistence handler rejects sales not being processed from
the retained dictionary.

The worker serializes processing and retries unfinished entries after two seconds,
without passing a pending earlier entry for the same dispenser. Receipt time is
used for the sale timestamp, and receipt user/shift/discount are retained for
delayed invoicing. Invoice issuance retains its existing date behavior.

Successful preparation proceeds directly to queued invoicing in the same attempt;
the retry delay applies to unfinished work, not a successfully prepared sale.
When one dispenser is waiting for its retry, ready sales on other dispensers can
still be selected. Processing itself remains serialized, so an in-progress slow
database call can still delay other work.

## Persistence and release

The persisted `SalesTransactionId` is the stable `SalesId`. A retry queries that ID
using a fresh database context; it no longer treats matching totalizers as proof
of an existing sale. A matching ID with inconsistent nozzle, counters, volume,
unit price or invalid-sale flag is an error and remains retained.

Normal sale release requires a fresh read confirming the sale, its invoice line
and the associated invoice. Existing invalid-sale handling remains separate: an
invalid sale is released after verifying its invalid transaction record, without
inventing an invoice for it. Exceptions and unconfirmed saves retain the entry.
This also handles a successful database commit followed by an application-side
exception: retry finds the same sale ID instead of creating another invoice.

Each persistence attempt checks for an already committed sale before saving
shared context changes or updating nozzle state. Tank-history baseline refresh
runs only after the new sale/invoice commit has been verified. Refresh failures
are logged as `Sale tank baseline refresh` without resetting sale success; the
refresh stays on the existing thread to preserve tank-workflow ordering. This
does not remove network/fleet dependencies or replace configuration validation.

Normal workflow authorization for a dispenser is deferred while any sale for it
is pending. An unidentified dispenser blocks normal authorization station-wide
until its retained sale can be mapped. Nozzle mismatch checks defer for known
nozzles with pending entries. Neither a timer nor alert resolution removes sales
from the dictionary. Persistent preparation/database errors require diagnosis;
they do not discard the sale. Search the configured application log for
`Sale retention retry` and `Create Sale` when file logging is enabled.

This is not an atomic rewrite of all sale/nozzle database updates. It protects
received sale ownership and verifies completion across the existing save path.
It does not reconstruct transactions never delivered by a controller or recover
sales already lost before this change.

## Validation

Build `ASFuelControl.Windows.sln` in Release, then build
`tests/SaleRetention/SaleRetention.csproj`. Run from the repository root:

```powershell
& 'tests\SaleRetention\bin\Release\SaleRetention.Tests.exe' 'ASFuelControl.Windows\bin\Release'
```

The executable tests concurrent receipt, status coalescing, receipt-handler
exceptions, distinct sales with identical totalizers, repeat receipt of one ID,
failed/unconfirmed persistence dispatch, workflow completion without immediate
invoicing, shared retention across controller instances, and preparation retries
without overwriting captured context. Persistence dispatch is simulated; this
suite does not connect to a live station database or physical controller. Additional
scenarios verify retention without an application subscriber, throwing receipt and
completion subscribers, waiting total samples, missing nozzle mapping, and retry
without substituting later mutable fuel-point values.
The suite also checks immediate invoicing after successful preparation and that
a deferred failure on one dispenser does not prevent another dispenser's ready
sale from being processed.
