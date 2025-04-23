using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.OpenAccess;

namespace ASFuelControl.Data.Implementation
{
    public class ValidationErrorEventArgs : EventArgs
    {
        public ValidationErrorEventArgs()
        {
        }

        public ValidationErrorEventArgs(ErrorInfo error, PersistenceEventArgs args)
        {
            this.Error = error;
            this.PersistenceArgs = args;
        }

        public ErrorInfo Error { set; get; }
        public PersistenceEventArgs PersistenceArgs { set; get; }
        public bool Cancel { set; get; }
    }
}
