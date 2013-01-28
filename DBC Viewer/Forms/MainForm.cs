using PluginInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace DBCViewer
{
    public partial class MainForm : Form
    {
        // Fields
        private DataTable m_dataTable;
        private IWowClientDBReader m_reader;
        private FilterForm m_filterForm;
        private DefinitionSelect m_selector;
        private XmlDocument m_definitions;
        private XmlNodeList m_fields;
        private DirectoryCatalog m_catalog;
        private XmlElement m_definition;        // definition for current file
        private string m_dbcName;               // file name without extension
        private string m_dbcFile;               // path to current file
        private DateTime m_startTime;
        private string m_workingFolder;

        // Properties
        public DataTable DataTable { get { return m_dataTable; } }
        public string WorkingFolder { get { return m_workingFolder; } }
        public XmlElement Definition { get { return m_definition; } }
        public string DBCName { get { return m_dbcName; } }
        public int DefinitionIndex { get { return m_selector != null ? m_selector.DefinitionIndex : 0; } }
        public string DBCFile { get { return m_dbcFile; } }

        // Delegates
        delegate void SetDataViewDelegate(DataView view);

        // Plugins
        [ImportMany(AllowRecomposition = true)]
        List<IPlugin> Plugins { get; set; }

        [Export("PluginFinished")]
        public void PluginFinished(int result)
        {
            var msg = String.Format("Plugin finished! {0} rows affected.", result);
            toolStripStatusLabel1.Text = msg;
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
            toolStripStatusLabel1.Text = "Loading...";

            m_startTime = DateTime.Now;
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
            DefinitionEditor editor = new DefinitionEditor();
            var result = editor.ShowDialog(this);
            editor.Dispose();
            if (result == DialogResult.Abort)
                return;
            if (result == DialogResult.OK)
                LoadFile(m_dbcFile);
            else
                MessageBox.Show("Editor canceled! You can't open that file until you add proper definitions");
        }

        private XmlElement GetDefinition()
        {
            XmlNodeList definitions = m_definitions["DBFilesClient"].GetElementsByTagName(m_dbcName);

            if (definitions.Count == 0)
            {
                definitions = m_definitions["DBFilesClient"].GetElementsByTagName(Path.GetFileName(m_dbcFile));
            }

            if (definitions.Count == 0)
            {
                var msg = String.Format(CultureInfo.InvariantCulture, "{0} missing definition!", m_dbcName);
                ShowErrorMessageBox(msg);
                return null;
            }
            else if (definitions.Count == 1)
            {
                return ((XmlElement)definitions[0]);
            }
            else
            {
                m_selector = new DefinitionSelect();
                m_selector.SetDefinitions(definitions);
                var result = m_selector.ShowDialog(this);
                if (result != DialogResult.OK || m_selector.DefinitionIndex == -1)
                    return null;
                return ((XmlElement)definitions[m_selector.DefinitionIndex]);
            }
        }

        private static void ShowErrorMessageBox(string format, params object[] args)
        {
            var msg = String.Format(CultureInfo.InvariantCulture, format, args);
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void CreateIndexes()
        {
            XmlNodeList indexes = m_definition.GetElementsByTagName("index");
            var columns = new DataColumn[indexes.Count];
            var idx = 0;
            foreach (XmlElement index in indexes)
                columns[idx++] = m_dataTable.Columns[index["primary"].InnerText];
            m_dataTable.PrimaryKey = columns;
        }

        private void CreateColumns()
        {
            foreach (XmlElement field in m_fields)
            {
                var colName = field.Attributes["name"].Value;

                switch (field.Attributes["type"].Value)
                {
                    case "long":
                        m_dataTable.Columns.Add(colName, typeof(long));
                        break;
                    case "ulong":
                        m_dataTable.Columns.Add(colName, typeof(ulong));
                        break;
                    case "int":
                        m_dataTable.Columns.Add(colName, typeof(int));
                        break;
                    case "uint":
                        m_dataTable.Columns.Add(colName, typeof(uint));
                        break;
                    case "short":
                        m_dataTable.Columns.Add(colName, typeof(short));
                        break;
                    case "ushort":
                        m_dataTable.Columns.Add(colName, typeof(ushort));
                        break;
                    case "sbyte":
                        m_dataTable.Columns.Add(colName, typeof(sbyte));
                        break;
                    case "byte":
                        m_dataTable.Columns.Add(colName, typeof(byte));
                        break;
                    case "float":
                        m_dataTable.Columns.Add(colName, typeof(float));
                        break;
                    case "double":
                        m_dataTable.Columns.Add(colName, typeof(double));
                        break;
                    case "string":
                        m_dataTable.Columns.Add(colName, typeof(string));
                        break;
                    default:
                        throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Unknown field type {0}!", field.Attributes["type"].Value));
                }
            }
        }

        private void InitColumnsFilter()
        {
            columnsFilterToolStripMenuItem.DropDownItems.Clear();

            foreach (XmlElement field in m_fields)
            {
                var colName = field.Attributes["name"].Value;
                var type = field.Attributes["type"].Value;
                var format = field.Attributes["format"] != null ? field.Attributes["format"].Value : String.Empty;
                var visible = field.Attributes["visible"] != null ? field.Attributes["visible"].Value == "true" : true;
                var width = field.Attributes["width"] != null ? Convert.ToInt32(field.Attributes["width"].Value, CultureInfo.InvariantCulture) : 100;

                var item = new ToolStripMenuItem(colName);
                item.Click += new EventHandler(columnsFilterEventHandler);
                item.CheckOnClick = true;
                item.Name = colName;
                item.Checked = !visible;
                columnsFilterToolStripMenuItem.DropDownItems.Add(item);

                dataGridView1.Columns[colName].Visible = visible;
                dataGridView1.Columns[colName].Width = width;
                dataGridView1.Columns[colName].AutoSizeMode = GetColumnAutoSizeMode(type, format);
                dataGridView1.Columns[colName].SortMode = DataGridViewColumnSortMode.Automatic;
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

            if (String.IsNullOrEmpty(format))
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

        public void SetDataSource(DataView dataView)
        {
            bindingSource1.DataSource = dataView;
        }

        private void LoadDefinitions()
        {
            m_definitions = new XmlDocument();
            //var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            m_definitions.Load(Path.Combine(m_workingFolder, "dbclayout.xml"));
        }

        private void Compose()
        {
            m_catalog = new DirectoryCatalog(m_workingFolder);
            var container = new CompositionContainer(m_catalog);
            container.ComposeParts(this);
        }

        private void RunPlugin(object obj)
        {
            Plugins[(int)obj].Run(m_dataTable);
        }

        private static int GetFieldsCount(XmlNodeList fields)
        {
            int count = 0;
            foreach (XmlElement field in fields)
            {
                switch (field.Attributes["type"].Value)
                {
                    case "long":
                    case "ulong":
                    case "double":
                        count += 2;
                        break;
                    default:
                        count++;
                        break;
                }
            }
            return count;
        }
    }
}
