using System;

namespace IPOCS.JMRI
{
    public class Sop
    {
        public enum State { Unknown, Thrown, Closed, Moving, OutOfControl };

        public string name { get; }
        public string sysname { get; }
        public Ocs ocs { get; }

        public string url { get; }
        private State state { get; set; } 
        //private State previousState { get; set; } 

        private void SetState(State value) {
            if (state != value)
                changed = true;
            state = value;
         }

        public State GetState() {
            return state;
         }

        bool changed;

        public Sop(Ocs ocs, string name, string url, string sysname) {
            this.ocs = ocs;
            this.name = name;
            this.url = url;
            this.sysname = sysname;
            this.state = State.Unknown;
        }

        public void SetClosed() {
            SetState(State.Closed);
        }

        public void SetThrown() {
            SetState(State.Thrown);
        }

        public void SetUnknown() {
            SetState(State.Unknown);
        }
        public void SetLeft() {
            SetState(State.Closed);
        }
        public void SetRight() {
            SetState(State.Thrown);
        }

        public bool IsChanged() {
            return changed;
        }

        public void ClearChange() {
            changed = false;
        }
    }
}