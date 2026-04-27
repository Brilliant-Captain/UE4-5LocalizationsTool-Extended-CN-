using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UE4localizationsTool.Helper;

namespace UE4localizationsTool.Forms
{
    public sealed class FrmDoubaoTranslate : Form
    {
        private readonly ComboBox providerComboBox;
        private readonly ComboBox sourceLanguageComboBox;
        private readonly ComboBox targetLanguageComboBox;
        private readonly ComboBox scopeComboBox;
        private readonly NumericUpDown rowCountNumericUpDown;
        private readonly CheckBox preserveFormattingCheckBox;
        private readonly CheckBox skipPreviewedRowsCheckBox;
        private readonly Label selectedRowsInfoLabel;
        private readonly Label providerNoteLabel;

        public FrmDoubaoTranslate(int selectedRowCount)
        {
            SelectedRowCount = selectedRowCount;
            CurrentSettings = TranslationSettingsStore.LoadProviderSettings();

            Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(134)));
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Text = "翻译预览";
            ClientSize = new Size(600, 410);
            MinimumSize = new Size(600, 410);

            var rootPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(12),
            };
            rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(rootPanel);

            rootPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "翻译接口：",
                Anchor = AnchorStyles.Left
            }, 0, 0);
            var providerPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 0)
            };
            providerComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 140
            };
            providerComboBox.Items.Add(TranslationProviderType.Doubao);
            providerComboBox.Items.Add(TranslationProviderType.Google);
            providerComboBox.Items.Add(TranslationProviderType.Baidu);
            providerComboBox.Items.Add(TranslationProviderType.Tencent);
            providerComboBox.Format += (sender, e) =>
            {
                if (e.ListItem is TranslationProviderType provider)
                {
                    e.Value = TranslationProviderHelper.GetDisplayName(provider);
                }
            };
            providerComboBox.SelectedIndexChanged += ProviderComboBox_SelectedIndexChanged;
            providerPanel.Controls.Add(providerComboBox);

            var settingsButton = new Button
            {
                Text = "接口设置...",
                AutoSize = true
            };
            settingsButton.Click += SettingsButton_Click;
            providerPanel.Controls.Add(settingsButton);
            rootPanel.Controls.Add(providerPanel, 1, 0);

            providerNoteLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(110, 2, 0, 8)
            };
            rootPanel.Controls.Add(providerNoteLabel, 1, 1);

            rootPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "源语言：",
                Anchor = AnchorStyles.Left
            }, 0, 2);

            sourceLanguageComboBox = CreateLanguageComboBox();
            rootPanel.Controls.Add(sourceLanguageComboBox, 1, 2);

            rootPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "目标语言：",
                Anchor = AnchorStyles.Left
            }, 0, 3);

            targetLanguageComboBox = CreateLanguageComboBox();
            rootPanel.Controls.Add(targetLanguageComboBox, 1, 3);

            rootPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "翻译范围：",
                Anchor = AnchorStyles.Left
            }, 0, 4);

            scopeComboBox = new ComboBox
            {
                Dock = DockStyle.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220
            };
            scopeComboBox.Items.Add(new ScopeItem("仅翻译当前选中行", DoubaoTranslationScope.SelectedRows));
            scopeComboBox.Items.Add(new ScopeItem("翻译前 N 行", DoubaoTranslationScope.FirstNRows));
            scopeComboBox.Items.Add(new ScopeItem("翻译全部可用行", DoubaoTranslationScope.AllRows));
            scopeComboBox.SelectedIndexChanged += ScopeComboBox_SelectedIndexChanged;
            rootPanel.Controls.Add(scopeComboBox, 1, 4);

            var rowCountPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 3, 0, 3)
            };
            rowCountPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "单次行数：",
                Margin = new Padding(0, 5, 6, 0)
            });

            rowCountNumericUpDown = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10000,
                Value = 20,
                Width = 100
            };
            rowCountPanel.Controls.Add(rowCountNumericUpDown);
            rootPanel.Controls.Add(rowCountPanel, 1, 5);

            var optionsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 3, 0, 8)
            };

            preserveFormattingCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Text = "保留常见占位符、标签和首尾空白"
            };
            optionsPanel.Controls.Add(preserveFormattingCheckBox);

            var ruleSettingsButton = new Button
            {
                Text = "规则设置...",
                AutoSize = true,
                Margin = new Padding(20, 0, 0, 6)
            };
            ruleSettingsButton.Click += RuleSettingsButton_Click;
            optionsPanel.Controls.Add(ruleSettingsButton);

            var terminologyButton = new Button
            {
                Text = "术语管理...",
                AutoSize = true,
                Margin = new Padding(20, 0, 0, 6)
            };
            terminologyButton.Click += TerminologyButton_Click;
            optionsPanel.Controls.Add(terminologyButton);

            skipPreviewedRowsCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Text = "跳过已有翻译预览的行"
            };
            optionsPanel.Controls.Add(skipPreviewedRowsCheckBox);

            selectedRowsInfoLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Text = "当前已选中 0 行",
                Margin = new Padding(0, 0, 0, 6)
            };
            optionsPanel.Controls.Add(selectedRowsInfoLabel);

            rootPanel.Controls.Add(optionsPanel, 1, 6);

            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 8, 0, 0)
            };

            var okButton = new Button
            {
                Text = "开始翻译",
                AutoSize = true
            };
            okButton.Click += OkButton_Click;
            buttonsPanel.Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "取消",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            buttonsPanel.Controls.Add(cancelButton);

            rootPanel.Controls.Add(buttonsPanel, 1, 7);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            InitializeLanguages();
            scopeComboBox.SelectedIndex = SelectedRowCount > 0 ? 0 : 1;
        }

        public int SelectedRowCount { get; }

        public TranslationProviderSettings CurrentSettings { get; private set; }

        public TranslationProviderType SelectedProvider => providerComboBox.SelectedItem is TranslationProviderType provider ? provider : TranslationProviderType.Doubao;

        public string SourceLanguageCode => (sourceLanguageComboBox.SelectedItem as TranslationLanguage)?.Code ?? "";

        public string TargetLanguageCode => (targetLanguageComboBox.SelectedItem as TranslationLanguage)?.Code ?? "";

        public DoubaoTranslationScope TranslationScope => (scopeComboBox.SelectedItem as ScopeItem)?.Scope ?? DoubaoTranslationScope.SelectedRows;

        public int RowCountLimit => Decimal.ToInt32(rowCountNumericUpDown.Value);

        public bool PreserveFormatting => preserveFormattingCheckBox.Checked;

        public bool SkipPreviewedRows => skipPreviewedRowsCheckBox.Checked;

        private ComboBox CreateLanguageComboBox()
        {
            return new ComboBox
            {
                Dock = DockStyle.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220
            };
        }

        private void InitializeLanguages()
        {
            sourceLanguageComboBox.Items.AddRange(DoubaoTranslationService.SupportedLanguages.Cast<object>().ToArray());
            targetLanguageComboBox.Items.AddRange(
                DoubaoTranslationService.SupportedLanguages.Where(language => !string.IsNullOrEmpty(language.Code)).Cast<object>().ToArray());

            sourceLanguageComboBox.SelectedIndex = 0;

            for (int i = 0; i < targetLanguageComboBox.Items.Count; i++)
            {
                if ((targetLanguageComboBox.Items[i] as TranslationLanguage)?.Code == "zh")
                {
                    targetLanguageComboBox.SelectedIndex = i;
                    break;
                }
            }

            providerComboBox.SelectedItem = CurrentSettings.SelectedProvider;
        }

        private void ProviderComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedProvider == TranslationProviderType.Tencent)
            {
                providerNoteLabel.Text = "腾讯翻译当前需要手动指定源语言，不能留“自动检测”。";
            }
            else if (SelectedProvider == TranslationProviderType.Baidu)
            {
                providerNoteLabel.Text = "百度翻译需要在设置中填写 AppId 和密钥。";
            }
            else
            {
                providerNoteLabel.Text = "当前接口：" + TranslationProviderHelper.GetDisplayName(SelectedProvider);
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new FrmTranslationSettings())
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    CurrentSettings = TranslationSettingsStore.LoadProviderSettings();
                    providerComboBox.SelectedItem = CurrentSettings.SelectedProvider;
                }
            }
        }

        private void RuleSettingsButton_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new FrmTranslationRuleSettings())
            {
                settingsForm.ShowDialog(this);
            }
        }

        private void TerminologyButton_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new FrmTranslationTerminology())
            {
                settingsForm.ShowDialog(this);
            }
        }

        private void ScopeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool enableCount = TranslationScope == DoubaoTranslationScope.FirstNRows;
            rowCountNumericUpDown.Enabled = enableCount;
            selectedRowsInfoLabel.Text = "当前已选中 " + SelectedRowCount + " 行";
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            CurrentSettings.SelectedProvider = SelectedProvider;
            TranslationSettingsStore.SaveProviderSettings(CurrentSettings);

            string credentialError = TranslationProviderFactory.ValidateCredentials(CurrentSettings, SelectedProvider);
            if (!string.IsNullOrWhiteSpace(credentialError))
            {
                MessageBox.Show(credentialError, "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TargetLanguageCode))
            {
                MessageBox.Show("请选择目标语言。", "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (SelectedProvider == TranslationProviderType.Tencent && string.IsNullOrWhiteSpace(SourceLanguageCode))
            {
                MessageBox.Show("腾讯翻译当前需要手动指定源语言。", "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (TranslationScope == DoubaoTranslationScope.SelectedRows && SelectedRowCount <= 0)
            {
                MessageBox.Show("当前没有选中任何行。\n可以先手动选中，或使用 Ctrl+Shift+G 按行号批量选中。", "无法执行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class ScopeItem
        {
            public ScopeItem(string displayName, DoubaoTranslationScope scope)
            {
                DisplayName = displayName;
                Scope = scope;
            }

            public string DisplayName { get; }

            public DoubaoTranslationScope Scope { get; }

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }
}
