namespace Weppy.AIProvider.Editor
{
    [System.Serializable]
    public class BgRemovalHistoryEntry
    {
        public string ProviderName;
        public string Timestamp;
        public bool IsSuccess;
        public string ErrorMessage;
        public string ImageFileName;
        public string OriginalImageFileName;
        public int Width;
        public int Height;
        public long FileSize;
    }
}
