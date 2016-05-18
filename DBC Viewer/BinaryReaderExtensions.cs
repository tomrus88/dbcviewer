using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DBCViewer
{
    #region Coords3
    /// <summary>
    ///  Represents a coordinates of WoW object without orientation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct Coords3
    {
        public float X, Y, Z;

        /// <summary>
        ///  Converts the numeric values of this instance to its equivalent string representations, separator is space.
        /// </summary>
        public string GetCoords()
        {
            string coords = string.Empty;

            coords += X.ToString(CultureInfo.InvariantCulture);
            coords += " ";
            coords += Y.ToString(CultureInfo.InvariantCulture);
            coords += " ";
            coords += Z.ToString(CultureInfo.InvariantCulture);

            return coords;
        }
    }
    #endregion

    #region Coords4
    /// <summary>
    ///  Represents a coordinates of WoW object with specified orientation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct Coords4
    {
        public float X, Y, Z, O;

        /// <summary>
        ///  Converts the numeric values of this instance to its equivalent string representations, separator is space.
        /// </summary>
        public string GetCoordsAsString()
        {
            string coords = string.Empty;

            coords += X.ToString(CultureInfo.InvariantCulture);
            coords += " ";
            coords += Y.ToString(CultureInfo.InvariantCulture);
            coords += " ";
            coords += Z.ToString(CultureInfo.InvariantCulture);
            coords += " ";
            coords += O.ToString(CultureInfo.InvariantCulture);

            return coords;
        }
    }
    #endregion

    static class BinaryReaderExtensions
    {
        public static BinaryReader FromFile(string fileName)
        {
            return new BinaryReader(new FileStream(fileName, FileMode.Open), Encoding.UTF8);
        }

        #region ReadPackedGuid
        /// <summary>
        ///  Reads the packed guid from the current stream and advances the current position of the stream by packed guid size.
        /// </summary>
        public static ulong ReadPackedGuid(this BinaryReader reader)
        {
            ulong res = 0;
            byte mask = reader.ReadByte();

            if (mask == 0)
                return res;

            int i = 0;

            while (i < 9)
            {
                if ((mask & 1 << i) != 0)
                    res += (ulong)reader.ReadByte() << (i * 8);
                i++;
            }
            return res;
        }
        #endregion

        #region ReadStringNumber
        /// <summary>
        ///  Reads the string with known length from the current stream and advances the current position of the stream by string length.
        /// <seealso cref="GenericReader.ReadStringNull"/>
        /// </summary>
        public static string ReadStringNumber(this BinaryReader reader)
        {
            string text = string.Empty;
            uint num = reader.ReadUInt32(); // string length

            for (uint i = 0; i < num; i++)
            {
                text += (char)reader.ReadByte();
            }
            return text;
        }
        #endregion

        #region ReadStringNull
        /// <summary>
        ///  Reads the NULL terminated string from the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="GenericReader.ReadStringNumber"/>
        /// </summary>
        public static string ReadStringNull(this BinaryReader reader)
        {
            byte num;
            string text = string.Empty;
            System.Collections.Generic.List<byte> temp = new System.Collections.Generic.List<byte>();

            while ((num = reader.ReadByte()) != 0)
                temp.Add(num);

            text = Encoding.UTF8.GetString(temp.ToArray());

            return text;
        }
        #endregion

        #region ReadCoords3
        /// <summary>
        ///  Reads the object coordinates from the current stream and advances the current position of the stream by 12 bytes.
        /// </summary>
        public static Coords3 ReadCoords3(this BinaryReader reader)
        {
            Coords3 v;

            v.X = reader.ReadSingle();
            v.Y = reader.ReadSingle();
            v.Z = reader.ReadSingle();

            return v;
        }
        #endregion

        #region ReadCoords4
        /// <summary>
        ///  Reads the object coordinates and orientation from the current stream and advances the current position of the stream by 16 bytes.
        /// </summary>
        public static Coords4 ReadCoords4(this BinaryReader reader)
        {
            Coords4 v;

            v.X = reader.ReadSingle();
            v.Y = reader.ReadSingle();
            v.Z = reader.ReadSingle();
            v.O = reader.ReadSingle();

            return v;
        }
        #endregion

        #region ReadStruct
        /// <summary>
        /// Reads struct from the current stream and advances the current position if the stream by SizeOf(T) bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binReader"></param>
        /// <returns></returns>
        public static T ReadStruct<T>(this BinaryReader reader) where T : struct
        {
            byte[] rawData = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            T returnObject = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return returnObject;
        }
        #endregion

        #region ReadPackedInt32
        /// <summary>
        ///  Reads the packed Int32 from the current stream and advances the current position of the stream by packed Int32 size.
        /// </summary>
        public static int ReadPackedInt32(this BinaryReader reader, int bits)
        {
            byte[] b = reader.ReadBytes((32 - bits) >> 3);

            int i32 = 0;
            for (int i = 0; i < b.Length; i++)
                i32 |= b[i] << i * 8;

            return i32;
        }
        #endregion

        #region ReadPackedUInt32
        /// <summary>
        ///  Reads the packed UInt32 from the current stream and advances the current position of the stream by packed UInt32 size.
        /// </summary>
        public static uint ReadPackedUInt32(this BinaryReader reader, int bits)
        {
            byte[] b = reader.ReadBytes((32 - bits) >> 3);

            uint u32 = 0;
            for (int i = 0; i < b.Length; i++)
                u32 |= (uint)b[i] << i * 8;

            return u32;
        }
        #endregion

        #region ReadPackedInt64
        /// <summary>
        ///  Reads the packed Int64 from the current stream and advances the current position of the stream by packed Int64 size.
        /// </summary>
        public static long ReadPackedInt64(this BinaryReader reader, int bits)
        {
            byte[] b = reader.ReadBytes((32 - bits) >> 3);

            long i64 = 0;
            for (int i = 0; i < b.Length; i++)
                i64 |= (long)b[i] << i * 8;

            return i64;
        }
        #endregion

        #region ReadPackedUInt64
        /// <summary>
        ///  Reads the packed UInt64 from the current stream and advances the current position of the stream by packed UInt64 size.
        /// </summary>
        public static ulong ReadPackedUInt64(this BinaryReader reader, int bits)
        {
            byte[] b = reader.ReadBytes((32 - bits) >> 3);

            ulong u64 = 0;
            for (int i = 0; i < b.Length; i++)
                u64 |= (ulong)b[i] << i * 8;

            return u64;
        }
        #endregion

        public static T Read<T>(this BinaryReader reader, ColumnMeta meta) where T : struct
        {
            TypeCode code = Type.GetTypeCode(typeof(T));

            object value = null;

            switch (code)
            {
                case TypeCode.Byte:
                    if (meta != null && meta.Bits != 0x18)
                        throw new Exception("TypeCode.Byte Unknown meta.Flags");
                    value = reader.ReadByte();
                    break;
                case TypeCode.SByte:
                    if (meta != null && meta.Bits != 0x18)
                        throw new Exception("TypeCode.SByte Unknown meta.Flags");
                    value = reader.ReadSByte();
                    break;
                case TypeCode.Int16:
                    if (meta != null && meta.Bits != 0x10)
                        throw new Exception("TypeCode.Int16 Unknown meta.Flags");
                    value = reader.ReadInt16();
                    break;
                case TypeCode.UInt16:
                    if (meta != null && meta.Bits != 0x10)
                        throw new Exception("TypeCode.UInt16 Unknown meta.Flags");
                    value = reader.ReadUInt16();
                    break;
                case TypeCode.Int32:
                    if (meta == null)
                        value = reader.ReadInt32();
                    else
                        value = reader.ReadPackedInt32(meta.Bits);
                    break;
                case TypeCode.UInt32:
                    if (meta == null)
                        value = reader.ReadUInt32();
                    else
                        value = reader.ReadPackedUInt32(meta.Bits);
                    break;
                case TypeCode.Int64:
                    if (meta == null)
                        value = reader.ReadInt64();
                    else
                        value = reader.ReadPackedInt64(meta.Bits);
                    break;
                case TypeCode.UInt64:
                    if (meta == null)
                        value = reader.ReadUInt64();
                    else
                        value = reader.ReadPackedUInt64(meta.Bits);
                    break;
                case TypeCode.String:
                    if (meta != null && meta.Bits != 0x00)
                        throw new Exception("TypeCode.String Unknown meta.Flags");
                    value = reader.ReadStringNull();
                    break;
                case TypeCode.Single:
                    if (meta != null && meta.Bits != 0x00)
                        throw new Exception("TypeCode.Single Unknown meta.Flags");
                    value = reader.ReadSingle();
                    break;
                default:
                    throw new Exception("Unknown TypeCode " + code);
            }

            return (T)value;
        }
    }
}
