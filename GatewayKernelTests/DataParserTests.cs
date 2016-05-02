using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hub;
using System.Text;

namespace HubTests
{
    [TestClass]
    public class DataParserTests
    {
        [TestMethod]
        public void VerifyFromKeyValuePairs()
        {
            var kvpData = new Dictionary<string, string>();
            kvpData.Add("x", "test");
            var data = DataParser.FromKeyValuePairs(kvpData);
            var expectedData = new List<byte>();
            expectedData.Add(Encoding.UTF8.GetBytes("x")[0]);
            var encodedData = Encoding.UTF8.GetBytes("test");
            ushot
            expectedData.AddRange();
            expectedData.Add(Encoding.UTF8.GetBytes("test")[0]);
            Assert.IsTrue(ServerTests.AreSame(expectedData, data));
        }

        [TestMethod]
        public void ToKeyValuePairs()
        {
            var kvpData = new Dictionary<string, string>();
            kvpData.Add("x", "test");
            var data = DataParser.FromKeyValuePairs(kvpData);
            var actualData = new Dictionary<string, string>(DataParser.ToKeyValuePairs(data));
            Assert.IsNotNull(actualData);
            Assert.IsTrue(actualData.Count == 1);
            Assert.IsTrue(actualData.ContainsKey("x"));
            Assert.IsTrue(actualData["x"] == kvpData["x"]);
        }
    }
}
