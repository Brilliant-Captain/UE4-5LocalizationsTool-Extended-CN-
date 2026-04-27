using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UE4localizationsTool.Helper;

namespace UE4localizationsTool.Forms
{
    public sealed class FrmTranslationTerminology : Form
    {
        private readonly ListBox termListBox;
        private readonly ComboBox partOfSpeechComboBox;
        private readonly TextBox sourceTextBox;
        private readonly TextBox translationTextBox;
        private readonly TextBox variantsTextBox;
        private readonly TextBox notesTextBox;
        private readonly CheckBox caseSensitiveCheckBox;
        private readonly List<TranslationTerminologyEntry> entries;
        private readonly Label variantsHelpLabel;

        private bool isLoadingEditor;
        private int currentEditingIndex = -1;

        public FrmTranslationTerminology()
        {
            Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(134)));
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Text = "翻译术语管理";
            ClientSize = new Size(920, 560);
            MinimumSize = new Size(900, 540);

            entries = (TranslationSettingsStore.LoadTerminologyEntries() ?? new List<TranslationTerminologyEntry>())
                .Select(entry => (entry ?? new TranslationTerminologyEntry()).Normalize())
                .ToList();

            var rootPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(12)
            };
            rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(rootPanel);

            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.Controls.Add(leftPanel, 0, 0);

            leftPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "术语列表：",
                Margin = new Padding(0, 0, 0, 6)
            }, 0, 0);

            termListBox = new ListBox
            {
                Dock = DockStyle.Fill
            };
            termListBox.SelectedIndexChanged += TermListBox_SelectedIndexChanged;
            leftPanel.Controls.Add(termListBox, 0, 1);

            var leftButtonPanel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 8, 0, 0)
            };
            leftButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            leftButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            leftButtonPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftButtonPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var addButton = new Button
            {
                Text = "新增术语",
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 6)
            };
            addButton.Click += AddButton_Click;
            leftButtonPanel.Controls.Add(addButton, 0, 0);

            var deleteButton = new Button
            {
                Text = "删除术语",
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 6)
            };
            deleteButton.Click += DeleteButton_Click;
            leftButtonPanel.Controls.Add(deleteButton, 1, 0);

            var exportButton = new Button
            {
                Text = "导出术语...",
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0)
            };
            exportButton.Click += ExportButton_Click;
            leftButtonPanel.Controls.Add(exportButton, 0, 1);

            var importButton = new Button
            {
                Text = "导入术语...",
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0)
            };
            importButton.Click += ImportButton_Click;
            leftButtonPanel.Controls.Add(importButton, 1, 1);
            leftPanel.Controls.Add(leftButtonPanel, 0, 2);

            var editorPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(12, 0, 0, 0)
            };
            editorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            editorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 5; i++)
            {
                editorPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            editorPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));
            editorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootPanel.Controls.Add(editorPanel, 1, 0);

            partOfSpeechComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Width = 180
            };
            partOfSpeechComboBox.Items.Add(TerminologyPartOfSpeech.Noun);
            partOfSpeechComboBox.Items.Add(TerminologyPartOfSpeech.Verb);
            partOfSpeechComboBox.Items.Add(TerminologyPartOfSpeech.Adjective);
            partOfSpeechComboBox.Items.Add(TerminologyPartOfSpeech.Adverb);
            partOfSpeechComboBox.Format += (sender, e) =>
            {
                if (e.ListItem is TerminologyPartOfSpeech partOfSpeech)
                {
                    e.Value = TranslationProviderHelper.GetPartOfSpeechDisplayName(partOfSpeech);
                }
            };

            sourceTextBox = CreateSingleLineTextBox();
            translationTextBox = CreateSingleLineTextBox();
            variantsTextBox = CreateMultiLineTextBox(120);
            notesTextBox = CreateMultiLineTextBox(130);
            caseSensitiveCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "大小写敏感",
                Margin = new Padding(0, 6, 0, 6)
            };

            AddEditorRow(editorPanel, 0, "词性：", partOfSpeechComboBox);
            AddEditorRow(editorPanel, 1, "术语原文：", sourceTextBox);
            AddEditorRow(editorPanel, 2, "术语译文：", translationTextBox);
            AddEditorRow(editorPanel, 3, "匹配规则：", caseSensitiveCheckBox);

            var variantsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            variantsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            variantsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            variantsHelpLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                ForeColor = Color.DimGray,
                Text = BuildVariantsHelpText(),
                Margin = new Padding(0, 0, 0, 4)
            };
            variantsPanel.Controls.Add(variantsHelpLabel, 0, 0);
            variantsPanel.Controls.Add(variantsTextBox, 0, 1);
            AddEditorRow(editorPanel, 4, "术语变体：", variantsPanel);

            var notesPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            notesPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            notesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            notesPanel.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Text = "额外说明：可记录上下文、禁用场景或翻译要求",
                Margin = new Padding(0, 0, 0, 4)
            }, 0, 0);
            notesPanel.Controls.Add(notesTextBox, 0, 1);
            AddEditorRow(editorPanel, 5, "额外说明：", notesPanel);

            editorPanel.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Text = "说明：术语按完整词/完整短语匹配。命中后会直接使用你设置的术语译文，不让机器翻译改写。",
                Margin = new Padding(0, 10, 0, 0)
            }, 1, 6);

            var bottomButtonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 0)
            };

            var saveButton = new Button
            {
                Text = "保存",
                AutoSize = true
            };
            saveButton.Click += SaveButton_Click;
            bottomButtonPanel.Controls.Add(saveButton);

            var cancelButton = new Button
            {
                Text = "取消",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            bottomButtonPanel.Controls.Add(cancelButton);
            rootPanel.Controls.Add(bottomButtonPanel, 1, 1);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            variantsPanel.Resize += (sender, e) => UpdateDynamicHelpLabelHeight(variantsHelpLabel, variantsPanel.ClientSize.Width);

            RefreshTermList();
            if (entries.Count > 0)
            {
                termListBox.SelectedIndex = 0;
            }
            else
            {
                AddNewEntryAndSelect();
            }

            UpdateDynamicHelpLabelHeight(variantsHelpLabel, variantsPanel.ClientSize.Width);
        }

        private TextBox CreateSingleLineTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Top,
                Margin = new Padding(0, 3, 0, 6)
            };
        }

        private TextBox CreateMultiLineTextBox(int height)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = height,
                AcceptsReturn = true
            };
        }

        private void AddEditorRow(TableLayoutPanel panel, int rowIndex, string labelText, Control control)
        {
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = labelText,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 8, 6)
            }, 0, rowIndex);

            panel.Controls.Add(control, 1, rowIndex);
        }

        private string BuildVariantsHelpText()
        {
            var builder = new StringBuilder();
            builder.Append("术语变体：指“术语原文”的其他写法、复数/时态/常见短语写法。");
            builder.Append("每行一个，直接按回车添加。");
            builder.Append("这里填写的是原文变体，不是术语译文的变体。");
            return builder.ToString();
        }

        private void UpdateDynamicHelpLabelHeight(Label label, int availableWidth)
        {
            if (label == null)
            {
                return;
            }

            int targetWidth = Math.Max(240, availableWidth - label.Margin.Horizontal);
            Size preferredSize = TextRenderer.MeasureText(
                label.Text ?? "",
                label.Font,
                new Size(targetWidth, int.MaxValue),
                TextFormatFlags.WordBreak);

            label.Width = targetWidth;
            label.Height = preferredSize.Height + 6;
        }

        private void RefreshTermList()
        {
            int selectedIndex = termListBox.SelectedIndex;
            termListBox.Items.Clear();
            foreach (TranslationTerminologyEntry entry in entries)
            {
                string source = string.IsNullOrWhiteSpace(entry.SourceText) ? "未命名术语" : entry.SourceText;
                string target = string.IsNullOrWhiteSpace(entry.TargetText) ? "未设置译文" : entry.TargetText;
                string typeName = TranslationProviderHelper.GetPartOfSpeechDisplayName(entry.PartOfSpeech);
                termListBox.Items.Add($"[{typeName}] {source} -> {target}");
            }

            if (entries.Count == 0)
            {
                currentEditingIndex = -1;
                LoadEntryToEditor(null);
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= entries.Count)
            {
                selectedIndex = 0;
            }

            termListBox.SelectedIndex = selectedIndex;
        }

        private void AddNewEntryAndSelect()
        {
            SaveCurrentEditorToEntry(false);
            entries.Add(new TranslationTerminologyEntry());
            RefreshTermList();
            termListBox.SelectedIndex = entries.Count - 1;
        }

        private void LoadEntryToEditor(TranslationTerminologyEntry entry)
        {
            isLoadingEditor = true;
            try
            {
                TranslationTerminologyEntry safeEntry = (entry ?? new TranslationTerminologyEntry()).Normalize();
                partOfSpeechComboBox.SelectedItem = safeEntry.PartOfSpeech;
                sourceTextBox.Text = safeEntry.SourceText;
                translationTextBox.Text = safeEntry.TargetText;
                caseSensitiveCheckBox.Checked = safeEntry.CaseSensitive;
                variantsTextBox.Text = string.Join(Environment.NewLine, safeEntry.Variants.Where(item => !string.IsNullOrWhiteSpace(item)));
                notesTextBox.Text = safeEntry.Notes;
            }
            finally
            {
                isLoadingEditor = false;
            }
        }

        private void SaveCurrentEditorToEntry(bool refreshList)
        {
            if (isLoadingEditor || currentEditingIndex < 0 || currentEditingIndex >= entries.Count)
            {
                return;
            }

            TranslationTerminologyEntry entry = entries[currentEditingIndex];
            entry.PartOfSpeech = partOfSpeechComboBox.SelectedItem is TerminologyPartOfSpeech partOfSpeech
                ? partOfSpeech
                : TerminologyPartOfSpeech.Noun;
            entry.SourceText = sourceTextBox.Text.Trim();
            entry.TargetText = translationTextBox.Text.Trim();
            entry.CaseSensitive = caseSensitiveCheckBox.Checked;
            entry.Variants = variantsTextBox
                .Lines
                .Select(line => (line ?? "").Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            entry.Notes = notesTextBox.Text.Trim();

            if (refreshList)
            {
                int selectedIndex = currentEditingIndex;
                RefreshTermList();
                if (selectedIndex >= 0 && selectedIndex < termListBox.Items.Count)
                {
                    termListBox.SelectedIndex = selectedIndex;
                }
            }
        }

        private void TermListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int newIndex = termListBox.SelectedIndex;
            if (newIndex == currentEditingIndex)
            {
                return;
            }

            SaveCurrentEditorToEntry(false);
            currentEditingIndex = newIndex;
            LoadEntryToEditor(newIndex >= 0 && newIndex < entries.Count ? entries[newIndex] : null);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            AddNewEntryAndSelect();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (termListBox.SelectedIndex < 0 || termListBox.SelectedIndex >= entries.Count)
            {
                return;
            }

            int removeIndex = termListBox.SelectedIndex;
            entries.RemoveAt(removeIndex);
            currentEditingIndex = -1;
            RefreshTermList();

            if (entries.Count == 0)
            {
                AddNewEntryAndSelect();
            }
            else
            {
                termListBox.SelectedIndex = Math.Min(removeIndex, entries.Count - 1);
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            SaveCurrentEditorToEntry(false);
            List<TranslationTerminologyEntry> exportEntries = BuildValidatedTerminologyEntries(showMessageOnError: true);
            if (exportEntries == null)
            {
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "导出术语";
                dialog.Filter = "术语文件 (*.csv;*.json)|*.csv;*.json|CSV 文件 (*.csv)|*.csv|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.DefaultExt = "csv";
                dialog.FileName = "translation-terminology.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    TranslationSettingsStore.SaveTerminologyEntriesToFile(dialog.FileName, exportEntries);
                    MessageBox.Show("术语已导出到：\n" + dialog.FileName, "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导出术语失败：\n" + ex.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "导入术语";
                dialog.Filter = "术语文件 (*.csv;*.json)|*.csv;*.json|CSV 文件 (*.csv)|*.csv|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.Multiselect = false;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    List<TranslationTerminologyEntry> importedEntries = TranslationSettingsStore.LoadTerminologyEntriesFromFile(dialog.FileName);
                    if (importedEntries.Count == 0)
                    {
                        MessageBox.Show("所选文件中没有可用术语。", "导入完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    DialogResult mode = MessageBox.Show(
                        "检测到 " + importedEntries.Count + " 条术语。\n\n选择“是”将替换当前术语列表。\n选择“否”将追加到当前术语列表。",
                        "导入方式",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (mode == DialogResult.Cancel)
                    {
                        return;
                    }

                    SaveCurrentEditorToEntry(false);
                    if (mode == DialogResult.Yes)
                    {
                        entries.Clear();
                    }

                    entries.AddRange(importedEntries.Select(entry => (entry ?? new TranslationTerminologyEntry()).Normalize()));
                    currentEditingIndex = -1;
                    RefreshTermList();
                    if (entries.Count > 0)
                    {
                        termListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导入术语失败：\n" + ex.Message, "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveCurrentEditorToEntry(false);
            List<TranslationTerminologyEntry> finalEntries = BuildValidatedTerminologyEntries(showMessageOnError: true);
            if (finalEntries == null)
            {
                return;
            }

            TranslationSettingsStore.SaveTerminologyEntries(finalEntries);

            DialogResult = DialogResult.OK;
            Close();
        }

        private List<TranslationTerminologyEntry> BuildValidatedTerminologyEntries(bool showMessageOnError)
        {
            List<TranslationTerminologyEntry> finalEntries = entries
                .Select(entry => (entry ?? new TranslationTerminologyEntry()).Normalize())
                .Where(entry =>
                    !string.IsNullOrWhiteSpace(entry.SourceText) ||
                    !string.IsNullOrWhiteSpace(entry.TargetText) ||
                    entry.Variants.Count > 0 ||
                    !string.IsNullOrWhiteSpace(entry.Notes))
                .ToList();

            for (int i = 0; i < finalEntries.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(finalEntries[i].SourceText))
                {
                    if (showMessageOnError)
                    {
                        MessageBox.Show($"第 {i + 1} 条术语未填写“术语原文”。", "无法保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    return null;
                }

                if (string.IsNullOrWhiteSpace(finalEntries[i].TargetText))
                {
                    if (showMessageOnError)
                    {
                        MessageBox.Show($"第 {i + 1} 条术语未填写“术语译文”。", "无法保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    return null;
                }
            }

            return finalEntries;
        }
    }
}
