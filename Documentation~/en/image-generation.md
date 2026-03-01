# Image Generation API

Generate assets or artwork at runtime using state-of-the-art diffusion models.

## Supported Providers

- **OpenAI**: DALL-E 3, DALL-E 2
- **Google Gemini**: Models that support image generation
- **Google Imagen**: Imagen models
- **OpenRouter**: Accessible image generation models

## Basic Usage

Image generation uses `ImageRequestPayload` and `ImageProviderManager`.

```csharp
using UnityEngine;
using Weppy.AIProvider;

public async void GenerateImage()
{
    using (ImageProviderManager manager = new ImageProviderManager())
    {
        manager.AddProvider(ImageProviderType.OPEN_AI, new ImageProviderSettings("sk-your-api-key"));

        ImageRequestPayload payload = new ImageRequestPayload("A cyberpunk city floating above the clouds");

        payload.Model = "dall-e-3";
        ImageResponse response = await manager.GenerateImageAsync(payload);
        if (response.IsSuccess && response.FirstImage != null)
        {
            Texture2D texture = response.FirstImage.CreateTexture2D();
            myRawImage.texture = texture;
        }
    }
}
```

## Configuration Options

Depending on the provider support, you can set additional parameters like size and quality via `ImageRequestPayload` properties.

## Handling Errors

Check `ImageResponse.IsSuccess` and `ErrorMessage` for network or API issues.

```csharp
ImageResponse response = await manager.GenerateImageAsync(payload);
if (!response.IsSuccess)
{
    Debug.LogError($"Image generation failed: {response.ErrorMessage}");
}
```
