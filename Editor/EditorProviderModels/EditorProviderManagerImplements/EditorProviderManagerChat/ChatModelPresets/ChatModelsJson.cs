using System;
using System.Collections.Generic;
using System.Globalization;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class ChatModelsJson
    {
        public string provider;
        public List<ChatModelJsonEntry> models = new List<ChatModelJsonEntry>();
    }

    [Serializable]
    public class ChatModelJsonEntry
    {
        public string id;
        public string displayName;
        public List<ModelInfoLocalizedText> descriptions;
        public string inputPrice;
        public string outputPrice;
        public string contextWindow;
        public string maxOutputTokens;
        public string infoUrl;

        public ChatModelJsonEntry() { }

        public ChatModelJsonEntry(ChatModelInfo info_)
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
            inputPrice = info_.GetInputPriceDisplay();
            outputPrice = info_.GetOutputPriceDisplay();
            contextWindow = info_.ContextWindowSize > 0
                ? info_.ContextWindowSize.ToString(CultureInfo.InvariantCulture)
                : "";
            maxOutputTokens = info_.MaxOutputTokens.HasValue
                ? info_.MaxOutputTokens.Value.ToString(CultureInfo.InvariantCulture)
                : "";
            infoUrl = info_.InfoUrl;
        }

        public ChatModelInfo ToProviderModelInfo(bool isCustom_ = false)
        {
            ChatModelInfo info = new ChatModelInfo(
                id,
                displayName,
                "",
                null,
                null,
                0,
                null,
                infoUrl,
                isCustom_);
            info.InputPriceText = inputPrice;
            info.OutputPriceText = outputPrice;
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
