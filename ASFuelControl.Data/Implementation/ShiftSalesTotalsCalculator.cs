using ASFuelControl.Common.Enumerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASFuelControl.Data.Implementation
{
    public sealed class ShiftPaymentTotal
    {
        public PaymentTypeEnum PaymentType { get; private set; }

        public string Description { get; private set; }

        public decimal Amount { get; private set; }

        public ShiftPaymentTotal(PaymentTypeEnum paymentType, string description, decimal amount)
        {
            this.PaymentType = paymentType;
            this.Description = description;
            this.Amount = amount;
        }
    }

    public sealed class ShiftSalesTotals
    {
        public ShiftPaymentTotal[] PaymentTotals { get; private set; }

        public decimal GrandTotal { get; private set; }

        public Guid[] InvoiceIds { get; private set; }

        public ShiftSalesTotals(ShiftPaymentTotal[] paymentTotals, decimal grandTotal, Guid[] invoiceIds)
        {
            this.PaymentTotals = paymentTotals;
            this.GrandTotal = grandTotal;
            this.InvoiceIds = invoiceIds;
        }
    }

    public static class ShiftSalesTotalsCalculator
    {
        private const int QueryBatchSize = 500;

        private sealed class InvoiceLineLink
        {
            public Guid InvoiceLineId { get; set; }

            public Guid InvoiceId { get; set; }
        }

        public static ShiftSalesTotals Calculate(DatabaseModel database, Shift shift)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            if (shift == null)
            {
                throw new ArgumentNullException("shift");
            }

            Guid shiftId = shift.ShiftId;
            InvoiceLineLink[] rootLines = database.InvoiceLines
                .Where(line =>
                    line.SalesTransaction != null &&
                    line.SalesTransaction.ShiftId.HasValue &&
                    line.SalesTransaction.ShiftId.Value == shiftId &&
                    (!line.SalesTransaction.IsErrorResolving.HasValue || !line.SalesTransaction.IsErrorResolving.Value) &&
                    (!line.SalesTransaction.InvalidSale.HasValue || !line.SalesTransaction.InvalidSale.Value))
                .Select(line => new InvoiceLineLink
                {
                    InvoiceLineId = line.InvoiceLineId,
                    InvoiceId = line.InvoiceId
                })
                .ToArray();

            HashSet<Guid> relatedLineIds = new HashSet<Guid>();
            HashSet<Guid> relatedInvoiceIds = new HashSet<Guid>();
            Queue<Guid> pendingLineIds = new Queue<Guid>();
            Queue<Guid> pendingInvoiceIds = new Queue<Guid>();

            foreach (InvoiceLineLink rootLine in rootLines)
            {
                AddLine(rootLine, relatedLineIds, relatedInvoiceIds, pendingLineIds, pendingInvoiceIds);
            }

            ExpandRelations(
                database,
                relatedLineIds,
                relatedInvoiceIds,
                pendingLineIds,
                pendingInvoiceIds);

            Invoice[] invoices = LoadInvoices(database, relatedInvoiceIds);
            Dictionary<PaymentTypeEnum, decimal> amounts = CreateEmptyAmounts();

            foreach (Invoice invoice in invoices)
            {
                InvoiceType invoiceType = invoice.InvoiceType;
                if (invoiceType == null || invoiceType.HasFinancialTransactions == false)
                {
                    continue;
                }

                PaymentTypeEnum paymentType = NormalizePaymentType(invoice.PaymentType);
                decimal signedAmount = GetSignedAmount(invoice);
                amounts[paymentType] = amounts[paymentType] + signedAmount;
            }

            ShiftPaymentTotal[] paymentTotals = CreatePaymentTotals(amounts);
            decimal grandTotal = paymentTotals.Sum(total => total.Amount);
            Guid[] invoiceIds = invoices.Select(invoice => invoice.InvoiceId).Distinct().ToArray();
            return new ShiftSalesTotals(paymentTotals, grandTotal, invoiceIds);
        }

        private static void ExpandRelations(
            DatabaseModel database,
            HashSet<Guid> relatedLineIds,
            HashSet<Guid> relatedInvoiceIds,
            Queue<Guid> pendingLineIds,
            Queue<Guid> pendingInvoiceIds)
        {
            while (pendingLineIds.Count > 0 || pendingInvoiceIds.Count > 0)
            {
                Guid[] lineBatch = DequeueBatch(pendingLineIds);
                if (lineBatch.Length > 0)
                {
                    Guid[] childLineIds = database.InvoiceLineRelations
                        .Where(relation => lineBatch.Contains(relation.ParentLineId))
                        .Select(relation => relation.ChildRelationId)
                        .ToArray();
                    InvoiceLineLink[] childLines = LoadInvoiceLines(database, childLineIds, true);

                    foreach (InvoiceLineLink childLine in childLines)
                    {
                        AddLine(childLine, relatedLineIds, relatedInvoiceIds, pendingLineIds, pendingInvoiceIds);
                    }
                }

                Guid[] invoiceBatch = DequeueBatch(pendingInvoiceIds);
                if (invoiceBatch.Length == 0)
                {
                    continue;
                }

                Guid[] childInvoiceIds = database.InvoiceRelations
                    .Where(relation => invoiceBatch.Contains(relation.ParentInvoiceId))
                    .Select(relation => relation.ChildInvoiceId)
                    .ToArray();

                foreach (Guid childInvoiceId in childInvoiceIds)
                {
                    if (relatedInvoiceIds.Add(childInvoiceId))
                    {
                        pendingInvoiceIds.Enqueue(childInvoiceId);
                    }
                }

                InvoiceLineLink[] invoiceLines = LoadInvoiceLines(database, childInvoiceIds, false);
                foreach (InvoiceLineLink invoiceLine in invoiceLines)
                {
                    AddLine(invoiceLine, relatedLineIds, relatedInvoiceIds, pendingLineIds, pendingInvoiceIds);
                }
            }
        }

        private static InvoiceLineLink[] LoadInvoiceLines(DatabaseModel database, Guid[] ids, bool byLineId)
        {
            if (ids.Length == 0)
            {
                return new InvoiceLineLink[0];
            }

            if (byLineId)
            {
                return database.InvoiceLines
                    .Where(line => ids.Contains(line.InvoiceLineId))
                    .Select(line => new InvoiceLineLink
                    {
                        InvoiceLineId = line.InvoiceLineId,
                        InvoiceId = line.InvoiceId
                    })
                    .ToArray();
            }

            return database.InvoiceLines
                .Where(line => ids.Contains(line.InvoiceId))
                .Select(line => new InvoiceLineLink
                {
                    InvoiceLineId = line.InvoiceLineId,
                    InvoiceId = line.InvoiceId
                })
                .ToArray();
        }

        private static Invoice[] LoadInvoices(DatabaseModel database, HashSet<Guid> invoiceIds)
        {
            List<Invoice> invoices = new List<Invoice>();
            Guid[] allInvoiceIds = invoiceIds.ToArray();

            for (int index = 0; index < allInvoiceIds.Length; index = index + QueryBatchSize)
            {
                Guid[] invoiceBatch = allInvoiceIds.Skip(index).Take(QueryBatchSize).ToArray();
                Invoice[] batchInvoices = database.Invoices
                    .Where(invoice => invoiceBatch.Contains(invoice.InvoiceId))
                    .ToArray();
                invoices.AddRange(batchInvoices);
            }

            return invoices.ToArray();
        }

        private static void AddLine(
            InvoiceLineLink line,
            HashSet<Guid> relatedLineIds,
            HashSet<Guid> relatedInvoiceIds,
            Queue<Guid> pendingLineIds,
            Queue<Guid> pendingInvoiceIds)
        {
            if (relatedLineIds.Add(line.InvoiceLineId))
            {
                pendingLineIds.Enqueue(line.InvoiceLineId);
            }

            if (relatedInvoiceIds.Add(line.InvoiceId))
            {
                pendingInvoiceIds.Enqueue(line.InvoiceId);
            }
        }

        private static Guid[] DequeueBatch(Queue<Guid> pendingIds)
        {
            List<Guid> batch = new List<Guid>();
            while (pendingIds.Count > 0 && batch.Count < QueryBatchSize)
            {
                batch.Add(pendingIds.Dequeue());
            }

            return batch.ToArray();
        }

        private static Dictionary<PaymentTypeEnum, decimal> CreateEmptyAmounts()
        {
            Dictionary<PaymentTypeEnum, decimal> amounts = new Dictionary<PaymentTypeEnum, decimal>();
            amounts.Add(PaymentTypeEnum.Cash, 0m);
            amounts.Add(PaymentTypeEnum.Credit, 0m);
            amounts.Add(PaymentTypeEnum.CreditCard, 0m);
            amounts.Add(PaymentTypeEnum.FleetCard, 0m);
            amounts.Add(PaymentTypeEnum.IRIS, 0m);
            return amounts;
        }

        private static ShiftPaymentTotal[] CreatePaymentTotals(Dictionary<PaymentTypeEnum, decimal> amounts)
        {
            return new[]
            {
                new ShiftPaymentTotal(PaymentTypeEnum.Cash, "Μετρητά", amounts[PaymentTypeEnum.Cash]),
                new ShiftPaymentTotal(PaymentTypeEnum.Credit, "Επί πιστώσει", amounts[PaymentTypeEnum.Credit]),
                new ShiftPaymentTotal(PaymentTypeEnum.CreditCard, "Πιστωτική κάρτα", amounts[PaymentTypeEnum.CreditCard]),
                new ShiftPaymentTotal(PaymentTypeEnum.FleetCard, "Κάρτα στόλου", amounts[PaymentTypeEnum.FleetCard]),
                new ShiftPaymentTotal(PaymentTypeEnum.IRIS, "IRIS", amounts[PaymentTypeEnum.IRIS])
            };
        }

        private static PaymentTypeEnum NormalizePaymentType(int? paymentType)
        {
            if (!paymentType.HasValue || !Enum.IsDefined(typeof(PaymentTypeEnum), paymentType.Value))
            {
                return PaymentTypeEnum.Cash;
            }

            return (PaymentTypeEnum)paymentType.Value;
        }

        private static decimal GetSignedAmount(Invoice invoice)
        {
            decimal amount = invoice.TotalAmount.GetValueOrDefault();
            InvoiceType invoiceType = invoice.InvoiceType;
            bool isCancelation = invoiceType.IsCancelation == true;
            bool isNegative = invoiceType.TransactionSign.HasValue &&
                invoiceType.TransactionSign.Value == (int)TransactionSignEnum.Negative;

            if (isCancelation || isNegative)
            {
                return -Math.Abs(amount);
            }

            return Math.Abs(amount);
        }
    }
}
