using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using PluginInterface;

namespace DBCViewer
{
    public partial class PluginsForm : Form
    {
        public int PluginIndex { get; private set; }

        public PluginsForm()
        {
            InitializeComponent();
        }

        public void SetPlugins(IList<IPlugin> plugins)
        {
            foreach (IPlugin plugin in plugins)
            {
                var item = String.Format(CultureInfo.InvariantCulture, "{0}", plugin.GetType().Name);
                listBox1.Items.Add(item);
            }

            listBox1.SelectedIndex = 0;
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PluginIndex = listBox1.SelectedIndex;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
