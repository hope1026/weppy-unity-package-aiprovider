using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    internal class ClaudeCodeCliResponseResult
    {
        public bool IsSuccess { get; set; }
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public string ErrorMessage { get; set; }

        public Dictionary<string, object> GetJsonOutput()
        {
            if (string.IsNullOrEmpty(StandardOutput))
                return null;

            return JsonHelper.Deserialize(StandardOutput);
        }
    }
}
