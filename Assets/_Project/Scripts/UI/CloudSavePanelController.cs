using System;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Input;
using CheeseTama.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Explicit cloud-save surface. Remote downloads and conflicts never overwrite the
    /// local save until the player types the confirmation phrase and applies the result.
    /// </summary>
    public sealed class CloudSavePanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Cloud Save Overlay";
        public const string RemoteConfirmationPhrase = GameManager.CloudSaveApplyConfirmationPhrase;

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text providerText;
        [SerializeField] private Text statusText;
        [SerializeField] private InputField confirmationInput;
        [SerializeField] private Button synchronizeButton;
        [SerializeField] private Button applyRemoteButton;
        [SerializeField] private Button closeButton;

        private GameManager manager;
        private ICloudSaveProvider provider;
        private Action<bool> blockingChanged;
        private CloudSyncResult pendingResult;
        private bool hasPendingResult;
        private bool blockingNotified;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public CloudSyncResult PendingResult => pendingResult;
        public bool HasPendingRemote => hasPendingResult
            && pendingResult.Remote != null
            && (pendingResult.RequiresLocalWrite || pendingResult.RequiresUserChoice);

        public void Configure(
            GameObject root,
            Text providerLabel,
            Text statusLabel,
            InputField confirmation,
            Button syncButton,
            Button useCloudButton,
            Button closePanelButton,
            GameManager gameManager,
            ICloudSaveProvider cloudProvider = null,
            Action<bool> onBlockingChanged = null)
        {
            UnbindControls();
            NotifyBlocking(false);
            overlayRoot = root;
            providerText = providerLabel;
            statusText = statusLabel;
            confirmationInput = confirmation;
            synchronizeButton = syncButton;
            applyRemoteButton = useCloudButton;
            closeButton = closePanelButton;
            manager = gameManager;
            provider = cloudProvider ?? SteamCloudProviderFactory.CreateDefault();
            blockingChanged = onBlockingChanged;
            hasPendingResult = false;
            BindControls();
            RenderInitialState();
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        public void Open()
        {
            hasPendingResult = false;
            if (confirmationInput != null)
            {
                confirmationInput.text = string.Empty;
            }

            RenderInitialState();
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
                overlayRoot.transform.SetAsLastSibling();
            }

            NotifyBlocking(true);
            EventSystem.current?.SetSelectedGameObject(synchronizeButton?.gameObject);
        }

        public void Close()
        {
            hasPendingResult = false;
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            NotifyBlocking(false);
        }

        public void Synchronize()
        {
            if (manager == null)
            {
                SetStatus("게임 저장을 불러오지 못했습니다. 로컬 저장은 변경되지 않았습니다.");
                return;
            }

            pendingResult = manager.SynchronizeCloudSave(provider);
            hasPendingResult = true;
            if (confirmationInput != null)
            {
                confirmationInput.text = string.Empty;
            }

            RenderResult(pendingResult);
        }

        public void ApplyRemote()
        {
            if (!HasPendingRemote)
            {
                SetStatus("적용할 클라우드 저장이 없습니다.");
                RefreshApplyState();
                return;
            }

            var result = manager?.TryApplyCloudSave(pendingResult, confirmationInput?.text);
            if (result == null || !result.Succeeded)
            {
                SetStatus(result?.Message
                    ?? "클라우드 저장을 적용하지 못했습니다. 로컬 저장은 유지됩니다.");
                RefreshApplyState();
                return;
            }

            hasPendingResult = false;
            if (confirmationInput != null)
            {
                confirmationInput.text = string.Empty;
            }

            RebindLoadedState();
            SetStatus(result.Message);
            RefreshApplyState();
        }

        private void OnEnable()
        {
            BindControls();
            RefreshApplyState();
        }

        private void OnDisable()
        {
            UnbindControls();
            NotifyBlocking(false);
        }

        private void Update()
        {
            if (IsOpen && GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        private void BindControls()
        {
            UnbindControls();
            synchronizeButton?.onClick.AddListener(Synchronize);
            applyRemoteButton?.onClick.AddListener(ApplyRemote);
            closeButton?.onClick.AddListener(Close);
            confirmationInput?.onValueChanged.AddListener(HandleConfirmationChanged);
        }

        private void UnbindControls()
        {
            synchronizeButton?.onClick.RemoveListener(Synchronize);
            applyRemoteButton?.onClick.RemoveListener(ApplyRemote);
            closeButton?.onClick.RemoveListener(Close);
            confirmationInput?.onValueChanged.RemoveListener(HandleConfirmationChanged);
        }

        private void HandleConfirmationChanged(string _)
        {
            RefreshApplyState();
        }

        private void RenderInitialState()
        {
            var providerName = provider?.ProviderName ?? "LocalOnly";
            if (providerText != null)
            {
                AccessibilityRuntime.SetTextAndApply(
                    providerText,
                    $"연결 방식 · {providerName}");
            }

            SetStatus(IsProviderAvailable()
                ? "동기화를 누르면 로컬 저장과 Steam Cloud를 비교합니다."
                : "Steam Cloud가 연결되지 않았습니다. 로컬 저장을 안전하게 유지합니다.");
            RefreshApplyState();
        }

        private void RenderResult(CloudSyncResult result)
        {
            var message = result.Action switch
            {
                CloudSyncAction.InSync => "로컬 저장과 클라우드 저장이 같습니다.",
                CloudSyncAction.UploadedLocal => "최신 로컬 저장을 클라우드에 올렸습니다.",
                CloudSyncAction.DownloadedRemote =>
                    $"클라우드 저장이 더 최신입니다. 적용하려면 {RemoteConfirmationPhrase}를 입력하세요.",
                CloudSyncAction.ConflictNeedsResolution =>
                    $"동일 시점의 서로 다른 저장을 발견했습니다. 클라우드 사본을 쓰려면 {RemoteConfirmationPhrase}를 입력하세요.",
                CloudSyncAction.KeptLocalOffline =>
                    "Steam Cloud가 오프라인입니다. 로컬 저장을 그대로 유지합니다.",
                CloudSyncAction.KeptLocalAfterFailure =>
                    "Steam Cloud 동기화에 실패했습니다. 로컬 저장을 그대로 유지합니다.",
                _ => "로컬 저장을 확인하지 못해 클라우드 데이터를 변경하지 않았습니다."
            };
            if (!string.IsNullOrWhiteSpace(result.Message)
                && (result.Action == CloudSyncAction.KeptLocalOffline
                    || result.Action == CloudSyncAction.KeptLocalAfterFailure
                    || result.Action == CloudSyncAction.InvalidLocal))
            {
                message += "\n" + result.Message;
            }

            SetStatus(message);
            RefreshApplyState();
        }

        private void RefreshApplyState()
        {
            if (confirmationInput != null)
            {
                confirmationInput.gameObject.SetActive(HasPendingRemote);
            }

            if (applyRemoteButton != null)
            {
                applyRemoteButton.gameObject.SetActive(HasPendingRemote);
                applyRemoteButton.interactable = HasPendingRemote
                    && string.Equals(
                        confirmationInput?.text?.Trim(),
                        RemoteConfirmationPhrase,
                        System.StringComparison.Ordinal);
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                AccessibilityRuntime.SetTextAndApply(statusText, value ?? string.Empty);
            }
        }

        private bool IsProviderAvailable()
        {
            try
            {
                return provider?.Availability == CloudProviderAvailability.Available;
            }
            catch
            {
                return false;
            }
        }

        private void RebindLoadedState()
        {
            if (manager?.CurrentSave == null)
            {
                return;
            }

            UnityEngine.Object.FindFirstObjectByType<MilkroomUIController>(
                FindObjectsInactive.Include)?.Bind(manager.CurrentSave);
            UnityEngine.Object.FindFirstObjectByType<CheeseTamaVisualController>(
                FindObjectsInactive.Include)?.Bind(manager.CurrentTama);
            UnityEngine.Object.FindFirstObjectByType<GameSettingsPanelController>(
                FindObjectsInactive.Include)?.RefreshFromSave(true);
            UnityEngine.Object.FindFirstObjectByType<AccessibilitySettingsPanelController>(
                FindObjectsInactive.Include)?.RefreshFromSave("클라우드 접근성 설정을 적용했습니다.");

            var themeId = manager.CurrentSave.milkroomThemeId;
            UnityEngine.Object.FindFirstObjectByType<MilkroomThemeController>(
                FindObjectsInactive.Include)?.ApplyTheme(themeId);
            UnityEngine.Object.FindFirstObjectByType<MilkroomLightingController>(
                FindObjectsInactive.Include)?.ApplyTheme(themeId);
            UnityEngine.Object.FindFirstObjectByType<MilkroomAmbientEventController>(
                FindObjectsInactive.Include)?.SetTheme(themeId);
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
