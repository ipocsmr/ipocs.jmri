using System;

namespace ipocs.jmri
{
    public class Sop
    {
        public enum State { Unknown, Thrown, Closed };
        public enum Straight { Unknown, Left, Right };

        public string name { get; }
        public string sysname { get; }
        public Ocs ocs { get; }

        public string url { get; }
        private State state { get; set; } 

        private void SetState(State value) {
            if (state != value)
                changed = true;
            state = value;
         }

        public State GetState() {
            return state;
         }

        public Straight straight { get; }

        bool changed;

        public Sop(Ocs ocs, string name, string url, string sysname, Straight straight) {
            this.ocs = ocs;
            this.name = name;
            this.url = url;
            this.sysname = sysname;
            this.straight = straight;
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
            SetState(straight == Straight.Left ? State.Closed : State.Thrown);
        }
        public void SetRight() {
            SetState(straight == Straight.Right ? State.Closed : State.Thrown);
        }

        public bool IsChanged() {
            return changed;
        }

        public void ClearChange() {
            changed = false;
        }
    }
}