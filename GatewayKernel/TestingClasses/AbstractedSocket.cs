using System;
using GatewayKernel.TestingInterfaces;
using System.Net;
using System.Net.Sockets;

namespace GatewayKernel.TestingClasses
{
    public class AbstractedSocket : ISocket
    {
        public EndPoint RemoteEndPoint { get { return _activeSocket.RemoteEndPoint; } }
        private readonly Socket _activeSocket;

        public AbstractedSocket(Socket liveSocket)
        {
            if (liveSocket == null) throw new ArgumentNullException("liveSocket");
            _activeSocket = liveSocket;
        }

        public int Receive(byte[] buffer)
        {
            return _activeSocket.Receive(buffer);
        }

        public int Send(byte[] buffer)
        {
            return _activeSocket.Send(buffer);
        }

        public void Dispose()
        {
            if (_activeSocket != null) _activeSocket.Dispose();
        }
    }
}
