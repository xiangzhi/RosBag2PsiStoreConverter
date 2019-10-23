using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class Connection
    {
        public Connection(Dictionary<string, byte[]> header, byte[] data, Dictionary<string, RosMessageDefinition> knownDef)
        {
            var fields = Utils.ParseHeaderData(data);

            SavedTopicName = Encoding.UTF8.GetString(header["topic"]);
            if (fields.ContainsKey("topic"))
            {
                Topic = Encoding.UTF8.GetString(fields["topic"]);
            }
            else
            {
                Topic = SavedTopicName;
            }
            
            Type = Encoding.UTF8.GetString(fields["type"]);
            Md5Sum = Encoding.UTF8.GetString(fields["md5sum"]);
            MessageDefinitionText = Encoding.UTF8.GetString(fields["message_definition"]);

            if (fields.ContainsKey("callerid")){
                CallerId = Encoding.UTF8.GetString(fields["callerid"]);
            }

            if (fields.ContainsKey("latching"))
            {
                Latching = BitConverter.ToBoolean(fields["latching"], 0);
            }

            contructRosMessageDefinition(knownDef);

        }

        private void contructRosMessageDefinition(Dictionary<string, RosMessageDefinition> knownDef)
        {
            MessageDefinition = new RosMessageDefinition(Type, MessageDefinitionText, knownDef);
        }

        public string Topic { get; private set; } = "";
        public string SavedTopicName { get; private set; }
        public string Type { get; private set; }
        public string Md5Sum { get; private set; }
        public string MessageDefinitionText { get; private set; }
        public string CallerId { get; private set; } = String.Empty;
        public bool? Latching { get; private set; } = null;

        public RosMessageDefinition MessageDefinition { get; private set; }

    }
}
