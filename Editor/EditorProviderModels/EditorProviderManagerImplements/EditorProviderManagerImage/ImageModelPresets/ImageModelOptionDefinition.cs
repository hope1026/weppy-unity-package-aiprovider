using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class ImageModelOptionDefinition
    {
        public string type;
        public List<ModelInfoLocalizedText> labels;
        public List<ModelInfoLocalizedText> tooltips;
        public string apiParameterName;
        public List<ImageModelOptionValue> values = new List<ImageModelOptionValue>();

        public ImageModelOptionDefinition() { }

        public ImageModelOptionDefinition(string type_, string apiParameterName_)
        {
            type = type_;
            apiParameterName = apiParameterName_;
        }

        public void SetLabel(string languageCode_, string value_)
        {
            if (labels == null)
                labels = new List<ModelInfoLocalizedText>();

            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i].languageCode == languageCode_)
                {
                    labels[i].value = value_;
                    return;
                }
            }
            labels.Add(new ModelInfoLocalizedText(languageCode_, value_));
        }

        public void SetTooltip(string languageCode_, string value_)
        {
            if (tooltips == null)
                tooltips = new List<ModelInfoLocalizedText>();

            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].languageCode == languageCode_)
                {
                    tooltips[i].value = value_;
                    return;
                }
            }
            tooltips.Add(new ModelInfoLocalizedText(languageCode_, value_));
        }

        public string GetLabel(string languageCode_)
        {
            if (labels != null && labels.Count > 0)
            {
                foreach (ModelInfoLocalizedText text in labels)
                {
                    if (text.languageCode == languageCode_)
                        return text.value;
                }
                if (labels.Count > 0)
                    return labels[0].value;
            }
            return type;
        }

        public string GetTooltip(string languageCode_)
        {
            if (tooltips != null && tooltips.Count > 0)
            {
                foreach (ModelInfoLocalizedText text in tooltips)
                {
                    if (text.languageCode == languageCode_)
                        return text.value;
                }
                if (tooltips.Count > 0)
                    return tooltips[0].value;
            }
            return "";
        }

        public ImageModelOptionDefinition Clone()
        {
            ImageModelOptionDefinition clone = new ImageModelOptionDefinition(type, apiParameterName);
            if (labels != null)
            {
                clone.labels = new List<ModelInfoLocalizedText>();
                foreach (ModelInfoLocalizedText text in labels)
                {
                    clone.labels.Add(new ModelInfoLocalizedText(text.languageCode, text.value));
                }
            }
            if (tooltips != null)
            {
                clone.tooltips = new List<ModelInfoLocalizedText>();
                foreach (ModelInfoLocalizedText text in tooltips)
                {
                    clone.tooltips.Add(new ModelInfoLocalizedText(text.languageCode, text.value));
                }
            }
            foreach (ImageModelOptionValue optionValue in values)
            {
                clone.values.Add(optionValue.Clone());
            }
            return clone;
        }
    }
}
