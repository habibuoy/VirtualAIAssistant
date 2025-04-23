using System.Threading.Tasks;

public interface IChatPrompter
{
    string Model { get; }
    
    Task<string> PromptAsync(string prompt);
    Task<bool> IsModelValidAsync();
}