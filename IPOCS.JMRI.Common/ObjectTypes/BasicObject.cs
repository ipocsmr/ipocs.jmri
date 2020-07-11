using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public abstract class BasicObject
    {
        public string Name { get; set; }

        public string SystemName { get; set; }

        public abstract byte objectTypeId { get; }

        public virtual IList<Type> SupportedOrders
        {
            get
            {
                var list = new List<Type>();
                list.Add(typeof(IPOCS.Protocol.Packets.Orders.RequestStatus));
                return list;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public List<byte> Serialize()
        {
            var buffer = new List<byte>();
            buffer.Add(this.objectTypeId);
            int lengthPos = buffer.Count;
            buffer.Add(0); // Length;
            byte[] toBytes = Encoding.ASCII.GetBytes(this.Name);
            buffer.AddRange(toBytes);
            buffer.Add(0);
            this.Serialize(buffer);
            buffer[lengthPos] = (byte)(buffer.Count - lengthPos);
            return buffer;
        }

        protected abstract void Serialize(List<byte> buffer);
    }
}
