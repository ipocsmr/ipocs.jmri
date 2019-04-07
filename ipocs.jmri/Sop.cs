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
        State state {
            get => state;
            set {
                if (state != value)
                    changed = true;
                state = value;
            }
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
            state = State.Closed;
        }

        public void SetThrown() {
            state = State.Thrown;
        }

        public void SetUnknown() {
            state = State.Unknown;
        }
        public void SetLeft() {
            state = straight == Straight.Left ? State.Closed : State.Thrown;
        }
        public void SetRight() {
            state = straight == Straight.Right ? State.Closed : State.Thrown;
        }

        public bool IsChanged() {
            return changed;
        }

        public void ClearChange() {
            changed = false;
        }
    }
}