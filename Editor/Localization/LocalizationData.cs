using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    [Serializable]
    public class LocalizationData
    {
        public string languageCode;
        public string languageDisplayName;
        public List<LocalizationEntry> strings;
    }
}
