using System;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Platform;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class DataManagementPanelController : MonoBehaviour
    {
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button exportButton;
        [SerializeField] private Button selectImportButton;
        [SerializeField] private InputField importConfirmationInput;
        [SerializeField] private Button applyImportButton;
        [SerializeField] private Text statusText;
        [SerializeField] private ConfirmResetDialog resetDialog;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private CheeseTamaVisualController visualController;

        private readonly SaveTransferImportSession importSession = new SaveTransferImportSession();
        private SaveTransferFileBridge transferFileBridge;

        public void Configure(
            Button manualSaveButton,
            Button manualLoadButton,
            Button openResetDialogButton,
            Text dataStatusText,
            ConfirmResetDialog confirmResetDialog,
            MilkroomUIController uiController,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            UnbindControls();

            saveButton = manualSaveButton;
            loadButton = manualLoadButton;
            resetButton = openResetDialogButton;
            statusText = dataStatusText;
            resetDialog = confirmResetDialog;
            milkroomUi = uiController;
            visualController = cheeseTamaVisual;

            BindControls();
        }

        public void ConfigureSaveTransfer(
            Button exportSaveButton,
            Button chooseImportFileButton,
            InputField confirmationInput,
            Button confirmImportButton)
        {
            UnbindControls();
            exportButton = exportSaveButton;
            selectImportButton = chooseImportFileButton;
            importConfirmationInput = confirmationInput;
            applyImportButton = confirmImportButton;
            importSession.Clear();
            EnsureTransferFileBridge();
            ClearImportConfirmation();
            BindControls();
            RefreshImportState();
        }

        private void OnEnable()
        {
            BindControls();
        }

        private void OnDisable()
        {
            UnbindControls();
        }

        private void BindControls()
        {
            UnbindControls();

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(Save);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(Load);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OpenResetDialog);
            }

            if (exportButton != null)
            {
                exportButton.onClick.AddListener(ExportCurrentSave);
            }

            if (selectImportButton != null)
            {
                selectImportButton.onClick.AddListener(SelectImportFile);
            }

            if (applyImportButton != null)
            {
                applyImportButton.onClick.AddListener(ApplyPendingImport);
            }

            if (importConfirmationInput != null)
            {
                importConfirmationInput.onValueChanged.AddListener(HandleImportConfirmationChanged);
            }

            EnsureTransferFileBridge();
            if (transferFileBridge != null)
            {
                transferFileBridge.ImportCompleted -= HandleImportFileLoaded;
                transferFileBridge.ImportFailed -= HandleImportFileFailed;
                transferFileBridge.ImportCompleted += HandleImportFileLoaded;
                transferFileBridge.ImportFailed += HandleImportFileFailed;
            }
        }

        private void UnbindControls()
        {
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(Save);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(Load);
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(OpenResetDialog);
            }

            if (exportButton != null)
            {
                exportButton.onClick.RemoveListener(ExportCurrentSave);
            }

            if (selectImportButton != null)
            {
                selectImportButton.onClick.RemoveListener(SelectImportFile);
            }

            if (applyImportButton != null)
            {
                applyImportButton.onClick.RemoveListener(ApplyPendingImport);
            }

            if (importConfirmationInput != null)
            {
                importConfirmationInput.onValueChanged.RemoveListener(HandleImportConfirmationChanged);
            }

            if (transferFileBridge != null)
            {
                transferFileBridge.ImportCompleted -= HandleImportFileLoaded;
                transferFileBridge.ImportFailed -= HandleImportFileFailed;
            }
        }

        private void Save()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            manager.SaveGame();
            RefreshBoundViews(manager, "수동 저장을 완료했습니다.");
        }

        private void Load()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            manager.ReloadGame();
            var message = manager.LastTimeProgression.applied
                ? manager.LastTimeProgression.ToSummary("비운 사이")
                : "저장 데이터를 불러왔습니다.";
            GetComponent<GameSettingsPanelController>()?.RefreshFromSave(true);
            RefreshBoundViews(manager, message);
        }

        private void OpenResetDialog()
        {
            if (resetDialog != null)
            {
                resetDialog.Open();
            }
        }

        public void ExportCurrentSave()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager?.CurrentSave == null)
            {
                SetStatus("백업할 게임 기록을 찾지 못했어요.");
                return;
            }

            EnsureTransferFileBridge();
            if (transferFileBridge == null)
            {
                SetStatus("지금은 백업 파일을 만들 수 없어요.");
                return;
            }

            var now = DateTimeOffset.UtcNow;
            if (!SaveTransferCodec.TrySerialize(
                    manager.CurrentSave,
                    now,
                    out var envelopeJson,
                    out var errorMessage))
            {
                SetStatus(errorMessage);
                return;
            }

            if (!transferFileBridge.TryExport(
                    envelopeJson,
                    SaveTransferCodec.CreateFileName(now),
                    out var exportMessage))
            {
                SetStatus(exportMessage);
                return;
            }

            SetStatus("지금까지 한 게임을 백업 파일로 만들었어요. " + exportMessage);
        }

        public void SelectImportFile()
        {
            importSession.Clear();
            ClearImportConfirmation();
            RefreshImportState();
            EnsureTransferFileBridge();
            if (transferFileBridge == null)
            {
                SetStatus("지금은 백업 파일을 고를 수 없어요.");
                return;
            }

            SetStatus("불러올 CheeseTama 백업 파일을 골라 주세요.");
            transferFileBridge.RequestImport();
        }

        public void ApplyPendingImport()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            var authorization = importSession.Authorize(
                importConfirmationInput?.text,
                manager?.CurrentSave);
            if (!authorization.IsAuthorized)
            {
                SetStatus(authorization.Message);
                RefreshImportState();
                return;
            }

            var saveManager = manager.GetComponent<SaveManager>();
            if (saveManager == null)
            {
                SetStatus("이 기기의 게임 기록을 찾지 못해 멈췄어요. 기존 기록은 그대로예요.");
                return;
            }

            EnsureTransferFileBridge();
            if (transferFileBridge == null)
            {
                SetStatus("지금 기록을 먼저 백업하지 못해 멈췄어요. 기존 기록은 그대로예요.");
                return;
            }

            if (!SaveTransferCodec.TrySerialize(
                    manager.CurrentSave,
                    DateTimeOffset.UtcNow,
                    out var backupEnvelope,
                    out var backupError))
            {
                SetStatus(backupError);
                return;
            }

            var backupNow = DateTimeOffset.UtcNow;
            if (!transferFileBridge.TryExport(
                    backupEnvelope,
                    SaveTransferCodec.CreateFileName(backupNow, true),
                    out var backupMessage))
            {
                SetStatus("지금 기록을 먼저 백업하지 못했어요. " + backupMessage);
                return;
            }

            if (!saveManager.TryReplaceFromCloudPayload(authorization.Payload, out _))
            {
                SetStatus("백업은 만들었지만 고른 기록을 불러오지 못했어요. 기존 기록은 그대로예요.");
                return;
            }

            importSession.Clear();
            ClearImportConfirmation();
            manager.ReloadGame();
            RebindImportedState(manager);
            RefreshImportState();
            RefreshBoundViews(
                manager,
                "이전 기록을 백업한 뒤, 고른 게임 기록을 불러왔어요.");
        }

        private void HandleImportFileLoaded(string envelopeJson)
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            var validation = importSession.Begin(envelopeJson, manager?.CurrentSave);
            ClearImportConfirmation();
            RefreshImportState();
            if (!validation.IsValid)
            {
                SetStatus(validation.Message + " 기존 저장은 변경되지 않았습니다.");
                return;
            }

            SetStatus(
                validation.Preview.ToSummary()
                + $"\n불러오려면 확인 칸에 ‘{SaveTransferImportSession.ConfirmationPhrase}’라고 입력해 주세요.");
        }

        private void HandleImportFileFailed(string message)
        {
            importSession.Clear();
            ClearImportConfirmation();
            RefreshImportState();
            SetStatus(message);
        }

        private void HandleImportConfirmationChanged(string _)
        {
            RefreshImportState();
        }

        private void RefreshImportState()
        {
            var hasPending = importSession.HasPendingImport;
            if (importConfirmationInput != null)
            {
                importConfirmationInput.gameObject.SetActive(hasPending);
            }

            if (applyImportButton != null)
            {
                applyImportButton.gameObject.SetActive(hasPending);
                applyImportButton.interactable = hasPending
                    && string.Equals(
                        importConfirmationInput?.text?.Trim(),
                        SaveTransferImportSession.ConfirmationPhrase,
                        StringComparison.Ordinal);
            }
        }

        private void ClearImportConfirmation()
        {
            if (importConfirmationInput != null)
            {
                importConfirmationInput.SetTextWithoutNotify(string.Empty);
            }
        }

        private static void RebindImportedState(GameManager manager)
        {
            if (manager?.CurrentSave == null)
            {
                return;
            }

            UnityEngine.Object.FindFirstObjectByType<GameSettingsPanelController>(
                FindObjectsInactive.Include)?.RefreshFromSave(true);
            UnityEngine.Object.FindFirstObjectByType<AccessibilitySettingsPanelController>(
                FindObjectsInactive.Include)?.RefreshFromSave("가져온 접근성 설정을 적용했습니다.");

            var themeId = manager.CurrentSave.milkroomThemeId;
            UnityEngine.Object.FindFirstObjectByType<MilkroomThemeController>(
                FindObjectsInactive.Include)?.ApplyTheme(themeId);
            UnityEngine.Object.FindFirstObjectByType<MilkroomLightingController>(
                FindObjectsInactive.Include)?.ApplyTheme(themeId);
            UnityEngine.Object.FindFirstObjectByType<MilkroomAmbientEventController>(
                FindObjectsInactive.Include)?.SetTheme(themeId);
        }

        private void EnsureTransferFileBridge()
        {
            if (transferFileBridge != null
                || (exportButton == null && selectImportButton == null && applyImportButton == null))
            {
                return;
            }

            transferFileBridge = GetComponent<SaveTransferFileBridge>();
            if (transferFileBridge == null)
            {
                transferFileBridge = gameObject.AddComponent<SaveTransferFileBridge>();
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }

            Debug.Log(message ?? string.Empty);
        }

        private void RefreshBoundViews(GameManager manager, string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            if (milkroomUi != null)
            {
                milkroomUi.Bind(manager.CurrentSave);
                milkroomUi.ShowMessage(message);
            }

            if (visualController != null)
            {
                visualController.Bind(manager.CurrentTama);
                visualController.React(false);
            }

            Debug.Log(message);
        }
    }
}
