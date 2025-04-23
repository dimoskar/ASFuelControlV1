using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public class BaseViewModel<T> : INotifyPropertyChanged where T : class, new()
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<QueryLoadArrayArgs> QueryLoadArray;
        public event EventHandler<QueryLoadArrayArgs> QuertSaveArray;

        private bool hasChanges = false;

        public ValidationError[] Errors { set; get; }

        public bool HasChanges
        {
            set
            {
                if (this.hasChanges == value)
                    return;
                this.hasChanges = value;
                this.OnPropertyChanged("HasChanges", true);
            }
            get { return this.hasChanges; }
        }

        public EntityStateEnum EntityState { set; get; }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(propertyName, false);
        }

        protected virtual void OnPropertyChanged(string propertyName, bool preventChanges)
        {
            try
            {
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                if (preventChanges)
                    return;
                if (this.EntityState == EntityStateEnum.Unchanged)
                    this.EntityState = EntityStateEnum.Modified;
                this.HasChanges = true;
            }
            catch(Exception ex)
            {

            }
        }

        public BaseViewModel()
        {

        }

        public BaseViewModel(Data.DatabaseModel db, T entity)
        {
            this.Load(db, entity);
        }

        public virtual void Load(Guid id)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            try
            {
                this.Load(db, id);
            }
            catch(Exception ex)
            {

            }
            finally
            {
                db.Dispose();
            }
        }

        public virtual void Load(Data.DatabaseModel db, Guid id)
        {
            try
            {
                T entity = null;
                try
                {
                    entity = db.GetObjectByKey<T>(new Telerik.OpenAccess.ObjectKey(typeof(T).FullName, id));
                }
                catch(Telerik.OpenAccess.Exceptions.NoSuchObjectException sex)
                {
                    return;
                }
                if (entity == null)
                    return;
                this.Load(db, entity);
            }
            catch(Exception ex)
            {

            }
            finally
            {
                this.HasChanges = false;
            }
        }

        public virtual void Load(Data.DatabaseModel db, T entity)
        {
            
            var props = entity.GetType().GetProperties();
            foreach (var p in props)
            {
                var pdb = this.GetType().GetProperty(p.Name);
                if (pdb == null)
                {
                    if (p.PropertyType.GetInterface("System.Collections.IEnumerable") != null)
                    {
                        this.LoadArrayProperty(db, entity, p.Name);
                    }
                    else if (p.PropertyType.IsSubclassOf(typeof(Data.EntityBase)))
                    {
                        this.LoadChild(db, entity, p.Name);
                    }
                    continue;
                }
                object val = p.GetValue(entity);
                if (val != null && val.GetType() == typeof(byte[]))
                {
                    var valb = BitConverter.ToInt64(((byte[])val).Reverse().ToArray(), 0);
                    pdb.SetValue(this, valb);
                }
                else
                {
                    string pname = p.Name;
                    if (p.PropertyType.GetInterface("System.Collections.IEnumerable") == null || p.PropertyType == typeof(string))
                    {
                        if(p.PropertyType.IsSubclassOf(typeof(Data.EntityBase)))
                        {
                            this.LoadChild(db, entity, p.Name);
                        }
                        else
                        {
                            var original = pdb.GetValue(this);
                            if (val == null && p.PropertyType == typeof(string))
                                val = "";
                            if (original == null && val == null)
                                continue;
                            else if (original == null && val != null)
                                pdb.SetValue(this, val);
                            else if (!original.Equals(val))
                                pdb.SetValue(this, val);
                        }
                    }
                    else
                    {
                        this.LoadArrayProperty(db, entity, p.Name);
                    }
                }
            }
        }

        public virtual bool Save(Guid id)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            try
            {
                var ret = this.Save(db, id);
                if (ret == false)
                    return false;
                var changes = db.GetChanges();
                db.SaveChanges();
                this.HasChanges = false;
                return true;
            }
            catch(Exception ex)
            {
                this.AddError("Save Exception", ex.Message);
                return false;
            }
            finally
            {
                db.Dispose();
            }
        }

        public virtual bool Save(Data.DatabaseModel db, Guid id)
        {
            if (this.Errors == null)
                this.Errors = new ValidationError[] { };
            this.ClearUIErrors();
            List<ValidationError> errors = new List<ValidationError>(this.Errors);
            try
            {
                T entity = null;
                try
                {
                    entity = db.GetObjectByKey<T>(new Telerik.OpenAccess.ObjectKey(typeof(T).FullName, id));
                }
                catch (Telerik.OpenAccess.Exceptions.NoSuchObjectException sex)
                {
                    if (this.EntityState != EntityStateEnum.Added)
                    {
                        errors.Add(new ValidationError() { Caption = "Exception on GetObjectByKey", ErrorMessage = sex.Message });
                        return false;
                    }
                }

                if (entity == null && this.EntityState == EntityStateEnum.Added)
                {
                    entity = new T();
                }
                else if (entity == null && this.EntityState != EntityStateEnum.Added)
                {
                    errors.Add(new ValidationError() { Caption = "Wrong Entity State", ErrorMessage = "Entity is null but not new" });
                    return false;
                }
                var props = entity.GetType().GetProperties();
                if (this.EntityState == EntityStateEnum.Deleted)
                {
                    db.Delete(entity);
                    foreach (var p in props)
                    {
                        var pdb = this.GetType().GetProperty(p.Name);
                        if (pdb == null)
                            continue;
                        if (pdb.PropertyType.IsArray)
                            this.SaveArrayProperty(db, entity, pdb.Name);
                        
                    }
                    return true;
                }
                foreach (var p in props)
                {
                    var pdb = this.GetType().GetProperty(p.Name);
                    if (pdb == null)
                        continue;
                    object val = pdb.GetValue(this);
                    if (val != null && val.GetType() == typeof(byte[]))
                    {
                        var valb = BitConverter.ToInt64(((byte[])val).Reverse().ToArray(), 0);
                        p.SetValue(entity, valb);
                    }
                    else
                    {
                        string pname = p.Name;
                        if (!pdb.PropertyType.IsArray)
                        {
                            if (p.SetMethod == null)
                                continue;
                            var original = p.GetValue(entity);
                            if (val == null && pdb.PropertyType == typeof(string))
                                val = "";
                            if (original == null && val == null)
                                continue;
                            else if (original == null && val != null)
                                p.SetValue(entity, val);
                            else if (!original.Equals(val))
                                p.SetValue(entity, val);
                        }
                        else
                        {
                            string name = p.Name;
                            this.SaveArrayProperty(db, entity, pdb.Name);
                        }
                    }
                }
                if (this.EntityState == EntityStateEnum.Added)
                    db.Add(entity);
                this.EntityState = EntityStateEnum.Unchanged;
                this.HasChanges = false;
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
            finally
            {
                this.Errors = errors.ToArray();
            }
        }

        protected void AddError(string caption, string message)
        {
            AddError(caption, message, false);
        }

        protected void AddError(string caption, string message, bool isUIError)
        {
            List<ValidationError> list = new List<ValidationError>(this.Errors);
            list.Add(new ValidationError() { Caption = caption, ErrorMessage = message, IsUIError = isUIError });
            this.Errors = list.ToArray();
        }

        public void ClearUIErrors()
        {
            if (this.Errors == null)
                this.Errors = new ValidationError[] { };
            this.Errors = this.Errors.Where(e=>!e.IsUIError).ToArray();
        }

        protected virtual void LoadChild(Data.DatabaseModel db, T entity, string propName)
        {

        }

        protected virtual void LoadArrayProperty(Data.DatabaseModel db, T entity, string propName)
        {

        }

        protected virtual void SaveArrayProperty(Data.DatabaseModel db, T entity, string propName)
        {

        }

        protected virtual void OnQueryLoadArray(string propertyName)
        {
            if (this.QueryLoadArray != null)
                this.QueryLoadArray(this, new QueryLoadArrayArgs() { PropertyName = propertyName });
        }
    }

    public class ValidationError
    {
        public string ErrorMessage { set; get; }
        public string Caption { set; get; }
        public bool IsUIError { set; get; }
    }

    public enum EntityStateEnum
    {
        Unchanged,
        Added,
        Modified,
        Deleted
    }

    public class PrimaryKeyAttribute : System.Attribute
    {

    }

    public class QueryLoadArrayArgs : EventArgs
    {
        public string PropertyName { set; get; }
    }
}
