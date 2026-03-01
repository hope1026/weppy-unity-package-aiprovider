using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ProviderModelSelectionUtilityService<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;

        public ProviderModelSelectionUtilityService(ProviderModelSelectionConfig<TProviderType, TModelInfo> config_)
        {
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
        }

        public VisualElement GetRootVisualContainer(VisualElement root_)
        {
            VisualElement current = root_;
            while (current != null)
            {
                if (!string.IsNullOrEmpty(_config.RootContainerClassName) &&
                    current.ClassListContains(_config.RootContainerClassName))
                {
                    return current;
                }

                if (!string.IsNullOrEmpty(_config.RootContainerClassName))
                {
                    VisualElement foundContainer = current.Q(className: _config.RootContainerClassName);
                    if (foundContainer != null)
                    {
                        return foundContainer;
                    }
                }

                current = current.parent;
            }

            if (root_.panel?.visualTree != null)
            {
                if (!string.IsNullOrEmpty(_config.RootContainerClassName))
                {
                    VisualElement containerFromPanel = root_.panel.visualTree.Q(className: _config.RootContainerClassName);
                    if (containerFromPanel != null)
                    {
                        return containerFromPanel;
                    }
                }

                return root_.panel.visualTree;
            }

            return null;
        }

        public string BuildModelSearchText(string modelId_, string displayName_, string description_, TModelInfo modelInfo_, string languageCode_)
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(modelId_))
                parts.Add(modelId_);
            if (!string.IsNullOrEmpty(displayName_))
                parts.Add(displayName_);
            if (!string.IsNullOrEmpty(description_))
                parts.Add(description_);

            string displayText = _config.GetModelDisplayText != null
                ? _config.GetModelDisplayText(modelInfo_, languageCode_)
                : string.Empty;
            if (!string.IsNullOrEmpty(displayText))
                parts.Add(displayText);

            return string.Join(" ", parts);
        }

        public bool MatchesSearch(string haystack_, string needle_)
        {
            if (string.IsNullOrEmpty(needle_))
                return true;

            if (string.IsNullOrEmpty(haystack_))
                return false;

            return haystack_.IndexOf(needle_, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
