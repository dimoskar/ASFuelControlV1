using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ASFuelControl.Logging;
using System.Net.Sockets;
using System.Drawing.Printing;
using ASFuelControl.Communication.Enums;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    ///Class that provides an thread that handles all printing jobs that are generated from the main controller thread.
    /// </summary>
    public class PrintAgent
    {
        public static List<Guid> ExcludeInvoices = new List<Guid>();
        private static string[] installedPrintrers = new string[] { };
        private static DateTime closeSalesDate = Data.Implementation.OptionHandler.Instance.GetDateTimeOption("CloseSalesDate", DateTime.Now.AddDays(-1));
        FileSystemWatcher signWatcher = new FileSystemWatcher();
        private DateTime lastMyDataSent = DateTime.MinValue;
        private DateTime oldInvoicesDateStart = Data.Implementation.OptionHandler.Instance.GetDateTimeOption("MyDataStartDate", DateTime.Today);//DateTime.Parse("2022/01/01");
        
        #region private variables

        //private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private bool agentRunning = false;
        private string outFolder = "";
        private string signFolder = "";
        private string processFolder = "";
        private bool windowsPrint = false;
        private int thermalPaperWidth = 80;
        private bool printBarCode = false;
        private string invoiceSignLine = "";
        private int invoiceReplaceCodeFrom = 173;
        private int invoiceReplaceCodeTo = 222;
        private string taxNumber = "";
        private int tailInvLines = 0;
        private int invoiceCopies = 0;
        private string defaultTaxDevice = "";
        private System.Threading.Thread th;
        System.Threading.Thread thOldInvoices;
        System.Threading.Thread thResendInvoices;
        private Dictionary<string, List<Guid>> entriesProcessed = new Dictionary<string, List<Guid>>();
        private List<string> invoicesProcessed = new List<string>();
        private List<string> fillingsProcessed = new List<string>();
        private Guid incomeInvoiceType;
        private Guid litercheckInvoiceType;
        private Guid income2InvoiceType;
        private List<string> filesInProcess = new List<string>();
        private List<string> signsApplied = new List<string>();

        private bool printPhysical = false;
        private bool printAlertsPhysical = false;
        private bool printBalancesPhysical = false;
        private static bool oldInvoiceRunning = false;
        private static bool resendInvoiceRunning = false;
        private static bool invoicingProviderEnabled = false;

        private string samtecWSUrl = "";
        private string eftPosTid = "";

        #endregion

        public PrintAgent()
        {
            var samtecSetings = Samtec.WebService.SamtecSettings.ReadSettings();
            samtecWSUrl = samtecSetings.SamtecUrl + ":" + samtecSetings.SamtecURLPort;
            eftPosTid = samtecSetings.EftPosTID;
            List<string> instPrinterList = new List<string>();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                instPrinterList.Add(printer);
            }
            installedPrintrers = instPrinterList.ToArray();
            this.defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
            if(this.defaultTaxDevice.ToLower() == "samtec")
            {
                this.signFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_SignFolder");
                this.outFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_OutFolder");
            }
            else if(this.defaultTaxDevice.ToLower() == "signa")
            {
                this.signFolder = Data.Implementation.OptionHandler.Instance.GetOption("SignA_SignFolder");
                this.outFolder = this.signFolder;
            }
            this.processFolder = this.signFolder + "\\" + "ProcessDocuments";
            if(!Directory.Exists(this.processFolder))
                Directory.CreateDirectory(this.processFolder);
            this.tailInvLines = Data.Implementation.OptionHandler.Instance.GetIntOption("TailInvoiceLine", 5);
            this.invoiceCopies = Data.Implementation.OptionHandler.Instance.GetIntOption("InvoiceCopies", 2);
            this.invoiceSignLine = Data.Implementation.OptionHandler.Instance.GetOption("SingLine");
            this.windowsPrint = Data.Implementation.OptionHandler.Instance.GetBoolOption("WindowsInvoicePrint", false);
            this.thermalPaperWidth = Data.Implementation.OptionHandler.Instance.GetIntOption("ThermalPaperWidth", 80);
            this.printBarCode = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintInvoiceBarcode", false);
            this.litercheckInvoiceType = Data.Implementation.OptionHandler.Instance.GetGuidOption("3f54a35b-01f6-43b8-a17e-2d4927e319b8", Guid.Empty);
            this.printPhysical = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintOnPrinter", false);
            this.printAlertsPhysical = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintAlertsOnPrinter", false);
            this.printBalancesPhysical = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintBalancelsOnPrinter", true);
            this.invoiceReplaceCodeFrom = Data.Implementation.OptionHandler.Instance.GetIntOption("InvoiceReplaceCodeFrom", 173);
            this.invoiceReplaceCodeTo = Data.Implementation.OptionHandler.Instance.GetIntOption("InvoiceReplaceCodeTo", 222);
            invoicingProviderEnabled = Data.Implementation.OptionHandler.Instance.GetBoolOption("ProviderEnabled", false);
            if (!System.IO.Directory.Exists(this.outFolder))
                System.IO.Directory.CreateDirectory(this.outFolder);
            //signWatcher.Path = this.outFolder;
            //signWatcher.Created += SignWatcher_Created;
            
        }

        private void SignWatcher_Created(object sender, FileSystemEventArgs e)
        {
            if (!(e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed))
                return;
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                System.IO.FileInfo file = new FileInfo(e.FullPath);
                if (file.Name == "CloseSales.out")
                {
                    System.IO.File.Delete(file.FullName);
                    return;
                }
                FileTypeInfo ft = this.GetFileTypeInfo(file.Name, "out");

                string str = "";

                try
                {
                    str = System.IO.File.ReadAllText(file.FullName);
                }
                catch (Exception ex)
                {
                    return;
                }
                string evalResp = this.EvaluateSign(db, file.FullName, ft.Id, ft.FileType);
                if (evalResp == "Error" || evalResp == "")
                {
                    lock (this.filesInProcess)
                    {
                        if (this.filesInProcess.Contains(file.Name.Replace(".out", ".txt")))
                            this.filesInProcess.Remove(file.Name.Replace(".out", ".txt"));
                    }
                    lock (this.invoiceProcessed)
                    {
                        if (this.invoiceProcessed.Contains(ft.Id))
                            this.invoiceProcessed.Remove(ft.Id);
                        if (this.invoicesProcessed.Contains(ft.Id.ToString()))
                            this.invoicesProcessed.Remove(ft.Id.ToString());
                    }
                    file.Delete();
                }
            }
        }

        #region public methods

        public void Dispose()
        {
            //this.database.Dispose();
        }

        /// <summary>
        /// Starts the main thread of this Agent
        /// </summary>
        public void StartThread()
        {
            //this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            this.agentRunning = true;
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.Start();
            //if (Exedron.MyData.Settings.IsActive)
            //{
            //    thOldInvoices = new System.Threading.Thread(new System.Threading.ThreadStart(this.SendOldInvoiceThread));
            //    thOldInvoices.Start();
            //    thResendInvoices = new System.Threading.Thread(new System.Threading.ThreadStart(this.ResendFailedThread));
            //    thResendInvoices.Start();
            //    //var query =
            //    //   from inv in db.Invoices
            //    //   where inv.TransactionDate > DateTime.Parse("2021/01/01") &&
            //    //        inv.InvoiceType.SendToMyData &&
            //    //        !db.MyDataInvoices.Select(m=>m.InvoiceId).Contains(inv.InvoiceId)
            //    //   orderby inv.TransactionDate descending
            //    //   select inv.InvoiceId;
            //    //myDataInvoices = query.Take(1).ToArray();
            //}
        }

        /// <summary>
        /// Stops the main thread of this Agent
        /// </summary>
        public void StopThread()
        {
            this.agentRunning = false;
        }

        #endregion

        #region static methods

        public static void PrintInvoiceDirect(Data.Invoice inv)
        {
            Guid invoiceId = inv.InvoiceId;
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
                if (invoicingProviderEnabled == false && invoice.InvoiceType.SendToMyData)
                {
                    if (!MyDataSendInvoice(invoice.InvoiceId, 1))
                        return;
                }
                var myDataInvoice = db.MyDataInvoices.FirstOrDefault(m => m.InvoiceId == inv.InvoiceId);
                var myDataQrCode = "";
                if (myDataInvoice != null && myDataInvoice.Errors != null)
                {
                    var parms = myDataInvoice.Errors.Split('|');
                    if (parms.Length > 1)
                        myDataQrCode = parms[1];
                }
                if (invoice.InvoiceType.IsLaserPrint.HasValue && invoice.InvoiceType.IsLaserPrint.Value)
                {

                    bool isInternal = invoice.InvoiceType.IsInternal.HasValue ? invoice.InvoiceType.IsInternal.Value : true;


                    if (invoice.InvoiceType.IsInternal.HasValue && invoice.InvoiceType.IsInternal.Value)
                    {
                        using (Reports.Invoices.InvoiceReport report = new Reports.Invoices.InvoiceReport())
                        {
                            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");

                            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");
                            report.ReportParameters["InvoiceId"].Value = invoice.InvoiceId.ToString().ToLower();
                            report.ReportParameters["CompanyName"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                            report.ReportParameters["CompanyAddress"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
                            report.ReportParameters["CompanyCity"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                            report.ReportParameters["CompanyVATNumber"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                            report.ReportParameters["CompanyVATOffice"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
                            report.ReportParameters["CompanyOccupation"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
                            report.ReportParameters["CompanyReferenceNumber"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyReferenceNumber");

                            report.ReportParameters["ShowAdditionalData"].Value = isInternal;
                            report.ReportParameters["SignLine"].Value = "";// invoice.InvoiceSignature;
                            report.ReportParameters["CompanyEfk"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
                            
                            report.SetSupplyNumber(invoice, myDataQrCode);
                            System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                            short invCopies = (short)Data.Implementation.OptionHandler.Instance.GetIntOption("InvoiceCopies", 2);
                            printerSettings.Copies = invCopies;
                            PrintReport(report, printerSettings);

                            //report.ReportParameters["InvoiceId"].Value = invoice.InvoiceId.ToString().ToLower();
                            //report.ReportParameters["CompanyName"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                            //report.ReportParameters["CompanyAddress"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
                            //report.ReportParameters["CompanyCity"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                            //report.ReportParameters["CompanyVATNumber"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                            //report.ReportParameters["CompanyVATOffice"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
                            //report.ReportParameters["CompanyOccupation"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
                            //report.ReportParameters["ShowAdditionalData"].Value = isInternal;

                            //System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();

                            ////if(invoice.Printer != null && invoice.Printer != "")
                            ////    printerSettings.PrinterName = invoice.Printer;
                            //short invCopies = (short)Data.Implementation.OptionHandler.Instance.GetIntOption("InvoiceCopies", 2);
                            //printerSettings.Copies = invCopies;
                            //PrintReport(report, printerSettings);

                            ////Telerik.Reporting.Processing.ReportProcessor processor = new Telerik.Reporting.Processing.ReportProcessor();
                            ////processor.PrintReport(report, printerSettings);
                        }
                    }
                    else
                    {
                        using (Reports.Invoices.IncoiceReportShort report = new Reports.Invoices.IncoiceReportShort())
                        {
                            var qil = db.InvoiceLines.Where(il => il.InvoiceId == invoice.InvoiceId).ToArray();

                            if (invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
                            {
                                decimal vat = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 24);
                                foreach (var inl in qil)
                                {
                                    inl.UnitPrice = inl.UnitPrice / ((100 + vat) / 100);
                                }
                            }

                            report.DataSource = qil;
                            report.ReportParameters["ReplaceParameter"].Value = "";
                            //if (invoice.IsCanceling)
                            //    report.ReportParameters["ReplaceParameter"].Value = invoice.CancelingInvoiceString;
                            //if (invoice.IsReplacing)
                            //    report.ReportParameters["ReplaceParameter"].Value = invoice.ReplacingInvoiceString;
                            if (invoice.Notes == null)
                                report.ReportParameters["ReplaceParameter"].Value = "";
                            else
                                report.ReportParameters["ReplaceParameter"].Value = invoice.Notes;

                            report.ReportParameters["VATValue"].Value = invoice.InvoiceLines[0].VatPercentage;
                            if (invoice.InvoiceLines[0].VatPercentage == 0 && invoice.VatAmount > 0)
                            {
                                report.ReportParameters["VATValue"].Value = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
                            }

                            report.SetSupplyNumber(invoice, myDataQrCode);
                            report.SetRestAmounts(invoice);
                            System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                            short invCopies = (short)Data.Implementation.OptionHandler.Instance.GetIntOption("InvoiceCopies", 2);
                            printerSettings.Copies = invCopies;
                            PrintReport(report, printerSettings);
                        }
                    }
                }
                else
                {
                    bool windowsPrint = Data.Implementation.OptionHandler.Instance.GetBoolOption("WindowsInvoicePrint", false);
                    bool printBarCode = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintInvoiceBarcode", false);
                    int thermalPaperWidth = Data.Implementation.OptionHandler.Instance.GetIntOption("ThermalPaperWidth", 80);
                    int tailInvLines = Data.Implementation.OptionHandler.Instance.GetIntOption("TailInvoiceLine", 5);
                    if (windowsPrint)
                    {
                        //if (thermalPaperWidth == 80)
                        //{
                        //    using (Reports.InvoiceThermal report = new Reports.InvoiceThermal())
                        //    {
                        //        report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoicePrintViews");
                        //        report.ReportParameters[0].Value = invoice.InvoiceId.ToString().ToLower();
                        //        report.ReportParameters[1].Value = printBarCode;
                        //        report.ReportParameters[2].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                        //        report.ReportParameters[3].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
                        //        report.ReportParameters[4].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
                        //        report.ReportParameters[4].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                        //        report.ReportParameters[10].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyMainAddress");
                        //        report.ReportParameters[5].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                        //        report.ReportParameters[6].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                        //        report.ReportParameters[7].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
                        //        report.ReportParameters[8].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
                        //        report.ReportParameters[9].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
                        //        string efk = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
                        //        report.ReportParameters[11].Value = efk == null ? "" : efk;
                        //        report.SetSupplyNumber(invoice);
                        //        System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                        //        printerSettings.PrinterName = invoice.Printer;
                        //        PrintReport(report, printerSettings);
                        //    }
                        //}
                        //else
                        //{
                        using (Reports.Invoices.InvoiceReportNarrow report = new Reports.Invoices.InvoiceReportNarrow())
                        {
                            var fs = Data.Implementation.OptionHandler.Instance.GetDecimalOption("ThermalPrintFontSize", 9);
                            var fn = Data.Implementation.OptionHandler.Instance.GetOption("ThermalPrintFontName", "Bahnschrift Light Condensed");

                            report.SetFont(fn, (float)fs);
                            var invLines = db.InvoicePrintViews.Where(i => i.InvoiceId == invoice.InvoiceId);
                            foreach(var invLine in invLines)
                            {
                                invLine.InvoiceNettoAmount = invoice.NettoAmount.Value;
                                invLine.InvoiceTotalAmount = invoice.TotalAmount.Value;
                                invLine.InvoiceVatAmount = invoice.VatAmount.Value;
                                invLine.InvoiceDiscountAmount = invoice.DiscountAmount;
                                invLine.InvoiceNettoAfterDiscountAmount = invoice.NettoAfterDiscount;
                            }
                            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(invLines, "");
                            report.ReportParameters[0].Value = invoice.InvoiceId.ToString().ToLower();
                            report.ReportParameters[1].Value = printBarCode;
                            report.ReportParameters[2].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                            report.ReportParameters[3].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
                            report.ReportParameters[4].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
                            report.ReportParameters[4].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                            report.ReportParameters[10].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyMainAddress");
                            report.ReportParameters[5].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                            report.ReportParameters[6].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                            report.ReportParameters[7].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
                            report.ReportParameters[8].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
                            report.ReportParameters[9].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
                            string efk = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
                            report.ReportParameters[11].Value = efk == null ? "" : efk;
                            report.SetSupplyNumber(invoice, myDataQrCode);
                            System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                            SetPrinter(printerSettings, invoice);
                            PrintReport(report, printerSettings);
                        }
                        //}

                    }
                    else
                    {
                        if (Properties.Settings.Default.TCPPrinterAddress == "")
                        {
                            System.Diagnostics.Trace.WriteLine("Start Invoice Print : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.ffff"));
                            string[] invoiceLines = CreateInvoiceTextDirect(invoice);
                            //invoice.InvoiceLines[0].SalesTransaction.Nozzle.Dispenser.InvoicePrints
                            string printText = "";




                            for (int i = 0; i < invoiceLines.Length; i++)
                            {
                                printText = printText + invoiceLines[i] + "\r\n";
                            }

                            string sign = "Χώρος σήμανσης: " + invoice.InvoiceSignature;
                            printText = printText + sign;

                            for (int i = 0; i < tailInvLines; i++)
                            {
                                printText = printText + "\r\n";
                            }


                            string GS = Convert.ToString((char)29);
                            string ESC = Convert.ToString((char)27);
                            string COMMAND = "";
                            COMMAND = ESC + "@";
                            COMMAND += GS + "V" + (char)1;



                            //printText = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Default, Encoding.ASCII, Encoding.Default.GetBytes(printText)));

                            //DirectPrinter.SendToPrinter("ASFuelControl Invoice", printText, invoice.Printer);
                            DirectPrinter.SendToPrinter("ASFuelControl Invoice", printText + "\r\n" + COMMAND, invoice.Printer);

                            System.Diagnostics.Trace.WriteLine("Invoice Printed: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.ffff"));
                        }
                        else
                        {
                            TcpPrint(Properties.Settings.Default.TCPPrinterAddress, invoice);
                        }
                    }
                }
            }
            //new System.Threading.Tasks.Task(() =>
            //{
            //    if (inv.InvoiceType.SendToMyData)
            //    {
            //        if (!MyDataSendInvoice(inv.InvoiceId))
            //            failedInvoices.Add(inv.InvoiceId);
            //    }
            //}).Start();
        }

        public static void CloseSales()
        {
            string defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
            string signFolder = "";
            if (defaultTaxDevice.ToLower() == "samtec")
            {
                signFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_SignFolder");
            }
            else if (defaultTaxDevice.ToLower() == "signa")
            {
                signFolder = Data.Implementation.OptionHandler.Instance.GetOption("SignA_SignFolder");
            }

            string fileName = string.Format(signFolder + "\\{0}.txt", "CloseSales");

            if (System.IO.File.Exists(fileName))
                return;


            List<string> lines = new List<string>();
            lines.Add("[[CloseSales]]");

            System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);
            closeSalesDate = DateTime.Now;
            Data.Implementation.OptionHandler.Instance.SetOption("CloseSalesDate", closeSalesDate);
        }

        private static string[] CreateInvoiceTextDirect(Data.Invoice invoice)
        {
            bool microFont = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrinterMicroFont", false);
            int printerLineWidth = microFont ? 60 : 44;

            string address = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            string city = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            string fax = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
            string name = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            string occupation = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
            string phone = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
            string postalCode = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode");
            string taxOffice = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
            string tin = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            string efk = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
            string invoiceType = invoice.InvoiceType.Description;
            List<string> invoiceLines = new List<string>();
            invoiceLines.Add(name.CenterString(printerLineWidth));
            invoiceLines.Add(occupation.CenterString(printerLineWidth));
            invoiceLines.Add(address.CenterString(printerLineWidth));
            string taxData = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", tin, taxOffice).EquilizeString(printerLineWidth);
            string efkData = string.Format("ΑΦΚ / Αρ. Άδειας:{0}", efk);
            string contactData = string.Format("ΤΗΛ:{0}#FAX:{1}", phone, fax).EquilizeString(printerLineWidth);
            string dateInfo = string.Format("Ημερ/νία:{0}#Ώρα:{1}", invoice.TransactionDate.ToString("dd/MM/yyyy"), invoice.TransactionDate.ToString("HH:mm")).EquilizeString(printerLineWidth);
            string border1 = "--------------------------------------------";
            string border = "============================================";

            if (invoiceType.Length > 36)
                invoiceType = invoiceType.Substring(0, 36);
            string series = invoice.Series == null ? "" : invoice.Series;
            string invoiceData = string.Format("{0}#{2}{1}", invoiceType, invoice.Number, series).EquilizeString(printerLineWidth);
            invoiceLines.Add(taxData.CenterString(printerLineWidth));
            if(efk != null && efk != "")
                invoiceLines.Add(efkData.CenterString(printerLineWidth));
            invoiceLines.Add(contactData.CenterString(printerLineWidth));
            invoiceLines.Add("");
            invoiceLines.Add(invoiceData.CenterString(printerLineWidth));
            invoiceLines.Add(dateInfo.CenterString(printerLineWidth));
            invoiceLines.Add(border1.CenterString(printerLineWidth));
            if (invoice.Vehicle == null)
            {
                //invoiceLines.Add(("ΠΕΛΑΤΗΣ ΛΙΑΝΙΚΗΣ").CenterString(printerLineWidth));
            }
            else
            {
                string traderdata1 = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", invoice.Trader.TaxRegistrationNumber, invoice.Trader.TaxRegistrationOffice).EquilizeString(printerLineWidth);
                string traderdata2 = string.Format("Πελάτης:{0}", invoice.Trader.Name);
                string traderdata3 = string.Format("Οχημα  :{0}", invoice.Vehicle.PlateNumber);

                invoiceLines.Add(traderdata2);
                invoiceLines.Add(traderdata3);
                invoiceLines.Add(traderdata1.CenterString(printerLineWidth));
                invoiceLines.Add(border.CenterString(printerLineWidth));
            }
            //invoiceLines.Add(border1.CenterString(printerLineWidth));

            //string infoData = "Προς Πώληση#Μετρητοίς".EquilizeString(printerLineWidth);
            //invoiceLines.Add(infoData.CenterString(printerLineWidth));
            int eidosW = (int)(((decimal)18 / (decimal)44) * (decimal)printerLineWidth);
            int posotW = (int)(((decimal)7 / (decimal)44) * (decimal)printerLineWidth);
            int timiW = (int)(((decimal)6 / (decimal)44) * (decimal)printerLineWidth);
            int axiaW = (int)(((decimal)7 / (decimal)44) * (decimal)printerLineWidth);
            int fpaW = (int)(((decimal)6 / (decimal)44) * (decimal)printerLineWidth);
            string headerData = ("ΕΙΔΟΣ").CenterString(eidosW) + "ΠΟΣΟΤ.".CenterString(posotW) + "ΤΙΜΗ".CenterString(timiW) + "ΑΞΙΑ".CenterString(axiaW) + 
                "Φ.Π.Α.".CenterString(fpaW);
            invoiceLines.Add(headerData.CenterString(printerLineWidth));

            foreach(Data.InvoiceLine invLine in invoice.InvoiceLines)
            {
                invoiceLines.Add(border1.CenterString(printerLineWidth));

                string details = invLine.FuelType.Name.LeftString(eidosW) + invLine.Volume.ToString("N2").CenterString(posotW) + invLine.UnitPrice.ToString("N3").CenterString(timiW) +
                    (invLine.PreDiscountTotal - invLine.PreDiscountVAT).ToString("N2").CenterString(axiaW) +
                    (invLine.VatPercentage.ToString("N2") + "%").CenterString(fpaW);

                //string details = invLine.FuelType.Name.LeftString(18) + invLine.Volume.ToString("N2").CenterString(7) + invLine.UnitPrice.ToString("N3").CenterString(6) + invLine.TotalPrice.ToString("N2").CenterString(7) + invLine.VatAmount.ToString("N2").CenterString(6);
                invoiceLines.Add(details.CenterString(printerLineWidth));

                string salePoint = string.Format("Αντλία :{0} Ακροσωλήνιο :{1}", invLine.SalesTransaction.Nozzle.Dispenser.OfficialPumpNumber, invLine.SalesTransaction.Nozzle.OfficialNozzleNumber);
                invoiceLines.Add(salePoint);

            }

            //string quantitySum     = string.Format("          Σύνολο Ποσοτήτων  :#{0:N2}", invoice.InvoiceLines.Sum(il => il.Volume)).EquilizeString(printerLineWidth);
            //string amountNoDisSum  = string.Format("          Σύνολο προ Έκτωσης:#{0:N2}", invoice.TotalAmount + invoice.DiscountAmount.Value).EquilizeString(printerLineWidth);
            //string discountSum = string.Format("              Σύνολο Εκπτωσης   :#{0:N2}", (invoice.DiscountAmount.HasValue ? invoice.DiscountAmount : 0)).EquilizeString(printerLineWidth);

            decimal discAmount = invoice.DiscountAmount;

            invoiceLines.Add(border.CenterString(printerLineWidth));
            decimal sum = invoice.InvoiceLines.Sum(i => i.TotalPrice);
            decimal vatRatio = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 24);
            vatRatio = sum != 0 ? invoice.InvoiceLines.Sum(i => i.VatPercentage * i.TotalPrice) / sum : vatRatio;
            decimal totalNoVat = (invoice.TotalAmount.Value + discAmount) / ((decimal)1 + (vatRatio / 100));
            

            string quantitySum =    string.Format("          Σύνολο Ποσοτήτων  :#{0:N2}", invoice.InvoiceLines.Sum(il => il.Volume)).EquilizeString(printerLineWidth);
            string amountNoDisSum = string.Format("          Σύνολο προ Έκτωσης:#{0:N2}", totalNoVat).EquilizeString(printerLineWidth);
            string discountSum    = string.Format("          Σύνολο Εκπτωσης   :#{0:N2}", invoice.DiscountNettoAmount).EquilizeString(printerLineWidth);
            string vatSum         = string.Format("          Σύνολο Φ.Π.Α.     :#{0:N2}", invoice.VatAmount).EquilizeString(printerLineWidth);
            string amountSum      = string.Format("          Σύνολο Απόδειξης  :#{0:N2}", invoice.TotalAmount).EquilizeString(printerLineWidth);
            invoiceLines.Add(" ");
            invoiceLines.Add(quantitySum.RightString(printerLineWidth));
            invoiceLines.Add(amountNoDisSum.RightString(printerLineWidth));
            invoiceLines.Add(discountSum.RightString(printerLineWidth));
            invoiceLines.Add(vatSum.RightString(printerLineWidth));
            invoiceLines.Add(amountSum.RightString(printerLineWidth));

            //string quantitySum = string.Format("          Σύνολο Ποσοτήτων:#{0:N2}", invoice.InvoiceLines.Sum(il => il.Volume)).EquilizeString(printerLineWidth);
            //string discountSum = string.Format("          Σύνολο Εκπτωσης :#{0:N2}", invoice.DiscountAmount).EquilizeString(printerLineWidth);
            //string amountSum = string.Format(  "          Σύνολο Απόδειξης:#{0:N2}", invoice.TotalAmount).EquilizeString(printerLineWidth);
            //invoiceLines.Add(" ");
            ////string info2String = string.Format("Οι τιμές περιλαμβάνουν Φ.Π.Α.", invoice.TotalAmount);
            //invoiceLines.Add(quantitySum.RightString(printerLineWidth));
            //invoiceLines.Add(discountSum.RightString(printerLineWidth));
            //invoiceLines.Add(amountSum.RightString(printerLineWidth));
            //invoiceLines.Add(" ");
            //invoiceLines.Add(info2String.CenterString(printerLineWidth));
            //invoiceLines.Add("");
            //string info3String = string.Format("ΕΥΧΑΡΙΣΤΟΥΜΕ ΓΙΑ ΤΗΝ ΠΡΟΤΙΜΗΣΗΣ ΣΑΣ");
            //invoiceLines.Add(info3String.CenterString(printerLineWidth));


            if (invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
            {
                invoiceLines.Add("----------------ΑΠΑΛΛΑΓΗ ΦΠΑ----------------");
                string str = invoice.Trader.VatExemptionReason;
                if (str.Length > printerLineWidth)
                {
                    string str1 = str.Substring(0, printerLineWidth);
                    string str2 = str.Substring(printerLineWidth);
                    invoiceLines.Add(str1);
                    invoiceLines.Add(str2);
                }
                else
                    invoiceLines.Add(str);
            }
            else
            {
                string info2String = string.Format("Οι τιμές περιλαμβάνουν Φ.Π.Α.", invoice.TotalAmount);
                invoiceLines.Add(info2String.CenterString(printerLineWidth));
            }
            invoiceLines.Add("");
            string info3String = string.Format("ΕΥΧΑΡΙΣΤΟΥΜΕ ΓΙΑ ΤΗΝ ΠΡΟΤΙΜΗΣΗΣ ΣΑΣ");
            invoiceLines.Add(info3String.CenterString(printerLineWidth));

            if (invoice.ParentInvoiceRelations.Count > 0)
            {
                if (invoice.ParentInvoiceRelations[0].RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel)
                {
                    invoiceLines.Add("--------------------------------------------");
                    invoiceLines.Add("ΑΚΥΡΩΤΙΚΟ ΤΟΥ : " + invoice.ParentInvoiceRelations[0].ParentInvoice.Description);
                    invoiceLines.Add("                                            ");
                }
                else if (invoice.ParentInvoiceRelations[0].RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace)
                {
                    invoiceLines.Add("--------------------------------------------");
                    invoiceLines.Add("ΣΕ ΑΝΤΙΚΑΤΑΣΤΑΣΗ ΤΟΥ : " + invoice.ParentInvoiceRelations[0].ParentInvoice.Description);
                    invoiceLines.Add("                                            ");
                }
            }

            for(int i = 0; i < invoiceLines.Count; i++)
            {
                string str = invoiceLines[i];
                if(str.Contains("#"))
                    invoiceLines[i] = str.EquilizeString(printerLineWidth);
            }

            return invoiceLines.ToArray();
        }

        #endregion

        #region Help Methods

        private static void SetPrinter(System.Drawing.Printing.PrinterSettings printerSettings, Data.Invoice invoice)
        {
            if (invoice.Printer == null || invoice.Printer == "")
            {
                var invLine = invoice.InvoiceLines.FirstOrDefault();
                if (invLine != null)
                {
                    var st = invLine.SalesTransaction;
                    if (st != null)
                    {
                        var pr = st.Nozzle.Dispenser.InvoicePrints.Where(i => i.Printer != null && i.Printer != "").FirstOrDefault();
                        if (pr != null)
                            invoice.Printer = pr.Printer;
                    }
                }
                if (invoice.Printer == null || invoice.Printer == "")
                {
                    invoice.Printer = invoice.InvoiceType.Printer;
                }
                //.SalesTransaction.Nozzle
            }
            if (invoice.Printer != null && invoice.Printer != "")
                printerSettings.PrinterName = invoice.Printer;
        }

        private string CreateSignLine(Data.Invoice invoice)
        {
            string str = this.invoiceSignLine;
            str = str.Replace("[AFM]", Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN"));

            //[AdditionalInfo];
            //[ExemptionArticle];
            //[WithholdingSum];

            if (invoice == null || invoice.InvoiceLines.Count == 0)
            {
                string series = "";
                if (invoice != null)
                    series = string.IsNullOrEmpty(invoice.Series) ? "" : invoice.Series;
                str = str.Replace("[CustomerAfm]", "");
                str = str.Replace("[AdditionalInfo]", "");
                str = str.Replace("[Date]", DateTime.Now.ToString("ddMMyyyyHHmm"));
                str = str.Replace("[Description]", "501");
                str = str.Replace("[Series]", series);
                str = str.Replace("[InvoiceNumber]", "0");
                str = str.Replace("[AmountA]", "0.00");
                str = str.Replace("[AmountB]", "0.00");
                str = str.Replace("[AmountC]", "0.00");
                str = str.Replace("[AmountD]", "0.00");
                str = str.Replace("[AmountE]", "0.00");
                str = str.Replace("[TaxA]", "0.00");
                str = str.Replace("[TaxB]", "0.00");
                str = str.Replace("[TaxC]", "0.00");
                str = str.Replace("[TaxD]", "0.00");
                str = str.Replace("[SUM]", "0.00");
                str = str.Replace("[Currancy]", "1");
                str = str.Replace("[ExemptionArticle]", "");
                str = str.Replace("[WithholdingSum]", "");

                return str;
            }

            if(invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
            {
                str = str.Replace("[ExemptionArticle]", "#27");
            }
            str = str.Replace("[ExemptionArticle]", "");

            if (invoice.IsCanceling)
            {
                var rel = invoice.ParentInvoiceRelations.FirstOrDefault();
                if(rel != null)
                {
                    var parInvoice = rel.ParentInvoice;
                    string relInvCode = rel.ParentInvoice.InvoiceType.OfficialEnumerator.ToString();
                    string strReplace = string.Format("[Series]#{0}#{1}#{2}", relInvCode, parInvoice.Number, parInvoice.Series);
                    str = str.Replace("[Series]", strReplace);
                }
            }
            var volSum = invoice.InvoiceLines.Sum(i => i.Volume);
            var invLine = invoice.InvoiceLines.First();
            string additionalInfo = string.Format("?{0}!{1:N2}", invLine.FuelType.EnumeratorValue, volSum).Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "").Replace(",", ".");

            if(invoice.Trader != null)
                str = str.Replace("[CustomerAfm]", invoice.Trader.TaxRegistrationNumber);
            else
                str = str.Replace("[CustomerAfm]", "");
            str = str.Replace("[AdditionalInfo]", additionalInfo);
            str = str.Replace("[Date]", invoice.TransactionDate.ToString("ddMMyyyyHHmm"));

            string code = invoice.InvoiceType.OfficialEnumerator.ToString();
            if (invoice.InvoiceType.OfficialEnumerator == this.invoiceReplaceCodeFrom && invoice.TraderId.HasValue &&
                invoice.Trader.TaxRegistrationNumber != null && invoice.Trader.TaxRegistrationNumber != "" &&
                invLine.FuelType.EnumeratorValue != (int)FuelTypeEnum.DieselHeat &&
                invLine.FuelType.EnumeratorValue != (int)FuelTypeEnum.DieselHeatPremium)
            {
                code = this.invoiceReplaceCodeTo.ToString();
            }
            str = str.Replace("[Description]", code);
            str = str.Replace("[Series]", string.IsNullOrEmpty(invoice.Series) ? "" : invoice.Series);
            str = str.Replace("[InvoiceNumber]", invoice.Number.ToString());
            str = str.Replace("[AmountA]", "0.00");
            str = str.Replace("[AmountB]", "0.00");

            decimal netAmount = (invoice.TotalAmount.HasValue ? invoice.TotalAmount.Value : 0) - (invoice.VatAmount.HasValue ? invoice.VatAmount.Value : 0);

            str = str.Replace("[AmountC]", invoice.InvoiceType.ShowFinancialDataEx ? netAmount.ToString("N2").Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "").
                Replace(",", ".") : (0).ToString("N2").Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "").Replace(",", "."));
            str = str.Replace("[AmountD]", "0.00");
            str = str.Replace("[AmountE]", "0.00");
            str = str.Replace("[TaxA]", "0.00");
            str = str.Replace("[TaxB]", "0.00");
            str = str.Replace("[TaxC]", invoice.InvoiceType.ShowFinancialDataEx ? invoice.VatAmount.Value.ToString("N2").Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "").Replace(",", ".") :
                (0).ToString("N2").Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "").Replace(",", "."));
            str = str.Replace("[TaxD]", "0.00");
            str = str.Replace("[SUM]", invoice.InvoiceType.ShowFinancialDataEx ? invoice.TotalAmount.Value.ToString("N2").Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "").Replace(",", ".") :
                (0).ToString("N2").Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, "").Replace(",", "."));
            str = str.Replace("[Currancy]", "1");
            str = str.Replace("[WithholdingSum]", "");
            

            return str;
        }

        /// <summary>
        /// Creates the text is printed on the termal printer for each sale.
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        private string[] CreateInvoiceText(Data.DatabaseModel db, Data.Invoice invoice)
        {
            
            bool microFont = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrinterMicroFont", false);
            int printerLineWidth = microFont ? 60 : 44;

            string address = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            string city = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            string fax = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
            string name = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            string occupation = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
            string phone = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
            string postalCode = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode");
            string taxOffice = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
            string tin = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            string efk = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
            string invoiceType = invoice.InvoiceType.Description;
            List<string> invoiceLines = new List<string>();
            invoiceLines.Add(name.CenterString(printerLineWidth));
            invoiceLines.Add(occupation.CenterString(printerLineWidth));
            invoiceLines.Add(address.CenterString(printerLineWidth));
            string taxData = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", tin, taxOffice).EquilizeString(printerLineWidth);
            string efkData = string.Format("ΑΦΚ / Αρ. Άδειας:{0}", efk);
            string contactData = string.Format("ΤΗΛ:{0}#FAX:{1}", phone, fax).EquilizeString(printerLineWidth);
            string dateInfo = string.Format("Ημερ/νία:{0}#Ώρα:{1}", invoice.TransactionDate.ToString("dd/MM/yyyy"), invoice.TransactionDate.ToString("HH:mm")).EquilizeString(printerLineWidth);
            string supplyNumber = string.Format("Κωδ. Hλ. Πληρ. ΔΕΗ: {0}", invoice.SupplyNumber);
            string border1 = new string('-', printerLineWidth);
            string border = new string('=', printerLineWidth);

            if (invoiceType.Length > 36)
                invoiceType = invoiceType.Substring(0, 36);
            string series = invoice.Series == null ? "" : invoice.Series;
            string invoiceData = string.Format("{0}#{2}{1}", invoiceType, invoice.Number, series).EquilizeString(printerLineWidth);
            invoiceLines.Add(taxData.CenterString(printerLineWidth));
            if(efk != null && efk != "")
                invoiceLines.Add(efkData.CenterString(printerLineWidth));
            invoiceLines.Add(contactData.CenterString(printerLineWidth));
            invoiceLines.Add("");
            invoiceLines.Add(invoiceData.CenterString(printerLineWidth));
            invoiceLines.Add(dateInfo.CenterString(printerLineWidth));
            
            invoiceLines.Add(border1.CenterString(printerLineWidth));
            if(invoice.Vehicle == null)
            {
                //invoiceLines.Add(("ΠΕΛΑΤΗΣ ΛΙΑΝΙΚΗΣ").CenterString(printerLineWidth));
            }
            else
            {
                string traderdata1 = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", invoice.Trader.TaxRegistrationNumber, invoice.Trader.TaxRegistrationOffice).EquilizeString(printerLineWidth);
                string traderdata2 = string.Format("Πελάτης:{0}", invoice.Trader.Name);
                string traderdata3 = string.Format("Οχημα  :{0}", invoice.Vehicle.PlateNumber);

                invoiceLines.Add(traderdata2);
                bool supplyNumberFound = false;
                if(invoice.SupplyNumber != null && invoice.SupplyNumber != "")
                {
                    foreach(var line in invoice.InvoiceLines)
                    {
                        if (line.FuelType == null)
                            continue;
                        if (!line.FuelType.SupportsSupplyNumber.HasValue || !line.FuelType.SupportsSupplyNumber.Value)
                            continue;
                        supplyNumberFound = true;
                        break;
                    }
                }
                if(supplyNumberFound)
                    invoiceLines.Add(supplyNumber);
                else
                    invoiceLines.Add(traderdata3);
                invoiceLines.Add(traderdata1.CenterString(printerLineWidth));
                invoiceLines.Add(border.CenterString(printerLineWidth));
            }
            //invoiceLines.Add(border1.CenterString(printerLineWidth));

            //string infoData = "Προς Πώληση#Μετρητοίς".EquilizeString(printerLineWidth);
            //invoiceLines.Add(infoData.CenterString(printerLineWidth));


            int eidosW = (int)(((decimal)18 / (decimal)44) * (decimal)printerLineWidth);
            int posotW = (int)(((decimal)7 / (decimal)44) * (decimal)printerLineWidth);
            int timiW = (int)(((decimal)6 / (decimal)44) * (decimal)printerLineWidth);
            int axiaW = (int)(((decimal)7 / (decimal)44) * (decimal)printerLineWidth);
            int fpaW = (int)(((decimal)6 / (decimal)44) * (decimal)printerLineWidth);
            string headerData = ("ΕΙΔΟΣ").CenterString(eidosW) + "ΠΟΣΟΤ.".CenterString(posotW) + "ΤΙΜΗ".CenterString(timiW) + "ΑΞΙΑ".CenterString(axiaW) +
                "Φ.Π.Α.".CenterString(fpaW);

            invoiceLines.Add(headerData.CenterString(printerLineWidth));

            decimal vat = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 24);

            foreach (Data.InvoiceLine invLine in invoice.InvoiceLines)
            {
                invoiceLines.Add(border1.CenterString(printerLineWidth));

                if (invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
                {
                    decimal up = invLine.UnitPrice / ((100 + vat) / 100);

                    string details = invLine.FuelType.Name.LeftString(eidosW) + invLine.Volume.ToString("N2").CenterString(posotW) + up.ToString("N3").CenterString(timiW) +
                    (invLine.PreDiscountTotal - invLine.PreDiscountVAT).ToString("N2").CenterString(axiaW) +
                    (invLine.VatPercentage.ToString("N2") + "%").CenterString(fpaW);
                    invoiceLines.Add(details.CenterString(printerLineWidth));
                }
                else
                {
                    string details = invLine.FuelType.Name.LeftString(eidosW) + invLine.Volume.ToString("N2").CenterString(posotW) + invLine.UnitPrice.ToString("N3").CenterString(timiW) +
                    (invLine.PreDiscountTotal - invLine.PreDiscountVAT).ToString("N2").CenterString(axiaW) +
                    (invLine.VatPercentage.ToString("N2") + "%").CenterString(fpaW);
                    invoiceLines.Add(details.CenterString(printerLineWidth));
                }


                if (invLine.SalesTransaction != null)
                {
                    string salePoint = string.Format("Αντλία :{0} Ακροσωλήνιο :{1}", invLine.SalesTransaction.Nozzle.Dispenser.OfficialPumpNumber, invLine.SalesTransaction.Nozzle.OfficialNozzleNumber);
                    invoiceLines.Add(salePoint);
                }
            }

            decimal discAmount = invoice.DiscountAmount;
            decimal sum = invoice.InvoiceLines.Sum(i => i.TotalPrice);
            decimal vatRatio = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 24);

            vatRatio = sum != 0 ? invoice.InvoiceLines.Sum(i => i.VatPercentage * i.TotalPrice) / sum : vatRatio;
            decimal totalNoVat = (invoice.TotalAmount.Value + discAmount) / ((decimal)1 + (vatRatio / 100));

            invoiceLines.Add(border.CenterString(printerLineWidth));
            string quantitySum      = string.Format("          Σύνολο Ποσοτήτων  :#{0:N2}", invoice.InvoiceLines.Sum(il => il.Volume)).EquilizeString(printerLineWidth);
            string amountNoDisSum   = string.Format("          Σύνολο προ Έκτωσης:#{0:N2}", totalNoVat).EquilizeString(printerLineWidth);
            string discountSum      = string.Format("          Σύνολο Εκπτωσης   :#{0:N2}", invoice.DiscountNettoAmount).EquilizeString(printerLineWidth);
            string vatSum           = string.Format("          Σύνολο Φ.Π.Α.     :#{0:N2}", invoice.InvoiceLines.Sum(i => i.VatAmount)).EquilizeString(printerLineWidth);
            string amountSum        = string.Format("          Σύνολο Απόδειξης  :#{0:N2}", invoice.TotalAmount).EquilizeString(printerLineWidth);

            //invoiceLines.Add(border.CenterString(printerLineWidth));
            //string quantitySum    = string.Format("          Σύνολο Ποσοτήτων  :#{0:N2}", invoice.InvoiceLines.Sum(il => il.Volume)).EquilizeString(printerLineWidth);
            //string amountNoDisSum = string.Format("          Σύνολο προ Έκτωσης:#{0:N2}", invoice.TotalAmount + discAmount).EquilizeString(printerLineWidth);
            //string discountSum =    string.Format("          Σύνολο Εκπτωσης   :#{0:N2}", discAmount).EquilizeString(printerLineWidth);
            //string vatSum         = string.Format("          Σύνολο Φ.Π.Α.     :#{0:N2}", invoice.InvoiceLines.Sum(i=>i.VatAmount)).EquilizeString(printerLineWidth);
            //string amountSum      = string.Format("          Σύνολο Απόδειξης  :#{0:N2}", invoice.TotalAmount).EquilizeString(printerLineWidth);
            invoiceLines.Add(" ");
            invoiceLines.Add(quantitySum.RightString(printerLineWidth));
            invoiceLines.Add(amountNoDisSum.RightString(printerLineWidth));
            invoiceLines.Add(discountSum.RightString(printerLineWidth));
            invoiceLines.Add(vatSum.RightString(printerLineWidth));
            invoiceLines.Add(amountSum.RightString(printerLineWidth));
            invoiceLines.Add(" ");

            if (invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
            {
                invoiceLines.Add("----------------ΑΠΑΛΛΑΓΗ ΦΠΑ----------------");
                string str = invoice.Trader.VatExemptionReason == null ? "" : invoice.Trader.VatExemptionReason;
                if (str.Length > printerLineWidth)
                {
                    string str1 = str.Substring(0, 44);
                    string str2 = str.Substring(44);
                    invoiceLines.Add(str1);
                    invoiceLines.Add(str2);
                }
                else
                    invoiceLines.Add(str);
            }
            else
            {
                string info2String = string.Format("Οι τιμές περιλαμβάνουν Φ.Π.Α.", invoice.TotalAmount);
                invoiceLines.Add(info2String.CenterString(printerLineWidth));
            }
            invoiceLines.Add("");
            string info3String = string.Format("ΕΥΧΑΡΙΣΤΟΥΜΕ ΓΙΑ ΤΗΝ ΠΡΟΤΙΜΗΣΗΣ ΣΑΣ");
            invoiceLines.Add(info3String.CenterString(printerLineWidth));

            if (invoice.ParentInvoiceRelations.Count > 0)
            {
                if (invoice.ParentInvoiceRelations[0].RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel)
                {
                    invoiceLines.Add("--------------------------------------------");
                    invoiceLines.Add("ΑΚΥΡΩΤΙΚΟ ΤΟΥ : " + invoice.ParentInvoiceRelations[0].ParentInvoice.Description);
                    invoiceLines.Add("                                            ");
                }
                else if (invoice.ParentInvoiceRelations[0].RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace)
                {
                    invoiceLines.Add("--------------------------------------------");
                    invoiceLines.Add("ΣΕ ΑΝΤΙΚΑΤΑΣΤΑΣΗ ΤΟΥ : " + invoice.ParentInvoiceRelations[0].ParentInvoice.Description);
                    invoiceLines.Add("                                            ");
                }
            }
            
            //if (invoice.Notes != null && invoice.Notes != "")
            //{
            //    invoiceLines.Add("--------------------------------------------");
            //    invoiceLines.Add(invoice.Notes);
            //    invoiceLines.Add("                                            ");
            //}

            for(int i = 0; i < invoiceLines.Count; i++)
            {
                string str = invoiceLines[i];
                if(str.Contains("#"))
                    invoiceLines[i] = str.EquilizeString(printerLineWidth);
            }
            return invoiceLines.ToArray();
        }

        private string[] CreateFillingText(Data.TankFilling tankFilling)
        {
            List<string> lines = new List<string>();
            if(tankFilling.LevelEnd > tankFilling.LevelStart)
                lines.Add("ΠΑΡΑΛΑΒΗ ΚΑΥΣΙΜΟΥ");
            else
                lines.Add("ΕΞΑΓΩΓΗ ΚΑΥΣΙΜΟΥ");

            lines.Add(string.Format("Έναρξη : {0:dd/MM/yyyy HH:mm}", tankFilling.TransactionTime));
            lines.Add(string.Format("Λήξη : {0:dd/MM/yyyy HH:mm}", tankFilling.TransactionTimeEnd));

            lines.Add(string.Format("Δεξαμενή : {0} - {1}", tankFilling.Tank.TankNumber, tankFilling.Tank.TankSerialNumber));
            lines.Add(string.Format("Παραστατικό : {0}", tankFilling.InvoiceLines[0].Invoice.Description));
            lines.Add("ΤΙΜΕΣ ΕΝΑΡΞΗΣ");
            lines.Add(string.Format("Στάθμη Καυσίμου πρίν : {0}", tankFilling.LevelStart));
            lines.Add(string.Format("Θερμοκρασία πρίν : {0}", tankFilling.TankTemperatureStart));
            lines.Add(string.Format("Όγκος πρίν : {0}", tankFilling.Tank.GetTankVolume(tankFilling.LevelStart)));
            lines.Add("ΤΙΜΕΣ ΛΗΞΗΣ");
            lines.Add(string.Format("Στάθμη Καυσίμου πρίν : {0}", tankFilling.LevelEnd));
            lines.Add(string.Format("Θερμοκρασία μετά : {0}", tankFilling.TankTemperatureEnd));
            lines.Add(string.Format("Όγκος μετά : {0}", tankFilling.Tank.GetTankVolume(tankFilling.LevelEnd)));
            lines.Add(" ");
            lines.Add("ΟΓΚΟΙ");
            lines.Add(string.Format("Όγκος Παραστατικού : {0}", tankFilling.Volume));
            lines.Add(string.Format("Όγκος Παραστατικού 15oC : {0}", tankFilling.VolumeNormalized));
            lines.Add(string.Format("Όγκος Παραλαβής : {0}", tankFilling.VolumeReal));
            lines.Add(string.Format("Όγκος Παραλαβής 15oC : {0}", tankFilling.VolumeRealNormalized));
            return lines.ToArray();
        }

        private void PrintText(string text, string name)
        {
            string fileName = string.Format(this.signFolder + "\\{0}.txt", name);
            if(System.IO.File.Exists(fileName))
                return;
            if(this.defaultTaxDevice == "SignA")
            {
                System.IO.File.WriteAllText(fileName, text, Encoding.Default);
                return;
            }
            else if(this.defaultTaxDevice == "Samtec")
            {
                System.IO.File.WriteAllText(fileName, text, Encoding.Default);
                return;
            }
            return;
        }

        #endregion

        /// <summary>
        /// Main thread of the print agent
        /// </summary>
        private void ThreadRun()
        {
            oldInvoiceRunning = false;
            try
            {

                if(entriesProcessed.Count == 0)
                {
                    entriesProcessed.Add("Balances", new List<Guid>());
                    entriesProcessed.Add("Alerts", new List<Guid>());
                    entriesProcessed.Add("Titrimetries", new List<Guid>());
                }
                if(this.defaultTaxDevice.ToLower() == "samtec" && this.outFolder == "")
                {
                    return;
                }
                if(this.outFolder != "")
                {
                    System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.outFolder);
                    System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments");
                    System.IO.DirectoryInfo dirInfoAlerts = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments\\Alerts");
                    System.IO.DirectoryInfo dirInfoBalances = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments\\Balances");
                    System.IO.DirectoryInfo dirInfoTitrimetries = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments\\Titrimetries");

                    if(!dirInfo.Exists)
                        dirInfo.Create();
                    if(!dirInfoAlerts.Exists)
                        dirInfoAlerts.Create();
                    if(!dirInfoBalances.Exists)
                        dirInfoBalances.Create();
                    if(!dirInfoTitrimetries.Exists)
                        dirInfoTitrimetries.Create();
                }
            }
            catch(Exception ex)
            {
                Logger.Instance.LogToFile("Print Agent", ex);
                return;
            }
            int index = 0;
            while (agentRunning)
            {
                Guid[] myDataInvoices = null;
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    try
                    {

                        try
                        {
                            //this.CheckProcessFolder(db);

                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogToFile("CheckProcessFolder", ex);
                            System.Threading.Thread.Sleep(200);

                            //continue;
                        }

                        try
                        {

                            //this.CheckForSigns(db);

                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogToFile("CheckForSigns", ex);
                            System.Threading.Thread.Sleep(200);

                            //continue;
                        }

                        try
                        {
                            this.CheckAfterException(db);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogToFile("CheckAfterException", ex);
                            System.Threading.Thread.Sleep(200);
                            //continue;
                        }
                        index++;
                        var qBalances = db.Balances.Where(b => !b.PrintDate.HasValue);
                        var qInvoice = db.Invoices.Where(i =>
                            (
                                !i.IsPrinted.HasValue ||
                                i.IsPrinted.Value == false ||
                                i.InvoiceSignature == null ||
                                i.InvoiceSignature == ""
                            ) &&
                            (i.InvoiceType.Printable) &&
                            i.InvoiceLines.Count > 0
                        );
                        var dtAlert = DateTime.Now.AddMinutes(-2);
                        var qAlert = db.SystemEvents.Where(s => 
                            !s.PrintedDate.HasValue && 
                            s.EventDate > new DateTime(2019, 6, 29) && 
                            s.EventDate <= dtAlert &&
                            !s.ResolvedDate.HasValue);
                        var qTitration = db.Titrimetries.Where(t => !t.PrintDate.HasValue);
                        var qFillings = db.TankFillings.Where(t => (t.SignSignature == null || t.SignSignature == "") && t.InvoiceLines.Count > 0);

                        //this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qInvoice);
                        List<Data.Invoice> invoices = qInvoice.Where(ii=>!ExcludeInvoices.Contains(ii.InvoiceId)).ToList();

                        List<Data.Balance> balances = new List<Data.Balance>();
                        List<Data.SystemEvent> alerts = new List<Data.SystemEvent>();
                        List<Data.Titrimetry> titriemetries = new List<Data.Titrimetry>();
                        List<Data.TankFilling> tankFillings = new List<Data.TankFilling>();


                        foreach (Data.Invoice invoice in invoices)
                        {
                            try
                            {
                                lock (this.invoicesProcessed)
                                {
                                    if (this.invoicesProcessed.Contains(invoice.InvoiceId.ToString()))
                                        continue;
                                    this.invoicesProcessed.Add(invoice.InvoiceId.ToString());
                                }
                                if (invoice.Number == 0)
                                {
                                    invoice.Number = invoice.InvoiceType.LastNumber + 1;
                                    invoice.Series = invoice.InvoiceType.DefaultSeries == null ? "" : invoice.InvoiceType.DefaultSeries;
                                    invoice.InvoiceType.LastNumber = invoice.Number;
                                    db.SaveChanges();
                                }


                                if (invoice.InvoiceType.IsCancelation.HasValue && invoice.InvoiceType.IsCancelation.Value)
                                {
                                    var qit = invoice.ParentInvoiceRelations.Select(i => i.ParentInvoice.InvoiceType).Distinct();
                                    if (qit.Count() > 0)
                                    {
                                        foreach (var it in qit)
                                        {
                                            if (it.IsLaserPrint.HasValue && it.IsLaserPrint.Value)
                                                this.PrintInternalInvoice(db, invoice);
                                            else
                                                this.PrintInvoice(db, invoice);
                                            break;
                                        }
                                        continue;
                                    }
                                }

                                if (invoice.InvoiceType.IsLaserPrint.HasValue && invoice.InvoiceType.IsLaserPrint.Value && invoice.InvoiceType.IsInternal.HasValue)
                                    this.PrintInternalInvoice(db, invoice);
                                else
                                    this.PrintInvoice(db, invoice);

                                //if(invoice.InvoiceTypeId != this.litercheckInvoiceType && invoice.InvoiceType.IsInternal.HasValue && invoice.InvoiceType.IsInternal.Value && invoice.InvoiceType.Printable)
                                //{
                                //    this.PrintInternalInvoice(invoice);
                                //}
                                //else
                                //    this.PrintInvoice(invoice);
                            }
                            catch (Exception ex)
                            {
                                Logging.Logger.Instance.LogToFile("Σφάλμα Εκτύπωσης Παραστατικού", ex);
                            }
                        }

                        if (index % 10 == 0)
                        {

                            //this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qBalances);
                            //this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qAlert);
                            //this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qTitration);
                            //this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qFillings);
                            balances = qBalances.ToList();
                            alerts = qAlert.Take(100).ToList();
                            titriemetries = qTitration.ToList();
                            tankFillings = qFillings.ToList();

                            foreach (Data.Balance balance in balances)
                            {
                                try
                                {

                                    this.PrintBalance(db, balance);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Logger.Instance.LogToFile("Σφάλμα Εκτύπωσης Ισοζυγίου", ex);
                                }
                            }

                            foreach (Data.SystemEvent alert in alerts)
                            {
                                try
                                {
                                    this.PrintAlert(db, alert);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Logger.Instance.LogToFile("Σφάλμα Εκτύπωσης Συναγερμού", ex);
                                }
                            }


                            foreach (Data.Titrimetry titrimetry in titriemetries)
                            {
                                try
                                {
                                    this.PrintTitrimetry(db, titrimetry);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Logger.Instance.LogToFile("Σφάλμα Εκτύπωσης Ογκομ. Πίνακα", ex);
                                }
                            }

                            foreach (Data.TankFilling tankFilling in tankFillings)
                            {
                                try
                                {
                                    if (tankFilling.InvoiceLines.Count == 0)
                                        continue;
                                    this.PrintTankFilling(db, tankFilling);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Logger.Instance.LogToFile("Σφάλμα Εκτύπωσης Παραλαβής / Εξαγωγής", ex);
                                }
                            }

                            if (closeSalesDate.Date < DateTime.Now.Date)
                            {
                                CloseSales();
                            }
                            if (index % 100 == 0)
                            {
                                try
                                {
                                    System.GC.Collect();
                                }
                                catch
                                {
                                }
                                //this.database.Dispose();
                                //this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
                                //try
                                //{
                                //    System.GC.WaitForPendingFinalizers();
                                //    System.GC.Collect();
                                //    System.GC.WaitForPendingFinalizers();
                                //    System.GC.Collect();
                                //    System.GC.WaitForPendingFinalizers();
                                //    System.GC.Collect();
                                //}
                                //catch
                                //{
                                //}

                                index = 1;
                            }


                        }
                    }
                    catch (Exception ex2)
                    {
                        Logger.Instance.LogToFile("ThreadRun", ex2);
                    }
                   
                }
                //if (Exedron.MyData.Settings.IsActive)
                //{
                //    if (!oldInvoiceRunning)
                //    {
                //        thOldInvoices = new System.Threading.Thread(new System.Threading.ThreadStart(this.SendOldInvoiceThread));
                //        thOldInvoices.Start();
                //    }
                //    if (!resendInvoiceRunning)
                //    {
                //        thResendInvoices = new System.Threading.Thread(new System.Threading.ThreadStart(this.ResendFailedThread));
                //        thResendInvoices.Start();
                //    }
                //}
                System.Threading.Thread.Sleep(1000);
            }
        }

        #region Check Methods

        /// <summary>
        /// Processes all entries failed to process in previous cycles.
        /// </summary>
        private void CheckAfterException(Data.DatabaseModel db)
        {
            foreach(string key in this.entriesProcessed.Keys)
            {
                List<Guid> toRemove = new List<Guid>();
                foreach(Guid id in this.entriesProcessed[key])
                {
                    try
                    {
                        if(key == "Balances")
                        {
                            Data.Balance entry = db.Balances.Where(e => e.BalanceId == id).FirstOrDefault();
                            if(entry == null)
                                continue;
                            entry.PrintDate = DateTime.Now;
                            db.SaveChanges();
                        }
                        //else if(key == "Alerts")
                        //{
                        //    Data.SystemEvent entry = db.SystemEvents.Where(e => e.EventId == id).FirstOrDefault();
                        //    if(entry == null)
                        //        continue;
                        //    entry.PrintedDate = DateTime.Now;
                        //    db.SaveChanges();
                        //}
                        else if(key == "Titrimetries")
                        {
                            Data.Titrimetry entry = db.Titrimetries.Where(e => e.TitrimetryId == id).FirstOrDefault();
                            if(entry == null)
                                continue;
                            entry.PrintDate = DateTime.Now;
                            db.SaveChanges();
                        }
                        toRemove.Add(id);
                    }
                    catch
                    {
                    }
                }
                foreach(Guid id in toRemove)
                {
                    this.entriesProcessed[key].Remove(id);
                }
            }
            List<string> toRemoveInvs = new List<string>();
            foreach(string key in this.invoicesProcessed)
            {
                try
                {
                    string[] keys = key.Split(new string[] { "$$" }, StringSplitOptions.RemoveEmptyEntries);
                    if(keys.Length != 2)
                        continue;
                    Guid id = Guid.Parse(keys[0]);
                    Data.Invoice inv = db.Invoices.Where(i => i.InvoiceId == id).FirstOrDefault();
                    if(inv == null)
                        continue;
                    inv.IsPrinted = true;
                    inv.InvoiceSignature = keys[1];
                    db.SaveChanges();
                    toRemoveInvs.Add(key);
                }
                catch
                {
                }
            }
            foreach(string key in this.fillingsProcessed)
            {
                try
                {
                    string[] keys = key.Split(new string[] { "$$" }, StringSplitOptions.RemoveEmptyEntries);
                    if(keys.Length != 2)
                        continue;
                    Guid id = Guid.Parse(keys[0]);
                    Data.TankFilling inv = db.TankFillings.Where(i => i.TankFillingId == id).FirstOrDefault();
                    if(inv == null)
                        continue;

                    inv.SignSignature = keys[1];
                    db.SaveChanges();
                    toRemoveInvs.Add(key);
                }
                catch
                {
                }
            }
            foreach(string key in toRemoveInvs)
            {
                lock (this.invoiceProcessed)
                {
                    if (this.invoicesProcessed.Contains(key))
                        invoicesProcessed.Remove(key);
                }
                lock (this.fillingsProcessed)
                {
                    if (this.fillingsProcessed.Contains(key))
                        fillingsProcessed.Remove(key);
                }
            }
        }

        /// <summary>
        /// Checks if there are invoices signed from the Sign Device. All signs are evaluated
        /// </summary>
        private void CheckForSigns(Data.DatabaseModel db)
        {
            if(outFolder != null && outFolder != "")
            {
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.outFolder);
                if(!dir.Exists)
                    return;
                System.IO.FileInfo[] files = dir.GetFiles("*.out");
                if (files.Count() == 0)
                    this.signsApplied.Clear();

                foreach(System.IO.FileInfo file in files)
                {
                    try
                    {
                        if (file.Name == "CloseSales.out")
                        {
                            System.IO.File.Delete(file.FullName);
                            continue;
                        }
                        FileTypeInfo ft = this.GetFileTypeInfo(file.Name, "out");

                        string str = "";
                        
                        try
                        {
                            str = System.IO.File.ReadAllText(file.FullName);
                        }
                        catch(Exception ex)
                        {
                            continue;
                        }
                        string evalResp = this.EvaluateSign(db, file.FullName, ft.Id, ft.FileType);
                        if (evalResp == "Error" || evalResp == "")
                        {
                            if (this.filesInProcess.Contains(file.Name.Replace(".out", ".txt")))
                                this.filesInProcess.Remove(file.Name.Replace(".out", ".txt"));
                            if (this.invoiceProcessed.Contains(ft.Id))
                                this.invoiceProcessed.Remove(ft.Id);
                            if (this.invoicesProcessed.Contains(ft.Id.ToString()))
                                this.invoicesProcessed.Remove(ft.Id.ToString());
                            file.Delete();
                        }
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// Check if there are reports such as Balance report waiting to be signed.
        /// </summary>
        private void CheckProcessFolder(Data.DatabaseModel db)
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.processFolder);
            if(!dir.Exists)
                return;
            System.IO.FileInfo[] files = dir.GetFiles("*.xls");
            foreach (FileInfo file in files)
            {
                try
                {
                    FileTypeInfo ft = GetFileTypeInfo(file.Name, "xls");


                    string fileName = file.Name.Replace(".xls", ".txt");
                    if (this.filesInProcess.Contains(fileName))
                        continue;

                    string text = ExcelTextStripper.GetText(file.FullName, ft.ReportName);
                    
                    

                    if (this.defaultTaxDevice != "Samtec")
                    {
                        FileTypeInfo fType = this.GetFileTypeInfo(fileName, "txt");
                        
                        System.IO.File.WriteAllText(this.signFolder + "\\" + fileName, text, Encoding.Default);
                        
                        this.ApplySign(db, "Sign A", "", fType.Id, fType.FileType);
                    }
                    else
                    {
                        
                        FileTypeInfo fType = this.GetFileTypeInfo(fileName, "txt");
                        if (fType.FileType != "Invoice")
                            text = text + "\r\n" + this.CreateSignLine(null);
                        System.IO.File.WriteAllText(this.signFolder + "\\" + fileName, text, Encoding.Default);
                        filesInProcess.Add(file.Name.Replace(".xls", ".txt"));
                        
                    }

                    string appFolder = System.Environment.CurrentDirectory + "\\" + ft.FileType;
                    if (!Directory.Exists(appFolder))
                        Directory.CreateDirectory(appFolder);
                    if (File.Exists(appFolder + "\\" + file.Name))
                    {
                        File.Delete(appFolder + "\\" + file.Name);
                        System.Threading.Thread.Sleep(100);
                    }
                    File.Move(file.FullName, appFolder + "\\" + file.Name);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogToFile("CheckProcessFolder", ex);
                }
            }
            if(files.Length > 0)
                System.Threading.Thread.Sleep(1000);
        }

        /// <summary>
        /// Applies Sign to the given entry in the database
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="id"></param>
        /// <param name="fileType"></param>
        private bool ApplySign(Data.DatabaseModel db, string sign, string qrData, Guid id, string fileType)
        {
            if (sign != "-" && signsApplied.Contains(sign) && !sign.Contains("ΒΛΑΒΗ"))
                return true;
            if(sign != "-")
                signsApplied.Add(sign);
            try
            {
                switch (fileType)
                {
                    case "Balance":
                        Data.Balance balance = db.Balances.Where(b => b.BalanceId == id).FirstOrDefault();
                        if (balance != null)
                        {
                            balance.DocumentSign = sign;
                            balance.PrintDate = DateTime.Now;
                            db.SaveChanges();
                            this.PrintToPrinter(balance, true);
                            return true;
                        }
                        break;
                    case "Alert":
                        Data.SystemEvent alert = db.SystemEvents.Where(b => b.EventId == id).FirstOrDefault();
                        if (alert != null)
                        {
                            alert.DocumentSign = sign;
                            alert.PrintedDate = DateTime.Now;
                            db.SaveChanges();
                            this.PrintToPrinter(alert);
                            return true;
                        }
                        break;
                    case "TankFilling":
                        Data.TankFilling delicery = db.TankFillings.Where(b => b.TankFillingId == id).FirstOrDefault();
                        if (delicery != null)
                        {
                            delicery.SignSignature = sign;
                            db.SaveChanges();
                            this.PrintToPrinter(delicery);
                            return true;
                        }
                        break;
                    case "Titrimetry":
                        Data.Titrimetry titrimetry = db.Titrimetries.Where(b => b.TitrimetryId == id).FirstOrDefault();
                        if (titrimetry != null)
                        {
                            titrimetry.DocumentSign = sign;
                            titrimetry.PrintDate = DateTime.Now;
                            db.SaveChanges();
                            this.PrintToPrinter(titrimetry);
                            return true;
                        }
                        break;
                    case "Invoice":
                        Data.Invoice invoice = db.Invoices.Where(b => b.InvoiceId == id).FirstOrDefault();
                        if (invoice != null)
                        {
                            //if (invoice.Number == 0 && invoice.InvoiceType.Printable)
                            //{
                            //    invoice.Number = invoice.InvoiceType.LastNumber + 1;
                            //    invoice.InvoiceType.LastNumber = invoice.Number;
                            //}
                            invoice.InvoiceSignature = sign;
                            invoice.QRCodeData = qrData;
                            invoice.IsPrinted = true;
                            db.SaveChanges();

                            if(invoice.InvoiceType.ForcesDelivery.HasValue && invoice.InvoiceType.ForcesDelivery.Value)
                            {
                                foreach(var invLine in invoice.InvoiceLines)
                                {
                                    var tank = Program.ApplicationMainForm.ThreadControllerInstance.GetTanks().SingleOrDefault(t=>t.TankId == invLine.TankId.Value);
                                    if (tank == null)
                                        continue;
                                    //tank.FillingFuelTypeId = invLine.FuelTypeId;
                                    //tank.InvoiceTypeId = invoice.InvoiceTypeId;
                                    if (invoice.VehicleId.HasValue)
                                        tank.VehicleId = invoice.VehicleId.Value;
                                    if (invoice.InvoiceType.TransactionType == 0)
                                        tank.InitializeExtraction = true;
                                    else
                                        tank.InitializeFilling = true;
                                    tank.InvoiceTypeId = invoice.InvoiceTypeId;
                                    tank.InvoiceLineId = invLine.InvoiceLineId;
                                }
                            }

                            //Console.WriteLine("APPLY SIGN 1 | "  + invoice.InvoiceId.ToString())

                            bool isInternal = invoice.InvoiceType.IsInternal.HasValue ? invoice.InvoiceType.IsInternal.Value : true;

                            if (invoice.InvoiceType.IsLaserPrint.HasValue && invoice.InvoiceType.IsLaserPrint.Value)
                                this.PrintToPrinterInternalInvoice(invoice, isInternal);
                            else
                                this.PrintToPrinter(db, invoice);
                            return true;
                        }
                        
                        break;
                }
            }
            catch(Exception ex)
            {
                
            }
            return false;
        }

        /// <summary>
        /// Evaluates the sign given. If there is an error the entry with the specific id is sent to the Sign Device again
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="id"></param>
        /// <param name="fileType"></param>
        private string EvaluateSign(Data.DatabaseModel db, string fileName, Guid id, string fileType)
        {
            string text = File.ReadAllText(fileName, Encoding.GetEncoding(28597));
            if(text.Contains("Error"))
            {
                return "Error";
            }
            string sign = "";
            string qrData = "";
            if (text.Contains("ΔΦΣΣ"))
            {
                text = text.Replace("ΔΦΣΣ", "").Replace("[[", "").Replace("]]", "");
                string[] vals = text.Split(',');
                sign = vals[0];
                if (vals.Last().Contains("https://"))
                {
                    qrData = vals.Last();
                }
            }
            if(sign == "")
                return "";
            bool ret = ApplySign(db, sign, qrData, id, fileType);
            if (ret)
                return "";
            return "Error Apply";
        }

        /// <summary>
        /// Return the FileTypeInfo of the specific filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        private FileTypeInfo GetFileTypeInfo(string fileName, string extension)
        {
            string name = fileName.Replace("." + extension, "");
            string[] parms = name.Split('_');
            Guid id = Guid.Empty;
            string fileType = "Invoice";

            if(parms.Length == 1)
            {
                id = Guid.Parse(parms[0]);
            }
            else
            {
                fileType = parms[0];
                id = Guid.Parse(parms[1]);
            }
            FileTypeInfo ft = new FileTypeInfo()
            {
                FileType = fileType,
                Id = id
            };
            return ft;
        }

        #endregion

        #region Print Methods for Samtec

        private void PrintToPrinter(Data.SystemEvent alert)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            using (Reports.AlertReport report = new Reports.AlertReport())
            {
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "SystemEvents");
                report.ReportParameters[0].Value = alert.EventId.ToString().ToLower();

                if (this.printAlertsPhysical)
                {
                    System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                    PrintReport(report, printerSettings);
                }
                string alertDirectory = System.Environment.CurrentDirectory + "\\Συναγερμοί";
                if (!System.IO.Directory.Exists(alertDirectory))
                {
                    System.IO.Directory.CreateDirectory(alertDirectory);
                }
                string fileName = alertDirectory + "\\" + string.Format("Alert_{0:yyyyMMdd_HHmmssfff}.pdf", DateTime.Now);
                this.SaveReport(report, fileName);

                string compName = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                string taxNumber = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                
                MailSender.Instance.SendMail(new string[] { fileName }, "Συναγερμός " + compName + "(" + taxNumber + ")", "Συναγερμός " + compName + "(" + taxNumber + ")");

            }
            db.Dispose();
        }

        private void PrintToPrinter(Data.Titrimetry titrimetry)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            using (Reports.TitrimetryReport report = new Reports.TitrimetryReport())
            {
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db.TitrimetryLevels.Where(tl => tl.TitrimetryId == titrimetry.TitrimetryId), "");
                //report.ReportParameters[0].Value = titrimetry.TitrimetryId.ToString().ToLower();

                if (this.printPhysical)
                {
                    System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                    PrintReport(report, printerSettings);
                }

                string alertDirectory = System.Environment.CurrentDirectory + "\\ΟγκομετρικοίΠίνακες";
                if (!System.IO.Directory.Exists(alertDirectory))
                {
                    System.IO.Directory.CreateDirectory(alertDirectory);
                }
                string fileName = alertDirectory + "\\" + string.Format("Titrimetry_{0:yyyyMMdd_HHmmssfff}.pdf", DateTime.Now);
                this.SaveReport(report, fileName);
            }
            db.Dispose();
        }

        public void PrintToPrinter(Data.Balance balance, bool sendMail)
        {
            //Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.BalanceReports.BalanceLoad bl = new Reports.BalanceReports.BalanceLoad();
            bl.LoadBalance(balance.BalanceId, balance.BalanceText);
            bl.Model.Balance[0].Sign = balance.DocumentSign;
            using (Reports.BalanceReport report = new Reports.BalanceReport())
            {
                Reports.BalanceReports.TankBalanceReport subReport1 = new Reports.BalanceReports.TankBalanceReport();
                Reports.BalanceReports.PumpBalanceReport subReport2 = new Reports.BalanceReports.PumpBalanceReport();
                Reports.BalanceReports.DivergenceBalanceReport subReport3 = new Reports.BalanceReports.DivergenceBalanceReport();
                Reports.BalanceReports.TankFillingBalanceReport subReport4 = new Reports.BalanceReports.TankFillingBalanceReport();

                Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                ds1.DataSource = bl.Model;
                ds1.DataMember = "TankData";

                Telerik.Reporting.ObjectDataSource ds2 = new Telerik.Reporting.ObjectDataSource();
                ds2.DataSource = bl.Model;
                ds2.DataMember = "DispenserData";

                Telerik.Reporting.ObjectDataSource ds3 = new Telerik.Reporting.ObjectDataSource();
                ds3.DataSource = bl.Model;
                ds3.DataMember = "FuelTypeData";

                Telerik.Reporting.ObjectDataSource ds4 = new Telerik.Reporting.ObjectDataSource();
                ds4.DataSource = bl.Model;
                ds4.DataMember = "Balance";

                Telerik.Reporting.ObjectDataSource ds5 = new Telerik.Reporting.ObjectDataSource();
                ds5.DataSource = bl.Model;
                ds5.DataMember = "TankFillingData";


                subReport1.DataSource = ds1;
                subReport2.DataSource = ds2;
                subReport3.DataSource = ds3;
                subReport4.DataSource = ds5;
                report.DataSource = ds4;
                report.Name = "BalanceReport";
                report.DocumentName = "BalanceReport";

                Telerik.Reporting.InstanceReportSource subReportSource1 = new Telerik.Reporting.InstanceReportSource();
                subReportSource1.ReportDocument = subReport1;

                Telerik.Reporting.InstanceReportSource subReportSource2 = new Telerik.Reporting.InstanceReportSource();
                subReportSource2.ReportDocument = subReport2;

                Telerik.Reporting.InstanceReportSource subReportSource3 = new Telerik.Reporting.InstanceReportSource();
                subReportSource3.ReportDocument = subReport3;

                Telerik.Reporting.InstanceReportSource subReportSource4 = new Telerik.Reporting.InstanceReportSource();
                subReportSource4.ReportDocument = subReport4;

                //Telerik.Reporting.InstanceReportSource repSource = new Telerik.Reporting.InstanceReportSource();
                //repSource.ReportDocument = report;

                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport1"]).ReportSource = subReportSource1;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport2"]).ReportSource = subReportSource2;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport3"]).ReportSource = subReportSource3;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport4"]).ReportSource = subReportSource4;

                Telerik.Reporting.InstanceReportSource reportSource = new Telerik.Reporting.InstanceReportSource();
                reportSource.ReportDocument = report;

                if (this.printBalancesPhysical)
                {
                    System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                    PrintReport(report, printerSettings);
                }

                string balanceDirectory = System.Environment.CurrentDirectory + "\\Ισοζύγια";
                if (!System.IO.Directory.Exists(balanceDirectory))
                {
                    System.IO.Directory.CreateDirectory(balanceDirectory);
                }
                string fileName = balanceDirectory + "\\" + string.Format("Balance_{0:yyyyMMdd}.pdf", balance.StartDate.Date);
                if (balance.StartDate.Date != balance.EndDate.Date && balance.StartDate.Day == 1 && balance.EndDate.AddDays(1).Day == 1)
                    fileName = balanceDirectory + "\\" + string.Format("Monthly_Balance_{0:yyyyMMdd}_{1:yyyyMMdd}.pdf", balance.StartDate.Date, balance.EndDate.Date);
                if (balance.StartDate.Date != balance.EndDate.Date && balance.StartDate.Day != 1 && balance.EndDate.AddDays(1).Day != 1)
                    fileName = balanceDirectory + "\\" + string.Format("Custom_Balance_{0:yyyyMMdd}_{1:yyyyMMdd}.pdf", balance.StartDate.Date, balance.EndDate.Date);

                string compName = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                string taxNumber = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                if (compName == null || compName == "")
                    return;
                if (taxNumber == null || taxNumber == "")
                    return;
                if (System.IO.File.Exists(fileName))
                    System.IO.File.Delete(fileName);

                SaveReport(report, fileName);
                bool hasError = false;
                if (bl.Model.FuelTypeData.Where(f => Math.Abs(f.DiffPercentafeNormelized) > (decimal)1.5).Count() > 0)
                {
                    hasError = true;
                }
                string companyDesc = string.Format("Ισοζύγιο {0}, {1} από :{2:dd/MM/yyyy HH:mm} έως: {3:dd/MM/yyyy HH:mm}", compName, taxNumber, balance.StartDate, balance.EndDate);
                if (hasError)
                    companyDesc = companyDesc + " ΕΚΤΟΣ ΟΡΙΩΝ";
                //MailSender.Instance.SendSms(fileName);
                MailSender.Instance.SendMail(new string[] { fileName }, companyDesc, companyDesc);
                bool isCustom = false;
                string newFileName = "";
                if (hasError)
                {
                    if (!System.IO.Directory.Exists(balanceDirectory + "\\Εκτός Ορίων"))
                        System.IO.Directory.CreateDirectory(balanceDirectory + "\\Εκτός Ορίων");
                    newFileName = balanceDirectory + "\\Εκτός Ορίων\\" + string.Format("Balance_{0:yyyyMMdd}.pdf", balance.StartDate.Date);
                    if (balance.StartDate.Date != balance.EndDate.Date && balance.EndDate.Subtract(balance.StartDate).TotalMinutes > 1441)
                    {
                        if(balance.StartDate.Day == 1 && balance.EndDate.AddDays(1).Day == 1)
                            newFileName = balanceDirectory + "\\Εκτός Ορίων\\" + string.Format("Monthly_Balance_{0:yyyyMMdd}_{1:yyyyMMdd}.pdf", balance.StartDate.Date, balance.EndDate.Date);
                        else
                            newFileName = balanceDirectory + "\\Εκτός Ορίων\\" + string.Format("Custom_Balance_{0:yyyyMMdd}_{1:yyyyMMdd}.pdf", balance.StartDate.Date, balance.EndDate.Date);
                        isCustom = true;
                    }
                    if (System.IO.File.Exists(newFileName))
                        System.IO.File.Delete(newFileName);
                    System.IO.File.Move(fileName, newFileName);
                }
                else
                {
                    if (!System.IO.Directory.Exists(balanceDirectory + "\\Εντός Ορίων"))
                        System.IO.Directory.CreateDirectory(balanceDirectory + "\\Εντός Ορίων");
                    newFileName = balanceDirectory + "\\Εντός Ορίων\\" + string.Format("Balance_{0:yyyyMMdd}.pdf", balance.StartDate.Date);
                    if (balance.StartDate.Date != balance.EndDate.Date && balance.EndDate.Subtract(balance.StartDate).TotalMinutes > 1441)
                    {
                        if (balance.StartDate.Day == 1 && balance.EndDate.AddDays(1).Day == 1)
                            newFileName = balanceDirectory + "\\Εκτός Ορίων\\" + string.Format("Monthly_Balance_{0:yyyyMMdd}_{1:yyyyMMdd}.pdf", balance.StartDate.Date, balance.EndDate.Date);
                        else
                            newFileName = balanceDirectory + "\\Εντός Ορίων\\" + string.Format("Custom_Balance_{0:yyyyMMdd}_{1:yyyyMMdd}.pdf", balance.StartDate.Date, balance.EndDate.Date);
                        isCustom = true;
                    }
                    if (System.IO.File.Exists(newFileName))
                        System.IO.File.Delete(newFileName);
                    System.IO.File.Move(fileName, newFileName);

                }
                if (isCustom)
                {
                    System.Diagnostics.Process.Start(newFileName);
                }

                try
                {
                    subReport1.Dispose();
                    subReport2.Dispose();
                    subReport3.Dispose();
                    subReport4.Dispose();
                }
                catch
                {
                }
            }
        }

        void SaveReport(Telerik.Reporting.Report report, string fileName)
        {
            Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            Telerik.Reporting.InstanceReportSource instanceReportSource = new Telerik.Reporting.InstanceReportSource();
            instanceReportSource.ReportDocument = report;
            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", instanceReportSource, null);

            using(FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }
        }

        private void PrintToPrinter(Data.DatabaseModel db, Data.Invoice invoice)
        {
            if (invoicingProviderEnabled == false && invoice.InvoiceType.SendToMyData)
            {
                if (!MyDataSendInvoice(invoice.InvoiceId, 2))
                    return;
            }
            else if (invoicingProviderEnabled)
            {

            }
            var myDataInvoice = db.MyDataInvoices.FirstOrDefault(m => m.InvoiceId == invoice.InvoiceId);
            var myDataQrCode = "";
            if (myDataInvoice != null && myDataInvoice.Errors != null)
            {
                var parms = myDataInvoice.Errors.Split('|');
                if (parms.Length > 1)
                    myDataQrCode = parms[1];
            }
            //if(invoice.InvoiceType.TransactionType == 1
            if (windowsPrint)
            {
                try
                {
                    //if (this.thermalPaperWidth == 80)
                    //{
                    //    using (Reports.InvoiceThermal report = new Reports.InvoiceThermal())
                    //    {

                    //        report.SetPaperWidth(this.thermalPaperWidth);
                    //        report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoicePrintViews");
                    //        report.ReportParameters[0].Value = invoice.InvoiceId.ToString().ToLower();
                    //        report.ReportParameters[1].Value = this.printBarCode;
                    //        report.ReportParameters[2].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                    //        report.ReportParameters[3].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
                    //        report.ReportParameters[4].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                    //        report.ReportParameters[10].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyMainAddress");
                    //        report.ReportParameters[5].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                    //        report.ReportParameters[6].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                    //        report.ReportParameters[7].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
                    //        report.ReportParameters[8].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
                    //        report.ReportParameters[9].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
                    //        string efk = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
                    //        report.ReportParameters[11].Value = efk == null ? "" : efk;
                    //        report.SetSupplyNumber(invoice);
                    //        System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                    //        printerSettings.PrinterName = invoice.Printer;

                    //        PrintReport(report, printerSettings);
                    //    }
                    //}
                    //else
                    //{
                    using (Reports.Invoices.InvoiceReportNarrow report = new Reports.Invoices.InvoiceReportNarrow())
                    {
                        var fs = Data.Implementation.OptionHandler.Instance.GetDecimalOption("ThermalPrintFontSize", 9);
                        var fn = Data.Implementation.OptionHandler.Instance.GetOption("ThermalPrintFontName", "Bahnschrift Light Condensed");

                        report.SetFont(fn, (float)fs);

                        var invLines = db.InvoicePrintViews.Where(i => i.InvoiceId == invoice.InvoiceId);
                        foreach (var invLine in invLines)
                        {
                            invLine.InvoiceNettoAmount = invoice.NettoAmount.Value;
                            invLine.InvoiceTotalAmount = invoice.TotalAmount.Value;
                            invLine.InvoiceVatAmount = invoice.VatAmount.Value;
                            invLine.InvoiceDiscountAmount = invoice.DiscountAmount;
                            invLine.InvoiceNettoAfterDiscountAmount = invoice.NettoAfterDiscount;
                        }

                        report.DataSource = new Telerik.Reporting.OpenAccessDataSource(invLines, "");
                        report.ReportParameters[0].Value = invoice.InvoiceId.ToString().ToLower();
                        report.ReportParameters[1].Value = this.printBarCode;
                        report.ReportParameters[2].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                        report.ReportParameters[3].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
                        report.ReportParameters[4].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                        report.ReportParameters[10].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyMainAddress");
                        report.ReportParameters[5].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                        report.ReportParameters[6].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                        report.ReportParameters[7].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
                        report.ReportParameters[8].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
                        report.ReportParameters[9].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
                        report.ReportParameters[11].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
                        report.SetSupplyNumber(invoice, myDataQrCode);
                        System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                        SetPrinter(printerSettings, invoice);
                        PrintReport(report, printerSettings);
                    }
                    //}
                }
                catch(Exception ex)
                {
                    Logger.Instance.LogToFile("PrintToPrinter : Windows", ex);
                }
            }
            else
            {
                try
                {
                    if (Properties.Settings.Default.TCPPrinterAddress == "")
                    {
                        System.Diagnostics.Trace.WriteLine("Start Invoice Print : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.ffff"));
                        string[] invoiceLines = this.CreateInvoiceText(db, invoice);
                        //invoice.InvoiceLines[0].SalesTransaction.Nozzle.Dispenser.InvoicePrints
                        string printText = "";
                        for (int i = 0; i < invoiceLines.Length; i++)
                        {
                            printText = printText + invoiceLines[i] + "\r\n";
                        }

                        string sign = "Χώρος σήμανσης: " + invoice.InvoiceSignature;
                        printText = printText + sign;

                        for (int i = 0; i < this.tailInvLines; i++)
                        {
                            printText = printText + "\r\n";
                        }

                        string GS = Convert.ToString((char)29);
                        string ESC = Convert.ToString((char)27);
                        string COMMAND = "";
                        COMMAND = ESC + "@";
                        COMMAND += GS + "V" + (char)1;

                        //DirectPrinter.SendToPrinter("ASFuelControl Invoice", printText, invoice.Printer);
                        DirectPrinter.SendToPrinter("ASFuelControl Invoice", printText + "\r\n" + COMMAND, invoice.Printer);

                        System.Diagnostics.Trace.WriteLine("Invoice Printed: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.ffff"));
                    }
                    else
                    {
                        TcpPrint(Properties.Settings.Default.TCPPrinterAddress, invoice);
                    }
                }
                catch(Exception ex)
                {
                    Logger.Instance.LogToFile("PrintToPrinter : Other", ex);
                }
            }
        }

        private void PrintToPrinter(Data.TankFilling tankFilling)
        {
            if(tankFilling.InvoiceLines.Count == 0)
                return;
            try
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                using (Reports.Invoices.TankFillingReport report = new Reports.Invoices.TankFillingReport())
                {
                    report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");
                    report.ReportParameters[0].Value = tankFilling.TankFillingId.ToString();

                    string idd = tankFilling.InvoiceLines[0].Invoice.InvoiceType.InternalDeliveryDescription == null ? "" : tankFilling.InvoiceLines[0].Invoice.InvoiceType.InternalDeliveryDescription;

                    string alertDirectory = System.Environment.CurrentDirectory + "\\Παραλαβές";

                    if (idd != "")
                        report.ReportParameters[1].Value = idd;
                    else
                    {

                        if (tankFilling.LevelStart > tankFilling.LevelEnd)
                            report.ReportParameters[1].Value = "Εξαγωγή Καυσίμου";
                        else
                            report.ReportParameters[1].Value = "Παραλαβή Καυσίμου";
                    }

                    if (tankFilling.LevelStart > tankFilling.LevelEnd)
                        alertDirectory = System.Environment.CurrentDirectory + "\\Εξαγωγές";
                    else
                        alertDirectory = System.Environment.CurrentDirectory + "\\Παραλαβές";

                    if (this.printBalancesPhysical)
                    {
                        System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                        PrintReport(report, printerSettings);
                    }


                    if (!System.IO.Directory.Exists(alertDirectory))
                    {
                        System.IO.Directory.CreateDirectory(alertDirectory);
                    }
                    string fileName = alertDirectory + "\\" + string.Format("TankFilling_{0:yyyyMMdd_HHmmssfff}.pdf", DateTime.Now);
                    this.SaveReport(report, fileName);
                }
                db.Dispose();
            }
            catch(Exception ex)
            {
                Logger.Instance.LogToFile("PrintToPrinter : Windows", ex);
            }

        }

        private void PrintToPrinterInternalInvoice(Data.Invoice inv, bool showAdditionalData)
        {
            Guid invoiceId = inv.InvoiceId;
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var invoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
                if (invoicingProviderEnabled == false && invoice.InvoiceType.SendToMyData)
                {
                    if (!MyDataSendInvoice(invoice.InvoiceId, 3))
                        return;
                }
                else if (invoicingProviderEnabled)
                {

                }
                var myDataInvoice = db.MyDataInvoices.FirstOrDefault(m => m.InvoiceId == inv.InvoiceId);
                var myDataQrCode = "";
                if (myDataInvoice != null && myDataInvoice.Errors != null)
                {
                    var parms = myDataInvoice.Errors.Split('|');
                    if (parms.Length > 1)
                        myDataQrCode = parms[1];
                }
                if (invoice.InvoiceType.IsInternal.HasValue && invoice.InvoiceType.IsInternal.Value)
                {
                    using (Reports.Invoices.InvoiceReport report = new Reports.Invoices.InvoiceReport())
                    {
                        report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");
                        report.ReportParameters["InvoiceId"].Value = invoice.InvoiceId.ToString().ToLower();
                        report.ReportParameters["CompanyName"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
                        report.ReportParameters["CompanyAddress"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
                        report.ReportParameters["CompanyCity"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
                        report.ReportParameters["CompanyVATNumber"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
                        report.ReportParameters["CompanyVATOffice"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
                        report.ReportParameters["CompanyOccupation"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
                        report.ReportParameters["ShowAdditionalData"].Value = showAdditionalData;
                        report.ReportParameters["SignLine"].Value = "";// invoice.InvoiceSignature;
                        report.ReportParameters["CompanyEfk"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyEFK");
                        report.ReportParameters["CompanyReferenceNumber"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyReferenceNumber");
                        
                        report.SetSupplyNumber(invoice, myDataQrCode);
                        System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                        short invCopies = Data.Implementation.OptionHandler.Instance.GetShortOption("InvoiceCopies", 2);
                        printerSettings.Copies = invCopies;
                        PrintReport(report, printerSettings);
                    }
                }
                else
                {
                    using (Reports.Invoices.IncoiceReportShort report = new Reports.Invoices.IncoiceReportShort())
                    {
                        var qil = db.InvoiceLines.Where(il => il.InvoiceId == invoice.InvoiceId).ToList();
                        report.DataSource = qil;

                        if (invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
                        {
                            decimal vat = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 24);
                            foreach (var inl in qil)
                            {
                                inl.UnitPrice = inl.UnitPrice / ((100 + vat) / 100);
                            }
                        }

                        report.ReportParameters["ReplaceParameter"].Value = "";
                        if (invoice.Notes == null)
                            report.ReportParameters["ReplaceParameter"].Value = "";
                        else
                            report.ReportParameters["ReplaceParameter"].Value = invoice.Notes;

                        report.ReportParameters["VATValue"].Value = invoice.InvoiceLines[0].VatPercentage;
                        if (invoice.InvoiceLines[0].VatPercentage == 0 && invoice.VatAmount > 0)
                        {
                            report.ReportParameters["VATValue"].Value = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", (decimal)23);
                        }
                        report.SetSupplyNumber(invoice, myDataQrCode);
                        report.SetRestAmounts(invoice);
                        System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                        short invCopies = Data.Implementation.OptionHandler.Instance.GetShortOption("InvoiceCopies", 2);
                        Console.WriteLine(invCopies);
                        printerSettings.Copies = invCopies;
                        PrintReport(report, printerSettings);
                    }
                }
            }
        }

        private void ExportReportAsText(Telerik.Reporting.Report report, string fileName)
        {
            Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            Telerik.Reporting.InstanceReportSource instanceReportSource = new Telerik.Reporting.InstanceReportSource();
            instanceReportSource.ReportDocument = report;
            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("XLS", instanceReportSource, null);

            fileName = this.processFolder + "\\" + fileName;

            using(FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
                //byte[] line = Encoding.Default.GetBytes(this.CreateSignLine(null));
                //fs.Write(line, 0, line.Length);
                
            }
            
            //string text = ExcelTextStripper.GetText(fileName);
            //System.IO.File.WriteAllText(fileName.Replace(".xls", ".txt"), text);
        }

        public void SendMyDataToPrint(Guid invoiceId)
        {
            int printMode = 0;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var mydatInv = db.MyDataInvoices.FirstOrDefault(m => m.InvoiceId == invoiceId);
                if (mydatInv == null)
                    return;
                var parms = mydatInv.Errors.Split('|');
                if (parms.Length < 2)
                    return;
                printMode = int.Parse(parms[0]);
                var invoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
                if (invoice == null)
                    return;
                if (printMode == 1)
                    PrintInvoiceDirect(invoice);
                else if (printMode == 2)
                    PrintToPrinter(db, invoice);
                else if (printMode == 3)
                {
                    bool isInternal = invoice.InvoiceType.IsInternal.HasValue ? invoice.InvoiceType.IsInternal.Value : true;
                    PrintToPrinterInternalInvoice(invoice, isInternal);
                }
            }
        }

        #endregion

        #region Print Methods

        public static void TcpPrint(string host, Data.Invoice invoice)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int port = Properties.Settings.Default.TCPPrinterPort;
            s.Connect(host, port);
            s.Send(CreatePrint(invoice));
            s.Disconnect(false);
        }

        const char ESC = '\x1b';
        const char FS = '\x1c';
        const char GS = '\x1d';

        static byte[] CreatePrint(Data.Invoice invoice)
        {
            StringBuilder sb = new StringBuilder();

            // Initialize printer
            sb.Append(ESC + "@");
            sb.Append(ESC + "R" + (char)(Properties.Settings.Default.TCPPrinterInternationalCharacter));
            // Align center
            sb.Append(ESC + "a" + (char)1);
            sb.Append(ESC + "t" + (char)(Properties.Settings.Default.TCPPrinterCodeTable));
            string[] lines = CreateInvoiceTextDirect(invoice);
            foreach (string line in lines)
                sb.AppendLine(line);

            string sign = "Χώρος σήμανσης: " + invoice.InvoiceSignature;
            sb.AppendLine(sign);
            //string[] cmd = Properties.Settings.Default.CutCommand.Split(';');
            //string cutCommand = "";
            //string cutCommand2 = GS + "V\x41\0";
            //foreach (string c in cmd)
            //{
            //    cutCommand = cutCommand + (char)(int.Parse(c));
            //}
            sb.Append(GS + "V\x41\0");//Kopse Xarti

            return Encoding.Default.GetBytes(sb.ToString());
        }

        List<Guid> invoiceProcessed = new List<Guid>();

        private void BuildInvoiceDetails(Samtec.WebService.Models.Request req, Data.Invoice invoice)
        {
            var invDetails = new Samtec.WebService.Models.Invoicedetails();
            invDetails.InvoiceUID = invoice.InvoiceId.ToString();
            invDetails.InvoiceType = invoice.InvoiceType.OfficialEnumerator;
            invDetails.PrintDevice = 0;
            invDetails.ReqForToken = 0;
            if (invoice.InvoiceType.IsCancelation.HasValue && invoice.InvoiceType.IsCancelation.Value && invoice.InvoiceType.OfficialEnumerator == 215)
            {
                var rel = invoice.ParentInvoiceRelations.FirstOrDefault();
                if(rel != null)
                {
                    var cinv = rel.ParentInvoice;
                    var giReq = GetInvoiceInfoRequest(cinv);
                    var giResp = Samtec.WebService.HttpClient.CallWS(giReq, samtecWSUrl);
                    var cddn = giResp.GetInvoiceDevDailyNum();

                    invDetails.CancelInvType = cinv.InvoiceType.OfficialEnumerator;
                    invDetails.CancelDevDailyNum = cddn;
                    invDetails.CancelInvNo = cinv.Number.ToString();
                    invDetails.CancelInvSeries = cinv.Series;
                }
                
            }
            else
            {
                invDetails.CancelInvType = null;
                invDetails.CancelDevDailyNum = null;
            }
            invDetails.InvoiceNo = invoice.Number.ToString();
            invDetails.InvoiceSeries = invoice.Series;
            bool isNegative = invoice.InvoiceType.OfficialEnumerator == 175;
            invDetails.InvoiceTotal = (isNegative ? -1 : 1) * (float)invoice.TotalAmount.Value;
            invDetails.GasStationLicNum = 0;
            invDetails.GasStationInstalNum = 0;
            invDetails.InvWithholdingTaxTotal = 0;
            if (invoice.InvoiceType.OfficialEnumerator == 316)
            {
                var invLine = invoice.InvoiceLines.First();
                var volume = (int)(invLine.VolumeNormalized * 1000);
                var volStr = volume.ToString();
                List<char> volList = new List<char>(volStr.ToCharArray());
                volList.Insert(volStr.Length - 3, '.');
                volStr = new string(volList.ToArray());
                invDetails.PrintLine = string.Format("?{0}!{1}", invLine.FuelType.EnumeratorValue, volStr);
            }
            req.Doc.InvoiceDetails = invDetails;
        }
        private void BuildInvoiceDetails(Samtec.WebService.Models.Request req, Guid id)
        {
            var invDetails = new Samtec.WebService.Models.Invoicedetails();
            invDetails.InvoiceUID = id.ToString();
            invDetails.InvoiceType = 501;
            invDetails.PrintDevice = 0;
            invDetails.ReqForToken = 0;
            invDetails.CancelInvType = 0;
            invDetails.CancelDevDailyNum = 0;
            invDetails.InvoiceTotal = (float)0;
            invDetails.GasStationLicNum = 0;
            invDetails.GasStationInstalNum = 0;
            invDetails.InvWithholdingTaxTotal = 0;
            req.Doc.InvoiceDetails = invDetails;
        }
        private void BuildTransactionLines(Samtec.WebService.Models.Request req, Data.Invoice invoice)
        {
            List<Samtec.WebService.Models.Transactionline> transactionLines = new List<Samtec.WebService.Models.Transactionline>();
            if (!(invoice.InvoiceType.IsCancelation.HasValue && invoice.InvoiceType.IsCancelation.Value && invoice.InvoiceType.OfficialEnumerator == 215))
            {
                foreach (var invLine in invoice.InvoiceLines)
                {
                    var trans = new Samtec.WebService.Models.Transactionline();
                    trans.ClassCategory = 1;
                    if (invLine.FuelType.EnumeratorValue != 100)
                        trans.FuelCode = invLine.FuelType.EnumeratorValue;
                    trans.Description = invLine.FuelType.Name;
                    int decimalPlaces = 3;

                    if (invLine.VatPercentage == 0)
                    {
                        decimalPlaces = decimal.Round(invLine.UnitPriceWhole, 3) != decimal.Round(invLine.UnitPriceWhole, 5) ? 5 : 3;
                        trans.ItemAmount = decimal.Round(invLine.UnitPriceWhole, decimalPlaces);
                    }
                    else
                    {
                        if (invLine.UnitPrice < invLine.UnitPriceRetail)
                        {
                            decimalPlaces = decimal.Round(invLine.UnitPriceRetail, 3) != decimal.Round(invLine.UnitPriceRetail, 5) ? 5 : 3;
                            trans.ItemAmount = decimal.Round(invLine.UnitPriceRetail, decimalPlaces);
                        }
                        else
                        {
                            decimalPlaces = decimal.Round(invLine.UnitPrice, 3) != decimal.Round(invLine.UnitPrice, 5) ? 5 : 3;
                            trans.ItemAmount = decimal.Round(invLine.UnitPrice, decimalPlaces);
                        }
                    }
                    trans.MeasurementUnit = 3;
                    trans.SaleQty = decimal.Round(invLine.Volume, 3);
                    trans.NetAmount = decimal.Round((invLine.TotalPrice - invLine.VatAmount), 2);
                    trans.VatAmount = decimal.Round(invLine.VatAmount, 2);
                    bool isNegative = invoice.InvoiceType.OfficialEnumerator == 175;
                    trans.GrossAmount = (isNegative ? -1 : 1) * decimal.Round(invLine.TotalPrice, 2);
                    trans.VatRate = invLine.VatPercentage.ToString("N2").Replace(",", ".");
                    if(invLine.VatPercentage == 0 && invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
                        trans.VatExCategory = (int)Exedron.MyData.Interfaces.VATExemptionCategoryEnum.Article27;
                    else
                        trans.VatExCategory = 0;
                    trans.FeeCategory = 0;
                    if(invLine.DiscountAmount > 0)
                    {
                        trans.DMType = 1;
                        trans.DMValue = (isNegative ? -1 : 1) * decimal.Round(invLine.DiscountAmount, 2);
                    }
                    transactionLines.Add(trans);
                }
            }
            req.Doc.TransactionLines = transactionLines.ToArray();
        }
        private void BuildReceiverHeader(Samtec.WebService.Models.Request req, Data.Invoice invoice)
        {
            if (invoice.Trader == null)
                return;
            var header = new Samtec.WebService.Models.Receiverheader();
            header.TaxID = invoice.Trader.TaxRegistrationNumber;
            header.PrintLine = string.Format("{0}\n{1}\n{2} {3} {4}\nΑΦΜ:{5}\n{6}",
                invoice.Trader.Name,
                invoice.Trader.Occupation,
                invoice.Trader.Address, invoice.Trader.ZipCode, invoice.Trader.City, 
                invoice.Trader.TaxRegistrationNumber,
                invoice.Trader.TaxRegistrationOffice);
            req.Doc.ReceiverHeader = header;
        }
        private void BuildPayments(Samtec.WebService.Models.Request req, Data.Invoice invoice)
        {
            List<Samtec.WebService.Models.Payment> payments = new List<Samtec.WebService.Models.Payment>();
            if (!(invoice.InvoiceType.IsCancelation.HasValue && invoice.InvoiceType.IsCancelation.Value && invoice.InvoiceType.OfficialEnumerator == 215))
            {
                int payType = 0;
                if (invoice.PaymentType.HasValue)
                {
                    Common.Enumerators.PaymentTypeEnum enumVal = (Common.Enumerators.PaymentTypeEnum)invoice.PaymentType.Value;
                    switch (enumVal)
                    {
                        case Common.Enumerators.PaymentTypeEnum.Cash:
                            payType = 0;
                            break;
                        case Common.Enumerators.PaymentTypeEnum.Credit:
                            payType = 5;
                            break;
                        case Common.Enumerators.PaymentTypeEnum.CreditCard:
                            payType = 1;
                            break;
                    }
                }
                var payment = new Samtec.WebService.Models.Payment();
                payment.Type = payType;
                payment.EftposTransType = 0;
                if(payType == 1)
                {
                    payment.EftposTID = eftPosTid;
                }
                bool isNegative = (new int[] { 169, 175}).Contains(invoice.InvoiceType.OfficialEnumerator);
                payment.Value = (isNegative ? -1 : 1) * decimal.Round(invoice.TotalAmount.Value, 2);
                payments.Add(payment);
            }
            req.Doc.Payments = payments.ToArray();
        }

        private Samtec.WebService.Models.Request CreateInvoiceRequest(Data.Invoice invoice)
        {
            try
            {
                Samtec.WebService.Models.Request req = new Samtec.WebService.Models.Request();
                
                req.JobType = "Sign";
                if ((new int[]{ 40, 63, 65, 158, 159, 160, 177,181, 219, 238, 298, 299, 303, 304, 316, 317, 500, 501, 502}).Contains(invoice.InvoiceType.OfficialEnumerator))
                    req.InputType = 2;
                else
                    req.InputType = 1;
                req.Doc = new Samtec.WebService.Models.Doc();
                BuildReceiverHeader(req, invoice);
                BuildInvoiceDetails(req, invoice);
                if (req.InputType == 1)
                {
                    BuildTransactionLines(req, invoice);
                    BuildPayments(req, invoice);
                }
                ASFuelControl.Common.Logger.Instance.Debug("CreateInvoiceRequest::" + req.ToString());
                return req;
            }
            catch (Exception ex)
            {
                ASFuelControl.Common.Logger.Instance.Error("Message: " + ex.Message + "\r\nStackTrace" + ex.StackTrace);
                return null;
            }
        }
        private Samtec.WebService.Models.Request CreateBalanceRequest(Data.Balance balance)
        {
            Samtec.WebService.Models.Request req = new Samtec.WebService.Models.Request();
            req.JobType = "Sign";
            req.InputType = 2;
            req.Doc = new Samtec.WebService.Models.Doc();
            BuildInvoiceDetails(req, balance.BalanceId);
            return req;
        }
        private Samtec.WebService.Models.Request CreateAlertRequest(Data.SystemEvent alert)
        {
            Samtec.WebService.Models.Request req = new Samtec.WebService.Models.Request();
            req.JobType = "Sign";
            req.InputType = 2;
            req.Doc = new Samtec.WebService.Models.Doc();
            BuildInvoiceDetails(req, alert.EventId);
            return req;
        }
        private Samtec.WebService.Models.Request CreateTitrimetryRequest(Data.Titrimetry titrimetry)
        {
            Samtec.WebService.Models.Request req = new Samtec.WebService.Models.Request();
            req.JobType = "Sign";
            req.InputType = 2;
            req.Doc = new Samtec.WebService.Models.Doc();
            BuildInvoiceDetails(req, titrimetry.TitrimetryId);
            return req;
        }
        private Samtec.WebService.Models.Request CreateTankFillingRequest(Data.TankFilling tankFilling)
        {
            Samtec.WebService.Models.Request req = new Samtec.WebService.Models.Request();
            req.JobType = "Sign";
            req.InputType = 2;
            req.Doc = new Samtec.WebService.Models.Doc();
            BuildInvoiceDetails(req, tankFilling.TankFillingId);
            return req;
        }

        private void RecalculateInvoice(Data.Invoice invoice)
        {
            if(invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
            {
                foreach (var invLine in invoice.InvoiceLines)
                {
                    if (invLine.VatPercentage != 0)
                        invLine.VatPercentage = 0;
                }
            }
            decimal net = 0;
            decimal tot = 0;
            decimal vat = 0;
            decimal disc = 0;
            decimal discWhole = 0;
            foreach (var line in invoice.InvoiceLines)
            {
                decimal totalDiscounted = 0;
                decimal totalPreDiscount = 0;
                decimal discPercentage = 0;
                decimal discountRetail = 0;
                decimal discountWhole = 0;
                decimal netPreDiscount = 0;
                decimal netDiscounted = 0;
                decimal lineVat = 0;
                if (line.SalesTransaction != null)
                {
                    totalDiscounted = line.SalesTransaction.TotalPrice;
                    discPercentage = line.SalesTransaction.DiscountPercentage.HasValue ? line.SalesTransaction.DiscountPercentage.Value : 0;
                    discountRetail = decimal.Round((discPercentage * decimal.Round(line.Volume * line.UnitPrice, 2)) / 100, 2);
                    totalDiscounted = line.SalesTransaction.TotalPrice;
                    totalPreDiscount = totalDiscounted + discountRetail;
                }
                else
                {
                    discountRetail = decimal.Round((discPercentage * decimal.Round(line.Volume * line.UnitPrice, 2)) / 100, 2);
                    totalDiscounted = decimal.Round(line.Volume * line.UnitPrice, 2) - discountRetail;
                    totalPreDiscount = totalDiscounted + discountRetail;
                }
                discountWhole = decimal.Round((100 * discountRetail) / (100 + line.VatPercentage), 2);
                netPreDiscount = decimal.Round((100 * totalPreDiscount) / (100 + line.VatPercentage), 2);
                netDiscounted = decimal.Round((100 * totalDiscounted) / (100 + line.VatPercentage), 2);
                lineVat = totalDiscounted - netDiscounted;

                line.TotalPrice = totalDiscounted;
                line.DiscountAmount = discountRetail;
                line.DiscountAmountWhole = discountWhole;
                line.VatAmount = lineVat;

                net = net + netDiscounted;// line.TotalPrice - line.VatAmount + line.DiscountAmountWhole;
                tot = tot + line.TotalPrice;
                vat = vat + line.VatAmount;
                disc = disc + discountRetail;
                discWhole = discWhole + discountWhole;
            }
            // decimal.Round(tot, 2);
            net = decimal.Round(net, 2);
            vat = decimal.Round(vat, 2);
            disc = decimal.Round(disc, 2);
            //tot = net + vat - disc;
            invoice.NettoAmount = net + discWhole;
            invoice.NettoAfterDiscount = net;
            invoice.TotalAmount = decimal.Round(tot, 2);
            invoice.DiscountAmountWhole = discWhole;
            invoice.DiscountAmount = disc;
            //this.DiscountAmountView = disc;
            invoice.VatAmount = vat;

            //foreach(var invLine in invoice.InvoiceLines)
            //{

            //    var quantity = invLine.Volume;
            //    var price = invLine.UnitPriceWhole;
            //    if (invLine.VatPercentage == 0)
            //    {
            //        decimal vatDef = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 24);
            //        price = decimal.Round(100 * invLine.UnitPriceRetail / (100 + vatDef), 5);
            //        invLine.UnitPriceWhole = price;
            //    }
            //    decimal totalPrice = decimal.Round(invLine.Volume * price, 2);
            //    if (invLine.SalesTransaction != null)
            //        totalPrice = invLine.SalesTransaction.TotalPrice;
            //    var nettPrice = totalPrice;
            //    var vatAmount = decimal.Round((nettPrice * invLine.VatPercentage) / 100, 2);
            //    var discAmount = decimal.Round(((nettPrice + vatAmount) * invLine.DiscountPercentage / 100), 2);
            //    var total = nettPrice + vatAmount - discAmount;
            //    invLine.VatAmount = vatAmount;
            //    invLine.TotalPrice = total;
            //    invLine.DiscountNettoAmount = discAmount / ((100 + invLine.VatPercentage) / 100);
            //    invLine.DiscountAmount = discAmount;
            //    //invLine.TotalPrice = 
            //}
            //invoice.TotalAmount = invoice.InvoiceLines.Sum(i => i.TotalPrice);
            //invoice.NettoAmount = invoice.InvoiceLines.Sum(i => decimal.Round(i.Volume * i.UnitPriceWhole, 2));
            //invoice.VatAmount = invoice.InvoiceLines.Sum(i => i.VatAmount);
            //invoice.DiscountAmount = invoice.InvoiceLines.Sum(i => i.DiscountAmount);
        }

        private Samtec.WebService.Models.Request GetInvoiceInfoRequest(Data.Invoice invoice)
        {
            Samtec.WebService.Models.Request req = new Samtec.WebService.Models.Request();
            req.JobType = "GetInvoiceInfo";
            req.JobParams = string.Format("{0},{1},{2}", 2, invoice.TransactionDate.ToString("ddMMyy"), invoice.InvoiceId);
            return req;
        }
        private Samtec.WebService.Models.Request GetInvoiceLastInfoRequest(Data.Invoice invoice)
        {
            Samtec.WebService.Models.Request req = new Samtec.WebService.Models.Request();
            req.JobType = "GetLastInvoiceInfo";
            req.JobParams = string.Format("{0}", invoice.InvoiceId);
            return req;
        }

        public void SignInvoice(Data.DatabaseModel db, Data.Invoice invoice)
        {
            string sign = "";
            string qrData = "";
            if (invoicingProviderEnabled)
            {
                ApplySign(db, sign, qrData, invoice.InvoiceId, "Invoice");
            }
            else
            {
                //string fileName = string.Format(this.signFolder + "\\{0}.txt", invoice.InvoiceId);
                //if (System.IO.File.Exists(fileName))
                //    return;
                if (invoice.Number == 0)
                {
                    invoice.Number = invoice.InvoiceType.LastNumber + 1;
                    invoice.Series = invoice.InvoiceType.DefaultSeries == null ? "" : invoice.InvoiceType.DefaultSeries;
                    invoice.InvoiceType.LastNumber = invoice.Number;
                    db.SaveChanges();
                }

                List<string> lines = new List<string>(this.CreateInvoiceText(db, invoice));

                var req = CreateInvoiceRequest(invoice);
                if (req == null)
                    return;
                req.AtxtContent = string.Join("\r\n", lines);
                var resp = Samtec.WebService.HttpClient.CallWS(req, samtecWSUrl);
                ASFuelControl.Common.Logger.Instance.Debug(resp.ResultCode);
                var text = resp.ResultCode;
                if (text.Contains("Error"))
                {
                    if (invoiceProcessed.Contains(invoice.InvoiceId))
                        invoiceProcessed.Remove(invoice.InvoiceId);
                    if (invoicesProcessed.Contains(invoice.InvoiceId.ToString()))
                        this.invoicesProcessed.Remove(invoice.InvoiceId.ToString());
                    return;
                }
                if (text.Contains("ΔΦΣΣ"))
                {
                    //text = text.Replace("ΔΦΣΣ", "").Replace("[[", "").Replace("]]", "");
                    string[] vals = text.Split(',');
                    sign = vals[1];
                    if (vals.Length > 2 && vals[2].Contains("https://"))
                    {
                        qrData = vals[2];
                    }
                }
                if (sign != "")
                {
                    ApplySign(db, sign, qrData, invoice.InvoiceId, "Invoice");
                }
            }
        }
        private void SignAlert(Data.DatabaseModel db, Data.SystemEvent alert, string alertText)
        {
            //var req = CreateAlertRequest(alert);
            //req.AtxtContent = alertText;
            //var resp = Samtec.WebService.HttpClient.CallWS(req, samtecWSUrl);
            //var text = resp.ResultCode;
            //if (text.Contains("Error"))
            //{
            //    return;
            //}
            //string sign = "";
            //string qrData = "";
            //if (text.Contains("ΔΦΣΣ"))
            //{
            //    //text = text.Replace("ΔΦΣΣ", "").Replace("[[", "").Replace("]]", "");
            //    string[] vals = text.Split(',');
            //    sign = vals[1];
            //    if (vals.Last().Contains("https://"))
            //    {
            //        qrData = vals[2];
            //    }
            //}
            string sign = "-";
            if (sign != "")
            {
                ApplySign(db, sign, "", alert.EventId, "Alert");
            }
        }
        private void SignBalance(Data.DatabaseModel db, Data.Balance balance, string balanceText)
        {
            var req = CreateBalanceRequest(balance);
            req.AtxtContent = balanceText;
            var resp = Samtec.WebService.HttpClient.CallWS(req, samtecWSUrl);
            var text = resp.ResultCode;
            if (text.Contains("Error"))
            {
                if(this.balancesProcessed.Contains(balance.BalanceId))
                    this.balancesProcessed.Remove(balance.BalanceId);
                return;
            }
            string sign = "";
            string qrData = "";
            if (text.Contains("ΔΦΣΣ"))
            {
                //text = text.Replace("ΔΦΣΣ", "").Replace("[[", "").Replace("]]", "");
                string[] vals = text.Split(',');
                sign = vals[1];
                if (vals[2].Contains("https://"))
                {
                    qrData = vals[2];
                }
            }
            if (sign != "")
            {
                ApplySign(db, sign, qrData, balance.BalanceId, "Balance");
            }
        }
        private void SignTitrimetry(Data.DatabaseModel db, Data.Titrimetry titrimetry, string alertText)
        {
            //var req = CreateTitrimetryRequest(titrimetry);
            //req.AtxtContent = alertText;
            //var resp = Samtec.WebService.HttpClient.CallWS(req, samtecWSUrl);
            //var text = resp.ResultCode;
            //if (text.Contains("Error"))
            //{
            //    return;
            //}
            //string sign = "";
            //string qrData = "";
            //if (text.Contains("ΔΦΣΣ"))
            //{
            //    //text = text.Replace("ΔΦΣΣ", "").Replace("[[", "").Replace("]]", "");
            //    string[] vals = text.Split(',');
            //    sign = vals[1];
            //    if (vals[2].Contains("https://"))
            //    {
            //        qrData = vals[2];
            //    }
            //}
            string sign = "-";
            if (sign != "")
            {
                ApplySign(db, sign, "", titrimetry.TitrimetryId, "Titrimetry");
            }
        }
        private void SignTankFilling(Data.DatabaseModel db, Data.TankFilling tankFilling, string alertText)
        {
            //var req = CreateTankFillingRequest(tankFilling);
            //req.AtxtContent = alertText;
            //var resp = Samtec.WebService.HttpClient.CallWS(req, samtecWSUrl);
            //var text = resp.ResultCode;
            //if (text.Contains("Error"))
            //{
            //    return;
            //}
            //string sign = "";
            //string qrData = "";
            //if (text.Contains("ΔΦΣΣ"))
            //{
            //    //text = text.Replace("ΔΦΣΣ", "").Replace("[[", "").Replace("]]", "");
            //    string[] vals = text.Split(',');
            //    sign = vals[1];
            //    if (vals[2].Contains("https://"))
            //    {
            //        qrData = vals[2];
            //    }
            //}
            string sign = "-";
            if (sign != "")
            {
                ApplySign(db, sign, "", tankFilling.TankFillingId, "TankFilling");
            }
        }

        private void PrintInvoice(Data.DatabaseModel db, Data.Invoice invoice)
        {
            if (invoiceProcessed.Contains(invoice.InvoiceId))
                return;
            invoiceProcessed.Add(invoice.InvoiceId);
            try
            {
                if (invoice.InvoiceType.Printable)
                {
                    invoice.RecalculateInvoice();
                    //RecalculateInvoice(invoice);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("Print Agent", ex);
            }
            if (this.defaultTaxDevice == "Samtec")
            {
                SignInvoice(db, invoice);   
                //lines.Add(this.CreateSignLine(invoice));
                //for(int i = 0; i < this.tailInvLines; i++)
                //{
                //    lines.Add(" ");
                //}

                //System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);
                //this.invoicesProcessed.Add(invoice.InvoiceId.ToString());
            }
            else
            {
                string fileName = string.Format(this.signFolder + "\\{0}.txt", invoice.InvoiceId);
                if (invoice.Printer != null && invoice.Printer != "")
                    fileName = string.Format(invoice.Printer + "\\{0}.txt", invoice.InvoiceId);

                if (System.IO.File.Exists(fileName))
                    return;

                string[] invoiceLines = this.CreateInvoiceText(db, invoice);


                var req = CreateInvoiceRequest(invoice);
                if (req == null)
                    return;
                req.AtxtContent = string.Join("\r\n", invoiceLines);
                Samtec.WebService.HttpClient.CallWS(req, "");

                List<string> lines = new List<string>(invoiceLines);
                lines.Add(this.CreateSignLine(invoice));
                for (int i = 0; i < this.tailInvLines; i++)
                {
                    lines.Add(" ");
                }
                System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);

                invoice.IsPrinted = true;
                invoice.InvoiceSignature = "Sign Device A";
                //this.invoicesProcessed.Add(invoice.InvoiceId.ToString() + "$$" + "Sign Device A");
            }

            db.SaveChanges();            
        }

        List<Guid> alertsProcessed = new List<Guid>();
        private void PrintAlert(Data.DatabaseModel db, Data.SystemEvent alert)
        {
            if (alertsProcessed.Contains(alert.EventId))
                return;
            alertsProcessed.Add(alert.EventId);

            using (Reports.AlertReport report = new Reports.AlertReport())
            {
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "SystemEvents");
                report.ReportParameters[0].Value = alert.EventId.ToString().ToLower();

                this.ExportReportAsText(report, "Alert_" + alert.EventId.ToString() + ".xls");
                var fileName = this.processFolder + "\\" + "Alert_" + alert.EventId.ToString() + ".xls";
                FileTypeInfo ft = GetFileTypeInfo(fileName, "xls");
                string alertText = ExcelTextStripper.GetText(fileName, ft.ReportName);
                SignAlert(db, alert, alertText);
            }
        }

        List<Guid> balancesProcessed = new List<Guid>();
        private void PrintBalance(Data.DatabaseModel db, Data.Balance balance)
        {
            if (balancesProcessed.Contains(balance.BalanceId))
                return;
            balancesProcessed.Add(balance.BalanceId);
            string data = balance.BalanceText;


            Reports.BalanceReports.BalanceLoad bl = new Reports.BalanceReports.BalanceLoad();
            bl.LoadBalance(balance.BalanceId, balance.BalanceText);
            bl.Model.Balance[0].Sign = balance.DocumentSign;
            using (Reports.BalanceReport report = new Reports.BalanceReport())
            {
                Reports.BalanceReports.TankBalanceReport subReport1 = new Reports.BalanceReports.TankBalanceReport();
                Reports.BalanceReports.PumpBalanceReport subReport2 = new Reports.BalanceReports.PumpBalanceReport();
                Reports.BalanceReports.DivergenceBalanceReport subReport3 = new Reports.BalanceReports.DivergenceBalanceReport();
                Reports.BalanceReports.TankFillingBalanceReport subReport4 = new Reports.BalanceReports.TankFillingBalanceReport();

                Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                ds1.DataSource = bl.Model;
                ds1.DataMember = "TankData";

                Telerik.Reporting.ObjectDataSource ds2 = new Telerik.Reporting.ObjectDataSource();
                ds2.DataSource = bl.Model;
                ds2.DataMember = "DispenserData";

                Telerik.Reporting.ObjectDataSource ds3 = new Telerik.Reporting.ObjectDataSource();
                ds3.DataSource = bl.Model;
                ds3.DataMember = "FuelTypeData";

                Telerik.Reporting.ObjectDataSource ds4 = new Telerik.Reporting.ObjectDataSource();
                ds4.DataSource = bl.Model;
                ds4.DataMember = "Balance";

                Telerik.Reporting.ObjectDataSource ds5 = new Telerik.Reporting.ObjectDataSource();
                ds5.DataSource = bl.Model;
                ds5.DataMember = "TankFillingData";


                subReport1.DataSource = ds1;
                subReport2.DataSource = ds2;
                subReport3.DataSource = ds3;
                subReport4.DataSource = ds5;
                report.DataSource = ds4;
                report.Name = "BalanceReport";// +balance.StartDate.ToString("yyyyMMdd");
                report.DocumentName = "BalanceReport";// +balance.StartDate.ToString("yyyyMMdd");

                Telerik.Reporting.InstanceReportSource subReportSource1 = new Telerik.Reporting.InstanceReportSource();
                subReportSource1.ReportDocument = subReport1;

                Telerik.Reporting.InstanceReportSource subReportSource2 = new Telerik.Reporting.InstanceReportSource();
                subReportSource2.ReportDocument = subReport2;

                Telerik.Reporting.InstanceReportSource subReportSource3 = new Telerik.Reporting.InstanceReportSource();
                subReportSource3.ReportDocument = subReport3;

                Telerik.Reporting.InstanceReportSource subReportSource4 = new Telerik.Reporting.InstanceReportSource();
                subReportSource4.ReportDocument = subReport4;

                //Telerik.Reporting.InstanceReportSource repSource = new Telerik.Reporting.InstanceReportSource();
                //repSource.ReportDocument = report;

                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport1"]).ReportSource = subReportSource1;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport2"]).ReportSource = subReportSource2;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport3"]).ReportSource = subReportSource3;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport4"]).ReportSource = subReportSource4;

                Telerik.Reporting.InstanceReportSource reportSource = new Telerik.Reporting.InstanceReportSource();
                reportSource.ReportDocument = report;

                this.ExportReportAsText(report, "Balance_" + balance.BalanceId.ToString() + ".xls");
                //var fileName = this.processFolder + "\\" + "Balance_" + balance.BalanceId.ToString() + ".xls";
                //FileTypeInfo ft = GetFileTypeInfo(fileName, "xls");
                //string balanceText = ExcelTextStripper.GetText(fileName, ft.ReportName);
                SignBalance(db, balance, balance.BalanceText);
                //this.PrintText(balance.BalanceText, "Balance_" + balance.BalanceId.ToString());
                //if (this.defaultTaxDevice == "SignA")
                //{
                //    balance.PrintDate = DateTime.Now;
                //    this.entriesProcessed["Balances"].Add(balance.BalanceId);
                //}
            }
        }

        List<Guid> titrimetryProcessed = new List<Guid>();
        private void PrintTitrimetry(Data.DatabaseModel db, Data.Titrimetry titrimetry)
        {
            if (titrimetryProcessed.Contains(titrimetry.TitrimetryId))
                return;
            titrimetryProcessed.Add(titrimetry.TitrimetryId);
            //string data = this.database.SerializeEntity(titrimetry);

            using (Reports.TitrimetryReport report = new Reports.TitrimetryReport())
            {
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db.TitrimetryLevels.Where(tl => tl.TitrimetryId == titrimetry.TitrimetryId), "");
                //report.ReportParameters[0].Value = titrimetry.TitrimetryId.ToString().ToLower();

                this.ExportReportAsText(report, "Titrimetry_" + titrimetry.TitrimetryId.ToString() + ".xls");
                var fileName = this.processFolder + "\\" + "Titrimetry_" + titrimetry.TitrimetryId.ToString() + ".xls";
                FileTypeInfo ft = GetFileTypeInfo(fileName, "xls");
                string titrimetryText = ExcelTextStripper.GetText(fileName, ft.ReportName);
                SignTitrimetry(db, titrimetry, titrimetryText);
            }
        }

        List<Guid> tankFillingsProcessed = new List<Guid>();
        private void PrintTankFilling(Data.DatabaseModel db, Data.TankFilling tankFilling)
        {
            if (tankFillingsProcessed.Contains(tankFilling.TankFillingId))
                return;
            tankFillingsProcessed.Add(tankFilling.TankFillingId);

            if (tankFilling == null || tankFilling.InvoiceLines.Count == 0)
                return;

            using (Reports.Invoices.TankFillingReport report = new Reports.Invoices.TankFillingReport())
            {
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");
                report.ReportParameters[0].Value = tankFilling.TankFillingId.ToString();

                string idd = tankFilling.InvoiceLines[0].Invoice.InvoiceType.InternalDeliveryDescription == null ? "" : tankFilling.InvoiceLines[0].Invoice.InvoiceType.InternalDeliveryDescription;

                if (idd != "")
                    report.ReportParameters[1].Value = idd;
                else
                {
                    if (tankFilling.LevelStart > tankFilling.LevelEnd)
                        report.ReportParameters[1].Value = "Εξαγωγή Καυσίμου";
                    else
                        report.ReportParameters[1].Value = "Παραλαβή Καυσίμου";
                }
                this.ExportReportAsText(report, "TankFilling_" + tankFilling.TankFillingId.ToString() + ".xls");
                var fileName = this.processFolder + "\\" + "TankFilling_" + tankFilling.TankFillingId.ToString() + ".xls";
                FileTypeInfo ft = GetFileTypeInfo(fileName, "xls");
                string tankFillingText = ExcelTextStripper.GetText(fileName, ft.ReportName);
                SignTankFilling(db, tankFilling, tankFillingText);
            }
        }

        private void CreateInternalInvoiceText(Data.DatabaseModel db, Data.Invoice invoice)
        {
            string fileName = this.signFolder + "\\Invoice_" + invoice.InvoiceId.ToString() + ".txt";
            string str = Properties.Resources.InvoiceText;
            str = str.Replace("[CompanyName]", Data.Implementation.OptionHandler.Instance.GetOption("CompanyName"));
            str = str.Replace("[CompanyAddress]", Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress"));
            str = str.Replace("[CompanyCity]", Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity"));
            str = str.Replace("[CompanyVATNumber]", Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN"));
            str = str.Replace("[CompanyVATOffice]", Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice"));
            str = str.Replace("[CompanyOccupation]", Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation"));

            if (invoice.Trader != null)
            {
                str = str.Replace("[Trader.Name]", invoice.Trader.Name);
                str = str.Replace("[Trader.Address]", invoice.Trader.Address);
                str = str.Replace("[Trader.City]", invoice.Trader.City);
                str = str.Replace("[Trader.TaxRegistrationNumber]", invoice.Trader.TaxRegistrationNumber);
                str = str.Replace("[Trader.TaxRegistrationOffice]", invoice.Trader.TaxRegistrationOffice);
                str = str.Replace("[Trader.Occupation]", invoice.Trader.Occupation);
                if(invoice.Vehicle != null)
                    str = str.Replace("[Vehicle.PlateNumber]", invoice.Vehicle.PlateNumber);
            }
            else
            {
                str = str.Replace("[Trader.Name]", "");
                str = str.Replace("[Trader.Address]", "");
                str = str.Replace("[Trader.City]", "");
                str = str.Replace("[Trader.TaxRegistrationNumber]", "");
                str = str.Replace("[Trader.TaxRegistrationOffice]", "");
                str = str.Replace("[Trader.Occupation]", "");
                str = str.Replace("[Vehicle.PlateNumber]", "");
            }
            str = str.Replace("[InvoiceType.Description]", invoice.InvoiceType.Description);
            str = str.Replace("[Invoice.Series]", invoice.Series);
            str = str.Replace("[Invoice.Number]", invoice.Number.ToString());
            str = str.Replace("[Invoice.TransactionDate]", invoice.TransactionDate.ToString("dd/MM/yyyy HH:mm"));

            string lines = "";
            foreach(var invLine in invoice.InvoiceLines)
            {
                var lineStr = Properties.Resources.InvoiceLineText;
                lineStr = lineStr.Replace("[FuelType.Code]", invLine.FuelType != null ? invoice.InvoiceLines[0].FuelType.Code : "");
                lineStr = lineStr.Replace("[FuelType.Name]", invLine.FuelType != null ? invoice.InvoiceLines[0].FuelType.Name : "");
                lineStr = lineStr.Replace("[Volume]", invLine.Volume.ToString("N2"));
                decimal unitPrice = decimal.Round(invoice.InvoiceLines[0].UnitPrice / ((100 + invLine.VatPercentage) / 100), 3);
                lineStr = lineStr.Replace("[UnitPrice]", invoice.InvoiceType.ShowFinancialDataEx ? unitPrice.ToString("N5") : (0).ToString("N5"));
                lineStr = lineStr.Replace("[Discount]", invoice.InvoiceType.ShowFinancialDataEx ? invLine.DiscountAmount.ToString("N5") : (0).ToString("N5"));
                lineStr = lineStr.Replace("[VatAmount]", invoice.InvoiceType.ShowFinancialDataEx ? invLine.VatAmount.ToString("N2") : (0).ToString("N2"));
                lineStr = lineStr.Replace("[TotalPrice]", invoice.InvoiceType.ShowFinancialDataEx ? invLine.TotalPrice.ToString("N2") : (0).ToString("N2"));
                if(lines == "")
                    lines = lineStr;
                else
                    lines = lines + "\r\n" + lineStr;
            }

            str = str.Replace("[InvoiceLines]", lines);

            str = str.Replace("[Invoice.NettoAmount]", invoice.InvoiceType.ShowFinancialDataEx ? invoice.NettoAmount.Value.ToString("N2") : (0).ToString("N2"));
            str = str.Replace("[Invoice.DiscountNettoAmount]", invoice.InvoiceType.ShowFinancialDataEx ? invoice.DiscountNettoAmount.ToString("N2") : (0).ToString("N2"));
            str = str.Replace("[Invoice.NettoAmount - Invoice.DiscountNettoAmount]", invoice.InvoiceType.ShowFinancialDataEx ? (invoice.NettoAmount.Value - invoice.DiscountNettoAmount).ToString("N2") : (0).ToString("N2"));
            str = str.Replace("[Invoice.VatAmount]", invoice.InvoiceType.ShowFinancialDataEx ? invoice.VatAmount.Value.ToString("N2") : (0).ToString("N2"));
            str = str.Replace("[Invoice.TotalAmount]", invoice.InvoiceType.ShowFinancialDataEx ? invoice.TotalAmount.Value.ToString("N2") : (0).ToString("N2"));
            if (invoice.Trader!= null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
                str = str.Replace("[Invoice.Trader.VatExemptionReason]", ":ΑΠΑΛΛΑΓΗ Φ.Π.Α.: " + invoice.Trader.VatExemptionReason);
            str = str.Replace("[ELINE]", this.CreateSignLine(invoice));
            System.IO.File.AppendAllText(this.signFolder + "\\Invoice_" + invoice.InvoiceId.ToString() + ".txt", str, Encoding.Default);
        }

        private void PrintInternalInvoice(Data.DatabaseModel db, Data.Invoice invoice)
        {
            try
            {
                if (invoice.InvoiceType.Printable)
                {
                    invoice.RecalculateInvoice();
                    db.SaveChanges();
                }
            }
            catch(Exception ex)
            {
                Logger.Instance.LogToFile("Print Agent", ex);
            }
            SignInvoice(db, invoice);
            //CreateInternalInvoiceText(db, invoice);
            //Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            //using (Reports.Invoices.InvoiceReport report = new Reports.Invoices.InvoiceReport())
            //{
            //    report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");
            //    report.ReportParameters["InvoiceId"].Value = invoice.InvoiceId.ToString().ToLower();
            //    report.ReportParameters["CompanyName"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            //    report.ReportParameters["CompanyAddress"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            //    report.ReportParameters["CompanyCity"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            //    report.ReportParameters["CompanyVATNumber"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            //    report.ReportParameters["CompanyVATOffice"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
            //    report.ReportParameters["CompanyOccupation"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
            //    report.ReportParameters["SignLine"].Value = this.CreateSignLine(invoice);
            //    this.ExportReportAsText(report, "Invoice_" + invoice.InvoiceId.ToString() + ".xls");
            //}
            //db.Dispose();
        }

        #endregion

        //private static List<Guid> failedInvoices = new List<Guid>();
        //private void SendOldInvoiceThread()
        //{
        //    if (oldInvoiceRunning)
        //        return;
        //    oldInvoicesDateStart = Data.Implementation.OptionHandler.Instance.GetDateTimeOption("MyDataStartDate", DateTime.Today);//DateTime.Parse("2022/01/01");
        //    oldInvoiceRunning = true;

        //    //using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
        //    //{
        //    //    var invoiceIds = db.MyDataInvoices.Where(m => m.Status == -10).Select(i => i.MyDataInvoiceId).ToArray();
        //    //    if (invoiceIds.Count() > 0)
        //    //    {
        //    //        foreach (var invoiceId in invoiceIds)
        //    //        {
        //    //            var toDelete = db.MyDataInvoices.Where(m => m.MyDataInvoiceId == invoiceId).FirstOrDefault();
        //    //            if (toDelete == null)
        //    //                continue;
        //    //            if (toDelete.CanceledByMark.HasValue)
        //    //                continue;
        //    //            if (!toDelete.Mark.HasValue)
        //    //                continue;
        //    //            CancelInvoice(db, toDelete);
        //    //            System.Threading.Thread.Sleep(500);
        //    //        }
        //    //        string command = "INSERT [MyDataInvoiceHistory] SELECT * FROM [MyDataInvoice] where Status = -10";
        //    //        var rows = db.ExecuteNonQuery(command, new System.Data.Common.DbParameter[] { });

        //    //        command = "Delete [MyDataInvoice]  where Status = -10";
        //    //        rows = db.ExecuteNonQuery(command, new System.Data.Common.DbParameter[] { });
        //    //        db.SaveChanges();
        //    //    }
        //    //}

        //    //while (true)
        //    //{
        //    //    try
        //    //    {
        //    //        DateTime dateEnd = DateTime.Now.AddHours(-1);
        //    //        using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
        //    //        {
        //    //            var query =
        //    //                from inv in db.Invoices
        //    //                where inv.TransactionDate >= oldInvoicesDateStart &&
        //    //                        inv.TransactionDate < dateEnd && 
        //    //                        inv.InvoiceType.SendToMyData &&
        //    //                        inv.Number > 0 &&
        //    //                        !db.MyDataInvoices.Select(m => m.InvoiceId).Contains(inv.InvoiceId)
        //    //                orderby inv.TransactionDate
        //    //                select inv.InvoiceId;
        //    //            var invoiceId = query.ToArray().Where(i=>!failedInvoices.Contains(i)).FirstOrDefault();
        //    //            if (invoiceId != Guid.Empty)
        //    //            {
        //    //                if(!MyDataSendInvoice(invoiceId))
        //    //                    failedInvoices.Add(invoiceId);
        //    //            }
        //    //            if (!agentRunning)
        //    //                break;
        //    //            if (!Data.Implementation.OptionHandler.Instance.GetBoolOption("MyDataIsActive", false))
        //    //                break;
        //    //            System.Threading.Thread.Sleep(1000);
        //    //        }
        //    //        //using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
        //    //        //{
        //    //        //    var query =
        //    //        //        from inv in db.MyDataInvoices
        //    //        //        where inv.Status == -1 && 
        //    //        //              inv.Errors.Contains("202 -")
        //    //        //        select inv.InvoiceId;
        //    //        //    var invoiceId = query.FirstOrDefault();
        //    //        //    if (invoiceId != Guid.Empty)
        //    //        //    {
        //    //        //        ResendInvoice(invoiceId);
        //    //        //    }
        //    //        //    if (!agentRunning)
        //    //        //        break;
        //    //        //    System.Threading.Thread.Sleep(1000);
        //    //        //}
        //    //        //202 - 101031677   : Invalid Greek VAT number
        //    //    }
        //    //    catch(Exception ex)
        //    //    {
        //    //        Common.Logger.Instance.Error(ex, "", 2);
        //    //    }
        //    //}
        //    //oldInvoiceRunning = false;
        //}

        //private void ResendFailedThread()
        //{
        //    resendInvoiceRunning = true;
        //    while (true)
        //    {
        //        try
        //        {
        //            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
        //            {
        //                DateTime lastSend = DateTime.Now.AddMinutes(-10);
        //                var query =
        //                    from inv in db.Invoices
        //                    join md in db.MyDataInvoices on inv.InvoiceId equals md.InvoiceId
        //                    where   (md.Status == -1 && (md.Errors.Contains("XML (1, 2)") || md.Errors.Contains("XML (1, 1)"))) || 
        //                            (md.Status == 0 && md.DateTimeSent <= lastSend) ||
        //                            (md.Status == -1 && (md.Errors.Contains("202 -")))
        //                    orderby md.DateTimeSent
        //                    select md.MyDataInvoiceId;
        //                var invoiceId = query.FirstOrDefault();
        //                if (invoiceId != Guid.Empty)
        //                {
        //                    ResendInvoice(invoiceId);
        //                }
        //                if (!agentRunning)
        //                    break;
        //                if (!Data.Implementation.OptionHandler.Instance.GetBoolOption("MyDataIsActive", false))
        //                    break;
        //                System.Threading.Thread.Sleep(1000);
        //            }
        //            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
        //            {
        //                var wrongInvoice = db.MyDataInvoices.FirstOrDefault(m => m.Status > 0 && m.Data.Contains("category1_2"));
        //                if(wrongInvoice != null)
        //                {
        //                    CancelAndResend(db, wrongInvoice);
        //                    db.SaveChanges();
        //                    System.Threading.Thread.Sleep(1000);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Common.Logger.Instance.Error(ex, "", 2);
        //        }
        //    }
        //    resendInvoiceRunning = false;
        //}

        public void BuildBrandCode(Data.Invoice inv)
        {
            if (!Data.Implementation.OptionHandler.Instance.GetBoolOption("HasBrandQRCode", false))
                return;
            if (!inv.InvoiceType.DispenserType.HasValue || !inv.InvoiceType.DispenserType.Value)
                return;
            if (inv.InvoiceLines.Count != 1)
                return;
            var invLine = inv.InvoiceLines[0];
            int brand = Data.Implementation.OptionHandler.Instance.GetIntOption("Brand", 1);
            if (brand == 1 || brand == 2)
            {
                ViewModels.EKOQRData ekoData = new ViewModels.EKOQRData();
                ekoData.AMDIKA = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");
                ekoData.Brand = brand;
                ekoData.DocDate = inv.TransactionDate;
                ekoData.DocNo = inv.Number;
                ekoData.DocTime = inv.TransactionDate;
                ekoData.DocType = inv.InvoiceType.OfficialEnumerator == 316 ? "B" : "A";
                ekoData.ID = inv.Number;
                ekoData.PayCode = inv.PaymentType.HasValue ? inv.PaymentType.Value : 1;
                ekoData.Price = invLine.UnitPrice;
                ekoData.Product = invLine.FuelType.Name;
                ekoData.ProductType = invLine.FuelType.Code;
                ekoData.Ser = (inv.Series == null || inv.Series == "") ? "A" : inv.Series;
                ekoData.Store = Data.Implementation.OptionHandler.Instance.GetOption("StoreId");
                ekoData.Value = inv.TotalAmount.HasValue ? inv.TotalAmount.Value : (decimal)0;
                ekoData.Volume = invLine.Volume;
                string brandQRCode = ekoData.GetQData();
            }
        }

        private static void PrintReport(Telerik.Reporting.Report report, System.Drawing.Printing.PrinterSettings printerSettings)
        {
            try
            {
                string psize = Data.Implementation.OptionHandler.Instance.GetOptionOrAdd("InvoicePageSetting", "A4");

                if (psize == "A5" && (report.GetType() == typeof(Reports.InvoiceReport) || report.GetType() == typeof(Reports.Invoices.IncoiceReportShort)))
                {
                    report.PageSettings.Landscape = true;
                    report.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.A5;
                }

                Telerik.Reporting.Processing.ReportProcessor processor = new Telerik.Reporting.Processing.ReportProcessor();
                Telerik.Reporting.InstanceReportSource reportSource = new Telerik.Reporting.InstanceReportSource();
                reportSource.ReportDocument = report;
                if(!installedPrintrers.Contains(printerSettings.PrinterName))
                {
                    PrinterSettings defPrinterSettings = new PrinterSettings();
                    printerSettings.PrinterName = defPrinterSettings.PrinterName;
                }
                processor.PrintReport(reportSource, printerSettings);
            }
            catch(Exception ex)
            {
            }
        }

        private static bool MyDataSendInvoice(Guid invoiceId, int printMode)
        {
            if (!Exedron.MyData.Settings.IsActive)
                return true;

            Exedron.MyData.Settings.SubscriptionKey = Data.Implementation.OptionHandler.Instance.GetOption("MyDataSubscriptionKey", "");
            Exedron.MyData.Settings.Username = Data.Implementation.OptionHandler.Instance.GetOption("MyDataUserName");

            if (string.IsNullOrEmpty(Exedron.MyData.Settings.SubscriptionKey) || string.IsNullOrEmpty(Exedron.MyData.Settings.SubscriptionKey))
                return true;

            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var dbInvoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
                if (dbInvoice == null)
                    return false;
                if (!dbInvoice.InvoiceType.SendToMyData)
                    return true;

                var myDataEntry = db.MyDataInvoices.FirstOrDefault(m => m.InvoiceId == invoiceId);
                if (myDataEntry != null && myDataEntry.Status == 3)
                    return true;
                try
                {
                    string msg = "";
                    var invoice = InvoiceHelper.CreateInvoice(db, invoiceId, out msg);
                    if (invoice == null)
                    {
                        if (myDataEntry == null)
                        {
                            myDataEntry = new Data.MyDataInvoice();
                            myDataEntry.MyDataInvoiceId = Guid.NewGuid();
                            db.Add(myDataEntry);
                        }
                        myDataEntry.DateTimeSent = DateTime.Now;
                        myDataEntry.Status = -1;
                        myDataEntry.Data = "";
                        myDataEntry.Errors = printMode.ToString() + "|" + msg;
                        myDataEntry.InvoiceId = invoiceId;
                        db.SaveChanges();
                        return false;
                    }
                    Exedron.MyData.InvoiceModels.InvoiceDoc doc = new Exedron.MyData.InvoiceModels.InvoiceDoc();
                    doc.Invoices = new Exedron.MyData.Interfaces.IInvoice[] { invoice };
                    string xml = doc.AsXml();

                    if(dbInvoice.InvoiceType.OfficialEnumerator == 158)
                    {
                        xml = xml.Replace("<currency>", "");
                        xml = xml.Replace("</currency>", "");
                        xml = xml.Replace("<paymentMethods>", "");
                        xml = xml.Replace("</paymentMethods>", "");
                    }
                    else
                    {
                        xml = xml.Replace("<movePurpose>1</movePurpose>", "");
                    }

                    xml = xml.Replace("<deductionsAmount>0.00</deductionsAmount>", "");

                    return SendInvoice(db, invoiceId, xml, printMode);
                }
                catch(Exception ex)
                {
                    if (myDataEntry == null)
                    {
                        myDataEntry = new Data.MyDataInvoice();
                        myDataEntry.MyDataInvoiceId = Guid.NewGuid();
                        db.Add(myDataEntry);
                    }
                    myDataEntry.DateTimeSent = DateTime.Now;
                    myDataEntry.Status = -1;
                    myDataEntry.Data = "";
                    myDataEntry.Errors = printMode.ToString() + "|" + ex.Message;
                    myDataEntry.InvoiceId = invoiceId;
                    db.SaveChanges();
                    return false;
                }
            }
        }

        public static void CancelInvoice(Data.DatabaseModel db, Data.MyDataInvoice invToCancel)
        {
            var url = Data.Implementation.OptionHandler.Instance.GetOption("MyDataUrl", "https://mydata-dev.azure-api.net");
            var respCancel = Exedron.MyData.InvoiceCancelation.MakeRequest(url, invToCancel.Mark.Value);
            if (respCancel.response.cancellationMark.HasValue)
            {
                long mark = (long)respCancel.response.cancellationMark.Value;
                invToCancel.CanceledByMark = mark;
                db.SaveChanges();
            }
        }

        private static bool SendInvoice(Data.DatabaseModel db, Guid invoiceId, string xml, int printMode)
        {
            var myDataEnabled = Data.Implementation.OptionHandler.Instance.GetBoolOption("MyDataIsActive", false);
            if (!myDataEnabled)
                return true;
            var myDataUrl = Data.Implementation.OptionHandler.Instance.GetOption("MyDataUrl", "https://mydata-dev.azure-api.net");

            var invoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
            //if(invoice.InvoiceType.IsCancelation.HasValue && invoice.InvoiceType.IsCancelation.Value)
            //{
            //    var parentInvoices = invoice.ParentInvoiceRelations.Select(s => s.ParentInvoice).ToArray();
            //    var noDateInvoice = parentInvoices.FirstOrDefault(s => s.TransactionDate.Date != invoice.TransactionDate.Date);
            //    if (noDateInvoice == null && parentInvoices.Length > 0)
            //    {
            //        //foreach (var parInvoice in parentInvoices)
            //        //{
            //        //    var parMyDataInvoice = db.MyDataInvoices.FirstOrDefault(m => m.InvoiceId == parInvoice.InvoiceId);
            //        //    if (parMyDataInvoice != null)
            //        //    {
            //        //        CancelInvoice(db, parMyDataInvoice);
            //        //    }
            //        //}
            //        var mdCancelInvoice = new Data.MyDataInvoice();
            //        mdCancelInvoice.MyDataInvoiceId = Guid.NewGuid();
            //        mdCancelInvoice.InvoiceId = invoice.InvoiceId;
            //        mdCancelInvoice.DateTimeSent = DateTime.Now;
            //        mdCancelInvoice.Status = 4;
            //        db.Add(mdCancelInvoice);
            //        db.SaveChanges();
            //        return;
            //    }
            //}

            Guid mdInvoiceId = Guid.NewGuid();

            Data.MyDataInvoice mdInvoice = db.MyDataInvoices.FirstOrDefault(d => d.InvoiceId == invoiceId);
            if (mdInvoice == null)
            {
                mdInvoice = new Data.MyDataInvoice();
                mdInvoice.MyDataInvoiceId = mdInvoiceId;
                db.Add(mdInvoice);
            }
            mdInvoice.DateTimeSent = DateTime.Now;
            mdInvoice.Status = 0;
            mdInvoice.Data = xml;
            mdInvoice.InvoiceId = invoiceId;
            db.SaveChanges();

            var resp = Exedron.MyData.InvoiceSender.MakeRequest(myDataUrl, xml);

            if (resp != null && resp.response != null)
            {
                mdInvoice.DateTimeSent = DateTime.Now;
                if (!string.IsNullOrEmpty(resp.response.invoiceUid))
                {

                    mdInvoice.Uid = resp.response.invoiceUid;
                    mdInvoice.Mark = (long)resp.response.invoiceMark.Value;
                    mdInvoice.Errors = printMode.ToString() + "|" + resp.response.qrUrl;
                    mdInvoice.Status = 3;
                }
                else if (resp.response.errors != null)
                {
                    mdInvoice.Status = -1;
                    mdInvoice.Errors = printMode.ToString() + "|" + string.Join("\r\n", resp.response.errors.Select(e => e.code.ToString() + " - " + e.message));
                }
            }
            else
            {
                mdInvoice.DateTimeSent = DateTime.Now;
                mdInvoice.Status = 0;
                mdInvoice.Errors = mdInvoice.Errors = printMode.ToString() + "|" + "No response";
            }
            db.SaveChanges();
            if(mdInvoice.Status == 3)
                return true;
            return false;
        }

        //private static void ResendInvoice(Guid mdInvoiceId)
        //{
        //    var myDataEnabled = Data.Implementation.OptionHandler.Instance.GetBoolOption("MyDataIsActive", false);
        //    if (!myDataEnabled)
        //        return;

        //    var myDataUrl = Data.Implementation.OptionHandler.Instance.GetOption("MyDataUrl", "https://mydata-dev.azure-api.net");
        //    using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
        //    {
        //        Data.MyDataInvoice mdInvoice = db.MyDataInvoices.FirstOrDefault(d => d.MyDataInvoiceId == mdInvoiceId);
        //        if (mdInvoice == null)
        //        {
        //            return;
        //        }
        //        var invoiceId = mdInvoice.InvoiceId;
        //        string msg = "";
        //        var invoice = InvoiceHelper.CreateInvoice(db, invoiceId, out msg);
        //        if (invoice == null)
        //        {
        //            mdInvoice.DateTimeSent = DateTime.Now;
        //            mdInvoice.Status = -1;
        //            mdInvoice.Errors = msg;
        //            db.SaveChanges();
        //            return;
        //        }
        //        var oldStatus = mdInvoice.Status;
        //        Exedron.MyData.InvoiceModels.InvoiceDoc doc = new Exedron.MyData.InvoiceModels.InvoiceDoc();
        //        doc.Invoices = new Exedron.MyData.Interfaces.IInvoice[] { invoice };
        //        string xml = doc.AsXml();

        //        var resp = Exedron.MyData.InvoiceSender.MakeRequest(myDataUrl, xml);

        //        if (resp != null && resp.response != null)
        //        {
        //            mdInvoice.DateTimeSent = DateTime.Now;
        //            if (!string.IsNullOrEmpty(resp.response.invoiceUid))
        //            {

        //                mdInvoice.Uid = resp.response.invoiceUid;
        //                mdInvoice.Mark = (long)resp.response.invoiceMark.Value;
        //                mdInvoice.Status = 3;
        //            }
        //            else if (resp.response.errors != null)
        //            {
        //                if(oldStatus == -1)
        //                    mdInvoice.Status = -2;
        //                else
        //                    mdInvoice.Status = -1;
        //                mdInvoice.Errors = string.Join("\r\n", resp.response.errors.Select(e => e.code.ToString() + " - " + e.message));
        //            }
        //        }
        //        else
        //        {
        //            mdInvoice.DateTimeSent = DateTime.Now;
        //            mdInvoice.Status = 0;
        //            mdInvoice.Errors = "No response";
        //        }
        //        db.SaveChanges();
        //    }
        //}

        //private static void CancelAndResend(Data.DatabaseModel db, Data.MyDataInvoice invToCancel)
        //{
        //    var invoiceId = invToCancel.InvoiceId;

        //    var invoice = InvoiceHelper.CreateInvoice(db, invoiceId);
        //    if (invoice == null)
        //    {
        //        return;
        //    }

        //    if (!invToCancel.CanceledByMark.HasValue)
        //    {
        //        CancelInvoice(db, invToCancel);
        //    }
        //    var mdInvoice = db.MyDataInvoices.FirstOrDefault(d => d.InvoiceId == invoiceId);
        //    if (!mdInvoice.CanceledByMark.HasValue)
        //        return;

        //    Exedron.MyData.InvoiceModels.InvoiceDoc doc = new Exedron.MyData.InvoiceModels.InvoiceDoc();
        //    doc.Invoices = new Exedron.MyData.Interfaces.IInvoice[] { invoice };
        //    string xml = doc.AsXml();
        //    SendInvoice(db, invoiceId, xml);
        //}

        //private void PrintInvoice(System.Drawing.Image img, string printerName)
        //{
        //    PaperSize paperSize = new PaperSize("Reciept", img.Width, img.Height);
        //    paperSize.RawKind = (int)PaperKind.Custom;
        //    PrintDocument pd = new PrintDocument();

        //    pd.DefaultPageSettings.PaperSize = paperSize;
        //    pd.DefaultPageSettings.Landscape = false;
        //    pd.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
        //    pd.PrinterSettings.PrinterName = printerName;
        //    pd.PrintPage += Pd_PrintPage;
        //    pd.Print();
        //}

        //private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        //{
        //    System.Drawing.Point loc = new System.Drawing.Point(10, 10);
        //    e.Graphics.DrawImage(img, loc);
        //}
    }

    public class InvoiceHelper
    {
        public static bool RecalculateInvoicePrices(Data.Invoice invoice, Data.InvoiceLine[] invoiceItems)
        {
            var disc = invoiceItems.Sum(i => i.DiscountAmount);
            var vat = invoiceItems.Sum(i => i.VatAmount);
            var tot = invoiceItems.Sum(i => i.TotalPrice);
            var diffVat = (invoice.VatAmount.HasValue ? invoice.VatAmount.Value : 0) - vat;
            var diffDisc = invoice.DiscountAmount - disc;
            var diffTotal = (invoice.TotalAmount.HasValue ? invoice.TotalAmount.Value : 0) - tot;

            bool hasChanges = false;
            if (diffVat != 0)
            {
                while (diffVat != 0)
                {
                    foreach (var i in invoiceItems)
                    {
                        if (diffVat > 0)
                            i.VatAmount = i.VatAmount + (decimal)0.01;
                        else
                            i.VatAmount = i.VatAmount - (decimal)0.01;
                        hasChanges = true;
                        vat = invoiceItems.Sum(iv => iv.VatAmount);
                        diffVat = (invoice.VatAmount.HasValue ? invoice.VatAmount.Value : 0) - vat;
                        if (diffVat == 0)
                            break;
                    }
                }
            }
            if (diffDisc != 0)
            {
                while (diffDisc != 0)
                {
                    foreach (var i in invoiceItems)
                    {
                        if (diffDisc > 0)
                            i.DiscountAmount = i.DiscountAmount + (decimal)0.01;
                        else
                            i.DiscountAmount = i.DiscountAmount - (decimal)0.01;
                        hasChanges = true;
                        disc = invoiceItems.Sum(iv => iv.DiscountAmount);
                        diffDisc = invoice.DiscountAmount - disc;
                        if (diffDisc == 0)
                            break;
                    }
                }
            }
            if (diffTotal != 0)
            {
                while (diffTotal != 0)
                {
                    foreach (var i in invoiceItems)
                    {
                        if (diffTotal > 0)
                            i.TotalPrice = i.TotalPrice + (decimal)0.01;
                        else
                            i.TotalPrice = i.TotalPrice - (decimal)0.01;
                        hasChanges = true;
                        tot = invoiceItems.Sum(iv => iv.TotalPrice);
                        diffTotal = (invoice.TotalAmount.HasValue ? invoice.TotalAmount.Value : 0) - tot;
                        if (diffTotal == 0)
                            break;
                    }
                }
            }
            if (invoice.TotalAmount - ((invoice.NettoAmount - invoice.DiscountAmount) + (invoice.VatAmount.HasValue ? invoice.VatAmount.Value : 0)) != 0)
            {
                invoice.TotalAmount = (invoice.NettoAmount - invoice.DiscountAmount) + (invoice.VatAmount.HasValue ? invoice.VatAmount.Value : 0);
                hasChanges = true;
            }
            return hasChanges;
        }

        public static Exedron.MyData.InvoiceModels.Invoice CreateInvoice(Data.DatabaseModel db, Guid invoiceId, out string errorMessage)
        {
            List<int> wholeSaleCodes = new List<int>();
            foreach(var wsc in Properties.Settings.Default.WholeSaleCodes)
            {
                wholeSaleCodes.Add(int.Parse(wsc));
            }

            string vatNumber = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN").Trim();
            if (string.IsNullOrEmpty(vatNumber))
            {
                errorMessage = "Company VAT is not set";
                return null;
            }
            var invoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
            //if (RecalculateInvoicePrices(invoice, invoice.InvoiceLines.ToArray()))
            //    db.SaveChanges();
            var invoceItems = invoice.InvoiceLines;
            var invType = invoice.InvoiceType;
            bool isService = false;
            bool isCanceling = invType.IsCancelation.HasValue && invType.IsCancelation.Value;
            bool isRetail = !wholeSaleCodes.Contains(invType.OfficialEnumerator);
            List<long> corInvoices = new List<long>();
            if (isCanceling)
            {
                var invTrans = invoice.ParentInvoiceRelations.ToArray();
                if (invTrans != null)
                {
                    foreach (var it in invTrans)
                    {
                        var dbParent = it.ParentInvoice;
                        if (!dbParent.InvoiceType.SendToMyData)
                        {
                            errorMessage = "Parent's InvoiceType SendToMyData flag is set to false";
                            return null;
                        }
                        isRetail = !wholeSaleCodes.Contains(dbParent.InvoiceType.OfficialEnumerator);
                        Common.Logger.Instance.CurrentLogger.Debug(string.Format("Invoice {0} isRetail = {1}", dbParent.InvoiceId, isRetail));
                        var invToCancel = db.MyDataInvoices.FirstOrDefault(i => i.InvoiceId == dbParent.InvoiceId);
                        if (invToCancel != null)
                        {
                            if (!invToCancel.Mark.HasValue)
                            {
                                errorMessage = "Invoice to cancel has no Mark";
                                return null;
                            }
                            
                            corInvoices.Add(invToCancel.Mark.Value);
                        }
                    }
                }
            }



            Exedron.MyData.InvoiceModels.Invoice inv = new Exedron.MyData.InvoiceModels.Invoice();
            
            bool vatExemption = false;
            if (invoice.Trader != null && invoice.Trader.VatExemption.HasValue && invoice.Trader.VatExemption.Value)
            {
                vatExemption = true;
            }
            string country = "GR";
            if (invoice.Trader != null && !string.IsNullOrEmpty(invoice.Trader.Country))
            {
                country = invoice.Trader.Country;
            }
            else
            {
                if(invoice.Trader != null && !char.IsDigit(invoice.Trader.TaxRegistrationNumber[0]))
                {
                    country = "";
                }
            }
            bool isGreece = false;
            bool isEu = false;
            if (country == "GR")
                isGreece = true;
            else
            {
                //vatExemption = true;
                isGreece = false;
                isEu = Data.Implementation.Country.IsEuCountry(country);
            }

            inv.Issuer = new Exedron.MyData.InvoiceModels.Issuer();
            inv.Issuer.Country = "GR";
            inv.Issuer.VATNumber = vatNumber;
            inv.Issuer.Branch = Data.Implementation.OptionHandler.Instance.GetIntOption("CompanyBranch", 0);
            bool isDelivery = (new int[] { 158, 159 }).Contains(invoice.InvoiceType.OfficialEnumerator);
            if (!isRetail || isDelivery)
            {

                //country = invoice.Trader.Country;
                inv.CounterPart = new Exedron.MyData.InvoiceModels.CouterPart();
                inv.CounterPart.Country = country;
                inv.CounterPart.VATNumber = invoice.Trader.TaxRegistrationNumber.Trim();
                if(!isGreece || isDelivery)
                {
                    Data.CompanyData company = new Data.CompanyData();
                    inv.Issuer.KeepName = isDelivery;
                    inv.Issuer.Name = company.CompanyName;
                    inv.Issuer.Address = new Exedron.MyData.InvoiceModels.Address();
                    inv.Issuer.Address.City = company.CompanyCity;
                    inv.Issuer.Address.PostalCode = company.CompanyPostalCode;
                    inv.Issuer.Address.Street = company.CompanyAddress.Replace("&", "&amp;");
                    ((Exedron.MyData.InvoiceModels.Address)inv.Issuer.Address).ReplaceNumber();

                    inv.CounterPart.KeepName = isDelivery;
                    inv.CounterPart.Name = invoice.Trader.Name;
                    inv.CounterPart.Address = new Exedron.MyData.InvoiceModels.Address();
                    inv.CounterPart.Address.City = invoice.Trader.City;
                    inv.CounterPart.Address.PostalCode = invoice.Trader.ZipCode;
                    inv.CounterPart.Address.Street = invoice.Trader.Address.Replace("&", "&amp;");
                    ((Exedron.MyData.InvoiceModels.Address)inv.CounterPart.Address).ReplaceNumber();
                }
            }
            Exedron.MyData.InvoiceModels.InvoiceHeader invHeader = new Exedron.MyData.InvoiceModels.InvoiceHeader();

            if (isDelivery)
            {
                invHeader.OtherDeliveryNoteHeader = new Exedron.MyData.InvoiceModels.OtherDeliveryNoteHeader();
                invHeader.OtherDeliveryNoteHeader.LoadingAddress = inv.Issuer.Address;
                invHeader.OtherDeliveryNoteHeader.DeliveryAddress = inv.CounterPart.Address;
            }

            invHeader.AA = invoice.Number.ToString();
            invHeader.Currency = "EUR";
            if (isDelivery)
            {
                invHeader.Currency = "";
            }
            invHeader.IssueDate = invoice.TransactionDate;
            if (isCanceling)
            {
                if (isRetail)
                    invHeader.InvoiceType = "11.4";
                else
                {
                    if(corInvoices.Count > 0)
                        invHeader.InvoiceType = "5.1";
                    else
                        invHeader.InvoiceType = "5.2";
                }
            }
            else
            {
                if (isService)
                {
                    if (isRetail)
                        invHeader.InvoiceType = "11.2";
                    else
                        invHeader.InvoiceType = "2.1";
                }
                else
                {
                    if (isDelivery)
                    {
                        invHeader.InvoiceType = "9.3";
                    }
                    else
                    {
                        if (isRetail)
                            invHeader.InvoiceType = "11.1";
                        else
                        {
                            if (inv.CounterPart != null && inv.CounterPart.VATNumber == vatNumber)
                            {
                                invHeader.InvoiceType = "6.1";
                            }
                            else
                            {
                                if (isGreece)
                                    invHeader.InvoiceType = "1.1";
                                else
                                {
                                    if (isEu)
                                        invHeader.InvoiceType = "1.2";
                                    else
                                        invHeader.InvoiceType = "1.3";
                                }
                            }
                        }
                    }
                }
            }
            if (invHeader.InvoiceType == "5.1" && corInvoices != null && corInvoices.Count > 0)
                invHeader.CorrelatedInvoices = corInvoices.ToArray();

            if (invoice.Vehicle != null)
            {
                if (invoice.Vehicle.PlateNumber != null && invoice.Vehicle.PlateNumber.Length > 0)
                {
                    if (invoice.Vehicle.PlateNumber.Length > 6 && invoice.Vehicle.PlateNumber.Length < 15)
                    {
                        if (invoice.Vehicle.PlateNumber.Where(a => char.IsDigit(a)).Count() > 0)
                            invHeader.VehicleNumber = invoice.Vehicle.PlateNumber;
                    }
                }
            }

            invHeader.MovePurpose = Exedron.MyData.Interfaces.MovePurposeEnum.Sales;
            invHeader.Series = string.IsNullOrEmpty(invoice.Series) ? "" : invoice.Series;
            inv.InvoiceHeader = invHeader;

            if (!isDelivery)
            {
                Exedron.MyData.InvoiceModels.PaymentMethodDetails payment = new Exedron.MyData.InvoiceModels.PaymentMethodDetails();
                payment.Amount = invoice.TotalAmount.Value;
                if (invoice.PaymentType.HasValue && invoice.PaymentType.Value == 0)
                    payment.Type = Exedron.MyData.Interfaces.PaymentMethodEnum.Credit;
                else if (invoice.PaymentType.HasValue && invoice.PaymentType.Value == 1)
                    payment.Type = Exedron.MyData.Interfaces.PaymentMethodEnum.Cash;
                else
                    payment.Type = Exedron.MyData.Interfaces.PaymentMethodEnum.Cash;
                inv.PaymentMethods = new Exedron.MyData.Interfaces.IPaymentMethodDetailType[] { payment };
            }
            Exedron.MyData.InvoiceModels.InvoiceSummary summary = new Exedron.MyData.InvoiceModels.InvoiceSummary();
            if (!isDelivery)
            {
                summary.TotalGrossValue = invoice.TotalAmount.Value;
                summary.TotalNetValue = invoice.TotalAmount.Value - invoice.VatAmount.Value;// invoice.NettoAmount.Value;
                summary.TotalVATAmount = invoice.VatAmount.Value;
            }
            else
            {
                summary.TotalGrossValue = 0;
                summary.TotalNetValue = 0;
                summary.TotalVATAmount = 0;
            }
            inv.InvoiceSummary = summary;
            

            List<Exedron.MyData.InvoiceModels.InvoiceRow> rows = new List<Exedron.MyData.InvoiceModels.InvoiceRow>();
            foreach (var line in invoceItems)
            {
                Exedron.MyData.InvoiceModels.InvoiceRow row1 = new Exedron.MyData.InvoiceModels.InvoiceRow();
                if(vatExemption && isGreece)
                {
                    if (line.VatPercentage == 0)
                    {
                        row1.VATExemptionCategory = Exedron.MyData.Interfaces.VATExemptionCategoryEnum.Article27;
                        invHeader.VATPaymentSuspension = true;
                    }
                }
                row1.LineNumber = rows.Count + 1;
                if (isService)
                {
                    row1.MeasurementUnit = Exedron.MyData.Interfaces.MeasurementUnitEnum.None;
                    row1.Quantity = 0;
                }
                else
                {
                    row1.MeasurementUnit = Exedron.MyData.Interfaces.MeasurementUnitEnum.Items;
                    row1.Quantity = line.Volume;
                }
                if (!isDelivery)
                {
                    row1.NetValue = line.TotalPrice - line.VatAmount;
                    row1.VATAmount = line.VatAmount;
                    row1.VATCategory = Exedron.MyData.RequestHelpers.GetVATCategory(line.VatPercentage);
                }
                else
                {
                    row1.NetValue = 0;
                    row1.VATAmount = 0;
                    row1.VATCategory = Exedron.MyData.Interfaces.VATCategoryEnum.NoVATEntry;
                }
                if (!isGreece && row1.VATAmount == 0)
                {
                    row1.VATCategory = Exedron.MyData.Interfaces.VATCategoryEnum.NoVAT;
                    if(isEu)
                        row1.VATExemptionCategory = Exedron.MyData.Interfaces.VATExemptionCategoryEnum.Article28;
                    else
                        row1.VATExemptionCategory = Exedron.MyData.Interfaces.VATExemptionCategoryEnum.Article24;
                }
                row1.IncomeClassification = new Exedron.MyData.InvoiceModels.IncomeClassification();
                row1.IncomeClassification.Amount = row1.NetValue;
                if (isDelivery)
                {
                    row1.ItemDescription = line.FuelType.Name;
                }

                if (inv.CounterPart != null && inv.CounterPart.VATNumber == vatNumber)
                {
                    row1.IncomeClassification.ClassificationCategory = "category1_6";
                    row1.IncomeClassification.ClassificationType = "E3_595";
                }
                else
                {
                    if (isService)
                        row1.IncomeClassification.ClassificationCategory = "category1_3";
                    else
                    {
                        if(isDelivery)
                        {
                            row1.IncomeClassification.ClassificationCategory = "category3";
                        }
                        else
                            row1.IncomeClassification.ClassificationCategory = "category1_1";
                    }
                    if (isDelivery)
                    {
                        row1.IncomeClassification.ClassificationType = "";
                    }
                    else
                    {
                        if (isRetail)
                            row1.IncomeClassification.ClassificationType = "E3_561_003";
                        else
                        {
                            if (isGreece)
                                row1.IncomeClassification.ClassificationType = "E3_561_001";
                            else if (isEu)
                                row1.IncomeClassification.ClassificationType = "E3_561_005";
                            else
                                row1.IncomeClassification.ClassificationType = "E3_561_006";
                        }
                    }
                }
                rows.Add(row1);
            }
            inv.InvoiceDetails = rows.ToArray();
            errorMessage = "";
            return inv;
        }
    }

    internal class FileTypeInfo
    {
        string fileType = "";

        public string FileType
        {
            set
            {
                this.fileType = value;
                switch(fileType)
                {
                    case "Alert":
                        this.ReportName = "AlertReport";
                        break;
                    case "Invoice":
                        this.ReportName = "InvoiceReport";
                        break;
                    case "Balance":
                        this.ReportName = "BalanceReport";
                        break;
                    case "TankFilling":
                        this.ReportName = "TankFillingReport";
                        break;
                    case "Titrimetry":
                        this.ReportName = "TitrimetryReport";
                        break;
                }
            }
            get
            {
                return this.fileType;
            }

        }

        public Guid Id
        {
            set;
            get;
        }
        public string ReportName
        {
            private set;
            get;
        }
    }
}
