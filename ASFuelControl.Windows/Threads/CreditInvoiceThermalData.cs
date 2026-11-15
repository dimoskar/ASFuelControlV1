using System;
using System.Collections.Generic;
using System.Linq;

namespace ASFuelControl.Windows.Threads
{
    internal static class CreditInvoiceThermalData
    {
        internal static bool IsCreditInvoice(Data.Invoice invoice)
        {
            return invoice.InvoiceType != null && invoice.InvoiceType.IsCancelation == true;
        }

        internal static CreditInvoiceThermalRow[] CreateRows(Data.Invoice invoice)
        {
            if (invoice.InvoiceLines == null || invoice.InvoiceLines.Count == 0)
            {
                throw new InvalidOperationException("Cannot print a credit invoice without invoice lines: " + invoice.InvoiceId);
            }

            var rows = new List<CreditInvoiceThermalRow>();
            foreach (Data.InvoiceLine line in invoice.InvoiceLines)
            {
                var visited = new HashSet<Data.InvoiceLine>();
                var nozzles = new Dictionary<Guid, Data.Nozzle>();
                CollectSourceNozzles(line, visited, nozzles);
                Data.Nozzle nozzle = nozzles.Count == 1 ? nozzles.Values.First() : null;
                var row = new CreditInvoiceThermalRow();
                row.InvoiceId = invoice.InvoiceId;
                row.InvoiceLineId = line.InvoiceLineId;
                row.Description = invoice.InvoiceType.Description;
                row.Number = invoice.Number;
                row.Series = invoice.Series;
                row.TransactionDate = invoice.TransactionDate;
                row.InvoiceSignature = invoice.InvoiceSignature;
                row.TraderName = invoice.Trader == null ? "" : invoice.Trader.Name;
                row.TraderTin = invoice.Trader == null ? "" : invoice.Trader.TaxRegistrationNumber;
                row.VehicleNumber = invoice.VehiclePlateNumber;
                row.Name = line.FuelType == null ? "" : line.FuelType.Name;
                row.Volume = line.Volume;
                row.UnitPrice = line.UnitPrice;
                row.NettoAmount = line.TotalPrice - line.VatAmount;
                row.VatAmount = line.VatAmount;
                row.TotalAmount = line.TotalPrice;
                row.VATPercentage = line.VatPercentage;
                if (nozzle != null)
                {
                    row.OfficialNozzleNumber = nozzle.OfficialNozzleNumber;
                    if (nozzle.Dispenser != null)
                    {
                        row.OfficialPumpNumber = nozzle.Dispenser.OfficialPumpNumber;
                    }
                }
                rows.Add(row);
            }
            return rows.ToArray();
        }

        private static void CollectSourceNozzles(Data.InvoiceLine line,
            ISet<Data.InvoiceLine> visited, IDictionary<Guid, Data.Nozzle> nozzles)
        {
            if (line == null || !visited.Add(line))
            {
                return;
            }
            Data.SalesTransaction sale = line.SalesTransaction;
            if (sale != null && sale.Nozzle != null)
            {
                nozzles[sale.Nozzle.NozzleId] = sale.Nozzle;
                return;
            }

            // Explicit line provenance is more precise than an invoice-level match.
            if (line.InvoiceLineRelations_ChildRelationId.Count > 0)
            {
                foreach (Data.InvoiceLineRelation relation in line.InvoiceLineRelations_ChildRelationId)
                {
                    CollectSourceNozzles(relation.InvoiceLine_ParentLineId, visited, nozzles);
                }
                return;
            }

            if (line.Invoice == null)
            {
                return;
            }
            foreach (Data.InvoiceRelation relation in line.Invoice.ParentInvoiceRelations)
            {
                Data.Invoice parent = relation.ParentInvoice;
                if (parent == null)
                {
                    continue;
                }
                foreach (Data.InvoiceLine parentLine in parent.InvoiceLines)
                {
                    if (parentLine.FuelTypeId == line.FuelTypeId)
                    {
                        CollectSourceNozzles(parentLine, visited, nozzles);
                    }
                }
            }
        }

        internal static string SalePointText(CreditInvoiceThermalRow[] rows)
        {
            // The existing receipt has one pump/nozzle field for the whole invoice.
            // Do not label a multi-pump credit with the first source pump alone.
            CreditInvoiceThermalRow first = rows[0];
            bool samePoint = rows.All(row => row.OfficialPumpNumber == first.OfficialPumpNumber && row.OfficialNozzleNumber == first.OfficialNozzleNumber);
            if (!samePoint || !first.OfficialPumpNumber.HasValue || !first.OfficialNozzleNumber.HasValue)
            {
                return "";
            }
            return "Αντλία: " + first.OfficialPumpNumber + " Ακροσωλήνιο: " + first.OfficialNozzleNumber;
        }
    }

    // Detached print data: source sales supply only location, never fiscal values.
    public sealed class CreditInvoiceThermalRow
    {
        public Guid InvoiceId { get; set; }
        public Guid InvoiceLineId { get; set; }
        public string Description { get; set; }
        public int Number { get; set; }
        public string Series { get; set; }
        public DateTime TransactionDate { get; set; }
        public string InvoiceSignature { get; set; }
        public string TraderName { get; set; }
        public string TraderTin { get; set; }
        public string VehicleNumber { get; set; }
        public string Name { get; set; }
        public decimal Volume { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal NettoAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal VATPercentage { get; set; }
        public int? OfficialPumpNumber { get; set; }
        public int? OfficialNozzleNumber { get; set; }
        public bool HasTrader
        {
            get
            {
                return !string.IsNullOrEmpty(this.TraderTin);
            }
        }
        public string TraderDescription
        {
            get
            {
                return this.HasTrader ? this.TraderName : "ΠΕΛΑΤΗΣ ΛΙΑΝΙΚΗΣ";
            }
        }
    }
}
