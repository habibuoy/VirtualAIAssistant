using System.Threading.Tasks;

namespace VirtualAiAssistant.Ai
{
    public interface IChatAi
    {
        string Model { get; }

        Task<string> PromptChatAsync(string prompt);
        Task<bool> IsModelValidAsync();
    }
}