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
            if (e.RowIndex == -1)
                return;

            ulong val = 0;

            Type dataType = m_dataTable.Columns[e.ColumnIndex].DataType;
            CultureInfo culture = CultureInfo.InvariantCulture;
            object value = dataGridView1[e.ColumnIndex, e.RowIndex].Value;

            //val = (ulong)Convert.ChangeType(value, dataType);

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
                if (!(m_dbreader is WDBReader) && !(m_dbreader is STLReader))
                    val = (uint)(from k in m_dbreader.StringTable where string.Compare(k.Value, (string)value, StringComparison.Ordinal) == 0 select k.Key).FirstOrDefault();
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(culture, "Integer: {0:D}{1}", val, Environment.NewLine);
            sb.AppendFormat(new BinaryFormatter(), "HEX: {0:X}{1}", val, Environment.NewLine);
            sb.AppendFormat(new BinaryFormatter(), "BIN: {0:B}{1}", val, Environment.NewLine);
            sb.AppendFormat(culture, "Float: {0}{1}", BitConverter.ToSingle(BitConverter.GetBytes(val), 0), Environment.NewLine);
            sb.AppendFormat(culture, "Double: {0}{1}", BitConverter.ToDouble(BitConverter.GetBytes(val), 0), Environment.NewLine);

            try
            {
                sb.AppendFormat(culture, "String: {0}{1}", !(m_dbreader is WDBReader) ? m_dbreader.StringTable[(int)val] : string.Empty, Environment.NewLine);
            }
            catch
            {
                sb.AppendFormat(culture, "String: <empty>{0}", Environment.NewLine);
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
            string file = (string)e.Argument;

            try
            {
                m_dbreader = DBReaderFactory.GetReader(file, m_definition);
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex.Message);
                e.Cancel = true;
                return;
            }

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

            // hack for *.adb files (because they don't have FieldsCount)
            bool notADB = !(m_dbreader is ADBReader);
            // hack for *.wdb files (because they don't have FieldsCount)
            bool notWDB = !(m_dbreader is WDBReader);
            // hack for *.wdb files (because they don't have FieldsCount)
            bool notSTL = !(m_dbreader is STLReader);
            // hack for *.db2 files v3 (because they don't have FieldsCount)
            bool notDB3 = !(m_dbreader is DB3Reader);
            bool notDB4 = !(m_dbreader is DB4Reader);
            bool isDB5 = m_dbreader is DB5Reader;
            bool notDB5 = !isDB5;

            int fcount = GetFieldsCount(m_fields);
            if (fcount != m_dbreader.FieldsCount && notADB && notWDB && notSTL && notDB3 && notDB4 && notDB5)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "{0} has invalid definition!\nFields count mismatch: got {1}, expected {2}", Path.GetFileName(file), fcount, m_dbreader.FieldsCount);
                ShowErrorMessageBox(msg);
                e.Cancel = true;
                return;
            }

            m_dataTable = new DataTable(Path.GetFileName(file));
            m_dataTable.Locale = CultureInfo.InvariantCulture;

            CreateColumns();                                // Add columns

            CreateIndexes();                                // Add indexes

            var meta = (m_dbreader as DB5Reader)?.Meta;

            //bool extraData = false;

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
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<long>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<long>(meta?[j]);
                                break;
                            case "ulong":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<ulong>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<ulong>(meta?[j]);
                                break;
                            case "int":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<int>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<int>(meta?[j]);
                                break;
                            case "uint":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<uint>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<uint>(meta?[j]);
                                break;
                            case "short":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<short>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<short>(meta?[j]);
                                break;
                            case "ushort":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<ushort>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<ushort>(meta?[j]);
                                break;
                            case "sbyte":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<sbyte>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<sbyte>(meta?[j]);
                                break;
                            case "byte":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<byte>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<byte>(meta?[j]);
                                break;
                            case "float":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.Read<float>(meta?[j]);
                                }
                                else
                                    dataRow[colNames[j]] = br.Read<float>(meta?[j]);
                                break;
                            case "double":
                                if (arraySizes[j] > 1)
                                {
                                    for (int i = 0; i < arraySizes[j]; i++)
                                        dataRow[colNames[j] + "_" + (i + 1)] = br.ReadDouble();
                                }
                                else
                                    dataRow[colNames[j]] = br.ReadDouble();
                                break;
                            case "string":
                                if (m_dbreader is WDBReader)
                                    dataRow[j] = br.ReadStringNull();
                                else if (m_dbreader is STLReader)
                                {
                                    int offset = br.ReadInt32();
                                    dataRow[j] = (m_dbreader as STLReader).ReadString(offset);
                                }
                                else
                                {
                                    if (arraySizes[j] > 1)
                                    {
                                        for (int i = 0; i < arraySizes[j]; i++)
                                        {
                                            try
                                            {
                                                dataRow[colNames[j] + "_" + (i + 1)] = m_dbreader.StringTable[br.Read<int>(meta?[j])];
                                            }
                                            catch
                                            {
                                                dataRow[colNames[j]] = "Invalid string index!";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            dataRow[colNames[j]] = m_dbreader.StringTable[br.Read<int>(meta?[j])];
                                        }
                                        catch
                                        {
                                            dataRow[colNames[j]] = "Invalid string index!";
                                        }
                                    }
                                }
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

            //if (extraData)
            //{
            //    MessageBox.Show("extra data detected!");
            //}

            if (dataGridView1.InvokeRequired)
            {
                SetDataViewDelegate d = new SetDataViewDelegate(SetDataSource);
                Invoke(d, new object[] { m_dataTable.DefaultView });
            }
            else
                SetDataSource(m_dataTable.DefaultView);

            e.Result = file;
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
                ShowErrorMessageBox(e.Error.ToString());
                toolStripStatusLabel1.Text = "Error.";
            }
            else if (e.Cancelled == true)
            {
                toolStripStatusLabel1.Text = "Error in definitions.";
                StartEditor();
            }
            else
            {
                TimeSpan total = DateTime.Now - m_startTime;
                toolStripStatusLabel1.Text = string.Format(CultureInfo.InvariantCulture, "Ready. Loaded in {0} sec", total.TotalSeconds);
                Text = string.Format(CultureInfo.InvariantCulture, "DBC Viewer - {0}", e.Result.ToString());
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

            SetDataSource(m_dataTable.DefaultView);
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

            toolStripStatusLabel1.Text = "Plugin working...";
            Thread pluginThread = new Thread(RunPlugin);
            pluginThread.Start(selector.PluginIndex);
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            int columnIndex = e.ColumnIndex;
            int columnIndexFix = 0;

            for (int i = 0; i < m_fields.Count; i++)
            {
                for (int j = 0; j < m_fields[i].ArraySize; j++)
                {
                    columnIndexFix++;

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
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //DBCReaderGeneric<AreaTableRecord> at = new DBCReaderGeneric<AreaTableRecord>(@"c:\my_old_files\Development\git\CASCExplorer\CASCConsole\bin\Debug\DBFilesClient\AreaTable.dbc");
            //sw.Stop();
            //MessageBox.Show(sw.Elapsed.ToString());

            WindowState = Properties.Settings.Default.WindowState;
            Size = Properties.Settings.Default.WindowSize;
            Location = Properties.Settings.Default.WindowLocation;

            m_workingFolder = Application.StartupPath;
            dataGridView1.AutoGenerateColumns = true;

            LoadDefinitions();
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
            else
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
    }
}
