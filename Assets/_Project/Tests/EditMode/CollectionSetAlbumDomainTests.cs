using System.Collections.Generic;
using CheeseTama.Collections;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class CollectionSetAlbumDomainTests
    {
        [Test]
        public void InitialPublicSnapshotShowsOnlyThreeSetsAndNoHiddenMetadata()
        {
            var snapshot = new CollectionSetAlbumSystem().BuildPublicProgressSnapshot(
                new CollectionSetAlbumSaveData(),
                new CollectionSaveData());

            Assert.That(snapshot.Sets.Count, Is.EqualTo(3));
            Assert.That(
                snapshot.Find(CollectionSetAlbumSystem.MilkFirstStepsSetId),
                Is.Not.Null);
            Assert.That(
                snapshot.Find(CollectionSetAlbumSystem.DeepFlavorTrailSetId),
                Is.Not.Null);
            Assert.That(
                snapshot.Find(CollectionSetAlbumSystem.MilkroomDailyMomentsSetId),
                Is.Not.Null);
            Assert.That(
                snapshot.Find(CollectionSetAlbumSystem.NormalEvolutionCircleSetId),
                Is.Null);
            Assert.That(
                snapshot.Find(CollectionSetAlbumSystem.MainMilkMasterySetId),
                Is.Null);

            var renderedText = string.Empty;
            foreach (var set in snapshot.Sets)
            {
                renderedText += set.SetId + set.DisplayName + set.Description;
                Assert.That(set.Records, Is.Not.Empty);
            }

            Assert.That(renderedText, Does.Not.Contain("여섯 갈래의 성장"));
            Assert.That(renderedText, Does.Not.Contain("일곱 우유의 기억"));
            Assert.That(renderedText, Does.Not.Contain("album_normal_evolution_circle"));
            Assert.That(renderedText, Does.Not.Contain("album_main_milk_mastery"));
        }

        [Test]
        public void ExistingPublicRecordsBackfillProgressAndRevealBothHiddenSets()
        {
            var state = new CollectionSetAlbumSaveData();
            var collections = CreateCompletePublicCollections();
            collections.milk.Add(" " + MilkCatalog.BasicMilkId + " ");
            collections.milk.Add(null);
            collections.evolution.Add(EvolutionSystem.CreamEvolutionId);
            collections.events.Add("future_public_event");
            var system = new CollectionSetAlbumSystem();

            var firstRevealCount = system.RecalculateProgress(state, collections);
            var secondRevealCount = system.RecalculateProgress(state, collections);
            var snapshot = system.BuildPublicProgressSnapshot(state, collections);

            Assert.That(firstRevealCount, Is.EqualTo(2));
            Assert.That(secondRevealCount, Is.Zero);
            Assert.That(snapshot.Sets.Count, Is.EqualTo(5));
            Assert.That(
                state.IsHiddenSetRevealed(
                    CollectionSetAlbumSystem.NormalEvolutionCircleSetId),
                Is.True);
            Assert.That(
                state.IsHiddenSetRevealed(CollectionSetAlbumSystem.MainMilkMasterySetId),
                Is.True);

            var firstMilkSet = snapshot.Find(CollectionSetAlbumSystem.MilkFirstStepsSetId);
            var evolutionSet = snapshot.Find(
                CollectionSetAlbumSystem.NormalEvolutionCircleSetId);
            var masterySet = snapshot.Find(CollectionSetAlbumSystem.MainMilkMasterySetId);
            Assert.That(firstMilkSet.DiscoveredCount, Is.EqualTo(3));
            Assert.That(firstMilkSet.RequiredCount, Is.EqualTo(3));
            Assert.That(firstMilkSet.Complete, Is.True);
            Assert.That(evolutionSet.DiscoveredCount, Is.EqualTo(6));
            Assert.That(evolutionSet.Complete, Is.True);
            Assert.That(masterySet.DiscoveredCount, Is.EqualTo(7));
            Assert.That(masterySet.Complete, Is.True);

            foreach (var set in snapshot.Sets)
            {
                foreach (var record in set.Records)
                {
                    Assert.That(
                        record.Category,
                        Is.EqualTo(CollectionSetAlbumRecordCategory.Milk)
                            .Or.EqualTo(CollectionSetAlbumRecordCategory.Evolution)
                            .Or.EqualTo(CollectionSetAlbumRecordCategory.Event));
                }
            }
        }

        [Test]
        public void HiddenCollectionEntriesNeverAdvanceOrRevealPublicRecordSets()
        {
            var collections = new CollectionSaveData
            {
                milk = null,
                evolution = null,
                events = null,
                hiddenUnlockedOnly = new List<HiddenCollectionSaveEntry>
                {
                    new HiddenCollectionSaveEntry { id = MilkCatalog.BasicMilkId },
                    new HiddenCollectionSaveEntry { id = EvolutionSystem.CreamEvolutionId },
                    new HiddenCollectionSaveEntry { id = "daily_routine_complete" }
                }
            };
            var state = new CollectionSetAlbumSaveData();
            var snapshot = new CollectionSetAlbumSystem().BuildPublicProgressSnapshot(
                state,
                collections);

            Assert.That(snapshot.Sets.Count, Is.EqualTo(3));
            Assert.That(
                snapshot.Find(CollectionSetAlbumSystem.MilkFirstStepsSetId).DiscoveredCount,
                Is.Zero);
            Assert.That(
                snapshot.Find(CollectionSetAlbumSystem.NormalEvolutionCircleSetId),
                Is.Null);
            Assert.That(state.revealedHiddenSetIds, Is.Empty);
        }

        [Test]
        public void RewardClaimUsesBothSetAndReceiptIdempotency()
        {
            var collections = new CollectionSaveData();
            collections.milk.Add(MilkCatalog.BasicMilkId);
            collections.milk.Add(MilkCatalog.WarmMilkId);
            collections.milk.Add(MilkCatalog.ColdMilkId);
            var state = new CollectionSetAlbumSaveData();
            var system = new CollectionSetAlbumSystem();

            var applied = system.TryClaimReward(
                state,
                collections,
                CollectionSetAlbumSystem.MilkFirstStepsSetId,
                " album-claim-001 ");
            var stateAfterApplied = JsonUtility.ToJson(state);
            var duplicateReceipt = system.TryClaimReward(
                state,
                collections,
                CollectionSetAlbumSystem.DeepFlavorTrailSetId,
                "album-claim-001");
            var alreadyClaimed = system.TryClaimReward(
                state,
                collections,
                CollectionSetAlbumSystem.MilkFirstStepsSetId,
                "album-claim-002");

            Assert.That(applied.Status, Is.EqualTo(CollectionSetAlbumClaimStatus.Applied));
            Assert.That(applied.Reward.Coins, Is.EqualTo(40));
            Assert.That(applied.Reward.MilkDrops, Is.EqualTo(2));
            Assert.That(applied.Reward.CollectionFragments, Is.EqualTo(1));
            Assert.That(
                duplicateReceipt.Status,
                Is.EqualTo(CollectionSetAlbumClaimStatus.AlreadyApplied));
            Assert.That(duplicateReceipt.Reward.Coins, Is.Zero);
            Assert.That(
                alreadyClaimed.Status,
                Is.EqualTo(CollectionSetAlbumClaimStatus.AlreadyClaimed));
            Assert.That(state.HasAppliedClaimReceipt("album-claim-002"), Is.False);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(stateAfterApplied));

            var snapshot = system.BuildPublicProgressSnapshot(state, collections);
            var progress = snapshot.Find(CollectionSetAlbumSystem.MilkFirstStepsSetId);
            Assert.That(progress.RewardClaimed, Is.True);
            Assert.That(progress.CanClaimReward, Is.False);
        }

        [Test]
        public void IncompleteHiddenUnknownAndInvalidClaimsDoNotMutateOrLeak()
        {
            var state = new CollectionSetAlbumSaveData();
            var collections = new CollectionSaveData();
            var system = new CollectionSetAlbumSystem();
            var before = JsonUtility.ToJson(state);

            var hidden = system.TryClaimReward(
                state,
                collections,
                CollectionSetAlbumSystem.NormalEvolutionCircleSetId,
                "hidden-claim");
            var unknown = system.TryClaimReward(
                state,
                collections,
                "future_album_set",
                "unknown-claim");
            var invalidReceipt = system.TryClaimReward(
                state,
                collections,
                CollectionSetAlbumSystem.MilkFirstStepsSetId,
                " ");
            var incomplete = system.TryClaimReward(
                state,
                collections,
                CollectionSetAlbumSystem.MilkFirstStepsSetId,
                "incomplete-claim");

            Assert.That(hidden.Status, Is.EqualTo(CollectionSetAlbumClaimStatus.NotVisible));
            Assert.That(hidden.SetId, Is.Empty);
            Assert.That(hidden.Reward.Coins, Is.Zero);
            Assert.That(unknown.Status, Is.EqualTo(CollectionSetAlbumClaimStatus.UnknownSet));
            Assert.That(unknown.SetId, Is.Empty);
            Assert.That(invalidReceipt.Status, Is.EqualTo(CollectionSetAlbumClaimStatus.InvalidReceipt));
            Assert.That(incomplete.Status, Is.EqualTo(CollectionSetAlbumClaimStatus.Incomplete));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(state.appliedClaimReceiptKeys, Is.Empty);
        }

        [Test]
        public void SaveDtoNormalizesNullDuplicateAndUnknownValuesWithoutDroppingFutureIds()
        {
            var state = new CollectionSetAlbumSaveData
            {
                schemaVersion = 0,
                revealedHiddenSetIds = new List<string>
                {
                    null,
                    " " + CollectionSetAlbumSystem.NormalEvolutionCircleSetId + " ",
                    CollectionSetAlbumSystem.NormalEvolutionCircleSetId,
                    "future_hidden_set"
                },
                claimedSetIds = new List<string>
                {
                    "future_claimed_set",
                    "future_claimed_set",
                    " "
                },
                appliedClaimReceiptKeys = new List<string>
                {
                    null,
                    " future-receipt ",
                    "future-receipt"
                }
            };

            Assert.That(state.EnsureRuntimeDefaults(), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(CollectionSetAlbumSaveData.CurrentSchemaVersion));
            Assert.That(state.revealedHiddenSetIds.Count, Is.EqualTo(2));
            Assert.That(state.IsHiddenSetRevealed("future_hidden_set"), Is.True);
            Assert.That(state.claimedSetIds, Is.EqualTo(new[] { "future_claimed_set" }));
            Assert.That(state.appliedClaimReceiptKeys, Is.EqualTo(new[] { "future-receipt" }));
            Assert.That(state.EnsureRuntimeDefaults(), Is.False);

            var restored = JsonUtility.FromJson<CollectionSetAlbumSaveData>(
                JsonUtility.ToJson(state));
            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
            Assert.That(restored.IsHiddenSetRevealed("future_hidden_set"), Is.True);
            Assert.That(restored.IsRewardClaimed("future_claimed_set"), Is.True);
            Assert.That(restored.HasAppliedClaimReceipt("future-receipt"), Is.True);
        }

        [Test]
        public void PublicProgressNormalizesWhitespaceDuplicatesAndIgnoresUnknownRecords()
        {
            var collections = new CollectionSaveData
            {
                milk = new List<string>
                {
                    null,
                    " ",
                    " " + MilkCatalog.BasicMilkId + " ",
                    MilkCatalog.BasicMilkId,
                    "future_public_milk"
                },
                evolution = new List<string> { "future_evolution" },
                events = new List<string> { "future_event" }
            };

            var snapshot = new CollectionSetAlbumSystem().BuildPublicProgressSnapshot(
                new CollectionSetAlbumSaveData(),
                collections);
            var firstSet = snapshot.Find(CollectionSetAlbumSystem.MilkFirstStepsSetId);

            Assert.That(firstSet.DiscoveredCount, Is.EqualTo(1));
            Assert.That(firstSet.RequiredCount, Is.EqualTo(3));
            Assert.That(firstSet.Complete, Is.False);
            Assert.That(firstSet.Records.Count, Is.EqualTo(3));
            foreach (var record in firstSet.Records)
            {
                Assert.That(record.RecordId, Does.Not.StartWith("future_"));
            }
        }

        private static CollectionSaveData CreateCompletePublicCollections()
        {
            return new CollectionSaveData
            {
                milk = new List<string>
                {
                    MilkCatalog.BasicMilkId,
                    MilkCatalog.WarmMilkId,
                    MilkCatalog.ColdMilkId,
                    MilkCatalog.NuttyMilkId,
                    MilkCatalog.RichMilkId,
                    MilkCatalog.FermentedMilkId,
                    MilkCatalog.CoffeeMilkId
                },
                evolution = new List<string>
                {
                    EvolutionSystem.CreamEvolutionId,
                    EvolutionSystem.CheddarEvolutionId,
                    EvolutionSystem.RicottaEvolutionId,
                    EvolutionSystem.MozzarellaEvolutionId,
                    EvolutionSystem.BlueEvolutionId,
                    EvolutionSystem.CoffeeEvolutionId
                },
                events = new List<string>
                {
                    "daily_routine_complete",
                    "milk_drop_catch",
                    "bouncy_jump"
                }
            };
        }
    }
}
