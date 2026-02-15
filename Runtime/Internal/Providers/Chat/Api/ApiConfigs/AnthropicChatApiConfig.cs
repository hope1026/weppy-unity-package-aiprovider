using System.Collections.Generic;
using System.Linq;

namespace Weppy.AIProvider.Chat
{
    internal static class AnthropicChatApiConfig
    {
        internal const string BASE_URL = "https://api.anthropic.com/v1";
        internal const string CHAT_ENDPOINT = "/messages";
        internal const string API_VERSION = "2023-06-01";
        internal const string DEFAULT_CHAT_MODEL = ChatModelPresets.Anthropic.CLAUDE_SONNET_45;
        internal const int DEFAULT_MAX_TOKENS = 4096;

        internal static Dictionary<string, string> GetAuthHeaders(string apiKey_, Dictionary<string, string> customHeaders_ = null)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                ["x-api-key"] = SanitizeHeaderValue(apiKey_),
                ["anthropic-version"] = API_VERSION
            };

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
