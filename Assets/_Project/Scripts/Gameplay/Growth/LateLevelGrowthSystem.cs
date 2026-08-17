using System;
using System.Collections.Generic;
using System.Text;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public sealed class LateLevelGrowthRequirement
    {
        public LateLevelGrowthRequirement(
            int currentLevel,
            int targetLevel,
            int requiredProgressUnits,
            int minimumAffection,
            int minimumMilkTypeCount,
            int minimumMilkGrowthLevel,
            int minimumStableStatusCount)
        {
            CurrentLevel = currentLevel;
            TargetLevel = targetLevel;
            RequiredProgressUnits = Math.Max(1, requiredProgressUnits);
            MinimumAffection = Math.Max(0, minimumAffection);
            MinimumMilkTypeCount = Math.Max(0, minimumMilkTypeCount);
            MinimumMilkGrowthLevel = Math.Max(0, minimumMilkGrowthLevel);
            MinimumStableStatusCount = Math.Max(0, minimumStableStatusCount);
        }

        public int CurrentLevel { get; }
        public int TargetLevel { get; }
        public int RequiredProgressUnits { get; }
        public int MinimumAffection { get; }
        public int MinimumMilkTypeCount { get; }
        public int MinimumMilkGrowthLevel { get; }
        public int MinimumStableStatusCount { get; }
    }

    /// <summary>
    /// Lv.30 is included because its next transition reaches the design's first
    /// extreme-maturation level, Lv.31. Levels 1-30 keep the existing 100-unit rule.
    /// </summary>
    public static class LateLevelGrowthCatalog
    {
        public const int FirstTrackedLevel = 30;
        public const int FinalLevel = 33;

        private static readonly LateLevelGrowthRequirement[] Requirements =
        {
            // The Lv.31 gate is progress-only, matching the design's "greatly increased XP" rule.
            new LateLevelGrowthRequirement(30, 31, 200, 0, 0, 0, 0),
            // Lv.32 combines affection, milk-growth variety, and balanced state care.
            new LateLevelGrowthRequirement(31, 32, 300, 55, 3, 2, 4),
            // Lv.33 is the final-form threshold, but does not require all milks at Lv.5;
            // that stricter condition remains reserved for the later star route.
            new LateLevelGrowthRequirement(32, 33, 500, 75, 5, 3, 5)
        };

        public static IReadOnlyList<LateLevelGrowthRequirement> All => Requirements;

        public static bool TryGetForCurrentLevel(
            int currentLevel,
            out LateLevelGrowthRequirement requirement)
        {
            for (var index = 0; index < Requirements.Length; index += 1)
            {
                if (Requirements[index].CurrentLevel == currentLevel)
                {
                    requirement = Requirements[index];
                    return true;
                }
            }

            requirement = null;
            return false;
        }
    }

    public readonly struct LateLevelGateStatus
    {
        public LateLevelGateStatus(
            LateLevelGrowthRequirement requirement,
            int affection,
            int qualifyingMilkTypeCount,
            int stableStatusCount)
        {
            Requirement = requirement;
            Affection = Math.Max(0, Math.Min(100, affection));
            QualifyingMilkTypeCount = Math.Max(0, qualifyingMilkTypeCount);
            StableStatusCount = Math.Max(0, stableStatusCount);
        }

        public LateLevelGrowthRequirement Requirement { get; }
        public int Affection { get; }
        public int QualifyingMilkTypeCount { get; }
        public int StableStatusCount { get; }
        public bool HasRequirement => Requirement != null;
        public bool AffectionMet => HasRequirement && Affection >= Requirement.MinimumAffection;
        public bool MilkGrowthDiversityMet => HasRequirement
            && QualifyingMilkTypeCount >= Requirement.MinimumMilkTypeCount;
        public bool StableStatusDiversityMet => HasRequirement
            && StableStatusCount >= Requirement.MinimumStableStatusCount;
        public bool IsSatisfied => HasRequirement
            && AffectionMet
            && MilkGrowthDiversityMet
            && StableStatusDiversityMet;

        public string BuildMissingRequirementsMessage()
        {
            if (!HasRequirement || IsSatisfied)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendMissing(
                builder,
                AffectionMet,
                $"애정 {Affection}/{Requirement.MinimumAffection}");
            AppendMissing(
                builder,
                MilkGrowthDiversityMet,
                $"우유 성장 다양성 {QualifyingMilkTypeCount}/{Requirement.MinimumMilkTypeCount} (Lv.{Requirement.MinimumMilkGrowthLevel} 이상)");
            AppendMissing(
                builder,
                StableStatusDiversityMet,
                $"안정 상태 {StableStatusCount}/{Requirement.MinimumStableStatusCount}");
            return builder.ToString();
        }

        private static void AppendMissing(StringBuilder builder, bool met, string text)
        {
            if (met)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(" · ");
            }

            builder.Append(text);
        }
    }

    public enum LateLevelGrowthOutcome
    {
        NotApplicable = 0,
        MissingState = 1,
        UnsupportedSaveVersion = 2,
        InvalidProgress = 3,
        NoChange = 4,
        Progressed = 5,
        GateBlocked = 6,
        LevelAdvanced = 7,
        ReachedFinalLevel = 8
    }

    public readonly struct LateLevelGrowthResult
    {
        public LateLevelGrowthResult(
            LateLevelGrowthOutcome outcome,
            int previousLevel,
            int currentLevel,
            int acceptedProgressUnits,
            int carriedProgressUnits,
            int unusedProgressUnits,
            int progressUnits,
            int requiredProgressUnits,
            int normalizedPercent,
            LateLevelGateStatus gateStatus)
        {
            Outcome = outcome;
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
            AcceptedProgressUnits = Math.Max(0, acceptedProgressUnits);
            CarriedProgressUnits = Math.Max(0, carriedProgressUnits);
            UnusedProgressUnits = Math.Max(0, unusedProgressUnits);
            ProgressUnits = Math.Max(0, progressUnits);
            RequiredProgressUnits = Math.Max(0, requiredProgressUnits);
            NormalizedPercent = Math.Max(0, Math.Min(100, normalizedPercent));
            GateStatus = gateStatus;
        }

        public LateLevelGrowthOutcome Outcome { get; }
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public int AcceptedProgressUnits { get; }
        public int CarriedProgressUnits { get; }
        public int UnusedProgressUnits { get; }
        public int ProgressUnits { get; }
        public int RequiredProgressUnits { get; }
        public int NormalizedPercent { get; }
        public LateLevelGateStatus GateStatus { get; }
        public bool LevelChanged => CurrentLevel > PreviousLevel;
        public bool IsBlocked => Outcome == LateLevelGrowthOutcome.GateBlocked;
    }

    public sealed class LateLevelGrowthSystem
    {
        public const int StableHungerMinimum = 55;
        public const int StableMoodMinimum = 55;
        public const int StableCleanlinessMinimum = 55;
        public const int StableHealthMinimum = 65;
        public const int StableSleepinessMaximum = 45;

        public LateLevelGateStatus EvaluateGate(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth)
        {
            if (tama == null
                || !LateLevelGrowthCatalog.TryGetForCurrentLevel(tama.level, out var requirement))
            {
                return default;
            }

            return new LateLevelGateStatus(
                requirement,
                tama.stats?.affection ?? 0,
                CountQualifyingMainMilks(
                    milkGrowth,
                    requirement.MinimumMilkGrowthLevel),
                CountStableStatuses(tama));
        }

        public LateLevelGrowthResult TryAdvance(
            CheeseTamaModel tama,
            LateLevelGrowthSaveData state,
            IList<MilkGrowthSaveEntry> milkGrowth)
        {
            return AddProgress(tama, state, milkGrowth, 0);
        }

        /// <summary>
        /// Adds late-level units and advances at most one level per call. Overflow
        /// is carried into the next tracked level but cannot skip its gate. Any
        /// amount beyond the next level's capacity is returned as unused units.
        /// </summary>
        public LateLevelGrowthResult AddProgress(
            CheeseTamaModel tama,
            LateLevelGrowthSaveData state,
            IList<MilkGrowthSaveEntry> milkGrowth,
            int amount)
        {
            var previousLevel = tama?.level ?? 0;
            if (tama == null
                || !LateLevelGrowthCatalog.TryGetForCurrentLevel(tama.level, out var requirement))
            {
                return Result(
                    LateLevelGrowthOutcome.NotApplicable,
                    previousLevel,
                    tama?.level ?? 0,
                    state,
                    null,
                    default);
            }

            if (state == null)
            {
                return Result(
                    LateLevelGrowthOutcome.MissingState,
                    previousLevel,
                    tama.level,
                    null,
                    requirement,
                    default);
            }

            if (amount < 0)
            {
                return Result(
                    LateLevelGrowthOutcome.InvalidProgress,
                    previousLevel,
                    tama.level,
                    state,
                    requirement,
                    EvaluateGate(tama, milkGrowth));
            }

            var migration = LateLevelProgressMigration.EnsureCurrent(tama, state);
            if (migration.Status == LateLevelProgressMigrationStatus.UnsupportedFutureVersion)
            {
                return Result(
                    LateLevelGrowthOutcome.UnsupportedSaveVersion,
                    previousLevel,
                    tama.level,
                    state,
                    requirement,
                    EvaluateGate(tama, milkGrowth));
            }

            var before = Math.Max(0, Math.Min(requirement.RequiredProgressUnits, state.progressUnits));
            var availableCapacity = requirement.RequiredProgressUnits - before;
            var accepted = Math.Min(amount, availableCapacity);
            state.progressUnits = before + accepted;
            var overflow = amount - accepted;
            LateLevelProgressMigration.SyncCompatibilityPercent(tama, state);

            var gateStatus = EvaluateGate(tama, milkGrowth);
            if (state.progressUnits < requirement.RequiredProgressUnits)
            {
                return Result(
                    accepted > 0 ? LateLevelGrowthOutcome.Progressed : LateLevelGrowthOutcome.NoChange,
                    previousLevel,
                    tama.level,
                    state,
                    requirement,
                    gateStatus,
                    accepted,
                    0,
                    overflow);
            }

            if (!gateStatus.IsSatisfied)
            {
                return Result(
                    LateLevelGrowthOutcome.GateBlocked,
                    previousLevel,
                    tama.level,
                    state,
                    requirement,
                    gateStatus,
                    accepted,
                    0,
                    overflow);
            }

            tama.level = requirement.TargetLevel;
            if (!LateLevelGrowthCatalog.TryGetForCurrentLevel(tama.level, out var nextRequirement))
            {
                state.BeginLevel(tama.level, 0);
                tama.levelProgress = 0;
                return Result(
                    LateLevelGrowthOutcome.ReachedFinalLevel,
                    previousLevel,
                    tama.level,
                    state,
                    null,
                    gateStatus,
                    accepted,
                    0,
                    overflow);
            }

            var carried = Math.Min(overflow, nextRequirement.RequiredProgressUnits);
            var unused = overflow - carried;
            state.BeginLevel(tama.level, carried);
            LateLevelProgressMigration.SyncCompatibilityPercent(tama, state);
            return Result(
                LateLevelGrowthOutcome.LevelAdvanced,
                previousLevel,
                tama.level,
                state,
                nextRequirement,
                gateStatus,
                accepted,
                carried,
                unused);
        }

        public static int CountStableStatuses(CheeseTamaModel tama)
        {
            var stats = tama?.stats;
            if (stats == null)
            {
                return 0;
            }

            var count = 0;
            count += stats.hunger >= StableHungerMinimum ? 1 : 0;
            count += stats.mood >= StableMoodMinimum ? 1 : 0;
            count += stats.cleanliness >= StableCleanlinessMinimum ? 1 : 0;
            count += stats.health >= StableHealthMinimum ? 1 : 0;
            count += stats.sleepiness <= StableSleepinessMaximum ? 1 : 0;
            return count;
        }

        public static int CountQualifyingMainMilks(
            IList<MilkGrowthSaveEntry> milkGrowth,
            int minimumGrowthLevel)
        {
            if (minimumGrowthLevel <= 0)
            {
                // A progress-only transition has no milk gate, so expose zero
                // qualifying types instead of a misleading synthetic full count.
                return 0;
            }

            if (milkGrowth == null)
            {
                return 0;
            }

            var qualifying = 0;
            for (var milkIndex = 0; milkIndex < MilkCatalog.MainMilks.Length; milkIndex += 1)
            {
                var milk = MilkCatalog.MainMilks[milkIndex];
                var bestLevel = 0;
                for (var entryIndex = 0; entryIndex < milkGrowth.Count; entryIndex += 1)
                {
                    var entry = milkGrowth[entryIndex];
                    if (entry == null
                        || !string.Equals(entry.milkId, milk.id, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    bestLevel = Math.Max(bestLevel, ResolveGrowthLevel(entry));
                }

                if (bestLevel >= minimumGrowthLevel)
                {
                    qualifying += 1;
                }
            }

            return qualifying;
        }

        private static int ResolveGrowthLevel(MilkGrowthSaveEntry entry)
        {
            if (entry == null)
            {
                return 0;
            }

            var savedLevel = Math.Max(
                0,
                Math.Min(MilkCatalog.MainMilkMaxGrowthLevel, entry.growthLevel));
            var levelFromPoints = entry.growthPoints <= 0
                ? 0
                : Math.Min(
                    MilkCatalog.MainMilkMaxGrowthLevel,
                    Math.Max(0, entry.growthPoints) / 10 + 1);
            return Math.Max(savedLevel, levelFromPoints);
        }

        private static LateLevelGrowthResult Result(
            LateLevelGrowthOutcome outcome,
            int previousLevel,
            int currentLevel,
            LateLevelGrowthSaveData state,
            LateLevelGrowthRequirement requirement,
            LateLevelGateStatus gateStatus,
            int accepted = 0,
            int carried = 0,
            int unused = 0)
        {
            var required = requirement?.RequiredProgressUnits ?? 0;
            var progress = state?.progressUnits ?? 0;
            var percent = required > 0
                ? LateLevelProgressMigration.GetDisplayPercent(progress, required)
                : 0;
            return new LateLevelGrowthResult(
                outcome,
                previousLevel,
                currentLevel,
                accepted,
                carried,
                unused,
                progress,
                required,
                percent,
                gateStatus);
        }
    }
}
