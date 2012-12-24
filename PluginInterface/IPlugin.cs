using System;
using System.Data;

namespace PluginInterface
{
    public interface IPlugin
    {
        // main plugin method
        void Run(DataTable data);
        // callback to main program
        Action<int> Finished { get; set; }
    }
}
