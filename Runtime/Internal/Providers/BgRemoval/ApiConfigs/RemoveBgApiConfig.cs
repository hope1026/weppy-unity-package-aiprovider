using System.Collections.Generic;
using System.Linq;

namespace Weppy.AIProvider
{
    internal static class RemoveBgApiConfig
    {
        internal const string API_URL = "https://api.remove.bg/v1.0/removebg";
        internal const string API_KEY_HEADER = "X-Api-Key";

        internal const string DEFAULT_SIZE = "auto";
        internal const string DEFAULT_FORMAT = "png";

        internal static Dictionary<string, string> GetAuthHeaders(string apiKey_)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                [API_KEY_HEADER] = SanitizeHeaderValue(apiKey_)
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
