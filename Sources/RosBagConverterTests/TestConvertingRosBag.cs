using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using RosBagConverter;

namespace RosBagConverterTests
{
    [TestClass]
    public class TestConvertingRosBag
    {
        [TestMethod]
        public void TestConvertSimpleRosBag()
        {
            var pathToFile = "psi_simple.bag";
            var bag = new RosBag(pathToFile);
            var textOutput = bag.ReadTopic("/text");
            var enumerator = textOutput.GetEnumerator();
            for(var i = 0; i < 56; i++){
                Assert.AreSame(enumerator.Current.GetField("data"), $"Text{i}");
                enumerator.MoveNext();
            }
        }

        // [TestMethod]
        // public void TestConvertImage()
        // {
        //     var bag = new RosBag("TestBags");
        //     Assert.AreEqual(bag.TopicList.Count, 2);
        //     string[] expectedTopicList = { "/usb_cam/image_raw", "/usb_cam/image_raw/compressed" };
        //     CollectionAssert.AreEqual(bag.TopicList.OrderBy(x => x).ToList(), expectedTopicList.OrderBy(x => x).ToList());
        //     Assert.AreEqual(bag.MessageCounts["/usb_cam/image_raw"], 24 );            
        // }
    }
}
 