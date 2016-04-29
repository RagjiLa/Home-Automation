using System;
using System.Net;

namespace GatewayKernel.TestingInterfaces
{
    public interface IObjectCreator:IDisposable
    {
        ITask GetTask();
        ITcpListner GetTcpListner(IPEndPoint listneingEp);
    }
}
