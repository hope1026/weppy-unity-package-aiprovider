namespace Weppy.AIProvider
{
    public partial class ChatCliResponse
    {
        public ChatCliResponse() { }

        private static ChatCliResponse FromErrorInternal(string errorMessage_)
        {
            return new ChatCliResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage_
            };
        }
    }
}
