using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ASFuelControl.WorkFlow
{
    public class WorkFlowProcess
    {
        public event EventHandler StateChanged;

        public delegate bool ValidationkDelegate(object values);

        private List<ProcessState> states = new List<ProcessState>();
        private List<StateTransition> transitions = new List<StateTransition>();
        private ProcessState currentState;
        private ProcessState previousState;

        public DateTime ChangedStateTime { private set; get; }

        public StateTransition[] Transitions
        {
            get { return this.transitions.ToArray(); }
        }

        public bool IsInitialized { set; get; }

        public ProcessState CurrentState 
        {
            set 
            {
                ProcessState pr = this.currentState;
                this.currentState = value;
                this.currentState.StateInitializedTime = DateTime.Now;
                if(pr != this.currentState)
                    this.ChangedStateTime = DateTime.Now;
            }
            get { return this.currentState; }
        }

        public ProcessState PreviousState
        {
            set { this.previousState = value; }
            get { return this.previousState; }
        }

        public virtual void Initialize()
        {
            
        }

        public ProcessState AddProcessState(string name)
        {
            ProcessState state = new ProcessState(this, name);
            this.states.Add(state);
            return state;
        }

        public ProcessState AddProcessState(string name, int errorDuration)
        {
            ProcessState state = new ProcessState(this, name, errorDuration);
            this.states.Add(state);
            state.QueryChangeState += new EventHandler(state_QueryChangeState);
            return state;
        }

        void state_QueryChangeState(object sender, EventArgs e)
        {
            ProcessState state = sender as ProcessState;
            if (state == null || state != this.currentState || this.currentState.FallbackState == null)
                return;
            this.previousState = this.currentState;
            this.CurrentState = this.currentState.FallbackState;


            if (this.StateChanged != null)
                this.StateChanged(this, new EventArgs());
        }

        public void AddProcessTransition(ProcessState stateSource, ProcessState stateTarget, ValidationkDelegate method, object parameter)
        {

            if (stateSource == null)
                throw (new Exception(string.Format("Source State cannot be null")));
            if (stateTarget == null)
                throw (new Exception(string.Format("Target State cannot be null")));

            StateTransition transition = new StateTransition(this, stateSource, stateTarget, method);
            transition.ValidationParameter = parameter;
            transition.TransitionValidated += new EventHandler(transition_TransitionValidated);
            //transition.TransitionValidating += new EventHandler(transition_TransitionValidating);
            this.transitions.Add(transition);
        }

        public void AddProcessTransition(ProcessState stateSource, ProcessState stateTarget, ValidationkDelegate method, object parameter, TimeSpan ts)
        {

            if (stateSource == null)
                throw (new Exception(string.Format("Source State cannot be null")));
            if (stateTarget == null)
                throw (new Exception(string.Format("Target State cannot be null")));

            StateTransition transition = new StateTransition(this, stateSource, stateTarget, method);
            transition.WaitDuration = ts;
            transition.ValidationParameter = parameter;
            transition.TransitionValidated += new EventHandler(transition_TransitionValidated);
            //transition.TransitionValidating += new EventHandler(transition_TransitionValidating);
            this.transitions.Add(transition);
        }

        public StateTransition GetTransition(string sourceState, string targetState)
        {
            return this.transitions.Where(tr => tr.SourceState.Name == sourceState && tr.TargetState.Name == targetState).FirstOrDefault();
        }

        public ProcessState GetState(string name)
        {
            return this.states.Where(s => s.Name == name).FirstOrDefault();
        }

        public void EvaluateWorkFlow()
        {
            try
            {
                var q = this.transitions.Where(t => t.WaitDuration.TotalMilliseconds > 0).ToList();
                foreach (StateTransition transition in q)
                {
                    if (transition.ValidateTransition())
                        return;
                }
                var q1 = this.transitions.Where(t => t.WaitDuration.TotalMilliseconds == 0 && t.SourceState == this.CurrentState).ToList();
                foreach (StateTransition transition in q1)
                {
                    if (transition.ValidateTransition())
                        break;
                }
            }
            catch (Exception ex)
            {
            }
        }

        void  transition_TransitionValidated(object sender, EventArgs e)
        {
            StateTransition transition = sender as StateTransition;

            foreach (StateTransition trans in this.transitions)
            {
                if(trans.SourceState.Name == transition.TargetState.Name && trans.WaitDuration.TotalMilliseconds > 0)
                    trans.StartTimer();
            }
            
            this.previousState = this.currentState;
            this.CurrentState = transition.TargetState;
            

            if (this.StateChanged != null)
                this.StateChanged(this, new EventArgs());
        }
    }
}
