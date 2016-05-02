using Kernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Hub.Utilities.Tests
{
    [TestClass()]
    public class CacheServiceTests
    {
        [TestMethod()]
        public void GetValueTestWhenKeyIsNotPresentStructType()
        {
            var target = new CacheService<int>();
            var result = target.GetValue("");
            Assert.IsTrue(result == 0);
        }

        [TestMethod()]
        public void GetValueTestWhenKeyIsNotPresentClassType()
        {
            var target = new CacheService<List<int>>();
            var result = target.GetValue("");
            Assert.IsTrue(result == null);
        }

        [TestMethod()]
        public void GetValueTestWhenKeyIsPresent()
        {
            var target = new CacheService<int>();
            target.SetValue("", 1001);
            var result = target.GetValue("");
            Assert.IsTrue(result == 1001);
        }

    }
}