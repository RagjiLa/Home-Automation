using System;
using System.Net;

namespace GatewayKernel.TestingInterfaces
{
    public interface ISocket : IDisposable
    {
        int Receive(byte[] buffer);
        int Send(byte[] buffer);
        EndPoint RemoteEndPoint { get; }
    }
}
