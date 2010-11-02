using System.IO;

namespace dbc2sql
{
    interface IWowClientDBReader
    {
        int RecordsCount { get; }
        int FieldsCount { get; }
        int RecordSize { get; }
        int StringTableSize { get; }
        StringTable StringTable { get; }
        byte[] GetRowAsByteArray(int row);
        BinaryReader this[int row] { get; }
    }
}
