using System;
using CheeseTama.Gameplay.Snacks;

namespace CheeseTama.Gameplay.Milk
{
    public sealed class MilkBlendIngredientDefinition
    {
        public MilkBlendIngredientDefinition(
            string id,
            string displayName,
            string description)
        {
            this.id = Normalize(id);
            this.displayName = Normalize(displayName);
            this.description = Normalize(description);
        }

        public string id { get; }
        public string displayName { get; }
        public string description { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class MilkBlendRecipeDefinition
    {
        public MilkBlendRecipeDefinition(
            string milkId,
            string ingredientId,
            string resultSnackId)
            : this(
                milkId,
                ingredientId,
                resultSnackId,
                string.Empty,
                0d)
        {
        }

        public MilkBlendRecipeDefinition(
            string milkId,
            string ingredientId,
            string resultSnackId,
            string specialResultSnackId,
            double specialResultChance)
        {
            this.milkId = Normalize(milkId);
            this.ingredientId = Normalize(ingredientId);
            this.resultSnackId = Normalize(resultSnackId);
            this.specialResultSnackId = Normalize(specialResultSnackId);
            this.specialResultChance = NormalizeChance(specialResultChance);
        }

        public string milkId { get; }
        public string ingredientId { get; }
        public string resultSnackId { get; }
        public string specialResultSnackId { get; }
        public double specialResultChance { get; }
        public SnackDefinition ResultSnack => SnackCatalog.Find(resultSnackId);
        public SnackDefinition SpecialResultSnack => SnackCatalog.Find(specialResultSnackId);
        public bool HasSpecialResult => !string.IsNullOrEmpty(specialResultSnackId)
            && specialResultChance > 0d;
        public int coinCost => Math.Max(0, ResultSnack?.coinCost ?? 0);
        public int dropCost => Math.Max(0, ResultSnack?.dropCost ?? 0);
        public int fragmentCost => Math.Max(0, ResultSnack?.fragmentCost ?? 0);
        public bool requiresStarMilk => ResultSnack?.requiresStarMilk ?? false;

        public bool IsSpecialResultRoll(double roll)
        {
            return HasSpecialResult && roll < specialResultChance;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static double NormalizeChance(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0d;
            }

            return Math.Min(1d, Math.Max(0d, value));
        }
    }

    public sealed class MilkBlendMasteryMilestoneDefinition
    {
        public MilkBlendMasteryMilestoneDefinition(
            int stage,
            int requiredUseCount,
            string title,
            string researchNote,
            int milkCoinReward,
            int milkDropReward,
            int collectionFragmentReward)
        {
            this.stage = Math.Max(1, stage);
            this.requiredUseCount = Math.Max(1, requiredUseCount);
            this.title = Normalize(title);
            this.researchNote = Normalize(researchNote);
            this.milkCoinReward = Math.Max(0, milkCoinReward);
            this.milkDropReward = Math.Max(0, milkDropReward);
            this.collectionFragmentReward = Math.Max(0, collectionFragmentReward);
        }

        public int stage { get; }
        public int requiredUseCount { get; }
        public string title { get; }
        public string researchNote { get; }
        public int milkCoinReward { get; }
        public int milkDropReward { get; }
        public int collectionFragmentReward { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class MilkBlendMasteryResearchRecord
    {
        public MilkBlendMasteryResearchRecord(
            string recordId,
            string ingredientId,
            int stage,
            string title,
            string detail)
        {
            this.recordId = Normalize(recordId);
            this.ingredientId = Normalize(ingredientId);
            this.stage = Math.Max(1, stage);
            this.title = Normalize(title);
            this.detail = Normalize(detail);
        }

        public string recordId { get; }
        public string ingredientId { get; }
        public int stage { get; }
        public string title { get; }
        public string detail { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class MilkBlendingCatalog
    {
        public const double DefaultSpecialResultChance = 0.07d;
        public const int FirstMasteryUseCount = 3;
        public const int SecondMasteryUseCount = 7;
        public const int FinalMasteryUseCount = 12;
        public const string SoftDoughIngredientId = "ingredient_soft_dough";
        public const string HoneyPowderIngredientId = "ingredient_honey_powder";
        public const string PuddingMixIngredientId = "ingredient_pudding_mix";
        public const string NutCrumbIngredientId = "ingredient_nut_crumb";
        public const string RiceGrainIngredientId = "ingredient_rice_grain";
        public const string YogurtCultureIngredientId = "ingredient_yogurt_culture";
        public const string CoffeeJellyIngredientId = "ingredient_coffee_jelly";
        public const string StarlightCreamIngredientId = "ingredient_starlight_cream";

        public static readonly MilkBlendIngredientDefinition SoftDough =
            new MilkBlendIngredientDefinition(
                SoftDoughIngredientId,
                "말랑 반죽",
                "부드럽고 폭신한 식감을 더하는 작은 반죽 조각입니다.");

        public static readonly MilkBlendIngredientDefinition HoneyPowder =
            new MilkBlendIngredientDefinition(
                HoneyPowderIngredientId,
                "꿀가루",
                "따뜻한 향과 은은한 단맛을 더하는 고운 가루입니다.");

        public static readonly MilkBlendIngredientDefinition PuddingMix =
            new MilkBlendIngredientDefinition(
                PuddingMixIngredientId,
                "푸딩 가루",
                "차갑게 굳으면 탱글한 식감이 되는 재료입니다.");

        public static readonly MilkBlendIngredientDefinition NutCrumb =
            new MilkBlendIngredientDefinition(
                NutCrumbIngredientId,
                "고소한 부스러기",
                "바삭한 식감과 고소한 향을 살려 주는 조각입니다.");

        public static readonly MilkBlendIngredientDefinition RiceGrain =
            new MilkBlendIngredientDefinition(
                RiceGrainIngredientId,
                "포근한 쌀알",
                "천천히 익히면 든든하고 부드러워지는 재료입니다.");

        public static readonly MilkBlendIngredientDefinition YogurtCulture =
            new MilkBlendIngredientDefinition(
                YogurtCultureIngredientId,
                "발효 씨앗",
                "시간을 들여 새로운 풍미를 깨우는 작은 발효 씨앗입니다.");

        public static readonly MilkBlendIngredientDefinition CoffeeJelly =
            new MilkBlendIngredientDefinition(
                CoffeeJellyIngredientId,
                "젤리 결정",
                "쌉싸름한 향을 머금고 반짝이는 젤리 결정입니다.");

        public static readonly MilkBlendIngredientDefinition StarlightCream =
            new MilkBlendIngredientDefinition(
                StarlightCreamIngredientId,
                "별빛 크림",
                "별빛 우유와 공명하는 희귀한 크림입니다.");

        public static readonly MilkBlendIngredientDefinition[] AllIngredients =
        {
            SoftDough,
            HoneyPowder,
            PuddingMix,
            NutCrumb,
            RiceGrain,
            YogurtCulture,
            CoffeeJelly,
            StarlightCream
        };

        public static readonly MilkBlendMasteryMilestoneDefinition[] AllMasteryMilestones =
        {
            new MilkBlendMasteryMilestoneDefinition(
                1,
                FirstMasteryUseCount,
                "첫 배합",
                "향과 질감의 첫 변화를 기록했습니다.",
                milkCoinReward: 4,
                milkDropReward: 0,
                collectionFragmentReward: 0),
            new MilkBlendMasteryMilestoneDefinition(
                2,
                SecondMasteryUseCount,
                "익숙한 손길",
                "안정적인 배합 비율을 기록했습니다.",
                milkCoinReward: 0,
                milkDropReward: 2,
                collectionFragmentReward: 0),
            new MilkBlendMasteryMilestoneDefinition(
                3,
                FinalMasteryUseCount,
                "재료 연구 완성",
                "완성된 배합법을 도감에 정리했습니다.",
                milkCoinReward: 8,
                milkDropReward: 0,
                collectionFragmentReward: 1)
        };

        public static readonly string[] AllMilkIds =
        {
            MilkCatalog.BasicMilkId,
            MilkCatalog.WarmMilkId,
            MilkCatalog.ColdMilkId,
            MilkCatalog.NuttyMilkId,
            MilkCatalog.RichMilkId,
            MilkCatalog.FermentedMilkId,
            MilkCatalog.CoffeeMilkId,
            MilkCatalog.StarMilkId
        };

        public static readonly MilkBlendRecipeDefinition[] AllRecipes =
        {
            new MilkBlendRecipeDefinition(
                MilkCatalog.BasicMilkId,
                SoftDoughIngredientId,
                SnackCatalog.SoftSnackDoughId,
                SnackCatalog.CreamSoupId,
                DefaultSpecialResultChance),
            new MilkBlendRecipeDefinition(
                MilkCatalog.WarmMilkId,
                HoneyPowderIngredientId,
                SnackCatalog.WarmMilkSoupId,
                SnackCatalog.CreamSoupId,
                DefaultSpecialResultChance),
            new MilkBlendRecipeDefinition(
                MilkCatalog.ColdMilkId,
                PuddingMixIngredientId,
                SnackCatalog.ColdMilkPuddingId,
                SnackCatalog.CreamSoupId,
                DefaultSpecialResultChance),
            new MilkBlendRecipeDefinition(
                MilkCatalog.NuttyMilkId,
                NutCrumbIngredientId,
                SnackCatalog.NuttyCheeseCrackerId,
                SnackCatalog.CreamSoupId,
                DefaultSpecialResultChance),
            new MilkBlendRecipeDefinition(
                MilkCatalog.RichMilkId,
                RiceGrainIngredientId,
                SnackCatalog.RichMilkRisottoId,
                SnackCatalog.CreamSoupId,
                DefaultSpecialResultChance),
            new MilkBlendRecipeDefinition(
                MilkCatalog.FermentedMilkId,
                YogurtCultureIngredientId,
                SnackCatalog.FermentedYogurtBowlId,
                SnackCatalog.CreamSoupId,
                DefaultSpecialResultChance),
            new MilkBlendRecipeDefinition(
                MilkCatalog.CoffeeMilkId,
                CoffeeJellyIngredientId,
                SnackCatalog.CoffeeMilkJellyId,
                SnackCatalog.CreamSoupId,
                DefaultSpecialResultChance),
            new MilkBlendRecipeDefinition(
                MilkCatalog.StarMilkId,
                StarlightCreamIngredientId,
                SnackCatalog.StarCreamId)
        };

        public static MilkBlendIngredientDefinition FindIngredient(string ingredientId)
        {
            if (string.IsNullOrWhiteSpace(ingredientId))
            {
                return null;
            }

            var normalizedId = ingredientId.Trim();
            return Array.Find(
                AllIngredients,
                ingredient => ingredient != null
                    && string.Equals(ingredient.id, normalizedId, StringComparison.Ordinal));
        }

        public static MilkBlendRecipeDefinition FindRecipe(
            string milkId,
            string ingredientId)
        {
            if (string.IsNullOrWhiteSpace(milkId)
                || string.IsNullOrWhiteSpace(ingredientId))
            {
                return null;
            }

            var normalizedMilkId = milkId.Trim();
            var normalizedIngredientId = ingredientId.Trim();
            return Array.Find(
                AllRecipes,
                recipe => recipe != null
                    && string.Equals(
                        recipe.milkId,
                        normalizedMilkId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        recipe.ingredientId,
                        normalizedIngredientId,
                        StringComparison.Ordinal));
        }

        public static MilkBlendRecipeDefinition FindByResult(string resultSnackId)
        {
            if (string.IsNullOrWhiteSpace(resultSnackId))
            {
                return null;
            }

            var normalizedResultId = resultSnackId.Trim();
            return Array.Find(
                AllRecipes,
                recipe => recipe != null
                    && string.Equals(
                        recipe.resultSnackId,
                        normalizedResultId,
                        StringComparison.Ordinal));
        }

        public static MilkBlendMasteryMilestoneDefinition FindMasteryMilestone(int stage)
        {
            return Array.Find(
                AllMasteryMilestones,
                milestone => milestone != null && milestone.stage == stage);
        }

        public static int GetMasteryStage(int ingredientUseCount)
        {
            var normalizedCount = Math.Max(0, ingredientUseCount);
            var reachedStage = 0;
            for (var index = 0; index < AllMasteryMilestones.Length; index += 1)
            {
                var milestone = AllMasteryMilestones[index];
                if (milestone != null && normalizedCount >= milestone.requiredUseCount)
                {
                    reachedStage = Math.Max(reachedStage, milestone.stage);
                }
            }

            return reachedStage;
        }

        public static MilkBlendMasteryMilestoneDefinition GetNextMasteryMilestone(
            int ingredientUseCount)
        {
            var normalizedCount = Math.Max(0, ingredientUseCount);
            for (var index = 0; index < AllMasteryMilestones.Length; index += 1)
            {
                var milestone = AllMasteryMilestones[index];
                if (milestone != null && normalizedCount < milestone.requiredUseCount)
                {
                    return milestone;
                }
            }

            return null;
        }

        public static string BuildMasteryResearchRecordId(string ingredientId, int stage)
        {
            var ingredient = FindIngredient(ingredientId);
            var milestone = FindMasteryMilestone(stage);
            return ingredient == null || milestone == null
                ? string.Empty
                : $"milk_blend_mastery_{ingredient.id}_lv_{milestone.stage}";
        }

        public static MilkBlendMasteryResearchRecord FindMasteryResearchRecord(
            string recordId)
        {
            if (string.IsNullOrWhiteSpace(recordId))
            {
                return null;
            }

            var normalizedRecordId = recordId.Trim();
            for (var ingredientIndex = 0;
                ingredientIndex < AllIngredients.Length;
                ingredientIndex += 1)
            {
                var ingredient = AllIngredients[ingredientIndex];
                for (var milestoneIndex = 0;
                    milestoneIndex < AllMasteryMilestones.Length;
                    milestoneIndex += 1)
                {
                    var milestone = AllMasteryMilestones[milestoneIndex];
                    var candidate = CreateMasteryResearchRecord(ingredient, milestone);
                    if (candidate != null
                        && string.Equals(
                            candidate.recordId,
                            normalizedRecordId,
                            StringComparison.Ordinal))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        public static MilkBlendMasteryResearchRecord CreateMasteryResearchRecord(
            MilkBlendIngredientDefinition ingredient,
            MilkBlendMasteryMilestoneDefinition milestone)
        {
            if (ingredient == null || milestone == null)
            {
                return null;
            }

            var recordId = BuildMasteryResearchRecordId(ingredient.id, milestone.stage);
            return string.IsNullOrEmpty(recordId)
                ? null
                : new MilkBlendMasteryResearchRecord(
                    recordId,
                    ingredient.id,
                    milestone.stage,
                    $"{ingredient.displayName} 연구 Lv.{milestone.stage} · {milestone.title}",
                    $"{ingredient.displayName}을 {milestone.requiredUseCount}회 사용해 "
                    + milestone.researchNote);
        }

        public static string FormatCost(MilkBlendRecipeDefinition recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            var parts = new System.Collections.Generic.List<string>(3);
            if (recipe.coinCost > 0)
            {
                parts.Add($"코인 {recipe.coinCost}");
            }

            if (recipe.dropCost > 0)
            {
                parts.Add($"우유방울 {recipe.dropCost}");
            }

            if (recipe.fragmentCost > 0)
            {
                parts.Add($"수집 조각 {recipe.fragmentCost}");
            }

            return parts.Count > 0 ? string.Join(" · ", parts) : "무료";
        }
    }
}
