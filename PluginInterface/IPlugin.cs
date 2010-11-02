using System;
using System.Data;

namespace PluginInterface
{
    public interface IPlugin
    {
        // main plugin method
        void Run(DataTable data);
        // callback to main program
        Func<int, int> Finished { get; set; }
    }
}
