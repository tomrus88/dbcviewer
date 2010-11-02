using System;
using System.Collections.Generic;

namespace dbc2sql
{
    class StringTable : Dictionary<int, string>
    {
        public StringTable()
            : base()
        {
        }

        public new string this[int offset]
        {
            get
            {
                if(base.ContainsKey(offset))
                    return base[offset];
                return String.Empty;
            }
            set { base[offset] = value; }
        }
    }
}
