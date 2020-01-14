using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Data;

namespace RosBagConverter.MessageSerializers
{
    public class StdMsgsSerializers : BaseMsgsSerializer
    {
        public StdMsgsSerializers()
        {
        }

        public bool SerializeMessage(Pipeline pipeline, Exporter store, string streamName, IEnumerable<RosMessage> messages, string messageType)
        {
            switch (messageType)
            {
                case "std_msgs/Int8":
                case "std_msgs/UInt8":
                case "std_msgs/Int16":
                case "std_msgs/UInt16":
                case "std_msgs/Int32":
                case "std_msgs/UInt32":
                case "std_msgs/Int64":
                case "std_msgs/UInt64":
                case "std_msgs/Float32":
                case "std_msgs/Float64":
                case "std_msgs/String":
                case "std_msgs/Bool":
                    DynamicSerializers.WriteDynamic(pipeline, streamName, messages.Select(m => (m.GetField("data"), m.Time.ToDateTime())), store);
                    return true;
                case "std_msgs/Header":
                    // For header, return time for now
                    DynamicSerializers.WriteDynamic(pipeline, streamName, messages.Select(m => 
                    {
                        var headerTime = m.GetField("stamp") as RosTime;
                        return ((dynamic) headerTime.ToDateTime(), m.Time.ToDateTime());
                    }), store);
                    return true;
                default: return false;

            }
        }
    }
}
