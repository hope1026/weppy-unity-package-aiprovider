using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class ChatModelInfo
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public List<ModelInfoLocalizedText> Descriptions;
        public double? InputPricePerMillion;
        public double? OutputPricePerMillion;
        public int ContextWindowSize;
        public int? MaxOutputTokens;
        public string InputPriceText;
        public string OutputPriceText;
        public string InfoUrl;
        public bool IsCustom;

        public ChatModelInfo() { }

        public ChatModelInfo(
            string id_,
            string displayName_,
            string description_,
            double? inputPricePerMillion_,
            double? outputPricePerMillion_,
            int contextWindowSize_,
            int? maxOutputTokens_,
            string infoUrl_,
            bool isCustom_ = false)
        {
            Id = id_;
            DisplayName = displayName_;
            Description = description_;
            InputPricePerMillion = inputPricePerMillion_;
            OutputPricePerMillion = outputPricePerMillion_;
            ContextWindowSize = contextWindowSize_;
            MaxOutputTokens = maxOutputTokens_;
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

        public string GetInputPriceDisplay()
        {
            if (!string.IsNullOrEmpty(InputPriceText))
                return InputPriceText;

            if (!InputPricePerMillion.HasValue)
                return "";

            if (InputPricePerMillion.Value == 0)
                return "Free";

            return $"${InputPricePerMillion.Value:0.##}/1M";
        }

        public string GetOutputPriceDisplay()
        {
            if (!string.IsNullOrEmpty(OutputPriceText))
                return OutputPriceText;

            if (!OutputPricePerMillion.HasValue)
                return "";

            if (OutputPricePerMillion.Value == 0)
                return "Free";

            return $"${OutputPricePerMillion.Value:0.##}/1M";
        }

        public string GetPriceDisplay()
        {
            string inputPrice = GetInputPriceDisplay();
            string outputPrice = GetOutputPriceDisplay();

            if (string.IsNullOrEmpty(inputPrice) && string.IsNullOrEmpty(outputPrice))
                return "";

            if (!string.IsNullOrEmpty(inputPrice) && !string.IsNullOrEmpty(outputPrice))
                return $"In: {inputPrice} / Out: {outputPrice}";

            if (!string.IsNullOrEmpty(inputPrice))
                return inputPrice;

            return outputPrice;
        }

        public string GetContextWindowDisplay()
        {
            if (ContextWindowSize <= 0)
                return "";

            return FormatTokenCount(ContextWindowSize);
        }

        public string GetMaxOutputDisplay()
        {
            if (!MaxOutputTokens.HasValue || MaxOutputTokens.Value <= 0)
                return "";

            return FormatTokenCount(MaxOutputTokens.Value);
        }

        public string GetContextAndMaxOutputDisplay()
        {
            string context = GetContextWindowDisplay();
            string maxOutput = GetMaxOutputDisplay();

            if (string.IsNullOrEmpty(context) && string.IsNullOrEmpty(maxOutput))
                return "";

            if (!string.IsNullOrEmpty(context) && !string.IsNullOrEmpty(maxOutput))
                return $"Ctx: {context} / Max: {maxOutput}";

            if (!string.IsNullOrEmpty(context))
                return $"Ctx: {context}";

            return $"Max: {maxOutput}";
        }

        public string GetTooltip()
        {
            string tooltip = DisplayName;
            if (!string.IsNullOrEmpty(Description))
                tooltip += $"\n{Description}";

            string contextWindow = GetContextWindowDisplay();
            if (!string.IsNullOrEmpty(contextWindow))
                tooltip += $"\nContext: {contextWindow}";

            string maxOutput = GetMaxOutputDisplay();
            if (!string.IsNullOrEmpty(maxOutput))
                tooltip += $"\nMax Output: {maxOutput}";

            string price = GetPriceDisplay();
            if (!string.IsNullOrEmpty(price))
                tooltip += $"\n{price}";

            return tooltip;
        }

        private string FormatTokenCount(int tokens_)
        {
            if (tokens_ >= 1000000)
                return $"{tokens_ / 1000000.0:0.#}M tokens";
            if (tokens_ >= 1000)
                return $"{tokens_ / 1000.0:0.#}K tokens";
            return $"{tokens_} tokens";
        }

        public void NormalizePriceFromText()
        {
            if (!string.IsNullOrEmpty(InputPriceText))
            {
                if (ModelInfoTextParser.TryParsePricePerMillion(InputPriceText, out double inputPricePerMillion))
                    InputPricePerMillion = inputPricePerMillion;
            }

            if (!string.IsNullOrEmpty(OutputPriceText))
            {
                if (ModelInfoTextParser.TryParsePricePerMillion(OutputPriceText, out double outputPricePerMillion))
                    OutputPricePerMillion = outputPricePerMillion;
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

        public ChatModelInfo Clone()
        {
            ChatModelInfo clone = new ChatModelInfo();
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
            clone.InputPricePerMillion = InputPricePerMillion;
            clone.OutputPricePerMillion = OutputPricePerMillion;
            clone.ContextWindowSize = ContextWindowSize;
            clone.MaxOutputTokens = MaxOutputTokens;
            clone.InputPriceText = InputPriceText;
            clone.OutputPriceText = OutputPriceText;
            clone.InfoUrl = InfoUrl;
            clone.IsCustom = IsCustom;
            return clone;
        }
    }
}
