using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ASFuelControl.Logging;

namespace ASFuelControl.Windows.Threads
{
    public class PrintAgent
    {
        #region private variables

        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private bool agentRunning = false;
        private string outFolder = "";
        private string signFolder = "";
        private string processFolder = "";
        private bool windowsPrint = false;
        private bool printBarCode = false;
        private string invoiceSignLine = "";
        private string taxNumber = "";
        private int tailInvLines = 0;
        private string defaultTaxDevice = "";
        private System.Threading.Thread th;
        private Dictionary<string, List<Guid>> entriesProcessed = new Dictionary<string, List<Guid>>();
        private List<string> invoicesProcessed = new List<string>();
        private List<string> fillingsProcessed = new List<string>();
        private Guid incomeInvoiceType;
        private Guid litercheckInvoiceType;
        private Guid income2InvoiceType;
        private List<string> filesInProcess = new List<string>();

        private bool printPhysical = false; 

        #endregion

        public PrintAgent()
        {
            this.defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
            if (this.defaultTaxDevice == "Samtec")
            {
                this.signFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_SignFolder");
                this.outFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_OutFolder");
            }
            else if (this.defaultTaxDevice == "SignA")
            {
                this.signFolder = Data.Implementation.OptionHandler.Instance.GetOption("SignA_SignFolder");
                this.outFolder = this.signFolder;
            }
            this.processFolder = this.signFolder + "\\" + "ProcessDocuments";
            if (!Directory.Exists(this.processFolder))
                Directory.CreateDirectory(this.processFolder);
            this.tailInvLines = Data.Implementation.OptionHandler.Instance.GetIntOption("TailInvoiceLine", 5);
            this.invoiceSignLine = Data.Implementation.OptionHandler.Instance.GetOption("SingLine");
            this.windowsPrint = Data.Implementation.OptionHandler.Instance.GetBoolOption("WindowsInvoicePrint", false);
            this.printBarCode = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintInvoiceBarcode", false);
            this.litercheckInvoiceType = Data.Implementation.OptionHandler.Instance.GetGuidOption("3f54a35b-01f6-43b8-a17e-2d4927e319b8", Guid.Empty);
            this.printPhysical = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintOnPrinter", false);
        }

        #region public methods

        public void StartThread()
        {
            this.database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            this.agentRunning = true;
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.Start();
        }

        public void StopThread()
        {
            this.agentRunning = false;
        }

        #endregion

        #region static methods

        public static void PrintInvoiceDirect(Data.Invoice invoice)
        {
            bool windowsPrint = Data.Implementation.OptionHandler.Instance.GetBoolOption("WindowsInvoicePrint", false);
            bool printBarCode = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintInvoiceBarcode", false);
            int tailInvLines = Data.Implementation.OptionHandler.Instance.GetIntOption("TailInvoiceLine", 5);
            if (windowsPrint)
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Reports.InvoiceThermal report = new Reports.InvoiceThermal();
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoicePrintViews");
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
                System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                printerSettings.PrinterName = invoice.Printer;
                Telerik.Reporting.Processing.ReportProcessor.Print(report, printerSettings);
            }
            else
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

                //DirectPrinter.SendToPrinter("ASFuelControl Invoice", printText, invoice.Printer);
                DirectPrinter.SendToPrinter("ASFuelControl Invoice", printText + "\r\n" + COMMAND, invoice.Printer);

