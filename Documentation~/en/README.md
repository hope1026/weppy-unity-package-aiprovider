# Weppy AI Provider Chat

A Unity package for chat-only integration across multiple AI providers.

## Features

- Unified API chat providers: OpenAI, Google, Anthropic, HuggingFace, OpenRouter
- Unified CLI chat providers: Codex CLI, Claude Code CLI, Gemini CLI
- Streaming responses for API providers
- Unity Editor window support (`Window > Weppy > AI Provider Chat`)

## Installation

1. Open **Window > Package Manager** in Unity.
2. Click the **+** button and select **Add package from git URL...**.
3. Paste the URL below and click **Add**.

`https://github.com/hope1026/weppy-aiprovider-chat-package.git`

## Quick Start

```csharp
using UnityEngine;
using Weppy.AIProvider.Chat;

public class HelloAI : MonoBehaviour
{
    private async void Start()
    {
        using (ChatProviderManager manager = new ChatProviderManager())
        {
            manager.AddProvider(
                ChatProviderType.OPEN_AI,
                new ChatProviderSettings("sk-your-api-key")
                {
                    DefaultModel = ChatModelPresets.OpenAI.GPT_4O_MINI
                });

            ChatRequestPayload payload = new ChatRequestPayload()
                .AddUserMessage("Hello! Give me one short tip.");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## Documentation

- [Index](index.md)
- [Getting Started](getting-started.md)
- [Chat API](chat.md)
- [Editor Window](editor-window.md)
