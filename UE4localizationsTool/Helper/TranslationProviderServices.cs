using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UE4localizationsTool.Helper
{
    public enum TranslationProviderType
    {
        Doubao,
        Google,
        Baidu,
        Tencent
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
    }

    public static class TranslationSettingsStore
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UE4本地化工具");

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "translation-settings.json");

        public static TranslationProviderSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return new TranslationProviderSettings();
                }

                using (var stream = File.OpenRead(SettingsPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(TranslationProviderSettings));
                    return (TranslationProviderSettings)serializer.ReadObject(stream);
                }
            }
            catch
            {
                return new TranslationProviderSettings();
            }
        }

        public static void Save(TranslationProviderSettings settings)
        {
            Directory.CreateDirectory(SettingsDirectory);
            using (var stream = File.Create(SettingsPath))
            {
                var serializer = new DataContractJsonSerializer(typeof(TranslationProviderSettings));
                serializer.WriteObject(stream, settings ?? new TranslationProviderSettings());
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

        public PreservedTextFormat(string maskedCoreText, string leadingWhitespace, string trailingWhitespace, Dictionary<string, string> placeholders)
        {
            MaskedCoreText = maskedCoreText ?? "";
            LeadingWhitespace = leadingWhitespace ?? "";
            TrailingWhitespace = trailingWhitespace ?? "";
            this.placeholders = placeholders ?? new Dictionary<string, string>();
        }

        public string MaskedCoreText { get; }

        public string LeadingWhitespace { get; }

        public string TrailingWhitespace { get; }

        public string Restore(string translatedText)
        {
            string restored = translatedText ?? "";
            foreach (var pair in placeholders)
            {
                restored = restored.Replace(pair.Key, pair.Value);
            }

            return LeadingWhitespace + restored.Trim() + TrailingWhitespace;
        }
    }

    internal static class TranslationTextFormatter
    {
        private static readonly Regex PlaceholderRegex = new Regex(
            @"(\\r\\n|\\n|\\r|\\t|%\d*\$?[-+# 0,(]*\d*(?:\.\d+)?[a-zA-Z]|%\w|\{[^{}\r\n]+\}|\$\{[^{}\r\n]+\}|<[^<>\r\n]+>)",
            RegexOptions.Compiled);

        public static async Task<string> TranslateAsync(
            string sourceText,
            string sourceLanguage,
            string targetLanguage,
            bool preserveFormatting,
            Func<string, string, string, Task<string>> translateCoreAsync)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                return "";
            }

            if (!preserveFormatting)
            {
                return await translateCoreAsync(sourceText, sourceLanguage, targetLanguage);
            }

            var preserved = PreserveFormatting(sourceText);
            if (string.IsNullOrEmpty(preserved.MaskedCoreText))
            {
                return sourceText;
            }

            string translated = await translateCoreAsync(preserved.MaskedCoreText, sourceLanguage, targetLanguage);
            return preserved.Restore(translated);
        }

        private static PreservedTextFormat PreserveFormatting(string text)
        {
            string leadingWhitespace = Regex.Match(text ?? "", @"^\s+").Value;
            string trailingWhitespace = Regex.Match(text ?? "", @"\s+$").Value;
            int coreStartIndex = leadingWhitespace.Length;
            int coreLength = Math.Max(0, (text ?? "").Length - leadingWhitespace.Length - trailingWhitespace.Length);
            string coreText = (text ?? "").Substring(coreStartIndex, coreLength);

            if (string.IsNullOrEmpty(coreText))
            {
                return new PreservedTextFormat("", leadingWhitespace, trailingWhitespace, new Dictionary<string, string>());
            }

            int placeholderIndex = 0;
            var placeholders = new Dictionary<string, string>(StringComparer.Ordinal);
            string maskedText = PlaceholderRegex.Replace(
                coreText,
                match =>
                {
                    string placeholder = string.Format("__BXS_TOKEN_{0}__", placeholderIndex++);
                    placeholders[placeholder] = match.Value;
                    return placeholder;
                });

            return new PreservedTextFormat(maskedText, leadingWhitespace, trailingWhitespace, placeholders);
        }
    }

    internal sealed class GoogleTranslationService : ITranslationProvider
    {
        private readonly string apiKey;

        public GoogleTranslationService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("未提供谷歌 API Key。", nameof(apiKey));
            }

            this.apiKey = apiKey.Trim();
        }

        public TranslationProviderType ProviderType => TranslationProviderType.Google;

        public Task<string> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, bool preserveFormatting = true)
        {
            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                throw new InvalidOperationException("目标语言不能为空。");
            }

            return TranslationTextFormatter.TranslateAsync(sourceText, sourceLanguage, targetLanguage, preserveFormatting, TranslateCoreAsync);
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

        public BaiduTranslationService(string appId, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentException("未提供百度翻译所需的 AppId 或密钥。");
            }

            this.appId = appId.Trim();
            this.secretKey = secretKey.Trim();
        }

        public TranslationProviderType ProviderType => TranslationProviderType.Baidu;

        public Task<string> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, bool preserveFormatting = true)
        {
            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                throw new InvalidOperationException("目标语言不能为空。");
            }

            return TranslationTextFormatter.TranslateAsync(sourceText, sourceLanguage, targetLanguage, preserveFormatting, TranslateCoreAsync);
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

        public TencentTranslationService(string secretId, string secretKey, string region)
        {
            if (string.IsNullOrWhiteSpace(secretId) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentException("未提供腾讯翻译所需的 SecretId 或 SecretKey。");
            }

            this.secretId = secretId.Trim();
            this.secretKey = secretKey.Trim();
            this.region = string.IsNullOrWhiteSpace(region) ? "ap-beijing" : region.Trim();
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

            return TranslationTextFormatter.TranslateAsync(sourceText, sourceLanguage, targetLanguage, preserveFormatting, TranslateCoreAsync);
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
