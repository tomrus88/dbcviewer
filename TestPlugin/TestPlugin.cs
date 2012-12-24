using System;
using System.ComponentModel.Composition;
using System.Data;
using System.Diagnostics;
using PluginInterface;

namespace TestPlugin
{
    [Export(typeof(IPlugin))]
    public class FactionTemplateDebug : IPlugin
    {
        [Import("PluginFinished")]
        public Action<int> Finished { get; set; }

        public void Run(DataTable data)
        {
            int count = 0;

            foreach (DataRow row in data.Rows)
            {
                uint flags = (uint)row[2];

                uint reaction = ~(flags >> 12) & 2 | 1; // flags & 0x2000 ? 1 : 3

                if (reaction != 0)
                {
                    Debug.Print("template {0}, reaction {1}", row[0], reaction);
                    count++;
                }
            }

            Finished(count);
        }
    }
}
