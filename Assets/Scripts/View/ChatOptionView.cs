using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualAiAssistant.View
{
    public class ChatOptionView : MonoBehaviour
    {
        [SerializeField] private Toggle offlineTtsToggle;
        [SerializeField] private TMP_Dropdown aiChatProviderDropdown;

        public Action<bool> OfflineTtsChanged;
        public Action<string> AiChatProviderChanged;

        private void Awake()
        {
            offlineTtsToggle.onValueChanged.AddListener(OnOfflineTtsToggleValueChanged);
        }

        private void OnDestroy()
        {
            offlineTtsToggle.onValueChanged.RemoveListener(OnOfflineTtsToggleValueChanged);
            aiChatProviderDropdown.onValueChanged.RemoveListener(OnAiProviderDropdownValueChanged);
            OfflineTtsChanged = null;
        }

        private void OnAiProviderDropdownValueChanged(int index)
        {
            AiChatProviderChanged?.Invoke(aiChatProviderDropdown.options[index].text);
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

        public void SetupAiChatProviderDropdown(List<string> optionText)
        {
            aiChatProviderDropdown.onValueChanged.RemoveListener(OnAiProviderDropdownValueChanged);
            aiChatProviderDropdown.onValueChanged.AddListener(OnAiProviderDropdownValueChanged);
            aiChatProviderDropdown.ClearOptions();
            aiChatProviderDropdown.AddOptions(optionText);
        }

        public void EnableAiChatProviderDropdown(bool interactable)
        {
            aiChatProviderDropdown.interactable = interactable;
        }

        public void ChooseAiProvider(int index)
        {
            aiChatProviderDropdown.value = index;
        }
    }
}