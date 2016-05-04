using Kernel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Device Simulator";
            Parallel.For(0, 500, (ctr) =>
            {
                using (var device = new TcpClient())
                {
                    device.Connect(new IPEndPoint(GetLocalIpAddress(), 9000));
                    //Console.WriteLine("Connected to hub");

                    var dataToSend = new List<byte>(SqlPluginTest(ctr));
                    using (var tcpStream = device.GetStream())
                    {
                        tcpStream.Write(dataToSend.ToArray(), 0, dataToSend.Count);
                        Console.Write(Thread.CurrentThread.ManagedThreadId.ToString() + " Sent " + dataToSend.Count + " bytes");
                        var responseRaw = new byte[1024];
                        Console.WriteLine(tcpStream.Read(responseRaw, 0, 1024) > 0 ? Encoding.UTF8.GetString(responseRaw): "");
                    }

                }
            });
            Console.ReadLine();
        }

        private static IEnumerable<byte> DweetPluginTest()
        {
            var jsongData = new Dictionary<string, string>();
            jsongData.Add("W", "8");
            var jsonString = ToJsonString(jsongData);
            var pluginData = new Dictionary<string, string>();
            pluginData.Add("T", "TagLaukik");
            pluginData.Add("D", jsonString);
            return DataParser.GeneratePacket(PluginName.DweetPlugin, pluginData).ToList();
        }

        private static IEnumerable<byte> SqlPluginTest(int value)
        {
            var dataForSql = new Dictionary<string, string>();

          
            dataForSql.Add("T", @"TimeSeries");
            dataForSql.Add("S", value.ToString(CultureInfo.InvariantCulture));
            dataForSql.Add("X", @"6.5");
            dataForSql.Add("g", @"285.0");
            return DataParser.GeneratePacket(PluginName.SqLitePlugin, dataForSql).ToList();
        }



        private static string ToJsonString(IDictionary<string, string> data)
        {
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{");
            foreach (var kvp in data)
            {
                jsonString.Append("\"");
                jsonString.Append(kvp.Key);
                jsonString.Append("\"");
                jsonString.Append(":");
                jsonString.Append("\"");
                jsonString.Append(kvp.Value);
                jsonString.Append("\"");
                jsonString.Append(",");
            }
            jsonString.Remove(jsonString.Length - 1, 1);
            jsonString.Append("}");
            return jsonString.ToString();
        }

        private static IPAddress GetLocalIpAddress()
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
