using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class MyDataCatalogForm : RadForm
    {
        public MyDataCatalogForm()
        {
            InitializeComponent();
            Catalogs.MyDataCatalog catalog = new Catalogs.MyDataCatalog();
            this.Controls.Add(catalog);
            catalog.Dock = DockStyle.Fill;
        }
    }
}
