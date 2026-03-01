# 채팅 API (Chat API)

Weppy AI Provider의 핵심 기능은 다양한 대규모 언어 모델(LLM)과 통합된 채팅 인터페이스입니다.

## 지원하는 공급자 (Supported Providers)

- **OpenAI**: GPT-4, GPT-3.5 Turbo 등 모든 모델
- **Google**: Gemini 1.5 Pro, Flash 등 모든 모델
- **Anthropic**: Claude 3.5 Sonnet, Opus, Haiku 등 모든 모델
- **HuggingFace**: 수천 개의 오픈 소스 모델
- **OpenRouter**: 접근 가능한 모든 모델

### CLI 공급자 (CLI Providers)

- **Codex CLI**
- **Claude Code CLI**
- **Gemini CLI**

## 기본 사용법 (Basic Usage)

메시지를 보내려면 `ChatRequestPayload`를 생성하고 `ChatProviderManager`에 전달합니다.

```csharp
using UnityEngine;
using Weppy.AIProvider;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.OPEN_AI, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .WithSystemPrompt("당신은 도움이 되는 비서입니다.")
        .AddUserMessage("양자 역학을 5단어로 설명해줘.");

    payload.Model = "gpt-4o";
    ChatResponse response = await manager.SendMessageAsync(payload);
    Debug.Log(response.IsSuccess ? response.Content : response.ErrorMessage);
}
```

## 스트리밍 응답 (Streaming Responses)

실시간 피드백(타이핑 효과 등)을 위해 `StreamMessageAsync`를 사용하세요. 콜백으로 텍스트 청크를 받습니다.

```csharp
using Weppy.AIProvider;

using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.ANTHROPIC, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload()
        .AddUserMessage("짧은 시를 하나 써줘.");

    payload.Model = "claude-3-opus";
    await manager.StreamMessageAsync(
        payload,
        (string chunk) =>
        {
            myTextField.text += chunk;
            return System.Threading.Tasks.Task.CompletedTask;
        });
}
```

## 대화 기록 관리 (Managing Conversation History)

`ChatRequestPayload`는 대화 기록을 유지 관리합니다. 문맥을 유지하려면 동일한 payload 객체에 메시지를 계속 추가하면 됩니다.

```csharp
using (ChatProviderManager manager = new ChatProviderManager())
{
    manager.AddProvider(ChatProviderType.GOOGLE, new ChatProviderSettings("sk-your-api-key"));

    ChatRequestPayload payload = new ChatRequestPayload();
    payload.AddUserMessage("프랑스의 수도는 어디야?");

    payload.Model = "gemini-1.5-pro";
    ChatResponse response1 = await manager.SendMessageAsync(payload);
    payload.AddAssistantMessage(response1.Content);
    payload.AddUserMessage("그곳의 인구는 얼마나 돼?");
    ChatResponse response2 = await manager.SendMessageAsync(payload);
}
```

## 커스텀 모델 ID (Custom Model IDs)

하드코딩된 모델 목록에 제한되지 않습니다. 공급자가 새로운 모델을 출시하면, 해당 모델의 ID 문자열을 직접 사용하여 바로 접근할 수 있습니다.

- **OpenAI**: `gpt-4-turbo`, `gpt-4o`
- **Anthropic**: `claude-3-5-sonnet-20240620`
- **Google**: `gemini-1.5-pro`
