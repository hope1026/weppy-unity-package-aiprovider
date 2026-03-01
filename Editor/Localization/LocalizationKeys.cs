namespace Weppy.AIProvider.Editor
{
    public static class LocalizationKeys
    {
        // Common
        public const string COMMON_MODEL_SELECTION = "common.modelSelection";
        public const string COMMON_MODEL = "common.model";
        public const string COMMON_PRIORITY = "common.priority";
        public const string COMMON_SUCCESS = "common.success";
        public const string COMMON_ERROR = "common.error";
        public const string COMMON_CANCEL = "common.cancel";
        public const string COMMON_DELETE = "common.delete";
        public const string COMMON_SEARCH_PLACEHOLDER_PROVIDERS = "common.searchPlaceholderProviders";
        public const string COMMON_SEARCH_PLACEHOLDER_MODELS = "common.searchPlaceholderModels";
        public const string COMMON_SORT_PRIORITY_DESC = "common.sortPriorityDesc";
        public const string COMMON_SORT_PRICE_ASC = "common.sortPriceAsc";
        public const string COMMON_SORT_PRICE_DESC = "common.sortPriceDesc";
        public const string COMMON_SORT_CONTEXT_DESC = "common.sortContextDesc";
        public const string HISTORY_LABEL = "history.label";
        public const string HISTORY_LOAD = "history.load";
        public const string HISTORY_EMPTY = "history.empty";

        // Settings View
        public const string SETTINGS_LANGUAGE_CHANGED = "settings.languageChanged";
        public const string SETTINGS_GET_API_KEY = "settings.getApiKey";
        public const string SETTINGS_API_KEY_SET = "settings.apiKeySet";
        public const string SETTINGS_NO_API_KEY = "settings.noApiKey";
        public const string SETTINGS_DELETE_ALL_DATA_BUTTON = "settings.deleteAllDataButton";
        public const string SETTINGS_DELETE_ALL_DATA_TITLE = "settings.deleteAllDataTitle";
        public const string SETTINGS_DELETE_ALL_DATA_MESSAGE = "settings.deleteAllDataMessage";
        public const string SETTINGS_DELETE_ALL_DATA_SUCCESS = "settings.deleteAllDataSuccess";

        // API Key Section in Popup
        public const string API_KEY_SECTION_TITLE = "apiKey.sectionTitle";
        public const string API_KEY_LABEL = "apiKey.label";
        public const string API_KEY_GET_LINK = "apiKey.getLink";
        public const string API_KEY_INLINE_WARNING_MESSAGE_GENERIC = "apiKey.inlineWarningMessageGeneric";
        public const string API_KEY_INLINE_WARNING_MESSAGE = "apiKey.inlineWarningMessage";
        public const string API_KEY_INLINE_WARNING_ACTION = "apiKey.inlineWarningAction";
        public const string API_KEY_OPTIONAL_TOGGLE_LABEL = "apiKey.optionalToggleLabel";
        public const string API_KEY_OPTIONAL_STATUS = "apiKey.optionalStatus";
        public const string CLI_PATH_SECTION_TITLE = "cliPath.sectionTitle";
        public const string CLI_PATH_AUTO_DETECT_BUTTON = "cliPath.autoDetectButton";
        public const string CLI_PATH_BROWSE_BUTTON = "cliPath.browseButton";
        public const string CLI_PATH_INVALID_WARNING = "cliPath.invalidWarning";
        public const string CLI_PATH_GO_TO_SETTINGS = "cliPath.goToSettings";
        public const string CLI_PATH_DIALOG_TITLE = "cliPath.dialogTitle";
        public const string CLI_PATH_GUIDE_BUTTON = "cliPath.guideButton";
        public const string APP_PATH_SECTION_TITLE = "appPath.sectionTitle";
        public const string APP_PATH_AUTO_DETECT_BUTTON = "appPath.autoDetectButton";
        public const string APP_PATH_BROWSE_BUTTON = "appPath.browseButton";
        public const string APP_PATH_INVALID_WARNING = "appPath.invalidWarning";
        public const string APP_PATH_GO_TO_SETTINGS = "appPath.goToSettings";
        public const string APP_PATH_DIALOG_TITLE = "appPath.dialogTitle";
        public const string APP_PATH_GUIDE_BUTTON = "appPath.guideButton";
        public const string API_KEY_EDIT_BUTTON = "apiKey.editButton";
        public const string API_KEY_DELETE_BUTTON = "apiKey.deleteButton";
        public const string API_KEY_SAVE_BUTTON = "apiKey.saveButton";
        public const string API_KEY_DELETE_CONFIRM_TITLE = "apiKey.deleteConfirmTitle";
        public const string API_KEY_DELETE_CONFIRM_MESSAGE = "apiKey.deleteConfirmMessage";

        // Custom Model
        public const string CUSTOM_MODEL_PROVIDER = "customModel.provider";
        public const string CUSTOM_MODEL_ID = "customModel.id";
        public const string CUSTOM_MODEL_ID_REQUIRED = "customModel.idRequired";
        public const string CUSTOM_MODEL_DESCRIPTION = "customModel.description";
        public const string CUSTOM_MODEL_ADD_BUTTON = "customModel.addButton";
        public const string CUSTOM_MODEL_TITLE = "customModel.title";
        public const string CUSTOM_MODEL_NAME = "customModel.name";
        public const string CUSTOM_MODEL_PRICING = "customModel.pricing";
        public const string CUSTOM_MODEL_TOKENS = "customModel.tokens";
        public const string CUSTOM_MODEL_LOCKED = "customModel.locked";
        public const string CUSTOM_MODEL_SAVE_BUTTON = "customModel.saveButton";
        public const string CUSTOM_MODEL_NAME_PLACEHOLDER = "customModel.namePlaceholder";
        public const string CUSTOM_MODEL_ID_PLACEHOLDER = "customModel.idPlaceholder";
        public const string CUSTOM_MODEL_PRICING_PLACEHOLDER = "customModel.pricingPlaceholder";
        public const string CUSTOM_MODEL_TOKENS_PLACEHOLDER = "customModel.tokensPlaceholder";
        public const string CUSTOM_MODEL_DESCRIPTION_PLACEHOLDER = "customModel.descriptionPlaceholder";

        // Chat View
        public const string CHAT_SYSTEM_PROMPT_TOOLTIP = "chat.systemPromptTooltip";
        public const string CHAT_USER_PROMPT_TOOLTIP = "chat.userPromptTooltip";
        public const string CHAT_NO_API_KEYS = "chat.noApiKeys";
        public const string CHAT_GO_TO_SETTINGS = "chat.goToSettings";
        public const string CHAT_SENDING = "chat.sending";
        public const string CHAT_PLEASE_ENTER_MESSAGE = "chat.pleaseEnterMessage";
        public const string CHAT_SENDING_REQUEST = "chat.sendingRequest";
        public const string CHAT_COMPLETED = "chat.completed";
        public const string CHAT_FAILED = "chat.failed";
        public const string CHAT_CANCELLED = "chat.cancelled";
        public const string CHAT_SENDING_TO_ALL = "chat.sendingToAll";
        public const string CHAT_RECEIVED_RESPONSES = "chat.receivedResponses";
        public const string CHAT_TOKENS = "chat.tokens";
        public const string CHAT_ADD_FILE = "chat.addFile";
        public const string CHAT_ADD_TEXTURE = "chat.addTexture";
        public const string CHAT_IMAGES_CLEARED = "chat.imagesCleared";
        public const string CHAT_MESSAGES_CLEARED = "chat.messagesCleared";
        public const string CHAT_IMAGE_ATTACHED = "chat.imageAttached";
        public const string CHAT_IMAGE_REMOVED = "chat.imageRemoved";
        public const string CHAT_API_KEY_REQUIRED = "chat.apiKeyRequired";
        public const string CHAT_API_KEY_REQUIRED_TITLE = "chat.apiKeyRequiredTitle";
        public const string CHAT_NO_ENABLED_PROVIDER = "chat.noEnabledProvider";
        public const string CHAT_FEATURE_DISABLED_TITLE = "chat.featureDisabledTitle";
        public const string CHAT_NO_MODELS_ENABLED_TITLE = "chat.noModelsEnabledTitle";
        
        // Image Generation View
        public const string IMAGE_GENERATION_PROMPT_TOOLTIP = "imageGeneration.promptTooltip";
        public const string IMAGE_GENERATION_NEGATIVE_PROMPT_TOOLTIP = "imageGeneration.negativePromptTooltip";
        public const string IMAGE_GENERATION_NO_API_KEYS = "imageGeneration.noApiKeys";
        public const string IMAGE_GENERATION_GO_TO_SETTINGS = "imageGeneration.goToSettings";
        public const string IMAGE_GENERATION_GENERATING = "imageGeneration.generating";
        public const string IMAGE_GENERATION_PLEASE_ENTER_PROMPT = "imageGeneration.pleaseEnterPrompt";
        public const string IMAGE_GENERATION_GENERATING_REQUEST = "imageGeneration.generatingRequest";
        public const string IMAGE_GENERATION_GENERATED = "imageGeneration.generated";
        public const string IMAGE_GENERATION_FAILED = "imageGeneration.failed";
        public const string IMAGE_GENERATION_CANCELLED = "imageGeneration.cancelled";
        public const string IMAGE_GENERATION_GENERATING_ALL = "imageGeneration.generatingAll";
        public const string IMAGE_GENERATION_RECEIVED_RESPONSES = "imageGeneration.receivedResponses";
        public const string IMAGE_GENERATION_REVISED_PROMPT = "imageGeneration.revisedPrompt";
        public const string IMAGE_GENERATION_SAVE_BUTTON = "imageGeneration.saveButton";
        public const string IMAGE_GENERATION_DELETE_BUTTON = "imageGeneration.deleteButton";
        public const string IMAGE_GENERATION_TEXTURE_FAILED = "imageGeneration.textureFailed";
        public const string IMAGE_GENERATION_NO_IMAGES = "imageGeneration.noImages";
        public const string IMAGE_GENERATION_SELECT_IMAGE = "imageGeneration.selectImage";
        public const string IMAGE_GENERATION_REFS_CLEARED = "imageGeneration.refsCleared";
        public const string IMAGE_GENERATION_REF_ATTACHED = "imageGeneration.refAttached";
        public const string IMAGE_GENERATION_REF_REMOVED = "imageGeneration.refRemoved";
        public const string IMAGE_GENERATION_API_KEY_REQUIRED = "imageGeneration.apiKeyRequired";
        public const string IMAGE_GENERATION_API_KEY_REQUIRED_TITLE = "imageGeneration.apiKeyRequiredTitle";
        public const string IMAGE_GENERATION_NO_ENABLED_PROVIDER = "imageGeneration.noEnabledProvider";
        public const string IMAGE_GENERATION_FEATURE_DISABLED_TITLE = "imageGeneration.featureDisabledTitle";
        public const string IMAGE_GENERATION_NO_MODELS_ENABLED = "imageGeneration.noModelsEnabled";
        public const string IMAGE_GENERATION_NO_MODELS_ENABLED_TITLE = "imageGeneration.noModelsEnabledTitle";
        public const string IMAGE_GENERATION_SEND_MODE_PRIORITY = "imageGeneration.sendModePriority";
        public const string IMAGE_GENERATION_SEND_MODE_ALL = "imageGeneration.sendModeAll";
        public const string IMAGE_GENERATION_LOADING_CARD = "imageGeneration.loadingCard";
        public const string IMAGE_GENERATION_ERROR_CARD = "imageGeneration.errorCard";
        public const string IMAGE_GENERATION_CANCELLED_CARD = "imageGeneration.cancelledCard";

        // Background Removal View
        public const string BGREMOVAL_NO_API_KEYS = "bgRemoval.noApiKeys";
        public const string BGREMOVAL_GO_TO_SETTINGS = "bgRemoval.goToSettings";
        public const string BGREMOVAL_PROCESSING = "bgRemoval.processing";
        public const string BGREMOVAL_PLEASE_SELECT_IMAGE = "bgRemoval.pleaseSelectImage";
        public const string BGREMOVAL_REMOVING = "bgRemoval.removing";
        public const string BGREMOVAL_SUCCESS = "bgRemoval.success";
        public const string BGREMOVAL_FAILED = "bgRemoval.failed";
        public const string BGREMOVAL_CANCELLED = "bgRemoval.cancelled";
        public const string BGREMOVAL_SAVE_BUTTON = "bgRemoval.saveButton";
        public const string BGREMOVAL_TEXTURE_FAILED = "bgRemoval.textureFailed";
        public const string BGREMOVAL_SELECT_IMAGE = "bgRemoval.selectImage";
        public const string BGREMOVAL_IMAGE_CLEARED = "bgRemoval.imageCleared";
        public const string BGREMOVAL_IMAGE_SET = "bgRemoval.imageSet";
        public const string BGREMOVAL_API_KEY_REQUIRED = "bgRemoval.apiKeyRequired";
        public const string BGREMOVAL_API_KEY_REQUIRED_TITLE = "bgRemoval.apiKeyRequiredTitle";
        public const string BGREMOVAL_NO_ENABLED_PROVIDER = "bgRemoval.noEnabledProvider";
        public const string BGREMOVAL_FEATURE_DISABLED_TITLE = "bgRemoval.featureDisabledTitle";
        public const string BGREMOVAL_NO_MODELS_ENABLED = "bgRemoval.noModelsEnabled";
        public const string BGREMOVAL_NO_MODELS_ENABLED_TITLE = "bgRemoval.noModelsEnabledTitle";
        public const string BGREMOVAL_PROCESS_MODE_PRIORITY = "bgRemoval.processModePriority";
        public const string BGREMOVAL_PROCESS_MODE_ALL = "bgRemoval.processModeAll";
        public const string BGREMOVAL_REMOVING_ALL = "bgRemoval.removingAll";
        public const string BGREMOVAL_RECEIVED_RESPONSES = "bgRemoval.receivedResponses";

        // Main Window Tabs
        public const string MAIN_TAB_CHAT = "mainTab.chat";
        public const string MAIN_TAB_IMAGE_GENERATION = "mainTab.imageGeneration";
        public const string MAIN_TAB_BACKGROUND_REMOVAL = "mainTab.BgRemoval";
        public const string MAIN_TAB_SETTINGS = "mainTab.settings";
        public const string STATUS_READY = "status.ready";

        // Provider Labels
        public const string PROVIDER_PRIORITY_PROVIDER = "provider.priorityProvider";
        public const string PROVIDER_PRIORITY_DESCRIPTION = "provider.priorityDescription";
        public const string PROVIDER_MODEL_WARNING = "provider.modelWarning";
        public const string PROVIDER_OFFICIAL_DOCS = "provider.officialDocs";
        public const string PROVIDER_CUSTOM_MODEL_BADGE = "provider.customModelBadge";
        public const string PROVIDER_DELETE_CUSTOM_MODEL_BUTTON = "provider.deleteCustomModelButton";
        public const string PROVIDER_DELETE_CUSTOM_MODEL_CONFIRM = "provider.deleteCustomModelConfirm";
        public const string PROVIDER_DELETE_ALL_CUSTOM_MODELS = "provider.deleteAllCustomModels";
        public const string PROVIDER_DELETE_ALL_CUSTOM_MODELS_TITLE = "provider.deleteAllCustomModelsTitle";
        public const string PROVIDER_DELETE_ALL_CUSTOM_MODELS_MESSAGE = "provider.deleteAllCustomModelsMessage";

        // Button Tooltips
        public const string IMAGE_GENERATION_GENERATE_PRIORITY_TOOLTIP = "imageGeneration.generatePriorityTooltip";
        public const string IMAGE_GENERATION_GENERATE_ALL_TOOLTIP = "imageGeneration.generateAllTooltip";
        public const string BGREMOVAL_PROCESS_TOOLTIP = "bgRemoval.processTooltip";
        public const string BGREMOVAL_PROCESS_ALL_TOOLTIP = "bgRemoval.processAllTooltip";

        // Drop Area and Attachment Hints
        public const string CHAT_DROP_AREA_HINT = "chat.dropAreaHint";
        public const string CHAT_ATTACHMENT_HINT = "chat.attachmentHint";
        public const string IMAGE_GENERATION_DROP_AREA_HINT = "imageGeneration.dropAreaHint";
        public const string IMAGE_GENERATION_ATTACHMENT_HINT = "imageGeneration.attachmentHint";
        public const string IMAGE_GENERATION_ADD_IMAGE = "imageGeneration.addImage";
        public const string IMAGE_GENERATION_ADD_PROJECT_TEXTURE = "imageGeneration.addProjectTexture";
        public const string IMAGE_GENERATION_OPTIONS_LABEL = "imageGeneration.optionsLabel";
        public const string IMAGE_GENERATION_OPTIONS_HINT = "imageGeneration.optionsHint";
        public const string IMAGE_GENERATION_ADD_ATTACHMENT_TOOLTIP = "imageGeneration.addAttachmentTooltip";
        public const string BGREMOVAL_DROP_AREA_HINT = "bgRemoval.dropAreaHint";
        public const string BGREMOVAL_ATTACHMENT_HINT = "bgRemoval.attachmentHint";

        // Dynamic Options
        public const string OPTION_NO_ACTIVE_MODELS = "option.noActiveModels";
        public const string OPTION_NO_CONFIGURABLE_OPTIONS = "option.noConfigurableOptions";
        public const string OPTION_NOT_SET_DEFAULT = "option.notSetDefault";
        public const string OPTION_NOT_SET = "option.notSet";
        public const string OPTION_SUGGESTIONS = "option.suggestions";
        public const string OPTION_DEFAULT_SUFFIX = "option.defaultSuffix";
        public const string OPTION_NOT_SELECTED = "option.notSelected";
        public const string OPTION_CUSTOM_INPUT = "option.customInput";
        public const string OPTION_SUPPORTED_MODELS = "option.supportedModels";

        // Send Mode Options
        public const string CHAT_SEND_MODE_PRIORITY = "chat.sendModePriority";
        public const string CHAT_SEND_MODE_ALL = "chat.sendModeAll";
        public const string CHAT_SEND_MODE_TOOLTIP = "chat.sendModeTooltip";

        // Stream Options
        public const string CHAT_STREAM_LABEL = "chat.streamLabel";
        public const string CHAT_STREAM_TOOLTIP = "chat.streamTooltip";
        public const string CHAT_CLI_PERSISTENT_LABEL = "chat.cliPersistentLabel";
        public const string CHAT_CLI_PERSISTENT_TOOLTIP = "chat.cliPersistentTooltip";
        public const string CHAT_CLI_RESET_SESSION = "chat.cliResetSession";
        public const string CHAT_CLI_RESET_SESSION_TOOLTIP = "chat.cliResetSessionTooltip";
        public const string CHAT_CLI_SESSION_RESET = "chat.cliSessionReset";

        // Provider Labels
        public const string CHAT_STREAMING = "chat.streaming";
    }
}
