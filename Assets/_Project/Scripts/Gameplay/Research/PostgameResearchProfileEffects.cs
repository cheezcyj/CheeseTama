using System;
using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Research
{
    public enum PostgameResearchProfileEffectKind
    {
        None = 0,
        FinalMaturationProgress = 1,
        BlendingSpecialResult = 2,
        NpcEpisodeAffinity = 3,
        StarAndCollectionReward = 4
    }

    /// <summary>
    /// Immutable, presentation-safe view of the currently active postgame research effect.
    /// A default snapshot deliberately exposes no locked profile name, value, or condition.
    /// </summary>
    public readonly struct PostgameResearchProfileEffectSnapshot
    {
        private const int FinalMaturationProgressBonusValue = 1;
        private const double BlendingSpecialResultChanceBonusValue = 0.02d;
        private const int NpcEpisodeAffinityBonusValue = 1;
        private const int StarDropRewardBonusValue = 1;
        private const int CollectionFragmentRewardBonusValue = 1;

        private readonly string effectTitle;
        private readonly string effectDescription;

        internal PostgameResearchProfileEffectSnapshot(
            PostgameResearchNodeDefinition sourceDefinition,
            PostgameResearchProfileEffectKind kind,
            string effectTitle,
            string effectDescription)
        {
            SourceDefinition = sourceDefinition;
            Kind = sourceDefinition == null
                ? PostgameResearchProfileEffectKind.None
                : kind;
            this.effectTitle = Kind == PostgameResearchProfileEffectKind.None
                ? string.Empty
                : Normalize(effectTitle);
            this.effectDescription = Kind == PostgameResearchProfileEffectKind.None
                ? string.Empty
                : Normalize(effectDescription);
        }

        public bool Available => Kind != PostgameResearchProfileEffectKind.None
            && SourceDefinition != null;

        public PostgameResearchNodeDefinition SourceDefinition { get; }

        public PostgameResearchProfileEffectKind Kind { get; }

        public string ActiveNodeId => Available
            ? SourceDefinition.Id
            : string.Empty;

        public string ActiveProfileId => Available
            ? SourceDefinition.ProfileId
            : string.Empty;

        public string EffectTitle => Available
            ? effectTitle ?? string.Empty
            : string.Empty;

        public string EffectDescription => Available
            ? effectDescription ?? string.Empty
            : string.Empty;

        public int FinalMaturationProgressBonus =>
            Kind == PostgameResearchProfileEffectKind.FinalMaturationProgress
                ? FinalMaturationProgressBonusValue
                : 0;

        public double BlendingSpecialResultChanceBonus =>
            Kind == PostgameResearchProfileEffectKind.BlendingSpecialResult
                ? BlendingSpecialResultChanceBonusValue
                : 0d;

        public int NpcEpisodeAffinityBonus =>
            Kind == PostgameResearchProfileEffectKind.NpcEpisodeAffinity
                ? NpcEpisodeAffinityBonusValue
                : 0;

        public int StarDropRewardBonus =>
            Kind == PostgameResearchProfileEffectKind.StarAndCollectionReward
                ? StarDropRewardBonusValue
                : 0;

        public int CollectionFragmentRewardBonus =>
            Kind == PostgameResearchProfileEffectKind.StarAndCollectionReward
                ? CollectionFragmentRewardBonusValue
                : 0;

        public int ApplyFinalMaturationProgress(int baseProgress)
        {
            return AddBonusToPositive(baseProgress, FinalMaturationProgressBonus);
        }

        public double ApplyBlendingSpecialResultRoll(double baseRoll)
        {
            if (BlendingSpecialResultChanceBonus <= 0d
                || double.IsNaN(baseRoll)
                || double.IsInfinity(baseRoll)
                || baseRoll < 0d
                || baseRoll > 1d)
            {
                return baseRoll;
            }

            return Math.Max(0d, baseRoll - BlendingSpecialResultChanceBonus);
        }

        public int ApplyNpcEpisodeAffinityGain(int baseAffinityGain)
        {
            return AddBonusToPositive(baseAffinityGain, NpcEpisodeAffinityBonus);
        }

        public int ApplyStarDropReward(int baseReward)
        {
            return AddBonusToPositive(baseReward, StarDropRewardBonus);
        }

        public int ApplyCollectionFragmentReward(int baseReward)
        {
            return AddBonusToPositive(baseReward, CollectionFragmentRewardBonus);
        }

        private static int AddBonusToPositive(int value, int bonus)
        {
            if (value <= 0 || bonus <= 0)
            {
                return value;
            }

            return value > int.MaxValue - bonus
                ? int.MaxValue
                : value + bonus;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// Resolves the active profile against the existing research catalog without mutating save data.
    /// Corrupt, future, locked, or out-of-order profile state fails closed to an empty snapshot.
    /// </summary>
    public sealed class PostgameResearchProfileEffectSystem
    {
        public PostgameResearchProfileEffectSnapshot BuildSnapshot(
            int currentLevel,
            PostgameResearchSaveData state)
        {
            if (currentLevel < PostgameResearchSystem.RequiredLevel || state == null)
            {
                return default;
            }

            var activeProfileId = Normalize(state.activeProfileId);
            if (string.IsNullOrEmpty(activeProfileId))
            {
                return default;
            }

            var acceptedNodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < PostgameResearchCatalog.All.Count; index += 1)
            {
                var definition = PostgameResearchCatalog.All[index];
                if (definition == null
                    || !ContainsNormalized(state.unlockedNodeIds, definition.Id)
                    || (!string.IsNullOrEmpty(definition.PrerequisiteNodeId)
                        && !acceptedNodeIds.Contains(definition.PrerequisiteNodeId)))
                {
                    continue;
                }

                acceptedNodeIds.Add(definition.Id);
                if (!string.Equals(
                        definition.ProfileId,
                        activeProfileId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                return CreateSnapshot(definition, index);
            }

            return default;
        }

        public PostgameResearchProfileEffectSnapshot BuildPreview(
            PostgameResearchNodeDefinition definition)
        {
            if (definition == null)
            {
                return default;
            }

            for (var index = 0; index < PostgameResearchCatalog.All.Count; index += 1)
            {
                if (ReferenceEquals(PostgameResearchCatalog.All[index], definition)
                    || string.Equals(
                        PostgameResearchCatalog.All[index]?.Id,
                        definition.Id,
                        StringComparison.Ordinal))
                {
                    return CreateSnapshot(definition, index);
                }
            }

            return default;
        }

        private static PostgameResearchProfileEffectSnapshot CreateSnapshot(
            PostgameResearchNodeDefinition definition,
            int catalogIndex)
        {
            switch (catalogIndex)
            {
                case 0:
                    return new PostgameResearchProfileEffectSnapshot(
                        definition,
                        PostgameResearchProfileEffectKind.FinalMaturationProgress,
                        "숙성 관찰 보정",
                        "최종 숙성 행동 진행량이 1 증가합니다.");
                case 1:
                    return new PostgameResearchProfileEffectSnapshot(
                        definition,
                        PostgameResearchProfileEffectKind.BlendingSpecialResult,
                        "우유 섞기 도움",
                        "우유를 섞을 때 특별한 음식이 나올 확률이 2% 높아져요.");
                case 2:
                    return new PostgameResearchProfileEffectSnapshot(
                        definition,
                        PostgameResearchProfileEffectKind.NpcEpisodeAffinity,
                        "관계 기억 보정",
                        "NPC 승급 에피소드의 호감도 증가량이 1 늘어납니다.");
                case 3:
                    return new PostgameResearchProfileEffectSnapshot(
                        definition,
                        PostgameResearchProfileEffectKind.StarAndCollectionReward,
                        "별빛·도감 보정",
                        "기존 별빛과 도감조각 보상이 각각 1 증가합니다.");
                default:
                    return default;
            }
        }

        private static bool ContainsNormalized(
            IReadOnlyList<string> values,
            string expected)
        {
            if (values == null || string.IsNullOrEmpty(expected))
            {
                return false;
            }

            for (var index = 0; index < values.Count; index += 1)
            {
                if (string.Equals(
                        Normalize(values[index]),
                        expected,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
