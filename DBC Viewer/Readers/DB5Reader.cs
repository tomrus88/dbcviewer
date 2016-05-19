using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DBCViewer
{
    class ColumnMeta
    {
        public short Bits;
        public short Offset;
    }

    class DB5Reader : IWowClientDBReader
    {
        private const int HeaderSize = 48;
        private const uint DB5FmtSig = 0x35424457;          // WDB5
        List<ColumnMeta> columnMeta;

        public int RecordsCount => Lookup.Count;
        public int FieldsCount { get; private set; }
        public int RecordSize { get; private set; }
        public int StringTableSize { get; private set; }

        public Dictionary<int, string> StringTable { get; private set; }

        private SortedDictionary<int, byte[]> Lookup = new SortedDictionary<int, byte[]>();

        public List<ColumnMeta> Meta { get { return columnMeta; } }

        public IEnumerable<BinaryReader> Rows
        {
            get
            {
                foreach (var row in Lookup)
                {
                    yield return new BinaryReader(new MemoryStream(row.Value), Encoding.UTF8);
                }
            }
        }

        public bool IsSparseTable { get; private set; }

        public DB5Reader(string fileName, Table def)
        {
            using (var reader = BinaryReaderExtensions.FromFile(fileName))
            {
                if (reader.BaseStream.Length < HeaderSize)
                {
                    throw new InvalidDataException(string.Format("File {0} is corrupted!", fileName));
                }

                if (reader.ReadUInt32() != DB5FmtSig)
                {
                    throw new InvalidDataException(string.Format("File {0} isn't valid DB2 file!", fileName));
                }

                int recordsCount = reader.ReadInt32();
                FieldsCount = reader.ReadInt32();
                RecordSize = reader.ReadInt32();
                StringTableSize = reader.ReadInt32(); // also offset for sparse table

                uint tableHash = reader.ReadUInt32();
                uint build = reader.ReadUInt32();

                int MinId = reader.ReadInt32();
                int MaxId = reader.ReadInt32();
                int locale = reader.ReadInt32();
                int CopyTableSize = reader.ReadInt32();
                int metaFlags = reader.ReadInt32();

                IsSparseTable = (metaFlags & 0x1) != 0;
                bool hasIndex = (metaFlags & 0x4) != 0;
                int colMetaSize = FieldsCount * 4;

                columnMeta = new List<ColumnMeta>();

                if (hasIndex)
                {
                    columnMeta.Add(new ColumnMeta());
                }

                for (int i = 0; i < FieldsCount; i++)
                {
                    columnMeta.Add(new ColumnMeta() { Bits = reader.ReadInt16(), Offset = (short)(reader.ReadInt16() + (hasIndex ? 4 : 0)) });
                }

                long recordsOffset = HeaderSize + colMetaSize;
                long eof = reader.BaseStream.Length;
                long copyTablePos = eof - CopyTableSize;
                long indexTablePos = copyTablePos - (hasIndex ? recordsCount * 4 : 0);
                long stringTablePos = indexTablePos - (IsSparseTable ? 0 : StringTableSize);

                // Index table
                int[] m_indexes = null;

                if (hasIndex)
                {
                    reader.BaseStream.Position = indexTablePos;

                    m_indexes = new int[recordsCount];

                    for (int i = 0; i < recordsCount; i++)
                        m_indexes[i] = reader.ReadInt32();
                }

                if (IsSparseTable)
                {
                    // Records table
                    reader.BaseStream.Position = StringTableSize;

                    int ofsTableSize = MaxId - MinId + 1;

                    for (int i = 0; i < ofsTableSize; i++)
                    {
                        int offset = reader.ReadInt32();
                        int length = reader.ReadInt16();

                        if (offset == 0 || length == 0)
                            continue;

                        int id = MinId + i;

                        long oldPos = reader.BaseStream.Position;

                        reader.BaseStream.Position = offset;

                        byte[] recordBytes = reader.ReadBytes(length);

                        byte[] newRecordBytes = new byte[recordBytes.Length + 4];

                        Array.Copy(BitConverter.GetBytes(id), newRecordBytes, 4);
                        Array.Copy(recordBytes, 0, newRecordBytes, 4, recordBytes.Length);

                        Lookup.Add(id, newRecordBytes);

                        reader.BaseStream.Position = oldPos;
                    }
                }
                else
                {
                    // Records table
                    reader.BaseStream.Position = recordsOffset;

                    for (int i = 0; i < recordsCount; i++)
                    {
                        reader.BaseStream.Position = recordsOffset + i * RecordSize;

                        byte[] recordBytes = reader.ReadBytes(RecordSize);

                        if (hasIndex)
                        {
                            byte[] newRecordBytes = new byte[RecordSize + 4];

                            Array.Copy(BitConverter.GetBytes(m_indexes[i]), newRecordBytes, 4);
                            Array.Copy(recordBytes, 0, newRecordBytes, 4, recordBytes.Length);

                            Lookup.Add(m_indexes[i], newRecordBytes);
                        }
                        else
                        {
                            int idxCol = def.Fields.FindIndex(f => f.IsIndex);

                            if (idxCol == -1)
                                throw new Exception(string.Format("Definition for file {0} has no index field specified!", fileName));

                            int numBytes = (32 - columnMeta[idxCol].Bits) >> 3;
                            int offset = columnMeta[idxCol].Offset;
                            int id = 0;

                            for (int j = 0; j < numBytes; j++)
                                id |= (recordBytes[offset + j] << (j * 8));

                            Lookup.Add(id, recordBytes);
                        }
                    }

                    // Strings table
                    reader.BaseStream.Position = stringTablePos;

                    StringTable = new Dictionary<int, string>();

                    while (reader.BaseStream.Position != stringTablePos + StringTableSize)
                    {
                        int index = (int)(reader.BaseStream.Position - stringTablePos);
                        StringTable[index] = reader.ReadStringNull();
                    }
                }

                // Copy index table
                if (copyTablePos != reader.BaseStream.Length && CopyTableSize != 0)
                {
                    reader.BaseStream.Position = copyTablePos;

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        int id = reader.ReadInt32();
                        int idcopy = reader.ReadInt32();

                        recordsCount++;

                        byte[] copyRow = Lookup[idcopy];
                        byte[] newRow = new byte[copyRow.Length];
                        Array.Copy(copyRow, newRow, newRow.Length);
                        Array.Copy(BitConverter.GetBytes(id), newRow, 4);

                        Lookup.Add(id, newRow);
                    }
                }
            }
        }
    }
}
