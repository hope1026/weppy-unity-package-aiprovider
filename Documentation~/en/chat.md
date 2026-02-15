# Chat API

This package is focused on chat functionality.

## API Providers

- OpenAI (`ChatProviderType.OPEN_AI`)
- Google (`ChatProviderType.GOOGLE`)
- Anthropic (`ChatProviderType.ANTHROPIC`)
- HuggingFace (`ChatProviderType.HUGGING_FACE`)
- OpenRouter (`ChatProviderType.OPEN_ROUTER`)

## CLI Providers

- Codex CLI (`ChatCliProviderType.CODEX_CLI`)
- Claude Code CLI (`ChatCliProviderType.CLAUDE_CODE_CLI`)
- Gemini CLI (`ChatCliProviderType.GEMINI_CLI`)

## Basic API Usage

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
        .WithSystemPrompt("You are a concise assistant.")
        .AddUserMessage("Explain this in one line.");

    ChatResponse response = await manager.SendMessageAsync(payload);
    Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
}
```

## Streaming (API)

```csharp
using System.Text;
using System.Threading.Tasks;
using Weppy.AIProvider.Chat;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.ANTHROPIC, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .AddUserMessage("Write a short poem.");

    StringBuilder builder = new StringBuilder();

    await manager.StreamMessageAsync(payload, (string chunk) =>
    {
        builder.Append(chunk);
        return Task.CompletedTask;
    });
}
```

## Conversation History

Reuse the same `ChatRequestPayload` and append messages to keep context.

```csharp
ChatRequestPayload payload = new ChatRequestPayload();
payload.AddUserMessage("What is the capital of France?");

ChatResponse first = await manager.SendMessageAsync(payload);
payload.AddAssistantMessage(first.Content);
payload.AddUserMessage("How many people live there?");

ChatResponse second = await manager.SendMessageAsync(payload);
```

## Basic CLI Usage

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
        .AddUserMessage("List 3 testing tips.");

    ChatCliResponse response = await manager.SendMessageAsync(payload);
}
```

## Notes

- `ChatProviderManager` selects the highest-priority available provider when no explicit targets are given.
- For CLI-based long conversations, use `SendPersistentMessageAsync` and `ResetSession`.
