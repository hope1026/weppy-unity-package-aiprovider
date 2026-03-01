# 图像生成 API

使用先进的扩散模型在运行时生成资源或艺术作品。

## 支持的提供商

- **OpenAI**：DALL-E 3、DALL-E 2
- **Google Gemini**：支持图像生成的模型
- **Google Imagen**：Imagen 模型
- **OpenRouter**：可访问的图像生成模型

## 基本用法

图像生成使用 `ImageRequestPayload` 与 `ImageProviderManager`。

```csharp
using UnityEngine;
using Weppy.AIProvider;

public async void GenerateImage()
{
    using (ImageProviderManager manager = new ImageProviderManager())
    {
        manager.AddProvider(ImageProviderType.OPEN_AI, new ImageProviderSettings("sk-your-api-key"));

        ImageRequestPayload payload = new ImageRequestPayload("漂浮在云端的赛博朋克城市");

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

## 配置选项

根据提供商支持情况，可以通过 `ImageRequestPayload` 属性设置尺寸、质量等额外参数。

## 错误处理

网络错误或 API 问题请查看 `ImageResponse.IsSuccess` 与 `ErrorMessage`。

```csharp
ImageResponse response = await manager.GenerateImageAsync(payload);
if (!response.IsSuccess)
{
    Debug.LogError($"图像生成失败: {response.ErrorMessage}");
}
```
