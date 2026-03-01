# Getting Started

## Installation

### Install via Unity Asset Store

1.  Open the **Package Manager** window in Unity (`Window > Package Manager`).
2.  Switch to the **My Assets** tab.
3.  Search for **Weppy AI Provider**.
4.  Click **Download**, then **Import**.

## Setup

No extra settings window is required. You can set API keys and add providers directly in code.

## Your First Chat Interaction

A simple example to verify everything works.

1.  Create a new script named `HelloAI.cs`.
2.  Add the following code:
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
                .AddUserMessage("Hello! Tell me a short joke.");

            payload.Model = "gpt-4o";
            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```
3.  Attach the script to a GameObject in the scene.
4.  Press **Play**.
5.  Check the AI response in the **Console** window.

## Next Steps

- Learn about streaming and conversation history in [Chat Features](chat.md).
- Try [Image Generation](image-generation.md).
- Try [Background Removal](bg-removal.md).
- Check [Editor Window](editor-window.md) for Editor usage and testing.
