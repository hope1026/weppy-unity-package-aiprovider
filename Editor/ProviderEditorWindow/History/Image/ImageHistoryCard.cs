namespace Weppy.AIProvider.Editor
{
    [System.Serializable]
    public class ImageHistoryCard
    {
        public ImageHistoryCardState State;
        public string ProviderName;
        public string ModelName;
        public string ImageFileName;
        public int Width;
        public int Height;
        public long FileSize;
        public string ErrorMessage;
    }
}
