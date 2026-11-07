namespace ASFuelControl.Reports.Invoices
{
    public partial class InvoiceReportNarrow
    {
        private const double SharedFooterQrStartCm = 4.1677079200744629D;
        private bool thermalPrintSafetyApplied;
        private bool thermalFooterFlowsAfterDetails;
        private bool thermalDateFlowsBeforeColumns;

        /// <summary>
        /// Page headers do not reliably expose the detail row's Fields collection
        /// in every Telerik rendering path. Freeze the issued invoice date as text
        /// so both the label and value are always rendered.
        /// </summary>
        public void SetTransactionDate(System.DateTime transactionDate)
        {
            transactionDateDataTextBox.Visible = true;
            transactionDateDataTextBox.CanGrow = false;
            transactionDateDataTextBox.Value = "\u0397\u03BC\u03B5\u03C1/\u03BD\u03AF\u03B1: " +
                transactionDate.ToString("dd/MM/yyyy HH:mm");
        }

        /// <summary>
        /// Uses the full safe width of an 80 mm roll with one balanced hardware
        /// margin. The old recursive item shift moved nested controls more than
        /// once and produced the visibly oversized right margin.
        /// </summary>
        public void ApplyThermalPrintSafety()
        {
            if (thermalPrintSafetyApplied)
                return;
            thermalPrintSafetyApplied = true;

            volumeCaptionTextBox.Value = "\u03A0\u039F\u03A3.";
            ExpandTotalsLabels();
            ExpandLineAmountColumn();
            FlowTransactionDateBeforeColumns();

            PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(
                Telerik.Reporting.Drawing.Unit.Mm(2D),
                Telerik.Reporting.Drawing.Unit.Mm(2D),
                PageSettings.Margins.Top,
                PageSettings.Margins.Bottom);
            Width = Telerik.Reporting.Drawing.Unit.Mm(76D);
        }

        private void ExpandLineAmountColumn()
        {
            // The original 12 mm amount column wraps or clips values such as
            // 1.009,85 on an 80 mm roll. Use the remaining printable width and
            // move the compact VAT column beside it.
            var amountWidth = Telerik.Reporting.Drawing.Unit.Cm(1.35D);
            totalAmountCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(
                amountWidth,totalAmountCaptionTextBox.Size.Height);
            totalAmountDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(
                amountWidth,totalAmountDataTextBox.Size.Height);

            var vatX = Telerik.Reporting.Drawing.Unit.Cm(6.55D);
            var vatWidth = Telerik.Reporting.Drawing.Unit.Cm(0.95D);
            textBox12.Location = new Telerik.Reporting.Drawing.PointU(
                vatX,textBox12.Location.Y);
            textBox12.Size = new Telerik.Reporting.Drawing.SizeU(
                vatWidth,textBox12.Size.Height);
            textBoxVATPercentage.Location = new Telerik.Reporting.Drawing.PointU(
                vatX,textBoxVATPercentage.Location.Y);
            textBoxVATPercentage.Size = new Telerik.Reporting.Drawing.SizeU(
                vatWidth,textBoxVATPercentage.Size.Height);
        }

        private void FlowTransactionDateBeforeColumns()
        {
            if (thermalDateFlowsBeforeColumns)
                return;
            thermalDateFlowsBeforeColumns = true;

            const double dateRowHeightCm = 0.55D;
            var columnItems = new System.Collections.Generic.List<Telerik.Reporting.ReportItem>();
            foreach (object entry in groupHeaderSection1.Items)
            {
                var item = entry as Telerik.Reporting.ReportItem;
                if (item != null)
                    columnItems.Add(item);
            }
            foreach (var item in columnItems)
                item.Location = new Telerik.Reporting.Drawing.PointU(
                    item.Location.X,
                    Telerik.Reporting.Drawing.Unit.Cm(item.Location.Y.Value + dateRowHeightCm));

            pageHeaderSection1.Items.Remove(transactionDateDataTextBox);
            transactionDateDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(
                Telerik.Reporting.Drawing.Unit.Cm(0.3D),
                Telerik.Reporting.Drawing.Unit.Cm(0D));
            transactionDateDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(
                Telerik.Reporting.Drawing.Unit.Cm(6.97D),
                Telerik.Reporting.Drawing.Unit.Cm(0.5D));
            groupHeaderSection1.Items.Add(transactionDateDataTextBox);
            groupHeaderSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(
                groupHeaderSection1.Height.Value + dateRowHeightCm);
        }

        private void ExpandTotalsLabels()
        {
            var labels = new[] { textBox9, textBox19, textBox111, textBox16, textBox10 };
            foreach (var label in labels)
            {
                label.Location = new Telerik.Reporting.Drawing.PointU(
                    Telerik.Reporting.Drawing.Unit.Cm(0.3D),label.Location.Y);
                label.Size = new Telerik.Reporting.Drawing.SizeU(
                    Telerik.Reporting.Drawing.Unit.Cm(4.9D),label.Size.Height);
                label.CanGrow = false;
                label.TextWrap = false;
            }
        }

        /// <summary>
        /// Places the accepted fiscal evidence in the report's existing signature
        /// area. The roll page dimensions are supplied later by the printer profile;
        /// they must remain the fixed values selected by the user.
        /// </summary>
        public void ApplySharedThermalLayout(string signature, string evidence)
        {
            // SetSupplyNumber may hide the first QR and shorten the footer before
            // the accepted myDATA/provider QR is assigned to pictureBox2.
            double requiredQrFooterHeightCm = pageFooterSection1.Height.Value;
            int qrCodeCount = 0;
            if (pictureBox1.Visible && pictureBox1.Value != null)
            {
                qrCodeCount++;
            }
            if (pictureBox2.Visible && pictureBox2.Value != null)
            {
                qrCodeCount++;
            }
            if (pictureBox3.Visible && pictureBox3.Value != null)
            {
                qrCodeCount++;
            }
            if (qrCodeCount > 0 && qrCodeCount <= 2)
            {
                requiredQrFooterHeightCm = System.Math.Max(
                    requiredQrFooterHeightCm,TwoQrFooterHeightCm);
            }
            if (qrCodeCount > 2)
            {
                requiredQrFooterHeightCm = System.Math.Max(
                    requiredQrFooterHeightCm,ThreeQrFooterHeightCm);
            }
            if (requiredQrFooterHeightCm > pageFooterSection1.Height.Value)
                pageFooterSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(requiredQrFooterHeightCm);

            string signLine = "\u03A7\u03CE\u03C1\u03BF\u03C2 \u03A3\u03AE\u03BC\u03B1\u03BD\u03C3\u03B7\u03C2:";
            if (!string.IsNullOrWhiteSpace(signature))
                signLine += " " + signature.Trim();
            string value = string.IsNullOrWhiteSpace(evidence)
                ? signLine
                : signLine + "\r\n" + evidence.Trim();
            textBox11.Value = value;

            int visualLines = CountVisualLines(value,54);
            double lineHeightCm = System.Math.Max(0.38D,textBox11.Style.Font.Size.Value * 0.045D);
            double requiredEvidenceHeightCm = System.Math.Max(1.1066669225692749D,
                visualLines * lineHeightCm + 0.10D);
            double extraEvidenceHeightCm = requiredEvidenceHeightCm - textBox11.Size.Height.Value;
            if (extraEvidenceHeightCm > 0.001D)
            {
                textBox11.Size = new Telerik.Reporting.Drawing.SizeU(
                    textBox11.Size.Width,
                    Telerik.Reporting.Drawing.Unit.Cm(requiredEvidenceHeightCm));
                ShiftFooterItemsBelowEvidence(extraEvidenceHeightCm);
                pageFooterSection1.Height = Telerik.Reporting.Drawing.Unit.Cm(
                    pageFooterSection1.Height.Value + extraEvidenceHeightCm);
            }

        }

        /// <summary>
        /// A Telerik PageFooter is anchored to the physical bottom of a fixed
        /// page, which leaves unused paper between the lines and totals. Move the
        /// existing footer items into a ReportFooter so they flow immediately
        /// after the details; any configured spare paper remains below them.
        /// </summary>
        public void FlowFooterAfterDetails()
        {
            if (thermalFooterFlowsAfterDetails)
                return;
            thermalFooterFlowsAfterDetails = true;

            var items = new System.Collections.Generic.List<Telerik.Reporting.ReportItemBase>();
            foreach (object entry in pageFooterSection1.Items)
            {
                var item = entry as Telerik.Reporting.ReportItemBase;
                if (item != null)
                    items.Add(item);
            }

            var footer = new Telerik.Reporting.ReportFooterSection
            {
                Name = "thermalReportFooterSection",
                Height = pageFooterSection1.Height
            };
            pageFooterSection1.Items.Clear();
            footer.Items.AddRange(items.ToArray());
            Items.Add(footer);

            pageFooterSection1.Height = Telerik.Reporting.Drawing.Unit.Mm(0D);
            pageFooterSection1.Style.Visible = false;
        }

        private void ShiftFooterItemsBelowEvidence(double offsetCm)
        {
            foreach (object entry in pageFooterSection1.Items)
            {
                var item = entry as Telerik.Reporting.ReportItem;
                if (item == null || object.ReferenceEquals(item,textBox11) ||
                    item.Location.Y.Value < SharedFooterQrStartCm)
                    continue;
                item.Location = new Telerik.Reporting.Drawing.PointU(
                    item.Location.X,
                    Telerik.Reporting.Drawing.Unit.Cm(item.Location.Y.Value + offsetCm));
            }
        }

        private static int CountVisualLines(string text, int charactersPerLine)
        {
            int count = 0;
            foreach (string line in (text ?? "").Split(new[] { "\r\n", "\n" },
                System.StringSplitOptions.None))
                count += System.Math.Max(1,(line.Length + charactersPerLine - 1) / charactersPerLine);
            return System.Math.Max(1,count);
        }
    }
}
