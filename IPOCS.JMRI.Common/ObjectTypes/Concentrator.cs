using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class Concentrator
    {
        public ushort UnitID { get; set; }

        public string Name { get; set; }

        public List<BasicObject> Objects { get; set; } = new List<BasicObject>();

        public List<byte> Serialize()
        {
            var vector = new List<byte>();

            foreach (var basicObject in this.Objects)
            {
                var objectVector = basicObject.Serialize();
                vector.AddRange(objectVector);
            }

            return vector;
        }
    }
}
