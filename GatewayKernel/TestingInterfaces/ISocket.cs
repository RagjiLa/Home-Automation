using System;
using System.Net;

namespace Hub.TestingInterfaces
{
    public interface ISocket : IDisposable
    {
        int Receive(byte[] buffer);
        int Send(byte[] buffer);
        EndPoint RemoteEndPoint { get; }
    }
}
