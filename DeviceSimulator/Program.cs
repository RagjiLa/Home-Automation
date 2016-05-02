using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
                device.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.5"), 900));
                Console.WriteLine("Connected to hub");
                var data = new Dictionary<string, string>();
                data.Add("W", "5");
                var jsonData = ToJsonString(data);
                var packet = GeneratePacket(Encoding.UTF8.GetBytes("DP"), Encoding.UTF8.GetBytes("T:PlantManager442214Config,D:" + jsonData)).ToList();
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
    }
}
