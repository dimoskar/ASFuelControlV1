using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class HeatingBatchRow
    {
        public bool Selected { get; set; }
        public string Document { get; set; }
        public string Provider { get; set; }
        public string Supply { get; set; }
        public decimal Amount { get; set; }
        public string Credit { get; set; }
        public string Replacement { get; set; }
        public string Status { get; set; }
        public bool Eligible { get; set; }
        internal Guid Id;
        internal HeatingCorrectionPlan Plan;
    }
    internal sealed class HeatingCreditType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool Retail { get; set; }
    }
    internal static partial class SharedInvoiceRuntime
    {
        internal static bool IsHeatingBatchChild(Guid id) => new SqlHeatingCorrectionStore(sql.Value).IsChild(id);
        private static string HeatingProvider(ProviderRoute route) => InvoiceProviderCatalog.Get(route.ProviderKey).DisplayName+
            (route.TestMode?" / Δοκιμαστικό":" / Παραγωγικό");
        internal static Guid RequireHeatingAdmin()
        {
            var id=Data.DatabaseModel.UserLoggedIn;
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                if(!db.ApplicationUsers.Any(u=>u.ApplicationUserId==id&&u.UserLevel==0))
                    throw new InvalidOperationException("Η διόρθωση επιδόματος θέρμανσης επιτρέπεται μόνο σε συνδεδεμένο διαχειριστή.");
            return id;
        }
        internal static HeatingCreditType[] HeatingCreditTypes()
        {
            RequireHeatingAdmin();
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                return db.InvoiceTypes.Where(t=>t.Printable&&t.Invalidated!=true&&t.IsCancelation==true&&t.OfficialEnumerator!=215).ToArray()
                    .Select(t=>new HeatingCreditType{Id=t.InvoiceTypeId,Name=t.Description+" / "+t.DefaultSeries,Retail=t.RetailInvoice==true}).ToArray();
        }
        internal static HeatingBatchRow[] ScanHeating(DateTime from,DateTime to,Guid retailCredit,Guid wholesaleCredit,Func<bool> pause=null)
        {
            RequireHeatingAdmin();if(to.Date<from.Date)throw new ArgumentException("Μη έγκυρο διάστημα ημερομηνιών.");
            var store=new SqlHeatingCorrectionStore(sql.Value);var saved=store.Load();var results=new List<HeatingBatchRow>();
            Guid[] ids;DateTime end=to.Date.AddDays(1);
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                ids=db.Invoices.Where(i=>i.TransactionDate>=from.Date&&i.TransactionDate<end&&
                    i.InvoiceLines.Any(l=>l.FuelType.EnumeratorValue==30||l.FuelType.EnumeratorValue==31)).Select(i=>i.InvoiceId).ToArray();
            foreach(var id in ids.Concat(saved.Where(r=>r.State!="Completed").Select(r=>r.OriginalInvoiceId)).Distinct()) {
                if(pause?.Invoke()==true)break;
                var existing=saved.SingleOrDefault(r=>r.OriginalInvoiceId==id);
                if(existing!=null){results.Add(HeatingSavedRow(existing));continue;}
                using(var lease=sql.Value.EnterInvoice(id))
                using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection)) {
                    var invoice=db.Invoices.Single(i=>i.InvoiceId==id);
                    // Only potential heating documents belong in this specialist report.
                    var bytes=new SqlInvoiceWorkflowStore(sql.Value).Load(lease);
                    var snapshot=bytes==null?null:JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(bytes));
                    if(snapshot==null) {
                        results.Add(new HeatingBatchRow{Id=id,Document=invoice.Description,Supply=invoice.SupplyNumber,Amount=invoice.TotalAmount??0,
                            Status="Εξαιρείται: δεν υπάρχει αποθηκευμένη κοινή έκδοση για ασφαλή επαλήθευση του αρχικού αιτήματος."});
                        continue;
                    }
                    if(snapshot.Invoice?.Invoice?.InvoiceLines.Any(l=>l.OfficialFuelCode==30||l.OfficialFuelCode==31)!=true)continue;
                    if(snapshot.Provider?.ProviderKey!=InvoiceProviderCatalog.ITimologiaKey&&snapshot.Provider?.ProviderKey!=InvoiceProviderCatalog.DirectIlydaKey)continue;
                    var row=new HeatingBatchRow{Id=id,Document=invoice.Description,Provider=HeatingProvider(snapshot.Provider),Supply=invoice.SupplyNumber,Amount=invoice.TotalAmount??0};
                    try {
                        row.Plan=PrepareHeatingPlan(lease,invoice,snapshot,retailCredit,wholesaleCredit,store);
                        row.Provider=HeatingProvider(row.Plan.Original.Provider);
                        row.Eligible=true;row.Selected=true;row.Status="Έτοιμο — λείπει η ένδειξη 11";
                    } catch(Exception ex){row.Status="Εξαιρείται: "+ex.Message;}
                    results.Add(row);
                }
            }
            return results.ToArray();
        }
        private static HeatingCorrectionPlan PrepareHeatingPlan(IFiscalDocumentLease lease,Data.Invoice invoice,InvoiceWorkflowSnapshot snapshot,
            Guid retailCredit,Guid wholesaleCredit,SqlHeatingCorrectionStore store)
        {
            var history=new SqlFiscalHistoryStore(sql.Value);
            var submission=history.Load(lease,FiscalJournalFamily.Submission).LastOrDefault();
            if(submission?.State!="Accepted")throw new InvalidOperationException("Η αρχική έκδοση δεν έχει επιβεβαιωθεί. Απαιτείται ανάκτηση κατάστασης.");
            if(invoice.ChildInvoiceRelations.Any()||invoice.ParentInvoiceRelations.Any())throw new InvalidOperationException("Υπάρχουν ήδη συσχετισμένα παραστατικά / πιστώσεις.");
            if(new[]{FiscalJournalFamily.Cancellation,FiscalJournalFamily.Signing,FiscalJournalFamily.Payment,FiscalJournalFamily.ProviderSession}.Any(f=>history.Load(lease,f).Any()))
                throw new InvalidOperationException("Υπάρχει ιστορικό POS, σήμανσης ή ακύρωσης που απαιτεί συμφωνία.");
            var providers=new ProviderSubmissionService(sql.Value,new CurrentCredentials());
            var evidence=providers.LoadParentEvidence(invoice.InvoiceId);
            var route=JObject.Parse((string)JObject.Parse(submission.SnapshotJson)["RoutingContext"])["Route"].ToObject<ProviderRoute>();
            snapshot=HeatingCorrectionPlan.Copy(snapshot);snapshot.Provider=route;
            new CurrentCredentials().Resolve(route); // Check retained profile without authenticating/sending.
            bool retail=snapshot.Invoice.Invoice.InvoiceSeries.InvoiceType.IsRetail;
            Guid creditId=retail?retailCredit:wholesaleCredit;
            var config=SharedInvoicingSettings.LoadRuntime();
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection)) {
                var credit=db.InvoiceTypes.SingleOrDefault(t=>t.InvoiceTypeId==creditId);
                if(credit==null||credit.IsCancelation!=true||credit.OfficialEnumerator==215||!credit.Printable||credit.Invalidated==true||
                    credit.ForcesDelivery==true||credit.TransactionType!=0||invoice.InvoiceType.TransactionType!=0||
                    credit.HasFinancialTransactions==false||invoice.InvoiceType.HasFinancialTransactions==false||
                    (credit.RetailInvoice==true)!=retail)
                    throw new InvalidOperationException("Επιλέξτε οικονομικό πιστωτικό πωλήσεων (κίνηση εξαγωγής), χωρίς υποχρεωτική διακίνηση, του αντίστοιχου τύπου (λιανική / χονδρική).");
                // A dedicated historical route is used, but the document must still be configured for provider issuance.
                var creditRoute=config.Route(credit.InvoiceTypeId,credit.Printer);
                var originalRoute=config.Route(invoice.InvoiceTypeId,invoice.InvoiceType.Printer);
                if(creditRoute.LocalOnly||creditRoute.Signing!=null||creditRoute.Provider==null||originalRoute.LocalOnly||originalRoute.Signing!=null||originalRoute.Provider==null)
                    throw new InvalidOperationException("Οι τύποι πιστωτικού και επανέκδοσης πρέπει να έχουν διαβίβαση σε πάροχο χωρίς φορολογικό μηχανισμό.");
                if(invoice.PaymentType!=0&&invoice.PaymentType!=1)throw new InvalidOperationException("Η παρτίδα δέχεται μόνο μετρητά ή επί πιστώσει. Οι πληρωμές POS/IRIS/στόλου εξαιρούνται.");
                if(invoice.IsEuromat==true||!invoice.TraderId.HasValue)throw new InvalidOperationException("Απαιτείται συναλλασσόμενος και απουσία πληρωμής στόλου.");
                var mapping=HeatingCorrectionPlan.Copy(config.Mapping);mapping.Company=ReadHostCompany();
                mapping.Environment=route.TestMode?FiscalEnvironment.Test:FiscalEnvironment.Production;
                var live=new HostInvoiceMapper().Map(invoice,mapping);
                var builder=new InvoiceBuilder(new InvoiceTypeResolver());
                if(JsonConvert.SerializeObject(builder.Build(live))!=JsonConvert.SerializeObject(builder.Build(snapshot.Invoice)))
                    throw new InvalidOperationException("Τα σημερινά στοιχεία διαφέρουν από την αποθηκευμένη αρχική έκδοση. Απαιτείται συμφωνία.");
                var plan=new HeatingCorrectionPlan{OriginalId=invoice.InvoiceId,CreditId=Guid.NewGuid(),ReplacementId=Guid.NewGuid(),OriginalTypeId=invoice.InvoiceTypeId,
                    CreditTypeId=creditId,CreditSeries=credit.DefaultSeries??"",ReplacementSeries=invoice.Series??"",OriginalDescription=invoice.Description,
                    OriginalMark=evidence.Transmission.Mark.Value,OriginalAttemptId=submission.AttemptId,Original=snapshot,Host=store.ReadHost(invoice.InvoiceId)};
                HeatingCorrectionPolicy.Validate(plan,Encoding.UTF8.GetString(submission.RequestPayload));return plan;
            }
        }
        private static HeatingBatchRow HeatingSavedRow(HeatingCorrectionRecord row)
        {
            var p=JsonConvert.DeserializeObject<HeatingCorrectionPlan>(row.PlanJson);
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection)) {
                var credit=db.Invoices.SingleOrDefault(i=>i.InvoiceId==row.CreditInvoiceId);
                var replacement=db.Invoices.SingleOrDefault(i=>i.InvoiceId==row.ReplacementInvoiceId);
                return new HeatingBatchRow{Id=row.OriginalInvoiceId,Document=p.OriginalDescription,Provider=HeatingProvider(p.Original.Provider),
                    Supply=p.Original.Invoice.Invoice.SupplyCode,Amount=p.Original.Invoice.Invoice.TotalAmount,
                    Credit=credit?.Description??"—",Replacement=replacement?.Description??"—",Status=row.Message,Eligible=false};
            }
        }
        internal static HeatingBatchRow[] RunHeating(HeatingBatchRow[] selected,bool resume,Func<bool> pause,Action<string> report)
        {
            var user=RequireHeatingAdmin();var store=new SqlHeatingCorrectionStore(sql.Value);
            using(store.EnterBatch()) {
                var all=store.Load();Guid batch=Guid.NewGuid();
                if(!resume)foreach(var row in selected.Where(r=>r.Selected&&r.Eligible)) {
                    if(pause())break;RequireHeatingAdmin();
                    using(var lease=sql.Value.EnterInvoice(row.Id)) {
                        if(all.Any(r=>r.OriginalInvoiceId==row.Id))throw new InvalidOperationException("Το παραστατικό ανήκει ήδη σε παρτίδα. Χρησιμοποιήστε συνέχεια εκκρεμοτήτων.");
                        using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection)) {
                            var invoice=db.Invoices.Single(i=>i.InvoiceId==row.Id);
                            var latest=PrepareHeatingPlan(lease,invoice,row.Plan.Original,row.Plan.CreditTypeId,row.Plan.CreditTypeId,store);
                            if(!JToken.DeepEquals(row.Plan.Host,latest.Host)||row.Plan.OriginalAttemptId!=latest.OriginalAttemptId||row.Plan.CreditSeries!=latest.CreditSeries)
                                throw new InvalidOperationException("Τα στοιχεία άλλαξαν μετά τον έλεγχο. Εκτελέστε νέο έλεγχο παραστατικών.");
                        }
                        store.Reserve(lease,new HeatingCorrectionRecord{OriginalInvoiceId=row.Id,BatchId=batch,CreditInvoiceId=row.Plan.CreditId,
                            ReplacementInvoiceId=row.Plan.ReplacementId,PlanJson=JsonConvert.SerializeObject(row.Plan),CreatedBy=user});
                    }
                }
                var work=store.Load().Where(r=>r.State!="Completed"&&(resume||r.BatchId==batch)).ToArray();
                foreach(var record in work) {
                    if(pause())break;RequireHeatingAdmin();
                    var plan=JsonConvert.DeserializeObject<HeatingCorrectionPlan>(record.PlanJson);
                    report(plan.OriginalDescription+": επεξεργασία...");
                    using(var lease=sql.Value.EnterInvoice(record.OriginalInvoiceId)) {
                        try {
                            HeatingCorrectionRunner.Run(credit=>{
                                // Recheck type/accounting and print-source data at each stage, including after a restart.
                                using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection)) {
                                    var latest=PrepareHeatingPlan(lease,db.Invoices.Single(i=>i.InvoiceId==plan.OriginalId),plan.Original,plan.CreditTypeId,plan.CreditTypeId,store);
                                    if(latest.OriginalMark!=plan.OriginalMark||latest.OriginalAttemptId!=plan.OriginalAttemptId)
                                        throw new InvalidOperationException("Άλλαξε η αποθηκευμένη αρχική διαβίβαση.");
                                }
                                return IssueHeatingChild(plan,credit,record.CreatedBy);
                            },
                                (state,message)=>{store.Progress(lease,state,message);report(plan.OriginalDescription+": "+message);},pause);
                        } catch(Exception ex) {store.Progress(lease,"NeedsReview",ex.Message);report(plan.OriginalDescription+": "+ex.Message);}
                    }
                }
                return store.Load().Select(HeatingSavedRow).ToArray();
            }
        }
        private static InvoiceWorkflowResult IssueHeatingChild(HeatingCorrectionPlan plan,bool credit,Guid user)
        {
            RequireHeatingAdmin();Guid id=credit?plan.CreditId:plan.ReplacementId;
            using(var lease=sql.Value.EnterInvoice(id)) {
                var store=new SqlHeatingCorrectionStore(sql.Value);
                store.CreateChild(lease,plan.Host,credit?plan.CreditTypeId:plan.OriginalTypeId,credit?plan.CreditSeries:plan.ReplacementSeries,user,
                    (credit?"Πλήρης πίστωση":"Επανέκδοση")+" του "+plan.OriginalDescription+" — διόρθωση ένδειξης επιδόματος θέρμανσης. Συμψηφισμός χωρίς νέα είσπραξη.",DateTime.Now);
                // Preserve the original provider/account/environment; new global settings must not redirect this batch.
                var engine=new InvoiceWorkflowService(sql.Value,new ProviderSubmissionService(sql.Value,new CurrentCredentials()),null,null,
                    new FiscalPrintService(sql.Value,new SharedInvoicePrinting(()=>settings.Value.DefaultPrinter)),SharedInvoicePrinting.Render,
                    (invoiceId,result)=>{if(result.Accepted)Project(invoiceId,result);});
                return engine.Process(id,new HeatingSource(plan,credit),false,retryRejected:true);
            }
        }
        private sealed class HeatingSource : IInvoiceWorkflowSource
        {
            private readonly HeatingCorrectionPlan plan;private readonly bool credit;
            internal HeatingSource(HeatingCorrectionPlan value,bool isCredit){plan=value;credit=isCredit;}
            public InvoiceWorkflowSnapshot Prepare(IFiscalDocumentLease lease)
            {
                using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection)) {
                    var invoice=db.Invoices.Single(i=>i.InvoiceId==lease.Document.DocumentId);
                    return new InvoiceWorkflowSnapshot{InvoiceId=invoice.InvoiceId,Provider=plan.Original.Provider,
                        Invoice=plan.Request(credit,invoice.Number,invoice.Series,invoice.TransactionDate),
                        PrintFormat=SharedInvoicePrinting.Format,PrintDestination=JsonConvert.SerializeObject(
                            settings.Value.Route(invoice.InvoiceTypeId,invoice.InvoiceType.Printer).Printer??settings.Value.DefaultPrinter??new SharedPrinterProfile()),
                        PrintContent=Encoding.UTF8.GetBytes(PrintAgent.RenderSharedInvoiceContent(db,invoice))};
                }
            }
        }
        internal static void PrintHeatingCompleted(Func<bool> pause)
        {
            RequireHeatingAdmin();
            foreach(var row in new SqlHeatingCorrectionStore(sql.Value).Load().Where(r=>r.State=="Completed"))
            foreach(var id in new[]{row.CreditInvoiceId,row.ReplacementInvoiceId}) {
                if(pause())return;RequireHeatingAdmin();
                using(var lease=sql.Value.EnterInvoice(id)) {
                    // Repeated clicks do not create copies or bypass uncertain spool results.
                    var result=Process(id,true);
                    if(result.Print?.State!="HandedOff"&&result.Print?.State!="Confirmed")
                        throw new InvalidOperationException("Δεν επιβεβαιώθηκε η εκτύπωση του "+id+" ("+result.Print?.State+"). Ελέγξτε την έκβαση εκτύπωσης στο παραστατικό πριν συνεχίσετε.");
                }
            }
        }
    }
}
