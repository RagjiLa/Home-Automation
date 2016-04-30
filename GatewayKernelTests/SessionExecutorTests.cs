using Hub;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HubTests
{
    [TestClass]
    public class SessionExecutorTests
    {
        [TestMethod]
        public void VerifyOnlyOneThreadCanExecuteForAnyPlugin()
        {
            using (var handle = new ManualResetEvent(false))
            {
                var singleSessionPlugin = new Mock<ISingleSessionPlugin>();
                singleSessionPlugin.Setup(s => s.PostResponseProcess(It.IsAny<IEnumerable<byte>>(), It.IsAny<IEnumerable<byte>>())).Callback(() =>
                {
                    handle.Set();
                });
                using (var target = new SessionExecutor(singleSessionPlugin.Object))
                {
                    target.Respond(new byte[0]);
                    Thread predatorThread = new Thread(() =>
                    {
                        Thread.CurrentThread.Name = "Predator Thread";
                        target.PostResponseProcess(new byte[0], new byte[0]);
                    });
                    predatorThread.Start();
                    Assert.IsFalse(handle.WaitOne(TimeSpan.FromSeconds(1)), "Session broke other method executed on another thread.");
                    target.PostResponseProcess(new byte[0], new byte[0]);
                    Assert.IsTrue(handle.WaitOne(TimeSpan.FromSeconds(1)), "Session did not complete.");
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void VerifyColningFailsForSingleSessionPlugins()
        {
            var singleSessionPlugin = new Mock<ISingleSessionPlugin>();
            using (var target = new SessionExecutor(singleSessionPlugin.Object))
            {
                target.CreateNewSession();
            }
        }

        [TestMethod]
        public void VerifyDetectionOfMultiSessionPlugins()
        {
            var multipleSessionPlugin = new Mock<IMultiSessionPlugin>();
            using (var target = new SessionExecutor(multipleSessionPlugin.Object))
            {
                Assert.IsTrue(target.CanHaveMultipleSessions);
            }
        }
    }
}
