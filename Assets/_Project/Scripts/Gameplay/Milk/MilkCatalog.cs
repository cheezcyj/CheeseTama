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
            "처음 주기 좋은 우유예요. 배부름을 크게 채워 줘요.",
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
            "배부름과 기분을 올려 줘요. 졸릴 수 있어서 자기 전 돌봄에 잘 어울려요.",
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
            "배부름과 기분을 빠르게 올려 줘요. 밤에 마시면 몸이 떨릴 수 있어요.",
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
            "배를 든든하게 채우고 건강 회복을 도와주는 고소한 우유예요.",
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
            "배부름과 성장을 올려 주지만 조금 졸릴 수 있는 진한 우유예요.",
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
            "성장과 건강을 크게 올려 주지만 몸이 조금 지저분해져요.",
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
            "밤에도 잘 움직이도록 도와주는 우유예요. 졸림을 낮춰 줘요.",
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
            "주요 우유를 모두 레벨 5로 키우고 치즈타마가 레벨 33이 되면 만날 수 있는 별빛 우유예요.",
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
