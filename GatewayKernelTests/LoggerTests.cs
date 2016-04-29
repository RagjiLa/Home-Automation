using GatewayKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace GatewayKernelTests
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void VerifyErrorIsRaisingEvent()
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                string sourceString = string.Empty;
                LoggedArgs argsLoad = null;
                Logger.LogedEventHandler eventAction = (source, args) =>
                 {
                     sourceString = source.ToString();
                     argsLoad = args;
                     waitHandle.Set();
                 };
                Logger.Logged += eventAction;

                Logger.Error("Error");
                Assert.IsTrue(waitHandle.WaitOne(10), "Even was not raised");
                Assert.AreEqual("Error", argsLoad.Message);
                Assert.AreEqual(LoggedType.Error, argsLoad.Type);
                Logger.Logged -= eventAction;
            }
        }

        [TestMethod]
        public void VerifyWarnIsRaisingEvent()
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                string sourceString = string.Empty;
                LoggedArgs argsLoad = null;
                Logger.LogedEventHandler eventAction = (source, args) =>
                {
                    sourceString = source.ToString();
                    argsLoad = args;
                    waitHandle.Set();
                };
                Logger.Logged += eventAction;

                Logger.Warning("Warning");
                Assert.IsTrue(waitHandle.WaitOne(10), "Even was not raised");
                Assert.AreEqual("Warning", argsLoad.Message);
                Assert.AreEqual(LoggedType.Warning, argsLoad.Type);
                Logger.Logged -= eventAction;
            }
        }

        [TestMethod]
        public void VerifyLogIsRaisingEvent()
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                string sourceString = string.Empty;
                LoggedArgs argsLoad = null;
                Logger.LogedEventHandler eventAction = (source, args) =>
                {
                    sourceString = source.ToString();
                    argsLoad = args;
                    waitHandle.Set();
                };
                Logger.Logged += eventAction;

                Logger.Log("Log");
                Assert.IsTrue(waitHandle.WaitOne(10), "Even was not raised");
                Assert.AreEqual("Log", argsLoad.Message);
                Assert.AreEqual(LoggedType.Log, argsLoad.Type);
                Logger.Logged -= eventAction;
            }
        }

        [TestMethod]
        public void VerifyErrorExceptionIsRaisingEvent()
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                string sourceString = string.Empty;
                LoggedArgs argsLoad = null;
                Logger.LogedEventHandler eventAction = (source, args) =>
                {
                    sourceString = source.ToString();
                    argsLoad = args;
                    waitHandle.Set();
                };
                Logger.Logged += eventAction;

                Logger.Error(new InvalidCastException());
                Assert.IsTrue(waitHandle.WaitOne(10), "Even was not raised");
                Assert.AreEqual("System.InvalidCastException: Specified cast is not valid.", argsLoad.Message);
                Assert.AreEqual(LoggedType.Error, argsLoad.Type);
                Logger.Logged -= eventAction;
            }
        }
    }
}
