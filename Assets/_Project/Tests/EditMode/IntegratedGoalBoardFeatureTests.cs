using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Guidance;
using CheeseTama.Save;
using NUnit.Framework;

namespace CheeseTama.Tests
{
    public sealed class IntegratedGoalBoardFeatureTests
    {
        [Test]
        public void LateLevelBoardCompressesEveryMissingConditionIntoThreePublicHorizons()
        {
            var snapshot = NextActionGoalBoardSystem.BuildLateLevel(
                currentLevel: 31,
                progressUnits: 75,
                affection: 10,
                qualifyingMilkTypeCount: 1,
                stableStatusCount: 2);

            Assert.That(snapshot.IsApplicable, Is.True);
            Assert.That(snapshot.IsReadyForLevelUp, Is.False);
            Assert.That(snapshot.CurrentLevel, Is.EqualTo(31));
            Assert.That(snapshot.TargetLevel, Is.EqualTo(32));
            Assert.That(snapshot.ProgressPercent, Is.EqualTo(31));
            Assert.That(snapshot.Goals.Count, Is.EqualTo(3));
            Assert.That(snapshot.MissingConditions.Count, Is.EqualTo(4));

            Assert.That(snapshot.Goals[0].Urgency, Is.EqualTo(NextActionUrgency.Urgent));
            Assert.That(snapshot.Goals[0].ProgressPercent, Is.EqualTo(50));
            Assert.That(snapshot.Goals[0].MissingCondition, Does.Contain("안정 상태 2/4"));
            Assert.That(snapshot.Goals[0].DestinationRouteId, Is.EqualTo(NextActionRouteIds.Care));

            Assert.That(snapshot.Goals[1].Urgency, Is.EqualTo(NextActionUrgency.Today));
            Assert.That(snapshot.Goals[1].ProgressPercent, Is.EqualTo(18));
            Assert.That(snapshot.Goals[1].MissingCondition, Does.Contain("성장 진행 75/300"));
            Assert.That(snapshot.Goals[1].MissingCondition, Does.Contain("애정 10/55"));

            Assert.That(snapshot.Goals[2].Urgency, Is.EqualTo(NextActionUrgency.LongTerm));
            Assert.That(snapshot.Goals[2].ProgressPercent, Is.EqualTo(33));
            Assert.That(snapshot.Goals[2].MissingCondition, Does.Contain("우유 성장 다양성 1/3"));
            Assert.That(
                snapshot.Goals[2].DestinationRouteId,
                Is.EqualTo(NextActionRouteIds.MilkGrowth));
        }

        [Test]
        public void LateLevelBoardHandlesProgressOnlyReadyAndFinalStates()
        {
            var progressing = NextActionGoalBoardSystem.BuildLateLevel(30, 100, 0, 0, 0);
            Assert.That(progressing.IsApplicable, Is.True);
            Assert.That(progressing.ProgressPercent, Is.EqualTo(50));
            Assert.That(progressing.Goals.Count, Is.EqualTo(1));
            Assert.That(progressing.Goals[0].Urgency, Is.EqualTo(NextActionUrgency.Today));
            Assert.That(progressing.Goals[0].ProgressPercent, Is.EqualTo(50));

            var ready = NextActionGoalBoardSystem.BuildLateLevel(30, int.MaxValue, 0, 0, 0);
            Assert.That(ready.IsReadyForLevelUp, Is.True);
            Assert.That(ready.ProgressPercent, Is.EqualTo(100));
            Assert.That(ready.Goals, Is.Empty);
            Assert.That(ready.MissingConditions, Is.Empty);

            var final = NextActionGoalBoardSystem.BuildLateLevel(33, 500, 100, 6, 5);
            Assert.That(final.IsApplicable, Is.False);
            Assert.That(final.IsReadyForLevelUp, Is.False);
            Assert.That(final.Goals, Is.Empty);
        }

        [Test]
        public void AutonomousDiscoverySnapshotAlwaysHasSixSlotsAndHidesUndiscoveredIds()
        {
            var discoveredAt = new DateTimeOffset(
                2026,
                8,
                17,
                10,
                30,
                0,
                TimeSpan.FromHours(9));
            var save = new AutonomousLifeSaveData
            {
                firstDiscoveries = new List<AutonomousLifeDiscoverySaveEntry>
                {
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = " dance ",
                        firstDiscoveredAtIso = discoveredAt.ToString("O")
                    },
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = "window",
                        firstDiscoveredAtIso = "not-a-time"
                    },
                    new AutonomousLifeDiscoverySaveEntry
                    {
                        behaviourId = "future_hidden_behaviour",
                        firstDiscoveredAtIso = discoveredAt.ToString("O")
                    },
                    null
                }
            };

