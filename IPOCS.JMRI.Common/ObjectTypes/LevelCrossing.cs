using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class LevelCrossing : BasicObject
    {
        public override byte objectTypeId { get { return 20; } }

        public byte lightWhite { get; set; }
        public byte lightRed { get; set; }
        public byte sound { get; set; }
        public byte trainDetection { get; set; }
        public byte barrier1 { get; set; }
        public byte barrier2 { get; set; }
        public byte barrier3 { get; set; }
        public byte barrier4 { get; set; }

        public override IList<Type> SupportedOrders
        {
            get
            {
                var list = base.SupportedOrders;
                list.Add(typeof(IPOCS.Protocol.Packets.Orders.SetLevelCrossing));
                return list;
            }
        }

        protected override void Serialize(List<byte> buffer)
        {
            buffer.Add(this.lightWhite);
            buffer.Add(this.lightRed);
            buffer.Add(this.sound);
            buffer.Add(this.trainDetection);
            buffer.Add(this.barrier1);
            buffer.Add(this.barrier2);
            buffer.Add(this.barrier3);
            buffer.Add(this.barrier4);
        }
    }
}
