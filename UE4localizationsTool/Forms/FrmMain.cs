using AssetParser;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using UE4localizationsTool.Controls;
using UE4localizationsTool.Core.Hash;
using UE4localizationsTool.Core.locres;
using UE4localizationsTool.Forms;
using UE4localizationsTool.Helper;

namespace UE4localizationsTool
{
    public partial class FrmMain : NForm
    {
        private const string TranslationPreviewColumnName = DoubaoTranslationService.PreviewColumnName;

        private sealed class LocresEntrySnapshot
        {
            public string Name { get; set; }
            public string NameSpace { get; set; }
            public string Key { get; set; }
            public string TextValue { get; set; }
            public HashTable HashTable { get; set; }
            public DataGridViewRow Row { get; set; }
        }

        private enum HashOverwriteMode
        {
            MatchingTextOnly,
            DifferentTextOnly,
            SameNameForce
        }

        IAsset Asset;
        String ToolName = Application.ProductName + " v" + Application.ProductVersion;
        string FilePath = "";
        bool SortApply = false;
        public FrmMain()
        {
            InitializeComponent();
            dataGridView1.RowCountChanged += (x, y) => this.UpdateCounter();
            ResetControls();
            pictureBox1.Height = menuStrip1.Height;
            darkModeToolStripMenuItem.Checked = Properties.Settings.Default.DarkMode;
            Checkforupdates.Checked = Properties.Settings.Default.CheckForUpdates;
        }

        private void OpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "所有本地化文件|*.uasset;*.locres;*.umap|Uasset 文件|*.uasset|Locres 文件|*.locres|Umap 文件|*.umap";
            ofd.Title = "打开本地化文件";


            if (ofd.ShowDialog() == DialogResult.OK)
            {

                LoadFile(ofd.FileName);
            }
        }


        public async void LoadFile(string filePath)
        {
            ResetControls();
            ControlsMode(false);

            try
            {
                StatusMessage("正在加载文件...", "正在加载文件，请稍候。");

                if (filePath.ToLower().EndsWith(".locres"))
                {
                    Asset = await Task.Run(() => new LocresFile(filePath));
                    locresOprationsToolStripMenuItem.Visible = true;
                    CreateBackupList();
                }
                else if (filePath.ToLower().EndsWith(".uasset") || filePath.ToLower().EndsWith(".umap"))
                {
                    IUasset Uasset = await Task.Run(() => Uexp.GetUasset(filePath));
                    Uasset.UseMethod2 = Uasset.UseMethod2 ? Uasset.UseMethod2 : Method2.Checked;
                    Asset = await Task.Run(() => new Uexp(Uasset));
                    CreateBackupList();
                    if (!Asset.IsGood)
                    {
                        StateLabel.Text = "警告：该文件未被完整解析，可能缺少部分文本。";
                    }
                }

                this.FilePath = filePath;
                this.Text = ToolName + " - " + Path.GetFileName(FilePath);
                ControlsMode(true);
                CloseFromState();
            }
            catch (Exception ex)
            {
                CloseFromState();
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void CreateBackupList()
        {
            Asset.AddItemsToDataGridView(dataGridView1);
            ConfigureLocresPreviewColumn();
        }

        private void ConfigureLocresPreviewColumn()
        {
            DataGridViewColumn previewColumn = dataGridView1.Columns[TranslationPreviewColumnName];
            if (previewColumn == null)
            {
                return;
            }

            previewColumn.ReadOnly = true;
            previewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            previewColumn.FillWeight = 50F;
            previewColumn.HeaderText = TranslationPreviewColumnName;
            previewColumn.ToolTipText = "该列仅显示豆包翻译预览，不参与保存。";

            DataGridViewColumn textValueColumn = dataGridView1.Columns["Text value"];
            if (textValueColumn != null)
            {
                textValueColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                textValueColumn.FillWeight = 50F;
            }
        }

        private string BuildLocresName(string nameSpaceValue, string keyValue)
        {
            return string.IsNullOrEmpty(nameSpaceValue) ? keyValue : nameSpaceValue + "::" + keyValue;
        }

        private void SplitLocresName(string nameValue, out string nameSpaceValue, out string keyValue)
        {
            var items = nameValue.Split(new string[] { "::" }, StringSplitOptions.None);
            if (items.Length == 2)
            {
                nameSpaceValue = items[0];
                keyValue = items[1];
                return;
            }

            nameSpaceValue = "";
            keyValue = items[0];
        }

        private Dictionary<string, LocresEntrySnapshot> GetCurrentLocresEntries()
        {
            var entries = new Dictionary<string, LocresEntrySnapshot>(StringComparer.Ordinal);
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow || row.Cells["Name"].Value == null)
                {
                    continue;
                }

                string name = row.Cells["Name"].Value.ToString();
                string nameSpaceValue;
                string keyValue;
                SplitLocresName(name, out nameSpaceValue, out keyValue);

                entries[name] = new LocresEntrySnapshot
                {
                    Name = name,
                    NameSpace = nameSpaceValue,
                    Key = keyValue,
                    TextValue = row.Cells["Text value"].Value?.ToString() ?? "",
                    HashTable = row.Cells["Hash Table"].Value as HashTable,
                    Row = row
                };
            }

            return entries;
        }

        private List<LocresEntrySnapshot> GetLocresEntries(LocresFile locresFile)
        {
            var entries = new List<LocresEntrySnapshot>();
            foreach (var names in locresFile)
            {
                foreach (var table in names)
                {
                    entries.Add(new LocresEntrySnapshot
                    {
                        NameSpace = names.Name ?? "",
                        Key = table.key,
                        Name = BuildLocresName(names.Name ?? "", table.key),
                        TextValue = table.Value ?? "",
                        HashTable = new HashTable(names.NameHash, table.keyHash, table.ValueHash)
                    });
                }
            }

            return entries;
        }

