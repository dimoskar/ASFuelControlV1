using System;
using Telerik.Reporting;
using Telerik.Reporting.Drawing;

namespace ASFuelControl.Reports.Invoices
{
    public partial class InvoiceReportNarrow
    {
        public const string RecommendedThermalFontName = "Tahoma";
        public const float RecommendedThermalFontSize = 9f;

        /// <summary>
        /// Applies the thermal-printer typography after the legacy configurable
        /// font pass. Existing custom faces remain supported; only the former
        /// Bahnschrift default is migrated to the clearer Tahoma face.
        /// </summary>
        public void ApplyThermalTypography(string configuredFontName, float configuredSize)
        {
            string effectiveName = EffectiveThermalFontName(configuredFontName);
            float effectiveSize = EffectiveThermalFontSize(configuredFontName, configuredSize);
            double sizeFactor = configuredSize >= 4f ? effectiveSize / configuredSize : 1D;

            ApplyThermalFont(pageFooterSection1.Items,effectiveName,sizeFactor);
            ApplyThermalFont(pageHeaderSection1.Items,effectiveName,sizeFactor);
            ApplyThermalFont(detail.Items,effectiveName,sizeFactor);
            ApplyThermalFont(groupHeaderSection1.Items,effectiveName,sizeFactor);

            // Keep the body regular and reserve emphasis for document identity,
            // column headings and the final total.
            textBox14.Style.Font.Bold = true;
            descriptionDataTextBox.Style.Font.Bold = true;
            textBox20.Style.Font.Bold = true;
            textBox17.Style.Font.Bold = true;
            nameCaptionTextBox.Style.Font.Bold = true;
            volumeCaptionTextBox.Style.Font.Bold = true;
            unitPriceCaptionTextBox.Style.Font.Bold = true;
            totalAmountCaptionTextBox.Style.Font.Bold = true;
            textBox10.Style.Font.Bold = true;
            textBox6.Style.Font.Bold = true;

            // Keep long quantities and totals on one physical line. The legacy
            // detail cells were 10 pt even when the configured receipt font was
            // 9 pt, which wrapped values such as 580,00 and 1.009,85.
            float numericSize = Math.Max(8f,effectiveSize - 1f);
            var numericCells = new[]
            {
                volumeDataTextBox, unitPriceDataTextBox,
                totalAmountDataTextBox, textBoxVATPercentage
            };
            foreach (var cell in numericCells)
            {
                cell.Style.Font.Size = Unit.Point(numericSize);
                cell.CanGrow = false;
                cell.TextWrap = false;
            }
        }

        public static string EffectiveThermalFontName(string configuredFontName)
        {
            return IsLegacyThermalDefault(configuredFontName)
                ? RecommendedThermalFontName
                : configuredFontName.Trim();
        }

        public static float EffectiveThermalFontSize(string configuredFontName, float configuredSize)
        {
            return IsLegacyThermalDefault(configuredFontName) && configuredSize <= 9f
                ? RecommendedThermalFontSize
                : configuredSize;
        }

        private static bool IsLegacyThermalDefault(string configuredFontName)
        {
            return string.IsNullOrWhiteSpace(configuredFontName) ||
                string.Equals(configuredFontName,"Bahnschrift Light Condensed",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyThermalFont(System.Collections.IEnumerable items,
            string fontName, double sizeFactor)
        {
            foreach (object entry in items)
            {
                var item = entry as ReportItem;
                if (item == null)
                    continue;
                item.Style.Font.Name = fontName;
                if (Math.Abs(sizeFactor - 1D) > 0.0001D)
                    item.Style.Font.Size = new Unit(item.Style.Font.Size.Value * sizeFactor,
                        item.Style.Font.Size.Type);
                var panel = item as Panel;
                if (panel != null)
                    ApplyThermalFont(panel.Items,fontName,sizeFactor);
            }
        }
    }
}
