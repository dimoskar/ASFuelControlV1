using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Globalization;

namespace ASFuelControl.Data.Implementation
{
    public class OptionHandler
    {
        private ConcurrentDictionary<string, string> optionValues = new ConcurrentDictionary<string, string>();

        public static string ConnectionString { set; get; }

        private static OptionHandler instance;

        public static OptionHandler Instance
        {
            get
            {
                if (instance == null)
                    instance = new OptionHandler();
                return instance;
            }
        }

        DatabaseModel database = new DatabaseModel(ConnectionString);

        public DatabaseModel Database
        {
            get { return this.database; }
        }

        public bool IsFinalized
        {
            get
            {
                return this.GetBoolOption("IsFinalized", false);
            }
        }

        public OptionHandler()
        {
            this.LoadOptions();
        }

        public string GetOption(string key)
        {
            if (!this.optionValues.ContainsKey(key))
                return null;

            try
            {
                string val = AESEncryption.Decrypt(this.optionValues[key], "Exedron@#");
                return val;
            }
            catch
            {
                return this.optionValues[key];
            }
            
        }

        public string GetOption(string key, string defaultValue)
        {
            if (!this.optionValues.ContainsKey(key))
                return defaultValue;

            try
            {
                string val = AESEncryption.Decrypt(this.optionValues[key], "Exedron@#");
                return val;
            }
            catch
            {
                return this.optionValues[key];
            }

        }

        public string GetOptionOrAdd(string key, string defaultValue)
        {
            if (!this.optionValues.ContainsKey(key))
            {
                this.SetOption(key, defaultValue);
                return defaultValue;
            }
            string val = AESEncryption.Decrypt(this.optionValues[key], "Exedron@#");
            return val;
        }

        public bool OptionExists(string key)
        {
            if (!this.optionValues.ContainsKey(key))
                return false;
            return true;
        }

        public bool GetBoolOption(string key, bool defaultValue)
        {
            string val = this.GetOption(key);
            if (val == null)
                return defaultValue;
            bool outVal = defaultValue;
            if (bool.TryParse(val, out outVal))
                return outVal;
            return defaultValue;
        }

        public int GetIntOption(string key, int defaultValue)
        {
            string val = this.GetOption(key);
            if (val == null)
                return defaultValue;
            int outVal = defaultValue;
            if (int.TryParse(val, out outVal))
                return outVal;
            return defaultValue;
        }

        public short GetShortOption(string key, short defaultValue)
        {
            string val = this.GetOption(key);
            if (val == null)
                return defaultValue;
            short outVal = defaultValue;
            if (short.TryParse(val, out outVal))
                return outVal;
            return defaultValue;
        }

        public decimal GetDecimalOption(string key, decimal defaultValue)
        {
            string val = this.GetOption(key);
            if (val == null)
                return defaultValue;
            decimal outVal = defaultValue;
            val = val.Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            if (decimal.TryParse(val, out outVal))
                return outVal;
            return defaultValue;
        }

        public DateTime GetDateTimeOption(string key, DateTime defaultValue)
        {
            string val = this.GetOption(key);
            if (val == null)
                return defaultValue;
            DateTime outVal = defaultValue;

            if (DateTime.TryParse(val, out outVal))
                return outVal;
            return defaultValue;
        }

        public TimeSpan GetTimeSpanOption(string key, TimeSpan defaultValue)
        {
            string val = this.GetOption(key);
            if (val == null)
                return defaultValue;
            TimeSpan outVal = defaultValue;
            if (TimeSpan.TryParseExact(val, "g", CultureInfo.CurrentCulture, out outVal))
                return outVal;
            return defaultValue;
        }

        public Guid GetGuidOption(string key, Guid defaultValue)
        {
            string val = this.GetOption(key);
            if (val == null)
                return defaultValue;
            Guid outVal = defaultValue;
            if (Guid.TryParse(val, out outVal))
                return outVal;
            return defaultValue;
        }

        public void SetOption(DatabaseModel db, string key, object value)
        {
            string encVal = AESEncryption.Encrypt(value.ToString(), "Exedron@#");

            DatabaseModel data = db;
            Option option = data.Options.Where(o => o.OptionKey == key).FirstOrDefault();
            if (option == null)
            {
                option = new Option();
                option.OptionId = Guid.NewGuid();
                option.OptionKey = key;
                option.OptionValue = encVal;// value.ToString();
                data.Add(option);
            }
            option.OptionValue = encVal;
            data.SaveChanges();
            this.LoadOptions();
            //data.Dispose();
        }

        public void SetOption(string key, object value)
        {
            string encVal = AESEncryption.Encrypt(value.ToString(), "Exedron@#");

            DatabaseModel data = this.database;
            Option option = data.Options.Where(o => o.OptionKey == key).FirstOrDefault();
            if(option == null)
            {
                option = new Option();
                option.OptionId = Guid.NewGuid();
                option.OptionKey = key;
                option.OptionValue = encVal;// value.ToString();
                data.Add(option);
            }
            option.OptionValue = encVal;
            data.SaveChanges();
            this.LoadOptions();
        }

        public string DecryptValue(string value)
        {
            return AESEncryption.Decrypt(value, "Exedron@#");
        }

        public void LoadOptions()
        {
            this.database.Refresh( Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, this.database.Options);
            this.optionValues.Clear();
            foreach (Option option in this.database.Options)
            {
                this.optionValues.TryAdd(option.OptionKey, option.OptionValue);
            }
        }

        public bool HasOTPEnabled
        {
            get
            {
                return this.database.OutdoorPaymentTerminals.Count() > 0;
            }
        }
    }
}
