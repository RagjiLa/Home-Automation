using Hub;
using Hub.TestingClasses;
using HubPlugins;
using Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Hub Host";
            CacheService<float> floatcs = new CacheService<float>();
            using (Server s = new Server(new ObjectCreator()))
            {
                var bufferCache = new Dictionary<string, FiniteBufferQue<SqlData>>();
                DweetPlugin dpPlugin = new DweetPlugin();
                SqLitePlugin sqlPlugin = new SqLitePlugin(Environment.CurrentDirectory + @"\Data.db3", bufferCache, 100);
                PlantMangerPlugin pmPlugin = new PlantMangerPlugin(360, 10, floatcs);
                Task.Run(() =>
                {
                    while (true)
                    {
                        Console.Title = "Hub Host" + Process.GetCurrentProcess().Threads.Count;
                        Thread.Sleep(1000);
                    }
                });
                s.StartDispatching(new IPEndPoint(GetLocalIpAddress(), 9000),new List<ISingleSessionPlugin> { dpPlugin, pmPlugin, sqlPlugin });
                Logger.Logged += Logger_Logged;

                Console.ReadLine();
                Console.WriteLine("Shutdown sucessfull " + s.StopDispatching(TimeSpan.FromSeconds(10)));
                Console.ReadLine();
            }
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
