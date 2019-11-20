using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class RosMessageDefinition
    {
        private Dictionary<string, string> PropertyTypeMap = new Dictionary<string, string>();
        private List<Tuple<string, string>> Properties = new List<Tuple<string, string>>();
        public Dictionary<string, RosMessageDefinition> KnownDefinitions;

        public RosMessageDefinition(string typeName, string definition, Dictionary<string, RosMessageDefinition> knownDef)
        {
            this.Type = typeName;
            this.KnownDefinitions = knownDef;
            this.parseDefinitionText(definition);

        }
        public RosMessageDefinition(List<string> definition)
        {
            // the first line tells us the typ
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

                // split the sentence by space
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
                this.ParseSingleDefinitonText(sentences.Take(sentences.IndexOf(definitionSplit)).ToList());
                sentences.RemoveRange(0, sentences.IndexOf(definitionSplit) + 1);

                while (sentences.Contains(definitionSplit))
                {
                    var subSentences = sentences.Take(sentences.IndexOf(definitionSplit)).ToList();
                    var loopRosDef = new RosMessageDefinition(subSentences);
                    if (!this.KnownDefinitions.ContainsKey(loopRosDef.Type))
                    {
                        this.KnownDefinitions.Add(loopRosDef.Type, loopRosDef);
                    }
                    sentences.RemoveRange(0, sentences.IndexOf(definitionSplit) + 1);
                }
                var newRosDef = new RosMessageDefinition(sentences);
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
            int offset = 0; //This is the size of the message
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
                        if (this.KnownDefinitions.ContainsKey(fieldType))
                        {
                            return this.KnownDefinitions[fieldType].GetSize(rawData, offset);
                        }
                        foreach (var key in this.KnownDefinitions.Keys)
                        {
                            if (key.EndsWith(fieldType)){
                                return this.KnownDefinitions[key].GetSize(rawData, offset);
                            }
                        }
                        throw new NotImplementedException("Unknown Type!!");
                    }
            }
        }

        public Tuple<string, byte[]> GetData(byte[] rawData, string indexString)
        {
            var fieldType = this.Properties.Where(e => e.Item2 == indexString).First().Item1;
            var offset = this.GetOffset(rawData, indexString);
            var fieldSize = this.GetSizeOfProperty(rawData, fieldType, offset);

            return new Tuple<string, byte[]>(fieldType, rawData.Skip(offset).Take(fieldSize).ToArray());
        }

        public string GetFieldType(string indexString)
        {
            return this.Properties.Where(e => e.Item2 == indexString).First().Item1;
        }
    }
}
