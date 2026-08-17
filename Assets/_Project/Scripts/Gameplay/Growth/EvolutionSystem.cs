using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Data;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public sealed class NormalEvolutionProfile
    {
        public NormalEvolutionProfile(
            string id,
            string displayName,
            string description,
            string tendencyHint,
            string primaryMilkId)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            TendencyHint = tendencyHint;
            PrimaryMilkId = primaryMilkId;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string TendencyHint { get; }
        public string PrimaryMilkId { get; }
    }

    public readonly struct NormalEvolutionResult
    {
        public NormalEvolutionResult(NormalEvolutionProfile profile, int score)
        {
            Profile = profile;
            Score = Math.Max(0, score);
        }

        public NormalEvolutionProfile Profile { get; }
        public int Score { get; }
        public bool HasEvolution => Profile != null;
        public string EvolutionId => Profile?.Id ?? string.Empty;
        public string DisplayName => Profile?.DisplayName ?? string.Empty;
        public string Description => Profile?.Description ?? string.Empty;
        public string TendencyHint => Profile?.TendencyHint ?? string.Empty;
    }

    public sealed class EvolutionSystem
    {
        public const int NormalEvolutionLevel = 21;

        public const string CreamEvolutionId = "cream_cheesetama";
        public const string CheddarEvolutionId = "cheddar_cheesetama";
        public const string RicottaEvolutionId = "ricotta_cheesetama";
        public const string MozzarellaEvolutionId = "mozzarella_cheesetama";
        public const string BlueEvolutionId = "blue_cheesetama";
        public const string CoffeeEvolutionId = "coffee_cheesetama";

        private const string WarmMilkSoupId = "recipe_warm_milk_soup";
        private const string NuttyCheeseCrackerId = "recipe_nutty_cheese_cracker";
        private const string FermentedYogurtBowlId = "recipe_fermented_yogurt_bowl";
        private const string CoffeeMilkJellyId = "recipe_coffee_milk_jelly";
        private const int MilkLevelScore = 20;
        private const int MilkPointDivisor = 5;
        private const int MaximumMilkPointsForScoring = 100;
        private const int PreferredMilkBonus = 12;
        private const int PreferredIngredientBonus = 20;

        // Array order is the stable tie-break rule. It must not depend on save-list order.
        private static readonly NormalEvolutionProfile[] NormalEvolutionProfiles =
        {
            new NormalEvolutionProfile(
                CreamEvolutionId,
                "크림치즈타마",
                "다정한 돌봄을 닮아 부드럽고 온순하게 자란 치즈타마예요.",
                "따뜻한 우유와 애정 어린 돌봄의 기운이 느껴져요.",
                MilkCatalog.WarmMilkId),
            new NormalEvolutionProfile(
                CheddarEvolutionId,
                "체다치즈타마",
                "함께 논 추억을 품고 활발하고 밝게 자란 치즈타마예요.",
                "고소한 우유와 신나는 놀이의 기운이 느껴져요.",
                MilkCatalog.NuttyMilkId),
            new NormalEvolutionProfile(
                RicottaEvolutionId,
                "리코타치즈타마",
                "꾸준하고 편안한 돌봄 속에서 담백하고 순하게 자란 치즈타마예요.",
                "기본 우유와 정성스러운 요리의 기운이 느껴져요.",
                MilkCatalog.BasicMilkId),
            new NormalEvolutionProfile(
                MozzarellaEvolutionId,
                "모짜렐라치즈타마",
                "균형 잡힌 하루를 보내며 말랑하고 건강하게 자란 치즈타마예요.",
                "고른 상태 관리와 균형 잡힌 돌봄의 기운이 느껴져요.",
                MilkCatalog.BasicMilkId),
            new NormalEvolutionProfile(
                BlueEvolutionId,
                "블루치즈타마",
                "충분히 숙성되어 독특하고 섬세한 개성을 지닌 치즈타마예요.",
                "발효우유와 깊은 숙성의 기운이 느껴져요.",
                MilkCatalog.FermentedMilkId),
            new NormalEvolutionProfile(
                CoffeeEvolutionId,
                "커피치즈타마",
                "조용한 시간에 집중하며 차분하게 자란 치즈타마예요.",
                "커피우유와 늦은 시간 돌봄의 기운이 느껴져요.",
                MilkCatalog.CoffeeMilkId)
        };

        public static IReadOnlyList<NormalEvolutionProfile> NormalEvolutions => NormalEvolutionProfiles;

        public bool CanUseEvolution(CheeseTamaModel tama, UnlockSaveData unlocks, EvolutionDefinition evolution)
        {
            if (tama == null || unlocks == null || evolution == null || evolution.requirements == null)
            {
                return false;
            }

            var requirements = evolution.requirements;
            return tama.level >= requirements.cheeseTamaLevel
                && (!requirements.starEggUnlocked || unlocks.starEggUnlocked)
                && (!requirements.starMilkUnlocked || unlocks.starMilkUnlocked);
        }

        public bool TryApplyEvolution(CheeseTamaModel tama, UnlockSaveData unlocks, EvolutionDefinition evolution)
        {
            if (!CanUseEvolution(tama, unlocks, evolution))
            {
                return false;
            }

            tama.evolutionId = evolution.id;
            tama.form = evolution.id;
            return true;
        }

        public bool CanResolveNormalEvolution(CheeseTamaModel tama)
        {
            return tama != null
                && tama.level >= NormalEvolutionLevel
                && string.IsNullOrWhiteSpace(tama.evolutionId);
        }

        /// <summary>
        /// Evaluates the current tendency without applying it or enforcing the level gate.
        /// This is suitable for indirect preview text before level 21.
        /// </summary>
        public NormalEvolutionResult EvaluateNormalEvolution(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth,
            CareHistorySaveData careHistory)
        {
            if (tama == null)
            {
                return default;
            }

            var scores = new int[NormalEvolutionProfiles.Length];
            scores[0] = ScoreCream(tama, milkGrowth, careHistory);
            scores[1] = ScoreCheddar(tama, milkGrowth, careHistory);
            scores[2] = ScoreRicotta(tama, milkGrowth, careHistory);
            scores[3] = ScoreMozzarella(tama, milkGrowth);
            scores[4] = ScoreBlue(tama, milkGrowth, careHistory);
            scores[5] = ScoreCoffee(tama, milkGrowth, careHistory);

            var winnerIndex = 0;
            for (var i = 1; i < scores.Length; i++)
            {
                // A strict comparison intentionally preserves catalog order on ties.
                if (scores[i] > scores[winnerIndex])
                {
                    winnerIndex = i;
                }
            }

            return new NormalEvolutionResult(NormalEvolutionProfiles[winnerIndex], scores[winnerIndex]);
        }

        public NormalEvolutionResult ResolveNormalEvolution(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth,
            CareHistorySaveData careHistory)
        {
            return CanResolveNormalEvolution(tama)
                ? EvaluateNormalEvolution(tama, milkGrowth, careHistory)
                : default;
        }

        public bool TryApplyNormalEvolution(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth,
            CareHistorySaveData careHistory,
            out NormalEvolutionResult result)
        {
            result = ResolveNormalEvolution(tama, milkGrowth, careHistory);
            if (!result.HasEvolution)
            {
                return false;
            }

            tama.evolutionId = result.EvolutionId;
            tama.form = result.EvolutionId;
            return true;
        }

        public static NormalEvolutionProfile FindNormalEvolution(string evolutionId)
        {
            if (string.IsNullOrWhiteSpace(evolutionId))
            {
                return null;
            }

            for (var i = 0; i < NormalEvolutionProfiles.Length; i++)
            {
                if (string.Equals(NormalEvolutionProfiles[i].Id, evolutionId, StringComparison.Ordinal))
                {
                    return NormalEvolutionProfiles[i];
                }
            }

            return null;
        }

        private static int ScoreCream(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth,
            CareHistorySaveData history)
        {
            return GetMilkScore(milkGrowth, MilkCatalog.WarmMilkId)
                + GetMilkScore(milkGrowth, MilkCatalog.RichMilkId) / 3
                + GetPreferredMilkBonus(tama, MilkCatalog.WarmMilkId)
                + GetPreferredIngredientBonus(tama, WarmMilkSoupId)
                + ClampStat(tama.stats?.affection ?? 0) / 4
                + ClampCount(history?.petSessions ?? 0, 12) * 2;
        }

        private static int ScoreCheddar(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth,
            CareHistorySaveData history)
        {
            return GetMilkScore(milkGrowth, MilkCatalog.NuttyMilkId)
                + GetPreferredMilkBonus(tama, MilkCatalog.NuttyMilkId)
                + GetPreferredIngredientBonus(tama, NuttyCheeseCrackerId)
                + ClampStat(tama.stats?.mood ?? 0) / 5
                + ClampCount(history?.playSessions ?? 0, 15) * 2;
        }

        private static int ScoreRicotta(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth,
            CareHistorySaveData history)
        {
            var stats = tama.stats;
            var restfulness = 100 - ClampStat(stats?.sleepiness ?? 100);
            var stableCare = (ClampStat(stats?.health ?? 0)
                + ClampStat(stats?.cleanliness ?? 0)
                + restfulness) / 15;

            return GetMilkScore(milkGrowth, MilkCatalog.BasicMilkId)
                + GetPreferredMilkBonus(tama, MilkCatalog.BasicMilkId)
                + GetPreferredIngredientBonus(tama, FermentedYogurtBowlId)
                + stableCare
                + ClampCount(history?.cookings ?? 0, 10) * 2
                + ClampCount(history?.snacksFed ?? 0, 10);
        }

        private static int ScoreMozzarella(CheeseTamaModel tama, IList<MilkGrowthSaveEntry> milkGrowth)
        {
            return GetMilkScore(milkGrowth, MilkCatalog.BasicMilkId) * 4 / 5
                + GetMilkScore(milkGrowth, MilkCatalog.ColdMilkId) / 4
                + GetPreferredMilkBonus(tama, MilkCatalog.BasicMilkId)
                + CalculateBalancedCareScore(tama) / 2;
        }

        private static int ScoreBlue(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth,
            CareHistorySaveData history)
        {
            return GetMilkScore(milkGrowth, MilkCatalog.FermentedMilkId)
                + GetPreferredMilkBonus(tama, MilkCatalog.FermentedMilkId)
                + ClampStat(tama.stats?.maturation ?? 0) / 3
                + ClampCount(history?.cleanings ?? 0, 15) * 2;
        }

        private static int ScoreCoffee(
            CheeseTamaModel tama,
            IList<MilkGrowthSaveEntry> milkGrowth,
            CareHistorySaveData history)
        {
            var restfulness = 100 - ClampStat(tama.stats?.sleepiness ?? 100);
            return GetMilkScore(milkGrowth, MilkCatalog.CoffeeMilkId)
                + GetPreferredMilkBonus(tama, MilkCatalog.CoffeeMilkId)
                + GetPreferredIngredientBonus(tama, CoffeeMilkJellyId)
                + restfulness / 5
                + ClampCount(history?.rests ?? 0, 10)
                + ClampCount(history?.waitHours ?? 0, 10)
                + GetNightCareBonus(history?.lastCareActionAtIso);
        }

        private static int GetMilkScore(IList<MilkGrowthSaveEntry> entries, string milkId)
        {
            if (entries == null || string.IsNullOrWhiteSpace(milkId))
            {
                return 0;
            }

            var bestLevel = 0;
            var bestPoints = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || !string.Equals(entry.milkId, milkId, StringComparison.Ordinal))
                {
                    continue;
                }

                bestPoints = Math.Max(bestPoints, ClampCount(entry.growthPoints, MaximumMilkPointsForScoring));
                var levelFromPoints = entry.growthPoints <= 0
                    ? 0
                    : Math.Min(MilkCatalog.MainMilkMaxGrowthLevel, entry.growthPoints / 10 + 1);
                bestLevel = Math.Max(bestLevel, Math.Max(
                    Math.Min(MilkCatalog.MainMilkMaxGrowthLevel, Math.Max(0, entry.growthLevel)),
                    levelFromPoints));
            }

            return bestLevel * MilkLevelScore + bestPoints / MilkPointDivisor;
        }

        private static int GetPreferredMilkBonus(CheeseTamaModel tama, string milkId)
        {
            return string.Equals(tama.growthHistory?.mostUsedMilkId, milkId, StringComparison.Ordinal)
                ? PreferredMilkBonus
                : 0;
        }

        private static int GetPreferredIngredientBonus(CheeseTamaModel tama, string ingredientId)
        {
            return string.Equals(tama.growthHistory?.mostUsedIngredientId, ingredientId, StringComparison.Ordinal)
                ? PreferredIngredientBonus
                : 0;
        }

        private static int CalculateBalancedCareScore(CheeseTamaModel tama)
        {
            var stats = tama.stats;
            if (stats == null)
            {
                return 0;
            }

            var totalDistance = Math.Abs(ClampStat(stats.hunger) - 80)
                + Math.Abs(ClampStat(stats.mood) - 75)
                + Math.Abs(ClampStat(stats.cleanliness) - 85)
                + Math.Abs((100 - ClampStat(stats.sleepiness)) - 80)
                + Math.Abs(ClampStat(stats.health) - 90);
            return Math.Max(0, 100 - totalDistance / 5);
        }

        private static int GetNightCareBonus(string lastCareActionAtIso)
        {
            if (!DateTimeOffset.TryParse(
                    lastCareActionAtIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var lastCareAt))
            {
                return 0;
            }

            return lastCareAt.Hour >= 21 || lastCareAt.Hour < 6 ? 16 : 0;
        }

        private static int ClampStat(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }

        private static int ClampCount(int value, int maximum)
        {
            return Math.Max(0, Math.Min(maximum, value));
        }
    }
}
