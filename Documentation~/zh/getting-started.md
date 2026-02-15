# Getting Started

## 安装

1. 在 Unity 中打开 **Window > Package Manager**。
2. 点击左上角 **+**，选择 **Add package from git URL...**。
3. 输入下方 Git URL，然后点击 **Add**。

`https://github.com/hope1026/weppy-aiprovider-chat-package.git`

## 第一次 API 聊天请求

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
                .WithSystemPrompt("你是一位有帮助的助手。")
                .AddUserMessage("请用一句话打招呼。");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## 第一次 CLI 聊天请求

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
                .AddUserMessage("请用一句话总结这个项目。");

            ChatCliResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## 编辑器窗口

如需在编辑器中测试，请打开 `Window > Weppy > AI Provider Chat`。

## 下一步

- 在 [Chat API](chat.md) 查看请求参数与流式响应
- 在 [Editor Window](editor-window.md) 查看编辑器工作流
