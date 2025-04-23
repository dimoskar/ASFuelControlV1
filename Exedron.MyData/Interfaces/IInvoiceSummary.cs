using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IInvoiceSummary : IRequestMember
    {
        decimal TotalNetValue { set; get; }
        decimal TotalVATAmount { set; get; }
        decimal TotalWithheldAmount { set; get; }
        decimal TotalFeesAmount { set; get; }
        decimal TotalStampDutyAmount { set; get; }
        decimal TotalOtherTaxesAmount { set; get; }
        decimal TotalDeductionsAmount { set; get; }
        decimal TotalGrossValue { set; get; }
        IIncomeClassification[] IncomeClassification { set; get; }
        IExpenseClassification[] ExpenseClassification { set; get; }
    }
}
