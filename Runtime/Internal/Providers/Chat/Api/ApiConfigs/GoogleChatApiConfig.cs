using System.Collections.Generic;
using System.Linq;

namespace Weppy.AIProvider.Chat
{
    internal static class GoogleChatApiConfig
    {
        internal const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta";
        internal const string DEFAULT_CHAT_MODEL = ChatModelPresets.Google.GEMINI_25_FLASH;

        internal static string GetChatUrl(string baseUrl_, string model_, string apiKey_, bool stream_ = false)
        {
            string action = stream_ ? "streamGenerateContent" : "generateContent";
            string streamParam = stream_ ? "alt=sse&" : "";
            return $"{baseUrl_}/models/{model_}:{action}?{streamParam}key={apiKey_}";
        }

        internal static Dictionary<string, string> GetHeaders(Dictionary<string, string> customHeaders_ = null)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (customHeaders_ != null)
            {
                foreach (KeyValuePair<string, string> header in customHeaders_)
                    headers[header.Key] = SanitizeHeaderValue(header.Value);
            }

            return headers;
        }

        private static string SanitizeHeaderValue(string value_)
        {
            if (string.IsNullOrEmpty(value_))
                return value_;

            if (IsAscii(value_))
                return value_;

            return System.Uri.EscapeDataString(value_);
        }

        private static bool IsAscii(string value_)
        {
            return value_.All(c => c < 128);
        }
    }
}
