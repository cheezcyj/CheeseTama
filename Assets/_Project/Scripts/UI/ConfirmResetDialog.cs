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

            SetMessage("Type RESET to clear all local CheeseTama progress.");

            if (dialogRoot != null)
            {
                dialogRoot.SetActive(true);
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
            if (resetInput == null || resetInput.text != "RESET")
            {
                SetMessage("Reset blocked. Type RESET exactly to continue.");
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

        private void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }
    }
}
