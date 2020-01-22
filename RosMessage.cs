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
        private Dictionary<string, dynamic> fields;

        public RosMessage(RosMessageDefinition type, Dictionary<string, byte[]> headerField, byte[] data)
            : this(type, RosTime.FromRosBytes(headerField["time"]), BitConverter.ToInt32(headerField["conn"], 0), data) 
        {
        }

        public RosMessage(RosMessageDefinition type, RosTime time, int conn, byte[] data)
        {
            this.messageType = type;
            this.Time = time;
            this.Conn = conn;

            // parse message
            var parseResult = type.ParseMessage(data);
            this.fields = parseResult.Item2;
        }

        public RosMessage(RosMessageDefinition type, RosTime time, int conn, Dictionary<string, dynamic> fields)
        {
            this.messageType = type;
            this.Time = time;
            this.Conn = conn;
            this.fields = fields;
        }
    
        public RosMessageDefinition MessageType => messageType;
        public RosTime Time { get; private set; }
        public int Conn { get; private set; }
        public byte[] RawData { get; private set; }
        
        public RosMessage GetFieldAsRosMessage(string indexString)
        {
            var msgDef = this.messageType.KnownDefinitions[this.messageType.GetFieldType(indexString)];
            return this.GetFieldAsRosMessage(msgDef, indexString);
        }


        public RosMessage GetFieldAsRosMessage(RosMessageDefinition def, string indexString)
        {
            return new RosMessage(def, this.Time, this.Conn, this.fields[indexString]);
        }

        public dynamic GetField(string indexString)
        {
            // Make sure the field exist
            if (this.fields.ContainsKey(indexString)){
                return this.fields[indexString];
            }
            else
            {
                throw new Exception("Attempt to get not existent field in the Message");
            }
        }

        public string GetPropertyType(string indexString)
        {
            return messageType.GetFieldType(indexString);
        }
    }
}