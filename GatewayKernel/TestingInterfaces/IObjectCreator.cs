using System;
using System.Net;

namespace Hub.TestingInterfaces
{
    public interface IObjectCreator:IDisposable
    {
        ITask GetTask();
        ITcpListner GetTcpListner(IPEndPoint listneingEp);
    }
}
