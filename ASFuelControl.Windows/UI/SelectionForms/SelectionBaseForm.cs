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
    public partial class SelectionBaseForm : RadForm
    {
        protected BindingSource theSource = new BindingSource();

        public BindingSource TheSource { get { return this.theSource; } }

        public object SelectedEntity { set; get; }

        public bool SeachOnLoad { set; get; }

        public SelectionBaseForm()
        {
            InitializeComponent();
            if (this.DesignMode)
                return;
            this.SeachOnLoad = true;
            this.Load += new EventHandler(SelectionBaseForm_Load);
            this.FormClosed += new FormClosedEventHandler(SelectionBaseForm_FormClosed);
        }

        void SelectionBaseForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.OnDispose();
            this.Dispose();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }


        protected virtual void OnDispose()
        {
        }

        protected virtual void OnLoadForm()
        {
            if (this.DesignMode)
                return;
            if (this.SeachOnLoad)
            {
                string filter = this.radTextBox1.Text;
                theSource.DataSource = this.SearchData(filter);
                this.DataLoaded();
            }
            this.dataRadGridView.DataSource = this.theSource;
        }

        void SelectionBaseForm_Load(object sender, EventArgs e)
        {
            this.OnLoadForm();
        }

        protected virtual void AddNew()
        {
        }

        protected virtual object SearchData(string filter)
        {
            return null;
        }

        protected virtual object SearchData(Guid id)
        {
            return null;
        }

        protected virtual void OnSelectionClick()
        {
            if (this.dataRadGridView.CurrentRow != null)
            {
                this.SelectedEntity = this.dataRadGridView.CurrentRow.DataBoundItem;
                if (this.SelectedEntity != null)
                    this.DialogResult = System.Windows.Forms.DialogResult.OK;
                else
                    this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            }
            else
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        protected virtual void DataLoaded()
        {

        }

        protected virtual void LoadData()
        {
            string filter = this.radTextBox1.Text;
            theSource.DataSource = this.SearchData(filter);
            this.theSource.ResetBindings(false);
            this.DataLoaded();
        }

        protected virtual void LoadDataById(Guid id)
        {
            theSource.DataSource = this.SearchData(id);
            this.theSource.ResetBindings(false);
            this.DataLoaded();
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            this.OnSelectionClick();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            this.AddNew();
        }

        private void dataRadGridView_CellDoubleClick(object sender, Telerik.WinControls.UI.GridViewCellEventArgs e)
        {
            if (e.Row == null)
                return;
            if (e.Column.GetType() == typeof(Telerik.WinControls.UI.GridViewCommandColumn))
                return;
            this.SelectedEntity = e.Row.DataBoundItem;
            if (this.SelectedEntity != null)
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void SelectionBaseForm_Resize(object sender, EventArgs e)
        {
            this.dataRadGridView.Size = new Size(this.Width - 32, this.Height - 111);
        }

        protected void radTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.Handled = true;
            this.ExecuteSearch();
        }

        protected void ExecuteSearch()
        {
            string filter = this.radTextBox1.Text;
            theSource.DataSource = this.SearchData(filter);
            this.theSource.ResetBindings(false);
            this.DataLoaded();
            this.dataRadGridView.Focus();
        }

        private void dataRadGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.Handled = true;
            if (this.dataRadGridView.CurrentRow == null)
                return;

            this.SelectedEntity = this.dataRadGridView.CurrentRow.DataBoundItem;
            if (this.SelectedEntity != null)
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
