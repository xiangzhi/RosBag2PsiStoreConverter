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
        
        /// <summary>
        /// The time the message was received
        /// </summary>
        /// <value></value>
        public RosTime Time { get; private set; }
        public int Conn { get; private set; }

        /// <summary>
        /// List of Fields in this message.
        /// </summary>
        public List<string> FieldList => messageType.FieldList;
        

        /// <summary>
        /// Return the data of the field as a RosMessage object. This is useful in cases
        /// where the field is another custom message type.
        /// </summary>
        /// <param name="indexString">Name of the field</param>
        /// <returns>The data in the field as its own RosMessage</returns>
        public RosMessage GetFieldAsRosMessage(string indexString)
        {
            var msgDef = this.messageType.KnownMsgDefinitions[this.messageType.GetFieldType(indexString)];
            return this.GetFieldAsRosMessage(msgDef, indexString);
        }


        /// <summary>
        /// Return the data of the field as a RosMessage object. This is useful in cases
        /// where the field is another custom message type.
        /// </summary>
        /// <param name="def">Definition of the Field</param>
        /// <param name="indexString">Name of the Field</param>
        /// <returns>The data in the field as its own RosMessage</returns>
        public RosMessage GetFieldAsRosMessage(RosMessageDefinition def, string indexString)
        {
            return new RosMessage(def, this.Time, this.Conn, this.fields[indexString]);
        }


        /// <summary>
        /// Return the data of the field as its casted type.
        /// </summary>
        /// <param name="indexString">Name of the field</param>
        /// <returns>converted data type</returns>
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
    }
}