using System;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class SaveRecoveryNoticeBridge : MonoBehaviour
    {
        private const float PollIntervalSeconds = 0.25f;

        [SerializeField] private SaveRecoveryNoticeController noticeController;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;

        private Func<SaveRecoveryReport> recoveryReportProvider;
        private SaveRecoveryReport displayedReport;
        private SaveRecoveryReport acknowledgedReport;
        private float nextPollAt;
        private bool configured;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomActionBarWasEnabled;
        private bool devPanelWasEnabled;

        public bool IsBlockingGameplay => noticeController != null
            && noticeController.IsBlockingGameplay;

        public SaveRecoveryReport AcknowledgedReport => acknowledgedReport;

        public void Configure(
            SaveRecoveryNoticeController controller,
            Func<SaveRecoveryReport> getRecoveryReport,
            TopMenuController menuController = null,
            BottomActionBarController actionBarController = null,
            DevPanelController developerPanelController = null)
        {
            HideNoticeAndRestoreControls();
            noticeController = controller;
            recoveryReportProvider = getRecoveryReport;
            topMenuController = menuController;
            bottomActionBarController = actionBarController;
            devPanelController = developerPanelController;
            configured = noticeController != null && recoveryReportProvider != null;
            nextPollAt = 0f;

            if (isActiveAndEnabled)
            {
                TryShowPendingNotice();
            }
        }

        public bool TryShowPendingNotice()
        {
            if (!configured
                || noticeController == null
                || noticeController.IsVisible)
            {
                return false;
            }

            var report = recoveryReportProvider?.Invoke();
            if (report == null
                || !report.UserNotificationRecommended
                || ReferenceEquals(report, acknowledgedReport))
            {
                return false;
            }

            SuspendControls();
            if (!noticeController.Show(report, HandleConfirmed))
            {
                RestoreControls();
                return false;
            }

            displayedReport = report;
            return true;
        }

        public void HidePendingNotice()
        {
            HideNoticeAndRestoreControls();
            nextPollAt = 0f;
        }

        private void OnEnable()
        {
            nextPollAt = 0f;
            if (configured)
            {
                TryShowPendingNotice();
            }
        }

        private void OnDisable()
        {
            HidePendingNotice();
        }

        private void Update()
        {
            if (!configured
                || !Application.isPlaying
                || IsBlockingGameplay
                || Time.unscaledTime < nextPollAt)
            {
                return;
            }

            nextPollAt = Time.unscaledTime + PollIntervalSeconds;
            TryShowPendingNotice();
        }

        private void HandleConfirmed()
        {
            acknowledgedReport = displayedReport;
            displayedReport = null;
            RestoreControls();
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
            bottomActionBarWasEnabled = bottomActionBarController != null
                && bottomActionBarController.enabled;
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

        private void HideNoticeAndRestoreControls()
        {
            if (noticeController != null)
            {
                noticeController.Hide();
            }

            displayedReport = null;
            RestoreControls();
        }
    }
}
