using System.Collections.Generic;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using NUnit.Framework;

namespace CheeseTama.Tests
{
    public sealed class LateLevelGrowthFeatureTests
    {
        [Test]
        public void CatalogMatchesTheThreePlannedLateLevelTransitions()
        {
            Assert.That(LateLevelGrowthCatalog.All.Count, Is.EqualTo(3));

            AssertRequirement(30, 31, 200, 0, 0, 0, 0);
            AssertRequirement(31, 32, 300, 55, 3, 2, 4);
            AssertRequirement(32, 33, 500, 75, 5, 3, 5);
            Assert.That(LateLevelGrowthCatalog.TryGetForCurrentLevel(29, out _), Is.False);
            Assert.That(LateLevelGrowthCatalog.TryGetForCurrentLevel(33, out _), Is.False);
            Assert.That(LateLevelGrowthSystem.CountQualifyingMainMilks(null, 0), Is.Zero);
        }

        [TestCase(30, 37, 74)]
        [TestCase(31, 37, 111)]
        [TestCase(32, 37, 185)]
        public void LegacyPercentMigrationPreservesExactVisibleRatio(
            int level,
            int legacyPercent,
            int expectedUnits)
        {
            var tama = CreateTama(level);
            tama.levelProgress = legacyPercent;
            tama.evolutionId = EvolutionSystem.CoffeeEvolutionId;
            tama.form = EvolutionSystem.CoffeeEvolutionId;
            var state = new LateLevelGrowthSaveData { schemaVersion = 0 };

            var first = LateLevelProgressMigration.EnsureCurrent(tama, state);
            var second = LateLevelProgressMigration.EnsureCurrent(tama, state);

            Assert.That(first.Status, Is.EqualTo(LateLevelProgressMigrationStatus.InitializedFromLegacyPercent));
            Assert.That(first.ProgressUnits, Is.EqualTo(expectedUnits));
            Assert.That(first.DisplayPercent, Is.EqualTo(legacyPercent));
            Assert.That(tama.levelProgress, Is.EqualTo(legacyPercent));
            Assert.That(state.trackedLevel, Is.EqualTo(level));
            Assert.That(state.migratedFromLegacyPercent, Is.True);
            Assert.That(second.Status, Is.EqualTo(LateLevelProgressMigrationStatus.AlreadyCurrent));
            Assert.That(second.Changed, Is.False);
            Assert.That(tama.evolutionId, Is.EqualTo(EvolutionSystem.CoffeeEvolutionId));
            Assert.That(tama.form, Is.EqualTo(EvolutionSystem.CoffeeEvolutionId));
        }

        [Test]
        public void FutureSaveSchemaIsRejectedWithoutMutatingProgress()
        {
            var tama = CreateTama(31);
            tama.levelProgress = 42;
            var state = new LateLevelGrowthSaveData
            {
                schemaVersion = LateLevelGrowthSaveData.CurrentSchemaVersion + 1,
                initialized = true,
                trackedLevel = 31,
                progressUnits = 123
            };

            var migration = LateLevelProgressMigration.EnsureCurrent(tama, state);
            var synced = LateLevelProgressMigration.SyncCompatibilityPercent(tama, state);
            var result = new LateLevelGrowthSystem().AddProgress(tama, state, null, 10);

            Assert.That(migration.Status, Is.EqualTo(LateLevelProgressMigrationStatus.UnsupportedFutureVersion));
            Assert.That(synced, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(LateLevelGrowthOutcome.UnsupportedSaveVersion));
            Assert.That(tama.level, Is.EqualTo(31));
            Assert.That(tama.levelProgress, Is.EqualTo(42));
            Assert.That(state.progressUnits, Is.EqualTo(123));
            Assert.That(state.schemaVersion, Is.EqualTo(LateLevelGrowthSaveData.CurrentSchemaVersion + 1));
        }

        [Test]
        public void LevelThirtyToThirtyOneUsesProgressOnlyAndCarriesOverflow()
        {
            var tama = CreateTama(30);
            var state = new LateLevelGrowthSaveData();

            var result = new LateLevelGrowthSystem().AddProgress(tama, state, null, 250);

            Assert.That(result.Outcome, Is.EqualTo(LateLevelGrowthOutcome.LevelAdvanced));
            Assert.That(result.PreviousLevel, Is.EqualTo(30));
            Assert.That(tama.level, Is.EqualTo(31));
            Assert.That(result.AcceptedProgressUnits, Is.EqualTo(200));
            Assert.That(result.CarriedProgressUnits, Is.EqualTo(50));
            Assert.That(result.UnusedProgressUnits, Is.Zero);
            Assert.That(state.trackedLevel, Is.EqualTo(31));
            Assert.That(state.progressUnits, Is.EqualTo(50));
            Assert.That(tama.levelProgress, Is.EqualTo(16));
        }

