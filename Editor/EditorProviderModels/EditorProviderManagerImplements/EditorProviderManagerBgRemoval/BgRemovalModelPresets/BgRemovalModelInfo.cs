using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class BgRemovalModelInfo
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public List<ModelInfoLocalizedText> Descriptions;
        public double? PricePerImage;
        public string PricePerImageText;
        public string InfoUrl;
        public bool IsCustom;
        public int ContextWindowSize;
        public int? MaxOutputTokens;

        public BgRemovalModelInfo() { }

        public BgRemovalModelInfo(string id_, string displayName_, string description_, double? pricePerImage_, string infoUrl_, bool isCustom_ = false)
        {
            Id = id_;
            DisplayName = displayName_;
            Description = description_;
            PricePerImage = pricePerImage_;
            InfoUrl = infoUrl_;
            IsCustom = isCustom_;
        }

        public void SetDescription(string languageCode_, string value_)
        {
            if (Descriptions == null)
                Descriptions = new List<ModelInfoLocalizedText>();

            for (int i = 0; i < Descriptions.Count; i++)
            {
                if (Descriptions[i].languageCode == languageCode_)
                {
                    Descriptions[i].value = value_;
                    return;
                }
            }
            Descriptions.Add(new ModelInfoLocalizedText(languageCode_, value_));
        }

        public string GetDescription(string languageCode_)
        {
            if (Descriptions != null && Descriptions.Count > 0)
            {
                foreach (ModelInfoLocalizedText text in Descriptions)
                {
                    if (text.languageCode == languageCode_)
                        return text.value;
                }
                if (Descriptions.Count > 0)
                    return Descriptions[0].value;
            }
            return Description ?? "";
        }

        public string GetPriceDisplay()
        {
            if (!string.IsNullOrEmpty(PricePerImageText))
                return PricePerImageText;

            if (!PricePerImage.HasValue)
                return "";

            if (PricePerImage.Value == 0)
                return "Free";

            return $"${PricePerImage.Value:0.###}/image";
        }

        public void NormalizePriceFromText()
        {
            if (!string.IsNullOrEmpty(PricePerImageText))
            {
                if (ModelInfoTextParser.TryParsePricePerImage(PricePerImageText, out double pricePerImage))
                    PricePerImage = pricePerImage;
            }
        }

        public void NormalizeTokenInfoFromText(string contextWindowText_, string maxOutputTokensText_)
        {
            if (!string.IsNullOrEmpty(contextWindowText_))
            {
                if (ModelInfoTextParser.TryParseTokenCount(contextWindowText_, out int contextWindowSize))
                    ContextWindowSize = contextWindowSize;
            }

            if (!string.IsNullOrEmpty(maxOutputTokensText_))
            {
                if (ModelInfoTextParser.TryParseTokenCount(maxOutputTokensText_, out int maxOutputTokens))
                    MaxOutputTokens = maxOutputTokens;
            }
        }

        public BgRemovalModelInfo Clone()
        {
            BgRemovalModelInfo clone = new BgRemovalModelInfo();
            clone.Id = Id;
            clone.DisplayName = DisplayName;
            clone.Description = Description;
            if (Descriptions != null)
            {
                clone.Descriptions = new List<ModelInfoLocalizedText>();
                foreach (ModelInfoLocalizedText text in Descriptions)
                {
                    clone.Descriptions.Add(new ModelInfoLocalizedText(text.languageCode, text.value));
                }
            }
            clone.PricePerImage = PricePerImage;
            clone.PricePerImageText = PricePerImageText;
            clone.InfoUrl = InfoUrl;
            clone.IsCustom = IsCustom;
            clone.ContextWindowSize = ContextWindowSize;
            clone.MaxOutputTokens = MaxOutputTokens;
            return clone;
        }
    }
}
