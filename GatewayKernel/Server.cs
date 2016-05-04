using Hub.TestingInterfaces;
using Kernel;
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
        private ManualResetEvent _isDispatchingActive = new ManualResetEvent(true);
        private readonly IObjectCreator _creator;
        private const byte Sop = 0xFF;

        public Server(IObjectCreator creator)
        {
            _creator = creator;
        }

        public void StartDispatching(IPEndPoint listeningEndpoint, IEnumerable<ISingleSessionPlugin> plugins)
        {
            if (_isDispatchingActive.WaitOne(1))
            {
                _isDispatchingActive.Reset();
                var threadSafePlugins = plugins.Select(p => new SessionExecutor(p));
                _responders = threadSafePlugins.ToDictionary(k => k.Name.ToString());
                _creator.GetTask().Run(() => ListeningLoop(listeningEndpoint), "Listening Loop");
            }
            else
            {
                throw new InvalidOperationException("Dispatcher is already active");
            }
        }

        public bool StopDispatching(TimeSpan spanTowaitFor)
        {
            if (_tokenSource != null) _tokenSource.Cancel();
            return _isDispatchingActive.WaitOne(spanTowaitFor);
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
                    _creator.GetTask()
                        .Run(() => ServeConnection(clientSocket, _responders), clientSocket.RemoteEndPoint.ToString());
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
                foreach (var handler in _responders)
                {
                    handler.Value.ShutDown();
                    handler.Value.Dispose();
                }
                _responders.Clear();
                _isDispatchingActive.Set();
            }
        }

        private static void ServeConnection(ISocket connection, Dictionary<string, SessionExecutor> responders)
        {
            try
            {
                Dictionary<string, SessionExecutor> respondersThreadSafeCopy = responders;
                ISocket connectionThreadSafeCopy = connection;
                using (connectionThreadSafeCopy)
                {
                    byte[] databuff = new byte[1024];
                    int bytesRead = connectionThreadSafeCopy.Receive(databuff);
                    if (bytesRead > 1)
                    {
                        //SOP 
                        //Header(1) DataLen(2) Data(X)
                        //H(1)         HeaderDATA
                        //D(1)         DataPlayload
                        //C(1)         CRCDATA(4)  
                        using (var dataReader = new BinaryReader(new MemoryStream(databuff), Encoding.UTF8, false))
                        {
                            if (IsStartOfPacket(dataReader.ReadByte()))
                            {
                                var parsedPacket = DataParser.ToKeyValuePairsBinary(databuff.Skip(1).Take(bytesRead - 1));
                                if (parsedPacket.ContainsKey("C"))
                                {
                                    if (IsCrcValid(parsedPacket["C"], bytesRead))
                                    {
                                        if (parsedPacket.ContainsKey("H"))
                                        {
                                            var packetheader = Encoding.UTF8.GetString(parsedPacket["H"].ToArray());
                                            if (parsedPacket.ContainsKey("D"))
                                            {
                                                var requestData = parsedPacket["D"];
                                                if (respondersThreadSafeCopy.ContainsKey(packetheader))
                                                {
                                                    var activeHandler = respondersThreadSafeCopy[packetheader];
                                                    activeHandler = activeHandler.CanHaveMultipleSessions ? activeHandler.CreateNewSession() : activeHandler;
                                                    var requestSample = activeHandler.AssociatedSample;
                                                    requestSample.FromByteArray(requestData);
                                                    var messageBus = new MessageBus(respondersThreadSafeCopy, activeHandler.Name);
                                                    var sendResponse =
                                                        new Action<IEnumerable<byte>>((response) =>
                                                        {
                                                            if (response != null)
                                                            {
                                                                var responseData = new List<byte>(response);
                                                                if (connectionThreadSafeCopy.Send(responseData.ToArray()) != responseData.Count())
                                                                {
                                                                    Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " Sending data failed");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No response was generated from handler or is disposed");
                                                            }

                                                        });
                                                    activeHandler.Invoke(requestSample, sendResponse, messageBus);
                                                }
                                                else
                                                {
                                                    Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No handlers for " + packetheader);
                                                }
                                            }
                                            else
                                            {
                                                Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No data info (Invalid Packet)");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No handler info (Invalid Packet)");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " Invalid CRC (Invalid Packet)");
                                    }
                                }
                                else
                                {
                                    Logger.Error(connectionThreadSafeCopy.RemoteEndPoint + " No CRC Info present (Invalid Packet)");
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
            }
            catch (Exception ex)
            {
                Logger.Error(new Exception("Serve Connection", ex));
            }
        }

        private static bool IsCrcValid(IEnumerable<byte> crcData, int bytesread)
        {
            //TODO:Implement CRC
            return BitConverter.ToInt32(crcData.ToArray(), 0) == bytesread;
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
                StopDispatching(TimeSpan.FromSeconds(30));

                if (_isDispatchingActive != null)
                {
                    _isDispatchingActive.Set();
                    _isDispatchingActive.Close();
                    _isDispatchingActive.Dispose();
                    _isDispatchingActive = null;
                }
                if (_tokenSource != null)
                {
                    _tokenSource.Dispose();
                }
            }

            // free native resources if there are any.
        }
    }
}
