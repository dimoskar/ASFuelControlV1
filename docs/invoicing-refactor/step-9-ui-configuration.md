# Step 9 — native settings, per-type policy and invoice status

The existing Telerik WinForms screens now edit the shared invoicing configuration. No application database, provider, POS, fiscal device or physical printer was used during implementation. Deployment does not automatically enable the shared workflow.

## Configuration ownership

The active `UI/Forms/InvoiceTypesForm` preserves its grid, numbering, printer and transformations. Its previous `SendToMyData` checkbox is replaced with:

| Per-type setting | Choices |
|---|---|
| Τρόπος διαβίβασης | Χωρίς διαβίβαση / Πάροχος τιμολόγησης / Απευθείας myDATA |
| Σήμανση από φορολογικό μηχανισμό | On / off |

The device-signing checkbox does not suppress signatures returned by an invoicing provider. Provider issuance plus an additional Samtec signature remains unsupported and is rejected when saving the policy. Direct myDATA can run with or without Samtec. No transmission can run with Samtec, or as explicitly local, unsigned printing.

An absent policy appears unselected and blocks new shared issuance. Migration does not guess signing policy or interpret the ambiguous legacy boolean as a three-way transmission choice. Select and save each active printable type before activation. The legacy `InvoiceType.SendToMyData` column remains for the legacy path; version 2 uses the reviewed policy instead. No generated OpenAccess entity or mapping is changed.

General Options retains its current layout and exposes:

- **Α.Α.Δ.Ε.**: shared enable switch, one environment, cutover date/time, direct myDATA account/key/endpoint, branch and wholesale mapping codes. Existing tax-ID lookup credentials remain on this page.
- **Πάροχος Τιμολόγησης**: Ilyda or iTimologia and a transition check. Only the selected provider's fields are shown: Ilyda username/password, or iTimologia subscription ID/master token. Ilyda settings create password-authenticated profiles without an API-key reference; older API-key profiles remain available only for historical recovery. An older profile with no saved password requires password entry when saving Ilyda settings. Switching retains each provider's separate inputs; blank secrets retain only that provider's matching saved secret. Credential labels use `Κωδικός πρόσβασης Ilyda`, `Master token` and `Subscription key`, without “Νέος/Νέο” prefixes. The AADE code and software licence are read-only and supplied automatically from the provider catalogue. Ilyda uses its direct adapter; old Arbitrans credentials are not silently repurposed.
- **Λοιπά Στοιχεία**: global POS mode, GBridge endpoint/TID, alongside the existing bank details/application lock. The named mode selector replaces numeric signature formats and free-text protocols.
- **Παλαιές συνδέσεις**: the legacy connection controls remain available for installations awaiting cutover.

Printer Settings keeps the original controls in a legacy tab and adds separate **Σήμανση** and **Εκτύπωση** tabs. The device is global, with an optional explicit balance/tank-filling rollout. Windows printer, page geometry, copies and font are shared defaults. An installed per-type printer takes precedence; a missing, undefined or old folder/TCP value falls back to the installation's configured default, then the Windows default. The default-printer field may be left blank to use Windows. New snapshots retain the resolved printer, and later retries/reprints retain saved pages while applying the same fallback if that printer has been removed. The successful handoff record includes the printer actually used. Missing defaults produce one actionable printer error in the settings check, not one error per invoice type.

New configuration revisions retain previous credential/device profiles. Secret values use unique keys in the existing encrypted `Option` storage, not workflow/configuration JSON. Leaving a new-secret field empty preserves the existing credential reference only for the same provider, account, environment and issuer scope. Changing those requires credentials. A failed save may leave an unused new secret key, but cannot overwrite an old profile. Concurrent editors cannot overwrite newer configuration/policy revisions.

Verification of the Ilyda username/password settings update: **45 provider checks passed**, including Basic authentication with no API-key header. Native UI checks passed for retaining a saved password, requiring a password for an old API-key-only profile, preserving historical API-key references, and keeping provider fields separate; all three credential screens were visually inspected. Debug and Release Windows diagnostic builds passed with the existing Unimep exclusion. Logs: `tests/ilyda-password-*.log`. No application database or live provider was accessed, and no database migration is needed.

Routing, type policies and device settings are captured once per process; restart the application/workers after changing them. Before any fiscal attempt exists, invoice processing reads the latest saved account/subscription and credential profile for the same provider, environment, issuer/branch and endpoint. This includes resuming after an authentication failure (for example iTimologia subscription 3 rejected at `tokens/create`, corrected to 1 in settings). It does not require deleting the workflow or restarting workers for the account/key change.

