using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    public class Utils
    {
        public static Dictionary<string, byte[]> ParseHeaderData(byte[] headerBytes)
        {
            var fieldProperties = new Dictionary<string, byte[]>();

            // TODO: Rewrite this to use offsets instead of copying arrays.
            while (headerBytes.Length > 0)
            {
                var fieldLen = BitConverter.ToInt32(headerBytes, 0);
                var fieldValuePair = headerBytes.Skip(4).Take(fieldLen).ToArray();
                int cutoffIndex = Array.IndexOf(fieldValuePair, (byte)61);
                var fieldName = Encoding.UTF8.GetString(fieldValuePair, 0, cutoffIndex);
                fieldProperties.Add(fieldName, fieldValuePair.Skip(cutoffIndex + 1).ToArray());

                headerBytes = headerBytes.Skip(4 + fieldLen).ToArray();
            }
            return fieldProperties;
        }
    }
}
