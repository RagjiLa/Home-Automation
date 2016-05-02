using Kernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Device Simulator Post on Dweet";

            using (var device = new TcpClient())
            {
                device.Connect(new IPEndPoint(GetLocalIPAddress(), 900));
                Console.WriteLine("Connected to hub");
                var jsongData = new Dictionary<string, string>();
                jsongData.Add("W", "8");
                var jsonString = ToJsonString(jsongData);
                var pluginData = new Dictionary<string, string>();
                pluginData.Add("T", "TagLaukik");
                pluginData.Add("D", jsonString);
                var packet = DataParser.GeneratePacket(PluginName.DweetPlugin, pluginData).ToList();
                using (var tcpStream = device.GetStream())
                {
                    tcpStream.Write(packet.ToArray(), 0, packet.Count);
                    Console.Write("Sent ");
                    var responseRaw = new byte[1024];
                    tcpStream.Read(responseRaw, 0, 1024);
                    Console.WriteLine(Encoding.UTF8.GetString(responseRaw));
                }
            }

            Console.ReadLine();
        }

        public static IEnumerable<byte> GeneratePacket(byte[] headerBytes, byte[] dataBytes)
        {
            if (headerBytes.Length + dataBytes.Length > 1002) throw new InvalidDataException("Header and data should not exceed 1002 bytes");
            using (var msData = new MemoryStream())
            {
                using (var packetWritter = new BinaryWriter(msData, Encoding.UTF8, false))
                {
                    uint packetLen = 0;
                    packetWritter.Write((byte)0xFF); //SOP
                    packetLen += 1;
                    packetWritter.Write((UInt32)headerBytes.Length); //Header Length
                    packetLen += 4;
                    packetWritter.Write(headerBytes); //Header
                    packetLen += (uint)headerBytes.Length;
                    packetWritter.Write((UInt32)dataBytes.Length); //DATA Length
                    packetLen += 4;
                    packetWritter.Write(dataBytes); //DATA
                    packetLen += (uint)dataBytes.Length;
                    packetWritter.Write((UInt32)4); //CRC Length
                    packetLen += 4;
                    packetLen += 4;
                    packetWritter.Write(packetLen); //CRC
                }
                return msData.ToArray();
            }
        }

        public static string ToJsonString(IDictionary<string, string> data)
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

        public static IPAddress GetLocalIPAddress()
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
