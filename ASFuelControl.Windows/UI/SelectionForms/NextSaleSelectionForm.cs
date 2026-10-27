using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using ASFuelControl.Windows.UI.Forms;
using Exedron.Invoicing.Core;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    /// <summary>
    /// Consolidated preparation of the next dispenser sale: customer, vehicle,
    /// invoice type and an optional staged myDiesel coupon.
    /// </summary>
    internal sealed class NextSaleSelectionForm : RadForm
    {
        private readonly Guid dispenserId;
        private readonly Guid initialVehicleId;
        private readonly Guid initialInvoiceTypeId;
        private readonly int initialOdometer;
        private readonly bool myDieselVisible;

        private readonly RadTextBox filter = new RadTextBox { Dock = DockStyle.Fill };
        private readonly RadGridView vehicles = new RadGridView { Dock = DockStyle.Fill };
        private readonly RadTextBox selectedTrader = ReadOnlyTextBox();
        private readonly RadTextBox selectedVehicle = ReadOnlyTextBox();
        private readonly RadDropDownList invoiceType = new RadDropDownList { Dock = DockStyle.Fill };
        private readonly RadSpinEditor odometer = new RadSpinEditor { Dock = DockStyle.Fill };
        private readonly RadTextBox couponCode = ReadOnlyTextBox();
        private readonly RadTextBox beneficiaryVat = ReadOnlyTextBox();
        private readonly RadTextBox couponStatus = ReadOnlyTextBox();
        private readonly RadTextBox couponLimit = ReadOnlyTextBox();
        private readonly RadTextBox couponFuel = ReadOnlyTextBox();
        private readonly RadTextBox exciseDiscount = ReadOnlyTextBox();
        private readonly RadLabel couponMessage = new RadLabel { AutoSize = false, Dock = DockStyle.Fill, TextAlignment = ContentAlignment.MiddleLeft };
        private readonly RadButton scanCoupon = new RadButton();
        private readonly RadButton apply = new RadButton();

        private MyDieselCaptureInput stagedCoupon;
        private bool myDieselEnabled;
        private decimal configuredExciseDiscount;

        internal ViewModels.VehicleViewModel SelectedVehicle { get; private set; }
        internal Guid SelectedInvoiceTypeId { get; private set; }
        internal int SelectedOdometer { get; private set; }
        internal bool SelectionCleared { get; private set; }

        internal NextSaleSelectionForm(Guid dispenserId, Guid vehicleId, Guid invoiceTypeId, int currentOdometer)
        {
            this.dispenserId = dispenserId;
            initialVehicleId = vehicleId;
            initialInvoiceTypeId = invoiceTypeId;
            initialOdometer = currentOdometer;
            myDieselVisible = Data.Implementation.OptionHandler.Instance.MyDieselEnabled;
            InitializeForm();
            LoadInvoiceTypes();
            LoadMyDieselState();
            LoadVehicles();
        }

        private void InitializeForm()
        {
            Text = "Στοιχεία επόμενης πώλησης";
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            MinimumSize = new Size(900, myDieselVisible ? 650 : 460);
            ClientSize = new Size(900, myDieselVisible ? 650 : 460);
            Font = new Font("Segoe UI", 9F);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 6 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, myDieselVisible ? 190 : 0));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            var search = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            search.Controls.Add(new RadLabel { Text = "Φίλτρο", Dock = DockStyle.Fill, TextAlignment = ContentAlignment.MiddleLeft }, 0, 0);
            search.Controls.Add(filter, 1, 0);
            var searchButton = new RadButton { Text = "⌕", Dock = DockStyle.Fill, Margin = new Padding(5, 1, 0, 4) };
            searchButton.Click += (s, e) => LoadVehicles();
            search.Controls.Add(searchButton, 2, 0);
            filter.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; LoadVehicles(); } };
            root.Controls.Add(search, 0, 0);

            ConfigureVehicleGrid();
            root.Controls.Add(vehicles, 0, 1);
            root.Controls.Add(CreateSaleGroup(), 0, 2);
            if (myDieselVisible) root.Controls.Add(CreateCouponGroup(), 0, 3);

            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            var addCustomer = new RadButton { Text = "+ Νέος πελάτης", Dock = DockStyle.Fill, Margin = new Padding(0, 4, 7, 0) };
            addCustomer.Click += AddCustomer;
            var clear = new RadButton { Text = "Καθαρισμός", Dock = DockStyle.Fill, Margin = new Padding(0, 4, 7, 0) };
            clear.Click += ClearSelection;
            var cancel = new RadButton { Text = "Άκυρο", Dock = DockStyle.Fill, DialogResult = DialogResult.Cancel, Margin = new Padding(0, 4, 7, 0) };
            apply.Text = "Ορισμός για επόμενη πώληση";
            apply.Dock = DockStyle.Fill;
            apply.Margin = new Padding(0, 4, 0, 0);
            apply.Click += Apply;
            actions.Controls.Add(addCustomer, 0, 0);
            actions.Controls.Add(clear, 2, 0);
            actions.Controls.Add(cancel, 3, 0);
            actions.Controls.Add(apply, 4, 0);
            root.Controls.Add(actions, 0, 5);

            Controls.Add(root);
            AcceptButton = apply;
            CancelButton = cancel;
        }

        private void ConfigureVehicleGrid()
        {
            vehicles.AutoGenerateColumns = false;
            vehicles.ShowGroupPanel = false;
            vehicles.AllowAddNewRow = false;
            vehicles.AllowDeleteRow = false;
            vehicles.ReadOnly = true;
            vehicles.EnableAlternatingRowColor = true;
            vehicles.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
            vehicles.Columns.Add(TextColumn("TraderName", "Συναλλασσόμενος", 250));
            vehicles.Columns.Add(TextColumn("TraderTaxRegistration", "Α.Φ.Μ.", 100));
            vehicles.Columns.Add(TextColumn("PlateNumber", "Πινακίδες", 105));
            vehicles.Columns.Add(TextColumn("TraderDeliveryAddress", "Παράδοση", 150));
            vehicles.Columns.Add(new GridViewDecimalColumn
            {
                FieldName = "Odometer", Name = "Odometer", HeaderText = "Χιλιόμετρα",
                DecimalPlaces = 0, Minimum = 0, Maximum = 999999999, Width = 105
            });
            vehicles.CurrentRowChanged += (s, e) => SelectCurrentVehicle();
            vehicles.CellDoubleClick += (s, e) => { if (e.Row != null) Apply(s, EventArgs.Empty); };
        }

        private static GridViewTextBoxColumn TextColumn(string field, string header, int width)
        {
            return new GridViewTextBoxColumn { FieldName = field, Name = field, HeaderText = header, Width = width, ReadOnly = true };
        }

        private Control CreateSaleGroup()
        {
            var group = new GroupBox { Text = "Επόμενη πώληση", Dock = DockStyle.Fill };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10, 6, 10, 7), ColumnCount = 4, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            for (int row = 0; row < 3; row++) layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            AddField(layout, 0, 0, "Συναλλασσόμενος", selectedTrader, 3);
            AddField(layout, 1, 0, "Όχημα", selectedVehicle, 1);
            AddField(layout, 1, 2, "Χιλιόμετρα", odometer, 1);
            AddField(layout, 2, 0, "Τύπος παραστατικού", invoiceType, 3);
            odometer.Minimum = 0;
            odometer.Maximum = 999999999;
            odometer.DecimalPlaces = 0;
            group.Controls.Add(layout);
            return group;
        }

        private Control CreateCouponGroup()
        {
            var group = new GroupBox { Text = "Κουπόνι myDiesel (προαιρετικό)", Dock = DockStyle.Fill };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10, 7, 10, 7), ColumnCount = 6, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 195));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(new RadLabel { Text = "QR JSON ή κωδικός κουπονιού", Dock = DockStyle.Fill, TextAlignment = ContentAlignment.MiddleLeft }, 0, 0);
            layout.Controls.Add(couponCode, 1, 0);
            layout.SetColumnSpan(couponCode, 3);
            scanCoupon.Text = "Σάρωση / επικόλληση";
            scanCoupon.Dock = DockStyle.Fill;
            scanCoupon.Margin = new Padding(7, 2, 0, 3);
            scanCoupon.Click += ScanCoupon;
            layout.Controls.Add(scanCoupon, 4, 0);
            layout.SetColumnSpan(scanCoupon, 2);

            AddField(layout, 1, 0, "ΑΦΜ δικαιούχου", beneficiaryVat, 1);
            AddField(layout, 1, 2, "Κατάσταση", couponStatus, 1);
            AddField(layout, 1, 4, "Όριο", couponLimit, 1);
            AddField(layout, 2, 0, "Καύσιμο", couponFuel, 1);
            AddField(layout, 2, 2, "Μείωση ΕΦΚ", exciseDiscount, 1);
            var remove = new RadButton { Text = "Αφαίρεση κουπονιού", Dock = DockStyle.Fill, Margin = new Padding(7, 2, 0, 3) };
            remove.Click += (s, e) => { stagedCoupon = null; RenderCoupon(); };
            layout.Controls.Add(remove, 4, 2);
            layout.SetColumnSpan(remove, 2);

            couponMessage.BackColor = Color.FromArgb(238, 247, 253);
            couponMessage.ForeColor = Color.FromArgb(20, 86, 122);
            couponMessage.Padding = new Padding(10, 3, 4, 3);
            layout.Controls.Add(couponMessage, 0, 3);
            layout.SetColumnSpan(couponMessage, 6);
            group.Controls.Add(layout);
            return group;
        }

        private static void AddField(TableLayoutPanel layout, int row, int column, string label, Control value, int span)
        {
            layout.Controls.Add(new RadLabel { Text = label, Dock = DockStyle.Fill, TextAlignment = ContentAlignment.MiddleLeft }, column, row);
            value.Dock = DockStyle.Fill;
            value.Margin = new Padding(2, 3, 5, 3);
            layout.Controls.Add(value, column + 1, row);
            if (span > 1) layout.SetColumnSpan(value, span);
        }

        private static RadTextBox ReadOnlyTextBox()
        {
            return new RadTextBox { ReadOnly = true, Dock = DockStyle.Fill, TabStop = false };
        }

        private void LoadInvoiceTypes()
        {
            var values = ViewModels.CommonCache.Instance.InvoiceTypes.Where(value => value.DispenserTypeEx).ToArray();
            invoiceType.DisplayMember = "Description";
            invoiceType.ValueMember = "InvoiceTypeId";
            invoiceType.DataSource = values;
            invoiceType.DropDownStyle = RadDropDownStyle.DropDownList;
            SelectInvoiceType(initialInvoiceTypeId);
        }

        private void LoadMyDieselState()
        {
            if (!myDieselVisible)
            {
                myDieselEnabled = false;
                stagedCoupon = null;
                PendingMyDieselSelectionStore.Clear(dispenserId);
                return;
            }

            try
            {
                var configuration = SharedInvoicingSettings.LoadRuntime();
                myDieselEnabled = configuration.MyDiesel != null && configuration.MyDiesel.Enabled;
                configuredExciseDiscount = myDieselEnabled ? configuration.MyDiesel.ExciseDiscountPerLitre : 0;
            }
            catch
            {
                myDieselEnabled = false;
            }

            var pending = PendingMyDieselSelectionStore.Get(dispenserId);
            if (pending != null)
                stagedCoupon = new MyDieselCaptureInput { Identity = pending.Identity, Qr = pending.Qr };
            RenderCoupon();
        }

        private void LoadVehicles()
        {
            Guid keep = SelectedVehicle == null ? initialVehicleId : SelectedVehicle.VehicleId;
            var list = new List<ViewModels.VehicleViewModel>();
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var query = db.Vehicles;
                string text = (filter.Text ?? "").Trim();
                if (text.Length > 0)
                    query = query.Where(value => value.Trader.Name.Contains(text) ||
                        value.Trader.TaxRegistrationNumber.Contains(text) || value.Trader.DeliveryAddress.Contains(text) ||
                        value.PlateNumber.Contains(text));
                foreach (var entity in query.OrderBy(value => value.Trader.Name).ThenBy(value => value.PlateNumber).ToArray())
                {
                    var model = new ViewModels.VehicleViewModel();
                    model.Load(db, entity.VehicleId);
                    model.TraderName = entity.Trader.Name;
                    model.TraderTaxRegistration = entity.Trader.TaxRegistrationNumber;
                    model.TraderDeliveryAddress = entity.Trader.DeliveryAddress;
                    model.DefaultInvoiceTypeId = entity.Trader.InvoiceTypeId ?? Guid.Empty;
                    list.Add(model);
                }
            }

            vehicles.DataSource = list;
            var row = vehicles.Rows.FirstOrDefault(value => ((ViewModels.VehicleViewModel)value.DataBoundItem).VehicleId == keep);
            if (row != null) vehicles.CurrentRow = row;
            else if (vehicles.Rows.Count > 0) vehicles.CurrentRow = vehicles.Rows[0];
            SelectCurrentVehicle();
        }

        private void SelectCurrentVehicle()
        {
            var value = vehicles.CurrentRow == null ? null : vehicles.CurrentRow.DataBoundItem as ViewModels.VehicleViewModel;
            SelectedVehicle = value;
            if (value == null)
            {
                selectedTrader.Text = "";
                selectedVehicle.Text = "";
                apply.Enabled = false;
                return;
            }
            selectedTrader.Text = value.TraderName + " · ΑΦΜ " + value.TraderTaxRegistration;
            selectedVehicle.Text = value.PlateNumber;
            odometer.Value = value.VehicleId == initialVehicleId && initialOdometer > 0 ? initialOdometer : value.Odometer;
            Guid preferred = value.VehicleId == initialVehicleId && initialInvoiceTypeId != Guid.Empty
                ? initialInvoiceTypeId : value.DefaultInvoiceTypeId;
            SelectInvoiceType(preferred);
            apply.Enabled = true;
            RenderCoupon();
        }

        private void SelectInvoiceType(Guid id)
        {
            if (id == Guid.Empty) return;
            for (int index = 0; index < invoiceType.Items.Count; index++)
            {
                var value = invoiceType.Items[index].DataBoundItem as ViewModels.InvoiceTypeViewModel;
                if (value != null && value.InvoiceTypeId == id)
                {
                    invoiceType.SelectedIndex = index;
                    return;
                }
            }
        }

        private void ScanCoupon(object sender, EventArgs e)
        {
            if (!myDieselEnabled)
            {
                RadMessageBox.Show("Η δημιουργία νέων δεσμεύσεων myDiesel δεν είναι ενεργή στις ρυθμίσεις.",
                    "myDiesel", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            string traderVat = SelectedVehicle == null ? null : SelectedVehicle.TraderTaxRegistration;
            using (var dialog = new MyDieselCouponCaptureDialog(traderVat, false))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                stagedCoupon = dialog.Value;
                RenderCoupon();
            }
        }

        private void RenderCoupon()
        {
            scanCoupon.Enabled = myDieselEnabled;
            if (stagedCoupon == null)
            {
                couponCode.Text = "";
                beneficiaryVat.Text = "";
                couponStatus.Text = myDieselEnabled ? "Δεν έχει καταχωριστεί" : "Ανενεργό";
                couponLimit.Text = "";
                couponFuel.Text = "";
                exciseDiscount.Text = myDieselEnabled ? configuredExciseDiscount.ToString("0.0000") + " €/L" : "";
                couponMessage.Text = myDieselEnabled
                    ? "ℹ  Το ΑΦΜ προέρχεται από τον επιλεγμένο πελάτη. Η δέσμευση γίνεται όταν δημιουργηθεί το παραστατικό της πώλησης."
                    : "ℹ  Ενεργοποιήστε και ελέγξτε τις ρυθμίσεις myDiesel για να καταχωρίσετε κουπόνι.";
                couponMessage.ForeColor = Color.FromArgb(20, 86, 122);
                return;
            }

            couponCode.Text = stagedCoupon.Identity.CouponCode;
            beneficiaryVat.Text = stagedCoupon.Identity.BeneficiaryVat;
            couponStatus.Text = "Έτοιμο για δέσμευση";
            couponLimit.Text = stagedCoupon.Qr == null ? "Θα ελεγχθεί" : stagedCoupon.Qr.IssuedLitres.ToString("0.00") + " L";
            couponFuel.Text = "Θα ελεγχθεί μετά την πώληση";
            exciseDiscount.Text = configuredExciseDiscount.ToString("0.0000") + " €/L";

            string traderVat = SelectedVehicle == null ? null : (SelectedVehicle.TraderTaxRegistration ?? "").Trim();
            bool mismatch = traderVat != null && traderVat != stagedCoupon.Identity.BeneficiaryVat;
            couponMessage.ForeColor = mismatch ? Color.FromArgb(170, 60, 35) : Color.FromArgb(20, 86, 122);
            couponMessage.Text = mismatch
                ? "⚠  Το ΑΦΜ του επιλεγμένου πελάτη δεν συμφωνεί με το κουπόνι. Επιλέξτε τον σωστό πελάτη ή αφαιρέστε το κουπόνι."
                : "✓  Το ΑΦΜ συμφωνεί. Καύσιμο και λίτρα θα ελεγχθούν πριν από τη δέσμευση· αν δεν είναι επιλέξιμα, το παραστατικό θα συνεχίσει χωρίς κουπόνι.";
            apply.Enabled = SelectedVehicle != null && !mismatch;
        }

        private void AddCustomer(object sender, EventArgs e)
        {
            string requiredVat = stagedCoupon == null ? null : stagedCoupon.Identity.BeneficiaryVat;
            Guid preferred = SelectedInvoiceType();
            using (var dialog = new QuickCustomerForm(requiredVat, preferred))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                SelectedVehicle = null;
                LoadVehicles();
                var row = vehicles.Rows.FirstOrDefault(value =>
                    ((ViewModels.VehicleViewModel)value.DataBoundItem).VehicleId == dialog.SelectedVehicleId);
                if (row != null) vehicles.CurrentRow = row;
                odometer.Value = dialog.SelectedOdometer;
                SelectInvoiceType(dialog.SelectedInvoiceTypeId);
                SelectCurrentVehicle();
            }
        }

        private Guid SelectedInvoiceType()
        {
            var value = invoiceType.SelectedItem == null
                ? null
                : invoiceType.SelectedItem.DataBoundItem as ViewModels.InvoiceTypeViewModel;
            return value == null ? Guid.Empty : value.InvoiceTypeId;
        }

        private void Apply(object sender, EventArgs e)
        {
            if (SelectedVehicle == null)
            {
                RadMessageBox.Show("Επιλέξτε πελάτη και όχημα.", "Επόμενη πώληση", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            Guid typeId = SelectedInvoiceType();
            if (typeId == Guid.Empty)
            {
                RadMessageBox.Show("Επιλέξτε τύπο παραστατικού.", "Επόμενη πώληση", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            if (stagedCoupon != null && (SelectedVehicle.TraderTaxRegistration ?? "").Trim() != stagedCoupon.Identity.BeneficiaryVat)
            {
                RadMessageBox.Show("Το ΑΦΜ του πελάτη δεν συμφωνεί με το ΑΦΜ δικαιούχου του κουπονιού.",
                    "myDiesel", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }

            SelectedInvoiceTypeId = typeId;
            SelectedOdometer = Decimal.ToInt32(odometer.Value);
            if (stagedCoupon == null) PendingMyDieselSelectionStore.Clear(dispenserId);
            else PendingMyDieselSelectionStore.Set(dispenserId, stagedCoupon.Identity, stagedCoupon.Qr);
            DialogResult = DialogResult.OK;
        }

        private void ClearSelection(object sender, EventArgs e)
        {
            PendingMyDieselSelectionStore.Clear(dispenserId);
            SelectionCleared = true;
            DialogResult = DialogResult.OK;
        }
    }
}
