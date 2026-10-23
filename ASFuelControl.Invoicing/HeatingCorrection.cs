using System;
using System.Linq;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing
{
    public sealed class HeatingCorrectionPlan
    {
        public Guid OriginalId, CreditId, ReplacementId, OriginalTypeId, CreditTypeId, OriginalAttemptId;
        public string OriginalDescription, CreditSeries, ReplacementSeries;
        public long OriginalMark;
        public InvoiceWorkflowSnapshot Original;
        public JObject Host;

        public InvoiceBuildRequest Request(bool credit,int number,string series,DateTime date)
        {
            var request=Copy(Original.Invoice);
            request.Invoice.InvoiceId=credit?CreditId:ReplacementId;
            request.Invoice.Number=number;request.Invoice.Series=series;request.Invoice.PrintedDateTime=date;
            request.Invoice.InvoiceSeries.Series=series;
            // No cash refund/recollection: the two correction documents offset one another.
            request.Invoice.PaymentType=InvoicePaymentType.Credit;
            request.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode=credit?InvoiceMode.Credit:InvoiceMode.Normal;
            request.Parents=credit?new[]{new InvoiceParent {Invoice=Copy(Original.Invoice.Invoice),Transmission=new TransmissionEvidence {Status=3,Mark=OriginalMark}}}:new InvoiceParent[0];
            request.SaleCorrelations=null;
            request.RequireIssuedIdentity=true;
            return request;
        }
        public static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }

    public static class HeatingCorrectionPolicy
    {
        public static void Validate(HeatingCorrectionPlan plan,string originalPayload)
        {
            if(plan?.Original?.Invoice==null||plan.Original.Provider==null||plan.OriginalMark<=0||plan.OriginalAttemptId==Guid.Empty)
                throw new InvalidOperationException("Απαιτείται αποθηκευμένη και αποδεκτή αρχική διαβίβαση.");
            string provider=plan.Original.Provider.ProviderKey;
            if(provider!=InvoiceProviderCatalog.ITimologiaKey && provider!=InvoiceProviderCatalog.DirectIlydaKey)
                throw new InvalidOperationException("Η παρτίδα αφορά μόνο iTimologia και Ilyda χωρίς Arbitrans.");
            if(plan.Original.Payment!=null||plan.Original.Signing!=null||
                plan.Original.Invoice.Invoice.PaymentType!=InvoicePaymentType.Cash && plan.Original.Invoice.Invoice.PaymentType!=InvoicePaymentType.Credit)
                throw new InvalidOperationException("Εξαιρείται: πληρωμή POS/IRIS/στόλου ή πρόσθετη σήμανση.");
            if(plan.Original.Invoice.Movement!=null||plan.Original.Invoice.Delivery!=null||plan.Original.Invoice.Invoice.InvoiceSeries.IsDeliveryNote||
                plan.Original.Invoice.SaleCorrelations?.Any()==true||plan.Original.Invoice.Parents?.Any()==true)
                throw new InvalidOperationException("Εξαιρείται: απαιτείται ιδιαίτερη συμφωνία διακίνησης ή συσχετισμένων παραστατικών.");
            var builder=new InvoiceBuilder(new InvoiceTypeResolver());
            var model=builder.Build(plan.Original.Invoice);
            if(plan.Original.Invoice.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode!=InvoiceMode.Normal||
                MyDataHeaderFields.SpecialInvoiceCategory(model)!=11||model.InvoiceSummary.TotalGrossValue<=0)
                throw new InvalidOperationException("Δεν είναι αρχική πώληση που πληροί τους όρους επιδόματος θέρμανσης.");
            var sent=JObject.Parse(originalPayload);
            if(provider==InvoiceProviderCatalog.ITimologiaKey) {
                if(sent["invoice_header"]==null)throw new InvalidOperationException("Δεν αναγνωρίζεται το αρχικό αίτημα.");
                RequireText(sent["invoice_header"]["invoice_type"],model.InvoiceHeader.InvoiceType);
                RequireText(sent["invoice_header"]["aa"],model.InvoiceHeader.AA);
                RequireText(sent["invoice_header"]["series"],model.InvoiceHeader.Series);
                RequireText(sent["issuer"]?["vat_number"],model.Issuer.VATNumber);
                RequireAmount(sent["invoice_summary"]?["total_gross_value"],model.InvoiceSummary.TotalGrossValue);
                RequireAmount(sent["invoice_summary"]?["total_net_value"],model.InvoiceSummary.TotalNetValue);
                RequireAmount(sent["invoice_summary"]?["total_vat_amount"],model.InvoiceSummary.TotalVATAmount);
                if(sent["invoice_header"]["correlated_invoices"]?.HasValues==true)throw new InvalidOperationException("Το αρχικό αίτημα έχει συσχετίσεις.");
                if(sent["invoice_header"]["special_invoice_category"]?.Type!=null && sent["invoice_header"]["special_invoice_category"].Type!=JTokenType.Null)
                    throw new InvalidOperationException("Το αρχικό αίτημα έχει ήδη ειδική κατηγορία παραστατικού.");
            } else {
                if(sent["aadeData"]?["aadeSpecialInvoiceCategory"]!=null && sent["aadeData"]["aadeSpecialInvoiceCategory"].Type!=JTokenType.Null)
                    throw new InvalidOperationException("Το αρχικό αίτημα έχει ήδη ειδική κατηγορία παραστατικού.");
                var docs=sent["additionalSupportDocs"] as JArray;
                var doc=docs?.SingleOrDefault(d=>(string)d["description"]=="##InvoicesDoc##");
                if(doc==null)throw new InvalidOperationException("Λείπει το αρχικό XML του Ilyda.");
                var xml=System.Xml.Linq.XDocument.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String((string)doc["reference"])));
                var header=xml.Descendants().Single(e=>e.Name.LocalName=="invoiceHeader");
                RequireText(header.Elements().Single(e=>e.Name.LocalName=="invoiceType").Value,model.InvoiceHeader.InvoiceType);
                RequireText(header.Elements().Single(e=>e.Name.LocalName=="aa").Value,model.InvoiceHeader.AA);
                RequireText(header.Elements().Single(e=>e.Name.LocalName=="series").Value,model.InvoiceHeader.Series);
                RequireText(sent["serialNumber"],model.InvoiceHeader.AA);RequireText(sent["seriesNumber"],model.InvoiceHeader.Series);
                RequireText(sent["seller"]?["sellerVatIdentifier"],model.Issuer.VATNumber);
                RequireAmount(sent["docTotal"]?["invoiceTotalAmountWithVat"],model.InvoiceSummary.TotalGrossValue);
                RequireAmount(sent["docTotal"]?["invoiceTotalWithoutVat"],model.InvoiceSummary.TotalNetValue);
                RequireAmount(sent["docTotal"]?["invoiceTotalVatAmount"],model.InvoiceSummary.TotalVATAmount);
                if(sent["precedingInvoices"]?.HasValues==true)throw new InvalidOperationException("Το αρχικό αίτημα έχει συσχετίσεις.");
                if(xml.Descendants().Any(e=>e.Name.LocalName=="specialInvoiceCategory"))throw new InvalidOperationException("Το αρχικό XML έχει ήδη ειδική κατηγορία παραστατικού.");
            }
            foreach(bool credit in new[]{true,false}) {
                var corrected=builder.Build(plan.Request(credit,1,credit?plan.CreditSeries:plan.ReplacementSeries,DateTime.Now));
                if(MyDataHeaderFields.SpecialInvoiceCategory(corrected)!=11)throw new InvalidOperationException("Η διόρθωση δεν παράγει την ένδειξη 11.");
                new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper(provider).Serialize(corrected,credit?plan.CreditId:plan.ReplacementId);
            }
        }
        private static void RequireText(JToken actual,string expected)
        {
            if((string)actual!=expected)throw new InvalidOperationException("Η ταυτότητα του αρχικού αιτήματος διαφέρει από την αποθηκευμένη έκδοση.");
        }
        private static void RequireAmount(JToken actual,decimal expected)
        {
            decimal amount;
            if(actual==null||!decimal.TryParse((string)actual,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out amount)||amount!=expected)
                throw new InvalidOperationException("Τα ποσά του αρχικού αιτήματος διαφέρουν από την αποθηκευμένη έκδοση.");
        }
    }

    // The batch UI runs this same state machine once per selected row. No automatic retry loops.
    public static class HeatingCorrectionRunner
    {
        public static void Run(Func<bool,InvoiceWorkflowResult> issue,Action<string,string> save,Func<bool> paused)
        {
            if(paused())return;
            var credit=issue(true);
            if(!credit.Accepted) {save("CreditPending",Describe("Πιστωτικό",credit));return;}
            save("CreditAccepted","Το πιστωτικό έγινε αποδεκτό. Εκκρεμεί η επανέκδοση.");
            if(paused())return;
            var replacement=issue(false);
            if(!replacement.Accepted) {save("ReplacementPending",Describe("Επανέκδοση",replacement));return;}
            save("Completed","Ολοκληρώθηκε η πίστωση και επανέκδοση.");
        }
        private static string Describe(string stage,InvoiceWorkflowResult result) => stage+": "+result.FiscalState+". "+string.Join("; ",result.Response?.Errors??new string[0]);
    }
}
