using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Memories;
using CheeseTama.Save;
using NUnit.Framework;

namespace CheeseTama.Tests.EditMode
{
    public sealed class MemoryJournalTests
    {
        private static readonly DateTimeOffset Baseline =
            new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.FromHours(9));

        private MemoryJournalSystem system;
        private MemoryJournalSaveData journal;

        [SetUp]
        public void SetUp()
        {
            system = new MemoryJournalSystem();
            journal = new MemoryJournalSaveData();
        }

        [Test]
        public void SameSourceOccurrenceIsRecordedOnlyOnce()
        {
            var first = system.TryRecordReturn(
                journal,
                "return_001",
                125,
                Baseline,
                "모짜",
                "soft_cheesetama",
                out var recorded);
            var duplicate = system.TryRecordReturn(
                journal,
                "return_001",
                180,
                Baseline.AddHours(3),
                "모짜",
                "soft_cheesetama",
                out _);

            Assert.That(first, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(recorded, Is.Not.Null);
            Assert.That(journal.entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void OnlyFirstPetFeedOrPlayCreatesDailyCareMemory()
        {
            Assert.That(
                system.TryRecordFirstDailyCare(
                    journal,
                    "pet",
                    Baseline,
                    "모짜",
                    "soft_cheesetama",
                    out var first),
                Is.True);
            Assert.That(
                system.TryRecordFirstDailyCare(
                    journal,
                    "feed_milk",
                    Baseline.AddHours(1),
                    "모짜",
                    "soft_cheesetama",
                    out _),
                Is.False);
            Assert.That(
                system.TryRecordFirstDailyCare(
                    journal,
                    "play",
                    Baseline.AddDays(1),
                    "모짜",
                    "soft_cheesetama",
                    out _),
                Is.True);
            Assert.That(
                system.TryRecordFirstDailyCare(
                    journal,
                    "clean",
                    Baseline.AddDays(2),
                    "모짜",
                    "soft_cheesetama",
                    out _),
                Is.False);

            Assert.That(first.detailId, Is.EqualTo("pet"));
            Assert.That(journal.entries, Has.Count.EqualTo(2));
        }

        [Test]
        public void CapacityTrimsOldRoutineMemoriesBeforeGrowthAndEvolution()
        {
            Assert.That(
                system.TryRecordGrowth(
                    journal,
                    "growth_stage_2",
                    "growth_occurrence_2",
                    11,
                    "말랑 치즈타마",
                    Baseline,
                    "모짜",
                    "soft_cheesetama",
                    false,
                    string.Empty,
                    out var growth),
                Is.True);
            Assert.That(
                system.TryRecordEvolution(
                    journal,
                    "evolution_mozzarella",
                    "evolution_occurrence_1",
                    22,
                    "모짜렐라 치즈타마",
                    Baseline.AddMinutes(1),
                    "모짜",
                    "mozzarella_cheesetama",
                    false,
                    string.Empty,
                    out var evolution),
                Is.True);

            for (var day = 0; day < 65; day += 1)
            {
                Assert.That(
                    system.TryRecordFirstDailyCare(
                        journal,
                        "pet",
                        Baseline.AddDays(day + 1),
                        "모짜",
                        "soft_cheesetama",
                        out _),
                    Is.True);
            }

            Assert.That(journal.entries, Has.Count.EqualTo(MemoryJournalSaveData.MaximumEntries));
            Assert.That(journal.entries.Exists(entry => entry.id == growth.id), Is.True);
            Assert.That(journal.entries.Exists(entry => entry.id == evolution.id), Is.True);
            Assert.That(journal.entries.Exists(entry => entry.dateKey == "2026-08-15"), Is.False);
            Assert.That(journal.entries.Exists(entry => entry.dateKey == "2026-10-18"), Is.True);
        }

        [Test]
        public void UnreadEntriesCanBeReadIndividuallyOrTogether()
        {
            system.TryRecordFirstDailyCare(
                journal,
                "pet",
                Baseline,
                "모짜",
                "soft_cheesetama",
                out var care);
            system.TryRecordReturn(
                journal,
                "return_002",
                75,
                Baseline.AddDays(1),
                "모짜",
                "soft_cheesetama",
                out _);

            Assert.That(system.CountUnread(journal), Is.EqualTo(2));
            Assert.That(system.TryMarkRead(journal, care.id), Is.True);
            Assert.That(system.TryMarkRead(journal, care.id), Is.False);
            Assert.That(system.CountUnread(journal), Is.EqualTo(1));
            Assert.That(system.MarkAllRead(journal), Is.EqualTo(1));
            Assert.That(system.CountUnread(journal), Is.Zero);
        }

        [Test]
        public void HiddenContentIsMaskedUntilItsUnlockResolverAllowsIt()
        {
            system.TryRecordEvolution(
                journal,
                "star_secret_evolution",
                "secret_occurrence_1",
                33,
                "별빛 왕관 치즈타마",
                Baseline,
                "모짜",
                "star_crown_cheesetama",
                true,
                "star_route",
                out var hidden);

            var masked = system.CreatePresentation(hidden);
            var revealed = system.CreatePresentation(hidden, unlockId => unlockId == "star_route");

            Assert.That(masked.IsMasked, Is.True);
            Assert.That(masked.Title, Does.Not.Contain("별빛"));
            Assert.That(masked.Quote, Does.Not.Contain("왕관"));
            Assert.That(masked.FormId, Is.Empty);
            Assert.That(revealed.IsMasked, Is.False);
            Assert.That(revealed.Title, Does.Contain("별빛 왕관 치즈타마"));
            Assert.That(revealed.FormId, Is.EqualTo("star_crown_cheesetama"));
        }

        [Test]
        public void LatestMemoryIsRecalledOnlyOnceUntilANewerMemoryArrives()
        {
            system.TryRecordFirstDailyCare(
                journal,
                "pet",
                Baseline,
                "모짜",
                "soft_cheesetama",
                out _);
            system.TryRecordReturn(
                journal,
                "return_003",
                180,
                Baseline.AddDays(1),
                "모짜",
                "soft_cheesetama",
                out var latest);

            Assert.That(system.TrySelectLatestRecall(journal, null, out var recall), Is.True);
            Assert.That(recall.MemoryId, Is.EqualTo(latest.id));
            Assert.That(system.AcknowledgeRecall(journal, recall.MemoryId), Is.True);
            Assert.That(system.TrySelectLatestRecall(journal, null, out _), Is.False);

            system.TryRecordGrowth(
                journal,
                "growth_stage_3",
                "growth_occurrence_3",
                22,
                "단단한 치즈타마",
                Baseline.AddDays(2),
                "모짜",
                "firm_cheesetama",
                false,
                string.Empty,
                out var newer);

            Assert.That(system.TrySelectLatestRecall(journal, null, out recall), Is.True);
            Assert.That(recall.MemoryId, Is.EqualTo(newer.id));
            Assert.That(recall.DialogueLine, Does.StartWith("그날의 기억:"));
        }

        [Test]
        public void GrowthAndEvolutionSourcesRemainIdempotentAcrossNewOccurrenceIds()
        {
            Assert.That(
                system.TryRecordGrowth(
                    journal,
                    "growth_stage_final",
                    "growth_run_1",
                    33,
                    "최종 성장",
                    Baseline,
                    "몽글이",
                    "final_cheesetama",
                    false,
                    string.Empty,
                    out _),
                Is.True);
            Assert.That(
                system.TryRecordGrowth(
                    journal,
                    "growth_stage_final",
                    "growth_run_2",
                    33,
                    "최종 성장",
                    Baseline.AddDays(1),
                    "몽글이",
                    "final_cheesetama",
                    false,
                    string.Empty,
                    out _),
                Is.False);

            Assert.That(
                system.TryRecordEvolution(
                    journal,
                    "cream_cheesetama",
                    "evolution_run_1",
                    21,
                    "크림치즈타마",
                    Baseline,
                    "몽글이",
                    "cream_cheesetama",
                    false,
                    string.Empty,
                    out _),
                Is.True);
            Assert.That(
                system.TryRecordEvolution(
                    journal,
                    "cream_cheesetama",
                    "evolution_run_2",
                    21,
                    "크림치즈타마",
                    Baseline.AddDays(1),
                    "몽글이",
                    "cream_cheesetama",
                    false,
                    string.Empty,
                    out _),
                Is.False);

            Assert.That(journal.entries, Has.Count.EqualTo(2));
        }

        [Test]
        public void EnsureRuntimeDefaultsRepairsPartialLegacyEntries()
        {
            journal.schemaVersion = 0;
            journal.entries = new List<MemoryJournalEntrySaveData>
            {
                null,
                new MemoryJournalEntrySaveData
                {
                    id = null,
                    idempotencyKey = null,
                    kind = MemoryJournalKind.Growth,
                    sourceId = "growth_stage_legacy",
                    occurredAtIso = Baseline.ToString("O"),
                    title = null,
                    quote = null
                }
            };
            journal.lastRecalledMemoryId = null;

            Assert.That(journal.EnsureRuntimeDefaults(), Is.True);
            Assert.That(journal.schemaVersion, Is.EqualTo(MemoryJournalSaveData.CurrentSchemaVersion));
            Assert.That(journal.entries, Has.Count.EqualTo(1));
            Assert.That(journal.entries[0].id, Is.Not.Empty);
            Assert.That(journal.entries[0].idempotencyKey, Is.Not.Empty);
            Assert.That(journal.entries[0].important, Is.True);
            Assert.That(journal.entries[0].dateKey, Is.EqualTo("2026-08-14"));
            Assert.That(journal.entries[0].unread, Is.False);
            Assert.That(journal.lastRecalledMemoryId, Is.Empty);
        }
    }
}
