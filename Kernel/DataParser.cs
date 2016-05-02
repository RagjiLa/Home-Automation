using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kernel
{
    public class DataParser
    {
        private readonly static Encoding Encoding = Encoding.UTF8;

        private static readonly object SyncLockForParse = new object();
        public static IDictionary<string, string> ToKeyValuePairs(IEnumerable<byte> data)
        {
            lock (SyncLockForParse)
            {

                var parsedData = new Dictionary<string, string>();
                using (var reader = new BinaryReader(new MemoryStream(data.ToArray())))
                {
                    while (reader.PeekChar() > -1)
                    {
                        var key = reader.ReadByte();
                        var contentLength = reader.ReadUInt16();
                        var content = reader.ReadBytes(contentLength);
                        var strContent = Encoding.GetString(content);
                        var strKey = Encoding.GetString(new[] { key });
                        parsedData.Add(strKey, strContent);
                    }
                }
                return parsedData;
            }
        }

        private static readonly object SyncLockForParseBinary = new object();
        public static IDictionary<string, IEnumerable<byte>> ToKeyValuePairsBinary(IEnumerable<byte> data)
        {
            lock (SyncLockForParseBinary)
            {
                var parsedData = new Dictionary<string, IEnumerable<byte>>();
                using (var reader = new BinaryReader(new MemoryStream(data.ToArray())))
                {
                    while (reader.PeekChar() > -1)
                    {
                        var key = reader.ReadByte();
                        var contentLength = reader.ReadUInt16();
                        var content = reader.ReadBytes(contentLength);
                        var strKey = Encoding.GetString(new[] { key });
                        parsedData.Add(strKey, content);
                    }
                }
                return parsedData;
            }
        }

        private static readonly object SyncLockForConvert = new object();
        public static IEnumerable<byte> FromKeyValuePairs(IDictionary<string, string> data)
        {
            lock (SyncLockForConvert)
            {
                byte[] returnValue;

                using (var memoryStream = new MemoryStream())
                using (var writer = new BinaryWriter(memoryStream, Encoding))
                {
                    foreach (var kvp in data)
                    {
                        if (kvp.Key.Length > 1)
                            throw new InvalidDataException("Key for data cannot be more than one character.");
                        var encodedData = Encoding.GetBytes(kvp.Value);
                        if (encodedData.Length > UInt16.MaxValue)
                            throw new InvalidDataException("Value for data cannot be more than " + uint.MaxValue + " character.");
                        writer.Write(kvp.Key[0]);
                        writer.Write((ushort)encodedData.Length);
                        writer.Write(encodedData);
                    }
                    returnValue = memoryStream.ToArray();
                }

                return returnValue;
            }
        }

        private static readonly object SyncLockForConvertBinary = new object();
        public static IEnumerable<byte> FromKeyValuePairsBinary(IDictionary<string, IEnumerable<byte>> data)
        {
            lock (SyncLockForConvert)
            {
                byte[] returnValue;

                using (var memoryStream = new MemoryStream())
                using (var writer = new BinaryWriter(memoryStream, Encoding))
                {
                    foreach (var kvp in data)
                    {
                        if (kvp.Key.Length > 1)
                            throw new InvalidDataException("Key for data cannot be more than one character.");
                        var encodedData = kvp.Value.ToArray();
                        if (encodedData.Length > UInt16.MaxValue)
                            throw new InvalidDataException("Value for data cannot be more than " + uint.MaxValue + " character.");
                        writer.Write(kvp.Key[0]);
                        writer.Write((ushort)encodedData.Length);
                        writer.Write(encodedData);
                    }
                    returnValue = memoryStream.ToArray();
                }

                return returnValue;
            }
        }

        public static IEnumerable<byte> GeneratePacket(PluginName name, IDictionary<string, string> data)
        {
            return GeneratePacket(name.ToBytes(), DataParser.FromKeyValuePairs(data));
        }

        private static IEnumerable<byte> GeneratePacket(IEnumerable<byte> header, IEnumerable<byte> data)
        {
            //SOP 
            //Header(1) DataLen(2) Data(X)
            //H(1)         HeaderDATA
            //D(1)         DataPlayload
            //C(1)         CRCDATA(4)  

            var packetDictionary = new Dictionary<string, IEnumerable<byte>>();
            var headerBytes = header.ToArray();
            var dataBytes = data.ToArray();
            var packet = new List<byte>() { 0xFF };//SOP

            packetDictionary.Add("H", headerBytes);//1+2+2
            packetDictionary.Add("D", dataBytes);//1+2+20
            packetDictionary.Add("C", BitConverter.GetBytes((uint)(1 + 3 + headerBytes.Length + 3 + dataBytes.Length + 3 + 4)));//1+2+4

            if (headerBytes.Length + dataBytes.Length > 1014) throw new InvalidDataException("Header and data should not exceed 1014 bytes");

            packet.AddRange(FromKeyValuePairsBinary(packetDictionary));
            return packet;

        }
    }
}
