using ArbitransMyData.MyDataApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exedron.ProviderInvoicing
{
    public class InvoiceHandler
    {
        private static InvoiceHandler instance;
        public static InvoiceHandler Instance
        {
            get
            {
                if (instance == null)
                    instance = new InvoiceHandler();
                return instance;
            }
        }

        private LicenseHandler lic = new LicenseHandler("Exedron");
        private DateTime lastActivationCheck;

        public string AadeUserName { set; get; }
        public string AadeSubscriptionKey { set; get; }
        public string ArbitransName { set; get; }
        public string ArbitransKey { set; get; }
        public string IlydaUsername { set; get; }
        public string IlydaPassword { set; get; }
        public string IlydaUsernameTest { set; get; }
        public string IlydaPasswordTest { set; get; }
        public bool ProviderIsTestMode { set; get; }

        public string CompanyVAT { set; get; }
        public string CompanyTaxOffice { set; get; }
        public string CompanyOccupation { set; get; }
        public string CompanyEmail { set; get; }
        public string CompanyName { set; get; }
        public string CompanyPhone { set; get; }
        public int CompanyBranch { set; get; } = 0;
        public string CompanyCity { set; get; }
        public string CompanyPostalCode { set; get; }
        public string CompanyAddressNumber { set; get; }
        public string CompanyAddressStreet { set; get; }
        public string PosTerminalId { set; get; }
        public string VivaWalletClientId { set; get; }
        public string VivaWalletClientSecret { set; get; }
        public string MellonGroupApiKey { set; get; }
        public PosTypeEnum PosType { set; get; }
        public int MellonGroupNsp { set; get; }
        public string Organization { set; get; }
        private ArbitransMyData.POS.VivaWalletPOS VivaWallet;
        private ArbitransMyData.POS.MellonGroupPOS MellonGroup;
        private string MellonTerminalId = "";
        private string MellonTerminalId2 = "";
        public string[] Errors
        {
            get
            {
                List<string> errors = new List<string>();
                if (string.IsNullOrEmpty(AadeUserName))
                    errors.Add("Δεν βρέθηκε Ονομα Χρήστη ΑΑΔΕ");
                if (string.IsNullOrEmpty(AadeSubscriptionKey))
                    errors.Add("Δεν βρέθηκε Subscription Key ΑΑΔΕ");
                if (string.IsNullOrEmpty(ArbitransName))
                    errors.Add("Δεν βρέθηκε Όνομα Χρήστη Arbitrans");
                if (string.IsNullOrEmpty(ArbitransKey))
                    errors.Add("Δεν βρέθηκε Κλειδί Arbitrans");
                if (string.IsNullOrEmpty(IlydaUsername))
                    errors.Add("Δεν βρέθηκε Όνομα Χρήστη Παρόχου Ηλ. Τιμολόγησης");
                if (string.IsNullOrEmpty(IlydaPassword))
                    errors.Add("Δεν βρέθηκε Κωδικός Πρόσβασης Παρόχου Ηλ. Τιμολόγησης");
                return errors.ToArray();
            }
        }

        private InvoiceHandler()
        {
        }

        public void CreatePosHandlers(bool reset = false)
        {
            if (VivaWallet != null && !reset)
                return;
            this.VivaWallet = new ArbitransMyData.POS.VivaWalletPOS(ArbitransName, ArbitransKey, "ClientID", "ClientSecret", ProviderIsTestMode);
            this.MellonGroup = new ArbitransMyData.POS.MellonGroupPOS(ArbitransName, ArbitransKey, MellonGroupNsp, ProviderIsTestMode);
            var terminals = this.MellonGroup.getTerminalList(PosTerminalId);
            if (terminals.Count > 0)
            {
                MellonTerminalId = terminals[0].TerminalID;
                MellonTerminalId2 = terminals[0].Id;
            }
        }

        private ActivationResult CheckActivation()
        {
            try
            {
                if (lastActivationCheck.Date == DateTime.Now.Date)
                {
                    return new ActivationResult()
                    {
                        Override = true
                    };
                }
                lastActivationCheck = DateTime.Now;
                var info = lic.ReadCurrentInfo();
                if (info.SubscriptionInfo.ExirationDate > DateTime.Now.Date && info.SubscriptionInfo.User == ArbitransName && info.SubscriptionInfo.Key == ArbitransKey)
                {
                    return new ActivationResult()
                    {
                        Override = true
                    };
                }
                return lic.EnsureActivated(ArbitransName, ArbitransKey);
            }
            catch
            {
                return new ActivationResult()
                {
                    HasError = true
                };
            }
        }

        public ProviderTypeEnum Provider { set; get; } = ProviderTypeEnum.Ilyda;

        public InvoiceResponse SendInvoice(InvoiceModel inv)
        {
            //Arbitrans Name: ARAISKάKISDIMOSTH
            //Arbitrans Key: 35V8ITQ36GNO7JAO534NA2BCQ8V
            //ilyda username: arambatsis_test
            //ilyda password: Gfykd@DbB!RD1$tdwxD&K6hl

            var res = CheckActivation();
            if (res.HasError)
            {
                return new InvoiceResponse()
                {
                    Errors = new string[] { "License Validation Error" }
                };
            }
            bool isTest = ProviderIsTestMode;
            ArbitransMyData.MyDataUtilities.myDATATools tools = new ArbitransMyData.MyDataUtilities.myDATATools(ArbitransName, ArbitransKey);
            var sni = new SendInvoices(AadeUserName, AadeSubscriptionKey, ArbitransName, ArbitransKey, isTest);
            ArbitransMyData.MyDataApi.SendInvoices.invoice invoice = sni.newInvoice();
            //var movePurposes = tools.getMovePurpose();
            //var vivaPos = new ArbitransMyData.POS.VivaWalletPOS(ArbitransName, ArbitransKey, "", "", isTest);// VivaWallet.getAvailablePOS()
            //var mellonPos = new ArbitransMyData.POS.MellonGroupPOS(ArbitransName, ArbitransKey, 4, isTest);
            //var mesUnits = tools.getMeasurementUnit();
            //var movePurposes = tools.getMovePurpose();

            //tools.get

            #region issuer and counterpart
            invoice.issuer.vatNumber = inv.Issuer.VATNumber;
            invoice.issuer.country = inv.Issuer.Country;
            invoice.issuer.street = inv.Issuer.Address.Street;
            invoice.issuer.streetNo = inv.Issuer.Address.Number;
            invoice.issuer.city = inv.Issuer.Address.City;
            invoice.issuer.postalCode = inv.Issuer.Address.PostalCode;

            if (inv.CounterPart != null)
            {
                invoice.counterpart.vatNumber = inv.CounterPart.VATNumber;
                invoice.counterpart.country = inv.CounterPart.Country;
                invoice.counterpart.street = inv.CounterPart.Address.Street;
                invoice.counterpart.streetNo = inv.CounterPart.Address.Number;
                invoice.counterpart.city = inv.CounterPart.Address.City;
                invoice.counterpart.postalCode = inv.CounterPart.Address.PostalCode;
                if (inv.InvoiceHeader.FuelInvoice)
                {
                    invoice.counterpart.supplyAccountNo = inv.CounterPart.SupplyAccountNo;
                }
            }
            if (inv.InvoiceHeader.FuelInvoice)
            {
                invoice.elements.fuelInvoice = true;
            }
            if (inv.InvoiceHeader.DeliveryNote)
            {
                invoice.elements.isDeliveryNote = true; // REQUIRED FOR DELIVERY NOTES
                invoice.elements.noteAddress.loaAdd.street = inv.Issuer.Address.Street;
                invoice.elements.noteAddress.loaAdd.streetNo = inv.Issuer.Address.Number;
                invoice.elements.noteAddress.loaAdd.postalCode = inv.Issuer.Address.PostalCode;
                invoice.elements.noteAddress.loaAdd.city = inv.Issuer.Address.City;

                invoice.elements.noteAddress.delAdd.street = inv.Issuer.Address.Street;
                invoice.elements.noteAddress.delAdd.streetNo = inv.Issuer.Address.Number;
                invoice.elements.noteAddress.delAdd.postalCode = inv.Issuer.Address.PostalCode;
                invoice.elements.noteAddress.delAdd.city = inv.Issuer.Address.City;
                invoice.elements.dispatchDate = inv.InvoiceHeader.DispatchDateTime;
                invoice.elements.movePurpose = (int)inv.InvoiceHeader.MovePurpose;
                invoice.elements.vehicleNumber = inv.InvoiceHeader.VehicleNumber;
            }
            #endregion

            #region elements
            invoice.elements.series = inv.InvoiceHeader.Series;
            invoice.elements.aa = long.Parse(inv.InvoiceHeader.AA);
            invoice.elements.issueDate = inv.InvoiceHeader.IssueDate;
            invoice.elements.invoiceType = inv.InvoiceHeader.InvoiceType;
            if (inv.InvoiceHeader.OtherDeliveryNoteHeader != null)
                invoice.elements.isDeliveryNote = inv.InvoiceHeader.OtherDeliveryNoteHeader.IsDeliveryNote;
            if (inv.InvoiceHeader.CorrelatedInvoices != null && inv.InvoiceHeader.CorrelatedInvoices.Length > 0)
                invoice.elements.correlatedInvoices = string.Join(",", inv.InvoiceHeader.CorrelatedInvoices);
            #endregion

            #region Lines
            invoice.lines = new List<SendInvoices.invoiceLine>();
            foreach (var line in inv.InvoiceDetails)
            {
                var ln = sni.newLine();
                if (inv.InvoiceHeader.InvoiceType.StartsWith("9."))
                {
                    line.VATCategory = VATCategoryEnum.NoVATEntry;
                    line.VATInvoicingCategory = VATInvoicingCategoryEnum.Zero;
                    line.IncomeClassification.ClassificationType = "category3";
                    line.IncomeClassification.ClassificationCategory = "";
                    line.NetValue = 0;
                    line.VATAmount = 0;
                    ln.itemCode = line.FuelCode;
                    ln.notVAT195 = true;
                }
                ln.fuelCode = string.IsNullOrEmpty(line.FuelCode) ? 0 : int.Parse(line.FuelCode);
                ln.netValue = (double)line.NetValue;
                ln.VatCategory = (int)line.VATCategory;
                ln.VatAmount = (double)line.VATAmount;
                ln.classification.Type = line.IncomeClassification.ClassificationType;
                ln.classification.Category = line.IncomeClassification.ClassificationCategory;
                ln.lineComments = line.LineComments;

                ln.measurementUnit = (int)line.MeasurementUnit;
                if (line.VATCategory == VATCategoryEnum.NoVAT || line.VATCategory == VATCategoryEnum.NoVATEntry)
                    ln.itemDescr = line.ItemDescription;
                invoice.lines.Add(ln);
            }
            #endregion

            #region payments
            if (!inv.InvoiceHeader.InvoiceType.StartsWith("9."))
            {
                invoice.payments = new List<SendInvoices.paymentMethod>();
                if (inv.PaymentMethods != null)
                {
                    foreach (var pm in inv.PaymentMethods)
                    {
                        var tmpPay = sni.newPaymentMethod();
                        tmpPay.@type = (int)pm.Type;
                        tmpPay.amount = (double)pm.Amount;
                        invoice.payments.Add(tmpPay);
                    }
                }
            }
            #endregion
            if (Provider == ProviderTypeEnum.Ilyda)
            {
                string iun = ProviderIsTestMode ? IlydaUsernameTest : IlydaUsername;
                string iup = ProviderIsTestMode ? IlydaPasswordTest : IlydaPassword;
                ArbitransMyData.Providers.ProvIlyda ilyda = new ArbitransMyData.Providers.ProvIlyda(iun, iup, ArbitransName, ArbitransKey, isTest);

                //var resultMyData = sni.SendInvoice(invoice, true, true);

                var ilydaData = new SendInvoices.ilydaData();

                inv.Issuer.Email = CompanyEmail;
                if (inv.CounterPart != null)
                {
                    ilydaData.extraDetails.counterpartName = inv.CounterPart.Name;
                    ilydaData.extraDetails.counterpartAddress = inv.CounterPart.Address.ToString();
                    ilydaData.extraDetails.counterpartCity = inv.CounterPart.Address.City;
                    ilydaData.extraDetails.counterpartPostalCode = inv.CounterPart.Address.PostalCode;
                    ilydaData.extraDetails.counterpartEmail = string.IsNullOrEmpty(inv.CounterPart.Email) ? inv.Issuer.Email : inv.CounterPart.Email;
                    ilydaData.extraDetails.counterpartPhone = inv.CounterPart.Phone;
                    ilydaData.extraDetails.counterpartCode = "";
                    ilydaData.extraDetails.counterpartJob = "";
                    ilydaData.extraDetails.counterpartTaxOffice = inv.CounterPart.TaxOffice;
                }
                ilydaData.extraDetails.issuerName = inv.Issuer.Name;
                ilydaData.extraDetails.issuerAddress = inv.Issuer.Address.ToString();
                ilydaData.extraDetails.issuerPostalCode = inv.Issuer.Address.PostalCode;
                ilydaData.extraDetails.issuerCity = inv.Issuer.Address.City;
                ilydaData.extraDetails.issuerEmail = inv.Issuer.Email;
                ilydaData.extraDetails.issuerPhone = inv.Issuer.Phone;
                ilydaData.extraDetails.issuerJob = "";
                ilydaData.extraDetails.issuerTaxOffice = inv.Issuer.TaxOffice;
                ilydaData.extraDetails.issuerGemh = string.IsNullOrEmpty(inv.Issuer.Gemi) ? "" : inv.Issuer.Gemi;
                ilydaData.extraDetails.b2g = false;
                ilydaData.extraDetails.govID = "";
                ilydaData.extraDetails.invoiceNotes = "";
                ilydaData.extraDetails.paymentTerms = inv.PaymentTerms;
                ilydaData.extraDetails.contractReference = ""; // For B2B transactions leave empty.
                ilydaData.extraDetails.projectReference = "";
                if (inv.PaymentMethods != null)
                {
                    if (inv.PaymentMethods.Any(p => p.Type == PaymentMethodEnum.POS || inv.PaymentMethods.Any(pp => pp.Type == PaymentMethodEnum.IRIS)))
                        ilydaData.extraDetails.paymentTerminalID = PosType == PosTypeEnum.VivaWallet ? PosTerminalId : MellonTerminalId;
                    else
                        ilydaData.extraDetails.paymentTerminalID = "";
                }
                ilydaData.extraDetails.correlatedInvoice = string.IsNullOrEmpty(inv.InvoiceHeader.CorrelatedInvoice) ? "" : inv.InvoiceHeader.CorrelatedInvoice;

                ilydaData.extraDetails.invoiceName = "";
                ilydaData.extraDetails.buyerReference = "";
                ilydaData.extraDetails.movePurpose = "";
                ilydaData.extraDetails.dispatchPlace = "";
                ilydaData.extraDetails.partyName = "";
                ilydaData.extraDetails.partyAddress = "";
                ilydaData.extraDetails.partyPostalCode = "";
                ilydaData.extraDetails.partyCity = "";
                ilydaData.extraDetails.partyCountryCode = "";
                ilydaData.extraDetails.b2gAddDocs = new List<SendInvoices.B2GDocs>();
                ilydaData.extraDetails.purchaseOrderReference = "";
                ilydaData.invoiceLines = new List<SendInvoices.provLine>();
                foreach (var il in inv.InvoiceDetails)
                {
                    var invLine = new SendInvoices.provLine();
                    //invLine.discountAmount = il.d
                    //invLine.discountPercentage
                    //invLine.itemCode
                    //invLine.itemClass = B2G
                    string vatCategory = "";
                    switch (il.VATInvoicingCategory)
                    {
                        case VATInvoicingCategoryEnum.Excemption:
                            vatCategory = "E";
                            break;
                        case VATInvoicingCategoryEnum.Export:
                            vatCategory = "G";
                            break;
                        case VATInvoicingCategoryEnum.Zero:
                            vatCategory = "Z";
                            break;
                        default:
                            vatCategory = "S";
                            break;
                    }
                    invLine.itemMeasurementUnit = il.MeasurementUnit.ToString();
                    invLine.itemName = string.IsNullOrEmpty(il.ItemDescription) ? "" : il.ItemDescription;
                    invLine.itemCode = il.ItemCode;

                    invLine.countryOfOrigin = "";
                    invLine.peppolTaxCategory = vatCategory;
                    invLine.peppolMeasurementUnit = "";
                    invLine.peppolExemptionCode = "";
                    invLine.peppolExemptionText = "";

                    invLine.itemClass = new List<SendInvoices.peppolItemClass>();
                    SendInvoices.peppolItemClass ic = new SendInvoices.peppolItemClass();
                    ic.itemClass = "";
                    ic.itemID = "";
                    invLine.itemClass.Add(ic);
                    ilydaData.invoiceLines.Add(invLine);
                }

                //var invJson = Newtonsoft.Json.JsonConvert.SerializeObject(invoice);
                //var ilDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(ilydaData);
                CreatePosHandlers();
                var terminalId = PosTerminalId;
                string transactionId = "";
                string sessionID = string.Format(DateTime.Now.ToString("yyyyMMddmmssfff-EX1")); // The SessionID must be unique. The same SessionID cannot be used twice.
                if (inv.PaymentMethods != null && !string.IsNullOrEmpty(terminalId) && inv.PaymentMethods.Any(pp => pp.Type == PaymentMethodEnum.POS || pp.Type == PaymentMethodEnum.IRIS))
                {
                    if (PosType == PosTypeEnum.VivaWallet)
                        terminalId = PosTerminalId;
                    else
                    {
                        terminalId = MellonTerminalId;
                    }
                    ilydaData.extraDetails.paymentTerminalID = terminalId;
                    Dictionary<string, string> posSign = null;
                    if (PosType == PosTypeEnum.Mellon)
                        posSign = ilyda.getPOSSignature(invoice, terminalId, 2);
                    else
                        posSign = ilyda.getPOSSignature(invoice, terminalId, 1);
                    if (!posSign.ContainsKey("invoiceSignatures[0].signedContent"))
                    {
                        return new InvoiceResponse()
                        {
                            Errors = new string[] { "POS Failed to send Signature" }
                        };
                    }
                    string signature = posSign["invoiceSignatures[0].signature"];
                    if (PosType == PosTypeEnum.VivaWallet)
                    {

                        ArbitransMyData.POS.VivaWalletPOS.Transaction tra = this.VivaWallet.NewTransaction(); // initialize a new Sale Object
                        tra.sessionID = sessionID; // "08122023-11" 'A unique Session ID
                        tra.terminalID = ilydaData.extraDetails.paymentTerminalID;
                        tra.cashRegisterID = "no Cash Register"; // The Cash Register ID
                        tra.amount = (double)inv.PaymentMethods.Where(pp => pp.Type == PaymentMethodEnum.POS || pp.Type == PaymentMethodEnum.IRIS).Sum(pp => pp.Amount); // The amount to be charged
                        tra.currencyCode = "978"; // The currency ISO Code. The 978 is EUR ISO Code
                        tra.merchantReference = string.Format("{2}{1} - {1:dd/MM/yyyy}", inv.InvoiceHeader.AA, inv.InvoiceHeader.IssueDate, inv.InvoiceHeader.Series);
                        tra.customerTrns = string.Format("{2}{1} - {1:dd/MM/yyyy}", inv.InvoiceHeader.AA, inv.InvoiceHeader.IssueDate, inv.InvoiceHeader.Series);
                        tra.preauth = false; // Boolean flag indicating whether the payment is or is not a pre-authorization.
                        tra.maxInstalments = 12; // Max instalments allowed during card presentment
                        tra.tipAmount = 0.0d; // The desired Tip Amount
                        if (inv.PaymentMethods.Any(pp => pp.Type == PaymentMethodEnum.POS))
                            tra.paymentMethod = 1;

                        tra.aadeProviderId = 008; // The Invoicing Provider Code. Check re "Remarks" of the field aadeProviderId to find the Provider code you need.
                        tra.aadeProviderSignatureData = signature; // The aadeProviderSignatureData
                        tra.aadeProviderSignature = posSign["invoiceSignatures[0].signature"]; // The aadeProviderSignature
                        string resultVW = this.VivaWallet.newSale(tra); // Send the sale to the POS.
                        while (true)
                        {
                            string traStatus = this.VivaWallet.checkSession(tra.sessionID); // Get the status of the session
                            string statusStart = traStatus.Substring(3);

                            switch (statusStart)
                            {
                                case "200": //Transaction successful 
                                    break;
                                case "204": //The session is being processed 
                                    continue;
                                case "205": //Other Message 
                                case "998": //Transaction aborted successfully  
                                case "999": //Transaction aborted successfully
                                    return new InvoiceResponse()
                                    {
                                        Errors = new string[] { "Transaction Failed or Aborted" }
                                    };
                            }
                            transactionId = this.VivaWallet.getTransactionID(sessionID);
                            break;
                        }
                    }
                    else if (PosType == PosTypeEnum.Mellon)
                    {
                        var paymentData = posSign["invoiceSignatures[0].signedContent"];
                        var paymentSignature = posSign["invoiceSignatures[0].signature"];
                        string[] payData = paymentData.Split(';'); // Split the returned paymentData

                        ArbitransMyData.POS.MellonGroupPOS.posTransaction tra = this.MellonGroup.newTransaction(); // initialize a new Sale Object
                        tra.Amount = (double)inv.PaymentMethods.Where(pp => pp.Type == PaymentMethodEnum.POS || pp.Type == PaymentMethodEnum.IRIS).Sum(pp => pp.Amount);
                        tra.CashbackAmount = 0;
                        tra.CurrencyCode = "978";
                        tra.CustomerReference = Guid.NewGuid().ToString();
                        tra.Instalments = 1;
                        tra.PaymentType = inv.PaymentMethods.Any(pp => pp.Type == PaymentMethodEnum.Credit) ? 0 : 1;
                        tra.TxnType = 0;
                        tra.Timeout = 60;

                        tra.ProviderData.Uid = payData[0];
                        tra.ProviderData.SignatureTimestamp = payData[2];
                        tra.ProviderData.NetAmount = int.Parse(payData[3]);
                        tra.ProviderData.VatAmount = int.Parse(payData[4]);
                        tra.ProviderData.TotalAmount = int.Parse(payData[5]);
                        tra.ProviderData.ProviderId = 8; // 7 for PRIMER, 8 for ILYDA, 15 for ORIAN
                        tra.ProviderData.Signature = paymentSignature;
                        var traRes = this.MellonGroup.sendTransaction(PosTerminalId, MellonTerminalId2, tra); // Send the sale to the POS.

                        if (traRes.ContainsKey("TransactionId"))
                        {
                            transactionId = traRes["TransactionId"];
                            if (string.IsNullOrEmpty(transactionId))
                                return new InvoiceResponse()
                                {
                                    Errors = new string[] { "Transaction Failed or Aborted" }
                                };
                        }
                        else
                        {
                            return new InvoiceResponse()
                            {
                                Errors = new string[] { "Transaction Failed or Aborted" }
                            };
                        }
                    }
                    for (int i = 0; i < invoice.payments.Count(); i++)
                    {
                        var payment = invoice.payments[i];
                        if (payment.type == (int)PaymentMethodEnum.POS || payment.type == (int)PaymentMethodEnum.IRIS)
                        {
                            if (payment.type == (int)PaymentMethodEnum.POS)
                                payment.transactionID = transactionId;
                            else
                                payment.ProviderSignature.EndToΕndReferenceID = transactionId;

                            payment.tid = ilydaData.extraDetails.paymentTerminalID;
                            payment.ProviderSignature.Signature = signature;
                            payment.ProviderSignature.SigningAuthor = "008";
                            invoice.payments[i] = payment;
                        }
                    }

                }

                var resultIlyda = ilyda.sendInvoice(invoice, ilydaData);
                var response = new InvoiceResponse();
                response.Mark = resultIlyda.ContainsKey("invoiceMarking.mark") ? resultIlyda["invoiceMarking.mark"] : "";
                response.Uid = resultIlyda.ContainsKey("invoiceMarking.invoiceId") ? resultIlyda["invoiceMarking.invoiceId"] : "";
                response.QrCodeUrl = resultIlyda.ContainsKey("invoiceMarking.myDataQrCode") ? resultIlyda["invoiceMarking.myDataQrCode"] : "";
                response.InvoiceUrl = resultIlyda.ContainsKey("invoiceMarking.qrCode") ? resultIlyda["invoiceMarking.qrCode"] : "";
                response.VerificationHash = resultIlyda.ContainsKey("invoiceMarking.verificationHash") ? resultIlyda["invoiceMarking.verificationHash"] : "";
                response.ProviderUrl = resultIlyda.ContainsKey("invoiceMarking.providerUrl") ? resultIlyda["invoiceMarking.providerUrl"] : "";
                response.InvoiceSignature = resultIlyda.ContainsKey("invoiceSignatures") ? resultIlyda["invoiceSignatures"] : "";
                int errorIndex = 0;
                List<string> errors = new List<string>();
                List<string> warnings = new List<string>();
                while (true)
                {
                    string errorKey = "errors[" + errorIndex.ToString() + "]";
                    if (resultIlyda.ContainsKey(errorKey + ".code"))
                    {
                        string code = resultIlyda[errorKey + ".code"];
                        string message = resultIlyda[errorKey + ".defaultMessage"];
                        string fatal = resultIlyda[errorKey + ".fatal"];
                        if (resultIlyda.ContainsKey(errorKey + ".fields"))
                        {
                            string fields = resultIlyda[errorKey + ".fields"];
                            //if(!string.IsNullOrEmpty(fields))
                            //{

                            //}
                        }
                        if (fatal == "True")
                            errors.Add(code + " - " + message);
                        else
                            warnings.Add(code + " - " + message);
                        errorIndex++;
                    }
                    else
                        break;
                }
                response.Errors = errors.ToArray();
                response.Warnings = warnings.ToArray();
                if (response.HasErrors && !string.IsNullOrEmpty(transactionId))
                {
                    if (PosType == PosTypeEnum.Mellon)
                    {
                        var resAbort = this.MellonGroup.cancelCompletedTransaction(PosTerminalId, MellonTerminalId2, transactionId);
                    }
                    else if (PosType == PosTypeEnum.VivaWallet)
                    {
                        var resAbort = this.VivaWallet.sendAbort(sessionID, "no Cash Register");
                    }
                }
                return response;
            }
            else
            {
                return null;
            }
        }

        public enum ProviderTypeEnum
        {
            Ilyda,
            Orian
        }
        public enum PosTypeEnum
        {
            None,
            Mellon,
            VivaWallet
        }
    }
}
