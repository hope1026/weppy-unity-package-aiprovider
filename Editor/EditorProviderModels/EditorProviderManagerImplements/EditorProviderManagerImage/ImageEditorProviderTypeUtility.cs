namespace Weppy.AIProvider.Editor
{
    public static class ImageEditorProviderTypeUtility
    {
        public static bool IsAppProvider(ImageEditorProviderType providerType_)
        {
            return providerType_ == ImageEditorProviderType.CODEX_APP;
        }

        public static bool IsApiProvider(ImageEditorProviderType providerType_)
        {
            return providerType_ != ImageEditorProviderType.NONE && !IsAppProvider(providerType_);
        }

        public static ImageProviderType ToApiProviderType(ImageEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ImageEditorProviderType.OPEN_AI => ImageProviderType.OPEN_AI,
                ImageEditorProviderType.GOOGLE_GEMINI => ImageProviderType.GOOGLE_GEMINI,
                ImageEditorProviderType.GOOGLE_IMAGEN => ImageProviderType.GOOGLE_IMAGEN,
                ImageEditorProviderType.OPEN_ROUTER => ImageProviderType.OPEN_ROUTER,
                _ => ImageProviderType.NONE
            };
        }

        public static ImageAppProviderType ToAppProviderType(ImageEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ImageEditorProviderType.CODEX_APP => ImageAppProviderType.CODEX_APP,
                _ => ImageAppProviderType.NONE
            };
        }

        public static ImageEditorProviderType FromApiProviderType(ImageProviderType providerType_)
        {
            return providerType_ switch
            {
                ImageProviderType.OPEN_AI => ImageEditorProviderType.OPEN_AI,
                ImageProviderType.GOOGLE_GEMINI => ImageEditorProviderType.GOOGLE_GEMINI,
                ImageProviderType.GOOGLE_IMAGEN => ImageEditorProviderType.GOOGLE_IMAGEN,
                ImageProviderType.OPEN_ROUTER => ImageEditorProviderType.OPEN_ROUTER,
                _ => ImageEditorProviderType.NONE
            };
        }

        public static ImageEditorProviderType FromAppProviderType(ImageAppProviderType providerType_)
        {
            return providerType_ switch
            {
                ImageAppProviderType.CODEX_APP => ImageEditorProviderType.CODEX_APP,
                _ => ImageEditorProviderType.NONE
            };
        }
    }
}
