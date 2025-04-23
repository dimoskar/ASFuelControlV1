using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Windows.Threads
{
    public class PrintHandler
    {
        private string outFolder = "";
        private string signFolder = "";
        private bool windowsPrint = false;
        private bool printBarCode = false;
        private string connectionString = "";
        private string invoiceSignLine = "";
        private string taxNumber = "";
        private int tailInvLines = 0;
        private string defaultTaxDevice = "";
        private System.IO.FileSystemWatcher watcher;
        private List<Data.Invoice> readyInvoices = new List<Data.Invoice>();

        public PrintHandler()
        {
            this.defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
            if (this.defaultTaxDevice == "Samtec")
            {
                this.signFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_SignFolder");
                this.outFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_OutFolder");
                //watcher = new System.IO.FileSystemWatcher(this.outFolder, "*.out");
                //watcher.Created += new System.IO.FileSystemEventHandler(watcher_Created);
                //watcher.EnableRaisingEvents = true;
            }
            else if (this.defaultTaxDevice == "SignA")
            {
                this.signFolder = Data.Implementation.OptionHandler.Instance.GetOption("SignA_SignFolder");
            }
            this.tailInvLines = Data.Implementation.OptionHandler.Instance.GetIntOption("TailInvoiceLine", 5);
            this.invoiceSignLine = Data.Implementation.OptionHandler.Instance.GetOption("SingLine");
            this.windowsPrint = Data.Implementation.OptionHandler.Instance.GetBoolOption("WindowsInvoicePrint", false);
            this.printBarCode = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintInvoiceBarcode", false);
        }

        public bool SignInvoice(Data.Invoice invoice)
        {
            if (this.defaultTaxDevice == "SignA")
            {
                string[] invoiceLines = this.CreateInvoiceText(invoice);

                string fileName = string.Format(this.signFolder + "\\{0}.txt", invoice.InvoiceId);
                if (invoice.Printer != null && invoice.Printer != "")
                    fileName = string.Format(invoice.Printer + "\\{0}.txt", invoice.InvoiceId);
                List<string> lines = new List<string>(invoiceLines);
                lines.Add(this.CreateSignLine(invoice));
                for (int i = 0; i < this.tailInvLines; i++)
                {
                    lines.Add(" ");
                }
                //lines.Add("<!7062;1022;B;998129283;;0;0;100,00;0;0;0;0;23,00;0;123,00;;;;info@solidusnet.gr;;c;>");
                System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);

                //System.IO.File.WriteAllLines(fileName + ".out", lines.ToArray(), Encoding.Default);
                //this.readyInvoices.Add(invoice);
                invoice.IsPrinted = true;
                return false;
            }
            else if (this.defaultTaxDevice == "Samtec")
            {
                List<string> lines = new List<string>(this.CreateInvoiceText(invoice));
                lines.Add(this.CreateSignLine(invoice));
                for (int i = 0; i < this.tailInvLines; i++)
                {
                    lines.Add(" ");
                }
                string fileName = string.Format(this.signFolder + "\\{0}.txt", invoice.InvoiceId);
                //string fileName2 = string.Format("d:\\{0}.txt", invoice.InvoiceId);

                System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);//.Select(s => Encoding.GetEncoding(437).GetString(Encoding.Default.GetBytes(s))));
                //System.IO.File.WriteAllLines(fileName2, invoiceLines.ToArray());

                return true;
            }
            return false;
        }

        public bool SignAlert(Data.SystemEvent alert)
        {
            if (this.defaultTaxDevice == "SignA")
            {
                string[] alertLines = this.CreateAlertText(alert);

                string fileName = string.Format(this.signFolder + "\\{0}.txt", alert.EventId);
                //if (invoice.Printer != null && invoice.Printer != "")
                //    fileName = string.Format(invoice.Printer + "\\{0}.txt", invoice.InvoiceId);
                List<string> lines = new List<string>(alertLines);
                
                for (int i = 0; i < this.tailInvLines; i++)
                {
                    lines.Add(" ");
                }
                
                System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);
                return false;
            }
            else if (this.defaultTaxDevice == "Samtec")
            {
                List<string> lines = new List<string>(this.CreateAlertText(alert));
                
                for (int i = 0; i < this.tailInvLines; i++)
                {
                    lines.Add(" ");
                }
                string fileName = string.Format(this.signFolder + "\\{0}.txt", alert.EventId);
                System.IO.File.WriteAllLines(fileName, lines.ToArray(), Encoding.Default);
                
                return true;
            }
            return false;
        }

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

        private string[] CreateAlertText(Data.SystemEvent alert)
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

            string taxData = string.Format("ΑΦΜ:{0}#ΔΟΥ:{1}", tin, taxOffice).EquilizeString(printerLineWidth);
            string contactData = string.Format("ΤΗΛ:{0}#FAX:{1}", phone, fax).EquilizeString(printerLineWidth);

            List<string> invoiceLines = new List<string>();
            invoiceLines.Add(name.CenterString(printerLineWidth));
            invoiceLines.Add(occupation.CenterString(printerLineWidth));
            invoiceLines.Add(address.CenterString(printerLineWidth));
            invoiceLines.Add(taxData.CenterString(printerLineWidth));
            invoiceLines.Add(contactData.CenterString(printerLineWidth));
            invoiceLines.Add("");
            invoiceLines.Add(("ΣΥΝΑΓΕΡΜΟΣ").CenterString(printerLineWidth));
            invoiceLines.Add((alert.EventDate.ToString("dd/MM/yyyy HH:mm:ss")).CenterString(printerLineWidth));
            if (alert.TankId.HasValue)
            {

            }

            return invoiceLines.ToArray();

        }

        public void PrintInvoice(Data.Invoice invoice)
        {
            if (windowsPrint)
            {
                Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

                Reports.InvoiceThermal report = new Reports.InvoiceThermal();
                report.DataSource = new Telerik.Reporting.OpenAccessDataSource(db, "InvoicePrintViews");
                report.ReportParameters[0].Value = invoice.InvoiceId.ToString().ToLower();
                report.ReportParameters[1].Value = this.printBarCode;

                System.Drawing.Printing.PrinterSettings printerSettings = new System.Drawing.Printing.PrinterSettings();
                //printerSettings.PrinterName = invoice.Printer;
                Telerik.Reporting.Processing.ReportProcessor.Print(report, printerSettings);
            }
            else
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
        }

        public bool PrintText(string text, string name)
        {
            string fileName = string.Format(this.signFolder + "\\{0}.txt", name);
            if (System.IO.File.Exists(fileName))
                return false;
            if (this.defaultTaxDevice == "SignA")
            {                
                System.IO.File.WriteAllText(fileName, text, Encoding.Default);
                return false;
            }
            else if (this.defaultTaxDevice == "Samtec")
            {   
                System.IO.File.WriteAllText(fileName, text, Encoding.Default);
                return false;
            }
            return false;
        }

        private void ApplySign(string fileName, string fullName)
        {
            string str = System.IO.File.ReadAllText(fullName);
            string id = fileName.Replace(".out", "");
            Guid invoiceId = Guid.Parse(id);
            Data.Invoice invoice = this.readyInvoices.Where(inv => inv.InvoiceId == invoiceId).FirstOrDefault();
            if (invoice == null)
                return;
            int signIndex = str.IndexOf(",");
            if (signIndex < 0)
                return;

            str = str.Substring(7, signIndex - 6);
            invoice.InvoiceSignature = str;
            System.IO.File.Delete(fullName);
        }
    }
}