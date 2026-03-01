using System;
using System.Globalization;

namespace Weppy.AIProvider.Editor
{
    public static class ModelInfoTextParser
    {
        private static readonly NumberStyles NumberStylesInvariant = NumberStyles.Float | NumberStyles.AllowThousands;

        public static bool TryParsePricePerMillion(string text_, out double pricePerMillion_)
        {
            pricePerMillion_ = 0;
            if (string.IsNullOrEmpty(text_))
                return false;

            string trimmed = text_.Trim();
            if (trimmed.IndexOf("free", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pricePerMillion_ = 0;
                return true;
            }

            if (!TryExtractFirstNumber(trimmed, out double value, out int endIndex))
                return false;

            string suffix = trimmed.Substring(endIndex + 1).ToLowerInvariant();
            if (suffix.Contains("/1k") || suffix.Contains("per 1k"))
            {
                pricePerMillion_ = value * 1000;
                return true;
            }

            pricePerMillion_ = value;
            return true;
        }

        public static bool TryParsePricePerImage(string text_, out double pricePerImage_)
        {
            pricePerImage_ = 0;
            if (string.IsNullOrEmpty(text_))
                return false;

            string trimmed = text_.Trim();
            if (trimmed.IndexOf("free", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pricePerImage_ = 0;
                return true;
            }

            if (!TryExtractFirstNumber(trimmed, out double value, out int endIndex))
                return false;

            pricePerImage_ = value;
            return true;
        }

        public static bool TryParseTokenCount(string text_, out int tokenCount_)
        {
            tokenCount_ = 0;
            if (string.IsNullOrEmpty(text_))
                return false;

            string trimmed = text_.Trim();
            if (!TryExtractFirstNumber(trimmed, out double value, out int endIndex))
                return false;

            string suffix = trimmed.Substring(endIndex + 1).TrimStart();
            if (suffix.StartsWith("k", StringComparison.OrdinalIgnoreCase))
                value *= 1000;
            else if (suffix.StartsWith("m", StringComparison.OrdinalIgnoreCase))
                value *= 1000000;

            tokenCount_ = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            return true;
        }

        private static bool TryExtractFirstNumber(string text_, out double value_, out int endIndex_)
        {
            value_ = 0;
            endIndex_ = -1;
            if (string.IsNullOrEmpty(text_))
                return false;

            int startIndex = -1;
            for (int i = 0; i < text_.Length; i++)
            {
                char ch = text_[i];
                if (char.IsDigit(ch) || ch == '.' || ch == '-')
                {
                    if (startIndex < 0)
                        startIndex = i;
                    endIndex_ = i;
                }
                else if (startIndex >= 0)
                {
                    break;
                }
            }

            if (startIndex < 0 || endIndex_ < startIndex)
                return false;

            string numberText = text_.Substring(startIndex, endIndex_ - startIndex + 1);
            numberText = numberText.Replace(",", "");
            return double.TryParse(numberText, NumberStylesInvariant, CultureInfo.InvariantCulture, out value_);
        }
    }
}
