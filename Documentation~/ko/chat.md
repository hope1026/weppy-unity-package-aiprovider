# 채팅 API

이 패키지는 채팅 기능에 집중되어 있습니다.

## API 공급자

- OpenAI (`ChatProviderType.OPEN_AI`)
- Google (`ChatProviderType.GOOGLE`)
- Anthropic (`ChatProviderType.ANTHROPIC`)
- HuggingFace (`ChatProviderType.HUGGING_FACE`)
- OpenRouter (`ChatProviderType.OPEN_ROUTER`)

## CLI 공급자

- Codex CLI (`ChatCliProviderType.CODEX_CLI`)
- Claude Code CLI (`ChatCliProviderType.CLAUDE_CODE_CLI`)
- Gemini CLI (`ChatCliProviderType.GEMINI_CLI`)

## API 기본 사용

```csharp
using UnityEngine;
using Weppy.AIProvider.Chat;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(
        ChatProviderType.OPEN_AI,
        new ChatProviderSettings("sk-your-api-key")
        {
            DefaultModel = ChatModelPresets.OpenAI.GPT_4O_MINI
        });

    ChatRequestPayload payload = new ChatRequestPayload()
        .WithSystemPrompt("당신은 간결한 비서입니다.")
        .AddUserMessage("이 내용을 한 줄로 설명해줘.");

    ChatResponse response = await manager.SendMessageAsync(payload);
    Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
}
```

## 스트리밍 (API)

```csharp
using System.Text;
using System.Threading.Tasks;
using Weppy.AIProvider.Chat;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.ANTHROPIC, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .AddUserMessage("짧은 시를 써줘.");

    StringBuilder builder = new StringBuilder();

    await manager.StreamMessageAsync(payload, (string chunk) =>
    {
        builder.Append(chunk);
        return Task.CompletedTask;
    });
}
```

## 대화 기록 유지

같은 `ChatRequestPayload` 객체에 메시지를 계속 추가하면 문맥을 유지할 수 있습니다.

```csharp
ChatRequestPayload payload = new ChatRequestPayload();
payload.AddUserMessage("프랑스 수도는 어디야?");

ChatResponse first = await manager.SendMessageAsync(payload);
payload.AddAssistantMessage(first.Content);
payload.AddUserMessage("인구는 얼마나 돼?");

ChatResponse second = await manager.SendMessageAsync(payload);
```

## CLI 기본 사용

```csharp
using Weppy.AIProvider.Chat;

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
        .AddUserMessage("테스트 팁 3가지를 알려줘.");

    ChatCliResponse response = await manager.SendMessageAsync(payload);
}
```

## 참고

- 대상 공급자를 지정하지 않으면 `ChatProviderManager`는 사용 가능한 최고 우선순위 공급자를 선택합니다.
- CLI 장문 대화는 `SendPersistentMessageAsync`와 `ResetSession` 사용을 권장합니다.
