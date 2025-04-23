using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Data
{
    public partial class EntityBase : IDataErrorInfo
    {
        private string error = string.Empty;

        public bool Selected { set; get; }

        public string Error
        {
            get
            {
                return this.error;
            }
        }

        public string this[string propertyName]
        {
            get
            {
                this.ValidatePropertyInternal(propertyName, ref this.error);

                return this.error;
            }
        }

        protected virtual void ValidatePropertyInternal(string propertyName, ref string error)
        {
            this.ValidateProperty(propertyName, ref error);
        }

        // Please implement this method in a partial class in order to provide the error message depending on each of the properties.
        partial void ValidateProperty(string propertyName, ref string error);
    }
}
