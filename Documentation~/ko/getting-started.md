# 시작하기

## 설치

1. Unity에서 **Window > Package Manager**를 엽니다.
2. 좌측 상단의 **+** 버튼을 클릭하고 **Add package from git URL...**을 선택합니다.
3. 아래 Git URL을 입력하고 **Add**를 클릭합니다.

`https://github.com/hope1026/weppy-aiprovider-chat-package.git`

## 첫 API 채팅 요청

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
                .WithSystemPrompt("당신은 도움이 되는 비서입니다.")
                .AddUserMessage("한 줄로 인사해줘.");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## 첫 CLI 채팅 요청

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
                .AddUserMessage("프로젝트를 한 문장으로 요약해줘.");

            ChatCliResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## 에디터 창

에디터에서 바로 테스트하려면 `Window > Weppy > AI Provider Chat`을 엽니다.

## 다음 단계

- [채팅 API](chat.md)에서 요청 옵션과 스트리밍을 확인하세요.
- [에디터 창](editor-window.md) 사용 흐름을 확인하세요.
