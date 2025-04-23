using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Data.Implementation
{
    public class Country
    {
        public string DisplayValue { set; get; }
        public string CodeValue { set; get; }
        public bool IsEu { set; get; }

        public static Country[] GetCountries()
        {
            List<Country> countryList = new List<Country>();
            using (var db = new Data.DatabaseModel(DatabaseModel.ConnectionString))
            {
                var countries = db.Countries.OrderBy(a => a.CountryCode);
                foreach(var c in countries)
                {
                    countryList.Add(new Country() { CodeValue = c.CountryCode, DisplayValue = c.DisplayMember, IsEu = c.IsEu });
                }
            }
            return countryList.ToArray();
        }

        public static bool IsEuCountry(string code)
        {
            using (var db = new Data.DatabaseModel(DatabaseModel.ConnectionString))
            {
                var c = db.Countries.FirstOrDefault(c1 => c1.CountryCode == code);
                if (c == null)
                    return false;
                return c.IsEu;
            }
        }
    }
}
