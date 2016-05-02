using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hub.Utilities
{
    public class PacketGenerator
    {
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
    }
}