Once any submission, provider session, payment, signing, print or cancellation attempt exists, the original connection remains fixed, including rejected or uncertain outcomes. Recovery resolves its exact historical credential profile from the latest configuration. Credential values are read through a fresh database context, bypassing the process-wide option cache so another worker can use newly saved keys. The invoice identity, content, POS mode/terminal, device and print destination are never remapped by a connection refresh. Version-1 per-type configurations retain their original behavior.

The invoice summary previews the current account with `(ρυθμίσεις)` while no attempt exists; opening the form does not update or send the invoice. On processing, `SqlInvoiceWorkflowStore.RefreshProviderConnection` updates only the provider connection and its matching POS provider under the invoice lease, with a compare-and-swap check and a database guard against existing attempts. Previous non-secret connections are retained in `PreviousProviderConnections` inside the workflow JSON. No schema migration is needed; the runtime SQL remains SQL Server 2008 R2 compatible.

Verification of the configured-connection update: **184 checks passed** (101 workflow, 26 persistence including SQL-100 parsing, 57 payment). Coverage includes subscription 3 authentication failure followed by configured subscription 1 and its credentials, retained rejected/uncertain/accepted attempts, POS refresh before signing/charging, prepared signing protection, stale writes, unchanged invoice identity/pages and decimal precision, and exact historical credential selection. Native UI assertions passed with fourteen renders; the configured-subscription summary was visually inspected. Shared projects built in Debug/Release; Windows diagnostic builds passed in both configurations, excluding the existing Unimep enum error. The Release diagnostic build succeeded after releasing this task's compiler cache following a paging-file resource failure. Logs: `tests/configured-connection-*.log` (successful Release Windows result: `configured-connection-windows-Release-final.log`). Tests used disposable databases and fake transports; no production configuration, invoice, provider, POS or printer was accessed.

### POS modes and documentation

`Exedron.Invoicing.Core.InvoiceProviderCatalog` is the single source for provider identity, including the shared and retained legacy POS paths. Numeric protocols use `AadeProviderId`; padded signing-author fields use `AadeProviderCode`. GBridge's compatibility property refers to the same identity. There is no editable provider-code or licence field in the settings form.

