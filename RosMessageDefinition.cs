using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class RosMessageDefinition
    {
        private List<Tuple<string, string>> Properties = new List<Tuple<string, string>>();
        public Dictionary<string, RosMessageDefinition> KnownMsgDefinitions;

        public RosMessageDefinition(string typeName, string definition, Dictionary<string, RosMessageDefinition> knownDef)
        {
            this.Type = typeName;
            this.KnownMsgDefinitions = knownDef;
            // Parse the ROS Message Defintions
            this.parseDefinitionText(definition);
            // Add to the known list
            if (!this.KnownMsgDefinitions.ContainsKey(this.Type))
            {
                this.KnownMsgDefinitions.Add(this.Type, this);
            }
        }

        public RosMessageDefinition(List<string> definition, Dictionary<string, RosMessageDefinition> knownDef)
        {
            this.KnownMsgDefinitions = knownDef;
            // the first line tells us the type
            var firstLine = definition[0];
            this.Type = firstLine.Substring(5, firstLine.Length - 5);
            definition.RemoveAt(0);
            this.ParseSingleDefinitonText(definition);
        }

        private void ParseSingleDefinitonText(List<string> sentences)
        {

            foreach (var rawSentence in sentences)
            {
                // trim the sentence
                var sentence = rawSentence.Trim();
                // If the sentence starts with a '#' or is empty then move onto next line
                if (sentence.StartsWith("#") || sentence.Length == 0)
                {
                    continue;
                }

                // split the sentence by space and ignore all items that are spaces
                var sentence_array = sentence.Split(' ').Where(m => m.Length > 0).ToArray();

                // check if this sentence is an constant
                bool valid = true;
                foreach(var s in sentence_array)
                {
                    // ignore if its a comment
                    if (s.Contains('#'))
                    {
                        break;
                    }

                    // if there is an equal sign showing up before any comments
                    // it is constant and should be ignored.
                    if (s.Contains('='))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                {
                    continue;
                }
                // add the field
                this.Properties.Add(new Tuple<string, string>(sentence_array[0], sentence_array[1]));
            }
        }

        private void parseDefinitionText(string text)
        {
            string definitionSplit = "================================================================================";
            var sentences = new List<string>(text.Split('\n'));
            if (sentences.Contains(definitionSplit))
            {
                // This means there are multiple definitions
                // The first one is the definition of this message type
                this.ParseSingleDefinitonText(sentences.Take(sentences.IndexOf(definitionSplit)).ToList());
                sentences.RemoveRange(0, sentences.IndexOf(definitionSplit) + 1);

                // loops through all the dependencies definitions
                while (sentences.Contains(definitionSplit))
                {
                    var subSentences = sentences.Take(sentences.IndexOf(definitionSplit)).ToList();
                    var loopRosDef = new RosMessageDefinition(subSentences, this.KnownMsgDefinitions);
                    if (!this.KnownMsgDefinitions.ContainsKey(loopRosDef.Type))
                    {
                        this.KnownMsgDefinitions.Add(loopRosDef.Type, loopRosDef);
                    }
                    sentences.RemoveRange(0, sentences.IndexOf(definitionSplit) + 1);
                }
                var newRosDef = new RosMessageDefinition(sentences, this.KnownMsgDefinitions);
                if (!this.KnownMsgDefinitions.ContainsKey(newRosDef.Type))
                {
                    this.KnownMsgDefinitions.Add(newRosDef.Type, newRosDef);
                }
            }
            else
            {
                this.ParseSingleDefinitonText(sentences);
            }   
        }

        public List<string> FieldList => this.Properties.Select(e => e.Item2).ToList();

        public string Type { get; private set; }

        public static bool IsBuiltInType(string fieldType)
        {
            // remove the array modifier
            if (fieldType.Contains('['))
            {
                fieldType = fieldType.Substring(0, fieldType.IndexOf('['));
            }
            switch (fieldType)
            {
                case "bool":
                case "int8":
                case "uint8":
                case "char":
                case "byte":
                case "int16":
                case "uint16":
                case "int32":
                case "uint32":
                case "float32":
                case "time":
                case "duration":
                case "int64":
                case "uint64":
                case "float64":
                case "header":
                case "Header":
                case "std_msgs/Header":
                case "string":
                    return true;
            }
            return false;
        }
  
        /// <summary>
        /// Parse the build in type and return the length of object in the bytes and record
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="type"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private Tuple<int, dynamic> ParseBuildInTypes(byte[] rawData, string type, int offset = 0)
        {
            switch (type)
            {
                case "string":
                    var len = (int)BitConverter.ToInt32(rawData, offset);
                    return new Tuple<int, dynamic>(len+4, Encoding.UTF8.GetString(rawData, offset + 4, len));
                case "bool":
                    return new Tuple<int, dynamic>(1, BitConverter.ToBoolean(rawData, offset));
                case "int8":
                    return new Tuple<int, dynamic>(1, rawData[offset]);
                case "uint8":
                    return new Tuple<int, dynamic>(1, rawData[offset]);
                case "int16":
                    return new Tuple<int, dynamic>(2, BitConverter.ToInt16(rawData, offset));
                case "uint16":
                    return new Tuple<int, dynamic>(2, BitConverter.ToUInt16(rawData, offset));
                case "int32":
                    return new Tuple<int, dynamic>(4, BitConverter.ToInt32(rawData, offset));
                case "uint32":
                    return new Tuple<int, dynamic>(4, BitConverter.ToUInt32(rawData, offset));
                case "int64":
                    return new Tuple<int, dynamic>(8, BitConverter.ToInt64(rawData, offset));
                case "uint64":
                    return new Tuple<int, dynamic>(8, BitConverter.ToUInt64(rawData, offset));
                case "float32":
                    return new Tuple<int, dynamic>(4, BitConverter.ToSingle(rawData, offset));
                case "float64":
                    return new Tuple<int, dynamic>(8, BitConverter.ToDouble(rawData, offset));
                case "time":
                    return new Tuple<int, dynamic>(8, RosTime.FromRosBytes(rawData, offset));
                case "duration":
                    return new Tuple<int, dynamic>(8, RosDuration.FromRosBytes(rawData, offset));
                case "header":
                case "Header":
                    var header = RosHeader.FromRosBytes(rawData, offset);
                    return new Tuple<int, dynamic>(header.HeaderByteSize, header);
            }
            return null;
        }

        private (int, dynamic) ParseSingleFieldType(byte[] rawData, string type, int offset = 0)
        {
            if (RosMessageDefinition.IsBuiltInType(type)){
                var result = this.ParseBuildInTypes(rawData, type, offset);
                return (result.Item1, result.Item2);
            }
            else
            {
                // If not built in type
                // get the message definition of the type
                var msgDef = this.KnownMsgDefinitions[type];
                var subMessageResult = msgDef.ParseMessage(rawData, offset);
                return subMessageResult;
            }
        }


        private (int, dynamic) ParseArrayField<T>(byte[] data, int arrSize, string type, int offset)
        {
            var size = 0;
            var arr = new T[arrSize];
            for (var i = 0; i < arrSize; i++)
            {
                var parseResult = this.ParseSingleFieldType(data, type, offset: offset);
                offset += (int)parseResult.Item1;
                size += (int)parseResult.Item1;
                arr[i] = parseResult.Item2;
            }
            return (size, (dynamic) arr);
        }

        private (int, dynamic) PraseArrayFieldType(byte[] rawData, string arrayType, int arrSize, int offset)
        {
            // now parse the array
            switch (arrayType)
            {
                case "string": return this.ParseArrayField<string>(rawData, arrSize, arrayType, offset);
                case "bool": return this.ParseArrayField<bool>(rawData, arrSize, arrayType, offset);
                case "int8": return this.ParseArrayField<sbyte>(rawData, arrSize, arrayType, offset);
                case "uint8": return (arrSize, (dynamic) rawData.Skip(offset).Take(arrSize).ToArray());
                case "int16": return this.ParseArrayField<short>(rawData, arrSize, arrayType, offset);
                case "uint16": return this.ParseArrayField<ushort>(rawData, arrSize, arrayType, offset);
                case "int32": return this.ParseArrayField<int>(rawData, arrSize, arrayType, offset);
                case "uint32": return this.ParseArrayField<uint>(rawData, arrSize, arrayType, offset);
                case "int64": return this.ParseArrayField<long>(rawData, arrSize, arrayType, offset);
                case "uint64": return this.ParseArrayField<ulong>(rawData, arrSize, arrayType, offset);
                case "float32": return this.ParseArrayField<float>(rawData, arrSize, arrayType, offset);
                case "float64": return this.ParseArrayField<double>(rawData, arrSize, arrayType, offset);
                case "time": return this.ParseArrayField<RosTime>(rawData, arrSize, arrayType, offset);
                case "duration": return this.ParseArrayField<RosDuration>(rawData, arrSize, arrayType, offset);
                default:
                    // this is an array of a constructed type. We return it as a array field of ROSMessages
                    return this.ParseArrayField<Dictionary<string, dynamic>>(rawData, arrSize, arrayType, offset);
            }
        }

        public (int, Dictionary<string, dynamic>) ParseMessage(byte[] data, int offset = 0){
            
            var originalOffset = offset;
            var fieldDict = new Dictionary<string, dynamic>();
            // go through each property
            foreach(var property in this.Properties){
                // First check if its an array type
                if(property.Item1.Contains('[')){
                    // get type
                    var arrType = property.Item1.Substring(0, property.Item1.IndexOf('['));
                    var arrSize = 0;
                    if (property.Item1.EndsWith("[]"))
                    {
                        // this is variable length array
                        arrSize = BitConverter.ToInt32(data, offset);
                        offset += 4;
                    }
                    else if (property.Item1.EndsWith("]"))
                    {
                        // this is a fixed length array
                        arrSize = Int32.Parse(property.Item1.Substring(property.Item1.IndexOf('[') + 1, property.Item1.IndexOf(']') - property.Item1.IndexOf('[') - 1));
                    }
                    // now parse the array
                    var parseResult = this.PraseArrayFieldType(data, arrType, arrSize, offset);
                    offset += parseResult.Item1;
                    fieldDict[property.Item2] = parseResult.Item2;
                }
                else{
                    // parse a single field
                    var parseResult = this.ParseSingleFieldType(data, property.Item1, offset);
                    offset += parseResult.Item1; // update offset
                    fieldDict[property.Item2] = parseResult.Item2; // add it to the list
                }
            }
            return (offset - originalOffset, fieldDict);
        }

        /// <summary>
        /// Return the type of the field given its name
        /// </summary>
        /// <param name="indexString">Name of field</param>
        /// <returns>The type of the field</returns>
        public string GetFieldType(string indexString)
        {
            return this.Properties.Where(e => e.Item2 == indexString).First().Item1;
        }
        
    }
}
