using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using RosBagConverter;

namespace RosBagConverterTests
{
    [TestClass]
    public class TestParsingRosBag
    {
        [TestMethod]
        public void TestSimpleRosBag()
        {
            var pathToFile = "psi_simple.bag";
            var bag = new RosBag(pathToFile);
            // validate the bag information
            Assert.AreEqual(bag.TopicList.Count, 2);
            string[] expectedTopicList = { "/rosout", "/text" };
            CollectionAssert.AreEqual(bag.TopicList, expectedTopicList);
            Assert.AreEqual(bag.MessageCounts["/text"], 56 );
        }

        [TestMethod]
        public void TestMultipleBags()
        {
            var bag = new RosBag("TestBags");
            Assert.AreEqual(bag.TopicList.Count, 2);
            string[] expectedTopicList = { "/usb_cam/image_raw", "/usb_cam/image_raw/compressed" };
            CollectionAssert.AreEqual(bag.TopicList.OrderBy(x => x).ToList(), expectedTopicList.OrderBy(x => x).ToList());
            Assert.AreEqual(bag.MessageCounts["/usb_cam/image_raw"], 24 );            
        }
    }
}
 