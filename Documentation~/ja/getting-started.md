# Getting Started

## インストール

1. Unity で **Window > Package Manager** を開きます。
2. 左上の **+** ボタンを押して **Add package from git URL...** を選択します。
3. 以下の Git URL を入力して **Add** を押します。

`https://github.com/hope1026/weppy-aiprovider-chat-package.git`

## API チャットの最初のリクエスト

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
                .WithSystemPrompt("あなたは有能なアシスタントです。")
                .AddUserMessage("1文で挨拶してください。");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## CLI チャットの最初のリクエスト

```csharp
using UnityEngine;
using Weppy.AIProvider.Chat;

public class HelloCliAI : MonoBehaviour
{
    private async void Start()
    {
        using (ChatCliProviderManager manager = new ChatCliProviderManager())
        {
            ChatCliProviderSettings settings = new ChatCliProviderSettings
            {
                UseApiKey = false,
                CliExecutablePath = GeminiCliWrapper.FindGeminiExecutablePath(),
                DefaultModel = GeminiCliWrapper.AUTO_MODEL_ID
            };

            manager.AddProvider(ChatCliProviderType.GEMINI_CLI, settings);

            ChatCliRequestPayload payload = new ChatCliRequestPayload()
                .AddUserMessage("このプロジェクトを1文で要約してください。");

            ChatCliResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## エディターウィンドウ

エディター内テストは `Window > Weppy > AI Provider Chat` を開いて実行します。

## 次のステップ

- [Chat API](chat.md) でリクエストオプションとストリーミングを確認
- [Editor Window](editor-window.md) でエディター運用を確認
