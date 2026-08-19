using System;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Reset;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class ConfirmResetDialog : MonoBehaviour
    {
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private InputField resetInput;
        [SerializeField] private Text messageText;
        [SerializeField] private Button careProgressButton;
        [SerializeField] private Button fullLocalDataButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;

        private ProgressResetMode selectedMode = ProgressResetMode.CareProgressOnly;
        private Action<bool> blockingChanged;
        private bool blockingNotified;

        public void SetBlockingCallback(Action<bool> onBlockingChanged)
        {
            NotifyBlocking(false);
            blockingChanged = onBlockingChanged;
        }

        public void Configure(
            GameObject root,
            InputField input,
            Text messageLabel,
            Button resetConfirmButton,
            Button resetCancelButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            Configure(
                root,
                input,
                messageLabel,
                null,
                null,
                resetConfirmButton,
                resetCancelButton,
                uiController,
                cheeseTamaVisual);
        }

        public void Configure(
            GameObject root,
            InputField input,
            Text messageLabel,
            Button careOnlyButton,
            Button fullResetButton,
            Button resetConfirmButton,
            Button resetCancelButton,
            MilkroomUIController uiController,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            UnbindControls();

            dialogRoot = root;
            resetInput = input;
            messageText = messageLabel;
            careProgressButton = careOnlyButton;
            fullLocalDataButton = fullResetButton;
            confirmButton = resetConfirmButton;
            cancelButton = resetCancelButton;
            milkroomUi = uiController;
            visualController = cheeseTamaVisual;

            BindControls();
            Close();
        }

        private void OnEnable()
        {
            BindControls();
            RefreshConfirmButtonState();
        }

        private void OnDisable()
        {
            UnbindControls();
            NotifyBlocking(false);
        }

        private void Update()
        {
            if (dialogRoot != null
                && dialogRoot.activeInHierarchy
                && GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        private void BindControls()
        {
            UnbindControls();

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmReset);
            }

            if (careProgressButton != null)
            {
                careProgressButton.onClick.AddListener(SelectCareProgressOnly);
            }

            if (fullLocalDataButton != null)
            {
                fullLocalDataButton.onClick.AddListener(SelectFullLocalData);
            }

            if (resetInput != null)
            {
                resetInput.onValueChanged.AddListener(HandleInputChanged);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Close);
            }
        }

        private void UnbindControls()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmReset);
            }

            if (careProgressButton != null)
            {
                careProgressButton.onClick.RemoveListener(SelectCareProgressOnly);
            }

            if (fullLocalDataButton != null)
            {
                fullLocalDataButton.onClick.RemoveListener(SelectFullLocalData);
            }

            if (resetInput != null)
            {
                resetInput.onValueChanged.RemoveListener(HandleInputChanged);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Close);
            }
        }

        public void Open()
        {
            selectedMode = ProgressResetMode.CareProgressOnly;
            if (resetInput != null)
            {
                resetInput.text = string.Empty;
            }

            RefreshModeUi();

            if (dialogRoot != null)
            {
                dialogRoot.SetActive(true);
            }

            NotifyBlocking(true);
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

            NotifyBlocking(false);
        }

        private void ConfirmReset()
        {
            if (!IsResetInputValid())
            {
                SetMessage(ProgressResetPolicy.BuildSummary(
                    ProgressResetPolicy.BuildPreview(selectedMode)));
                RefreshConfirmButtonState();
                return;
            }

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            var result = manager.TryResetProgress(selectedMode, resetInput?.text);
            if (!result.Succeeded)
            {
                SetMessage(string.IsNullOrWhiteSpace(result.Message)
                    ? "초기화를 실행하지 못했습니다. 로컬 저장은 변경되지 않았습니다."
                    : result.Message);
                RefreshConfirmButtonState();
                return;
            }

            milkroomUi?.Bind(manager.CurrentSave);
            milkroomUi?.ShowMessage(result.Message);
            visualController?.Bind(manager.CurrentTama);
            visualController?.React(false);
            UnityEngine.Object.FindFirstObjectByType<GameSettingsPanelController>(
                FindObjectsInactive.Include)?.RefreshFromSave(true);
            UnityEngine.Object.FindFirstObjectByType<AccessibilitySettingsPanelController>(
                FindObjectsInactive.Include)?.RefreshFromSave("접근성 설정을 다시 적용했습니다.");
            UnityEngine.Object.FindFirstObjectByType<MilkroomThemeController>(
                FindObjectsInactive.Include)?.ApplyTheme(manager.CurrentSave?.milkroomThemeId);
            UnityEngine.Object.FindFirstObjectByType<MilkroomLightingController>(
                FindObjectsInactive.Include)?.ApplyTheme(manager.CurrentSave?.milkroomThemeId);
            UnityEngine.Object.FindFirstObjectByType<MilkroomAmbientEventController>(
                FindObjectsInactive.Include)?.SetTheme(manager.CurrentSave?.milkroomThemeId);
            SetMessage(result.Message);
            Close();
        }

        private void HandleInputChanged(string value)
        {
            RefreshConfirmButtonState();
            if (IsResetInputValid())
            {
                SetMessage("확인 문구가 일치합니다. 선택한 범위만 초기화됩니다.");
            }
            else
            {
                SetMessage(ProgressResetPolicy.BuildSummary(
                    ProgressResetPolicy.BuildPreview(selectedMode)));
            }
        }

        private void SelectCareProgressOnly()
        {
            SelectMode(ProgressResetMode.CareProgressOnly);
        }

        private void SelectFullLocalData()
        {
            SelectMode(ProgressResetMode.FullLocalData);
        }

        private void SelectMode(ProgressResetMode mode)
        {
            selectedMode = mode;
            if (resetInput != null)
            {
                resetInput.text = string.Empty;
            }

            RefreshModeUi();
        }

        private void RefreshModeUi()
        {
            if (careProgressButton != null)
            {
                careProgressButton.interactable = selectedMode != ProgressResetMode.CareProgressOnly;
            }

            if (fullLocalDataButton != null)
            {
                fullLocalDataButton.interactable = selectedMode != ProgressResetMode.FullLocalData;
            }

            SetMessage(ProgressResetPolicy.BuildSummary(
                ProgressResetPolicy.BuildPreview(selectedMode)));
            RefreshConfirmButtonState();
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
                && ProgressResetPolicy.MatchesConfirmation(
                    ProgressResetPolicy.BuildPreview(selectedMode),
                    resetInput.text);
        }

        private void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        private void NotifyBlocking(bool blocked)
        {
            if (blockingNotified == blocked)
            {
                return;
            }

            blockingNotified = blocked;
            blockingChanged?.Invoke(blocked);
        }
    }
}
