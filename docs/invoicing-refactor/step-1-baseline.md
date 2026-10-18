# Invoicing refactor step 1 baseline

Recorded on 2026-10-15 against working-tree source at Git HEAD `ae20ab1a5e5fe70173e2f9634bdc74afc5483cda`. The new Core and Adapters folders were present but untracked and were not wired into the application. This step adds documentation and offline characterization only; it changes no production source, database, settings or solution membership.

The runnable baseline is [Invoicing.Characterization](../../tests/Invoicing.Characterization/README.md). Its inputs and responses are synthetic, not captured customer transactions. Exact provider SDK wire requests and physical device behavior remain outside this offline baseline.

## Active workflow inventory

Source locations below identify methods rather than relying on line numbers that will change during refactoring.

| Workflow | Current entry and execution | Observable completion and recovery |
|---|---|---|
| Background invoices | `ThreadController` creates/starts `PrintAgent`. `ThreadRun` selects printable invoices with lines where printing/signature appears incomplete; assigns numbers in chronological batches, then processes recent/backlog batches through `PrintInvoice` or `PrintInternalInvoice`. | `IsPrinted` and `InvoiceSignature` drive polling. Instance lists are released in the loop's `finally`; they do not protect manual callers or other processes. |
| Fiscal device invoice | `PrintInvoice` recalculates and saves, then invokes `SignInvoice` for the exact `Samtec` branch. `SignInvoice` can allocate a number, constructs Samtec request/text, calls `CallWS`, parses `ResultCode`, then invokes `ApplySign`. | `Error` text returns early; accepted `ΔΦΣΣ` text supplies signature and optionally QR. `ApplySign` saves signature/QR and `IsPrinted=true` before printing/transmission. |
| Provider invoice | With the cached global provider flag, `SignInvoice` calls `ApplySign` with an empty signature, bypassing Samtec. The print method then calls `MyDataSendInvoice`. | Successful provider response overwrites `Invoice.InvoiceSignature` in a separate database context and updates `MyDataInvoice`; printing still uses the caller's context. |
| Direct myDATA | `PrintInvoiceDirect`, `PrintToPrinter`, and `PrintToPrinterInternalInvoice` call `MyDataSendInvoice` with print modes 1, 2, 3. `InvoiceHelper.CreateInvoice` builds the model, XML is serialized and edited with string replacements, then sent. | Request is saved before transport. UID/MARK/QR and status 3 represent acceptance. A failed send returns before rendering. |
| Provider transmission | Same entry points, selecting `CreateProviderInvoice` and `InvoiceHandler.Instance.SendInvoice`. | Stores XML serialization of the provider model before SDK execution; this is not provider wire JSON/XML. Acceptance saves verification hash, URLs, UID, MARK and printable signature. |
| Manual print/reprint | `InvoiceFormEx` can create another `PrintAgent` and call `SignInvoice`, or call static `PrintInvoiceDirect`. Other direct-print callers include cash and dispenser transaction forms. | Can overlap background work. Printing may invoke issuance again unless an existing status-3 summary prevents it. |
| Transmission retry | `MyDataCatalog` creates another agent and calls `SendMyDataToPrint`; that method parses print mode from `MyDataInvoice.Errors` and re-enters one of the printing paths. | No separate recover/reconcile operation. Only status 3 suppresses sending. Historical route/environment is not saved in the summary. |
| In-process exception cleanup | `ThreadRun` actively calls `CheckAfterException`, which inspects remembered entry IDs and `id$$signature` strings. | Updates local print/signature fields; it does not query provider/device acceptance or reconcile uncertain transmissions. `GetInvoiceInfoRequest`/`GetInvoiceLastInfoRequest` helpers are present but not invoked by an active recovery path in this file. |
| Cancellation | `MyDataCatalog` calls `PrintAgent.CancelInvoice` with the summary row. `MyDataCorrection` independently calls the legacy cancellation API. | Uses current myDATA URL/credentials and original MARK; saves `CanceledByMark`. No attempt journal or uncertain cancellation recovery; missing responses are dereferenced. |
| Balance | `SignBalance` builds text/request, calls Samtec and applies signature; `ApplySign` saves print date before printer handoff. | Parsing assumes signature response fields. `PrintToPrinter(balance, true)` also owns downstream report/email behavior; that remains in the host. |
| Tank filling | `SignTankFilling` builds a request using `GenerateTankFillingContext`, calls Samtec, parses response and calls `ApplySign`. | Stores `SignSignature` before print handoff. Parsed QR is not forwarded by this method. |
| Alerts and titrimetry | `SignAlert` and `SignTitrimetry` have commented device calls and currently call `ApplySign` with `"-"`. | Placeholder state and print date are persisted. These are not real fiscal-device signatures. |
| Tank-control side effects | Invoice branch of `ApplySign`, when `ForcesDelivery` is set, updates tank controller flags and invoice references. | Runs before transmission/printing completes. Must remain an explicit application workflow, with a decision on its required prerequisite. |
| Non-Samtec/file path | `PrintInvoice` writes text in the configured signing/printer folder, also calls Samtec with an empty URL in that branch, then assigns `"Sign Device A"` and `IsPrinted=true`. | Watcher subscriptions and periodic `CheckProcessFolder`/`CheckForSigns` calls are commented out. Do not assume legacy file recovery is operational; inventory installations before implementing an adapter. |
| Close sales | `CloseSales` writes a `CloseSales.txt` command and updates the close-sales option. | Separate device command; preserve it explicitly if that integration remains supported. |

