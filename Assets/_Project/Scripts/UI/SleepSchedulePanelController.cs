using System;
using CheeseTama.Gameplay.Sleep;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Callback-only uGUI surface for the sleep schedule feature. It does not
    /// discover or own GameManager/save objects, so scene integration remains
    /// explicit and testable.
    /// </summary>
    public sealed class SleepSchedulePanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Sleep Schedule Overlay";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text scheduleDetailText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text[] durationButtonTexts;
        [SerializeField] private Text wakeButtonText;
        [SerializeField] private Button[] durationButtons;
        [SerializeField] private Button startButton;
        [SerializeField] private Button wakeButton;
        [SerializeField] private Button closeButton;

        private Func<SleepScheduleSnapshot> snapshotProvider;
        private Func<int, SleepScheduleStartResult> startCommand;
        private Func<SleepScheduleWakeResult> wakeCommand;
        private Action onClosed;
        private Action<bool> modalBlockingChanged;
        private UnityAction[] durationListeners;
        private CanvasGroup panelCanvasGroup;
        private int selectedDurationHours = SleepScheduleSaveData.MinimumScheduledHours;
        private bool hasReportedBlockingState;
        private bool lastReportedBlockingState;

        public int SelectedDurationHours => selectedDurationHours;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>
        /// Integration contract for gameplay input routers and other modal
        /// owners. True means this full-screen surface owns pointer/keyboard input.
        /// </summary>
        public bool BlocksGameplayInput => IsOpen;

        public void Configure(
            GameObject root,
            Text title,
            Text summary,
            Text scheduleDetail,
            Text status,
            Text[] durationLabels,
            Text wakeLabel,
            Button[] hourButtons,
            Button beginButton,
            Button earlyWakeButton,
            Button dismissButton,
            Func<SleepScheduleSnapshot> getSnapshot,
            Func<int, SleepScheduleStartResult> beginSleep,
            Func<SleepScheduleWakeResult> wake,
            Action closed,
            Action<bool> blockingChanged = null)
        {
            UnbindControls();

            panelRoot = root;
            titleText = title;
            summaryText = summary;
            scheduleDetailText = scheduleDetail;
            statusText = status;
            durationButtonTexts = durationLabels;
            wakeButtonText = wakeLabel;
            durationButtons = hourButtons;
            startButton = beginButton;
            wakeButton = earlyWakeButton;
            closeButton = dismissButton;
            snapshotProvider = getSnapshot;
            startCommand = beginSleep;
            wakeCommand = wake;
            onClosed = closed;
            modalBlockingChanged = blockingChanged;
            selectedDurationHours = SleepScheduleSaveData.MinimumScheduledHours;

            panelCanvasGroup = panelRoot == null
                ? null
                : panelRoot.GetComponent<CanvasGroup>();
            if (panelRoot != null && panelCanvasGroup == null)
            {
                panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            BindControls();
            ApplyModalBlocking(IsOpen, notify: false);
            Refresh();
        }

        public bool Open()
        {
            if (panelRoot == null || snapshotProvider == null)
            {
                return false;
            }

            panelRoot.SetActive(true);
            ApplyModalBlocking(true, notify: true);
            Refresh();
            return true;
        }

        public void Close()
        {
            if (panelRoot == null)
            {
                return;
            }

            panelRoot.SetActive(false);
            ApplyModalBlocking(false, notify: true);
            onClosed?.Invoke();
        }

        public void Refresh()
        {
            if (titleText != null)
            {
                titleText.text = "수면 예약";
            }

            var snapshot = snapshotProvider?.Invoke() ?? default;
            if (snapshot.IsSleeping)
            {
                selectedDurationHours = Math.Max(
                    SleepScheduleSaveData.MinimumScheduledHours,
                    Math.Min(
                        SleepScheduleSaveData.MaximumScheduledHours,
                        snapshot.ScheduledHours));
            }

            RefreshDurationButtons(snapshot);
            RefreshScheduleText(snapshot);

            if (startButton != null)
            {
                startButton.interactable = snapshot.CanStart && startCommand != null;
            }

            if (wakeButton != null)
            {
                wakeButton.interactable = snapshot.CanWake && wakeCommand != null;
            }

            if (wakeButtonText != null)
            {
                wakeButtonText.text = snapshot.IsDue ? "기상 완료" : "지금 깨우기";
            }
        }

        private void BindControls()
        {
            if (durationButtons != null)
            {
                durationListeners = new UnityAction[durationButtons.Length];
                for (var index = 0; index < durationButtons.Length; index += 1)
                {
                    var capturedHours = index + 1;
                    UnityAction listener = () => SelectDuration(capturedHours);
                    durationListeners[index] = listener;
                    durationButtons[index]?.onClick.AddListener(listener);
                }
            }

            startButton?.onClick.AddListener(StartSelectedSchedule);
            wakeButton?.onClick.AddListener(WakeNow);
            closeButton?.onClick.AddListener(Close);
        }

        private void UnbindControls()
        {
            if (durationButtons != null && durationListeners != null)
            {
                var count = Math.Min(durationButtons.Length, durationListeners.Length);
                for (var index = 0; index < count; index += 1)
                {
                    if (durationButtons[index] != null
                        && durationListeners[index] != null)
                    {
                        durationButtons[index].onClick.RemoveListener(
                            durationListeners[index]);
                    }
                }
            }

            startButton?.onClick.RemoveListener(StartSelectedSchedule);
            wakeButton?.onClick.RemoveListener(WakeNow);
            closeButton?.onClick.RemoveListener(Close);
            durationListeners = null;
        }

        private void SelectDuration(int hours)
        {
            selectedDurationHours = Math.Max(
                SleepScheduleSaveData.MinimumScheduledHours,
                Math.Min(SleepScheduleSaveData.MaximumScheduledHours, hours));
            Refresh();
        }

        private void StartSelectedSchedule()
        {
            if (startCommand == null)
            {
                SetStatus("수면 예약 기능을 연결하지 못했어요.");
                return;
            }

            var result = startCommand.Invoke(selectedDurationHours);
            SetStatus(result.Message);
            Refresh();
        }

        private void WakeNow()
        {
            if (wakeCommand == null)
            {
                SetStatus("기상 기능을 연결하지 못했어요.");
                return;
            }

            var result = wakeCommand.Invoke();
            SetStatus(result.Message);
            Refresh();
        }

        private void RefreshDurationButtons(SleepScheduleSnapshot snapshot)
        {
            if (durationButtons == null)
            {
                return;
            }

            for (var index = 0; index < durationButtons.Length; index += 1)
            {
                var hours = index + 1;
                var inRange = hours <= SleepScheduleSaveData.MaximumScheduledHours;
                var button = durationButtons[index];
                if (button != null)
                {
                    button.interactable = inRange && snapshot.CanStart;
                }

                if (durationButtonTexts != null
                    && index < durationButtonTexts.Length
                    && durationButtonTexts[index] != null)
                {
                    durationButtonTexts[index].text = hours == selectedDurationHours
                        ? $"✓ {hours}시간"
                        : $"{hours}시간";
                }
            }
        }

        private void RefreshScheduleText(SleepScheduleSnapshot snapshot)
        {
            if (!snapshot.IsHatched)
            {
                if (summaryText != null)
                {
                    summaryText.text = "부화한 뒤 수면 예약을 이용할 수 있어요.";
                }

                if (scheduleDetailText != null)
                {
                    scheduleDetailText.text = "알 상태에서는 수면 회복이 적용되지 않아요.";
                }

                return;
            }

            if (!snapshot.IsSleeping)
            {
                if (summaryText != null)
                {
                    summaryText.text = "1~8시간 중 잘 시간을 정해 주세요.";
                }

                if (scheduleDetailText != null)
                {
                    scheduleDetailText.text =
                        "앱을 닫아도 예약은 저장되며, 실제로 쉰 시간만큼 회복해요.";
                }

                return;
            }

            if (summaryText != null)
            {
                summaryText.text = snapshot.IsDue
                    ? "예약 시간이 끝났어요. 기상하면 회복 결과를 받아요."
                    : $"{snapshot.ScheduledHours}시간 수면 중 · {snapshot.RemainingMinutes}분 뒤 기상";
            }

            if (scheduleDetailText != null)
            {
                scheduleDetailText.text =
                    $"취침 {FormatLocalTime(snapshot.SleepStartedAt)}\n"
                    + $"예정 기상 {FormatLocalTime(snapshot.PlannedWakeAt)}\n"
                    + $"지금까지 {snapshot.ElapsedMinutes}분 휴식";
            }
        }

        private void ApplyModalBlocking(bool blocked, bool notify)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = blocked;
                panelCanvasGroup.blocksRaycasts = blocked;
            }

            if (!notify)
            {
                return;
            }

            if (hasReportedBlockingState
                && lastReportedBlockingState == blocked)
            {
                return;
            }

            hasReportedBlockingState = true;
            lastReportedBlockingState = blocked;
            modalBlockingChanged?.Invoke(blocked);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }

        private static string FormatLocalTime(DateTimeOffset value)
        {
            return value.ToLocalTime().ToString("MM월 dd일 HH:mm");
        }

        private void OnDestroy()
        {
            UnbindControls();
            if (IsOpen)
            {
                ApplyModalBlocking(false, notify: true);
            }
        }
    }
}
