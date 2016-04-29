using System.Threading;

namespace GatewayKernel.TestingInterfaces
{
    public interface ITcpListner
    {
        void Start(int backlog);
        void Stop();
        ISocket AcceptSocket(CancellationToken token);
        bool Pending();
    }
}