Primary sources: [PrintAgent](../../ASFuelControl.Windows/Threads/PrintAgent.cs), [ThreadController](../../ASFuelControl.Windows/Threads/ThreadController.cs), [InvoiceFormEx](../../ASFuelControl.Windows/UI/Forms/InvoiceFormEx.cs), [MyDataCatalog](../../ASFuelControl.Windows/UI/Catalogs/MyDataCatalog.cs), [MainSettingsForm](../../ASFuelControl.Windows/UI/SettingForms/MainSettingsForm.cs), [legacy provider handler](../../Exedron.ProviderInvoicing/InvoiceHandler.cs).

## Provider and payment boundary

The current provider handler uses Arbitrans activation/credentials and implements Ilyda. Its `Orian` enum branch does not issue an invoice. For POS/IRIS it obtains a provider signature, calls Mellon/Viva payment routines, then submits the invoice. Error handling can attempt Mellon cancellation or Viva abort. `CreatePosHandlers` and `MellonGroupHelper` also depend on Arbitrans.

Step 1 does not execute that handler: activation, SDK behavior, payment polling and compensation cannot be safely inferred from a normalized `InvoiceResponse` alone. These need fake-transport tests of the selected replacement adapter and installation-specific test-mode verification in steps 5 and 7. iTimologia is new functionality, not a current ASFuelControl baseline route.

## Current state interpretation

| Existing field | Current meaning | Refactor requirement |
|---|---|---|
| `MyDataInvoice.Status = 3` | Successful response recorded; suppresses subsequent sending | Preserve accepted evidence and protect against another issuance. |
| `Status = 0` | Set before send; also retained for no response | Separate prepared/sending/unknown states; never treat uncertainty as a new-send authorization. |
| `Status = -1` | Validation/mapping errors, provider rejection and caught exceptions share this value | Distinguish definitive rejection, local failure and unknown external outcome. |
| `Data` | Last outgoing direct XML or serialized provider model | Preserve every original attempt; never erase it after uncertain transport. |
| `Errors` | Print mode prefix plus errors, warnings, or even a QR URL | Split operational metadata from diagnostics and fiscal output. |
| `Invoice.IsPrinted` | Set by `ApplySign` before transmission/printing, or file-path processing | Cannot establish physical print completion; migrate conservatively. |
| `InvoiceSignature` | Samtec signature, provider display text, or a placeholder | Record signature source/type and explicit state. |
| `CanceledByMark` / `CancelationMark` | Cancellation evidence | Preserve on migration and use it when validating parent correlation. |

