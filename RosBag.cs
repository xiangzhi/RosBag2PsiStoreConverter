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
        private List<FileStream> bagFileStreams = new List<FileStream>();
        private Dictionary<int, int> ConnCounts = new Dictionary<int, int>();
        private Dictionary<int, int> ChunkCounts = new Dictionary<int, int>();
        private Dictionary<int, Dictionary<int, Connection>> EachBagConnections = new Dictionary<int, Dictionary<int, Connection>>();
        private Dictionary<int, List<ChunkInfo>> EachChunkInfoList = new Dictionary<int, List<ChunkInfo>>();
        private string rosbagVersion = null;
        public Dictionary<string, RosMessageDefinition> KnownRosMessageDefinitions = new Dictionary<string, RosMessageDefinition>();
        private object mutex;

        public RosBag(string bagPath)
            : this()
        {
            if (File.Exists(bagPath))
            {
                // This is a single file
                LoadRosBagInfo(new List<string>() { bagPath });
            }
            else if (Directory.Exists(bagPath))
            {
                var bagFiles = Directory.GetFiles(bagPath, "*.bag");
                if (bagFiles.Length <= 0) throw new InvalidDataException($"No Bag Files found in {bagPath}");
                LoadRosBagInfo(bagFiles.ToList());
            }
        }

        public RosBag(List<string> bagPaths)
            : this()
        {
            if (bagPaths.Count == 1 && Directory.Exists(bagPaths[0]))
            {
                var bagFiles = Directory.GetFiles(bagPaths[0], "*.bag");
                if (bagFiles.Length <= 0) throw new InvalidDataException($"No Bag Files found in {bagPaths[0]}");
                LoadRosBagInfo(bagFiles.ToList());
            }
            else
            {
                LoadRosBagInfo(bagPaths);
            }
        }

        public RosBag()
        {
            this.mutex = new object();
        }

        private void validateRosBag(FileStream fs)
        {
            byte[] headerBytes = new byte[13];
            // get the first 13 bytes and see whether it's the header
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(headerBytes, 0, 13);
            // get the version of the ROS BAG
            this.rosbagVersion = Encoding.UTF8.GetString(headerBytes);
            if (this.rosbagVersion.Trim() != "#ROSBAG V2.0")
            {
                throw new NotImplementedException(String.Format("Unable to Handle ROSBAG Version {0}", this.rosbagVersion));
            }
        }

        /// <summary>
        /// Read the next record and return the offset of the beginning of the data and also datalen
        /// </summary>
        /// <param name="bagStream">Bag filestream to read</param>
        /// <param name="offset">Where the record is in the stream</param>
        /// <returns>A tuple of the header, offset to the data and length of the data</returns>
        private Tuple<Dictionary<string, byte[]>, long, int> ReadNextRecord(FileStream bagStream, long offset)
        {
            lock (mutex)
            {
                // reinitialize the filestream header from the beginning to the header.
                bagStream.Seek(offset, SeekOrigin.Begin);

                // read the headerlen
                byte[] intBytes = new byte[4];
                bagStream.Read(intBytes, 0, 4);
                var recordHeaderLen = BitConverter.ToInt32(intBytes, 0);

                // now create bit array for the header
                byte[] headerDataBytes = new byte[recordHeaderLen];
                bagStream.Read(headerDataBytes, 0, recordHeaderLen);
                // parse the header
                var recordFieldProperties = Utils.ParseHeaderData(headerDataBytes);

                // now we read the datalen
                bagStream.Read(intBytes, 0, 4);
                var recordDataLen = BitConverter.ToInt32(intBytes, 0);

                // Return all the information
                return Tuple.Create(recordFieldProperties, bagStream.Position, recordDataLen);
            }
        }

        private long readSingleRosBagHeader(int streamIndex, long offset)
        {
            (var header, long dataOffset, int dataLen) = this.ReadNextRecord(this.bagFileStreams[streamIndex], offset);
            // store the header information about the rosbag
            ConnCounts[streamIndex] = BitConverter.ToInt32(header["conn_count"], 0);
            ChunkCounts[streamIndex] = BitConverter.ToInt32(header["chunk_count"], 0);

            return BitConverter.ToInt64(header["index_pos"], 0);
        }

        private void LoadRosBagInfo(List<string> bagPaths)
        {
            // Sort the paths to ensure the timing information 
            // is in order
            bagPaths.Sort();

            // add each path
            foreach(var path in bagPaths)
            {
                // open the file stream
                var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                // validate the filestream is a rosbag and we can figure out the version
                this.validateRosBag(fileStream);
                // add file stream to the list
                this.bagFileStreams.Add(fileStream);
                // get the index
                var bagIndex = this.bagFileStreams.Count - 1;
                // Read the ros bag header information (3.1 in the rosbag format)
                long nextRecordOffset = readSingleRosBagHeader(bagIndex, 13);

                // Get the connection data and chunk information for the rosbag
                while (nextRecordOffset < this.bagFileStreams[bagIndex].Length)
                {
                    // read the next record
                    (var headers, var dataOffset, var dataLen) = this.ReadNextRecord(this.bagFileStreams[bagIndex], nextRecordOffset);

                    // check the types 
                    if (headers["op"][0] == (byte)0x05)
                    {
                        // This is a chunk record, ignore.
                    }
                    if (headers["op"][0] == (byte)0x07)
                    {
                        // This is connection record
                        var connId = BitConverter.ToInt32(headers["conn"], 0);
                        // get the data 
                        this.bagFileStreams[bagIndex].Seek(dataOffset, SeekOrigin.Begin);
                        var dataBytes = new byte[dataLen];
                        this.bagFileStreams[bagIndex].Read(dataBytes, 0, dataLen);
                        if (!this.EachBagConnections.ContainsKey(bagIndex))
                        {
                            this.EachBagConnections[bagIndex] = new Dictionary<int, Connection>();
                        }
                        this.EachBagConnections[bagIndex].Add(connId, new Connection(headers, dataBytes, KnownRosMessageDefinitions));
                    }
                    else if (headers["op"][0] == (byte)0x06)
                    {
                        // get the data 
                        this.bagFileStreams[bagIndex].Seek(dataOffset, SeekOrigin.Begin);
                        var dataBytes = new byte[dataLen];
                        this.bagFileStreams[bagIndex].Read(dataBytes, 0, dataLen);
                        if (!this.EachChunkInfoList.ContainsKey(bagIndex))
                        {
                            this.EachChunkInfoList[bagIndex] = new List<ChunkInfo>();
                        }
                        this.EachChunkInfoList[bagIndex].Add(new ChunkInfo(headers, dataBytes));
                    }
                    else
                    {
                        throw new InvalidDataException($"Unknown OP: {headers["op"][0]}");
                    }
                    // progress to next record
                    nextRecordOffset = dataOffset + dataLen;
                }
            }

            // Pre-calculate offsets
            foreach(var msgDef in this.KnownRosMessageDefinitions){
                msgDef.Value.PreCalculateOffsets();
            }
        }


        public IEnumerable<RosMessage> ReadTopic(string name)
        {
            return ReadTopic(new List<string> { name });
        }

        public RosMessageDefinition GetMessageDefinition(string name)
        {
            // TODO rewrite the beginning to get this
            // first figure out which connection we want
            foreach( var bagConn in EachBagConnections.Values)
            {
                foreach (var conn in bagConn)
                {
                    if (conn.Value.Topic == name)
                    {
                        return conn.Value.MessageDefinition;
                    }
                }
            }
            throw new InvalidDataException($"Unknown Topic Name {name}");
        }

        public IEnumerable<RosMessage> ReadTopic(List<string> topicNames)
        {

            byte[] indexDataBytes = new byte[12];
            byte[] intBytes = new byte[4];

            RosMessageDefinition msgDef = null;
            Dictionary<string, byte[]> headerField;
            byte[] data = null;

            List<RosMessage> msgList = new List<RosMessage>();

            // We assume the file streams are all sorted
            for (var bi = 0; bi < this.bagFileStreams.Count; bi++)
            {
                var fs = this.bagFileStreams[bi];

                // first figure out which chunk has the information
                List<int> savedConnections = new List<int>();


                // first figure out which connection we want
                foreach (var conn in this.EachBagConnections[bi])
                {
                    if (topicNames.Contains(conn.Value.Topic))
                    {
                        savedConnections.Add(conn.Key);
                    }
                }

                //now we figure out which chunk we want
                for (var i = 0; i < this.EachChunkInfoList[bi].Count(); i++)
                {
                    var chunkInfo = this.EachChunkInfoList[bi][i];
                    foreach(var connId in savedConnections)
                    {
                        if (chunkInfo.MessageCount.ContainsKey(connId))
                        {
                            // read the chunk data
                            (var header, var chuckDataOffset, var chunkDataLen) = this.ReadNextRecord(fs, chunkInfo.ChunkPos);
                            // check to make sure its a chunk
                            if (header["op"][0] != (byte)0x05) throw new Exception($"Except to see chunk(0x05) but got {header["op"][0]}");

                            // read the next records which are index records for all messages in the chunk
                            long indexDataOffset = -1;
                            int indexDataLen = -1;
                            int msgCount = -1;
                            Dictionary<string, byte[]> indexHeader;
                            long nextRecordPos = chuckDataOffset + chunkDataLen;
                            while (true)
                            {
                                (indexHeader, indexDataOffset, indexDataLen) = this.ReadNextRecord(fs, nextRecordPos);
                                if (indexHeader["op"][0] != (byte)0x04) throw new Exception($"Except to see Index data Record (0x04) but got {indexHeader["op"][0]}");
                                msgCount = BitConverter.ToInt32(indexHeader["count"], 0);
                                // if this index header belongs to the one we are working on break out
                                if (savedConnections.Contains(BitConverter.ToInt32(indexHeader["conn"], 0))) break;
                                nextRecordPos = indexDataOffset + indexDataLen;
                            }

                            // loop through and read each message
                            long messageOffset = chuckDataOffset;
                            for(var msgIndex = 0; msgIndex < msgCount; msgIndex++)
                            {
                                lock (this.mutex)
                                {
                                    // seek to the correct part
                                    fs.Seek(indexDataOffset + msgIndex * 12, SeekOrigin.Begin);

                                    // from the index, read the time and offset into chunk
                                    fs.Read(indexDataBytes, 0, 12);
                                    var messageTime = RosTime.FromRosBytes(indexDataBytes, 0);
                                    var offset = BitConverter.ToInt32(indexDataBytes, 8);

                                    // now we can look into the chunk and read data
                                    fs.Seek(chuckDataOffset + offset, SeekOrigin.Begin);
                                    // read header
                                    fs.Read(intBytes, 0, 4);
                                    var headerLen = BitConverter.ToInt32(intBytes, 0);
                                    byte[] headerBytes = new byte[headerLen];
                                    fs.Read(headerBytes, 0, headerLen);
                                    headerField = Utils.ParseHeaderData(headerBytes);
                                    // read data
                                    fs.Read(intBytes, 0, 4);
                                    var dataLen = BitConverter.ToInt32(intBytes, 0);
                                    // read data
                                    data = new byte[dataLen];
                                    fs.Read(data, 0, dataLen);
                                    msgDef = EachBagConnections[bi][BitConverter.ToInt32(headerField["conn"], 0)].MessageDefinition;
                                }
                                yield return new RosMessage(msgDef, headerField, data);
                                // msgList.Add(new RosMessage(msgDef, headerField, data));
                            }
                        }
                    }
                }
            }
            //return msgList;
        }

        public List<string> TopicList
        {
            get
            {
                return EachBagConnections.Values.Select(m => m.Values.Select(x => x.Topic)).SelectMany(x => x).Distinct().ToList();
            }
        }

        public List<Tuple<string, string>> TopicTypeList
        {
            get
            {
                return EachBagConnections.Values.Select(m => m.Values.Select(x => new Tuple<string,string>(x.Topic, x.Type))).SelectMany(x => x).Distinct().ToList();
            }
        }


        /// <summary>
        /// A dictoionary of all topics and how many messages in each topic
        /// </summary>
/*        public Dictionary<string, int> MessageCounts
        {
            get
            {
                // get a list of topics
                var topics = this.TopicList;
                // loop through them to find
                foreach(var )


                return EachChunkInfoList.Values.Select(m => m.Select(x => x.MessageCount)).SelectMany(x => x).OrderBy(x => x).First().ToDateTime();

                return EachBagConnections.Values.Select(m => m.Values.Select(x => x.Topic)).SelectMany(x => x);
            }
        }*/

        /// <summary>
        /// The earliest timestamp of all message in the RosBag(s)
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return EachChunkInfoList.Values.Select(m => m.Select(x => x.StartTime)).SelectMany(x => x).OrderBy(x => x).First().ToDateTime();
            }
        }


        /// <summary>
        /// The latest timestamp of all message in the RosBag(s)
        /// </summary>
        public DateTime EndTime
        {
            get
            {
                return EachChunkInfoList.Values.Select(m => m.Select(x => x.EndTime)).SelectMany(x => x).OrderByDescending(x => x).First().ToDateTime();
            }
        }

    }
}
