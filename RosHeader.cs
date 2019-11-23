using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class RosHeader
    {


        public RosHeader(uint seq, RosTime time, string frameId, uint headerSize)
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

        public static RosHeader FromRosBytes(byte[] headerBytes, int offset)
        {
            return new RosHeader(BitConverter.ToUInt32(headerBytes, 0), RosTime.FromRosBytes(headerBytes, 4), Encoding.UTF8.GetString(headerBytes, 12, headerBytes.Length - 12), (uint)headerBytes.Length);
        }


        public RosTime Time { get; private set; }
        public uint Seq { get; private set; }
        public string FrameId { get; private set; }
        public uint HeaderByteSize { get; private set; }
    }
}
