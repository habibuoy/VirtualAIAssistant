using System;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualAiAssistant.View
{
    public class ChatOptionView : MonoBehaviour
    {
        [SerializeField] private Toggle offlineTtsToggle;

        public Action<bool> OfflineTtsChanged;

        private void Awake()
        {
            offlineTtsToggle.onValueChanged.AddListener(OnOfflineTtsToggleValueChanged);
        }

        private void OnDestroy()
        {
            offlineTtsToggle.onValueChanged.RemoveListener(OnOfflineTtsToggleValueChanged);
            OfflineTtsChanged = null;
        }

        private void OnOfflineTtsToggleValueChanged(bool isOn)
        {
            OfflineTtsChanged?.Invoke(isOn);
        }

        public void ChangeOfflineTtsToggle(bool enable)
        {
            offlineTtsToggle.isOn = enable;
        }

        public void EnableOfflineTtsToggle(bool interactable)
        {
            offlineTtsToggle.interactable = interactable;
        }
    }
}