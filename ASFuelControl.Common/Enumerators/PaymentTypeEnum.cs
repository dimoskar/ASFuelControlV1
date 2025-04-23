using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Enumerators
{
    public enum PaymentTypeEnum
    {
        Credit = 0,
        Cash = 1,
        CreditCard = 2,
        FleetCard = 3
    }

    public enum TransactionTypeEnum
    {
        Extraction,
        Delivery
    }

    public enum FinTransactionTypeEnum
    {
        Credit,
        Debit
    }

    public enum TransactionSignEnum
    {
        Negative,
        Positive
    }
}
