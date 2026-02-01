using System;
using System.Collections.Generic;
using System.Linq;

namespace Exedron.ProviderInvoicing.Resolvers
{
    public class IncomeClassification
    {
        public string Category { get; set; }
        public string Type { get; set; }
    }
    public class IncomeClassificationRule
    {
        public Func<InvoiceContext, bool> When { get; set; }
        public IncomeClassification Result { get; set; }
    }
    public static class IncomeClassificationResolver
    {
        private static readonly List<IncomeClassificationRule> Rules =
            new List<IncomeClassificationRule>
            {
            // Counterpart VAT match overrides everything
            new IncomeClassificationRule {When = c => c.CounterPartVatMatches,Result = new IncomeClassification{Category = "category1_6",Type = "E3_595" } },

            // Service
            new IncomeClassificationRule{When = c => c.IsService,Result = new IncomeClassification{Category = "category1_3",Type = "E3_561_001" } },

            // Delivery
            new IncomeClassificationRule{When = c => c.IsDelivery,Result = new IncomeClassification{Category = "category3",Type = ""} },

            // Default non-service, non-delivery
            new IncomeClassificationRule{When = c => true,Result = new IncomeClassification{Category = "category1_1",Type = "E3_561_001"} }
            };

        public static IncomeClassification Resolve(InvoiceContext ctx)
        {
            var result = Rules.First(r => r.When(ctx)).Result;

            // Post‑processing for Type (only when not delivery and not VAT match)
            if (!ctx.CounterPartVatMatches && !ctx.IsDelivery)
            {
                if (ctx.IsRetail)
                    result.Type = "E3_561_003";
                else if (ctx.IsGreece)
                    result.Type = "E3_561_001";
                else if (ctx.IsEu)
                    result.Type = "E3_561_005";
                else
                    result.Type = "E3_561_006";
            }

            return result;
        }
    }
}