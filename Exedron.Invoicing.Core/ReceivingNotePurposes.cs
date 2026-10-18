using System;
using System.Runtime.CompilerServices;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Core
{
    // Supplement the legacy vendor model without changing serialization in other providers.
    public static class ReceivingNotePurposes
    {
        public const int UndeliveredReturn = 6;
        private sealed class Value { public int Purpose; }
        private static readonly ConditionalWeakTable<InvoiceHeader, Value> Values = new ConditionalWeakTable<InvoiceHeader, Value>();
        public static void Set(InvoiceHeader header, int purpose)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));
            if (header.InvoiceType != "10.1" && header.InvoiceType != "10.2") throw new InvalidOperationException("Receiving purpose requires a quantitative receipt.");
            // Purpose 7 also needs a title, which this workflow does not currently support.
            if (purpose < 1 || purpose > 6) throw new ArgumentOutOfRangeException(nameof(purpose));
            Values.GetOrCreateValue(header).Purpose = purpose;
        }
        public static int? Get(InvoiceHeader header)
        {
            Value value;
            return header != null && Values.TryGetValue(header, out value) ? (int?)value.Purpose : null;
        }
    }
}
