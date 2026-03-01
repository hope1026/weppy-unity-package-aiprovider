using System;
using System.Collections.Generic;
using System.Globalization;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class BgRemovalModelsJson
    {
        public string provider;
        public string modelsUrl;
        public List<BgRemovalModelJsonEntry> models = new List<BgRemovalModelJsonEntry>();
    }

    [Serializable]
    public class BgRemovalModelJsonEntry
    {
        public string id;
        public string displayName;
        public List<ModelInfoLocalizedText> descriptions;
        public string pricePerImage;
        public string contextWindow;
        public string maxOutputTokens;
        public string infoUrl;

        public BgRemovalModelJsonEntry() { }

        public BgRemovalModelJsonEntry(BgRemovalModelInfo info_)
        {
            id = info_.Id;
            displayName = info_.DisplayName;
            if (info_.Descriptions != null && info_.Descriptions.Count > 0)
            {
                descriptions = new List<ModelInfoLocalizedText>();
                foreach (ModelInfoLocalizedText text in info_.Descriptions)
                {
                    descriptions.Add(new ModelInfoLocalizedText(text.languageCode, text.value));
                }
            }
            pricePerImage = info_.GetPriceDisplay();
            contextWindow = info_.ContextWindowSize > 0
                ? info_.ContextWindowSize.ToString(CultureInfo.InvariantCulture)
                : "";
            maxOutputTokens = info_.MaxOutputTokens.HasValue
                ? info_.MaxOutputTokens.Value.ToString(CultureInfo.InvariantCulture)
                : "";
            infoUrl = info_.InfoUrl;
        }

        public BgRemovalModelInfo ToBgRemovalModelInfo(bool isCustom_ = false)
        {
            BgRemovalModelInfo info = new BgRemovalModelInfo(id, displayName, "", null, infoUrl, isCustom_);
            info.PricePerImageText = pricePerImage;
            info.NormalizePriceFromText();

            info.NormalizeTokenInfoFromText(contextWindow, maxOutputTokens);
            if (descriptions != null && descriptions.Count > 0)
            {
                info.Descriptions = new List<ModelInfoLocalizedText>();
                foreach (ModelInfoLocalizedText text in descriptions)
                {
                    info.Descriptions.Add(new ModelInfoLocalizedText(text.languageCode, text.value));
                }
            }
            return info;
        }
    }
}
