using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectInvoiceControl : UserControl
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private DateTime dateFrom;
        private DateTime dateTo;

        public enum FillingModeEnum
        {
            Delivery,
            LiterCheck,
            Return
        }

        public event EventHandler<SelectionClickedArgs> SelectionClicked;

        private FillingModeEnum fillingMode = FillingModeEnum.Delivery;
        private Guid deliveriesGuid;
        private Guid literCheckGuid;
        private Guid returnsGuid;

        public DateTime DateFrom 
        {
            set 
            {
                if (this.dateFrom == value)
                    return;
                this.dateFrom = value;
                //this.LoadData();
            }
            get { return this.dateFrom; }
        }

        public DateTime DateTo
        {
            set
            {
                if (this.dateTo == value)
                    return;
                this.dateTo = value;
                //this.LoadData();
            }
            get { return this.dateTo; }
        }

        public bool Initialized { set; get; }

        public Guid TankId { set; get; }

        public Guid FuelTypeId { set; get; }

        public FillingModeEnum FillingMode
        {
            set { this.fillingMode = value; }
            get { return this.fillingMode; }
        }

        public SelectInvoiceControl()
        {
            InitializeComponent();
            this.Disposed += SelectInvoiceControl_Disposed;
        }

        private void SelectInvoiceControl_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        public void LoadData()
        {
            if (!this.Initialized)
                return;
            this.invoiceLineRadGridView.Columns["InvoiceId"].FieldName = "Invoice.Description";
            this.invoiceLineRadGridView.Columns["FuelTypeId"].FieldName = "FuelType.Name";
            this.deliveriesGuid = Data.Implementation.OptionHandler.Instance.GetGuidOption("DeliveryCheckInvoiceType", Guid.Empty);
            this.literCheckGuid = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            this.returnsGuid = Data.Implementation.OptionHandler.Instance.GetGuidOption("ReturnInvoiceType", Guid.Empty);

            var q1 = this.database.InvoiceLines.Where(il =>
                !il.TankFillingId.HasValue &&
                il.Invoice.InvoiceType.TransactionType == 1 &&
                il.FuelTypeId == this.FuelTypeId &&
                il.Invoice.TransactionDate.Date <= dateTo &&
                il.Invoice.TransactionDate.Date >= dateFrom &&
                il.Invoice.Number > 0 &&
                il.VolumeNormalized > 0 &&
                il.Temperature < 60 &&
                il.Volume > 0 &&
                il.FuelDensity > 500 &&
                il.FuelDensity < 900 &&
                il.Invoice.InvoiceType.DeliveryType.HasValue &&
                (
                    il.Invoice.InvoiceType.DeliveryType.Value == (int)Common.Enumerators.DeliveryTypeEnum.Delivery ||
                    il.Invoice.InvoiceType.DeliveryType.Value == (int)Common.Enumerators.DeliveryTypeEnum.Return ||
                    il.Invoice.InvoiceType.DeliveryType.Value == (int)Common.Enumerators.DeliveryTypeEnum.TransfusionIn
                )
            );

            var q2 = this.database.InvoiceLines.Where(il => !il.TankFillingId.HasValue && il.FuelTypeId == this.FuelTypeId &&
               il.Invoice.InvoiceTypeId == literCheckGuid && il.Invoice.TransactionDate.Date <= dateTo && il.Invoice.TransactionDate.Date >= dateFrom &&
               il.Invoice.Number > 0 && il.VolumeNormalized > 0 && il.Temperature < 60 && il.Volume > 0 && il.FuelDensity > 500 && il.FuelDensity < 900);

            //var q2 = this.database.TankSales.Where(ts => ts.TankId == this.TankId).Select(ts => ts.SalesTransaction).SelectMany(s => s.InvoiceLines).Where(il => !il.TankFillingId.HasValue &&
            //    il.Invoice.InvoiceTypeId == literCheckGuid && il.FuelTypeId == this.FuelTypeId && il.Invoice.TransactionDate.Date <= dateTo && il.Invoice.TransactionDate.Date >= dateFrom);

            var qw = this.database.FuelTypes.Where(f => f.Code == "WAT").FirstOrDefault();
            Guid waterTypeId = qw == null ? Guid.Empty : qw.FuelTypeId;
            var qmix = this.database.FuelTypes.Where(f => f.Code == "MIX").FirstOrDefault();
            Guid mixTypeId = qmix == null ? Guid.Empty : qmix.FuelTypeId;

            var q3 = this.database.InvoiceLines.Where(il =>
                !il.TankFillingId.HasValue &&
                il.Invoice.InvoiceType.TransactionType == 0 &&
                il.Invoice.InvoiceType.IsInternal.HasValue &&
                il.Invoice.InvoiceType.IsInternal.Value &&
                il.Invoice.InvoiceTypeId != literCheckGuid &&
                (
                    il.FuelTypeId == this.FuelTypeId ||
                    il.FuelTypeId == waterTypeId ||
                    il.FuelTypeId == mixTypeId
                ) &&
                il.TankId == this.TankId &&
                il.Invoice.TransactionDate.Date <= dateTo &&
                il.Invoice.TransactionDate.Date >= dateFrom &&
                il.Invoice.Number > 0 &&
                il.VolumeNormalized > 0 &&
                il.Temperature < 60 &&
                il.Volume > 0 &&
                il.FuelDensity > 500 &&
                il.FuelDensity < 900 &&
                il.Invoice.InvoiceType.DeliveryType.HasValue &&
                (
                    il.Invoice.InvoiceType.DeliveryType.Value == (int)Common.Enumerators.DeliveryTypeEnum.Drain ||
                    il.Invoice.InvoiceType.DeliveryType.Value == (int)Common.Enumerators.DeliveryTypeEnum.TransfusionOut
                ));

            if (this.fillingMode == FillingModeEnum.Delivery)
                this.invoiceLineBindingSource.DataSource = q1;
            else if(this.fillingMode == FillingModeEnum.LiterCheck)
                this.invoiceLineBindingSource.DataSource = q2;
            else if (this.fillingMode == FillingModeEnum.Return)
                this.invoiceLineBindingSource.DataSource = q3;
        }

        private void invoiceLineRadGridView_CommandCellClick(object sender, EventArgs e)
        {
            GridCommandCellElement cell = sender as GridCommandCellElement;
            if (cell == null)
                return;
            if (cell.ColumnInfo.Name == "column1")
            {
                Data.InvoiceLine invLine = cell.RowInfo.DataBoundItem as Data.InvoiceLine;
                if (invLine == null)
                    return;
                if (this.SelectionClicked != null)
                    this.SelectionClicked(this, new SelectionClickedArgs(invLine.InvoiceLineId) { Volume = invLine.Volume });
            }
            else if (cell.ColumnInfo.Name == "column2")
            {

            }
            else if (cell.ColumnInfo.Name == "openInvoice")
            {
                Data.InvoiceLine invLine = cell.RowInfo.DataBoundItem as Data.InvoiceLine;
                if (invLine == null)
                    return;
                Guid invId = invLine.InvoiceId;
                Forms.InvoiceFormEx iform = new Forms.InvoiceFormEx();
                iform.LoadInvoice(invId);
                iform.StartPosition = FormStartPosition.CenterScreen;
                iform.Show(this);
            }
        }
    }

    public class SelectionClickedArgs : EventArgs
    {
        public Guid SelectedInvoiceLineId { set; get; }
        public decimal Volume { set; get; }

        public SelectionClickedArgs(Guid invoiceLineId)
        {
            this.SelectedInvoiceLineId = invoiceLineId;
        }
    }
}
