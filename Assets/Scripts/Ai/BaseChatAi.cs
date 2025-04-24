using System;
using System.Threading.Tasks;
using UnityEngine;

namespace VirtualAiAssistant.Ai
{
    public abstract class BaseChatAi : IChatAi
    {
        private string model = "";
        protected string apiKey;

        protected abstract Uri BaseUrl { get; }
        protected abstract string DefaultModel { get; }

        public string Model => model;

        public BaseChatAi(string apiKey, string model)
        {
            this.apiKey = apiKey;
            this.model = string.IsNullOrEmpty(model) ? DefaultModel : model;

            Debug.Log($"{GetType()} initialized with model: {this.model}");
        }


        public abstract Task<bool> IsModelValidAsync();

        public abstract Task<string> PromptChatAsync(string prompt);
    }
}