# Weppy AI Provider Chat

一个仅提供聊天能力的 Unity 多供应商 AI 包。

## 主要功能

- API 聊天统一接口: OpenAI, Google, Anthropic, HuggingFace, OpenRouter
- CLI 聊天统一接口: Codex CLI, Claude Code CLI, Gemini CLI
- 支持 API 供应商流式响应
- 支持 Unity 编辑器窗口 (`Window > Weppy > AI Provider Chat`)

## 安装

1. 在 Unity 中打开 **Window > Package Manager**。
2. 点击左上角 **+**，选择 **Add package from git URL...**。
3. 输入下方 Git URL，然后点击 **Add**。

`https://github.com/hope1026/weppy-aiprovider-chat-package.git`

## 快速开始

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
                .AddUserMessage("你好，请给我一条简短建议。");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## 文档

- [Index](index.md)
- [Getting Started](getting-started.md)
- [Chat API](chat.md)
- [Editor Window](editor-window.md)
