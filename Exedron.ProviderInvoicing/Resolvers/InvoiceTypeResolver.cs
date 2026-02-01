using System;
using System.Collections.Generic;
using System.Linq;

namespace Exedron.ProviderInvoicing.Resolvers
{
    public class InvoiceContext
    {
        public bool IsCanceling { get; set; }
        public bool IsRetail { get; set; }
        public bool IsService { get; set; }
        public bool IsDelivery { get; set; }
        public bool IsGreece { get; set; }
        public bool IsEu { get; set; }
        public int CorInvoicesCount { get; set; }
        public bool CounterPartVatMatches { get; set; }
    }

    public class InvoiceRule
    {
        public Func<InvoiceContext, bool> When { get; set; }
        public string Result { get; set; }
    }

    public static class InvoiceTypeResolver
    {
        private static readonly List<InvoiceRule> Rules = new List<InvoiceRule>
    {
        new InvoiceRule { When = c => c.IsCanceling && c.IsRetail, Result = "11.4" },
        new InvoiceRule { When = c => c.IsCanceling && !c.IsRetail && c.CorInvoicesCount > 0, Result = "5.1" },
        new InvoiceRule { When = c => c.IsCanceling && !c.IsRetail && c.CorInvoicesCount == 0, Result = "5.2" },
        new InvoiceRule { When = c => c.IsService && c.IsRetail, Result = "11.2" },
        new InvoiceRule { When = c => c.IsService && !c.IsRetail, Result = "2.1" },
        new InvoiceRule { When = c => c.IsDelivery, Result = "9.3" },
        new InvoiceRule { When = c => !c.IsService && !c.IsDelivery && c.IsRetail, Result = "11.1" },
        new InvoiceRule { When = c => c.CounterPartVatMatches, Result = "6.1" },
        new InvoiceRule { When = c => c.IsGreece, Result = "1.1" },
        new InvoiceRule { When = c => c.IsEu, Result = "1.2" },
        // default fallback
        new InvoiceRule { When = c => true, Result = "1.3" }
    };

        public static string Resolve(InvoiceContext ctx)
        {
            return Rules.First(r => r.When(ctx)).Result;
        }
    }
}