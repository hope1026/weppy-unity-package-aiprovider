using System.Collections.Generic;
using System.Linq;

namespace Weppy.AIProvider
{
    internal static class OpenAIImageApiConfig
    {
        internal const string BASE_URL = "https://api.openai.com/v1";
        internal const string IMAGE_ENDPOINT = "/images/generations";
        internal const string DEFAULT_IMAGE_MODEL = ImageModelPresets.OpenAI.GPT_IMAGE_1;

        internal static Dictionary<string, string> GetAuthHeaders(string apiKey_, string organizationId_ = null)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {SanitizeHeaderValue(apiKey_)}"
            };

            if (!string.IsNullOrEmpty(organizationId_))
                headers["OpenAI-Organization"] = SanitizeHeaderValue(organizationId_);

            return headers;
        }

        internal static Dictionary<string, string> GetAuthHeaders(
            string apiKey_,
            string organizationId_,
            Dictionary<string, string> customHeaders_)
        {
            Dictionary<string, string> headers = GetAuthHeaders(apiKey_, organizationId_);

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
