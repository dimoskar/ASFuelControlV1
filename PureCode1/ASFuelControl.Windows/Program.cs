using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Threading;

namespace ASFuelControl.Windows
{
    static class Program
    {

        private static Mutex m;
        private static bool adminConnected = false;

        public static event EventHandler AdminConnectedEvent;

        public static Guid CurrentUserId { set; get; }
        public static string CurrentUserName { set; get; }
        public static Common.Enumerators.ApplicationUserLevelEnum CurrentUserLevel { set; get; }

        public static bool AdminConnected 
        {
            set 
            {
                if (adminConnected == value)
                    return;
                adminConnected = value;
                if (AdminConnectedEvent != null)
                    AdminConnectedEvent(null, new EventArgs());
            }
            get { return adminConnected; }
        }

        public static Guid CurrentShiftId 
        {
            set { Data.DatabaseModel.CurrentShiftId = value; }
            get { return Data.DatabaseModel.CurrentShiftId; }
        }

        public static string ApplicationCRC
        {
            get
            {
                eTokenLib.eTokenLib lib = new eTokenLib.eTokenLib();
                string crc =  lib.GetDirFileHash(System.Environment.CurrentDirectory);
                int pos = crc.IndexOf("|");
                return crc.Substring(0, pos);
            }
        }

        public static int  MainVersion
        {
            get { return 1; }
        }

        public static int SubVersion
        {
            get { return 1; }
        }

        public static MainForm ApplicationMainForm
        {
            set;
            get;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool first = false;
            m = new Mutex(true, Application.ProductName.ToString(), out first);
            while (!first)
            {
                System.Threading.Thread.Sleep(500);
                m.Dispose();
                m = new Mutex(true, Application.ProductName.ToString(), out first);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                ASFuelControl.Logging.Logger.Instance.LogFile = Properties.Settings.Default.LogFile;
                ASFuelControl.Logging.Logger.Instance.LogLevel = Properties.Settings.Default.LogLevel;

                if (!CheckDatabase(Properties.Settings.Default.DBConnection))
                {
                    CreateDatabase();
                }
                Data.Implementation.OptionHandler.ConnectionString = Properties.Settings.Default.DBConnection;

                ApplicationMainForm = new MainForm();
                Application.Run(ApplicationMainForm);
            }
            catch(Exception e)
            {
                System.IO.File.AppendAllText(System.Environment.CurrentDirectory+"\\millog.txt",e.ToString());

            }
            
        }

        private static string GetDatabaseName()
        {
            string databaseName = "";
            string[] param = Properties.Settings.Default.DBConnection.Split(';');
            foreach (string p in param)
            {
                string[] operators = p.Split('=');
                if (operators.Length != 2)
                    continue;
                if (operators[0].ToLower() != "initial catalog")
                    continue;
                databaseName = operators[1];
            }
            return databaseName;
        }

        private static bool CheckDatabase(string connString)
        {
            string sqlCreateDBQuery;
            string databaseName = GetDatabaseName();
            if (databaseName == "")
                return false;

            sqlCreateDBQuery = string.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", databaseName);

            SqlConnection tmpConn = new SqlConnection(Properties.Settings.Default.DBMasterConnection);
            using (tmpConn)
            {
                using (SqlCommand sqlCmd = new SqlCommand(sqlCreateDBQuery, tmpConn))
                {
                    tmpConn.Open();
                    object obj = sqlCmd.ExecuteScalar();
                    int databaseID = 0;
                    if (obj != null)
                        databaseID = (int)obj;    
                    tmpConn.Close();

                    return databaseID > 0;
                }
            }
        }

        private static void CreateDatabase()
        {
            string databaseName = GetDatabaseName();
            if (databaseName == "")
                return;
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory + "\\Database");
            if (!dir.Exists)
                dir.Create();

            string str = Properties.Resources.ResourceManager.GetString("CreateDatabase").Replace("[DatabaseName]", databaseName).Replace("[FileName]", dir.FullName + "\\" + databaseName);
            string[] commands = str.Split(new string[] { "GO\r\n" }, StringSplitOptions.RemoveEmptyEntries);


            SqlConnection tmpConn = new SqlConnection(Properties.Settings.Default.DBMasterConnection);
            using (tmpConn)
            {
                tmpConn.Open();
                foreach (string command in commands)
                {
                    try
                    {
                        SqlCommand sqlCmd = new SqlCommand(command, tmpConn);
                        sqlCmd.ExecuteNonQuery();
                        sqlCmd.Dispose();
                    }
                    catch (Exception ex)
                    {
                    }

                }
                tmpConn.Close();
            }
        }
    }
}
