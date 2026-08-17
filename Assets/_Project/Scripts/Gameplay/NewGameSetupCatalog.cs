using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.NewGameSetup
{
    public enum NewGameSetupStep
    {
        EggSelection = 0,
        FirstMilkSelection = 1,
        Complete = 2
    }

    public sealed class NewGameSetupChoiceDefinition
    {
        internal NewGameSetupChoiceDefinition(
            string id,
            string displayName,
            string description,
            TemperamentProfile profile)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Profile = profile;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        internal TemperamentProfile Profile { get; }
    }

    internal readonly struct TemperamentProfile
    {
        public TemperamentProfile(
            int balance,
            int activity,
            int expressiveness,
            int composure,
            int focus)
        {
            Balance = balance;
            Activity = activity;
            Expressiveness = expressiveness;
            Composure = composure;
            Focus = focus;
        }

        public int Balance { get; }
        public int Activity { get; }
        public int Expressiveness { get; }
        public int Composure { get; }
        public int Focus { get; }
    }

    public static class NewGameSetupCatalog
    {
        public const string CreamEggId = "egg_cream";
        public const string ButterEggId = "egg_butter";
        public const string StrawberryEggId = "egg_strawberry";
        public const string MintEggId = "egg_mint";
        public const string CoffeeEggId = "egg_coffee";
        public const string StarEggId = "egg_star";

        public const string BasicFirstMilkId = MilkCatalog.BasicMilkId;
        public const string WarmFirstMilkId = MilkCatalog.WarmMilkId;
        public const string ColdFirstMilkId = MilkCatalog.ColdMilkId;
        public const string NuttyFirstMilkId = MilkCatalog.NuttyMilkId;
        public const string CoffeeFirstMilkId = MilkCatalog.CoffeeMilkId;

        public const string BalancedTraitId = "balanced";
        public const string LivelyTraitId = "lively";
        public const string ExpressiveTraitId = "expressive";
        public const string CalmTraitId = "calm";
        public const string FocusedTraitId = "focused";

        public const string LegacySeedKey = "setup:v1:legacy";
        public const string SkippedSeedKey = "setup:v1:skipped";
        public const string RecoveredSeedKey = "setup:v1:recovered";

        private const int EggWeight = 3;
        private const int FirstMilkWeight = 2;
        private const int TotalWeight = EggWeight + FirstMilkWeight;

        private static readonly NewGameSetupChoiceDefinition[] EggChoicesInternal =
        {
            new NewGameSetupChoiceDefinition(
                CreamEggId,
                "크림 알",
                "어느 돌봄에도 잘 적응하는 균형형 알이에요.",
                new TemperamentProfile(60, 10, 10, 10, 10)),
            new NewGameSetupChoiceDefinition(
                ButterEggId,
                "버터빛 알",
                "움직임과 놀이를 좋아하는 활발한 알이에요.",
                new TemperamentProfile(10, 60, 10, 10, 10)),
            new NewGameSetupChoiceDefinition(
                StrawberryEggId,
                "딸기빛 알",
                "기분을 솔직하게 표현하는 다정한 알이에요.",
                new TemperamentProfile(10, 10, 60, 10, 10)),
            new NewGameSetupChoiceDefinition(
                MintEggId,
                "민트빛 알",
                "깨끗하고 차분한 환경을 좋아하는 알이에요.",
                new TemperamentProfile(10, 10, 10, 60, 10)),
            new NewGameSetupChoiceDefinition(
                CoffeeEggId,
                "커피빛 알",
                "밤에도 집중력을 잃지 않는 신중한 알이에요.",
                new TemperamentProfile(10, 10, 10, 10, 60))
        };

        private static readonly NewGameSetupChoiceDefinition[] FirstMilkChoicesInternal =
        {
            new NewGameSetupChoiceDefinition(
                BasicFirstMilkId,
                "기본 우유",
                "고르게 적응할 수 있는 담백한 첫 우유예요.",
                new TemperamentProfile(55, 15, 10, 10, 10)),
            new NewGameSetupChoiceDefinition(
                WarmFirstMilkId,
                "따뜻한 우유",
                "마음을 편하게 열고 표현하도록 도와줘요.",
                new TemperamentProfile(10, 10, 60, 10, 10)),
            new NewGameSetupChoiceDefinition(
                ColdFirstMilkId,
                "차가운 우유",
                "서두르지 않고 차분하게 관찰하도록 도와줘요.",
                new TemperamentProfile(10, 10, 10, 60, 10)),
            new NewGameSetupChoiceDefinition(
                NuttyFirstMilkId,
                "고소한 우유",
                "새로운 놀이에 씩씩하게 나서도록 도와줘요.",
                new TemperamentProfile(10, 60, 10, 10, 10)),
            new NewGameSetupChoiceDefinition(
                CoffeeFirstMilkId,
                "커피우유",
                "한 가지 일에 깊이 집중하도록 도와줘요.",
                new TemperamentProfile(10, 10, 10, 10, 60))
        };

        public static IReadOnlyList<NewGameSetupChoiceDefinition> EggChoices => EggChoicesInternal;
        public static IReadOnlyList<NewGameSetupChoiceDefinition> FirstMilkChoices =>
            FirstMilkChoicesInternal;

        public static NewGameSetupChoiceDefinition StarEggChoice { get; } =
            new NewGameSetupChoiceDefinition(
                StarEggId,
                "별빛 알",
                "모든 우유의 성장을 마친 뒤 만날 수 있는 특별한 알이에요.",
                new TemperamentProfile(14, 14, 24, 14, 34));

        public static bool TryGetEgg(
            string eggId,
            out NewGameSetupChoiceDefinition definition)
        {
            if (string.Equals(eggId, StarEggId, StringComparison.Ordinal))
            {
                definition = StarEggChoice;
                return true;
            }

            return TryFind(EggChoicesInternal, eggId, out definition);
        }

        public static bool TryGetFirstMilk(
            string milkId,
            out NewGameSetupChoiceDefinition definition)
        {
            return TryFind(FirstMilkChoicesInternal, milkId, out definition);
        }

        public static bool TryCreateTemperamentSeed(
            string eggId,
            string firstMilkId,
            out InitialTemperamentSeedSaveData seed)
        {
            seed = null;
            if (!TryGetEgg(eggId, out var egg)
                || !TryGetFirstMilk(firstMilkId, out var firstMilk))
            {
                return false;
            }

            var balance = Blend(egg.Profile.Balance, firstMilk.Profile.Balance);
            var activity = Blend(egg.Profile.Activity, firstMilk.Profile.Activity);
            var expressiveness = Blend(
                egg.Profile.Expressiveness,
                firstMilk.Profile.Expressiveness);
            var composure = Blend(egg.Profile.Composure, firstMilk.Profile.Composure);
            var focus = Blend(egg.Profile.Focus, firstMilk.Profile.Focus);

            seed = new InitialTemperamentSeedSaveData
            {
                seedKey = $"setup:v1:{egg.Id}:{firstMilk.Id}",
                dominantTraitId = ResolveDominantTrait(
                    balance,
                    activity,
                    expressiveness,
                    composure,
                    focus),
                balance = balance,
                activity = activity,
                expressiveness = expressiveness,
                composure = composure,
                focus = focus
            };
            return true;
        }

        public static InitialTemperamentSeedSaveData CreateNeutralSeed(string seedKey)
        {
            return new InitialTemperamentSeedSaveData
            {
                seedKey = seedKey ?? string.Empty,
                dominantTraitId = BalancedTraitId,
                balance = 20,
                activity = 20,
                expressiveness = 20,
                composure = 20,
                focus = 20
            };
        }

        private static bool TryFind(
            NewGameSetupChoiceDefinition[] choices,
            string id,
            out NewGameSetupChoiceDefinition definition)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (var index = 0; index < choices.Length; index++)
                {
                    var candidate = choices[index];
                    if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        private static int Blend(int eggScore, int firstMilkScore)
        {
            return ((eggScore * EggWeight) + (firstMilkScore * FirstMilkWeight))
                / TotalWeight;
        }

        private static string ResolveDominantTrait(
            int balance,
            int activity,
            int expressiveness,
            int composure,
            int focus)
        {
            var dominantId = BalancedTraitId;
            var dominantScore = balance;
            if (activity > dominantScore)
            {
                dominantId = LivelyTraitId;
                dominantScore = activity;
            }

            if (expressiveness > dominantScore)
            {
                dominantId = ExpressiveTraitId;
                dominantScore = expressiveness;
            }

            if (composure > dominantScore)
            {
                dominantId = CalmTraitId;
                dominantScore = composure;
            }

            if (focus > dominantScore)
            {
                dominantId = FocusedTraitId;
            }

            return dominantId;
        }
    }
}
