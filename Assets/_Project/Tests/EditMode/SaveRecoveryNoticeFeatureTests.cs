using System;
using System.Reflection;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class SaveRecoveryNoticeFeatureTests
    {
        [TestCase(
            SaveRecoveryOutcome.RecoveredFromTemporaryFile,
            "저장 복구 완료",
            "중단된 저장 작업")]
        [TestCase(
            SaveRecoveryOutcome.RecoveredFromBackup,
            "저장 복구 완료",
            "직전 백업")]
        [TestCase(
            SaveRecoveryOutcome.CreatedFreshSaveAfterCorruption,
            "저장 상태 안내",
            "새 저장으로 시작")]
        public void RecoveryOutcomeRendersExpectedKoreanNotice(
            SaveRecoveryOutcome outcome,
            string expectedTitle,
            string expectedMessageFragment)
        {
            using var fixture = NoticeFixture.Create();
            var report = CreateReport(outcome, 2);

            Assert.That(fixture.Controller.Show(report, null), Is.True);

            Assert.That(fixture.Title.text, Is.EqualTo(expectedTitle));
            Assert.That(fixture.Message.text, Does.Contain(expectedMessageFragment));
            Assert.That(fixture.Message.text, Does.Contain("손상된 파일 2개"));
            Assert.That(fixture.Controller.CurrentOutcome, Is.EqualTo(outcome));
        }

        [Test]
        public void NoRecoveryDoesNotOpenOrSuspendControls()
        {
            using var fixture = NoticeFixture.Create();
            fixture.ConfigureBridge(() => SaveRecoveryReport.NoRecovery);

            Assert.That(fixture.Bridge.TryShowPendingNotice(), Is.False);
            Assert.That(fixture.Controller.IsVisible, Is.False);
            Assert.That(fixture.TopMenu.enabled, Is.True);
            Assert.That(fixture.ActionBar.enabled, Is.True);
            Assert.That(fixture.DevPanel.enabled, Is.True);
        }

        [Test]
        public void NoticeIsFullScreenBlockingAndConfirmRestoresControlsOnce()
        {
            using var fixture = NoticeFixture.Create();
            var report = CreateReport(SaveRecoveryOutcome.RecoveredFromBackup, 1);
            fixture.ConfigureBridge(() => report);

            Assert.That(fixture.Controller.IsBlockingGameplay, Is.True);
            Assert.That(fixture.OverlayRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.OverlayRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(fixture.OverlayRect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.OverlayRect.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(fixture.Overlay.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            Assert.That(fixture.Overlay.GetComponent<Graphic>().raycastTarget, Is.True);
            Assert.That(fixture.TopMenu.enabled, Is.False);
            Assert.That(fixture.ActionBar.enabled, Is.False);
            Assert.That(fixture.DevPanel.enabled, Is.False);

            fixture.Confirm.onClick.Invoke();

            Assert.That(fixture.Controller.IsVisible, Is.False);
            Assert.That(fixture.TopMenu.enabled, Is.True);
            Assert.That(fixture.ActionBar.enabled, Is.True);
            Assert.That(fixture.DevPanel.enabled, Is.True);
            Assert.That(fixture.Bridge.AcknowledgedReport, Is.SameAs(report));
            Assert.That(fixture.Bridge.TryShowPendingNotice(), Is.False);
        }

        [Test]
        public void ControlRestorePreservesPreexistingDisabledState()
        {
            using var fixture = NoticeFixture.Create();
            fixture.ActionBar.enabled = false;
            var report = CreateReport(SaveRecoveryOutcome.RecoveredFromTemporaryFile, 0);

            fixture.ConfigureBridge(() => report);
            fixture.Confirm.onClick.Invoke();

            Assert.That(fixture.TopMenu.enabled, Is.True);
            Assert.That(fixture.ActionBar.enabled, Is.False);
            Assert.That(fixture.DevPanel.enabled, Is.True);
        }

        [Test]
        public void NewReportObjectCanBePresentedAfterEarlierLoadWasAcknowledged()
        {
            using var fixture = NoticeFixture.Create();
            var current = CreateReport(SaveRecoveryOutcome.RecoveredFromBackup, 1);
            fixture.ConfigureBridge(() => current);
            fixture.Confirm.onClick.Invoke();

            current = CreateReport(SaveRecoveryOutcome.CreatedFreshSaveAfterCorruption, 3);

            Assert.That(fixture.Bridge.TryShowPendingNotice(), Is.True);
            Assert.That(fixture.Controller.CurrentOutcome,
                Is.EqualTo(SaveRecoveryOutcome.CreatedFreshSaveAfterCorruption));
        }

        [Test]
        public void HidingPendingNoticeClosesAndRestoresControlsWithoutAcknowledging()
        {
            using var fixture = NoticeFixture.Create();
            var report = CreateReport(SaveRecoveryOutcome.RecoveredFromBackup, 1);
            fixture.ConfigureBridge(() => report);

            fixture.Bridge.HidePendingNotice();

            Assert.That(fixture.Controller.IsVisible, Is.False);
            Assert.That(fixture.TopMenu.enabled, Is.True);
            Assert.That(fixture.ActionBar.enabled, Is.True);
            Assert.That(fixture.DevPanel.enabled, Is.True);
            Assert.That(fixture.Bridge.AcknowledgedReport, Is.Null);
        }

        private static SaveRecoveryReport CreateReport(
            SaveRecoveryOutcome outcome,
            int quarantinedFileCount)
        {
            return (SaveRecoveryReport)Activator.CreateInstance(
                typeof(SaveRecoveryReport),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { outcome, quarantinedFileCount },
                null);
        }

        private sealed class NoticeFixture : IDisposable
        {
            private readonly GameObject root;

            private NoticeFixture(
                GameObject root,
                GameObject overlay,
                SaveRecoveryNoticeController controller,
                SaveRecoveryNoticeBridge bridge,
                Text title,
                Text message,
                Button confirm,
                TopMenuController topMenu,
                BottomActionBarController actionBar,
                DevPanelController devPanel)
            {
                this.root = root;
                Overlay = overlay;
                Controller = controller;
                Bridge = bridge;
                Title = title;
                Message = message;
                Confirm = confirm;
                TopMenu = topMenu;
                ActionBar = actionBar;
                DevPanel = devPanel;
            }

            public GameObject Overlay { get; }
            public RectTransform OverlayRect => (RectTransform)Overlay.transform;
            public SaveRecoveryNoticeController Controller { get; }
            public SaveRecoveryNoticeBridge Bridge { get; }
            public Text Title { get; }
            public Text Message { get; }
            public Button Confirm { get; }
            public TopMenuController TopMenu { get; }
            public BottomActionBarController ActionBar { get; }
            public DevPanelController DevPanel { get; }

            public static NoticeFixture Create()
            {
                var root = new GameObject("Save Recovery Notice Test Root", typeof(RectTransform));
                var overlay = CreateChild(root.transform, SaveRecoveryNoticeController.OverlayObjectName);
                var title = CreateChild(overlay.transform, "Title").AddComponent<Text>();
                var message = CreateChild(overlay.transform, "Message").AddComponent<Text>();
                var confirm = CreateChild(overlay.transform, "Confirm").AddComponent<Button>();
                var topMenu = CreateChild(root.transform, "Top Menu").AddComponent<TopMenuController>();
                var actionBar = CreateChild(root.transform, "Action Bar").AddComponent<BottomActionBarController>();
                var devPanel = CreateChild(root.transform, "Dev Panel").AddComponent<DevPanelController>();
                var controller = root.AddComponent<SaveRecoveryNoticeController>();
                var bridge = root.AddComponent<SaveRecoveryNoticeBridge>();

                controller.Configure(overlay, title, message, confirm);
                return new NoticeFixture(
                    root,
                    overlay,
                    controller,
                    bridge,
                    title,
                    message,
                    confirm,
                    topMenu,
                    actionBar,
                    devPanel);
            }

            public void ConfigureBridge(Func<SaveRecoveryReport> reportProvider)
            {
                Bridge.Configure(
                    Controller,
                    reportProvider,
                    TopMenu,
                    ActionBar,
                    DevPanel);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            private static GameObject CreateChild(Transform parent, string objectName)
            {
                var child = new GameObject(objectName, typeof(RectTransform));
                child.transform.SetParent(parent, false);
                return child;
            }
        }
    }
}
