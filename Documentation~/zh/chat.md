# Chat API

本包专注于聊天能力。

## API 供应商

- OpenAI (`ChatProviderType.OPEN_AI`)
- Google (`ChatProviderType.GOOGLE`)
- Anthropic (`ChatProviderType.ANTHROPIC`)
- HuggingFace (`ChatProviderType.HUGGING_FACE`)
- OpenRouter (`ChatProviderType.OPEN_ROUTER`)

## CLI 供应商

- Codex CLI (`ChatCliProviderType.CODEX_CLI`)
- Claude Code CLI (`ChatCliProviderType.CLAUDE_CODE_CLI`)
- Gemini CLI (`ChatCliProviderType.GEMINI_CLI`)

## API 基础用法

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
        .WithSystemPrompt("你是一个简洁的助手。")
        .AddUserMessage("请用一行解释这个内容。");

    ChatResponse response = await manager.SendMessageAsync(payload);
    Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
}
```

## 流式响应 (API)

```csharp
using System.Text;
using System.Threading.Tasks;
using Weppy.AIProvider.Chat;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.ANTHROPIC, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .AddUserMessage("写一首短诗。");

    StringBuilder builder = new StringBuilder();

    await manager.StreamMessageAsync(payload, (string chunk) =>
    {
        builder.Append(chunk);
        return Task.CompletedTask;
    });
}
```

## 会话历史

持续向同一个 `ChatRequestPayload` 添加消息即可保持上下文。

```csharp
ChatRequestPayload payload = new ChatRequestPayload();
payload.AddUserMessage("法国的首都是哪里？");

ChatResponse first = await manager.SendMessageAsync(payload);
payload.AddAssistantMessage(first.Content);
payload.AddUserMessage("那里的常住人口大约多少？");

ChatResponse second = await manager.SendMessageAsync(payload);
```

## CLI 基础用法

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
        .AddUserMessage("给我 3 条测试建议。");

    ChatCliResponse response = await manager.SendMessageAsync(payload);
}
```

## 说明

- 未显式指定目标时，`ChatProviderManager` 会使用可用且优先级最高的供应商。
- CLI 长会话建议使用 `SendPersistentMessageAsync` 与 `ResetSession`。
