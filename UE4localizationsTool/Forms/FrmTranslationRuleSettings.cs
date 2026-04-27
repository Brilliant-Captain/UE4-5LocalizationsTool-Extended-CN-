using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UE4localizationsTool.Helper;

namespace UE4localizationsTool.Forms
{
    public sealed class FrmTranslationRuleSettings : Form
    {
        private readonly CheckBox preserveEscapeSequencesCheckBox;
        private readonly CheckBox preservePlaceholdersCheckBox;
        private readonly CheckBox preserveAngleBracketTagsCheckBox;
        private readonly CheckBox preserveSquareBracketTagsCheckBox;
        private readonly CheckBox preserveWhitespaceCheckBox;
        private readonly TextBox customPatternsTextBox;

        public FrmTranslationRuleSettings()
        {
            Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(134)));
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Text = "翻译规则设置";
            ClientSize = new Size(620, 470);

            var rootPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(rootPanel);

            var rulesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            rootPanel.Controls.Add(rulesPanel, 0, 0);

            preserveEscapeSequencesCheckBox = CreateCheckBox("保留转义控制符（\\n / \\r / \\t）");
            rulesPanel.Controls.Add(preserveEscapeSequencesCheckBox);

            preservePlaceholdersCheckBox = CreateCheckBox("保留常见占位符（%s / %d / {0} / ${name}）");
            rulesPanel.Controls.Add(preservePlaceholdersCheckBox);

            preserveAngleBracketTagsCheckBox = CreateCheckBox("保留尖括号标签（<color> / <br> 等）");
            rulesPanel.Controls.Add(preserveAngleBracketTagsCheckBox);

            preserveSquareBracketTagsCheckBox = CreateCheckBox("保留方括号标签（[b] / [/b] / [Icon] 等）");
            rulesPanel.Controls.Add(preserveSquareBracketTagsCheckBox);

            preserveWhitespaceCheckBox = CreateCheckBox("保留首尾空白（开头/结尾空格、Tab、换行）");
            rulesPanel.Controls.Add(preserveWhitespaceCheckBox);

            var quickActionPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            var useDefaultButton = new Button
            {
                Text = "全部默认",
                AutoSize = true
            };
            useDefaultButton.Click += UseDefaultButton_Click;
            quickActionPanel.Controls.Add(useDefaultButton);

            var clearAllButton = new Button
            {
                Text = "全部取消",
                AutoSize = true
            };
            clearAllButton.Click += ClearAllButton_Click;
            quickActionPanel.Controls.Add(clearAllButton);
            rulesPanel.Controls.Add(quickActionPanel);

            var customPatternsLabel = new Label
            {
                AutoSize = true,
                Text = "自定义额外保护规则（每行一个正则）：",
                Margin = new Padding(0, 8, 0, 6)
            };
            rulesPanel.Controls.Add(customPatternsLabel);

            customPatternsTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Width = 560,
                Height = 130,
                Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)))
            };
            rulesPanel.Controls.Add(customPatternsTextBox);

            var noteLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Text = "说明：仅在“翻译预览”里启用“保留常见占位符、标签和首尾空白”时，这些规则才会生效。\n自定义规则支持正则表达式，匹配到的内容会被保护，不参与翻译。"
            };
            rootPanel.Controls.Add(noteLabel, 0, 1);

            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Bottom
            };

            var saveButton = new Button
            {
                Text = "保存",
                AutoSize = true
            };
            saveButton.Click += SaveButton_Click;
            buttonPanel.Controls.Add(saveButton);

            var cancelButton = new Button
            {
                Text = "取消",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            buttonPanel.Controls.Add(cancelButton);
            rootPanel.Controls.Add(buttonPanel, 0, 2);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            LoadSettings();
        }

        private CheckBox CreateCheckBox(string text)
        {
            return new CheckBox
            {
                AutoSize = true,
                Text = text,
                Margin = new Padding(0, 0, 0, 10)
            };
        }

        private void LoadSettings()
        {
            TranslationFormattingRules rules = TranslationSettingsStore.LoadFormattingRules();
            preserveEscapeSequencesCheckBox.Checked = rules.PreserveEscapeSequences;
            preservePlaceholdersCheckBox.Checked = rules.PreservePlaceholders;
            preserveAngleBracketTagsCheckBox.Checked = rules.PreserveAngleBracketTags;
            preserveSquareBracketTagsCheckBox.Checked = rules.PreserveSquareBracketTags;
            preserveWhitespaceCheckBox.Checked = rules.PreserveLeadingAndTrailingWhitespace;
            customPatternsTextBox.Text = string.Join(Environment.NewLine, rules.CustomProtectedPatterns.Where(pattern => !string.IsNullOrWhiteSpace(pattern)));
        }

        private void UseDefaultButton_Click(object sender, EventArgs e)
        {
            preserveEscapeSequencesCheckBox.Checked = true;
            preservePlaceholdersCheckBox.Checked = true;
            preserveAngleBracketTagsCheckBox.Checked = true;
            preserveSquareBracketTagsCheckBox.Checked = true;
            preserveWhitespaceCheckBox.Checked = true;
        }

        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            preserveEscapeSequencesCheckBox.Checked = false;
            preservePlaceholdersCheckBox.Checked = false;
            preserveAngleBracketTagsCheckBox.Checked = false;
            preserveSquareBracketTagsCheckBox.Checked = false;
            preserveWhitespaceCheckBox.Checked = false;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            List<string> customPatterns = customPatternsTextBox
                .Lines
                .Select(line => (line ?? "").Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct()
                .ToList();

            for (int i = 0; i < customPatterns.Count; i++)
            {
                try
                {
                    _ = new Regex(customPatterns[i], RegexOptions.Compiled);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(
                        $"第 {i + 1} 条自定义规则无效：{customPatterns[i]}\n\n{ex.Message}",
                        "规则格式错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            TranslationFormattingRules rules = new TranslationFormattingRules
            {
                PreserveEscapeSequences = preserveEscapeSequencesCheckBox.Checked,
                PreservePlaceholders = preservePlaceholdersCheckBox.Checked,
                PreserveAngleBracketTags = preserveAngleBracketTagsCheckBox.Checked,
                PreserveSquareBracketTags = preserveSquareBracketTagsCheckBox.Checked,
                PreserveLeadingAndTrailingWhitespace = preserveWhitespaceCheckBox.Checked,
                CustomProtectedPatterns = customPatterns
            };

            TranslationSettingsStore.SaveFormattingRules(rules);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
