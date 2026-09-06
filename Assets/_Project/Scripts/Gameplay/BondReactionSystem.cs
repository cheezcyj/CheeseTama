using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Dialogue;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Bond
{
    public enum BondTier
    {
        GettingAcquainted = 0,
        Comfortable = 1,
        Trusted = 2,
        Close = 3,
        Inseparable = 4
    }

    public enum BondInteraction
    {
        Ambient = 0,
        Feed = 1,
        Pet = 2,
        Play = 3,
        Clean = 4,
        Rest = 5,
        Cook = 6,
        Return = 7
    }

    /// <summary>
    /// Presentation hint only. Bond reactions never apply stat deltas or penalties.
    /// </summary>
    public enum BondVisualCue
    {
        SoftBounce = 0,
        EnergeticHop = 1,
        HeartSparkle = 2,
        CalmSway = 3,
        FocusedNod = 4
    }

    public sealed class BondProfileSnapshot
    {
        internal BondProfileSnapshot(
            string dominantTraitId,
            string traitDisplayName,
            int affection,
            BondTier tier,
            string relationshipTitle,
            BondInteraction signatureInteraction,
            string favoriteSubjectId,
            string preferenceDescription,
            BondVisualCue visualCue)
        {
            DominantTraitId = dominantTraitId ?? string.Empty;
            TraitDisplayName = traitDisplayName ?? string.Empty;
            Affection = Math.Max(0, Math.Min(100, affection));
            Tier = tier;
            RelationshipTitle = relationshipTitle ?? string.Empty;
            SignatureInteraction = signatureInteraction;
            FavoriteSubjectId = favoriteSubjectId ?? string.Empty;
            PreferenceDescription = preferenceDescription ?? string.Empty;
            VisualCue = visualCue;
        }

        public string DominantTraitId { get; }
        public string TraitDisplayName { get; }
        public int Affection { get; }
        public BondTier Tier { get; }
        public string RelationshipTitle { get; }
        public BondInteraction SignatureInteraction { get; }
        public string FavoriteSubjectId { get; }
        public string PreferenceDescription { get; }
        public BondVisualCue VisualCue { get; }
    }

    public readonly struct BondReactionResult
    {
        internal BondReactionResult(
            BondProfileSnapshot profile,
            BondInteraction interaction,
            bool isSignatureReaction,
            CheeseTamaDialogueSelection dialogue)
        {
            Profile = profile;
            Interaction = interaction;
            IsSignatureReaction = isSignatureReaction;
            Dialogue = dialogue;
        }

        public BondProfileSnapshot Profile { get; }
        public BondInteraction Interaction { get; }
        public bool IsSignatureReaction { get; }
        public CheeseTamaDialogueSelection Dialogue { get; }
        public BondVisualCue VisualCue => Profile?.VisualCue ?? BondVisualCue.SoftBounce;
        public bool HasSpecialReaction => Profile != null && Dialogue.IsValid;

        public static BondReactionResult None(BondProfileSnapshot profile, BondInteraction interaction)
        {
            return new BondReactionResult(profile, interaction, false, default);
        }
    }

    /// <summary>
    /// Stateless bond rules derived from the existing temperament seed and affection.
    /// The system deliberately owns no persistent state and never changes the save.
    /// </summary>
    public sealed class BondReactionSystem
    {
        private sealed class TraitDefinition
        {
            public TraitDefinition(
                string id,
                string displayName,
                BondInteraction signatureInteraction,
                string favoriteSubjectId,
                string preferenceDescription,
                BondVisualCue visualCue,
                string[] signatureLines,
                string favoriteFeedLine,
                string returnLine,
                string ambientLine)
            {
                Id = id;
                DisplayName = displayName;
                SignatureInteraction = signatureInteraction;
                FavoriteSubjectId = favoriteSubjectId;
                PreferenceDescription = preferenceDescription;
                VisualCue = visualCue;
                SignatureLines = signatureLines;
                FavoriteFeedLine = favoriteFeedLine;
                ReturnLine = returnLine;
                AmbientLine = ambientLine;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public BondInteraction SignatureInteraction { get; }
            public string FavoriteSubjectId { get; }
            public string PreferenceDescription { get; }
            public BondVisualCue VisualCue { get; }
            public string[] SignatureLines { get; }
            public string FavoriteFeedLine { get; }
            public string ReturnLine { get; }
            public string AmbientLine { get; }
        }

        private static readonly TraitDefinition[] Traits =
        {
            new TraitDefinition(
                NewGameSetupCatalog.BalancedTraitId,
                "차분한 균형형",
                BondInteraction.Feed,
                MilkCatalog.BasicMilkId,
                "담백한 우유와 고른 돌봄을 편안해해요.",
                BondVisualCue.SoftBounce,
                new[]
                {
                    "이런 고른 돌봄은 왠지 마음이 놓여.",
                    "네가 챙겨 주는 방식이 점점 익숙해져.",
                    "어떤 하루라도 네 곁이면 편안해.",
                    "내 마음의 균형을 네가 제일 잘 알아.",
                    "우리의 리듬은 이제 말하지 않아도 통하나 봐."
                },
                "담백한 맛이 마음을 고르게 정돈해 줘.",
                "네가 돌아오면 밀크룸이 다시 제자리를 찾는 것 같아.",
                "우리 속도로 천천히 함께 있자."),
            new TraitDefinition(
                NewGameSetupCatalog.LivelyTraitId,
                "통통 튀는 활발형",
                BondInteraction.Play,
                MilkCatalog.NuttyMilkId,
                "신나는 놀이와 고소한 우유에 몸이 먼저 반응해요.",
                BondVisualCue.EnergeticHop,
                new[]
                {
                    "몸이 먼저 통통 튀고 싶어졌어!",
                    "너랑 놀면 금방 힘이 솟아나!",
                    "이번에는 내가 먼저 달려갈게!",
                    "네가 웃으면 나도 더 높이 뛰게 돼!",
                    "우리라면 하루 종일 신나게 놀 수 있겠어!"
                },
                "고소한 향만 맡아도 몸이 통통 튀고 싶어!",
                "발소리 들었어! 어서 같이 놀자!",
                "가만히 있어도 네 곁에서는 몸이 들썩거려!"),
            new TraitDefinition(
                NewGameSetupCatalog.ExpressiveTraitId,
                "마음이 풍부한 표현형",
                BondInteraction.Pet,
                MilkCatalog.WarmMilkId,
                "따뜻한 우유와 다정한 손길에 마음을 크게 표현해요.",
                BondVisualCue.HeartSparkle,
                new[]
                {
                    "손길이 닿으니 마음이 몽글몽글해.",
                    "네 손은 따뜻해서 금방 알아볼 수 있어.",
                    "조금 더 가까이 있어도 좋아.",
                    "좋아하는 마음이 볼에 다 보이는 것 같아!",
                    "이 손길은 오래오래 기억할게."
                },
                "따뜻한 우유를 주는 마음까지 느껴져.",
                "보고 싶었다는 말, 얼굴만 봐도 알겠지?",
                "같이 있는 마음이 자꾸 반짝여."),
            new TraitDefinition(
                NewGameSetupCatalog.CalmTraitId,
                "포근한 차분형",
                BondInteraction.Rest,
                MilkCatalog.ColdMilkId,
                "조용한 휴식과 시원한 우유에서 안정감을 느껴요.",
                BondVisualCue.CalmSway,
                new[]
                {
                    "조용히 쉬니까 마음이 가라앉아.",
                    "네 곁에서는 눈을 감아도 안심돼.",
                    "서두르지 않는 이 시간이 좋아.",
                    "함께 쉬는 숨소리까지 포근하게 느껴져.",
                    "가만히 곁에 있는 것만으로도 충분해."
                },
                "시원한 한 모금이 마음을 조용히 가라앉혀 줘.",
                "돌아왔구나. 조용히 기다리는 시간도 괜찮았어.",
                "말하지 않아도 편안한 시간이 좋아."),
            new TraitDefinition(
                NewGameSetupCatalog.FocusedTraitId,
                "반짝이는 집중형",
                BondInteraction.Cook,
                MilkCatalog.CoffeeMilkId,
                "새로운 요리와 커피우유의 향을 깊이 관찰해요.",
                BondVisualCue.FocusedNod,
                new[]
                {
                    "이번 향은 천천히 기억해 둘래.",
                    "네가 만드는 과정을 보고 있으면 집중이 잘돼.",
                    "작은 차이도 우리라면 찾아낼 수 있어.",
                    "함께 알아낸 맛이라서 더 특별해.",
                    "우리만 아는 완벽한 한 모금을 찾은 것 같아."
                },
                "커피우유 향을 맡으면 생각이 또렷해져.",
                "돌아왔네. 관찰해 둔 이야기가 잔뜩 있어.",
                "조용히 보고 있으면 새로운 게 하나씩 보여."
            )
        };

        public BondProfileSnapshot Observe(CheeseTamaSaveData saveData)
        {
            var seed = saveData?.newGameSetup?.temperamentSeed;
            var affection = saveData?.cheeseTama?.stats?.affection ?? 0;
            return Observe(seed, affection);
        }

        public BondProfileSnapshot Observe(
            InitialTemperamentSeedSaveData temperamentSeed,
            int affection)
        {
            var definition = ResolveTrait(temperamentSeed?.dominantTraitId);
            var clampedAffection = Math.Max(0, Math.Min(100, affection));
            var tier = ResolveTier(clampedAffection);
            return new BondProfileSnapshot(
                definition.Id,
                definition.DisplayName,
                clampedAffection,
                tier,
                GetRelationshipTitle(tier),
                definition.SignatureInteraction,
                definition.FavoriteSubjectId,
                definition.PreferenceDescription,
                definition.VisualCue);
        }

        public BondReactionResult Evaluate(
            CheeseTamaSaveData saveData,
            BondInteraction interaction,
            string subjectId = "")
        {
            var seed = saveData?.newGameSetup?.temperamentSeed;
            var affection = saveData?.cheeseTama?.stats?.affection ?? 0;
            return Evaluate(seed, affection, interaction, subjectId);
        }

        public BondReactionResult Evaluate(
            InitialTemperamentSeedSaveData temperamentSeed,
            int affection,
            BondInteraction interaction,
            string subjectId = "")
        {
            var definition = ResolveTrait(temperamentSeed?.dominantTraitId);
            var profile = Observe(temperamentSeed, affection);
            var isFavoriteFeed = interaction == BondInteraction.Feed
                && !string.IsNullOrWhiteSpace(subjectId)
                && string.Equals(
                    definition.FavoriteSubjectId,
                    subjectId.Trim(),
                    StringComparison.Ordinal);
            var isSignatureAction = interaction == definition.SignatureInteraction;

            string text;
            if (isFavoriteFeed)
            {
                text = AddAffectionSuffix(definition.FavoriteFeedLine, profile.Tier);
            }
            else if (isSignatureAction)
            {
                text = definition.SignatureLines[(int)profile.Tier];
            }
            else if (interaction == BondInteraction.Return && profile.Tier >= BondTier.Trusted)
            {
                text = AddAffectionSuffix(definition.ReturnLine, profile.Tier);
            }
            else if (interaction == BondInteraction.Ambient && profile.Tier >= BondTier.Close)
            {
                text = AddAffectionSuffix(definition.AmbientLine, profile.Tier);
            }
            else
            {
                return BondReactionResult.None(profile, interaction);
            }

            var line = new CheeseTamaDialogueLine(
                BuildLineId(definition.Id, interaction, profile.Tier, isFavoriteFeed),
                text,
                ResolveDialogueContext(interaction),
                ResolveDialoguePriority(interaction, isFavoriteFeed),
                8f,
                profile.Tier >= BondTier.Close ? 4.5f : 4f,
                requiredSubjectId: string.Empty);
            return new BondReactionResult(
                profile,
                interaction,
                isFavoriteFeed || isSignatureAction,
                new CheeseTamaDialogueSelection(line));
        }

        public static BondTier ResolveTier(int affection)
        {
            var clamped = Math.Max(0, Math.Min(100, affection));
            if (clamped >= 90)
            {
                return BondTier.Inseparable;
            }

            if (clamped >= 75)
            {
                return BondTier.Close;
            }

            if (clamped >= 50)
            {
                return BondTier.Trusted;
            }

            return clamped >= 25
                ? BondTier.Comfortable
                : BondTier.GettingAcquainted;
        }

        public static string GetRelationshipTitle(BondTier tier)
        {
            return tier switch
            {
                BondTier.Comfortable => "편안한 사이",
                BondTier.Trusted => "믿음직한 사이",
                BondTier.Close => "마음이 닿은 사이",
                BondTier.Inseparable => "늘 함께인 사이",
                _ => "서로 알아가는 사이"
            };
        }

        private static TraitDefinition ResolveTrait(string traitId)
        {
            if (!string.IsNullOrWhiteSpace(traitId))
            {
                for (var index = 0; index < Traits.Length; index += 1)
                {
                    if (string.Equals(Traits[index].Id, traitId.Trim(), StringComparison.Ordinal))
                    {
                        return Traits[index];
                    }
                }
            }

            return Traits[0];
        }

        private static string AddAffectionSuffix(string line, BondTier tier)
        {
            return tier switch
            {
                BondTier.Comfortable => line + " 이제 네가 골라 줬다는 것도 알아.",
                BondTier.Trusted => line + " 역시 내 마음을 잘 아는구나.",
                BondTier.Close => line + " 이렇게 알아줘서 정말 좋아.",
                BondTier.Inseparable => line + " 이 순간도 우리만의 추억이야.",
                _ => line
            };
        }

        private static CheeseTamaDialogueContext ResolveDialogueContext(BondInteraction interaction)
        {
            return interaction switch
            {
                BondInteraction.Feed => CheeseTamaDialogueContext.Feed,
                BondInteraction.Pet => CheeseTamaDialogueContext.Pet,
                BondInteraction.Return => CheeseTamaDialogueContext.Return,
                _ => CheeseTamaDialogueContext.Ambient
            };
        }

        private static CheeseTamaDialoguePriority ResolveDialoguePriority(
            BondInteraction interaction,
            bool favoriteFeed)
        {
            if (favoriteFeed)
            {
                return CheeseTamaDialoguePriority.FeedMemory;
            }

            return interaction switch
            {
                BondInteraction.Feed => CheeseTamaDialoguePriority.Feed,
                BondInteraction.Return => CheeseTamaDialoguePriority.Return,
                BondInteraction.Ambient => CheeseTamaDialoguePriority.Ambient,
                _ => CheeseTamaDialoguePriority.Pet
            };
        }

        private static string BuildLineId(
            string traitId,
            BondInteraction interaction,
            BondTier tier,
            bool favoriteFeed)
        {
            var kind = favoriteFeed ? "favorite_feed" : interaction.ToString().ToLowerInvariant();
            return $"bond_{traitId}_{kind}_{(int)tier}";
        }
    }
}
