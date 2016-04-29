using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(new IPEndPoint(IPAddress.Any, 9000));

            //            server.Start();
            //            server.AcceptSocket ()
        }
    }
}
