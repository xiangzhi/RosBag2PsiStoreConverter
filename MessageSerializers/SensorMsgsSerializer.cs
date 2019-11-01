using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;

namespace RosBagConverter.MessageSerializers
{
    public class SensorMsgsSerializer
    {
        public SensorMsgsSerializer()
        {
            // TODO I wanted everyone to share a serializer but somehow they currently randomly get deconstructed.
        }

        public bool SerializeMessage(Pipeline pipeline, Exporter store, string streamName, List<RosMessage> messages, string messageType)
        {
            switch (messageType)
            {
                case ("sensor_msgs/Image"):
                    DynamicSerializers.Write(pipeline, streamName, messages.Select(m => (this.RosMessageToPsiImage(m), m.Time.ToDateTime())), store);
                    return true;
                default: return false;
            }
        }

        private PixelFormat EncodingToPixelFormat(string encoding)
        {
            switch (encoding)
            {
                case ("BGR8"): return PixelFormat.BGR_24bpp;
                case ("BGRA8"): return PixelFormat.BGRA_32bpp;
                case ("MONO8"): return PixelFormat.Gray_8bpp;
                case ("MONO16"): return PixelFormat.Gray_16bpp;
                case ("RGBA16"): return PixelFormat.RGBA_64bpp;
                default: return PixelFormat.Undefined;
            }
        }

        private dynamic RosMessageToPsiImage(RosMessage message)
        {
            int width = (int)message.GetField("width");
            int height = (int)message.GetField("height");

            // convert formats
            string encoding = (string)message.GetField("encoding");
            var format = EncodingToPixelFormat(encoding);
            if (format == PixelFormat.Undefined)
            {
                Console.WriteLine($"Image Encoding Type {encoding} is not supported. Defaulting to writeout");
                return false;
                // throw new NotSupportedException($"Image Encoding Type {encoding} is not supported");
            }

            using (var sharedImage = ImagePool.GetOrCreate(width, height, PixelFormat.BGRA_32bpp))
            {
                sharedImage.Resource.CopyFrom(message.GetRawField("data"));
                return sharedImage;
            }
        }
    }
}
