using System;
using System.IO;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.Sleep;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class SleepScheduleIntegrationTests
    {
        [Test]
        public void ManagerStartWakeAndReloadApplyRecoveryAndRestExactlyOnce()
        {
            using var fixture = IsolatedGameManagerFixture.Create("exactly_once");
            fixture.Manager.LoadOrCreateGame();
            PrepareHatchedTama(fixture.Manager);

            var startedAt = DateTimeOffset.Now.AddMinutes(-40);
            const string receiptKey = "sleep-integration-exactly-once";
            var started = fixture.Manager.StartSleepSchedule(
                2,
                receiptKey,
                startedAt);

            Assert.That(started.Started, Is.True);
            Assert.That(fixture.Manager.IsSleepScheduleActive, Is.True);
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.activeSession.receiptKey,
                Is.EqualTo(receiptKey));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.IsSleepScheduleActive, Is.True);
            Assert.That(fixture.Manager.GetSleepScheduleSnapshot(startedAt.AddMinutes(30)).ReceiptKey,
                Is.EqualTo(receiptKey));

            var historyBefore = fixture.Manager.CurrentSave.careHistory;
            var totalBefore = historyBefore.totalCareActions;
            var restsBefore = historyBefore.rests;
            var dailyRestsBefore = fixture.Manager.CurrentSave.dailyCare.rests;
            var wake = fixture.Manager.WakeSleepSchedule(startedAt.AddMinutes(30));

            Assert.That(wake.Applied, Is.True);
            Assert.That(wake.ElapsedMinutes, Is.EqualTo(30));
            Assert.That(fixture.Manager.IsSleepScheduleActive, Is.False);
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.recoveryReceipts,
                Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.totalCareActions,
                Is.EqualTo(totalBefore + 1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.rests,
                Is.EqualTo(restsBefore + 1));
            Assert.That(fixture.Manager.CurrentSave.dailyCare.rests,
                Is.EqualTo(dailyRestsBefore + 1));

            var duplicateWake = fixture.Manager.WakeSleepSchedule(startedAt.AddMinutes(31));
            Assert.That(duplicateWake.Status, Is.EqualTo(SleepScheduleWakeStatus.NoActiveSession));
            Assert.That(duplicateWake.StateChanged, Is.False);
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.recoveryReceipts,
                Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.totalCareActions,
                Is.EqualTo(totalBefore + 1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.rests,
                Is.EqualTo(restsBefore + 1));
            Assert.That(fixture.Manager.CurrentSave.dailyCare.rests,
                Is.EqualTo(dailyRestsBefore + 1));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.IsSleepScheduleActive, Is.False);
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.recoveryReceipts,
                Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.totalCareActions,
                Is.EqualTo(totalBefore + 1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.rests,
                Is.EqualTo(restsBefore + 1));
            Assert.That(fixture.Manager.CurrentSave.dailyCare.rests,
                Is.EqualTo(dailyRestsBefore + 1));

            var reusedReceipt = fixture.Manager.StartSleepSchedule(
                2,
                receiptKey,
                startedAt.AddHours(3));
            Assert.That(reusedReceipt.Status,
                Is.EqualTo(SleepScheduleStartStatus.ReceiptAlreadyUsed));
            Assert.That(reusedReceipt.StateChanged, Is.False);
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.recoveryReceipts,
                Has.Count.EqualTo(1));
        }

        [Test]
        public void ManagerWakeBeforeThirtyMinutesDoesNotRegisterRestCare()
        {
            using var fixture = IsolatedGameManagerFixture.Create("under_threshold");
            fixture.Manager.LoadOrCreateGame();
            PrepareHatchedTama(fixture.Manager);

            var startedAt = DateTimeOffset.Now.AddMinutes(-35);
            var totalBefore = fixture.Manager.CurrentSave.careHistory.totalCareActions;
            var restsBefore = fixture.Manager.CurrentSave.careHistory.rests;
            var dailyRestsBefore = fixture.Manager.CurrentSave.dailyCare.rests;

            Assert.That(
                fixture.Manager.StartSleepSchedule(
                    2,
                    "sleep-integration-under-thirty",
                    startedAt).Started,
                Is.True);

            var wake = fixture.Manager.WakeSleepSchedule(startedAt.AddMinutes(29));

            Assert.That(wake.Applied, Is.True);
            Assert.That(wake.ElapsedMinutes, Is.EqualTo(29));
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.recoveryReceipts,
                Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.totalCareActions,
                Is.EqualTo(totalBefore));
            Assert.That(fixture.Manager.CurrentSave.careHistory.rests,
                Is.EqualTo(restsBefore));
            Assert.That(fixture.Manager.CurrentSave.dailyCare.rests,
                Is.EqualTo(dailyRestsBefore));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.recoveryReceipts,
                Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.careHistory.totalCareActions,
                Is.EqualTo(totalBefore));
            Assert.That(fixture.Manager.CurrentSave.careHistory.rests,
                Is.EqualTo(restsBefore));
            Assert.That(fixture.Manager.CurrentSave.dailyCare.rests,
                Is.EqualTo(dailyRestsBefore));
        }

        [Test]
        public void BuilderCreatesSleepScheduleUiIdempotentlyAndReconfiguresSleepButton()
        {
            var canvas = new GameObject(
                "Sleep Schedule Builder Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                var actionBar = new GameObject(
                    "Bottom Action Bar",
                    typeof(RectTransform),
                    typeof(BottomActionBarController));
                actionBar.transform.SetParent(canvas.transform, false);
                var sleepButton = CreateButton(actionBar.transform, "Sleep Button");
                CreateText(sleepButton.transform, "Label");

                InvokeBuilder("EnsureSleepSchedulePanel", canvas.transform, null, null);
                InvokeBuilder("EnsureSleepSchedulePanel", canvas.transform, null, null);

                AssertNamedObjectCount(canvas.transform,
                    SleepSchedulePanelController.OverlayObjectName, 1);
                AssertNamedObjectCount(canvas.transform, "Sleep Schedule Card", 1);
                Assert.That(canvas.GetComponents<SleepSchedulePanelController>(), Has.Length.EqualTo(1));
                Assert.That(canvas.GetComponents<SleepScheduleBridge>(), Has.Length.EqualTo(1));

                var overlay = Require(canvas.transform,
                    SleepSchedulePanelController.OverlayObjectName);
                Assert.That(overlay.activeSelf, Is.False);
                var card = Require(overlay.transform, "Sleep Schedule Card");
                for (var hour = 1; hour <= 8; hour += 1)
                {
                    AssertNamedObjectCount(
                        card.transform,
                        $"Sleep Duration Button {hour}",
                        1);
                }

                var careButton = sleepButton.GetComponent<MilkroomCareButton>();
                Assert.That(careButton, Is.Not.Null);
                Assert.That(ReadPrivateField<MilkroomCareAction>(careButton, "action"),
                    Is.EqualTo(MilkroomCareAction.SleepSchedule));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void LegacySaveWithoutSleepScheduleMigratesAndPersistsField()
        {
            using var fixture = IsolatedGameManagerFixture.Create("legacy_migration");
            fixture.WriteRawJson(
                "{\"version\":\"0.1.0\",\"playerId\":\"legacy_sleep_player\"}");

            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.SaveManager.LastLoadMigratedData, Is.True);
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule, Is.Not.Null);
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.schemaVersion,
                Is.EqualTo(SleepScheduleSaveData.CurrentSchemaVersion));
            Assert.That(File.ReadAllText(fixture.SaveManager.SaveFilePath),
                Does.Contain("\"sleepSchedule\""));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.sleepSchedule, Is.Not.Null);
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.schemaVersion,
                Is.EqualTo(SleepScheduleSaveData.CurrentSchemaVersion));
            Assert.That(fixture.Manager.CurrentSave.sleepSchedule.HasActiveSession, Is.False);
        }

        private static void PrepareHatchedTama(GameManager manager)
        {
            manager.CurrentTama.isHatched = true;
            manager.CurrentTama.form = "soft_cheesetama";
            manager.CurrentTama.stats.sleepiness = 80;
            manager.CurrentTama.stats.health = 80;
            manager.CurrentTama.stats.mood = 80;
            manager.SaveGame();
        }

        private static void InvokeBuilder(string methodName, params object[] arguments)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, methodName);
            try
            {
                method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static T ReadPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static GameObject Require(Transform root, string objectName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index += 1)
            {
                if (string.Equals(transforms[index].name, objectName, StringComparison.Ordinal))
                {
                    return transforms[index].gameObject;
                }
            }

            Assert.Fail($"Missing object: {objectName}");
            return null;
        }

        private static void AssertNamedObjectCount(
            Transform root,
            string objectName,
            int expected)
        {
            var count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index += 1)
            {
                if (string.Equals(transforms[index].name, objectName, StringComparison.Ordinal))
                {
                    count += 1;
                }
            }

            Assert.That(count, Is.EqualTo(expected), objectName);
        }

        private static Button CreateButton(Transform parent, string objectName)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string objectName)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Text>();
        }

        private sealed class IsolatedGameManagerFixture : IDisposable
        {
            private readonly GameObject root;

            private IsolatedGameManagerFixture(
                GameObject root,
                SaveManager saveManager,
                GameManager manager,
                string isolatedFileName,
                string previousSaveFileNameOverride,
                string previousEnvironmentSaveFileName)
            {
                this.root = root;
                SaveManager = saveManager;
                Manager = manager;
                IsolatedFileName = isolatedFileName;
                PreviousSaveFileNameOverride = previousSaveFileNameOverride;
                PreviousEnvironmentSaveFileName = previousEnvironmentSaveFileName;
            }

            public SaveManager SaveManager { get; }
            public GameManager Manager { get; private set; }
            private string IsolatedFileName { get; }
            private string PreviousSaveFileNameOverride { get; }
            private string PreviousEnvironmentSaveFileName { get; }

            public static IsolatedGameManagerFixture Create(string label)
            {
                var isolatedFileName =
                    $"{SaveManager.PlayModeTestSaveFileNamePrefix}{Guid.NewGuid():N}.json";
                var previousSaveFileNameOverride =
                    SaveManager.PlayModeTestSaveFileNameOverride;
                var previousEnvironmentSaveFileName = System.Environment.GetEnvironmentVariable(
                    SaveManager.PlayModeTestSaveFileNameEnvironmentVariable);
                var root = new GameObject($"{label} Sleep Integration Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                System.Environment.SetEnvironmentVariable(
                    SaveManager.PlayModeTestSaveFileNameEnvironmentVariable,
                    isolatedFileName);
                SaveManager.SetPlayModeTestSaveFileNameOverride(isolatedFileName);
                saveManager.SetIsolatedSaveFileNameForTests(isolatedFileName);
                Assert.That(
                    Path.GetFileName(saveManager.SaveFilePath),
                    Is.EqualTo(isolatedFileName),
                    "Refusing to run against a non-isolated save path.");

                var manager = root.AddComponent<GameManager>();
                SetPrivateField(manager, "saveManager", saveManager);
                return new IsolatedGameManagerFixture(
                    root,
                    saveManager,
                    manager,
                    isolatedFileName,
                    previousSaveFileNameOverride,
                    previousEnvironmentSaveFileName);
            }

            public void WriteRawJson(string json)
            {
                VerifyIsolatedPath();
                Directory.CreateDirectory(Path.GetDirectoryName(SaveManager.SaveFilePath));
                File.WriteAllText(SaveManager.SaveFilePath, json);
            }

            public void RecreateManager()
            {
                UnityEngine.Object.DestroyImmediate(Manager);
                Manager = root.AddComponent<GameManager>();
                SetPrivateField(Manager, "saveManager", SaveManager);
            }

            public void Dispose()
            {
                VerifyIsolatedPath();
                SaveManager.DeleteSave();
                SaveManager.ClearPlayModeTestSaveFileNameOverride(IsolatedFileName);
                if (!string.IsNullOrWhiteSpace(PreviousSaveFileNameOverride))
                {
                    SaveManager.SetPlayModeTestSaveFileNameOverride(
                        PreviousSaveFileNameOverride);
                }

                System.Environment.SetEnvironmentVariable(
                    SaveManager.PlayModeTestSaveFileNameEnvironmentVariable,
                    PreviousEnvironmentSaveFileName);

                UnityEngine.Object.DestroyImmediate(root);
            }

            private void VerifyIsolatedPath()
            {
                Assert.That(
                    Path.GetFileName(SaveManager.SaveFilePath),
                    Is.EqualTo(IsolatedFileName),
                    "Refusing to access a non-isolated save path.");
            }

            private static void SetPrivateField(
                object target,
                string fieldName,
                object value)
            {
                var field = target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, fieldName);
                field.SetValue(target, value);
            }
        }
    }
}
