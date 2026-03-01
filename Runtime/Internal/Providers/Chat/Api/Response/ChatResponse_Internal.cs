namespace Weppy.AIProvider
{
    public partial class ChatResponse
    {
        public ChatResponse() { }

        public ChatResponse(string content_)
        {
            Content = content_;
        }

        protected ChatResponse(bool isSuccess_, string errorMessage_)
        {
            IsSuccess = isSuccess_;
            ErrorMessage = errorMessage_;
        }

        private static ChatResponse FromErrorInternal(string errorMessage_)
        {
            return new ChatResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage_
            };
        }
    }
}
