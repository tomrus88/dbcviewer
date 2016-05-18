using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBCViewer
{
    public partial class DefinitionEditor : Form
    {
        private DataGridViewRow rowToDrag;
        private bool m_changed;
        private bool m_saved;
        private MainForm m_mainForm;
        private Table editingTable;

        public DefinitionEditor(MainForm mainForm)
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
            var names = from Field i in editingTable.Fields select i.Name;
            if (names.Distinct().Count() != names.Count())
                return false;
            return true;
        }

        private void WriteXml()
        {
            string docPath = Path.Combine(m_mainForm.WorkingFolder, "dblayout.xml");

            Table newnode = new Table();
            newnode.Name = m_mainForm.DBCName;
            newnode.Build = Convert.ToInt32(textBox1.Text);
            newnode.Fields = new List<Field>(editingTable.Fields);

            if (editingTable.Build != newnode.Build)
                m_mainForm.Definitions.Tables.Add(newnode);

            DBFilesClient.Save(m_mainForm.Definitions, docPath);
            m_saved = true;
        }

        public void InitDefinitions()
        {
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
                    MessageBox.Show(string.Format("Can't create default definitions for {0}", m_mainForm.DBCName));

                    def = new Table();
                    def.Name = m_mainForm.DBCName;
                    def.Fields = new List<Field>();
                }
            }

            InitForm(def);
        }

        private void InitForm(Table def)
        {
            editingTable = def;

            textBox1.Text = def.Build.ToString();

            for (int i = 0; i < def.Fields.Count; i++)
                def.Fields[i].Index = i;

            editorDataGridView.DataSource = null;
            editorDataGridView.DataMember = "Fields";
            editorDataGridView.DataSource = def;
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
        }

        private void editorDataGridView_DragDrop(object sender, DragEventArgs e)
        {
            Point clientPoint = editorDataGridView.PointToClient(new Point(e.X, e.Y));
            int dragToIndex = editorDataGridView.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            if (dragToIndex == -1 || rowToDrag.Index == -1 || dragToIndex == rowToDrag.Index)
                return;

            Field removed = editingTable.Fields[rowToDrag.Index];
            editingTable.Fields.RemoveAt(rowToDrag.Index);
            editingTable.Fields.Insert(dragToIndex, removed);

            InitForm(editingTable);
        }

        private void editorDataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            int rowToDragIndex = editorDataGridView.HitTest(e.X, e.Y).RowIndex;

            if (rowToDragIndex == -1)
                return;

            rowToDrag = editorDataGridView.Rows[rowToDragIndex];
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
