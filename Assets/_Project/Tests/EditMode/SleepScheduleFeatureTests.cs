using System;
using System.Collections.Generic;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Sleep;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class SleepScheduleFeatureTests
    {
        private static readonly DateTimeOffset BaseTime =
            new DateTimeOffset(2026, 8, 14, 22, 0, 0, TimeSpan.FromHours(9));

        [Test]
        public void StartStoresCanonicalScheduleAndBlocksDuplicateOrInvalidDurations()
        {
            var system = new SleepScheduleSystem();
            var save = new SleepScheduleSaveData();
            var tama = CreateHatchedTama();

            Assert.That(
                system.TryStart(save, tama, 0, "invalid-low", BaseTime).Status,
                Is.EqualTo(SleepScheduleStartStatus.InvalidDuration));
            Assert.That(
                system.TryStart(save, tama, 9, "invalid-high", BaseTime).Status,
                Is.EqualTo(SleepScheduleStartStatus.InvalidDuration));

            var started = system.TryStart(
                save,
                tama,
                4,
                " schedule-001 ",
                BaseTime);

            Assert.That(started.Started, Is.True);
            Assert.That(started.StateChanged, Is.True);
            Assert.That(started.ReceiptKey, Is.EqualTo("schedule-001"));
            Assert.That(save.activeSession, Is.Not.Null);
            Assert.That(save.activeSession.scheduledHours, Is.EqualTo(4));
            Assert.That(
                Parse(save.activeSession.sleepStartedAtIso),
                Is.EqualTo(BaseTime));
            Assert.That(
                Parse(save.activeSession.plannedWakeAtIso),
                Is.EqualTo(BaseTime.AddHours(4)));

            var duplicate = system.TryStart(
                save,
                tama,
                2,
                "schedule-002",
                BaseTime.AddMinutes(1));
            Assert.That(
                duplicate.Status,
                Is.EqualTo(SleepScheduleStartStatus.AlreadySleeping));
            Assert.That(save.activeSession.receiptKey, Is.EqualTo("schedule-001"));
        }

        [Test]
        public void SaveNormalizationRepairsCanonicalFieldsAndFailsClosedForFutureOrMalformedStarts()
        {
            var save = new SleepScheduleSaveData
            {
                schemaVersion = -3,
                activeSession = new SleepSessionSaveData
                {
                    receiptKey = " receipt ",
                    sleepStartedAtIso = "2026-08-14T22:00:00+09:00",
                    plannedWakeAtIso = "2026-08-15T02:00:00+09:00",
                    scheduledHours = 99
                },
                recoveryReceipts = null,
                lastWakeAtIso = null
            };

            Assert.That(save.EnsureRuntimeDefaults(BaseTime), Is.True);
            Assert.That(save.schemaVersion, Is.EqualTo(SleepScheduleSaveData.CurrentSchemaVersion));
            Assert.That(save.activeSession, Is.Not.Null);
            Assert.That(save.activeSession.receiptKey, Is.EqualTo("receipt"));
            Assert.That(save.activeSession.scheduledHours, Is.EqualTo(4));
            Assert.That(Parse(save.activeSession.sleepStartedAtIso), Is.EqualTo(BaseTime));
            Assert.That(save.recoveryReceipts, Is.Empty);
            Assert.That(save.lastWakeAtIso, Is.Empty);

            save.activeSession = new SleepSessionSaveData
            {
                receiptKey = "future",
                sleepStartedAtIso = BaseTime.AddMinutes(1).ToString("O"),
                plannedWakeAtIso = BaseTime.AddHours(1).AddMinutes(1).ToString("O"),
                scheduledHours = 1
            };
            Assert.That(save.EnsureRuntimeDefaults(BaseTime), Is.True);
            Assert.That(save.activeSession, Is.Null);

            save.activeSession = new SleepSessionSaveData
            {
                receiptKey = "malformed",
                sleepStartedAtIso = "not-a-time",
                plannedWakeAtIso = BaseTime.AddHours(2).ToString("O"),
                scheduledHours = 2
            };
            Assert.That(save.EnsureRuntimeDefaults(BaseTime), Is.True);
            Assert.That(save.activeSession, Is.Null);
        }

        [Test]
        public void EarlyWakeUsesActualElapsedMinutesAndAppliesReceiptExactlyOnce()
        {
            var system = new SleepScheduleSystem();
            var save = new SleepScheduleSaveData();
            var tama = CreateHatchedTama();
            tama.stats.sleepiness = 80;
            tama.stats.health = 95;
            tama.stats.mood = 98;
            system.TryStart(save, tama, 4, "early-001", BaseTime);

            var result = system.TryWakeEarly(
                save,
                tama,
                BaseTime.AddMinutes(90));

            Assert.That(result.Status, Is.EqualTo(SleepScheduleWakeStatus.WokeEarly));
            Assert.That(result.Applied, Is.True);
            Assert.That(result.ElapsedMinutes, Is.EqualTo(90));
            Assert.That(result.SleepinessDelta, Is.EqualTo(-22));
            Assert.That(result.HealthDelta, Is.EqualTo(1));
            Assert.That(result.MoodDelta, Is.Zero);
            Assert.That(tama.stats.sleepiness, Is.EqualTo(58));
            Assert.That(tama.stats.health, Is.EqualTo(96));
            Assert.That(tama.stats.mood, Is.EqualTo(98));
            Assert.That(save.activeSession, Is.Null);
            Assert.That(save.recoveryReceipts, Has.Count.EqualTo(1));
            Assert.That(save.recoveryReceipts[0].wasEarlyWake, Is.True);
            Assert.That(save.recoveryReceipts[0].elapsedMinutes, Is.EqualTo(90));

            var sleepinessAfterFirstApply = tama.stats.sleepiness;
            var second = system.TryWakeEarly(
                save,
                tama,
                BaseTime.AddMinutes(100));
            Assert.That(
                second.Status,
                Is.EqualTo(SleepScheduleWakeStatus.NoActiveSession));
            Assert.That(tama.stats.sleepiness, Is.EqualTo(sleepinessAfterFirstApply));
            Assert.That(save.recoveryReceipts, Has.Count.EqualTo(1));
        }

        [Test]
        public void ReloadResumesAndLateClaimCreditsOnlyScheduledDuration()
        {
            var system = new SleepScheduleSystem();
            var save = new SleepScheduleSaveData();
            var tama = CreateHatchedTama();
            tama.stats.sleepiness = 90;
            tama.stats.health = 90;
            tama.stats.mood = 90;
            system.TryStart(save, tama, 3, "reload-001", BaseTime);

            var restored = JsonUtility.FromJson<SleepScheduleSaveData>(
                JsonUtility.ToJson(save));
            var midway = system.BuildSnapshot(
                restored,
                tama,
                BaseTime.AddMinutes(90));

            Assert.That(midway.IsSleeping, Is.True);
            Assert.That(midway.ElapsedMinutes, Is.EqualTo(90));
            Assert.That(midway.RemainingMinutes, Is.EqualTo(90));
            Assert.That(midway.IsDue, Is.False);
            Assert.That(
                system.TryCompleteDue(
                    restored,
                    tama,
                    BaseTime.AddMinutes(90)).Status,
                Is.EqualTo(SleepScheduleWakeStatus.NotDue));

            var completed = system.TryCompleteDue(
                restored,
                tama,
                BaseTime.AddHours(12));

            Assert.That(completed.Status, Is.EqualTo(SleepScheduleWakeStatus.Completed));
            Assert.That(completed.ElapsedMinutes, Is.EqualTo(180));
            Assert.That(completed.SleepinessDelta, Is.EqualTo(-45));
            Assert.That(completed.HealthDelta, Is.EqualTo(3));
            Assert.That(completed.MoodDelta, Is.EqualTo(1));
            Assert.That(
                Parse(completed.Receipt.wokeAtIso),
                Is.EqualTo(BaseTime.AddHours(3)));
            Assert.That(
                Parse(completed.Receipt.claimedAtIso),
                Is.EqualTo(BaseTime.AddHours(12)));
        }

        [Test]
        public void FutureClockAndReceiptReplayNeverApplyRecoveryTwice()
        {
            var system = new SleepScheduleSystem();
            var tama = CreateHatchedTama();
            tama.stats.sleepiness = 80;
            var futureSave = new SleepScheduleSaveData
            {
                activeSession = new SleepSessionSaveData
                {
                    receiptKey = "future-001",
                    sleepStartedAtIso = BaseTime.AddHours(1).ToString("O"),
                    plannedWakeAtIso = BaseTime.AddHours(2).ToString("O"),
                    scheduledHours = 1
                }
            };

            var invalidClock = system.TryWakeEarly(futureSave, tama, BaseTime);
            Assert.That(
                invalidClock.Status,
                Is.EqualTo(SleepScheduleWakeStatus.InvalidClock));
            Assert.That(futureSave.activeSession, Is.Null);
            Assert.That(futureSave.recoveryReceipts, Is.Empty);
            Assert.That(tama.stats.sleepiness, Is.EqualTo(80));

            var replaySave = new SleepScheduleSaveData();
            system.TryStart(replaySave, tama, 1, "replay-001", BaseTime);
            system.TryCompleteDue(replaySave, tama, BaseTime.AddHours(1));
            var afterFirstApply = tama.stats.sleepiness;
            replaySave.activeSession = new SleepSessionSaveData
            {
                receiptKey = "replay-001",
                sleepStartedAtIso = BaseTime.ToString("O"),
                plannedWakeAtIso = BaseTime.AddHours(1).ToString("O"),
                scheduledHours = 1
            };

            var replay = system.TryWakeEarly(
                replaySave,
                tama,
                BaseTime.AddHours(1));
            Assert.That(replay.Status, Is.EqualTo(SleepScheduleWakeStatus.AlreadyApplied));
            Assert.That(replaySave.activeSession, Is.Null);
            Assert.That(replaySave.recoveryReceipts, Has.Count.EqualTo(1));
            Assert.That(tama.stats.sleepiness, Is.EqualTo(afterFirstApply));
        }

        [Test]
        public void EggCannotStartAndCorruptActiveEggSessionClearsWithoutReward()
        {
            var system = new SleepScheduleSystem();
            var save = new SleepScheduleSaveData();
            var tama = CreateHatchedTama();
            tama.isHatched = false;

            var rejected = system.TryStart(save, tama, 2, "egg-001", BaseTime);
            Assert.That(rejected.Status, Is.EqualTo(SleepScheduleStartStatus.NotHatched));
            Assert.That(save.activeSession, Is.Null);

            tama.isHatched = true;
            system.TryStart(save, tama, 2, "egg-002", BaseTime);
            tama.isHatched = false;
            var sleepinessBefore = tama.stats.sleepiness;
            var wake = system.TryWakeEarly(save, tama, BaseTime.AddHours(1));

            Assert.That(wake.Status, Is.EqualTo(SleepScheduleWakeStatus.NotHatched));
            Assert.That(save.activeSession, Is.Null);
            Assert.That(save.recoveryReceipts, Is.Empty);
            Assert.That(tama.stats.sleepiness, Is.EqualTo(sleepinessBefore));
        }

        [Test]
        public void CallbackPanelUsesKoreanCopyBlocksInputAndDoesNotDuplicateListeners()
        {
            var host = new GameObject("Sleep Schedule Test Host");
            var root = new GameObject(
                SleepSchedulePanelController.OverlayObjectName,
                typeof(RectTransform),
                typeof(Image));
            root.transform.SetParent(host.transform);
            root.SetActive(false);

            try
            {
                var controller = host.AddComponent<SleepSchedulePanelController>();
                var title = CreateText(root.transform, "Title");
                var summary = CreateText(root.transform, "Summary");
                var detail = CreateText(root.transform, "Detail");
                var status = CreateText(root.transform, "Status");
                var durationButtons = CreateButtons(root.transform, "Duration", 8);
                var durationTexts = CreateTexts(root.transform, "Duration Label", 8);
                var startButton = CreateButton(root.transform, "Start");
                var wakeButton = CreateButton(root.transform, "Wake");
                var wakeText = CreateText(wakeButton.transform, "Wake Label");
                var closeButton = CreateButton(root.transform, "Close");
                var system = new SleepScheduleSystem();
                var save = new SleepScheduleSaveData();
                var tama = CreateHatchedTama();
                var now = BaseTime;
                var startCount = 0;
                var wakeCount = 0;
                var closeCount = 0;
                var blockingStates = new List<bool>();

                Func<SleepScheduleSnapshot> snapshot =
                    () => system.BuildSnapshot(save, tama, now);
                Func<int, SleepScheduleStartResult> start = hours =>
                {
                    startCount += 1;
                    return system.TryStart(save, tama, hours, "ui-001", now);
                };
                Func<SleepScheduleWakeResult> wake = () =>
                {
                    wakeCount += 1;
                    return system.TryWakeEarly(save, tama, now);
                };

                ConfigurePanel(
                    controller,
                    root,
                    title,
                    summary,
                    detail,
                    status,
                    durationTexts,
                    wakeText,
                    durationButtons,
                    startButton,
                    wakeButton,
                    closeButton,
                    snapshot,
                    start,
                    wake,
                    () => closeCount += 1,
                    blocked => blockingStates.Add(blocked));
                ConfigurePanel(
                    controller,
                    root,
                    title,
                    summary,
                    detail,
                    status,
                    durationTexts,
                    wakeText,
                    durationButtons,
                    startButton,
                    wakeButton,
                    closeButton,
                    snapshot,
                    start,
                    wake,
                    () => closeCount += 1,
                    blocked => blockingStates.Add(blocked));

                Assert.That(controller.Open(), Is.True);
                Assert.That(controller.BlocksGameplayInput, Is.True);
                Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                Assert.That(title.text, Is.EqualTo("수면 예약"));
                Assert.That(summary.text, Does.Contain("1~8시간"));

                durationButtons[2].onClick.Invoke();
                startButton.onClick.Invoke();
                Assert.That(controller.SelectedDurationHours, Is.EqualTo(3));
                Assert.That(startCount, Is.EqualTo(1));
                Assert.That(summary.text, Does.Contain("3시간 수면 중"));
                Assert.That(status.text, Does.Contain("3시간 수면 예약"));

                now = now.AddHours(1);
                wakeButton.onClick.Invoke();
                Assert.That(wakeCount, Is.EqualTo(1));
                Assert.That(status.text, Does.Contain("일찍 일어났어요"));

                closeButton.onClick.Invoke();
                Assert.That(controller.BlocksGameplayInput, Is.False);
                Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(closeCount, Is.EqualTo(1));
                Assert.That(blockingStates, Is.EqualTo(new[] { true, false }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static CheeseTamaModel CreateHatchedTama()
        {
            var tama = new CheeseTamaModel
            {
                isHatched = true,
                form = "soft_cheesetama"
            };
            tama.EnsureRuntimeDefaults();
            return tama;
        }

        private static DateTimeOffset Parse(string value)
        {
            Assert.That(DateTimeOffset.TryParse(value, out var parsed), Is.True);
            return parsed;
        }

        private static void ConfigurePanel(
            SleepSchedulePanelController controller,
            GameObject root,
            Text title,
            Text summary,
            Text detail,
            Text status,
            Text[] durationTexts,
            Text wakeText,
            Button[] durationButtons,
            Button startButton,
            Button wakeButton,
            Button closeButton,
            Func<SleepScheduleSnapshot> snapshot,
            Func<int, SleepScheduleStartResult> start,
            Func<SleepScheduleWakeResult> wake,
            Action close,
            Action<bool> blockingChanged)
        {
            controller.Configure(
                root,
                title,
                summary,
                detail,
                status,
                durationTexts,
                wakeText,
                durationButtons,
                startButton,
                wakeButton,
                closeButton,
                snapshot,
                start,
                wake,
                close,
                blockingChanged);
        }

        private static Text[] CreateTexts(Transform parent, string baseName, int count)
        {
            var values = new Text[count];
            for (var index = 0; index < count; index += 1)
            {
                values[index] = CreateText(parent, baseName + " " + index);
            }

            return values;
        }

        private static Button[] CreateButtons(Transform parent, string baseName, int count)
        {
            var values = new Button[count];
            for (var index = 0; index < count; index += 1)
            {
                values[index] = CreateButton(parent, baseName + " " + index);
            }

            return values;
        }

        private static Text CreateText(Transform parent, string objectName)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text));
            gameObject.transform.SetParent(parent);
            return gameObject.GetComponent<Text>();
        }

        private static Button CreateButton(Transform parent, string objectName)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent);
            return gameObject.GetComponent<Button>();
        }
    }
}