            var snapshot = AutonomousLifeDiscoveryCatalog.CreateSnapshot(save);

            Assert.That(snapshot.TotalCount, Is.EqualTo(6));
            Assert.That(snapshot.TotalCount, Is.EqualTo(AutonomousLifeDiscoveryCatalog.TotalDiscoveryCount));
            Assert.That(snapshot.DiscoveredCount, Is.EqualTo(2));
            Assert.That(save.firstDiscoveries, Has.Count.EqualTo(4));
            Assert.That(save.firstDiscoveries[0].behaviourId, Is.EqualTo(" dance "));

            var hiddenCount = 0;
            var dance = default(AutonomousLifeDiscoveryItemSnapshot);
            var window = default(AutonomousLifeDiscoveryItemSnapshot);
            for (var index = 0; index < snapshot.Items.Count; index += 1)
            {
                var item = snapshot.Items[index];
                if (!item.IsDiscovered)
                {
                    hiddenCount += 1;
                    Assert.That(item.BehaviourId, Is.Empty);
                    Assert.That(item.FirstDiscoveredAtIso, Is.Empty);
                    Assert.That(
                        item.DisplayName,
                        Is.EqualTo(AutonomousLifeDiscoveryCatalog.HiddenDisplayName));
                    continue;
                }

                if (item.BehaviourId == AutonomousLifeBehaviourCatalog.DanceId)
                {
                    dance = item;
                }
                else if (item.BehaviourId == AutonomousLifeBehaviourCatalog.WindowId)
                {
                    window = item;
                }
            }

