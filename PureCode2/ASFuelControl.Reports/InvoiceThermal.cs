namespace ASFuelControl.Reports
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Summary description for InvoiceThermal.
    /// </summary>
    public partial class InvoiceThermal : Telerik.Reporting.Report
    {
        public InvoiceThermal()
        {
            //
            // Required for telerik Reporting designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }
    }

    public static class InvoiceReportFunctions
    {
        private static Data.DatabaseModel database = new Data.DatabaseModel(Data.Implementation.OptionHandler.ConnectionString);

        [Telerik.Reporting.Expressions.Function(Category = "Invoice Functions", Description = "Returns the Barcode for the Invoice")]
        public static string GetBarcode(Guid invoiceLineId)
        {
            Data.InvoiceLine invLine = database.InvoiceLines.Where(i=>i.InvoiceLineId == invoiceLineId).FirstOrDefault();
            if(invLine == null)
                return "";

            string pattern = Data.Implementation.OptionHandler.Instance.GetOption("BarCodePattern");
            bool addEvenZero = false;
            if (pattern.StartsWith("0"))
            {
                addEvenZero = true;
                pattern = pattern.Substring(1);
            }
            string[] strings = pattern.Split(new string[] { "][" }, StringSplitOptions.RemoveEmptyEntries);
            string val = "";
            foreach (string str in strings)
            {
                string strNew = str.Replace("[", "").Replace("]", "");
                string[] vals = strNew.Split(':');
                if (vals.Length >= 2)
                {
                    string format = "";
                    if(vals.Length == 3)
                        format = vals[2];
                    val = val + GetValue(invLine, vals[0], int.Parse(vals[1]), format);
                }
                else
                {
                    val = val + GetValue(invLine, strNew, 0, "");
                }
            }

            if (addEvenZero)
                val = "0" + val;
            return val;
        }

        private static string GetValue(Data.InvoiceLine invLine, string pattern, int len, string format)
        {
            try
            {
                object val = invLine;
                string[] strs = pattern.Split('.');

                if (strs.Length < 2)
                {
                    PropertyInfo propInfo = invLine.GetType().GetProperty(pattern);
                    if (propInfo == null)
                        return "";
                    val = propInfo.GetValue(invLine, new object[] { });
                }
                else
                {
                    Type t = invLine.GetType();
                    val = invLine;
                    for (int i = 0; i < strs.Length - 1; i++)
                    {
                        PropertyInfo propInfo = val.GetType().GetProperty(strs[i]);
                        if (propInfo == null)
                            return "";
                        val = propInfo.GetValue(invLine, new object[] { });
                        t = val.GetType();
                    }
                    if (t == null)
                        return "";
                    PropertyInfo propInfo2 = t.GetProperty(strs[strs.Length - 1]);
                    if (propInfo2 == null)
                        return "";

                    val = propInfo2.GetValue(val, new object[] { });

                }
                string value = string.Format("{0:" + format + "}", val).Replace(",", "").Replace(".", "");
                if (len == 0)
                    return value;
                else
                {
                    if (value.Length > len)
                        value = value.Substring(0, len);
                    else
                    {
                        value = value.PadLeft(len, '0');
                    }
                }
                return value;
            }
            catch
            {
                return "";
            }
        }
    }
}