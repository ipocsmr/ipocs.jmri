using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class GenericInput : BasicObject
    {
        public override byte objectTypeId { get { return 11; } }

        public byte inputPin { get; set; }

        public byte debounceTime { get; set; }

        public byte releaseHoldTime { get; set; }

        protected override void Serialize(List<byte> buffer)
        {
            buffer.Add(this.inputPin);
            buffer.Add(this.debounceTime);
            buffer.Add(this.releaseHoldTime);
        }
    }
}
