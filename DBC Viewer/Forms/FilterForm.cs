using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Linq;

namespace DBCViewer
{
    public partial class FilterForm : Form
    {
        EnumerableRowCollection<DataRow> m_filter;

        Object[] decimalOperators = new Object[]
        {
            ComparisonType.And,
            ComparisonType.AndNot,
            ComparisonType.Equal,
            ComparisonType.NotEqual,
            ComparisonType.Less,
            ComparisonType.Greater
        };

        Object[] stringOperators = new Object[]
        {
            ComparisonType.Equal,
            ComparisonType.NotEqual,
            ComparisonType.StartWith,
            ComparisonType.EndsWith,
            ComparisonType.Contains
        };

        Object[] floatOperators = new Object[]
        {
            ComparisonType.Equal,
            ComparisonType.NotEqual,
            ComparisonType.Less,
            ComparisonType.Greater
        };

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
            if (listBox1.Items.Count == 0)
            {
                MessageBox.Show("Add filter(s) first!");
                return;
            }

            //Stopwatch sw = Stopwatch.StartNew();

            var owner = ((MainForm)Owner);
            var dt = owner.DataTable;

            if (m_filter == null)
                m_filter = dt.AsEnumerable();

            if (!checkBox1.Checked)
                m_filter = dt.AsEnumerable();

            var temp = m_filter.AsParallel().AsOrdered().Where(Compare);

            if (temp.Count() != 0)
                m_filter = temp.CopyToDataTable().AsEnumerable();
            else
                m_filter = new DataTable().AsEnumerable();

            //m_filter = m_filter.Where(Compare);

            owner.SetDataSource(m_filter.AsDataView());

            //sw.Stop();

            //MessageBox.Show(sw.Elapsed.TotalMilliseconds.ToString());
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

            comboBox3.Items.Clear();

            if (col.DataType == typeof(string))
                comboBox3.Items.AddRange(stringOperators);
            else if (col.DataType == typeof(float))
                comboBox3.Items.AddRange(floatOperators);
            else if (col.DataType == typeof(double))
                comboBox3.Items.AddRange(floatOperators);
            else if (col.DataType.IsPrimitive)
                comboBox3.Items.AddRange(decimalOperators);
            else
                MessageBox.Show("Unhandled type?");

            comboBox3.SelectedIndex = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("Enter something first!");
                textBox2.Focus();
                return;
            }

            var fi = new FilterOptions((string)listBox2.SelectedItem, (ComparisonType)comboBox3.SelectedItem, textBox2.Text);

            var dt = (Owner as MainForm).DataTable;
            var col = dt.Columns[fi.Column];

            try
            {
                if (col.DataType.IsPrimitive && col.DataType != typeof(float) && col.DataType != typeof(double))
                    if (fi.Value.StartsWith("0x", true, CultureInfo.InvariantCulture))
                        fi.Value = Convert.ToUInt64(fi.Value, 16).ToString(CultureInfo.InvariantCulture);

                Convert.ChangeType(fi.Value, col.DataType, CultureInfo.InvariantCulture);
            }
            catch
            {
                MessageBox.Show("Invalid filter!");
                return;
            }

            listBox1.Items.Add(fi);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
                return;

            listBox1.Items.RemoveAt(listBox1.SelectedIndex);

            if (listBox1.Items.Count > 0)
                listBox1.SelectedIndex = 0;
        }

        public void ResetFilters()
        {
            listBox1.Items.Clear();
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
