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
        {
            this.milkId = Normalize(milkId);
            this.ingredientId = Normalize(ingredientId);
            this.resultSnackId = Normalize(resultSnackId);
        }

        public string milkId { get; }
        public string ingredientId { get; }
        public string resultSnackId { get; }
        public SnackDefinition ResultSnack => SnackCatalog.Find(resultSnackId);
        public int coinCost => Math.Max(0, ResultSnack?.coinCost ?? 0);
        public int dropCost => Math.Max(0, ResultSnack?.dropCost ?? 0);
        public int fragmentCost => Math.Max(0, ResultSnack?.fragmentCost ?? 0);
        public bool requiresStarMilk => ResultSnack?.requiresStarMilk ?? false;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class MilkBlendingCatalog
    {
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
                SnackCatalog.SoftSnackDoughId),
            new MilkBlendRecipeDefinition(
                MilkCatalog.WarmMilkId,
                HoneyPowderIngredientId,
                SnackCatalog.WarmMilkSoupId),
            new MilkBlendRecipeDefinition(
                MilkCatalog.ColdMilkId,
                PuddingMixIngredientId,
                SnackCatalog.ColdMilkPuddingId),
            new MilkBlendRecipeDefinition(
                MilkCatalog.NuttyMilkId,
                NutCrumbIngredientId,
                SnackCatalog.NuttyCheeseCrackerId),
            new MilkBlendRecipeDefinition(
                MilkCatalog.RichMilkId,
                RiceGrainIngredientId,
                SnackCatalog.RichMilkRisottoId),
            new MilkBlendRecipeDefinition(
                MilkCatalog.FermentedMilkId,
                YogurtCultureIngredientId,
                SnackCatalog.FermentedYogurtBowlId),
            new MilkBlendRecipeDefinition(
                MilkCatalog.CoffeeMilkId,
                CoffeeJellyIngredientId,
                SnackCatalog.CoffeeMilkJellyId),
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

        public static string FormatCost(MilkBlendRecipeDefinition recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            var parts = new System.Collections.Generic.List<string>(3);
            if (recipe.coinCost > 0)
            {
                parts.Add($"우유코인 {recipe.coinCost}");
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
