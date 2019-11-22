using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class RosTime : IComparable
    {
        public RosTime(UInt32 secs, UInt32 nSecs)
        {
            Seconds = secs;
            NanoSeconds = nSecs;
        }

        public static RosTime FromRosBytes(byte[] timeBytes)
        {
            return RosTime.FromRosBytes(timeBytes, 0);
        }

        public static RosTime FromRosBytes(byte[] timeBytes, int offset)
        {
            return new RosTime(BitConverter.ToUInt32(timeBytes, offset), BitConverter.ToUInt32(timeBytes, offset + 4));
        }

        public DateTime ToDateTime()
        {
            // Each tick is about 100 Nano Seconds
            return DateTimeOffset.FromUnixTimeSeconds(Seconds).DateTime + TimeSpan.FromTicks(NanoSeconds / 100);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            RosTime otherTime = obj as RosTime;
            if (otherTime.Seconds == this.Seconds)
            {
                return (int)this.NanoSeconds - (int)otherTime.NanoSeconds;
            }
            else
            {
                return (int)this.Seconds - (int)otherTime.Seconds;
            }
        }

        public UInt32 Seconds { get; private set; }

        public UInt32 NanoSeconds { get; private set; }
    }
}
