using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Persistence;
using Microsoft.Psi.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter.MessageSerializers
{
    public class StdMsgsSerializers
    {
        private KnownSerializers serializers = new KnownSerializers();
        private SerializationContext context = new SerializationContext();

        public StdMsgsSerializers(KnownSerializers serializers = null)
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
                case ("std_msgs/Int8"):
                case ("std_msgs/UInt8"):
                case ("std_msgs/Int16"):
                case ("std_msgs/UInt16"):
                case ("std_msgs/Int32"):
                case ("std_msgs/UInt32"):
                    var intSerializer = this.serializers.GetHandler<int>();
                    store.OpenStream(streamId, streamName, true, intSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        intSerializer.Serialize(dataBuffer, (int)messages[i].GetField("data"), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return true;
                case ("std_msgs/Int64"):
                case ("std_msgs/UInt64"):
                    var longSerializer = this.serializers.GetHandler<long>();
                    store.OpenStream(streamId, streamName, true, longSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        longSerializer.Serialize(dataBuffer, (long)messages[i].GetField("data"), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return true;
                case ("std_msgs/Float32"):
                case ("std_msgs/Float64"):
                    var doubleSerializer = this.serializers.GetHandler<double>();
                    store.OpenStream(streamId, streamName, true, doubleSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        doubleSerializer.Serialize(dataBuffer, (double)messages[i].GetField("data"), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return true;
                case ("std_msgs/String"):
                    var stringSerializer = this.serializers.GetHandler<string>();
                    store.OpenStream(streamId, streamName, true, stringSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        stringSerializer.Serialize(dataBuffer, (string)messages[i].GetField("data"), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return true;
                case ("std_msgs/Bool"):
                    var boolSerializer = this.serializers.GetHandler<bool>();
                    store.OpenStream(streamId, streamName, true, boolSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        boolSerializer.Serialize(dataBuffer, (bool)messages[i].GetField("data"), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return true;
            }
            return false;
        }
    }
}
