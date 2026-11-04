using System;

namespace ASFuelControl.Invoicing.Mapping
{
    /// <summary>
    /// Keeps the selected price basis authoritative while deriving the other
    /// stored representation exactly once at the InvoiceLine database scale.
    /// </summary>
    public sealed class TransformationUnitPrice
    {
        private const int StorageScale = 5;

        public decimal Retail { get; private set; }
        public decimal Net { get; private set; }

        private TransformationUnitPrice(decimal retail, decimal net)
        {
            Retail = retail;
            Net = net;
        }

        public static TransformationUnitPrice FromRetail(decimal retail,
            decimal vatPercentage)
        {
            Validate(retail, vatPercentage);
            decimal storedRetail = Store(retail);
            decimal storedNet = vatPercentage == 0
                ? storedRetail
                : Store(storedRetail / Multiplier(vatPercentage));
            return new TransformationUnitPrice(storedRetail, storedNet);
        }

        public static TransformationUnitPrice FromNet(decimal net,
            decimal vatPercentage)
        {
            Validate(net, vatPercentage);
            decimal storedNet = Store(net);
            decimal storedRetail = vatPercentage == 0
                ? storedNet
                : Store(storedNet * Multiplier(vatPercentage));
            return new TransformationUnitPrice(storedRetail, storedNet);
        }

        private static decimal Store(decimal value)
        {
            return decimal.Round(value, StorageScale, MidpointRounding.ToEven);
        }

        private static decimal Multiplier(decimal vatPercentage)
        {
            return (100m + vatPercentage) / 100m;
        }

        private static void Validate(decimal value, decimal vatPercentage)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value");
            if (vatPercentage < 0)
                throw new ArgumentOutOfRangeException("vatPercentage");
        }
    }
}
