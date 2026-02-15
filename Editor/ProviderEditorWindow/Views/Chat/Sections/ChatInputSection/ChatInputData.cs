using System.Collections.Generic;

namespace Weppy.AIProvider.Chat.Editor
{
    public class ChatInputData
    {
        public string UserPrompt { get; set; }
        public string SystemPrompt { get; set; }
        public List<AttachedFileData> AttachedFiles { get; set; }
        public bool SendToAll { get; set; }
        public bool UseStreaming { get; set; }
        public bool UsePersistent { get; set; }
    }
}