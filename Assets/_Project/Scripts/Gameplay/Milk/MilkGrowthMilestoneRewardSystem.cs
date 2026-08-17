using System;
using System.Collections.Generic;
using CheeseTama.Save;
using CheeseTama.Gameplay.Snacks;

namespace CheeseTama.Gameplay.Milk
{
    public sealed class MilkGrowthMilestoneRewardResult
    {
        public static readonly MilkGrowthMilestoneRewardResult None = new MilkGrowthMilestoneRewardResult(
            string.Empty,
            0,
            0,
            0,
            0,
            Array.Empty<string>(),
            string.Empty);

        public MilkGrowthMilestoneRewardResult(
            string milkId,
            int reachedLevel,
            int milkCoins,
            int milkDrops,
            int collectionFragments,
            IReadOnlyList<string> claimedKeys,
            string message)
        {
            this.milkId = milkId ?? string.Empty;
            this.reachedLevel = Math.Max(0, reachedLevel);
            this.milkCoins = Math.Max(0, milkCoins);
            this.milkDrops = Math.Max(0, milkDrops);
            this.collectionFragments = Math.Max(0, collectionFragments);
            this.claimedKeys = claimedKeys ?? Array.Empty<string>();
            this.message = message ?? string.Empty;
        }

        public string milkId { get; }
        public int reachedLevel { get; }
        public int milkCoins { get; }
        public int milkDrops { get; }
        public int collectionFragments { get; }
        public IReadOnlyList<string> claimedKeys { get; }
        public string message { get; }
        public bool granted => claimedKeys.Count > 0;
    }

    public static class MilkGrowthMilestoneRewardSystem
    {
        public const int FirstRewardLevel = 2;
        public const int MaximumRewardLevel = 5;

        public static MilkGrowthMilestoneRewardResult ClaimReachedMilestones(
            string milkId,
            int growthLevel,
            IList<string> claimedKeys)
        {
            if (string.IsNullOrWhiteSpace(milkId)
                || claimedKeys == null
                || growthLevel < FirstRewardLevel)
            {
                return MilkGrowthMilestoneRewardResult.None;
            }

            var normalizedLevel = Math.Min(MaximumRewardLevel, Math.Max(0, growthLevel));
            var newlyClaimed = new List<string>();
            var milkCoins = 0;
            var milkDrops = 0;
            var fragments = 0;
            var milestoneMessages = new List<string>();
            for (var level = FirstRewardLevel; level <= normalizedLevel; level += 1)
            {
                var key = BuildClaimKey(milkId, level);
                if (Contains(claimedKeys, key))
                {
                    continue;
                }

                claimedKeys.Add(key);
                newlyClaimed.Add(key);
                switch (level)
                {
                    case 2:
                        milkCoins += 4;
                        milestoneMessages.Add("Lv.2 반응 대사가 열렸어요");
                        break;
                    case 3:
                        milkDrops += 2;
                        milestoneMessages.Add($"Lv.3 요리 힌트: {GetRecipeHint(milkId)}");
                        break;
                    case 4:
                        milkCoins += 8;
                        fragments += 1;
                        milestoneMessages.Add("Lv.4 전용 성장 기록을 발견했어요");
                        break;
                    case 5:
                        milkCoins += 12;
                        milkDrops += 3;
                        fragments += 1;
                        milestoneMessages.Add("Lv.5 완전 성장 기록을 달성했어요");
                        break;
                }
            }

            if (newlyClaimed.Count == 0)
            {
                return MilkGrowthMilestoneRewardResult.None;
            }

            var milk = MilkCatalog.Find(milkId);
            var displayName = milk != null ? milk.displayName : milkId;
            var message = $"{displayName} 성장 보상: {string.Join(" · ", milestoneMessages)}. "
                + $"코인 +{milkCoins}, 우유방울 +{milkDrops}, 도감조각 +{fragments}.";
            return new MilkGrowthMilestoneRewardResult(
                milkId,
                normalizedLevel,
                milkCoins,
                milkDrops,
                fragments,
                newlyClaimed,
                message);
        }

        public static string BuildClaimKey(string milkId, int level)
        {
            return $"{milkId}:growth:{Math.Max(0, level)}";
        }

        public static string BuildEventId(string milkId, int level)
        {
            return $"milk_growth_reward_{milkId}_lv_{Math.Max(0, level)}";
        }

        private static string GetRecipeHint(string milkId)
        {
            var recipeId = milkId switch
            {
                MilkCatalog.ColdMilkId => SnackCatalog.ColdMilkPuddingId,
                MilkCatalog.NuttyMilkId => SnackCatalog.NuttyCheeseCrackerId,
                MilkCatalog.RichMilkId => SnackCatalog.RichMilkRisottoId,
                MilkCatalog.FermentedMilkId => SnackCatalog.FermentedYogurtBowlId,
                MilkCatalog.CoffeeMilkId => SnackCatalog.CoffeeMilkJellyId,
                _ => SnackCatalog.WarmMilkSoupId
            };
            return SnackCatalog.Find(recipeId)?.displayName ?? "새 우유 요리";
        }

        private static bool Contains(IList<string> values, string expected)
        {
            for (var index = 0; index < values.Count; index += 1)
            {
                if (string.Equals(values[index], expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
