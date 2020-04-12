using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System.IO.Compression;
using System.Threading;
using System.Reactive;

using CommonTypes;


namespace DataSources
{
    public class DukasDataSource : GenericFileBasedDataSource<DukasDataReader>
    {
        public DukasDataSource(int contractId, string baseDirectory)
            : base(contractId, baseDirectory, 50)
        {
        }


        public DukasDataSource(int contractId, Dictionary<string, object> config)
            : this(contractId, config["BaseDirectory"].ToString())
        {
        }
    }


    public class DukasDataReader : IDataReader
    {
        struct typZipHead
        {
            public long zipCRC;
            public long zipCompressedSize;
            public long zipUncompressedSize;
            public int zipFileNameLength;
        }


        int ContractId;
        string Filename;
        byte[] UnzippedData;


        public DukasDataReader()
        {
        }


        public void Initialise(int contractId, string filename)
        {
            ContractId = contractId;
            Filename = filename;
            UnzippedData = unZip(Filename);
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            int counter = 0;

            int num3;

            MemoryStream stream = new MemoryStream(UnzippedData)
            {
                Position = 0L
            };

            num3 = 40;

            while (stream.Position < stream.Length)
            {
                int num5 = 0;
                do
                {
                    int num8 = num3 - 8;
                    for (int j = 0; j <= num8; j += 8)
                    {
                        byte num = UnzippedData[(((int)stream.Position) + j) + num5];
                        UnzippedData[(((int)stream.Position) + j) + num5] = UnzippedData[(((int)stream.Position) + j) + (7 - num5)];
                        UnzippedData[(((int)stream.Position) + j) + (7 - num5)] = num;
                    }
                    num5++;
                }
                while (num5 <= 3);

                DateTime dt;
                decimal bid = decimal.Zero, ask = decimal.Zero;
                int bidSize = 0, askSize = 0;

                long num2 = BitConverter.ToInt64(UnzippedData, (int)stream.Position);
                int num9 = num3 - 8;

                int jjj = 0;
                for (int i = 8; i <= num9; i += 8)
                {
                    double num10 = new double();
                    double num4 = num10;

                    if (jjj == 0)
                        ask = (decimal)BitConverter.ToDouble(UnzippedData, ((int)stream.Position) + i);
                    else if (jjj == 1)
                        bid = (decimal)BitConverter.ToDouble(UnzippedData, ((int)stream.Position) + i);
                    else if (jjj == 2)
                        askSize = (int)BitConverter.ToDouble(UnzippedData, ((int)stream.Position) + i);
                    else if (jjj == 3)
                        bidSize = (int)BitConverter.ToDouble(UnzippedData, ((int)stream.Position) + i);

                    jjj++;
                }

                dt = DateTime.FromOADate((((((double)num2) / 1000.0) / 24.0) / 3600.0) + 25569.0);
                dt.AddMilliseconds((double)(num2 % 0x3e8L));

                yield return new Market(new DateTimeOffset(dt, new TimeSpan(0, 0, 0)), ContractId, bidSize, bid, ask, askSize);
                counter++;

                MemoryStream stream2 = stream;
                stream2.Position += num3;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        static byte[] unZip(string filZipname)
        {
            BinaryReader reader = new BinaryReader(File.Open(filZipname, FileMode.Open, FileAccess.Read));
            typZipHead objZiphead = new typZipHead();
            reader.BaseStream.Seek(0x1aL, SeekOrigin.Begin);
            objZiphead.zipFileNameLength = reader.ReadInt16();
            reader.BaseStream.Seek((long)(-80 - objZiphead.zipFileNameLength), SeekOrigin.End);
            objZiphead.zipCRC = reader.ReadInt32();
            objZiphead.zipCompressedSize = reader.ReadInt32();
            objZiphead.zipUncompressedSize = reader.ReadInt32();
            reader.BaseStream.Seek(30L, SeekOrigin.Begin);
            byte[] bytBuffer = new byte[(objZiphead.zipFileNameLength + ((int)objZiphead.zipCompressedSize)) + 1];
            int num2 = bytBuffer.Length - 1;
            for (int i = 0; i <= num2; i++)
            {
                bytBuffer[i] = reader.ReadByte();
            }
            bytBuffer = doGZconvert(ref objZiphead, ref bytBuffer);
            reader.Close();
            return bytBuffer;
        }


        static byte[] doGZconvert(ref typZipHead objZiphead, ref byte[] bytBuffer)
        {
            MemoryStream objMemory = new MemoryStream();
            objMemory.Write(new byte[] { 0x1f, 0x8b, 8, 8 }, 0, 4);
            objMemory.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
            objMemory.Write(new byte[] { 2, 0 }, 0, 2);
            objMemory.Write(bytBuffer, 0, objZiphead.zipFileNameLength);
            objMemory.WriteByte(0);
            objMemory.Write(bytBuffer, objZiphead.zipFileNameLength, (int)objZiphead.zipCompressedSize);
            objMemory.Write(BitConverter.GetBytes(objZiphead.zipCRC), 0, 4);
            objMemory.Write(BitConverter.GetBytes(objZiphead.zipUncompressedSize), 0, 4);
            bytBuffer = doGZextract(ref objMemory);
            return bytBuffer;
        }


        static byte[] doGZextract(ref MemoryStream objMemory)
        {
            GZipStream stream = new GZipStream(objMemory, CompressionMode.Decompress);
            byte[] buffer = new byte[4];
            objMemory.Position = objMemory.Length - 4L;
            objMemory.Read(buffer, 0, 4);
            int count = BitConverter.ToInt32(buffer, 0);
            objMemory.Position = 0L;
            byte[] array = new byte[(count - 1) + 1];
            stream.Read(array, 0, count);
            stream.Dispose();
            objMemory.Dispose();
            return array;
        }
    }
}
