using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using dbc2sql;

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

            int val = 0;

            if (m_dataTable.Columns[e.ColumnIndex].DataType != typeof(string))
            {
                if (m_dataTable.Columns[e.ColumnIndex].DataType == typeof(int))
                    val = Convert.ToInt32(dataGridView1[e.ColumnIndex, e.RowIndex].Value, CultureInfo.InvariantCulture);
                else if (m_dataTable.Columns[e.ColumnIndex].DataType == typeof(float))
                    val = (int)BitConverter.ToUInt32(BitConverter.GetBytes((float)dataGridView1[e.ColumnIndex, e.RowIndex].Value), 0);
                else
                    val = (int)Convert.ToUInt32(dataGridView1[e.ColumnIndex, e.RowIndex].Value, CultureInfo.InvariantCulture);
            }
            else
            {
                if (!(m_reader is WDBReader))
                    val = (from k in m_reader.StringTable where string.Compare(k.Value, (string)dataGridView1[e.ColumnIndex, e.RowIndex].Value, StringComparison.Ordinal) == 0 select k.Key).FirstOrDefault();
            }

            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "Integer: {0:D}{1}", val, Environment.NewLine);
            sb.AppendFormat(new BinaryFormatter(), "HEX: {0:X}{1}", val, Environment.NewLine);
            sb.AppendFormat(new BinaryFormatter(), "BIN: {0:B}{1}", val, Environment.NewLine);
            sb.AppendFormat(CultureInfo.InvariantCulture, "Float: {0}{1}", BitConverter.ToSingle(BitConverter.GetBytes(val), 0), Environment.NewLine);
            //sb.AppendFormat(CultureInfo.InvariantCulture, "Double: {0}{1}", BitConverter.ToDouble(BitConverter.GetBytes(val), 0), Environment.NewLine);
            sb.AppendFormat(CultureInfo.InvariantCulture, "String: {0}{1}", !(m_reader is WDBReader) ? m_reader.StringTable[(int)val] : String.Empty, Environment.NewLine);
            e.ToolTipText = sb.ToString();
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell != null)
                label1.Text = String.Format(CultureInfo.InvariantCulture, "Current Cell: {0}x{1}", dataGridView1.CurrentCell.RowIndex, dataGridView1.CurrentCell.ColumnIndex);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var file = (string)e.Argument;

            try
            {
                var ext = Path.GetExtension(file).ToUpperInvariant();
                if (ext == ".DBC")
                    m_reader = new DBCReader(file);
                else if (ext == ".DB2")
                    m_reader = new DB2Reader(file);
                else if (ext == ".ADB")
                    m_reader = new ADBReader(file);
                else if (ext == ".WDB")
                    m_reader = new WDBReader(file);
                else
                    throw new InvalidDataException(String.Format("Unknown file type {0}", ext));
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex.Message);
                e.Cancel = true;
                return;
            }

            m_fields = m_definition.GetElementsByTagName("field");

            string[] types = new string[m_fields.Count];

            for (var j = 0; j < m_fields.Count; ++j)
                types[j] = m_fields[j].Attributes["type"].Value;

            // hack for *.adb files (because they don't have FieldsCount)
            var notADB = !(m_reader is ADBReader);
            // hack for *.wdb files (because they don't have FieldsCount)
            var notWDB = !(m_reader is WDBReader);

            if (GetFieldsCount(m_fields) != m_reader.FieldsCount && notADB && notWDB)
            {
                var msg = String.Format(CultureInfo.InvariantCulture, "{0} has invalid definition!\nFields count mismatch: got {1}, expected {2}", Path.GetFileName(file), m_fields.Count, m_reader.FieldsCount);
                ShowErrorMessageBox(msg);
                e.Cancel = true;
                return;
            }

            m_dataTable = new DataTable(Path.GetFileName(file));
            m_dataTable.Locale = CultureInfo.InvariantCulture;

            CreateColumns();                                // Add columns

            CreateIndexes();                                // Add indexes

            for (var i = 0; i < m_reader.RecordsCount; ++i) // Add rows
            {
                var dataRow = m_dataTable.NewRow();

                #region Test
                //var bytes = m_reader.GetRowAsByteArray(i);
                //unsafe
                //{
                //    fixed (void* b = bytes)
                //    {
                //        IntPtr ptr = new IntPtr(b);

                //        int offset = 0;

                //        for (var j = 0; j < m_fields.Count; ++j)    // Add cells
                //        {
                //            switch (types[j])
                //            {
                //                case "long":
                //                    dataRow[j] = *(long*)(ptr + offset);
                //                    offset += 8;
                //                    break;
                //                case "ulong":
                //                    dataRow[j] = *(ulong*)(ptr + offset);
                //                    offset += 8;
                //                    break;
                //                case "int":
                //                    dataRow[j] = *(int*)(ptr + offset);
                //                    offset += 4;
                //                    break;
                //                case "uint":
                //                    dataRow[j] = *(uint*)(ptr + offset);
                //                    offset += 4;
                //                    break;
                //                case "short":
                //                    dataRow[j] = *(short*)(ptr + offset);
                //                    offset += 2;
                //                    break;
                //                case "ushort":
                //                    dataRow[j] = *(ushort*)(ptr + offset);
                //                    offset += 2;
                //                    break;
                //                case "sbyte":
                //                    dataRow[j] = *(sbyte*)(ptr + offset);
                //                    offset += 1;
                //                    break;
                //                case "byte":
                //                    dataRow[j] = *(byte*)(ptr + offset);
                //                    offset += 1;
                //                    break;
                //                case "float":
                //                    dataRow[j] = *(float*)(ptr + offset);
                //                    offset += 4;
                //                    break;
                //                case "double":
                //                    dataRow[j] = *(double*)(ptr + offset);
                //                    offset += 8;
                //                    break;
                //                case "string":
                //                    dataRow[j] = m_reader.StringTable[*(int*)(ptr + offset)];
                //                    offset += 4;
                //                    break;
                //                default:
                //                    throw new Exception(String.Format("Unknown field type {0}!", m_fields[j].Attributes["type"].Value));
                //            }
                //        }
                //    }
                //}
                #endregion
                var br = m_reader[i];

                for (var j = 0; j < m_fields.Count; ++j)    // Add cells
                {
                    switch (types[j])
                    {
                        case "long":
                            dataRow[j] = br.ReadInt64();
                            break;
                        case "ulong":
                            dataRow[j] = br.ReadUInt64();
                            break;
                        case "int":
                            dataRow[j] = br.ReadInt32();
                            break;
                        case "uint":
                            dataRow[j] = br.ReadUInt32();
                            break;
                        case "short":
                            dataRow[j] = br.ReadInt16();
                            break;
                        case "ushort":
                            dataRow[j] = br.ReadUInt16();
                            break;
                        case "sbyte":
                            dataRow[j] = br.ReadSByte();
                            break;
                        case "byte":
                            dataRow[j] = br.ReadByte();
                            break;
                        case "float":
                            dataRow[j] = br.ReadSingle();
                            break;
                        case "double":
                            dataRow[j] = br.ReadDouble();
                            break;
                        case "string":
                            dataRow[j] = m_reader is WDBReader ? br.ReadStringNull() : m_reader.StringTable[br.ReadInt32()];
                            break;
                        default:
                            throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Unknown field type {0}!", types[j]));
                    }
                }

                m_dataTable.Rows.Add(dataRow);

                int percent = (int)((float)m_dataTable.Rows.Count / (float)m_reader.RecordsCount * 100.0f);
                (sender as BackgroundWorker).ReportProgress(percent);
            }

            if (dataGridView1.InvokeRequired)
            {
                SetDataViewDelegate d = new SetDataViewDelegate(SetDataSource);
                Invoke(d, new object[] { m_dataTable.DefaultView });
            }
            else
            {
                SetDataSource(m_dataTable.DefaultView);
            }

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
                toolStripStatusLabel1.Text = String.Format(CultureInfo.InvariantCulture, "Ready. Loaded in {0} sec", total.TotalSeconds);
                Text = String.Format(CultureInfo.InvariantCulture, "DBC Viewer - {0}", e.Result.ToString());
                InitColumnsFilter();
            }
        }

        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
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

            m_catalog.Refresh();

            if (Plugins.Count == 0)
            {
                ShowErrorMessageBox("No plugins found!");
                return;
            }

            PluginsForm selector = new PluginsForm();
            selector.SetPlugins(Plugins);
            var result = selector.ShowDialog(this);
            selector.Dispose();
            if (result != DialogResult.OK || selector.PluginIndex == -1)
            {
                ShowErrorMessageBox("No plugin selected!");
                return;
            }

            toolStripStatusLabel1.Text = "Plugin working...";
            Thread pluginThread = new Thread(RunPlugin);
            pluginThread.Start(selector.PluginIndex);
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var attribute = m_fields[e.ColumnIndex].Attributes["format"];

            if (attribute == null)
                return;

            var fmtStr = "{0:" + attribute.Value + "}";
            e.Value = String.Format(new BinaryFormatter(), fmtStr, e.Value);
            e.FormattingApplied = true;
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
            var control = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem item in autoSizeModeToolStripMenuItem.DropDownItems)
            {
                if (item != control)
                    item.Checked = false;
            }

            var index = (int)columnContextMenuStrip.Tag;
            dataGridView1.Columns[index].AutoSizeMode = (DataGridViewAutoSizeColumnMode)Enum.Parse(typeof(DataGridViewAutoSizeColumnMode), (string)control.Tag);
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var index = (int)columnContextMenuStrip.Tag;
            dataGridView1.Columns[index].Visible = false;
            ((ToolStripMenuItem)columnsFilterToolStripMenuItem.DropDownItems[index]).Checked = true;
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                return;

            DataGridView.HitTestInfo hit = dataGridView1.HitTest(e.X, e.Y);

            if (hit.Type == DataGridViewHitTestType.ColumnHeader)
            {
                columnContextMenuStrip.Tag = hit.ColumnIndex;

                foreach (ToolStripMenuItem item in autoSizeModeToolStripMenuItem.DropDownItems)
                {
                    if (item.Tag.ToString() == dataGridView1.Columns[hit.ColumnIndex].AutoSizeMode.ToString())
                        item.Checked = true;
                    else
                        item.Checked = false;
                }

                columnContextMenuStrip.Show((Control)sender, e.Location.X, e.Location.Y);
            }
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

            LoadDefinitions();
            Compose();

            var cmds = Environment.GetCommandLineArgs();
            if (cmds.Length > 1)
            {
                LoadFile(cmds[1]);
            }
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
            label2.Text = String.Format(CultureInfo.InvariantCulture, "Rows Displayed: {0}", dataGridView1.RowCount);
        }
    }
}
