using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections.Generic;
using System.Net;

namespace VirtualAiAssistant.Ai.Implementations
{
    public class ChatGptAi : BaseChatAi, ITtsAi
    {
        private const string CompletionEndpoint = "v1/responses";
        private const string ModelsEndpoint = "v1/models/";
        private const string TtsEndpoint = "v1/audio/speech";
        private const string TtsModel = "gpt-4o-mini-tts";

        protected override Uri BaseUrl => new("https://api.openai.com/");
        protected override string DefaultModel => "gpt-4.1-nano";

        public ChatGptAi(string apiKey, string model)
            : base(apiKey, model) { }

        public override async Task<string> PromptChatAsync(string prompt)
        {
            using (var webrequest = new UnityWebRequest(BaseUrl + CompletionEndpoint, UnityWebRequest.kHttpVerbPOST))
            {
                var requestBody = new ChatGptRequestBody
                {
                    model = Model,
                    input = prompt
                };

                var jsonBody = JsonUtility.ToJson(requestBody);

                if (string.IsNullOrEmpty(jsonBody))
                {
                    Debug.LogError("Failed to serialize request body.");
                    return null;
                }

                webrequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
                webrequest.downloadHandler = new DownloadHandlerBuffer();

                webrequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webrequest.SetRequestHeader("Content-Type", "application/json");
                var request = webrequest.SendWebRequest();

                while (!request.isDone)
                {
                    await Task.Yield();
                }

                string responseBody = request.webRequest.downloadHandler.text;

                if (request.webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error: {request.webRequest.error}, message: {responseBody}");
                    return null;
                }

                var responseObject = JsonUtility.FromJson<ChatGptCompletionResponse>(responseBody);
                if (responseObject == null)
                {
                    Debug.LogError("Failed to parse response.");
                    return null;
                }

                return responseObject.Message;
            }
        }

        public override async Task<bool> IsModelValidAsync()
        {
            using (var webrequest = UnityWebRequest.Get(BaseUrl + ModelsEndpoint + Model))
            {
                webrequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webrequest.SetRequestHeader("Content-Type", "application/json");
                var request = webrequest.SendWebRequest();

                while (!request.isDone)
                {
                    await Task.Yield();
                }

                return request.webRequest.result == UnityWebRequest.Result.Success;
            }
        }

        public async Task<AudioClip> PromptAudioAsync(string text)
        {
            using (var webRequest = UnityWebRequestMultimedia.GetAudioClip(BaseUrl + TtsEndpoint, AudioType.MPEG))
            {
                Debug.Log("Running ChatGpt TTS");
                var requestBody = new ChatGptTtsRequestBody
                {
                    model = TtsModel,
                    input = text,
                    voice = "coral"
                };

                var jsonBody = JsonUtility.ToJson(requestBody);

                if (string.IsNullOrEmpty(jsonBody))
                {
                    Debug.LogError("Failed to serialize request body.");
                    return null;
                }

                webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
                webRequest.method = UnityWebRequest.kHttpVerbPOST;

                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.SetRequestHeader("Content-Type", "application/json");

                webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error while requesting audio: {webRequest.error}");
                    return null;
                }

                return DownloadHandlerAudioClip.GetContent(webRequest);
            }
        }
    }

    [Serializable]
    public class ChatGptRequestBody
    {
        public string model;
        public string input;
    }

    [Serializable]
    public class ChatGptTtsRequestBody : ChatGptRequestBody
    {
        public string voice;
    }

    [Serializable]
    public class ChatGptCompletionResponse
    {
    public string id;
    public string model;
    public List<ChatGptResponseOutput> output;

    public string Message => output[0]?.content[0].text ?? string.Empty;

        [Serializable]
        public class ChatGptResponseOutput
        {
            public string id;
            public string type;
            public string role;
            public List<ChatGptResponseOutputContent> content;

            [Serializable]
            public class ChatGptResponseOutputContent
            {
                public string type;
                public string text;
            }
        }
    }
}