                System.Diagnostics.Trace.WriteLine("Invoice Printed: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.ffff"));
            }
        }

        private static string[] CreateInvoiceTextDirect(Data.Invoice invoice)
        {
            int printerLineWidth = 44;

            string address = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            string city = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            string fax = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
            string name = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            string occupation = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
            string phone = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
            string postalCode = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode");
            string taxOffice = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
            string tin = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            string invoiceType = invoice.InvoiceType.Description;
            List<string> invoiceLines = new List<string>();
            invoiceLines.Add(name.CenterString(printerLineWidth));
            invoiceLines.Add(occupation.CenterString(printerLineWidth));
            invoiceLines.Add(address.CenterString(printerLineWidth));
            string taxData = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", tin, taxOffice).EquilizeString(printerLineWidth);
            string contactData = string.Format("ΤΗΛ:{0}#FAX:{1}", phone, fax).EquilizeString(printerLineWidth);
            string dateInfo = string.Format("Ημερ/νία:{0}#Ώρα:{1}", invoice.TransactionDate.ToString("dd/MM/yyyy"), invoice.TransactionDate.ToString("HH:mm")).EquilizeString(printerLineWidth);

            if (invoiceType.Length > 36)
                invoiceType = invoiceType.Substring(0, 36);

            string invoiceData = string.Format("{0}#{1}", invoiceType, invoice.Number).EquilizeString(printerLineWidth);
            invoiceLines.Add(taxData.CenterString(printerLineWidth));
            invoiceLines.Add(contactData.CenterString(printerLineWidth));
            invoiceLines.Add("");
            invoiceLines.Add(invoiceData.CenterString(printerLineWidth));
            invoiceLines.Add(dateInfo.CenterString(printerLineWidth));
            string border1 = "--------------------------------------------";
            invoiceLines.Add(border1.CenterString(printerLineWidth));
            if (invoice.Vehicle == null)
            {
                invoiceLines.Add(("ΠΕΛΑΤΗΣ ΛΙΑΝΙΚΗΣ").CenterString(printerLineWidth));
            }
            else
            {
                string traderdata1 = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", invoice.Trader.TaxRegistrationNumber, invoice.Trader.TaxRegistrationOffice).EquilizeString(printerLineWidth);
                string traderdata2 = string.Format("Πελάτης:{0}", invoice.Trader.Name);
                string traderdata3 = string.Format("Οχημα  :{0}", invoice.Vehicle.PlateNumber);

                invoiceLines.Add(traderdata2);
                invoiceLines.Add(traderdata3);
                invoiceLines.Add(traderdata1.CenterString(printerLineWidth));
            }
            invoiceLines.Add(border1.CenterString(printerLineWidth));

            string infoData = "Προς Πώληση#Μετρητοίς".EquilizeString(printerLineWidth);
            string border = "============================================";



            invoiceLines.Add(infoData.CenterString(printerLineWidth));
            invoiceLines.Add(border.CenterString(printerLineWidth));
            string headerData = ("ΕΙΔΟΣ").CenterString(18) + "ΠΟΣΟΤ.".CenterString(7) + "ΤΙΜΗ".CenterString(6) + "ΑΞΙΑ".CenterString(7) + "Φ.Π.Α.".CenterString(6);
            invoiceLines.Add(headerData.CenterString(printerLineWidth));

            foreach (Data.InvoiceLine invLine in invoice.InvoiceLines)
            {
                invoiceLines.Add(border1.CenterString(printerLineWidth));

                string details = invLine.FuelType.Name.LeftString(18) + invLine.Volume.ToString("N2").CenterString(7) + invLine.UnitPrice.ToString("N3").CenterString(6) + invLine.TotalPrice.ToString("N2").CenterString(7) + invLine.VatAmount.ToString("N2").CenterString(6);
                invoiceLines.Add(details.CenterString(printerLineWidth));

                string salePoint = string.Format("Αντλία :{0} Ακροσωλήνιο :{1}", invLine.SalesTransaction.Nozzle.Dispenser.OfficialPumpNumber, invLine.SalesTransaction.Nozzle.OfficialNozzleNumber);
                invoiceLines.Add(salePoint);

            }
            invoiceLines.Add(border.CenterString(printerLineWidth));
            string quantitySum = string.Format("          Σύνολο Ποσοτήτων:#{0:N2}", invoice.InvoiceLines.Sum(il => il.Volume)).EquilizeString(printerLineWidth);
            string amountSum = string.Format("          Σύνολο Απόδειξης:#{0:N2}", invoice.TotalAmount).EquilizeString(printerLineWidth);
            invoiceLines.Add(" ");
            string info2String = string.Format("Οι τιμές περιλαμβάνουν Φ.Π.Α.", invoice.TotalAmount);
            invoiceLines.Add(quantitySum.RightString(printerLineWidth));
            invoiceLines.Add(amountSum.RightString(printerLineWidth));
            invoiceLines.Add(" ");
            invoiceLines.Add(info2String.CenterString(printerLineWidth));
            invoiceLines.Add("");
            string info3String = string.Format("ΕΥΧΑΡΙΣΤΟΥΜΕ ΓΙΑ ΤΗΝ ΠΡΟΤΙΜΗΣΗΣ ΣΑΣ");
            invoiceLines.Add(info3String.CenterString(printerLineWidth));


            for (int i = 0; i < invoiceLines.Count; i++)
            {
                string str = invoiceLines[i];
                if (str.Contains("#"))
                    invoiceLines[i] = str.EquilizeString(printerLineWidth);
            }

            return invoiceLines.ToArray();
        }

        #endregion

        #region Help Methods

        private string CreateSignLine(Data.Invoice invoice)
        {
            string str = this.invoiceSignLine;
            str = str.Replace("[AFM]", this.taxNumber);
            if (invoice.Trader != null)
                str = str.Replace("[CustomerAfm]", invoice.Trader.TaxRegistrationNumber);
            else
                str = str.Replace("[CustomerAfm]", "");

            str = str.Replace("[Date]", invoice.TransactionDate.ToString("ddMMyyyyHHmm"));
            str = str.Replace("[Description]", invoice.InvoiceType.OfficialEnumerator.ToString());
            str = str.Replace("[Series]", "");
            str = str.Replace("[InvoiceNumber]", invoice.Number.ToString());
            str = str.Replace("[AmountA]", "0.00");
            str = str.Replace("[AmountB]", "0.00");
            str = str.Replace("[AmountC]", invoice.NettoAmount.Value.ToString("N2").Replace(",", "."));
            str = str.Replace("[AmountD]", "0.00");
            str = str.Replace("[AmountE]", "0.00");
            str = str.Replace("[TaxA]", "0.00");
            str = str.Replace("[TaxB]", "0.00");
            str = str.Replace("[TaxC]", invoice.VatAmount.Value.ToString("N2").Replace(",", "."));
            str = str.Replace("[TaxD]", "0.00");
            str = str.Replace("[SUM]", invoice.TotalAmount.Value.ToString("N2").Replace(",", "."));
            str = str.Replace("[Currancy]", "1");
            return str;
        }

        private string[] CreateInvoiceText(Data.Invoice invoice)
        {
            int printerLineWidth = 44;

            string address = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            string city = Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            string fax = Data.Implementation.OptionHandler.Instance.GetOption("CompanyFax");
            string name = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            string occupation = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
            string phone = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
            string postalCode = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode");
            string taxOffice = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
            string tin = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            string invoiceType = invoice.InvoiceType.Description;
            List<string> invoiceLines = new List<string>();
            invoiceLines.Add(name.CenterString(printerLineWidth));
            invoiceLines.Add(occupation.CenterString(printerLineWidth));
            invoiceLines.Add(address.CenterString(printerLineWidth));
            string taxData = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", tin, taxOffice).EquilizeString(printerLineWidth);
            string contactData = string.Format("ΤΗΛ:{0}#FAX:{1}", phone, fax).EquilizeString(printerLineWidth);
            string dateInfo = string.Format("Ημερ/νία:{0}#Ώρα:{1}", invoice.TransactionDate.ToString("dd/MM/yyyy"), invoice.TransactionDate.ToString("HH:mm")).EquilizeString(printerLineWidth);

            if (invoiceType.Length > 36)
                invoiceType = invoiceType.Substring(0, 36);

            string invoiceData = string.Format("{0}#{1}", invoiceType, invoice.Number).EquilizeString(printerLineWidth);
            invoiceLines.Add(taxData.CenterString(printerLineWidth));
            invoiceLines.Add(contactData.CenterString(printerLineWidth));
            invoiceLines.Add("");
            invoiceLines.Add(invoiceData.CenterString(printerLineWidth));
            invoiceLines.Add(dateInfo.CenterString(printerLineWidth));
            string border1 = "--------------------------------------------";
            invoiceLines.Add(border1.CenterString(printerLineWidth));
            if (invoice.Vehicle == null)
            {
                invoiceLines.Add(("ΠΕΛΑΤΗΣ ΛΙΑΝΙΚΗΣ").CenterString(printerLineWidth));
            }
            else
            {
                string traderdata1 = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", invoice.Trader.TaxRegistrationNumber, invoice.Trader.TaxRegistrationOffice).EquilizeString(printerLineWidth);
                string traderdata2 = string.Format("Πελάτης:{0}", invoice.Trader.Name);
                string traderdata3 = string.Format("Οχημα  :{0}", invoice.Vehicle.PlateNumber);

                invoiceLines.Add(traderdata2);
                invoiceLines.Add(traderdata3);
                invoiceLines.Add(traderdata1.CenterString(printerLineWidth));
            }
            invoiceLines.Add(border1.CenterString(printerLineWidth));

            string infoData = "Προς Πώληση#Μετρητοίς".EquilizeString(printerLineWidth);
            string border = "============================================";



            invoiceLines.Add(infoData.CenterString(printerLineWidth));
            invoiceLines.Add(border.CenterString(printerLineWidth));
            string headerData = ("ΕΙΔΟΣ").CenterString(18) + "ΠΟΣΟΤ.".CenterString(7) + "ΤΙΜΗ".CenterString(6) + "ΑΞΙΑ".CenterString(7) + "Φ.Π.Α.".CenterString(6);
            invoiceLines.Add(headerData.CenterString(printerLineWidth));

            foreach (Data.InvoiceLine invLine in invoice.InvoiceLines)
            {
                invoiceLines.Add(border1.CenterString(printerLineWidth));

                string details = invLine.FuelType.Name.LeftString(18) + invLine.Volume.ToString("N2").CenterString(7) + invLine.UnitPrice.ToString("N3").CenterString(6) + invLine.TotalPrice.ToString("N2").CenterString(7) + invLine.VatAmount.ToString("N2").CenterString(6);
                invoiceLines.Add(details.CenterString(printerLineWidth));

                string salePoint = string.Format("Αντλία :{0} Ακροσωλήνιο :{1}", invLine.SalesTransaction.Nozzle.Dispenser.OfficialPumpNumber, invLine.SalesTransaction.Nozzle.OfficialNozzleNumber);
                invoiceLines.Add(salePoint);

            }
            invoiceLines.Add(border.CenterString(printerLineWidth));
            string quantitySum = string.Format("          Σύνολο Ποσοτήτων:#{0:N2}", invoice.InvoiceLines.Sum(il => il.Volume)).EquilizeString(printerLineWidth);
            string amountSum = string.Format("          Σύνολο Απόδειξης:#{0:N2}", invoice.TotalAmount).EquilizeString(printerLineWidth);
            invoiceLines.Add(" ");
            string info2String = string.Format("Οι τιμές περιλαμβάνουν Φ.Π.Α.", invoice.TotalAmount);
            invoiceLines.Add(quantitySum.RightString(printerLineWidth));
            invoiceLines.Add(amountSum.RightString(printerLineWidth));
            invoiceLines.Add(" ");
            invoiceLines.Add(info2String.CenterString(printerLineWidth));
            invoiceLines.Add("");
            string info3String = string.Format("ΕΥΧΑΡΙΣΤΟΥΜΕ ΓΙΑ ΤΗΝ ΠΡΟΤΙΜΗΣΗΣ ΣΑΣ");
            invoiceLines.Add(info3String.CenterString(printerLineWidth));


            for (int i = 0; i < invoiceLines.Count; i++)
            {
                string str = invoiceLines[i];
                if (str.Contains("#"))
                    invoiceLines[i] = str.EquilizeString(printerLineWidth);
            }

            return invoiceLines.ToArray();
        }

        private string[] CreateFillingText(Data.TankFilling tankFilling)
        {
            List<string> lines = new List<string>();
            if (tankFilling.LevelEnd > tankFilling.LevelStart)
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
            if (System.IO.File.Exists(fileName))
                return;
            if (this.defaultTaxDevice == "SignA")
            {
                System.IO.File.WriteAllText(fileName, text, Encoding.Default);
                return;
            }
            else if (this.defaultTaxDevice == "Samtec")
            {
                System.IO.File.WriteAllText(fileName, text, Encoding.Default);
                return;
            }
            return;
        }

        #endregion

        private void ThreadRun()
        {
            try
            {

                if (entriesProcessed.Count == 0)
                {
                    entriesProcessed.Add("Balances", new List<Guid>());
                    entriesProcessed.Add("Alerts", new List<Guid>());
                    entriesProcessed.Add("Titrimetries", new List<Guid>());
                }
                if (this.defaultTaxDevice == "Samtec" && this.outFolder == "")
                {
                    return;
                }
                if (this.outFolder != "")
                {
                    System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.outFolder);
                    System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments");
                    System.IO.DirectoryInfo dirInfoAlerts = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments\\Alerts");
                    System.IO.DirectoryInfo dirInfoBalances = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments\\Balances");
                    System.IO.DirectoryInfo dirInfoTitrimetries = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments\\Titrimetries");

                    if (!dirInfo.Exists)
                        dirInfo.Create();
                    if (!dirInfoAlerts.Exists)
                        dirInfoAlerts.Create();
                    if (!dirInfoBalances.Exists)
                        dirInfoBalances.Create();
                    if (!dirInfoTitrimetries.Exists)
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
                try
                {

                    try
                    {
                        this.CheckProcessFolder();
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogToFile("CheckProcessFolder", ex);
                        System.Threading.Thread.Sleep(200);

                        continue;
                    }

                    try
                    {
                        this.CheckForSigns();
                    }
                    catch(Exception ex)
                    {
                        Logger.Instance.LogToFile("CheckForSigns", ex);
                        System.Threading.Thread.Sleep(200);

                        continue;
                    }

                    try
                    {
                        this.CheckAfterException();
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogToFile("CheckAfterException", ex);
                        System.Threading.Thread.Sleep(200);
                        continue;
                    }
                    index++;
                    var qBalances = this.database.Balances.Where(b => !b.PrintDate.HasValue);
                    var qInvoice = this.database.Invoices.Where(i => (!i.IsPrinted.HasValue || i.IsPrinted.Value == false || i.InvoiceSignature == null || i.InvoiceSignature == "") && (i.InvoiceType.Printable));
                    var qAlert = this.database.SystemEvents.Where(s => !s.PrintedDate.HasValue);
                    var qTitration = this.database.Titrimetries.Where(t => !t.PrintDate.HasValue);
                    var qFillings = this.database.TankFillings.Where(t => t.SignSignature == null || t.SignSignature == "");

                    this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qInvoice);
                    List<Data.Invoice> invoices = qInvoice.ToList();

                    List<Data.Balance> balances = new List<Data.Balance>();
                    List<Data.SystemEvent> alerts = new List<Data.SystemEvent>();
                    List<Data.Titrimetry> titriemetries = new List<Data.Titrimetry>();
                    List<Data.TankFilling> tankFillings = new List<Data.TankFilling>();

                    if (index % 10 == 0)
                    {
                        this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qBalances);
                        this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qAlert);
                        this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qTitration);
                        this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, qFillings);
                        balances = qBalances.ToList();
                        alerts = qAlert.ToList();
                        titriemetries = qTitration.ToList();
                        tankFillings = qFillings.ToList();
                        index = 1;
                    }
                    

                    foreach (Data.Invoice invoice in invoices)
                    {
                        try
                        {

                            if (invoice.InvoiceTypeId != this.litercheckInvoiceType && invoice.InvoiceType.IsInternal.HasValue && invoice.InvoiceType.IsInternal.Value && invoice.InvoiceType.Printable)
                            {
                                this.PrintInternalInvoice(invoice);
                            }
                            else
                                this.PrintInvoice(invoice);
                        }
                        catch(Exception ex)
                        {
                            Logging.Logger.Instance.LogToFile("Σφάλμα Εκτύπωσης Παραστατικού", ex);
                        }
                    }
                    foreach (Data.Balance balance in balances)
                    {
                        try
                        {

                            this.PrintBalance(balance);
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
                            this.PrintAlert(alert);
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
                            this.PrintTitrimetry(titrimetry);
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
                            this.PrintTankFilling(tankFilling);
                        }
                        catch (Exception ex)
                        {
                            Logging.Logger.Instance.LogToFile("Σφάλμα Εκτύπωσης Παραλαβής / Εξαγωγής", ex);
                        }
                    }
                }
                catch(Exception ex2)
                {
                    Logger.Instance.LogToFile("ThreadRun", ex2);
                }
                System.GC.Collect();
                System.Threading.Thread.Sleep(500);
            }
        }

        #region Check Methods

        private void CheckAfterException()
        {
            foreach (string key in this.entriesProcessed.Keys)
            {
                List<Guid> toRemove = new List<Guid>();
                foreach (Guid id in this.entriesProcessed[key])
                {
                    try
                    {
                        if (key == "Balances")
                        {
                            Data.Balance entry = this.database.Balances.Where(e => e.BalanceId == id).FirstOrDefault();
                            if (entry == null)
                                continue;
                            entry.PrintDate = DateTime.Now;
                            this.database.SaveChanges();
                        }
                        else if (key == "Alerts")
                        {
                            Data.SystemEvent entry = this.database.SystemEvents.Where(e => e.EventId == id).FirstOrDefault();
                            if (entry == null)
                                continue;
                            entry.PrintedDate = DateTime.Now;
                            this.database.SaveChanges();
                        }
                        else if (key == "Titrimetries")
                        {
                            Data.Titrimetry entry = this.database.Titrimetries.Where(e => e.TitrimetryId == id).FirstOrDefault();
                            if (entry == null)
                                continue;
                            entry.PrintDate = DateTime.Now;
                            this.database.SaveChanges();
                        }
                        toRemove.Add(id);
                    }
                    catch
                    {
                    }
                }
                foreach (Guid id in toRemove)
                {
                    this.entriesProcessed[key].Remove(id);
                }
            }
            List<string> toRemoveInvs = new List<string>();
            foreach (string key in this.invoicesProcessed)
            {
                try
                {
                    string[] keys = key.Split(new string[] { "$$" }, StringSplitOptions.RemoveEmptyEntries);
                    if (keys.Length != 2)
                        continue;
                    Guid id = Guid.Parse(keys[0]);
                    Data.Invoice inv = this.database.Invoices.Where(i => i.InvoiceId == id).FirstOrDefault();
                    if (inv == null)
                        continue;
                    inv.IsPrinted = true;
                    inv.InvoiceSignature = keys[1];
                    this.database.SaveChanges();
                    toRemoveInvs.Add(key);
                }
                catch
                {
                }
            }
            foreach (string key in this.fillingsProcessed)
            {
                try
                {
                    string[] keys = key.Split(new string[] { "$$" }, StringSplitOptions.RemoveEmptyEntries);
                    if (keys.Length != 2)
                        continue;
                    Guid id = Guid.Parse(keys[0]);
                    Data.TankFilling inv = this.database.TankFillings.Where(i => i.TankFillingId == id).FirstOrDefault();
                    if (inv == null)
                        continue;
                    
                    inv.SignSignature = keys[1];
                    this.database.SaveChanges();
                    toRemoveInvs.Add(key);
                }
                catch
                {
                }
            }
            foreach (string key in toRemoveInvs)
            {
                if(this.invoicesProcessed.Contains(key))
                    invoicesProcessed.Remove(key);
                if (this.fillingsProcessed.Contains(key))
                    fillingsProcessed.Remove(key);
            }
        }

        private void CheckForSigns()
        {
            if (outFolder != null && outFolder != "")
            {
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.outFolder);
                if (!dir.Exists)
                    return;
                System.IO.FileInfo[] files = dir.GetFiles("*.out");
                foreach (System.IO.FileInfo file in files)
                {
                    try
                    {
                        FileTypeInfo ft = this.GetFileTypeInfo(file.Name, "out");

                        string str = System.IO.File.ReadAllText(file.FullName);

                        this.EvaluateSign(file.FullName, ft.Id, ft.FileType);

                        if (this.filesInProcess.Contains(file.Name.Replace(".out", ".txt")))
                            this.filesInProcess.Remove(file.Name.Replace(".out", ".txt"));
                        //int signIndex = str.IndexOf(",");
                        //if (signIndex < 0)
                        //    continue;

                        //str = str.Substring(7, signIndex - 6);
                        //if (str == null || str == "")
                        //{
                        //    file.Delete();

                        //    if (file.Name.StartsWith("Alert_"))
                        //    {
                        //    }
                        //    else if (file.Name.StartsWith("Balance_"))
                        //    {
                        //    }
                        //    else if (file.Name.StartsWith("Titrimetry_"))
                        //    {
                        //    }
                        //    else if (file.Name.StartsWith("TankFilling_"))
                        //    {
                        //    }
                        //    else
                        //    {
                        //        string id = file.Name.Replace(".out", "");
                        //        if(this.invoicesProcessed.Contains(id))
                        //            this.invoicesProcessed.Remove(id);
                        //    }

                        //    continue;
                        //}

                        //if (file.Name.StartsWith("Alert_"))
                        //{
                        //    string id = file.Name.Replace("Alert_", "").Replace(".out", "");
                        //    Data.SystemEvent alert = this.database.SystemEvents.Where(s => s.EventId.ToString().ToLower() == id.ToLower()).FirstOrDefault();
                        //    if (alert == null)
                        //        continue;
                        //    alert.DocumentSign = str;
                        //    alert.PrintedDate = DateTime.Now;
                        //    try
                        //    {
                        //        this.database.SaveChanges();
                        //    }
                        //    catch
                        //    {
                        //        this.entriesProcessed["Alerts"].Add(alert.EventId);
                        //    }
                        //    PrintToPrinter(alert);
                        //}
                        //else if (file.Name.StartsWith("Balance_"))
                        //{
                        //    string id = file.Name.Replace("Balance_", "").Replace(".out", "");
                        //    Data.Balance balance = this.database.Balances.Where(s => s.BalanceId.ToString().ToLower() == id.ToLower()).FirstOrDefault();
                        //    if (balance == null)
                        //        continue;
                        //    balance.DocumentSign = str;
                        //    balance.PrintDate = DateTime.Now;
                        //    try
                        //    {
                        //        this.database.SaveChanges();
                        //    }
                        //    catch
                        //    {
                        //        this.entriesProcessed["Balances"].Add(balance.BalanceId);
                        //    }
                        //    this.PrintToPrinter(balance);

                        //}
                        //else if (file.Name.StartsWith("Titrimetry_"))
                        //{
                        //    string id = file.Name.Replace("Titrimetry_", "").Replace(".out", "");
                        //    Data.Titrimetry titrimetry = this.database.Titrimetries.Where(s => s.TitrimetryId.ToString().ToLower() == id.ToLower()).FirstOrDefault();
                        //    if (titrimetry == null)
                        //        continue;
                        //    titrimetry.DocumentSign = str;
                        //    titrimetry.PrintDate = DateTime.Now;
                        //    try
                        //    {
                        //        this.database.SaveChanges();
                        //    }
                        //    catch
                        //    {
                        //        this.entriesProcessed["Titrimetries"].Add(titrimetry.TitrimetryId);
                        //    }
                        //    this.PrintToPrinter(titrimetry);
                        //}
                        //else if (file.Name.StartsWith("TankFilling_"))
                        //{
                        //    string id = file.Name.Replace(".out", "").Replace("TankFilling_", "");
                        //    Data.TankFilling tankFilling = this.database.TankFillings.Where(s => s.TankFillingId.ToString().ToLower() == id.ToLower()).FirstOrDefault();
                        //    if (tankFilling == null)
                        //        continue;
                        //    tankFilling.SignSignature = str;
                        //    try
                        //    {
                        //        this.database.SaveChanges();
                        //    }
                        //    catch
                        //    {
                        //        this.fillingsProcessed.Add(tankFilling.TankFillingId.ToString() + "$$" + str);
                        //    }
                        //    this.PrintToPrinter(tankFilling);
                        //}
                        //else
                        //{
                        //    string id = file.Name.Replace(".out", "");
                        //    Data.Invoice invoice = this.database.Invoices.Where(s => s.InvoiceId.ToString().ToLower() == id.ToLower()).FirstOrDefault();
                        //    if (invoice == null)
                        //        continue;
                        //    invoice.InvoiceSignature = str;
                        //    invoice.IsPrinted = true;
                        //    try
                        //    {
                        //        this.database.SaveChanges();
                        //        if(this.invoicesProcessed.Contains(invoice.InvoiceId.ToString()))
                        //            this.invoicesProcessed.Remove(invoice.InvoiceId.ToString());
                        //    }
                        //    catch
                        //    {
                        //        //this.invoicesProcessed.Add(invoice.InvoiceId.ToString() + "$$" + str);
                        //    }
                        //    this.PrintToPrinter(invoice);
                        //}
                        file.Delete();
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void CheckProcessFolder()
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.processFolder);
            if (!dir.Exists)
                return;
            System.IO.FileInfo[] files = dir.GetFiles("*.xls");
            foreach (FileInfo file in files)
            {
                FileTypeInfo ft = GetFileTypeInfo(file.Name, "xls");


                string fileName = file.Name.Replace(".xls", ".txt");
                if (this.filesInProcess.Contains(fileName))
                    continue;

                string text = ExcelTextStripper.GetText(file.FullName, ft.ReportName);


                if (this.defaultTaxDevice != "Samtec")
                {
                    System.IO.File.WriteAllText(this.signFolder + "\\" + fileName, text, Encoding.Default);
                    FileTypeInfo fType = this.GetFileTypeInfo(fileName, "txt");
                    this.ApplySign("Sign A", fType.Id, fType.FileType);
                }
                else
                {
                    System.IO.File.WriteAllText(this.signFolder + "\\" + fileName, text, Encoding.Default);
                    filesInProcess.Add(file.Name.Replace(".xls", ".txt"));
                }

                string appFolder = System.Environment.CurrentDirectory + "\\" + ft.FileType;
                if(!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);
                if (File.Exists(appFolder + "\\" + file.Name))
                {
                    File.Delete(appFolder + "\\" + file.Name);
                    System.Threading.Thread.Sleep(500);
                }
                File.Move(file.FullName, appFolder + "\\" + file.Name);
            }
            if (files.Length > 0)
                System.Threading.Thread.Sleep(1000);
        }

        private void ApplySign(string sign, Guid id, string fileType)
        {
            switch (fileType)
            {
                case "Balance":
                    Data.Balance balance = this.database.Balances.Where(b => b.BalanceId == id).FirstOrDefault();
                    if (balance != null)
                    {
                        balance.DocumentSign = sign;
                        balance.PrintDate = DateTime.Now;
                        this.database.SaveChanges();
                        this.PrintToPrinter(balance);
                    }
                    break;
                case "Alert":
                    Data.SystemEvent alert = this.database.SystemEvents.Where(b => b.EventId == id).FirstOrDefault();
                    if (alert != null)
                    {
                        alert.DocumentSign = sign;
                        alert.PrintedDate = DateTime.Now;
                        this.database.SaveChanges();
                        this.PrintToPrinter(alert);
                    }
                    break;
                case "TankFilling":
                    Data.TankFilling delicery = this.database.TankFillings.Where(b => b.TankFillingId == id).FirstOrDefault();
                    if (delicery != null)
                    {
                        delicery.SignSignature = sign;
                        this.database.SaveChanges();
                        this.PrintToPrinter(delicery);
                    }
                    break;
                case "Titrimetry":
                    Data.Titrimetry titrimetry = this.database.Titrimetries.Where(b => b.TitrimetryId == id).FirstOrDefault();
                    if (titrimetry != null)
                    {
                        titrimetry.DocumentSign = sign;
                        titrimetry.PrintDate = DateTime.Now;
                        this.database.SaveChanges();
                        this.PrintToPrinter(titrimetry);
                    }
                    break;
                    break;
                case "Invoice":
                    Data.Invoice invoice = this.database.Invoices.Where(b => b.InvoiceId == id).FirstOrDefault();
                    if (invoice != null)
                    {
                        invoice.InvoiceSignature = sign;
                        invoice.IsPrinted = true;
                        this.database.SaveChanges();
                        if (invoice.InvoiceLines[0].SaleTransactionId.HasValue)
                            this.PrintToPrinter(invoice);
                        else
                            this.PrintToPrinterInternalInvoice(invoice);
                    }
                    break;
            }
        }

        private void EvaluateSign(string fileName, Guid id, string fileType)
        {
            string text = File.ReadAllText(fileName, Encoding.GetEncoding(28597));
            if (text.Contains("Error"))
            {
                if(invoicesProcessed.Contains(id.ToString()))
                    this.invoicesProcessed.Remove(id.ToString());
                return;
            }
            string sign = "";
            if (text.Contains("ΔΦΣΣ"))
            {
                text = text.Replace("ΔΦΣΣ", "").Replace("[[", "").Replace("]]", "");
                string[] vals = text.Split(',');
                sign = vals[0];
            }
            if (sign == "")
                return;
            ApplySign(sign, id, fileType);
            
        }

        private FileTypeInfo GetFileTypeInfo(string fileName, string extension)
        {
            string name = fileName.Replace("." + extension, "");
            string[] parms = name.Split('_');
            Guid id = Guid.Empty;
            string fileType = "Invoice";

            if (parms.Length == 1)
            {
                id = Guid.Parse(parms[0]);
            }
            else
            {
                fileType = parms[0];
                id = Guid.Parse(parms[1]);
            }
            FileTypeInfo ft = new FileTypeInfo() { FileType = fileType, Id = id };
            return ft;
        }

        #endregion

        #region Print Methods for Samtec

        private void PrintToPrinter(Data.SystemEvent alert)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.AlertReport report = new Reports.AlertReport();
            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "SystemEvents");
            report.ReportParameters[0].Value = alert.EventId.ToString().ToLower();

            if (this.printPhysical)
            {
                System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                Telerik.Reporting.Processing.ReportProcessor.Print(report, printerSettings);
            }
            string alertDirectory = System.Environment.CurrentDirectory + "\\Συναγερμοί";
            if (!System.IO.Directory.Exists(alertDirectory))
            {
                System.IO.Directory.CreateDirectory(alertDirectory);
            }
            string fileName = alertDirectory + "\\" + string.Format("Alert_{0:yyyyMMdd_HHmmssfff}.pdf", DateTime.Now);
            this.SaveReport(report, fileName);
        }

        private void PrintToPrinter(Data.Titrimetry titrimetry)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.TitrimetryReport report = new Reports.TitrimetryReport();
            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "Titrimetries");
            report.ReportParameters[0].Value = titrimetry.TitrimetryId.ToString().ToLower();

            if (this.printPhysical)
            {
                System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                Telerik.Reporting.Processing.ReportProcessor.Print(report, printerSettings);
            }

            string alertDirectory = System.Environment.CurrentDirectory + "\\ΟγκομετρικοίΠίνακες";
            if (!System.IO.Directory.Exists(alertDirectory))
            {
                System.IO.Directory.CreateDirectory(alertDirectory);
            }
            string fileName = alertDirectory + "\\" + string.Format("Titrimetry_{0:yyyyMMdd_HHmmssfff}.pdf", DateTime.Now);
            this.SaveReport(report, fileName);
        }

        public void PrintToPrinter(Data.Balance balance)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.BalanceReports.BalanceLoad bl = new Reports.BalanceReports.BalanceLoad();
            bl.LoadBalance(balance.BalanceId, balance.BalanceText);

            Reports.BalanceReport report = new Reports.BalanceReport();
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

            if (this.printPhysical)
            {
                System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                Telerik.Reporting.Processing.ReportProcessor.Print(report, printerSettings);
            }

            string balanceDirectory = System.Environment.CurrentDirectory + "\\Ισοζύγια";
            if (!System.IO.Directory.Exists(balanceDirectory))
            {
                System.IO.Directory.CreateDirectory(balanceDirectory);
            }
            string fileName = balanceDirectory + "\\" + string.Format("Balance_{0:yyyyMMdd_HHmm}.pdf", DateTime.Now);
            string compName = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            string taxNumber = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            if (compName == null || compName == "")
                return;
            if (taxNumber == null || taxNumber == "")
                return;
            SaveReport(report, fileName);
            string companyDesc = string.Format("Ισοζύγιο {0}, {1} από :{2:dd/MM/yyyy HH:mm} έως: {3:dd/MM/yyyy HH:mm}", compName, taxNumber, balance.StartDate, balance.EndDate);
            //MailSender.Instance.SendSms(fileName);
            MailSender.Instance.SendMail(new string[] { fileName }, companyDesc, companyDesc);
            
        }

        void SaveReport(Telerik.Reporting.Report report, string fileName)
        {
            Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            Telerik.Reporting.InstanceReportSource instanceReportSource = new Telerik.Reporting.InstanceReportSource();
            instanceReportSource.ReportDocument = report;
            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", instanceReportSource, null);

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }
        }

        private void PrintToPrinter(Data.Invoice invoice)
        {
            //if(invoice.InvoiceType.TransactionType == 1
            if (windowsPrint)
            {
                try
                {
                    Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                    Reports.InvoiceThermal report = new Reports.InvoiceThermal();
                    report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoicePrintViews");
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

                    System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                    printerSettings.PrinterName = invoice.Printer;
                    Telerik.Reporting.Processing.ReportProcessor.Print(report, printerSettings);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogToFile("PrintToPrinter : Windows", ex);
                }
            }
            else
            {
                try
                {
                    System.Diagnostics.Trace.WriteLine("Start Invoice Print : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.ffff"));
                    string[] invoiceLines = this.CreateInvoiceText(invoice);
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
                catch (Exception ex)
                {
                    Logger.Instance.LogToFile("PrintToPrinter : Other", ex);
                }
            }
        }

        private void PrintToPrinter(Data.TankFilling tankFilling)
        {
            if (tankFilling.InvoiceLines.Count == 0)
                return;
            try
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Reports.Invoices.TankFillingReport report = new Reports.Invoices.TankFillingReport();
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

                if (this.printPhysical)
                {
                    System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                    Telerik.Reporting.Processing.ReportProcessor.Print(report, printerSettings);
                }

                
                if (!System.IO.Directory.Exists(alertDirectory))
                {
                    System.IO.Directory.CreateDirectory(alertDirectory);
                }
                string fileName = alertDirectory + "\\" + string.Format("TankFilling_{0:yyyyMMdd_HHmmssfff}.pdf", DateTime.Now);
                this.SaveReport(report, fileName);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("PrintToPrinter : Windows", ex);
            }
            
        }

        private void PrintToPrinterInternalInvoice(Data.Invoice invoice)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.Invoices.InvoiceReport report = new Reports.Invoices.InvoiceReport();
            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");
            report.ReportParameters["InvoiceId"].Value = invoice.InvoiceId.ToString().ToLower();
            report.ReportParameters["CompanyName"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            report.ReportParameters["CompanyAddress"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            report.ReportParameters["CompanyCity"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            report.ReportParameters["CompanyVATNumber"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            report.ReportParameters["CompanyVATOffice"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
            report.ReportParameters["CompanyOccupation"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");

            System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
            Telerik.Reporting.Processing.ReportProcessor.Print(report, printerSettings);
        }

        private void ExortReportAsText(Telerik.Reporting.Report report, string fileName)
        {
            Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            Telerik.Reporting.InstanceReportSource instanceReportSource = new Telerik.Reporting.InstanceReportSource();
            instanceReportSource.ReportDocument = report;
            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("XLS", instanceReportSource, null);

            fileName = this.processFolder + "\\" + fileName;

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }

            //string text = ExcelTextStripper.GetText(fileName);
            //System.IO.File.WriteAllText(fileName.Replace(".xls", ".txt"), text);
        }

        #endregion

        #region Print Methods

        private void PrintInvoice(Data.Invoice invoice)
        {
            if (this.defaultTaxDevice == "Samtec")
            {
                if (this.invoicesProcessed.Contains(invoice.InvoiceId.ToString()))
                    return;
                string fileName = string.Format(this.signFolder + "\\{0}.txt", invoice.InvoiceId);
                if (System.IO.File.Exists(fileName))
                    return;
                List<string> lines = new List<string>(this.CreateInvoiceText(invoice));
                lines.Add(this.CreateSignLine(invoice));
                for (int i = 0; i < this.tailInvLines; i++)
                {
                    lines.Add(" ");
                }
                
                System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);
                this.invoicesProcessed.Add(invoice.InvoiceId.ToString());
            }
            else
            {
                string fileName = string.Format(this.signFolder + "\\{0}.txt", invoice.InvoiceId);
                if (invoice.Printer != null && invoice.Printer != "")
                    fileName = string.Format(invoice.Printer + "\\{0}.txt", invoice.InvoiceId);

                if (System.IO.File.Exists(fileName))
                    return;

                string[] invoiceLines = this.CreateInvoiceText(invoice);

                List<string> lines = new List<string>(invoiceLines);
                lines.Add(this.CreateSignLine(invoice));
                for (int i = 0; i < this.tailInvLines; i++)
                {
                    lines.Add(" ");
                }
                System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);

                invoice.IsPrinted = true;
                invoice.InvoiceSignature = "Sign Device A";
                this.invoicesProcessed.Add(invoice.InvoiceId.ToString() + "$$" + "Sign Device A");
            }
            database.SaveChanges();
        }

        private void PrintAlert(Data.SystemEvent alert)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.AlertReport report = new Reports.AlertReport();
            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "SystemEvents");
            report.ReportParameters[0].Value = alert.EventId.ToString().ToLower();

            this.ExortReportAsText(report, "Alert_" + alert.EventId.ToString() + ".xls");

            //string text = "ΣΥΝΑΓΕΡΜΟΣ " + alert.EventDate.ToString("dd/MM/yyyy HH:mm:ss") + "\r\n";
            //text = text + alert.DeviceDescription;
            //text = text + "\r\n" + alert.Message;

            //this.PrintText(text, "Alert_" + alert.EventId.ToString());
            //if (this.defaultTaxDevice == "SignA")
            //{
            //    alert.PrintedDate = DateTime.Now;
            //    this.entriesProcessed["Alerts"].Add(alert.EventId);
            //}
        }

        private void PrintBalance(Data.Balance balance)
        {
            string data = balance.BalanceText;


            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.BalanceReports.BalanceLoad bl = new Reports.BalanceReports.BalanceLoad();
            bl.LoadBalance(balance.BalanceId, balance.BalanceText);

            Reports.BalanceReport report = new Reports.BalanceReport();
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

            this.ExortReportAsText(report, "Balance_" + balance.BalanceId.ToString() + ".xls");

            //this.PrintText(balance.BalanceText, "Balance_" + balance.BalanceId.ToString());
            //if (this.defaultTaxDevice == "SignA")
            //{
            //    balance.PrintDate = DateTime.Now;
            //    this.entriesProcessed["Balances"].Add(balance.BalanceId);
            //}
        }

        private void PrintTitrimetry(Data.Titrimetry titrimetry)
        {
            //string data = this.database.SerializeEntity(titrimetry);

            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.TitrimetryReport report = new Reports.TitrimetryReport();
            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "Titrimetries");
            report.ReportParameters[0].Value = titrimetry.TitrimetryId.ToString().ToLower();

            this.ExortReportAsText(report, "Titrimetry_" + titrimetry.TitrimetryId.ToString() + ".xls");
            //string data = string.Format("Αλλαγή Ογκομετρικού Πίνακα : {0}", titrimetry.TitrationDate.Value.ToString("ddd dd/MM/yyyy HH:mm:ss"));
            //data = data + "\r\n" + string.Format("Δεξαμενή : {0}", titrimetry.Tank.Description);
            //data = data + "\r\n\r\n" + "Στάθμη\tΌγκος";
            //data = data + "\r\n=================================";
            //List<Data.TitrimetryLevel> levels = titrimetry.TitrimetryLevels.OrderBy(tl => tl.Height).ToList();
            //foreach (Data.TitrimetryLevel level in levels)
            //{
            //    data = data + String.Format("\r\n" + "{0}\t{1}", level.Height.Value.ToString("N0"), level.Volume.Value.ToString("N2"));
            //}
            //this.PrintText(data, "Titrimetry_" + titrimetry.TitrimetryId.ToString());
            //if (this.defaultTaxDevice == "SignA")
            //{
            //    titrimetry.PrintDate = DateTime.Now;
            //    this.entriesProcessed["Titrimetries"].Add(titrimetry.TitrimetryId);
            //}
        }

        private void PrintTankFilling(Data.TankFilling tankFilling)
        {
            if (tankFilling == null || tankFilling.InvoiceLines.Count == 0)
                return;

            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.Invoices.TankFillingReport report = new Reports.Invoices.TankFillingReport();
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
            this.ExortReportAsText(report, "TankFilling_" + tankFilling.TankFillingId.ToString() + ".xls");

            //if (this.defaultTaxDevice == "Samtec")
            //{
            //    string fileName = string.Format(this.signFolder + "\\TankFilling_{0}.txt", tankFilling.TankFillingId);
            //    if (System.IO.File.Exists(fileName))
            //        return;
            //    List<string> lines = new List<string>(this.CreateFillingText(tankFilling));
            //    System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);

            //}
            //else
            //{
                
            //}

            
        }

        private void PrintInternalInvoice(Data.Invoice invoice)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

            Reports.Invoices.InvoiceReport report = new Reports.Invoices.InvoiceReport();
            report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoiceLines");
            report.ReportParameters["InvoiceId"].Value = invoice.InvoiceId.ToString().ToLower();
            report.ReportParameters["CompanyName"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            report.ReportParameters["CompanyAddress"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress");
            report.ReportParameters["CompanyCity"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPostalCode") + " " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            report.ReportParameters["CompanyVATNumber"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN");
            report.ReportParameters["CompanyVATOffice"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");
            report.ReportParameters["CompanyOccupation"].Value = Data.Implementation.OptionHandler.Instance.GetOption("CompanyOccupation");
            this.ExortReportAsText(report, "Invoice_" + invoice.InvoiceId.ToString() + ".xls");
        }

        #endregion
    }

    internal class FileTypeInfo
    {
        string fileType = "";

        public string FileType 
        {
            set 
            { 
                this.fileType = value;
                switch (fileType)
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
            get { return this.fileType; }

        }
        
        public Guid Id { set; get; }
        public string ReportName { private set;  get; }
    }
}
