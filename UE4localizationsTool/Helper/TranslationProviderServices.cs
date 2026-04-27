using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Csv;

namespace UE4localizationsTool.Helper
{
    public enum TranslationProviderType
    {
        Doubao,
        Google,
        Baidu,
        Tencent
    }

    public enum TerminologyPartOfSpeech
    {
        Noun,
        Verb,
        Adjective,
        Adverb
    }

    [DataContract]
    public sealed class TranslationTerminologyEntry
    {
        public TranslationTerminologyEntry()
        {
            PartOfSpeech = TerminologyPartOfSpeech.Noun;
            SourceText = "";
            TargetText = "";
            Variants = new List<string>();
            Notes = "";
        }

        [DataMember]
        public TerminologyPartOfSpeech PartOfSpeech { get; set; }

        [DataMember]
        public string SourceText { get; set; }

        [DataMember]
        public string TargetText { get; set; }

        [DataMember]
        public List<string> Variants { get; set; }

        [DataMember]
        public string Notes { get; set; }

        [DataMember]
        public bool CaseSensitive { get; set; }

        public TranslationTerminologyEntry Normalize()
        {
            SourceText = SourceText ?? "";
            TargetText = TargetText ?? "";
            Notes = Notes ?? "";
            Variants = Variants ?? new List<string>();
            return this;
        }
    }

    [DataContract]
    public sealed class TranslationFormattingRules
    {
        public TranslationFormattingRules()
        {
            PreserveEscapeSequences = true;
            PreservePlaceholders = true;
            PreserveAngleBracketTags = true;
            PreserveSquareBracketTags = true;
            PreserveLeadingAndTrailingWhitespace = true;
            CustomProtectedPatterns = new List<string>();
        }

        [DataMember]
        public bool PreserveEscapeSequences { get; set; }

        [DataMember]
        public bool PreservePlaceholders { get; set; }

        [DataMember]
        public bool PreserveAngleBracketTags { get; set; }

        [DataMember]
        public bool PreserveSquareBracketTags { get; set; }

        [DataMember]
        public bool PreserveLeadingAndTrailingWhitespace { get; set; }

        [DataMember]
        public List<string> CustomProtectedPatterns { get; set; }

        public TranslationFormattingRules Normalize()
        {
            if (CustomProtectedPatterns == null)
            {
                CustomProtectedPatterns = new List<string>();
            }

            return this;
        }
    }

    [DataContract]
    public sealed class TranslationProviderSettings
    {
        [DataMember]
        public TranslationProviderType SelectedProvider { get; set; } = TranslationProviderType.Doubao;

        [DataMember]
        public string DoubaoApiKey { get; set; } = "";

        [DataMember]
        public string GoogleApiKey { get; set; } = "";

        [DataMember]
        public string BaiduAppId { get; set; } = "";

        [DataMember]
        public string BaiduSecretKey { get; set; } = "";

        [DataMember]
        public string TencentSecretId { get; set; } = "";

        [DataMember]
        public string TencentSecretKey { get; set; } = "";

        [DataMember]
        public string TencentRegion { get; set; } = "ap-beijing";

        [DataMember]
        public TranslationFormattingRules FormattingRules { get; set; } = new TranslationFormattingRules();

        [DataMember]
        public List<TranslationTerminologyEntry> TerminologyEntries { get; set; } = new List<TranslationTerminologyEntry>();

        public TranslationProviderSettings Normalize()
        {
            if (string.IsNullOrWhiteSpace(TencentRegion))
            {
                TencentRegion = "ap-beijing";
            }

            if (FormattingRules == null)
            {
                FormattingRules = new TranslationFormattingRules();
            }
            else
            {
                FormattingRules.Normalize();
            }

            if (TerminologyEntries == null)
            {
                TerminologyEntries = new List<TranslationTerminologyEntry>();
            }
            else
            {
                for (int i = 0; i < TerminologyEntries.Count; i++)
                {
                    TerminologyEntries[i] = (TerminologyEntries[i] ?? new TranslationTerminologyEntry()).Normalize();
                }
            }

            return this;
        }
    }

    [DataContract]
    public sealed class TranslationApiSettings
    {
        [DataMember]
        public TranslationProviderType SelectedProvider { get; set; } = TranslationProviderType.Doubao;

        [DataMember]
        public string DoubaoApiKey { get; set; } = "";

        [DataMember]
        public string GoogleApiKey { get; set; } = "";

        [DataMember]
        public string BaiduAppId { get; set; } = "";

        [DataMember]
        public string BaiduSecretKey { get; set; } = "";

        [DataMember]
        public string TencentSecretId { get; set; } = "";

        [DataMember]
        public string TencentSecretKey { get; set; } = "";

        [DataMember]
        public string TencentRegion { get; set; } = "ap-beijing";

        public TranslationApiSettings Normalize()
        {
            DoubaoApiKey = DoubaoApiKey ?? "";
            GoogleApiKey = GoogleApiKey ?? "";
            BaiduAppId = BaiduAppId ?? "";
            BaiduSecretKey = BaiduSecretKey ?? "";
            TencentSecretId = TencentSecretId ?? "";
            TencentSecretKey = TencentSecretKey ?? "";

            if (string.IsNullOrWhiteSpace(TencentRegion))
            {
                TencentRegion = "ap-beijing";
            }

            return this;
        }
    }

    public interface ITranslationProvider
    {
        TranslationProviderType ProviderType { get; }

