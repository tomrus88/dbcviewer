using System;
using System.Data;
using System.Globalization;

namespace DBCViewer
{
    partial class FilterForm
    {
        private bool Equal<T>(DataRow row) where T : IComparable<T>
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "==" || row[filter.Col].GetType() != typeof(T))
                    continue;
                if (row.Field<T>(filter.Col).CompareTo((T)Convert.ChangeType(filter.Val, typeof(T), CultureInfo.InvariantCulture)) == 0)
                    result = true;
            }

            return result;
        }

        private bool NotEqual<T>(DataRow row) where T : IComparable<T>
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "!=" || row[filter.Col].GetType() != typeof(T))
                    continue;
                if (row.Field<T>(filter.Col).CompareTo((T)Convert.ChangeType(filter.Val, typeof(T), CultureInfo.InvariantCulture)) != 0)
                    result = true;
            }

            return result;
        }

        private bool Less<T>(DataRow row) where T : IComparable<T>
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "<" || row[filter.Col].GetType() != typeof(T))
                    continue;
                if (row.Field<T>(filter.Col).CompareTo((T)Convert.ChangeType(filter.Val, typeof(T), CultureInfo.InvariantCulture)) < 0)
                    result = true;
            }

            return result;
        }

        private bool Greater<T>(DataRow row) where T : IComparable<T>
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != ">" || row[filter.Col].GetType() != typeof(T))
                    continue;
                if (row.Field<T>(filter.Col).CompareTo((T)Convert.ChangeType(filter.Val, typeof(T), CultureInfo.InvariantCulture)) > 0)
                    result = true;
            }

            return result;
        }

        private bool StartWith(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "*__")
                    continue;
                if (row.Field<string>(filter.Col).StartsWith(filter.Val, StringComparison.Ordinal))
                    result = true;
            }

            return result;
        }

        private bool StartWithNoCase(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "*__")
                    continue;
                if (row.Field<string>(filter.Col).StartsWith(filter.Val, StringComparison.OrdinalIgnoreCase))
                    result = true;
            }

            return result;
        }

        private bool EndsWith(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "__*")
                    continue;
                if (row.Field<string>(filter.Col).EndsWith(filter.Val, StringComparison.Ordinal))
                    result = true;
            }

            return result;
        }

        private bool EndsWithNoCase(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "__*")
                    continue;
                if (row.Field<string>(filter.Col).EndsWith(filter.Val, StringComparison.OrdinalIgnoreCase))
                    result = true;
            }

            return result;
        }

        private bool Contains(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "_*_")
                    continue;
                if (row.Field<string>(filter.Col).Contains(filter.Val))
                    result = true;
            }

            return result;
        }

        private bool ContainsNoCase(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "_*_")
                    continue;
                if (row.Field<string>(filter.Col).ToUpperInvariant().Contains(filter.Val.ToUpperInvariant()))
                    result = true;
            }

            return result;
        }

        private bool AndUnsigned<T>(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "&")
                    continue;
                if (((ulong)Convert.ChangeType(row.Field<T>(filter.Col), typeof(ulong), CultureInfo.InvariantCulture) & Convert.ToUInt64(filter.Val, CultureInfo.InvariantCulture)) != 0)
                    result = true;
            }

            return result;
        }

        private bool AndSigned<T>(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "&")
                    continue;
                if (((long)Convert.ChangeType(row.Field<T>(filter.Col), typeof(long), CultureInfo.InvariantCulture) & Convert.ToInt64(filter.Val, CultureInfo.InvariantCulture)) != 0)
                    result = true;
            }

            return result;
        }

        private bool AndNotUnsigned<T>(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "~&")
                    continue;
                if (((ulong)Convert.ChangeType(row.Field<T>(filter.Col), typeof(ulong), CultureInfo.InvariantCulture) & Convert.ToUInt64(filter.Val, CultureInfo.InvariantCulture)) == 0)
                    result = true;
            }

            return result;
        }

        private bool AndNotSigned<T>(DataRow row)
        {
            var result = false;

            foreach (var filter in m_filters.Values)
            {
                if (filter.Op != "~&")
                    continue;
                if (((long)Convert.ChangeType(row.Field<T>(filter.Col), typeof(long), CultureInfo.InvariantCulture) & Convert.ToInt64(filter.Val, CultureInfo.InvariantCulture)) == 0)
                    result = true;
            }

            return result;
        }
    }
}
