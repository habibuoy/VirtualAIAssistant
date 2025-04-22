using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private WhisperManager whisperManager;
    [SerializeField] private MicrophoneRecord recorder;
    [SerializeField] private ChatView chatView;

    public bool IsRecording => recorder.IsRecording;

    private void Awake()
    {
        chatView.TalkButtonPressed += OnTalkButtonPressed;
        recorder.OnRecordStop += OnRecordStopped;
    }

    private void OnRecordStopped(AudioChunk recordedAudio)
    {
        // discard and process the audio
        _ = ProcessAudio(recordedAudio);

        chatView.ToggleTalkButtonText(false);
    }

    private async Task ProcessAudio(AudioChunk audio)
    {
        var result = await whisperManager.GetTextAsync(audio.Data, audio.Frequency, audio.Channels);
        if (result == null)
        {
            Debug.LogError("Failed to process audio.");
            return;
        }

        chatView.ScrollChatToBottom();
        chatView.SetText(result.Result);
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
