using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;

namespace RosBagConverter.MessageSerializers
{
    public class GeometryMsgsSerializer
    {
        private bool useHeaderTime; // Whether to use the header time (if available) or message publish time

        public GeometryMsgsSerializer(bool useHeaderTime = false)
        {
            this.useHeaderTime = useHeaderTime;
        }

        public bool SerializeMessage(Pipeline pipeline, Exporter store, string streamName, IEnumerable<RosMessage> messages, string messageType)
        {
            try
            {
                switch (messageType)
                {
                    case ("geometry_msgs/Point"):
                        DynamicSerializers.WriteStronglyTyped<Point3D>(pipeline, streamName, messages.Select(m =>
                        {
                            // convert to point
                            return ((dynamic)new Point3D(m.GetField("x"), m.GetField("y"), m.GetField("z")), m.Time.ToDateTime());
                        }), store);
                        return true;
/*                    case ("geometry_msgs/PointStamped"):
                        DynamicSerializers.WriteStronglyTyped<Point3D>(pipeline, streamName, messages.Select(m =>
                        {
                            // var header = m.GetField("header");
                            var pointObject = m.GetField("point");
                            // convert to point
                            return ((dynamic)new Point3D(pointObject.GetField("x"), pointObject.GetField("y"), pointObject.GetField("z")), m.Time.ToDateTime());
                        }), store);
                        return true;*/
                    default: return false;
                }
            }
            catch (NotSupportedException)
            {
                // Not supported default to total copy
                return false;
            }

        }
    }
}
