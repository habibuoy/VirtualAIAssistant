using System.Threading.Tasks;

public interface IChatAi
{
    string Model { get; }
    
    Task<string> PromptChatAsync(string prompt);
    Task<bool> IsModelValidAsync();
}