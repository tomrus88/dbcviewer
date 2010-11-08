using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace DBCViewer
{
    public partial class FilterForm : Form
    {
        EnumerableRowCollection<DataRow> m_filter;
        Object[] decimalOperators = new Object[] { "&", "~&", "==", "!=", "<", ">" };
        Object[] stringOperators = new Object[] { "==", "!=", "*__", "__*", "_*_" };
        Object[] floatOperators = new Object[] { "==", "!=", "<", ">" };
        Dictionary<int, FilterOptions> m_filters = new Dictionary<int, FilterOptions>();

        public FilterForm()
        {
            InitializeComponent();
        }

        private void FilterForm_Load(object sender, EventArgs e)
        {
            var dt = ((MainForm)Owner).DataTable;

            for (var i = 0; i < dt.Columns.Count; ++i)
                listBox2.Items.Add(dt.Columns[i].ColumnName);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (m_filters.Count == 0)
            {
                MessageBox.Show("Add filter(s) first!");
                return;
            }

            var owner = ((MainForm)Owner);
            var dt = owner.DataTable;

            if (m_filter == null)
                m_filter = dt.AsEnumerable();

            if (!checkBox1.Checked)
                m_filter = dt.AsEnumerable();

            if (FilterTable(dt))
                owner.SetDataSource(m_filter.AsDataView());
            else
                MessageBox.Show("Unhandled type?");
        }

        private bool FilterTable(DataTable dt)
        {
            bool result = false;
            foreach (var filter in m_filters)
            {
                var colName = filter.Value.Col;
                var col = dt.Columns[colName];
                var op = filter.Value.Op;

                if (col.DataType == typeof(string))
                    result = FilterString(op);
                else if (col.DataType == typeof(long))
                    result = FilterInt64(op);
                else if (col.DataType == typeof(int))
                    result = FilterInt32(op);
                else if (col.DataType == typeof(short))
                    result = FilterInt16(op);
                else if (col.DataType == typeof(sbyte))
                    result = FilterInt8(op);
                else if (col.DataType == typeof(ulong))
                    result = FilterUInt64(op);
                else if (col.DataType == typeof(uint))
                    result = FilterUInt32(op);
                else if (col.DataType == typeof(ushort))
                    result = FilterUInt16(op);
                else if (col.DataType == typeof(byte))
                    result = FilterUInt8(op);
                else if (col.DataType == typeof(float))
                    result = FilterSingle(op);
                else if (col.DataType == typeof(double))
                    result = FilterDouble(op);
                else
                    MessageBox.Show("Unhandled type?");
            }

            return result;
        }

        private void FilterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                Owner.Activate();
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            var owner = ((MainForm)Owner);
            var dt = owner.DataTable;
            var colName = (string)listBox2.SelectedItem;
            var col = dt.Columns[colName];

            if (col.DataType == typeof(string))
                checkBox2.Visible = true;
            else
                checkBox2.Visible = false;

            FillComboBox(comboBox3, col);
        }

        private void FillComboBox(ComboBox comboBox, DataColumn col)
        {
            comboBox.Items.Clear();

            if (col.DataType == typeof(string))
                comboBox.Items.AddRange(stringOperators);
            else if (col.DataType == typeof(float))
                comboBox.Items.AddRange(floatOperators);
            else if (col.DataType == typeof(double))
                comboBox.Items.AddRange(floatOperators);
            else if (col.DataType.IsPrimitive)
                comboBox.Items.AddRange(decimalOperators);
            else
                MessageBox.Show("Unhandled type?");

            comboBox.SelectedIndex = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("Enter something first!");
                textBox2.Focus();
                return;
            }

            var colName = (string)listBox2.SelectedItem;
            var op = (string)comboBox3.SelectedItem;
            var val = textBox2.Text;

            var owner = ((MainForm)Owner);
            var dt = owner.DataTable;
            var col = dt.Columns[colName];

            try
            {
                if (col.DataType.IsPrimitive && col.DataType != typeof(float) && col.DataType != typeof(double))
                {
                    if (val.StartsWith("0x", true, CultureInfo.InvariantCulture))
                        val = Convert.ToUInt64(val, 16).ToString();
                }

                Convert.ChangeType(val, col.DataType, CultureInfo.InvariantCulture);
            }
            catch
            {
                MessageBox.Show("Invalid filter!");
                return;
            }

            listBox1.Items.Add(String.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", colName, op, val));
            SyncFilters();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
                return;

            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            SyncFilters();

            if (listBox1.Items.Count > 0)
                listBox1.SelectedIndex = 0;
        }

        private void SyncFilters()
        {
            m_filters.Clear();

            var delimiter = new char[] { ' ' };

            for (var i = 0; i < listBox1.Items.Count; ++i)
            {
                string filter = (string)listBox1.Items[i];
                var args = filter.Split(delimiter, 3);
                if (args.Length != 3)
                    throw new ArgumentException("We got a trouble!");

                m_filters[i] = new FilterOptions(args[0], args[1], args[2]);
            }
        }

        public void ResetFilters()
        {
            listBox1.Items.Clear();
            SyncFilters();
        }
    }
}
