using PluginInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace DBCViewer
{
    public partial class MainForm : Form
    {
        // Fields
        private DataTable m_dataTable;
        private IClientDBReader m_dbreader;
        private FilterForm m_filterForm;
        private DefinitionSelect m_selector;
        private DBFilesClient m_definitions;
        private List<Field> m_fields;
        private AggregateCatalog m_catalog;
        private Table m_definition;             // definition for current file
        private string m_dbcName;               // file name without extension
        private string m_dbcFile;               // path to current file
        private DateTime m_startTime;
        private string m_workingFolder;

        // Properties
        public DataTable DataTable { get { return m_dataTable; } }
        public string WorkingFolder { get { return m_workingFolder; } }
        public Table Definition { get { return m_definition; } }
        public DBFilesClient Definitions { get { return m_definitions; } }
        public string DBCName { get { return m_dbcName; } }
        public int DefinitionIndex { get { return m_selector != null ? m_selector.DefinitionIndex : 0; } }
        public string DBCFile { get { return m_dbcFile; } }

        // Plugins
        [ImportMany(AllowRecomposition = true)]
        List<IPlugin> Plugins { get; set; }

        [Export("PluginFinished")]
        public void PluginFinished(int result)
        {
            var msg = string.Format("Plugin finished! {0} rows affected.", result);
            statusToolStripLabel.Text = msg;
            MessageBox.Show(msg);
        }

        // MainForm
        public MainForm()
        {
            InitializeComponent();
        }

        private void LoadFile(string file)
        {
            m_dbcFile = file;
            Text = "DBC Viewer";
            SetDataSource(null);

            DisposeFilterForm();

            m_dbcName = Path.GetFileNameWithoutExtension(file);

            LoadDefinitions(); // reload in case of modification

            m_definition = GetDefinition();

            if (m_definition == null)
            {
                StartEditor();
                return;
            }

            toolStripProgressBar1.Visible = true;
            statusToolStripLabel.Text = "Loading...";

            backgroundWorker1.RunWorkerAsync(file);
        }

        private void CloseFile()
        {
            Text = "DBC Viewer";
            SetDataSource(null);

            DisposeFilterForm();

            m_definition = null;
            m_dataTable = null;
            columnsFilterToolStripMenuItem.DropDownItems.Clear();
        }

        private void DisposeFilterForm()
        {
            if (m_filterForm != null)
                m_filterForm.Dispose();
        }

        private void StartEditor()
        {
            using (DefinitionEditor editor = new DefinitionEditor(this))
            {
                var result = editor.ShowDialog();
                if (result == DialogResult.Abort)
                    return;
                if (result == DialogResult.OK)
                    LoadFile(m_dbcFile);
                else
                    MessageBox.Show("Editor canceled! You can't open that file until you add proper definitions");
            }
        }

        private Table GetDefinition()
        {
            var definitions = m_definitions.Tables.Where(t => t.Name == m_dbcName);

            if (!definitions.Any())
            {
                definitions = m_definitions.Tables.Where(t => t.Name == Path.GetFileName(m_dbcFile));
            }

            if (!definitions.Any())
            {
                return null;
            }
            else if (definitions.Count() == 1)
            {
                return definitions.First();
            }
            else
            {
                m_selector = new DefinitionSelect();
                m_selector.SetDefinitions(definitions);
                var result = m_selector.ShowDialog();
                if (result != DialogResult.OK || m_selector.DefinitionIndex == -1)
                    return null;
                return definitions.ElementAt(m_selector.DefinitionIndex);
            }
        }

        private static void ShowErrorMessageBox(string format, params object[] args)
        {
            var msg = string.Format(CultureInfo.InvariantCulture, format, args);
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void CreateIndexes()
        {
            var indexes = m_definition.Fields.Where(f => f.IsIndex);

            if (!indexes.Any())
                throw new Exception("No indexes!");

            if (indexes.Count() > 1)
                throw new Exception("Too many indexes!");

            m_dataTable.PrimaryKey = new DataColumn[] { m_dataTable.Columns[indexes.First().Name] };
        }

        private void CreateColumns()
        {
            foreach (Field field in m_fields)
            {
                var colName = field.Name;

                switch (field.Type)
                {
                    case "long":
                        CreateColumn<long>(field, colName);
                        break;
                    case "ulong":
                        CreateColumn<ulong>(field, colName);
                        break;
                    case "int":
                        CreateColumn<int>(field, colName);
                        break;
                    case "uint":
                        CreateColumn<uint>(field, colName);
                        break;
                    case "short":
                        CreateColumn<short>(field, colName);
                        break;
                    case "ushort":
                        CreateColumn<ushort>(field, colName);
                        break;
                    case "sbyte":
                        CreateColumn<sbyte>(field, colName);
                        break;
                    case "byte":
                        CreateColumn<byte>(field, colName);
                        break;
                    case "float":
                        CreateColumn<float>(field, colName);
                        break;
                    case "double":
                        CreateColumn<double>(field, colName);
                        break;
                    case "string":
                        CreateColumn<string>(field, colName);
                        break;
                    default:
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unknown field type {0}!", field.Type));
                }
            }
        }

        private void CreateColumn<T>(Field field, string colName)
        {
            if (field.ArraySize > 1)
            {
                for (int i = 0; i < field.ArraySize; i++)
                    m_dataTable.Columns.Add(string.Format("{0}_{1}", colName, i + 1), typeof(T));
            }
            else
                m_dataTable.Columns.Add(colName, typeof(T));
        }

        private void InitColumnsFilter()
        {
            columnsFilterToolStripMenuItem.DropDownItems.Clear();

            foreach (Field field in m_fields)
            {
                var colName = field.Name;

                var item = new ToolStripMenuItem(colName);
                item.Click += new EventHandler(columnsFilterEventHandler);
                item.CheckOnClick = true;
                item.Name = colName;
                item.Checked = !field.Visible;
                columnsFilterToolStripMenuItem.DropDownItems.Add(item);

                if (field.ArraySize > 1)
                {
                    for (int i = 0; i < field.ArraySize; i++)
                    {
                        dataGridView1.Columns[colName + "_" + (i + 1)].Visible = field.Visible;
                        dataGridView1.Columns[colName + "_" + (i + 1)].Width = field.Width;
                        dataGridView1.Columns[colName + "_" + (i + 1)].AutoSizeMode = GetColumnAutoSizeMode(field.Type, field.Format);
                        dataGridView1.Columns[colName + "_" + (i + 1)].SortMode = DataGridViewColumnSortMode.Automatic;
                    }
                }
                else
                {
                    dataGridView1.Columns[colName].Visible = field.Visible;
                    dataGridView1.Columns[colName].Width = field.Width;
                    dataGridView1.Columns[colName].AutoSizeMode = GetColumnAutoSizeMode(field.Type, field.Format);
                    dataGridView1.Columns[colName].SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }
        }

        private static DataGridViewAutoSizeColumnMode GetColumnAutoSizeMode(string type, string format)
        {
            switch (type)
            {
                case "string":
                    return DataGridViewAutoSizeColumnMode.NotSet;
                default:
                    break;
            }

            if (string.IsNullOrEmpty(format))
                return DataGridViewAutoSizeColumnMode.DisplayedCells;

            switch (format.Substring(0, 1).ToUpper(CultureInfo.InvariantCulture))
            {
                case "X":
                case "B":
                case "O":
                    return DataGridViewAutoSizeColumnMode.DisplayedCells;
                default:
                    return DataGridViewAutoSizeColumnMode.ColumnHeader;
            }
        }

        public void SetDataSource(DataTable dataView)
        {
            bindingSource1.DataSource = dataView;
        }

        private void LoadDefinitions()
        {
            string oldDefsPath = Path.Combine(m_workingFolder, "dbclayout.xml");

            // convert...
            if (File.Exists(oldDefsPath))
            {
                XmlDocument oldDefs = new XmlDocument();
                //var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                oldDefs.Load(oldDefsPath);

                if (oldDefs["DBFilesClient"].GetElementsByTagName("Table").Count == 0)
                {
                    DBFilesClient db = new DBFilesClient();

                    db.Tables = new List<Table>();

                    foreach (XmlElement def in oldDefs["DBFilesClient"])
                    {
                        string name = def.Name;

                        var fields = def.GetElementsByTagName("field");
                        var index = def.GetElementsByTagName("index");
                        var hasIndex = index.Count > 0;
                        var build = Convert.ToInt32(def.Attributes["build"].Value);

                        Table table = new Table();

                        table.Name = name;
                        table.Build = build;
                        table.Fields = new List<Field>();

                        for (int i = 0; i < fields.Count; i++)
                        {
                            var oldField = fields[i];

                            Field field = new Field();

                            field.Name = oldField.Attributes["name"].Value;
                            field.ArraySize = Convert.ToInt32(oldField.Attributes["arraysize"]?.Value ?? "1");
                            field.Format = oldField.Attributes["format"]?.Value ?? "";
                            field.Type = oldField.Attributes["type"]?.Value ?? "int";
                            field.Index = i;
                            field.Visible = true;
                            field.Width = 0;
                            field.IsIndex = hasIndex ? index[0]["primary"].InnerText == field.Name : i == 0;

                            table.Fields.Add(field);
                        }

                        db.Tables.Add(table);
                    }

                    db.File = Path.Combine(m_workingFolder, "definitions", "dblayout_old.xml");
                    DBFilesClient.Save(db);

                    File.Move(oldDefsPath, Path.Combine(m_workingFolder, "dbclayout.xml.bak"));
                }
            }

            m_definitions = DefinitionCatalog.SelectCatalog(m_workingFolder);
        }

        private void Compose()
        {
            m_catalog = new AggregateCatalog();
            m_catalog.Catalogs.Add(new DirectoryCatalog(m_workingFolder));
            //m_catalog.Catalogs.Add(new AssemblyCatalog(m_workingFolder));
            var container = new CompositionContainer(m_catalog);
            container.ComposeParts(this);
        }

        private void RunPlugin(int index)
        {
            Plugins[index].Run(m_dataTable);
            Plugins[index].Run(m_dbreader);
        }
    }
}
