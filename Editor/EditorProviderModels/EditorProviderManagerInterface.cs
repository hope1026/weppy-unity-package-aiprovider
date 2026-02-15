using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Chat.Editor
{
    public interface EditorProviderManagerInterface<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        List<TProviderType> GetProviders();
        bool IsProviderEnabled(TProviderType providerType_);
        void SetProviderEnabled(TProviderType providerType_, bool enabled_);
        List<TModelInfo> GetAllModelInfos(TProviderType providerType_);
        string GetPrimarySelectedModelId(TProviderType providerType_);
        IReadOnlyList<string> GetSelectedModelIds(TProviderType providerType_);
        void SetSelectedModelId(TProviderType providerType_, string modelId_);
        void SetSelectedModelIds(TProviderType providerType_, List<string> modelIds_);
    }
}
