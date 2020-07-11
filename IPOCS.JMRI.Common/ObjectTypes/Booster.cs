using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class Booster : BasicObject
    {
        public override byte objectTypeId { get { return 5; } }

        public byte numBoosters { get; set; }

        protected override void Serialize(List<byte> buffer)
        {
            buffer.Add(this.numBoosters);
        }
    }
}
