# Weppy AI Provider

一个用于集成多种 AI 提供商（OpenAI、Google Gemini、Anthropic、HuggingFace、OpenRouter）的统一 Unity 包，支持聊天、图像生成与背景去除。

## 截图

### 聊天
> 多提供商聊天（API 和 CLI），支持流式输出

![Chat](../images/chat01.png)

### 图像生成
> 使用 DALL-E、Imagen 等生成图像

![Image Generation](../images/image-gen-01.png)

### 背景去除
> 一键去除图像背景

![Background Removal](../images/gb-removal-01.png)

### 提供商设置
> 在编辑器窗口中配置提供商和模型

![Provider Settings - Chat](../images/provider-window-01.png)
![Provider Settings - Image](../images/provider-window-02.png)

### 自定义模型
> 添加带有定价和令牌限制的自定义模型

![Custom Model](../images/provider-window-custom-01.png)

## 功能

- **聊天**：GPT-4、Gemini Pro、Claude 3 等的统一 API，支持流式输出。
- **图像生成**：DALL-E 3、Imagen。
- **工具**：RemoveBg 背景去除。
- **编辑器集成**：可直接在 Unity 编辑器内测试提示词。

## 安装

### Git URL

1. 在 Unity 中打开 **Window > Package Manager**。
2. 点击 **+** 按钮并选择 **Add package from git URL...**。
3. 粘贴以下 URL 并点击 **Add**。

`https://github.com/hope1026/weppy-unity-package-aiprovider.git`

## 快速开始

```csharp
using UnityEngine;
using Weppy.AIProvider;

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
                    DefaultModel = "gpt-4o"
                });

            ChatRequestPayload payload = new ChatRequestPayload()
                .AddUserMessage("Hello!");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
        }
    }
}
```

## 示例

- `Samples~/SimpleChatApiSample`
- `Samples~/SimpleChatCliSample`
- `Samples~/SimpleImageSample`

## 文档

- [Index](index.md)
- [Getting Started](getting-started.md)
- [Chat API](chat.md)
- [Image Generation](image-generation.md)
- [Background Removal](bg-removal.md)
- [Editor Window](editor-window.md)

## 许可

许可证详情请参阅 [LICENSE.md](../../LICENSE.md)。
