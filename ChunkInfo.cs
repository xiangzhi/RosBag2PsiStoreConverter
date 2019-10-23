using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class ChunkInfo
    {
        public ChunkInfo(Dictionary<string, byte[]> fields, byte[] data)
        {

            StartTime = BitConverter.ToInt64(fields["start_time"], 0);
            EndTime = BitConverter.ToInt64(fields["end_time"], 0);
            ChunkPos = BitConverter.ToInt64(fields["chunk_pos"], 0);
            Count = BitConverter.ToInt32(fields["count"], 0);

            for (var i = 0; i < Count; i++)
            {
                var conn = BitConverter.ToInt32(data, (i * 8));
                var count = BitConverter.ToInt32(data, (i * 8) + 4);
                MessageCount.Add(conn, count);
            }
        }

        public long ChunkPos { get; private set; }
        public long StartTime { get; private set; }
        public long EndTime { get; private set; }
        public int  Count { get; private set; }

        public Dictionary<int, int> MessageCount { get; private set; } = new Dictionary<int, int>();
    }
}
