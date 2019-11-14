using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Data;
using RosBagConverter.MessageSerializers;

namespace RosBagConverter
{
    public class DynamicSerializers
    {
        private StdMsgsSerializers stdMsgsSerializer;
        private SensorMsgsSerializer sensorMsgsSerializer;
        private Dictionary<string, RosMessageDefinition> knowMessageDefinitions;

        public DynamicSerializers(Dictionary<string, RosMessageDefinition> knownDefinitions)
        {
            this.knowMessageDefinitions = knownDefinitions;
            this.stdMsgsSerializer = new StdMsgsSerializers();
            this.sensorMsgsSerializer = new SensorMsgsSerializer();
        }

        private static void WriteStronglyTyped<T>(Pipeline pipeline, string topic, IEnumerable<(dynamic, DateTime)> messages, Exporter store)
        {
            Console.WriteLine($"Stream: {topic} ({typeof(T)})");
            Generators.Sequence(pipeline, messages.Select(m => ((T)m.Item1, m.Item2))).Write(topic, store);
        }

        public static void WriteDynamic(Pipeline pipeline, string topic, IEnumerable<(dynamic, DateTime)> messages, Exporter store)
        {
            switch (messages.First().Item1)
            {
                case string _:
                    WriteStronglyTyped<string>(pipeline, topic, messages, store);
                    break;
                case bool _:
                    WriteStronglyTyped<bool>(pipeline, topic, messages, store);
                    break;
                case byte _:
                    WriteStronglyTyped<byte>(pipeline, topic, messages, store);
                    break;
                case sbyte _:
                    WriteStronglyTyped<sbyte>(pipeline, topic, messages, store);
                    break;
                case short _:
                    WriteStronglyTyped<short>(pipeline, topic, messages, store);
                    break;
                case ushort _:
                    WriteStronglyTyped<ushort>(pipeline, topic, messages, store);
                    break;
                case int _:
                    WriteStronglyTyped<int>(pipeline, topic, messages, store);
                    break;
                case uint _:
                    WriteStronglyTyped<uint>(pipeline, topic, messages, store);
                    break;
                case long _:
                    WriteStronglyTyped<long>(pipeline, topic, messages, store);
                    break;
                case ulong _:
                    WriteStronglyTyped<ulong>(pipeline, topic, messages, store);
                    break;
                case float _:
                    WriteStronglyTyped<float>(pipeline, topic, messages, store);
                    break;
                case double _:
                    WriteStronglyTyped<double>(pipeline, topic, messages, store);
                    break;
                case RosTime _:
                    WriteStronglyTyped<RosTime>(pipeline, topic, messages, store);
                    break;
                case RosDuration _:
                    WriteStronglyTyped<RosDuration>(pipeline, topic, messages, store);
                    break;
                case byte[] _:
                    WriteStronglyTyped<byte[]>(pipeline, topic, messages, store);
                    break;
                case sbyte[] _:
                    WriteStronglyTyped<sbyte[]>(pipeline, topic, messages, store);
                    break;
                case short[] _:
                    WriteStronglyTyped<short[]>(pipeline, topic, messages, store);
                    break;
                case ushort[] _:
                    WriteStronglyTyped<ushort[]>(pipeline, topic, messages, store);
                    break;
                case int[] _:
                    WriteStronglyTyped<int[]>(pipeline, topic, messages, store);
                    break;
                case uint[] _:
                    WriteStronglyTyped<uint[]>(pipeline, topic, messages, store);
                    break;
            }
        }

        /// <summary>
        /// Serialize the ROS Message by converting to \psi streams and writing into PsiStore.
        /// If they are common types, they are converted into formats that are more suitable for Psi.
        /// </summary>
        public void SerializeMessages(Pipeline pipeline, Exporter store, string streamName, string messageType, IEnumerable<RosMessage> messages)
        {
            // If it's a known type, we serialize according to a pre-defined schema

            // Loop through a list of known message type
            // Cool to do some reflection on how to automate this.
            if (messageType.StartsWith("std_msgs"))
            {
                if (this.stdMsgsSerializer.SerializeMessage(pipeline, store, streamName, messages, messageType))
                {
                    return;
                }
            }
            else if (messageType.StartsWith("sensor_msgs"))
            {
                if (this.sensorMsgsSerializer.SerializeMessage(pipeline, store, streamName, messages, messageType))
                {
                    return;
                }
            }

            // If it's an unknown field type, we try our best to serialize it by parsing each part
            var messageDefiniton = messages.First().MessageType;
            foreach (var fieldName in messageDefiniton.FieldList)
            {
                //Try to serialize build in types
                if (RosMessageDefinition.IsBuiltInType(messageDefiniton.GetFieldType(fieldName)))
                {
                    this.serializeBuiltInFields(pipeline, store, fieldName, messageDefiniton.GetFieldType(fieldName), $"{streamName}.{fieldName}", messages);
                }
               
                // Try to see if the field type is a known type that we read from the message definition.
                foreach(var knownType in this.knowMessageDefinitions.Keys)
                {
                    if (knownType.EndsWith(messageDefiniton.GetFieldType(fieldName)))
                    {
                        // we construct a submessage using the same variables as before. This is to setup a recursive call on the serialization
                        var subMessages = messages.Select(x => x.GetFieldAsRosMessage(this.knowMessageDefinitions[knownType], fieldName)).ToList();
                        // Recursively call the serialization code
                        this.SerializeMessages(pipeline, store, $"{streamName}.{fieldName}", knownType, subMessages);
                    }
                }
            }
        }

        private void serializeBuiltInFields(Pipeline pipeline, Exporter store, string fieldName, string fieldType, string streamName, IEnumerable<RosMessage> messages)
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
                case ("int64"):
                case ("uint64"):
                case ("float32"):
                case ("float64"):
                case ("string"):
                case ("bool"):
                    WriteDynamic(pipeline, streamName, messages.Select(m => (m.GetField(fieldName), m.Time.ToDateTime())), store);
                    return;
                case ("time"):
                    WriteDynamic(pipeline, streamName, messages.Select(m => ((dynamic)((RosTime)m.GetField(fieldName)).ToDateTime(), m.Time.ToDateTime())), store);
                    return;
                case ("duration"):
                    WriteDynamic(pipeline, streamName, messages.Select(m => ((dynamic)((RosDuration)m.GetField(fieldName)).ToTimeSpan(), m.Time.ToDateTime())), store);
                    return;
            }

            // serialize arrays
            if (fieldType.Contains("["))
            {
                fieldType = fieldType.Substring(0, fieldType.IndexOf('['));
                switch (fieldType)
                {
                    case ("uint8"):
                    case ("int32"):
                    case ("float64"):
                        WriteDynamic(pipeline, streamName, messages.Select(m => ((dynamic)m.GetField(fieldName), m.Time.ToDateTime())), store);
                        return;
                }
            }
            
        }
    }
}
