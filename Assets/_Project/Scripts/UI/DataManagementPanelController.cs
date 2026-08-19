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
                SetStatus("내보낼 로컬 저장을 찾지 못했습니다.");
                return;
            }

            EnsureTransferFileBridge();
            if (transferFileBridge == null)
            {
                SetStatus("이 환경에서는 저장 백업을 만들 수 없습니다.");
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

            SetStatus("현재 진행도의 백업을 만들었습니다. " + exportMessage);
        }

        public void SelectImportFile()
        {
            importSession.Clear();
            ClearImportConfirmation();
            RefreshImportState();
            EnsureTransferFileBridge();
            if (transferFileBridge == null)
            {
                SetStatus("이 환경에서는 백업 파일을 선택할 수 없습니다.");
                return;
            }

            SetStatus("가져올 CheeseTama 백업 파일을 선택해 주세요.");
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
                SetStatus("로컬 저장 장치를 찾지 못해 가져오기를 중단했습니다.");
                return;
            }

            EnsureTransferFileBridge();
            if (transferFileBridge == null)
            {
                SetStatus("현재 저장의 안전 백업을 만들 수 없어 가져오기를 중단했습니다.");
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
                SetStatus("현재 저장의 안전 백업을 먼저 만들지 못했습니다. " + backupMessage);
                return;
            }

            if (!saveManager.TryReplaceFromCloudPayload(authorization.Payload, out _))
            {
                SetStatus("백업을 만들었지만 가져온 저장을 안전하게 기록하지 못했습니다. 기존 저장은 유지됩니다.");
                return;
            }

            importSession.Clear();
            ClearImportConfirmation();
            manager.ReloadGame();
            RebindImportedState(manager);
            RefreshImportState();
            RefreshBoundViews(
                manager,
                "이전 진행도를 백업한 뒤 선택한 저장을 가져왔습니다.");
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
                + $"\n가져오려면 {SaveTransferImportSession.ConfirmationPhrase}를 입력하세요.");
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
