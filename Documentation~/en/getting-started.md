# Getting Started

## Installation

1. Open **Window > Package Manager** in Unity.
2. Click the **+** button and select **Add package from git URL...**.
3. Paste the URL below and click **Add**.

`https://github.com/hope1026/weppy-aiprovider-chat-package.git`

## First API Chat Request

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
                .WithSystemPrompt("You are a helpful assistant.")
                .AddUserMessage("Say hello in one sentence.");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## First CLI Chat Request

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
                .AddUserMessage("Summarize this project in one sentence.");

            ChatCliResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## Editor Window

For in-Editor testing, open `Window > Weppy > AI Provider Chat`.

## Next Steps

- Learn request options and streaming in [Chat API](chat.md).
- Use the Editor workflow in [Editor Window](editor-window.md).
