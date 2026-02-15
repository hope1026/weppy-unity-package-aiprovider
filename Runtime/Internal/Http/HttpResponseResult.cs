using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    internal class HttpResponseResult
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string Content { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public byte[] RawBytes { get; set; }

        public Dictionary<string, object> GetJsonContent()
        {
            if (string.IsNullOrEmpty(Content))
                return null;

            return JsonHelper.Deserialize(Content);
        }
    }
}
