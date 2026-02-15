using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    public partial class ChatCliRequestPayload
    {
        public ChatCliRequestPayload() { }

        public ChatCliRequestPayload(string userMessage_, string model_ = null)
        {
            Messages.Add(new ChatCliRequestMessage(ChatCliRequestMessageRoleType.USER, userMessage_));
            Model = model_;
        }

        private ChatCliRequestPayload WithSystemPromptInternal(string systemPrompt_)
        {
            SystemPrompt = systemPrompt_;
            return this;
        }

        private ChatCliRequestPayload WithTemperatureInternal(float temperature_)
        {
            Temperature = temperature_;
            return this;
        }

        private ChatCliRequestPayload WithMaxTokensInternal(int maxTokens_)
        {
            MaxTokens = maxTokens_;
            return this;
        }

        private ChatCliRequestPayload WithTopPInternal(float topP_)
        {
            TopP = topP_;
            return this;
        }

        private ChatCliRequestPayload WithStopInternal(List<string> stop_)
        {
            Stop = stop_;
            return this;
        }

        private ChatCliRequestPayload AddMessageInternal(ChatCliRequestMessage requestMessage_)
        {
            Messages.Add(requestMessage_);
            return this;
        }

        private ChatCliRequestPayload AddUserMessageInternal(string content_)
        {
            Messages.Add(new ChatCliRequestMessage(ChatCliRequestMessageRoleType.USER, content_));
            return this;
        }

        private ChatCliRequestPayload AddAssistantMessageInternal(string content_)
        {
            Messages.Add(new ChatCliRequestMessage(ChatCliRequestMessageRoleType.ASSISTANT, content_));
            return this;
        }

        private ChatCliRequestPayload WithAdditionalBodyParameterInternal(string key_, object value_)
        {
            if (AdditionalBodyParameters == null)
                AdditionalBodyParameters = new Dictionary<string, object>();

            AdditionalBodyParameters[key_] = value_;
            return this;
        }

        private ChatCliRequestPayload WithAdditionalBodyParametersInternal(Dictionary<string, object> parameters_)
        {
            if (parameters_ == null)
                return this;

            if (AdditionalBodyParameters == null)
                AdditionalBodyParameters = new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> kvp in parameters_)
            {
                AdditionalBodyParameters[kvp.Key] = kvp.Value;
            }
            return this;
        }
    }
}
