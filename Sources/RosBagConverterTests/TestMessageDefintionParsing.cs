using Microsoft.VisualStudio.TestTools.UnitTesting;
using RosBagConverter;
using System.Collections.Generic;

namespace RosBagConverterTests
{
    [TestClass]
    public class TestMessageDefintionParsing
    {

        [TestMethod]
        public void TestMessageDefinitionProperties()
        {
            var testDefText = "string text";
            var typeName = "string";
            var msgDef = new RosMessageDefinition(typeName, testDefText, new Dictionary<string, RosMessageDefinition>());

            Assert.AreEqual(msgDef.Type, typeName);
            Assert.AreEqual(msgDef.FieldList[0], "text");
            Assert.AreEqual(msgDef.GetFieldType("text"), "string");
        }


        [TestMethod]
        public void TestSingleMessageDefition()
        {
            var testDefText = @"# Standard metadata for higher-level stamped data types.
# This is generally used to communicate timestamped data 
# in a particular coordinate frame.
# 
# sequence ID: consecutively increasing ID 
uint32 seq
#Two-integer timestamp that is expressed as:
# * stamp.sec: seconds (stamp_secs) since epoch (in Python the variable is called 'secs')
# * stamp.nsec: nanoseconds since stamp_secs (in Python the variable is called 'nsecs')
# time-handling sugar is provided by the client library
time stamp
#Frame this data is associated with
string frame_id
";
            var typeName = "string";
            var msgDef = new RosMessageDefinition(typeName, testDefText, new Dictionary<string, RosMessageDefinition>());

            Assert.AreEqual(msgDef.FieldList.Count, 3);
            Assert.AreEqual(msgDef.GetFieldType("stamp"), "time");
            Assert.AreEqual(msgDef.GetFieldType("seq"), "uint32");
            Assert.AreEqual(msgDef.GetFieldType("frame_id"), "string");
        }

        [TestMethod]
        public void TestNestedMessageDefition()
        {
            var testDefText1 = "string text";
            var typeName1 = "string_msgs";
            var msgDef1 = new RosMessageDefinition(typeName1, testDefText1, new Dictionary<string, RosMessageDefinition>());

            var testDefText2 = "int seq\n string_msgs next ";
            var msgDef2 = new RosMessageDefinition("someMessage", testDefText2, msgDef1.KnownMsgDefinitions);

            Assert.AreEqual(msgDef2.GetFieldType("next"), "string_msgs");
            Assert.AreEqual(msgDef2.FieldList.Count, 2);
        }
    }
}
