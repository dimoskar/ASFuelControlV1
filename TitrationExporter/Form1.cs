using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using System.Data.SqlClient;

namespace TitrationExporter
{
    public partial class Form1 : RadForm
    {
        private string connectionString = "";

        public Form1()
        {
            InitializeComponent();

            this.radTextBox1.Text = System.Environment.CurrentDirectory;

            System.Data.Sql.SqlDataSourceEnumerator instance = System.Data.Sql.SqlDataSourceEnumerator.Instance;
            DataTable dt = instance.GetDataSources();
            this.radDropDownList1.DataSource = dt;

            dt.Columns.Add("ServerFullName", typeof(string));
            foreach (DataRow row in dt.Rows)
            {
                row["ServerFullName"] = row["ServerName"].ToString() + "\\" + row["InstanceName"].ToString();
            }

            this.radDropDownList1.DisplayMember = "InstanceName";
            this.radDropDownList1.ValueMember = "ServerFullName";
            this.radDropDownList1.SelectedIndexChanged += new Telerik.WinControls.UI.Data.PositionChangedEventHandler(radDropDownList1_SelectedIndexChanged);
            this.radDropDownList1.SelectedIndex = 0;

            this.radDropDownList2.SelectedIndexChanged += new Telerik.WinControls.UI.Data.PositionChangedEventHandler(radDropDownList2_SelectedIndexChanged);
        }

        void radDropDownList2_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            if (this.radDropDownList1.SelectedIndex < 0)
                return;
            if (this.radDropDownList2.SelectedIndex < 0)
                return;
            string serverName = ((DataTable)this.radDropDownList1.DataSource).Rows[this.radDropDownList1.SelectedIndex]["ServerFullName"].ToString();
            string catalogName = ((List<string>)this.radDropDownList2.DataSource).ElementAt(this.radDropDownList2.SelectedIndex);

            this.connectionString = string.Format("Data Source={0};Database={1};" + "Integrated Security=true;", serverName, catalogName);
        }

        void radDropDownList1_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            if (e.Position < 0)
                return;
            string SelectedServer = ((DataTable)this.radDropDownList1.DataSource).Rows[e.Position]["ServerFullName"].ToString();

            SqlConnectionStringBuilder connection = new SqlConnectionStringBuilder();

            connection.DataSource = SelectedServer;
            connection.IntegratedSecurity = true;

            String strConn = connection.ToString();

            //create connection
            SqlConnection sqlConn = new SqlConnection(strConn);

            //open connection
            sqlConn.Open();

            //get databases
            DataTable tblDatabases = sqlConn.GetSchema("Databases");

            //close connection
            sqlConn.Close();
            List<string> dbnames = new List<string>();
            foreach (DataRow row in tblDatabases.Rows)
            {
                if (row["database_name"].ToString().ToLower() == "master")
                    continue;
                if (row["database_name"].ToString().ToLower() == "model")
                    continue;
                if (row["database_name"].ToString().ToLower() == "tempdb")
                    continue;
                if (row["database_name"].ToString().ToLower() == "msdb")
                    continue;
                dbnames.Add(row["database_name"].ToString().ToLower());
            }

            this.radDropDownList2.DataSource = dbnames.OrderBy(s => s).ToList();
            this.radDropDownList2.DisplayMember = "Value";
            this.radDropDownList2.ValueMember = "Value";
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            DatabaseModel db = new DatabaseModel(this.connectionString);
            foreach (Tank tank in db.Tanks)
            {
                Titrimetry titration = tank.Titrimetries.OrderBy(t => t.TitrationDate).LastOrDefault();
                if (titration == null)
                    continue;
                List<TitrimetryLevel> levels = titration.TitrimetryLevels.OrderBy(t => t.Height).ToList();
                string text = "";
                foreach (TitrimetryLevel level in levels)
                {
                    text = text + string.Format("{0:N2}\t{1:N2}\r\n", level.Height, level.Volume);
                }

                string tankDesc = string.Format("Δεξαμενή {0}-{1}", tank.TankNumber, tank.FuelType.Name);
                string directory = this.radTextBox1.Text;
                System.IO.File.WriteAllText(directory + "\\" + tankDesc + ".txt", text);
            }
        }
    }
}
