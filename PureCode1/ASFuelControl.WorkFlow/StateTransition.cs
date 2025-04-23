using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace ASFuelControl.WorkFlow
{
    public class StateTransition
    {
        public event EventHandler TransitionValidated;
        public event EventHandler TransitionValidating;

        delegate void TimerEllapsedDelegate(object state);

        System.Threading.Timer t;

        public ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate ValidationMethod { set; get; }
        public object ValidationParameter { set; get; }
        public WorkFlowProcess Process { set; get; }
        public ProcessState SourceState { set; get; }
        public ProcessState TargetState { set; get; }
        public TimeSpan WaitDuration { set; get; }
        public DateTime TimeInit { set; get; }

        public StateTransition(WorkFlowProcess process, ProcessState source, ProcessState target, ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate method)
        {
            this.WaitDuration = TimeSpan.FromMilliseconds(0);
            this.Process = process;
            this.SourceState = source;
            this.TargetState = target;
            this.ValidationMethod = method;
            t = new System.Threading.Timer(this.TimerEllapsed, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        public void StartTimer()
        {
            this.t.Change((int)this.WaitDuration.TotalMilliseconds, System.Threading.Timeout.Infinite);
            
        }

        private void TimerEllapsed(object state)
        {
            //foreach (string varName in this.variableChanges.Keys)
            //    this.Process.SetVariableValue(varName, this.variableChanges[varName]);

            if (this.TransitionValidating != null)
                this.TransitionValidating(this, new EventArgs());

            //foreach (MethodInfo m in this.transitionMethods)
            //    this.InvokeTransitionMethod(m);

            if (this.TransitionValidated != null)
                this.TransitionValidated(this, new EventArgs());
        }

        public bool ValidateTransition()
        {
            if (!this.Process.IsInitialized)
                return false;
            bool returnValue = false;
            try
            {
                if (WaitDuration.TotalMilliseconds > 0)
                {
                    if (TimeInit.Add(WaitDuration) >= DateTime.Now)
                    {
                        returnValue = true;
                        return returnValue;
                    }
                    return returnValue;
                }
                //if(this.ValidationParameter == null)
                //    System.IO.File.AppendAllText("WorkFlow.log", this.ValidationMethod.Method.Name + " - ");
                //else
                //    System.IO.File.AppendAllText("WorkFlow.log", this.ValidationMethod.Method.Name + " - " + this.ValidationParameter.ToString());
                bool ret = this.ValidationMethod.Invoke(this.ValidationParameter);
                if (ret)
                {
                    returnValue = true;
                    //System.IO.File.AppendAllText("WorkFlow.log", "   TRUE " + "\r\n");
                    return returnValue;
                }
                //System.IO.File.AppendAllText("WorkFlow.log", "   FALSE " + "\r\n");
                return returnValue;
                
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                if (returnValue)
                {
                    System.Console.WriteLine("EVALUATED Transition : " + this.ToString());
                    //foreach (string varName in this.variableChanges.Keys)
                    //    this.Process.SetVariableValue(varName, this.variableChanges[varName]);

                    if (this.TransitionValidating != null)
                        this.TransitionValidating(this, new EventArgs());

                    //foreach (MethodInfo m in this.transitionMethods)
                    //    this.InvokeTransitionMethod(m);

                    if (this.TransitionValidated != null)
                        this.TransitionValidated(this, new EventArgs());
                }
                else
                {
                    //System.Console.WriteLine("FAILED Transition : " + this.ToString());
                }
            }
        }

        public override string ToString()
        {
            return this.SourceState.ToString() + " -> " + this.TargetState.ToString();
        }
    }
}
