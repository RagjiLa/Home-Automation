using Hub.TestingInterfaces;
using Hub.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Hub
{
    public class Server : IDisposable
    {
        private Dictionary<string, SessionExecutor> _responders = new Dictionary<string, SessionExecutor>();
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private ManualResetEvent _IsDispatchingActive = new ManualResetEvent(true);
        private readonly IObjectCreator _creator;
        private const byte Sop = 0xFF;

        public Server(IObjectCreator creator)
        {
            _creator = creator;
        }

        public void StartDispatching(IPEndPoint listeningEndpoint, IEnumerable<ISingleSessionPlugin> plugins)
        {
            if (_IsDispatchingActive.WaitOne(1))
            {
                _IsDispatchingActive.Reset();
                var threadSafePlugins = plugins.Select(p => new SessionExecutor(p));
                _responders = threadSafePlugins.ToDictionary(k => k.Name.ToString());
                _creator.GetTask().Run(() => ListeningLoop(listeningEndpoint), "Listening Loop");
            }
            else
            {
                throw new InvalidOperationException("Dispatcher is already active");
            }
        }

        private void ListeningLoop(IPEndPoint listeningEndpoint)
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
                    _creator.GetTask().Run(() => ServeConnection(clientSocket, _responders), clientSocket.RemoteEndPoint.ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.Error(new Exception("Listening Loop", ex));
            }
            finally
            {
                _tokenSource.Dispose();
                _tokenSource = null;
                coreSocket.Stop();
                foreach (var handler in _responders) handler.Value.Dispose();
                _responders.Clear();
            }
        }

        private static void ServeConnection(ISocket connection, Dictionary<string, SessionExecutor> responders)
        {
            try
            {
                Dictionary<string, SessionExecutor> respondersThreadSafeCopy = responders;
                ISocket connectionThreadSafeCopy = connection;
                SessionExecutor activeHandler = null;
                List<byte> responseData = null;
                ISample requestSample = null;
                using (connectionThreadSafeCopy)
                {
                    byte[] databuff = new byte[1024];
                    int bytesRead = connectionThreadSafeCopy.Receive(databuff);
                    if (bytesRead > 1)
                    {
                        //SOP(1) LENHeader(4) HEADER LENData(4) DATA LENCrc(4) CRC
                        using (var dataReader = new BinaryReader(new MemoryStream(databuff), Encoding.UTF8, false))
                        {
                            if (IsStartOfPacket(dataReader.ReadByte()))
                            {
                                var headerlength = dataReader.ReadUInt32();
                                var packetheader = Encoding.UTF8.GetString(dataReader.ReadBytes((int)headerlength)).ToLower();
                                var datalength = dataReader.ReadUInt32();
                                var requestData = new List<byte>(dataReader.ReadBytes((int)datalength));
                                var crclength = dataReader.ReadUInt32();
                                var crc = dataReader.ReadBytes((int)crclength);
                                if (IsCrcValid(databuff, crc, bytesRead))
                                {
                                    if (respondersThreadSafeCopy.ContainsKey(packetheader))
                                    {
                                        activeHandler = respondersThreadSafeCopy[packetheader];
                                        activeHandler = activeHandler.CanHaveMultipleSessions ? activeHandler.CreateNewSession() : activeHandler;
                                        requestSample = activeHandler.AssociatedSample;
                                        requestSample.FromByteArray(requestData);
                                        var response = activeHandler.Respond(requestSample);
                                        if (response != null)
                                        {
                                            responseData = new List<byte>(response);
                                            if (connectionThreadSafeCopy.Send(responseData.ToArray()) != responseData.Count())
                                            {
                                                activeHandler = null;
                                                Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " Sending data failed");
                                            }
                                        }
                                        else
                                        {
                                            activeHandler = null;
                                            Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No response was generated from handler or is disposed");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No handlers for " + packetheader);
                                    }
                                }
                                else
                                {
                                    Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " Invalid CRC (Invalid Packet)");
                                }
                            }
                            else
                            {
                                Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No SOP Byte found (Invalid Packet)");
                            }
                        }
                    }
                    else
                    {
                        Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No bytes received (Invalid Packet) ");
                    }
                }
                if (activeHandler != null)
                {
                    var messageBus = new MessageBus(respondersThreadSafeCopy, activeHandler.Name);
                    activeHandler.PostResponseProcess(requestSample, responseData, messageBus);
                }
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourceCleanUp)
        {
            if (managedResourceCleanUp)
            {
                // free managed resources
                if (_IsDispatchingActive != null)
                {
                    _IsDispatchingActive.Set();
                    _IsDispatchingActive.Close();
                    _IsDispatchingActive.Dispose();
                    _IsDispatchingActive = null;
                }
                if (_tokenSource != null)
                {
                    _tokenSource.Cancel();
                    _tokenSource.Dispose();
                }
            }

            // free native resources if there are any.
        }
    }
}
