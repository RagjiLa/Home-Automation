using Hub;
using Hub.TestingClasses;
using HubPlugins;
using Kernel;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Hub Host";
            CacheService<float> floatcs = new CacheService<float>();
            Server s = new Server(new ObjectCreator());
            var bufferCache = new Dictionary<string, FiniteBufferQue<SqlData>>();
            DweetPlugin dpPlugin = new DweetPlugin();
            SqLitePlugin sqlPlugin = new SqLitePlugin(bufferCache, 0);
            PlantMangerPlugin pmPlugin = new PlantMangerPlugin(360, 10, floatcs);
            s.StartDispatching(new IPEndPoint(GetLocalIpAddress(), 900), new List<ISingleSessionPlugin>{ dpPlugin, pmPlugin, sqlPlugin });
            Logger.Logged += Logger_Logged;
            Console.ReadLine();
        }

        private static void Logger_Logged(object sender, LoggedArgs e)
        {
            Console.WriteLine(e.Message);
        }

        public static IPAddress GetLocalIpAddress()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip;
                    }
                }
                throw new Exception("Local IP Address Not Found!");
            }
            else
            {
                throw new Exception("Network not connected");
            }
        }
    }
}
