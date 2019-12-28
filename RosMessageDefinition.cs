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
        public Dictionary<string, RosMessageDefinition> KnownDefinitions;

        public RosMessageDefinition(string typeName, string definition, Dictionary<string, RosMessageDefinition> knownDef)
        {
            this.Type = typeName;
            this.KnownDefinitions = knownDef;
            // Parse the ROS Message Defintions
            this.parseDefinitionText(definition);
            // Add to the known list
            if (!this.KnownDefinitions.ContainsKey(this.Type))
            {
                this.KnownDefinitions.Add(this.Type, this);
            }
        }

        public RosMessageDefinition(List<string> definition, Dictionary<string, RosMessageDefinition> knownDef)
        {
            this.KnownDefinitions = knownDef;
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
                    var loopRosDef = new RosMessageDefinition(subSentences, this.KnownDefinitions);
                    if (!this.KnownDefinitions.ContainsKey(loopRosDef.Type))
                    {
                        this.KnownDefinitions.Add(loopRosDef.Type, loopRosDef);
                    }
                    sentences.RemoveRange(0, sentences.IndexOf(definitionSplit) + 1);
                }
                var newRosDef = new RosMessageDefinition(sentences, this.KnownDefinitions);
                if (!this.KnownDefinitions.ContainsKey(newRosDef.Type))
                {
                    this.KnownDefinitions.Add(newRosDef.Type, newRosDef);
                }
            }
            else
            {
                this.ParseSingleDefinitonText(sentences);
            }   
        }

        public List<string> FieldList
        {
            get
            {
                return Properties.Select(e => e.Item2).ToList();
            }
        }
        public string Type { get; private set; }

        public int GetOffset(byte[] rawData, string indexString)
        {
            int offset = 0;
            
            // Loop through each of the properties and calculate its size
            foreach(var field in this.Properties)
            {
                if (field.Item2 == indexString)
                {
                    return offset;
                }
                else
                {
                    var fieldSize = this.GetSizeOfProperty(rawData, field.Item1, offset);
                    offset += fieldSize;
                }
            }
            return -1; // Cannot find field in the Properties.
        }

        public static bool IsBuiltInType(string fieldType)
        {
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
                case "string":
                    return true;
            }
            return false;
        }

        private int GetSize(byte[] rawData, int offset)
        {
            int total_size = 0;

            // We go through each field and get the size of the properties.
            foreach (var field in this.Properties)
            {
                var fieldSize = this.GetSizeOfProperty(rawData, field.Item1, offset);
                offset += fieldSize;
                total_size += fieldSize;
            }
            return total_size;
        }


        private int GetSizeOfProperty(byte[] rawData, string fieldType, int offset)
        {
            switch (fieldType)
            {
                case "bool":
                case "int8":
                case "uint8":
                case "char":
                case "byte":
                    return 1;
                case "int16":
                case "uint16":
                    return 2;
                case "int32":
                case "uint32":
                case "float32":
                    return 4;
                case "time":
                case "duration":
                case "int64":
                case "uint64":
                case "float64":
                    return 8;
                case "header":
                case "std_msgs/Header":
                case "Header":
                    offset += 4; // Sequence ID
                    offset += 8; // time
                                 // figure out the size of the string
                    var frameStrLength = BitConverter.ToInt32(rawData, offset);
                    return 4 + 8 + frameStrLength + 4;
                case "string":
                    var strLength = BitConverter.ToInt32(rawData, offset);
                    return 4 + strLength;
                default:

                    // Check if the type is an array
                    if (fieldType.Contains("["))
                    {
                        int total_size = 0;
                        var arrayType = fieldType.Substring(0, fieldType.IndexOf('['));
                        var arrSize = 0;
                        if (fieldType.EndsWith("[]"))
                        {
                            // this is variable length array
                            arrSize = BitConverter.ToInt32(rawData, offset);
                            total_size += 4;
                        }
                        else if (fieldType.EndsWith("]"))
                        {
                            // this is a fixed length array
                            arrSize = Int32.Parse(fieldType.Substring(fieldType.IndexOf('[') + 1, fieldType.IndexOf(']') - fieldType.IndexOf('[') - 1));
                        }

                        for (var i = 0; i < arrSize; i++)
                        {
                            total_size += this.GetSizeOfProperty(rawData, arrayType, offset + total_size);
                        }
                        return total_size;
                    }
                    else
                    {
                        // Check if its a known types that were encoded at the beginning of the RosBag
                        if (this.KnownDefinitions.ContainsKey(fieldType))
                        {
                            return this.KnownDefinitions[fieldType].GetSize(rawData, offset);
                        }
                        // TODO: Check why this line exist here
                        foreach (var key in this.KnownDefinitions.Keys)
                        {
                            if (key.EndsWith(fieldType)){
                                return this.KnownDefinitions[key].GetSize(rawData, offset);
                            }
                        }
                        throw new NotImplementedException($"Unknown Type:{fieldType}");
                    }
            }
        }

        private int GetSizeOfNonArrayField(string singleFieldType){
            switch (singleFieldType)
            {
                case "bool":
                case "int8":
                case "uint8":
                case "char":
                case "byte":
                    return 1;
                case "int16":
                case "uint16":
                    return 2;
                case "int32":
                case "uint32":
                case "float32":
                    return 4;
                case "time":
                case "duration":
                case "int64":
                case "uint64":
                case "float64":
                    return 8;
                case "string":
                case "header":
                    return -1;
                default:
                    // Check if this is a nested message type
                    if(this.KnownDefinitions.ContainsKey(singleFieldType)){
                        // If we haven't count it yet, count it
                        if (!this.KnownDefinitions[singleFieldType].PreCalculatedFieldSize){
                            this.KnownDefinitions[singleFieldType].PreCalculateOffsets();
                        }
                        // check if it has a fixed size
                        if (this.KnownDefinitions[singleFieldType].HasStaticSize){
                            return this.KnownDefinitions[singleFieldType].MessageSize;
                        }
                    }
                    return -1;
            }
        }

        
        // Whether this ROS Message has a static field size or not
        public bool HasStaticSize { get; private set; } = false;
        public bool PreCalculatedFieldSize { get; private set; } = false;
        public int MessageSize { get; private set; } = -1;
        public Dictionary<string, int> PropertyOffsets {get; private set;} = new Dictionary<string, int>();

        public bool PreCalculateOffsets(){

            // check if we already pre-calculated this field.
            if (this.PreCalculatedFieldSize){
                return this.HasStaticSize;
            }

            // mark that we have start precalculating the message size.
            this.PreCalculatedFieldSize = true;   

            int offset = 0;
            foreach(var properties in this.Properties){
                // Add the current property to the list
                PropertyOffsets.Add(properties.Item2, offset);

                // Now we calculate the offsets
                // First check if its an array
                if(properties.Item1.Contains("[")){
                    // check if its a varied array or fixed size one
                    if (properties.Item1.Contains("[]")){
                        // The size could only be determined at runtime.
                        this.HasStaticSize = false;
                        return false;
                    }
                    else{
                        // get the length of array
                        var arrSize = Int32.Parse(properties.Item1.Substring(properties.Item1.IndexOf('[') + 1, properties.Item1.IndexOf(']') - properties.Item1.IndexOf('[') - 1));
                        var singleFieldType = properties.Item1.Substring(0, properties.Item1.IndexOf('['));

                        int fieldSize = this.GetSizeOfNonArrayField(singleFieldType);
                        if(fieldSize == -1){
                            // The size could only be determined at runtime.
                            this.HasStaticSize = false;
                            return false;
                        }
                        offset += (fieldSize * arrSize);
                    }
                }
                else{
                    // Not an array
                    // Check if it has a fixed message size
                    var fieldSize = this.GetSizeOfNonArrayField(properties.Item1);
                    // make sure it has a valid size
                    if (fieldSize == -1)
                    {
                        // The size could only be determined at runtime.
                        this.HasStaticSize = false;
                        return false;
                    }
                }
            }
            this.HasStaticSize = true;
            return true;
        }


        public Tuple<string, byte[]> GetData(byte[] rawData, string indexString)
        {
            // Get the field type of the index string.
            var fieldType = this.Properties.Where(e => e.Item2 == indexString).First().Item1;

            // find the offset to the field in the given mesage data.
            var offset = this.GetOffset(rawData, indexString);

            // Calculate the field size of the data
            var fieldSize = this.GetSizeOfProperty(rawData, fieldType, offset);

            // extract and return the bytes of the field.
            return new Tuple<string, byte[]>(fieldType, rawData.Skip(offset).Take(fieldSize).ToArray());
        }

        public string GetFieldType(string indexString)
        {
            return this.Properties.Where(e => e.Item2 == indexString).First().Item1;
        }
        
    }
}
