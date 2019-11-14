using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class RosBag
    {
        //private byte[] bagData;
        private FileStream bagFileStream = null;
        private Dictionary<int, Connection> BagConnections = new Dictionary<int, Connection>();
        private List<ChunkInfo> chunkInfoList = new List<ChunkInfo>();
        public Dictionary<string, RosMessageDefinition> KnownRosMessageDefinitions = new Dictionary<string, RosMessageDefinition>();

        public RosBag(string bagPath)
        {
            LoadFile(bagPath);
        }

        private void validateRosBag()
        {
            byte[] headerBytes = new byte[13];
            // get the bytes for the header
            bagFileStream.Read(headerBytes, 0, 13);

            // get the version of the ROS BAG
            string rosbag_version = Encoding.UTF8.GetString(headerBytes);
            if (rosbag_version.Trim() != "#ROSBAG V2.0")
            {
                throw new NotImplementedException(String.Format("Unable to Handle ROSBAG Version {0}", rosbag_version));
            }
        }

        private Tuple<Dictionary<string, byte[]>, byte[]> ReadNextRecord(bool skipChunk = false, int offset = 0)
        {

            byte[] intBytes = new byte[4];
            // read the header len
            bagFileStream.Read(intBytes, offset, 4);
            var recordHeaderLen = BitConverter.ToInt32(intBytes, 0);

            // now create bit array for the header
            byte[] headerDataBytes = new byte[recordHeaderLen];
            bagFileStream.Read(headerDataBytes, 0, recordHeaderLen);
            // parse the header
            var recordFieldProperties = Utils.ParseHeaderData(headerDataBytes);

            byte[] dataBytes = null;
            // now we read the datalen
            bagFileStream.Read(intBytes, 0, 4);
            var recordDataLen = BitConverter.ToInt32(intBytes, 0);
            if (skipChunk && recordFieldProperties["op"][0] == (byte)0x05)
            {
                // still advance the read 
                bagFileStream.Seek((long)recordDataLen, SeekOrigin.Current);
            }
            else
            {
                // now we split out the record data
                dataBytes = new byte[recordDataLen];
                bagFileStream.Read(dataBytes, 0, recordDataLen);
            }
            
            return Tuple.Create(recordFieldProperties, dataBytes);
        }

        private long readBagHeader()
        {
            // first read the actual header
            var record = ReadNextRecord();

            // parse what we know
            ConnCount = BitConverter.ToInt32(record.Item1["conn_count"], 0);
            ChunkCount = BitConverter.ToInt32(record.Item1["chunk_count"], 0);

            // make sure the position is at the next starting point

            return BitConverter.ToInt64(record.Item1["index_pos"], 0);
        }

        private void LoadFile(string bagPath)
        {
            // open the file stream
            bagFileStream = new FileStream(bagPath, FileMode.Open, FileAccess.Read);

            // validate it's a rosbag file
            validateRosBag();

            // Note the offset is relative to the beginning of the file and not from the header.
            long firstNonChunkRecordOffset = readBagHeader();

            // now jump to the connection data part to get information about the topcis
            bagFileStream.Seek(firstNonChunkRecordOffset, SeekOrigin.Begin);
            while (bagFileStream.Position < bagFileStream.Length)
            {
                var readPosition = bagFileStream.Position;
                // read the next records
                var record = ReadNextRecord(skipChunk: true);

                if (record.Item1["op"][0] == (byte)0x07)
                {
                    // This is connection record
                    var connId = BitConverter.ToInt32(record.Item1["conn"], 0);
                    BagConnections.Add(connId, new Connection(record.Item1, record.Item2, KnownRosMessageDefinitions));
                }
                else if (record.Item1["op"][0] == (byte)0x06)
                {
                    chunkInfoList.Add(new ChunkInfo(record.Item1, record.Item2));
                }
                else
                {
                    throw new InvalidDataException($"Unknown OP: {record.Item1["op"][0]}");
                }
            }
        }

        public IEnumerable<RosMessage> ReadTopic(string name)
        {
            return ReadTopic(new List<string> { name });
        }

        public RosMessageDefinition GetMessageDefinition(string name)
        {
            // first figure out which connection we want
            foreach (var conn in BagConnections)
            {
                if (conn.Value.Topic == name)
                {
                    return conn.Value.MessageDefinition;
                }
            }
            throw new InvalidDataException($"Unknown Topic Name {name}");
            //return null;
        }

        public IEnumerable<RosMessage> ReadTopic(List<string> topicNames)
        {
            // first figure out which chunk has the information
            List<int> validChunks = new List<int>();
            List<int> savedConnections = new List<int>();

            // List of Message:
            var msgList = new List<RosMessage>();

            // first figure out which connection we want
            foreach (var conn in BagConnections)
            {
                if (topicNames.Contains(conn.Value.Topic))
                {
                    savedConnections.Add(conn.Key);
                }
            }

            //now we figure out which chunk we want
            for(var i = 0; i < chunkInfoList.Count(); i++)
            {
                if(chunkInfoList[i].MessageCount.Keys.Any(m => savedConnections.Contains(m)))
                {
                    validChunks.Add(i);
                }
            }

            // now we parse the chunk to find messages
            foreach (var chunkListId in validChunks)
            {
                var chunkInfo = chunkInfoList[chunkListId];
                // change the read position
                bagFileStream.Seek(chunkInfo.ChunkPos, SeekOrigin.Begin);
                // get the record

                var chunkRecord = ReadNextRecord();

                if (chunkRecord.Item1["op"][0] == (byte)0x02)
                {
                    throw new Exception("Not Chunk");
                }

               var connIndexList = new Dictionary<int, dynamic>();

                for (var j = 0; j < chunkInfo.MessageCount.Count; j++)
                {
                    var record = ReadNextRecord();
                    if (savedConnections.Contains(BitConverter.ToInt32(record.Item1["conn"], 0))){
                        // this is the one we want
                        var count = BitConverter.ToInt32(record.Item1["count"], 0);
                        for(var k = 0; k < count; k++)
                        {
                            var time = BitConverter.ToInt64(record.Item2, (k * 12));
                            var offset = BitConverter.ToInt32(record.Item2, (k * 12) + 8);

                            // header len
                            var headerLen = BitConverter.ToInt32(chunkRecord.Item2, offset);
                            var headerField = Utils.ParseHeaderData(chunkRecord.Item2.Skip(offset + 4).Take(headerLen).ToArray());
                            // data len
                            var dataLen = BitConverter.ToInt32(chunkRecord.Item2, offset + 4 + headerLen);
                            var data = chunkRecord.Item2.Skip(offset + 4 + headerLen + 4).Take(dataLen).ToArray();

                            var msgDef = BagConnections[BitConverter.ToInt32(headerField["conn"], 0)].MessageDefinition;

                            // The problem with making it lazy is that the reading operation must be contain in this state machine.
                            // The current implementation relies on the file read header to remain on the same between calls.
                            // yield return new RosMessage(msgDef, headerField, data);
                            msgList.Add(new RosMessage(msgDef, headerField, data));
                        }
                    }
                }
            }
            return msgList;
        }

        public int ConnCount { get; private set; }
        public int ChunkCount { get; private set; }

        public List<string> TopicList
        {
            get
            {
                return BagConnections.Values.Select(m => m.Topic).Distinct().ToList();
            }
        }

        public List<int> ConnectionList
        {
            get
            {
                return BagConnections.Keys.ToList();
            }
        }

    }
}
