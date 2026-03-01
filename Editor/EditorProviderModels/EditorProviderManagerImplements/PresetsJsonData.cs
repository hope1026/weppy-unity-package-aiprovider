using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{

    [Serializable]
    public class DefaultChatPresetsJson
    {
        public List<ChatModelsJson> chatProviders = new List<ChatModelsJson>();
    }

    [Serializable]
    public class DefaultImagePresetsJson
    {
        public List<ImageModelsJson> imageProviders = new List<ImageModelsJson>();
    }

    [Serializable]
    public class DefaultBgRemovalPresetsJson
    {
        public List<BgRemovalModelsJson> BgRemovalProviders = new List<BgRemovalModelsJson>();
    }

    [Serializable]
    public class ModelEnableEntry
    {
        public string provider;
        public string modelId;

        public ModelEnableEntry() { }

        public ModelEnableEntry(string provider_, string modelId_)
        {
            provider = provider_;
            modelId = modelId_;
        }
    }

    [Serializable]
    public class CustomPresetsJson
    {
        public List<ChatModelsJson> customChatProviders = new List<ChatModelsJson>();
        public List<ImageModelsJson> customImageProviders = new List<ImageModelsJson>();
        public List<BgRemovalModelsJson> customBgRemovalProviders = new List<BgRemovalModelsJson>();
    }
}
