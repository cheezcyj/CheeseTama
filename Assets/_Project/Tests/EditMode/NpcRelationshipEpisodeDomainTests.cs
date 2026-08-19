using System;
using System.Collections.Generic;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class NpcRelationshipEpisodeDomainTests
    {
        private static readonly DateTimeOffset FixedNow =
            new DateTimeOffset(2026, 8, 18, 14, 30, 0, TimeSpan.FromHours(9));

        [Test]
        public void CatalogContainsFriendAndTrustedEpisodeWithTwoChoicesForEveryNpc()
        {
            var system = new NpcRelationshipEpisodeSystem();

            Assert.That(system.All.Count, Is.EqualTo(NpcRelationshipEpisodeSystem.EpisodeCount));
            AssertNpcEpisodes(system, NpcVisitSystem.MilkyDoctorId);
            AssertNpcEpisodes(system, NpcVisitSystem.FermentationFairyId);
            AssertNpcEpisodes(system, NpcVisitSystem.MilkCatId);

            foreach (var episode in system.All)
            {
                Assert.That(episode.Choices.Count, Is.EqualTo(NpcRelationshipEpisodeSystem.ChoicesPerEpisode));
                Assert.That(episode.Title, Is.Not.Empty);
                Assert.That(episode.Description, Is.Not.Empty);
                foreach (var choice in episode.Choices)
                {
                    Assert.That(choice.Id, Is.Not.Empty);
                    Assert.That(choice.Label, Is.Not.Empty);
                    Assert.That(choice.MemoryTitle, Is.Not.Empty);
                    Assert.That(choice.MemoryDetail, Is.Not.Empty);
                    Assert.That(
                        string.IsNullOrEmpty(choice.RewardDecorationId)
                        ^ string.IsNullOrEmpty(choice.RewardKeepsakeId),
                        Is.True,
                        $"{episode.Id}/{choice.Id} must expose exactly one integration reward id.");
                }
            }
        }

        [Test]
        public void NextSnapshotLocksAtBoundaryAndOrdersLegacyTrustedRelationshipThroughFriendFirst()
        {
            var system = new NpcRelationshipEpisodeSystem();
            var state = new NpcRelationshipEpisodeSaveData();
            var relationships = CreateRelationships(NpcVisitSystem.MilkyDoctorId, 24);

            var locked = system.BuildNextEpisodeSnapshot(
                state,
                relationships,
                NpcVisitSystem.MilkyDoctorId);
            Assert.That(locked.Status, Is.EqualTo(NpcRelationshipEpisodeSnapshotStatus.AffinityLocked));
            Assert.That(locked.Episode.Id, Is.EqualTo(NpcRelationshipEpisodeIds.DoctorFriend));
            Assert.That(locked.RequiredAffinity, Is.EqualTo(NpcRelationshipQuestSystem.FriendAffinityThreshold));

            relationships.relationships[0].affinity =
                NpcRelationshipQuestSystem.TrustedFriendAffinityThreshold;
            Assert.That(system.TryGetNextEligibleEpisode(
                state,
                relationships,
                NpcVisitSystem.MilkyDoctorId,
                out var friend), Is.True);
            Assert.That(friend.Episode.Id, Is.EqualTo(NpcRelationshipEpisodeIds.DoctorFriend));

            var tama = CreateTama();
            var applied = system.TryApplyChoice(
                state,
                relationships,
                tama,
                friend.Episode.Id,
                friend.Episode.Choices[0].Id,
                "doctor-friend-legacy",
                FixedNow);
            Assert.That(applied.Applied, Is.True);

            Assert.That(system.TryGetNextEligibleEpisode(
                state,
                relationships,
                NpcVisitSystem.MilkyDoctorId,
                out var trusted), Is.True);
            Assert.That(trusted.Episode.Id, Is.EqualTo(NpcRelationshipEpisodeIds.DoctorTrustedFriend));
            Assert.That(trusted.RequiredTier, Is.EqualTo(NpcRelationshipTier.TrustedFriend));
        }

        [Test]
        public void AppliedChoiceMutatesAffinityAndStatsAndReturnsKeepsakeAndMemoryPayload()
        {
            var system = new NpcRelationshipEpisodeSystem();
            var state = new NpcRelationshipEpisodeSaveData();
            var relationships = CreateRelationships(
                NpcVisitSystem.MilkyDoctorId,
                NpcRelationshipQuestSystem.FriendAffinityThreshold);
            var tama = CreateTama();
            tama.stats.health = 40;
            tama.stats.mood = 40;
            var episode = system.Find(NpcRelationshipEpisodeIds.DoctorFriend);
            var choice = episode.Choices[0];

            var result = system.TryApplyChoice(
                state,
                relationships,
                tama,
                episode.Id,
                choice.Id,
                "doctor-friend-choice",
                FixedNow);

            Assert.That(result.Applied, Is.True);
            Assert.That(result.CompletionId, Is.EqualTo(episode.Id));
            Assert.That(result.ReceiptId, Is.EqualTo("doctor-friend-choice"));
            Assert.That(result.AffinityBefore, Is.EqualTo(25));
            Assert.That(result.AffinityAfter, Is.EqualTo(28));
            Assert.That(result.AffinityGained, Is.EqualTo(3));
            Assert.That(result.StatEffect.health, Is.EqualTo(5));
            Assert.That(tama.stats.health, Is.EqualTo(45));
            Assert.That(tama.stats.mood, Is.EqualTo(42));
            Assert.That(relationships.relationships[0].affinity, Is.EqualTo(28));
            Assert.That(relationships.relationships[0].storyStep, Is.EqualTo(1));
            Assert.That(result.RewardDecorationId, Is.Empty);
            Assert.That(result.RewardKeepsakeId, Is.EqualTo(NpcRelationshipKeepsakeIds.DoctorHealthNotebook));
            Assert.That(result.MemoryTitle, Is.Not.Empty);
            Assert.That(result.MemoryDetail, Is.Not.Empty);
            Assert.That(result.MemorySourceId, Is.EqualTo(episode.Id));
            Assert.That(result.MemoryDetailId, Is.EqualTo(choice.Id));
            Assert.That(result.CompletedAt, Is.EqualTo(FixedNow));
            Assert.That(state.HasCompletedEpisode(episode.Id), Is.True);
            Assert.That(state.HasReceipt("doctor-friend-choice"), Is.True);
            Assert.That(state.HasKeepsake(NpcRelationshipKeepsakeIds.DoctorHealthNotebook), Is.True);
            Assert.That(state.receipts[0].completedAtIso, Is.EqualTo(FixedNow.ToString("O")));
        }

        [Test]
        public void AllSixEpisodesApplyOnceInCatalogOrder()
        {
            var system = new NpcRelationshipEpisodeSystem();
            var state = new NpcRelationshipEpisodeSaveData();
            var relationships = new NpcVisitSaveData
            {
                relationships = new List<NpcRelationshipSaveEntry>
                {
                    new NpcRelationshipSaveEntry
                    {
                        npcId = NpcVisitSystem.MilkyDoctorId,
                        affinity = 99
                    },
                    new NpcRelationshipSaveEntry
                    {
                        npcId = NpcVisitSystem.FermentationFairyId,
                        affinity = 99
                    },
                    new NpcRelationshipSaveEntry
                    {
                        npcId = NpcVisitSystem.MilkCatId,
                        affinity = 99
                    }
                }
            };
            var tama = CreateTama();

            for (var index = 0; index < system.All.Count; index += 1)
            {
                var episode = system.All[index];
                var choice = episode.Choices[index % NpcRelationshipEpisodeSystem.ChoicesPerEpisode];
                var result = system.TryApplyChoice(
                    state,
                    relationships,
                    tama,
                    episode.Id,
                    choice.Id,
                    $"all-episodes-{index}",
                    FixedNow.AddMinutes(index));
                Assert.That(result.Applied, Is.True, episode.Id);
                Assert.That(result.RewardKeepsakeId, Is.Not.Empty, episode.Id);
            }

            Assert.That(state.completedEpisodeIds.Count, Is.EqualTo(6));
            Assert.That(state.receipts.Count, Is.EqualTo(6));
            Assert.That(state.keepsakeIds.Count, Is.EqualTo(6));
            foreach (var snapshot in system.BuildNextEpisodeSnapshots(state, relationships))
            {
                Assert.That(snapshot.Status, Is.EqualTo(NpcRelationshipEpisodeSnapshotStatus.AllCompleted));
                Assert.That(snapshot.HasEpisode, Is.False);
            }
        }

        [Test]
        public void DuplicateReceiptAndCompletedEpisodeNeverApplyAgain()
        {
            var system = new NpcRelationshipEpisodeSystem();
            var state = new NpcRelationshipEpisodeSaveData();
            var relationships = CreateRelationships(NpcVisitSystem.MilkCatId, 99);
            var tama = CreateTama();
            var friend = system.Find(NpcRelationshipEpisodeIds.CatFriend);
            var trusted = system.Find(NpcRelationshipEpisodeIds.CatTrustedFriend);

            var first = system.TryApplyChoice(
                state,
                relationships,
                tama,
                friend.Id,
                friend.Choices[0].Id,
                "cat-once",
                FixedNow);
            Assert.That(first.Applied, Is.True);
            var affinityAfter = relationships.relationships[0].affinity;
            var moodAfter = tama.stats.mood;

            var duplicateReceipt = system.TryApplyChoice(
                state,
                relationships,
                tama,
                trusted.Id,
                trusted.Choices[0].Id,
                "cat-once",
                FixedNow.AddMinutes(1));
            var duplicateEpisode = system.TryApplyChoice(
                state,
                relationships,
                tama,
                friend.Id,
                friend.Choices[1].Id,
                "cat-twice",
                FixedNow.AddMinutes(2));

            Assert.That(duplicateReceipt.Status, Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.DuplicateReceipt));
            Assert.That(duplicateEpisode.Status, Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.AlreadyCompleted));
            Assert.That(relationships.relationships[0].affinity, Is.EqualTo(affinityAfter));
            Assert.That(tama.stats.mood, Is.EqualTo(moodAfter));
            Assert.That(state.receipts.Count, Is.EqualTo(1));
        }

        [Test]
        public void NullUnknownAndInvalidInputsFailWithoutMutation()
        {
            var system = new NpcRelationshipEpisodeSystem();
            var state = new NpcRelationshipEpisodeSaveData();
            var relationships = CreateRelationships(NpcVisitSystem.FermentationFairyId, 99);
            var tama = CreateTama();
            var episode = system.Find(NpcRelationshipEpisodeIds.FairyFriend);
            var initialAffinity = relationships.relationships[0].affinity;
            var initialMood = tama.stats.mood;

            Assert.That(system.TryApplyChoice(
                null, relationships, tama, episode.Id, episode.Choices[0].Id, "missing-state", FixedNow).Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.MissingState));
            Assert.That(system.TryApplyChoice(
                state, null, tama, episode.Id, episode.Choices[0].Id, "missing-rel", FixedNow).Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.MissingRelationshipState));
            Assert.That(system.TryApplyChoice(
                state, relationships, null, episode.Id, episode.Choices[0].Id, "missing-tama", FixedNow).Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.MissingTama));
            Assert.That(system.TryApplyChoice(
                state, relationships, tama, episode.Id, episode.Choices[0].Id, " ", FixedNow).Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.InvalidReceiptId));
            Assert.That(system.TryApplyChoice(
                state, relationships, tama, episode.Id, episode.Choices[0].Id, "invalid-time", default).Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.InvalidCompletionTime));
            Assert.That(system.TryApplyChoice(
                state, relationships, tama, "future_episode", "future_choice", "unknown-episode", FixedNow).Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.UnknownEpisode));
            Assert.That(system.TryApplyChoice(
                state, relationships, tama, episode.Id, "future_choice", "unknown-choice", FixedNow).Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.UnknownChoice));
            Assert.That(
                system.BuildNextEpisodeSnapshot(state, relationships, "future_npc").Status,
                Is.EqualTo(NpcRelationshipEpisodeSnapshotStatus.UnknownNpc));

            Assert.That(relationships.relationships[0].affinity, Is.EqualTo(initialAffinity));
            Assert.That(tama.stats.mood, Is.EqualTo(initialMood));
            Assert.That(state.completedEpisodeIds, Is.Empty);
            Assert.That(state.receipts, Is.Empty);
            Assert.That(state.keepsakeIds, Is.Empty);
        }

        [Test]
        public void EarlierTrustedEpisodeAndLowAffinityAreRejected()
        {
            var system = new NpcRelationshipEpisodeSystem();
            var state = new NpcRelationshipEpisodeSaveData();
            var relationships = CreateRelationships(NpcVisitSystem.MilkyDoctorId, 99);
            var tama = CreateTama();
            var trusted = system.Find(NpcRelationshipEpisodeIds.DoctorTrustedFriend);

            var outOfOrder = system.TryApplyChoice(
                state,
                relationships,
                tama,
                trusted.Id,
                trusted.Choices[0].Id,
                "trusted-too-early",
                FixedNow);
            Assert.That(
                outOfOrder.Status,
                Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.EarlierEpisodeIncomplete));

            relationships.relationships[0].affinity =
                NpcRelationshipQuestSystem.FriendAffinityThreshold - 1;
            var friend = system.Find(NpcRelationshipEpisodeIds.DoctorFriend);
            var locked = system.TryApplyChoice(
                state,
                relationships,
                tama,
                friend.Id,
                friend.Choices[0].Id,
                "friend-locked",
                FixedNow);
            Assert.That(locked.Status, Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.AffinityLocked));
            Assert.That(state.receipts, Is.Empty);
        }

        [Test]
        public void LegacyNullsAndUnknownForwardDataRepairAndRoundTripJson()
        {
            var state = new NpcRelationshipEpisodeSaveData
            {
                schemaVersion = 0,
                completedEpisodeIds = new List<string>
                {
                    null,
                    " future_episode ",
                    "future_episode"
                },
                keepsakeIds = new List<string>
                {
                    " future_keepsake ",
                    "future_keepsake"
                },
                receipts = new List<NpcRelationshipEpisodeReceiptSaveData>
                {
                    null,
                    new NpcRelationshipEpisodeReceiptSaveData
                    {
                        receiptId = "broken",
                        episodeId = "broken_episode",
                        npcId = "future_npc",
                        choiceId = "future_choice",
                        completedAtIso = "not-a-date"
                    },
                    new NpcRelationshipEpisodeReceiptSaveData
                    {
                        receiptId = " future_receipt ",
                        episodeId = " future_episode_two ",
                        npcId = " future_npc ",
                        choiceId = " future_choice ",
                        completedAtIso = FixedNow.ToString("O")
                    }
                }
            };

            Assert.That(state.EnsureRuntimeDefaults(), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(NpcRelationshipEpisodeSaveData.CurrentSchemaVersion));
            Assert.That(state.completedEpisodeIds, Is.EquivalentTo(new[]
            {
                "future_episode",
                "future_episode_two"
            }));
            Assert.That(state.keepsakeIds, Is.EqualTo(new[] { "future_keepsake" }));
            Assert.That(state.receipts.Count, Is.EqualTo(1));
            Assert.That(state.receipts[0].receiptId, Is.EqualTo("future_receipt"));
            Assert.That(state.receipts[0].episodeId, Is.EqualTo("future_episode_two"));

            var json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<NpcRelationshipEpisodeSaveData>(json);
            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
            Assert.That(restored.HasCompletedEpisode("future_episode"), Is.True);
            Assert.That(restored.HasCompletedEpisode("future_episode_two"), Is.True);
            Assert.That(restored.HasReceipt("future_receipt"), Is.True);
            Assert.That(restored.HasKeepsake("future_keepsake"), Is.True);
        }

        [Test]
        public void AppliedStateRoundTripsWithoutChangingEligibilityOrReceiptSafety()
        {
            var system = new NpcRelationshipEpisodeSystem();
            var state = new NpcRelationshipEpisodeSaveData();
            var relationships = CreateRelationships(NpcVisitSystem.MilkCatId, 99);
            var tama = CreateTama();
            var episode = system.Find(NpcRelationshipEpisodeIds.CatFriend);
            var result = system.TryApplyChoice(
                state,
                relationships,
                tama,
                episode.Id,
                episode.Choices[1].Id,
                "cat-roundtrip",
                FixedNow);
            Assert.That(result.Applied, Is.True);

            var restored = JsonUtility.FromJson<NpcRelationshipEpisodeSaveData>(
                JsonUtility.ToJson(state));
            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
            Assert.That(restored.HasCompletedEpisode(episode.Id), Is.True);
            Assert.That(restored.HasReceipt("cat-roundtrip"), Is.True);
            Assert.That(restored.HasKeepsake(NpcRelationshipKeepsakeIds.CatPawMap), Is.True);
            Assert.That(
                system.BuildNextEpisodeSnapshot(
                    restored,
                    relationships,
                    NpcVisitSystem.MilkCatId).Episode.Id,
                Is.EqualTo(NpcRelationshipEpisodeIds.CatTrustedFriend));

            var duplicate = system.TryApplyChoice(
                restored,
                relationships,
                tama,
                episode.Id,
                episode.Choices[0].Id,
                "cat-roundtrip-new-receipt",
                FixedNow.AddMinutes(1));
            Assert.That(duplicate.Status, Is.EqualTo(NpcRelationshipEpisodeChoiceStatus.AlreadyCompleted));
        }

        private static void AssertNpcEpisodes(NpcRelationshipEpisodeSystem system, string npcId)
        {
            var friendCount = 0;
            var trustedCount = 0;
            foreach (var episode in system.All)
            {
                if (!string.Equals(episode.NpcId, npcId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (episode.RequiredTier == NpcRelationshipTier.Friend)
                {
                    friendCount += 1;
                }
                else if (episode.RequiredTier == NpcRelationshipTier.TrustedFriend)
                {
                    trustedCount += 1;
                }
            }

            Assert.That(friendCount, Is.EqualTo(1), npcId);
            Assert.That(trustedCount, Is.EqualTo(1), npcId);
        }

        private static NpcVisitSaveData CreateRelationships(string npcId, int affinity)
        {
            return new NpcVisitSaveData
            {
                relationships = new List<NpcRelationshipSaveEntry>
                {
                    new NpcRelationshipSaveEntry
                    {
                        npcId = npcId,
                        affinity = affinity
                    }
                }
            };
        }

        private static CheeseTamaModel CreateTama()
        {
            var tama = new CheeseTamaModel();
            tama.EnsureRuntimeDefaults();
            return tama;
        }
    }
}
