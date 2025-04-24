using System.Threading.Tasks;
using UnityEngine;

namespace VirtualAiAssistant.Ai
{
    public interface ITtsAi
    {
        Task<AudioClip> PromptAudioAsync(string text);
    }
}