using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;
using VirtualAiAssistant.Ai;
using VirtualAiAssistant.Ai.Implementations;
using VirtualAiAssistant.Tts;
using VirtualAiAssistant.View;

namespace VirtualAiAssistant
{
    public class ChatManager : MonoBehaviour
    {
        [SerializeField] private WhisperManager whisperManager;
        [SerializeField] private MicrophoneRecord recorder;
        [SerializeField] private ChatView chatView;
        [SerializeField] private CharacterView characterView;
        [SerializeField] private JetsTts ttsRunner;
        [SerializeField] private AiChatProvider aiChatProvider = AiChatProvider.Gemini;
        [SerializeField] private string chatApiKey;
        [SerializeField] private string modelName;
        [SerializeField] private bool generateSpeechUsingOfflineModel = true;

        private const string AIChatConfigPath = "AIChatConfig/config.json";

        public bool IsRecording => recorder.IsRecording;
        private IChatAi chatAi;

        private async void Awake()
        {
            chatView.TalkButtonPressed += OnTalkButtonPressed;
            recorder.OnRecordStop += OnRecordStopped;
            ttsRunner.ProcessCancelled += OnTtsCancelled;
            ttsRunner.SpeechStarted += OnSpeechStarted;
            ttsRunner.SpeechCompleted += OnSpeechCompleted;

            chatView.EnableTalkButton(false);

            AiChatConfig aiConfig = null;
            var keyFilePath = Path.Combine(Application.dataPath, AIChatConfigPath);
            if (File.Exists(keyFilePath))
            {
                var configString = File.ReadAllText(keyFilePath);
                aiConfig = JsonUtility.FromJson<AiChatConfig>(configString);
            }
            else
            {
                aiConfig = new AiChatConfig
                {
                    aiProvider = aiChatProvider.ToString(),
                    model = modelName,
                    apiKey = chatApiKey
                };
            }

            if (aiConfig == null
                || string.IsNullOrEmpty(aiConfig.apiKey))
            {
                Debug.LogWarning($"API key is not set. Please provide a valid API key. Either put in the editor or in a json file with key 'aiProvider', 'model' and 'apiKey' in: {keyFilePath}");
            }
            
            chatAi = ChatAiFactory.Create(aiConfig);
            if (await chatAi.IsModelValidAsync())
            {
                Debug.Log($"Chat model {chatAi.Model} is valid.");
                chatView.EnableTalkButton(true);
            }
            else
            {
                Debug.LogError($"Chat model {chatAi.Model} is not valid.");
            }
        }

        private void OnRecordStopped(AudioChunk recordedAudio)
        {
            // process the audio and discard the task
            _ = ProcessAudio(recordedAudio);
        }

        private void OnSpeechStarted()
        {
            characterView.FadeToTalking();
        }

        private void OnTtsCancelled()
        {
            characterView.FadeToIdle();
        }

        private void OnSpeechCompleted()
        {
            characterView.FadeToIdle();
        }

        private async Task ProcessAudio(AudioChunk audio)
        {
            chatView.ToggleTalkButtonText(false);
            characterView.FadeToIdle();

            var result = await whisperManager.GetTextAsync(audio.Data, audio.Frequency, audio.Channels);
            if (result == null)
            {
                Debug.LogError("Failed to process audio.");
                return;
            }

            chatView.ScrollChatToBottom();
            chatView.SetText(result.Result);
            chatView.EnableTalkButton(false);

            var chatResult = await chatAi.PromptChatAsync(result.Result);

            if (string.IsNullOrEmpty(chatResult))
            {
                chatView.EnableTalkButton(true);
                Debug.LogError("Failed to get chat response.");
                return;
            }

            bool useSpeechFromOnlineSource = false;

            if (!generateSpeechUsingOfflineModel
                && chatAi is ITtsAi ttsAi)
            {
                var speech = await ttsAi.PromptAudioAsync(chatResult);

                if (speech == null)
                {
                    Debug.LogWarning("Failed to process speech from AI");
                }
                else if (ttsRunner)
                {
                    chatView.SetText(chatResult);
                    ttsRunner.SpeakSpeech(speech);
                }

                useSpeechFromOnlineSource = true;
            }

            chatView.SetText(chatResult);
            chatView.EnableTalkButton(true);

            if (useSpeechFromOnlineSource) return;

            if (ttsRunner)
            {
                chatView.SetText(chatResult);
                ttsRunner.TextToSpeech(chatResult);
            }
        }

        private void OnTalkButtonPressed()
        {
            if (IsRecording)
            {
                StopRecording();
                return;
            }

            StartRecording();
            chatView.ToggleTalkButtonText(true);
            characterView.FadeToListening();
        }

        private void StartRecording()
        {
            recorder.StartRecord();
        }

        private void StopRecording()
        {
            recorder.StopRecord();
        }
    }
}

