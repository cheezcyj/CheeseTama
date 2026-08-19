using System.Text;
using CheeseTama.Audio;
using CheeseTama.Core;
using CheeseTama.Gameplay.Stats;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class ReturnSummaryController : MonoBehaviour
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text elapsedText;
        [SerializeField] private Text changesText;
        [SerializeField] private Text rewardsText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private GameManager boundManager;
        private bool configured;
        private string displayedSummaryId = string.Empty;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomActionBarWasEnabled;
        private bool devPanelWasEnabled;

        public bool IsBlockingGameplay => Application.isPlaying
            && overlayRoot != null
            && overlayRoot.activeSelf;

        public void Configure(
            GameObject root,
            Text elapsedLabel,
            Text changesLabel,
            Text rewardsLabel,
            Button closeButton,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController)
        {
            RestoreControls();
            UnbindButton();

            overlayRoot = root;
            elapsedText = elapsedLabel;
            changesText = changesLabel;
            rewardsText = rewardsLabel;
            confirmButton = closeButton;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            configured = true;

            BindButton();
            SetOverlayActive(false);
            if (Application.isPlaying)
            {
                BindManager(GameManager.Instance);
                TryShowPendingSummary();
            }
        }

        private void OnEnable()
        {
            if (!configured || overlayRoot == null || confirmButton == null)
            {
                return;
            }

            BindButton();
            if (Application.isPlaying)
            {
                BindManager(GameManager.Instance);
                TryShowPendingSummary();
            }
        }

        private void OnDisable()
        {
            UnbindButton();
            BindManager(null);
            RestoreControls();
        }

        private void Update()
        {
            if (!configured || overlayRoot == null || confirmButton == null || !Application.isPlaying)
            {
                return;
            }

            if (IsBlockingGameplay && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Confirm();
                return;
            }

            if (!IsBlockingGameplay)
            {
                TryShowPendingSummary();
            }
        }

        public void Confirm()
        {
            if (boundManager != null && !string.IsNullOrWhiteSpace(displayedSummaryId))
            {
                boundManager.ConsumePendingReturnSummary(displayedSummaryId);
            }

            displayedSummaryId = string.Empty;
            SetOverlayActive(false);
            RestoreControls();
        }

        private void BindManager(GameManager manager)
        {
            if (boundManager == manager)
            {
                return;
            }

            if (boundManager != null)
            {
                boundManager.ReturnSummaryAvailable -= HandleSummaryAvailable;
                boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
            }

            boundManager = manager;
            if (boundManager != null)
            {
                boundManager.ReturnSummaryAvailable += HandleSummaryAvailable;
                boundManager.SaveDataReplaced += HandleSaveDataReplaced;
            }
        }

        private void HandleSummaryAvailable(ReturnSummaryData summary)
        {
            TryShowPendingSummary();
        }

        private void HandleSaveDataReplaced()
        {
            if (IsBlockingGameplay
                && (boundManager == null
                    || !boundManager.TryGetPendingReturnSummary(out var pending)
                    || pending.id != displayedSummaryId))
            {
                displayedSummaryId = string.Empty;
                SetOverlayActive(false);
                RestoreControls();
            }

            TryShowPendingSummary();
        }

        private void TryShowPendingSummary()
        {
            if (!configured
                || overlayRoot == null
                || confirmButton == null
                || !Application.isPlaying
                || IsBlockingGameplay)
            {
                return;
            }

            boundManager ??= GameManager.Instance;
            if (boundManager == null
                || !boundManager.TryGetPendingReturnSummary(out var summary)
                || summary == null
                || IsAnotherModalBlocking())
            {
                return;
            }

            displayedSummaryId = summary.id;
            Populate(summary);
            SuspendControls();
            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            if (EventSystem.current != null && confirmButton != null)
            {
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
            }

            CheeseTamaAudioController.Instance?.PlayReturnSummary();
        }

        private bool IsAnotherModalBlocking()
        {
            var onboarding = GetComponent<FirstMeetingOnboardingController>();
            if (onboarding != null && onboarding.IsBlockingGameplay)
            {
                return true;
            }

            var saveData = boundManager?.CurrentSave;
            if (saveData?.onboarding != null
                && (!saveData.onboarding.completed || saveData.onboarding.replaying))
            {
                return true;
            }

            return IsActive("CheeseTama Name Dialog")
                || IsActive("Settings Modal")
                || IsActive("Confirm Reset Dialog")
                || IsActive("Decorate Overlay")
                || IsActive("Milk Panel")
                || IsActive("Cooking Panel")
                || IsActive("Snack Panel")
                || IsActive("Growth Achievement Overlay")
                || IsActive("Evolution Achievement Overlay")
                || IsActive("Milk Drop Catch Overlay")
                || IsActive("Cleaning Mini Game Overlay")
                || IsActive("Decoration Shop Overlay")
                || IsActive("Care Event Overlay")
                || IsActive("New Game Setup Overlay")
                || IsActive("Growth Journey Overlay")
                || IsActive("Play Choice Overlay")
                || IsActive("Bouncy Jump Overlay")
                || IsActive(FirstDayJourneyController.OverlayObjectName)
                || IsActive("Cheese Star Delivery Overlay")
                || IsActive("Memory Journal Overlay")
                || IsActive("Fantasy Powder Overlay")
                || IsActive(SaveRecoveryNoticeController.OverlayObjectName)
                || IsActive(CheeseTamaProfileMenuController.OverlayObjectName)
                || IsActive(InputBindingsPanelController.OverlayObjectName)
                || IsActive("Milk Blending Overlay")
                || IsActive(CookingChoicePanelController.OverlayObjectName)
                || IsActive(NpcVisitCardController.OverlayObjectName)
                || IsActive(JourneyHubPanelController.OverlayObjectName)
                || IsActive(SleepSchedulePanelController.OverlayObjectName)
                || IsActive("Star Legacy Overlay")
                || IsActive("Bond Status Overlay")
                || IsActive("Hidden Career Card Overlay")
                || IsActive("Dev Panel");
        }

        private bool IsActive(string childName)
        {
            var child = transform.Find(childName);
            return child != null && child.gameObject.activeInHierarchy;
        }

        private void Populate(ReturnSummaryData summary)
        {
            var hours = summary.elapsedMinutes / 60;
            var minutes = summary.elapsedMinutes % 60;
            SetText(
                elapsedText,
                minutes > 0
                    ? $"{hours}시간 {minutes}분 만에 돌아왔어요."
                    : $"{Mathf.Max(summary.appliedHours, hours)}시간 만에 돌아왔어요.");

            var changes = new StringBuilder();
            AppendChange(changes, "포만감", summary.before.hunger, summary.after.hunger);
            AppendChange(changes, "기분", summary.before.mood, summary.after.mood);
            AppendChange(changes, "청결", summary.before.cleanliness, summary.after.cleanliness);
            AppendChange(changes, "졸림", summary.before.sleepiness, summary.after.sleepiness);
            AppendChange(changes, "건강", summary.before.health, summary.after.health);
            AppendChange(changes, "과포만", summary.before.overfullness, summary.after.overfullness);
            SetText(
                changesText,
                changes.Length > 0
                    ? changes.ToString()
                    : "쉬는 동안 상태 변화는 없었어요.");

            if (rewardsText != null)
            {
                rewardsText.gameObject.SetActive(summary.HasRewards);
                SetText(
                    rewardsText,
                    summary.HasRewards
                        ? FormatRewards(summary)
                        : string.Empty);
            }
        }

        private static void AppendChange(StringBuilder builder, string label, int before, int after)
        {
            if (before == after)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            var delta = after - before;
            builder.Append(label)
                .Append("  ")
                .Append(before)
                .Append(" → ")
                .Append(after)
                .Append("  (")
                .Append(delta > 0 ? "+" : string.Empty)
                .Append(delta)
                .Append(')');
        }

        private static string FormatRewards(ReturnSummaryData summary)
        {
            var builder = new StringBuilder("받은 보상  ");
            if (summary.milkCoinsDelta != 0)
            {
                builder.Append("코인 ").Append(FormatSigned(summary.milkCoinsDelta)).Append("  ");
            }

            if (summary.milkDropsDelta != 0)
            {
                builder.Append("우유방울 ").Append(FormatSigned(summary.milkDropsDelta)).Append("  ");
            }

            if (summary.collectionFragmentsDelta != 0)
            {
                builder.Append("도감조각 ").Append(FormatSigned(summary.collectionFragmentsDelta));
            }

            return builder.ToString().TrimEnd();
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomActionBarWasEnabled = bottomActionBarController != null && bottomActionBarController.enabled;
            devPanelWasEnabled = devPanelController != null && devPanelController.enabled;
            if (topMenuController != null)
            {
                topMenuController.enabled = false;
            }

            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = false;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = false;
            }

            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null)
            {
                topMenuController.enabled = topMenuWasEnabled;
            }

            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = bottomActionBarWasEnabled;
            }

            if (devPanelController != null)
            {
                devPanelController.enabled = devPanelWasEnabled;
            }

            controlsSuspended = false;
        }

        private void BindButton()
        {
            if (confirmButton == null)
            {
                return;
            }

            confirmButton.onClick.RemoveListener(Confirm);
            confirmButton.onClick.AddListener(Confirm);
        }

        private void UnbindButton()
        {
            confirmButton?.onClick.RemoveListener(Confirm);
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
