using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class Points : BasicObject
    {
        public override byte objectTypeId { get { return 1; } }

        public byte frogOutput { get; set; }

        public List<PointsMotor> Motors { get; } = new List<PointsMotor>();

        public override IList<Type> SupportedOrders
        {
            get
            {
                var list = base.SupportedOrders;
                list.Add(typeof(IPOCS.Protocol.Packets.Orders.ThrowPoints));
                return list;
            }
        }

        protected override void Serialize(List<byte> buffer)
        {
            buffer.Add(this.frogOutput);
            foreach (var motor in Motors)
            {
                var motorVector = motor.Serialize();
                buffer.AddRange(motorVector);
            }
        }
    }
}
