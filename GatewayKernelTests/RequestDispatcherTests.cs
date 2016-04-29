using GatewayKernel;
using GatewayKernel.TestingInterfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace GatewayKernelTests
{
    [TestClass]
    public class RequestDispatcherTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void VerifyStartCannotBeCalledTwice()
        {
            var creator = new Mock<IObjectCreator>();
            var task = new Mock<ITask>();
            var endpoint = new IPEndPoint(IPAddress.Any, 9000);
            var emptyPlugins = new List<ISingleSessionPlugin>();

            creator.Setup(c => c.GetTask()).Returns(task.Object);
            using (var target = new RequestDispatcher(creator.Object))
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

            using (var target = new RequestDispatcher(creator.Object))
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
                mockPlugin.SetupGet(p => p.Name).Returns("Mocked");
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

            mockPlugin.Verify(p => p.PostResponseProcess(It.IsAny<IEnumerable<byte>>(), It.IsAny<IEnumerable<byte>>()), Times.Never);
            mockPlugin.Verify(p => p.Respond(It.IsAny<IEnumerable<byte>>()), Times.Never);
        }

        [TestMethod]
        public void VerifyPluginIsInvokedWhenValidRequestAndResponseIsGenerated()
        {
            //SOP(1) LENPack(4) LENHeader(4) HEADER LENData(4) DATA LENCrc(4) CRC
            var connectionLoopCounter = 0;
            var creator = new Mock<IObjectCreator>();
            var task = new Mock<ITask>();
            var tcplistner = new Mock<ITcpListner>();
            var clientSocket = new Mock<ISocket>();
            var mockPlugin = new Mock<ISingleSessionPlugin>();
            var endpoint = new IPEndPoint(IPAddress.Any, 9000);
            var plugins = new List<ISingleSessionPlugin>();
            var mockPacket = new MemoryStream(1024);
            var packetWritter = new BinaryWriter(mockPacket);
            var responsePacket = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
            var headerBytes = Encoding.UTF8.GetBytes("Mocked");
            var dataBytes = Encoding.UTF8.GetBytes("This is a test.");

            using (var target = new RequestDispatcher(creator.Object))
            {
                mockPlugin.Setup(p => p.Respond(It.Is<IEnumerable<byte>>(request => AreSame(dataBytes, request)))).Returns(responsePacket).Verifiable();
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
                mockPlugin.SetupGet(p => p.Name).Returns("Mocked");
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

                GeneratePacket(packetWritter, headerBytes, dataBytes);

                target.StartDispatching(endpoint, plugins);
                tcplistner.Verify(t => t.Start(10), Times.Once);
                tcplistner.Verify(t => t.Stop(), Times.Once);
            }

            mockPlugin.Verify(p => p.PostResponseProcess(It.Is<IEnumerable<byte>>(request => AreSame(dataBytes, request)), It.Is<IEnumerable<byte>>(response => AreSame(responsePacket, response))), Times.Once);
            mockPlugin.VerifyAll();
        }

        [TestMethod]
        public void VerifyPluginIsNotInvokedWhenSendingFails()
        {
            //SOP(1) LENPack(4) LENHeader(4) HEADER LENData(4) DATA LENCrc(4) CRC
            var connectionLoopCounter = 0;
            var creator = new Mock<IObjectCreator>();
            var task = new Mock<ITask>();
            var tcplistner = new Mock<ITcpListner>();
            var clientSocket = new Mock<ISocket>();
            var mockPlugin = new Mock<ISingleSessionPlugin>();
            var endpoint = new IPEndPoint(IPAddress.Any, 9000);
            var plugins = new List<ISingleSessionPlugin>();
            var mockPacket = new MemoryStream(1024);
            var packetWritter = new BinaryWriter(mockPacket);
            var responsePacket = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
            var headerBytes = Encoding.UTF8.GetBytes("Mocked");
            var dataBytes = Encoding.UTF8.GetBytes("This is a test.");

            using (var target = new RequestDispatcher(creator.Object))
            {
                mockPlugin.Setup(p => p.Respond(It.Is<IEnumerable<byte>>(request => AreSame(dataBytes, request)))).Returns(responsePacket).Verifiable();
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
                mockPlugin.SetupGet(p => p.Name).Returns("Mocked");
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

                GeneratePacket(packetWritter, headerBytes, dataBytes);

                target.StartDispatching(endpoint, plugins);
                tcplistner.Verify(t => t.Start(10), Times.Once);
                tcplistner.Verify(t => t.Stop(), Times.Once);
            }

            mockPlugin.Verify(p => p.PostResponseProcess(It.IsAny<IEnumerable<byte>>(), It.IsAny<IEnumerable<byte>>()), Times.Never);
            mockPlugin.VerifyAll();
        }

        private bool AreSame(IEnumerable<byte> expected, IEnumerable<byte> actual)
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

        private static void GeneratePacket(BinaryWriter packetWritter, byte[] headerBytes, byte[] dataBytes)
        {
            uint packetLen = 0;
            packetWritter.Write((byte)0xFF); //SOP
            packetLen += 1;
            packetWritter.Write((UInt32)headerBytes.Length); //Header Length
            packetLen += 4;
            packetWritter.Write(headerBytes); //Header
            packetLen += (uint)headerBytes.Length;
            packetWritter.Write((UInt32)dataBytes.Length); //DATA Length
            packetLen += 4;
            packetWritter.Write(dataBytes); //DATA
            packetLen += (uint)dataBytes.Length;
            packetWritter.Write((UInt32)4); //CRC Length
            packetLen += 4;
            packetLen += 4;
            packetWritter.Write(packetLen); //CRC
        }
    }
}
