# 배경 제거 API (Background Removal API)

Remove.bg API (또는 호환 서비스)를 사용하여 이미지에서 배경을 즉시 제거합니다.

## 지원하는 공급자 (Supported Providers)

- **Remove.bg**

### 사용법 (Usage)

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
