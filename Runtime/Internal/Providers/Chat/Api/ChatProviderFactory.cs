using UnityEngine;

namespace Weppy.AIProvider.Chat
{
    internal static class ChatProviderFactory
    {
        internal static ChatProviderAbstract Create(ChatProviderType providerType_, ChatProviderSettings settings_)
        {
            if (settings_ == null)
            {
                Debug.LogError($"Chat provider:{providerType_} settings cannot be null.");
                return null;
            }

            switch (providerType_)
            {
                case ChatProviderType.OPEN_AI:
                {
                    return new OpenAIChatProvider(settings_);
                }
                case ChatProviderType.GOOGLE:
                {
                    return new GoogleChatProvider(settings_);
                }
                case ChatProviderType.ANTHROPIC:
                {
                    return new AnthropicChatProvider(settings_);
                }
                case ChatProviderType.HUGGING_FACE:
                {
                    return new HuggingFaceChatProvider(settings_);
                }
                case ChatProviderType.OPEN_ROUTER:
                {
                    return new OpenRouterChatProvider(settings_);
                }
            }

            Debug.LogError($"Chat provider:{providerType_} is not supported.");
            return null;
        }
    }
}
