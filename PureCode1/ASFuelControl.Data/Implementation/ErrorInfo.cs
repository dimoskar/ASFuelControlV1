using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Data.Implementation
{
    public class ErrorInfo
    {
        private List<ErrorInfoEntry> entries = new List<ErrorInfoEntry>();

        public ErrorInfoEntry[] Entries
        {
            get
            {
                return this.entries.ToArray();
            }
        }

        public ErrorInfoStateEnum ErrorInfoState { private set; get; }

        public void AddErrorInfo(string message, ErrorInfoStateEnum state)
        {
            this.entries.Add(new ErrorInfoEntry(message, state));
            if (this.ErrorInfoState < state)
                this.ErrorInfoState = state;
        }

        public void AddErrorInfo(string propertyName, string message, ErrorInfoStateEnum state)
        {
            this.entries.Add(new ErrorInfoEntry(propertyName, message, state));
            if (this.ErrorInfoState < state)
                this.ErrorInfoState = state;
        }

        public void MergeErrorInfo(ErrorInfo info)
        {
            foreach (ErrorInfoEntry entry in info.Entries)
                this.AddErrorInfo(entry.EntityPropertyName, entry.Message, entry.ErrorInfoState);
        }
    }

    public class ErrorInfoEntry
    {
        public string Message { private set; get; }
        public ErrorInfoStateEnum ErrorInfoState { private set; get; }
        public string EntityPropertyName { set; get; }

        public ErrorInfoEntry(string message, ErrorInfoStateEnum state)
        {
            Message = message;
            ErrorInfoState = state;
        }

        public ErrorInfoEntry(string propertyName, string message, ErrorInfoStateEnum state)
        {
            Message = message;
            ErrorInfoState = state;
            EntityPropertyName = propertyName;
        }
    }

    public enum ErrorInfoStateEnum
    {
        Info,
        Warning,
        Error
    }
}
