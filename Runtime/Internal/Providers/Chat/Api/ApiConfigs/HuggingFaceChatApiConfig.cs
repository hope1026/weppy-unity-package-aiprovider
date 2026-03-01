using System.Collections.Generic;
using System.Linq;

namespace Weppy.AIProvider
{
    internal static class HuggingFaceChatApiConfig
    {
        internal const string BASE_URL = "https://router.huggingface.co/v1";
        internal const string CHAT_ENDPOINT = "/chat/completions";
        internal const string DEFAULT_CHAT_MODEL = ChatModelPresets.HuggingFace.QWEN_25_72B_INSTRUCT;

        internal static Dictionary<string, string> GetAuthHeaders(string apiKey_)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {SanitizeHeaderValue(apiKey_)}"
            };

            return headers;
        }

        internal static Dictionary<string, string> GetAuthHeaders(
            string apiKey_,
            Dictionary<string, string> customHeaders_)
        {
            Dictionary<string, string> headers = GetAuthHeaders(apiKey_);

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
