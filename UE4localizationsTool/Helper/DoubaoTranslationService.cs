using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace UE4localizationsTool.Helper
{
    public enum DoubaoTranslationScope
    {
        SelectedRows,
        FirstNRows,
        AllRows
    }

    public sealed class TranslationLanguage
    {
        public TranslationLanguage(string code, string displayName)
        {
            Code = code ?? "";
            DisplayName = displayName ?? "";
        }

        public string Code { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class DoubaoTranslationService : ITranslationProvider
    {
        public const string PreviewColumnName = "机器翻译预览";
        public const string ModelName = "doubao-seed-translation-250915";

        private readonly string apiKey;
        private readonly TranslationFormattingRules formattingRules;
        private readonly System.Collections.Generic.List<TranslationTerminologyEntry> terminologyEntries;

        public DoubaoTranslationService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("未提供豆包 API Key。请先在“翻译接口设置”中填写。", nameof(apiKey));
            }

            this.apiKey = apiKey.Trim();
            TranslationProviderSettings settings = TranslationSettingsStore.Load();
            formattingRules = (settings.FormattingRules ?? new TranslationFormattingRules()).Normalize();
            terminologyEntries = settings.TerminologyEntries ?? new System.Collections.Generic.List<TranslationTerminologyEntry>();
        }

        public TranslationProviderType ProviderType => TranslationProviderType.Doubao;

        public static IReadOnlyList<TranslationLanguage> SupportedLanguages { get; } = new List<TranslationLanguage>
        {
            new TranslationLanguage("", "自动检测"),
            new TranslationLanguage("zh", "中文（简体）"),
            new TranslationLanguage("zh-Hant", "中文（繁体）"),
            new TranslationLanguage("en", "英语"),
            new TranslationLanguage("ja", "日语"),
            new TranslationLanguage("ko", "韩语"),
            new TranslationLanguage("de", "德语"),
            new TranslationLanguage("fr", "法语"),
            new TranslationLanguage("es", "西班牙语"),
            new TranslationLanguage("it", "意大利语"),
            new TranslationLanguage("pt", "葡萄牙语"),
            new TranslationLanguage("ru", "俄语"),
            new TranslationLanguage("th", "泰语"),
            new TranslationLanguage("vi", "越南语"),
            new TranslationLanguage("ar", "阿拉伯语"),
            new TranslationLanguage("cs", "捷克语"),
            new TranslationLanguage("da", "丹麦语"),
            new TranslationLanguage("fi", "芬兰语"),
            new TranslationLanguage("hr", "克罗地亚语"),
            new TranslationLanguage("hu", "匈牙利语"),
            new TranslationLanguage("id", "印尼语"),
            new TranslationLanguage("ms", "马来语"),
            new TranslationLanguage("nb", "挪威布克莫尔语"),
            new TranslationLanguage("nl", "荷兰语"),
            new TranslationLanguage("pl", "波兰语"),
            new TranslationLanguage("ro", "罗马尼亚语"),
            new TranslationLanguage("sv", "瑞典语"),
            new TranslationLanguage("tr", "土耳其语"),
            new TranslationLanguage("uk", "乌克兰语")
        };

        public async Task<string> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, bool preserveFormatting = true)
        {
            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                throw new InvalidOperationException("目标语言不能为空。");
            }

            if (string.IsNullOrEmpty(sourceText))
            {
                return "";
            }

            return await TranslationTextFormatter.TranslateAsync(
                sourceText,
                string.IsNullOrWhiteSpace(sourceLanguage) ? null : sourceLanguage.Trim(),
                targetLanguage.Trim(),
                preserveFormatting,
                formattingRules,
                terminologyEntries,
                SendRequestAsync);
        }

        private Task<string> SendRequestAsync(string sourceText, string sourceLanguage, string targetLanguage)
        {
            return Task.Run(() => SendRequest(sourceText, sourceLanguage, targetLanguage));
        }

        private string SendRequest(string sourceText, string sourceLanguage, string targetLanguage)
        {
            const string endpoint = "https://ark.cn-beijing.volces.com/api/v3/responses";
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var payload = new ArkResponseRequest
            {
                Model = ModelName,
                Input = new List<ArkInputItem>
                {
                    new ArkInputItem
                    {
                        Role = "user",
                        Content = new List<ArkInputContent>
                        {
                            new ArkInputContent
                            {
                                Type = "input_text",
                                Text = sourceText,
                                TranslationOptions = new ArkTranslationOptions
                                {
                                    SourceLanguage = sourceLanguage,
                                    TargetLanguage = targetLanguage
                                }
                            }
                        }
                    }
                }
            };

            string requestBody = TranslationWebRequestHelper.Serialize(payload);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Timeout = 60000;
            request.ReadWriteTimeout = 60000;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + apiKey;

            try
            {
                using (var requestStream = request.GetRequestStream())
                using (var writer = new StreamWriter(requestStream, new UTF8Encoding(false)))
                {
                    writer.Write(requestBody);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream ?? Stream.Null, Encoding.UTF8))
                {
                    string responseBody = reader.ReadToEnd();
                    return ExtractTranslatedText(responseBody);
                }
            }
            catch (WebException ex)
            {
                throw new InvalidOperationException(TranslationWebRequestHelper.GetApiErrorMessage("豆包翻译", ex), ex);
            }
        }

        private static string ExtractTranslatedText(string responseBody)
        {
            var response = TranslationWebRequestHelper.Deserialize<ArkResponsePayload>(responseBody);
            if (!string.IsNullOrWhiteSpace(response?.OutputText))
            {
                return response.OutputText;
            }

            string text = response?.Output?
                .Where(item => item?.Content != null)
                .SelectMany(item => item.Content)
                .Select(content => content?.Text)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("豆包翻译接口未返回可用译文。");
            }

            return text;
        }


        [DataContract]
        private sealed class ArkResponseRequest
        {
            [DataMember(Name = "model")]
            public string Model { get; set; }

            [DataMember(Name = "input")]
            public List<ArkInputItem> Input { get; set; }
        }

        [DataContract]
        private sealed class ArkInputItem
        {
            [DataMember(Name = "role")]
            public string Role { get; set; }

            [DataMember(Name = "content")]
            public List<ArkInputContent> Content { get; set; }
        }

        [DataContract]
        private sealed class ArkInputContent
        {
            [DataMember(Name = "type")]
            public string Type { get; set; }

            [DataMember(Name = "text")]
            public string Text { get; set; }

            [DataMember(Name = "translation_options")]
            public ArkTranslationOptions TranslationOptions { get; set; }
        }

        [DataContract]
        private sealed class ArkTranslationOptions
        {
            [DataMember(Name = "source_language", EmitDefaultValue = false)]
            public string SourceLanguage { get; set; }

            [DataMember(Name = "target_language")]
            public string TargetLanguage { get; set; }
        }

        [DataContract]
        private sealed class ArkResponsePayload
        {
            [DataMember(Name = "output_text", EmitDefaultValue = false)]
            public string OutputText { get; set; }

            [DataMember(Name = "output", EmitDefaultValue = false)]
            public List<ArkOutputItem> Output { get; set; }
        }

        [DataContract]
        private sealed class ArkOutputItem
        {
            [DataMember(Name = "content", EmitDefaultValue = false)]
            public List<ArkOutputContent> Content { get; set; }
        }

        [DataContract]
        private sealed class ArkOutputContent
        {
            [DataMember(Name = "text", EmitDefaultValue = false)]
            public string Text { get; set; }
        }
    }
}
