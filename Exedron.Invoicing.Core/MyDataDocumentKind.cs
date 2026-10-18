namespace Exedron.Invoicing.Core
{
    // Persisted values: do not reorder. Zero intentionally requires configuration.
    public enum MyDataDocumentKind
    {
        Unspecified = 0,
        Goods = 1,
        Services = 2,
        Delivery = 3,
        ConsolidatedDelivery = 4,
        CorrelatedQuantityReceipt = 5
    }
}
