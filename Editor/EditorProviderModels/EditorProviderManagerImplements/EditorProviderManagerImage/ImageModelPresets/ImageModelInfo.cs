using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class ImageModelInfo
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public List<ModelInfoLocalizedText> Descriptions;
        public double? PricePerImage;
        public string PricePerImageText;
        public string InfoUrl;
        public bool IsCustom;
        public List<ImageModelOptionDefinition> Options;
        public int ContextWindowSize;
        public int? MaxOutputTokens;

        public ImageModelInfo() { }

        public ImageModelInfo(string id_, string displayName_, string description_, double? pricePerImage_, string infoUrl_, bool isCustom_ = false)
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

        public string GetTooltip()
        {
            string tooltip = DisplayName;
            if (!string.IsNullOrEmpty(Description))
                tooltip += $"\n{Description}";
            
            string price = GetPriceDisplay();
            if (!string.IsNullOrEmpty(price))
                tooltip += $"\n{price}";
            return tooltip;
        }

        public bool HasOption(string optionType_)
        {
            if (Options == null)
                return false;
            foreach (ImageModelOptionDefinition option in Options)
            {
                if (option.type == optionType_)
                    return true;
            }
            return false;
        }

        public ImageModelOptionDefinition GetOption(string optionType_)
        {
            if (Options == null)
                return null;
            foreach (ImageModelOptionDefinition option in Options)
            {
                if (option.type == optionType_)
                    return option;
            }
            return null;
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

        public ImageModelInfo Clone()
        {
            ImageModelInfo clone = new ImageModelInfo();
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
            if (Options != null)
            {
                clone.Options = new List<ImageModelOptionDefinition>();
                foreach (ImageModelOptionDefinition option in Options)
                {
                    clone.Options.Add(option.Clone());
                }
            }
            return clone;
        }
    }
}
