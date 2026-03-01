namespace Weppy.AIProvider
{
    /// <summary>
    /// Specifies a target provider, model, and priority for a chat request.
    /// </summary>
    public struct ChatRequestProviderTarget
    {
        public ChatProviderType ProviderType;
        public string Model;
        public int Priority;
    }
}
