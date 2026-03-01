namespace Weppy.AIProvider
{
    /// <summary>
    /// OpenAI-specific message options for chat requests.
    /// </summary>
    public class OpenAIChatRequestMessageOptions
    {
        public string Name { get; set; }

        /// <summary>
        /// Creates an empty options instance.
        /// </summary>
        public OpenAIChatRequestMessageOptions() { }

        /// <summary>
        /// Creates options with a message name.
        /// </summary>
        /// <param name="name_">Message name.</param>
        public OpenAIChatRequestMessageOptions(string name_)
        {
            Name = name_;
        }
    }
}
