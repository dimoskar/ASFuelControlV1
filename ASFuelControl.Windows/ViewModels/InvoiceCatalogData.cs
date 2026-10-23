using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    // Capture on the UI thread: changing the controls must not change an in-flight query.
    internal sealed class InvoiceCatalogFilter
    {
        public readonly DateTime From, To;
        public readonly Guid InvoiceTypeId;
        public readonly string Search;

        public InvoiceCatalogFilter(DateTime from, DateTime to, Guid invoiceTypeId, string search)
        {
            From = from.Date;
            To = to.Date;
            InvoiceTypeId = invoiceTypeId;
            Search = search;
        }

        public IQueryable<Data.InvoiceCatalogView> Apply(IQueryable<Data.InvoiceCatalogView> query)
        {
            var from = From;
            query = query.Where(i => i.TransactionDate >= from);
            if (To < DateTime.MaxValue.Date)
            {
                var until = To.AddDays(1);
                query = query.Where(i => i.TransactionDate < until);
            }
            if (InvoiceTypeId != Guid.Empty)
            {
                var type = InvoiceTypeId;
                query = query.Where(i => i.InvoiceTypeId == type);
            }
            // Preserve the existing string matching, including comma lists, leading
            // zeros, whitespace and substring searches. Do not parse numbers here.
            var search = Search;
            if (!string.IsNullOrEmpty(search))
            {
                if (search.Contains(","))
                {
                    var numbers = search.Split(',');
                    query = query.Where(i => numbers.Contains(i.Number.ToString()));
                }
                else
                    query = query.Where(i => i.Number.ToString().Contains(search) ||
                        i.TraderName.Contains(search) || i.PlateNumber.Contains(search) ||
                        i.SupplyNumber.Contains(search));
            }
            return query.OrderByDescending(i => i.TransactionDate);
        }
    }

    internal sealed class InvoiceCatalogTotals
    {
        private readonly decimal[,] amounts = new decimal[5, 2];

        public InvoiceCatalogTotals(IEnumerable<Data.InvoiceCatalogView> invoices, IDictionary<Guid, int> transactionTypes)
        {
            foreach (var invoice in invoices)
            {
                if (invoice.IsCanceled || invoice.IsReplaced) continue;
                int transactionType;
                if (!transactionTypes.TryGetValue(invoice.InvoiceTypeId, out transactionType))
                    throw new InvalidOperationException("Missing invoice type for catalog totals: " + invoice.InvoiceTypeId);
                int paymentRow;
                switch ((Common.Enumerators.PaymentTypeEnum)invoice.PaymentType)
                {
                    case Common.Enumerators.PaymentTypeEnum.Cash: paymentRow = 0; break;
                    case Common.Enumerators.PaymentTypeEnum.Credit: paymentRow = 1; break;
                    case Common.Enumerators.PaymentTypeEnum.CreditCard: paymentRow = 2; break;
                    case Common.Enumerators.PaymentTypeEnum.FleetCard: paymentRow = 3; break;
                    default: paymentRow = -1; break;
                }
                // Both columns historically total signed TotalAmount, split by
                // TransactionType. RestAmount here is NOT CreditAmount - DebitAmount.
                decimal amount = invoice.TransactionSign * invoice.TotalAmount;
                if (transactionType != 1)
                {
                    amounts[4, 0] += amount;
                    if (paymentRow >= 0) amounts[paymentRow, 0] += amount;
                }
                if (transactionType != 0)
                {
                    amounts[4, 1] += amount;
                    if (paymentRow >= 0) amounts[paymentRow, 1] += amount;
                }
            }
        }

        public decimal Get(int row, string column)
        {
            return amounts[row, column == "TotalAmount" ? 0 : 1];
        }
    }

    internal sealed class InvoiceCatalogSnapshot
    {
        public Data.InvoiceCatalogView[] Invoices;
        public InvoiceCatalogTotals Totals;
        public bool IsAdmin;
    }

    internal static class InvoiceCatalogData
    {
        public static InvoiceCatalogSnapshot Read(string connection, InvoiceCatalogFilter filter,
            Guid userId, IDictionary<Guid, int> transactionTypes)
        {
            using (var db = new Data.DatabaseModel(connection))
            {
                var invoices = filter.Apply(db.InvoiceCatalogViews).ToArray();
                var traderIds = invoices.Where(i => i.TraderId.HasValue)
                    .Select(i => i.TraderId.Value).Distinct().ToArray();
                var vatNumbers = new Dictionary<Guid, string>();
                // Stay below SQL Server's parameter limit even for a long date range.
                // Project only the two needed fields, not complete Trader entities.
                for (int offset = 0; offset < traderIds.Length; offset += 1000)
                {
                    var batch = traderIds.Skip(offset).Take(1000).ToArray();
                    var traders = db.Traders.Where(t => batch.Contains(t.TraderId))
                        .Select(t => new { t.TraderId, t.TaxRegistrationNumber }).ToArray();
                    foreach (var trader in traders) vatNumbers[trader.TraderId] = trader.TaxRegistrationNumber;
                }
                foreach (var invoice in invoices)
                {
                    string vat;
                    if (invoice.TraderId.HasValue && vatNumbers.TryGetValue(invoice.TraderId.Value, out vat))
                        invoice.VATNumber = vat;
                }
                return new InvoiceCatalogSnapshot
                {
                    Invoices = invoices,
                    Totals = new InvoiceCatalogTotals(invoices, transactionTypes),
                    IsAdmin = db.ApplicationUsers.Any(u => u.ApplicationUserId == userId && u.UserLevel == 0)
                };
            }
        }
    }

    // One database load at a time. Queued obsolete searches are skipped and an
    // older result (or error) can never replace the latest search's result.
    internal sealed class InvoiceCatalogLoadCoordinator
    {
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private int version;

        public void Invalidate() { Interlocked.Increment(ref version); }

        public async Task<InvoiceCatalogSnapshot> LoadAsync(Func<InvoiceCatalogSnapshot> read)
        {
            int request = Interlocked.Increment(ref version);
            await gate.WaitAsync();
            try
            {
                if (request != Volatile.Read(ref version)) return null;
                var result = await Task.Run(read);
                return request == Volatile.Read(ref version) ? result : null;
            }
            catch
            {
                if (request != Volatile.Read(ref version)) return null;
                throw;
            }
            finally { gate.Release(); }
        }
    }
}