        [Test]
        public void FullLevelThirtyOneBarWaitsForCombinedCareGate()
        {
            var tama = CreateTama(31);
            tama.stats.affection = 20;
            tama.stats.sleepiness = 90;
            var state = new LateLevelGrowthSaveData();
            state.BeginLevel(31, 300);

            var result = new LateLevelGrowthSystem().TryAdvance(tama, state, Growth(2, 2));

            Assert.That(result.Outcome, Is.EqualTo(LateLevelGrowthOutcome.GateBlocked));
            Assert.That(result.IsBlocked, Is.True);
            Assert.That(result.NormalizedPercent, Is.EqualTo(100));
            Assert.That(tama.levelProgress, Is.EqualTo(99));
            Assert.That(tama.level, Is.EqualTo(31));
            Assert.That(result.GateStatus.AffectionMet, Is.False);
            Assert.That(result.GateStatus.MilkGrowthDiversityMet, Is.False);
            Assert.That(result.GateStatus.StableStatusDiversityMet, Is.True);
            Assert.That(result.GateStatus.BuildMissingRequirementsMessage(), Does.Contain("애정"));
            Assert.That(result.GateStatus.BuildMissingRequirementsMessage(), Does.Contain("우유 성장 다양성"));
        }

        [Test]
        public void LevelThirtyTwoGateAcceptsThreeMilksAndFourStableStatuses()
        {
            var tama = CreateTama(31);
            tama.stats.affection = 55;
            tama.stats.sleepiness = 90;
            var state = new LateLevelGrowthSaveData();
            state.BeginLevel(31, 300);

            var result = new LateLevelGrowthSystem().TryAdvance(tama, state, Growth(3, 2));

            Assert.That(result.Outcome, Is.EqualTo(LateLevelGrowthOutcome.LevelAdvanced));
            Assert.That(tama.level, Is.EqualTo(32));
            Assert.That(state.trackedLevel, Is.EqualTo(32));
            Assert.That(state.progressUnits, Is.Zero);
            Assert.That(tama.levelProgress, Is.Zero);
        }

        [Test]
        public void FinalLevelRequiresFiveLevelThreeMilksAndAllStableStatuses()
        {
            var tama = CreateTama(32);
            tama.stats.affection = 75;
            var state = new LateLevelGrowthSaveData();
            state.BeginLevel(32, 500);
            var system = new LateLevelGrowthSystem();

            tama.stats.cleanliness = 20;
            var blocked = system.TryAdvance(tama, state, Growth(5, 3));
            Assert.That(blocked.Outcome, Is.EqualTo(LateLevelGrowthOutcome.GateBlocked));
            Assert.That(blocked.GateStatus.StableStatusCount, Is.EqualTo(4));

            tama.stats.cleanliness = 90;
            var completed = system.TryAdvance(tama, state, Growth(5, 3));
            Assert.That(completed.Outcome, Is.EqualTo(LateLevelGrowthOutcome.ReachedFinalLevel));
            Assert.That(tama.level, Is.EqualTo(33));
            Assert.That(tama.levelProgress, Is.Zero);
            Assert.That(state.trackedLevel, Is.EqualTo(33));
            Assert.That(state.progressUnits, Is.Zero);
        }

        [Test]
        public void MilkDiversityCountsDistinctMainMilkIdsAndRepairsSavedLevelsFromPoints()
        {
            var entries = new List<MilkGrowthSaveEntry>
            {
                new MilkGrowthSaveEntry { milkId = MilkCatalog.BasicMilkId, growthLevel = 3 },
                new MilkGrowthSaveEntry { milkId = MilkCatalog.BasicMilkId, growthLevel = 5 },
                new MilkGrowthSaveEntry { milkId = MilkCatalog.WarmMilkId, growthPoints = 20 },
                new MilkGrowthSaveEntry { milkId = MilkCatalog.StarMilkId, growthLevel = 5 },
                new MilkGrowthSaveEntry { milkId = "unknown", growthLevel = 5 },
                null
            };

            var count = LateLevelGrowthSystem.CountQualifyingMainMilks(entries, 3);

            Assert.That(count, Is.EqualTo(2));
        }

        private static CheeseTamaModel CreateTama(int level)
        {
            var tama = new CheeseTamaModel
            {
                level = level,
                maxLevel = 33,
                isHatched = true,
                form = "soft_cheesetama",
                evolutionId = EvolutionSystem.CreamEvolutionId
            };
            tama.stats.hunger = 80;
            tama.stats.mood = 70;
            tama.stats.cleanliness = 90;
            tama.stats.sleepiness = 20;
            tama.stats.health = 100;
            return tama;
        }

        private static List<MilkGrowthSaveEntry> Growth(int count, int level)
        {
            var entries = new List<MilkGrowthSaveEntry>();
            for (var index = 0; index < count; index += 1)
            {
                entries.Add(new MilkGrowthSaveEntry
                {
                    milkId = MilkCatalog.MainMilks[index].id,
                    growthLevel = level,
                    growthPoints = 0
                });
            }

            return entries;
        }

        private static void AssertRequirement(
            int currentLevel,
            int targetLevel,
            int progress,
            int affection,
            int milkCount,
            int milkLevel,
            int stableStatuses)
        {
            Assert.That(
                LateLevelGrowthCatalog.TryGetForCurrentLevel(currentLevel, out var requirement),
                Is.True);
            Assert.That(requirement.TargetLevel, Is.EqualTo(targetLevel));
            Assert.That(requirement.RequiredProgressUnits, Is.EqualTo(progress));
            Assert.That(requirement.MinimumAffection, Is.EqualTo(affection));
            Assert.That(requirement.MinimumMilkTypeCount, Is.EqualTo(milkCount));
            Assert.That(requirement.MinimumMilkGrowthLevel, Is.EqualTo(milkLevel));
            Assert.That(requirement.MinimumStableStatusCount, Is.EqualTo(stableStatuses));
        }
    }
}
