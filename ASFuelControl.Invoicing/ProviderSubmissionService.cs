using System;
using System.Linq;
using System.Net.Http;
using System.Globalization;
using System.Xml.Linq;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing
{
    public sealed class ProviderSubmissionResult
    {
        public Guid? AttemptId { get; internal set; }
        // NotSubmitted, Sending, Unknown, Queued, Rejected or Accepted; independent of print/payment.
        public string State { get; internal set; }
        public InvoiceResponse Response { get; internal set; }
        public bool Accepted => State == "Accepted" && Response != null && !Response.HasErrors;
    }

    // Synchronous on purpose: SqlFiscalDatabase's document lease is thread-affine.
    public sealed class ProviderSubmissionService
    {
        private readonly SqlFiscalDatabase database;
        private readonly SqlFiscalHistoryStore history;
        private readonly IProviderCredentialsResolver credentials;
        private readonly Func<ProviderRoute, HttpMessageHandler> transport;
        private readonly InvoiceAdapterFactory factory = new InvoiceAdapterFactory(new InvoiceTypeResolver());

        public ProviderSubmissionService(SqlFiscalDatabase database, IProviderCredentialsResolver credentials,
            Func<ProviderRoute, HttpMessageHandler> transport = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            this.transport = transport;
            history = new SqlFiscalHistoryStore(database);
        }

        public ProviderSubmissionResult Send(Guid invoiceId, ProviderRoute route, IInvoiceSubmissionSource source,
            bool retryRejected = false)
        { return SendCore(invoiceId, route, source, retryRejected, null); }

        // Only the composed host workflow may cross signing -> direct myDATA.
        internal ProviderSubmissionResult SendAfterSigning(Guid invoiceId, ProviderRoute route,
            InvoiceWorkflowSnapshot source, Guid signingAttemptId)
        { return SendCore(invoiceId, route, source, false, signingAttemptId); }

        private ProviderSubmissionResult SendCore(Guid invoiceId, ProviderRoute route, IInvoiceSubmissionSource source,
            bool retryRejected, Guid? signingAttemptId)
        {
            using (var lease = database.EnterInvoice(invoiceId))
            {
                var previous = Latest(lease);
                if (previous != null && previous.State != "Rejected")
                    return Recover(lease, invoiceId, previous); // No remapping or current-setting fallback.
                if (previous != null && !retryRejected)
                    return Result(previous, Error("The previous attempt was rejected. An explicit corrected retry is required."));
                Require(route != null && source != null, "A route and host invoice source are required.");
                // These stages are integrated in steps 6-8. Do not bypass an old/unknown
                // payment, signature, cancellation or print by switching the invoice provider.
                var signs = history.Load(lease, FiscalJournalFamily.Signing);
                if (signingAttemptId.HasValue)
                {
                    var snapshot = source as InvoiceWorkflowSnapshot;
                    Require(route?.ProviderKey == InvoiceProviderCatalog.MyDataKey && snapshot?.Signing != null &&
                        snapshot.Signing.AttemptId == signingAttemptId && signs.LastOrDefault()?.AttemptId == signingAttemptId &&
                        signs.Last().State == "Signed" && signs.Last().RequestPayload.SequenceEqual(snapshot.Signing.Payload),
                        "Direct myDATA requires the original workflow's durable signature.");
                }
                else Require(signs.Count == 0, "Existing signing history requires the composed workflow.");
                foreach (var family in new[] { FiscalJournalFamily.ProviderSession,
                    FiscalJournalFamily.Payment, FiscalJournalFamily.Cancellation, FiscalJournalFamily.Print })
                    Require(history.Load(lease, family).Count == 0,
                        "Existing " + family + " history requires the complete fiscal workflow before submission.");
                var request = source.Prepare(invoiceId, route);
                Require(request?.Invoice != null && request.Invoice.InvoiceId == invoiceId && request.RequireIssuedIdentity,
                    "The host returned a different or unissued invoice.");
                Require(request.Company != null && request.Company.CompanyTaxId?.Trim() == route.IssuerVat,
                    "The invoice issuer does not match the selected route.");
                var model = new InvoiceBuilder(new InvoiceTypeResolver()).Build(request);
                Require(model.Issuer.Branch == route.IssuerBranch, "The invoice branch does not match the selected route.");
                Require(!(model.PaymentMethods ?? new PaymentMethod[0]).Any(p => (int)p.Type == 7 || (int)p.Type == 8),
                    "Card/IRIS issuance requires a configured payment workflow; no submission was attempted.");
                // Validate all channel-specific limitations before authentication or durable dispatch.
                factory.CreatePayloadMapper(route.ProviderKey).Serialize(model, invoiceId);
                string routingContext = Context(route, model);
                if (previous != null)
                    Require(ReadContext(previous).ToString(Formatting.None) == routingContext,
                        "A rejected retry must retain its original route and fiscal identity.");
                using (var provider = Open(route, routingContext, invoiceId))
                {
                    var response = provider.SendInvoice(model, invoiceId, null);
                    return Result(Latest(lease), response);
                }
            }
        }

        public ProviderSubmissionResult Recover(Guid invoiceId)
        {
            using (var lease = database.EnterInvoice(invoiceId))
            {
                var saved = Latest(lease);
                Require(saved != null, "There is no saved submission to recover. Reconcile legacy history separately.");
                return Recover(lease, invoiceId, saved);
            }
        }

        // Read-only bridge for MappingContext.Parents. No credentials or provider call.
        // A legacy MARK or an uncertain cancellation is never upgraded to accepted evidence.
        public ParentEvidence LoadParentEvidence(Guid invoiceId)
        {
            using (var lease = database.EnterInvoice(invoiceId))
            {
                var saved = Latest(lease);
                Require(saved != null && saved.State == "Accepted", "Parent submission has not been durably accepted.");
                Require(!history.Load(lease, FiscalJournalFamily.Cancellation).Any(a => a.State != "Rejected" && a.State != "FailedBeforeDispatch"),
                    "Parent cancellation is accepted or unresolved; it cannot be correlated.");
                var route = ReadContext(saved)["Route"].ToObject<ProviderRoute>();
                var snapshot = JObject.Parse(saved.SnapshotJson);
                string value;
                if (route.ProviderKey == InvoiceProviderCatalog.MyDataKey)
                    value = XDocument.Parse((string)snapshot["ResponseXml"]).Descendants().Single(e => e.Name.LocalName == "invoiceMark").Value;
                else
                {
                    var response = JObject.Parse((string)snapshot["ResponseJson"]);
                    value = route.ProviderKey == InvoiceProviderCatalog.ITimologiaKey
                        ? (string)response["data"]?["mark"] : (string)response["invoiceMarking"]?["mark"];
                }
                long mark;
                Require(long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out mark) && mark > 0, "Parent acceptance has no valid MARK.");
                return new ParentEvidence { InvoiceId = invoiceId, IssuerVat = route.IssuerVat, IssuerBranch = route.IssuerBranch,
                    Environment = route.TestMode ? FiscalEnvironment.Test : FiscalEnvironment.Production,
                    Transmission = new TransmissionEvidence { Status = 3, Mark = mark } };
            }
        }

        public void UploadIlydaPdf(Guid invoiceId, byte[] pdf)
        {
            using (var lease = database.EnterInvoice(invoiceId))
            {
                var saved = Latest(lease);
                Require(saved != null && saved.State == "Accepted", "PDF upload requires a durably accepted invoice.");
                var context = ReadContext(saved);
                var route = context["Route"].ToObject<ProviderRoute>();
                Require(route.ProviderKey == InvoiceProviderCatalog.DirectIlydaKey, "The saved route does not support Ilyda PDF upload.");
                using (var provider = Open(route, context.ToString(Formatting.None), invoiceId))
                    ((IInvoiceDocumentUpload)provider).UploadInvoiceDocument(invoiceId, pdf);
            }
        }

        private ProviderSubmissionResult Recover(IFiscalDocumentLease lease, Guid invoiceId, FiscalAttemptEvidence saved)
        {
            var context = ReadContext(saved);
            if (saved.State == "Rejected") return Result(saved, Error("This attempt was rejected; recovery does not resubmit it."));
            var route = context["Route"].ToObject<ProviderRoute>();
            using (var provider = Open(route, context.ToString(Formatting.None), invoiceId))
            {
                var response = ((IInvoiceProviderRecovery)provider).RecoverInvoice(invoiceId);
                return Result(Latest(lease), response);
            }
        }

        internal IInvoiceProvider Open(ProviderRoute route, string routingContext, Guid invoiceId, PaymentRoute paymentRoute = null)
        {
            var secret = credentials.Resolve(route);
            Require(secret != null, "Credentials for the original provider route are unavailable.");
            var logger = new InvoiceLogger(new SqlEvidenceLogger(database), invoiceId);
            var handler = transport?.Invoke(route);
            switch (route.ProviderKey)
            {
                case InvoiceProviderCatalog.MyDataKey:
                    Require(!string.IsNullOrWhiteSpace(secret.SubscriptionKey), "The original myDATA subscription key is unavailable.");
                    return new MyDataClient(new MyDataOptions { TestMode = route.TestMode, BaseUrl = route.MyDataBaseUrl,
                        UserId = route.Account, SubscriptionKey = secret.SubscriptionKey, Logger = logger,
                        Store = new SqlMyDataSubmissionStore(database, routingContext) }, new InvoiceXmlSerializer(), handler);
                case InvoiceProviderCatalog.ITimologiaKey:
                    Require(!string.IsNullOrWhiteSpace(secret.MasterToken), "The original iTimologia master token is unavailable.");
                    return new ITimologiaInvoiceProvider(new ITimologiaProviderOptions { TestMode = route.TestMode,
                        IssuerVat = route.IssuerVat, SubscriptionId = route.Account, MasterToken = secret.MasterToken,
                        PosTerminalId = paymentRoute?.TerminalId, PosSignatureFormat = paymentRoute?.ITimologiaSignatureFormat ?? 0,
                        Logger = logger, SubmissionStore = new SqlITimologiaSubmissionStore(database, routingContext) }, handler);
                case InvoiceProviderCatalog.DirectIlydaKey:
                    Require(!string.IsNullOrWhiteSpace(secret.ApiKey) || !string.IsNullOrWhiteSpace(secret.Password),
                        "The original Ilyda credentials are unavailable.");
                    return new IlydaInvoiceProvider(new IlydaProviderOptions { TestMode = route.TestMode, IssuerVat = route.IssuerVat,
                        ApiKey = secret.ApiKey, UserName = route.Account, Password = secret.Password, Logger = logger,
                        PosTerminalId = paymentRoute?.TerminalId, NspProtocol = paymentRoute?.IlydaProtocol,
                        PosStore = paymentRoute == null ? null : new SqlIlydaPosStore(database),
                        Store = new SqlIlydaSubmissionStore(database, routingContext), XmlSerializer = new InvoiceXmlSerializer() }, handler);
                default: throw new InvalidOperationException("Unsupported saved provider route.");
            }
        }

        private FiscalAttemptEvidence Latest(IFiscalDocumentLease lease) => history.Load(lease, FiscalJournalFamily.Submission).LastOrDefault();
        private static JObject ReadContext(FiscalAttemptEvidence saved)
        {
            string json = (string)JObject.Parse(saved.SnapshotJson)["RoutingContext"];
            Require(!string.IsNullOrWhiteSpace(json), "The saved attempt has no verified historical routing context; reconcile it before proceeding.");
            var value = JObject.Parse(json);
            Require((int?)value["Version"] == 1 && value["Route"] != null && value["Identity"] != null,
                "Unsupported or incomplete historical routing context.");
            var route = value["Route"].ToObject<ProviderRoute>();
            Require(saved.Scope.StartsWith(route.ProviderKey + "|", StringComparison.Ordinal), "Historical provider metadata does not match the saved attempt.");
            return value;
        }
        internal static string Context(ProviderRoute route, InvoiceModel model) => JsonConvert.SerializeObject(new
        {
            Version = 1, Route = route,
            Identity = new { model.InvoiceHeader.Series, model.InvoiceHeader.AA, model.InvoiceHeader.IssueDate, model.InvoiceHeader.InvoiceType }
        });
        internal static ProviderSubmissionResult Result(FiscalAttemptEvidence saved, InvoiceResponse response)
        {
            string state = saved?.State ?? "NotSubmitted";
            if (state != "Accepted" && response != null && !response.HasErrors)
                response = Error("The provider response has not been durably confirmed. Recover the saved attempt before continuing.");
            return new ProviderSubmissionResult { AttemptId = saved?.AttemptId, State = state,
                Response = response ?? Error("The provider returned no confirmed response.") };
        }
        private static InvoiceResponse Error(string message) => new InvoiceResponse { Errors = new[] { message }, Warnings = new string[0] };
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        internal sealed class InvoiceLogger : IRequestResponseLogger
        {
            private readonly IRequestResponseLogger inner;
            private readonly Guid invoiceId;
            internal InvoiceLogger(IRequestResponseLogger inner, Guid invoiceId) { this.inner = inner; this.invoiceId = invoiceId; }
            public void RequestStarted(InvoiceRequestLog entry) { entry.InvoiceId = invoiceId; inner.RequestStarted(entry); }
            public void ResponseReceived(InvoiceResponseLog entry) { inner.ResponseReceived(entry); }
        }
    }
}
