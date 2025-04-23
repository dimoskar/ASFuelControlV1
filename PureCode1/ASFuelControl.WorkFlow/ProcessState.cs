using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.WorkFlow
{
    public class ProcessState
    {
        public event EventHandler QueryChangeState;

        private Dictionary<string, object> variableChanges = new Dictionary<string, object>();
        private DateTime stateInitializedTime = DateTime.MaxValue;
        System.Threading.Timer t = null;

        public ProcessState FallbackState { set; get; }

        public DateTime StateInitializedTime 
        {
            set 
            { 
                this.stateInitializedTime = value;
                if (this.ErrorDuration == 0)
                    return;
                t.Change(this.ErrorDuration, System.Threading.Timeout.Infinite);
                
            }
            get { return this.stateInitializedTime; }
        }
        public int ErrorDuration { set; get; }

        public string Name { set; get; }
        public WorkFlowProcess Process { set; get; }

        public ProcessState(WorkFlowProcess process, string name)
        {
            this.t = new System.Threading.Timer(new System.Threading.TimerCallback(this.LeaveState), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            this.Process = process;
            this.Name = name;
        }

        public ProcessState(WorkFlowProcess process, string name, int errorDuration)
        {
            this.t = new System.Threading.Timer(new System.Threading.TimerCallback(this.LeaveState), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            this.Process = process;
            this.Name = name;
            this.ErrorDuration = errorDuration;
        }

        public void DefinaVariableChange(string varName, object value)
        {
            this.variableChanges.Add(varName, value);
        }

        private void LeaveState(object foo)
        {
            if (this.QueryChangeState != null)
                this.QueryChangeState(this, new EventArgs());
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
