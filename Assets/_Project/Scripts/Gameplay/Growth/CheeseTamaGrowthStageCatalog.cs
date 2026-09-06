using System.Collections.Generic;
using CheeseTama.Gameplay;

namespace CheeseTama.Gameplay.Growth
{
    public enum CheeseTamaGrowthStage
    {
        Egg,
        Hatchling,
        Soft,
        Grown,
        Mature,
        Final
    }

    public readonly struct CheeseTamaGrowthStageDefinition
    {
        public CheeseTamaGrowthStageDefinition(
            CheeseTamaGrowthStage stage,
            string recordId,
            string displayName,
            string description,
            int minimumLevel,
            bool requiresHatched)
        {
            Stage = stage;
            RecordId = recordId;
            DisplayName = displayName;
            Description = description;
            MinimumLevel = minimumLevel;
            RequiresHatched = requiresHatched;
        }

        public CheeseTamaGrowthStage Stage { get; }
        public string RecordId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int MinimumLevel { get; }
        public bool RequiresHatched { get; }
    }

    public static class CheeseTamaGrowthStageCatalog
    {
        private static readonly CheeseTamaGrowthStageDefinition[] Definitions =
        {
            new CheeseTamaGrowthStageDefinition(
                CheeseTamaGrowthStage.Egg,
                "growth_stage_egg",
                "치즈타마 알",
                "따뜻한 치즈빛 알 속에서 첫 성장을 기다리는 모습입니다.",
                1,
                false),
            new CheeseTamaGrowthStageDefinition(
                CheeseTamaGrowthStage.Hatchling,
                "soft_cheesetama",
                "부화 치즈타마",
                "껍질 조각을 달고 세상에 막 나온 작은 치즈타마입니다.",
                10,
                true),
            new CheeseTamaGrowthStageDefinition(
                CheeseTamaGrowthStage.Soft,
                "growth_stage_soft",
                "말랑 치즈타마",
                "팔다리와 얼굴이 또렷해지고 작은 컬이 돋아난 기본형입니다.",
                15,
                true),
            new CheeseTamaGrowthStageDefinition(
                CheeseTamaGrowthStage.Grown,
                "growth_stage_grown",
                "성장한 치즈타마",
                "몸집과 치즈 무늬, 광택이 한층 풍성해진 성장형입니다.",
                20,
                true),
            new CheeseTamaGrowthStageDefinition(
                CheeseTamaGrowthStage.Mature,
                "growth_stage_mature",
                "숙성 치즈타마",
                "안정된 푸딩 실루엣과 깊어진 치즈 무늬를 지닌 숙성형입니다.",
                28,
                true),
            new CheeseTamaGrowthStageDefinition(
                CheeseTamaGrowthStage.Final,
                "growth_stage_final",
                "완성된 치즈타마",
                "대표 비율과 광택 디테일이 완성된 치즈타마의 최종형입니다.",
                33,
                true)
        };

        public static IReadOnlyList<CheeseTamaGrowthStageDefinition> All => Definitions;

        public static CheeseTamaGrowthStage Resolve(CheeseTamaModel tama)
        {
            if (tama == null || !tama.isHatched || tama.level < 10)
            {
                return CheeseTamaGrowthStage.Egg;
            }

            if (tama.level >= 33)
            {
                return CheeseTamaGrowthStage.Final;
            }

            if (tama.level >= 28)
            {
                return CheeseTamaGrowthStage.Mature;
            }

            if (tama.level >= 20)
            {
                return CheeseTamaGrowthStage.Grown;
            }

            if (tama.level >= 15)
            {
                return CheeseTamaGrowthStage.Soft;
            }

            return CheeseTamaGrowthStage.Hatchling;
        }

        public static bool IsReached(CheeseTamaModel tama, CheeseTamaGrowthStage stage)
        {
            if (tama == null)
            {
                return false;
            }

            var definition = Get(stage);
            if (definition.RequiresHatched && !tama.isHatched)
            {
                return false;
            }

            return tama.level >= definition.MinimumLevel;
        }

        public static CheeseTamaGrowthStageDefinition Get(CheeseTamaGrowthStage stage)
        {
            for (var i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].Stage == stage)
                {
                    return Definitions[i];
                }
            }

            return Definitions[0];
        }

        public static bool TryGetByRecordId(string recordId, out CheeseTamaGrowthStageDefinition definition)
        {
            for (var i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].RecordId == recordId)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }
}
