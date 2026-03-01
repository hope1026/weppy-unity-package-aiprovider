namespace Weppy.AIProvider
{
    /// <summary>
    /// Target app provider and model information for image requests.
    /// </summary>
    public class ImageAppRequestProviderTarget
    {
        public ImageAppProviderType ProviderType { get; set; } = ImageAppProviderType.NONE;
        public string Model { get; set; }
        public int Priority { get; set; }
    }
}
