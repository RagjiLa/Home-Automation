using GatewayKernel.TestingInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace GatewayKernel
{
    public class RequestDispatcher : IDisposable
    {
        private Dictionary<string, IDispatcherPlugin> _responders = new Dictionary<string, IDispatcherPlugin>();
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private ManualResetEvent _active = new ManualResetEvent(true);
        private readonly IObjectCreator _creator;
        private const byte Sop = 0xFF;

        public RequestDispatcher(IObjectCreator creator)
        {
            _creator = creator;
        }

        public void StartDispatching(IPEndPoint listeningEndpoint, IEnumerable<IDispatcherPlugin> plugins)
        {
            if (_active.WaitOne(1))
            {
                _active.Reset();
                _responders = plugins.ToDictionary(k => k.Name.ToLower());
                _creator.GetTask().Run(() => ConnectionLoop(listeningEndpoint), "Connection Loop");
            }
            else
            {
                throw new InvalidOperationException("Dispatcher is already active");
            }
        }

        private void ConnectionLoop(IPEndPoint listeningEndpoint)
        {
            var coreSocket = _creator.GetTcpListner(listeningEndpoint);
            try
            {
                coreSocket.Start(10);
                while (!_tokenSource.Token.IsCancellationRequested || coreSocket.Pending())
                {
                    var clientSocket = coreSocket.AcceptSocket(_tokenSource.Token);
                    if (clientSocket == null)
                        break;
                    _creator.GetTask().Run(() => ServeConnection(clientSocket), clientSocket.RemoteEndPoint.ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.Error(new Exception("Connection Loop", ex));
            }
            finally
            {
                _tokenSource.Dispose();
                _tokenSource = null;
                coreSocket.Stop();
            }
        }

        private void ServeConnection(ISocket connection)
        {
            try
            {
                IDispatcherPlugin activeHandler = null;
                List<byte> responseData = null;
                List<byte> requestData = null;
                using (connection)
                {
                    byte[] databuff = new byte[1024];
                    int bytesRead = connection.Receive(databuff);
                    if (bytesRead > 1)
                    {
                        //SOP(1) LENHeader(4) HEADER LENData(4) DATA LENCrc(4) CRC
                        using (var dataMs = new MemoryStream(databuff))
                        using (var dataReader = new BinaryReader(dataMs))
                        {
                            if (IsStartOfPacket(dataReader.ReadByte()))
                            {
                                var headerlength = dataReader.ReadUInt32();
                                var packetheader = Encoding.UTF8.GetString(dataReader.ReadBytes((int)headerlength)).ToLower();
                                var datalength = dataReader.ReadUInt32();
                                requestData = new List<byte>(dataReader.ReadBytes((int)datalength));
                                var crclength = dataReader.ReadUInt32();
                                var crc = dataReader.ReadBytes((int)crclength);
                                if (IsCrcValid(databuff, crc, bytesRead))
                                {
                                    if (_responders.ContainsKey(packetheader))
                                    {
                                        activeHandler = _responders[packetheader];
                                        responseData = new List<byte>(activeHandler.Respond(requestData));
                                        if (connection.Send(responseData.ToArray()) != responseData.Count())
                                        {
                                            activeHandler = null;
                                            Logger.Error(connection.RemoteEndPoint + " Sending data failed");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error(connection.RemoteEndPoint + " No handlers for " + packetheader);
                                    }
                                }
                                else
                                {
                                    Logger.Error(connection.RemoteEndPoint + " Invalid CRC (Invalid Packet)");
                                }
                            }
                            else
                            {
                                Logger.Error(connection.RemoteEndPoint + " No SOP Byte found (Invalid Packet)");
                            }
                        }
                    }
                    else
                    {
                        Logger.Error(connection.RemoteEndPoint + " No bytes received (Invalid Packet) ");
                    }
                }
                if (activeHandler != null)
                    activeHandler.PostResponseProcess(requestData, responseData);
            }
            catch (Exception ex)
            {
                Logger.Error(new Exception("Serve Connection", ex));
            }
        }

        private static bool IsCrcValid(byte[] databuff, byte[] crc, int length)
        {
            using (var reader = new BinaryReader(new MemoryStream(crc), Encoding.UTF8, true))
            {
                return length == reader.ReadUInt32();
            }
        }

        private static bool IsStartOfPacket(int value)
        {
            return value == Sop;
        }

        public void Dispose()
        {
            if (_active != null)
            {
                _active.Set();
                _active.Close();
                _active.Dispose();
                _active = null;
            }
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
            }
            _responders.Clear();
        }
    }
}
