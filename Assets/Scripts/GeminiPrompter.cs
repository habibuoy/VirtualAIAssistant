using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections.Generic;

public class GeminiPrompter : BaseChatPrompter
{
    private const string ModelsEndpoint = "v1beta/models/";
    private const string TextGenerationParam = ":generateContent";

    private string apiKeyParam;

    protected override Uri BaseUrl => new ("https://generativelanguage.googleapis.com/");
    protected override string DefaultModel => "gemini-2.0-flash";

    public GeminiPrompter(string apiKey, string model)
        : base(apiKey, model)
    {
        apiKeyParam = $"?key={apiKey}";
    }

    public override async Task<string> PromptAsync(string prompt)
    {
        using (var webrequest = new UnityWebRequest(BaseUrl + ModelsEndpoint + Model + $"{TextGenerationParam}{apiKeyParam}",
            UnityWebRequest.kHttpVerbPOST))
        {
            var jsonBody = JsonUtility.ToJson(GeminiApiRequestBody.WithPrompt(prompt));

            if (string.IsNullOrEmpty(jsonBody))
            {
                Debug.LogError("Failed to serialize request body.");
                return null;
            }

            webrequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
            webrequest.downloadHandler = new DownloadHandlerBuffer();

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

            var responseObject = JsonUtility.FromJson<GeminiApiResponseBody>(responseBody);
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
        using (var webrequest = UnityWebRequest.Get(BaseUrl + ModelsEndpoint + apiKeyParam))
        {
            var request = webrequest.SendWebRequest();

            while (!request.isDone)
            {
                await Task.Yield();
            }

            return request.webRequest.result == UnityWebRequest.Result.Success;
        }
    }
}

[Serializable]
public class GeminiApiRequestBody
{
    public List<Content> contents;

    [Serializable]
    public class Content
    {
        public List<Part> parts;

        [Serializable]
        public class Part
        {
            public string text;
        }
    }

    public static GeminiApiRequestBody WithPrompt(string inputPrompt)
    {
        return new GeminiApiRequestBody
        {
            contents = new List<Content>
            {
                new Content
                {
                    parts = new List<Content.Part>
                    {
                        new() { text = inputPrompt }
                    }
                }
            }
        };
    }
}

[Serializable]
public class GeminiApiResponseBody
{
    public List<Canditate> candidates;
    public string modelVersion;

    public string Message =>
        candidates?[0]?.content?.parts[0]?.text ?? string.Empty;

    [Serializable]
    public class Canditate
    {
        public Content content;
        public string finishReason;
        public float avgLogprobs;

        [Serializable]
        public class Content
        {
            public List<Part> parts;
            public string role;

            [Serializable]
            public class Part
            {
                public string text;
            }
        }
    }
}