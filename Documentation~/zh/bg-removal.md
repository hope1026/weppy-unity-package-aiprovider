# 背景去除 API

使用 Remove.bg API（或兼容服务）可即时去除图像背景。

## 支持的提供商

- **Remove.bg**

### 用法

```csharp
using System;
using UnityEngine;
using Weppy.AIProvider;

public async void RemoveBackground(Texture2D inputTexture)
{
    byte[] pngBytes = inputTexture.EncodeToPNG();
    string base64Image = Convert.ToBase64String(pngBytes);

    BgRemovalRequestPayload payload = new BgRemovalRequestPayload(base64Image, "image/png");
    using (BgRemovalProviderManager manager = new BgRemovalProviderManager())
    {
        manager.AddProvider(BgRemovalProviderType.REMOVE_BG, new BgRemovalProviderSettings("your-api-key"));

        payload.Model = BgRemovalModelPresets.RemoveBg.AUTO;
        BgRemovalResponse response = await manager.RemoveBackgroundAsync(payload);
        if (response.IsSuccess && response.HasImage)
        {
            byte[] resultBytes = response.GetImageBytes();
            Texture2D result = new Texture2D(2, 2);
            result.LoadImage(resultBytes);
            myRawImage.texture = result;
        }
    }
}
```
