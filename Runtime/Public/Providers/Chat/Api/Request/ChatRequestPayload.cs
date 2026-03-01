using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a chat request payload with messages and options.
    /// </summary>
    public partial class ChatRequestPayload
    {
        public List<ChatRequestMessage> Messages { get; } = new List<ChatRequestMessage>();
        public string Model { get; set; }
        public float? Temperature { get; private set; }
        public int? MaxTokens { get; private set; }
        public float? TopP { get; private set; }
        public List<string> Stop { get; private set; }
        public string SystemPrompt { get; private set; }
        public Dictionary<string, object> AdditionalBodyParameters { get; private set; }

        public OpenAIChatRequestOptions OpenAIOptions { get; set; }
        public GoogleChatRequestOptions GoogleOptions { get; set; }
        public AnthropicChatRequestOptions AnthropicOptions { get; set; }
        public HuggingFaceChatRequestOptions HuggingFaceOptions { get; set; }
        public OpenRouterChatRequestOptions OpenRouterOptions { get; set; }

        /// <summary>
        /// Sets the system prompt for the request.
        /// </summary>
        /// <param name="systemPrompt_">System prompt text.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload WithSystemPrompt(string systemPrompt_)
        {
            return WithSystemPromptInternal(systemPrompt_);
        }

        /// <summary>
        /// Sets the sampling temperature.
        /// </summary>
        /// <param name="temperature_">Temperature value.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload WithTemperature(float temperature_)
        {
            return WithTemperatureInternal(temperature_);
        }

        /// <summary>
        /// Sets the maximum token count.
        /// </summary>
        /// <param name="maxTokens_">Maximum tokens.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload WithMaxTokens(int maxTokens_)
        {
            return WithMaxTokensInternal(maxTokens_);
        }

        /// <summary>
        /// Sets nucleus sampling probability.
        /// </summary>
        /// <param name="topP_">Top-p value.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload WithTopP(float topP_)
        {
            return WithTopPInternal(topP_);
        }

        /// <summary>
        /// Sets stop sequences.
        /// </summary>
        /// <param name="stop_">Stop tokens list.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload WithStop(List<string> stop_)
        {
            return WithStopInternal(stop_);
        }

        /// <summary>
        /// Adds a message to the payload.
        /// </summary>
        /// <param name="requestMessageBase_">Message to add.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload AddMessage(ChatRequestMessage requestMessageBase_)
        {
            return AddMessageInternal(requestMessageBase_);
        }

        /// <summary>
        /// Adds a user message to the payload.
        /// </summary>
        /// <param name="content_">Message content.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload AddUserMessage(string content_)
        {
            return AddUserMessageInternal(content_);
        }

        /// <summary>
        /// Adds an assistant message to the payload.
        /// </summary>
        /// <param name="content_">Message content.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload AddAssistantMessage(string content_)
        {
            return AddAssistantMessageInternal(content_);
        }

        /// <summary>
        /// Adds a custom body parameter.
        /// </summary>
        /// <param name="key_">Parameter key.</param>
        /// <param name="value_">Parameter value.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload WithAdditionalBodyParameter(string key_, object value_)
        {
            return WithAdditionalBodyParameterInternal(key_, value_);
        }

        /// <summary>
        /// Adds custom body parameters.
        /// </summary>
        /// <param name="parameters_">Parameters to merge.</param>
        /// <returns>The updated payload.</returns>
        public ChatRequestPayload WithAdditionalBodyParameters(Dictionary<string, object> parameters_)
        {
            return WithAdditionalBodyParametersInternal(parameters_);
        }
    }
}
