using System;
using System.IO;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class NpcRelationshipEpisodeIntegrationTests
    {
        private static readonly DateTimeOffset FixedNow =
            new DateTimeOffset(2026, 8, 18, 16, 10, 0, TimeSpan.FromHours(9));

        [Test]
        public void AppliedChoicePersistsJournalCollectionAndNextSnapshotAcrossReload()
        {
            using var fixture = IsolatedGameManagerFixture.Create("episode_apply_reload");
            var save = SaveManager.CreateDefaultSave();
            save.cheeseTama.name = "모짜";
            save.cheeseTama.form = "soft_cheesetama";
            save.npcVisits.relationships.Add(new NpcRelationshipSaveEntry
            {
                npcId = NpcVisitSystem.MilkyDoctorId,
                visits = 3,
                affinity = NpcRelationshipQuestSystem.FriendAffinityThreshold,
                storyStep = 1
            });
            fixture.WriteSave(save);
            fixture.Manager.LoadOrCreateGame();

            var journeyChanged = 0;
            var memoryChanged = 0;
            var completionNotified = 0;
            NpcRelationshipEpisodeChoiceResult notifiedResult = null;
            fixture.Manager.JourneyHubChanged += () => journeyChanged += 1;
            fixture.Manager.MemoryJournalChanged += () => memoryChanged += 1;
            fixture.Manager.NpcRelationshipEpisodeCompleted += result =>
            {
                completionNotified += 1;
                notifiedResult = result;
            };

            var available = fixture.Manager.GetNpcRelationshipEpisodeSnapshot(
                NpcVisitSystem.MilkyDoctorId);
            Assert.That(available.IsEligible, Is.True);
            Assert.That(available.Episode.Id, Is.EqualTo(NpcRelationshipEpisodeIds.DoctorFriend));
            Assert.That(fixture.Manager.GetNpcRelationshipEpisodeSnapshots().Count, Is.EqualTo(3));

            var choice = available.Episode.Choices[0];
            var result = fixture.Manager.TryApplyNpcRelationshipEpisodeChoice(
                FixedNow,
                available.Episode.Id,
                choice.Id,
                "episode-integration-doctor-friend");

            Assert.That(result.Applied, Is.True);
            Assert.That(result.CompletedAt, Is.EqualTo(FixedNow));
            Assert.That(result.RewardKeepsakeId,
                Is.EqualTo(NpcRelationshipKeepsakeIds.DoctorHealthNotebook));
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes
                .HasCompletedEpisode(NpcRelationshipEpisodeIds.DoctorFriend), Is.True);
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes
                .HasKeepsake(NpcRelationshipKeepsakeIds.DoctorHealthNotebook), Is.True);
            Assert.That(fixture.Manager.CurrentSave.collections.events,
                Does.Contain($"npc_episode_{NpcRelationshipEpisodeIds.DoctorFriend}"));
            Assert.That(fixture.Manager.CurrentSave.memoryJournal.entries, Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.CurrentSave.memoryJournal.entries[0].sourceId,
                Is.EqualTo(NpcRelationshipEpisodeIds.DoctorFriend));
            Assert.That(fixture.Manager.CurrentSave.memoryJournal.entries[0].occurrenceId,
                Is.EqualTo("episode-integration-doctor-friend"));
            Assert.That(fixture.Manager.CurrentSave.memoryJournal.entries[0].detailId,
                Is.EqualTo(choice.Id));
            Assert.That(fixture.Manager.CurrentSave.memoryJournal.entries[0].title,
                Is.EqualTo(result.MemoryTitle));
            Assert.That(fixture.Manager.CurrentSave.memoryJournal.entries[0].quote,
                Is.EqualTo(result.MemoryDetail));
            Assert.That(journeyChanged, Is.EqualTo(1));
            Assert.That(memoryChanged, Is.EqualTo(1));
            Assert.That(completionNotified, Is.EqualTo(1));
            Assert.That(notifiedResult, Is.SameAs(result));

            var next = fixture.Manager.GetNpcRelationshipEpisodeSnapshot(
                NpcVisitSystem.MilkyDoctorId);
            Assert.That(next.Episode.Id,
                Is.EqualTo(NpcRelationshipEpisodeIds.DoctorTrustedFriend));
            Assert.That(next.Status,
                Is.EqualTo(NpcRelationshipEpisodeSnapshotStatus.AffinityLocked));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes
                .HasCompletedEpisode(NpcRelationshipEpisodeIds.DoctorFriend), Is.True);
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes
                .HasReceipt("episode-integration-doctor-friend"), Is.True);
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes
                .HasKeepsake(NpcRelationshipKeepsakeIds.DoctorHealthNotebook), Is.True);
            Assert.That(fixture.Manager.CurrentSave.collections.events,
                Does.Contain($"npc_episode_{NpcRelationshipEpisodeIds.DoctorFriend}"));
            Assert.That(fixture.Manager.CurrentSave.memoryJournal.entries, Has.Count.EqualTo(1));
            Assert.That(fixture.Manager.GetNpcRelationshipEpisodeSnapshot(
                    NpcVisitSystem.MilkyDoctorId).Episode.Id,
                Is.EqualTo(NpcRelationshipEpisodeIds.DoctorTrustedFriend));
        }

        [Test]
        public void DuplicateReceiptDoesNotSaveOrRaiseIntegrationEvents()
        {
            using var fixture = IsolatedGameManagerFixture.Create("episode_duplicate");
            var save = SaveManager.CreateDefaultSave();
            save.npcVisits.relationships.Add(new NpcRelationshipSaveEntry
            {
                npcId = NpcVisitSystem.MilkCatId,
                visits = 4,
                affinity = 99,
                storyStep = 2
            });
            fixture.WriteSave(save);
            fixture.Manager.LoadOrCreateGame();

            var snapshot = fixture.Manager.GetNpcRelationshipEpisodeSnapshot(
                NpcVisitSystem.MilkCatId);
            var first = fixture.Manager.TryApplyNpcRelationshipEpisodeChoice(
                FixedNow,
                snapshot.Episode.Id,
                snapshot.Episode.Choices[0].Id,
                "episode-integration-duplicate");
            Assert.That(first.Applied, Is.True);

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes
                .HasReceipt("episode-integration-duplicate"), Is.True);

            var persistedBeforeDuplicate = File.ReadAllText(fixture.SaveManager.SaveFilePath);
            var affinityBeforeDuplicate = fixture.Manager.GetNpcRelationshipSnapshot(
                NpcVisitSystem.MilkCatId).Affinity;
            var memoryCountBeforeDuplicate = fixture.Manager.CurrentSave.memoryJournal.entries.Count;
            var collectionCountBeforeDuplicate = fixture.Manager.CurrentSave.collections.events.Count;
            var journeyChanged = 0;
            var memoryChanged = 0;
            var completionNotified = 0;
            fixture.Manager.JourneyHubChanged += () => journeyChanged += 1;
            fixture.Manager.MemoryJournalChanged += () => memoryChanged += 1;
            fixture.Manager.NpcRelationshipEpisodeCompleted += _ => completionNotified += 1;

            var duplicate = fixture.Manager.TryApplyNpcRelationshipEpisodeChoice(
                FixedNow.AddMinutes(1),
                NpcRelationshipEpisodeIds.CatTrustedFriend,
                new NpcRelationshipEpisodeSystem()
                    .Find(NpcRelationshipEpisodeIds.CatTrustedFriend).Choices[0].Id,
                "episode-integration-duplicate");

            Assert.That(duplicate.Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.DuplicateReceipt));
            Assert.That(fixture.Manager.GetNpcRelationshipSnapshot(
                NpcVisitSystem.MilkCatId).Affinity, Is.EqualTo(affinityBeforeDuplicate));
            Assert.That(fixture.Manager.CurrentSave.memoryJournal.entries,
                Has.Count.EqualTo(memoryCountBeforeDuplicate));
            Assert.That(fixture.Manager.CurrentSave.collections.events,
                Has.Count.EqualTo(collectionCountBeforeDuplicate));
            Assert.That(File.ReadAllText(fixture.SaveManager.SaveFilePath),
                Is.EqualTo(persistedBeforeDuplicate));
            Assert.That(journeyChanged, Is.Zero);
            Assert.That(memoryChanged, Is.Zero);
            Assert.That(completionNotified, Is.Zero);
        }

        [Test]
        public void LegacySaveWithoutEpisodeStateMigratesPersistsAndReloads()
        {
            using var fixture = IsolatedGameManagerFixture.Create("episode_legacy");
            fixture.WriteRawJson(
                "{\"version\":\"0.1.0\",\"playerId\":\"legacy_episode_player\"}");

            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.SaveManager.LastLoadMigratedData, Is.True);
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes, Is.Not.Null);
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes.schemaVersion,
                Is.EqualTo(NpcRelationshipEpisodeSaveData.CurrentSchemaVersion));
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes.completedEpisodeIds,
                Is.Empty);
            Assert.That(File.ReadAllText(fixture.SaveManager.SaveFilePath),
                Does.Contain("\"npcRelationshipEpisodes\""));

            fixture.RecreateManager();
            fixture.Manager.LoadOrCreateGame();

            Assert.That(fixture.Manager.CurrentSave.playerId,
                Is.EqualTo("legacy_episode_player"));
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes, Is.Not.Null);
            Assert.That(fixture.Manager.CurrentSave.npcRelationshipEpisodes.receipts, Is.Empty);
            Assert.That(fixture.Manager.GetNpcRelationshipEpisodeSnapshot(
                    NpcVisitSystem.FermentationFairyId).Status,
                Is.EqualTo(NpcRelationshipEpisodeSnapshotStatus.RelationshipNotStarted));
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
                var previousSaveFileNameOverride = SaveManager.PlayModeTestSaveFileNameOverride;
                var previousEnvironmentSaveFileName = System.Environment.GetEnvironmentVariable(
                    SaveManager.PlayModeTestSaveFileNameEnvironmentVariable);
                var root = new GameObject($"{label} Relationship Episode Integration Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                System.Environment.SetEnvironmentVariable(
                    SaveManager.PlayModeTestSaveFileNameEnvironmentVariable,
                    isolatedFileName);
                SaveManager.SetPlayModeTestSaveFileNameOverride(isolatedFileName);
                saveManager.SetIsolatedSaveFileNameForTests(isolatedFileName);
                Assert.That(Path.GetFileName(saveManager.SaveFilePath), Is.EqualTo(isolatedFileName));

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

            public void WriteSave(CheeseTamaSaveData save)
            {
                WriteRawJson(JsonUtility.ToJson(save, true));
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
