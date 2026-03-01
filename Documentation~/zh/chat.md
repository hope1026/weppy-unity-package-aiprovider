# 聊天 API

Weppy AI Provider 的核心是与多个大语言模型（LLM）集成的统一聊天接口。

## 支持的提供商

- **OpenAI**：GPT-4、GPT-3.5 Turbo 等全部模型
- **Google**：Gemini 1.5 Pro、Flash 等全部模型
- **Anthropic**：Claude 3.5 Sonnet、Opus、Haiku 等全部模型
- **HuggingFace**：数千个开源模型
- **OpenRouter**：所有可访问模型

### CLI 提供商

- **Codex CLI**
- **Claude Code CLI**
- **Gemini CLI**

## 基本用法

发送消息需要创建 `ChatRequestPayload`，并传给 `ChatProviderManager`。

```csharp
using UnityEngine;
using Weppy.AIProvider;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.OPEN_AI, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .WithSystemPrompt("你是一名乐于助人的助手。")
        .AddUserMessage("用 5 个词解释量子力学。");

    payload.Model = "gpt-4o";
    ChatResponse response = await manager.SendMessageAsync(payload);
    Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
}
```

## 流式响应

若需要实时反馈（打字效果等），请使用 `StreamMessageAsync`。回调会收到文本分片。

```csharp
using Weppy.AIProvider;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.ANTHROPIC, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .AddUserMessage("写一首短诗。");

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

## 对话历史管理

`ChatRequestPayload` 会维护对话历史。要保持上下文，请持续向同一个 payload 添加消息。

```csharp
using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.GOOGLE, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload();
    payload.AddUserMessage("法国的首都是哪里？");

    payload.Model = "gemini-1.5-pro";
    ChatResponse response1 = await manager.SendMessageAsync(payload);
    payload.AddAssistantMessage(response1.Content);
    payload.AddUserMessage("那里有多少人口？");
    ChatResponse response2 = await manager.SendMessageAsync(payload);
}
```

## 自定义模型 ID

不受硬编码模型列表限制。当提供商发布新模型时，可以直接使用模型 ID 字符串访问。

- **OpenAI**：`gpt-4-turbo`, `gpt-4o`
- **Anthropic**：`claude-3-5-sonnet-20240620`
- **Google**：`gemini-1.5-pro`
