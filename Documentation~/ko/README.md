# Weppy AI Provider Chat

여러 AI 공급자를 대상으로 채팅 기능만 제공하는 Unity 패키지입니다.

## 주요 기능

- API 기반 채팅 통합: OpenAI, Google, Anthropic, HuggingFace, OpenRouter
- CLI 기반 채팅 통합: Codex CLI, Claude Code CLI, Gemini CLI
- API 공급자 스트리밍 응답 지원
- Unity 에디터 창 지원 (`Window > Weppy > AI Provider Chat`)

## 설치

1. Unity에서 **Window > Package Manager**를 엽니다.
2. 좌측 상단의 **+** 버튼을 클릭하고 **Add package from git URL...**을 선택합니다.
3. 아래 Git URL을 입력하고 **Add**를 클릭합니다.

`https://github.com/hope1026/weppy-aiprovider-chat-package.git`

## 빠른 시작

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
                .AddUserMessage("안녕하세요. 한 줄 팁을 알려주세요.");

            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```

## 문서

- [Index](index.md)
- [시작하기](getting-started.md)
- [채팅 API](chat.md)
- [에디터 창](editor-window.md)
