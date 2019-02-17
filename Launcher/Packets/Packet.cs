﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zlib;

namespace Launcher
{
    public class Packet
    {
        #region Static fields
        private static readonly Log _log = Log.Instance;
        #endregion

        #region Properties
        public byte IsAuthenticated { get; private set; } = 0x01; //0x00: isAuthenticated;
        public byte IsEncoded { get; private set; } = 0x00; //0x01: isCompressed/encoded;
        public ushort ConnType { get; set; } //0x02: connectionType;
        public ushort Size { get; private set; } = 0x10; //0x04: packetSize;
        public ushort NumSubpackets { get; private set; } //0x06: numSubpackets;
        public uint TimeStamp { get; private set; } //0x08: timestamp; //Miliseconds
        public byte[] Data { get; set; }       
        public Queue<SubPacket> SubPacketQueue { get; set; } = new Queue<SubPacket>();
        public List<SubPacket> SubPacketList { get; set; } = new List<SubPacket>();
        #endregion

        #region Constructors
        public Packet() { }
        public Packet(byte[] incoming) => PacketSetup(incoming);
        public Packet(SubPacket subPacket) => AddSubPacket(subPacket);       
        public Packet(GamePacket gamePacket)
        {
            SubPacket subPacket = new SubPacket(gamePacket);            
            AddSubPacket(subPacket);
        }    
        public Packet(MessagePacket messagePacket)
        {
            SubPacket subPacket = new SubPacket(messagePacket);
            AddSubPacket(subPacket);
        }
        #endregion

        public void AddSubPacket(SubPacket subPacket)
        {
            Size += subPacket.Size;
            SubPacketList.Add(subPacket);
        }

        public byte[] ToBytes(Blowfish blowfish = null)
        {
            byte[] toBytes = new byte[Size];

            byte[] header = new byte[0x10];            
            header[0x00] = IsAuthenticated;
            header[0x01] = IsEncoded;
            Buffer.BlockCopy(BitConverter.GetBytes(Size), 0, header, 0x04, 0x02);
            header[0x06] = (byte)SubPacketList.Count;
            Buffer.BlockCopy(Server.GetTimeStampHex(), 0, header, 0x08, 0x04);   

            int index = 0x10;
            Buffer.BlockCopy(header, 0, toBytes, 0, header.Length);

            foreach (SubPacket sp in SubPacketList)
            {
                Buffer.BlockCopy(sp.ToBytes(blowfish), 0, toBytes, index, sp.Size);
                index += sp.Size;
            }

            return toBytes;
        }

        public byte[] ToBytesZipped()
        {
            //get ready-to-send packet
            byte[] toBytes = ToBytes();
            //separate data from packet header
            byte[] data = new byte[toBytes.Length - 0x10];            
            Buffer.BlockCopy(toBytes, 0x10, data, 0, (toBytes.Length - 0x10));
            //zip data and get size
            byte[] zipped = Zip(data);
            ushort zippedSize = (ushort)zipped.Length;
            //write zipped data to result array
            byte[] result = new byte[zippedSize + 0x10];
            //write packet header back
            Buffer.BlockCopy(toBytes, 0, result, 0, 0x10);
            //update packet size after compression
            Buffer.BlockCopy(BitConverter.GetBytes(zippedSize), 0, result, 0x04, sizeof(ushort));
            //write zpped data to result
            Buffer.BlockCopy(zipped, 0, result, 0x10, zippedSize);
            //turn packet zipped switch on
            result[0x01] = 0x01;
            return result;
        }

