using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class RosDuration
    {
        public RosDuration(Int32 secs, Int32 nSecs)
        {
            Seconds = secs;
            NanoSeconds = nSecs;
        }

        public static RosDuration FromRosBytes(byte[] timeBytes)
        {
            return RosDuration.FromRosBytes(timeBytes, 0);
        }

        public static RosDuration FromRosBytes(byte[] timeBytes, int offset)
        {
            return new RosDuration(BitConverter.ToInt32(timeBytes, offset), BitConverter.ToInt32(timeBytes, offset + 4));
        }

        public TimeSpan ToTimeSpan()
        {
            return TimeSpan.FromSeconds(Seconds) + TimeSpan.FromTicks(NanoSeconds / 100);
        }

        public Int32 Seconds { get; private set; }

        public Int32 NanoSeconds { get; private set; }
    }
}
