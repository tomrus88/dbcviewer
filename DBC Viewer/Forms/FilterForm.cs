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
            Filter();
        }

        private void Filter()
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

            m_filter = m_filter.Where(Compare);
            owner.SetDataSource(m_filter.AsDataView());
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

                m_filters[i] = new FilterOptions(args[0], StringToCompType(args[1]), args[2]);
            }
        }

        private ComparisonType StringToCompType(string str)
        {
            switch (str)
            {
                case "&":
                    return ComparisonType.And;
                case "~&":
                    return ComparisonType.AndNot;
                case "==":
                    return ComparisonType.Equal;
                case "!=":
                    return ComparisonType.NotEqual;
                case "<":
                    return ComparisonType.Less;
                case ">":
                    return ComparisonType.Greater;
                case "*__":
                    return ComparisonType.StartWith;
                case "__*":
                    return ComparisonType.EndsWith;
                case "_*_":
                    return ComparisonType.Contains;
                default:
                    throw new Exception("Bad comparison string!");
            }
        }

        public void ResetFilters()
        {
            listBox1.Items.Clear();
            SyncFilters();
        }

        public void SetSelection(string column, string value)
        {
            for (var i = 0; i < listBox2.Items.Count; ++i)
            {
                if ((string)listBox2.Items[i] == column)
                {
                    listBox2.SelectedIndex = i;
                    break;
                }
            }

            textBox2.Text = value;
        }
    }
}
