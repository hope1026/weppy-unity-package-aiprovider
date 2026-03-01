using System;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class ModelInfoLocalizedText
    {
        public string languageCode;
        public string value;

        public ModelInfoLocalizedText() { }

        public ModelInfoLocalizedText(string languageCode_, string value_)
        {
            languageCode = languageCode_;
            value = value_;
        }
    }
}
