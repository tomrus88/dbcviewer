using System;
using System.Data;

namespace DBCViewer
{
    partial class FilterForm
    {
        private bool FilterDouble(string op)
        {
            switch (op)
            {
                case "==":
                    m_filter = m_filter.Where(Equal<double>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<double>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<double>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<double>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterSingle(string op)
        {
            switch (op)
            {
                case "==":
                    m_filter = m_filter.Where(Equal<float>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<float>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<float>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<float>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterUInt8(string op)
        {
            switch (op)
            {
                case "&":
                    m_filter = m_filter.Where(AndUnsigned<byte>);
                    break;
                case "~&":
                    m_filter = m_filter.Where(AndNotUnsigned<byte>);
                    break;
                case "==":
                    m_filter = m_filter.Where(Equal<byte>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<byte>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<byte>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<byte>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterInt8(string op)
        {
            switch (op)
            {
                case "&":
                    m_filter = m_filter.Where(AndSigned<sbyte>);
                    break;
                case "~&":
                    m_filter = m_filter.Where(AndNotSigned<sbyte>);
                    break;
                case "==":
                    m_filter = m_filter.Where(Equal<sbyte>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<sbyte>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<sbyte>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<sbyte>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterUInt16(string op)
        {
            switch (op)
            {
                case "&":
                    m_filter = m_filter.Where(AndUnsigned<ushort>);
                    break;
                case "~&":
                    m_filter = m_filter.Where(AndNotUnsigned<ushort>);
                    break;
                case "==":
                    m_filter = m_filter.Where(Equal<ushort>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<ushort>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<ushort>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<ushort>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterInt16(string op)
        {
            switch (op)
            {
                case "&":
                    m_filter = m_filter.Where(AndSigned<short>);
                    break;
                case "~&":
                    m_filter = m_filter.Where(AndNotSigned<short>);
                    break;
                case "==":
                    m_filter = m_filter.Where(Equal<short>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<short>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<short>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<short>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterUInt32(string op)
        {
            switch (op)
            {
                case "&":
                    m_filter = m_filter.Where(AndUnsigned<uint>);
                    break;
                case "~&":
                    m_filter = m_filter.Where(AndNotUnsigned<uint>);
                    break;
                case "==":
                    m_filter = m_filter.Where(Equal<uint>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<uint>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<uint>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<uint>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterInt32(string op)
        {
            switch (op)
            {
                case "&":
                    m_filter = m_filter.Where(AndSigned<int>);
                    break;
                case "~&":
                    m_filter = m_filter.Where(AndNotSigned<int>);
                    break;
                case "==":
                    m_filter = m_filter.Where(Equal<int>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<int>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<int>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<int>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterUInt64(string op)
        {
            switch (op)
            {
                case "&":
                    m_filter = m_filter.Where(AndUnsigned<ulong>);
                    break;
                case "~&":
                    m_filter = m_filter.Where(AndNotUnsigned<ulong>);
                    break;
                case "==":
                    m_filter = m_filter.Where(Equal<ulong>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<ulong>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<ulong>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<ulong>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterInt64(string op)
        {
            switch (op)
            {
                case "&":
                    m_filter = m_filter.Where(AndSigned<long>);
                    break;
                case "~&":
                    m_filter = m_filter.Where(AndNotSigned<long>);
                    break;
                case "==":
                    m_filter = m_filter.Where(Equal<long>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<long>);
                    break;
                case "<":
                    m_filter = m_filter.Where(Less<long>);
                    break;
                case ">":
                    m_filter = m_filter.Where(Greater<long>);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool FilterString(string op)
        {
            switch (op)
            {
                case "==":
                    m_filter = m_filter.Where(Equal<string>);
                    break;
                case "!=":
                    m_filter = m_filter.Where(NotEqual<string>);
                    break;
                case "*__":
                    if (checkBox2.Checked)
                        m_filter = m_filter.Where(StartWithNoCase);
                    else
                        m_filter = m_filter.Where(StartWith);
                    break;
                case "__*":
                    if (checkBox2.Checked)
                        m_filter = m_filter.Where(EndsWithNoCase);
                    else
                        m_filter = m_filter.Where(EndsWith);
                    break;
                case "_*_":
                    if (checkBox2.Checked)
                        m_filter = m_filter.Where(ContainsNoCase);
                    else
                        m_filter = m_filter.Where(Contains);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
