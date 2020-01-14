using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;


namespace RosBagConverter.MessageSerializers
{
    public class SensorMsgsSerializer : BaseMsgsSerializer
    {
        public SensorMsgsSerializer(bool useHeaderTime)
            : base(useHeaderTime)
        {
            // TODO I wanted everyone to share a serializer but somehow they currently randomly get deconstructed.
        }

        public bool SerializeMessage(Pipeline pipeline, Exporter store, string streamName, IEnumerable<RosMessage> messages, string messageType)
        {
            try
            {
                switch (messageType)
                {
                    case ("sensor_msgs/Image"):
                        DynamicSerializers.WriteStronglyTyped<Shared<Image>>(pipeline, streamName, messages.Select(m => (this.ImageToPsiImage(m), m.Time.ToDateTime())), store);
                        return true;
                    case ("sensor_msgs/CompressedImage"):
                        // get header
                        DynamicSerializers.WriteStronglyTyped<Shared<Image>>(pipeline, streamName, messages.Select(m => (this.CompressedImageToPsiImage(m), this.useHeaderTime ? ((RosHeader)m.GetField("header")).Time.ToDateTime() : m.Time.ToDateTime())), store);
                        return true;
                    default: return false;
                }
            }
            catch (NotSupportedException)
            {
                // Not supported default to total copy
                return false;
            }

        }

        private PixelFormat EncodingToPixelFormat(string encoding)
        {
            switch (encoding.ToUpper())
            {
                case ("BGR8"): return PixelFormat.BGR_24bpp;
                case ("BGRA8"): return PixelFormat.BGRA_32bpp;
                case ("MONO8"): return PixelFormat.Gray_8bpp;
                case ("MONO16"): return PixelFormat.Gray_16bpp;
                case ("RGBA16"): return PixelFormat.RGBA_64bpp;
                default: return PixelFormat.Undefined;
            }
        }

        private dynamic ImageToPsiImage(RosMessage message)
        {
            int width = (int)message.GetField("width");
            int height = (int)message.GetField("height");

            // convert formats
            string encoding = (string)message.GetField("encoding");
            var format = this.EncodingToPixelFormat(encoding);
            if (format == PixelFormat.Undefined)
            {
                Console.WriteLine($"Image Encoding Type {encoding} is not supported. Defaulting to writeout");
                throw new NotSupportedException($"Image Encoding Type {encoding} is not supported");
            }

            using (var sharedImage = ImagePool.GetOrCreate(width, height, format))
            {
                // skip the first 4 bytes because in ROS Message its a varied length array where the first 4 bytes tell us the length.
                sharedImage.Resource.CopyFrom(message.GetRawField("data").Skip(4).ToArray());
                return sharedImage.AddRef();
                // return sharedImage;
            }
        }

        private dynamic CompressedImageToPsiImage(RosMessage message)
        {
            // get format
            var format = (string)message.GetField("format");

            var imageMemoryStream = new MemoryStream(message.GetRawField("data").Skip(4).ToArray());
            using (var image = System.Drawing.Image.FromStream(imageMemoryStream))
            {
                var bitmap = new System.Drawing.Bitmap(image);
                using (var sharedImage = ImagePool.GetOrCreate(image.Width, image.Height, PixelFormat.BGR_24bpp))
                {
                    sharedImage.Resource.CopyFrom(bitmap);
                    return sharedImage.AddRef();
                }
            }
        }
    }
}
