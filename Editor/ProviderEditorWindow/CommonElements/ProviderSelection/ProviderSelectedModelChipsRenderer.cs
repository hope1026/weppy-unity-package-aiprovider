using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ProviderSelectedModelChipsRenderer<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;

        public ProviderSelectedModelChipsRenderer(ProviderModelSelectionConfig<TProviderType, TModelInfo> config_)
        {
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
        }

        public int Render(
            VisualElement chipsContainer_,
            DragDropReorderController<TProviderType> dragDropController_,
            string currentLanguageCode_,
            Func<TProviderType, bool> hasProviderAuth_,
            Func<TProviderType, bool> isProviderEnabled_,
            Func<TProviderType, List<string>> getSelectedModelIds_,
            Func<TProviderType, HashSet<string>> getDisabledModelIds_,
            Action<TProviderType, string> toggleModelActiveState_,
            Action refreshAfterToggle_)
        {
            if (chipsContainer_ == null)
                return 0;

            chipsContainer_.Clear();

            if (dragDropController_ == null)
                return 0;

            IReadOnlyList<TProviderType> providerOrder = dragDropController_.ItemOrder;

            List<(TProviderType providerType, int originalIndex, HashSet<string> allModelIds)> validProviders =
                new List<(TProviderType, int, HashSet<string>)>();

            for (int i = 0; i < providerOrder.Count; i++)
            {
                TProviderType providerType = providerOrder[i];

                bool hasProviderAuth = hasProviderAuth_ != null && hasProviderAuth_(providerType);
                if (!hasProviderAuth)
                    continue;

                bool isProviderEnabled = isProviderEnabled_ == null || isProviderEnabled_(providerType);
                if (!isProviderEnabled)
                    continue;

                List<string> selectedModelIds = getSelectedModelIds_ != null
                    ? getSelectedModelIds_(providerType)
                    : new List<string>();

                HashSet<string> disabledModelIds = getDisabledModelIds_ != null
                    ? getDisabledModelIds_(providerType)
                    : new HashSet<string>();

                HashSet<string> allModelIds = new HashSet<string>(selectedModelIds);
                allModelIds.UnionWith(disabledModelIds);

                if (allModelIds.Count == 0)
                    continue;

                validProviders.Add((providerType, i, allModelIds));
            }

            validProviders.Sort((a_, b_) => a_.originalIndex.CompareTo(b_.originalIndex));

            int selectedCount = 0;

            foreach ((TProviderType providerType, int originalIndex, HashSet<string> allModelIds) item in validProviders)
            {
                TProviderType providerType = item.providerType;

                int priority = dragDropController_.GetPriorityForIndex(item.originalIndex);
                HashSet<string> disabledModelIds = getDisabledModelIds_ != null
                    ? getDisabledModelIds_(providerType)
                    : new HashSet<string>();

                foreach (string modelId in item.allModelIds)
                {
                    bool isModelDisabled = disabledModelIds.Contains(modelId);

                    if (!isModelDisabled)
                    {
                        selectedCount++;
                    }

                    TModelInfo modelInfo = _config.GetModelInfo?.Invoke(providerType, modelId);
                    string displayName = _config.GetModelDisplayName?.Invoke(modelInfo, modelId) ?? modelId;
                    string chipLabelText = $"{providerType} - {displayName}";

                    VisualElement chip = new VisualElement();
                    chip.AddToClassList("model-chip");

                    if (isModelDisabled)
                    {
                        chip.AddToClassList("model-chip--disabled");
                    }

                    string tooltipText = string.Empty;
                    if (modelInfo != null)
                    {
                        string description = _config.GetModelDescription?.Invoke(modelInfo, currentLanguageCode_);
                        if (!string.IsNullOrEmpty(description))
                        {
                            tooltipText = description;
                        }
                    }

                    string toggleHint = LocalizationManager.Get(LocalizationKeys.COMMON_MODEL_SELECTION);
                    chip.tooltip = string.IsNullOrEmpty(tooltipText) ? toggleHint : $"{tooltipText}\n{toggleHint}";

                    TProviderType capturedProviderType = providerType;
                    string capturedModelId = modelId;
                    chip.RegisterCallback<ClickEvent>(evt_ =>
                    {
                        toggleModelActiveState_?.Invoke(capturedProviderType, capturedModelId);
                        refreshAfterToggle_?.Invoke();
                        evt_.StopPropagation();
                    });

                    Label nameLabel = new Label(chipLabelText);
                    nameLabel.AddToClassList("model-chip-label");
                    chip.Add(nameLabel);

                    Label priorityLabel = new Label($"#{priority}");
                    priorityLabel.AddToClassList("model-chip-priority");
                    chip.Add(priorityLabel);

                    chipsContainer_.Add(chip);
                }
            }

            return selectedCount;
        }
    }
}
