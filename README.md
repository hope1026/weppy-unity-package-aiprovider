# Weppy AI Provider

A unified Unity package for integrating multiple AI providers (OpenAI, Google Gemini, Anthropic, HuggingFace, OpenRouter) for Chat, Image Generation, and Background Removal.

Language: [English](Documentation~/en/README.md) | [한국어](Documentation~/ko/README.md) | [日本語](Documentation~/ja/README.md) | [中文](Documentation~/zh/README.md)

## Screenshots

### Chat
> Multi-provider chat with streaming support (API & CLI)

![Chat](Documentation~/images/chat01.png)

### Image Generation
> Generate images with DALL-E, Imagen, and more

![Image Generation](Documentation~/images/image-gen-01.png)

### Background Removal
> Remove image backgrounds with one click

![Background Removal](Documentation~/images/gb-removal-01.png)

### Provider Settings
> Configure providers and models in the Editor window

![Provider Settings - Chat](Documentation~/images/provider-window-01.png)
![Provider Settings - Image](Documentation~/images/provider-window-02.png)

### Custom Model
> Add custom models with pricing and token limits

![Custom Model](Documentation~/images/provider-window-custom-01.png)

## Features

- **Chat**: Unified API for GPT-4, Gemini Pro, Claude 3, and more. Supports streaming.
- **Image Generation**: DALL-E 3, Imagen.
- **Tools**: Background removal via RemoveBg.
- **Editor Integration**: Test prompts directly in the Unity Editor.

## Installation

### Git URL (Recommended)

1. In Unity, open **Window > Package Manager**.
2. Click the **+** button and select **Add package from git URL...**.
3. Paste the URL below and click **Add**.

`https://github.com/hope1026/weppy-unity-package-aiprovider.git`

## Quick Start

```csharp
using UnityEngine;
using Weppy.AIProvider;

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
                    DefaultModel = "gpt-4o"
                });

            ChatRequestPayload payload = new ChatRequestPayload()
                .AddUserMessage("Hello!");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
        }
    }
}
```

## Samples

- `Samples~/SimpleChatApiSample`
- `Samples~/SimpleChatCliSample`
- `Samples~/SimpleImageSample`

## Documentation

See `Documentation~` for full guides.

- [Index](Documentation~/en/index.md)
- [Getting Started](Documentation~/en/getting-started.md)
- [Chat API](Documentation~/en/chat.md)
- [Image Generation](Documentation~/en/image-generation.md)
- [Background Removal](Documentation~/en/bg-removal.md)
- [Editor Window](Documentation~/en/editor-window.md)

## License

See [LICENSE.md](LICENSE.md) for license details.
