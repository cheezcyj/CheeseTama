namespace CheeseTama.Gameplay.Milk
{
    public static class MilkCatalog
    {
        public const string BasicMilkId = "basic_milk";
        public const string WarmMilkId = "warm_milk";
        public const string ColdMilkId = "cold_milk";
        public const string NuttyMilkId = "nutty_milk";
        public const string RichMilkId = "rich_milk";
        public const string FermentedMilkId = "fermented_milk";
        public const string CoffeeMilkId = "coffee_milk";
        public const string StarMilkId = "star_milk";

        public const int MainMilkMaxGrowthLevel = 5;
        public const int SequentialUnlockLevel = 2;

        public static readonly MilkDefinition BasicMilk = new MilkDefinition(
            BasicMilkId,
            "기본 우유",
            "common",
            "가장 안정적인 기본 우유입니다. 포만감을 크게 채웁니다.",
            "feed_milk",
            string.Empty,
            0,
            1,
            8,
            25,
            0,
            0,
            0,
            0,
            0,
            1,
            0);

        public static readonly MilkDefinition WarmMilk = new MilkDefinition(
            WarmMilkId,
            "따뜻한 우유",
            "common",
            "포만감과 기분을 올리고 졸림을 높입니다. 자기 전 케어에 어울립니다.",
            "feed_warm_milk",
            BasicMilkId,
            SequentialUnlockLevel,
            1,
            8,
            20,
            5,
            0,
            15,
            0,
            0,
            1,
            1);

        public static readonly MilkDefinition ColdMilk = new MilkDefinition(
            ColdMilkId,
            "차가운 우유",
            "common",
            "포만감과 기분을 빠르게 올립니다. 밤에는 몸 떨림 패널티 후보가 됩니다.",
            "feed_cold_milk",
            WarmMilkId,
            SequentialUnlockLevel,
            1,
            8,
            20,
            10,
            0,
            0,
            0,
            0,
            1,
            1);

        public static readonly MilkDefinition NuttyMilk = new MilkDefinition(
            NuttyMilkId,
            "고소한 우유",
            "common",
            "든든한 포만감과 건강 회복을 주는 고소한 우유입니다.",
            "feed_nutty_milk",
            ColdMilkId,
            SequentialUnlockLevel,
            1,
            8,
            25,
            0,
            0,
            0,
            3,
            0,
            1,
            2);

        public static readonly MilkDefinition RichMilk = new MilkDefinition(
            RichMilkId,
            "진한 우유",
            "Rare",
            "포만감과 숙성도를 올리지만 졸림이 함께 오르는 진한 우유입니다.",
            "feed_rich_milk",
            NuttyMilkId,
            SequentialUnlockLevel,
            1,
            9,
            30,
            0,
            0,
            10,
            0,
            5,
            1,
            3);

        public static readonly MilkDefinition FermentedMilk = new MilkDefinition(
            FermentedMilkId,
            "발효우유",
            "Rare",
            "숙성도와 건강을 크게 올리지만 청결도가 내려갑니다.",
            "feed_fermented_milk",
            RichMilkId,
            SequentialUnlockLevel,
            1,
            9,
            0,
            0,
            -5,
            0,
            5,
            15,
            1,
            2);

        public static readonly MilkDefinition CoffeeMilk = new MilkDefinition(
            CoffeeMilkId,
            "커피우유",
            "Epic",
            "집중 상태와 밤 시간 성장 보너스를 담당하는 우유입니다. 졸림을 낮춥니다.",
            "feed_coffee_milk",
            FermentedMilkId,
            SequentialUnlockLevel,
            1,
            10,
            0,
            6,
            0,
            -15,
            0,
            4,
            1,
            3);

        public static readonly MilkDefinition StarMilk = new MilkDefinition(
            StarMilkId,
            "별빛 우유",
            "Legendary",
            "모든 주요 우유 성장도 Lv.5와 치즈타마 Lv.33 이후 열리는 별빛 루트 우유입니다.",
            "feed_star_milk",
            string.Empty,
            0,
            2,
            16,
            0,
            8,
            0,
            0,
            0,
            25,
            10,
            8);

        public static readonly MilkDefinition[] MainMilks =
        {
            BasicMilk,
            WarmMilk,
            ColdMilk,
            NuttyMilk,
            RichMilk,
            FermentedMilk,
            CoffeeMilk
        };

        public static readonly MilkDefinition[] VisibleMilks =
        {
            BasicMilk,
            WarmMilk,
            ColdMilk,
            NuttyMilk,
            RichMilk,
            FermentedMilk,
            CoffeeMilk,
            StarMilk
        };

        public static MilkDefinition Find(string milkId)
        {
            if (milkId == BasicMilkId)
            {
                return BasicMilk;
            }

            if (milkId == WarmMilkId)
            {
                return WarmMilk;
            }

            if (milkId == ColdMilkId)
            {
                return ColdMilk;
            }

            if (milkId == NuttyMilkId)
            {
                return NuttyMilk;
            }

            if (milkId == RichMilkId)
            {
                return RichMilk;
            }

            if (milkId == FermentedMilkId)
            {
                return FermentedMilk;
            }

            if (milkId == CoffeeMilkId)
            {
                return CoffeeMilk;
            }

            if (milkId == StarMilkId)
            {
                return StarMilk;
            }

            return null;
        }

        public static string GetDisplayName(string milkId)
        {
            return Find(milkId)?.displayName ?? milkId;
        }
    }
}
