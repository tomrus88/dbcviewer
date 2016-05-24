using PluginInterface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace DBCViewer
{
    class ColumnMeta
    {
        public short Bits;
        public short Offset;
    }

    class DB5Reader : IClientDBReader
    {
        private const int HeaderSize = 48;
        public const uint DB5FmtSig = 0x35424457;          // WDB5
        private List<ColumnMeta> columnMeta;

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
        public bool HasIndexTable { get; private set; }
        public uint TableHash { get; private set; }
        public uint LayoutHash { get; private set; }
        public int Locale { get; private set; }
        public string FileName { get; private set; }

        public DB5Reader(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                if (reader.BaseStream.Length < HeaderSize)
                {
                    throw new InvalidDataException(string.Format("File {0} is corrupted!", FileName));
                }

                if (reader.ReadUInt32() != DB5FmtSig)
                {
                    throw new InvalidDataException(string.Format("File {0} isn't valid DB2 file!", FileName));
                }

                int recordsCount = reader.ReadInt32();
                FieldsCount = reader.ReadInt32();
                RecordSize = reader.ReadInt32();
                StringTableSize = reader.ReadInt32(); // also offset for sparse table

                TableHash = reader.ReadUInt32();
                LayoutHash = reader.ReadUInt32(); // 21737: changed from build number to layoutHash

                int MinId = reader.ReadInt32();
                int MaxId = reader.ReadInt32();
                Locale = reader.ReadInt32();
                int CopyTableSize = reader.ReadInt32();
                ushort flags = reader.ReadUInt16();
                ushort IDIndex = reader.ReadUInt16();

                IsSparseTable = (flags & 0x1) != 0;
                HasIndexTable = (flags & 0x4) != 0;
                int colMetaSize = FieldsCount * 4;

                columnMeta = new List<ColumnMeta>();

                for (int i = 0; i < FieldsCount; i++)
                {
                    columnMeta.Add(new ColumnMeta() { Bits = reader.ReadInt16(), Offset = (short)(reader.ReadInt16() + (HasIndexTable ? 4 : 0)) });
                }

                if (HasIndexTable)
                {
                    FieldsCount++;
                    columnMeta.Insert(0, new ColumnMeta());
                }

                long recordsOffset = HeaderSize + colMetaSize;
                long eof = reader.BaseStream.Length;
                long copyTablePos = eof - CopyTableSize;
                long indexTablePos = copyTablePos - (HasIndexTable ? recordsCount * 4 : 0);
                long stringTablePos = indexTablePos - (IsSparseTable ? 0 : StringTableSize);

                // Index table
                int[] m_indexes = null;

                if (HasIndexTable)
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

                        if (HasIndexTable)
                        {
                            byte[] newRecordBytes = new byte[RecordSize + 4];

                            Array.Copy(BitConverter.GetBytes(m_indexes[i]), newRecordBytes, 4);
                            Array.Copy(recordBytes, 0, newRecordBytes, 4, recordBytes.Length);

                            Lookup.Add(m_indexes[i], newRecordBytes);
                        }
                        else
                        {
                            int numBytes = (32 - columnMeta[IDIndex].Bits) >> 3;
                            int offset = columnMeta[IDIndex].Offset;
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

        public DB5Reader(string fileName) : this(new FileStream(fileName, FileMode.Open))
        {
            FileName = fileName;
        }

        public void Save(DataTable table, Table def, string path)
        {
            int IDColumn = table.Columns.IndexOf(table.PrimaryKey[0]);
            var idValues = table.Rows.Cast<DataRow>().Select(r => (int)r[IDColumn]);

            DataRowComparer comparer = new DataRowComparer();
            comparer.IdColumnIndex = IDColumn;

            var uniqueRows = table.Rows.Cast<DataRow>().Distinct(comparer).ToArray();

            int minId = idValues.Min();
            int maxId = idValues.Max();
            bool hasStrings = table.Columns.Cast<DataColumn>().Any(c => c.DataType == typeof(string));

            using (var fs = new FileStream(path, FileMode.Create))
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(DB5FmtSig); // magic
                bw.Write(table.Rows.Count);
                bw.Write(HasIndexTable ? FieldsCount - 1 : FieldsCount);
                bw.Write(RecordSize);
                bw.Write(0); // stringTableSize placeholder
                bw.Write(TableHash);
                bw.Write(LayoutHash);
                bw.Write(minId);
                bw.Write(maxId);
                bw.Write(Locale);
                bw.Write(0); // CopyTableSize
                bw.Write((ushort)(HasIndexTable ? 4 : 0)); // flags
                bw.Write((ushort)IDColumn); // IDIndex

                for (int i = 0; i < columnMeta.Count; i++)
                {
                    if (HasIndexTable && i == 0)
                        continue;

                    bw.Write(columnMeta[i].Bits);
                    bw.Write(HasIndexTable ? (short)(columnMeta[i].Offset - 4) : columnMeta[i].Offset);
                }

                var columnTypeCodes = table.Columns.Cast<DataColumn>().Select(c => Type.GetTypeCode(c.DataType)).ToArray();

                var stringLookup = hasStrings ? new Dictionary<string, int>() : null;
                var stringTable = hasStrings ? new MemoryStream() : null;

                if (hasStrings)
                {
                    stringLookup[""] = 0;
                    stringTable.WriteByte(0);
                    stringTable.WriteByte(0);
                }

                var fields = def.Fields;

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    int colIndex = 0;

                    DataRow row = table.Rows[i];

                    for (int j = 0; j < fields.Count; j++)
                    {
                        if (HasIndexTable && j == 0)
                        {
                            colIndex++;
                            continue;
                        }

                        int arraySize = fields[j].ArraySize;

                        for (int k = 0; k < arraySize; k++)
                        {
                            switch (columnTypeCodes[colIndex])
                            {
                                case TypeCode.Byte:
                                    bw.Write(row.Field<byte>(colIndex));
                                    break;
                                case TypeCode.SByte:
                                    bw.Write(row.Field<sbyte>(colIndex));
                                    break;
                                case TypeCode.Int16:
                                    bw.Write(row.Field<short>(colIndex));
                                    break;
                                case TypeCode.UInt16:
                                    bw.Write(row.Field<ushort>(colIndex));
                                    break;
                                case TypeCode.Int32:
                                    int count1 = (32 - columnMeta[j].Bits) >> 3;
                                    byte[] bytes1 = BitConverter.GetBytes(row.Field<int>(colIndex));
                                    bw.Write(bytes1, 0, count1);
                                    break;
                                case TypeCode.UInt32:
                                    int count2 = (32 - columnMeta[j].Bits) >> 3;
                                    byte[] bytes2 = BitConverter.GetBytes(row.Field<uint>(colIndex));
                                    bw.Write(bytes2, 0, count2);
                                    break;
                                case TypeCode.Int64:
                                    int count3 = (32 - columnMeta[j].Bits) >> 3;
                                    byte[] bytes3 = BitConverter.GetBytes(row.Field<long>(colIndex));
                                    bw.Write(bytes3, 0, count3);
                                    break;
                                case TypeCode.UInt64:
                                    int count4 = (32 - columnMeta[j].Bits) >> 3;
                                    byte[] bytes4 = BitConverter.GetBytes(row.Field<ulong>(colIndex));
                                    bw.Write(bytes4, 0, count4);
                                    break;
                                case TypeCode.Single:
                                    bw.Write(row.Field<float>(colIndex));
                                    break;
                                case TypeCode.Double:
                                    bw.Write(row.Field<double>(colIndex));
                                    break;
                                case TypeCode.String:
                                    string str = row.Field<string>(colIndex);
                                    int offset;
                                    if (stringLookup.TryGetValue(str, out offset))
                                    {
                                        bw.Write(offset);
                                    }
                                    else
                                    {
                                        byte[] strBytes = Encoding.UTF8.GetBytes(str);
                                        if (strBytes.Length == 0)
                                        {
                                            throw new Exception("should not happen");
                                            //bw.Write(0);
                                        }
                                        else
                                        {
                                            stringLookup[str] = (int)stringTable.Position;
                                            bw.Write((int)stringTable.Position);
                                            stringTable.Write(strBytes, 0, strBytes.Length);
                                            stringTable.WriteByte(0);
                                        }
                                    }
                                    break;
                                default:
                                    throw new Exception("Unknown TypeCode " + columnTypeCodes[colIndex]);
                            }

                            colIndex++;
                        }
                    }

                    // padding at the end of the row
                    long rem = ms.Position % 4;
                    if (rem != 0)
                        ms.Position += (4 - rem);
                }

                if (hasStrings)
                {
                    // update stringTableSize in the header
                    long oldPos = ms.Position;
                    ms.Position = 0x10;
                    bw.Write((int)stringTable.Length);
                    ms.Position = oldPos;

                    // write strings
                    stringTable.Position = 0;
                    stringTable.CopyTo(ms);
                }

                if (HasIndexTable)
                {
                    foreach (var id in idValues)
                    {
                        bw.Write(id);
                    }
                }

                // copy data to file
                ms.Position = 0;
                ms.CopyTo(fs);
            }
        }
    }

    class DataRowComparer : IEqualityComparer<DataRow>
    {
        public int IdColumnIndex { get; set; }

        public bool Equals(DataRow x, DataRow y)
        {
            var xa = x.ItemArray;
            var ya = y.ItemArray;

            for (int i = 0; i < xa.Length; i++)
            {
                if (IdColumnIndex == i)
                    continue;

                if (!xa[i].Equals(ya[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(DataRow obj)
        {
            return obj.GetHashCode();
        }
    }
}
