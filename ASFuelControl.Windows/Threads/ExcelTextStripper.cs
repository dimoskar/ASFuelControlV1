using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;

namespace ASFuelControl.Windows.Threads
{

    /// <summary>
    /// Class providing functionality for extracting text of an Excel File
    /// </summary>
    public class ExcelTextStripper
    {

        public static string GetText(string fileName, string reportName)
        {
            
            OleDbConnectionStringBuilder OleStringBuilder = null;

            try
            {
                OleStringBuilder =
                    new OleDbConnectionStringBuilder(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileName + ";Extended Properties='Excel 8.0;HDR=Yes;IMEX=1';");
                OleStringBuilder.DataSource = fileName;

                using (OleDbConnection ExcelConection = new OleDbConnection())
                {
                    ExcelConection.ConnectionString = OleStringBuilder.ConnectionString;


                    using (OleDbCommand ExcelCommand = new OleDbCommand())
                    {
                        ExcelCommand.Connection = ExcelConection;
                        ExcelCommand.CommandText = "Select * From [" + reportName + "$]";


                        ExcelConection.Open();
                        OleDbDataReader ExcelReader = ExcelCommand.ExecuteReader();
                        
                        List<String> list = new List<string>();
                        while (ExcelReader.Read())
                        {
                            string strInner = "";
                            for (int i = 0; i < ExcelReader.FieldCount; i++)
                            {

                                Type t = ExcelReader.GetFieldType(i);
                                if (t == typeof(decimal))
                                    strInner = strInner + ExcelReader.GetDecimal(i).ToString("N2") + "\t";
                                else if (t == typeof(int))
                                    strInner = strInner + ExcelReader.GetInt32(i).ToString() + "\t";
                                else if (t == typeof(float))
                                    strInner = strInner + ExcelReader.GetFloat(i).ToString() + "\t";
                                else if (t == typeof(bool))
                                    strInner = strInner + ExcelReader.GetBoolean(i).ToString() + "\t";
                                else if (t == typeof(DateTime))
                                    strInner = strInner + ExcelReader.GetDateTime(i).ToString("dd/MM/yyyy HH:mm") + "\t";
                                //else if (t == typeof(string))
                                //    strInner = strInner + ExcelReader.GetString(i) + "\t";
                                else
                                    strInner = strInner + ExcelReader.GetValue(i).ToString() + "\t";

                            }
                            strInner = strInner.Replace("\t\t\t\t\t\t\t\t", "\t").Replace("\t\t\t\t\t\t\t", "\t").Replace("\t\t\t\t\t\t", "\t").Replace("\t\t\t\t\t", "\t").Replace("\t\t\t\t", "\t").Replace("\t\t\t", "\t").Replace("\t\t", "\t").Replace("\t", "\t");
                            list.Add(strInner);
                        }
                        string retString = "";
                        foreach (string str in list)
                        {
                            retString = retString + str + "\r\n";
                        }
                        return retString;
                    }
                }
            }
            catch (Exception Args)
            {
                return "";
            }
            finally
            {
                
                
            }
        }
    }
}
