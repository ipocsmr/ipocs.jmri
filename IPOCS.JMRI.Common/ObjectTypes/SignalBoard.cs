using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class SignalBoard : BasicObject
    {
        public override byte objectTypeId { get { return 25; } }

        public byte frequency { get; set; }
        public byte signalHead1 { get; set; }
        public byte signalHead2 { get; set; }
        public byte signalHead3 { get; set; }
        public byte signalHead4 { get; set; }
        public byte signalHead5 { get; set; }
        public byte signalHead6 { get; set; }

        protected override void Serialize(List<byte> buffer)
        {
            buffer.Add(this.frequency);
            buffer.Add(this.signalHead1);
            buffer.Add(this.signalHead2);
            buffer.Add(this.signalHead3);
            buffer.Add(this.signalHead4);
            buffer.Add(this.signalHead5);
            buffer.Add(this.signalHead6);
        }
    }
}
