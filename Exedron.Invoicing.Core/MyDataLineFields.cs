namespace Exedron.Invoicing.Core
{
    public static class MyDataLineFields
    {
        // These service types do not transmit quantities/units to myDATA.
        // Keep the source model intact for application pricing and provider rendering.
        public static bool IncludeQuantityAndUnit(string invoiceType)
        {
            return invoiceType != "2.1" && invoiceType != "2.2" &&
                invoiceType != "2.3" && invoiceType != "11.2";
        }
    }
}
