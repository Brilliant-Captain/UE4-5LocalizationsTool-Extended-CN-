using System;
using System.Drawing;
using System.Windows.Forms;
using UE4localizationsTool.Helper;

namespace UE4localizationsTool.Forms
{
    public sealed class FrmTranslationSettings : Form
    {
        private readonly ComboBox defaultProviderComboBox;
        private readonly TextBox doubaoApiKeyTextBox;
        private readonly TextBox googleApiKeyTextBox;
        private readonly TextBox baiduAppIdTextBox;
        private readonly TextBox baiduSecretKeyTextBox;
        private readonly TextBox tencentSecretIdTextBox;
        private readonly TextBox tencentSecretKeyTextBox;
        private readonly TextBox tencentRegionTextBox;

        public FrmTranslationSettings()
        {
            Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(134)));
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Text = "翻译接口设置";
            ClientSize = new Size(560, 430);

            var rootPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(12)
            };
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(rootPanel);

            var topPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            topPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "默认翻译接口：",
                Margin = new Padding(0, 6, 6, 0)
            });

            defaultProviderComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160
            };
            defaultProviderComboBox.Items.Add(TranslationProviderType.Doubao);
            defaultProviderComboBox.Items.Add(TranslationProviderType.Google);
            defaultProviderComboBox.Items.Add(TranslationProviderType.Baidu);
            defaultProviderComboBox.Items.Add(TranslationProviderType.Tencent);
            defaultProviderComboBox.Format += (sender, e) =>
            {
                if (e.ListItem is TranslationProviderType provider)
                {
                    e.Value = TranslationProviderHelper.GetDisplayName(provider);
                }
            };
            topPanel.Controls.Add(defaultProviderComboBox);
            rootPanel.Controls.Add(topPanel, 0, 0);

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            rootPanel.Controls.Add(tabControl, 0, 1);

            doubaoApiKeyTextBox = AddSingleSecretTab(tabControl, "豆包", "API Key：");
            googleApiKeyTextBox = AddSingleSecretTab(tabControl, "谷歌", "API Key：");

            var baiduTable = AddTabWithTable(tabControl, "百度", 2);
            baiduAppIdTextBox = AddRow(baiduTable, 0, "AppId：", false);
            baiduSecretKeyTextBox = AddRow(baiduTable, 1, "密钥：", true);

            var tencentTable = AddTabWithTable(tabControl, "腾讯", 3);
            tencentSecretIdTextBox = AddRow(tencentTable, 0, "SecretId：", false);
            tencentSecretKeyTextBox = AddRow(tencentTable, 1, "SecretKey：", true);
            tencentRegionTextBox = AddRow(tencentTable, 2, "Region：", false);

            var noteLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Text = "说明：百度需要 AppId + 密钥，腾讯需要 SecretId + SecretKey。腾讯默认 Region 可用 ap-beijing。"
            };
            rootPanel.Controls.Add(noteLabel, 0, 2);

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
            rootPanel.Controls.Add(buttonPanel, 0, 3);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            LoadSettings();
        }

        private TextBox AddSingleSecretTab(TabControl tabControl, string title, string labelText)
        {
            var table = AddTabWithTable(tabControl, title, 1);
            return AddRow(table, 0, labelText, true);
        }

        private TableLayoutPanel AddTabWithTable(TabControl tabControl, string title, int rowCount)
        {
            var page = new TabPage(title);
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = rowCount,
                Padding = new Padding(12, 16, 12, 12),
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < rowCount; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            }
            page.Controls.Add(table);
            tabControl.TabPages.Add(page);
            return table;
        }

        private TextBox AddRow(TableLayoutPanel table, int rowIndex, string labelText, bool secret)
        {
            var label = new Label
            {
                AutoSize = false,
                Text = labelText,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 10, 6)
            };
            table.Controls.Add(label, 0, rowIndex);

            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = secret,
                Margin = new Padding(0, 6, 0, 6),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            table.Controls.Add(textBox, 1, rowIndex);
            return textBox;
        }

        private void LoadSettings()
        {
            TranslationProviderSettings settings = TranslationSettingsStore.LoadProviderSettings();
            defaultProviderComboBox.SelectedItem = settings.SelectedProvider;
            doubaoApiKeyTextBox.Text = settings.DoubaoApiKey ?? "";
            googleApiKeyTextBox.Text = settings.GoogleApiKey ?? "";
            baiduAppIdTextBox.Text = settings.BaiduAppId ?? "";
            baiduSecretKeyTextBox.Text = settings.BaiduSecretKey ?? "";
            tencentSecretIdTextBox.Text = settings.TencentSecretId ?? "";
            tencentSecretKeyTextBox.Text = settings.TencentSecretKey ?? "";
            tencentRegionTextBox.Text = string.IsNullOrWhiteSpace(settings.TencentRegion) ? "ap-beijing" : settings.TencentRegion;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            TranslationProviderSettings settings = TranslationSettingsStore.LoadProviderSettings();
            settings.SelectedProvider = defaultProviderComboBox.SelectedItem is TranslationProviderType provider ? provider : TranslationProviderType.Doubao;
            settings.DoubaoApiKey = doubaoApiKeyTextBox.Text.Trim();
            settings.GoogleApiKey = googleApiKeyTextBox.Text.Trim();
            settings.BaiduAppId = baiduAppIdTextBox.Text.Trim();
            settings.BaiduSecretKey = baiduSecretKeyTextBox.Text.Trim();
            settings.TencentSecretId = tencentSecretIdTextBox.Text.Trim();
            settings.TencentSecretKey = tencentSecretKeyTextBox.Text.Trim();
            settings.TencentRegion = string.IsNullOrWhiteSpace(tencentRegionTextBox.Text) ? "ap-beijing" : tencentRegionTextBox.Text.Trim();

            TranslationSettingsStore.SaveProviderSettings(settings);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