| Provider | Numeric ID / padded code | AADE software licence |
|---|---|---|
| [Ilyda](https://www.aade.gr/node/14465) | `8` / `008` | `2021_09_108ILYDA_001_ Meg myData_V1_08092021` |
| [iTimologia](https://www.aade.gr/node/14610) | `34` / `034` | `2025_11_134TECHNORAN_001_iTimologia_V1_17112025` |

Verified against AADE on 2026-10-16. The full software licence is distinct from the provider code. New profiles retain the catalogue licence; footer generation obtains it from the same catalogue. Historical route JSON remains unchanged to preserve its fingerprints, and existing saved print pages are reused unchanged. These identity/UI changes require no database migration.

Verification of the identity/UI update: **190 checks passed** (89 workflow, 57 payment, 44 provider), including both GBridge provider IDs and Ilyda's JSON/XML signing-author code. Native UI assertions passed with twelve renders; both provider settings screens were visually inspected. Shared invoicing and retained legacy provider builds passed in Debug and Release, as did the Windows diagnostic builds that exclude the existing broken Unimep reference. Logs: `tests/provider-identity-*.log`. Tests used disposable databases and mocked transports; no production database, provider, POS or printer was used.

`Exedron.Invoicing.Core.PosModeCatalog` owns the named modes available for each invoice provider. GBridge remains the transport. The settings builder maps the selected mode to the provider's signature configuration, and `PaymentRoute` validates and freezes both values. The invoice Payment tab displays its saved mode.

| Provider | Available modes | Mapping |
|---|---|---|
| Ilyda | EDPS, Mellon, Viva, ePay, Cardlink, Nexi, Neosoft, Attica, Worldline | EDPS maps to `nspProtocol=DEFAULT`; other choices use the corresponding uppercase enum. |
| iTimologia | EDPS, Mellon, Viva, ePay, Cardlink, Nexi | EDPS/Viva use payment/net/VAT/gross ordering; Mellon/ePay/Cardlink/Nexi use net/VAT/gross/payment ordering. |

Verified against the supplied documents under `E:\Exedron\2026\ASSalesManager\docs`:

- `Ilyda/Οδηγίες υλοποίησης A1155 POS v1.3.2.pdf`: section 5.3, p. 12 defines the EDPS default; section 7.7, pp. 27–28 lists the nine NSP protocols and their encoding/order; p. 31 records Attica/Worldline additions.
- `GBridge/Development notes 2.1.pdf`: p. 1, lines 54–61 defines the two NSP ordering groups, including Mellon REST.
- `iTimologia/iTimologia Development Files/iTimologia DLLVer_2.0.1.pdf`: p. 7 defines `acq_id`. Its numeric examples conflict with the endpoint behavior recorded in the existing adapter on 2026-10-14. The adapter's existing wire translation and returned-format validation are retained; the UI does not expose or reinterpret that wire number.

Unsupported modes are removed when changing provider. A still-supported choice is retained; otherwise explicit selection is required before saving enabled POS. Old iTimologia configurations contain only a format group, so the editor requires a named selection instead of guessing which terminal is used. Historical routes without `PosMode` remain readable and serialize identically, preserving saved request fingerprints. No new table/column or SQL dialect change is needed: the named mode is stored in the existing versioned configuration and workflow JSON. This is a configuration mapping check, not live terminal certification.

The earlier proposal's **Έκδοση myDATA** referred to a connection-profile revision. It is generated internally; the implemented form has no editable version field.

## Database migration 59

| Table | Contents |
|---|---|
| `FiscalConfiguration` | Append-only configuration JSON revisions, UTC creation time and operator. The latest committed revision is current. |
| `FiscalInvoiceTypePolicy` | InvoiceTypeId, transmission mode (0 none, 1 provider, 2 direct myDATA), signing flag, optimistic revision and update time. |

`Program.CurrentDBVersion`, the Windows resource and the standalone persistence schema installer include migration 59. The migration is transactional and rerunnable, adds no inferred policies, and removes no existing data. There are no new generated ORM mappings. Configuration JSON is processed in .NET; all migration, settings and catalogue SQL uses SQL Server 2008 R2 / compatibility-level-100 syntax.

Version-1 JSON files remain a fallback only while no database configuration exists. Saving General Options upgrades to version 2, initially disabled, retains historical profiles and requires explicit review of types. The database enable flag then takes precedence over the old `SharedInvoicingEnabled` Option.

## Local unsigned printing

An explicit `LocalOnly` workflow snapshot permits none + unsigned without inventing fiscal acceptance, a MARK or signature. A local print job has no `OriginalAttemptId`; the persistence store verifies its fingerprinted local-only invoice snapshot and absence of external fiscal/payment history before allowing it. A missing route without that explicit policy still fails.

Local invoices use the same immutable page storage, handoff journal, Unknown-state handling, evidence-based reconciliation and reasoned reprint behavior as fiscal invoices. `ReadyToPrint` distinguishes local completion from fiscal acceptance, and legacy `IsPrinted` is projected after handoff/confirmation.

## Status and recovery

Activation no longer fails because historical printed/submitted invoices exist or because the type catalog contains special cancellation/tank types. The manual settings check reports these as informational warnings; per-document restrictions remain. Financial credit types are distinguished from the special cancellation type `215`, rather than blocking every type with `IsCancelation`.

For an eligible printed invoice without a shared workflow, the summary explains that its saved number, series and date will be retained, and the **Διαβίβαση** action starts transmission without printing again. This also supports explicit submission of pre-cutover printed invoices. Existing fiscal/submission evidence, a zero printed number, a previous card/IRIS payment or a required fiscal-device route prevent this action and require individual review. Provider validation still determines acceptance. Current configuration is used only for a first shared submission; saved workflows retain their frozen routes and existing recovery actions.

If preparation succeeds but authentication fails before the invoice POST, the saved route may exist without a submission attempt. This is not acceptance. The workflow preserves the provider error even for `NotSubmitted` and `Rejected` results. Send, resume and manual-print actions show the returned details, and the open panel retains the last result across refreshes; changing invoice or starting another action clears it. The last-action text is a UI diagnostic, not new durable fiscal evidence. The existing HTTP journal remains unchanged. iTimologia errors expose recognized error fields and redact known tokens rather than displaying raw authentication responses.

For an invoice already marked printed in the host with no shared print attempt, the panel explicitly says it was already printed and no new print occurred. This distinguishes the original print flag from the shared print journal. **Συνέχιση έκδοσης** continues the frozen submission without requesting a print. The credential profile identifier is shown under Evidence rather than in the main summary. No new schema or SQL statement is needed.

Verified on 2026-10-16: **135 checks passed** (91 workflow, 44 provider), including failed authentication with zero invoice POSTs, preserved error details, token redaction, unchanged saved invoice identity, and continuation without printing. Native UI assertions and thirteen renders passed; the send-only error view was visually inspected. Results are scoped to the selected invoice even if an asynchronous action finishes after selection changes. Debug/Release invoicing builds and Windows diagnostic compiles passed with the existing Unimep exclusion. Logs: `tests/submission-status-*.log`. No production database or external provider/printer was used.

The invoicing catalogue retains its date/search bar and grid, showing separate route, signing, payment, submission and printing states. It includes invoices without a `MyDataInvoice` row, uses invoice dates, parameterizes filters, and limits a result to the newest 1,000 rows. Narrow the date range to inspect older results.

The catalogue and Invoice form share Summary, History, Payment, Printing and Evidence tabs. Issued Invoice form fields are locked, including save-on-close. Recovery and resume are separate actions; recovery does not initiate a new payment, signature or submission. Prepared Samtec requests have an explicit resume action using their saved payload. The reprint action requests a reason and reuses the saved pages. Unknown/Dispatching print jobs require an administrator to record an explicit printed/not-printed outcome with evidence before another copy is possible. Printer handoff is not presented as proof of physical printing.

For qualifying GBridge **F4** or **F4 → F2 → 0E** evidence, the payment status is **Αναμονή επανάληψης** and the action bar enables **Επανάληψη πληρωμής**. It sends one replacement payment with a new UID/provider signature, without a confirmation dialog. PrintAgent and ordinary issuance do not retry it automatically. **Ανάκτηση κατάστασης** remains lookup-only, including interrupted replacements saved by older builds. The service revalidates the displayed attempt under the invoice lock; stale/double clicks and conflicting or incomplete transaction evidence cannot authorize another payment. The button performs payment only; an approved result can be reused by subsequent issuance. It is disabled for ordinary unresolved, mismatched, approved and refund attempts. Interrupted unsent F4 replacements can be resumed through the same explicit button using their saved identity.

Invoices without a shared workflow or attempt now display **Κοινή έκδοση: Δεν ξεκίνησε**, not a legacy label. Their summary resolves the saved invoice type against the same runtime configuration used by PrintAgent and shows the configured provider/direct-myDATA/no-transmission destination, environment, signing choice and active/paused/disabled state. It explains known blockers such as missing configuration, unsupported cancellation/tank types, cutover date, saved lines, printer or payment routing, and actual legacy evidence. Viewing this explanation does not allocate a number, save a workflow or contact a transport; payload validation still occurs during issuance. Loading or saving the Invoice form refreshes the summary. Once a workflow exists, its saved destination takes precedence over current configuration.

Verified on 2026-10-16: **71 workflow/configuration checks**, native UI checks with ten renders, and the diagnostic Windows compile passed. Pending provider and cancellation summaries were visually inspected. Checks cover all configured destinations, inactive/paused state, actual legacy evidence, missing policy, cutover date, and saved-route precedence. No application database or live transport was used.

Evidence controls display request fingerprints, attempt/revision identifiers and whitelisted result fields; raw credential-bearing sessions/request bodies are not rendered. Legacy errors remain visible separately. Shared invoices cannot use the old catalogue resend/cancel menu items.

## Cutover and remaining operating limits

Save shared connections and printer/device defaults while disabled, review each active printable type, then run the transition check. It checks route/environment consistency, missing type policies, installed Windows printers, unsupported active invoice types, and legacy issuance evidence after the cutover timestamp. It does not transmit test documents or adopt old records as new accepted journal entries. Existing shared records are excluded from the legacy-evidence count.

The existing operating limits remain: no provider + Samtec invoice combination, no Samtec POS or direct-myDATA integrated POS, no Ilyda IRIS, no Sign A shared adapter, and no shared tank-control/cancellation issuance. Local unsigned processing supports cash/credit; standalone card/IRIS needs a separate payment workflow. Payment void/refund services retain their explicit service entry points; these screens do not add automatic compensation. The dedicated payment retry button is limited to the validated F4 cases above. Unknown Samtec outcomes remain blocked because the adapter has no verified lookup protocol. Historical acceptance/unknown outcomes require reconciliation, not silent migration.

## Verification

- `tests/Verify-InvoicingWorkflow.ps1`: original workflow coverage plus routing combinations, Ilyda/iTimologia selection, stale configuration/policy writers, print-only dispatch/reprints/reconciliation, catalogue filtering and cutover ownership checks.
- `tests/Verify-InvoicingPersistence.ps1`: SQL-100 parsing for the added settings/catalogue batches and all active migration scripts, plus isolated persistence tests.
- `tests/Invoicing.UI/build-preview.py`: compiles the real Designer/Step 9 UI partials against inert test collaborators, checks selector bindings/credential masking and renders seven native views. No application constructors/startup, database or transport runs.
- The Windows diagnostic build still omits only the pre-existing broken Unimep reference; it is a source compilation check, not a deployable full-solution build. The ordinary solution has the existing missing `ControllerTypeEnum.Unimep` blocker.

Native SQL Server 2008 R2 execution and deployed OpenAccess/provider/device/POS/printer checks remain deployment checks. LocalDB tests use compatibility level 100 and disposable private databases.

Verified on 2026-10-16: **413 regression checks** passed (37 contracts, 80 mapping, 98 characterization, 26 persistence, 32 providers, 55 signing, 44 payments, 41 workflow/configuration). The native UI binding checks passed and seven renders were inspected. The diagnostic Windows compile passed with the existing dependency/COM warnings. Test LocalDB instances were removed after execution. No application database or live provider/device/printer was accessed.
