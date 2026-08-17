using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public sealed class StarRouteProgress
    {
        public StarRouteProgress(
            int level,
            int maximumLevel,
            int completedMilkCount,
            int requiredMilkCount,
            bool unlocked,
            string nextGoal)
        {
            this.level = Math.Max(1, level);
            this.maximumLevel = Math.Max(1, maximumLevel);
            this.completedMilkCount = Math.Max(0, completedMilkCount);
            this.requiredMilkCount = Math.Max(0, requiredMilkCount);
            this.unlocked = unlocked;
            this.nextGoal = nextGoal ?? string.Empty;
        }

        public int level { get; }
        public int maximumLevel { get; }
        public int completedMilkCount { get; }
        public int requiredMilkCount { get; }
        public bool unlocked { get; }
        public string nextGoal { get; }
    }

    public static class StarRouteSystem
    {
        public static StarRouteProgress Evaluate(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth)
        {
            var level = Math.Max(1, tama?.level ?? 1);
            var maximumLevel = Math.Max(UnlockSystem.MaxLevel, tama?.maxLevel ?? UnlockSystem.MaxLevel);
            var completedMilkCount = CountCompletedMainMilks(milkGrowth);
            var requiredMilkCount = MilkCatalog.MainMilks.Length;
            var unlocked = level >= UnlockSystem.MaxLevel
                && requiredMilkCount > 0
                && completedMilkCount >= requiredMilkCount;

            string nextGoal;
            if (unlocked)
            {
                nextGoal = "별빛 우유와 별빛 알이 열렸어요. 새로운 숙성의 길을 확인해 보세요.";
            }
            else if (level < UnlockSystem.MaxLevel)
            {
                nextGoal = $"치즈타마를 Lv.{UnlockSystem.MaxLevel}까지 키워 주세요. 현재 Lv.{level}.";
            }
            else
            {
                nextGoal = $"주요 우유를 모두 Lv.{MilkCatalog.MainMilkMaxGrowthLevel}로 성장시켜 주세요. "
                    + $"현재 {completedMilkCount}/{requiredMilkCount}.";
            }

            return new StarRouteProgress(
                level,
                maximumLevel,
                completedMilkCount,
                requiredMilkCount,
                unlocked,
                nextGoal);
        }

        private static int CountCompletedMainMilks(IList<MilkGrowthSaveEntry> milkGrowth)
        {
            if (milkGrowth == null)
            {
                return 0;
            }

            var completed = 0;
            foreach (var milk in MilkCatalog.MainMilks)
            {
                if (milk == null)
                {
                    continue;
                }

                for (var index = 0; index < milkGrowth.Count; index += 1)
                {
                    var entry = milkGrowth[index];
                    if (entry != null
                        && string.Equals(entry.milkId, milk.id, StringComparison.Ordinal)
                        && entry.growthLevel >= MilkCatalog.MainMilkMaxGrowthLevel)
                    {
                        completed += 1;
                        break;
                    }
                }
            }

            return completed;
        }
    }
}
