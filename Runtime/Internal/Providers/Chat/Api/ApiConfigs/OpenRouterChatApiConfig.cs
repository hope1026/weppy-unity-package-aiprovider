using System.Collections.Generic;
using System.Linq;

namespace Weppy.AIProvider
{
    internal static class OpenRouterChatApiConfig
    {
        internal const string BASE_URL = "https://openrouter.ai/api/v1";
        internal const string CHAT_ENDPOINT = "/chat/completions";
        internal const string DEFAULT_CHAT_MODEL = "openai/gpt-3.5-turbo";

        internal static Dictionary<string, string> GetAuthHeaders(
            string apiKey_,
            string httpReferer_ = null,
            string appTitle_ = null)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {SanitizeHeaderValue(apiKey_)}"
            };

            if (!string.IsNullOrEmpty(httpReferer_))
                headers["HTTP-Referer"] = SanitizeHeaderValue(httpReferer_);

            if (!string.IsNullOrEmpty(appTitle_))
                headers["X-Title"] = SanitizeHeaderValue(appTitle_);

            return headers;
        }

        internal static Dictionary<string, string> GetAuthHeaders(
            string apiKey_,
            string httpReferer_,
            string appTitle_,
            Dictionary<string, string> customHeaders_)
        {
            Dictionary<string, string> headers = GetAuthHeaders(apiKey_, httpReferer_, appTitle_);

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
