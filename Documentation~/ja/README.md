# Weppy AI Provider

Chat、画像生成、背景除去のために複数の AI プロバイダー（OpenAI、Google Gemini、Anthropic、HuggingFace、OpenRouter）を統合する Unity パッケージです。

## スクリーンショット

### チャット
> マルチプロバイダーチャット（API & CLI）、ストリーミング対応

![Chat](../images/chat01.png)

### 画像生成
> DALL-E、Imagen などによる画像生成

![Image Generation](../images/image-gen-01.png)

### 背景除去
> ワンクリックで画像の背景を除去

![Background Removal](../images/gb-removal-01.png)

### プロバイダー設定
> エディターウィンドウでプロバイダーとモデルを設定

![Provider Settings - Chat](../images/provider-window-01.png)
![Provider Settings - Image](../images/provider-window-02.png)

### カスタムモデル
> 料金やトークン制限付きのカスタムモデルを追加

![Custom Model](../images/provider-window-custom-01.png)

## 特長

- **チャット**: GPT-4、Gemini Pro、Claude 3 などに対応した統一 API。ストリーミング対応。
- **画像生成**: DALL-E 3、Imagen。
- **ツール**: RemoveBg による背景除去。
- **エディター統合**: Unity エディター内でプロンプトをテスト。

## インストール

### Unity Asset Store

1. Unity Asset Store でアセットを購入またはライブラリに追加します。
2. Unity で **Window > Package Manager** を開きます。
3. 左上のドロップダウンから **My Assets** を選択します。
4. "Weppy AI Provider" を検索し、**Download** をクリックしてから **Import** をクリックします。

### Git URL

1. Unity で **Window > Package Manager** を開きます。
2. **+** ボタンをクリックし、**Add package from git URL...** を選択します。
3. 以下の URL を貼り付けて **Add** をクリックします。

`https://github.com/hope1026/weppy-unity-package-aiprovider.git`

## はじめに

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

## サンプル

- `Samples~/SimpleChatApiSample`
- `Samples~/SimpleChatCliSample`
- `Samples~/SimpleImageSample`

## ドキュメント

- [Index](index.md)
- [Getting Started](getting-started.md)
- [Chat API](chat.md)
- [Image Generation](image-generation.md)
- [Background Removal](bg-removal.md)
- [Editor Window](editor-window.md)

## ライセンス

このパッケージは Unity Asset Store EULA の下で配布されています。
