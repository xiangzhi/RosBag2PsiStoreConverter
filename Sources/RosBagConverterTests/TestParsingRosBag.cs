using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
