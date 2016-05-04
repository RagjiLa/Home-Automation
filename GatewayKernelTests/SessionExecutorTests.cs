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
        //[TestMethod]
        //public void VerifyOnlyOneThreadCanExecuteForAnyPlugin()
        //{
        //    using (var proceedHandle = new ManualResetEvent(false))
        //    {
        //         using (var probe = new ManualResetEvent(false))
        //        {
        //            using (var probe2 = new ManualResetEvent(false))
        //            {
        //                var singleSessionPlugin = new Mock<ISingleSessionPlugin>();
        //                var mockSample = new Mock<ISample>();
        //                //mockSample.Setup(s=>s.FromKeyValuePair )
        //                singleSessionPlugin.Setup(
        //                    s =>
        //                        s.Invoke(It.IsAny<ISample>(), It.IsAny<Action<IEnumerable<byte>>>(),
        //                            It.IsAny<MessageBus>()))
        //                    .Callback<ISample, Action<IEnumerable<byte>>, MessageBus>((a, b, c) =>
        //                    {
        //                        b(new byte[0]);
        //                    });
        //                using (var target = new SessionExecutor(singleSessionPlugin.Object))
        //                {
        //                    target.Invoke(mockSample.Object, (b) =>
        //                    {
        //                        probe.Set();
        //                        proceedHandle.WaitOne(10000);
        //                    }, null);
        //                    Thread predatorThread = new Thread(() =>
        //                    {
        //                        Thread.CurrentThread.Name = "Predator Thread";
        //                        target.Invoke(mockSample.Object, (b) =>
        //                        {
        //                            probe2.Set();
        //                            proceedHandle.WaitOne(10000);
        //                        }, null);
        //                    });
        //                    predatorThread.Start();
        //                    Assert.IsTrue(probe.WaitOne(TimeSpan.FromSeconds(1)));
        //                    Assert.IsFalse(probe2.WaitOne(TimeSpan.FromSeconds(1)));
        //                    proceedHandle.Set();
        //                    Assert.IsTrue(probe2.WaitOne(TimeSpan.FromSeconds(1)));
        //                }
        //            }
        //        }
        //    }
        //}

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
