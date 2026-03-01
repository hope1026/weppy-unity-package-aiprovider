# 이미지 생성 API (Image Generation API)

최첨단 확산 모델(Diffusion Model)을 사용하여 런타임에 에셋이나 예술 작품을 생성하세요.

## 지원하는 공급자 (Supported Providers)

- **OpenAI**: DALL-E 3, DALL-E 2
- **Google Gemini**: 이미지 생성 지원 모델
- **Google Imagen**: Imagen 모델
- **OpenRouter**: 접근 가능한 이미지 생성 모델

## 기본 사용법 (Basic Usage)

이미지 생성은 `ImageRequestPayload`와 `ImageProviderManager`를 사용합니다.

```csharp
using UnityEngine;
using Weppy.AIProvider;

public async void GenerateImage()
{
    using (ImageProviderManager manager = new ImageProviderManager())
    {
        manager.AddProvider(ImageProviderType.OPEN_AI, new ImageProviderSettings("sk-your-api-key"));

        ImageRequestPayload payload = new ImageRequestPayload("구름 위에 떠 있는 사이버펑크 스타일의 도시");

        payload.Model = "dall-e-3";
        ImageResponse response = await manager.GenerateImageAsync(payload);
        if (response.IsSuccess && response.FirstImage != null)
        {
            Texture2D texture = response.FirstImage.CreateTexture2D();
            myRawImage.texture = texture;
        }
    }
}
```

## 설정 옵션 (Configuration Options)

공급자의 지원 여부에 따라 `ImageRequestPayload` 속성을 통해 크기, 품질 등의 추가 매개변수를 지정할 수 있습니다.

## 에러 처리 (Handling Errors)

네트워크 오류나 API 문제는 `ImageResponse.IsSuccess`와 `ErrorMessage`로 확인하세요.

```csharp
ImageResponse response = await manager.GenerateImageAsync(payload);
if (!response.IsSuccess)
{
    Debug.LogError($"이미지 생성 실패: {response.ErrorMessage}");
}
```
