using System;
using CheeseTama.Gameplay.Snacks;

namespace CheeseTama.Gameplay.HiddenRecipes
{
    public sealed class FantasyPowderHiddenRecipeDefinition
    {
        public FantasyPowderHiddenRecipeDefinition(
            string id,
            string displayName,
            string description,
            string resultSnackId,
            int resultSnackQuantity,
            string byproductSnackId,
            int byproductSnackQuantity,
            int successStarDrops,
            int byproductMilkDrops)
        {
            this.id = Normalize(id);
            this.displayName = Normalize(displayName);
            this.description = Normalize(description);
            this.resultSnackId = Normalize(resultSnackId);
            this.resultSnackQuantity = Math.Max(0, resultSnackQuantity);
            this.byproductSnackId = Normalize(byproductSnackId);
            this.byproductSnackQuantity = Math.Max(0, byproductSnackQuantity);
            this.successStarDrops = Math.Max(0, successStarDrops);
            this.byproductMilkDrops = Math.Max(0, byproductMilkDrops);
        }

        public string id { get; }
        public string displayName { get; }
        public string description { get; }
        public string resultSnackId { get; }
        public int resultSnackQuantity { get; }
        public string byproductSnackId { get; }
        public int byproductSnackQuantity { get; }
        public int successStarDrops { get; }
        public int byproductMilkDrops { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class FantasyPowderHiddenRecipeCatalog
    {
        public const string CreamCloudDoughId = "hidden_recipe_cream_cloud_dough";
        public const string QuietAgingBowlId = "hidden_recipe_quiet_aging_bowl";
        public const string MidnightMilkJellyId = "hidden_recipe_midnight_milk_jelly";

        public static readonly FantasyPowderHiddenRecipeDefinition CreamCloudDough =
            new FantasyPowderHiddenRecipeDefinition(
                CreamCloudDoughId,
                "크림 구름 반죽",
                "가볍게 부푼 크림 결을 말랑한 반죽에 겹친 비밀 조리법입니다.",
                SnackCatalog.SoftSnackDoughId,
                2,
                SnackCatalog.WarmMilkSoupId,
                1,
                successStarDrops: 1,
                byproductMilkDrops: 2);

        public static readonly FantasyPowderHiddenRecipeDefinition QuietAgingBowl =
            new FantasyPowderHiddenRecipeDefinition(
                QuietAgingBowlId,
                "고요한 숙성볼",
                "잔잔한 빛을 오래 머금어 숙성 향이 깊어진 비밀 요거트볼입니다.",
                SnackCatalog.FermentedYogurtBowlId,
                2,
                SnackCatalog.NuttyCheeseCrackerId,
                1,
                successStarDrops: 1,
                byproductMilkDrops: 2);

        public static readonly FantasyPowderHiddenRecipeDefinition MidnightMilkJelly =
            new FantasyPowderHiddenRecipeDefinition(
                MidnightMilkJellyId,
                "밤빛 밀크젤리",
                "어두운 유리처럼 반짝이며 천천히 흔들리는 비밀 우유 젤리입니다.",
                SnackCatalog.CoffeeMilkJellyId,
                2,
                SnackCatalog.ColdMilkPuddingId,
                1,
                successStarDrops: 1,
                byproductMilkDrops: 2);

        public static readonly FantasyPowderHiddenRecipeDefinition[] All =
        {
            CreamCloudDough,
            QuietAgingBowl,
            MidnightMilkJelly
        };

        public static FantasyPowderHiddenRecipeDefinition Find(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return null;
            }

            return Array.Find(
                All,
                recipe => recipe != null
                    && string.Equals(recipe.id, recipeId.Trim(), StringComparison.Ordinal));
        }
    }
}
