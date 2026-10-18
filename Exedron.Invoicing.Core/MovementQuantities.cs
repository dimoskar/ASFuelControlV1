using System;
using System.Runtime.CompilerServices;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Core
{
    // The legacy vendor model has no quantity15 member. Keep the supplementary value
    // attached to the exact detail instance; each adapter must explicitly support it.
    public static class MovementQuantities
    {
        private sealed class Value { public decimal Quantity; }
        private static readonly ConditionalWeakTable<InvoiceDetail, Value> Values = new ConditionalWeakTable<InvoiceDetail, Value>();
        public static void Set(InvoiceDetail detail, decimal quantity15)
        {
            if (quantity15 <= 0) throw new ArgumentOutOfRangeException(nameof(quantity15));
            Values.Add(detail, new Value { Quantity = quantity15 });
        }
        public static decimal? Get(InvoiceDetail detail)
        {
            Value value;
            return Values.TryGetValue(detail, out value) ? (decimal?)value.Quantity : null;
        }
    }
}
