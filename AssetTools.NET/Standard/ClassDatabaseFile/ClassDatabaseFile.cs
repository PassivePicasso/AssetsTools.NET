using AssetsTools.NET.Extra;
using AssetsTools.NET.Extra.Decompressors.LZ4;
using SevenZip.Compression.LZMA;
using System;
using System.Collections.Generic;
using System.IO;

namespace AssetsTools.NET
{
    public class ClassDatabaseFile
    {
        public bool valid;
        public ClassDatabaseFileHeader header;

        public List<ClassDatabaseType> classes;

        public byte[] stringTable;

        public bool Read(AssetsFileReader reader)
        {
            header = new ClassDatabaseFileHeader();
            header.Read(reader);
            if (header.header != "cldb" || header.fileVersion > 4 || header.fileVersion < 1)
            {
                valid = false;
                return valid;
            }
            classes = new List<ClassDatabaseType>();

            long classTablePos = reader.Position;
            AssetsFileReader newReader = reader;
            if (header.compressionType != 0)
            {
                classTablePos = 0;
                MemoryStream ms;
                if (header.compressionType == 1) //lz4
                {
                    byte[] uncompressedBytes = new byte[header.uncompressedSize];
                    using (MemoryStream tempMs = new MemoryStream(reader.ReadBytes((int)header.compressedSize)))
                    {
                        Lz4DecoderStream decoder = new Lz4DecoderStream(tempMs);
                        decoder.Read(uncompressedBytes, 0, (int)header.uncompressedSize);
                        decoder.Dispose();
                    }
                    ms = new MemoryStream(uncompressedBytes);
                }
                else if (header.compressionType == 2) //lzma
                {
                    using (MemoryStream tempMs = new MemoryStream(reader.ReadBytes((int)header.compressedSize)))
                    {
                        ms = SevenZipHelper.StreamDecompress(tempMs);
                    }
                }
                else
                {
                    valid = false;
                    return valid;
                }

                newReader = new AssetsFileReader(ms);
                newReader.bigEndian = false;
            }

            newReader.Position = header.stringTablePos;
            stringTable = newReader.ReadBytes((int)header.stringTableLen);
            newReader.Position = classTablePos;
            uint size = newReader.ReadUInt32();
            for (int i = 0; i < size; i++)
            {
                ClassDatabaseType cdt = new ClassDatabaseType();
                cdt.Read(newReader, header.fileVersion, header.flags);
                classes.Add(cdt);
            }
            valid = true;
            return valid;
        }

        public void Write(AssetsFileWriter writer)
        {
            if (!valid)
            {
                throw new InvalidDataException("cldb is not valid");
            }

            var stringTablePos = 0L;
            var uncompressedSize = 0L;
            var compressedBytes = Net35Polyfill.ArrayEmpty<byte>();

            using (var uncompressedStream = new MemoryStream())
            using (var uncompressedWriter = new AssetsFileWriter(uncompressedStream))
            {
                uncompressedWriter.bigEndian = writer.bigEndian;
                uncompressedWriter.Write(classes.Count);
                for (int i = 0; i < classes.Count; i++)
                {
                    classes[i].Write(uncompressedWriter, header.fileVersion, header.flags);
                }
                stringTablePos = uncompressedWriter.Position;
                uncompressedWriter.Write(stringTable);
                uncompressedSize = uncompressedWriter.Position;

                switch (header.compressionType)
                {
                    case 0:
                        compressedBytes = uncompressedStream.ToArray();
                        break;
                    case 2:
                        using (var compressedStream = new MemoryStream())
                        {
                            uncompressedStream.Position = 0;
                            SevenZipHelper.Compress(uncompressedStream, compressedStream);
                            compressedBytes = compressedStream.ToArray();
                        }
                        break;
                    default:
                        throw new NotSupportedException("Only lzma (2) compression is supported, or no compression at all (0)");
                }
            }
            
            header.stringTablePos = (uint)stringTablePos;
            header.stringTableLen = (uint)(uncompressedSize - stringTablePos);
            header.uncompressedSize = (uint)uncompressedSize;
            header.compressedSize = (uint)compressedBytes.Length;

            header.Write(writer);
            writer.Write(compressedBytes);
        }

        public bool IsValid()
        {
            return valid;
        }
        
        public ClassDatabaseFile() { }
    }
}
