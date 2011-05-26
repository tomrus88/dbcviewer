using System;
using System.Data;
using System.Globalization;

namespace DBCViewer
{
    public enum ComparisonType
    {
        Equal,
        NotEqual,
        Less,
        Greater,
        StartWith,
        EndsWith,
        Contains,
        And,
        AndNot
    }

    partial class FilterForm
    {
        private bool Compare(DataRow row)
        {
            var matches = 0;
            var checks = 0;

            foreach (var filter in m_filters.Values)
            {
                checks++;

                var type = row[filter.Col].GetType();

                var value1 = (IComparable)row[filter.Col];
                var value2 = (IComparable)Convert.ChangeType(filter.Val, type, CultureInfo.InvariantCulture);

                switch (filter.Type)
                {
                    case ComparisonType.And:
                        if (And(type, filter, row))
                            matches++;
                        break;
                    case ComparisonType.AndNot:
                        if (AndNot(type, filter, row))
                            matches++;
                        break;
                    case ComparisonType.Contains:
                        if (Contains(filter, row))
                            matches++;
                        break;
                    case ComparisonType.EndsWith:
                        if (EndsWith(filter, row))
                            matches++;
                        break;
                    case ComparisonType.Equal:
                        if (Equal(type, filter, row))
                            matches++;
                        break;
                    case ComparisonType.Greater:
                        if (Greater(type, filter, row))
                            matches++;
                        break;
                    case ComparisonType.Less:
                        if (Less(type, filter, row))
                            matches++;
                        break;
                    case ComparisonType.NotEqual:
                        if (NotEqual(type, filter, row))
                            matches++;
                        break;
                    case ComparisonType.StartWith:
                        if (StartWith(filter, row))
                            matches++;
                        break;
                    default:
                        break;
                }
            }

            return checks == matches;
        }

        private bool Equal(Type type, FilterOptions filter, DataRow row)
        {
            var value1 = (IComparable)row[filter.Col];
            var value2 = (IComparable)Convert.ChangeType(filter.Val, type, CultureInfo.InvariantCulture);

            if (value1.CompareTo(value2) == 0)
                return true;

            return false;
        }

        private bool NotEqual(Type type, FilterOptions filter, DataRow row)
        {
            var value1 = (IComparable)row[filter.Col];
            var value2 = (IComparable)Convert.ChangeType(filter.Val, type, CultureInfo.InvariantCulture);

            if (value1.CompareTo(value2) != 0)
                return true;

            return false;
        }

        private bool Less(Type type, FilterOptions filter, DataRow row)
        {
            var value1 = (IComparable)row[filter.Col];
            var value2 = (IComparable)Convert.ChangeType(filter.Val, type, CultureInfo.InvariantCulture);

            if (value1.CompareTo(value2) < 0)
                return true;

            return false;
        }

        private bool Greater(Type type, FilterOptions filter, DataRow row)
        {
            var value1 = (IComparable)row[filter.Col];
            var value2 = (IComparable)Convert.ChangeType(filter.Val, type, CultureInfo.InvariantCulture);

            if (value1.CompareTo(value2) > 0)
                return true;

            return false;
        }

        private bool StartWith(FilterOptions filter, DataRow row)
        {
            if (row.Field<string>(filter.Col).StartsWith(filter.Val, checkBox2.Checked ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return true;

            return false;
        }

        private bool EndsWith(FilterOptions filter, DataRow row)
        {
            if (row.Field<string>(filter.Col).EndsWith(filter.Val, checkBox2.Checked ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return true;

            return false;
        }

        private bool Contains(FilterOptions filter, DataRow row)
        {
            if (checkBox2.Checked)
            {
                if (row.Field<string>(filter.Col).ToUpperInvariant().Contains(filter.Val.ToUpperInvariant()))
                    return true;

                return false;
            }
            else
            {
                if (row.Field<string>(filter.Col).Contains(filter.Val))
                    return true;

                return false;
            }
        }

        private bool And(Type type, FilterOptions filter, DataRow row)
        {
            var typeCode = Type.GetTypeCode(type);

            if (typeCode == TypeCode.Byte || typeCode == TypeCode.UInt16 || typeCode == TypeCode.UInt32 || typeCode == TypeCode.UInt64)
            {
                if (((ulong)Convert.ChangeType(row[filter.Col], typeof(ulong), CultureInfo.InvariantCulture) & Convert.ToUInt64(filter.Val, CultureInfo.InvariantCulture)) != 0)
                    return true;

                return false;
            }
            else if (typeCode == TypeCode.SByte || typeCode == TypeCode.Int16 || typeCode == TypeCode.Int32 || typeCode == TypeCode.Int64)
            {
                if (((long)Convert.ChangeType(row[filter.Col], typeof(long), CultureInfo.InvariantCulture) & Convert.ToInt64(filter.Val, CultureInfo.InvariantCulture)) != 0)
                    return true;

                return false;
            }
            else
                return false;
        }

        private bool AndNot(Type type, FilterOptions filter, DataRow row)
        {
            var typeCode = Type.GetTypeCode(type);

            if (typeCode == TypeCode.Byte || typeCode == TypeCode.UInt16 || typeCode == TypeCode.UInt32 || typeCode == TypeCode.UInt64)
            {
                if (((ulong)Convert.ChangeType(row[filter.Col], typeof(ulong), CultureInfo.InvariantCulture) & Convert.ToUInt64(filter.Val, CultureInfo.InvariantCulture)) == 0)
                    return true;

                return false;
            }
            else if (typeCode == TypeCode.SByte || typeCode == TypeCode.Int16 || typeCode == TypeCode.Int32 || typeCode == TypeCode.Int64)
            {
                if (((long)Convert.ChangeType(row[filter.Col], typeof(long), CultureInfo.InvariantCulture) & Convert.ToInt64(filter.Val, CultureInfo.InvariantCulture)) == 0)
                    return true;

                return false;
            }
            else
                return false;
        }
    }
}
