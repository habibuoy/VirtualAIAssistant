using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;
using VirtualAiAssistant.Ai;
using VirtualAiAssistant.Tts;
using VirtualAiAssistant.View;
using System.Collections.Generic;
using System;

namespace VirtualAiAssistant
{
    public class ChatManager : MonoBehaviour
    {
        [SerializeField] private WhisperManager whisperManager;
        [SerializeField] private MicrophoneRecord recorder;
        [SerializeField] private ChatView chatView;
        [SerializeField] private CharacterView characterView;
        [SerializeField] private ChatOptionView chatOptionView;
        [SerializeField] private JetsTts ttsRunner;
        [SerializeField] private bool generateSpeechUsingOfflineModel = true;

        private const string AIChatConfigPath = "AIChatConfig/config.json";
        private const string BlankAudioText = "[BLANK_AUDIO]";

        private readonly Dictionary<string, IChatAi> aiChatProviders = new();

        public bool IsRecording => recorder.IsRecording;
        private IChatAi chatAi;

        private async void Awake()
        {
            chatView.TalkButtonPressed += OnTalkButtonPressed;
            recorder.OnRecordStop += OnRecordStopped;
            ttsRunner.ProcessCancelled += OnTtsCancelled;
            ttsRunner.SpeechStarted += OnSpeechStarted;
            ttsRunner.SpeechCompleted += OnSpeechCompleted;
            chatOptionView.OfflineTtsChanged += OnOfflineTtsToggleChanged;
            chatOptionView.AiChatProviderChanged += OnAiChatDropdownProviderChanged;

            chatView.EnableTalkButton(false);
            chatOptionView.EnableAiChatProviderDropdown(false);

            AiChatConfiguration aiConfigs = null;
            var keyFilePath = Path.Combine(Application.dataPath, AIChatConfigPath);
            if (File.Exists(keyFilePath))
            {
                var configString = File.ReadAllText(keyFilePath);
                aiConfigs = JsonUtility.FromJson<AiChatConfiguration>(configString);

                List<string> providers = new();

                foreach (var config in aiConfigs.configs)
                {
                    var aiProvider = config.aiProvider;
                    var aiChat = ChatAiFactory.Create(config);
                    if (!await aiChat.IsModelValidAsync())
                    {
                        Debug.LogWarning($"Model {aiChat.Model} of {aiChat.GetType()} is not valid");
                        continue;
                    }

                    aiChatProviders.TryAdd(aiProvider, aiChat);
                    if (!providers.Contains(aiProvider))
                    {
                        providers.Add(aiProvider);
                    }

                    if (config.aiProvider == "Gemini")
                    {
                        chatAi = aiChat;
                    }
                }

                chatOptionView.SetupAiChatProviderDropdown(providers);
            }

            if (aiConfigs == null
                || aiChatProviders.Count == 0)
            {
                Debug.LogError($"Please provde a config json file in: {keyFilePath}");
                return;
            }

            chatView.EnableTalkButton(true);
            chatView.UpdateAction(ChatAction.Waiting);
            chatOptionView.ChangeOfflineTtsToggle(true);
            chatOptionView.EnableAiChatProviderDropdown(true);
        }

        private void OnAiChatDropdownProviderChanged(string provider)
        {
            if (!aiChatProviders.TryGetValue(provider, out chatAi))
            {
                throw new InvalidOperationException($"Ai Chat Provider {provider} does not exist here!");
            }
        }

        private void OnOfflineTtsToggleChanged(bool isOn)
        {
            generateSpeechUsingOfflineModel = isOn;
        }

        private void OnRecordStopped(AudioChunk recordedAudio)
        {
            // process the audio and discard the task
            _ = ProcessAudio(recordedAudio);
        }

        private void OnSpeechStarted()
        {
            characterView.FadeToTalking();
            chatView.UpdateAction(ChatAction.Talking);
        }

        private void OnTtsCancelled()
        {
            SetWaiting();
        }

        private void OnSpeechCompleted()
        {
            SetWaiting();
        }

        private void SetWaiting()
        {
            characterView.FadeToIdle();
            chatView.UpdateAction(ChatAction.Waiting);
            chatOptionView.EnableOfflineTtsToggle(true);
            chatOptionView.EnableAiChatProviderDropdown(true);
        }

        private async Task ProcessAudio(AudioChunk audio)
        {
            chatView.ToggleTalkButtonText(false);
            chatView.UpdateAction(ChatAction.Thinking);
            characterView.FadeToIdle();
            chatOptionView.EnableOfflineTtsToggle(false);
            chatOptionView.EnableAiChatProviderDropdown(false);

            var result = await whisperManager.GetTextAsync(audio.Data, audio.Frequency, audio.Channels);
            if (result == null)
            {
                Debug.LogError("Failed to process audio.");
                return;
            }

            if (result.Result.Trim() == BlankAudioText)
            {
                chatView.SetText("<i><b>Your Voice Was Not Recorded Correctly, Please Ensure Your Mic is Functioning Properly and try again.</b></i>");
                SetWaiting();
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
            chatView.UpdateAction(ChatAction.Listnening);
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

