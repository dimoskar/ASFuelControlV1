using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Threading;
using System.Reflection;

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

        /// <summary>
        /// Static flag. Is setted to true when an Administrator is connected to the Console. 
        /// </summary>
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

        /// <summary>
        /// the Id of the CurrentShift. Used for all SaleTransaction or TankFilling entries generated.
        /// </summary>
        public static Guid CurrentShiftId 
        {
            set { Data.DatabaseModel.CurrentShiftId = value; }
            get { return Data.DatabaseModel.CurrentShiftId; }
        }

        /// <summary>
        /// Gets the CRC of the Application
        /// </summary>
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

        /// <summary>
        /// Gets the Main Version of the Application
        /// </summary>
        public static int  MainVersion
        {
            get { return 1; }
        }

        /// <summary>
        /// Gets the Subversion of the Application
        /// </summary>
        public static int SubVersion
        {
            get { return 1; }
        }

        /// <summary>
        /// Gets the Revision number of the Application
        /// </summary>
        public static int Revision
        {
            get { return 3; }
        }

        /// <summary>
        /// he Main form of the Application
        /// </summary>
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Telerik.WinControls.ThemeResolutionService.ApplicationThemeName = "Windows8";// "Windows8";//"TelerikMetroBlue";

            //Check if application is allready running. Waits until running instance is closed and proceeds.          
            bool first = false;
            m = new Mutex(true, Application.ProductName.ToString(), out first);
            bool questionShown = false;
            while (!first)
            {
                System.Threading.Thread.Sleep(500);
                m.Dispose();
                m = new Mutex(true, Application.ProductName.ToString(), out first);
                if (!first)
                {
                    if (!questionShown)
                    {
                        DialogResult res = Telerik.WinControls.RadMessageBox.Show("Το ASFuelControl εκτελείται ήδη. Θέλετε να κλείσουν οι υπόλοιπες εφαρμογές;", "Σφάλμα Εκκίνησης Εφαρμογής", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("ASFuelControl.Windows");
                            foreach (System.Diagnostics.Process process in processes)
                                process.Kill();
                        }
                        else
                            questionShown = true;
                    }
                }
            }
            

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
                ApplicationMainForm.WindowState = FormWindowState.Maximized;
                Application.Run(ApplicationMainForm);
            }
            catch(Exception e)
            {
                System.IO.File.AppendAllText(System.Environment.CurrentDirectory+"\\InitializeERROR.txt",e.ToString());
            }
          
        }


        /// <summary>
        /// Gets the Catalog Name of the database from the ConfigurationFile
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Checks if database exists.
        /// </summary>
        /// <param name="connString"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates the database
        /// </summary>
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
