namespace Weppy.AIProvider
{
    /// <summary>
    /// Specifies a target provider, model, and priority for an image request.
    /// </summary>
    public struct ImageRequestProviderTarget
    {
        public ImageProviderType ProviderType;
        public string Model;
        public int Priority;
    }
}
