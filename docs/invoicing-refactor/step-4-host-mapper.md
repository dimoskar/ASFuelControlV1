# Step 4: host invoice mapping

Implemented on 2026-10-15 in `ASFuelControl.Invoicing.Mapping`, targeting .NET Framework 4.6. The project is in the solution and references the real Data and Core projects. Core still has no dependency on ASFuelControl, Telerik, SQL or WinForms.

`HostInvoiceMapper.Map(Data.Invoice, MappingContext)` produces a validated, detached `InvoiceBuildRequest`. `Build` returns the shared `InvoiceModel`. Both are read-only: they do not allocate invoice numbers, recalculate prices, change printing/signature flags, save entities or invoke providers. The existing PrintAgent issuance path is unchanged; activating the mapper belongs to step 5 and the later PrintAgent integration.

## Mapping decisions

| Concern | Mapping and validation |
|---|---|
| Issued identity | Saved invoice/line IDs, positive number and fiscal wall-clock transaction date are required. UTC issue/dispatch dates are rejected. Series uses the Core empty-series convention (`0`). Lines are ordered by their saved GUID because the legacy table has no persisted line ordinal. |
| Net and discounts | Saved `TotalPrice - VatAmount` is the discounted line net. Add `DiscountAmountWhole` to obtain Core's pre-discount `NetAmount`, then let Core subtract the net discount once. `DiscountAmount` is gross and is checked against the saved net discount using the legacy two-decimal, midpoint-to-even conversion. Header gross, VAT and discounts must agree with lines. Legacy `NettoAmount` may match pre- or post-discount net; other discrepancies fail validation. No prices, VAT or quantities are recalculated or repaired. |
| Retail/wholesale | Use an explicitly supplied snapshot of `WholeSaleCodes`. The legacy `RetailInvoice` flag does not override it. `IsCancelation` maps to the existing credit-document behavior; cancelling an accepted transmission remains a separate operation. |
| Document kind | Existing fuel sales are goods; codes 158/159 are ordinary delivery notes. Explicitly mapped service-only invoices use Core's service document types. Mixed goods/services are blocked pending a document policy. Goods and non-fuel merchandise may coexist with per-line units and classification. |
| Items | A positive legacy `FuelType.EnumeratorValue` maps to a fuel code in litres, merchandise classification. Non-fuel items/services require an `ItemMapping` keyed by the legacy item's GUID. A two-character item code is not treated as a fuel-code fallback. |
| Payment | Explicit translation: host 0 → Credit, 1 → Cash, 2 → Card/POS, 4 → IRIS. Host 3/FleetCard requires `FleetCardPayment`; there is no default or enum cast. Missing/unknown financial payment methods fail validation. Delivery notes contain no payment entries. |
| VAT | Preserve saved rate/amount and the existing zero-VAT rules: domestic exempt trader → Article27 plus suspension, EU export → Article28, other export → Article24. An explicit item exemption takes precedence. Unknown rates and missing exemption reasons fail Core validation. These rules preserve repository behavior; they do not constitute new tax-policy approval. Ilyda Direct still rejects suspension payloads. |
| Parties | Copy names, raw addresses, branch and contacts independently. XML escaping is performed by serializers. Do not substitute issuer email for a missing customer email. Resolve country through the supplied catalog; missing country defaults to GR only for an absent or all-digit VAT. Unknown/ambiguous countries and foreign VAT without a country are rejected. |
| Electricity supply | Use the invoice's saved `SupplyNumber`; never substitute the trader's current number after issuance. Core requires an identified counterpart when transmitting a supply number. |
| Quantity | Use saved ambient `Volume`. `VolumeNormalized` is not substituted for it. Tanker route allocation and quantity-at-15-degree policy are outside this ordinary host mapping path. |
| Delivery | Codes 158/159 map to zero financial values while leaving saved entities untouched. Require explicit structured loading/delivery addresses, vehicle and departure in `DeliveryContext`; do not parse free-form legacy delivery addresses or infer departure from print time. Multiple lines are supported. |
| Correlations | Every linked parent requires a saved original document and reconciled `ParentEvidence` matching its invoice ID and the current issuer VAT, branch and environment. Core checks accepted positive MARK, no cancellation, matching customer, original document type and retail/wholesale compatibility. Normal-sale correlations require explicit `IncludeSaleCorrelations`; credits use the separate Core `Parents` input. Missing evidence blocks mapping instead of silently dropping a parent. |
| Self-supply | Same issuer/customer VAT is blocked. The legacy direct 6.1/provider 1.1 disagreement remains unresolved and Core does not implement the self-supply path. |

Returned settings, countries, contacts, delivery addresses, parent identity and transmission evidence are copied. Mutating entities or configuration after mapping does not change the result. The returned Core input is still a mutable DTO: step 5 must serialize and journal its exact payload before dispatch and recover from that saved payload instead of remapping.

## Integration contract for step 5

1. Acquire the document lease and complete numbering using the host transaction. Load the invoice, its lines/items/customer/type and linked original invoices while the Data context is alive. The mapper may read lazy-loaded navigation properties; it must not be called after disposing that context.
2. Capture company settings, wholesale codes and the country catalog once in `MappingContext`. Do not populate the context from provider singletons that can retain stale settings.
3. Load parent evidence from the durable historical submission/cancellation records. `ParentEvidence` is a typed boundary for that loader, not an independent proof of acceptance. Do not label legacy MARK-only rows with the current issuer/environment. Reconcile/import those records before using them; this step does not add an automatic history importer.
4. Supply any approved FleetCard/item mapping, normal-sale correlation policy and structured ordinary delivery details. The later configuration/UI step must make these choices available to operators. Until then, missing or unsupported choices remain validation errors.
5. Call the mapper, select the route's offline payload mapper, validate channel support, and save the exact payload in the step 3 journal before external I/O. Provider execution, retry/recovery and live PrintAgent routing are subsequent steps.

No database table, script or SQL statement was added in this step. The SQL Server 2008 R2 requirement remains in force. No application database or external provider/device/printer was accessed during verification.

## Verification

Run `tests/Verify-InvoicingMapping.ps1`. It builds the actual Data project with the Telerik enhancer and exercises real detached entities, rather than production code compiled against test doubles.

**80 mapper checks passed**, including all 18 step 1 cases under `en-US` and `el-GR`, intentional rejection cases, discounted totals and rounding, explicit payment translation, country validation, service/merchandise mappings, historical parent scope/cancellation checks, supply-number preservation, stable line ordering, detached snapshots, ordinary delivery and offline payload serialization for myDATA, iTimologia and Ilyda. The baseline cases check their reviewed business expectations, not byte equality with old provider payloads: Core intentionally corrects escaping, contacts, card/IRIS, correlation and delivery behavior.

The mapper and test project build successfully against .NET Framework 4.6, including the mapper target through `ASFuelControl.Windows.sln`. Regression verification also passed **37 shared contract checks** and **98 legacy characterization checks**. The first dependency build reports existing warnings in Common, Logging and Data (including the Telerik Logo change-tracking warning); no new mapper warnings were emitted. Full Windows application/UI execution, SQL lazy loading, provider acceptance/schema certification and device/printer behavior are not covered by these offline tests.
