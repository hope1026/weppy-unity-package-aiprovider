using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Chat.Editor
{

    [Serializable]
    public class DefaultChatPresetsJson
    {
        public List<ChatModelsJson> chatProviders = new List<ChatModelsJson>();
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
    }
}
