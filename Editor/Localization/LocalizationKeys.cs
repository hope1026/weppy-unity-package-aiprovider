namespace Weppy.AIProvider.Chat.Editor
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

        // Main Window Tabs
        public const string MAIN_TAB_CHAT = "mainTab.chat";
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

        // Drop Area and Attachment Hints
        public const string CHAT_DROP_AREA_HINT = "chat.dropAreaHint";
        public const string CHAT_ATTACHMENT_HINT = "chat.attachmentHint";
        
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