        private bool EnsureLocresAssetLoaded()
        {
            if (!(Asset is LocresFile) || dataGridView1.DataSource == null)
            {
                MessageBox.Show("请先打开一个新的 Locres 文件后再执行此操作。", "无法执行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private Version ParseVersionString(string versionText)
        {
            if (string.IsNullOrWhiteSpace(versionText))
            {
                return new Version(0, 0, 0, 0);
            }

            if (Version.TryParse(versionText, out Version parsedVersion))
            {
                return parsedVersion;
            }

            string[] parts = versionText.Split('.');
            int[] numbers = new int[4];

            for (int i = 0; i < numbers.Length && i < parts.Length; i++)
            {
                int.TryParse(parts[i], out numbers[i]);
            }

            return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        private async Task<LocresFile> PromptAndLoadOldLocresAsync(string dialogTitle, string statusTitle)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Locres 文件|*.locres";
            ofd.Title = dialogTitle;

            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            StatusMessage(statusTitle, "正在读取旧 Locres 文件，请稍候。");
            return await Task.Run(() => new LocresFile(ofd.FileName));
        }

        private void AddLocresGridRow(System.Data.DataTable dataTable, string name, string textValue, HashTable hashTable)
        {
            DataRow row = dataTable.NewRow();
            row["Name"] = name;
            row["Text value"] = textValue ?? "";
            row["Hash Table"] = hashTable;

            if (dataTable.Columns.Contains(TranslationPreviewColumnName))
            {
                row[TranslationPreviewColumnName] = "";
            }

            dataTable.Rows.Add(row);
        }

        private void AppendMissingEntriesFromOldLocres(LocresFile oldLocres)
        {
            var currentEntries = GetCurrentLocresEntries();
            var oldEntries = GetLocresEntries(oldLocres);
            var dataTable = (System.Data.DataTable)dataGridView1.DataSource;
            var appendedNames = new HashSet<string>(StringComparer.Ordinal);

            int addedCount = 0;
            int exactMatchCount = 0;
            int mismatchCount = 0;

            foreach (var oldEntry in oldEntries)
            {
                if (currentEntries.TryGetValue(oldEntry.Name, out LocresEntrySnapshot currentEntry))
                {
                    if (string.Equals(currentEntry.TextValue, oldEntry.TextValue, StringComparison.Ordinal))
                    {
                        exactMatchCount++;
                    }
                    else
                    {
                        mismatchCount++;
                    }

                    continue;
                }

                AddLocresGridRow(dataTable, oldEntry.Name, oldEntry.TextValue, oldEntry.HashTable);
                currentEntries[oldEntry.Name] = oldEntry;
                appendedNames.Add(oldEntry.Name);
                addedCount++;
            }

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow || row.Cells["Name"].Value == null)
                {
                    continue;
                }

                if (appendedNames.Contains(row.Cells["Name"].Value.ToString()))
                {
                    dataGridView1.HighlightAppendedRow(row);
                }
            }

            MessageBox.Show(
                $"处理完成。\n新增旧 Locres 独有条目：{addedCount}\n完全一致并跳过：{exactMatchCount}\n名称相同但文本不同，暂未处理：{mismatchCount}",
                "完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private int OverwriteDifferentTextValuesFromOldLocres(LocresFile oldLocres)
        {
            var currentEntries = GetCurrentLocresEntries();
            var oldEntries = GetLocresEntries(oldLocres);
            var mismatchedEntries = new List<Tuple<LocresEntrySnapshot, LocresEntrySnapshot>>();

            foreach (var oldEntry in oldEntries)
            {
                if (currentEntries.TryGetValue(oldEntry.Name, out LocresEntrySnapshot currentEntry) &&
                    !string.Equals(currentEntry.TextValue, oldEntry.TextValue, StringComparison.Ordinal))
                {
                    mismatchedEntries.Add(Tuple.Create(currentEntry, oldEntry));
                }
            }

            if (mismatchedEntries.Count == 0)
            {
                MessageBox.Show("没有找到“Name 相同但文本值不同”的条目。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            DialogResult confirm = MessageBox.Show(
                $"检测到 {mismatchedEntries.Count} 条 Name 相同但文本值不同的条目。\n确认使用旧 Locres 覆盖这些条目的文本值吗？",
                "确认覆盖",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return -1;
            }

            foreach (var entryPair in mismatchedEntries)
            {
                var currentEntry = entryPair.Item1;
                var oldEntry = entryPair.Item2;

                dataGridView1.SetValue(currentEntry.Row.Cells["Text value"], oldEntry.TextValue);

                var currentHash = currentEntry.Row.Cells["Hash Table"].Value as HashTable;
                uint valueHash = oldEntry.HashTable != null && oldEntry.HashTable.ValueHash != 0
                    ? oldEntry.HashTable.ValueHash
                    : oldEntry.TextValue.StrCrc32();

                var updatedHash = new HashTable(
                    currentHash?.NameHash ?? oldEntry.HashTable?.NameHash ?? 0,
                    currentHash?.KeyHash ?? oldEntry.HashTable?.KeyHash ?? 0,
                    valueHash);

                dataGridView1.SetValue(currentEntry.Row.Cells["Hash Table"], updatedHash);
            }

            MessageBox.Show(
                $"覆盖完成，共处理 {mismatchedEntries.Count} 条记录。\n新 Locres 中对应条目的文本值和 Value hash 已同步更新。",
                "完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return mismatchedEntries.Count;
        }

        private HashTable BuildRecalculatedHashTable(LocresEntrySnapshot entry, LocresFile locresAsset)
        {
            uint nameHash = string.IsNullOrEmpty(entry.NameSpace) ? 0 : locresAsset.CalcHash(entry.NameSpace);
            uint keyHash = locresAsset.CalcHash(entry.Key);
            uint valueHash = entry.TextValue.StrCrc32();
            return new HashTable(nameHash, keyHash, valueHash);
        }

        private List<Tuple<LocresEntrySnapshot, LocresEntrySnapshot>> GetHashOverwriteCandidates(
            LocresFile oldLocres,
            HashOverwriteMode mode)
        {
            var currentEntries = GetCurrentLocresEntries();
            var oldEntries = GetLocresEntries(oldLocres);
            var candidates = new List<Tuple<LocresEntrySnapshot, LocresEntrySnapshot>>();

            foreach (var oldEntry in oldEntries)
            {
                if (!currentEntries.TryGetValue(oldEntry.Name, out LocresEntrySnapshot currentEntry))
                {
                    continue;
                }

                bool isSameText = string.Equals(currentEntry.TextValue, oldEntry.TextValue, StringComparison.Ordinal);
                bool isEligible =
                    mode == HashOverwriteMode.SameNameForce ||
                    (mode == HashOverwriteMode.MatchingTextOnly && isSameText) ||
                    (mode == HashOverwriteMode.DifferentTextOnly && !isSameText);

                if (!isEligible)
                {
                    continue;
                }

                candidates.Add(Tuple.Create(currentEntry, oldEntry));
            }

            return candidates;
        }

        private string GetHashOverwriteEmptyMessage(HashOverwriteMode mode)
        {
            switch (mode)
            {
                case HashOverwriteMode.MatchingTextOnly:
                    return "没有找到“Name 相同且文本一致”的条目可用于覆盖哈希。";
                case HashOverwriteMode.DifferentTextOnly:
                    return "没有找到“Name 相同且文本不同”的条目可用于覆盖哈希。";
                default:
                    return "没有找到“Name 相同”的条目可用于覆盖哈希。";
            }
        }

        private string GetHashOverwriteConfirmationMessage(HashOverwriteMode mode, int candidateCount)
        {
            switch (mode)
            {
                case HashOverwriteMode.MatchingTextOnly:
                    return $"检测到 {candidateCount} 条“Name 相同且文本一致”的条目。\n确认使用旧 Locres 覆盖这些条目的命名空间哈希、键哈希、文本哈希吗？";
                case HashOverwriteMode.DifferentTextOnly:
                    return $"检测到 {candidateCount} 条“Name 相同且文本不同”的条目。\n确认仅覆盖这些条目的命名空间哈希、键哈希、文本哈希吗？\n\n注意：当前新文件的文本不会被修改，覆盖后文本哈希可能与当前文本内容不一致。";
                default:
                    return $"检测到 {candidateCount} 条“Name 相同”的条目。\n确认强制使用旧 Locres 覆盖这些条目的命名空间哈希、键哈希、文本哈希吗？\n\n注意：即使当前文本不同，也会写入旧文件的文本哈希。";
            }
        }

        private string GetHashOverwriteConfirmationTitle(HashOverwriteMode mode)
        {
            switch (mode)
            {
                case HashOverwriteMode.MatchingTextOnly:
                    return "确认覆盖哈希（文本一致）";
                case HashOverwriteMode.DifferentTextOnly:
                    return "确认覆盖哈希（文本不同）";
                default:
                    return "确认覆盖哈希（同名强制）";
            }
        }

        private string GetHashOverwriteResultMessage(HashOverwriteMode mode, int candidateCount, int changedCount)
        {
            switch (mode)
            {
                case HashOverwriteMode.MatchingTextOnly:
                    return $"处理完成。\n符合条件条目：{candidateCount}\n实际覆盖哈希：{changedCount}";
                case HashOverwriteMode.DifferentTextOnly:
                    return $"处理完成。\n文本不同条目：{candidateCount}\n实际覆盖哈希：{changedCount}";
                default:
                    return $"处理完成。\n同名条目：{candidateCount}\n实际覆盖哈希：{changedCount}";
            }
        }

        private int OverwriteHashesFromOldLocres(LocresFile oldLocres, HashOverwriteMode mode)
        {
            var candidates = GetHashOverwriteCandidates(oldLocres, mode);

            if (candidates.Count == 0)
            {
                MessageBox.Show(
                    GetHashOverwriteEmptyMessage(mode),
                    "完成",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return 0;
            }

            DialogResult confirm = MessageBox.Show(
                GetHashOverwriteConfirmationMessage(mode, candidates.Count),
                GetHashOverwriteConfirmationTitle(mode),
                MessageBoxButtons.YesNo,
                mode == HashOverwriteMode.SameNameForce || mode == HashOverwriteMode.DifferentTextOnly
                    ? MessageBoxIcon.Warning
                    : MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return -1;
            }

            int changedCount = 0;

            foreach (var entryPair in candidates)
            {
                var currentEntry = entryPair.Item1;
                var oldEntry = entryPair.Item2;

                var currentHash = currentEntry.Row.Cells["Hash Table"].Value as HashTable;
                var oldHash = oldEntry.HashTable ?? new HashTable();

                bool hashChanged =
                    currentHash == null ||
                    currentHash.NameHash != oldHash.NameHash ||
                    currentHash.KeyHash != oldHash.KeyHash ||
                    currentHash.ValueHash != oldHash.ValueHash;

                if (!hashChanged)
                {
                    continue;
                }

                dataGridView1.SetValue(
                    currentEntry.Row.Cells["Hash Table"],
                    new HashTable(oldHash.NameHash, oldHash.KeyHash, oldHash.ValueHash));
                dataGridView1.HighlightHashOverwrittenRow(currentEntry.Row);
                changedCount++;
            }

            MessageBox.Show(
                GetHashOverwriteResultMessage(mode, candidates.Count, changedCount),
                "完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return changedCount;
        }

        private int RecalculateAllLocresHashes()
        {
            if (!(Asset is LocresFile locresAsset))
            {
                return 0;
            }

            var currentEntries = GetCurrentLocresEntries();
            int changedCount = 0;

            foreach (var entry in currentEntries.Values)
            {
                var recalculatedHash = BuildRecalculatedHashTable(entry, locresAsset);
                var currentHash = entry.Row.Cells["Hash Table"].Value as HashTable;

                bool hashChanged =
                    currentHash == null ||
                    currentHash.NameHash != recalculatedHash.NameHash ||
                    currentHash.KeyHash != recalculatedHash.KeyHash ||
                    currentHash.ValueHash != recalculatedHash.ValueHash;

                if (!hashChanged)
                {
                    continue;
                }

                dataGridView1.SetValue(entry.Row.Cells["Hash Table"], recalculatedHash);
                dataGridView1.HighlightHashRecalculatedRow(entry.Row);
                changedCount++;
            }

            MessageBox.Show(
                $"重算完成。\n已更新哈希条目：{changedCount}\n命名空间为空的条目其 NameSpace hash 已按 0 处理。",
                "完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return changedCount;
        }

        private bool EnsureLocresTranslationReady()
        {
            if (!EnsureLocresAssetLoaded())
            {
                return false;
            }

            if (dataGridView1.Columns[TranslationPreviewColumnName] == null)
            {
                MessageBox.Show("当前表格未启用翻译预览列，请重新打开 Locres 文件后再试。", "无法执行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private List<DataGridViewRow> GetSelectedUniqueRows()
        {
            return dataGridView1.SelectedCells
                .Cast<DataGridViewCell>()
                .Where(cell => cell?.OwningRow != null && !cell.OwningRow.IsNewRow)
                .Select(cell => cell.OwningRow)
                .Distinct()
                .OrderBy(row => row.Index)
                .ToList();
        }

        private List<DataGridViewRow> GetTranslationTargetRows(
            DoubaoTranslationScope scope,
            int rowCountLimit,
            bool skipPreviewedRows,
            out int emptyTextSkippedCount,
            out int previewSkippedCount)
        {
            emptyTextSkippedCount = 0;
            previewSkippedCount = 0;

            IEnumerable<DataGridViewRow> sourceRows =
                scope == DoubaoTranslationScope.SelectedRows
                    ? GetSelectedUniqueRows()
                    : dataGridView1.Rows.Cast<DataGridViewRow>().Where(row => !row.IsNewRow);

            var targetRows = new List<DataGridViewRow>();

            foreach (DataGridViewRow row in sourceRows)
            {
                string textValue = row.Cells["Text value"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(textValue))
                {
                    emptyTextSkippedCount++;
                    continue;
                }

                if (skipPreviewedRows && !string.IsNullOrWhiteSpace(row.Cells[TranslationPreviewColumnName].Value?.ToString()))
                {
                    previewSkippedCount++;
                    continue;
                }

                targetRows.Add(row);
                if (scope == DoubaoTranslationScope.FirstNRows && targetRows.Count >= rowCountLimit)
                {
                    break;
                }
            }

            return targetRows;
        }

        private string BuildTranslationScopeDescription(DoubaoTranslationScope scope)
        {
            switch (scope)
            {
                case DoubaoTranslationScope.SelectedRows:
                    return "当前选中行";
                case DoubaoTranslationScope.FirstNRows:
                    return "前 N 行";
                default:
                    return "全部可用行";
            }
        }

        private List<DataGridViewRow> GetRowsWithPreviewToApply(bool selectedOnly)
        {
            IEnumerable<DataGridViewRow> sourceRows = selectedOnly
                ? GetSelectedUniqueRows()
                : dataGridView1.Rows.Cast<DataGridViewRow>().Where(row => !row.IsNewRow);

            return sourceRows
                .Where(row => !string.IsNullOrWhiteSpace(row.Cells[TranslationPreviewColumnName].Value?.ToString()))
                .ToList();
        }

        private int ApplyTranslationPreviewToTextValue(bool selectedOnly)
        {
            if (!EnsureLocresTranslationReady())
            {
                return -1;
            }

            List<DataGridViewRow> targetRows = GetRowsWithPreviewToApply(selectedOnly);
            if (targetRows.Count == 0)
            {
                MessageBox.Show(
                    selectedOnly
                        ? "当前选中行里没有可应用的机器翻译预览。"
                        : "当前没有可应用的机器翻译预览。",
                    "无法执行",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return 0;
            }

            DialogResult confirm = MessageBox.Show(
                (selectedOnly ? "已选中行" : "全部行") + "中检测到 " + targetRows.Count + " 条可应用的机器翻译预览。\n确认将这些预览写入“Text value”并同步更新文本哈希吗？\n\n说明：预览列内容会保留，不会被清空。",
                "确认应用预览",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return -1;
            }

            int changedCount = 0;
            foreach (DataGridViewRow row in targetRows)
            {
                string previewText = row.Cells[TranslationPreviewColumnName].Value?.ToString() ?? "";
                string currentText = row.Cells["Text value"].Value?.ToString() ?? "";

                if (!string.Equals(currentText, previewText, StringComparison.Ordinal))
                {
                    dataGridView1.SetValue(row.Cells["Text value"], previewText);
                    changedCount++;
                }

                var currentHash = row.Cells["Hash Table"].Value as HashTable;
                var updatedHash = new HashTable(
                    currentHash?.NameHash ?? 0,
                    currentHash?.KeyHash ?? 0,
                    previewText.StrCrc32());

                dataGridView1.SetValue(row.Cells["Hash Table"], updatedHash);
            }

            MessageBox.Show(
                "应用完成。\n已写入文本值：" + changedCount + "\n已同步更新文本哈希：" + targetRows.Count,
                "完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return changedCount;
        }

        private async Task TranslateRowsWithProviderAsync(
            List<DataGridViewRow> targetRows,
            TranslationProviderType providerType,
            TranslationProviderSettings providerSettings,
            string sourceLanguageCode,
            string targetLanguageCode,
            bool preserveFormatting)
        {
            var service = TranslationProviderFactory.Create(providerSettings, providerType);
            int successCount = 0;
            int failedCount = 0;
            var errorMessages = new List<string>();

            try
            {
                for (int i = 0; i < targetRows.Count; i++)
                {
                    DataGridViewRow row = targetRows[i];
                    string entryName = row.Cells["Name"].Value?.ToString() ?? ("第 " + (row.Index + 1) + " 行");
                    string textValue = row.Cells["Text value"].Value?.ToString() ?? "";

                    StatusMessage("正在调用" + TranslationProviderHelper.GetDisplayName(providerType) + "翻译...", "正在翻译 " + (i + 1) + "/" + targetRows.Count + " 行：" + entryName);

                    try
                    {
                        string translatedText = await service.TranslateAsync(textValue, sourceLanguageCode, targetLanguageCode, preserveFormatting);
                        dataGridView1.SetValue(row.Cells[TranslationPreviewColumnName], translatedText);
                        dataGridView1.HighlightTranslationPreviewCell(row.Cells[TranslationPreviewColumnName]);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        if (errorMessages.Count < 5)
                        {
                            errorMessages.Add("第 " + (row.Index + 1) + " 行：" + ex.Message);
                        }
                    }
                }
            }
            finally
            {
                CloseFromState();
            }

            string message =
                "翻译完成。\n" +
                "已生成预览：" + successCount + "\n" +
                "失败：" + failedCount + "\n" +
                "说明：本次仅写入右侧“" + TranslationPreviewColumnName + "”列，不会自动保存或覆盖原文本。";

            if (errorMessages.Count > 0)
            {
                message += "\n\n错误示例：\n" + string.Join("\n", errorMessages);
            }

            MessageBox.Show(
                message,
                failedCount > 0 ? "部分完成" : "完成",
                MessageBoxButtons.OK,
                failedCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private List<int> ParseRequestedRowIndexes(string input, int totalRowCount)
        {
            var rowIndexes = new SortedSet<int>();
            string[] parts = input.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                string[] rangeItems = part.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (rangeItems.Length == 1)
                {
                    int rowNumber = ParseRowNumber(rangeItems[0], totalRowCount);
                    rowIndexes.Add(rowNumber - 1);
                    continue;
                }

                if (rangeItems.Length != 2)
                {
                    throw new FormatException("无法解析行号片段：" + part);
                }

                int start = ParseRowNumber(rangeItems[0], totalRowCount);
                int end = ParseRowNumber(rangeItems[1], totalRowCount);
                if (end < start)
                {
                    int temp = start;
                    start = end;
                    end = temp;
                }

                for (int rowNumber = start; rowNumber <= end; rowNumber++)
                {
                    rowIndexes.Add(rowNumber - 1);
                }
            }

            return rowIndexes.ToList();
        }

        private int ParseRowNumber(string text, int totalRowCount)
        {
            if (!int.TryParse(text.Trim(), out int rowNumber))
            {
                throw new FormatException("无效的行号：" + text);
            }

            if (rowNumber < 1 || rowNumber > totalRowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(text), "行号超出范围：" + rowNumber + "。当前可选范围为 1-" + totalRowCount + "。");
            }

            return rowNumber;
        }

        private void SelectRowsByInput()
        {
            if (!EnsureLocresTranslationReady())
            {
                return;
            }

            int totalRowCount = dataGridView1.Rows.Cast<DataGridViewRow>().Count(row => !row.IsNewRow);
            if (totalRowCount == 0)
            {
                MessageBox.Show("当前没有可选中的行。", "无法执行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string input = Interaction.InputBox(
                "请输入要选中的行号，支持格式：1-10,15,20-25\n行号按当前表格显示顺序，从 1 开始。",
                "按行号批量选中",
                "");

            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            try
            {
                List<int> rowIndexes = ParseRequestedRowIndexes(input, totalRowCount);
                if (rowIndexes.Count == 0)
                {
                    MessageBox.Show("没有解析出任何有效行号。", "无法执行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataGridViewRow firstRow = dataGridView1.Rows[rowIndexes[0]];
                dataGridView1.ClearSelection();
                dataGridView1.CurrentCell = firstRow.Cells["Text value"];
                foreach (int rowIndex in rowIndexes)
                {
                    DataGridViewRow row = dataGridView1.Rows[rowIndex];
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.OwningColumn.Visible)
                        {
                            cell.Selected = true;
                        }
                    }
                }

                dataGridView1.FirstDisplayedScrollingRowIndex = firstRow.Index;
                dataGridView1.Focus();

                MessageBox.Show("已批量选中 " + rowIndexes.Count + " 行。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("批量选中行号时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetControls()
        {
            FilePath = "";
            StateLabel.Text = "";
            DataCount.Text = "";
            Text = ToolName;
            SortApply = false;
            locresOprationsToolStripMenuItem.Visible = false;
        }

        private void ControlsMode(bool Enabled)
        {
            saveToolStripMenuItem.Enabled = Enabled;
            exportAllTextToolStripMenuItem.Enabled = Enabled;
            importAllTextToolStripMenuItem.Enabled = Enabled;
            undoToolStripMenuItem.Enabled = Enabled;
            redoToolStripMenuItem.Enabled = Enabled;
            filterToolStripMenuItem.Enabled = Enabled;
            noNamesToolStripMenuItem.Enabled = Enabled;
            withNamesToolStripMenuItem.Enabled = Enabled;
            clearFilterToolStripMenuItem.Enabled = Enabled;
            csvFileToolStripMenuItem.Enabled = Enabled;
        }
        enum ExportType
        {
            NoNames = 0,
            WithNames
        }

        private void ExportAll(ExportType exportType)
        {

            if (this.SortApply && !(Asset is LocresFile)) SortDataGrid(2, true);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "文本文件|*.txt";
            sfd.Title = "导出全部文本";
            sfd.FileName = Path.GetFileName(FilePath) + ".txt";


            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {

                    using (var stream = new StreamWriter(sfd.FileName))
                    {
                        if (exportType == ExportType.WithNames)
                        {
                            stream.WriteLine(@"[~NAMES-INCLUDED~]//请勿编辑或删除此行。");
                        }

                        for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            if (exportType == ExportType.WithNames)
                            {
                                stream.WriteLine(dataGridView1.Rows[i].Cells["Name"].Value.ToString() + "=" + dataGridView1.Rows[i].Cells["Text value"].Value.ToString());
                                continue;
                            }
                            stream.WriteLine(dataGridView1.Rows[i].Cells["Text value"].Value.ToString());
                        }

                    }
                    if (dataGridView1.IsFilterApplied)
                    {
                        MessageBox.Show("导出成功。\n如果你当前启用了筛选，导入前请应用同样的筛选条件。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("导出成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch
                {
                    MessageBox.Show("无法写入导出文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
        }

        private void importAllTextToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "文本文件|*.txt;*.csv";
            ofd.Title = "导入全部文本";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (this.SortApply && !(Asset is LocresFile)) SortDataGrid(2, true);

                if (ofd.FileName.EndsWith(".csv", StringComparison.InvariantCulture))
                {
                    try
                    {
                        CSVFile.Instance.Load(this.dataGridView1, ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ToolName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    MessageBox.Show("导入成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }



                string[] DataGridStrings;
                try
                {
                    DataGridStrings = System.IO.File.ReadAllLines(ofd.FileName);
                }
                catch
                {
                    MessageBox.Show("无法读取文件，或者该文件正被其他进程占用。", "文件异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (DataGridStrings.Length < dataGridView1.Rows.Count)
                {
                    MessageBox.Show("该文件中的文本数量不足，无法重新导入。", "超出范围", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (DataGridStrings[0].StartsWith("[~NAMES-INCLUDED~]", StringComparison.OrdinalIgnoreCase))
                {
                    DataGridStrings = DataGridStrings.Skip(1).ToArray();
                    for (int n = 0; n < DataGridStrings.Length; n++)
                    {
                        try
                        {
                            if (DataGridStrings[n].Contains("="))
                                DataGridStrings[n] = DataGridStrings[n].Split(new char[] { '=' }, 2)[1];
                        }
                        catch
                        {
                            MessageBox.Show($"第 {n + 1} 行的字符串格式损坏。", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return;
                        }

                    }

                }

                for (int n = 0; n < dataGridView1.Rows.Count; n++)
                {
                    dataGridView1.SetValue(dataGridView1.Rows[n].Cells["Text value"], DataGridStrings[n]);
                }
                MessageBox.Show("导入成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);


            }



        }

        private async void SaveFile(object sender, EventArgs e)
        {

            SaveFileDialog sfd = new SaveFileDialog();
            if (FilePath.ToLower().EndsWith(".locres"))
            {
                sfd.Filter = "Locres 文件|*.locres";
            }
            else if (FilePath.ToLower().EndsWith(".uasset"))
            {
                sfd.Filter = "Uasset 文件|*.uasset";
            }
            else if (FilePath.ToLower().EndsWith(".umap"))
            {
                sfd.Filter = "Umap 文件|*.umap";
            }

            sfd.Title = "保存本地化文件";
            sfd.FileName = Path.GetFileNameWithoutExtension(FilePath) + "_NEW";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StatusMessage("正在保存文件...", "正在保存文件，请稍候。");
                    Asset.LoadFromDataGridView(dataGridView1);
                    await Task.Run(() => Asset.SaveFile(sfd.FileName));
                    MessageBox.Show("保存成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                CloseFromState();
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Paste();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Dialog Font select
            FontDialog fd = new FontDialog();
            fd.Font = dataGridView1.Font;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                dataGridView1.Font = fd.Font;
                dataGridView1.AutoResizeRows();
            }
        }

        private void rightToLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.RightToLeft = dataGridView1.RightToLeft == RightToLeft.Yes ? RightToLeft.No : RightToLeft.Yes;
        }


        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {

            dataGridView1.Redo();
        }

        private void commandLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Program.commandlines, "命令行", MessageBoxButtons.OK);
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new FrmAbout(this).ShowDialog();
        }

        private void clearFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.ClearFilter();
        }

        private void UpdateCounter()
        {
            DataCount.Text = "文本数量：" + dataGridView1.Rows.Count;
        }


        private async void FrmMain_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.CheckForUpdates)
            {
                await CheckForUpdatesAsync();
            }


            if (!UE4localizationsTool.Properties.Settings.Default.GoodByeMessage)
            {
                MessageBox.Show("感谢使用本工具。这将是最终版本，如有不便敬请谅解。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UE4localizationsTool.Properties.Settings.Default.GoodByeMessage = true;
                UE4localizationsTool.Properties.Settings.Default.Save();
            }
        }

        private void noNamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportAll(ExportType.NoNames);
        }

        private void withNamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportAll(ExportType.WithNames);
        }

        private void valueToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        void SortDataGrid(int Cel, bool Ascending)
        {
            this.SortApply = true;
            if (Ascending)
            {
                dataGridView1.Sort(dataGridView1.Columns[Cel], System.ComponentModel.ListSortDirection.Ascending);
                return;
            }
            dataGridView1.Sort(dataGridView1.Columns[Cel], System.ComponentModel.ListSortDirection.Descending);
        }

        private void ascendingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SortDataGrid(0, true);
        }

        private void descendingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SortDataGrid(0, false);
        }

        private void ascendingToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SortDataGrid(1, true);
        }

        private void descendingToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SortDataGrid(1, false);
        }

        private void dataGridView1_Sorted(object sender, EventArgs e)
        {
            this.SortApply = true;
        }

        private void FrmMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void FrmMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (array[0].Length >= 1 && (array[0].EndsWith(".uasset") || array[0].EndsWith(".umap") || array[0].EndsWith(".locres")))
            {
                LoadFile(array[0]);
            }
        }

        private void donateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://qm.qq.com/q/8h4XCJyv16");
        }

        private void Method2_CheckedChanged(object sender, EventArgs e)
        {

            if (Method2.Checked)
            {
                pictureBox1.Visible = true;
                fileToolStripMenuItem.Margin = new Padding(5, 0, 0, 0);
            }
            else
            {
                pictureBox1.Visible = false;
                fileToolStripMenuItem.Margin = new Padding(0, 0, 0, 0);
            }


        }

        private void darkModeToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            bool IsDark = Properties.Settings.Default.DarkMode;
            Properties.Settings.Default.DarkMode = darkModeToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();

            if (IsDark != darkModeToolStripMenuItem.Checked)
                Application.Restart();
        }

        private void Checkforupdates_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.CheckForUpdates = Checkforupdates.Checked;
            Properties.Settings.Default.Save();
        }

        private void csvFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV 文件|*.csv";
            sfd.Title = "导出全部文本";
            sfd.FileName = Path.GetFileName(FilePath) + ".csv";


            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    CSVFile.Instance.Save(this.dataGridView1, sfd.FileName);

                    if (dataGridView1.IsFilterApplied)
                    {
                        MessageBox.Show("导出成功。\n如果你当前启用了筛选，导入前请应用同样的筛选条件。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("导出成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch
                {
                    MessageBox.Show("无法写入导出文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
        }

        private void find_Click(object sender, EventArgs e)
        {
            searchBox.Show();

        }

        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Filter();
        }

        private void dataGridView1_FilterApplied(object sender, EventArgs e)
        {
            filterToolStripMenuItem.Visible = false;
            clearFilterToolStripMenuItem.Visible = true;
        }

        private void dataGridView1_FilterCleared(object sender, EventArgs e)
        {
            filterToolStripMenuItem.Visible = true;
            clearFilterToolStripMenuItem.Visible = false;
        }

        private void removeSelectedRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count == 0)
            {
                MessageBox.Show("没有选中要删除的行。", "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            DialogResult result = MessageBox.Show("确定要删除所选行吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                dataGridView1.BeginEdit(false);
                foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
                {
                    if (cell.RowIndex >= 0 && cell.RowIndex < dataGridView1.Rows.Count)
                    {
                        dataGridView1.Rows.Remove(cell.OwningRow);
                    }
                }
                dataGridView1.EndEdit();
            }
        }

        private void editSelectedRowToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (dataGridView1.SelectedCells.Count > 1 || dataGridView1.SelectedCells.Count == 0)
            {

                MessageBox.Show("请选择一个单元格进行编辑。", "编辑失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;

            }

            var EntryEditor = new FrmLocresEntryEditor(dataGridView1, (LocresFile)Asset);
            if (EntryEditor.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    EntryEditor.EditRow(dataGridView1);
                    MessageBox.Show("行编辑成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("编辑行时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void addNewRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var EntryEditor = new FrmLocresEntryEditor(this, (LocresFile)Asset);

            if (EntryEditor.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    dataGridView1.BeginEdit(false);
                    EntryEditor.AddRow(dataGridView1);
                    dataGridView1.EndEdit();
                    MessageBox.Show("新增行成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("新增行时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }

        private void mergeLocresFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Locres 文件|*.locres";
            ofd.Title = "选择本地化文件";
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                StatusMessage("正在合并 Locres 文件...", "正在合并 Locres 文件，请稍候。");
                var dataTable = new System.Data.DataTable();

                if (dataGridView1.DataSource is System.Data.DataTable sourceDataTable)
                {
                    foreach (DataColumn col in sourceDataTable.Columns)
                    {
                        dataTable.Columns.Add(col.ColumnName, col.DataType);
                    }
                }


                try
                {
                    foreach (string fileName in ofd.FileNames)
                    {
                        foreach (var names in new LocresFile(fileName))
                        {
                            foreach (var table in names)
                            {
                                string name = string.IsNullOrEmpty(names.Name) ? table.key : names.Name + "::" + table.key;
                                string textValue = table.Value;
                                AddLocresGridRow(dataTable, name, textValue, new HashTable(names.NameHash, table.keyHash, table.ValueHash));
                            }
                        }
                    }

                    ((System.Data.DataTable)dataGridView1.DataSource).Merge(dataTable);


                    MessageBox.Show("Locres 文件合并成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("合并 Locres 文件时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                CloseFromState();
            }
        }

        private async void appendMissingLocresEntriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureLocresAssetLoaded())
            {
                return;
            }

            try
            {
                var oldLocres = await PromptAndLoadOldLocresAsync("选择旧 Locres 文件", "正在比对缺失条目...");
                if (oldLocres == null)
                {
                    return;
                }

                AppendMissingEntriesFromOldLocres(oldLocres);
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理旧 Locres 缺失条目时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CloseFromState();
            }
        }

        private async void overwriteLocresDifferentValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureLocresAssetLoaded())
            {
                return;
            }

            try
            {
                var oldLocres = await PromptAndLoadOldLocresAsync("选择旧 Locres 文件", "正在检查可覆盖条目...");
                if (oldLocres == null)
                {
                    return;
                }

                OverwriteDifferentTextValuesFromOldLocres(oldLocres);
            }
            catch (Exception ex)
            {
                MessageBox.Show("覆盖 Locres 差异文本值时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CloseFromState();
            }
        }

        private async void overwriteMatchingHashesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureLocresAssetLoaded())
            {
                return;
            }

            try
            {
                var oldLocres = await PromptAndLoadOldLocresAsync("选择旧 Locres 文件", "正在按文本一致条件覆盖哈希...");
                if (oldLocres == null)
                {
                    return;
                }

                OverwriteHashesFromOldLocres(oldLocres, HashOverwriteMode.MatchingTextOnly);
            }
            catch (Exception ex)
            {
                MessageBox.Show("覆盖 Locres 哈希值时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CloseFromState();
            }
        }

        private async void overwriteDifferentHashesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureLocresAssetLoaded())
            {
                return;
            }

            try
            {
                var oldLocres = await PromptAndLoadOldLocresAsync("选择旧 Locres 文件", "正在按文本不同条件覆盖哈希...");
                if (oldLocres == null)
                {
                    return;
                }

                OverwriteHashesFromOldLocres(oldLocres, HashOverwriteMode.DifferentTextOnly);
            }
            catch (Exception ex)
            {
                MessageBox.Show("覆盖 Locres 哈希值时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CloseFromState();
            }
        }

        private async void forceOverwriteHashesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureLocresAssetLoaded())
            {
                return;
            }

            try
            {
                var oldLocres = await PromptAndLoadOldLocresAsync("选择旧 Locres 文件", "正在强制覆盖同名条目哈希...");
                if (oldLocres == null)
                {
                    return;
                }

                OverwriteHashesFromOldLocres(oldLocres, HashOverwriteMode.SameNameForce);
            }
            catch (Exception ex)
            {
                MessageBox.Show("强制覆盖 Locres 哈希值时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CloseFromState();
            }
        }

        private void recalculateAllHashesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureLocresAssetLoaded())
            {
                return;
            }

            try
            {
                RecalculateAllLocresHashes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("重算哈希值时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StatusMessage(string title, string message)
        {
            StatusTitle.Text = title;
            StatusText.Text = message;
            StatusBlock.Visible = true;
        }

        private void CloseFromState()
        {
            StatusBlock.Visible = false;
        }

        private void mergeUassetFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "所有本地化文件|*.uasset;*.umap|Uasset 文件|*.uasset|Umap 文件|*.umap";
            ofd.Title = "打开本地化文件";
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                StatusMessage("正在合并 Uasset 文件...", "正在合并 Uasset 文件，请稍候。");

                var dataTable = new System.Data.DataTable();

                if (dataGridView1.DataSource is System.Data.DataTable sourceDataTable)
                {
                    foreach (DataColumn col in sourceDataTable.Columns)
                    {
                        dataTable.Columns.Add(col.ColumnName, col.DataType);
                    }
                }


                try
                {
                    foreach (string fileName in ofd.FileNames)
                    {
                        foreach (var Strings in new Uexp(Uexp.GetUasset(fileName), true).StringNodes)
                        {
                            var locresasset = Asset as LocresFile;
                            var HashTable = new HashTable(locresasset.CalcHash(Strings.NameSpace), locresasset.CalcHash(Strings.Key), Strings.Value.StrCrc32());

                            AddLocresGridRow(dataTable, Strings.GetName(), Strings.Value, HashTable);
                        }
                    }

                      ((System.Data.DataTable)dataGridView1.DataSource).Merge(dataTable);


                    MessageBox.Show("Uasset 文件合并成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("合并 Uasset 文件时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                CloseFromState();

            }
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            searchBox.ShowReplacePanel();
        }

        private async void doubaoTranslateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureLocresTranslationReady())
            {
                return;
            }

            using (var dialog = new FrmDoubaoTranslate(GetSelectedUniqueRows().Count))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                int emptyTextSkippedCount;
                int previewSkippedCount;
                List<DataGridViewRow> targetRows = GetTranslationTargetRows(
                    dialog.TranslationScope,
                    dialog.RowCountLimit,
                    dialog.SkipPreviewedRows,
                    out emptyTextSkippedCount,
                    out previewSkippedCount);

                if (targetRows.Count == 0)
                {
                    MessageBox.Show(
                        "没有找到可翻译的行。\n" +
                        "范围：" + BuildTranslationScopeDescription(dialog.TranslationScope) + "\n" +
                        "空文本跳过：" + emptyTextSkippedCount + "\n" +
                        "已有预览跳过：" + previewSkippedCount,
                        "无法执行",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    await TranslateRowsWithProviderAsync(
                        targetRows,
                        dialog.SelectedProvider,
                        dialog.CurrentSettings,
                        dialog.SourceLanguageCode,
                        dialog.TargetLanguageCode,
                        dialog.PreserveFormatting);
                }
                catch (Exception ex)
                {
                    CloseFromState();
                    MessageBox.Show("执行豆包翻译时发生错误：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void batchSelectLocresRowsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectRowsByInput();
        }

        private void applyTranslationPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ApplyTranslationPreviewToTextValue(true);
        }

        private void applyAllTranslationPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ApplyTranslationPreviewToTextValue(false);
        }

        private void translationSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FrmTranslationSettings())
            {
                dialog.ShowDialog(this);
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            const string updateUrl = "https://raw.githubusercontent.com/Brilliant-Captain/UE4-5LocalizationsTool-Extended-CN/main/UE4localizationsTool/UpdateInfo.txt";

            try
            {
                using (WebClient client = new WebClient())
                {
                    var downloadTask = client.DownloadStringTaskAsync(new Uri(updateUrl));
                    var completedTask = await Task.WhenAny(downloadTask, Task.Delay(5000));
                    if (completedTask != downloadTask)
                    {
                        return;
                    }

                    Version toolVer = new Version(0, 0, 0, 0);
                    string toolSite = "";
                    string updateScript = await downloadTask;

                    if (!updateScript.StartsWith("UpdateFile", false, CultureInfo.InvariantCulture))
                    {
                        return;
                    }

                    var lines = Regex.Split(updateScript, "\r\n|\r|\n");
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Tool_UpdateVer", false, CultureInfo.InvariantCulture))
                        {
                            toolVer = ParseVersionString(line.Split(new char[] { '=' }, 2)[1].Trim());
                        }

                        if (line.StartsWith("Tool_UpdateSite", false, CultureInfo.InvariantCulture))
                        {
                            toolSite = line.Split(new char[] { '=' }, 2)[1].Trim();
                        }
                    }

                    if (toolVer > ParseVersionString(Application.ProductVersion))
                    {
                        DialogResult message = MessageBox.Show("检测到可用更新。\n是否现在下载？", "发现更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (message == DialogResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo { FileName = toolSite, UseShellExecute = true });
                            Application.Exit();
                        }
                    }
                }
            }
            catch
            {
                // Ignore update check failures so startup is never blocked.
            }
        }
    }
}
