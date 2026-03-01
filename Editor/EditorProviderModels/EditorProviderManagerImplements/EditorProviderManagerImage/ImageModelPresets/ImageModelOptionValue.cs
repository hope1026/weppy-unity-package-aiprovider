using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class ImageModelOptionValue
    {
        public string value;
        public string displayName;
        public List<ModelInfoLocalizedText> descriptions;

        public ImageModelOptionValue() { }

        public ImageModelOptionValue(string value_, string displayName_)
        {
            value = value_;
            displayName = displayName_;
        }

        public void SetDescription(string languageCode_, string value_)
        {
            if (descriptions == null)
                descriptions = new List<ModelInfoLocalizedText>();

            for (int i = 0; i < descriptions.Count; i++)
            {
                if (descriptions[i].languageCode == languageCode_)
                {
                    descriptions[i].value = value_;
                    return;
                }
            }
            descriptions.Add(new ModelInfoLocalizedText(languageCode_, value_));
        }

        public string GetDescription(string languageCode_)
        {
            if (descriptions != null && descriptions.Count > 0)
            {
                foreach (ModelInfoLocalizedText text in descriptions)
                {
                    if (text.languageCode == languageCode_)
                        return text.value;
                }
                if (descriptions.Count > 0)
                    return descriptions[0].value;
            }
            return "";
        }

        public ImageModelOptionValue Clone()
        {
            ImageModelOptionValue clone = new ImageModelOptionValue(value, displayName);
            if (descriptions != null)
            {
                clone.descriptions = new List<ModelInfoLocalizedText>();
                foreach (ModelInfoLocalizedText text in descriptions)
                {
                    clone.descriptions.Add(new ModelInfoLocalizedText(text.languageCode, text.value));
                }
            }
            return clone;
        }
    }
}
