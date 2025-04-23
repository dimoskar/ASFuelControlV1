using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Models
{
    public class InvoiceSummaryType
    {
        public decimal totalNetValue { set; get; }
        public decimal totalVatAmount { set; get; }
        public decimal totalWithheldAmount { set; get; }
        public decimal totalFeesAmount { set; get; }
        public decimal totalStampDutyAmount { set; get; }
        public decimal totalOtherTaxesAmount { set; get; }
        public decimal totalDeductionsAmount { set; get; }
        public decimal totalGrossValue { set; get; }
        public InvoiceRowType.IncomeClassificationType incomeClassification { set; get; }
        public InvoiceRowType.ExpensesClassificationType expensesClassification { set; get; }
    }
}
