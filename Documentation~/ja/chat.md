# チャット API

Weppy AI Provider の中核は、複数の大規模言語モデル（LLM）に統合されたチャットインターフェースです。

## 対応プロバイダー

- **OpenAI**: GPT-4、GPT-3.5 Turbo など全モデル
- **Google**: Gemini 1.5 Pro、Flash など全モデル
- **Anthropic**: Claude 3.5 Sonnet、Opus、Haiku など全モデル
- **HuggingFace**: 数千のオープンソースモデル
- **OpenRouter**: 利用可能な全モデル

### CLI プロバイダー

- **Codex CLI**
- **Claude Code CLI**
- **Gemini CLI**

## 基本的な使い方

メッセージを送るには `ChatRequestPayload` を作成し、`ChatProviderManager` に渡します。

```csharp
using UnityEngine;
using Weppy.AIProvider;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.OPEN_AI, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .WithSystemPrompt("あなたは役に立つアシスタントです。")
        .AddUserMessage("量子力学を5語で説明してください。");

    payload.Model = "gpt-4o";
    ChatResponse response = await manager.SendMessageAsync(payload);
    Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
}
```

## ストリーミング応答

リアルタイムのフィードバック（タイピング効果など）には `StreamMessageAsync` を使用します。コールバックでテキストチャンクを受け取ります。

```csharp
using Weppy.AIProvider;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.ANTHROPIC, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .AddUserMessage("短い詩を書いてください。");

    payload.Model = "claude-3-opus";
    await manager.StreamMessageAsync(
        payload,
        (string chunk) =>
        {
            myTextField.text += chunk;
            return System.Threading.Tasks.Task.CompletedTask;
        });
}
```

## 会話履歴の管理

`ChatRequestPayload` は会話履歴を管理します。文脈を維持するには、同じ payload オブジェクトにメッセージを追加し続けます。

```csharp
using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.GOOGLE, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload();
    payload.AddUserMessage("フランスの首都はどこですか？");

    payload.Model = "gemini-1.5-pro";
    ChatResponse response1 = await manager.SendMessageAsync(payload);
    payload.AddAssistantMessage(response1.Content);
    payload.AddUserMessage("人口はどのくらいですか？");
    ChatResponse response2 = await manager.SendMessageAsync(payload);
}
```

## カスタムモデル ID

ハードコードされたモデル一覧に制限されません。プロバイダーが新しいモデルをリリースしたら、そのモデル ID 文字列を直接使用できます。

- **OpenAI**: `gpt-4-turbo`, `gpt-4o`
- **Anthropic**: `claude-3-5-sonnet-20240620`
- **Google**: `gemini-1.5-pro`
