using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace Telerik.OpenAccess// ASFuelControl.Data
{
    public static class EntityFactory
    {
        public static T CreateEntity<T>(this ASFuelControl.Data.DatabaseModel db)
        {
            T entity = (T)Activator.CreateInstance(typeof(T));
            try
            {
                if (typeof(T) == typeof(ASFuelControl.Data.TankFilling))
                {
                    ASFuelControl.Data.TankFilling tf = entity as ASFuelControl.Data.TankFilling;
                    tf.ApplicationUserId = ASFuelControl.Data.DatabaseModel.CurrentUserId;
                }
                else if (typeof(T) == typeof(ASFuelControl.Data.SalesTransaction))
                {
                    ASFuelControl.Data.SalesTransaction st = entity as ASFuelControl.Data.SalesTransaction;
                    st.ApplicationUserId = ASFuelControl.Data.DatabaseModel.CurrentUserId;
                }
                else if (typeof(T) == typeof(ASFuelControl.Data.Invoice))
                {
                    ASFuelControl.Data.Invoice inv = entity as ASFuelControl.Data.Invoice;
                    inv.ApplicationUserId = ASFuelControl.Data.DatabaseModel.CurrentUserId;
                }
                else if (typeof(T) == typeof(ASFuelControl.Data.Balance))
                {
                    ASFuelControl.Data.Balance bal = entity as ASFuelControl.Data.Balance;
                    bal.ApplicationUserId = ASFuelControl.Data.DatabaseModel.CurrentUserId;
                }
                else if (typeof(T) == typeof(ASFuelControl.Data.SendLog))
                {

                }
                //if (!ASFuelControl.Data.Implementation.OptionHandler.Instance.IsFinalized)
                //{
                //    if (typeof(T) == typeof(ASFuelControl.Data.TankFilling))
                //        return entity;
                //    else if (typeof(T) == typeof(ASFuelControl.Data.SalesTransaction))
                //        return default(T);
                //    else if (typeof(T) == typeof(ASFuelControl.Data.NozzleUsagePeriod))
                //        return default(T);
                //    else if (typeof(T) == typeof(ASFuelControl.Data.TankUsagePeriod))
                //        return default(T);
                //}
                db.Add(entity);
                return entity;
            }
            catch
            {
                return default(T);
            }
        }

        public static T CreateEntity<T>(this ASFuelControl.Data.DatabaseModel db, List<NameValue> values)
        {
            T entity = (T)Activator.CreateInstance(typeof(T));
            try
            {
                if (typeof(T) == typeof(ASFuelControl.Data.TankFilling))
                {
                    ASFuelControl.Data.TankFilling tf = entity as ASFuelControl.Data.TankFilling;
                    tf.ApplicationUserId = ASFuelControl.Data.DatabaseModel.CurrentUserId;
                }
                else if (typeof(T) == typeof(ASFuelControl.Data.SalesTransaction))
                {
                    ASFuelControl.Data.SalesTransaction st = entity as ASFuelControl.Data.SalesTransaction;
                    st.ApplicationUserId = ASFuelControl.Data.DatabaseModel.CurrentUserId;
                }
                else if (typeof(T) == typeof(ASFuelControl.Data.Invoice))
                {
                    ASFuelControl.Data.Invoice inv = entity as ASFuelControl.Data.Invoice;
                    inv.ApplicationUserId = ASFuelControl.Data.DatabaseModel.CurrentUserId;
                }
                else if (typeof(T) == typeof(ASFuelControl.Data.Balance))
                {
                    ASFuelControl.Data.Balance bal = entity as ASFuelControl.Data.Balance;
                    bal.ApplicationUserId = ASFuelControl.Data.DatabaseModel.CurrentUserId;
                }
                //if (!ASFuelControl.Data.Implementation.OptionHandler.Instance.IsFinalized)
                //{
                //    if (typeof(T) == typeof(ASFuelControl.Data.TankFilling))
                //        return default(T);
                //    else if (typeof(T) == typeof(ASFuelControl.Data.SalesTransaction))
                //        return default(T);
                //    else if (typeof(T) == typeof(ASFuelControl.Data.NozzleUsagePeriod))
                //        return default(T);
                //    else if (typeof(T) == typeof(ASFuelControl.Data.TankUsagePeriod))
                //        return default(T);
                //}
                foreach (NameValue val in values)
                {
                    typeof(T).GetProperty(val.PropertyName).SetValue(entity, val.Value, null);
                }
                db.Add(entity);
                return entity;
            }
            catch
            {
                return default(T);
            }
        }

        public static void DeleteEntity(this ASFuelControl.Data.DatabaseModel db, object entity)
        {

            try
            {
                db.Delete(entity);
            }
            catch
            {
            }
        }

        public static UInt32 CalculateCRC32(this object entity)
        {
            PropertyInfo[] properties = entity.GetType().GetProperties();
            List<byte> bytes = new List<byte>();
            foreach (PropertyInfo prop in properties)
            {
                if (prop.GetSetMethod() == null)
                    continue;
                if (prop.PropertyType.IsArray)
                    continue;
                if (prop.Name == "CRC")
                    continue;
                //if (prop.PropertyType.IsArray)
                //    continue;
                object value = prop.GetValue(entity, null);
                if (value == null)
                    continue;
                bytes.AddRange(System.Text.Encoding.Default.GetBytes(value.ToString()));
            }
            return ASFuelControl.Data.Crc32.Compute(bytes.ToArray());
        }

        public static UInt32 CalculateCRC32(this object entity, uint seed)
        {
            PropertyInfo[] properties = entity.GetType().GetProperties();
            List<byte> bytes = new List<byte>();
            foreach (PropertyInfo prop in properties)
            {
                if (prop.GetSetMethod() == null)
                    continue;
                if (prop.PropertyType.IsArray)
                    continue;
                if (prop.Name == "CRC")
                    continue;
                //if (prop.PropertyType.IsArray)
                //    continue;
                object value = prop.GetValue(entity, null);
                if (value == null)
                    continue;
                bytes.AddRange(System.Text.Encoding.Default.GetBytes(value.ToString()));
            }
            return ASFuelControl.Data.Crc32.Compute(seed, bytes.ToArray());
        }

        public static byte[] GetByteArray(this object entity)
        {
            PropertyInfo[] properties = entity.GetType().GetProperties();
            List<byte> bytes = new List<byte>();
            foreach (PropertyInfo prop in properties)
            {
                if (prop.GetSetMethod() == null)
                    continue;
                if (prop.PropertyType.IsArray)
                    continue;
                if (prop.PropertyType.IsGenericType)
                    continue;
                if (prop.Name == "CRC")
                    continue;
                object value = prop.GetValue(entity, null);
                if (value == null)
                    continue;
                bytes.AddRange(System.Text.Encoding.Default.GetBytes(value.ToString()));
            }
            return bytes.ToArray();
        }
    }

    public class NameValue
    {
        public string PropertyName { set; get; }
        public object Value { set; get; }

        public NameValue()
        {
        }

        public NameValue(string propertyName, object value)
        {
            this.PropertyName = propertyName;
            this.Value = value;
        }
    }
}
