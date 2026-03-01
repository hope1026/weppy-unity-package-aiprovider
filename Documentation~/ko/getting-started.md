# 시작하기 (Getting Started)

## 설치 (Installation)

### Git URL로 설치

1.  유니티에서 **Package Manager** 창을 엽니다 (`Window > Package Manager`).
2.  **+** 버튼을 클릭하고 **Add package from git URL...**을 선택합니다.
3.  아래 URL을 붙여넣고 **Add**를 클릭합니다.

`https://github.com/hope1026/weppy-unity-package-aiprovider.git`

## 설정 (Setup)

별도의 설정 창 없이 코드에서 직접 API 키를 설정하고 프로바이더를 추가합니다.

## 첫 채팅 상호작용 (Your First Chat Interaction)

모든 것이 제대로 작동하는지 확인하기 위한 간단한 예제입니다.

1.  `HelloAI.cs`라는 새 스크립트를 생성합니다.
2.  다음 코드를 추가합니다:
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
                .AddUserMessage("안녕! 짧은 농담 하나만 해줘.");

            payload.Model = "gpt-4o";
            ChatResponse response = await manager.SendMessageAsync(payload);
            Debug.Log(response.IsSuccess ? response.Content : $"Error: {response.ErrorMessage}");
        }
    }
}
```
3.  스크립트를 씬(Scene)의 게임 오브젝트에 부착합니다.
4.  **Play** 버튼을 누릅니다.
5.  **Console** 창에서 AI의 응답을 확인하세요.

## 다음 단계

- [채팅 기능 (Chat Features)](chat.md)에서 스트리밍과 대화 기록 관리에 대해 알아보세요.
- [이미지 생성 (Image Generation)](image-generation.md)을 시도해 보세요.
- [배경 제거 (Background Removal)](bg-removal.md)을 시도해 보세요.
- [에디터 창 사용법 (Editor Window)](editor-window.md)에서 에디터 실행과 테스트 방법을 확인하세요.
