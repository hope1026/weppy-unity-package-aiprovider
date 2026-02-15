# Chat API

このパッケージはチャット機能に特化しています。

## API プロバイダー

- OpenAI (`ChatProviderType.OPEN_AI`)
- Google (`ChatProviderType.GOOGLE`)
- Anthropic (`ChatProviderType.ANTHROPIC`)
- HuggingFace (`ChatProviderType.HUGGING_FACE`)
- OpenRouter (`ChatProviderType.OPEN_ROUTER`)

## CLI プロバイダー

- Codex CLI (`ChatCliProviderType.CODEX_CLI`)
- Claude Code CLI (`ChatCliProviderType.CLAUDE_CODE_CLI`)
- Gemini CLI (`ChatCliProviderType.GEMINI_CLI`)

## API の基本使用例

```csharp
using UnityEngine;
using Weppy.AIProvider.Chat;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(
        ChatProviderType.OPEN_AI,
        new ChatProviderSettings("sk-your-api-key")
        {
            DefaultModel = ChatModelPresets.OpenAI.GPT_4O_MINI
        });

    ChatRequestPayload payload = new ChatRequestPayload()
        .WithSystemPrompt("あなたは簡潔なアシスタントです。")
        .AddUserMessage("この内容を1行で説明してください。");

    ChatResponse response = await manager.SendMessageAsync(payload);
    Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
}
```

## ストリーミング (API)

```csharp
using System.Text;
using System.Threading.Tasks;
using Weppy.AIProvider.Chat;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.ANTHROPIC, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .AddUserMessage("短い詩を書いてください。");

    StringBuilder builder = new StringBuilder();

    await manager.StreamMessageAsync(payload, (string chunk) =>
    {
        builder.Append(chunk);
        return Task.CompletedTask;
    });
}
```

## 会話履歴

同じ `ChatRequestPayload` にメッセージを追加し続けると文脈を維持できます。

```csharp
ChatRequestPayload payload = new ChatRequestPayload();
payload.AddUserMessage("フランスの首都は？");

ChatResponse first = await manager.SendMessageAsync(payload);
payload.AddAssistantMessage(first.Content);
payload.AddUserMessage("人口はどのくらい？");

ChatResponse second = await manager.SendMessageAsync(payload);
```

## CLI の基本使用例

```csharp
using Weppy.AIProvider.Chat;

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
        .AddUserMessage("テストのコツを3つ教えてください。");

    ChatCliResponse response = await manager.SendMessageAsync(payload);
}
```

## メモ

- 明示ターゲットなしの場合、`ChatProviderManager` は利用可能な最優先プロバイダーを選びます。
- CLI で長い対話を継続する場合は `SendPersistentMessageAsync` と `ResetSession` を使います。
