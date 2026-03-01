using System;

namespace Weppy.AIProvider
{
    public partial class BgRemovalResponse
    {
        public BgRemovalResponse() { }

        private static BgRemovalResponse FromErrorInternal(string errorMessage_)
        {
            return new BgRemovalResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage_
            };
        }

        private byte[] GetImageBytesInternal()
        {
            if (string.IsNullOrEmpty(Base64Image))
                return null;

            try
            {
                return Convert.FromBase64String(Base64Image);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
