using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Growth;

namespace CheeseTama.Gameplay.Guidance
{
    public enum NextActionUrgency
    {
        Urgent = 0,
        Today = 1,
        LongTerm = 2
    }

    public static class NextActionRouteIds
    {
        public const string Care = "milkroom.care";
        public const string MilkGrowth = "milkroom.milk";
    }

    public sealed class NextAction
    {
        internal NextAction(
            string id,
            NextActionUrgency urgency,
            string title,
            int progressPercent,
            string missingCondition,
            string destinationRouteId)
        {
            Id = id ?? string.Empty;
            Urgency = urgency;
            Title = title ?? string.Empty;
            ProgressPercent = Math.Max(0, Math.Min(100, progressPercent));
            MissingCondition = missingCondition ?? string.Empty;
            DestinationRouteId = destinationRouteId ?? string.Empty;
        }

        public string Id { get; }
        public NextActionUrgency Urgency { get; }
        public string Title { get; }
        public int ProgressPercent { get; }
        public string MissingCondition { get; }
        public string DestinationRouteId { get; }
    }

    public sealed class NextActionGoalBoardSnapshot
    {
        internal NextActionGoalBoardSnapshot(
            bool isApplicable,
            int currentLevel,
            int targetLevel,
            int progressPercent,
            IReadOnlyList<NextAction> goals,
            IReadOnlyList<string> missingConditions)
        {
            IsApplicable = isApplicable;
            CurrentLevel = Math.Max(0, currentLevel);
            TargetLevel = Math.Max(0, targetLevel);
            ProgressPercent = Math.Max(0, Math.Min(100, progressPercent));
            Goals = goals ?? Array.Empty<NextAction>();
            MissingConditions = missingConditions ?? Array.Empty<string>();
        }

        public bool IsApplicable { get; }
        public bool IsReadyForLevelUp => IsApplicable && MissingConditions.Count == 0;
        public int CurrentLevel { get; }
        public int TargetLevel { get; }
        public int ProgressPercent { get; }
        public IReadOnlyList<NextAction> Goals { get; }
        public IReadOnlyList<string> MissingConditions { get; }
    }

    /// <summary>
    /// Converts the public late-level growth requirements into at most one goal
    /// for each time horizon. It never changes the supplied growth or save state.
    /// </summary>
    public static class NextActionGoalBoardSystem
    {
        public const int MaximumPublicGoals = 3;

        public static NextActionGoalBoardSnapshot BuildLateLevel(
            int currentLevel,
            int progressUnits,
            int affection,
            int qualifyingMilkTypeCount,
            int stableStatusCount)
        {
            if (!LateLevelGrowthCatalog.TryGetForCurrentLevel(
                    currentLevel,
                    out var requirement))
            {
                return Empty(currentLevel);
            }

            return BuildLateLevel(
                requirement,
                progressUnits,
                new LateLevelGateStatus(
                    requirement,
                    affection,
                    qualifyingMilkTypeCount,
                    stableStatusCount));
        }

