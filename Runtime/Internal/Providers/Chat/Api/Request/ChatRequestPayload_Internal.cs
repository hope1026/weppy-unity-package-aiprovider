using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    public partial class ChatRequestPayload
    {
        public ChatRequestPayload() { }

        public ChatRequestPayload(string userMessage_, string model_ = null)
        {
            Messages.Add(new ChatRequestMessage(ChatRequestMessageRoleType.USER, userMessage_));
            Model = model_;
        }

        private ChatRequestPayload WithSystemPromptInternal(string systemPrompt_)
        {
            SystemPrompt = systemPrompt_;
            return this;
        }

        private ChatRequestPayload WithTemperatureInternal(float temperature_)
        {
            Temperature = temperature_;
            return this;
        }

        private ChatRequestPayload WithMaxTokensInternal(int maxTokens_)
        {
            MaxTokens = maxTokens_;
            return this;
        }

        private ChatRequestPayload WithTopPInternal(float topP_)
        {
            TopP = topP_;
            return this;
        }

        private ChatRequestPayload WithStopInternal(List<string> stop_)
        {
            Stop = stop_;
            return this;
        }

        private ChatRequestPayload AddMessageInternal(ChatRequestMessage requestMessageBase_)
        {
            Messages.Add(requestMessageBase_);
            return this;
        }

        private ChatRequestPayload AddUserMessageInternal(string content_)
        {
            Messages.Add(new ChatRequestMessage(ChatRequestMessageRoleType.USER, content_));
            return this;
        }

        private ChatRequestPayload AddAssistantMessageInternal(string content_)
        {
            Messages.Add(new ChatRequestMessage(ChatRequestMessageRoleType.ASSISTANT, content_));
            return this;
        }

        private ChatRequestPayload WithAdditionalBodyParameterInternal(string key_, object value_)
        {
            if (AdditionalBodyParameters == null)
                AdditionalBodyParameters = new Dictionary<string, object>();
            
            AdditionalBodyParameters[key_] = value_;
            return this;
        }

        private ChatRequestPayload WithAdditionalBodyParametersInternal(Dictionary<string, object> parameters_)
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