## Invoice fixture matrix

Each row produces a checked-in direct-myDATA XML and provider-model XML snapshot. Both run under English and Greek cultures. See [cases.json](../../tests/Invoicing.Characterization/Fixtures/cases.json).

| Fixture | Observed behavior / decision |
|---|---|
| `retail-cash` | Both routes choose 11.1, cash method 3, retail income classification. |
| `wholesale-credit` | Both choose 1.1, credit payment method 5. |
| `card` | Direct uses cash 3; provider uses POS 7. Explicit translation is required in the replacement. |
| `iris` | Direct uses cash 3; provider uses IRIS 8. |
| `fleet-card` | Both currently use cash 3. Core has no FleetCard member; decide intended treatment. |
| `discounted-fuel` | Already-discounted saved gross/VAT produce net 90 via `TotalPrice - VatAmount`; this does not test invoice-price recalculation or establish pre-discount amount semantics. |
| `vat-exempt-domestic` | Domestic exemption sets Article27 and VAT payment suspension. Preserve supported behavior through explicit input contracts. |
| `eu-export` / `non-eu-export` | 1.2 / 1.3, Article28 / Article24 and corresponding income classifications. These are code observations, not a current tax-policy validation. |
| `correlated-credit` | 5.1 and parent MARK. |
| `uncorrelated-credit` | 5.2 with no parent correlation. |
| `retail-credit` | 11.4 without serialized correlated MARKs. |
| `delivery-158` | 9.3, zero financial values, no payment methods; direct string edits retain movement purpose and remove empty currency/payment wrappers. |
| `delivery-159` | Also 9.3, but direct string edits remove movement purpose and leave empty currency/payment wrappers. Compare deliberately during migration. |
| `same-vat` | Direct chooses 6.1/category1_6; provider mapper hardcodes `CounterPartVatMatches=false` and chooses 1.1/category1_1. Requires an explicit mapping decision. |
| `linked-sale` | Provider 1.1 retains parent MARK; direct normal invoice does not. |
| `credit-with-cancelled-parent` | Both still correlate a cancelled parent; new validation should reject this case. |
| `two-fuel-lines` | Preserves line order, per-line amounts and aggregate totals/classification. |

Additional mapping observations from source and snapshots:

- Both current mapper methods hardcode `isService=false`; service invoices cannot be honestly claimed as tested current mapper behavior. Define and test their classification in step 4.
- Both use `WholeSaleCodes` rather than `InvoiceType.RetailInvoice` for retail classification.
- Vehicle number is populated on the legacy model, but the direct XML header template does not serialize it. Provider model XML does include it.
- Direct counterpart branch remains the default 0; provider mapping uses the saved trader branch.
- `VolumeNormalized`/quantity at 15 degrees is not populated by either mapper.
- No saved supply-number mapping appears in these legacy mapper methods. The new builder's supply-number behavior requires a deliberate fixture in step 4.
- The provider model always reads fuel metadata for item description/code; non-fuel/service inputs require additional mapping work.
- Direct text substitution does not escape every name. `A & B` as a foreign customer name fails serialization before transport; the exception is masked by formatter cleanup on the tested runtime.

## Known defects and acceptance expectations

The tests marked `KNOWN-DEFECT` reproduce current behavior. They are intentionally not requirements for the replacement.

