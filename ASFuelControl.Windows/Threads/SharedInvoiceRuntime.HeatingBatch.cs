using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class HeatingBatchRow
    {
        public bool Selected { get; set; }
        public string Document { get; set; }
        public Guid InvoiceTypeId { get; set; }
        public string InvoiceType { get; set; }
        public string Provider { get; set; }
        public string Supply { get; set; }
        public decimal Amount { get; set; }
        public string Credit { get; set; }
        public string Replacement { get; set; }
        public string Status { get; set; }
        public bool Completed { get; set; }
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
        private static string HeatingRoute(HeatingCorrectionPlan plan)
        {
            if(plan.Original.Provider!=null)
            {
                return HeatingProvider(plan.Original.Provider);
            }

            FiscalSigningScope scope=plan.OriginalSigningScope;
            string environment=scope.Environment==FiscalEnvironment.Test?
                "Δοκιμαστικό":"Παραγωγικό";
            return "Φορολογικός μηχανισμός / "+environment;
        }
        private static string HeatingReadyStatus(HeatingCorrectionPlan plan)
        {
            string payment=plan.OriginalHostPaymentType==2||
                plan.OriginalHostPaymentType==3||
                plan.OriginalHostPaymentType==4?
                (plan.ReplacementHostPaymentType==1?
                    " — επανέκδοση με μετρητά":" — επανέκδοση επί πιστώσει"):"";
            if(plan.EvidenceKind==HeatingCorrectionEvidenceKind.LegacyMyDataMark)
            {
                return "Έτοιμο — επιβεβαίωση από MyDataInvoice MARK"+payment;
            }

            if(plan.EvidenceKind==HeatingCorrectionEvidenceKind.LegacyFiscalSignature)
            {
                return "Έτοιμο — επιβεβαίωση από υπογραφή φορολογικού μηχανισμού"+
                    payment;
            }

            return "Έτοιμο — λείπει η ένδειξη 11"+payment;
        }
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
            return HeatingInvoiceTypes(true);
        }
        internal static HeatingCreditType[] HeatingReplacementTypes()
        {
            return HeatingInvoiceTypes(false);
        }
        private static HeatingCreditType[] HeatingInvoiceTypes(bool credit)
        {
            RequireHeatingAdmin();
            Data.InvoiceType[] candidates;
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                candidates=db.InvoiceTypes.Where(t=>t.Printable&&
                    t.Invalidated!=true).ToArray();
            }
            return candidates.Where(t=>(credit?t.IsCancelation==true:
                    t.IsCancelation!=true)&&t.OfficialEnumerator!=215&&
                    t.ForcesDelivery!=true&&t.TransactionType==0&&
                    t.HasFinancialTransactions!=false)
                .OrderBy(t=>t.Description).ThenBy(t=>t.DefaultSeries)
                .Select(t=>new HeatingCreditType{Id=t.InvoiceTypeId,
                    Name=t.Description+" / "+t.DefaultSeries,
                    Retail=t.RetailInvoice==true}).ToArray();
        }
        internal static HeatingBatchRow[] ScanHeating(DateTime from,DateTime to,
            Guid retailCredit,Guid wholesaleCredit,Guid retailReplacement,
            Guid wholesaleReplacement,int exceptionalReplacementPaymentType,
            Func<bool> pause=null)
        {
            RequireHeatingAdmin();
            if(to.Date<from.Date)
            {
                throw new ArgumentException("Μη έγκυρο διάστημα ημερομηνιών.");
            }

            var store=new SqlHeatingCorrectionStore(sql.Value);
            var saved=store.Load();
            var results=new List<HeatingBatchRow>();
            DateTime end=to.Date.AddDays(1);
            Guid[] ids;
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                ids=db.Invoices.Where(i=>i.TransactionDate>=from.Date&&i.TransactionDate<end&&
                    i.InvoiceLines.Any(l=>l.FuelType.EnumeratorValue==30||l.FuelType.EnumeratorValue==31)).Select(i=>i.InvoiceId).ToArray();
            }

            var pendingIds=saved.Where(r=>r.State!="Completed").Select(r=>r.OriginalInvoiceId);
            var scanIds=ids.Concat(pendingIds).Distinct();
            foreach(var id in scanIds)
            {
                if(pause?.Invoke()==true)
                {
                    break;
                }

                var existing=saved.SingleOrDefault(r=>r.OriginalInvoiceId==id);
                if(existing!=null)
                {
                    HeatingBatchRow savedRow=HeatingSavedRow(existing);
                    results.Add(savedRow);
                    continue;
                }

                using(var lease=sql.Value.EnterInvoice(id))
                using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice=db.Invoices.Single(i=>i.InvoiceId==id);
                    byte[] bytes=new SqlInvoiceWorkflowStore(sql.Value).Load(lease);
                    string snapshotJson=bytes==null?null:Encoding.UTF8.GetString(bytes);
                    var snapshot=snapshotJson==null?null:
                        JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(snapshotJson);
                    var row=new HeatingBatchRow
                    {
                        Id=id,
                        Document=invoice.Description,
                        InvoiceTypeId=invoice.InvoiceTypeId,
                        InvoiceType=invoice.InvoiceType.Description,
                        Supply=invoice.SupplyNumber,
                        Amount=invoice.TotalAmount??0
                    };
                    try
                    {
                        row.Plan=PrepareHeatingPlan(lease,invoice,snapshot,retailCredit,
                            wholesaleCredit,retailReplacement,wholesaleReplacement,
                            exceptionalReplacementPaymentType,store);
                        row.Provider=HeatingRoute(row.Plan);
                        row.Eligible=true;
                        row.Selected=true;
                        row.Status=HeatingReadyStatus(row.Plan);
                    }
                    catch(Exception ex)
                    {
                        row.Status="Εξαιρείται: "+ex.Message;
                    }
                    results.Add(row);
                }
            }
            return results.ToArray();
        }
        private sealed class HeatingEvidence
        {
            internal HeatingCorrectionEvidenceKind Kind;
            internal Guid AttemptId;
            internal Guid LegacyEvidenceId;
            internal long Mark;
            internal string Signature;
            internal string OriginalPayload;
            internal ProviderRoute Provider;
            internal FiscalSigningScope Signing;
        }
        private static HeatingCorrectionPlan PrepareHeatingPlan(IFiscalDocumentLease lease,
            Data.Invoice invoice,InvoiceWorkflowSnapshot snapshot,Guid retailCredit,
            Guid wholesaleCredit,Guid retailReplacement,Guid wholesaleReplacement,
            int exceptionalReplacementPaymentType,SqlHeatingCorrectionStore store)
        {
            if(invoice.ChildInvoiceRelations.Any()||invoice.ParentInvoiceRelations.Any())
            {
                throw new InvalidOperationException("Υπάρχουν ήδη συσχετισμένα παραστατικά / πιστώσεις.");
            }

            var config=SharedInvoicingSettings.LoadRuntime();
            InvoiceTypePolicy policy=HeatingPolicy(config,invoice);
            SharedInvoiceRoute originalRoute=config.Route(invoice);
            var history=new SqlFiscalHistoryStore(sql.Value);
            HeatingEvidence evidence=ResolveHeatingEvidence(lease,invoice,snapshot,
                config,policy,originalRoute,history);
            ValidateHeatingHistory(lease,history,evidence);
            int originalPaymentType=invoice.PaymentType??-1;
            bool exceptionalPayment=originalPaymentType==2||
                originalPaymentType==3||originalPaymentType==4;
            bool supportedPayment=originalPaymentType==0||
                originalPaymentType==1||exceptionalPayment;
            if(!supportedPayment)
            {
                throw new InvalidOperationException(
                    "Δεν υποστηρίζεται ο αποθηκευμένος τρόπος πληρωμής του αρχικού παραστατικού.");
            }
            if(!invoice.TraderId.HasValue)
            {
                throw new InvalidOperationException("Απαιτείται συναλλασσόμενος.");
            }
            if(exceptionalPayment&&exceptionalReplacementPaymentType!=0&&
                exceptionalReplacementPaymentType!=1)
            {
                throw new InvalidOperationException(
                    "Επιλέξτε μετρητά ή επί πιστώσει για την επανέκδοση παραστατικών POS/IRIS/στόλου.");
            }
            int replacementHostPaymentType=exceptionalPayment?
                exceptionalReplacementPaymentType:0;
            InvoicePaymentType replacementPaymentType=
                replacementHostPaymentType==1?InvoicePaymentType.Cash:
                InvoicePaymentType.Credit;

            MappingContext mapping=HeatingCorrectionPlan.Copy(config.Mapping);
            if(mapping==null)
            {
                throw new InvalidOperationException("Απαιτούνται αποθηκευμένες ρυθμίσεις χαρτογράφησης.");
            }
            CompanySettings company=ReadHostCompany();
            mapping.Company=company;
            mapping.Environment=HeatingEnvironment(evidence);
            var mapper=new HostInvoiceMapper();
            InvoiceBuildRequest live=mapper.Map(invoice,mapping);
            InvoiceWorkflowSnapshot original=PrepareHeatingOriginal(snapshot,invoice,
                live,evidence);
            bool retail=original.Invoice.Invoice.InvoiceSeries.InvoiceType.IsRetail;
            Guid creditId=retail?retailCredit:wholesaleCredit;
            Guid replacementId=retail?retailReplacement:wholesaleReplacement;
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var credit=db.InvoiceTypes.SingleOrDefault(t=>t.InvoiceTypeId==creditId);
                var replacement=db.InvoiceTypes.SingleOrDefault(t=>
                    t.InvoiceTypeId==replacementId);
                ValidateHeatingCreditType(invoice,credit,retail);
                ValidateHeatingReplacementType(replacement,retail);
                SharedInvoiceRoute creditRoute=config.Route(credit);
                SharedInvoiceRoute replacementRoute=config.Route(replacement);
                ValidateHeatingRoutes(evidence,originalRoute,creditRoute);
                CompareHeatingOriginal(live,original.Invoice,evidence.Kind);
                JObject host=store.ReadHost(invoice.InvoiceId);
                var plan=new HeatingCorrectionPlan
                {
                    OriginalId=invoice.InvoiceId,
                    CreditId=Guid.NewGuid(),
                    ReplacementId=Guid.NewGuid(),
                    OriginalTypeId=invoice.InvoiceTypeId,
                    CreditTypeId=creditId,
                    ReplacementTypeId=replacementId,
                    CreditSeries=credit.DefaultSeries??"",
                    ReplacementSeries=replacement.DefaultSeries??"",
                    OriginalDescription=invoice.Description,
                    OriginalMark=evidence.Mark,
                    OriginalAttemptId=evidence.AttemptId,
                    LegacyEvidenceId=evidence.LegacyEvidenceId,
                    OriginalSignature=evidence.Signature,
                    EvidenceKind=evidence.Kind,
                    OriginalHostPaymentType=originalPaymentType,
                    ReplacementHostPaymentType=replacementHostPaymentType,
                    ReplacementPaymentType=replacementPaymentType,
                    ReplacementPaymentConfigured=true,
                    OriginalSigningScope=evidence.Signing,
                    ReplacementProvider=replacementRoute.Provider,
                    ReplacementSigningScope=replacementRoute.Signing,
                    ReplacementLocalOnly=replacementRoute.LocalOnly,
                    ReplacementRouteConfigured=true,
                    Original=original,
                    Host=host
                };
                if(evidence.Kind==HeatingCorrectionEvidenceKind.SharedProvider)
                {
                    HeatingCorrectionPolicy.Validate(plan,evidence.OriginalPayload);
                }
                else
                {
                    HeatingCorrectionPolicy.ValidateLegacy(plan);
                }
                return plan;
            }
        }
        private static InvoiceTypePolicy HeatingPolicy(SharedInvoiceConfiguration config,
            Data.Invoice invoice)
        {
            InvoiceTypePolicy policy;
            if(config.Policies==null||
                !config.Policies.TryGetValue(invoice.InvoiceTypeId,out policy))
            {
                throw new InvalidOperationException("Δεν έχει αποθηκευτεί πολιτική διαβίβασης για τον τύπο παραστατικού.");
            }
            policy.Validate();
            return policy;
        }
        private static HeatingEvidence ResolveHeatingEvidence(IFiscalDocumentLease lease,
            Data.Invoice invoice,InvoiceWorkflowSnapshot snapshot,
            SharedInvoiceConfiguration config,InvoiceTypePolicy policy,
            SharedInvoiceRoute route,SqlFiscalHistoryStore history)
        {
            if(policy.Transmission==InvoiceTransmissionMode.Provider)
            {
                return ResolveSharedProviderEvidence(lease,invoice,snapshot,history);
            }
            if(policy.Transmission==InvoiceTransmissionMode.MyData)
            {
                return ResolveLegacyMyDataEvidence(invoice,snapshot,route);
            }
            if(policy.Transmission==InvoiceTransmissionMode.None&&
                policy.SignWithFiscalDevice)
            {
                return ResolveLegacySigningEvidence(invoice,route);
            }
            throw new InvalidOperationException("Ο τύπος παραστατικού δεν χρησιμοποιεί myDATA, πάροχο ή φορολογικό μηχανισμό.");
        }
        private static HeatingEvidence ResolveSharedProviderEvidence(
            IFiscalDocumentLease lease,Data.Invoice invoice,
            InvoiceWorkflowSnapshot snapshot,SqlFiscalHistoryStore history)
        {
            if(snapshot==null)
            {
                throw new InvalidOperationException("Δεν υπάρχει αποθηκευμένη κοινή έκδοση για τον τύπο παρόχου.");
            }
            FiscalAttemptEvidence submission=history.Load(lease,
                FiscalJournalFamily.Submission).LastOrDefault();
            if(submission?.State!="Accepted")
            {
                throw new InvalidOperationException("Η αρχική έκδοση δεν έχει επιβεβαιωθεί. Απαιτείται ανάκτηση κατάστασης.");
            }
            var credentials=new CurrentCredentials();
            var providers=new ProviderSubmissionService(sql.Value,credentials);
            ParentEvidence parentEvidence=providers.LoadParentEvidence(invoice.InvoiceId);
            JObject submissionEvidence=JObject.Parse(submission.SnapshotJson);
            string routingJson=(string)submissionEvidence["RoutingContext"];
            JObject routing=JObject.Parse(routingJson);
            ProviderRoute provider=routing["Route"].ToObject<ProviderRoute>();
            credentials.Resolve(provider);
            long mark=parentEvidence.Transmission.Mark.Value;
            string payload=Encoding.UTF8.GetString(submission.RequestPayload);
            return new HeatingEvidence
            {
                Kind=HeatingCorrectionEvidenceKind.SharedProvider,
                AttemptId=submission.AttemptId,
                Mark=mark,
                OriginalPayload=payload,
                Provider=provider
            };
        }
        private static HeatingEvidence ResolveLegacyMyDataEvidence(Data.Invoice invoice,
            InvoiceWorkflowSnapshot snapshot,SharedInvoiceRoute route)
        {
            if(route.Provider?.ProviderKey!=InvoiceProviderCatalog.MyDataKey)
            {
                throw new InvalidOperationException("Η πολιτική myDATA δεν έχει έγκυρη άμεση διαδρομή myDATA.");
            }
            Guid legacyEvidenceId;
            long legacyMark;
            bool cancelled;
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                Data.MyDataInvoice legacy=db.MyDataInvoices.Where(item=>item.InvoiceId==invoice.InvoiceId&&
                    item.Mark.HasValue&&item.Mark.Value>0)
                    .OrderByDescending(item=>item.DateTimeSent)
                    .ThenByDescending(item=>item.MyDataInvoiceId).FirstOrDefault();
                if(legacy==null)
                {
                    throw new InvalidOperationException("Δεν βρέθηκε θετικό MARK στο MyDataInvoice.");
                }
                legacyEvidenceId=legacy.MyDataInvoiceId;
                legacyMark=legacy.Mark.Value;
                long cancelledByMark=legacy.CanceledByMark.GetValueOrDefault();
                long cancellationMark=legacy.CancelationMark.GetValueOrDefault();
                cancelled=cancelledByMark>0||cancellationMark>0;
            }
            if(cancelled)
            {
                throw new InvalidOperationException("Το MARK του αρχικού παραστατικού έχει ακυρωθεί.");
            }
            ProviderRoute provider=snapshot?.Provider??route.Provider;
            if(provider.ProviderKey!=InvoiceProviderCatalog.MyDataKey)
            {
                throw new InvalidOperationException("Η αποθηκευμένη διαδρομή δεν συμφωνεί με την πολιτική myDATA.");
            }
            var credentials=new CurrentCredentials();
            credentials.Resolve(provider);
            return new HeatingEvidence
            {
                Kind=HeatingCorrectionEvidenceKind.LegacyMyDataMark,
                LegacyEvidenceId=legacyEvidenceId,
                Mark=legacyMark,
                Provider=provider,
                Signing=route.Signing
            };
        }
        private static HeatingEvidence ResolveLegacySigningEvidence(Data.Invoice invoice,
            SharedInvoiceRoute route)
        {
            if(route.Provider!=null||route.Signing==null)
            {
                throw new InvalidOperationException("Η πολιτική None πρέπει να χρησιμοποιεί φορολογικό μηχανισμό χωρίς διαβίβαση.");
            }
            if(string.IsNullOrWhiteSpace(invoice.InvoiceSignature))
            {
                throw new InvalidOperationException("Δεν βρέθηκε υπογραφή φορολογικού μηχανισμού στο παραστατικό.");
            }
            return new HeatingEvidence
            {
                Kind=HeatingCorrectionEvidenceKind.LegacyFiscalSignature,
                Signature=invoice.InvoiceSignature,
                Signing=route.Signing
            };
        }
        private static void ValidateHeatingHistory(IFiscalDocumentLease lease,
            SqlFiscalHistoryStore history,HeatingEvidence evidence)
        {
            IReadOnlyList<FiscalAttemptEvidence> cancellations=history.Load(lease,
                FiscalJournalFamily.Cancellation);
            IReadOnlyList<FiscalAttemptEvidence> payments=history.Load(lease,
                FiscalJournalFamily.Payment);
            IReadOnlyList<FiscalAttemptEvidence> sessions=history.Load(lease,
                FiscalJournalFamily.ProviderSession);
            if(cancellations.Any()||payments.Any()||sessions.Any())
            {
                throw new InvalidOperationException("Υπάρχει ιστορικό POS ή ακύρωσης που απαιτεί συμφωνία.");
            }
            IReadOnlyList<FiscalAttemptEvidence> signing=history.Load(lease,
                FiscalJournalFamily.Signing);
            if(evidence.Kind==HeatingCorrectionEvidenceKind.SharedProvider&&signing.Any())
            {
                throw new InvalidOperationException("Υπάρχει ιστορικό σήμανσης που δεν επιτρέπεται για τον τύπο παρόχου.");
            }
            if(evidence.Kind==HeatingCorrectionEvidenceKind.LegacyFiscalSignature&&
                signing.Any(item=>item.State!="Signed"&&item.State!="Rejected"))
            {
                throw new InvalidOperationException("Υπάρχει εκκρεμής ή άγνωστη προσπάθεια σήμανσης.");
            }
        }
        private static FiscalEnvironment HeatingEnvironment(HeatingEvidence evidence)
        {
            if(evidence.Provider!=null)
            {
                return evidence.Provider.TestMode?FiscalEnvironment.Test:
                    FiscalEnvironment.Production;
            }
            return evidence.Signing.Environment;
        }
        private static InvoiceWorkflowSnapshot PrepareHeatingOriginal(
            InvoiceWorkflowSnapshot snapshot,Data.Invoice invoice,
            InvoiceBuildRequest live,HeatingEvidence evidence)
        {
            InvoiceWorkflowSnapshot original;
            bool canUseSnapshot=snapshot?.Invoice!=null&&
                evidence.Kind!=HeatingCorrectionEvidenceKind.LegacyFiscalSignature;
            if(canUseSnapshot)
            {
                original=HeatingCorrectionPlan.Copy(snapshot);
            }
            else
            {
                original=new InvoiceWorkflowSnapshot
                {
                    InvoiceId=invoice.InvoiceId,
                    Invoice=HeatingCorrectionPlan.Copy(live)
                };
            }
            original.Provider=evidence.Provider;
            original.Payment=null;
            original.Signing=null;
            return original;
        }
        private static void ValidateHeatingCreditType(Data.Invoice invoice,
            Data.InvoiceType credit,bool retail)
        {
            if(credit==null||credit.IsCancelation!=true||credit.OfficialEnumerator==215||
                !credit.Printable||credit.Invalidated==true||credit.ForcesDelivery==true||
                credit.TransactionType!=0||invoice.InvoiceType.TransactionType!=0||
                credit.HasFinancialTransactions==false||
                invoice.InvoiceType.HasFinancialTransactions==false||
                (credit.RetailInvoice==true)!=retail)
            {
                throw new InvalidOperationException("Επιλέξτε οικονομικό πιστωτικό πωλήσεων (κίνηση εξαγωγής), χωρίς υποχρεωτική διακίνηση, του αντίστοιχου τύπου (λιανική / χονδρική).");
            }
        }
        private static void ValidateHeatingReplacementType(
            Data.InvoiceType replacement,bool retail)
        {
            if(replacement==null||replacement.IsCancelation==true||
                replacement.OfficialEnumerator==215||!replacement.Printable||
                replacement.Invalidated==true||replacement.ForcesDelivery==true||
                replacement.TransactionType!=0||
                replacement.HasFinancialTransactions==false||
                (replacement.RetailInvoice==true)!=retail)
            {
                throw new InvalidOperationException(
                    "Επιλέξτε οικονομικό τύπο πώλησης για την επανέκδοση " +
                    "(κίνηση εξαγωγής), χωρίς υποχρεωτική διακίνηση, του " +
                    "αντίστοιχου τύπου (λιανική / χονδρική).");
            }
        }
        private static void ValidateHeatingRoutes(HeatingEvidence evidence,
            SharedInvoiceRoute original,SharedInvoiceRoute credit)
        {
            if(original.LocalOnly||credit.LocalOnly)
            {
                throw new InvalidOperationException(
                    "Ο τύπος πιστωτικού πρέπει να έχει την ίδια εξωτερική " +
                    "φορολογική διαδρομή με το αρχικό παραστατικό.");
            }
            if(evidence.Kind==HeatingCorrectionEvidenceKind.SharedProvider)
            {
                bool valid=original.Provider!=null&&credit.Provider!=null&&
                    original.Signing==null&&credit.Signing==null&&
                    original.Provider.ProviderKey!=InvoiceProviderCatalog.MyDataKey&&
                    credit.Provider.ProviderKey!=InvoiceProviderCatalog.MyDataKey&&
                    SameHeatingProvider(original.Provider,credit.Provider);
                if(!valid)
                {
                    throw new InvalidOperationException(
                        "Ο τύπος πιστωτικού πρέπει να χρησιμοποιεί τον ίδιο " +
                        "πάροχο με το αρχικό παραστατικό, χωρίς φορολογικό μηχανισμό.");
                }
                return;
            }
            if(evidence.Kind==HeatingCorrectionEvidenceKind.LegacyMyDataMark)
            {
                bool creditSigning=SameSigningScope(original.Signing,
                    credit.Signing);
                bool valid=original.Provider?.ProviderKey==InvoiceProviderCatalog.MyDataKey&&
                    credit.Provider?.ProviderKey==InvoiceProviderCatalog.MyDataKey&&
                    SameHeatingProvider(original.Provider,credit.Provider)&&
                    creditSigning;
                if(!valid)
                {
                    throw new InvalidOperationException(
                        "Ο τύπος πιστωτικού πρέπει να έχει την ίδια διαδρομή " +
                        "myDATA και σήμανσης με το αρχικό παραστατικό.");
                }
                return;
            }
            bool creditDevice=SameSigningScope(original.Signing,credit.Signing);
            if(original.Provider!=null||credit.Provider!=null||
                original.Signing==null||credit.Signing==null||!creditDevice)
            {
                throw new InvalidOperationException(
                    "Ο τύπος πιστωτικού πρέπει να χρησιμοποιεί τον ίδιο " +
                    "φορολογικό μηχανισμό με το αρχικό παραστατικό, χωρίς διαβίβαση.");
            }
        }
        private static bool SameHeatingProvider(ProviderRoute left,
            ProviderRoute right)
        {
            if(left==null||right==null)
            {
                return left==null&&right==null;
            }
            return left.ProviderKey==right.ProviderKey&&
                left.TestMode==right.TestMode&&left.IssuerVat==right.IssuerVat&&
                left.IssuerBranch==right.IssuerBranch&&
                left.CredentialProfile==right.CredentialProfile&&
                left.Account==right.Account&&
                left.MyDataBaseUrl==right.MyDataBaseUrl;
        }
        private static bool SameSigningScope(FiscalSigningScope left,
            FiscalSigningScope right)
        {
            if(left==null||right==null)
            {
                return left==null&&right==null;
            }
            return FiscalSigningCoordinator.SameScope(left,right);
        }
        private static void CompareHeatingOriginal(InvoiceBuildRequest live,
            InvoiceBuildRequest original,HeatingCorrectionEvidenceKind evidenceKind)
        {
            if(evidenceKind!=HeatingCorrectionEvidenceKind.SharedProvider)
            {
                return;
            }
            var resolver=new InvoiceTypeResolver();
            var builder=new InvoiceBuilder(resolver);
            var liveModel=builder.Build(live);
            var originalModel=builder.Build(original);
            string liveJson=JsonConvert.SerializeObject(liveModel);
            string originalJson=JsonConvert.SerializeObject(originalModel);
            if(liveJson!=originalJson)
            {
                throw new InvalidOperationException("Τα σημερινά στοιχεία διαφέρουν από την αποθηκευμένη αρχική έκδοση. Απαιτείται συμφωνία.");
            }
        }
        private static HeatingBatchRow HeatingSavedRow(HeatingCorrectionRecord row)
        {
            var p=JsonConvert.DeserializeObject<HeatingCorrectionPlan>(row.PlanJson);
            using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection)) {
                var credit=db.Invoices.SingleOrDefault(i=>i.InvoiceId==row.CreditInvoiceId);
                var replacement=db.Invoices.SingleOrDefault(i=>i.InvoiceId==row.ReplacementInvoiceId);
                var originalType=db.InvoiceTypes.Single(t=>
                    t.InvoiceTypeId==p.OriginalTypeId);
                return new HeatingBatchRow{Id=row.OriginalInvoiceId,
                    Document=p.OriginalDescription,
                    InvoiceTypeId=p.OriginalTypeId,
                    InvoiceType=originalType.Description,
                    Provider=HeatingRoute(p),
                    Supply=p.Original.Invoice.Invoice.SupplyCode,Amount=p.Original.Invoice.Invoice.TotalAmount,
                    Credit=credit?.Description??"—",
                    Replacement=replacement?.Description??"—",
                    Status=row.Message,Completed=row.State=="Completed",
                    Eligible=false};
            }
        }
        private static bool SameHeatingEvidence(HeatingCorrectionPlan expected,
            HeatingCorrectionPlan actual)
        {
            if(expected.EvidenceKind!=actual.EvidenceKind||
                expected.OriginalMark!=actual.OriginalMark||
                expected.OriginalAttemptId!=actual.OriginalAttemptId||
                expected.LegacyEvidenceId!=actual.LegacyEvidenceId||
                expected.OriginalSignature!=actual.OriginalSignature)
            {
                return false;
            }
            if(expected.ReplacementPaymentConfigured&&
                (!actual.ReplacementPaymentConfigured||
                 expected.OriginalHostPaymentType!=actual.OriginalHostPaymentType||
                 expected.ReplacementHostPaymentType!=
                    actual.ReplacementHostPaymentType||
                 expected.ReplacementPaymentType!=actual.ReplacementPaymentType))
            {
                return false;
            }
            string expectedProvider=JsonConvert.SerializeObject(
                expected.Original.Provider);
            string actualProvider=JsonConvert.SerializeObject(
                actual.Original.Provider);
            if(expectedProvider!=actualProvider)
            {
                return false;
            }
            if(expected.ReplacementRouteConfigured)
            {
                string expectedReplacementProvider=JsonConvert.SerializeObject(
                    expected.ReplacementProvider);
                string actualReplacementProvider=JsonConvert.SerializeObject(
                    actual.ReplacementProvider);
                if(expectedReplacementProvider!=actualReplacementProvider||
                    expected.ReplacementLocalOnly!=actual.ReplacementLocalOnly||
                    !actual.ReplacementRouteConfigured||
                    !SameSigningScope(expected.ReplacementSigningScope,
                        actual.ReplacementSigningScope))
                {
                    return false;
                }
            }
            bool originalSigning=SameSigningScope(expected.OriginalSigningScope,
                actual.OriginalSigningScope);
            return originalSigning;
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
                            Guid replacementTypeId=HeatingReplacementTypeId(row.Plan);
                            var latest=PrepareHeatingPlan(lease,invoice,row.Plan.Original,
                                row.Plan.CreditTypeId,row.Plan.CreditTypeId,
                                replacementTypeId,replacementTypeId,
                                row.Plan.ReplacementHostPaymentType,store);
                            if(!JToken.DeepEquals(row.Plan.Host,latest.Host)||
                                !SameHeatingEvidence(row.Plan,latest)||
                                row.Plan.CreditSeries!=latest.CreditSeries||
                                row.Plan.ReplacementSeries!=latest.ReplacementSeries)
                                throw new InvalidOperationException("Τα στοιχεία άλλαξαν μετά τον έλεγχο. Εκτελέστε νέο έλεγχο παραστατικών.");
                        }
                        Guid selectedReplacementTypeId=
                            HeatingReplacementTypeId(row.Plan);
                        string creditNote=HeatingCorrectionNote(row.Plan,true);
                        string replacementNote=HeatingCorrectionNote(row.Plan,false);
                        string planJson=JsonConvert.SerializeObject(row.Plan);
                        DateTime issuedAt=DateTime.Now;
                        var record=new HeatingCorrectionRecord
                        {
                            OriginalInvoiceId=row.Id,
                            BatchId=batch,
                            CreditInvoiceId=row.Plan.CreditId,
                            ReplacementInvoiceId=row.Plan.ReplacementId,
                            PlanJson=planJson,
                            CreatedBy=user
                        };
                        store.ReserveAndCreateChildren(lease,record,row.Plan.Host,
                            row.Plan.CreditTypeId,row.Plan.CreditSeries,creditNote,
                            selectedReplacementTypeId,row.Plan.ReplacementSeries,
                            replacementNote,row.Plan.ReplacementHostPaymentType,
                            issuedAt);
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
                                ValidateHeatingPlanCurrent(lease,plan,store);
                                return IssueHeatingChild(plan,credit,record.CreatedBy);
                            },
                                (state,message)=>{store.Progress(lease,state,message);report(plan.OriginalDescription+": "+message);},pause);
                        } catch(Exception ex) {store.Progress(lease,"NeedsReview",ex.Message);report(plan.OriginalDescription+": "+ex.Message);}
                    }
                }
                return store.Load().Select(HeatingSavedRow).ToArray();
            }
        }
        internal static InvoiceWorkflowResult ResumeHeatingCorrection(Guid childId)
        {
            RequireHeatingAdmin();
            var store=new SqlHeatingCorrectionStore(sql.Value);
            using(store.EnterBatch())
            {
                HeatingCorrectionRecord record=store.Load().SingleOrDefault(item=>
                    item.State!="Completed"&&(item.CreditInvoiceId==childId||
                    item.ReplacementInvoiceId==childId));
                if(record==null)
                {
                    throw new InvalidOperationException(
                        "Δεν βρέθηκε εκκρεμής παρτίδα διόρθωσης για το παραστατικό.");
                }
                var plan=JsonConvert.DeserializeObject<HeatingCorrectionPlan>(
                    record.PlanJson);
                InvoiceWorkflowResult lastResult=null;
                InvoiceWorkflowResult selectedResult=null;
                using(var lease=sql.Value.EnterInvoice(record.OriginalInvoiceId))
                {
                    try
                    {
                        HeatingCorrectionRunner.Run(credit=>
                        {
                            ValidateHeatingPlanCurrent(lease,plan,store);
                            InvoiceWorkflowResult result=IssueHeatingChild(plan,credit,
                                record.CreatedBy);
                            Guid processedId=credit?plan.CreditId:
                                plan.ReplacementId;
                            lastResult=result;
                            if(processedId==childId)
                            {
                                selectedResult=result;
                            }
                            return result;
                        },(state,message)=>store.Progress(lease,state,message),
                            ()=>false);
                    }
                    catch(Exception ex)
                    {
                        store.Progress(lease,"NeedsReview",ex.Message);
                        throw;
                    }
                }
                InvoiceWorkflowResult resultToReturn=selectedResult??lastResult;
                if(resultToReturn==null)
                {
                    throw new InvalidOperationException(
                        "Η επανεπεξεργασία δεν παρήγαγε αποτέλεσμα.");
                }
                return resultToReturn;
            }
        }
        private static void ValidateHeatingPlanCurrent(IFiscalDocumentLease lease,
            HeatingCorrectionPlan plan,SqlHeatingCorrectionStore store)
        {
            using(var db=new Data.DatabaseModel(
                Properties.Settings.Default.DBConnection))
            {
                var invoice=db.Invoices.Single(i=>i.InvoiceId==plan.OriginalId);
                Guid replacementTypeId=HeatingReplacementTypeId(plan);
                var latest=PrepareHeatingPlan(lease,invoice,plan.Original,
                    plan.CreditTypeId,plan.CreditTypeId,replacementTypeId,
                    replacementTypeId,plan.ReplacementHostPaymentType,store);
                if(!SameHeatingEvidence(plan,latest))
                {
                    throw new InvalidOperationException(
                        "Άλλαξε η αποθηκευμένη αρχική διαβίβαση ή η επιλογή επανέκδοσης.");
                }
            }
        }
        private static InvoiceWorkflowResult IssueHeatingChild(HeatingCorrectionPlan plan,bool credit,Guid user)
        {
            RequireHeatingAdmin();
            Guid id=credit?plan.CreditId:plan.ReplacementId;
            using(var lease=sql.Value.EnterInvoice(id))
            {
                var store=new SqlHeatingCorrectionStore(sql.Value);
                Guid replacementTypeId=HeatingReplacementTypeId(plan);
                Guid typeId=credit?plan.CreditTypeId:replacementTypeId;
                string series=credit?plan.CreditSeries:plan.ReplacementSeries;
                string notes=HeatingCorrectionNote(plan,credit);
                int paymentType=credit?0:plan.ReplacementHostPaymentType;
                DateTime created=DateTime.Now;
                store.CreateChild(lease,plan.Host,typeId,series,user,notes,created,
                    paymentType);
                var credentials=new CurrentCredentials();
                var providers=new ProviderSubmissionService(sql.Value,credentials);
                var signing=new FiscalSigningService(sql.Value,scope=>
                {
                    SamtecDeviceProfile[] devices=settings.Value.Devices??
                        new SamtecDeviceProfile[0];
                    SamtecDeviceProfile device=devices.SingleOrDefault(item=>
                        FiscalSigningCoordinator.SameScope(item.Scope,scope));
                    if(device==null)
                    {
                        throw new InvalidOperationException("Το αποθηκευμένο προφίλ φορολογικού μηχανισμού δεν είναι διαθέσιμο.");
                    }
                    return new SamtecFiscalSigner(device);
                });
                var printer=new SharedInvoicePrinting(()=>settings.Value.DefaultPrinter);
                var printing=new FiscalPrintService(sql.Value,printer);
                var engine=new InvoiceWorkflowService(sql.Value,providers,null,signing,
                    printing,
                    (snapshot,response)=>SharedInvoicePrinting.Render(snapshot,response,SharedInvoiceLaserReportRenderer.Render),
                    (invoiceId,result)=>{if(result.Accepted)Project(invoiceId,result);});
                var source=new HeatingSource(plan,credit);
                return engine.Process(id,source,false,retryRejected:true);
            }
        }
        private static string HeatingCorrectionNote(HeatingCorrectionPlan plan,
            bool credit)
        {
            string stage=credit?"Πλήρης πίστωση":"Επανέκδοση";
            return stage+" του "+plan.OriginalDescription+
                " — διόρθωση ένδειξης επιδόματος θέρμανσης. " +
                "Συμψηφισμός χωρίς νέα είσπραξη.";
        }
        private static Guid HeatingReplacementTypeId(HeatingCorrectionPlan plan)
        {
            if(plan.ReplacementTypeId!=Guid.Empty)
            {
                return plan.ReplacementTypeId;
            }
            return plan.OriginalTypeId;
        }
        private sealed class HeatingSource : IInvoiceWorkflowSource
        {
            private readonly HeatingCorrectionPlan plan;
            private readonly bool credit;
            internal HeatingSource(HeatingCorrectionPlan value,bool isCredit)
            {
                plan=value;
                credit=isCredit;
            }
            public InvoiceWorkflowSnapshot Prepare(IFiscalDocumentLease lease)
            {
                using(var db=new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var invoice=db.Invoices.Single(i=>i.InvoiceId==lease.Document.DocumentId);
                    SharedInvoiceRoute route=settings.Value.Route(invoice);
                    SharedPrinterProfile printer=route.Printer??
                        settings.Value.DefaultPrinter??new SharedPrinterProfile();
                    string destination=JsonConvert.SerializeObject(printer);
                    string content=PrintAgent.RenderSharedInvoiceContent(db,invoice);
                    byte[] printContent=Encoding.UTF8.GetBytes(content);
                    InvoiceBuildRequest request=plan.Request(credit,invoice.Number,
                        invoice.Series,invoice.TransactionDate);
                    bool useReplacementRoute=!credit&&
                        plan.ReplacementRouteConfigured;
                    ProviderRoute provider=useReplacementRoute?
                        plan.ReplacementProvider:plan.Original.Provider;
                    FiscalSigningScope signingScope=useReplacementRoute?
                        plan.ReplacementSigningScope:plan.OriginalSigningScope;
                    bool localOnly=useReplacementRoute&&
                        plan.ReplacementLocalOnly;
                    FiscalSigningRequest signing=null;
                    if(signingScope!=null)
                    {
                        SamtecDeviceProfile device=settings.Value.SamtecDevice(
                            signingScope);
                        string eftPosTID=device?.EftPosTID;
                        // Heating-correction lines deliberately have no pump or
                        // sale relation. They still preserve the frozen physical
                        // sale amounts, so reconcile only the Samtec wire quantity
                        // without restoring a physical-movement association.
                        signing=HostFiscalSigningMapper.Invoice(invoice,
                            signingScope,content,false,eftPosTID,true);
                    }
                    string companyQr=Reports.BrandData.SetBrandData(invoice);
                    return new InvoiceWorkflowSnapshot
                    {
                        InvoiceId=invoice.InvoiceId,
                        Provider=provider,
                        LocalOnly=localOnly,
                        Invoice=request,
                        Signing=signing,
                        PrintFormat=SharedInvoicePrinting.Format,
                        PrintDestination=destination,
                        PrintContent=printContent,
                        CompanyQrContent=companyQr
                    };
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
