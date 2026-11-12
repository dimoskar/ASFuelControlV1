using System;

namespace ASFuelControl.Invoicing.Mapping
{
    /// <summary>
    /// Determines whether a transformed line still represents the source line at
    /// the storage precision used by InvoiceLine. The selected price basis is
    /// important because a replacement can display the VAT-exclusive price while
    /// the originating dispenser sale stores the VAT-inclusive price.
    /// </summary>
    public static class TransformationFiscalAmountPolicy
    {
        private const int VolumeStorageScale = 3;
        private const int PriceStorageScale = 5;

        public static bool HasUnchangedPriceAndVolume(decimal sourceVolume,
            decimal targetVolume, TransformationUnitPrice sourcePrice,
            decimal targetRetailPrice, decimal targetNetPrice,
            bool targetPriceIncludesVat)
        {
            if (sourcePrice == null)
            {
                throw new ArgumentNullException("sourcePrice");
            }

            decimal storedSourceVolume = StoreVolume(sourceVolume);
            decimal storedTargetVolume = StoreVolume(targetVolume);
            if (storedSourceVolume != storedTargetVolume)
            {
                return false;
            }

            decimal expectedPrice = targetPriceIncludesVat
                ? sourcePrice.Retail
                : sourcePrice.Net;
            decimal targetPrice = targetPriceIncludesVat
                ? targetRetailPrice
                : targetNetPrice;
            decimal storedExpectedPrice = StorePrice(expectedPrice);
            decimal storedTargetPrice = StorePrice(targetPrice);
            return storedExpectedPrice == storedTargetPrice;
        }

        private static decimal StoreVolume(decimal value)
        {
            return decimal.Round(value, VolumeStorageScale,
                MidpointRounding.ToEven);
        }

        private static decimal StorePrice(decimal value)
        {
            return decimal.Round(value, PriceStorageScale,
                MidpointRounding.ToEven);
        }
    }
}
