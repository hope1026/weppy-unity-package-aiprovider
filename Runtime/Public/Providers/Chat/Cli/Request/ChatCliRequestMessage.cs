namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Represents a single CLI chat message in a request.
    /// </summary>
    public partial class ChatCliRequestMessage
    {
        public ChatCliRequestMessageRoleType RequestMessageRoleType { get; set; }
        public string Content { get; set; }

        /// <summary>
        /// Parses a role string into a role enum.
        /// </summary>
        /// <param name="role_">Role string.</param>
        /// <returns>Parsed role type.</returns>
        public static ChatCliRequestMessageRoleType ParseRole(string role_)
        {
            return ParseRoleInternal(role_);
        }

        /// <summary>
        /// Gets the role string for this message.
        /// </summary>
        /// <returns>Role string.</returns>
        public string GetRoleString()
        {
            return GetRoleStringInternal();
        }
    }
}
