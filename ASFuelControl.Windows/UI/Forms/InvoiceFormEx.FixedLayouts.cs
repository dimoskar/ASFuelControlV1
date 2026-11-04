using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceFormEx
    {
        private static readonly Font EditorFont = new Font("Segoe UI", 9.75F);

        private void InitializeFixedLayout(ViewModels.InvoiceEditorLayout layout)
        {
            bool supplier = layout == ViewModels.InvoiceEditorLayout.SupplierPurchase;
            components = new Container();
            InitializeFixedBindingSources();

            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;
            Font = EditorFont;
            MinimumSize = new Size(1050, supplier ? 620 : 760);
            ClientSize = new Size(1180, supplier ? 650 : 590);
            StartPosition = FormStartPosition.CenterScreen;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = supplier ? 3 : 4,
                Padding = new Padding(6),
                BackColor = Color.White
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, supplier ? 132F : 126F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            if (!supplier)
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            root.Controls.Add(BuildFixedActionBar(layout), 0, 0);
            root.Controls.Add(BuildFixedHeader(layout), 0, 1);
            root.Controls.Add(layout == ViewModels.InvoiceEditorLayout.RetailReceipt
                ? BuildRetailReceiptBody()
                : BuildDetailedBody(layout), 0, 2);
            if (!supplier)
                root.Controls.Add(BuildFixedSignaturePanel(), 0, 3);
            Controls.Add(root);
            ResumeLayout(false);
        }

        private void InitializeFixedBindingSources()
        {
            invoiceViewModelBindingSource = new BindingSource(components);
            invoiceLinesBindingSource = new BindingSource(components);
            invoiceTypesBindingSource = new BindingSource(components);
            fuelTypesBindingSource = new BindingSource(components);
            tanksBindingSource = new BindingSource(components);
            dispensersBindingSource = new BindingSource(components);
            nozzlesBindingSource = new BindingSource(components);
            paymentTypesBindingSource = new BindingSource(components);

            // Match the designer-generated initialization order.  Telerik's
            // dependent BindingSources need a typed parent before DataMember is
            // assigned; otherwise the drop-downs can display the item type name
            // and the nested dispenser/nozzle sources remain empty.
            invoiceViewModelBindingSource.DataSource = typeof(ViewModels.InvoiceDetailsViewModel);
            invoiceLinesBindingSource.DataMember = "InvoiceLines";
            invoiceLinesBindingSource.DataSource = invoiceViewModelBindingSource;
            invoiceTypesBindingSource.DataMember = "InvoiceTypes";
            invoiceTypesBindingSource.DataSource = invoiceViewModelBindingSource;
            fuelTypesBindingSource.DataMember = "FuelTypes";
            fuelTypesBindingSource.DataSource = invoiceViewModelBindingSource;
            tanksBindingSource.DataMember = "Tanks";
            tanksBindingSource.DataSource = invoiceViewModelBindingSource;
            dispensersBindingSource.DataMember = "Dispensers";
            dispensersBindingSource.DataSource = invoiceViewModelBindingSource;
            nozzlesBindingSource.DataMember = "Nozzles";
            nozzlesBindingSource.DataSource = dispensersBindingSource;
            paymentTypesBindingSource.DataMember = "PaymentTypes";
            paymentTypesBindingSource.DataSource = invoiceViewModelBindingSource;
            invoiceViewModelBindingSource.DataSource = ivm;
            invoiceLinesBindingSource.CurrentItemChanged += invoiceLinesBindingSource_CurrentItemChanged;
        }

        private Control BuildFixedHeader(ViewModels.InvoiceEditorLayout layout)
        {
            bool supplier = layout == ViewModels.InvoiceEditorLayout.SupplierPurchase;
            tableLayoutPanel1 = new Controls.DoubleBufferTableLayout
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 3,
                Margin = new Padding(0)
            };
            if (supplier)
            {
                // Supplier captions are considerably longer than the sales
                // captions. Give them dedicated space so labels never paint on
                // top of the adjacent editors at the minimum supported width.
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
            }
            else
            {
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
            }
            for (int i = 0; i < 3; i++)
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

            radDateTimePicker1 = NewDatePicker("TransactionDate");
            radDropDownList1 = NewDropDown(invoiceTypesBindingSource, "Description", "InvoiceTypeId");
            radDropDownList1.DataBindings.Add("SelectedValue", invoiceViewModelBindingSource,
                "InvoiceTypeId", true, DataSourceUpdateMode.OnPropertyChanged);
            radTextBox1 = NewTextBox("Series");
            radSpinEditor1 = NewSpin("Number", 0, 999999999);
            radTextBox3 = NewTextBox("TraderName", true);
            radTextBox4 = NewTextBox("PlateNumber", true);
            radTextBox7 = NewTextBox("DeliveryAddress");
            radTextBox5 = NewTextBox("SupplyNumber");
            radSpinEditor6 = NewSpin("VehicleOdometer", 0, 0);

            if (supplier)
            {
                AddHeaderCell(tableLayoutPanel1, "Προμηθευτής", radTextBox3, 0, 0);
                AddHeaderCell(tableLayoutPanel1, "Ημερομηνία Παραστατικού", radDateTimePicker1, 2, 0);
                AddHeaderCell(tableLayoutPanel1, "Σειρά Προμηθευτή", radTextBox1, 4, 0);
                AddHeaderCell(tableLayoutPanel1, "Αριθμός Προμηθευτή", radSpinEditor1, 6, 0);
                AddHeaderCell(tableLayoutPanel1, "Τύπος Παραστατικού", radDropDownList1, 0, 1, 3);
                AddHeaderCell(tableLayoutPanel1, "Τόπος Παράδοσης", radTextBox7, 4, 1, 3);
                AddHeaderCell(tableLayoutPanel1, "Αριθμός Αναφοράς", radTextBox5, 0, 2, 3);
            }
            else
            {
                AddHeaderCell(tableLayoutPanel1, "Ημερομηνία", radDateTimePicker1, 0, 0);
                AddHeaderCell(tableLayoutPanel1, "Τύπος Παραστατικού", radDropDownList1, 2, 0);
                AddHeaderCell(tableLayoutPanel1, "Σειρά", radTextBox1, 4, 0);
                AddHeaderCell(tableLayoutPanel1, "Αριθμός", radSpinEditor1, 6, 0);
                AddHeaderCell(tableLayoutPanel1, "Συναλλασσόμενος", radTextBox3, 0, 1, 3);
                AddHeaderCell(tableLayoutPanel1, "Όχημα", radTextBox4, 4, 1, 3);
                AddHeaderCell(tableLayoutPanel1, "Παράδοση", radTextBox7, 0, 2);
                AddHeaderCell(tableLayoutPanel1, "Αρ. Παροχής", radTextBox5, 2, 2);
                AddHeaderCell(tableLayoutPanel1, "Χιλιόμετρα", radSpinEditor6, 4, 2, 3);
            }
            tableLayoutPanel1.DataBindings.Add("Enabled", invoiceViewModelBindingSource, "EditEnabled", true);
            return tableLayoutPanel1;
        }

        private void AddHeaderCell(TableLayoutPanel host, string caption, Control editor,
            int column, int row, int editorSpan = 1)
        {
            var label = NewLabel(caption);
            host.Controls.Add(label, column, row);
            host.Controls.Add(editor, column + 1, row);
            if (editorSpan > 1)
                host.SetColumnSpan(editor, editorSpan);
        }

        private Control BuildFixedActionBar(ViewModels.InvoiceEditorLayout layout)
        {
            panel3 = new Panel { Dock = DockStyle.Fill };
            btnSelectTrader = NewToolbarButton(Properties.Resources.CustomerSelect36, btnSelectTrader_Click);
            btnSelectVehicle = NewToolbarButton(Properties.Resources.Vehicle, btnSelectVehicle_Click);
            btnSave = NewToolbarButton(Properties.Resources.Save, btnSave_Click);
            btnPrint = NewToolbarButton(Properties.Resources.Printer, btnPrint_Click);
            btnSelectTrader.Dock = DockStyle.Left;
            btnSelectVehicle.Dock = DockStyle.Left;
            btnSave.Dock = DockStyle.Right;
            btnPrint.Dock = DockStyle.Right;
            btnSave.DataBindings.Add("Enabled", invoiceViewModelBindingSource, "HasChanges", true);
            btnSelectTrader.DataBindings.Add("Enabled", invoiceViewModelBindingSource, "SelectCustomerEnabled", true);
            btnSelectVehicle.DataBindings.Add("Enabled", invoiceViewModelBindingSource, "SelectCustomerEnabled", true);

            panel6 = new Panel { Dock = DockStyle.Fill, Visible = false };
            radLabel35 = NewLabel("Τρόπος Πληρωμής");
            radDropDownList5 = NewPaymentDropDown();
            radLabel35.Dock = DockStyle.Left;
            radLabel35.Width = 135;
            radDropDownList5.Dock = DockStyle.Fill;
            panel6.Controls.Add(radDropDownList5);
            panel6.Controls.Add(radLabel35);

            // Preserve the original InvoiceFormEx toolbar order and controls on
            // every fixed editor, including purchases.
            panel3.Controls.Add(panel6);
            panel3.Controls.Add(btnPrint);
            panel3.Controls.Add(btnSelectVehicle);
            panel3.Controls.Add(btnSave);
            panel3.Controls.Add(btnSelectTrader);
            return panel3;
        }

        private Control BuildRetailReceiptBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 154F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            tableLayoutPanel2 = new Controls.DoubleBufferTableLayout
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 2,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(248, 251, 254)
            };
            for (int i = 0; i < 7; i++)
                tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            // Keep the three line lookups independent from the invoice and line
            // BindingSource currency. A parent ResetBindings must not empty a
            // lookup or move its selection to the first item.
            var dispenserItems = (ivm.Dispensers ?? new ViewModels.DispenserViewModel[0]);
            var nozzleItems = dispenserItems
                .Where(dispenser => dispenser.Nozzles != null)
                .SelectMany(dispenser => dispenser.Nozzles)
                .ToArray();
            var fuelItems = (ivm.FuelTypes ?? new ViewModels.FuelTypeViewModel[0]).ToArray();

            radDropDownList3 = NewDropDown(dispenserItems.ToArray(), "Description", "DispenserId");
            radDropDownList3.DataBindings.Add("SelectedValue", invoiceLinesBindingSource,
                "CurrentDispenserId", true, DataSourceUpdateMode.OnPropertyChanged);
            radDropDownList2 = NewDropDown(nozzleItems, "Description", "NozzleId");
            radDropDownList2.DataBindings.Add("SelectedValue", invoiceLinesBindingSource,
                "CurrentNozzleId", true, DataSourceUpdateMode.OnPropertyChanged);
            var fuel = NewDropDown(fuelItems, "Name", "FuelTypeId");
            fuel.DataBindings.Add("SelectedValue", invoiceLinesBindingSource,
                "FuelTypeId", true, DataSourceUpdateMode.Never);
            fuel.Enabled = false;
            volume1 = NewSpin("Volume", 3, 999999999);
            unitPrice1 = NewSpin("UnitPriceView", 5, 999999999);
            discount = NewSpin("DiscountPercentageView", 2, 100);
            priceTotal1 = NewSpin("TotalAmount", 2, 999999999);

            string[] captions = { "Αντλία", "Ακροσωλήνιο", "Καύσιμο", "Όγκος",
                ivm.PricesIncludeVat ? "Τιμή με ΦΠΑ" : "Τιμή χωρίς ΦΠΑ", "Έκπτωση %", "Σύνολο" };
            Control[] editors = { radDropDownList3, radDropDownList2, fuel, volume1, unitPrice1, discount, priceTotal1 };
            for (int i = 0; i < captions.Length; i++)
            {
                tableLayoutPanel2.Controls.Add(NewLabel(captions[i]), i, 0);
                tableLayoutPanel2.Controls.Add(editors[i], i, 1);
            }
            tableLayoutPanel2.DataBindings.Add("Enabled", invoiceViewModelBindingSource, "LineEditEnabled", true);

            body.Controls.Add(tableLayoutPanel2, 0, 0);
            body.Controls.Add(BuildNotesAndSummaryPanel("Παρατηρήσεις Τεχνικού", "Γεν. Σύνολο"), 0, 1);
            return body;
        }

        private Control BuildDetailedBody(ViewModels.InvoiceEditorLayout layout)
        {
            bool supplier = layout == ViewModels.InvoiceEditorLayout.SupplierPurchase;
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, supplier ? 190F : 210F));

            panel2 = new Panel { Dock = DockStyle.Fill };
            btnAddVehicle = NewToolbarButton(Properties.Resources.Add, btnAddVehicle_Click,
                "Προσθήκη γραμμής");
            btnDeleteVehicle = NewToolbarButton(Properties.Resources.Delete, btnDeleteVehicle_Click,
                "Διαγραφή γραμμής");
            btnAddVehicle.Dock = DockStyle.Left;
            btnDeleteVehicle.Dock = DockStyle.Left;
            btnAddVehicle.DataBindings.Add("Enabled", invoiceViewModelBindingSource, "CanAdd", true);
            panel2.DataBindings.Add("Enabled", invoiceViewModelBindingSource, "EditEnabled", true);
            panel2.Controls.Add(btnDeleteVehicle);
            panel2.Controls.Add(btnAddVehicle);

            var priceInfo = NewLabel(supplier
                ? "Οι καθαρές τιμές αγοράς καταχωρούνται χωρίς ΦΠΑ"
                : ivm.PricesIncludeVat
                    ? "Οι τιμές καταχωρούνται με ΦΠΑ"
                    : "Οι τιμές καταχωρούνται χωρίς ΦΠΑ");
            priceInfo.ForeColor = Color.FromArgb(20, 92, 170);
            priceInfo.Font = new Font(EditorFont, FontStyle.Bold);
            priceInfo.TextAlignment = ContentAlignment.MiddleRight;
            priceInfo.Dock = DockStyle.Right;
            priceInfo.Width = 390;
            panel2.Controls.Add(priceInfo);

            radGridView1 = BuildDetailedGrid(supplier);

            body.Controls.Add(panel2, 0, 0);
            body.Controls.Add(radGridView1, 0, 1);
            body.Controls.Add(BuildNotesAndSummaryPanel(
                supplier ? "Παρατηρήσεις Αγοράς" : "Παρατηρήσεις",
                supplier ? "Συνολική Αξία" : "Γεν. Σύνολο"), 0, 2);
            return body;
        }

        private RadGridView BuildDetailedGrid(bool supplier)
        {
            var grid = new RadGridView
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                MinimumSize = new Size(0, supplier ? 160 : 0)
            };
            grid.MasterTemplate.AllowAddNewRow = false;
            grid.MasterTemplate.AllowDeleteRow = false;
            grid.MasterTemplate.AutoGenerateColumns = false;
            grid.MasterTemplate.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.None;
            grid.MasterTemplate.EnableGrouping = false;
            grid.MasterTemplate.ShowRowHeaderColumn = false;
            grid.TableElement.TableHeaderHeight = 42;
            grid.DataBindings.Add("AllowEditRow", invoiceViewModelBindingSource, "LineEditEnabled", true);

            var fuel = new GridViewComboBoxColumn("FuelType")
            {
                HeaderText = "Καύσιμο",
                FieldName = "FuelTypeId",
                DataSource = fuelTypesBindingSource,
                DisplayMember = "Name",
                ValueMember = "FuelTypeId",
                Width = 110,
                WrapText = true
            };
            var tank = new GridViewComboBoxColumn("TankId")
            {
                HeaderText = supplier ? "Δεξαμενή Παραλαβής *" : "Δεξαμενή",
                FieldName = "TankId",
                DataSource = tanksBindingSource,
                DisplayMember = "Description",
                ValueMember = "TankId",
                Width = 155,
                WrapText = true
            };
            grid.MasterTemplate.Columns.Add(fuel);
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("Volume", supplier ? "Παραδοθείς Όγκος" : "Όγκος", 3, false, supplier ? 130 : 95));
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("VolumeNormalized", "Όγκος 15", 3, false, 80));
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("Temperature", "Θερμοκρασία", 2, false, 95));
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("FuelDensity", "Πυκνότητα", 2, false, 85));
            var price = NewDecimalColumn("UnitPrice", supplier ? "Καθαρή Τιμή Μονάδας" :
                (ivm.PricesIncludeVat ? "Τιμή με ΦΠΑ" : "Τιμή χωρίς ΦΠΑ"), 5, false, supplier ? 150 : 115);
            // Bind the editor to the unified property when the column is created.
            // Telerik caches the property descriptor after the grid is bound, so
            // changing FieldName later from UnitPriceRetail/Whole to UnitPriceView
            // can leave the transformation price cell without a writable editor.
            // UnitPriceView already selects gross/net according to PricesIncludeVat
            // and applies the transformation-specific recalculation policy.
            price.FieldName = "UnitPriceView";
            grid.MasterTemplate.Columns.Add(price);
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("DiscountAmountView", "Έκπτωση", 2, false, 80));
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("NettoPrice", "Καθαρή Αξία", 2, true, 100));
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("VatPercentageView", "ΦΠΑ %", 2, true, 70));
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("VatAmount", "Ποσό ΦΠΑ", 2, true, 90));
            grid.MasterTemplate.Columns.Add(NewDecimalColumn("TotalPrice", "Συνολική Αξία", 2, true, 105));
            grid.MasterTemplate.Columns.Add(tank);
            grid.MasterTemplate.DataSource = invoiceLinesBindingSource;
            grid.CellBeginEdit += radGridView1_CellBeginEdit;
            return grid;
        }

        private GridViewDecimalColumn NewDecimalColumn(string name, string header, int decimals,
            bool readOnly = false, int width = 90)
        {
            return new GridViewDecimalColumn(name)
            {
                HeaderText = header,
                FieldName = name,
                DecimalPlaces = decimals,
                FormatString = "{0:N" + decimals + "}",
                ReadOnly = readOnly,
                Width = width,
                WrapText = true
            };
        }

        private Control BuildNotesAndSummaryPanel(string notesCaption, string totalCaption)
        {
            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 116F));
            footer.Controls.Add(NewLabel(notesCaption), 0, 0);
            radTextBox2 = NewTextBox("Notes");
            radTextBox2.Multiline = true;
            footer.Controls.Add(radTextBox2, 0, 1);
            footer.Controls.Add(BuildPaymentAndTotalsPanel(totalCaption), 0, 2);
            return footer;
        }

        private Control BuildPaymentAndTotalsPanel(string totalCaption)
        {
            tableLayoutPanel3 = new Controls.DoubleBufferTableLayout
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 4,
                Margin = new Padding(0)
            };
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            for (int i = 0; i < 4; i++)
                tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            paymentTypeDropDownList = NewPaymentDropDown();
            tableLayoutPanel3.Controls.Add(NewLabel("Τρόπος Πληρωμής"), 0, 0);
            tableLayoutPanel3.Controls.Add(paymentTypeDropDownList, 1, 0);
            radSpinEditor2 = NewSpin("NettoAmount", 2, 999999999);
            radSpinEditor3 = NewSpin(ivm.PricesIncludeVat ? "DiscountAmount" : "DiscountAmountWhole", 2, 999999999);
            radSpinEditor4 = NewSpin("VatAmount", 2, 999999999);
            radSpinEditor5 = NewSpin("TotalAmount", 2, 999999999);
            radSpinEditor2.ReadOnly = radSpinEditor3.ReadOnly = radSpinEditor4.ReadOnly = radSpinEditor5.ReadOnly = true;
            string[] labels = { "Καθαρή Αξία", "Έκπτωση", "Φ.Π.Α.", totalCaption };
            Control[] values = { radSpinEditor2, radSpinEditor3, radSpinEditor4, radSpinEditor5 };
            for (int i = 0; i < labels.Length; i++)
            {
                var label = NewLabel(labels[i]);
                if (i == 3) label.Font = new Font(EditorFont, FontStyle.Bold);
                tableLayoutPanel3.Controls.Add(label, 2, i);
                tableLayoutPanel3.Controls.Add(values[i], 3, i);
            }
            return tableLayoutPanel3;
        }

        private Control BuildFixedSignaturePanel()
        {
            var signature = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(3, 0, 3, 0)
            };
            signature.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
            signature.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            signature.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            radLabel12 = NewLabel("Σήμανση");
            radLabel13 = NewLabel("");
            radLabel13.DataBindings.Add("Text", invoiceViewModelBindingSource,
                "InvoiceSignature", true, DataSourceUpdateMode.Never);
            signature.Controls.Add(radLabel12, 0, 0);
            signature.Controls.Add(radLabel13, 1, 0);
            return signature;
        }

        private RadLabel NewLabel(string text)
        {
            return new RadLabel
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = EditorFont,
                TextAlignment = ContentAlignment.MiddleLeft,
                Margin = new Padding(3)
            };
        }

        private RadTextBox NewTextBox(string property, bool readOnly = false)
        {
            var control = new RadTextBox { Dock = DockStyle.Fill, Font = EditorFont, ReadOnly = readOnly };
            control.DataBindings.Add("Text", invoiceViewModelBindingSource, property, true,
                DataSourceUpdateMode.OnPropertyChanged);
            return control;
        }

        private RadDateTimePicker NewDatePicker(string property)
        {
            var control = new RadDateTimePicker { Dock = DockStyle.Fill, Font = EditorFont };
            control.DataBindings.Add("Value", invoiceViewModelBindingSource, property, true,
                DataSourceUpdateMode.OnPropertyChanged);
            return control;
        }

        private RadSpinEditor NewSpin(string property, int decimals, decimal maximum)
        {
            var control = new RadSpinEditor
            {
                Dock = DockStyle.Fill,
                Font = EditorFont,
                DecimalPlaces = decimals,
                Maximum = maximum,
                Minimum = -maximum,
                TextAlignment = HorizontalAlignment.Right
            };
            control.DataBindings.Add("Value", property == "Volume" || property == "UnitPriceView" ||
                    property == "DiscountPercentage" || property == "DiscountPercentageView"
                    ? invoiceLinesBindingSource : invoiceViewModelBindingSource,
                property, true, DataSourceUpdateMode.OnPropertyChanged);
            return control;
        }

        private RadDropDownList NewDropDown(object source, string displayMember, string valueMember)
        {
            var control = new RadDropDownList
            {
                Dock = DockStyle.Fill,
                Font = EditorFont,
                DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList
            };
            control.AutoCompleteDisplayMember = displayMember;
            control.AutoCompleteValueMember = valueMember;
            control.DisplayMember = displayMember;
            control.ValueMember = valueMember;
            control.DataSource = source;
            return control;
        }

        private RadDropDownList NewPaymentDropDown()
        {
            var control = NewDropDown(paymentTypesBindingSource, "Description", "Value");
            control.DataBindings.Add("SelectedValue", invoiceViewModelBindingSource,
                "PaymentType", true, DataSourceUpdateMode.OnPropertyChanged);
            return control;
        }

        private RadButton NewToolbarButton(Image image, EventHandler handler, string accessibleText = null)
        {
            var button = new RadButton
            {
                DisplayStyle = Telerik.WinControls.DisplayStyle.Image,
                Image = image,
                ImageAlignment = ContentAlignment.MiddleCenter,
                Margin = new Padding(0),
                Size = new Size(40, 40)
            };
            button.RootElement.UseDefaultDisabledPaint = true;
            if (!string.IsNullOrEmpty(accessibleText))
            {
                button.AccessibleName = accessibleText;
                button.ButtonElement.ToolTipText = accessibleText;
            }
            button.Click += handler;
            return button;
        }
    }
}
