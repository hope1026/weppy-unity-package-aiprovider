using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    public enum ProviderExecutionPathType
    {
        NONE = 0,
        CLI,
        APP
    }

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

    public interface EditorProviderExecutionPathSupport<TProviderType>
        where TProviderType : struct, Enum
    {
        ProviderExecutionPathType GetExecutionPathType(TProviderType providerType_);
        string GetExecutablePath(TProviderType providerType_);
        void SetExecutablePath(TProviderType providerType_, string path_);
        string AutoDetectExecutablePath(TProviderType providerType_);
        string GetExecutableInstallGuideUrl(TProviderType providerType_);
        bool SupportsNodeExecutablePath(TProviderType providerType_);
        string GetNodeExecutablePath(TProviderType providerType_);
        void SetNodeExecutablePath(TProviderType providerType_, string path_);
        string AutoDetectNodeExecutablePath(TProviderType providerType_);
    }
}
