using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace UE4localizationsTool.Controls
{
    public partial class SearchBox : UserControl
    {

        private string ColumnName = "Text value";

        [Browsable(true)]
        public NDataGridView DataGridView { get; set; }
        int CurrentRowIndex = -1;
        int CurrentColumnIndex = -1;

        public SearchBox()
        {
            InitializeComponent();
            Hide();
        }
        public SearchBox(NDataGridView dataGrid)
        {
            DataGridView = dataGrid;
            InitializeComponent();
            Hide();
        }

        private void SearchHide_Click(object sender, System.EventArgs e)
        {
            Hide();
            listView1.Visible = false;
            label2.Text = string.Empty;
        }

        private bool IsMatch(string value)
        {
            string searchText = InputSearch.Text;

            if (string.IsNullOrWhiteSpace(searchText))
                return false;

            return value.IndexOf(searchText, GetStringComparison()) > -1;
        }

        private bool IsMatch(string value, string match)
        {
            if (string.IsNullOrWhiteSpace(match))
                return false;

            return value.IndexOf(match, GetStringComparison()) > -1;
        }

        private StringComparison GetStringComparison()
        {
            return MatchCaseCheckBox.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        }

        private RegexOptions GetReplaceOptions()
        {
            return MatchCaseCheckBox.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
        }

        private void EnsureSearchColumns()
        {
            if (DataGridView == null)
            {
                return;
            }

            string selectedColumn = SearchColumn.SelectedItem?.ToString() ?? ColumnName;

            SearchColumn.Items.Clear();
            foreach (DataGridViewColumn column in DataGridView.Columns)
            {
                if (column.Visible)
                {
                    SearchColumn.Items.Add(column.Name);
                }
            }

            if (SearchColumn.Items.Count == 0)
            {
                return;
            }

            if (SearchColumn.Items.Contains(selectedColumn))
            {
                SearchColumn.SelectedItem = selectedColumn;
            }
            else if (SearchColumn.Items.Contains("Text value"))
            {
                SearchColumn.SelectedItem = "Text value";
            }
            else
            {
                SearchColumn.SelectedIndex = 0;
            }

            ColumnName = SearchColumn.SelectedItem?.ToString() ?? ColumnName;
        }

        private DataGridViewCell GetSearchCellForRow(int rowIndex)
        {
            EnsureSearchColumns();
            return DataGridView.Rows[rowIndex].Cells[ColumnName];
        }

        private DataGridViewCell GetActiveSearchCell()
        {
            if (DataGridView.Rows.Count == 0)
            {
                return null;
            }

            if (DataGridView.SelectedCells.Count > 0)
            {
                return GetSearchCellForRow(DataGridView.SelectedCells[0].RowIndex);
            }

            return GetSearchCellForRow(0);
        }

        private bool EnsureReplaceColumnEditable()
        {
            EnsureSearchColumns();
            if (!DataGridView.Columns.Contains(ColumnName))
            {
                return false;
            }

            if (DataGridView.Columns[ColumnName].ReadOnly)
            {
                MessageBox.Show("当前搜索列为只读，只能查找，不能替换。", "无法替换", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private string ReplaceText(string sourceText)
        {
            return Regex.Replace(
                sourceText,
                Regex.Escape(InputSearch.Text),
                _ => txtReplace.Text,
                GetReplaceOptions());
        }


        private DataGridViewCell FindCell(int startRowIndex, int endRowIndex, string columnName, string value, bool endToTop = false, bool returnNullIfNotFound = false)
        {
            int step = endToTop ? -1 : 1;

            for (int rowIndex = startRowIndex; endToTop ? rowIndex > endRowIndex : rowIndex < endRowIndex; rowIndex += step)
            {
                if (DataGridView.Columns.Contains(columnName))
                {
                    DataGridViewCell cell = DataGridView.Rows[rowIndex].Cells[columnName];
                    if (cell.Value != null && IsMatch(cell.Value.ToString(), value))
                    {
                        return cell;
                    }
                }
            }

            if (returnNullIfNotFound)
            {
                return null;
            }

            return FindCell(endToTop ? DataGridView.Rows.Count - 1 : 0, endToTop ? startRowIndex : endRowIndex, columnName, value, endToTop, true);
        }


        private void FindNext_Click(object sender, EventArgs e)
        {
            EnsureSearchColumns();
            if (DataGridView.Rows.Count == 0)
            {
                MessageBox.Show("未找到可搜索的数据。", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedCell = GetActiveSearchCell();

            var cell = FindCell(selectedCell.RowIndex + 1, DataGridView.Rows.Count, ColumnName, InputSearch.Text);

            if (cell != null)
            {

                if (DataGridView.SelectedCells.Count > 0 && object.ReferenceEquals(cell, GetSearchCellForRow(DataGridView.SelectedCells[0].RowIndex)))
                {
                    MessageBox.Show("没有更多匹配项了。", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SelectCell(cell.RowIndex, cell.ColumnIndex);
            }
            else
            {
                Failedmessage();
            }

        }


        private void FindPrevious_Click(object sender, EventArgs e)
        {
            EnsureSearchColumns();

            if (DataGridView.Rows.Count == 0)
            {
                MessageBox.Show("未找到可搜索的数据。", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedCell = GetActiveSearchCell();

            var cell = FindCell(selectedCell.RowIndex - 1, -1, ColumnName, InputSearch.Text, true);

            if (cell != null)
            {

                if (DataGridView.SelectedCells.Count > 0 && object.ReferenceEquals(cell, GetSearchCellForRow(DataGridView.SelectedCells[0].RowIndex)))
                {
                    MessageBox.Show("没有更多匹配项了。", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SelectCell(cell.RowIndex, cell.ColumnIndex);
            }
            else
            {
                Failedmessage();
            }
        }


        private void Failedmessage()
        {
            MessageBox.Show(
                text: $"未找到搜索内容“{InputSearch.Text}”。",
                caption: "搜索结果",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning
            );
        }

        private void SelectCell(int rowIndex, int colIndex)
        {
            DataGridView.ClearSelection();
            DataGridView.FirstDisplayedScrollingRowIndex = rowIndex;
            DataGridView.Rows[rowIndex].Cells[colIndex].Selected = true;

            CurrentRowIndex = rowIndex;
            CurrentColumnIndex = colIndex;
        }

        private void InputSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (!InputSearch.Focused)
            {
                InputSearch.Focus();
            }

            if (e.KeyCode == Keys.Enter)
            {
                FindNext_Click(sender, e);
            }
        }

        public new void Show()
        {
            EnsureSearchColumns();
            if (DataGridView.CurrentCell != null)
            {
                if (SearchColumn.Items.Contains(DataGridView.CurrentCell.OwningColumn.Name))
                {
                    SearchColumn.SelectedItem = DataGridView.CurrentCell.OwningColumn.Name;
                    ColumnName = SearchColumn.SelectedItem.ToString();
                }

                InputSearch.Text = DataGridView.CurrentCell.Value?.ToString() ?? "";
            }
            InputSearch.Focus();

            label2.Text = string.Empty;
            base.Show();
        }

        public void ShowReplacePanel()
        {
            Replacepanel.Visible = true;
            Show();
            txtReplace.Focus();
        }

        public int CountTotalMatches()
        {
            EnsureSearchColumns();
            int totalMatches = 0;

            for (int rowIndex = 0; rowIndex < DataGridView.Rows.Count; rowIndex++)
            {
                if (rowIndex >= 0 && rowIndex < DataGridView.Rows.Count)
                {
                    DataGridViewCell cell = DataGridView.Rows[rowIndex].Cells[ColumnName];
                    if (cell.Value != null && IsMatch(cell.Value.ToString()))
                    {
                        totalMatches++;
                    }
                }
            }

            return totalMatches;
        }

        string ButtonText;
        bool IsFindAll = false;

        private void FindAll_Click(object sender, EventArgs e)
        {
            Replacepanel.Visible = false;
            EnsureSearchColumns();

            if (DataGridView.Rows.Count == 0)
            {
                MessageBox.Show("未找到可搜索的数据。", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            listView1.Items.Clear();

            foreach (DataGridViewRow row in DataGridView.Rows)
            {
                if (DataGridView.Columns.Contains(ColumnName))
                {
                    if (row.Cells[ColumnName].Value != null && IsMatch(row.Cells[ColumnName].Value.ToString()))
                    {
                        ListViewItem item = new ListViewItem();
                        item.Text = (row.Index + 1).ToString();
                        item.SubItems.Add(row.Cells[ColumnName].Value.ToString());
                        item.Tag = row;
                        listView1.Items.Add(item);
                    }
                }
            }
            listView1.Visible = true;
            label2.Text = $"共 {listView1.Items.Count} 项";

        }


        private void Replace_Click(object sender, EventArgs e)
        {

            void ReplaceCell(DataGridViewCell Cell)
            {
                DataGridView.SetValue(Cell, ReplaceText(Cell.Value.ToString()));
            }

            EnsureSearchColumns();
            if (!EnsureReplaceColumnEditable())
            {
                return;
            }

            var selectedCell = GetActiveSearchCell();

            if (IsMatch(selectedCell.Value.ToString()))
            {
                ReplaceCell(selectedCell);
                return;
            }

            var cell = FindCell(selectedCell.RowIndex + 1, DataGridView.Rows.Count, ColumnName, InputSearch.Text);

            if (cell != null)
            {
                SelectCell(cell.RowIndex, cell.ColumnIndex);
                ReplaceCell(cell);
            }
            else
            {
                Failedmessage();
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {
            Replacepanel.Visible = false;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 0)
                return;
            var cell = (listView1.SelectedItems[0].Tag as DataGridViewRow).Cells[ColumnName];

            SelectCell(cell.RowIndex, cell.ColumnIndex);
        }

        private void ReplaceAll_Click(object sender, EventArgs e)
        {
            EnsureSearchColumns();
            if (DataGridView.Rows.Count == 0)
            {
                MessageBox.Show("未找到可搜索的数据。", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!EnsureReplaceColumnEditable())
            {
                return;
            }

            int totalMatches = 0;
            foreach (DataGridViewRow row in DataGridView.Rows)
            {
                if (DataGridView.Columns.Contains(ColumnName))
                {
                    if (row.Cells[ColumnName].Value != null && IsMatch(row.Cells[ColumnName].Value.ToString()))
                    {
                        DataGridView.SetValue(row.Cells[ColumnName], ReplaceText(row.Cells[ColumnName].Value.ToString()));
                        totalMatches++;
                    }
                }
            }


            MessageBox.Show($"已替换匹配项：{totalMatches}", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SearchColumn_SelectedIndexChanged(object sender, EventArgs e)
        {
            ColumnName = SearchColumn.SelectedItem?.ToString() ?? ColumnName;
            listView1.Visible = false;
            label2.Text = string.Empty;
        }
    }
}
