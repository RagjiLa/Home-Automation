using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hub;
using System.Text;
using Kernel;
using System.IO;

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
            var expectedData = new byte[0];
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8))
            {
                foreach (var kvp in kvpData)
                {
                    var encodedData = Encoding.UTF8.GetBytes(kvp.Value);
                    writer.Write(kvp.Key[0]);
                    writer.Write((ushort)encodedData.Length);
                    writer.Write(encodedData);
                }
                expectedData = memoryStream.ToArray();
            }
            var actualData = DataParser.FromKeyValuePairs(kvpData);
            Assert.IsTrue(ServerTests.AreSame(expectedData, actualData));
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
