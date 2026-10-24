using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Exedron.Invoicing.Core;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    /// <summary>
    /// Focused customer and vehicle creation used while preparing the next pump sale.
    /// It deliberately excludes account history, invoices and financial transactions.
    /// </summary>
    public sealed class QuickCustomerForm : RadForm
    {
        private readonly string requiredVat;
        private readonly Guid preferredInvoiceTypeId;

        private readonly RadTextBox customerName = new RadTextBox();
        private readonly RadTextBox vat = new RadTextBox();
        private readonly RadTextBox taxOffice = new RadTextBox();
        private readonly RadTextBox address = new RadTextBox();
        private readonly RadTextBox city = new RadTextBox();
        private readonly RadTextBox postalCode = new RadTextBox();
        private readonly RadDropDownList invoiceType = new RadDropDownList();
        private readonly RadTextBox plateNumber = new RadTextBox();
        private readonly RadSpinEditor odometer = new RadSpinEditor();
        private readonly RadButton saveAndSelect = new RadButton();
        private readonly RadButton cancel = new RadButton();

        public Guid SelectedCustomerId { get; private set; }
        public Guid SelectedVehicleId { get; private set; }
        public Guid SelectedInvoiceTypeId { get; private set; }
        public int SelectedOdometer { get; private set; }

        public QuickCustomerForm()
            : this(null, Guid.Empty)
        {
        }

        public QuickCustomerForm(string requiredTaxRegistrationNumber, Guid preferredInvoiceType)
        {
            if (!string.IsNullOrWhiteSpace(requiredTaxRegistrationNumber))
                requiredVat = MyDieselCouponIdentity.NormalizeVat(requiredTaxRegistrationNumber, "ΑΦΜ δικαιούχου");
            preferredInvoiceTypeId = preferredInvoiceType;

            InitializeForm();
            LoadInvoiceTypes();
            if (requiredVat != null)
            {
                vat.Text = requiredVat;
                vat.ReadOnly = true;
            }
        }

        private void InitializeForm()
        {
            Text = "Νέος πελάτης επόμενης πώλησης";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ClientSize = new Size(720, 438);
            Font = new Font("Segoe UI", 9.25F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 232F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 105F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var introduction = new RadLabel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Καταχωρήστε μόνο τα στοιχεία που απαιτούνται για την έκδοση της επόμενης πώλησης.",
                TextAlignment = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(introduction, 0, 0);

            var customerGroup = new GroupBox { Text = "Στοιχεία πελάτη", Dock = DockStyle.Fill };
            var customerLayout = CreateDetailsLayout(6);
            AddField(customerLayout, 0, "Επωνυμία / Ονοματεπώνυμο *", customerName, true);
            AddField(customerLayout, 1, "Α.Φ.Μ. *", vat, false);
            AddField(customerLayout, 1, "Δ.Ο.Υ.", taxOffice, false, 2);
            AddField(customerLayout, 2, "Διεύθυνση *", address, true);
            AddField(customerLayout, 3, "Πόλη *", city, false);
            AddField(customerLayout, 3, "Τ.Κ.", postalCode, false, 2);
            AddField(customerLayout, 4, "Προεπιλ. τύπος παραστατικού *", invoiceType, true);
            var vatNote = new RadLabel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Για myDiesel το ΑΦΜ πρέπει να έχει 9 ψηφία. Όταν προέρχεται από κουπόνι παραμένει μόνο για ανάγνωση.",
                ForeColor = Color.DimGray,
                TextAlignment = ContentAlignment.MiddleLeft
            };
            customerLayout.SetColumnSpan(vatNote, 4);
            customerLayout.Controls.Add(vatNote, 0, 5);
            customerGroup.Controls.Add(customerLayout);
            root.Controls.Add(customerGroup, 0, 1);

            var vehicleGroup = new GroupBox { Text = "Όχημα", Dock = DockStyle.Fill };
            var vehicleLayout = CreateDetailsLayout(2);
            AddField(vehicleLayout, 0, "Αριθμός κυκλοφορίας *", plateNumber, false);
            AddField(vehicleLayout, 0, "Χιλιόμετρα", odometer, false, 2);
            var vehicleNote = new RadLabel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Ο πελάτης και το όχημα αποθηκεύονται μαζί και επιλέγονται αυτόματα.",
                ForeColor = Color.DimGray,
                TextAlignment = ContentAlignment.MiddleLeft
            };
            vehicleLayout.SetColumnSpan(vehicleNote, 4);
            vehicleLayout.Controls.Add(vehicleNote, 0, 1);
            vehicleGroup.Controls.Add(vehicleLayout);
            root.Controls.Add(vehicleGroup, 0, 2);

            odometer.Minimum = 0;
            odometer.Maximum = 999999999;
            odometer.DecimalPlaces = 0;

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0),
                WrapContents = false
            };
            saveAndSelect.Text = "Αποθήκευση και επιλογή";
            saveAndSelect.Size = new Size(170, 34);
            saveAndSelect.Click += SaveAndSelect_Click;
            cancel.Text = "Άκυρο";
            cancel.Size = new Size(95, 34);
            cancel.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(saveAndSelect);
            actions.Controls.Add(cancel);
            root.Controls.Add(actions, 0, 3);

            AcceptButton = saveAndSelect;
            CancelButton = cancel;
            Controls.Add(root);
        }

        private static TableLayoutPanel CreateDetailsLayout(int rows)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 6),
                ColumnCount = 4,
                RowCount = rows
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int index = 0; index < rows; index++)
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
            return layout;
        }

        private static void AddField(TableLayoutPanel layout, int row, string labelText, Control control, bool span, int labelColumn = 0)
        {
            var label = new RadLabel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = labelText,
                TextAlignment = ContentAlignment.MiddleLeft
            };
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(2, 3, 2, 3);
            layout.Controls.Add(label, labelColumn, row);
            layout.Controls.Add(control, labelColumn + 1, row);
            if (span)
                layout.SetColumnSpan(control, 3);
        }

        private void LoadInvoiceTypes()
        {
            var values = ViewModels.CommonCache.Instance.InvoiceTypes.Where(value => value.DispenserTypeEx).ToArray();
            invoiceType.DisplayMember = "Description";
            invoiceType.ValueMember = "InvoiceTypeId";
            invoiceType.DataSource = values;
            invoiceType.DropDownStyle = RadDropDownStyle.DropDownList;

            if (preferredInvoiceTypeId == Guid.Empty)
                return;
            for (int index = 0; index < invoiceType.Items.Count; index++)
            {
                var value = invoiceType.Items[index].DataBoundItem as ViewModels.InvoiceTypeViewModel;
                if (value != null && value.InvoiceTypeId == preferredInvoiceTypeId)
                {
                    invoiceType.SelectedIndex = index;
                    break;
                }
            }
        }

        private void SaveAndSelect_Click(object sender, EventArgs e)
        {
            string normalizedVat;
            if (!ValidateInput(out normalizedVat))
                return;

            try
            {
                using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    if (db.Traders.Any(value => value.TaxRegistrationNumber == normalizedVat))
                    {
                        RadMessageBox.Show("Υπάρχει ήδη πελάτης με αυτό το ΑΦΜ. Επιλέξτε τον από τη λίστα.",
                            "Υπάρχων πελάτης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                }

                var selectedType = invoiceType.SelectedItem == null
                    ? null
                    : invoiceType.SelectedItem.DataBoundItem as ViewModels.InvoiceTypeViewModel;
                if (selectedType == null)
                    return;

                var model = new ViewModels.TraderDetailsViewModel();
                model.CreateNew();
                model.Name = customerName.Text.Trim();
                model.TaxRegistrationNumber = normalizedVat;
                model.TaxRegistrationOffice = taxOffice.Text.Trim();
                model.Address = address.Text.Trim();
                model.City = city.Text.Trim();
                model.ZipCode = postalCode.Text.Trim();
                model.Country = "GR";
                model.IsCustomer = true;
                model.InvoiceTypeId = selectedType.InvoiceTypeId;
                model.AddVehicle();

                var vehicle = model.Vehicles.Single();
                vehicle.VehicleId = Guid.NewGuid();
                vehicle.PlateNumber = plateNumber.Text.Trim().ToUpperInvariant();
                vehicle.Odometer = Decimal.ToInt32(odometer.Value);

                if (!model.Save(model.TraderId))
                {
                    string message = model.Errors == null
                        ? "Η αποθήκευση του πελάτη απέτυχε."
                        : string.Join(Environment.NewLine, model.Errors.Select(value => value.ErrorMessage));
                    RadMessageBox.Show(message, "Σφάλμα αποθήκευσης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }

                SelectedCustomerId = model.TraderId;
                SelectedVehicleId = vehicle.VehicleId;
                SelectedInvoiceTypeId = selectedType.InvoiceTypeId;
                SelectedOdometer = vehicle.Odometer;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(ex.Message, "Σφάλμα αποθήκευσης", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private bool ValidateInput(out string normalizedVat)
        {
            normalizedVat = null;
            if (string.IsNullOrWhiteSpace(customerName.Text) || string.IsNullOrWhiteSpace(address.Text) ||
                string.IsNullOrWhiteSpace(city.Text) || string.IsNullOrWhiteSpace(plateNumber.Text))
            {
                RadMessageBox.Show("Συμπληρώστε επωνυμία, διεύθυνση, πόλη και αριθμό κυκλοφορίας.",
                    "Ελλιπή στοιχεία", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return false;
            }
            if (requiredVat != null)
            {
                try
                {
                    normalizedVat = MyDieselCouponIdentity.NormalizeVat(vat.Text, "ΑΦΜ πελάτη");
                }
                catch (ArgumentException ex)
                {
                    RadMessageBox.Show(ex.Message, "Μη έγκυρο ΑΦΜ", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return false;
                }
            }
            else
            {
                normalizedVat = (vat.Text ?? "").Trim().ToUpperInvariant();
                if (normalizedVat.Length < 9 || normalizedVat.Length > 17 ||
                    normalizedVat.Any(value => !char.IsLetterOrDigit(value)))
                {
                    RadMessageBox.Show("Το ΑΦΜ πρέπει να περιέχει από 9 έως 17 γράμματα ή ψηφία.",
                        "Μη έγκυρο ΑΦΜ", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return false;
                }
            }
            if (requiredVat != null && !string.Equals(requiredVat, normalizedVat, StringComparison.Ordinal))
            {
                RadMessageBox.Show("Το ΑΦΜ του πελάτη δεν συμφωνεί με το ΑΦΜ δικαιούχου του κουπονιού.",
                    "Ασυμφωνία ΑΦΜ", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return false;
            }
            if (invoiceType.SelectedItem == null)
            {
                RadMessageBox.Show("Επιλέξτε τύπο παραστατικού.", "Ελλιπή στοιχεία",
                    MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return false;
            }
            return true;
        }
    }
}