        public static NextActionGoalBoardSnapshot BuildLateLevel(
            LateLevelGrowthRequirement requirement,
            int progressUnits,
            LateLevelGateStatus gateStatus)
        {
            if (requirement == null
                || requirement.CurrentLevel < LateLevelGrowthCatalog.FirstTrackedLevel
                || requirement.TargetLevel > LateLevelGrowthCatalog.FinalLevel
                || requirement.TargetLevel <= requirement.CurrentLevel)
            {
                return Empty(requirement?.CurrentLevel ?? 0);
            }

            var progress = ClampProgress(progressUnits, requirement.RequiredProgressUnits);
            var progressPercent = Percent(progress, requirement.RequiredProgressUnits);
            var affectionPercent = requirement.MinimumAffection > 0
                ? Percent(gateStatus.Affection, requirement.MinimumAffection)
                : 100;
            var milkPercent = requirement.MinimumMilkTypeCount > 0
                ? Percent(gateStatus.QualifyingMilkTypeCount, requirement.MinimumMilkTypeCount)
                : 100;
            var stablePercent = requirement.MinimumStableStatusCount > 0
                ? Percent(gateStatus.StableStatusCount, requirement.MinimumStableStatusCount)
                : 100;

            var requiredPercents = new List<int> { progressPercent };
            var missing = new List<string>(4);
            var goals = new List<NextAction>(MaximumPublicGoals);

            var progressMissing = progress < requirement.RequiredProgressUnits;
            var affectionMissing = requirement.MinimumAffection > 0
                && gateStatus.Affection < requirement.MinimumAffection;
            var milkMissing = requirement.MinimumMilkTypeCount > 0
                && gateStatus.QualifyingMilkTypeCount < requirement.MinimumMilkTypeCount;
            var stableMissing = requirement.MinimumStableStatusCount > 0
                && gateStatus.StableStatusCount < requirement.MinimumStableStatusCount;

            var progressCondition =
                $"성장 진행 {progress}/{requirement.RequiredProgressUnits}";
            if (progressMissing)
            {
                missing.Add(progressCondition);
            }

            string affectionCondition = string.Empty;
            if (requirement.MinimumAffection > 0)
            {
                requiredPercents.Add(affectionPercent);
                affectionCondition =
                    $"애정 {gateStatus.Affection}/{requirement.MinimumAffection}";
                if (affectionMissing)
                {
                    missing.Add(affectionCondition);
                }
            }

            string milkCondition = string.Empty;
            if (requirement.MinimumMilkTypeCount > 0)
            {
                requiredPercents.Add(milkPercent);
                milkCondition =
                    $"우유 성장 다양성 {gateStatus.QualifyingMilkTypeCount}/{requirement.MinimumMilkTypeCount} "
                    + $"(Lv.{requirement.MinimumMilkGrowthLevel} 이상)";
                if (milkMissing)
                {
                    missing.Add(milkCondition);
                }
            }

            string stableCondition = string.Empty;
            if (requirement.MinimumStableStatusCount > 0)
            {
                requiredPercents.Add(stablePercent);
                stableCondition =
                    $"안정 상태 {gateStatus.StableStatusCount}/{requirement.MinimumStableStatusCount}";
                if (stableMissing)
                {
                    missing.Add(stableCondition);
                    goals.Add(new NextAction(
                        "late_growth_stability",
                        NextActionUrgency.Urgent,
                        "치즈타마 상태 안정시키기",
                        stablePercent,
                        stableCondition,
                        NextActionRouteIds.Care));
                }
            }

            if (progressMissing || affectionMissing)
            {
                var todayConditions = new List<string>(2);
                var todayPercent = 100;
                if (progressMissing)
                {
                    todayConditions.Add(progressCondition);
                    todayPercent = Math.Min(todayPercent, progressPercent);
                }

                if (affectionMissing)
                {
                    todayConditions.Add(affectionCondition);
                    todayPercent = Math.Min(todayPercent, affectionPercent);
                }

                var title = progressMissing && affectionMissing
                    ? "오늘의 성장 돌봄 이어가기"
                    : progressMissing
                        ? "성장 경험 쌓기"
                        : "애정 쌓기";
                goals.Add(new NextAction(
                    "late_growth_daily_care",
                    NextActionUrgency.Today,
                    title,
                    todayPercent,
                    string.Join(" · ", todayConditions),
                    NextActionRouteIds.Care));
            }

            if (milkMissing)
            {
                goals.Add(new NextAction(
                    "late_growth_milk_diversity",
                    NextActionUrgency.LongTerm,
                    "우유 성장 다양성 넓히기",
                    milkPercent,
                    milkCondition,
                    NextActionRouteIds.MilkGrowth));
            }

            return new NextActionGoalBoardSnapshot(
                true,
                requirement.CurrentLevel,
                requirement.TargetLevel,
                Average(requiredPercents),
                goals.ToArray(),
                missing.ToArray());
        }

        private static NextActionGoalBoardSnapshot Empty(int currentLevel)
        {
            return new NextActionGoalBoardSnapshot(
                false,
                currentLevel,
                currentLevel,
                0,
                Array.Empty<NextAction>(),
                Array.Empty<string>());
        }

        private static int ClampProgress(int value, int target)
        {
            return Math.Max(0, Math.Min(Math.Max(0, target), value));
        }

        private static int Percent(int value, int target)
        {
            if (target <= 0)
            {
                return 100;
            }

            var clamped = Math.Max(0, Math.Min(target, value));
            return (int)Math.Min(100L, ((long)clamped * 100L) / target);
        }

        private static int Average(IReadOnlyList<int> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            long total = 0;
            for (var index = 0; index < values.Count; index += 1)
            {
                total += values[index];
            }

            return (int)(total / values.Count);
        }
    }
}
