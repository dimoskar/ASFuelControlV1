using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyDataCorrection
{
    public partial class Form1 : Form
    {
        List<ulong> corrupttedMarks = new List<ulong>();
        Dictionary<ulong, ulong> cancelations = new Dictionary<ulong, ulong>();
        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.textBox1.Text = "";
            corrupttedMarks.Clear();
            List<string> marks = new List<string>();
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "select * from dbo.MyDataInvoice where cast(Data as nvarchar(4000)) like '%<aa>0</aa>%'";
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var mark = (ulong)reader.GetInt64(2);
                            marks.Add(mark.ToString());
                            corrupttedMarks.Add(mark);
                            this.textBox1.Lines = marks.ToArray();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            this.textBox1.Text = "";
            string url = "";
            string userName = "";
            string key = "";
            List<string> lines = new List<string>();
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataUrl'";
                    url = command.ExecuteScalar().ToString();
                    url = AESEncryption.Decrypt(url, "Exedron@#");

                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataUserName'";
                    userName = command.ExecuteScalar().ToString();
                    userName = AESEncryption.Decrypt(userName, "Exedron@#");

                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataSubscriptionKey'";
                    key = command.ExecuteScalar().ToString();
                    key = AESEncryption.Decrypt(key, "Exedron@#");

                    lines.Add("User name: " + userName);
                    lines.Add("Subscription Key: " + key);

                    Exedron.MyData.Settings.IsActive = true;
                    Exedron.MyData.Settings.SubscriptionKey = key;
                    Exedron.MyData.Settings.Username = userName;

                    foreach (var mark in corrupttedMarks)
                    {
                        try
                        {
                            var resp = Exedron.MyData.InvoiceCancelation.MakeRequest(url, (long)mark);
                            if (resp.response != null)
                            {
                                if (resp.response.cancellationMark.HasValue)
                                {
                                    cancelations.Add(mark, resp.response.cancellationMark.Value);
                                    command.CommandText = string.Format("delete dbo.MyDataInvoice where mark={0}", mark);
                                    command.ExecuteNonQuery();
                                    lines.Add(string.Format("{0} - {1}", mark, resp.response.cancellationMark.Value));
                                }
                                else
                                {
                                    lines.Add(mark.ToString() + ": " + resp.response.responseData);
                                }
                            }
                            else
                            {
                                lines.Add(mark.ToString() + ": Response is Null");
                            }
                        }
                        catch(Exception ex)
                        {
                            lines.Add(ex.Message);
                        }
                        this.textBox1.Lines = lines.ToArray();
                    }
                    
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var textLines = this.textBox1.Lines;
            List<string> lines = new List<string>();
            string url = "";
            string userName = "";
            string key = "";
            this.textBox1.Text = "";
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataUrl'";
                    url = command.ExecuteScalar().ToString();
                    url = AESEncryption.Decrypt(url, "Exedron@#");

                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataUserName'";
                    userName = command.ExecuteScalar().ToString();
                    userName = AESEncryption.Decrypt(userName, "Exedron@#");

                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataSubscriptionKey'";
                    key = command.ExecuteScalar().ToString();
                    key = AESEncryption.Decrypt(key, "Exedron@#");

                    lines.Add("User name: " + userName);
                    lines.Add("Subscription Key: " + key);

                    Exedron.MyData.Settings.IsActive = true;
                    Exedron.MyData.Settings.SubscriptionKey = key;
                    Exedron.MyData.Settings.Username = userName;

                    foreach (var line in textLines)
                    {
                        try
                        {
                            ulong mark = ulong.Parse(line);
                            var resp = Exedron.MyData.InvoiceCancelation.MakeRequest(url, (long)mark);
                            if (resp.response != null)
                            {
                                if (resp.response.cancellationMark.HasValue)
                                {
                                    lines.Add(string.Format("{0} - {1}", mark, resp.response.cancellationMark.Value));
                                }
                                else
                                {
                                    lines.Add(mark.ToString() + ": " + resp.response.responseData);
                                }
                            }
                            else
                            {
                                lines.Add(mark.ToString() + ": Response is Null");
                            }
                        }
                        catch (Exception ex)
                        {
                            lines.Add(ex.Message);
                        }
                        this.textBox1.Lines = lines.ToArray();
                    }
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var textLines = this.textBox1.Lines;
            List<string> lines = new List<string>();
            string url = "";
            string userName = "";
            string key = "";
            this.textBox1.Text = "";
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.DBConnection))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataUrl'";
                    url = command.ExecuteScalar().ToString();
                    url = AESEncryption.Decrypt(url, "Exedron@#");

                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataUserName'";
                    userName = command.ExecuteScalar().ToString();
                    userName = AESEncryption.Decrypt(userName, "Exedron@#");

                    command.CommandText = "select OptionValue from dbo.[Option] where OptionKey = 'MyDataSubscriptionKey'";
                    key = command.ExecuteScalar().ToString();
                    key = AESEncryption.Decrypt(key, "Exedron@#");

                    lines.Add("User name: " + userName);
                    lines.Add("Subscription Key: " + key);

                    Exedron.MyData.Settings.IsActive = true;
                    Exedron.MyData.Settings.SubscriptionKey = key;
                    Exedron.MyData.Settings.Username = userName;

                    foreach (var line in textLines)
                    {
                        try
                        {
                            ulong mark = ulong.Parse(line);
                            command.CommandText = string.Format("delete dbo.MyDataInvoice where mark={0}", mark);
                            int rows = command.ExecuteNonQuery();
                            lines.Add(string.Format("{0} - {1}", mark, rows > 0 ? "Deleted" : "Not Found"));
                        }
                        catch (Exception ex)
                        {
                            lines.Add(ex.Message);
                        }
                        this.textBox1.Lines = lines.ToArray();
                    }
                }
            }
        }
    }
}
