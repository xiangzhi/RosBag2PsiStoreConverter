using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class RosHeader
    {
        public RosHeader(uint seq, RosTime time, string frameId, int headerSize)
        {
            this.Seq = seq;
            this.Time = time;
            this.FrameId = frameId;
            this.HeaderByteSize = headerSize;
        }

        public static RosHeader FromRosBytes(byte[] headerBytes)
        {
            return RosHeader.FromRosBytes(headerBytes, 0);
        }

        public static RosHeader FromRosBytes(byte[] headerBytes, int offset = 0)
        {
            // calculate the size of the frame string
            var frameStringLength = BitConverter.ToInt32(headerBytes, offset + 12);
            return new RosHeader(BitConverter.ToUInt32(headerBytes, offset), RosTime.FromRosBytes(headerBytes, offset + 4), Encoding.UTF8.GetString(headerBytes, offset + 16, frameStringLength), frameStringLength + 16);
        }


        public RosTime Time { get; private set; }
        public uint Seq { get; private set; }
        public string FrameId { get; private set; }
        public int HeaderByteSize { get; private set; }
    }
}
