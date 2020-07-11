using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class PointsMotor_Servo : PointsMotor
    {
        public override byte motorTypeId { get { return 2; } }
        public byte servoPin { get; set; }
        public byte positionPin { get; set; }
        public bool reverseStatus { get; set; }

        public override List<byte> Serialize()
        {
            var vector = new List<byte>();
            vector.Add(motorTypeId);
            vector.Add(this.servoPin);
            vector.Add(positionPin);
            vector.Add((byte)(reverseStatus ? 1 : 0));
            return vector;
        }

    }
}
