using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Win32;
using System.ServiceProcess;

namespace BalanceExporter
{
    public partial class Form1 : Form
    {
        System.Threading.Thread th;
        private delegate void UpdateProgressDelegate(int max, int cur);
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

            this.radDropDownList2.SelectedIndexChanged+=new Telerik.WinControls.UI.Data.PositionChangedEventHandler(radDropDownList2_SelectedIndexChanged);

        }

        private string GetImagePath(string servicename)
        {
            string registryPath = @"SYSTEM\CurrentControlSet\Services\" + servicename;
            RegistryKey keyHKLM = Registry.LocalMachine;

            RegistryKey key;
            
            key = keyHKLM.OpenSubKey(registryPath);
            

            string value = key.GetValue("ImagePath").ToString();
            key.Close();
            return System.Environment.ExpandEnvironmentVariables(value);
            //return value;
        }

        void  radDropDownList2_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
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

            this.radDropDownList2.DataSource = dbnames.OrderBy(s=>s).ToList();
            this.radDropDownList2.DisplayMember = "Value";
            this.radDropDownList2.ValueMember = "Value";
        }

        private void UpodatePrograss(int max, int cur)
        {
            this.radProgressBar1.Maximum = max;
            this.radProgressBar1.Value1 = cur;
            this.radProgressBar1.Text = string.Format("Εξήχθησαν {0} από {1} Ισοζύγια", cur, max);
            if (cur == max)
            {
                this.radTextBox1.Enabled = true;
                this.radButton1.Enabled = true;
                this.radButton2.Enabled = true;
            }
        }

        private void ThreadRun()
        {
            if (this.connectionString == "")
                return;
            DatabaseModel db = new DatabaseModel(this.connectionString);

            List<Balance> balances = db.Balances.OrderBy(b => b.StartDate).ToList();

            this.Invoke(new UpdateProgressDelegate(this.UpodatePrograss), new object[] { balances.Count, 0});
            int i = 0;
            foreach (Balance balance in balances)
            {

                Reports.BalanceLoad bl = new Reports.BalanceLoad();
                bl.LoadBalance(balance.BalanceId, balance.BalanceText);

                BalanceReport report = new BalanceReport();
                Reports.TankBalanceReport subReport1 = new Reports.TankBalanceReport();
                Reports.PumpBalanceReport subReport2 = new Reports.PumpBalanceReport();
                Reports.DivergenceBalanceReport subReport3 = new Reports.DivergenceBalanceReport();
                Reports.TankFillingBalanceReport subReport4 = new Reports.TankFillingBalanceReport();

                Telerik.Reporting.ObjectDataSource ds1 = new Telerik.Reporting.ObjectDataSource();
                ds1.DataSource = bl.Model;
                ds1.DataMember = "TankData";

                Telerik.Reporting.ObjectDataSource ds2 = new Telerik.Reporting.ObjectDataSource();
                ds2.DataSource = bl.Model;
                ds2.DataMember = "DispenserData";

                Telerik.Reporting.ObjectDataSource ds3 = new Telerik.Reporting.ObjectDataSource();
                ds3.DataSource = bl.Model;
                ds3.DataMember = "FuelTypeData";

                Telerik.Reporting.ObjectDataSource ds4 = new Telerik.Reporting.ObjectDataSource();
                ds4.DataSource = bl.Model;
                ds4.DataMember = "Balance";

                Telerik.Reporting.ObjectDataSource ds5 = new Telerik.Reporting.ObjectDataSource();
                ds5.DataSource = bl.Model;
                ds5.DataMember = "TankFillingData";


                subReport1.DataSource = ds1;
                subReport2.DataSource = ds2;
                subReport3.DataSource = ds3;
                subReport4.DataSource = ds5;
                report.DataSource = ds4;
                report.Name = "BalanceReport";
                report.DocumentName = "BalanceReport";

                Telerik.Reporting.InstanceReportSource subReportSource1 = new Telerik.Reporting.InstanceReportSource();
                subReportSource1.ReportDocument = subReport1;

                Telerik.Reporting.InstanceReportSource subReportSource2 = new Telerik.Reporting.InstanceReportSource();
                subReportSource2.ReportDocument = subReport2;

                Telerik.Reporting.InstanceReportSource subReportSource3 = new Telerik.Reporting.InstanceReportSource();
                subReportSource3.ReportDocument = subReport3;

                Telerik.Reporting.InstanceReportSource subReportSource4 = new Telerik.Reporting.InstanceReportSource();
                subReportSource4.ReportDocument = subReport4;

                //Telerik.Reporting.InstanceReportSource repSource = new Telerik.Reporting.InstanceReportSource();
                //repSource.ReportDocument = report;

                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport1"]).ReportSource = subReportSource1;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport2"]).ReportSource = subReportSource2;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport3"]).ReportSource = subReportSource3;
                ((Telerik.Reporting.SubReport)report.Items["detail"].Items["subReport4"]).ReportSource = subReportSource4;

                Telerik.Reporting.InstanceReportSource reportSource = new Telerik.Reporting.InstanceReportSource();
                reportSource.ReportDocument = report;

                string balanceDirectory = this.radTextBox1.Text;
                if (!System.IO.Directory.Exists(balanceDirectory))
                {
                    System.IO.Directory.CreateDirectory(balanceDirectory);
                }
                string fileName = balanceDirectory + "\\" + string.Format("Balance_{0:yyyyMMdd_HHmm}.pdf", balance.StartDate);

                SaveReport(report, fileName);
                i++;
                this.Invoke(new UpdateProgressDelegate(this.UpodatePrograss), new object[] { balances.Count, i });
            }
            db.Dispose();
            this.Invoke(new UpdateProgressDelegate(this.UpodatePrograss), new object[] { balances.Count, balances.Count });
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.radTextBox1.Enabled = false;
            this.radButton1.Enabled = false;
            this.radButton2.Enabled = false;
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.Start();
        }

        void SaveReport(Telerik.Reporting.Report report, string fileName)
        {
            Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
            Telerik.Reporting.InstanceReportSource instanceReportSource = new Telerik.Reporting.InstanceReportSource();
            instanceReportSource.ReportDocument = report;
            Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", instanceReportSource, null);

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = this.radTextBox1.Text;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.radTextBox1.Text = fbd.SelectedPath;
            }
        }
        
    }
}
