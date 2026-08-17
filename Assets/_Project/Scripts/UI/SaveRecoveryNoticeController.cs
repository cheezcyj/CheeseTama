using System;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class SaveRecoveryNoticeController : MonoBehaviour
    {
        public const string OverlayObjectName = "Save Recovery Notice Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;

        private Action confirmed;
        private GameObject previouslySelected;
        private bool confirmBound;
        private bool confirmationInFlight;

        public bool IsConfigured => overlayRoot != null
            && titleText != null
            && messageText != null
            && confirmButton != null;

        public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;

        public bool IsBlockingGameplay => IsVisible;

        public SaveRecoveryOutcome CurrentOutcome { get; private set; } = SaveRecoveryOutcome.None;

        public void Configure(
            GameObject root,
            Text titleLabel,
            Text messageLabel,
            Button confirmAction)
        {
            UnbindConfirmButton();
            overlayRoot = root;
            titleText = titleLabel;
            messageText = messageLabel;
            confirmButton = confirmAction;
            PrepareFullScreenBlocker();
            ClearPresentation();
            SetOverlayActive(false);
            BindConfirmButton();
        }

        public bool Show(SaveRecoveryReport report, Action onConfirmed)
        {
            if (!IsConfigured
                || report == null
                || !report.UserNotificationRecommended)
            {
                return false;
            }

            BindConfirmButton();
            CurrentOutcome = report.Outcome;
            confirmed = onConfirmed;
            confirmationInFlight = false;
            SetText(titleText, ResolveTitle(report.Outcome));
            SetText(messageText, ResolveMessage(report));

            previouslySelected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            SelectConfirmButton();
            return true;
        }

        public void Hide()
        {
            var selectionToRestore = previouslySelected;
            ClearPresentation();
            SetOverlayActive(false);

            if (selectionToRestore != null
                && selectionToRestore.activeInHierarchy
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectionToRestore);
            }
        }

        private void OnEnable()
        {
            BindConfirmButton();
        }

        private void OnDisable()
        {
            UnbindConfirmButton();
            Hide();
        }

        private void OnDestroy()
        {
            UnbindConfirmButton();
        }

        private void Update()
        {
            if (!IsVisible || confirmationInFlight)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                HandleConfirmed();
            }
        }

        private void HandleConfirmed()
        {
            if (!IsVisible || confirmationInFlight)
            {
                return;
            }

            confirmationInFlight = true;
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }

            var callback = confirmed;
            Hide();
            callback?.Invoke();
        }

        private void BindConfirmButton()
        {
            if (confirmBound || confirmButton == null)
            {
                return;
            }

            confirmButton.onClick.AddListener(HandleConfirmed);
            confirmBound = true;
        }

        private void UnbindConfirmButton()
        {
            if (!confirmBound)
            {
                return;
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmed);
            }

            confirmBound = false;
        }

        private void PrepareFullScreenBlocker()
        {
            if (overlayRoot == null)
            {
                return;
            }

            if (overlayRoot.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            var canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = overlayRoot.AddComponent<CanvasGroup>();
            }
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.ignoreParentGroups = false;

            var blockerGraphic = overlayRoot.GetComponent<Graphic>();
            if (blockerGraphic == null)
            {
                var blockerImage = overlayRoot.AddComponent<Image>();
                blockerImage.color = new Color(0f, 0f, 0f, 0.55f);
                blockerGraphic = blockerImage;
            }

            blockerGraphic.raycastTarget = true;
        }

        private void ClearPresentation()
        {
            confirmed = null;
            previouslySelected = null;
            confirmationInFlight = false;
            CurrentOutcome = SaveRecoveryOutcome.None;
            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
        }

        private void SelectConfirmButton()
        {
            if (confirmButton != null
                && confirmButton.interactable
                && confirmButton.gameObject.activeInHierarchy
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
            }
        }

        private static string ResolveTitle(SaveRecoveryOutcome outcome)
        {
            return outcome == SaveRecoveryOutcome.CreatedFreshSaveAfterCorruption
                ? "저장 상태 안내"
                : "저장 복구 완료";
        }

        private static string ResolveMessage(SaveRecoveryReport report)
        {
            var message = report.Outcome switch
            {
                SaveRecoveryOutcome.RecoveredFromTemporaryFile =>
                    "중단된 저장 작업에서 최신 진행 상황을 복구했습니다. 계속 플레이해도 안전합니다.",
                SaveRecoveryOutcome.RecoveredFromBackup =>
                    "저장 파일을 읽을 수 없어 직전 백업에서 진행 상황을 복구했습니다.",
                SaveRecoveryOutcome.CreatedFreshSaveAfterCorruption =>
                    "저장 파일과 복구본을 읽을 수 없어 손상된 파일을 보관하고 새 저장으로 시작합니다.",
                _ => string.Empty
            };

            if (report.QuarantinedFileCount > 0)
            {
                message += $"\n손상된 파일 {report.QuarantinedFileCount}개는 별도로 보관했습니다.";
            }

            return message;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
