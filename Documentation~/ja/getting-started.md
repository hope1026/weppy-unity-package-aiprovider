# はじめに

## インストール

### Git URL でインストール

1.  Unity で **Package Manager** ウィンドウを開きます（`Window > Package Manager`）。
2.  **+** ボタンをクリックし、**Add package from git URL...** を選択します。
3.  以下の URL を貼り付けて **Add** をクリックします。

`https://github.com/hope1026/weppy-unity-package-aiprovider.git`

## セットアップ

追加の設定ウィンドウは不要です。コード内で API キーを設定し、プロバイダーを追加します。

## 最初のチャット

正常に動作することを確認するための簡単な例です。

1.  `HelloAI.cs` という新しいスクリプトを作成します。
2.  次のコードを追加します:
```csharp
using UnityEngine;
using Weppy.AIProvider;

public class HelloAI : MonoBehaviour
{
    async void Start()
    {
        using (ChatProviderManager manager = new ChatProviderManager())
        {
            manager.AddProvider(ChatProviderType.OPEN_AI, new ChatProviderSettings("sk-your-api-key"));

            ChatRequestPayload payload = new ChatRequestPayload()
                .AddUserMessage("こんにちは！短いジョークをひとつお願いします。");

            payload.Model = "gpt-4o";
            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```
3.  スクリプトをシーンの GameObject にアタッチします。
4.  **Play** ボタンを押します。
5.  **Console** ウィンドウで AI の応答を確認します。

## 次のステップ

- [チャット機能](chat.md) でストリーミングと会話履歴の管理を学びましょう。
- [画像生成](image-generation.md) を試してください。
- [背景除去](bg-removal.md) を試してください。
- [エディターウィンドウ](editor-window.md) でエディター内の使い方とテスト方法を確認してください。
