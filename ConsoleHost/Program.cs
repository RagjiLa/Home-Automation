using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Hub;
using Hub.TestingClasses;
using HubPlugins;
using Hub.Utilities;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Hub Host";
            CacheService<float> floatcs = new CacheService<float>();
            Server s = new Server(new ObjectCreator());
            DweetPlugin dp = new DweetPlugin();
            PlantMangerPlugin pm = new PlantMangerPlugin(360, 10, floatcs);
            s.StartDispatching(new IPEndPoint(IPAddress.Any, 900), new List<ISingleSessionPlugin>() { dp, pm });
            Logger.Logged += Logger_Logged;
            Console.ReadLine();
        }

        private static void Logger_Logged(object sender, LoggedArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
