using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DBCViewer
{
    class DB3Reader : IWowClientDBReader
    {
        private const int HeaderSize = 48;
        private const uint DB3FmtSig = 0x33424457;          // WDB3

        public int RecordsCount { get; private set; }
        public int FieldsCount { get; private set; }
        public int RecordSize { get; private set; }
        public int StringTableSize { get; private set; }

        public Dictionary<int, string> StringTable { get; private set; }

        private MemoryStream[] m_records;
        private Dictionary<int, int> Lookup = new Dictionary<int, int>();

        public byte[] GetRowAsByteArray(int row)
        {
            return m_records[row].ToArray();
        }

        public BinaryReader this[int row]
        {
            get { return new BinaryReader(m_records[row], Encoding.UTF8); }
        }

        public DB3Reader(string fileName)
        {
            using (var reader = BinaryReaderExtensions.FromFile(fileName))
            {
                if (reader.BaseStream.Length < HeaderSize)
                {
                    throw new InvalidDataException(String.Format("File {0} is corrupted!", fileName));
                }

                if (reader.ReadUInt32() != DB3FmtSig)
                {
                    throw new InvalidDataException(String.Format("File {0} isn't valid DB2 file!", fileName));
                }

                RecordsCount = reader.ReadInt32();
                FieldsCount = reader.ReadInt32();
                RecordSize = reader.ReadInt32();
                StringTableSize = reader.ReadInt32();

                uint tableHash = reader.ReadUInt32();
                uint build = reader.ReadUInt32();
                uint unk1 = reader.ReadUInt32();

                int MinId = reader.ReadInt32();
                int MaxId = reader.ReadInt32();
                int locale = reader.ReadInt32();
                int CopyTableSize = reader.ReadInt32();

                int stringTableStart = HeaderSize + RecordsCount * RecordSize;

                bool hasIndex = stringTableStart + StringTableSize + CopyTableSize < reader.BaseStream.Length;

                m_records = new MemoryStream[RecordsCount];

                for (int i = 0; i < RecordsCount; i++)
                {
                    reader.BaseStream.Position = HeaderSize + i * RecordSize;

                    m_records[i] = new MemoryStream(RecordSize);
                    byte[] recordBytes = reader.ReadBytes(RecordSize);

                    if (hasIndex)
                    {
                        long oldpos = reader.BaseStream.Position;
                        reader.BaseStream.Position = stringTableStart + StringTableSize + i * 4;
                        byte[] indexBytes = reader.ReadBytes(4);
                        m_records[i].Write(indexBytes, 0, indexBytes.Length);
                        reader.BaseStream.Position = oldpos;

                        Lookup.Add(BitConverter.ToInt32(indexBytes, 0), i);
                    }
                    else
                    {
                        Lookup.Add(BitConverter.ToInt32(recordBytes, 0), i);
                    }

                    m_records[i].Write(recordBytes, 0, recordBytes.Length);

                    m_records[i].Position = 0;
                }

                StringTable = new Dictionary<int, string>();

                int stringTableEnd = HeaderSize + RecordsCount * RecordSize + StringTableSize;

                while (reader.BaseStream.Position != stringTableEnd)
                {
                    int index = (int)reader.BaseStream.Position - stringTableStart;
                    StringTable[index] = reader.ReadStringNull();
                }

                long copyTablePos = stringTableEnd;

                if (copyTablePos != reader.BaseStream.Length && CopyTableSize != 0)
                {
                    reader.BaseStream.Position = copyTablePos;

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        int id = reader.ReadInt32();
                        int idcopy = reader.ReadInt32();

                        Lookup.Add(id, Lookup[idcopy]);
                    }
                }
            }
        }
    }
}
