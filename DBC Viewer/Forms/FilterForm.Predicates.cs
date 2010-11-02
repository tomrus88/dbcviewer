using System;
using System.Data;
using System.Globalization;

namespace DBCViewer
{
    partial class FilterForm
    {
        private bool Equal<T>(DataRow row) where T : IComparable<T>
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "==")
                    continue;
                if (row.Field<T>(filter.Col).CompareTo((T)Convert.ChangeType(filter.Val, typeof(T), CultureInfo.InvariantCulture)) == 0)
                    return true;
            }

            return false;
        }

        private bool NotEqual<T>(DataRow row) where T : IComparable<T>
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "!=")
                    continue;
                if (row.Field<T>(filter.Col).CompareTo((T)Convert.ChangeType(filter.Val, typeof(T), CultureInfo.InvariantCulture)) != 0)
                    return true;
            }

            return false;
        }

        private bool Less<T>(DataRow row) where T : IComparable<T>
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "<")
                    continue;
                if (row.Field<T>(filter.Col).CompareTo((T)Convert.ChangeType(filter.Val, typeof(T), CultureInfo.InvariantCulture)) < 0)
                    return true;
            }

            return false;
        }

        private bool Greater<T>(DataRow row) where T : IComparable<T>
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != ">")
                    continue;
                if (row.Field<T>(filter.Col).CompareTo((T)Convert.ChangeType(filter.Val, typeof(T), CultureInfo.InvariantCulture)) > 0)
                    return true;
            }

            return false;
        }

        private bool StartWith(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "*__")
                    continue;
                if (row.Field<string>(filter.Col).StartsWith(filter.Val, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private bool StartWithNoCase(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "*__")
                    continue;
                if (row.Field<string>(filter.Col).StartsWith(filter.Val, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool EndsWith(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "__*")
                    continue;
                if (row.Field<string>(filter.Col).EndsWith(filter.Val, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private bool EndsWithNoCase(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "__*")
                    continue;
                if (row.Field<string>(filter.Col).EndsWith(filter.Val, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool Contains(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "_*_")
                    continue;
                if (row.Field<string>(filter.Col).Contains(filter.Val))
                    return true;
            }

            return false;
        }

        private bool ContainsNoCase(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "_*_")
                    continue;
                if (row.Field<string>(filter.Col).ToUpperInvariant().Contains(filter.Val.ToUpperInvariant()))
                    return true;
            }

            return false;
        }

        private bool AndUnsigned<T>(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "&")
                    continue;
                if (((ulong)Convert.ChangeType(row.Field<T>(filter.Col), typeof(ulong), CultureInfo.InvariantCulture) & Convert.ToUInt64(filter.Val, CultureInfo.InvariantCulture)) != 0)
                    return true;
            }

            return false;
        }

        private bool AndSigned<T>(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "&")
                    continue;
                if (((long)Convert.ChangeType(row.Field<T>(filter.Col), typeof(long), CultureInfo.InvariantCulture) & Convert.ToInt64(filter.Val, CultureInfo.InvariantCulture)) != 0)
                    return true;
            }

            return false;
        }

        private bool AndNotUnsigned<T>(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "~&")
                    continue;
                if (((ulong)Convert.ChangeType(row.Field<T>(filter.Col), typeof(ulong), CultureInfo.InvariantCulture) & Convert.ToUInt64(filter.Val, CultureInfo.InvariantCulture)) == 0)
                    return true;
            }

            return false;
        }

        private bool AndNotSigned<T>(DataRow row)
        {
            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "~&")
                    continue;
                if (((long)Convert.ChangeType(row.Field<T>(filter.Col), typeof(long), CultureInfo.InvariantCulture) & Convert.ToInt64(filter.Val, CultureInfo.InvariantCulture)) == 0)
                    return true;
            }

            return false;
        }
    }
}
