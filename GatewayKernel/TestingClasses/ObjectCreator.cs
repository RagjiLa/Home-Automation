using GatewayKernel.TestingInterfaces;
using System.Net;

namespace GatewayKernel.TestingClasses
{
    public class ObjectCreator : IObjectCreator
    {
        private ITask _tasker;
        private ITcpListner _tcpListner;

        public ITask GetTask()
        {
            return _tasker ?? (_tasker = new AbstractedTask());
        }

        public ITcpListner GetTcpListner(IPEndPoint listneingEp)
        {
            return _tcpListner ?? (_tcpListner = new AbstractedTcpListner(listneingEp));
        }

        public void Dispose()
        {
            
        }
    }
}
