using PluginInterface;
using System.IO;
using System.Windows.Forms;

namespace DBCViewer
{
    public partial class DefinitionCatalog : Form
    {
        private int DefinitionIndex;

        public DefinitionCatalog()
        {
            InitializeComponent();
        }

        public static DBFilesClient SelectCatalog(string path)
        {
            using (var selector = new DefinitionCatalog())
            {
                var files = Directory.GetFiles(Path.Combine(path, "definitions"), "*.xml");

                foreach (var file in files)
                    selector.listBox1.Items.Add(Path.GetFileName(file));

                selector.ShowDialog();

                if (selector.DefinitionIndex != -1)
                    return DBFilesClient.Load(files[selector.DefinitionIndex]);

                return null;
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DefinitionIndex = listBox1.IndexFromPoint(e.Location);
            if (DefinitionIndex == -1)
                return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
