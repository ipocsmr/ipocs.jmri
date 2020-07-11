using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class SemaphoreWing : BasicObject
    {
        public override byte objectTypeId { get { return 30; } }

        public byte output { get; set; }
        public byte speed { get; set; }

        protected override void Serialize(List<byte> buffer)
        {
            buffer.Add(this.output);
            buffer.Add(this.speed);
        }
    }
}
