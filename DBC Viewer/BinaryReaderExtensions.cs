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
            string coords = String.Empty;

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
            string coords = String.Empty;

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
            string text = String.Empty;
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
            string text = String.Empty;
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

        public static T Read<T>(this BinaryReader reader, ColumnMeta meta) where T : struct
        {
            TypeCode code = Type.GetTypeCode(typeof(T));

            object value = null;

            switch (code)
            {
                case TypeCode.Byte:
                    if (meta != null && meta.Flags != 0x18)
                        throw new Exception("TypeCode.Byte Unknown meta.Flags");
                    value = reader.ReadByte();
                    break;
                case TypeCode.SByte:
                    if (meta != null && meta.Flags != 0x18)
                        throw new Exception("TypeCode.SByte Unknown meta.Flags");
                    value = reader.ReadSByte();
                    break;
                case TypeCode.Int16:
                    if (meta != null && meta.Flags != 0x10)
                        throw new Exception("TypeCode.Int16 Unknown meta.Flags");
                    value = reader.ReadInt16();
                    break;
                case TypeCode.UInt16:
                    if (meta != null && meta.Flags != 0x10)
                        throw new Exception("TypeCode.UInt16 Unknown meta.Flags");
                    value = reader.ReadUInt16();
                    break;
                case TypeCode.Int32:
                    if (meta == null || meta.Flags == 0x00)
                        value = reader.ReadInt32();
                    else if (meta.Flags == 0x08)
                    {
                        byte[] b = reader.ReadBytes(3);
                        value = b[0] | b[1] << 8 | b[2] << 16;
                    }
                    else if (meta.Flags == 0x10)
                    {
                        byte[] b = reader.ReadBytes(2);
                        value = b[0] | b[1] << 8;
                    }
                    else if (meta.Flags == 0x18)
                        value = (int)reader.ReadByte();
                    else
                        throw new Exception("TypeCode.Int32 Unknown meta.Flags");
                    break;
                case TypeCode.UInt32:
                    if (meta == null || meta.Flags == 0x00)
                        value = reader.ReadUInt32();
                    else if (meta.Flags == 0x08)
                    {
                        byte[] b = reader.ReadBytes(3);
                        value = b[0] | (uint)(b[1] << 8) | (uint)(b[2] << 16);
                    }
                    else if (meta.Flags == 0x10)
                    {
                        byte[] b = reader.ReadBytes(2);
                        value = b[0] | (uint)(b[1] << 8);
                    }
                    else if (meta.Flags == 0x18)
                        value = (uint)reader.ReadByte();
                    else
                        throw new Exception("TypeCode.UInt32 Unknown meta.Flags");
                    break;
                case TypeCode.Int64:
                    value = reader.ReadInt64();
                    break;
                case TypeCode.UInt64:
                    value = reader.ReadUInt64();
                    break;
                case TypeCode.String:
                    if (meta != null && meta.Flags != 0x00)
                        throw new Exception("TypeCode.String Unknown meta.Flags");
                    value = reader.ReadStringNull();
                    break;
                case TypeCode.Single:
                    if (meta != null && meta.Flags != 0x00)
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
