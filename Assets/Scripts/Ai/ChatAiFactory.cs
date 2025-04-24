using System;
using VirtualAiAssistant.Ai.Implementations;

namespace VirtualAiAssistant.Ai
{
    public class ChatAiFactory
    {
        public static IChatAi Create(AiChatConfig config)
        {
            AiChatProvider provider;

            if (!Enum.TryParse(config.aiProvider, out provider))
            {
                throw new InvalidOperationException("Failed to parse AI provider! Please provide a valid AI provider!");
            }

            return Create(provider, config.model, config.apiKey);
        }

        public static IChatAi Create(AiChatProvider aiChatProvider, string model, string apiKey)
        {
            switch (aiChatProvider)
            {
                default:
                    return new GeminiAi(apiKey, model);
                case AiChatProvider.ChatGPT:
                    return new ChatGptAi(apiKey, model);
            }
        }
    }

    public class AiChatConfig
    {
        public string aiProvider;
        public string model;
        public string apiKey;
    }

    public enum AiChatProvider
    {
        ChatGPT,
        Gemini
    }
}