        Task<string> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, bool preserveFormatting = true);
    }

    public static class TranslationProviderHelper
    {
        public static string GetDisplayName(TranslationProviderType provider)
        {
            switch (provider)
            {
                case TranslationProviderType.Google:
                    return "谷歌";
                case TranslationProviderType.Baidu:
                    return "百度";
                case TranslationProviderType.Tencent:
                    return "腾讯";
                default:
                    return "豆包";
            }
        }

        public static string GetPartOfSpeechDisplayName(TerminologyPartOfSpeech partOfSpeech)
        {
            switch (partOfSpeech)
            {
                case TerminologyPartOfSpeech.Verb:
                    return "动词";
                case TerminologyPartOfSpeech.Adjective:
                    return "形容词";
                case TerminologyPartOfSpeech.Adverb:
                    return "副词";
                default:
                    return "名词";
            }
        }
    }

    public static class TranslationSettingsStore
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UE4本地化工具");

        private static readonly string LegacySettingsPath = Path.Combine(SettingsDirectory, "translation-settings.json");
        private static readonly string ProviderSettingsPath = Path.Combine(SettingsDirectory, "translation-provider-settings.json");
        private static readonly string RuleSettingsPath = Path.Combine(SettingsDirectory, "translation-rule-settings.json");
        private static readonly string TerminologySettingsPath = Path.Combine(SettingsDirectory, "translation-terminology-settings.json");

        public static TranslationProviderSettings Load()
        {
            TranslationProviderSettings settings = LoadProviderSettings();
            settings.FormattingRules = LoadFormattingRules();
            settings.TerminologyEntries = LoadTerminologyEntries();
            return settings.Normalize();
        }

        public static TranslationProviderSettings LoadProviderSettings()
        {
            try
            {
                TranslationApiSettings apiSettings = ReadJsonFile<TranslationApiSettings>(ProviderSettingsPath);
                if (apiSettings == null)
                {
                    TranslationProviderSettings legacySettings = LoadLegacySettings();
                    return new TranslationProviderSettings
                    {
                        SelectedProvider = legacySettings.SelectedProvider,
                        DoubaoApiKey = legacySettings.DoubaoApiKey,
                        GoogleApiKey = legacySettings.GoogleApiKey,
                        BaiduAppId = legacySettings.BaiduAppId,
                        BaiduSecretKey = legacySettings.BaiduSecretKey,
                        TencentSecretId = legacySettings.TencentSecretId,
                        TencentSecretKey = legacySettings.TencentSecretKey,
                        TencentRegion = legacySettings.TencentRegion
                    }.Normalize();
                }

                apiSettings = apiSettings.Normalize();
                return new TranslationProviderSettings
                {
                    SelectedProvider = apiSettings.SelectedProvider,
                    DoubaoApiKey = apiSettings.DoubaoApiKey,
                    GoogleApiKey = apiSettings.GoogleApiKey,
                    BaiduAppId = apiSettings.BaiduAppId,
                    BaiduSecretKey = apiSettings.BaiduSecretKey,
                    TencentSecretId = apiSettings.TencentSecretId,
                    TencentSecretKey = apiSettings.TencentSecretKey,
                    TencentRegion = apiSettings.TencentRegion
                }.Normalize();
            }
            catch
            {
                return new TranslationProviderSettings().Normalize();
            }
        }

        public static void SaveProviderSettings(TranslationProviderSettings settings)
        {
            Directory.CreateDirectory(SettingsDirectory);
            TranslationProviderSettings normalized = (settings ?? new TranslationProviderSettings()).Normalize();
            WriteJsonFile(ProviderSettingsPath, new TranslationApiSettings
            {
                SelectedProvider = normalized.SelectedProvider,
                DoubaoApiKey = normalized.DoubaoApiKey,
                GoogleApiKey = normalized.GoogleApiKey,
                BaiduAppId = normalized.BaiduAppId,
                BaiduSecretKey = normalized.BaiduSecretKey,
                TencentSecretId = normalized.TencentSecretId,
                TencentSecretKey = normalized.TencentSecretKey,
                TencentRegion = normalized.TencentRegion
            }.Normalize());
        }

        public static TranslationFormattingRules LoadFormattingRules()
        {
            try
            {
                TranslationFormattingRules rules = ReadJsonFile<TranslationFormattingRules>(RuleSettingsPath);
                if (rules != null)
                {
                    return rules.Normalize();
                }

                return (LoadLegacySettings().FormattingRules ?? new TranslationFormattingRules()).Normalize();
            }
            catch
            {
                return new TranslationFormattingRules().Normalize();
            }
        }

        public static void SaveFormattingRules(TranslationFormattingRules rules)
        {
            Directory.CreateDirectory(SettingsDirectory);
            WriteJsonFile(RuleSettingsPath, (rules ?? new TranslationFormattingRules()).Normalize());
        }

        public static List<TranslationTerminologyEntry> LoadTerminologyEntries()
        {
            try
            {
                List<TranslationTerminologyEntry> entries = ReadJsonFile<List<TranslationTerminologyEntry>>(TerminologySettingsPath);
                if (entries == null)
                {
                    entries = LoadLegacySettings().TerminologyEntries ?? new List<TranslationTerminologyEntry>();
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    entries[i] = (entries[i] ?? new TranslationTerminologyEntry()).Normalize();
                }

                return entries;
            }
            catch
            {
                return new List<TranslationTerminologyEntry>();
            }
        }

        public static void SaveTerminologyEntries(IEnumerable<TranslationTerminologyEntry> entries)
        {
            Directory.CreateDirectory(SettingsDirectory);
            WriteJsonFile(
                TerminologySettingsPath,
                (entries ?? new List<TranslationTerminologyEntry>())
                    .Select(entry => (entry ?? new TranslationTerminologyEntry()).Normalize())
                    .ToList());
        }

        public static void Save(TranslationProviderSettings settings)
        {
            TranslationProviderSettings normalized = (settings ?? new TranslationProviderSettings()).Normalize();
            SaveProviderSettings(normalized);
            SaveFormattingRules(normalized.FormattingRules);
            SaveTerminologyEntries(normalized.TerminologyEntries);
        }

        public static void SaveTerminologyEntriesToFile(string filePath, IEnumerable<TranslationTerminologyEntry> entries)
        {
            string path = filePath ?? "";
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("未指定术语文件路径。", nameof(filePath));
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                SaveTerminologyEntriesToCsv(path, entries);
                return;
            }

            var serializer = new DataContractJsonSerializer(typeof(List<TranslationTerminologyEntry>));
            using (var stream = File.Create(path))
            {
                serializer.WriteObject(
                    stream,
                    (entries ?? new List<TranslationTerminologyEntry>())
                        .Select(entry => (entry ?? new TranslationTerminologyEntry()).Normalize())
                        .ToList());
            }
        }

        public static List<TranslationTerminologyEntry> LoadTerminologyEntriesFromFile(string filePath)
        {
            string path = filePath ?? "";
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("未指定术语文件路径。", nameof(filePath));
            }

            if (string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return LoadTerminologyEntriesFromCsv(path);
            }

            using (var stream = File.OpenRead(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(List<TranslationTerminologyEntry>));
                var entries = (serializer.ReadObject(stream) as List<TranslationTerminologyEntry>) ?? new List<TranslationTerminologyEntry>();
                for (int i = 0; i < entries.Count; i++)
                {
                    entries[i] = (entries[i] ?? new TranslationTerminologyEntry()).Normalize();
                }

                return entries;
            }
        }

        private static void SaveTerminologyEntriesToCsv(string filePath, IEnumerable<TranslationTerminologyEntry> entries)
        {
            List<TranslationTerminologyEntry> normalizedEntries = (entries ?? new List<TranslationTerminologyEntry>())
                .Select(entry => (entry ?? new TranslationTerminologyEntry()).Normalize())
                .ToList();

            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                IEnumerable<string[]> rows = normalizedEntries.Select(entry => new[]
                {
                    TranslationProviderHelper.GetPartOfSpeechDisplayName(entry.PartOfSpeech),
                    entry.SourceText ?? "",
                    entry.TargetText ?? "",
                    string.Join(" | ", (entry.Variants ?? new List<string>())
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value.Trim())),
                    entry.Notes ?? "",
                    entry.CaseSensitive ? "是" : "否"
                });

                CsvWriter.Write(writer,
                    new[] { "词性", "术语原文", "术语译文", "术语变体", "额外说明", "大小写敏感" },
                    rows);
            }
        }

        private static List<TranslationTerminologyEntry> LoadTerminologyEntriesFromCsv(string filePath)
        {
            var entries = new List<TranslationTerminologyEntry>();

            using (var textReader = new StreamReader(filePath))
            {
                var options = new CsvOptions { AllowNewLineInEnclosedFieldValues = true };
                int rowIndex = -1;
                foreach (var line in CsvReader.Read(textReader, options))
                {
                    rowIndex++;
                    if (line == null || line.ColumnCount == 0)
                    {
                        continue;
                    }

                    if (rowIndex == 0 && IsTerminologyCsvHeader(line.Values))
                    {
                        continue;
                    }

                    var entry = new TranslationTerminologyEntry
                    {
                        PartOfSpeech = ParseTerminologyPartOfSpeech(GetCsvValue(line.Values, 0)),
                        SourceText = GetCsvValue(line.Values, 1).Trim(),
                        TargetText = GetCsvValue(line.Values, 2).Trim(),
                        Variants = SplitTerminologyVariants(GetCsvValue(line.Values, 3)),
                        Notes = GetCsvValue(line.Values, 4).Trim(),
                        CaseSensitive = ParseTerminologyBoolean(GetCsvValue(line.Values, 5))
                    }.Normalize();

                    if (string.IsNullOrWhiteSpace(entry.SourceText) &&
                        string.IsNullOrWhiteSpace(entry.TargetText) &&
                        entry.Variants.Count == 0 &&
                        string.IsNullOrWhiteSpace(entry.Notes))
                    {
                        continue;
                    }

                    entries.Add(entry);
                }
            }

            return entries;
        }

        private static bool IsTerminologyCsvHeader(string[] values)
        {
            return string.Equals(GetCsvValue(values, 0), "词性", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(GetCsvValue(values, 1), "术语原文", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCsvValue(string[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
            {
                return "";
            }

            return values[index] ?? "";
        }

        private static TerminologyPartOfSpeech ParseTerminologyPartOfSpeech(string value)
        {
            string text = (value ?? "").Trim();
            switch (text.ToLowerInvariant())
            {
                case "动词":
                case "verb":
                    return TerminologyPartOfSpeech.Verb;
                case "形容词":
                case "adjective":
                    return TerminologyPartOfSpeech.Adjective;
                case "副词":
                case "adverb":
                    return TerminologyPartOfSpeech.Adverb;
                default:
                    return TerminologyPartOfSpeech.Noun;
            }
        }

        private static List<string> SplitTerminologyVariants(string value)
        {
            return (value ?? "")
                .Replace("\r\n", "\n")
                .Split(new[] { "\n", "|", ";", "；" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static bool ParseTerminologyBoolean(string value)
        {
            string text = (value ?? "").Trim();
            switch (text.ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "y":
                case "是":
                case "开":
                case "开启":
                    return true;
                default:
                    return false;
            }
        }

        private static TranslationProviderSettings LoadLegacySettings()
        {
            TranslationProviderSettings legacy = ReadJsonFile<TranslationProviderSettings>(LegacySettingsPath);
            return (legacy ?? new TranslationProviderSettings()).Normalize();
        }

        private static T ReadJsonFile<T>(string path) where T : class
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            using (var stream = File.OpenRead(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return serializer.ReadObject(stream) as T;
            }
        }

        private static void WriteJsonFile<T>(string path, T value)
        {
            using (var stream = File.Create(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, value);
            }
        }
    }

    public static class TranslationProviderFactory
    {
        public static ITranslationProvider Create(TranslationProviderSettings settings, TranslationProviderType provider)
        {
            settings = settings ?? new TranslationProviderSettings();

            switch (provider)
            {
                case TranslationProviderType.Google:
                    return new GoogleTranslationService(settings.GoogleApiKey);
                case TranslationProviderType.Baidu:
                    return new BaiduTranslationService(settings.BaiduAppId, settings.BaiduSecretKey);
                case TranslationProviderType.Tencent:
                    return new TencentTranslationService(settings.TencentSecretId, settings.TencentSecretKey, settings.TencentRegion);
                default:
                    return new DoubaoTranslationService(settings.DoubaoApiKey);
            }
        }

        public static string ValidateCredentials(TranslationProviderSettings settings, TranslationProviderType provider)
        {
            settings = settings ?? new TranslationProviderSettings();

            switch (provider)
            {
                case TranslationProviderType.Google:
                    return string.IsNullOrWhiteSpace(settings.GoogleApiKey) ? "请先在“翻译接口设置”中填写谷歌 API Key。" : null;
                case TranslationProviderType.Baidu:
                    if (string.IsNullOrWhiteSpace(settings.BaiduAppId) || string.IsNullOrWhiteSpace(settings.BaiduSecretKey))
                    {
                        return "请先在“翻译接口设置”中填写百度 AppId 和密钥。";
                    }

                    return null;
                case TranslationProviderType.Tencent:
                    if (string.IsNullOrWhiteSpace(settings.TencentSecretId) || string.IsNullOrWhiteSpace(settings.TencentSecretKey))
                    {
                        return "请先在“翻译接口设置”中填写腾讯 SecretId 和 SecretKey。";
                    }

                    return null;
                default:
                    return string.IsNullOrWhiteSpace(settings.DoubaoApiKey) ? "请先在“翻译接口设置”中填写豆包 API Key。" : null;
            }
        }
    }

    internal sealed class PreservedTextFormat
    {
        private readonly Dictionary<string, string> placeholders;
        private readonly bool trimTranslatedText;

        public PreservedTextFormat(string maskedCoreText, string leadingWhitespace, string trailingWhitespace, Dictionary<string, string> placeholders, bool trimTranslatedText)
        {
            MaskedCoreText = maskedCoreText ?? "";
            LeadingWhitespace = leadingWhitespace ?? "";
            TrailingWhitespace = trailingWhitespace ?? "";
            this.placeholders = placeholders ?? new Dictionary<string, string>();
            this.trimTranslatedText = trimTranslatedText;
        }

        public string MaskedCoreText { get; private set; }

        public string LeadingWhitespace { get; }

        public string TrailingWhitespace { get; }

        public string Restore(string translatedText)
        {
            string restored = translatedText ?? "";
            foreach (var pair in placeholders)
            {
                restored = restored.Replace(pair.Key, pair.Value);
            }

            return trimTranslatedText
                ? LeadingWhitespace + restored.Trim() + TrailingWhitespace
                : restored;
        }

        public void SetMaskedCoreText(string maskedCoreText)
        {
            MaskedCoreText = maskedCoreText ?? "";
        }

        public void RegisterPlaceholder(string placeholder, string value)
        {
            placeholders[placeholder ?? ""] = value ?? "";
        }
    }

    internal static class TranslationTextFormatter
    {
        private const string EscapeSequencePattern = @"\\r\\n|\\n|\\r|\\t";
        private const string PlaceholderPattern = @"%\d*\$?[-+# 0,(]*\d*(?:\.\d+)?[a-zA-Z]|%\w|\{[^{}\r\n]+\}|\$\{[^{}\r\n]+\}";
        private const string AngleBracketTagPattern = @"<[^<>\r\n]+>";
        private const string SquareBracketTagPattern = @"\[[^\[\]\r\n]+\]";
        private static readonly Regex InternalPlaceholderRegex = new Regex(@"__BXS_(?:TOKEN|TERM)_\d+__", RegexOptions.Compiled);
        private static readonly Regex NonContentRegex = new Regex(@"[\s\p{P}\p{S}]+", RegexOptions.Compiled);

        public static async Task<string> TranslateAsync(
            string sourceText,
            string sourceLanguage,
            string targetLanguage,
            bool preserveFormatting,
            TranslationFormattingRules formattingRules,
            IReadOnlyList<TranslationTerminologyEntry> terminologyEntries,
            Func<string, string, string, Task<string>> translateCoreAsync)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                return "";
            }

            var effectiveFormattingRules = preserveFormatting
                ? (formattingRules ?? new TranslationFormattingRules())
                : CreateDisabledFormattingRules();

            var preserved = PreserveFormatting(sourceText, effectiveFormattingRules);
            ApplyTerminologyRules(preserved, terminologyEntries);
            if (string.IsNullOrEmpty(preserved.MaskedCoreText))
            {
                return sourceText;
            }

            if (!ContainsTranslatableContent(preserved.MaskedCoreText))
            {
                return preserved.Restore(preserved.MaskedCoreText);
            }

            string translated = await translateCoreAsync(preserved.MaskedCoreText, sourceLanguage, targetLanguage);
            return CleanupSuspiciousInstructionalResponse(preserved.Restore(translated));
        }

        private static TranslationFormattingRules CreateDisabledFormattingRules()
        {
            return new TranslationFormattingRules
            {
                PreserveEscapeSequences = false,
                PreservePlaceholders = false,
                PreserveAngleBracketTags = false,
                PreserveSquareBracketTags = false,
                PreserveLeadingAndTrailingWhitespace = false,
                CustomProtectedPatterns = new List<string>()
            };
        }

        private static PreservedTextFormat PreserveFormatting(string text, TranslationFormattingRules formattingRules)
        {
            string sourceText = text ?? "";
            bool preserveWhitespace = formattingRules?.PreserveLeadingAndTrailingWhitespace ?? true;
            string leadingWhitespace = preserveWhitespace ? Regex.Match(sourceText, @"^\s+").Value : "";
            string trailingWhitespace = preserveWhitespace ? Regex.Match(sourceText, @"\s+$").Value : "";
            int coreStartIndex = leadingWhitespace.Length;
            int coreLength = Math.Max(0, sourceText.Length - leadingWhitespace.Length - trailingWhitespace.Length);
            string coreText = preserveWhitespace ? sourceText.Substring(coreStartIndex, coreLength) : sourceText;

            if (string.IsNullOrEmpty(coreText))
            {
                return new PreservedTextFormat("", leadingWhitespace, trailingWhitespace, new Dictionary<string, string>(), preserveWhitespace);
            }

            Regex placeholderRegex = BuildProtectedContentRegex(formattingRules ?? new TranslationFormattingRules());
            if (placeholderRegex == null)
            {
                return new PreservedTextFormat(coreText, leadingWhitespace, trailingWhitespace, new Dictionary<string, string>(), preserveWhitespace);
            }

            int placeholderIndex = 0;
            var placeholders = new Dictionary<string, string>(StringComparer.Ordinal);
            string maskedText = placeholderRegex.Replace(
                coreText,
                match =>
                {
                    string placeholder = string.Format("__BXS_TOKEN_{0}__", placeholderIndex++);
                    placeholders[placeholder] = match.Value;
                    return placeholder;
                });

            return new PreservedTextFormat(maskedText, leadingWhitespace, trailingWhitespace, placeholders, preserveWhitespace);
        }

        private static void ApplyTerminologyRules(PreservedTextFormat preserved, IReadOnlyList<TranslationTerminologyEntry> terminologyEntries)
        {
            if (preserved == null || terminologyEntries == null || terminologyEntries.Count == 0)
            {
                return;
            }

            string currentText = preserved.MaskedCoreText;
            int termIndex = 0;

            foreach (var candidate in BuildTerminologyCandidates(terminologyEntries))
            {
                Regex regex = BuildTerminologyRegex(candidate.SourceText, candidate.CaseSensitive);
                currentText = regex.Replace(
                    currentText,
                    match =>
                    {
                        string placeholder = string.Format("__BXS_TERM_{0}__", termIndex++);
                        preserved.RegisterPlaceholder(placeholder, candidate.TargetText);
                        return placeholder;
                    });
            }

            preserved.SetMaskedCoreText(currentText);
        }

        private static IEnumerable<TerminologyCandidate> BuildTerminologyCandidates(IReadOnlyList<TranslationTerminologyEntry> terminologyEntries)
        {
            var candidates = new List<TerminologyCandidate>();

            foreach (TranslationTerminologyEntry entry in terminologyEntries)
            {
                TranslationTerminologyEntry normalizedEntry = (entry ?? new TranslationTerminologyEntry()).Normalize();
                if (string.IsNullOrWhiteSpace(normalizedEntry.SourceText) || string.IsNullOrWhiteSpace(normalizedEntry.TargetText))
                {
                    continue;
                }

                AddTerminologyCandidate(candidates, normalizedEntry.SourceText, normalizedEntry.TargetText, normalizedEntry.CaseSensitive);
                foreach (string variant in normalizedEntry.Variants ?? new List<string>())
                {
                    AddTerminologyCandidate(candidates, variant, normalizedEntry.TargetText, normalizedEntry.CaseSensitive);
                }
            }

            candidates.Sort((left, right) => right.SourceText.Length.CompareTo(left.SourceText.Length));
            return candidates;
        }

        private static void AddTerminologyCandidate(List<TerminologyCandidate> candidates, string sourceText, string targetText, bool caseSensitive)
        {
            string normalizedSource = (sourceText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalizedSource))
            {
                return;
            }

            if (candidates.Exists(candidate =>
                string.Equals(candidate.SourceText, normalizedSource, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) &&
                string.Equals(candidate.TargetText, targetText ?? "", StringComparison.Ordinal) &&
                candidate.CaseSensitive == caseSensitive))
            {
                return;
            }

            candidates.Add(new TerminologyCandidate
            {
                SourceText = normalizedSource,
                TargetText = targetText ?? "",
                CaseSensitive = caseSensitive
            });
        }

        private static Regex BuildTerminologyRegex(string sourceText, bool caseSensitive)
        {
            string pattern = @"(?<![\p{L}\p{Nd}_])" + Regex.Escape(sourceText ?? "") + @"(?![\p{L}\p{Nd}_])";
            RegexOptions options = RegexOptions.Compiled;
            if (!caseSensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }

            return new Regex(pattern, options);
        }

        private static Regex BuildProtectedContentRegex(TranslationFormattingRules formattingRules)
        {
            var patterns = new List<string>();

            if (formattingRules.PreserveEscapeSequences)
            {
                patterns.Add(EscapeSequencePattern);
            }

            if (formattingRules.PreservePlaceholders)
            {
                patterns.Add(PlaceholderPattern);
            }

            if (formattingRules.PreserveAngleBracketTags)
            {
                patterns.Add(AngleBracketTagPattern);
            }

            if (formattingRules.PreserveSquareBracketTags)
            {
                patterns.Add(SquareBracketTagPattern);
            }

            foreach (string customPattern in formattingRules.CustomProtectedPatterns ?? new List<string>())
            {
                string pattern = (customPattern ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    patterns.Add(pattern);
                }
            }

            if (patterns.Count == 0)
            {
                return null;
            }

            return new Regex("(" + string.Join("|", patterns) + ")", RegexOptions.Compiled);
        }

        private static bool ContainsTranslatableContent(string text)
        {
            string normalized = InternalPlaceholderRegex.Replace(text ?? "", "");
            normalized = NonContentRegex.Replace(normalized, "");
            return !string.IsNullOrWhiteSpace(normalized);
        }

        private static string CleanupSuspiciousInstructionalResponse(string translatedText)
        {
            string text = (translatedText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return translatedText ?? "";
            }

            if (!LooksLikeInstructionalWrapper(text))
            {
                return translatedText ?? "";
            }

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = (lines[i] ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(line) && !LooksLikeInstructionalWrapper(line))
                {
                    return line;
                }
            }

            return translatedText ?? "";
        }

        private static bool LooksLikeInstructionalWrapper(string text)
        {
            string value = (text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return
                (value.Contains("请将以下文本翻译成") && value.Contains("翻译过程")) ||
                value.StartsWith("请翻译以下文本", StringComparison.Ordinal) ||
                value.StartsWith("请将以下文本翻译", StringComparison.Ordinal) ||
                value.StartsWith("Translate the following text", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Please translate the following text", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class TerminologyCandidate
        {
            public string SourceText { get; set; }

            public string TargetText { get; set; }

            public bool CaseSensitive { get; set; }
        }
    }

    internal sealed class GoogleTranslationService : ITranslationProvider
    {
        private readonly string apiKey;
        private readonly TranslationFormattingRules formattingRules;
        private readonly List<TranslationTerminologyEntry> terminologyEntries;

        public GoogleTranslationService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("未提供谷歌 API Key。", nameof(apiKey));
            }

            this.apiKey = apiKey.Trim();
            TranslationProviderSettings settings = TranslationSettingsStore.Load();
            formattingRules = (settings.FormattingRules ?? new TranslationFormattingRules()).Normalize();
            terminologyEntries = settings.TerminologyEntries ?? new List<TranslationTerminologyEntry>();
        }

        public TranslationProviderType ProviderType => TranslationProviderType.Google;

        public Task<string> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, bool preserveFormatting = true)
        {
            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                throw new InvalidOperationException("目标语言不能为空。");
            }

            return TranslationTextFormatter.TranslateAsync(sourceText, sourceLanguage, targetLanguage, preserveFormatting, formattingRules, terminologyEntries, TranslateCoreAsync);
        }

        private async Task<string> TranslateCoreAsync(string sourceText, string sourceLanguage, string targetLanguage)
        {
            string endpoint = "https://translation.googleapis.com/language/translate/v2?key=" + Uri.EscapeDataString(apiKey);
            string formData =
                "q=" + Uri.EscapeDataString(sourceText ?? "") +
                "&target=" + Uri.EscapeDataString(targetLanguage ?? "") +
                "&format=text";

            if (!string.IsNullOrWhiteSpace(sourceLanguage))
            {
                formData += "&source=" + Uri.EscapeDataString(sourceLanguage);
            }

            string responseBody = await TranslationWebRequestHelper.SendFormRequestAsync(endpoint, formData, null);
            var response = TranslationWebRequestHelper.Deserialize<GoogleTranslateResponse>(responseBody);
            string translated = response?.Data?.Translations != null && response.Data.Translations.Count > 0
                ? response.Data.Translations[0].TranslatedText
                : null;

            if (string.IsNullOrWhiteSpace(translated))
            {
                throw new InvalidOperationException("谷歌翻译接口未返回可用译文。");
            }

            return WebUtility.HtmlDecode(translated);
        }
    }

    internal sealed class BaiduTranslationService : ITranslationProvider
    {
        private readonly string appId;
        private readonly string secretKey;
        private readonly TranslationFormattingRules formattingRules;
        private readonly List<TranslationTerminologyEntry> terminologyEntries;

        public BaiduTranslationService(string appId, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentException("未提供百度翻译所需的 AppId 或密钥。");
            }

            this.appId = appId.Trim();
            this.secretKey = secretKey.Trim();
            TranslationProviderSettings settings = TranslationSettingsStore.Load();
            formattingRules = (settings.FormattingRules ?? new TranslationFormattingRules()).Normalize();
            terminologyEntries = settings.TerminologyEntries ?? new List<TranslationTerminologyEntry>();
        }

        public TranslationProviderType ProviderType => TranslationProviderType.Baidu;

        public Task<string> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, bool preserveFormatting = true)
        {
            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                throw new InvalidOperationException("目标语言不能为空。");
            }

            return TranslationTextFormatter.TranslateAsync(sourceText, sourceLanguage, targetLanguage, preserveFormatting, formattingRules, terminologyEntries, TranslateCoreAsync);
        }

        private async Task<string> TranslateCoreAsync(string sourceText, string sourceLanguage, string targetLanguage)
        {
            string salt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string from = MapBaiduLanguage(string.IsNullOrWhiteSpace(sourceLanguage) ? "auto" : sourceLanguage);
            string to = MapBaiduLanguage(targetLanguage);
            string sign = TranslationWebRequestHelper.CreateMd5(appId + (sourceText ?? "") + salt + secretKey);

            string formData =
                "q=" + Uri.EscapeDataString(sourceText ?? "") +
                "&from=" + Uri.EscapeDataString(from) +
                "&to=" + Uri.EscapeDataString(to) +
                "&appid=" + Uri.EscapeDataString(appId) +
                "&salt=" + Uri.EscapeDataString(salt) +
                "&sign=" + Uri.EscapeDataString(sign);

            string responseBody = await TranslationWebRequestHelper.SendFormRequestAsync("https://fanyi-api.baidu.com/api/trans/vip/translate", formData, null);
            var response = TranslationWebRequestHelper.Deserialize<BaiduTranslateResponse>(responseBody);

            if (!string.IsNullOrWhiteSpace(response?.ErrorCode))
            {
                throw new InvalidOperationException("百度翻译接口返回错误：" + response.ErrorCode + " " + (response.ErrorMsg ?? ""));
            }

            string translated = response?.TransResult != null && response.TransResult.Count > 0
                ? response.TransResult[0].Dst
                : null;

            if (string.IsNullOrWhiteSpace(translated))
            {
                throw new InvalidOperationException("百度翻译接口未返回可用译文。");
            }

            return translated;
        }

        private static string MapBaiduLanguage(string languageCode)
        {
            switch (languageCode ?? "")
            {
                case "":
                case "auto":
                    return "auto";
                case "zh":
                    return "zh";
                case "zh-Hant":
                    return "cht";
                case "en":
                    return "en";
                case "ja":
                    return "jp";
                case "ko":
                    return "kor";
                case "fr":
                    return "fra";
                case "es":
                    return "spa";
                case "it":
                    return "it";
                case "de":
                    return "de";
                case "tr":
                    return "tr";
                case "ru":
                    return "ru";
                case "pt":
                    return "pt";
                case "vi":
                    return "vie";
                case "id":
                    return "id";
                case "th":
                    return "th";
                case "ms":
                    return "may";
                case "ar":
                    return "ara";
                case "nl":
                    return "nl";
                case "pl":
                    return "pl";
                case "ro":
                    return "rom";
                case "sv":
                    return "swe";
                case "uk":
                    return "ukr";
                case "da":
                    return "dan";
                case "fi":
                    return "fin";
                case "cs":
                    return "cs";
                case "hr":
                    return "hr";
                case "hu":
                    return "hu";
                case "nb":
                    return "nor";
                default:
                    throw new InvalidOperationException("百度翻译当前不支持该语言代码：" + languageCode);
            }
        }
    }

    internal sealed class TencentTranslationService : ITranslationProvider
    {
        private readonly string secretId;
        private readonly string secretKey;
        private readonly string region;
        private readonly TranslationFormattingRules formattingRules;
        private readonly List<TranslationTerminologyEntry> terminologyEntries;

        public TencentTranslationService(string secretId, string secretKey, string region)
        {
            if (string.IsNullOrWhiteSpace(secretId) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentException("未提供腾讯翻译所需的 SecretId 或 SecretKey。");
            }

            this.secretId = secretId.Trim();
            this.secretKey = secretKey.Trim();
            this.region = string.IsNullOrWhiteSpace(region) ? "ap-beijing" : region.Trim();
            TranslationProviderSettings settings = TranslationSettingsStore.Load();
            formattingRules = (settings.FormattingRules ?? new TranslationFormattingRules()).Normalize();
            terminologyEntries = settings.TerminologyEntries ?? new List<TranslationTerminologyEntry>();
        }

        public TranslationProviderType ProviderType => TranslationProviderType.Tencent;

        public Task<string> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, bool preserveFormatting = true)
        {
            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                throw new InvalidOperationException("目标语言不能为空。");
            }

            if (string.IsNullOrWhiteSpace(sourceLanguage))
            {
                throw new InvalidOperationException("腾讯翻译当前需要手动指定源语言。");
            }

            return TranslationTextFormatter.TranslateAsync(sourceText, sourceLanguage, targetLanguage, preserveFormatting, formattingRules, terminologyEntries, TranslateCoreAsync);
        }

        private Task<string> TranslateCoreAsync(string sourceText, string sourceLanguage, string targetLanguage)
        {
            return Task.Run(() =>
            {
                const string service = "tmt";
                const string host = "tmt.tencentcloudapi.com";
                const string action = "TextTranslate";
                const string version = "2018-03-21";
                const string algorithm = "TC3-HMAC-SHA256";
                string endpoint = "https://" + host + "/";

                string requestPayload = TranslationWebRequestHelper.Serialize(new TencentTranslateRequest
                {
                    SourceText = sourceText ?? "",
                    Source = MapTencentLanguage(sourceLanguage),
                    Target = MapTencentLanguage(targetLanguage),
                    ProjectId = 0
                });

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string date = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime.ToString("yyyy-MM-dd");

                string canonicalHeaders =
                    "content-type:application/json; charset=utf-8\n" +
                    "host:" + host + "\n" +
                    "x-tc-action:" + action.ToLowerInvariant() + "\n";
                string signedHeaders = "content-type;host;x-tc-action";
                string hashedRequestPayload = TranslationWebRequestHelper.Sha256Hex(requestPayload);
                string canonicalRequest =
                    "POST\n/\n\n" +
                    canonicalHeaders + "\n" +
                    signedHeaders + "\n" +
                    hashedRequestPayload;

                string credentialScope = date + "/" + service + "/tc3_request";
                string stringToSign =
                    algorithm + "\n" +
                    timestamp + "\n" +
                    credentialScope + "\n" +
                    TranslationWebRequestHelper.Sha256Hex(canonicalRequest);

                byte[] secretDate = TranslationWebRequestHelper.HmacSha256(Encoding.UTF8.GetBytes("TC3" + secretKey), date);
                byte[] secretService = TranslationWebRequestHelper.HmacSha256(secretDate, service);
                byte[] secretSigning = TranslationWebRequestHelper.HmacSha256(secretService, "tc3_request");
                string signature = TranslationWebRequestHelper.BytesToHex(TranslationWebRequestHelper.HmacSha256(secretSigning, stringToSign));
                string authorization =
                    algorithm + " " +
                    "Credential=" + secretId + "/" + credentialScope + ", " +
                    "SignedHeaders=" + signedHeaders + ", " +
                    "Signature=" + signature;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Accept = "application/json";
                request.Timeout = 60000;
                request.ReadWriteTimeout = 60000;
                request.Headers["Authorization"] = authorization;
                request.Headers["X-TC-Action"] = action;
                request.Headers["X-TC-Version"] = version;
                request.Headers["X-TC-Region"] = region;
                request.Headers["X-TC-Timestamp"] = timestamp.ToString();
                request.Host = host;

                try
                {
                    using (var requestStream = request.GetRequestStream())
                    using (var writer = new StreamWriter(requestStream, new UTF8Encoding(false)))
                    {
                        writer.Write(requestPayload);
                    }

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var responseStream = response.GetResponseStream())
                    using (var reader = new StreamReader(responseStream ?? Stream.Null, Encoding.UTF8))
                    {
                        string responseBody = reader.ReadToEnd();
                        var payload = TranslationWebRequestHelper.Deserialize<TencentTranslateResponse>(responseBody);
                        if (!string.IsNullOrWhiteSpace(payload?.Response?.Error?.Code))
                        {
                            throw new InvalidOperationException("腾讯翻译接口返回错误：" + payload.Response.Error.Code + " " + (payload.Response.Error.Message ?? ""));
                        }

                        if (string.IsNullOrWhiteSpace(payload?.Response?.TargetText))
                        {
                            throw new InvalidOperationException("腾讯翻译接口未返回可用译文。");
                        }

                        return payload.Response.TargetText;
                    }
                }
                catch (WebException ex)
                {
                    throw new InvalidOperationException(TranslationWebRequestHelper.GetApiErrorMessage("腾讯翻译", ex), ex);
                }
            });
        }

        private static string MapTencentLanguage(string languageCode)
        {
            switch (languageCode ?? "")
            {
                case "zh":
                case "zh-TW":
                case "en":
                case "ja":
                case "ko":
                case "fr":
                case "es":
                case "it":
                case "de":
                case "tr":
                case "ru":
                case "pt":
                case "vi":
                case "id":
                case "th":
                case "ms":
                case "ar":
                case "hi":
                    return languageCode;
                case "zh-Hant":
                    return "zh-TW";
                default:
                    throw new InvalidOperationException("腾讯翻译当前不支持该语言代码：" + languageCode);
            }
        }
    }

    internal static class TranslationWebRequestHelper
    {
        public static string GetApiErrorMessage(string providerName, WebException exception)
        {
            if (exception.Response == null)
            {
                return "请求" + providerName + "接口失败：" + exception.Message;
            }

            using (var stream = exception.Response.GetResponseStream())
            using (var reader = new StreamReader(stream ?? Stream.Null, Encoding.UTF8))
            {
                string body = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return "请求" + providerName + "接口失败：" + exception.Message;
                }

                Match match = Regex.Match(body, "\"(?:message|error_msg|errmsg|msg)\"\\s*:\\s*\"(?<msg>(?:\\\\.|[^\"])*)\"");
                if (match.Success)
                {
                    return "请求" + providerName + "接口失败：" + Regex.Unescape(match.Groups["msg"].Value);
                }

                return "请求" + providerName + "接口失败：" + body;
            }
        }

        public static async Task<string> SendFormRequestAsync(string endpoint, string formData, IDictionary<string, string> headers)
        {
            return await Task.Run(() =>
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Accept = "application/json";
                request.Timeout = 60000;
                request.ReadWriteTimeout = 60000;

                if (headers != null)
                {
                    foreach (var pair in headers)
                    {
                        request.Headers[pair.Key] = pair.Value;
                    }
                }

                try
                {
                    using (var requestStream = request.GetRequestStream())
                    using (var writer = new StreamWriter(requestStream, new UTF8Encoding(false)))
                    {
                        writer.Write(formData ?? "");
                    }

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var responseStream = response.GetResponseStream())
                    using (var reader = new StreamReader(responseStream ?? Stream.Null, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (WebException ex)
                {
                    throw new InvalidOperationException(GetApiErrorMessage("翻译", ex), ex);
                }
            });
        }

        public static T Deserialize<T>(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json ?? "")))
            {
                return (T)serializer.ReadObject(stream);
            }
        }

        public static string Serialize<T>(T value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static byte[] HmacSha256(byte[] key, string message)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(message ?? ""));
            }
        }

        public static string Sha256Hex(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                return BytesToHex(sha256.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")));
            }
        }

        public static string BytesToHex(byte[] value)
        {
            var builder = new StringBuilder((value ?? new byte[0]).Length * 2);
            foreach (byte item in value ?? new byte[0])
            {
                builder.Append(item.ToString("x2"));
            }

            return builder.ToString();
        }

        public static string CreateMd5(string value)
        {
            using (var md5 = MD5.Create())
            {
                return BytesToHex(md5.ComputeHash(Encoding.UTF8.GetBytes(value ?? "")));
            }
        }
    }

    [DataContract]
    internal sealed class GoogleTranslateResponse
    {
        [DataMember(Name = "data")]
        public GoogleTranslateData Data { get; set; }
    }

    [DataContract]
    internal sealed class GoogleTranslateData
    {
        [DataMember(Name = "translations")]
        public List<GoogleTranslateItem> Translations { get; set; }
    }

    [DataContract]
    internal sealed class GoogleTranslateItem
    {
        [DataMember(Name = "translatedText")]
        public string TranslatedText { get; set; }
    }

    [DataContract]
    internal sealed class BaiduTranslateResponse
    {
        [DataMember(Name = "error_code", EmitDefaultValue = false)]
        public string ErrorCode { get; set; }

        [DataMember(Name = "error_msg", EmitDefaultValue = false)]
        public string ErrorMsg { get; set; }

        [DataMember(Name = "trans_result", EmitDefaultValue = false)]
        public List<BaiduTranslateItem> TransResult { get; set; }
    }

    [DataContract]
    internal sealed class BaiduTranslateItem
    {
        [DataMember(Name = "dst")]
        public string Dst { get; set; }
    }

    [DataContract]
    internal sealed class TencentTranslateRequest
    {
        [DataMember(Name = "SourceText")]
        public string SourceText { get; set; }

        [DataMember(Name = "Source")]
        public string Source { get; set; }

        [DataMember(Name = "Target")]
        public string Target { get; set; }

        [DataMember(Name = "ProjectId")]
        public int ProjectId { get; set; }
    }

    [DataContract]
    internal sealed class TencentTranslateResponse
    {
        [DataMember(Name = "Response")]
        public TencentTranslateResult Response { get; set; }
    }

    [DataContract]
    internal sealed class TencentTranslateResult
    {
        [DataMember(Name = "TargetText", EmitDefaultValue = false)]
        public string TargetText { get; set; }

        [DataMember(Name = "Error", EmitDefaultValue = false)]
        public TencentTranslateError Error { get; set; }
    }

    [DataContract]
    internal sealed class TencentTranslateError
    {
        [DataMember(Name = "Code", EmitDefaultValue = false)]
        public string Code { get; set; }

        [DataMember(Name = "Message", EmitDefaultValue = false)]
        public string Message { get; set; }
    }
}
