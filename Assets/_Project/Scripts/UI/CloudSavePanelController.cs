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
        public const string RemoteConfirmationPhrase = "온라인 저장 사용";

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
                SetStatus("게임 기록을 확인하지 못했어요. 이 기기의 저장은 그대로예요.");
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
                SetStatus("불러올 온라인 저장이 없어요. 먼저 저장을 비교해 주세요.");
                RefreshApplyState();
                return;
            }

            if (!MatchesRemoteConfirmation())
            {
                SetStatus($"확인 칸에 ‘{RemoteConfirmationPhrase}’이라고 입력해 주세요.");
                RefreshApplyState();
                return;
            }

            var result = manager?.TryApplyCloudSave(
                pendingResult,
                GameManager.CloudSaveApplyConfirmationPhrase);
            if (result == null || !result.Succeeded)
            {
                SetStatus(ResolveApplyFailureMessage(result?.Message));
                RefreshApplyState();
                return;
            }

            hasPendingResult = false;
            if (confirmationInput != null)
            {
                confirmationInput.text = string.Empty;
            }

            RebindLoadedState();
            SetStatus("온라인 저장을 불러왔어요.");
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
            if (providerText != null)
            {
                AccessibilityRuntime.SetTextAndApply(
                    providerText,
                    IsProviderAvailable()
                        ? "저장 위치 · 이 기기와 온라인"
                        : "저장 위치 · 이 기기만");
            }

            SetStatus(IsProviderAvailable()
                ? "이 기기의 저장과 온라인 저장을 비교할 수 있어요."
                : "온라인 저장에 연결되지 않았어요. 이 기기에 안전하게 저장하고 있어요.");
            RefreshApplyState();
        }

        private void RenderResult(CloudSyncResult result)
        {
            var message = result.Action switch
            {
                CloudSyncAction.InSync => "이 기기와 온라인의 저장이 같아요.",
                CloudSyncAction.UploadedLocal => "이 기기의 최신 저장을 온라인에도 저장했어요.",
                CloudSyncAction.DownloadedRemote =>
                    $"온라인 저장이 더 새로워요. 쓰려면 확인 칸에 ‘{RemoteConfirmationPhrase}’이라고 입력해 주세요.",
                CloudSyncAction.ConflictNeedsResolution =>
                    $"이 기기와 온라인의 게임 기록이 달라요. 온라인 기록을 쓰려면 ‘{RemoteConfirmationPhrase}’이라고 입력해 주세요.",
                CloudSyncAction.KeptLocalOffline =>
                    "온라인 저장에 연결할 수 없어요. 이 기기의 저장은 그대로예요.",
                CloudSyncAction.KeptLocalAfterFailure =>
                    "온라인 저장을 확인하지 못했어요. 이 기기의 저장은 그대로예요.",
                _ => "이 기기의 저장을 확인하지 못해 온라인 저장을 바꾸지 않았어요."
            };

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
                    && MatchesRemoteConfirmation();
            }
        }

        private bool MatchesRemoteConfirmation()
        {
            return string.Equals(
                confirmationInput?.text?.Trim(),
                RemoteConfirmationPhrase,
                System.StringComparison.Ordinal);
        }

        private static string ResolveApplyFailureMessage(string message)
        {
            return !string.IsNullOrWhiteSpace(message)
                && message.IndexOf("변경", System.StringComparison.Ordinal) >= 0
                    ? "저장을 비교한 뒤 게임 기록이 바뀌었어요. 다시 비교해 주세요."
                    : "온라인 저장을 불러오지 못했어요. 이 기기의 저장은 그대로예요.";
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
                FindObjectsInactive.Include)?.RefreshFromSave("온라인 저장의 보기 편한 설정을 적용했어요.");

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
