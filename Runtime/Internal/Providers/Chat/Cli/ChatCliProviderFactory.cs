using UnityEngine;

namespace Weppy.AIProvider
{
    internal static class ChatCliProviderFactory
    {
        internal static ChatCliProviderAbstract Create(ChatCliProviderType providerType_, ChatCliProviderSettings settings_)
        {
            if (settings_ == null)
            {
                Debug.LogError($"Chat CLI provider:{providerType_} settings cannot be null.");
                return null;
            }

            switch (providerType_)
            {
                case ChatCliProviderType.CODEX_CLI:
                {
                    return new ChatCliProviderCodex(settings_);
                }
                case ChatCliProviderType.CLAUDE_CODE_CLI:
                {
                    return new ChatCliProviderClaudeCode(settings_);
                }
                case ChatCliProviderType.GEMINI_CLI:
                {
                    return new ChatCliProviderGemini(settings_);
                }
            }

            Debug.LogError($"Chat CLI provider:{providerType_} is not supported.");
            return null;
        }
    }
}
