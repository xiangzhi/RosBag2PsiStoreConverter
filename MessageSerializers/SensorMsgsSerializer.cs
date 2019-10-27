using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Persistence;
using Microsoft.Psi.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter.MessageSerializers
{
    public class SensorMsgsSerializer
    {
        private KnownSerializers serializers = new KnownSerializers();
        private SerializationContext context = new SerializationContext();

        public SensorMsgsSerializer(KnownSerializers serializers = null)
        {
            // TODO I wanted everyone to share a serializer but somehow they currently randomly get deconstructed.
        }

        private void WriteToStore(StoreWriter store, BufferWriter dataBuffer, RosMessage message, int streamId, int msgNum)
        {
            var envelope = new Envelope(message.Time.ToDateTime(), message.Time.ToDateTime(), streamId, msgNum);
            store.Write(new BufferReader(dataBuffer), envelope);
        }

        public bool SerializeMessage(StoreWriter store, string streamName, List<RosMessage> messages, int streamId)
        {
            string messageType = messages.FirstOrDefault()?.MessageType.Type;
            BufferWriter dataBuffer = new BufferWriter(128);

            switch (messageType)
            {
                case ("sensor_msgs/Image"):

                    var imgSerializer = this.serializers.GetHandler<Shared<Image>>();
                    store.OpenStream(streamId, streamName, true, imgSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();

                        int width = (int) messages[i].GetField("width");
                        int height = (int)messages[i].GetField("height");

                        // convert formats
                        string encoding = (string)messages[i].GetField("encoding");
                        PixelFormat format = PixelFormat.Undefined;
                        switch (encoding)
                        {
                            case ("BGR8"):
                                format = Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp;
                                break;
                            case ("BGRA8"):
                                format = Microsoft.Psi.Imaging.PixelFormat.BGRA_32bpp;
                                break;
                            case ("MONO8"):
                                format = Microsoft.Psi.Imaging.PixelFormat.Gray_8bpp;
                                break;
                            case ("MONO16"):
                                format = Microsoft.Psi.Imaging.PixelFormat.Gray_16bpp;
                                break;
                            case ("RGBA16"):
                                format = Microsoft.Psi.Imaging.PixelFormat.RGBA_64bpp;
                                break;
                        }
                        if (format == PixelFormat.Undefined)
                        {
                            Console.WriteLine($"Image Encoding Type {encoding} is not supported. Defaulting to writeout");
                            return false;
                            // throw new NotSupportedException($"Image Encoding Type {encoding} is not supported");
                        }

                        using (var sharedImage = ImagePool.GetOrCreate(width, height, Microsoft.Psi.Imaging.PixelFormat.BGRA_32bpp))
                        {
                            sharedImage.Resource.CopyFrom(messages[i].GetRawField("data"));

                            imgSerializer.Serialize(dataBuffer, sharedImage, context);
                            this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                        }
                    }
                    return true;
            }
            return false;
        }
    }
}
