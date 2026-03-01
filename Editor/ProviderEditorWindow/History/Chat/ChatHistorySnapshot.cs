using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    [System.Serializable]
    public class ChatHistorySnapshot
    {
        public string Id;
        public string DisplayName;
        public string CreatedAt;
        public int ItemCount;
        public List<ChatHistoryMessage> Messages;
    }
}
