using Hub.TestingInterfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Hub.TestingClasses
{
    public class AbstractedTcpListner : ITcpListner
    {
        private readonly TcpListener _listner;
        public AbstractedTcpListner(IPEndPoint listeningEndpoint)
        {
            _listner = new TcpListener(listeningEndpoint);
        }

        public void Start(int backlog)
        {
            _listner.Start(backlog);
        }

        public void Stop()
        {
            _listner.Stop();
        }

        public ISocket AcceptSocket(CancellationToken token)
        {
            var waitHandle = _listner.AcceptSocketAsync();
            try
            {

                waitHandle.Wait(token);
            }
            catch (OperationCanceledException)
            {
                //Cancelled dont do anything
            }
            if (token.IsCancellationRequested)
                return null;

            return new AbstractedSocket(waitHandle.Result);
        }

        public bool Pending()
        {
            return _listner.Pending();
        }
    }
}
