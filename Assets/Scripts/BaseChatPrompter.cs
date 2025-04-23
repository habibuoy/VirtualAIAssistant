using System;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseChatPrompter : IChatPrompter
{
    private string model = "";
    protected string apiKey;

    protected abstract Uri BaseUrl { get; }
    protected abstract string DefaultModel { get; }

    public string Model => model;

    public BaseChatPrompter(string apiKey, string model)
    {
        this.apiKey = apiKey;
        this.model = string.IsNullOrEmpty(model) ? DefaultModel : model;

        Debug.Log($"{GetType()} initialized with model: {this.model}");
    }


    public abstract Task<bool> IsModelValidAsync();

    public abstract Task<string> PromptAsync(string prompt);
}