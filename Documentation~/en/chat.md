# Chat API

The core of Weppy AI Provider is a unified chat interface integrated with multiple large language models (LLMs).

## Supported Providers

- **OpenAI**: GPT-4, GPT-3.5 Turbo, and all available models
- **Google**: Gemini 1.5 Pro, Flash, and all available models
- **Anthropic**: Claude 3.5 Sonnet, Opus, Haiku, and all available models
- **HuggingFace**: Thousands of open source models
- **OpenRouter**: All accessible models

### CLI Providers

- **Codex CLI**
- **Claude Code CLI**
- **Gemini CLI**

## Basic Usage

To send a message, create a `ChatRequestPayload` and pass it to `ChatProviderManager`.

```csharp
using UnityEngine;
using Weppy.AIProvider;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.OPEN_AI, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .WithSystemPrompt("You are a helpful assistant.")
        .AddUserMessage("Explain quantum mechanics in 5 words.");

    payload.Model = "gpt-4o";
    ChatResponse response = await manager.SendMessageAsync(payload);
    Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
}
```

## Streaming Responses

For real-time feedback (typing effect, etc.), use `StreamMessageAsync`. You will receive text chunks in the callback.

```csharp
using Weppy.AIProvider;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.ANTHROPIC, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .AddUserMessage("Write a short poem.");

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

## Managing Conversation History

`ChatRequestPayload` manages conversation history. To preserve context, keep adding messages to the same payload object.

```csharp
using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.GOOGLE, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload();
    payload.AddUserMessage("What is the capital of France?");

    payload.Model = "gemini-1.5-pro";
    ChatResponse response1 = await manager.SendMessageAsync(payload);
    payload.AddAssistantMessage(response1.Content);
    payload.AddUserMessage("How many people live there?");
    ChatResponse response2 = await manager.SendMessageAsync(payload);
}
```

## Custom Model IDs

You are not limited to hardcoded model lists. When a provider releases a new model, you can use the model ID string directly.

- **OpenAI**: `gpt-4-turbo`, `gpt-4o`
- **Anthropic**: `claude-3-5-sonnet-20240620`
- **Google**: `gemini-1.5-pro`
