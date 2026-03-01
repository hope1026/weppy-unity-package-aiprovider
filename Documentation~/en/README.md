# Weppy AI Provider

A unified Unity package for integrating multiple AI providers (OpenAI, Google Gemini, Anthropic, HuggingFace, OpenRouter) for Chat, Image Generation, and Background Removal.

## Screenshots

### Chat
> Multi-provider chat with streaming support (API & CLI)

![Chat](../images/chat01.png)

### Image Generation
> Generate images with DALL-E, Imagen, and more

![Image Generation](../images/image-gen-01.png)

### Background Removal
> Remove image backgrounds with one click

![Background Removal](../images/gb-removal-01.png)

### Provider Settings
> Configure providers and models in the Editor window

![Provider Settings - Chat](../images/provider-window-01.png)
![Provider Settings - Image](../images/provider-window-02.png)

### Custom Model
> Add custom models with pricing and token limits

![Custom Model](../images/provider-window-custom-01.png)

## Features

- **Chat**: Unified API for GPT-4, Gemini Pro, Claude 3, and more. Supports streaming.
- **Image Generation**: DALL-E 3, Imagen.
- **Tools**: Background removal via RemoveBg.
- **Editor Integration**: Test prompts directly in the Unity Editor.

## Installation

### Unity Asset Store

1. Purchase or add the asset to your library from the Unity Asset Store.
2. In Unity, go to **Window > Package Manager**.
3. Select **My Assets** from the top-left dropdown.
4. Search for "Weppy AI Provider" and click **Download**, then **Import**.

### Git URL

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

- [Index](index.md)
- [Getting Started](getting-started.md)
- [Chat API](chat.md)
- [Image Generation](image-generation.md)
- [Background Removal](bg-removal.md)
- [Editor Window](editor-window.md)

## License

This package is distributed under the Unity Asset Store EULA.
