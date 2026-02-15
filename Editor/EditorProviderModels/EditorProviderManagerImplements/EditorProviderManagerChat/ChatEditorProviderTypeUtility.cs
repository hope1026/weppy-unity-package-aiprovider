namespace Weppy.AIProvider.Chat.Editor
{
    public static class ChatEditorProviderTypeUtility
    {
        public static bool IsCliProvider(ChatEditorProviderType providerType_)
        {
            return providerType_ == ChatEditorProviderType.CODEX_CLI ||
                   providerType_ == ChatEditorProviderType.CLAUDE_CODE_CLI ||
                   providerType_ == ChatEditorProviderType.GEMINI_CLI;
        }

        public static bool IsApiProvider(ChatEditorProviderType providerType_)
        {
            return providerType_ != ChatEditorProviderType.NONE && !IsCliProvider(providerType_);
        }

        public static ChatProviderType ToApiProviderType(ChatEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatEditorProviderType.OPEN_AI => ChatProviderType.OPEN_AI,
                ChatEditorProviderType.GOOGLE => ChatProviderType.GOOGLE,
                ChatEditorProviderType.ANTHROPIC => ChatProviderType.ANTHROPIC,
                ChatEditorProviderType.HUGGING_FACE => ChatProviderType.HUGGING_FACE,
                ChatEditorProviderType.OPEN_ROUTER => ChatProviderType.OPEN_ROUTER,
                _ => ChatProviderType.NONE
            };
        }

        public static ChatCliProviderType ToCliProviderType(ChatEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatEditorProviderType.CODEX_CLI => ChatCliProviderType.CODEX_CLI,
                ChatEditorProviderType.CLAUDE_CODE_CLI => ChatCliProviderType.CLAUDE_CODE_CLI,
                ChatEditorProviderType.GEMINI_CLI => ChatCliProviderType.GEMINI_CLI,
                _ => ChatCliProviderType.NONE
            };
        }

        public static ChatEditorProviderType FromCliProviderType(ChatCliProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatCliProviderType.CODEX_CLI => ChatEditorProviderType.CODEX_CLI,
                ChatCliProviderType.CLAUDE_CODE_CLI => ChatEditorProviderType.CLAUDE_CODE_CLI,
                ChatCliProviderType.GEMINI_CLI => ChatEditorProviderType.GEMINI_CLI,
                _ => ChatEditorProviderType.NONE
            };
        }

        public static ChatEditorProviderType FromApiProviderType(ChatProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatProviderType.OPEN_AI => ChatEditorProviderType.OPEN_AI,
                ChatProviderType.GOOGLE => ChatEditorProviderType.GOOGLE,
                ChatProviderType.ANTHROPIC => ChatEditorProviderType.ANTHROPIC,
                ChatProviderType.HUGGING_FACE => ChatEditorProviderType.HUGGING_FACE,
                ChatProviderType.OPEN_ROUTER => ChatEditorProviderType.OPEN_ROUTER,
                _ => ChatEditorProviderType.NONE
            };
        }
    }
}
