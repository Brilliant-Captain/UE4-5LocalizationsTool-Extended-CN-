using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using UE4localizationsTool.Controls;

namespace UE4localizationsTool
{
    public partial class FrmFilter : NForm
    {
        private sealed class FilterColumnItem
        {
            public FilterColumnItem(string columnName, string displayName)
            {
                ColumnName = columnName ?? "";
                DisplayName = displayName ?? "";
            }

            public string ColumnName { get; }

            public string DisplayName { get; }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        public bool UseMatching;
        public bool RegularExpression;
        public bool ReverseMode;
        public string ColumnName;
        public List<string> ArrayValues;

        public FrmFilter(Form form)
        {
            InitializeComponent();
            this.Location = new Point(form.Location.X + (form.Width - this.Width) / 2, form.Location.Y + (form.Height - this.Height) / 2);
            ColumnPanel.Visible = false;
        }

        public FrmFilter(NDataGridView dataGridView)
        {
            InitializeComponent();
            Location = new Point(
                dataGridView.PointToScreen(Point.Empty).X + (dataGridView.Width - this.Width) / 2,
                dataGridView.PointToScreen(Point.Empty).Y + (dataGridView.Height - this.Height) / 2
            );
            ColumnPanel.Visible = true;

            foreach (DataGridViewColumn HeaderText in dataGridView.Columns)
            {
                if (HeaderText.Visible)
                {
                    Columns.Items.Add(new FilterColumnItem(HeaderText.Name, GetColumnDisplayName(HeaderText)));
                }
            }

            FilterColumnItem textValueItem = FindColumnItem("Text value");
            if (textValueItem != null)
            {
                Columns.SelectedItem = textValueItem;
            }
            else if (Columns.Items.Count > 0)
            {
                Columns.SelectedIndex = 0;
            }
        }



        private void button1_Click(object sender, EventArgs e)
        {
            ArrayValues = new List<string>();

            ArrayValues.Add(matchcase.Checked + "|" + regularexpression.Checked + "|" + reversemode.Checked+"|"+GetSelectedColumnName());
            foreach (string val in listBox1.Items)
            {
                ArrayValues.Add(val);
            }

            File.WriteAllLines("FilterValues.txt", ArrayValues.ToArray());
            ArrayValues.RemoveAt(0);
            UseMatching = matchcase.Checked;
            RegularExpression = regularexpression.Checked;
            ReverseMode = reversemode.Checked;
            ColumnName = GetSelectedColumnName();
            this.Close();
        }

        private void ClearList_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void RemoveSelected_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            else
            {
                MessageBox.Show("请先从列表中选择一个值。", "未选择项目", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void Add_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("输入内容不能为空。", "空值", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (!listBox1.Items.Contains(textBox1.Text))
                listBox1.Items.Add(textBox1.Text);
            else
            {
                MessageBox.Show($"值“{textBox1.Text}”已存在于列表中。", "值已存在", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

        }

        private void FrmFilter_Load(object sender, EventArgs e)
        {
            if (File.Exists("FilterValues.txt"))
            {
                listBox1.Items.Clear();
                List<string> FV = new List<string>();
                FV.AddRange(File.ReadAllLines("FilterValues.txt"));
                string[] Controls = FV[0].Split(new char[] { '|' });

                if (Controls.Length >0)
                {
                    if(Controls.Length > 0)
                    matchcase.Checked = Convert.ToBoolean(Controls[0]);
                    if (Controls.Length > 1)
                        regularexpression.Checked = Convert.ToBoolean(Controls[1]);
                    if (Controls.Length > 2)
                        reversemode.Checked = Convert.ToBoolean(Controls[2]);
                    if (Controls.Length > 3)
                    {
                        FilterColumnItem columnItem = FindColumnItem(Controls[3]);
                        if (columnItem != null)
                        {
                            Columns.SelectedItem = columnItem;
                        }
                    }
                    FV.RemoveAt(0);
                }
                listBox1.Items.AddRange(FV.ToArray());
            }
        }

        private void Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void regularexpression_CheckedChanged(object sender, EventArgs e)
        {

        }

        private string GetSelectedColumnName()
        {
            FilterColumnItem item = Columns.SelectedItem as FilterColumnItem;
            if (item != null)
            {
                return item.ColumnName;
            }

            return Columns.Text;
        }

        private FilterColumnItem FindColumnItem(string columnName)
        {
            foreach (object item in Columns.Items)
            {
                FilterColumnItem columnItem = item as FilterColumnItem;
                if (columnItem != null && string.Equals(columnItem.ColumnName, columnName, StringComparison.Ordinal))
                {
                    return columnItem;
                }
            }

            return null;
        }

        private string GetColumnDisplayName(DataGridViewColumn column)
        {
            if (column == null)
            {
                return "";
            }

            return string.IsNullOrWhiteSpace(column.HeaderText) ? column.Name : column.HeaderText;
        }
    }
}
