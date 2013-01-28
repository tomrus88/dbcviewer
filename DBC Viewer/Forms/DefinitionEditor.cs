using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace DBCViewer
{
    public partial class DefinitionEditor : Form
    {
        private ListViewItem.ListViewSubItem m_listViewSubItem;
        private readonly string[] comboBoxItems1 = new string[] { "long", "ulong", "int", "uint", "short", "ushort", "sbyte", "byte", "float", "double", "string" };
        private readonly string[] comboBoxItems2 = new string[] { "True", "False" };
        private string m_name;
        private bool m_changed;
        private bool m_saved;
        private MainForm m_mainForm;

        public DefinitionEditor()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!CheckColumns())
            {
                MessageBox.Show("Column names aren't unique. Please fix them first.");
                return;
            }
            WriteXml();
            Close();
        }

        private bool CheckColumns()
        {
            var names = from ListViewItem i in listView1.Items select i.SubItems[1].Text;
            if (names.Distinct().Count() != names.Count())
                return false;
            return true;
        }

        private void WriteXml()
        {
            XmlDocument doc = new XmlDocument();

            string docPath = Path.Combine(m_mainForm.WorkingFolder, "dbclayout.xml");

            doc.Load(docPath);

            XmlNode oldnode = null; // nodes.Count == 0

            XmlNodeList nodes = doc["DBFilesClient"].GetElementsByTagName(m_name);

            if (nodes.Count == 1)
                oldnode = nodes[0];
            else if (nodes.Count > 1)
                oldnode = nodes[m_mainForm.DefinitionIndex];

            XmlElement newnode = doc.CreateElement(m_name);
            newnode.SetAttributeNode("build", "").Value = textBox1.Text;

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.SubItems[3].Text == "True")
                {
                    XmlElement index = doc.CreateElement("index");
                    XmlNode primary = index.AppendChild(doc.CreateElement("primary"));
                    primary.InnerText = item.SubItems[1].Text;
                    newnode.AppendChild(index);
                }

                XmlElement ele = doc.CreateElement("field");
                ele.SetAttributeNode("type", "").Value = item.SubItems[2].Text;
                ele.SetAttributeNode("name", "").Value = item.SubItems[1].Text;
                newnode.AppendChild(ele);
            }

            if (oldnode == null || oldnode.Attributes["build"].Value != textBox1.Text)
                doc["DBFilesClient"].AppendChild(newnode);
            else
                doc["DBFilesClient"].ReplaceChild(newnode, oldnode);

            doc.Save(docPath);
            m_saved = true;
        }

        public void InitDefinitions()
        {
            m_name = m_mainForm.DBCName;

            XmlElement def = m_mainForm.Definition;

            if (def == null)
            {
                DialogResult result = MessageBox.Show(this, "Create default definition?", "Definition Missing!",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (result != DialogResult.Yes)
                    return;

                def = CreateDefaultDefinition();
                if (def == null)
                {
                    MessageBox.Show(String.Format("Can't create default definitions for {0}", m_name));
                    return;
                }
            }

            InitForm(def);
        }

        private void InitForm(XmlElement def)
        {
            textBox1.Text = def.Attributes["build"].Value;

            XmlNodeList fields = def.GetElementsByTagName("field");
            XmlNodeList indexes = def.GetElementsByTagName("index");

            var i = 0;
            foreach (XmlNode field in fields)
            {
                listView1.Items.Add(new ListViewItem(new string[] {
                    i.ToString(),
                    field.Attributes["name"].Value,
                    field.Attributes["type"].Value,
                    IsIndexColumn(field.Attributes["name"].Value, indexes).ToString()}));
                i++;
            }
        }

        private XmlElement CreateDefaultDefinition()
        {
            var file = m_mainForm.DBCFile;

            var ext = Path.GetExtension(file).ToUpperInvariant();

            if (ext != ".DBC" && ext != ".DB2") // only for dbc and db2, as other formats have no fields count stored
                return null;

            using (var br = new BinaryReader(new FileStream(file, FileMode.Open)))
            {
                br.ReadUInt32();
                br.ReadUInt32();
                var fieldsCount = br.ReadUInt32();
                var recordsize = br.ReadUInt32();

                if (recordsize % fieldsCount == 0) // only for files with 4 byte fields
                {
                    var doc = new XmlDocument();

                    XmlElement newnode = doc.CreateElement(m_name);
                    newnode.SetAttributeNode("build", "").Value = textBox1.Text;

                    for (int i = 0; i < fieldsCount; ++i)
                    {
                        if (i == 0)
                        {
                            XmlElement index = doc.CreateElement("index");
                            XmlNode primary = index.AppendChild(doc.CreateElement("primary"));
                            primary.InnerText = "field0";
                            newnode.AppendChild(index);
                        }

                        XmlElement ele = doc.CreateElement("field");
                        ele.SetAttributeNode("type", "").Value = "int";
                        ele.SetAttributeNode("name", "").Value = String.Format("field{0}", i);
                        newnode.AppendChild(ele);
                    }

                    br.Close();
                    m_changed = true;
                    return newnode;
                }
            }

            return null;
        }

        private void DefinitionEditor_Load(object sender, EventArgs e)
        {
            m_mainForm = (MainForm)Owner;

            InitDefinitions();
        }

        private static bool IsIndexColumn(string name, XmlNodeList indexes)
        {
            foreach (XmlNode index in indexes)
                if (index["primary"].InnerText == name)
                    return true;
            return false;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            Point point = listView1.PointToClient(new Point(e.X, e.Y));
            ListViewItem dragToItem = listView1.GetItemAt(point.X, point.Y);

            if (dragToItem == null)
                return;

            ListViewItem dragFromItem = listView1.SelectedItems[0];

            var dragToIndex = dragToItem.Index;
            var dragFromIndex = dragFromItem.Index;

            if (dragToIndex == dragFromIndex)
                return;

            if (dragFromIndex < dragToIndex)
                dragToIndex++;

            ListViewItem insertItem = (ListViewItem)dragFromItem.Clone();
            listView1.Items.Insert(dragToIndex, insertItem);
            listView1.Items.Remove(dragFromItem);
            m_changed = true;
        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            var item = listView1.GetItemAt(e.X, e.Y);
            if (item != null)
                item.Selected = true;

            if (e.Button == MouseButtons.Right && listView1.SelectedItems.Count > 0)
                listView1.DoDragDrop(listView1.SelectedItems[0], DragDropEffects.Move);
            else if (e.Button == MouseButtons.Left)
            {
                if (comboBox1.Visible)
                    comboBox1.Hide();
                if (textBox2.Visible)
                    textBox2.Hide();
            }
        }

        private void listView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var item = listView1.GetItemAt(e.X, e.Y);       // Get the item that was clicked
            m_listViewSubItem = item.GetSubItemAt(e.X, e.Y);// Get the sub item that was clicked

            var column = item.SubItems.IndexOf(m_listViewSubItem);

            switch (column)
            {
                case 0: // index
                    break;
                case 1: // name
                    ShowFakeControl(textBox2);
                    break;
                case 2: // type
                    comboBox1.Items.Clear();
                    comboBox1.Items.AddRange(comboBoxItems1);
                    ShowFakeControl(comboBox1);
                    break;
                case 3: // isIndex
                    comboBox1.Items.Clear();
                    comboBox1.Items.AddRange(comboBoxItems2);
                    ShowFakeControl(comboBox1);
                    break;
                default:
                    break;
            }
        }

        private void listView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete || listView1.SelectedItems.Count == 0)
                return;

            var index = listView1.SelectedItems[0].Index;
            listView1.Items.RemoveAt(index);

            index = listView1.Items.Count > index ? index : listView1.Items.Count - 1;
            if (index == -1)
                return;

            listView1.Items[index].Selected = true;
            m_changed = true;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (var i = 0; i < listView1.Items.Count; ++i) // reorder index column
                listView1.Items[i].SubItems[0].Text = i.ToString();
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (m_listViewSubItem.Text != comboBox1.Text)
                m_changed = true;

            m_listViewSubItem.Text = comboBox1.Text;        // Set text of ListView item

            comboBox1.Hide();                               // Hide the combobox
        }

        private void comboBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)                   // Check if user pressed "ESC"
                comboBox1.Hide();                           // Hide the combobox
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ListViewItem item;
            if (listView1.SelectedItems.Count > 0)
                item = listView1.Items.Insert(listView1.SelectedItems[0].Index + 1, new ListViewItem(new string[] { "0", "newField", "int", "False" }));
            else
                item = listView1.Items.Add(new ListViewItem(new string[] { "0", "newField", "int", "False" }));
            item.Selected = true;
            m_changed = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var index = listView1.SelectedItems[0].Index;
                var cloned = (ListViewItem)listView1.Items[index].Clone();
                listView1.Items.Insert(index, cloned);
                listView1.Items[index + 1].Selected = true;
                listView1.Select();
                m_changed = true;
            }
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (m_listViewSubItem.Text != textBox2.Text)
                    m_changed = true;
                m_listViewSubItem.Text = textBox2.Text;
            }
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                textBox2.Hide();
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            if (m_listViewSubItem.Text != textBox2.Text)
                m_changed = true;
            m_listViewSubItem.Text = textBox2.Text;
            textBox2.Hide();
        }

        private void ShowFakeControl(Control control)
        {
            if (m_listViewSubItem != null)                  // Check if an actual item was clicked
            {
                var ClickedItem = m_listViewSubItem.Bounds; // Get the bounds of the item clicked

                // Adjust the top and left of the control
                ClickedItem.X += listView1.Left;
                ClickedItem.Y += listView1.Top;

                control.Bounds = ClickedItem;               // Set Control bounds to match calculation

                control.Text = m_listViewSubItem.Text;      // Set the default text for the Control to be the clicked item's text

                control.Show();                             // Show the Control
                control.BringToFront();                     // Make sure it is on top
                control.Focus();                            // Give focus to the Control
            }
        }

        private void DefinitionEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (m_changed)
                {
                    if (m_saved)
                        DialogResult = DialogResult.OK;
                    else
                        DialogResult = DialogResult.Cancel;
                }
                else
                    DialogResult = DialogResult.Abort;
            }
        }
    }
}
