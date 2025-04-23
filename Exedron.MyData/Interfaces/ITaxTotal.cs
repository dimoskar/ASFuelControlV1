using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface ITaxTotal : IRequestMember
    {
        TaxTypeEnum TaxType { set; get; }
        int TaxCategory { set; get; }
        decimal UnderlyingValue { set; get; }
        decimal TaxAmount { set; get; }
        int Id { set; get; }
    }

    public enum TaxTypeEnum
    {
        WithheldTax = 1,
        Fees = 2,
        OtherTaxes = 3,
        StampTaxes = 4,
        Deductions = 5
    }
}
