using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectCompanyForm : RadForm
    {
        ASFuelControl.Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public Guid SelectedCompanyId { private set; get; }

        public byte[] ImageData { private set; get; }
        public Image SelectedImage { private set; get; }

        public SelectCompanyForm()
        {
            InitializeComponent();
            this.oilCompanyBindingSource.DataSource = database.OilCompanies.OrderBy(o => o.Name);
            this.Disposed += SelectCompanyForm_Disposed;
        }

        private void SelectCompanyForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        private void radListView1_ItemDataBound(object sender, ListViewItemEventArgs e)
        {
            if (radListView1.ViewType == Telerik.WinControls.UI.ListViewType.IconsView)
            {
                e.Item.Image = ((Data.OilCompany)e.Item.DataBoundItem).Image;
            }
        }

        private void radListView1_VisualItemFormatting(object sender, ListViewVisualItemEventArgs e)
        {
            e.VisualItem.ImageLayout = ImageLayout.Zoom;
            e.VisualItem.ImageAlignment = ContentAlignment.MiddleCenter;
            e.VisualItem.TextAlignment = ContentAlignment.BottomCenter;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.radListView1.SelectedItem == null)
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                return;
            }
            Data.OilCompany company = this.radListView1.SelectedItem.DataBoundItem as Data.OilCompany;
            if (company == null)
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                return;
            }
            this.SelectedCompanyId = company.OilCompanyId;
            this.SelectedImage = company.Image;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
        
        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
