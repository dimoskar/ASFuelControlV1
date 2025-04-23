using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Reflection;
using Telerik.OpenAccess;

namespace ASFuelControl.Data.Validators
{
    public class ValidatorContainer
    {
        private Dictionary<Type, IList> dictionary = new Dictionary<Type, IList>();

        public void AddValidator<T>(IDataValidator<T> validator) where T : IDataErrorInfo
        {
            if (!this.dictionary.ContainsKey(typeof(T)))
                this.dictionary.Add(typeof(T), new List<object>());
            this.dictionary[typeof(T)].Add(validator);
        }

        public IList GetValidators(Type entitType)
        {
            if (!this.dictionary.ContainsKey(entitType))
                return null;
            return this.dictionary[entitType];
        }

        public void AddDataValidator(Type validatorType)
        {
            object module = this.CreateModuleInstance(validatorType);
            MethodInfo method = this.GetType().GetMethod("AddValidator");
            MethodInfo generic = method.MakeGenericMethod(module.GetType());
            generic.Invoke(this, new object[] { module });
        }

        public Data.Implementation.ErrorInfo ExecuteBeforeInsert(AddEventArgs args, object validator)
        {
            MethodInfo method = validator.GetType().GetMethod("BeforeInsert");
            Data.Implementation.ErrorInfo error = method.Invoke(validator, new object[] { args }) as Data.Implementation.ErrorInfo;
            return error;
        }

        public Data.Implementation.ErrorInfo ExecuteBeforeUpdate(ChangeEventArgs args, object validator)
        {
            MethodInfo method = validator.GetType().GetMethod("BeforeUpdate");
            Data.Implementation.ErrorInfo error = method.Invoke(validator, new object[] { args }) as Data.Implementation.ErrorInfo;
            return error;
        }

        public Data.Implementation.ErrorInfo ExecuteBeforeDelete(RemoveEventArgs args, object validator)
        {
            MethodInfo method = validator.GetType().GetMethod("BeforeDelete");
            Data.Implementation.ErrorInfo error = method.Invoke(validator, new object[] { args }) as Data.Implementation.ErrorInfo;
            return error;
        }

        public Data.Implementation.ErrorInfo ExecuteAfterInsert(AddEventArgs args, object validator)
        {
            MethodInfo method = validator.GetType().GetMethod("AfterInsert");
            Data.Implementation.ErrorInfo error = method.Invoke(validator, new object[] { args }) as Data.Implementation.ErrorInfo;
            return error;
        }

        public Data.Implementation.ErrorInfo ExecuteAfterUpdate(ChangeEventArgs args, object validator)
        {
            MethodInfo method = validator.GetType().GetMethod("AfterUpdate");
            Data.Implementation.ErrorInfo error = method.Invoke(validator, new object[] { args }) as Data.Implementation.ErrorInfo;
            return error;
        }

        public Data.Implementation.ErrorInfo ExecuteAfterDelete(RemoveEventArgs args, object validator)
        {
            MethodInfo method = validator.GetType().GetMethod("AfterDelete");
            Data.Implementation.ErrorInfo error = method.Invoke(validator, new object[] { args }) as Data.Implementation.ErrorInfo;
            return error;
        }

        public void ResetChanges(ChangeEventArgs args)
        {
            if(args.NewValue == null && args.OldValue == null)
                return;
            if(args.NewValue.Equals(args.OldValue))
                return;
            var method = typeof(ExtensionMethods).GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name == "SetFieldValue").FirstOrDefault();
            MethodInfo generic = method.MakeGenericMethod(args.PersistentObject.GetType().GetProperty(args.PropertyName).PropertyType);
            generic.Invoke(args, new object[] { args.PersistentObject, args.PropertyName, args.OldValue });
        }

        public void InitializeObject(IDataErrorInfo entity, object validator)
        {
            MethodInfo method = validator.GetType().GetMethod("InitializeEntity");
            //MethodInfo generic = method.MakeGenericMethod(entity.GetType());
            method.Invoke(validator, new object[] { entity });
        }

        private object CreateModuleInstance(Type validatorType)
        {
            object validator = validatorType.Assembly.CreateInstance(validatorType.FullName);
            return validator;
        }
    }
}
