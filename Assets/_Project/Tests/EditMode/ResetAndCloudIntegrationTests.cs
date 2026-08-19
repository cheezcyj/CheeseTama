using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Reset;
using CheeseTama.Platform;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class ResetAndCloudIntegrationTests
    {
        [Test]
        public void LifeRecordsSnapshotIsExposedByAuthoritativeManager()
        {
            using var fixture = IsolatedGameManagerFixture.Create("life_records");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.playMiniGames.highestBouncyJumpScore = 73;
            fixture.Manager.CurrentSave.playMiniGames.totalBouncyJumpSessions = 4;
            fixture.Manager.CurrentSave.playMiniGames.totalBouncyJumpSuccesses = 12;

            var snapshot = fixture.Manager.GetLifeRecordsSnapshot();

            Assert.That(snapshot.BouncyJump.HighestScore, Is.EqualTo(73));
            Assert.That(snapshot.BouncyJump.TotalSessions, Is.EqualTo(4));
            Assert.That(snapshot.BouncyJump.TotalSuccesses, Is.EqualTo(12));
        }

        [Test]
        public void CareResetPreservesAccountProgressAndClearsOnlyCurrentCareAndActiveWork()
        {
            using var fixture = IsolatedGameManagerFixture.Create("care_reset");
            fixture.Manager.LoadOrCreateGame();
            ConfigureAccountAndActiveCare(fixture.Manager.CurrentSave);
            fixture.Manager.SaveGame();

            var eventCount = 0;
            fixture.Manager.SaveDataReplaced += () => eventCount += 1;

            var result = fixture.Manager.TryResetProgress(
                ProgressResetMode.CareProgressOnly,
                ProgressResetPolicy.CareProgressConfirmationPhrase);
            var save = fixture.Manager.CurrentSave;

            Assert.That(result.Status, Is.EqualTo(ProgressResetResultStatus.Applied));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(save.playerId, Is.EqualTo("preserved-player"));
            Assert.That(save.settings.masterVolume, Is.EqualTo(0.35f));
            Assert.That(save.settings.highContrastUi, Is.True);
            Assert.That(save.milkroomThemeId, Is.EqualTo(MilkroomThemeController.NightThemeId));
            Assert.That(
                save.decorations.ownedThemeIds,
                Does.Contain(MilkroomThemeController.NightThemeId));
            Assert.That(save.economy.milkCoins, Is.EqualTo(4321));
            Assert.That(save.snackInventory, Has.Count.EqualTo(1));
            Assert.That(save.snackInventory[0].snackId, Is.EqualTo("account_snack"));
            Assert.That(save.unlocks.starMilkUnlocked, Is.True);
            Assert.That(save.collections.events, Does.Contain("account_collection"));
            Assert.That(save.playMiniGames.highestBouncyJumpScore, Is.EqualTo(91));
            Assert.That(save.firstDayJourney.legacySuppressed, Is.True);
            Assert.That(save.npcVisits.relationships, Has.Count.EqualTo(1));
            Assert.That(save.sleepSchedule.recoveryReceipts, Has.Count.EqualTo(1));
            Assert.That(save.npcRelationshipQuests.claimReceipts, Has.Count.EqualTo(1));

            Assert.That(save.cheeseTama.level, Is.EqualTo(1));
            Assert.That(save.cheeseTama.isHatched, Is.False);
            Assert.That(save.milkGrowth, Is.Empty);
            Assert.That(save.claimedMilkGrowthRewardKeys,
                Does.Contain("basic_milk:2"));
            Assert.That(save.careHistory.totalCareActions, Is.Zero);
            Assert.That(save.dailyCare.milkFeeds, Is.EqualTo(3));
            Assert.That(save.dailyCare.completedRoutineCount, Is.EqualTo(4));
            Assert.That(save.dailyCare.lastCompletedDateKey,
                Is.EqualTo(save.dailyCare.dateKey));
            Assert.That(save.milkroomSession.totalSeconds, Is.EqualTo(1200));
            Assert.That(save.milkroomSession.highestClaimedSessionMinute,
                Is.EqualTo(10));
            Assert.That(save.newGameSetup.legacySuppressed, Is.True);
            Assert.That(save.newGameSetup.outcomeApplied, Is.True);
            Assert.That(save.lateLevelGrowth.progressUnits, Is.Zero);
            Assert.That(save.randomEvents.pendingEvent.HasValue, Is.False);
            Assert.That(save.randomEvents.history, Has.Count.EqualTo(1));
            Assert.That(save.sleepSchedule.HasActiveSession, Is.False);
            Assert.That(save.npcRelationshipQuests.activeQuest.HasValue, Is.False);
            Assert.That(File.Exists(fixture.SaveManager.BackupFilePath), Is.True,
                "Care-only reset keeps the previous primary as a normal recovery backup.");

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("preserved-player"));
            Assert.That(fixture.Manager.CurrentSave.economy.milkCoins, Is.EqualTo(4321));
            Assert.That(fixture.Manager.CurrentSave.collections.events,
                Does.Contain("account_collection"));
            Assert.That(fixture.Manager.CurrentSave.cheeseTama.level, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.claimedMilkGrowthRewardKeys,
                Does.Contain("basic_milk:2"));
            Assert.That(fixture.Manager.CurrentSave.dailyCare.completedRoutineCount,
                Is.EqualTo(4));
            Assert.That(fixture.Manager.CurrentSave.milkroomSession.totalSeconds,
                Is.EqualTo(1200));
            Assert.That(fixture.Manager.CurrentSave.newGameSetup.outcomeApplied, Is.True);
            Assert.That(fixture.Manager.CurrentSave.randomEvents.pendingEvent.HasValue, Is.False);
        }

        [Test]
        public void ResetConfirmationMismatchDoesNotWriteSwapOrNotify()
        {
            using var fixture = IsolatedGameManagerFixture.Create("confirmation_mismatch");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.playerId = "must-remain";
            fixture.Manager.SaveGame();
            var originalReference = fixture.Manager.CurrentSave;
            var originalFile = File.ReadAllText(fixture.SaveManager.SaveFilePath);
            var eventCount = 0;
            fixture.Manager.SaveDataReplaced += () => eventCount += 1;

            var result = fixture.Manager.TryResetProgress(
                ProgressResetMode.CareProgressOnly,
                "reset tama");

            Assert.That(result.Status, Is.EqualTo(ProgressResetResultStatus.ConfirmationMismatch));
            Assert.That(fixture.Manager.CurrentSave, Is.SameAs(originalReference));
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("must-remain"));
            Assert.That(File.ReadAllText(fixture.SaveManager.SaveFilePath), Is.EqualTo(originalFile));
            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void FullResetReplacesEveryLogicalCategoryAndSurvivesReload()
        {
            using var fixture = IsolatedGameManagerFixture.Create("full_reset");
            fixture.Manager.LoadOrCreateGame();
            ConfigureAccountAndActiveCare(fixture.Manager.CurrentSave);
            fixture.Manager.SaveGame();
            File.WriteAllText(
                fixture.SaveManager.SaveFilePath + ".corrupt.synthetic",
                "old recovery data");
            var eventCount = 0;
            fixture.Manager.SaveDataReplaced += () => eventCount += 1;

            var result = fixture.Manager.TryResetProgress(
                ProgressResetMode.FullLocalData,
                ProgressResetPolicy.FullLocalDataConfirmationPhrase);

            Assert.That(result.Status, Is.EqualTo(ProgressResetResultStatus.Applied));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("local_player"));
            Assert.That(fixture.Manager.CurrentSave.economy.milkCoins, Is.Zero);
            Assert.That(fixture.Manager.CurrentSave.settings.masterVolume, Is.EqualTo(1f));
            Assert.That(fixture.Manager.CurrentSave.collections.events, Is.Empty);
            Assert.That(fixture.Manager.CurrentSave.playMiniGames.highestBouncyJumpScore, Is.Zero);
            Assert.That(File.Exists(fixture.SaveManager.SaveFilePath), Is.True);
            Assert.That(File.Exists(fixture.SaveManager.BackupFilePath), Is.False);
            Assert.That(File.Exists(fixture.SaveManager.TemporaryFilePath), Is.False);
            Assert.That(FindCorruptRecoveryFiles(fixture.SaveManager), Is.Empty);

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("local_player"));
            Assert.That(fixture.Manager.CurrentSave.economy.milkCoins, Is.Zero);
            Assert.That(fixture.Manager.CurrentSave.collections.events, Is.Empty);
        }

        [Test]
        public void CloudSyncOfflineAndMissingRemoteKeepOrUploadDurableLocal()
        {
            using var fixture = IsolatedGameManagerFixture.Create("cloud_boundaries");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.playerId = "durable-local";
            var logicalModifiedAt = new DateTimeOffset(
                2026,
                8,
                17,
                12,
                0,
                0,
                TimeSpan.Zero).ToString("O");
            fixture.Manager.CurrentTama.lastSavedAtIso = logicalModifiedAt;

            var offline = new FakeCloudProvider
            {
                AvailabilityValue = CloudProviderAvailability.Offline
            };
            var offlineResult = fixture.Manager.SynchronizeCloudSave(offline);

            Assert.That(offlineResult.Action, Is.EqualTo(CloudSyncAction.KeptLocalOffline));
            Assert.That(offline.DownloadCount, Is.Zero);
            Assert.That(offline.UploadCount, Is.Zero);
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("durable-local"));
            Assert.That(fixture.Manager.CurrentTama.lastSavedAtIso,
                Is.EqualTo(logicalModifiedAt),
                "Cloud comparison must not make the local copy artificially newer.");

            var missingRemote = new FakeCloudProvider();
            var uploadResult = fixture.Manager.SynchronizeCloudSave(missingRemote);

            Assert.That(uploadResult.Action, Is.EqualTo(CloudSyncAction.UploadedLocal));
            Assert.That(missingRemote.DownloadCount, Is.EqualTo(1));
            Assert.That(missingRemote.UploadCount, Is.EqualTo(1));
            Assert.That(missingRemote.UploadedPayload.IsValid(), Is.True);
            Assert.That(missingRemote.UploadedPayload.slotId,
                Is.EqualTo(CloudSaveSlotRules.PrimarySlotId));
            Assert.That(missingRemote.UploadedPayload.contentJson, Does.Contain("durable-local"));
        }

        [Test]
        public void NewerCloudDownloadRequiresPhraseThenAppliesAndReloads()
        {
            using var fixture = IsolatedGameManagerFixture.Create("cloud_download");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.playerId = "local-copy";
            fixture.Manager.SaveGame();
            var remoteSave = Clone(fixture.Manager.CurrentSave);
            remoteSave.playerId = "cloud-copy";
            remoteSave.economy.milkCoins = 987;
            var remotePayload = CloudSavePayload.Create(
                CloudSaveSlotRules.PrimarySlotId,
                JsonUtility.ToJson(remoteSave, true),
                long.MaxValue,
                DateTimeOffset.UtcNow.AddMinutes(5));
            var provider = new FakeCloudProvider { RemotePayload = remotePayload };

            var result = fixture.Manager.SynchronizeCloudSave(provider);

            Assert.That(result.Action, Is.EqualTo(CloudSyncAction.DownloadedRemote));
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("local-copy"));
            Assert.That(
                fixture.Manager.TryApplyCloudSave(result, "use cloud").Succeeded,
                Is.False);
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("local-copy"));

            var eventCount = 0;
            fixture.Manager.SaveDataReplaced += () => eventCount += 1;
            Assert.That(
                fixture.Manager.TryApplyCloudSave(
                    result,
                    GameManager.CloudSaveApplyConfirmationPhrase).Succeeded,
                Is.True);
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("cloud-copy"));
            Assert.That(fixture.Manager.CurrentSave.economy.milkCoins, Is.EqualTo(987));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("cloud-copy"));
            Assert.That(fixture.Manager.CurrentSave.economy.milkCoins, Is.EqualTo(987));
        }

        [Test]
        public void ExplicitConflictChoiceCanApplyRemoteAndReload()
        {
            using var fixture = IsolatedGameManagerFixture.Create("cloud_conflict");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.playerId = "local-conflict";
            fixture.Manager.SaveGame();
            var fixedModifiedAt = new DateTimeOffset(
                2026,
                8,
                18,
                0,
                0,
                0,
                TimeSpan.Zero);
            fixture.Manager.CurrentTama.lastSavedAtIso = fixedModifiedAt.ToString("O");
            fixture.Manager.CurrentSave.EnsureRuntimeDefaults();
            var remoteSave = Clone(fixture.Manager.CurrentSave);
            remoteSave.playerId = "chosen-cloud-conflict";
            var remotePayload = CloudSavePayload.Create(
                CloudSaveSlotRules.PrimarySlotId,
                JsonUtility.ToJson(remoteSave, true),
                fixedModifiedAt.UtcDateTime.Ticks,
                fixedModifiedAt);
            var conflict = fixture.Manager.SynchronizeCloudSave(
                new FakeCloudProvider { RemotePayload = remotePayload });

            Assert.That(conflict.Action, Is.EqualTo(CloudSyncAction.ConflictNeedsResolution));

            Assert.That(
                fixture.Manager.TryApplyCloudSave(
                    conflict,
                    GameManager.CloudSaveApplyConfirmationPhrase).Succeeded,
                Is.True);
            Assert.That(fixture.Manager.CurrentSave.playerId,
                Is.EqualTo("chosen-cloud-conflict"));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();
            Assert.That(fixture.Manager.CurrentSave.playerId,
                Is.EqualTo("chosen-cloud-conflict"));
        }

        [Test]
        public void InvalidCloudPayloadNeverMutatesPrimaryBackupOrLiveSave()
        {
            using var fixture = IsolatedGameManagerFixture.Create("cloud_invalid");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.playerId = "protected-local";
            fixture.Manager.SaveGame();
            var primaryBefore = File.ReadAllText(fixture.SaveManager.SaveFilePath);
            var backupExistedBefore = File.Exists(fixture.SaveManager.BackupFilePath);
            var backupBefore = backupExistedBefore
                ? File.ReadAllText(fixture.SaveManager.BackupFilePath)
                : string.Empty;
            var liveBefore = fixture.Manager.CurrentSave;
            var eventCount = 0;
            fixture.Manager.SaveDataReplaced += () => eventCount += 1;

            var invalid = CloudSavePayload.Create(
                CloudSaveSlotRules.PrimarySlotId,
                "[1,2,3]",
                10L,
                DateTimeOffset.UtcNow);
            Assert.That(
                fixture.SaveManager.TryReplaceFromCloudPayload(invalid, out _),
                Is.False,
                "A correctly hashed value still needs a valid CheeseTama JSON object.");
            invalid.contentHash = "tampered";
            var download = new CloudSyncResult(
                CloudSyncAction.DownloadedRemote,
                invalid,
                invalid,
                string.Empty);

            Assert.That(
                fixture.SaveManager.TryReplaceFromCloudPayload(invalid, out _),
                Is.False);
            Assert.That(
                fixture.Manager.TryApplyCloudSave(
                    download,
                    GameManager.CloudSaveApplyConfirmationPhrase).Succeeded,
                Is.False);
            Assert.That(fixture.Manager.CurrentSave, Is.SameAs(liveBefore));
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("protected-local"));
            Assert.That(File.ReadAllText(fixture.SaveManager.SaveFilePath), Is.EqualTo(primaryBefore));
            Assert.That(File.Exists(fixture.SaveManager.BackupFilePath),
                Is.EqualTo(backupExistedBefore));
            if (backupExistedBefore)
            {
                Assert.That(File.ReadAllText(fixture.SaveManager.BackupFilePath),
                    Is.EqualTo(backupBefore));
            }

            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void CloudApplyRejectsResultWhenLocalChangedAfterComparison()
        {
            using var fixture = IsolatedGameManagerFixture.Create("cloud_stale_result");
            fixture.Manager.LoadOrCreateGame();
            fixture.Manager.CurrentSave.playerId = "local-before-sync";
            fixture.Manager.SaveGame();
            var remote = Clone(fixture.Manager.CurrentSave);
            remote.playerId = "remote-must-not-apply";
            var remotePayload = CloudSavePayload.Create(
                CloudSaveSlotRules.PrimarySlotId,
                JsonUtility.ToJson(remote, true),
                long.MaxValue,
                DateTimeOffset.UtcNow.AddMinutes(10));
            var result = fixture.Manager.SynchronizeCloudSave(
                new FakeCloudProvider { RemotePayload = remotePayload });
            Assert.That(result.Action, Is.EqualTo(CloudSyncAction.DownloadedRemote));

            fixture.Manager.CurrentSave.economy.milkCoins += 1;
            var fileBeforeApply = File.ReadAllText(fixture.SaveManager.SaveFilePath);
            var apply = fixture.Manager.TryApplyCloudSave(
                result,
                GameManager.CloudSaveApplyConfirmationPhrase);

            Assert.That(apply.Succeeded, Is.False);
            Assert.That(apply.Message, Does.Contain("다시 동기화"));
            Assert.That(fixture.Manager.CurrentSave.playerId, Is.EqualTo("local-before-sync"));
            Assert.That(File.ReadAllText(fixture.SaveManager.SaveFilePath), Is.EqualTo(fileBeforeApply));
        }

        private static void ConfigureAccountAndActiveCare(CheeseTamaSaveData save)
        {
            save.EnsureRuntimeDefaults();
            save.playerId = "preserved-player";
            save.settings.masterVolume = 0.35f;
            save.settings.highContrastUi = true;
            save.decorations.ownedThemeIds.Add(MilkroomThemeController.NightThemeId);
            save.milkroomThemeId = MilkroomThemeController.NightThemeId;
            save.economy.milkCoins = 4321;
            save.snackInventory.Add(new SnackInventorySaveEntry
            {
                snackId = "account_snack",
                quantity = 7
            });
            save.unlocks.starMilkUnlocked = true;
            save.collections.events.Add("account_collection");
            save.playMiniGames.highestBouncyJumpScore = 91;
            save.firstDayJourney = FirstDayJourneySaveData.CreateCompletedForLegacySave();
            save.npcVisits.relationships.Add(new NpcRelationshipSaveEntry
            {
                npcId = "account_npc",
                affinity = 8,
                storyStep = 2
            });

            save.cheeseTama.level = 22;
            save.cheeseTama.isHatched = true;
            save.cheeseTama.form = "soft_cheesetama";
            save.milkGrowth.Add(new MilkGrowthSaveEntry
            {
                milkId = "basic_milk",
                growthLevel = 4,
                growthPoints = 39
            });
            save.claimedMilkGrowthRewardKeys.Add("basic_milk:2");
            save.careHistory.totalCareActions = 88;
            var todayKey = DateTimeOffset.Now.ToString("yyyy-MM-dd");
            save.dailyCare.dateKey = todayKey;
            save.dailyCare.milkFeeds = 3;
            save.dailyCare.completedRoutineCount = 4;
            save.dailyCare.lastCompletedDateKey = todayKey;
            save.dailyCare.lastCompletedAtIso = DateTimeOffset.UtcNow.AddHours(-1).ToString("O");
            save.milkroomSession.totalSeconds = 1200;
            save.milkroomSession.highestClaimedSessionMinute = 10;
            save.milkroomSession.lastRewardAtIso =
                DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O");
            save.lateLevelGrowth.BeginLevel(31, 45);
            save.newGameSetup = NewGameSetupSaveData.CreateCompletedForLegacySave();

            save.randomEvents.history.Add(new RandomEventHistorySaveEntry
            {
                eventId = "care_memory",
                totalOccurrences = 2
            });
            save.randomEvents.pendingEvent.occurrenceId = "pending-care";
            save.randomEvents.pendingEvent.eventId = "care_memory";
            save.randomEvents.pendingEvent.title = "pending";
            save.randomEvents.pendingEvent.message = "pending";

            var now = DateTimeOffset.UtcNow;
            save.sleepSchedule.recoveryReceipts.Add(new SleepRecoveryReceiptSaveEntry
            {
                receiptKey = "preserved-sleep",
                claimedAtIso = now.AddDays(-1).ToString("O"),
                scheduledHours = 3,
                elapsedMinutes = 180
            });
            save.sleepSchedule.activeSession = new SleepSessionSaveData
            {
                receiptKey = "active-sleep",
                sleepStartedAtIso = now.AddHours(-1).ToString("O"),
                plannedWakeAtIso = now.AddHours(3).ToString("O"),
                scheduledHours = 4
            };

            save.npcRelationshipQuests.activeQuest.Set(
                "active-offer",
                "account_npc",
                "active-quest",
                now.AddMinutes(-5),
                now.AddHours(1),
                now.AddHours(2));
            save.npcRelationshipQuests.claimReceipts.Add(
                new NpcRelationshipQuestClaimReceiptSaveData
                {
                    claimReceiptId = "preserved-claim",
                    offerId = "completed-offer",
                    npcId = "account_npc",
                    questId = "completed-quest",
                    claimedAtIso = now.AddDays(-2).ToString("O")
                });
        }

        private static CheeseTamaSaveData Clone(CheeseTamaSaveData source)
        {
            return JsonUtility.FromJson<CheeseTamaSaveData>(JsonUtility.ToJson(source));
        }

        private static string[] FindCorruptRecoveryFiles(SaveManager saveManager)
        {
            var directoryPath = Path.GetDirectoryName(saveManager.SaveFilePath);
            return string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath)
                ? Array.Empty<string>()
                : Directory.GetFiles(
                    directoryPath,
                    Path.GetFileName(saveManager.SaveFilePath) + "*.corrupt.*");
        }

        private sealed class FakeCloudProvider : ICloudSaveProvider
        {
            public CloudProviderAvailability AvailabilityValue = CloudProviderAvailability.Available;
            public CloudSavePayload RemotePayload;
            public CloudSavePayload UploadedPayload;
            public int UploadCount;
            public int DownloadCount;

            public string ProviderName => "Integration Fake";
            public CloudProviderAvailability Availability => AvailabilityValue;

            public CloudTransferResult Upload(CloudSavePayload payload)
            {
                UploadCount += 1;
                UploadedPayload = payload;
                return CloudTransferResult.Success();
            }

            public CloudTransferResult Download(string slotId)
            {
                DownloadCount += 1;
                return RemotePayload == null
                    ? CloudTransferResult.NotFound()
                    : CloudTransferResult.Success(RemotePayload);
            }
        }

        private sealed class IsolatedGameManagerFixture : IDisposable
        {
            private readonly GameObject root;

            private IsolatedGameManagerFixture(
                GameObject root,
                SaveManager saveManager,
                GameManager manager)
            {
                this.root = root;
                SaveManager = saveManager;
                Manager = manager;
            }

            public SaveManager SaveManager { get; }
            public GameManager Manager { get; private set; }

            public static IsolatedGameManagerFixture Create(string label)
            {
                var root = new GameObject($"{label} Reset Cloud Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                SetPrivateField(
                    saveManager,
                    "saveFileName",
                    $"cheesetama_reset_cloud_test_{label}_{Guid.NewGuid():N}.json");
                var manager = root.AddComponent<GameManager>();
                SetPrivateField(manager, "saveManager", saveManager);
                return new IsolatedGameManagerFixture(root, saveManager, manager);
            }

            public void RecreateManager()
            {
                UnityEngine.Object.DestroyImmediate(Manager);
                Manager = root.AddComponent<GameManager>();
                SetPrivateField(Manager, "saveManager", SaveManager);
            }

            public void Dispose()
            {
                SaveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(root);
            }

            private static void SetPrivateField(object target, string fieldName, object value)
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