        #region Dummy packets  
        public static byte[] AckPacket { get; } =
        {
            0x00, 0x00, 0x00, 0x00, 0xA0, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x90, 0x02, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x0C, 0x69, 0x00, 0xE0, 0x00, 0x00, 0x00, 0x00, 0xD0, 0xED, 0x45, 0x02, 0x00, 0x00, 0x00, 0x00,
            0xC0, 0xED, 0xDF, 0xFF, 0xAF, 0xF7, 0xF7, 0xAF, 0x10, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF,
            0x42, 0x82, 0x63, 0x52, 0x01, 0x00, 0x00, 0x00, 0x10, 0xEF, 0xDF, 0xFF, 0x53, 0x61, 0x6D, 0x70,
            0x6C, 0x65, 0x20, 0x53, 0x61, 0x6D, 0x70, 0x6C, 0x65, 0x20, 0x52, 0x75, 0x6E, 0x52, 0x75, 0x6E,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0xF7, 0xAF, 0xAF, 0xF7, 0x00, 0x00, 0xB8, 0x6C, 0x4D, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x10, 0x6C, 0x4D, 0x02, 0x00, 0x00, 0x00, 0x00, 0x40, 0x2C, 0xAC, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x63, 0x72, 0x65, 0x61, 0x74, 0x65, 0x43, 0x61, 0x6C, 0x6C, 0x62, 0x61, 0x63, 0x6B, 0x4F, 0x62,
            0x6A, 0x65, 0x63, 0x74, 0x2E, 0x2E, 0x2E, 0x5B, 0x36, 0x36, 0x2E, 0x31, 0x33, 0x30, 0x2E, 0x39,
            0x39, 0x2E, 0x38, 0x32, 0x3A, 0x36, 0x33, 0x34, 0x30, 0x37, 0x5D, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x70, 0xEE, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0x6C, 0x4E, 0x38, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x32, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0xAF, 0xF7, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x38, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xC0, 0xEE, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0xFE, 0x4E, 0x38, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x0B, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x20, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF,
            0x00, 0x01, 0xCC, 0xCC, 0x0C, 0x69, 0x00, 0xE0, 0xD0, 0x58, 0x33, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x30, 0x00, 0x00, 0x00, 0x80, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF,
            0xC0, 0xEE, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0xD0, 0xED, 0x45, 0x02, 0x00, 0x00, 0x00, 0x00,
            0xF0, 0xEE, 0xDF, 0xFF, 0xAF, 0xF7, 0xF7, 0xAF, 0x20, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF,
            0x0C, 0x69, 0x00, 0xE0, 0x00, 0x00, 0x00, 0x00, 0x10, 0x6C, 0x4D, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x45, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x33, 0x34, 0x30, 0x37, 0x00, 0x00, 0x00, 0x00,
            0x90, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0x18, 0xBE, 0x34, 0x01, 0x00, 0x00, 0x00, 0x00,
            0xD8, 0x32, 0xAC, 0x01, 0x00, 0x00, 0x00, 0x00, 0xD0, 0x32, 0xAC, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0xF7, 0xAF, 0x42, 0x82, 0x63, 0x52, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x36, 0x2E, 0x31, 0x33, 0x30, 0x2E, 0x39, 0x39, 0x2E, 0x38, 0x32, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x36, 0x2E, 0x31, 0x33, 0x30, 0x2E, 0x39, 0x39, 0x2E, 0x38, 0x32, 0x00, 0xFF,
            0x90, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0x24, 0xCF, 0x76, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x10, 0x6C, 0x4D, 0x02, 0x00, 0x00, 0x00, 0x00, 0x70, 0x7A, 0xB7, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x6C, 0x4D, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x90, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0xD1, 0xF3, 0x37, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x10, 0x6C, 0x4D, 0x02, 0x00, 0x00, 0x00, 0x00, 0xA0, 0x32, 0xAC, 0x01, 0x00, 0x00, 0x00, 0x00,
            0xC0, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0xE8, 0x3E, 0x77, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x70, 0x99, 0xAA, 0x01, 0x0C, 0x69, 0x00, 0xE0, 0xA0, 0x32, 0xAC, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x58, 0x59, 0x33, 0x02, 0x00, 0x00, 0x00, 0x00, 0x10, 0x6C, 0x4D, 0x02, 0x00, 0x00, 0x00, 0x00,
            0xE0, 0xEF, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0x05, 0x3F, 0x77, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x0C, 0x69, 0x00, 0xE0, 0x0C, 0x69, 0x00, 0xE0, 0xA0, 0x32, 0xAC, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0xF0, 0xDF, 0xFF, 0x7F, 0xFD, 0xFF, 0xFF, 0x23, 0x3F, 0x77, 0x01, 0x00, 0x00, 0x00, 0x00,
            0xC0, 0x5A, 0x33, 0x02, 0x0C, 0x69, 0x00, 0xE0, 0xA0, 0x32, 0xAC, 0x01, 0x00, 0x00, 0x00, 0x00,
        };      
        #endregion
                   
        private void PacketSetup(byte[] data)
        {
            if(data.Any(b => b != 0))
            {                 
                IsAuthenticated = data[0x00];
                IsEncoded = data[0x01];
                ConnType = (ushort)(data[0x03] << 8 | data[0x02]);
                Size = (ushort)(data[0x05] << 8 | data[0x04]);
                NumSubpackets = (ushort)(data[0x07] << 8 | data[0x06]);
                //TimeStamp = (uint)(data[0x07] << 24 | data[0x07] << 16 | data[0x07] << 8 | data[0x06]);

                byte[] packetData = new byte[Size - 0x10];
                Buffer.BlockCopy(data, 0x10, packetData, 0, Size - 0x10);

                Data = packetData;
            }            
        }

        public void ProcessSubPackets(Blowfish bf)
        {            
            int index = 0;
            ushort subPacketSize = (ushort)(Data[0x01] << 8 | Data[0]);

            for (int i = 1; i <= NumSubpackets; i++)
            {
                try
                {
                    byte[] subpacketData = new byte[subPacketSize - 0x10];

                    if (subpacketData.Length > 0x8) //do not process small sync packets
                    {
                        Buffer.BlockCopy(Data, index + 0x10, subpacketData, 0, subpacketData.Length); //copy whole subpacket. + 0x10  = without subpacket header.            

                        SubPacket subpacket = new SubPacket
                        {
                            Size = subPacketSize,
                            Type = (ushort)(Data[index + 0x03] << 8 | Data[index + 0x02]),
                            SourceId = (uint)(Data[index + 0x07] << 24 | Data[index + 0x06] << 16 | Data[index + 0x05] << 8 | Data[index + 0x04]),
                            TargetId = (uint)(Data[index + 0x0b] << 24 | Data[index + 0x0a] << 16 | Data[index + 0x09] << 8 | Data[index + 0x08]),
                            Data = subpacketData
                        };

                        if (bf != null)
                            subpacket.Decrypt(bf);

                        SubPacketQueue.Enqueue(subpacket);
                    }

                    index += subPacketSize;

                    if(i < NumSubpackets)
                        subPacketSize = (ushort)(Data[index + 0x01] << 8 | Data[index + 0x00]);
                }
                catch (OverflowException) { break; }

            }
        }

        #region Compression/Decompression
        private byte[] Zlib(byte[] bytes, CompressionMode mode)
        {
            using (var compressedStream = new MemoryStream(bytes))
            using (var zipStream = new ZlibStream(compressedStream, mode))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
        private byte[] Zip(byte[] data) => Zlib(data, CompressionMode.Compress);
        private byte[] UnZip(byte[] data) => Zlib(data, CompressionMode.Decompress);
        #endregion
    }
}
