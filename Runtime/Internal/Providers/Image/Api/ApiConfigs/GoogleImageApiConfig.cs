using System.Collections.Generic;
using System.Linq;

namespace Weppy.AIProvider
{
    internal static class GoogleImageApiConfig
    {
        internal const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta";
        internal const string DEFAULT_GEMINI_IMAGE_MODEL = ImageModelPresets.GoogleGemini.GEMINI_25_FLASH_IMAGE;
        internal const string DEFAULT_IMAGEN_MODEL = ImageModelPresets.GoogleImagen.IMAGEN_4;

        internal static string GetGeminiImageUrl(string baseUrl_, string model_, string apiKey_)
        {
            return $"{baseUrl_}/models/{model_}:generateContent?key={apiKey_}";
        }

        internal static string GetImagenUrl(string baseUrl_, string model_, string apiKey_)
        {
            return $"{baseUrl_}/models/{model_}:predict?key={apiKey_}";
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