| ID | Current observation | Required acceptance expectation | Evidence / later step |
|---|---|---|---|
| D01 | Signing sets `IsPrinted` before printer handoff; a failure can leave it true. | Printing has an independent durable job state and observable failure. | Executable signing cases; step 8. |
| D02 | Signed invoice plus failed myDATA send can remain signed and printed while rendering never happened. | Pending transmission/printing remains discoverable without signing again. | Extracted signing/send methods with modeled printer boundary; steps 5, 8. |
| D03 | Missing response is submitted again; timeout clears an existing saved request and becomes -1. | Preserve original evidence; recover unknown outcomes before another send/payment. | Executable for both channels; steps 3, 5, 7. |
| D04 | First-send exception causes the outer handler to attempt a second summary insert. | One consistent summary plus separate attempts; handle constraints/transactions correctly. | In-memory attempted inserts; actual SQL behavior untested; step 3. |
| D05 | Provider-enabled issuance reports success when myDATA is disabled/key missing; username validation checks the key twice. | Validate the selected channel and return explicit not-required/accepted/failed outcomes. | Executable; steps 5, 8. |
| D06 | Instance lists do not protect manual/background/cross-process issuance. | A shared invoice lease prevents duplicate external side effects. | Source inspection only; SQL contention tests in step 3. |
| D07 | Provider options stop refreshing after initial AADE username; global provider flag is static. | Configuration changes rebuild services; existing attempts retain their historical scope. | Source inspection only; steps 5, 9. |
| D08 | Provider signature saved in a separate context can be absent from the entity used for raw/TCP printing. | Render an accepted immutable result or reload it before printing. | Source inspection only; real ORM identity-map test in step 8. |
| D09 | Cancellation with no response dereferences null; no cancellation attempt survives uncertainty. | Journal cancellation and recover/reconcile using original channel/environment. | Executable null response plus source inspection; steps 3, 5. |
| D10 | Correlation ignores cancelled-parent evidence. | Credits validate accepted, uncancelled parents and customer identity. | Snapshot fixture; step 4. |
| D11 | Unescaped foreign customer name fails before transport. | Structured XML generation safely handles special characters. | Executable; steps 4, 5. |

## Integration acceptance checklist for later steps

These checks are specified here, not claimed as passing in the offline harness.

- [ ] One series cannot allocate duplicate numbers under concurrent workers; numbering order and transaction rollback are verified against SQL.
- [ ] Manual/background/cross-process requests for one invoice produce at most one authorized issuance/payment operation.
- [ ] A crash after saving Sending, after remote acceptance, or before saving the result recovers without blind resend.
- [ ] An accepted payment followed by unknown invoice outcome never starts another authorization; refund/void recovery preserves references.
- [ ] Accepted/cancelled historical invoices retain route, environment and evidence even after settings change.
- [ ] Samtec payload generation covers invoice, balance and filling requests using fake transport; malformed/partial device responses have explicit outcomes.
- [ ] Unsupported device lookup capability leaves an uncertain signature for reconciliation, rather than inventing recovery or signing again.
- [ ] Printer/spooler failure after acceptance retries printing only; Windows/raw/TCP paths render saved signature and QR data.
- [ ] Reprinting cannot recalculate or mutate the accepted fiscal snapshot.
- [ ] Tank-control side effects execute at the agreed stage and do not repeat on print retries.
- [ ] Alerts/titrimetry retain explicit not-signed behavior; placeholder `-` is not imported as a device signature.
- [ ] Legacy file-signing and CloseSales installations are inventoried before that route is replaced or removed.
- [ ] Provider wire payloads and direct XML are validated against the applicable supplied schemas/contracts in test environments.
- [ ] New services, mixed goods/services, supply numbers, movement contexts, rounding and malformed data have explicit mapper tests.
- [ ] `MyDataCorrection` and all remaining external callers are migrated before legacy assemblies are removed.

## Step 1 completion boundary

The automated suite covers actual source-extracted mapping, submission and signing control flow against substituted infrastructure, with 36 structural payload snapshots and scripted normalized responses. The inventory distinguishes live paths, disabled/commented paths, baseline differences, known defects and future acceptance checks.

No deployment readiness or fiscal compliance is implied by a green characterization run. Database migrations, production fixes, new adapter integration and UI changes belong to subsequent steps.

Verification on 2026-10-15: a normal build/run against the saved fixtures completed with **98 passed, 0 failed**. MSBuild emitted three unused-variable warnings from linked/extracted legacy code. A negative-control run against a separate temporary copy changed one expected invoice number from 42 to 43; both culture variants failed as intended (**96 passed, 2 failed**, exit code 1). The saved baseline fixtures were not changed by that check.
