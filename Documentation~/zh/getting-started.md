# 快速开始

## 安装

### 通过 Git URL 安装

1.  在 Unity 中打开 **Package Manager**（`Window > Package Manager`）。
2.  点击 **+** 按钮并选择 **Add package from git URL...**。
3.  粘贴以下 URL 并点击 **Add**。

`https://github.com/hope1026/weppy-unity-package-aiprovider.git`

## 设置

无需额外的设置窗口。你可以在代码中直接设置 API Key 并添加提供商。

## 第一次聊天交互

以下是用于验证功能是否正常的简单示例。

1.  新建脚本 `HelloAI.cs`。
2.  添加以下代码:
```csharp
using UnityEngine;
using Weppy.AIProvider;

public class HelloAI : MonoBehaviour
{
    async void Start()
    {
        using (ChatProviderManager manager = new ChatProviderManager())
        {
            manager.AddProvider(ChatProviderType.OPEN_AI, new ChatProviderSettings("sk-your-api-key"));

            ChatRequestPayload payload = new ChatRequestPayload()
                .AddUserMessage("你好！讲个简短的笑话吧。");

            payload.Model = "gpt-4o";
            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```
3.  将脚本挂到场景中的 GameObject 上。
4.  点击 **Play**。
5.  在 **Console** 中查看 AI 的回复。

## 下一步

- 在 [聊天功能](chat.md) 中了解流式输出与对话历史管理。
- 试试 [图像生成](image-generation.md)。
- 试试 [背景去除](bg-removal.md)。
- 查看 [编辑器窗口](editor-window.md) 的使用与测试方式。
