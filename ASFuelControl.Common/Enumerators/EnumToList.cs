using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Enumerators
{
    public class EnumToList
    {
        public static List<EnumItem> EnumList<T>()
        {
            List<EnumItem> list = new List<EnumItem>();

            foreach (var it in Enum.GetValues(typeof(T)))
            {
                EnumItem item = new EnumItem();
                string resName = typeof(T).Name + "_" + it.ToString();
                item.Value = (int)it;
                try
                {
                    item.Description = Properties.Resources.ResourceManager.GetString(resName);
                }
                catch
                {
                    item.Description = "";
                }
                list.Add(item);
            }
            return list;
        }

        public static List<EnumItemSmallInt> EnumListSmallInt<T>()
        {
            List<EnumItemSmallInt> list = new List<EnumItemSmallInt>();

            foreach (var it in Enum.GetValues(typeof(T)))
            {
                EnumItemSmallInt item = new EnumItemSmallInt();
                string resName = typeof(T).Name + "_" + it.ToString();
                item.Value = (Int16)(int)it;
                try
                {
                    item.Description = Properties.Resources.ResourceManager.GetString(resName);
                }
                catch
                {
                    item.Description = "";
                }
                list.Add(item);
            }
            return list;
        }

        public static List<EnumItemNullable> EnumNullableList<T>()
        {
            List<EnumItemNullable> list = new List<EnumItemNullable>();

            foreach (var it in Enum.GetValues(typeof(T)))
            {
                EnumItemNullable item = new EnumItemNullable();
                string resName = typeof(T).Name + "_" + it.ToString();
                item.Value = (int)it;
                try
                {
                    item.Description = Properties.Resources.ResourceManager.GetString(resName);
                }
                catch
                {
                    item.Description = "";
                }
                list.Add(item);
            }
            return list;
        }

        public static string GetDescription<T>(object value)
        {
            foreach (var it in Enum.GetValues(typeof(T)))
            {
                if (!it.Equals(value))
                    continue;
                string resName = typeof(T).Name + "_" + it.ToString();

                try
                {
                    return Properties.Resources.ResourceManager.GetString(resName);
                }
                catch
                {
                    return value.ToString();
                }
            }
            return "";
        }
    }

    public class EnumItem
    {
        public string Description { set; get; }
        public int Value { set; get; }

    }

    public class EnumItemNullable : EnumItem
    {
        public new int? Value { set; get; }
    }

    public class EnumItemSmallInt
    {
        public string Description { set; get; }
        public Int16 Value { set; get; }

    }
}
