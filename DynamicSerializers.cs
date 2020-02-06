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
        private bool useHeaderTime;
        private bool useCustomSerializer;
        private List<BaseMsgsSerializer> customSerializerList = new List<BaseMsgsSerializer>();
        private Dictionary<string, RosMessageDefinition> KnownMsgDefinitions;
        private TimeSpan offset;

        public DynamicSerializers(Dictionary<string, RosMessageDefinition> knownDefinitions, bool useHeaderTime, TimeSpan? offset = null, bool useCustomSerializer = true)
        {
            this.offset = offset ?? TimeSpan.Zero;
            this.useHeaderTime = useHeaderTime;
            this.KnownMsgDefinitions = knownDefinitions;
            this.useCustomSerializer = useCustomSerializer;

            if (this.useCustomSerializer){
                // Add standard custom serializers
                this.AddCustomSerializer(new StdMsgsSerializers(offset: offset));
                this.AddCustomSerializer(new SensorMsgsSerializer(useHeaderTime, offset: offset));
                this.AddCustomSerializer(new GeometryMsgsSerializer(useHeaderTime, offset: offset));
            }
        }

        /// <summary>
        /// Create a PsiGenerator for the IEnumerable and write it to PsiStore
        /// </summary>
        /// <typeparam name="T">Type of Message to be written</typeparam>
        /// <param name="pipeline">Current pipeline</param>
        /// <param name="topic">Name of the stream</param>
        /// <param name="messages">Collection of Tuples of dynamic type and time. The true type must be T</param>
        /// <param name="store">Store to write to</param>
        public static void WriteStronglyTyped<T>(Pipeline pipeline, string topic, IEnumerable<(dynamic, DateTime)> messages, Exporter store)
        {
            Console.WriteLine($"Stream: {topic} ({typeof(T)})");
            Generators.Sequence(pipeline, messages.Select(m => ((T)m.Item1, m.Item2))).Write(topic, store);
        }

        /// <summary>
        /// Create a PsiGenerator for the IEnumerable and write it to PsiStore
        /// </summary>
        /// <typeparam name="T">Type of Message to be written</typeparam>
        /// <param name="pipeline">Current pipeline</param>
        /// <param name="topic">Name of the stream</param>
        /// <param name="messages">Collection of Tuples with the type and time</param>
        /// <param name="store">Store to write to</param>
        public static void WriteStronglyTyped<T>(Pipeline pipeline, string topic, IEnumerable<(T, DateTime)> messages, Exporter store)
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
                    WriteStronglyTyped<DateTime>(pipeline, topic, messages.Select(m => ((dynamic)(m.Item1 as RosTime).ToDateTime(), m.Item2)), store);
                    break;
                case RosDuration _:
                    WriteStronglyTyped<TimeSpan>(pipeline, topic, messages.Select(m => ((dynamic)(m.Item1 as RosDuration).ToTimeSpan(), m.Item2)), store);
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
                case string[] _:
                    WriteStronglyTyped<string[]>(pipeline, topic, messages, store);
                    break;
                case float[] _:
                    WriteStronglyTyped<float[]>(pipeline, topic, messages, store);
                    break;
                case double[] _:
                    WriteStronglyTyped<double[]>(pipeline, topic, messages, store);
                    break;
                case RosHeader _:
                    WriteStronglyTyped<DateTime>(pipeline, $"{topic}.Time", messages.Select(m => ((dynamic)(m.Item1 as RosHeader).Time.ToDateTime(), m.Item2)), store);
                    WriteStronglyTyped<String>(pipeline, $"{topic}.FrameId", messages.Select(m => ((dynamic)(m.Item1 as RosHeader).FrameId, m.Item2)), store);
                    break;
            }
        }


        public void AddCustomSerializer(BaseMsgsSerializer serializer)
        {
            this.customSerializerList.Add(serializer);
        }

        /// <summary>
        /// Serialize the ROS Message by converting to \psi streams and writing into PsiStore.
        /// If they are common types, they are converted into formats that are more suitable for Psi.
        /// </summary>
        public void SerializeMessages(Pipeline pipeline, Exporter store, string streamName, string messageType, IEnumerable<RosMessage> messages)
        {
            // If it's a known type, we serialize according to a pre-defined schema, if chosen.
            if (this.useCustomSerializer){
                // Loop through a list of known message type
                foreach(var serializer in this.customSerializerList)
                {
                    // Check if they start with the currect prefix
                    if (messageType.StartsWith(serializer.Prefix))
                    {
                        // try to serialize the message
                        if (serializer.SerializeMessage(pipeline, store, streamName, messages, messageType))
                        {
                            // if successful, return.
                            return;
                        }
                    }
                }
            }

            // If it's an unknown field type, we try our best to serialize it by parsing each part
            var messageDefiniton = messages.First().MessageType;
            // Go through each field in the message
            foreach (var fieldName in messageDefiniton.FieldList)
            {
                //If its a built in type, we serialize it
                if (RosMessageDefinition.IsBuiltInType(messageDefiniton.GetFieldType(fieldName)))
                {
                    this.serializeBuiltInFields(pipeline, store, fieldName, messageDefiniton.GetFieldType(fieldName), $"{streamName}.{fieldName}", messages);
                    continue;
                }

                var fieldType = messageDefiniton.GetFieldType(fieldName);

                // We going to ignore array of constructed type for now. Not too sure what the equivalent in Psi world might be.
                if (fieldType.EndsWith("]")){
                    continue;
                }

                // It is a constructred type

                RosMessageDefinition constructedMsgDef = null;
                if (this.KnownMsgDefinitions.ContainsKey(fieldType))
                {
                    constructedMsgDef = this.KnownMsgDefinitions[fieldType];
                }
                else
                {
                    foreach (var name in this.KnownMsgDefinitions.Keys)
                    {
                        if (name.EndsWith(fieldType))
                        {
                            constructedMsgDef = this.KnownMsgDefinitions[name];
                            break;
                        }
                    }
                    if (constructedMsgDef == null)
                    {
                        throw new KeyNotFoundException($"{fieldType} not found among known message definitions.");
                    }
                }
                // we construct a submessage using the same variables as before. This is to setup a recursive call on the serialization
                var subMessages = messages.Select(x => x.GetFieldAsRosMessage(constructedMsgDef, fieldName)).ToList();
                // Recursively call the serialization code
                this.SerializeMessages(pipeline, store, $"{streamName}.{fieldName}", constructedMsgDef.Type, subMessages);
            }
        }

        private void serializeBuiltInFields(Pipeline pipeline, Exporter store, string fieldName, string fieldType, string streamName, IEnumerable<RosMessage> messages)
        {
            // remove the array modifier
            if (fieldType.Contains('['))
            {
                fieldType = fieldType.Substring(0, fieldType.IndexOf('['));
            }
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
                case ("header"):
                case ("Header"):
                case ("std_msgs/Header"):
                    WriteDynamic(pipeline, streamName, messages.Select(m => (m.GetField(fieldName), m.Time.ToDateTime() + this.offset)), store);
                    return;
                case ("time"):
                    WriteDynamic(pipeline, streamName, messages.Select(m => ((dynamic)((RosTime)m.GetField(fieldName)).ToDateTime(), m.Time.ToDateTime() + this.offset)), store);
                    return;
                case ("duration"):
                    WriteDynamic(pipeline, streamName, messages.Select(m => ((dynamic)((RosDuration)m.GetField(fieldName)).ToTimeSpan(), m.Time.ToDateTime() + this.offset)), store);
                    return;                    
            }            
        }
    }
}
