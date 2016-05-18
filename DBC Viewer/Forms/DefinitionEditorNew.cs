using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBCViewer
{
    public partial class DefinitionEditorNew : Form
    {
        private DataGridViewRow rowToDrag;
        private Field fieldToDrag;
        private string m_name;
        private bool m_changed;
        private bool m_saved;
        private MainForm m_mainForm;

        public DefinitionEditorNew(MainForm mainForm)
        {
            m_mainForm = mainForm;

            InitializeComponent();
        }

        private void doneButton_Click(object sender, EventArgs e)
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
            var fields = (List<Field>)editorDataGridView.DataSource;

            var names = from Field i in fields select i.Name;
            if (names.Distinct().Count() != names.Count())
                return false;
            return true;
        }

        private void WriteXml()
        {
            string docPath = Path.Combine(m_mainForm.WorkingFolder, "dblayout.xml");

            DBFilesClient doc = DBFilesClient.Load(docPath);

            var nodes = doc.Tables.Where(t => t.Name == m_name);

            Table oldnode;

            if (nodes.Count() == 1)
                oldnode = nodes.First();
            else if (nodes.Count() > 1)
                oldnode = nodes.ElementAt(m_mainForm.DefinitionIndex);
            else
                oldnode = null;

            Table newnode = new Table() { Name = m_name };
            newnode.Build = Convert.ToInt32(textBox1.Text);
            newnode.Fields = (List<Field>)editorDataGridView.DataSource;
            //newnode.Fields = new List<Field>();

            //foreach (ListViewItem item in listView1.Items)
            //{
            //    Field ele = new Field();
            //    ele.Name = item.SubItems[1].Text;
            //    ele.Type = item.SubItems[2].Text;
            //    ele.IsIndex = item.SubItems[3].Text == "True";
            //    ele.ArraySize = 1;
            //    ele.Format = string.Empty;
            //    ele.Visible = true;
            //    ele.Width = 0;

            //    newnode.Fields.Add(ele);
            //}

            if (oldnode == null || oldnode.Build != newnode.Build)
                doc.Tables.Add(newnode);
            else
            {
                int index = doc.Tables.IndexOf(oldnode);
                doc.Tables.RemoveAt(index);
                doc.Tables.Insert(index, newnode);
            }

            DBFilesClient.Save(doc, docPath);
            m_saved = true;
        }

        public void InitDefinitions()
        {
            m_name = m_mainForm.DBCName;

            Table def = m_mainForm.Definition;

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
                    MessageBox.Show(string.Format("Can't create default definitions for {0}", m_name));

                    def = new Table();
                    def.Name = m_name;
                    def.Fields = new List<Field>();
                }
            }

            InitForm(def);
        }

        private void InitForm(Table def)
        {
            textBox1.Text = def.Build.ToString();

            editorDataGridView.DataSource = def.Fields;

            for (int i = 0; i < def.Fields.Count; i++)
                def.Fields[i].Index = i;
        }

        private Table CreateDefaultDefinition()
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

                // only for files with 4 byte fields (most of dbc's)
                if ((recordsize % fieldsCount == 0) && (fieldsCount * 4 == recordsize))
                {
                    var doc = new Table();

                    doc.Build = Convert.ToInt32(textBox1.Text);

                    for (int i = 0; i < fieldsCount; ++i)
                    {
                        var field = new Field();

                        if (i == 0)
                        {
                            field.IsIndex = true;
                            field.Name = "m_ID";
                        }
                        else
                        {
                            field.Name = string.Format("field{0}", i);
                        }

                        field.Type = "int";

                        doc.Fields.Add(field);
                    }

                    m_changed = true;
                    return doc;
                }
            }

            return null;
        }

        private void DefinitionEditorNew_Load(object sender, EventArgs e)
        {
            editorDataGridView.AutoGenerateColumns = false;

            InitDefinitions();
            //var temp = editorDataGridView.RowTemplate;

            //temp.CreateCells(editorDataGridView);

            //temp.Cells[0].ValueType = typeof(int);
            //temp.Cells[0].Value = 0;

            //temp.Cells[1].ValueType = typeof(string);
            //temp.Cells[1].Value = "field";

            //temp.Cells[2].ValueType = typeof(string);
            //(temp.Cells[2] as DataGridViewComboBoxCell).Items.AddRange(fieldTypes);

            //temp.Cells[4].ValueType = typeof(int);
            //temp.Cells[4].Value = 1;
        }

        private void editorDataGridView_DragDrop(object sender, DragEventArgs e)
        {
            Point clientPoint = editorDataGridView.PointToClient(new Point(e.X, e.Y));
            int dragToIndex = editorDataGridView.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            if (dragToIndex == -1 || rowToDrag.Index == -1 || dragToIndex == rowToDrag.Index)
                return;

            List<Field> fields = (List<Field>)editorDataGridView.DataSource;
            fields.RemoveAt(rowToDrag.Index);
            fields.Insert(dragToIndex, fieldToDrag);
            editorDataGridView.DataSource = null;
            editorDataGridView.DataSource = fields;
            //editorDataGridView.Rows.RemoveAt(rowToDrag.Index);
            //editorDataGridView.Rows.Insert(dragToIndex, rowToDrag);
        }

        private void editorDataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            int rowToDragIndex = editorDataGridView.HitTest(e.X, e.Y).RowIndex;

            if (rowToDragIndex == -1)
                return;

            rowToDrag = editorDataGridView.Rows[rowToDragIndex];
            fieldToDrag = (editorDataGridView.DataSource as List<Field>)[rowToDragIndex];
            editorDataGridView.DoDragDrop(rowToDrag, DragDropEffects.Move);
        }

        private void editorDataGridView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void DefinitionEditorNew_FormClosing(object sender, FormClosingEventArgs e)
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
