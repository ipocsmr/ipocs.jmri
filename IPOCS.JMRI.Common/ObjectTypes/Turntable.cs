using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS_Programmer.ObjectTypes
{
    public class Turntable : BasicObject
    {
        public override byte objectTypeId { get { return 40; } }

        public byte numPositions { get; set; }
        public byte resetPositionInput { get; set; }
        public byte trackDetectionInput { get; set; }
        public byte clockwiseOutput { get; set; }
        public byte counterClockwiseOutput { get; set; }

        protected override void Serialize(List<byte> buffer)
        {
            buffer.Add(this.numPositions);
            buffer.Add(this.resetPositionInput);
            buffer.Add(this.trackDetectionInput);
            buffer.Add(this.clockwiseOutput);
            buffer.Add(this.counterClockwiseOutput);
        }
    }
}
