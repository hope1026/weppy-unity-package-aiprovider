# 背景除去 API

Remove.bg API（または互換サービス）を使用して、画像から背景を即座に除去します。

## 対応プロバイダー

- **Remove.bg**

### 使い方

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
