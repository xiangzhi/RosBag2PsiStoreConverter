using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class RosMessage
    {
        private RosMessageDefinition messageType;

        public RosMessage(RosMessageDefinition type, Dictionary<string, byte[]> headerField, byte[] data)
            : this(type, RosTime.FromRosBytes(headerField["time"]), BitConverter.ToInt32(headerField["conn"], 0), data) 
        {
        }

        public RosMessage(RosMessageDefinition type, RosTime time, int conn, byte[] data)
        {
            this.messageType = type;
            this.Time = time;
            this.Conn = conn;
            this.RawData = data;
        }


        public RosMessageDefinition MessageType => messageType;

        public RosTime Time { get; private set; }
        public int Conn { get; private set; }
        public byte[] RawData { get; private set; }

        private Tuple<int, dynamic> ParseBuildInTypes(byte[] rawData, string type, int offset = 0)
        {
            switch (type)
            {
                case "string":
                    int len = BitConverter.ToInt32(rawData, offset);
                    return new Tuple<int, dynamic>(len+4, Encoding.UTF8.GetString(rawData.Skip(offset + 4).Take(len).ToArray()));
                case "bool":
                    return new Tuple<int, dynamic>(1, BitConverter.ToBoolean(rawData, offset));
                case "int8":
                    return new Tuple<int, dynamic>(1, rawData[0]);
                case "uint8":
                    return new Tuple<int, dynamic>(1, rawData[0]);
                case "int16":
                    return new Tuple<int, dynamic>(2, BitConverter.ToInt16(rawData, offset));
                case "uint16":
                    return new Tuple<int, dynamic>(2, BitConverter.ToUInt16(rawData, offset));
                case "int32":
                    return new Tuple<int, dynamic>(4, BitConverter.ToInt32(rawData, offset));
                case "uint32":
                    return new Tuple<int, dynamic>(4, BitConverter.ToUInt32(rawData, offset));
                case "int64":
                    return new Tuple<int, dynamic>(8, BitConverter.ToInt64(rawData, offset));
                case "uint64":
                    return new Tuple<int, dynamic>(8, BitConverter.ToUInt64(rawData, offset));
                case "float32":
                    return new Tuple<int, dynamic>(4, BitConverter.ToSingle(rawData, offset));
                case "float64":
                    return new Tuple<int, dynamic>(8, BitConverter.ToDouble(rawData, offset));
                case "time":
                    return new Tuple<int, dynamic>(8, RosTime.FromRosBytes(rawData, offset));
                case "duration":
                    return new Tuple<int, dynamic>(8, RosDuration.FromRosBytes(rawData, offset));
            }
            return null;
        }

        private Tuple<int, dynamic> ParseSingleFieldType(byte[] rawData, string type, int offset = 0)
        {
            if (RosMessageDefinition.IsBuiltInType(type))
            {
                return this.ParseBuildInTypes(rawData, type, offset);
            }
            // This is not a standard type
            // TODO Figure out a way to handle nested types
            return null;
            
        }

        public RosMessage GetFieldAsRosMessage(RosMessageDefinition def, string indexString)
        {
            // get the data and offset
            var fieldData = this.messageType.GetData(this.RawData, indexString);

            return new RosMessage(def, this.Time, this.Conn, fieldData.Item2);
        }


        public byte[] GetRawField(string indexString)
        {
            var fieldData = this.messageType.GetData(this.RawData, indexString);
            return fieldData.Item2;
        }

        public T[] ParseArrayField<T>(byte[] data, int arrSize, string type, int offset)
        {
            var arr = new T[arrSize];
            for (var i = 0; i < arrSize; i++)
            {
                var parseResult = this.ParseSingleFieldType(data, type, offset: offset);
                offset += parseResult.Item1;
                arr[i] = parseResult.Item2;
            }
            return arr;
        }

        public dynamic GetField(string indexString)
        {

            var fieldData = this.messageType.GetData(this.RawData, indexString);
            if (fieldData.Item1.Contains("["))
            {
                var arrayType = fieldData.Item1.Substring(0, fieldData.Item1.IndexOf('['));
                var arrSize = 0;
                var offset = 0;
                if (fieldData.Item1.EndsWith("[]"))
                {
                    // this is variable length array
                    arrSize = BitConverter.ToInt32(fieldData.Item2, offset);
                    offset += 4;
                }
                else if (fieldData.Item1.EndsWith("]"))
                {
                    // this is a fixed length array
                    arrSize = Int32.Parse(fieldData.Item1.Substring(fieldData.Item1.IndexOf('[') + 1, fieldData.Item1.IndexOf(']') - fieldData.Item1.IndexOf('[') - 1));
                }


                switch (arrayType)
                {
                    case "string": return this.ParseArrayField<string>(fieldData.Item2, arrSize, arrayType, offset);
                    case "bool": return this.ParseArrayField<bool>(fieldData.Item2, arrSize, arrayType, offset);
                    case "int8": return this.ParseArrayField<sbyte>(fieldData.Item2, arrSize, arrayType, offset);
                    case "uint8": return this.ParseArrayField<byte>(fieldData.Item2, arrSize, arrayType, offset);
                    case "int16": return this.ParseArrayField<short>(fieldData.Item2, arrSize, arrayType, offset);
                    case "uint16": return this.ParseArrayField<ushort>(fieldData.Item2, arrSize, arrayType, offset);
                    case "int32": return this.ParseArrayField<int>(fieldData.Item2, arrSize, arrayType, offset);
                    case "uint32": return this.ParseArrayField<uint>(fieldData.Item2, arrSize, arrayType, offset);
                    case "int64": return this.ParseArrayField<long>(fieldData.Item2, arrSize, arrayType, offset);
                    case "uint64": return this.ParseArrayField<ulong>(fieldData.Item2, arrSize, arrayType, offset);
                    case "float32": return this.ParseArrayField<float>(fieldData.Item2, arrSize, arrayType, offset);
                    case "float64": return this.ParseArrayField<double>(fieldData.Item2, arrSize, arrayType, offset);
                    case "time": return this.ParseArrayField<RosTime>(fieldData.Item2, arrSize, arrayType, offset);
                    case "duration": return this.ParseArrayField<RosDuration>(fieldData.Item2, arrSize, arrayType, offset);
                    default:
                        throw new NotSupportedException($"Unknown Type {arrayType}");

                }
            }
            else
            {
                // basic type
                return this.ParseSingleFieldType(fieldData.Item2, fieldData.Item1).Item2;

            }
        }

        public string GetPropertyType(string indexString)
        {
            return messageType.GetFieldType(indexString);
        }
    }
}
