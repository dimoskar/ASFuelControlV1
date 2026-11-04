using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class InvoiceTypesForm : RadForm
    {
        ViewModels.InvoiceTypeCatalogViewModel vm = new ViewModels.InvoiceTypeCatalogViewModel();
        private RadButton btnCopy;

        public InvoiceTypesForm()
        {
            InitializeComponent();
            InitializeCopyButton();
            PrepareTransformationLookupBindings();
            InitializeInvoicingPolicy();
            vm.PropertyChanged += Vm_PropertyChanged;
            this.Load += InvoiceTypesForm_Load;
        }

        private void InitializeCopyButton()
        {
            btnCopy = new RadButton
            {
                Name = "btnCopy",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                DisplayStyle = DisplayStyle.Image,
                Image = CreateCopyIcon(),
                ImageAlignment = ContentAlignment.MiddleCenter,
                AccessibleName = "Αντιγραφή τύπου παραστατικού"
            };
            btnCopy.ButtonElement.ToolTipText = "Αντιγραφή τύπου παραστατικού";
            btnCopy.Click += btnCopy_Click;

            // Add, delete, copy, filter area, save.
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel3.ColumnCount = 5;
            tableLayoutPanel3.ColumnStyles.Clear();
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,40F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,40F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,40F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,40F));
            tableLayoutPanel3.SetCellPosition(panel2,new TableLayoutPanelCellPosition(3,0));
            tableLayoutPanel3.SetCellPosition(radButton1,new TableLayoutPanelCellPosition(4,0));
            tableLayoutPanel3.Controls.Add(btnCopy,2,0);
            tableLayoutPanel3.ResumeLayout(true);
        }

        private static Image CreateCopyIcon()
        {
            // Material-style "content copy" glyph, using the same blue as the
            // existing invoice toolbar icons.
            var image = new Bitmap(32,32);
            using (var graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.FromArgb(57,68,150),3F))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pen.LineJoin = LineJoin.Round;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                graphics.DrawLines(pen,new[]
                {
                    new PointF(6.5F,22.5F),
                    new PointF(6.5F,4.5F),
                    new PointF(21.5F,4.5F),
                    new PointF(21.5F,8.5F)
                });
                graphics.DrawRectangle(pen,10.5F,8.5F,15F,19F);
            }
            return image;
        }

        private void PrepareTransformationLookupBindings()
        {
            // These lookup lists describe the values allowed in transformation
            // rows. They are not children of the currently selected invoice type:
            // changing/resetting that row must never move a combo editor to item 0
            // and write the new position into ChildInvoiceTypeId/CreationTypeId.
            var filteredTypes = vm.InvoiceTypes
                .Cast<ViewModels.InvoiceTypeViewModel>()
                .OrderBy(item => item.Description)
                .ToArray();
            var nullableTypes = new List<ViewModels.InvoiceTypeViewModel>
            {
                new ViewModels.InvoiceTypeViewModel { InvoiceTypeId = Guid.Empty }
            };
            nullableTypes.AddRange(filteredTypes);
            var transformationTypes = Common.Enumerators.EnumToList
                .EnumList<Common.Enumerators.InvoiceTransformationTypeEnum>()
                .ToArray();

            SetIndependentLookupSource(invoiceTypesBindingSource, filteredTypes);
            SetIndependentLookupSource(invoiceTypesNullableBindingSource, nullableTypes.ToArray());
            SetIndependentLookupSource(invoiceTransformationTypesBindingSource, transformationTypes);
        }

        private static void SetIndependentLookupSource(BindingSource source, object values)
        {
            source.DataSource = null;
            source.DataMember = string.Empty;
            source.DataSource = values;
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "InvoiceTypes")
                return;
            // Commit a user's active grid choice before replacing only the lookup
            // lists. The transformation row values themselves are never rebound.
            radGridView2.EndEdit();
            PrepareTransformationLookupBindings();
        }

        private void InvoiceTypesForm_Load(object sender, EventArgs e)
        {
            try
            {
                this.invoiceTypeCatalogViewModelBindingSource.DataSource = vm;
                this.invoiceTypeCatalogViewModelBindingSource.ResetBindings(false);
            }
            catch
            { }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            try { SaveInvoicingPolicies(); }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"Ρυθμίσεις τιμολόγησης",MessageBoxButtons.OK,RadMessageIcon.Error); }
           
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var it = this.vm.AddNewInvoiceType();
            this.invoiceTypeViewModelBindingSource.ResetBindings(false);
            this.invoiceTypeViewModelBindingSource.Position = this.invoiceTypeViewModelBindingSource.List.IndexOf(it);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            ValidateChildren();
            invoiceTypeViewModelBindingSource.EndEdit();
            radGridView2.EndEdit();
            var source = radGridView1.CurrentRow == null ? null :
                radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeDetailViewModel;
            if (source == null)
                return;

            var copy = vm.CopyInvoiceType(source);
            CopyInvoicingPolicy(source.InvoiceTypeId,copy.InvoiceTypeId);
            invoiceTypeViewModelBindingSource.ResetBindings(false);
            var index = invoiceTypeViewModelBindingSource.List.IndexOf(copy);
            if (index >= 0)
                invoiceTypeViewModelBindingSource.Position = index;
            ShowPolicy();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (this.radGridView1.CurrentRow == null)
                return;
            ViewModels.InvoiceTypeDetailViewModel it = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeDetailViewModel;
            if (it == null)
                return;
            var res = RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή;", "Διαγραφή Παραστατικού", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (res == DialogResult.No)
                return;
            if (vm.DeleteInvoiceType(it))
                return;
            RadMessageBox.Show("Η επιλεγμένη εγγραφή περιέχει συνδέσεις. Δεν μπορεί να διαγραφεί!", "Σφάλμα Διαγραφής Παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            ViewModels.InvoiceTypeDetailViewModel it = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeDetailViewModel;
            if (it == null)
                return;
            it.AddTransformation();
        }

        private void btnDeleteVehicle_Click(object sender, EventArgs e)
        {
            ViewModels.InvoiceTypeDetailViewModel it = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeDetailViewModel;
            if (it == null)
                return;
            if (this.radGridView2.CurrentRow == null)
                return;
            ViewModels.InvoiceTypeTransformViewModel itt = this.radGridView2.CurrentRow.DataBoundItem as ViewModels.InvoiceTypeTransformViewModel;
            if (itt == null)
                return;
            var res = RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή;", "Διαγραφή Μετασχηματισμού Παραστατικού", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (res == DialogResult.No)
                return;
            it.DeleteTransformation(itt);
        }
    }
}
