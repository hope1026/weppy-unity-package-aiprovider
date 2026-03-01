using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    [System.Serializable]
    public class BgRemovalHistorySnapshot
    {
        public string Id;
        public string DisplayName;
        public string CreatedAt;
        public int ItemCount;
        public List<BgRemovalHistoryEntry> Entries;
    }
}
