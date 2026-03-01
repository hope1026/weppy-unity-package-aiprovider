namespace Weppy.AIProvider
{
    public partial class ImageResponse
    {
        public ImageResponse() { }

        private static ImageResponse FromErrorInternal(string errorMessage_)
        {
            return new ImageResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage_
            };
        }
    }
}
