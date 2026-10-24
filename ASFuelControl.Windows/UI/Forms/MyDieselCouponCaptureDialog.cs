using System;
using System.Drawing;
using System.Windows.Forms;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class MyDieselCaptureInput
    {
        internal MyDieselCouponIdentity Identity { get; set; }
        internal MyDieselQrCoupon Qr { get; set; }
    }

    /// <summary>
    /// Shared scanner/manual-entry dialog.  A supplied trader VAT is authoritative
    /// and is therefore shown read-only and checked against the QR payload.
    /// </summary>
    internal sealed class MyDieselCouponCaptureDialog : RadForm
    {
        private readonly RadTextBox qr = new RadTextBox { Multiline = true, Dock = DockStyle.Fill };
        private readonly RadTextBox code = new RadTextBox { Dock = DockStyle.Fill };
        private readonly RadTextBox vat = new RadTextBox { Dock = DockStyle.Fill };
        private readonly string traderVat;

        internal MyDieselCaptureInput Value { get; private set; }

        internal MyDieselCouponCaptureDialog(string traderVat, bool reserveImmediately)
        {
            this.traderVat = string.IsNullOrWhiteSpace(traderVat)
                ? null
                : MyDieselCouponIdentity.NormalizeVat(traderVat, "ΑΦΜ πελάτη");

            Text = "Σάρωση κουπονιού myDiesel";
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ClientSize = new Size(720, 390);
            MinimumSize = new Size(680, 360);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(16) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

            var qrLabel = new RadLabel { Text = "QR JSON (σάρωση ή επικόλληση)", Dock = DockStyle.Fill };
            table.Controls.Add(qrLabel, 0, 0);
            table.SetColumnSpan(qrLabel, 2);
            table.Controls.Add(qr, 0, 1);
            table.SetColumnSpan(qr, 2);
            table.Controls.Add(new RadLabel { Text = "ή χειροκίνητος κωδικός", Dock = DockStyle.Fill }, 0, 2);
            table.Controls.Add(code, 1, 2);

            bool hasTraderVat = this.traderVat != null;
            vat.Text = this.traderVat ?? "";
            vat.ReadOnly = hasTraderVat;
            vat.TabStop = !hasTraderVat;
            table.Controls.Add(new RadLabel
            {
                Text = hasTraderVat ? "ΑΦΜ δικαιούχου (από πελάτη)" : "ΑΦΜ δικαιούχου (9 ψηφία)",
                Dock = DockStyle.Fill
            }, 0, 3);
            table.Controls.Add(vat, 1, 3);

            var accept = new RadButton
            {
                Text = reserveImmediately ? "Έλεγχος και δέσμευση" : "Καταχώριση κουπονιού",
                Dock = DockStyle.Right,
                Width = 190,
                Height = 34
            };
            accept.Click += Accept;
            table.Controls.Add(accept, 1, 4);
            Controls.Add(table);
            AcceptButton = accept;
        }

        private void Accept(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(qr.Text))
                {
                    var parsed = MyDieselQrParser.Parse(qr.Text.Trim());
                    if (traderVat != null && parsed.Identity.BeneficiaryVat != traderVat)
                        throw new InvalidOperationException("Το ΑΦΜ δικαιούχου του κουπονιού δεν συμφωνεί με το ΑΦΜ του επιλεγμένου πελάτη.");
                    Value = new MyDieselCaptureInput { Identity = parsed.Identity, Qr = parsed };
                }
                else
                {
                    Value = new MyDieselCaptureInput
                    {
                        Identity = new MyDieselCouponIdentity(code.Text, traderVat ?? vat.Text)
                    };
                }
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(ex.Message, "Κουπόνι myDiesel", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
            }
        }
    }
}
