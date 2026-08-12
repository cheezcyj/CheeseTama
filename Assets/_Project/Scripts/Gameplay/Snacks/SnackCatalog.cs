using System;

namespace CheeseTama.Gameplay.Snacks
{
    public static class SnackCatalog
    {
        public const string WarmMilkSoupId = "recipe_warm_milk_soup";
        public const string SoftSnackDoughId = "recipe_soft_snack_dough";
        public const string ColdMilkPuddingId = "recipe_cold_milk_pudding";
        public const string NuttyCheeseCrackerId = "recipe_nutty_cheese_cracker";
        public const string RichMilkRisottoId = "recipe_rich_milk_risotto";
        public const string FermentedYogurtBowlId = "recipe_fermented_yogurt_bowl";
        public const string CoffeeMilkJellyId = "recipe_coffee_milk_jelly";
        public const string StarCreamId = "recipe_star_cream";

        public static readonly SnackDefinition WarmMilkSoup = new SnackDefinition(
            WarmMilkSoupId,
            "따뜻한 우유 수프",
            "기본 우유에 따뜻한 향을 더한 부드러운 한 그릇입니다.",
            0,
            0,
            0,
            false,
            14,
            5,
            0,
            1,
            1,
            2,
            0,
            1,
            5,
            "basic_milk",
            1,
            "happy_wiggle");

        public static readonly SnackDefinition SoftSnackDough = new SnackDefinition(
            SoftSnackDoughId,
            "말랑 간식 반죽",
            "조금 달콤한 반죽 간식입니다. 기분은 좋아지지만 부스러기가 남습니다.",
            5,
            0,
            0,
            false,
            9,
            11,
            -3,
            3,
            0,
            3,
            0,
            1,
            5,
            string.Empty,
            0,
            "cheese_snack_fed");

        public static readonly SnackDefinition ColdMilkPudding = new SnackDefinition(
            ColdMilkPuddingId,
            "차가운 우유 푸딩",
            "차가운 우유를 굳혀 만든 산뜻한 푸딩입니다.",
            4,
            0,
            0,
            false,
            12,
            12,
            0,
            -2,
            0,
            2,
            0,
            1,
            5,
            "cold_milk",
            1,
            "happy_wiggle");

        public static readonly SnackDefinition NuttyCheeseCracker = new SnackDefinition(
            NuttyCheeseCrackerId,
            "고소한 치즈 크래커",
            "고소한 우유 향이 배어 든 바삭한 치즈 크래커입니다.",
            6,
            0,
            0,
            false,
            16,
            4,
            -2,
            0,
            4,
            2,
            0,
            1,
            6,
            "nutty_milk",
            1,
            "cheese_snack_fed");

        public static readonly SnackDefinition RichMilkRisotto = new SnackDefinition(
            RichMilkRisottoId,
            "진한 밀크 리조또",
            "진한 우유로 끓여 든든하고 묵직한 한 그릇입니다.",
            8,
            1,
            0,
            false,
            22,
            4,
            0,
            6,
            2,
            3,
            3,
            1,
            7,
            "rich_milk",
            1,
            "happy_wiggle");

        public static readonly SnackDefinition FermentedYogurtBowl = new SnackDefinition(
            FermentedYogurtBowlId,
            "발효우유 요거트볼",
            "발효우유를 부드럽게 섞은 숙성 향의 요거트볼입니다.",
            7,
            1,
            0,
            false,
            8,
            5,
            -2,
            0,
            7,
            2,
            5,
            1,
            6,
            "fermented_milk",
            1,
            "happy_wiggle");

        public static readonly SnackDefinition CoffeeMilkJelly = new SnackDefinition(
            CoffeeMilkJellyId,
            "커피우유 젤리",
            "커피우유로 만든 탱글한 젤리입니다. 졸림을 조금 낮춥니다.",
            9,
            2,
            0,
            false,
            6,
            8,
            0,
            -10,
            0,
            3,
            2,
            1,
            6,
            "coffee_milk",
            1,
            "happy_wiggle");

        public static readonly SnackDefinition StarCream = new SnackDefinition(
            StarCreamId,
            "별빛 크림",
            "해금된 별빛 우유로 만드는 반짝이는 크림입니다.",
            8,
            3,
            1,
            true,
            16,
            13,
            0,
            2,
            2,
            5,
            1,
            1,
            7,
            "star_milk",
            1,
            "happy_wiggle");

        public static readonly SnackDefinition[] VisibleCookingRecipes =
        {
            WarmMilkSoup,
            SoftSnackDough,
            ColdMilkPudding,
            NuttyCheeseCracker,
            RichMilkRisotto,
            FermentedYogurtBowl,
            CoffeeMilkJelly
        };

        public static readonly SnackDefinition[] VisibleSnackItems =
        {
            WarmMilkSoup,
            SoftSnackDough,
            ColdMilkPudding,
            NuttyCheeseCracker,
            RichMilkRisotto,
            FermentedYogurtBowl,
            CoffeeMilkJelly
        };

        public static readonly SnackDefinition[] All =
        {
            WarmMilkSoup,
            SoftSnackDough,
            ColdMilkPudding,
            NuttyCheeseCracker,
            RichMilkRisotto,
            FermentedYogurtBowl,
            CoffeeMilkJelly,
            StarCream
        };

        public static SnackDefinition Find(string snackId)
        {
            if (string.IsNullOrWhiteSpace(snackId))
            {
                return null;
            }

            return Array.Find(All, snack => snack != null && snack.id == snackId);
        }
    }
}
