using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class GenericOutput: BasicObject
    {
        public override byte objectTypeId { get { return 10; } }

        public byte outputPin { get; set; }

        public override IList<Type> SupportedOrders
        {
            get
            {
                var list = base.SupportedOrders;
                list.Add(typeof(IPOCS.Protocol.Packets.Orders.SetOutput));
                return list;
            }
        }

        protected override void Serialize(List<byte> buffer)
        {
            buffer.Add(this.outputPin);
        }
    }
}
