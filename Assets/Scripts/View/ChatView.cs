using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualAiAssistant.View
{
    public class ChatView : MonoBehaviour
    {
        [SerializeField] private Button talkButton;
        [SerializeField] private TextMeshProUGUI talkButtonText;
        [SerializeField] private TextMeshProUGUI textBox;
        [SerializeField] private ScrollRect chatScroll;
        [SerializeField] private TextMeshProUGUI actionText;

        public event Action TalkButtonPressed;

        private void Awake()
        {
            talkButton.onClick.AddListener(OnTalkButtonPressed);
        }

        private void OnTalkButtonPressed()
        {
            TalkButtonPressed?.Invoke();
        }

        public void SetText(string text)
        {
            textBox.text = text;
        }

        public void EnableTalkButton(bool enable)
        {
            talkButton.interactable = enable;
        }

        public void ToggleTalkButtonText(bool isRecording)
        {
            talkButtonText.text = isRecording ? "Stop" : "Talk";
        }

        public void ScrollChatToBottom()
        {
            chatScroll.verticalNormalizedPosition = 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScroll.content);
        }

        public void UpdateAction(ChatAction action)
        {
            actionText.text = action.ToString() + "...";
        }
    }

    public enum ChatAction
    {
        Waiting,
        Listnening,
        Thinking,
        Talking,
    }
}

