using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public abstract class PointsMotor
    {
        public abstract byte motorTypeId { get; }

        public abstract List<byte> Serialize();
    }
}
