using System;
using CheeseTama.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class ConfirmResetDialog : MonoBehaviour
    {
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private InputField resetInput;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;

        public void Configure(
            GameObject root,
            InputField input,
            Text messageLabel,
            Button resetConfirmButton,
            Button resetCancelButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            dialogRoot = root;
            resetInput = input;
            messageText = messageLabel;
            confirmButton = resetConfirmButton;
            cancelButton = resetCancelButton;
            milkroomUi = uiController;
            visualController = cheeseTamaVisual;

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmReset);
                confirmButton.onClick.AddListener(ConfirmReset);
            }

            if (resetInput != null)
            {
                resetInput.onValueChanged.RemoveListener(HandleInputChanged);
                resetInput.onValueChanged.AddListener(HandleInputChanged);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Close);
                cancelButton.onClick.AddListener(Close);
            }

            Close();
        }

        public void Open()
        {
            if (resetInput != null)
            {
                resetInput.text = string.Empty;
            }

            SetMessage("Type RESET in the box below. The Reset button unlocks only after the text matches.");
            RefreshConfirmButtonState();

            if (dialogRoot != null)
            {
                dialogRoot.SetActive(true);
            }

            if (resetInput != null)
            {
                resetInput.ActivateInputField();
                resetInput.Select();
            }
        }

        public void Close()
        {
            if (dialogRoot != null)
            {
                dialogRoot.SetActive(false);
            }
        }

        private void ConfirmReset()
        {
            if (!IsResetInputValid())
            {
                SetMessage("Reset is locked. Type RESET exactly, then press Reset.");
                RefreshConfirmButtonState();
                return;
            }

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            manager.ResetGame();
            milkroomUi?.Bind(manager.CurrentSave);
            milkroomUi?.ShowMessage("Save data reset.");
            visualController?.Bind(manager.CurrentTama);
            visualController?.React(false);
            SetMessage("Save data reset.");
            Close();
        }

        private void HandleInputChanged(string value)
        {
            RefreshConfirmButtonState();
            if (IsResetInputValid())
            {
                SetMessage("RESET matched. Press Reset to clear local progress.");
            }
            else
            {
                SetMessage("Type RESET in the box below. The Reset button unlocks only after the text matches.");
            }
        }

        private void RefreshConfirmButtonState()
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = IsResetInputValid();
            }
        }

        private bool IsResetInputValid()
        {
            return resetInput != null
                && string.Equals(resetInput.text.Trim(), "RESET", StringComparison.Ordinal);
        }

        private void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }
    }
}
