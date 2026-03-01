using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    public class ProviderModelSelectionConfig<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        public string RootContainerClassName;
        public string ProviderOrderStorageKey;
        public string SectionUssPath;

        public Func<TProviderType, bool> IsNoneProvider;
        public Func<TProviderType, bool> HasProviderAuth;
        public Func<TProviderType, bool> IsProviderEnabled;
        public Func<TProviderType, bool> IsApiKeyOptional;
        public Func<TProviderType, bool> IsApiKeyEnabled;
        public Action<TProviderType, bool> SetApiKeyEnabled;
        public Action<TProviderType, int> SetProviderPriority;
        public Func<int, string> GetSelectionButtonText;
        public Func<TProviderType, List<TModelInfo>> GetAllModelInfos;
        public Func<TProviderType, string> GetPrimarySelectedModelId;
        public Action<TProviderType, string> SaveSelectedModelId;
        public Func<TProviderType, IReadOnlyList<string>> GetSelectedModelIds;
        public Action<TProviderType, List<string>> SaveSelectedModelIds;
        public Func<TProviderType, string, TModelInfo> GetModelInfo;
        public Func<TModelInfo, string, string> GetModelDisplayText;
        public Func<TModelInfo, string, string> GetModelDisplayName;
        public Func<TModelInfo, string, string> GetModelDescription;
        public Func<TModelInfo, string> GetModelPriceDisplay;
        public Func<TProviderType, string> GetNoModelsEnabledLabelText;
        public Func<TProviderType, string> GetModelsUrl;
        public Action NotifyProviderOrderChanged;
        public Action<TProviderType, bool> NotifyProviderEnabledChanged;
        public Action<TProviderType, string> NotifyModelChanged;
        public Func<TModelInfo, string> GetModelIdFromInfo;
        public Func<TModelInfo, int> GetContextWindowSize;
        public Func<TModelInfo, int?> GetMaxOutputTokens;
        public Func<TModelInfo, double> GetModelPrice;
        public Action<TProviderType, TModelInfo> AddCustomModel;
        public Func<TProviderType, TModelInfo> CreateCustomModelInfo;
        public Action<TProviderType, string> RemoveCustomModel;
    }
}
