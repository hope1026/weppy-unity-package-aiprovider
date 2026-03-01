using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ImageDynamicOptionRenderer
    {
        private VisualElement _container;
        private string _languageCode;
        private Dictionary<string, DropdownField> _optionDropdowns = new Dictionary<string, DropdownField>();
        private Dictionary<string, TextField> _customInputFields = new Dictionary<string, TextField>();
        private Dictionary<string, string> _selectedValues = new Dictionary<string, string>();
        private Dictionary<string, ImageModelOptionDefinition> _mergedOptions = new Dictionary<string, ImageModelOptionDefinition>();
        private Dictionary<string, List<ImageModelInfo>> _optionSupportedModels = new Dictionary<string, List<ImageModelInfo>>();

        public ImageDynamicOptionRenderer(VisualElement container_, string languageCode_)
        {
            _container = container_;
            _languageCode = languageCode_;
        }

        public void SetLanguage(string languageCode_)
        {
            _languageCode = languageCode_;
            RefreshLabels();
        }

        public void RenderOptions(IReadOnlyList<ImageModelInfo> activeModels_)
        {
            _container.Clear();
            _optionDropdowns.Clear();
            _customInputFields.Clear();
            _mergedOptions.Clear();
            _optionSupportedModels.Clear();

            if (activeModels_ == null || activeModels_.Count == 0)
            {
                Label noOptionsLabel = new Label(LocalizationManager.Get(LocalizationKeys.OPTION_NO_ACTIVE_MODELS));
                noOptionsLabel.AddToClassList("no-options-label");
                _container.Add(noOptionsLabel);
                return;
            }

            MergeOptions(activeModels_);

            if (_mergedOptions.Count == 0)
            {
                Label noOptionsLabel = new Label(LocalizationManager.Get(LocalizationKeys.OPTION_NO_CONFIGURABLE_OPTIONS));
                noOptionsLabel.AddToClassList("no-options-label");
                _container.Add(noOptionsLabel);
                return;
            }

            // Create a grid container for options
            VisualElement optionsGrid = new VisualElement();
            optionsGrid.AddToClassList("options-grid");
            _container.Add(optionsGrid);

            foreach (KeyValuePair<string, ImageModelOptionDefinition> kvp in _mergedOptions)
            {
                VisualElement optionSection = CreateOptionSection(kvp.Value, activeModels_);
                optionsGrid.Add(optionSection);
            }
        }

        private void MergeOptions(IReadOnlyList<ImageModelInfo> activeModels_)
        {
            // Track which models support which options
            Dictionary<string, HashSet<ImageModelInfo>> optionToModels = new Dictionary<string, HashSet<ImageModelInfo>>();

            foreach (ImageModelInfo model in activeModels_)
            {
                if (model.Options == null)
                    continue;

                foreach (ImageModelOptionDefinition option in model.Options)
                {
                    if (!_mergedOptions.ContainsKey(option.type))
                    {
                        _mergedOptions[option.type] = option.Clone();
                        optionToModels[option.type] = new HashSet<ImageModelInfo>();
                    }
                    else
                    {
                        MergeOptionValues(_mergedOptions[option.type], option);
                    }

                    optionToModels[option.type].Add(model);
                }
            }

            // Store unique models for each option (remove duplicates by model ID)
            foreach (KeyValuePair<string, HashSet<ImageModelInfo>> kvp in optionToModels)
            {
                _optionSupportedModels[kvp.Key] = GetUniqueModels(kvp.Value.ToList());
            }
        }

        private List<ImageModelInfo> GetUniqueModels(List<ImageModelInfo> models_)
        {
            Dictionary<string, ImageModelInfo> uniqueModels = new Dictionary<string, ImageModelInfo>();

            foreach (ImageModelInfo model in models_)
            {
                if (!uniqueModels.ContainsKey(model.Id))
                {
                    uniqueModels[model.Id] = model;
                }
            }

            return uniqueModels.Values.ToList();
        }

        private void MergeOptionValues(ImageModelOptionDefinition target_, ImageModelOptionDefinition source_)
        {
            HashSet<string> existingValues = new HashSet<string>();
            foreach (ImageModelOptionValue value in target_.values)
            {
                existingValues.Add(value.value);
            }

            foreach (ImageModelOptionValue sourceValue in source_.values)
            {
                if (!existingValues.Contains(sourceValue.value))
                {
                    target_.values.Add(sourceValue.Clone());
                }
            }
        }

        private VisualElement CreateOptionSection(ImageModelOptionDefinition generationModelOption_, IReadOnlyList<ImageModelInfo> activeModels_)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("option-section");

            // Header with label and model tags
            VisualElement headerRow = new VisualElement();
            headerRow.AddToClassList("option-header-row");

            Label label = new Label(generationModelOption_.GetLabel(_languageCode));
            label.AddToClassList("option-label");
            label.tooltip = generationModelOption_.GetTooltip(_languageCode);
            headerRow.Add(label);

            // Add model tags (show only selected models that support this option)
            if (_optionSupportedModels.TryGetValue(generationModelOption_.type, out List<ImageModelInfo> supportedModels))
            {
                List<ImageModelInfo> selectedSupportedModels = GetSelectedModelsForOption(supportedModels, activeModels_);
                if (selectedSupportedModels.Count > 0)
                {
                    VisualElement tagsContainer = CreateModelTags(selectedSupportedModels);
                    headerRow.Add(tagsContainer);
                }
            }

            section.Add(headerRow);

            // Create dropdown with options
            string initialValue = _selectedValues.TryGetValue(generationModelOption_.type, out string previousValue)
                ? previousValue
                : "";

            DropdownField dropdown = CreateDropdownForOption(generationModelOption_, initialValue);
            _optionDropdowns[generationModelOption_.type] = dropdown;
            section.Add(dropdown);

            // Create custom input field (initially hidden)
            TextField customInputField = CreateCustomInputField(generationModelOption_, initialValue);
            _customInputFields[generationModelOption_.type] = customInputField;
            section.Add(customInputField);

            // Show/hide custom input based on initial selection
            bool showCustomInput = !string.IsNullOrEmpty(initialValue) &&
                                   !IsPresetValue(generationModelOption_, initialValue);
            customInputField.style.display = showCustomInput ? DisplayStyle.Flex : DisplayStyle.None;

            return section;
        }

        private DropdownField CreateDropdownForOption(ImageModelOptionDefinition option_, string initialValue_)
        {
            List<string> choices = new List<string>();

            // Add "Not selected" option
            choices.Add(LocalizationManager.Get(LocalizationKeys.OPTION_NOT_SELECTED));

            // Add preset values
            foreach (ImageModelOptionValue optionValue in option_.values)
            {
                choices.Add(optionValue.displayName);
            }

            // Add "Custom input" option
            choices.Add(LocalizationManager.Get(LocalizationKeys.OPTION_CUSTOM_INPUT));

            DropdownField dropdown = new DropdownField();
            dropdown.AddToClassList("option-dropdown");
            dropdown.choices = choices;

            // Set initial index
            int initialIndex = GetDropdownIndexForValue(option_, initialValue_);
            dropdown.index = initialIndex;

            // Register value changed callback
            dropdown.RegisterValueChangedCallback(evt_ =>
            {
                HandleDropdownChanged(option_, evt_.newValue);
            });

            return dropdown;
        }

        private TextField CreateCustomInputField(ImageModelOptionDefinition option_, string initialValue_)
        {
            TextField textField = new TextField();
            textField.AddToClassList("option-custom-input");
            textField.value = IsPresetValue(option_, initialValue_) ? "" : initialValue_;

            textField.RegisterValueChangedCallback(evt_ =>
            {
                if (string.IsNullOrEmpty(evt_.newValue))
                {
                    _selectedValues.Remove(option_.type);
                }
                else
                {
                    _selectedValues[option_.type] = evt_.newValue;
                }
            });

            return textField;
        }

        private int GetDropdownIndexForValue(ImageModelOptionDefinition option_, string value_)
        {
            if (string.IsNullOrEmpty(value_))
                return 0; // "Not selected"

            // Check if it's a preset value
            for (int i = 0; i < option_.values.Count; i++)
            {
                if (option_.values[i].value == value_)
                {
                    return i + 1; // +1 because "Not selected" is at index 0
                }
            }

            // If not a preset value, it's custom input
            return option_.values.Count + 1; // Last index (Custom input)
        }

        private bool IsPresetValue(ImageModelOptionDefinition option_, string value_)
        {
            if (string.IsNullOrEmpty(value_))
                return false;

            foreach (ImageModelOptionValue optionValue in option_.values)
            {
                if (optionValue.value == value_)
                    return true;
            }

            return false;
        }

        private void HandleDropdownChanged(ImageModelOptionDefinition option_, string newDisplayValue_)
        {
            TextField customInputField = _customInputFields[option_.type];

            // Check if "Not selected" was chosen
            if (newDisplayValue_ == LocalizationManager.Get(LocalizationKeys.OPTION_NOT_SELECTED))
            {
                _selectedValues.Remove(option_.type);
                customInputField.style.display = DisplayStyle.None;
                customInputField.value = "";
                return;
            }

            // Check if "Custom input" was chosen
            if (newDisplayValue_ == LocalizationManager.Get(LocalizationKeys.OPTION_CUSTOM_INPUT))
            {
                customInputField.style.display = DisplayStyle.Flex;
                customInputField.Focus();
                // Keep the value from _selectedValues if it exists
                return;
            }

            // A preset value was chosen
            customInputField.style.display = DisplayStyle.None;
            customInputField.value = "";

            // Find the actual value from the display name
            foreach (ImageModelOptionValue optionValue in option_.values)
            {
                string displayName = optionValue.displayName;
                if (displayName == newDisplayValue_)
                {
                    _selectedValues[option_.type] = optionValue.value;
                    return;
                }
            }
        }

        private List<ImageModelInfo> GetSelectedModelsForOption(IReadOnlyList<ImageModelInfo> supportedModels_, IReadOnlyList<ImageModelInfo> activeModels_)
        {
            List<ImageModelInfo> selectedModels = new List<ImageModelInfo>();

            if (supportedModels_ == null || activeModels_ == null)
                return selectedModels;

            foreach (ImageModelInfo supportedModel in supportedModels_)
            {
                foreach (ImageModelInfo activeModel in activeModels_)
                {
                    if (activeModel != null && supportedModel != null && activeModel.Id == supportedModel.Id)
                    {
                        selectedModels.Add(supportedModel);
                        break;
                    }
                }
            }

            return selectedModels;
        }

        private VisualElement CreateModelTags(List<ImageModelInfo> models_)
        {
            VisualElement tagsContainer = new VisualElement();
            tagsContainer.AddToClassList("model-tags-container");

            foreach (ImageModelInfo model in models_)
            {
                Label tag = new Label(GetModelDisplayName(model));
                tag.AddToClassList("model-chip");
                tag.AddToClassList("model-chip--selected");
                tag.tooltip = model.GetTooltip();

                // Apply dynamic color based on model ID (always selected since we only show selected models)
                (Color bgColor, Color borderColor) = GetModelColors(model);
                tag.style.backgroundColor = bgColor;
                tag.style.borderTopColor = borderColor;
                tag.style.borderBottomColor = borderColor;
                tag.style.borderLeftColor = borderColor;
                tag.style.borderRightColor = borderColor;

                tagsContainer.Add(tag);
            }

            return tagsContainer;
        }

        private string GetModelDisplayName(ImageModelInfo model_)
        {
            // Use DisplayName if available, otherwise use ID
            if (!string.IsNullOrEmpty(model_.DisplayName))
                return model_.DisplayName;

            // Fallback to DisplayName or ID
            return (!string.IsNullOrEmpty(model_.DisplayName) ? model_.DisplayName : model_.Id);
        }

        private (Color bgColor, Color borderColor) GetModelColors(ImageModelInfo model_)
        {
            string modelId = model_.Id.ToLower();
            float alpha =0.2f; // More opaque for selected models

            // Color scheme based on model type with transparency for subtle appearance
            if (modelId.Contains("dall-e") || modelId.Contains("gpt-image"))
            {
                // Purple/Violet for OpenAI
                return (new Color(0.45f, 0.34f, 0.59f, alpha), new Color(0.59f, 0.47f, 0.75f, alpha));
            }
            else if (modelId.Contains("gemini"))
            {
                // Blue for Gemini
                return (new Color(0.26f, 0.52f, 0.96f, alpha), new Color(0.39f, 0.63f, 1.0f, alpha));
            }
            else if (modelId.Contains("imagen"))
            {
                // Red for Imagen
                return (new Color(0.86f, 0.27f, 0.22f, alpha), new Color(1.0f, 0.39f, 0.35f, alpha));
            }
            else if (modelId.Contains("flux") || modelId.Contains("stable-diffusion"))
            {
                // Orange/Yellow for HuggingFace
                return (new Color(1.0f, 0.73f, 0.0f, alpha), new Color(1.0f, 0.78f, 0.24f, alpha));
            }
            else if (modelId.Contains("openrouter"))
            {
                // Green for OpenRouter
                return (new Color(0.20f, 0.66f, 0.33f, alpha), new Color(0.31f, 0.78f, 0.47f, alpha));
            }
            else
            {
                // Default: gray with transparency
                return (new Color(0.4f, 0.4f, 0.4f, alpha), new Color(0.5f, 0.5f, 0.5f, alpha));
            }
        }

        private void RefreshLabels()
        {
            // Re-render would be needed for full language change support
            // For now, this requires calling RenderOptions again
        }

        public Dictionary<string, string> GetSelectedValues()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (KeyValuePair<string, DropdownField> kvp in _optionDropdowns)
            {
                string optionType = kvp.Key;

                // Check if custom input is being used
                if (_customInputFields.TryGetValue(optionType, out TextField customField) &&
                    customField.style.display == DisplayStyle.Flex)
                {
                    string customValue = customField.value;
                    if (!string.IsNullOrEmpty(customValue))
                    {
                        result[optionType] = customValue;
                    }
                }
                else if (_selectedValues.TryGetValue(optionType, out string value))
                {
                    result[optionType] = value;
                }
            }

            return result;
        }
    }
}