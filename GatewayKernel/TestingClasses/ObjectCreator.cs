using Hub.TestingInterfaces;
using System;
using System.Net;

namespace Hub.TestingClasses
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourceCleanUp)
        {
            if (managedResourceCleanUp)
            {
                // free managed resources
               
            }

            // free native resources if there are any.
        }
    }
}
