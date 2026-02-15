# Weppy AI Provider Chat

複数 AI プロバイダー向けのチャット機能のみを提供する Unity パッケージです。

## 主な機能

- API チャット統合: OpenAI, Google, Anthropic, HuggingFace, OpenRouter
- CLI チャット統合: Codex CLI, Claude Code CLI, Gemini CLI
- API プロバイダーのストリーミング応答
- Unity エディターウィンドウ対応 (`Window > Weppy > AI Provider Chat`)

## インストール

1. Unity で **Window > Package Manager** を開きます。
2. 左上の **+** ボタンを押して **Add package from git URL...** を選択します。
3. 以下の Git URL を入力して **Add** を押します。

`https://github.com/hope1026/weppy-aiprovider-chat-package.git`

## クイックスタート

```csharp
using UnityEngine;
using Weppy.AIProvider.Chat;

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
                    DefaultModel = ChatModelPresets.OpenAI.GPT_4O_MINI
                });

            ChatRequestPayload payload = new ChatRequestPayload()
                .AddUserMessage("こんにちは。短いヒントを1つください。");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## ドキュメント

- [Index](index.md)
- [Getting Started](getting-started.md)
- [Chat API](chat.md)
- [Editor Window](editor-window.md)
