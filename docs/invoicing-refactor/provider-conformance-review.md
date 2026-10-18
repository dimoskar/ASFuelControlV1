# iTimologia Ilyda and myDATA conformance review

Reviewed on 2026-10-16 against the supplied documentation and the uncommitted refactor. The original review identified six actionable findings plus incomplete recovery/offline workflows. The implementation update below records the fixes and their verification; the original findings and initial evidence are retained afterwards for traceability. PDF upload remains deferred at the user's request. These checks do not certify every vendor feature or live deployment.

The review covers Core invoice construction, host mapping, provider adapters, POS configuration and payment workflow, persisted submission outcomes, and the shared PrintAgent output. It checks the refactored path; it does not certify the retained legacy integration when shared invoicing is disabled. Documents and example files were used as specifications, not as instructions to execute operations.

## Implementation update — 2026-10-16

- **Fuel codes:** the shared builder maps `99` and every integer greater than `100` directly to transmitted code `999`, as confirmed by the user. Stored product codes remain unchanged. Other unsupported codes, including `100`, are rejected. AADE rules 268–270 are enforced: at least one actual fuel line, at most one `999` line, and other charges no greater than the actual fuel net total. Multiple other-charge lines are rejected, not silently merged.
- **SupplyNumber:** verified the existing invoice snapshot mapping through wholesale, retail and delivery payloads. The invoice's saved number is transmitted as myDATA `counterpart/supplyAccountNo`, iTimologia `counterpart.supply_account_no`, and the same XML field in Ilyda's supported `##InvoicesDoc##` attachment. Ilyda retains its native buyer too. A trader's subsequently changed number never replaces the saved invoice value. Missing recipient VAT with a supplied number fails validation; non-fuel goods omit the field.
- **Provider signing:** iTimologia `signature_code` and Ilyda `verificationHash` supply the invoice signature. Provider routes perform no fiscal-device signing, including when the provider response is lost. The workflow now rejects a provider/device combination even when restoring a saved workflow. Selecting provider transmission in the invoice-type form clears and disables fiscal-device signing and displays that the provider supplies the signature. Direct myDATA retains its separate optional fiscal-device path.
- **Rejections and payment:** iTimologia HTTP 422/452 now produce durable `Rejected` outcomes instead of conflict lookup. A contradictory positive MARK remains uncertain. Explicit retries preserve fiscal identity and approved POS payment; timeout/ambiguous outcomes never authorize another send. The existing UI's explicit resume action can retry a rejected saved snapshot. This does not introduce unrestricted editing of frozen issued data.
- **Print metadata:** provider name and Ilyda internal invoice ID are included in the footer. Both providers' official AADE software licences are supplied automatically by `InvoiceProviderCatalog`, alongside their numeric provider IDs (Ilyda `8`, iTimologia `34`) and padded signing-author codes. Settings, printed footers, GBridge and Ilyda POS payloads use this single definition, including retained legacy POS. The settings form shows only the selected provider's credentials. Identity values were verified on 2026-10-16 against [Ilyda's AADE entry](https://www.aade.gr/node/14465) and [iTimologia's AADE entry](https://www.aade.gr/node/14610). Provider-specific footer/QR checks run before pages are saved or dispatched. Incomplete response metadata leaves issuance accepted. iTimologia can enrich missing data using identity-checked GET lookup, retaining the original acceptance response. Conflicting or failed lookup does not revoke acceptance or send another invoice.
- **VAT and preflight:** Ilyda line/breakdown categories preserve configured exemption meaning (`E`, `K`, `G`, `AE`, `O`). Shared validation checks series length, supported fuel/VAT codes, decimal precision, VAT arithmetic and fuel-charge rules. The supplied v2.0.0 XSDs are embedded for offline schema validation before host submission/payment preflight; no schema download occurs.
- **POS:** IRIS is restricted to iTimologia with Mellon, in backend preflight and invoice-form selection. Settings show this restriction. The user's confirmed `acq_id` mapping is retained and tested: EDPS/Viva `0`; Mellon/ePay/Cardlink/Nexi `1`. Documentation disagreement is not treated as permission to reverse that mapping.
- **Ilyda TF2:** terminal queue outcomes persist as `QueueFailed`, with raw lookup evidence, queue state and errors, and remain blocked against a fresh invoice POST. The status panel shows intervention separately from ordinary queuing. A documented MQ001/MQ002 response with UID, QR and provider URL can produce a TF2 receipt without fabricating a MARK or acceptance. The frozen receipt includes the QR and transmission-failure warning; reprints retain its pages. Explicit queue recovery still works after printing.
- **Direct myDATA recovery:** paginated `RequestTransmittedDocs` lookup searches the saved issue date, verifies issuer and invoice identity, compares the frozen invoice contents, and checks cancellation evidence. It retains queried pages and confirms acceptance only for one consistent active MARK/UID. Missing, duplicate, mismatched, incomplete-page and transport-error results stay `Unknown`; none causes a POST.

No database migration or new SQL statement was needed for these fixes. New metadata uses the existing append-only journal snapshots and configuration store. SQL Server 2008 R2 compatibility remains required; LocalDB compatibility level 100 is regression coverage, not a native 2008 R2 deployment test.

### Verification after fixes

| Check | Result |
|---|---|
| Core contracts | 37 passed |
| Host mapping, fuel normalization, SupplyNumber and VAT bases | 102 passed |
| Provider rejection, queue, metadata and myDATA recovery | 44 passed |
| POS modes and approved-payment reuse after 401/422/452 | 57 passed |
| Persistence | 26 passed |
| Fiscal signing | 55 passed |
| Workflow, printing, TF2 and provider-only signing | 49 passed |
| Legacy characterization | 98 passed |
| Supplied XSD validation | 25 emitted XML documents passed; invalid business cases blocked before emission |

The eight suites total **468 passing checks**. Logs are `tests/fix-*.log`. The conformance harness now asserts schema/business outcomes and writes to ignored `tests/Invoicing.ConformanceReview/fixed-output/`; its old `output/` remains the original review evidence. The Windows diagnostic compile passes with the existing diagnostic omission of the unrelated broken Unimep reference. Native form preview checks and visual inspection cover the changed controls; they do not start the production application or use its database. No live provider, POS, signer, physical printer or application database was used.

### Remaining boundaries

The later provider-identity/UI update passed 89 workflow, 57 payment and 44 provider checks (190 total), native UI assertions and both Debug/Release project and diagnostic Windows builds. See `tests/provider-identity-*.log` and the [Step 9 identity notes](step-9-ui-configuration.md#pos-modes-and-documentation) for the central catalogue, official AADE sources and UI behavior.

The full deployable solution still has the pre-existing `ControllerTypeEnum.Unimep` blocker. Provider test-environment and representative terminal/printer acceptance remain rollout work. Ilyda's documented pending API covers known TF2 only; an ordinary lost response or missing ordinary print metadata still requires provider reconciliation, with no guessed endpoint or duplicate POST. Older frozen routes retain their serialized licence field for fingerprint compatibility; footer generation now uses the application-owned licence catalogue, while already saved pages remain unchanged. TF1, B2G, broader tax cases, partial refunds, journalled invoice cancellation and other deliberately blocked features remain outside this refactor's supported paths. Ilyda receiving-note fields requiring a newer schema are rejected by the supplied v2.0.0 schema. PDF upload remains deferred.

## Original findings — before fixes

### P1 Include the required provider information in printed invoices

[SharedInvoicePrinting.cs:30](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/ASFuelControl.Windows/Threads/SharedInvoicePrinting.cs:30) appends the available signature, MARK, UID, verification hash and URL. It has no provider licence number, provider name or Ilyda internal invoice ID. The shared response model does not carry these fields, and [IlydaInvoiceProvider.cs:297](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/Exedron.Invoicing.Adapters/IlydaInvoiceProvider.cs:297) drops `invoiceMarking.invoiceId` when mapping the response. PrintAgent's existing `CompanyEFK` licence line identifies the station; it is not the software provider's licence.

The iTimologia guide requires the provider software licence number, provider website and QR on printed invoices (IT1, p. 9). Ilyda requires its name, internal `invoiceId`, website, MARK and both identification/authentication strings (IL1, p. 11, step 9). Normal successful issuance therefore still produces an incomplete provider footer.

There is also no separate print-readiness check: the offline response probes show that iTimologia accepts MARK and UID without authentication or QR data; Ilyda accepts MARK, UID and verification hash without QR or provider URL. `CreateQr` silently returns null for an absent URL.

**Required change:** carry and freeze provider identity/licence metadata and provider invoice ID with the accepted response. Validate the provider-specific print fields before generating the saved pages. If accepted issuance lacks printable metadata, retain its acceptance and require metadata recovery; never send the invoice again merely to obtain its footer or QR. Add content checks for both providers' printed output.

### P1 Classify documented iTimologia rejections as definitive rejections

[ITimologiaInvoiceProvider.cs:488](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/Exedron.Invoicing.Adapters/ITimologiaInvoiceProvider.cs:488) defaults the persisted outcome to `Unknown` and changes it to `Rejected` only for HTTP 401. The guide explicitly defines HTTP 422 as an input rejection and HTTP 452 as a myDATA rejection (IT1, p. 15). The 452 branch also invokes conflict-style lookup.

The offline probes returned each documented rejection, followed by a lookup returning 404. Both remained `Unknown`; repeating the operation performed lookup only, with no corrected POST. [ProviderSubmissionService.cs:58](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/ASFuelControl.Invoicing/ProviderSubmissionService.cs:58) permits correction only after a definitive rejection, so these invoices become stuck. When POS has already approved payment, the approved payment remains attached to an invoice that cannot use the intended rejected-invoice retry path.

**Required change:** distinguish documented negative outcomes from transport uncertainty and preserve their provider/AADE errors. Permit the explicit correction workflow after a durably recorded rejection, preserving invoice identity and any approved payment. Keep timeout, ambiguous response and missing-lookup outcomes blocked against resubmission. Add separate 422 and 452 tests.

### P2 Preserve the VAT exemption meaning in Ilyda JSON

[IlydaInvoiceMapper.cs:74](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/Exedron.Invoicing.Adapters/IlydaInvoiceMapper.cs:74) chooses native VAT category from the rate and AADE VAT category alone. All category-7 exemptions become `E`; the same simplification occurs in the VAT breakdown.

The generated goods invoices demonstrate the mismatch:

| Case | myDATA type and exemption | Native line and breakdown | Documented native category |
|---|---|---|---|
| Intra-community exempt goods | 1.2, exemption 14 | E | K |
| Export outside the EU | 1.3, exemption 8 | E | G |

IL1 pp. 47 and 103 distinguish these UNTDID 5305 meanings; IL4 pp. 57 and 63-64 give their codes. The embedded myDATA XML can be valid while Ilyda's native invoice data identifies a different tax treatment. This is a payload mismatch; live provider rejection was not tested.

**Required change:** resolve the native VAT category from the exemption basis and transaction type as well as the rate. Use the same result for line VAT information and VAT breakdowns. Add cases for domestic exemption, intra-community goods, export, reverse charge and outside-scope treatment where those cases are supported; block unsupported combinations explicitly.

### P2 Validate AADE restrictions before signing or payment

[HostInvoiceMapper.cs:38](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/ASFuelControl.Invoicing.Mapping/HostInvoiceMapper.cs:38) checks that fuel codes are positive and measured in litres. [InvoiceBuilder.cs:242](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/Exedron.Invoicing.Core/InvoiceBuilder.cs:242) copies the code without enforcing its allowed values or the rules for code 999.

The probes generated payloads for all three channels with:

- Fuel code 9999, outside the AADE enumeration.
- A 51-character series, exceeding the XSD maximum of 50.
- Only a fuel-code-999 line, violating AADE rule 268.
- Two code-999 lines, violating rule 270.
- A code-999 line with net value 200 and actual fuel net value 100, violating rule 269.

The first two fail the supplied v2.0.0 XSD. The other cases pass XSD validation but violate the separate business rules in MD1, physical PDF pp. 52 and 80, printed pp. 51 and 79. Current POS preflight calls the serializer, so it does not prevent a signature/payment for these locally detectable invalid invoices.

**Required change:** enforce these finite domain rules in Core and add version-specific payload/schema checks before external fiscal or payment activity. Preserve the distinction between schema validity and business validity. Tests should assert that invalid documents produce zero signature, terminal and invoice requests.

### P2 Restrict IRIS to documented terminal modes

[ProviderPaymentService.cs:118](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/ASFuelControl.Invoicing/ProviderPaymentService.cs:118) accepts iTimologia IRIS without checking `PaymentRoute.PosMode`. EDPS, Viva, ePay, Cardlink and Nexi routes therefore pass this validation as well as Mellon. The outgoing gateway payment translates IRIS to `TransactionData.PaymentType = 1`.

The supplied GBridge complete request template explicitly limits this field's use to Mellon terminals (GB1, line 8). The named mode catalogue describes signature format, but does not expose a payment-method capability restriction. Thus the UI can configure a combination outside the supplied gateway contract. Actual behaviour on other terminals remains unverified.

**Required change:** add payment capabilities to the POS mode definition, disable unsupported IRIS choices in the UI, and enforce the same restriction before requesting a provider signature. Treat historical routes without an identified mode as requiring capability reconciliation rather than inferring the terminal vendor from signature ordering. Broader support needs an updated vendor contract and integration evidence.

### P2 Retain terminal Ilyda queue outcomes

[IlydaInvoiceProvider.cs:148](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/Exedron.Invoicing.Adapters/IlydaInvoiceProvider.cs:148) only persists a recovered `SUBMITTED` result. `SUBMISSION_ERRORS` returns an error without updating the saved `Queued` status; `MAX_RETRIES_REACHED` falls through to the message that the invoice is still waiting in the queue.

IL3 p. 12 states that `SUBMISSION_ERRORS` receives no further automatic resubmissions and that `MAX_RETRIES_REACHED` has exhausted the attempts. Presenting these as ordinary queued work conceals the need for intervention. The HTTP journal retains the exchange, but the durable submission status does not represent it.

**Required change:** retain the provider queue state and errors in a submission revision and show terminal failures distinctly in the UI. Provide a controlled reconciliation path. Do not turn a queue failure into permission for a fresh invoice POST without the provider's documented correction procedure.

## Supported behaviour and unresolved coverage

| Rule family | Review result |
|---|---|
| myDATA namespaces, element order and common invoice shapes | Ordinary synthetic wholesale, retail, service, credit and delivery payloads passed the supplied v2.0.0 XSD. This does not cover every invoice type or business rule. |
| Totals, discounts and saved identity | Host mapping tests verify saved totals, discount treatment, stable identity and rejection of inconsistent source totals. VAT rate-to-amount arithmetic is not comprehensively validated; a 24% line with net 101 and VAT 23 still serializes. |
| Credit correlations | Host tests reject missing, cancelled, uncertain, wrong-customer or wrong-scope parent evidence and preserve the accepted parent MARK. |
| Ilyda XML attachment | Supported by the supplied `test_b2b.json` and `examples_bundle/*mydataxml.json`. Absence of native `aadeData` is **not** a finding: those examples establish the alternative `##InvoicesDoc##` path. Both representations need consistent invoice information when present. |
| Credentials and routing | HTTP/authentication setup is separated from invoice payloads. Accepted and uncertain attempts retain their original provider, environment and account route. |
| Retry safety | Existing provider/POS tests verify persist-before-dispatch, no repeated charge after uncertainty, approved-payment reuse, and immutable invoice/terminal routing. iTimologia definitive rejection handling remains defective as above. |
| Ilyda POS formats | The catalogue includes the nine named modes in IL2 pp. 27-28; EDPS maps to DEFAULT. The adapter handles documented Base64/hex conversion and requires a single VAT rate for VIVA/WORLDLINE. End-to-end terminal acceptance for every mode remains unverified. |
| iTimologia acq_id | IT2 p. 7 gives selector examples opposite to the code comment describing captured endpoint behaviour on 2026-10-14. Existing mocks implement the code's convention and cannot resolve that discrepancy. Obtain vendor confirmation or retained test-endpoint evidence before changing or certifying the mapping. |
| Direct myDATA recovery | [MyDataClient.cs:85](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/Exedron.Invoicing.Adapters/MyDataClient.cs:85) reuses accepted evidence but cannot reconcile a lost submission response. `RequestTransmittedDocs` exists as a raw call and is not wired into recovery. Complete paginated, identity-checked reconciliation before calling this workflow complete. A missing result must not by itself authorize another send. MD1 section 4.2.7 describes the API. |
| Ilyda ordinary response loss | Queue lookup is correctly limited to known TF2 queue entries, as IL3 p. 7 requires. A normal invoice POST whose response is lost still requires reconciliation; the queue API cannot prove that it was not issued. |
| Offline issuance | The workflow requires acceptance before printing. It does not deliver Ilyda's documented TF2 customer document containing the queued QR (IL3 p. 4), or a complete TF1 offline issuance/retransmission process. These are incomplete features, not support demonstrated by the current queued state. |
| Cancellation and advanced cases | Full API coverage is not implemented: journalled invoice cancellation, B2G, broader tax/withholding/exemption combinations, split/deferred payments, partial refunds and some device/POS routes remain blocked or unconnected. Their deliberate blocking must not be described as vendor conformance for those scenarios. |
| PDF upload | Explicitly deferred by the user. No upload work is required by this review's action list. |

The older Ilyda guide and newer myDATA ERP document do not establish that every v2.0.0 ERP field is available through every provider. The receiving-note adapter even references v2.0.2 fields; that branch is outside the supplied v2.0.0 verification. Provider-version negotiation and real test-environment acceptance remain necessary for any broader scope.

## Verification evidence

Fresh execution during this review:

| Check | Result |
|---|---|
| Existing host mapping suite | 80 passed |
| Existing provider integration suite | 32 passed |
| Existing POS payment suite | 44 passed |
| Offline conformance probes | 16 synthetic invoice cases; 47 payloads generated and one deliberately unsupported Ilyda VAT-suspension case blocked |
| Supplied AADE XSD checks | 27 XML payloads passed; 4 failed across the two invalid-code/oversize-series cases and their Ilyda embedded copies |
| Fuel business rules | Three generated scenarios violated rules 268, 269 or 270 despite being XSD-valid |
| Response probes | 422/452 persisted Unknown; incomplete print metadata did not prevent accepted iTimologia/Ilyda responses |

The 156 existing passing checks establish their tested behaviour, not complete compliance. The conformance harness records observations and returns successfully when its execution completes; its exit code is **not** a conformance pass. Inspect its JSON results.

The harness is [run.py](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/tests/Invoicing.ConformanceReview/run.py), with [Cases.cs](E:/Exedron/2024/ASFuelControl/ASFuelControlV1_initial/tests/Invoicing.ConformanceReview/Cases.cs). It uses synthetic host fixtures, compiled real mappers/adapters and fake HTTP. Run the existing mapping build first, then execute `run.py` with Python, `lxml`, Visual Studio's C# compiler and the supplied XSD directory available. Results are written to its ignored `output` directory. Review logs are in `tests/review-mapping.log`, `tests/review-providers.log`, `tests/review-payments.log` and `tests/review-conformance.log`.

The provider and payment suites used disposable private LocalDB databases at compatibility level 100; both instances were removed. No production code, SQL scripts, application database, live provider, terminal or printer was changed or used by this review. Native SQL Server 2008 R2 execution and the complete deployed Windows application remain outside these checks.

## Suggested implementation order

1. Correct iTimologia rejection classification and preserve approved-payment recovery.
2. Complete provider print metadata and add a separate print-readiness gate.
3. Add AADE preflight restrictions and correct Ilyda VAT category mapping.
4. Enforce POS payment capabilities and resolve the `acq_id` discrepancy with evidence.
5. Complete queue-state handling, direct myDATA reconciliation and the agreed offline issuance scope.
6. Run provider test-environment scenarios and representative terminal/print checks for each enabled mode. Keep PDF upload deferred.

Keep subsequent database changes and statements compatible with SQL Server 2008 R2. This review introduces no migration or SQL statement.

## Reference documents

Page numbers below refer to physical PDF pages unless a printed page is explicitly identified.

- **MD1:** [myDATA ERP API v2.0.0](<E:/Exedron/2020/AADE/myDATA API Documentation v2.0.0_official_erp.pdf>), especially section 4.2.7 and pp. 52, 80. Schema: [InvoicesDoc v2.0.0](E:/Exedron/2020/AADE/XSD/v2/InvoicesDoc-v2.0.0.xsd) and its sibling imports.
- **IT1:** [iTimologia technical methodology](<E:/Exedron/2026/ASSalesManager/docs/iTimologia/iTimologia Development Files/1α Έκδοση στοιχείων_τεχνική μεθοδολογία_αρ.15 ν.4308 2014.pdf>), pp. 9 and 15.
- **IT2:** [iTimologia DLL v2.0.1](<E:/Exedron/2026/ASSalesManager/docs/iTimologia/iTimologia Development Files/iTimologia DLLVer_2.0.1.pdf>), p. 7.
- **IL1:** [Ilyda eInvoicing implementation](<E:/Exedron/2026/ASSalesManager/docs/Ilyda/Οδηγίες υλοποίησης eInvoicing v1.pdf>), pp. 11, 47, 68-72 and 103; [B2B XML attachment example](E:/Exedron/2026/ASSalesManager/docs/Ilyda/test_b2b.json).
- **IL2:** [Ilyda A1155 POS v1.3.2](<E:/Exedron/2026/ASSalesManager/docs/Ilyda/Οδηγίες υλοποίησης A1155 POS v1.3.2.pdf>), especially pp. 27-28.
- **IL3:** [Ilyda offline TF2 handling](<E:/Exedron/2026/ASSalesManager/docs/Ilyda/Οδηγίες διαχείρισης offline παραστατικών μετά από transmission failure 2-1.pdf>), pp. 3-7 and 12.
- **IL4:** [National format and PEPPOL guide v7.3](<E:/Exedron/2026/ASSalesManager/docs/Ilyda/PEPPOL BIS_v 7.3-(ΗΤ_Οδηγίες Προμηθευτών και Παρόχων για τον Εθνικό Μορφότυπο).pdf>), pp. 57 and 63-64, used for the UNTDID VAT code meanings referenced by IL1.
- **GB1:** [GBridge complete request template](<E:/Exedron/2026/ASSalesManager/docs/GBridge/Gbridge Development files/Complete_Send_Template.json:8>), PaymentType comment.
