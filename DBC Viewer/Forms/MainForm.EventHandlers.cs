using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DBCViewer
{
    partial class MainForm
    {
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            LoadFile(openFileDialog1.FileName);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void dataGridView1_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.ColumnIndex == -1 || e.RowIndex == -1 || e.RowIndex >= m_dataTable.Rows.Count)
                return;

            ulong val = 0;

            Type dataType = m_dataTable.Columns[e.ColumnIndex].DataType;
            CultureInfo culture = CultureInfo.InvariantCulture;
            object value = dataGridView1[e.ColumnIndex, e.RowIndex].Value;

            if (dataType != typeof(string))
            {
                if (dataType == typeof(sbyte))
                    val = (ulong)Convert.ToSByte(value, culture);
                else if (dataType == typeof(byte))
                    val = Convert.ToByte(value, culture);
                else if (dataType == typeof(short))
                    val = (ulong)Convert.ToInt16(value, culture);
                else if (dataType == typeof(ushort))
                    val = Convert.ToUInt16(value, culture);
                else if (dataType == typeof(int))
                    val = (ulong)Convert.ToInt32(value, culture);
                else if (dataType == typeof(uint))
                    val = Convert.ToUInt32(value, culture);
                else if (dataType == typeof(long))
                    val = (ulong)Convert.ToInt64(value, culture);
                else if (dataType == typeof(ulong))
                    val = Convert.ToUInt64(value, culture);
                else if (dataType == typeof(float))
                    val = BitConverter.ToUInt32(BitConverter.GetBytes((float)value), 0);
                else if (dataType == typeof(double))
                    val = BitConverter.ToUInt64(BitConverter.GetBytes((double)value), 0);
                else
                    val = Convert.ToUInt32(value, culture);
            }
            else
            {
                if (m_dbreader.StringTable != null)
                    val = (uint)m_dbreader.StringTable.Where(kv => string.Compare(kv.Value, (string)value, StringComparison.Ordinal) == 0).Select(kv => kv.Key).FirstOrDefault();
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormatLine(culture, "Integer: {0:D}", val);
            sb.AppendFormatLine(new BinaryFormatter(), "HEX: {0:X}", val);
            sb.AppendFormatLine(new BinaryFormatter(), "BIN: {0:B}", val);
            sb.AppendFormatLine(culture, "Float: {0}", BitConverter.ToSingle(BitConverter.GetBytes(val), 0));
            sb.AppendFormatLine(culture, "Double: {0}", BitConverter.ToDouble(BitConverter.GetBytes(val), 0));

            string strValue;
            if (m_dbreader.StringTable != null && m_dbreader.StringTable.TryGetValue((int)val, out strValue))
            {
                sb.AppendFormatLine(culture, "String: {0}", strValue);
            }
            else
            {
                sb.AppendFormatLine(culture, "String: <empty>");
            }

            e.ToolTipText = sb.ToString();
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell != null)
                label1.Text = string.Format(CultureInfo.InvariantCulture, "Current Cell: {0}x{1}", dataGridView1.CurrentCell.RowIndex, dataGridView1.CurrentCell.ColumnIndex);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            m_startTime = DateTime.Now;

            string file = (string)e.Argument;

            m_dbreader = DBReaderFactory.GetReader(file, m_definition);

            m_fields = new List<Field>(m_definition.Fields);

            string[] types = new string[m_fields.Count];

            for (int j = 0; j < m_fields.Count; ++j)
                types[j] = m_fields[j].Type;

            string[] colNames = new string[m_fields.Count];

            for (int j = 0; j < m_fields.Count; ++j)
                colNames[j] = m_fields[j].Name;

            int[] arraySizes = new int[m_fields.Count];

            for (int j = 0; j < m_fields.Count; ++j)
                arraySizes[j] = m_fields[j].ArraySize;

            bool isDBCorDB2 = m_dbreader is DBCReader || m_dbreader is DB2Reader;

            m_dataTable = new DataTable(Path.GetFileName(file));
            m_dataTable.Locale = CultureInfo.InvariantCulture;

            CreateColumns();                                // Add columns

            CreateIndexes();                                // Add indexes

            var meta = (m_dbreader as DB5Reader)?.Meta;

            foreach (var row in m_dbreader.Rows) // Add rows
            {
                DataRow dataRow = m_dataTable.NewRow();

                using (BinaryReader br = row)
                {
                    for (int j = 0; j < m_fields.Count; ++j)    // Add cells
                    {
                        switch (types[j])
                        {
                            case "long":
                                ReadField<long>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "ulong":
                                ReadField<ulong>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "int":
                                ReadField<int>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "uint":
                                ReadField<uint>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "short":
                                ReadField<short>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "ushort":
                                ReadField<ushort>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "sbyte":
                                ReadField<sbyte>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "byte":
                                ReadField<byte>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                // bytes are padded with zeros in old format versions if next field isn't byte
                                if (isDBCorDB2 && (j + 1 < types.Length) && (br.BaseStream.Position % 4) != 0 && types[j + 1] != "byte")
                                    br.BaseStream.Position += (4 - br.BaseStream.Position % 4);
                                break;
                            case "float":
                                ReadField<float>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "double":
                                ReadField<float>(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            case "string":
                                ReadStringField(colNames[j], arraySizes[j], meta?[j], dataRow, br);
                                break;
                            default:
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unknown field type {0}!", types[j]));
                        }
                    }
                }

                m_dataTable.Rows.Add(dataRow);

                int percent = (int)((float)m_dataTable.Rows.Count / m_dbreader.RecordsCount * 100.0f);
                (sender as BackgroundWorker).ReportProgress(percent);
            }

            e.Result = file;
        }

        private static void ReadField<T>(string colName, int arraySize, ColumnMeta meta, DataRow dataRow, BinaryReader br)
        {
            if (arraySize > 1)
            {
                for (int i = 0; i < arraySize; i++)
                    dataRow[colName + "_" + (i + 1)] = br.Read<T>(meta);
            }
            else
                dataRow[colName] = br.Read<T>(meta);
        }

        private void ReadStringField(string colName, int arraySize, ColumnMeta meta, DataRow dataRow, BinaryReader br)
        {
            if (m_dbreader is WDBReader)
                dataRow[colName] = br.ReadStringNull();
            else if (m_dbreader is STLReader)
            {
                int offset = br.ReadInt32();
                dataRow[colName] = (m_dbreader as STLReader).ReadString(offset);
            }
            else
            {
                if (arraySize > 1)
                {
                    for (int i = 0; i < arraySize; i++)
                    {
                        try
                        {
                            dataRow[colName + "_" + (i + 1)] = m_dbreader.IsSparseTable ? br.ReadStringNull() : m_dbreader.StringTable[(int)br.Read<int>(meta)];
                        }
                        catch
                        {
                            dataRow[colName] = "Invalid string index!";
                        }
                    }
                }
                else
                {
                    try
                    {
                        dataRow[colName] = m_dbreader.IsSparseTable ? br.ReadStringNull() : m_dbreader.StringTable[(int)br.Read<int>(meta)];
                    }
                    catch
                    {
                        dataRow[colName] = "Invalid string index!";
                    }
                }
            }
        }

        private void columnsFilterEventHandler(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;

            dataGridView1.Columns[item.Name].Visible = !item.Checked;

            ((ToolStripMenuItem)item.OwnerItem).ShowDropDown();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripProgressBar1.Visible = false;
            toolStripProgressBar1.Value = 0;

            if (e.Error != null)
            {
                if (e.Error is InvalidDataException)
                {
                    ShowErrorMessageBox(e.Error.ToString());
                    statusToolStripLabel.Text = "Error.";
                }
                else
                {
                    statusToolStripLabel.Text = "Error in definitions.";
                    StartEditor();
                }
            }
            else
            {
                TimeSpan total = DateTime.Now - m_startTime;
                statusToolStripLabel.Text = string.Format(CultureInfo.InvariantCulture, "Ready. Loaded in {0} sec", total.TotalSeconds);
                Text = string.Format(CultureInfo.InvariantCulture, "DBC Viewer - {0}", e.Result);
                SetDataSource(m_dataTable);
                InitColumnsFilter();
            }
        }

        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFilterForm();
        }

        private void ShowFilterForm()
        {
            if (m_dataTable == null)
                return;

            if (m_filterForm == null || m_filterForm.IsDisposed)
                m_filterForm = new FilterForm();

            if (!m_filterForm.Visible)
                m_filterForm.Show(this);
        }

        private void resetFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_dataTable == null)
                return;

            if (m_filterForm != null)
                m_filterForm.ResetFilters();

            SetDataSource(m_dataTable);
        }

        private void runPluginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_dataTable == null)
            {
                ShowErrorMessageBox("Nothing loaded yet!");
                return;
            }

            //m_catalog.Refresh();

            if (Plugins.Count == 0)
            {
                ShowErrorMessageBox("No plugins found!");
                return;
            }

            PluginsForm selector = new PluginsForm();
            selector.SetPlugins(Plugins);
            DialogResult result = selector.ShowDialog(this);
            selector.Dispose();
            if (result != DialogResult.OK)
            {
                ShowErrorMessageBox("No plugin selected!");
                return;
            }

            if (selector.NewPlugin != null)
                m_catalog.Catalogs.Add(new AssemblyCatalog(selector.NewPlugin));

            statusToolStripLabel.Text = "Plugin working...";
            Thread pluginThread = new Thread(() => RunPlugin(selector.PluginIndex));
            pluginThread.Start();
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            int columnIndex = e.ColumnIndex;
            int columnIndexFix = 0;

            for (int i = 0; i < m_fields.Count; i++)
            {
                for (int j = 0; j < m_fields[i].ArraySize; j++)
                {
                    if (columnIndex == columnIndexFix)
                    {
                        string format = m_fields[i].Format;

                        if (string.IsNullOrWhiteSpace(format))
                            return;

                        string fmtStr = "{0:" + format + "}";
                        e.Value = string.Format(new BinaryFormatter(), fmtStr, e.Value);
                        e.FormattingApplied = true;
                        return;
                    }

                    columnIndexFix++;
                }
            }
        }

        private void resetColumnsFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.Visible = true;
                ((ToolStripMenuItem)columnsFilterToolStripMenuItem.DropDownItems[col.Name]).Checked = false;
            }
        }

        private void autoSizeColumnsModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem control = (ToolStripMenuItem)sender;

            foreach (ToolStripMenuItem item in autoSizeModeToolStripMenuItem.DropDownItems)
                if (item != control)
                    item.Checked = false;

            int index = (int)columnContextMenuStrip.Tag;
            dataGridView1.Columns[index].AutoSizeMode = (DataGridViewAutoSizeColumnMode)Enum.Parse(typeof(DataGridViewAutoSizeColumnMode), (string)control.Tag);
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var index = (int)columnContextMenuStrip.Tag;
            dataGridView1.Columns[index].Visible = false;
            ((ToolStripMenuItem)columnsFilterToolStripMenuItem.DropDownItems[index]).Checked = true;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            WindowState = Properties.Settings.Default.WindowState;
            Size = Properties.Settings.Default.WindowSize;
            Location = Properties.Settings.Default.WindowLocation;

            m_workingFolder = Application.StartupPath;
            dataGridView1.AutoGenerateColumns = true;

            Compose();

            string[] cmds = Environment.GetCommandLineArgs();
            if (cmds.Length > 1)
                LoadFile(cmds[1]);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.WindowState = WindowState;

            if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.WindowSize = Size;
                Properties.Settings.Default.WindowLocation = Location;
            }
            else
            {
                Properties.Settings.Default.WindowSize = RestoreBounds.Size;
                Properties.Settings.Default.WindowLocation = RestoreBounds.Location;
            }

            Properties.Settings.Default.Save();
        }

        private void difinitionEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_dbcName == null)
                return;

            StartEditor();
        }

        private void reloadDefinitionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadDefinitions();
        }

        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            label2.Text = string.Format(CultureInfo.InvariantCulture, "Rows Displayed: {0}", dataGridView1.RowCount);
        }

        private void dataGridView1_CellContextMenuStripNeeded(object sender, DataGridViewCellContextMenuStripNeededEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                columnContextMenuStrip.Tag = e.ColumnIndex;

                foreach (ToolStripMenuItem item in autoSizeModeToolStripMenuItem.DropDownItems)
                {
                    if (item.Tag.ToString() == dataGridView1.Columns[e.ColumnIndex].AutoSizeMode.ToString())
                        item.Checked = true;
                    else
                        item.Checked = false;
                }

                e.ContextMenuStrip = columnContextMenuStrip;
            }
            else if (e.ColumnIndex != -1)
            {
                cellContextMenuStrip.Tag = string.Format("{0} {1}", e.ColumnIndex, e.RowIndex);
                e.ContextMenuStrip = cellContextMenuStrip;
            }
        }

        private void filterThisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] meta = ((string)cellContextMenuStrip.Tag).Split(' ');
            int column = Convert.ToInt32(meta[0]);
            int row = Convert.ToInt32(meta[1]);
            ShowFilterForm();
            m_filterForm.SetSelection(dataGridView1.Columns[column].Name, dataGridView1[column, row].Value.ToString());
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("DBC Viewer @ 2010-2016 TOM_RUS", "About DBC Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.EndEdit();
            m_dbreader.Save(m_dataTable, m_dataTable.TableName);
        }

        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            foreach (DataGridViewCell cell in e.Row.Cells)
            {
                if (cell.ValueType == typeof(string))
                    cell.Value = string.Empty;
                else
                    cell.Value = 0;
            }
        }
    }
}
