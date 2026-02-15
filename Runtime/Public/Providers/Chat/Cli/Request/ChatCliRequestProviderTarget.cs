namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Specifies a target CLI provider, model, and priority.
    /// </summary>
    public struct ChatCliRequestProviderTarget
    {
        public ChatCliProviderType ProviderType;
        public string Model;
        public int Priority;
    }
}
