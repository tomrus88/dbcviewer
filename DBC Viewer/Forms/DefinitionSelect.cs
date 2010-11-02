using System;
using System.Globalization;
using System.Windows.Forms;
using System.Xml;

namespace DBCViewer
{
    public partial class DefinitionSelect : Form
    {
        public int DefinitionIndex { get; private set; }

        public DefinitionSelect()
        {
            InitializeComponent();
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DefinitionIndex = listBox1.SelectedIndex;
            DialogResult = DialogResult.OK;
            Close();
        }

        public void SetDefinitions(XmlNodeList definitions)
        {
            foreach (XmlElement def in definitions)
            {
                var item = String.Format(CultureInfo.InvariantCulture, "{0} (build {1})", def.Name, def.Attributes["build"].Value);
                listBox1.Items.Add(item);
            }

            listBox1.SelectedIndex = 0;
        }
    }
}
