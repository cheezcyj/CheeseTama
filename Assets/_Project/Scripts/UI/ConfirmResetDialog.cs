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

            SetMessage("아래 입력칸에 RESET을 입력하세요. 정확히 일치해야 초기화 버튼이 열립니다.");
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
                SetMessage("초기화가 잠겨 있습니다. RESET을 정확히 입력한 뒤 초기화를 누르세요.");
                RefreshConfirmButtonState();
                return;
            }

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            manager.ResetGame();
            milkroomUi?.Bind(manager.CurrentSave);
            milkroomUi?.ShowMessage("저장 데이터를 초기화했습니다.");
            visualController?.Bind(manager.CurrentTama);
            visualController?.React(false);
            SetMessage("저장 데이터를 초기화했습니다.");
            Close();
        }

        private void HandleInputChanged(string value)
        {
            RefreshConfirmButtonState();
            if (IsResetInputValid())
            {
                SetMessage("RESET이 일치합니다. 초기화를 누르면 로컬 진행도가 삭제됩니다.");
            }
            else
            {
                SetMessage("아래 입력칸에 RESET을 입력하세요. 정확히 일치해야 초기화 버튼이 열립니다.");
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
