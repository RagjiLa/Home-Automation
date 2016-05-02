using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hub
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
    }
}
