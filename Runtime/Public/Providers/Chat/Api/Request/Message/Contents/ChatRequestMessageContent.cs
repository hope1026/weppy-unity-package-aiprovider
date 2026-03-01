namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a multi-part content item in a chat message.
    /// </summary>
    public class ChatRequestMessageContent
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public ChatRequestMessageImageContent Image { get; set; }
        public ChatRequestMessageDocumentContent Document { get; set; }

        /// <summary>
        /// Creates a text content part.
        /// </summary>
        /// <param name="text_">Text content.</param>
        /// <returns>Content part.</returns>
        public static ChatRequestMessageContent CreateText(string text_) => new ChatRequestMessageContent { Type = "text", Text = text_ };

        /// <summary>
        /// Creates an image content part from base64 data.
        /// </summary>
        /// <param name="base64Data_">Base64 image data.</param>
        /// <param name="mediaType_">Image media type.</param>
        /// <returns>Content part.</returns>
        public static ChatRequestMessageContent CreateImage(string base64Data_, string mediaType_ = "image/png") =>
            new ChatRequestMessageContent { Type = "image", Image = new ChatRequestMessageImageContent { Base64Data = base64Data_, MediaType = mediaType_ } };

        /// <summary>
        /// Creates an image content part from a URL.
        /// </summary>
        /// <param name="url_">Image URL.</param>
        /// <returns>Content part.</returns>
        public static ChatRequestMessageContent CreateImageUrl(string url_) =>
            new ChatRequestMessageContent { Type = "image_url", Image = new ChatRequestMessageImageContent { Url = url_ } };

        /// <summary>
        /// Creates a document content part from base64 data.
        /// </summary>
        /// <param name="base64Data_">Base64 document data.</param>
        /// <param name="mediaType_">Document media type.</param>
        /// <param name="fileName_">Optional file name.</param>
        /// <returns>Content part.</returns>
        public static ChatRequestMessageContent CreateDocument(string base64Data_, string mediaType_, string fileName_ = null) =>
            new ChatRequestMessageContent
            {
                Type = "document",
                Document = new ChatRequestMessageDocumentContent { Base64Data = base64Data_, MediaType = mediaType_, FileName = fileName_ }
            };

        /// <summary>
        /// Creates a text file content part.
        /// </summary>
        /// <param name="textContent_">Text file content.</param>
        /// <param name="mediaType_">Text media type.</param>
        /// <param name="fileName_">Optional file name.</param>
        /// <returns>Content part.</returns>
        public static ChatRequestMessageContent CreateTextFile(string textContent_, string mediaType_ = "text/plain", string fileName_ = null) =>
            new ChatRequestMessageContent
            {
                Type = "text_file",
                Document = new ChatRequestMessageDocumentContent { TextContent = textContent_, MediaType = mediaType_, FileName = fileName_ }
            };
    }
}
