using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Gameplay.Weekly;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class RelationshipQuestWeeklyJourneyTests
    {
        private static readonly DateTimeOffset MondayNoon =
            new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.FromHours(9));

        [Test]
        public void NpcQuestCatalogHasTwoPublicQuestsForEachVisitorAndExactAffinityTiers()
        {
            var system = new NpcRelationshipQuestSystem();
            Assert.That(system.All.Count, Is.EqualTo(6));
            Assert.That(CountQuests(system, NpcVisitSystem.MilkyDoctorId), Is.EqualTo(2));
            Assert.That(CountQuests(system, NpcVisitSystem.FermentationFairyId), Is.EqualTo(2));
            Assert.That(CountQuests(system, NpcVisitSystem.MilkCatId), Is.EqualTo(2));
            foreach (var quest in system.All)
            {
                Assert.That(quest.Id, Is.Not.Empty);
                Assert.That(quest.Title, Is.Not.Empty);
                Assert.That(quest.Description, Is.Not.Empty);
            }

            Assert.That(system.GetEligibleQuests(NpcVisitSystem.MilkyDoctorId, 0).Count, Is.EqualTo(1));
            Assert.That(
                system.GetEligibleQuests(
                    NpcVisitSystem.MilkyDoctorId,
                    NpcRelationshipQuestSystem.FamiliarAffinityThreshold).Count,
                Is.EqualTo(2));
            Assert.That(NpcRelationshipQuestSystem.ResolveTier(9), Is.EqualTo(NpcRelationshipTier.NewFace));
            Assert.That(NpcRelationshipQuestSystem.ResolveTier(10), Is.EqualTo(NpcRelationshipTier.Familiar));
            Assert.That(NpcRelationshipQuestSystem.ResolveTier(24), Is.EqualTo(NpcRelationshipTier.Familiar));
            Assert.That(NpcRelationshipQuestSystem.ResolveTier(25), Is.EqualTo(NpcRelationshipTier.Friend));
            Assert.That(NpcRelationshipQuestSystem.ResolveTier(49), Is.EqualTo(NpcRelationshipTier.Friend));
            Assert.That(NpcRelationshipQuestSystem.ResolveTier(50), Is.EqualTo(NpcRelationshipTier.TrustedFriend));
        }

        [Test]
        public void NpcQuestActivationUsesOneSlotAndChecksRelationshipGate()
        {
            var system = new NpcRelationshipQuestSystem();
            var state = new NpcRelationshipQuestSaveData();
            var relationships = new NpcVisitSaveData();

            var locked = system.TryActivate(
                state,
                relationships,
                NpcVisitSystem.MilkyDoctorId,
                "doctor_care_notes",
                "offer-locked",
                MondayNoon);
            Assert.That(locked.Status, Is.EqualTo(NpcQuestActivationStatus.RelationshipLocked));
            Assert.That(state.activeQuest.HasValue, Is.False);

            relationships.relationships.Add(new NpcRelationshipSaveEntry
            {
                npcId = NpcVisitSystem.MilkyDoctorId,
                affinity = 10
            });
            var activated = system.TryActivate(
                state,
                relationships,
                NpcVisitSystem.MilkyDoctorId,
                "doctor_care_notes",
                "offer-doctor-1",
                MondayNoon);
            Assert.That(activated.Applied, Is.True);
            Assert.That(activated.ExpiresAt, Is.EqualTo(MondayNoon.AddDays(3)));
            Assert.That(activated.GraceEndsAt, Is.EqualTo(MondayNoon.AddDays(5)));

            var second = system.TryActivate(
                state,
                relationships,
                NpcVisitSystem.MilkCatId,
                "cat_cold_pudding",
                "offer-cat-1",
                MondayNoon);
            Assert.That(second.Status, Is.EqualTo(NpcQuestActivationStatus.AlreadyActive));
            Assert.That(state.activeQuest.offerId, Is.EqualTo("offer-doctor-1"));
        }

        [Test]
        public void NpcQuestWindowHasThreeDaysPlusTwoDayGraceWithExactBoundaries()
        {
            var system = new NpcRelationshipQuestSystem();
            var state = new NpcRelationshipQuestSaveData();
            var relationships = new NpcVisitSaveData();
            system.TryActivate(
                state,
                relationships,
                NpcVisitSystem.MilkCatId,
                "cat_cold_pudding",
                "offer-window",
                MondayNoon);

            Assert.That(
                system.ObserveActive(state, MondayNoon.AddDays(3)).Status,
                Is.EqualTo(NpcQuestWindowStatus.Active));
            Assert.That(
                system.ObserveActive(state, MondayNoon.AddDays(3).AddTicks(1)).Status,
                Is.EqualTo(NpcQuestWindowStatus.Grace));
            Assert.That(
                system.ObserveActive(state, MondayNoon.AddDays(5)).Status,
                Is.EqualTo(NpcQuestWindowStatus.Grace));
            Assert.That(
                system.ObserveActive(state, MondayNoon.AddDays(5).AddTicks(1)).Status,
                Is.EqualTo(NpcQuestWindowStatus.Expired));
            Assert.That(state.activeQuest.terminalExpired, Is.True);
            Assert.That(
                system.ObserveActive(state, MondayNoon.AddMinutes(-1)).Status,
                Is.EqualTo(NpcQuestWindowStatus.Expired),
                "A quest that reached terminal expiry must not revive after clock rollback.");

            var restored = JsonUtility.FromJson<NpcRelationshipQuestSaveData>(
                JsonUtility.ToJson(state));
            Assert.That(restored.activeQuest.terminalExpired, Is.True);
            Assert.That(
                system.ObserveActive(restored, MondayNoon.AddDays(1)).Status,
                Is.EqualTo(NpcQuestWindowStatus.Expired));
        }

        [Test]
        public void NpcQuestDeliveryValidatesBeforeAtomicConsumptionAndReward()
        {
            var system = new NpcRelationshipQuestSystem();
            var state = new NpcRelationshipQuestSaveData();
            var relationships = new NpcVisitSaveData();
            var economy = new EconomySaveData { milkCoins = 7, milkDrops = 2, collectionFragments = 1 };
            var inventory = new List<SnackInventorySaveEntry>();
            system.TryActivate(
                state,
                relationships,
                NpcVisitSystem.FermentationFairyId,
                "fairy_yogurt_bowl",
                "offer-fairy-1",
                MondayNoon);

            var insufficient = system.TryDeliver(
                state,
                relationships,
                economy,
                inventory,
                MondayNoon.AddDays(1),
                "claim-fairy-1");
            Assert.That(insufficient.Status, Is.EqualTo(NpcQuestDeliveryStatus.InsufficientResources));
            Assert.That(economy.milkCoins, Is.EqualTo(7));
            Assert.That(economy.milkDrops, Is.EqualTo(2));
            Assert.That(economy.collectionFragments, Is.EqualTo(1));
            Assert.That(relationships.relationships, Is.Empty);
            Assert.That(state.activeQuest.HasValue, Is.True);
            Assert.That(state.claimReceipts, Is.Empty);

            inventory.Add(new SnackInventorySaveEntry
            {
                snackId = SnackCatalog.FermentedYogurtBowlId,
                quantity = 1
            });
            economy.milkDrops = int.MaxValue;
            var capacityBlocked = system.TryDeliver(
                state,
                relationships,
                economy,
                inventory,
                MondayNoon.AddDays(1),
                "claim-fairy-capacity");
            Assert.That(capacityBlocked.Status, Is.EqualTo(NpcQuestDeliveryStatus.RewardCapacityFull));
            Assert.That(inventory[0].quantity, Is.EqualTo(1));
            Assert.That(economy.milkDrops, Is.EqualTo(int.MaxValue));
            Assert.That(state.claimReceipts, Is.Empty);
        }

        [Test]
        public void NpcQuestGraceDeliveryConsumesRewardsAdvancesTierAndIsIdempotent()
        {
            var system = new NpcRelationshipQuestSystem();
            var state = new NpcRelationshipQuestSaveData();
            var relationships = new NpcVisitSaveData
            {
                relationships = new List<NpcRelationshipSaveEntry>
                {
                    new NpcRelationshipSaveEntry
                    {
                        npcId = NpcVisitSystem.MilkyDoctorId,
                        affinity = 8
                    }
                }
            };
            var economy = new EconomySaveData();
            var inventory = new List<SnackInventorySaveEntry>
            {
                new SnackInventorySaveEntry
                {
                    snackId = SnackCatalog.WarmMilkSoupId,
                    quantity = 2
                }
            };
            system.TryActivate(
                state,
                relationships,
                NpcVisitSystem.MilkyDoctorId,
                "doctor_warm_soup",
                "offer-doctor-soup",
                MondayNoon);

            var delivered = system.TryDeliver(
                state,
                relationships,
                economy,
                inventory,
                MondayNoon.AddDays(4),
                "claim-doctor-soup");
            Assert.That(delivered.Applied, Is.True);
            Assert.That(delivered.UsedGrace, Is.True);
            Assert.That(delivered.AffinityBefore, Is.EqualTo(8));
            Assert.That(delivered.AffinityAfter, Is.EqualTo(13));
            Assert.That(delivered.TierAdvanced, Is.True);
            Assert.That(inventory[0].quantity, Is.EqualTo(1));
            Assert.That(economy.milkDrops, Is.EqualTo(2));
            Assert.That(economy.collectionFragments, Is.EqualTo(1));
            Assert.That(state.activeQuest.HasValue, Is.False);
            Assert.That(state.claimReceipts.Count, Is.EqualTo(1));

            var duplicate = system.TryDeliver(
                state,
                relationships,
                economy,
                inventory,
                MondayNoon.AddDays(4),
                "claim-doctor-soup");
            Assert.That(duplicate.Status, Is.EqualTo(NpcQuestDeliveryStatus.DuplicateClaim));
            Assert.That(inventory[0].quantity, Is.EqualTo(1));
            Assert.That(economy.milkDrops, Is.EqualTo(2));
            Assert.That(state.claimReceipts.Count, Is.EqualTo(1));

            var replayOffer = system.TryActivate(
                state,
                relationships,
                NpcVisitSystem.MilkyDoctorId,
                "doctor_warm_soup",
                "offer-doctor-soup",
                MondayNoon.AddDays(4));
            Assert.That(replayOffer.Status, Is.EqualTo(NpcQuestActivationStatus.AlreadyClaimedOffer));
        }

        [Test]
        public void NpcQuestStateNormalizesUnknownAndNullDataAndRoundTripsJson()
        {
            var system = new NpcRelationshipQuestSystem();
            var state = new NpcRelationshipQuestSaveData
            {
                schemaVersion = 0,
                activeQuest = new ActiveNpcRelationshipQuestSaveData
                {
                    offerId = "unknown-offer",
                    npcId = "unknown-npc",
                    questId = "unknown-quest",
                    startedAtIso = MondayNoon.ToString("O"),
                    expiresAtIso = MondayNoon.AddDays(3).ToString("O"),
                    graceEndsAtIso = MondayNoon.AddDays(5).ToString("O")
                },
                claimReceipts = new List<NpcRelationshipQuestClaimReceiptSaveData>
                {
                    null,
                    new NpcRelationshipQuestClaimReceiptSaveData
                    {
                        claimReceiptId = "unknown-claim",
                        offerId = "unknown-offer",
                        npcId = "unknown-npc",
                        questId = "unknown-quest",
                        claimedAtIso = MondayNoon.ToString("O")
                    }
                }
            };
            var relationships = new NpcVisitSaveData
            {
                relationships = new List<NpcRelationshipSaveEntry>
                {
                    null,
                    new NpcRelationshipSaveEntry { npcId = "unknown-npc", affinity = 999 }
                }
            };

            Assert.That(system.NormalizeState(state), Is.True);
            Assert.That(system.NormalizeRelationships(relationships), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(NpcRelationshipQuestSaveData.CurrentSchemaVersion));
            Assert.That(state.activeQuest.HasValue, Is.False);
            Assert.That(state.claimReceipts, Is.Empty);
            Assert.That(relationships.relationships, Is.Empty);

            var json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<NpcRelationshipQuestSaveData>(json);
            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
            Assert.That(restored.activeQuest, Is.Not.Null);
            Assert.That(restored.claimReceipts, Is.Empty);
        }

        [Test]
        public void WeekStartUsesInjectedOffsetLocalCalendarWithoutUtcOrKstConversion()
        {
            var sundayInMinusFive = new DateTimeOffset(
                2026,
                8,
                16,
                23,
                59,
                0,
                TimeSpan.FromHours(-5));
            var mondayInPlusNine = new DateTimeOffset(
                2026,
                8,
                17,
                0,
                0,
                0,
                TimeSpan.FromHours(9));

            Assert.That(
                WeeklyCareJourneySystem.GetWeekStart(sundayInMinusFive),
                Is.EqualTo(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.FromHours(-5))));
            Assert.That(
                WeeklyCareJourneySystem.GetWeekStart(mondayInPlusNine),
                Is.EqualTo(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.FromHours(9))));
            Assert.That(WeeklyCareJourneySystem.GetWeekKey(sundayInMinusFive), Is.EqualTo("2026-08-10"));
            Assert.That(WeeklyCareJourneySystem.GetWeekKey(mondayInPlusNine), Is.EqualTo("2026-08-17"));
        }

        [Test]
        public void WeeklyJourneyCompletesAnyThreeObjectivesWithoutConsecutiveDays()
        {
            var system = new WeeklyCareJourneySystem();
            var state = new WeeklyCareJourneySaveData();
            var initial = system.BuildSnapshot(state, MondayNoon);
            Assert.That(initial.Objectives.Count, Is.EqualTo(WeeklyCareJourneySystem.ObjectiveCount));
            Assert.That(initial.CompletedObjectives, Is.Zero);

            RecordMany(system, state, WeeklyCareEventIds.Feed, 6, MondayNoon, "feed");
            RecordMany(system, state, WeeklyCareEventIds.Play, 3, MondayNoon, "play");
            RecordMany(system, state, WeeklyCareEventIds.Discovery, 2, MondayNoon, "discovery");

            var sameDaySnapshot = system.BuildSnapshot(state, MondayNoon);
            Assert.That(sameDaySnapshot.CompletedObjectives, Is.EqualTo(3));
            Assert.That(sameDaySnapshot.CanClaimReward, Is.True);
            Assert.That(FindObjective(sameDaySnapshot, "weekly_care_12").Completed, Is.False);
            Assert.That(FindObjective(sameDaySnapshot, "weekly_feed_6").Completed, Is.True);
            Assert.That(FindObjective(sameDaySnapshot, "weekly_play_3").Completed, Is.True);
            Assert.That(FindObjective(sameDaySnapshot, "weekly_discovery_2").Completed, Is.True);
        }

        [Test]
        public void WeeklyEventRecordingIsCumulativeDeterministicAndReceiptIdempotent()
        {
            var system = new WeeklyCareJourneySystem();
            var state = new WeeklyCareJourneySaveData();

            var first = system.RecordEvent(
                state,
                WeeklyCareEventIds.Cook,
                2,
                MondayNoon,
                "weekly-event-1");
            var duplicate = system.RecordEvent(
                state,
                WeeklyCareEventIds.Cook,
                2,
                MondayNoon,
                "weekly-event-1");
            var second = system.RecordEvent(
                state,
                WeeklyCareEventIds.Blend,
                1,
                MondayNoon.AddDays(4),
                "weekly-event-2");
            var unknown = system.RecordEvent(
                state,
                "unknown.event",
                1,
                MondayNoon,
                "weekly-event-unknown");

            Assert.That(first.Applied, Is.True);
            Assert.That(duplicate.Status, Is.EqualTo(WeeklyCareRecordStatus.DuplicateReceipt));
            Assert.That(second.Applied, Is.True);
            Assert.That(unknown.Status, Is.EqualTo(WeeklyCareRecordStatus.UnknownEvent));
            var snapshot = system.BuildSnapshot(state, MondayNoon.AddDays(4));
            Assert.That(FindObjective(snapshot, "weekly_kitchen_3").Progress, Is.EqualTo(3));
            Assert.That(FindObjective(snapshot, "weekly_care_12").Progress, Is.EqualTo(3));
            Assert.That(state.eventReceipts.Count, Is.EqualTo(2));
        }

        [Test]
        public void WeeklyRewardClaimIsAtomicOneTimeAndReceiptIdempotent()
        {
            var system = new WeeklyCareJourneySystem();
            var state = new WeeklyCareJourneySaveData();
            RecordMany(system, state, WeeklyCareEventIds.Feed, 6, MondayNoon, "claim-feed");
            RecordMany(system, state, WeeklyCareEventIds.Play, 3, MondayNoon, "claim-play");
            RecordMany(system, state, WeeklyCareEventIds.Discovery, 2, MondayNoon, "claim-discovery");

            var economy = new EconomySaveData
            {
                milkCoins = int.MaxValue,
                milkDrops = 4,
                collectionFragments = 2
            };
            var blocked = system.TryClaimReward(state, economy, MondayNoon, "weekly-claim-1");
            Assert.That(blocked.Status, Is.EqualTo(WeeklyCareClaimStatus.RewardCapacityFull));
            Assert.That(economy.milkCoins, Is.EqualTo(int.MaxValue));
            Assert.That(economy.milkDrops, Is.EqualTo(4));
            Assert.That(economy.collectionFragments, Is.EqualTo(2));
            Assert.That(state.rewardReceipts, Is.Empty);

            economy.milkCoins = 0;
            var claimed = system.TryClaimReward(state, economy, MondayNoon, "weekly-claim-1");
            Assert.That(claimed.Applied, Is.True);
            Assert.That(economy.milkCoins, Is.EqualTo(WeeklyCareJourneySystem.RewardMilkCoins));
            Assert.That(economy.milkDrops, Is.EqualTo(4 + WeeklyCareJourneySystem.RewardMilkDrops));
            Assert.That(
                economy.collectionFragments,
                Is.EqualTo(2 + WeeklyCareJourneySystem.RewardCollectionFragments));

            var duplicate = system.TryClaimReward(state, economy, MondayNoon, "weekly-claim-1");
            var anotherReceipt = system.TryClaimReward(state, economy, MondayNoon, "weekly-claim-2");
            Assert.That(duplicate.Status, Is.EqualTo(WeeklyCareClaimStatus.DuplicateClaim));
            Assert.That(anotherReceipt.Status, Is.EqualTo(WeeklyCareClaimStatus.AlreadyClaimed));
            Assert.That(state.rewardReceipts.Count, Is.EqualTo(1));
        }

        [Test]
        public void WeeklyJourneyAdvancesWithoutStreakAndRejectsClockRollback()
        {
            var system = new WeeklyCareJourneySystem();
            var state = new WeeklyCareJourneySaveData();
            system.RecordEvent(
                state,
                WeeklyCareEventIds.Feed,
                4,
                MondayNoon,
                "week-one-feed");
            var nextMonday = MondayNoon.AddDays(7);
            var advanced = system.ReconcileWeek(state, nextMonday);
            Assert.That(advanced.Status, Is.EqualTo(WeeklyCareWeekStatus.Advanced));
            foreach (var objective in system.BuildSnapshot(state, nextMonday).Objectives)
            {
                Assert.That(objective.Progress, Is.Zero);
            }

            system.RecordEvent(
                state,
                WeeklyCareEventIds.Play,
                1,
                nextMonday,
                "week-two-play");
            var rollback = system.ReconcileWeek(state, MondayNoon);
            var blocked = system.RecordEvent(
                state,
                WeeklyCareEventIds.Play,
                1,
                MondayNoon,
                "rollback-play");
            Assert.That(rollback.Status, Is.EqualTo(WeeklyCareWeekStatus.ClockRollback));
            Assert.That(blocked.Status, Is.EqualTo(WeeklyCareRecordStatus.ClockRollback));
            Assert.That(
                FindObjective(system.BuildSnapshot(state, nextMonday), "weekly_play_3").Progress,
                Is.EqualTo(1));
            Assert.That(state.eventReceipts.Count, Is.EqualTo(2));
        }

        [Test]
        public void WeeklyLegacyNullAndUnknownDataRepairsAndRoundTripsJson()
        {
            var system = new WeeklyCareJourneySystem();
            var state = new WeeklyCareJourneySaveData
            {
                schemaVersion = 0,
                weekKey = null,
                objectives = new List<WeeklyCareObjectiveProgressSaveData>
                {
                    null,
                    new WeeklyCareObjectiveProgressSaveData
                    {
                        objectiveId = "unknown-objective",
                        progress = 999
                    }
                },
                eventReceipts = new List<WeeklyCareEventReceiptSaveData>
                {
                    null,
                    new WeeklyCareEventReceiptSaveData
                    {
                        receiptId = "unknown-event-receipt",
                        eventId = "unknown.event",
                        weekKey = "2026-08-17",
                        recordedAtIso = MondayNoon.ToString("O")
                    }
                },
                rewardReceipts = null
            };

            var repaired = system.ReconcileWeek(state, MondayNoon);
            Assert.That(repaired.Status, Is.EqualTo(WeeklyCareWeekStatus.Initialized));
            Assert.That(state.schemaVersion, Is.EqualTo(WeeklyCareJourneySaveData.CurrentSchemaVersion));
            Assert.That(state.weekKey, Is.EqualTo("2026-08-17"));
            Assert.That(state.objectives.Count, Is.EqualTo(WeeklyCareJourneySystem.ObjectiveCount));
            Assert.That(state.eventReceipts, Is.Empty);
            Assert.That(state.rewardReceipts, Is.Not.Null);

            var json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<WeeklyCareJourneySaveData>(json);
            var snapshot = system.BuildSnapshot(restored, MondayNoon);
            Assert.That(snapshot.Objectives.Count, Is.EqualTo(WeeklyCareJourneySystem.ObjectiveCount));
            Assert.That(snapshot.CompletedObjectives, Is.Zero);
            Assert.That(restored.EnsureRuntimeDefaults(), Is.False);
        }

        private static int CountQuests(NpcRelationshipQuestSystem system, string npcId)
        {
            var count = 0;
            foreach (var quest in system.All)
            {
                if (quest.NpcId == npcId)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static void RecordMany(
            WeeklyCareJourneySystem system,
            WeeklyCareJourneySaveData state,
            string eventId,
            int count,
            DateTimeOffset now,
            string receiptPrefix)
        {
            for (var index = 0; index < count; index += 1)
            {
                var result = system.RecordEvent(
                    state,
                    eventId,
                    1,
                    now,
                    $"{receiptPrefix}-{index}");
                Assert.That(result.Applied, Is.True, result.Status.ToString());
            }
        }

        private static WeeklyCareObjectiveSnapshot FindObjective(
            WeeklyCareJourneySnapshot snapshot,
            string objectiveId)
        {
            foreach (var objective in snapshot.Objectives)
            {
                if (objective.Definition?.Id == objectiveId)
                {
                    return objective;
                }
            }

            Assert.Fail($"Missing weekly objective: {objectiveId}");
            return default;
        }
    }
}
