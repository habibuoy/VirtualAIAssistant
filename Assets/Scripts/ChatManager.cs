using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private WhisperManager whisperManager;
    [SerializeField] private MicrophoneRecord recorder;
    [SerializeField] private ChatView chatView;
    [SerializeField] private CharacterView characterView;
    [SerializeField] private JetsTts ttsRunner;
    [SerializeField] private string chatApiKey;
    [SerializeField] private string modelName;

    private const string AIChatConfigPath = "AIChatConfig/config.json";

    public bool IsRecording => recorder.IsRecording;
    private IChatAi chatPrompter;

    private async void Awake()
    {
        chatView.TalkButtonPressed += OnTalkButtonPressed;
        recorder.OnRecordStop += OnRecordStopped;
        ttsRunner.ProcessCompleted += OnTtsCompleted;
        ttsRunner.ProcessCancelled += OnTtsCancelled;
        ttsRunner.SpeechCompleted += OnSpeechCompleted;

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
                model = modelName,
                apiKey = chatApiKey
            };
        }

        if (aiConfig == null
            || string.IsNullOrEmpty(aiConfig.apiKey))
        {
            Debug.LogWarning($"API key is not set. Please provide a valid API key. Either put in the editor or in a json file with key 'model' and 'apiKey' in: {keyFilePath}");
        }

        chatPrompter = new GeminiAi(aiConfig.apiKey, aiConfig.model);
        if (await chatPrompter.IsModelValidAsync())
        {
            Debug.Log($"Chat model {chatPrompter.Model} is valid.");
        }
        else
        {
            Debug.LogError($"Chat model {chatPrompter.Model} is not valid.");
        }
    }

    private void OnRecordStopped(AudioChunk recordedAudio)
    {
        // process the audio and discard the task
        _ = ProcessAudio(recordedAudio);
    }

    private void OnTtsCompleted(float audioDuration)
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

        var chatResult = await chatPrompter.PromptChatAsync(result.Result);

        chatView.EnableTalkButton(true);

        if (string.IsNullOrEmpty(chatResult))
        {
            Debug.LogError("Failed to get chat response.");
            return;
        }

        chatView.SetText(chatResult);
        if (ttsRunner)
        {
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

public class AiChatConfig
{
    public string model;
    public string apiKey;
}
