using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Persistence;
using Microsoft.Psi.Serialization;
using RosBagConverter.MessageSerializers;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class DynamicSerializers
    {
        private KnownSerializers serializers = new KnownSerializers();
        private SerializationContext context = new SerializationContext();
        private int streamCounter = 0;
        private StdMsgsSerializers stdMsgsSerializer;
        private SensorMsgsSerializer sensorMsgsSerializer;
        private Dictionary<string, RosMessageDefinition> knowMessageDefinitions;

        public DynamicSerializers(Dictionary<string, RosMessageDefinition> knownDefinitions)
        {
            this.knowMessageDefinitions = knownDefinitions;
            this.stdMsgsSerializer = new StdMsgsSerializers(this.serializers);
            this.sensorMsgsSerializer = new SensorMsgsSerializer(this.serializers);
        }


        /// <summary>
        /// Serialize the ROS Message into Buffer writer format to be written into PsiStore.
        /// If they are common types, they are converted into formats that are more suitable for Psi.
        /// </summary>
        public bool SerializeMessage(StoreWriter store, string streamName, List<RosMessage> messages)
        {

            // If it's a known type, we serialize according to a pre-defined schema
            bool complete = false;
            string messageType = messages.FirstOrDefault()?.MessageType.Type;
            if (messageType == null)
            {
                return false;
            }

            // Loop through a list of known message type
            // Cool to do some reflection on how to automate this.
            if (messageType.StartsWith("std_msgs"))
            {
                complete = this.stdMsgsSerializer.SerializeMessage(store, streamName, messages, streamCounter++);
            }
            else if (messageType.StartsWith("sensor_msgs"))
            {
                complete = this.sensorMsgsSerializer.SerializeMessage(store, streamName, messages, streamCounter++);
            }

            /*            else if (messageType.StartsWith("sensor_msgs"))
                        {
                            // call the sensor_msgs serializer
            *//*                if (messageType == "sensor_msgs/Image")
                            {
                                BufferWriter dataBuffer = new BufferWriter(128);

                                var ImageSerializer = this.serializers.GetHandler<Image>();
                                int streamId = streamCounter++;
                                store.OpenStream(streamId, streamName, true, ImageSerializer.Name);
                                for (var i = 0; i < messages.Count; i++)
                                {
                                    var height = (int)messages[i].GetField("height");
                                    var width = (int)messages[i].GetField("width");
                                    var data = ((List<dynamic>)messages[i].GetField("data")).Cast<byte>()
                                    IntPtr;
                                    var image = new Image(data, width, height, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
                                    context.Reset();
                                    dataBuffer.Reset();
                                    intSerializer.Serialize(dataBuffer, (int)messages[i].GetField("data"), context);
                                    this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                                }

                            }*//*
                        }*/

            if (complete)
            {
                return true;
            }

            // If it's an unknown field type, we try our best to serialize it by parsing each part
            var messageDefiniton = messages.First().MessageType;
            foreach (var fieldName in messageDefiniton.FieldList)
            {
                //Try to serialize build in types
                if (RosMessageDefinition.IsBuiltInType(messageDefiniton.GetFieldType(fieldName)))
                {
                    this.serializeBuiltInFields(store, fieldName, messageDefiniton.GetFieldType(fieldName), String.Format("{0}.{1}", streamName, fieldName), messages, streamCounter++);
                }
               
                // Try to see if the field type is a known type that we read from the message definition.
                foreach(var knownType in this.knowMessageDefinitions.Keys)
                {
                    if (knownType.EndsWith(messageDefiniton.GetFieldType(fieldName)))
                    {
                        // we construct a submessage using the same variables as before. This is to setup a recursive call on the serialization
                        var subMessages = messages.Select(x => x.GetFieldAsRosMessage(this.knowMessageDefinitions[knownType], fieldName)).ToList();
                        // Recursively call the serialization code
                        this.SerializeMessage(store, String.Format("{0}.{1}", streamName, fieldName), subMessages);
                    }
                }
            }
            return false;
        }

        private void WriteToStore(StoreWriter store, BufferWriter dataBuffer, RosMessage message, int streamId, int msgNum)
        {
            var envelope = new Envelope(message.Time.ToDateTime(), message.Time.ToDateTime(), streamId, msgNum);
            store.Write(new BufferReader(dataBuffer), envelope);
        }


        private void serializeBuiltInFields(StoreWriter store, string fieldName, string fieldType, string streamName, List<RosMessage> messages, int streamId)
        {
            BufferWriter dataBuffer = new BufferWriter(128);

            switch (fieldType)
            {
                case ("int8"):
                case ("uint8"):
                case ("int16"):
                case ("uint16"):
                case ("int32"):
                case ("uint32"):
                    var intSerializer = this.serializers.GetHandler<int>();
                    store.OpenStream(streamId, streamName, true, intSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        intSerializer.Serialize(dataBuffer, (int)messages[i].GetField(fieldName), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return;
                case ("int64"):
                case ("uint64"):
                    var longSerializer = this.serializers.GetHandler<long>();
                    store.OpenStream(streamId, streamName, true, longSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        longSerializer.Serialize(dataBuffer, (long)messages[i].GetField(fieldName), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return;
                case ("float32"):
                case ("float64"):
                    var doubleSerializer = this.serializers.GetHandler<double>();
                    store.OpenStream(streamId, streamName, true, doubleSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        doubleSerializer.Serialize(dataBuffer, (double)messages[i].GetField(fieldName), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return;
                case ("string"):
                    var stringSerializer = this.serializers.GetHandler<string>();
                    store.OpenStream(streamId, streamName, true, stringSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        stringSerializer.Serialize(dataBuffer, (string)messages[i].GetField(fieldName), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return;
                case ("bool"):
                    var boolSerializer = this.serializers.GetHandler<bool>();
                    store.OpenStream(streamId, streamName, true, boolSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        boolSerializer.Serialize(dataBuffer, (bool)messages[i].GetField(fieldName), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return;
                case ("time"):
                    var dateTimeSerializer = this.serializers.GetHandler<DateTime>();
                    store.OpenStream(streamId, streamName, true, dateTimeSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        dateTimeSerializer.Serialize(dataBuffer, ((RosTime)messages[i].GetField(fieldName)).ToDateTime(), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return;
                case ("duration"):
                    var timeSpanSerializer = this.serializers.GetHandler<TimeSpan>();
                    store.OpenStream(streamId, streamName, true, timeSpanSerializer.Name);
                    for (var i = 0; i < messages.Count; i++)
                    {
                        context.Reset();
                        dataBuffer.Reset();
                        timeSpanSerializer.Serialize(dataBuffer, ((RosDuration)messages[i].GetField(fieldName)).ToTimeSpan(), context);
                        this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                    }
                    return;
            }

            // serialize arrays
            if (fieldType.Contains("["))
            {
                fieldType = fieldType.Substring(0, fieldType.IndexOf('['));
                switch (fieldType)
                {
                    case ("uint8"):
                        var uInt8ListSerializer = this.serializers.GetHandler<byte[]>();
                        store.OpenStream(streamId, streamName, true, uInt8ListSerializer.Name);
                        for (var i = 0; i < messages.Count; i++)
                        {
                            context.Reset();
                            dataBuffer.Reset();
                            uInt8ListSerializer.Serialize(dataBuffer, ((List<dynamic>)messages[i].GetField(fieldName)).Cast<byte>().ToArray(), context);
                            this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                        }
                        return;
                    case ("int32"):
                        var intListSerializer = this.serializers.GetHandler<int[]>();
                        store.OpenStream(streamId, streamName, true, intListSerializer.Name);
                        for (var i = 0; i < messages.Count; i++)
                        {
                            context.Reset();
                            dataBuffer.Reset();
                            intListSerializer.Serialize(dataBuffer, ((List<dynamic>)messages[i].GetField(fieldName)).Cast<int>().ToArray(), context);
                            this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                        }
                        return;
                    case ("float64"):
                        var doublelistSerializer = this.serializers.GetHandler<double[]>();
                        store.OpenStream(streamId, streamName, true, doublelistSerializer.Name);
                        for (var i = 0; i < messages.Count; i++)
                        {
                            context.Reset();
                            dataBuffer.Reset();
                            doublelistSerializer.Serialize(dataBuffer, ((List<dynamic>)messages[i].GetField(fieldName)).Cast<double>().ToArray(), context);
                            this.WriteToStore(store, dataBuffer, messages[i], streamId, i);
                        }
                        return;
                }
            }
            
        }
    }
}
