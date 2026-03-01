namespace Weppy.AIProvider
{
    /// <summary>
    /// Specifies a target provider, model, and priority for background removal.
    /// </summary>
    public struct BgRemovalRequestProviderTarget
    {
        public BgRemovalProviderType ProviderType;
        public string Model;
        public int Priority;
    }
}
