using System.Collections.Generic;

namespace IPOCS.JMRI
{
    public class Ocs
    {
        public string unitid { get; }
        List<Sop> sops = new List<Sop>();

        bool connected;

        public Ocs(string unitid) {
            this.unitid = unitid;
            connected = false;
        }

        public void Lost() {
            foreach (Sop p in sops) {
                p.SetUnknown();
            }
            connected = false;
        }

        public void Add(Sop sop) {
            // todo check if ambigues object already in list
            sops.Add(sop);
        }

        public bool IsConnected() {
            return connected;
        }


    }
}