            Assert.That(hiddenCount, Is.EqualTo(4));
            Assert.That(dance, Is.Not.Null);
            Assert.That(dance.HasDiscoveryTime, Is.True);
            Assert.That(dance.FirstDiscoveredAtIso, Is.EqualTo(discoveredAt.ToString("O")));
            Assert.That(window, Is.Not.Null);
            Assert.That(window.HasDiscoveryTime, Is.False);
        }

        [Test]
        public void ObservedDiscoverySnapshotIsPublishedOnlyForTheFirstKnownDiscovery()
        {
            var now = new DateTimeOffset(
                2026,
                8,
                17,
                11,
                0,
                0,
                TimeSpan.FromHours(9));
            var save = new AutonomousLifeSaveData();
            var system = new AutonomousLifeSystem();

            var first = system.RecordFirstDiscovery(save, AutonomousLifeBehaviour.Play, now);
            var duplicate = system.RecordFirstDiscovery(
                save,
                AutonomousLifeBehaviour.Play,
                now.AddHours(1));

            Assert.That(
                AutonomousLifeDiscoveryCatalog.TryCreateObservedSnapshot(
                    first,
                    out var observed),
                Is.True);
            Assert.That(observed.IsDiscovered, Is.True);
            Assert.That(observed.BehaviourId, Is.EqualTo(AutonomousLifeBehaviourCatalog.PlayId));
            Assert.That(observed.FirstDiscoveredAtIso, Is.EqualTo(now.ToString("O")));
            Assert.That(
                AutonomousLifeDiscoveryCatalog.TryCreateObservedSnapshot(
                    duplicate,
                    out var ignored),
                Is.False);
            Assert.That(ignored, Is.Null);
        }

        [Test]
        public void EventJournalMergesKnownHistoryAndSummarizesLatestValidChoiceReadOnly()
        {
            var now = new DateTimeOffset(
                2026,
                8,
                17,
                12,
                0,
                0,
                TimeSpan.FromHours(9));
            var save = new RandomEventSaveData
            {
                history = new List<RandomEventHistorySaveEntry>
                {
                    new RandomEventHistorySaveEntry
                    {
                        eventId = "warm_lamp_choice",
                        totalOccurrences = 3,
                        lastOccurredAtIso = now.AddDays(-2).ToString("O")
                    },
                    new RandomEventHistorySaveEntry
                    {
                        eventId = "warm_lamp_choice",
                        totalOccurrences = 4,
                        lastOccurredAtIso = now.AddDays(-5).ToString("O")
                    },
                    new RandomEventHistorySaveEntry
                    {
                        eventId = "small_fever",
                        totalOccurrences = 2,
                        lastOccurredAtIso = now.AddDays(-4).ToString("O")
                    },
                    new RandomEventHistorySaveEntry
                    {
                        eventId = "unknown_hidden_event",
                        totalOccurrences = 999,
                        lastOccurredAtIso = now.ToString("O")
                    },
                    null
                },
                choiceReceipts = new List<CareEventChoiceReceiptSaveEntry>
                {
                    new CareEventChoiceReceiptSaveEntry
                    {
                        occurrenceId = "warm-old",
                        eventId = "warm_lamp_choice",
                        choiceId = "wrap_blanket",
                        resolvedAtIso = now.AddDays(-3).ToString("O")
                    },
                    new CareEventChoiceReceiptSaveEntry
                    {
                        occurrenceId = "warm-latest",
                        eventId = "warm_lamp_choice",
                        choiceId = "light_milk_lamp",
                        resolvedAtIso = now.AddDays(-1).ToString("O")
                    },
                    new CareEventChoiceReceiptSaveEntry
                    {
                        occurrenceId = "warm-unknown-choice",
                        eventId = "warm_lamp_choice",
                        choiceId = "hidden_choice",
                        resolvedAtIso = now.ToString("O")
                    },
                    null
                }
            };

            var snapshot = RandomEventJournalSystem.Build(save, now);

            Assert.That(snapshot.GeneratedAtIso, Is.EqualTo(now.ToString("O")));
            Assert.That(snapshot.Entries.Count, Is.EqualTo(2));
            Assert.That(snapshot.TotalOccurrences, Is.EqualTo(9));
            Assert.That(snapshot.Entries[0].EventId, Is.EqualTo("warm_lamp_choice"));
            Assert.That(snapshot.Entries[0].Title, Is.EqualTo("따뜻한 불빛 아래에서"));
            Assert.That(snapshot.Entries[0].TotalOccurrences, Is.EqualTo(7));
            Assert.That(snapshot.Entries[0].DaysSinceLastOccurrence, Is.EqualTo(2));
            Assert.That(snapshot.Entries[0].LatestChoiceLabel, Is.EqualTo("우유등을 켠다"));
            Assert.That(snapshot.Entries[0].LatestChoiceSummary, Does.Contain("은은한 우유등"));
            Assert.That(snapshot.Entries[0].LatestChoiceSummary, Does.Contain("우유방울 +2"));
            Assert.That(
                snapshot.Entries[0].LatestChoiceResolvedAtIso,
                Is.EqualTo(now.AddDays(-1).ToString("O")));
            Assert.That(snapshot.Entries[1].EventId, Is.EqualTo("small_fever"));
            Assert.That(snapshot.Entries[1].HasLatestChoice, Is.False);

            Assert.That(save.history, Has.Count.EqualTo(5));
            Assert.That(save.choiceReceipts, Has.Count.EqualTo(4));
            Assert.That(save.history[3].eventId, Is.EqualTo("unknown_hidden_event"));
        }

        [Test]
        public void EventJournalNeedsNoReceiptAndHandlesNullInvalidOrFutureTimes()
        {
            var now = new DateTimeOffset(
                2026,
                8,
                17,
                12,
                0,
                0,
                TimeSpan.Zero);

            var empty = RandomEventJournalSystem.Build(null, now);
            Assert.That(empty.Entries, Is.Empty);
            Assert.That(empty.TotalOccurrences, Is.Zero);

            var save = new RandomEventSaveData
            {
                history = new List<RandomEventHistorySaveEntry>
                {
                    new RandomEventHistorySaveEntry
                    {
                        eventId = "quiet_hum",
                        totalOccurrences = 1,
                        lastOccurredAtIso = "invalid"
                    },
                    new RandomEventHistorySaveEntry
                    {
                        eventId = "small_fever",
                        totalOccurrences = 1,
                        lastOccurredAtIso = now.AddDays(1).ToString("O")
                    }
                },
                choiceReceipts = null
            };

            var snapshot = RandomEventJournalSystem.Build(save, now);
            var quiet = FindJournalEntry(snapshot, "quiet_hum");
            var fever = FindJournalEntry(snapshot, "small_fever");

            Assert.That(quiet, Is.Not.Null);
            Assert.That(quiet.HasLastOccurrenceTime, Is.False);
            Assert.That(quiet.DaysSinceLastOccurrence, Is.EqualTo(-1));
            Assert.That(quiet.HasLatestChoice, Is.False);
            Assert.That(fever, Is.Not.Null);
            Assert.That(fever.DaysSinceLastOccurrence, Is.Zero);
            Assert.That(save.history[0].lastOccurredAtIso, Is.EqualTo("invalid"));
            Assert.That(save.choiceReceipts, Is.Null);
        }

        private static RandomEventJournalEntrySnapshot FindJournalEntry(
            RandomEventJournalSnapshot snapshot,
            string eventId)
        {
            for (var index = 0; index < snapshot.Entries.Count; index += 1)
            {
                if (snapshot.Entries[index].EventId == eventId)
                {
                    return snapshot.Entries[index];
                }
            }

            return null;
        }
    }
}
