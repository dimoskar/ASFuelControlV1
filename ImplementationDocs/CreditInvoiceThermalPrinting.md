# Credit-invoice thermal printing

Normal invoices continue to use `InvoicePrintView` and the existing report setup.
Only types marked `IsCancelation` (the application's financial credit-note flag)
use the new detached credit rows in both `PrintAgent` and
`SharedInvoiceLaserReportRenderer`.

The SQL view requires a direct invoice-line sales transaction. Derived credit
lines deliberately do not own that transaction, so the view can return no rows,
leaving only separately configured company details and totals on the receipt.
Credit print rows now come from the credit invoice's actual `InvoiceLines`.
Their identity, product, quantity, price, VAT and amounts remain the credit's
own values. No source sale or invoice is modified, and no schema update is needed.

Pump/nozzle resolution follows a direct sale first, then explicit parent-line
relations recursively. When there is no line relation, it follows parent-invoice
relations and matches the product. Cycles are guarded. Multiple possible nozzles
remain unresolved rather than selecting an arbitrary sale. Lines without a
physical sale are still printed. The receipt's single pump/nozzle field is filled
only when all credit rows resolve to the same complete location.

Credit identity is assigned directly to the report header so it does not depend
on page-header `Fields` binding. Per-line VAT comes from the stored credit line.
An invoice with no lines is rejected before rendering.

Validation: build the solution in Release, then build
`tests/CreditInvoiceThermal/CreditInvoiceThermal.csproj` and run its executable
with the absolute `ASFuelControl.Windows/bin/Release` directory as its argument.
The tests use detached entities and the real Telerik renderer; they do not access
a live database or printer. They cover normal-path selection, parent resolution,
partial-credit financial values, ambiguity, cyclic relations, missing sales,
zero VAT, multiple pumps, empty invoices, and CSV/image rendering.

This changes newly rendered thermal documents. It does not rewrite any previously
stored print artifact.
