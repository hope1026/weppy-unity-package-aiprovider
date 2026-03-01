# 画像生成 API

最先端の拡散モデルを使用して、ランタイムでアセットやアートワークを生成します。

## 対応プロバイダー

- **OpenAI**: DALL-E 3、DALL-E 2
- **Google Gemini**: 画像生成対応モデル
- **Google Imagen**: Imagen モデル
- **OpenRouter**: 利用可能な画像生成モデル

## 基本的な使い方

画像生成は `ImageRequestPayload` と `ImageProviderManager` を使用します。

```csharp
using UnityEngine;
using Weppy.AIProvider;

public async void GenerateImage()
{
    using (ImageProviderManager manager = new ImageProviderManager())
    {
        manager.AddProvider(ImageProviderType.OPEN_AI, new ImageProviderSettings("sk-your-api-key"));

        ImageRequestPayload payload = new ImageRequestPayload("雲の上に浮かぶサイバーパンク都市");

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

## 設定オプション

プロバイダーの対応状況に応じて、`ImageRequestPayload` のプロパティでサイズや品質などの追加パラメータを指定できます。

## エラー処理

ネットワークエラーや API 問題は `ImageResponse.IsSuccess` と `ErrorMessage` で確認してください。

```csharp
ImageResponse response = await manager.GenerateImageAsync(payload);
if (!response.IsSuccess)
{
    Debug.LogError($"画像生成に失敗しました: {response.ErrorMessage}");
}
```
