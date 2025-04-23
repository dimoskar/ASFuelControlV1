using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;

namespace ASFuelControl.Communication.Enums
{
    public static class LocalizedEnumExtensions
    {
        private static ResourceManager _resources = new ResourceManager("ASFuelControl.Communication.Properties.Resources",
            System.Reflection.Assembly.GetExecutingAssembly());

        public static string GetLocalizedName(Enum value)
        {
            Dictionary<Int16, string> strings = new Dictionary<Int16, string>();
            var enumValues = Enum.GetValues(value.GetType());
            foreach (var e in enumValues)
            {
                if (!e.Equals(value))
                    continue;
                string localizedDescription = _resources.GetString(String.Format("{0}_{1}", e.GetType().Name, e));
                return localizedDescription;
            }
            return "";
        }

        public static Dictionary<Int16, string> GetLocalizedNames(Type enum1)
        {
            Dictionary<Int16, string> strings = new Dictionary<Int16, string>();
            var enumValues = Enum.GetValues(enum1);
            foreach (var e in enumValues)
            {
                string localizedDescription = _resources.GetString(String.Format("{0}_{1}", e.GetType().Name, e));
                if (String.IsNullOrEmpty(localizedDescription))
                {
                    strings.Add((Int16)(int)e, e.ToString());
                }
                else
                {
                    strings.Add((Int16)(int)e, localizedDescription);
                }
            }
            return strings;
        }
    }
}
