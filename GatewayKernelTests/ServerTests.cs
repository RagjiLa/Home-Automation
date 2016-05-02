using Hub;
using Hub.TestingInterfaces;
using Kernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace HubTests
{
    [TestClass]
    public class ServerTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void VerifyStartCannotBeCalledTwice()
        {
            var creator = new Mock<IObjectCreator>();
            var task = new Mock<ITask>();
            var endpoint = new IPEndPoint(IPAddress.Any, 9000);

            creator.Setup(c => c.GetTask()).Returns(task.Object);
            using (var target = new Server(creator.Object))
            {
                target.StartDispatching(endpoint, new List<ISingleSessionPlugin>());
                target.StartDispatching(endpoint, new List<ISingleSessionPlugin>());
            }
        }

        [TestMethod]
        public void VerifyPacketWhenInvalidByteIsReturned()
        {
            var connectionLoopCounter = 0;
            var creator = new Mock<IObjectCreator>();
            var task = new Mock<ITask>();
            var tcplistner = new Mock<ITcpListner>();
            var clientSocket = new Mock<ISocket>();
            var mockPlugin = new Mock<ISingleSessionPlugin>();
            var endpoint = new IPEndPoint(IPAddress.Any, 9000);
            List<ISingleSessionPlugin> plugins = new List<ISingleSessionPlugin>();
            List<byte> mockPacket = new List<byte>();

            using (var target = new Server(creator.Object))
            {
                task.Setup(t => t.Run(It.IsAny<Action>(), It.IsAny<string>())).Callback<Action, string>((a, s) => a());
                creator.Setup(c => c.GetTask()).Returns(task.Object);
                tcplistner.Setup(t => t.AcceptSocket(It.IsAny<CancellationToken>())).Returns<CancellationToken>(
                    token =>
                    {
                        if (token.IsCancellationRequested || connectionLoopCounter >= 1)
                            return null;

                        connectionLoopCounter++;
                        return clientSocket.Object;
                    });
                mockPlugin.SetupGet(p => p.Name).Returns(PluginName.PlantManagerPlugin);
                plugins.Add(mockPlugin.Object);
                mockPacket.Add(0xFF);
                creator.Setup(c => c.GetTcpListner(It.Is<IPEndPoint>(e => e.Equals(endpoint)))).Returns(tcplistner.Object);
                clientSocket.SetupGet(c => c.RemoteEndPoint).Returns(endpoint);
                clientSocket.Setup(c => c.Receive(It.Is<byte[]>(b => b.Length == 1024))).Returns<byte[]>(r =>
                {
                    r[0] = mockPacket.ToArray()[0];
                    return 0;
                });


                target.StartDispatching(endpoint, plugins);
                tcplistner.Verify(t => t.Start(10), Times.Once);
                tcplistner.Verify(t => t.Stop(), Times.Once);
            }

            mockPlugin.Verify(p => p.PostResponseProcess(It.IsAny<ISample>(), It.IsAny<IEnumerable<byte>>(), It.IsAny<MessageBus>()), Times.Never);
            mockPlugin.Verify(p => p.Respond(It.IsAny<ISample>()), Times.Never);
        }

        [TestMethod]
        public void VerifyPluginIsInvokedWhenValidRequestAndResponseIsGenerated()
        {
            //SOP 
            //Header(1) DataLen(2) Data(X)
            //H(1)         HeaderDATA
            //D(1)         DataPlayload
            //C(1)         CRCDATA(4)  
            var connectionLoopCounter = 0;
            var creator = new Mock<IObjectCreator>();
            var task = new Mock<ITask>();
            var tcplistner = new Mock<ITcpListner>();
            var clientSocket = new Mock<ISocket>();
            var mockPlugin = new Mock<ISingleSessionPlugin>();
            var endpoint = new IPEndPoint(IPAddress.Any, 9000);
            var plugins = new List<ISingleSessionPlugin>();
            var mockPacket = new List<byte>();
            var responsePacket = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
            var plugin = PluginName.DweetPlugin;
            var testsample = new TestSample();
            var kvp = new Dictionary<string, string>();
            kvp.Add("T", "Tag");
            kvp.Add("D", "{\"f\":\"ggg\"}");
            var dataBytes = DataParser.FromKeyValuePairs(kvp).ToArray();
            mockPlugin.SetupGet(p => p.AssociatedSample).Returns(testsample);

            using (var target = new Server(creator.Object))
            {
                mockPlugin.Setup(p => p.Respond(It.Is<ISample>(sample => AreSame(dataBytes, sample.ToByteArray())))).Returns(responsePacket).Verifiable();
                task.Setup(t => t.Run(It.IsAny<Action>(), It.IsAny<string>())).Callback<Action, string>((a, s) => a());
                creator.Setup(c => c.GetTask()).Returns(task.Object);
                tcplistner.Setup(t => t.AcceptSocket(It.IsAny<CancellationToken>())).Returns<CancellationToken>(
                    token =>
                    {
                        if (token.IsCancellationRequested || connectionLoopCounter >= 1)
                            return null;

                        connectionLoopCounter++;
                        return clientSocket.Object;
                    });
                mockPlugin.SetupGet(p => p.Name).Returns(plugin);
                plugins.Add(mockPlugin.Object);
                creator.Setup(c => c.GetTcpListner(It.Is<IPEndPoint>(e => e.Equals(endpoint)))).Returns(tcplistner.Object);
                clientSocket.SetupGet(c => c.RemoteEndPoint).Returns(endpoint);
                clientSocket.Setup(c => c.Receive(It.Is<byte[]>(b => b.Length == 1024))).Returns<byte[]>(r =>
                {
                    int ctr = 0;
                    foreach (var dataByte in mockPacket.ToArray())
                    {
                        r[ctr] = dataByte;
                        ctr++;
                    }
                    return ctr;
                });
                clientSocket.Setup(c => c.Send(It.IsAny<byte[]>())).Returns<byte[]>(data => data.Length);

                mockPacket.AddRange(DataParser.GeneratePacket(plugin, kvp));

                target.StartDispatching(endpoint, plugins);
                tcplistner.Verify(t => t.Start(10), Times.Once);
                tcplistner.Verify(t => t.Stop(), Times.Once);
            }

            mockPlugin.Verify(p => p.PostResponseProcess(It.Is<ISample>(sample => AreSame(dataBytes, sample.ToByteArray())), It.Is<IEnumerable<byte>>(response => AreSame(responsePacket, response)), It.IsAny<MessageBus>()), Times.Once);
            mockPlugin.VerifyAll();
        }

        [TestMethod]
        public void VerifyPluginIsNotInvokedWhenSendingFails()
        {
            //SOP 
            //Header(1) DataLen(2) Data(X)
            //H(1)         HeaderDATA
            //D(1)         DataPlayload
            //C(1)         CRCDATA(4)  
            var connectionLoopCounter = 0;
            var creator = new Mock<IObjectCreator>();
            var task = new Mock<ITask>();
            var tcplistner = new Mock<ITcpListner>();
            var clientSocket = new Mock<ISocket>();
            var mockPlugin = new Mock<ISingleSessionPlugin>();
            var endpoint = new IPEndPoint(IPAddress.Any, 9000);
            var plugins = new List<ISingleSessionPlugin>();
            var mockPacket = new List<byte>();
            var plugin = PluginName.DweetPlugin;
            //var packetWritter = new BinaryWriter(mockPacket);
            var responsePacket = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
            //var headerBytes = Encoding.UTF8.GetBytes(PluginName.PlantManagerPlugin.ToString());
            var testsample = new TestSample();
            var kvp = new Dictionary<string, string>();
            kvp.Add("T", "Tag");
            kvp.Add("D", "{\"f\":\"ggg\"}");
            var dataBytes = DataParser.FromKeyValuePairs(kvp).ToArray();
            mockPlugin.SetupGet(p => p.AssociatedSample).Returns(testsample);

            using (var target = new Server(creator.Object))
            {
                mockPlugin.Setup(p => p.Respond(It.Is<ISample>(sample => AreSame(dataBytes, sample.ToByteArray())))).Returns(responsePacket).Verifiable();
                task.Setup(t => t.Run(It.IsAny<Action>(), It.IsAny<string>())).Callback<Action, string>((a, s) => a());
                creator.Setup(c => c.GetTask()).Returns(task.Object);
                tcplistner.Setup(t => t.AcceptSocket(It.IsAny<CancellationToken>())).Returns<CancellationToken>(
                    token =>
                    {
                        if (token.IsCancellationRequested || connectionLoopCounter >= 1)
                            return null;

                        connectionLoopCounter++;
                        return clientSocket.Object;
                    });
                mockPlugin.SetupGet(p => p.Name).Returns(PluginName.DweetPlugin);
                plugins.Add(mockPlugin.Object);
                creator.Setup(c => c.GetTcpListner(It.Is<IPEndPoint>(e => e.Equals(endpoint)))).Returns(tcplistner.Object);
                clientSocket.SetupGet(c => c.RemoteEndPoint).Returns(endpoint);
                clientSocket.Setup(c => c.Receive(It.Is<byte[]>(b => b.Length == 1024))).Returns<byte[]>(r =>
                {
                    int ctr = 0;
                    foreach (var dataByte in mockPacket.ToArray())
                    {
                        r[ctr] = dataByte;
                        ctr++;
                    }
                    return ctr;
                });
                clientSocket.Setup(c => c.Send(It.IsAny<byte[]>())).Returns<byte[]>(data => data.Length - 1);

                mockPacket.AddRange(DataParser.GeneratePacket(plugin, kvp));

                target.StartDispatching(endpoint, plugins);
                tcplistner.Verify(t => t.Start(10), Times.Once);
                tcplistner.Verify(t => t.Stop(), Times.Once);
            }

            mockPlugin.Verify(p => p.PostResponseProcess(It.IsAny<ISample>(), It.IsAny<IEnumerable<byte>>(), It.IsAny<MessageBus>()), Times.Never);
            mockPlugin.VerifyAll();
        }

        public static bool AreSame(IEnumerable<byte> expected, IEnumerable<byte> actual)
        {
            var expectedlst = expected.ToList();
            var actuallst = actual.ToList();
            if (expectedlst.Count() == actuallst.Count())
            {
                bool returnValue = true;
                for (var ctr = 0; ctr < expectedlst.Count; ctr++)
                    returnValue = actuallst[ctr] == expectedlst[ctr] & returnValue;
                return returnValue;
            }
            return false;
        }
    }

    public class TestSample : ISample
    {
        private IDictionary<string, string> _cache;

        public IDictionary<string, string> ToKeyValuePair()
        {
            return _cache;
        }

        public void FromKeyValuePair(IDictionary<string, string> kvpData)
        {
            _cache = kvpData;
        }
    }
}
