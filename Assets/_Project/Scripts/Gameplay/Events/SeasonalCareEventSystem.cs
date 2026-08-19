using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTama.Gameplay.Events
{
    public enum MilkroomSeason
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    public sealed class SeasonalCareEventDefinition
    {
        public SeasonalCareEventDefinition(
            MilkroomSeason season,
            CareEventDefinition careEvent,
            string collectionTitle,
            string collectionDetail)
        {
            Season = season;
            CareEvent = careEvent;
            CollectionTitle = collectionTitle ?? string.Empty;
            CollectionDetail = collectionDetail ?? string.Empty;
        }

        public MilkroomSeason Season { get; }
        public CareEventDefinition CareEvent { get; }
        public string CollectionTitle { get; }
        public string CollectionDetail { get; }
    }

    public static class SeasonalCareEventCatalog
    {
        public const float DefaultOccurrenceChance = 0.13f;

        private static readonly SeasonalCareEventDefinition[] Definitions =
        {
            new SeasonalCareEventDefinition(
                MilkroomSeason.Spring,
                new CareEventDefinition(
                    "season_spring_blossom_milk",
                    "우유빛 새싹이 피어난 날",
                    "창가의 빈 우유병에서 연두빛 새싹과 작은 꽃잎이 함께 올라왔어요.",
                    CareEventCondition.Ambient,
                    DefaultOccurrenceChance,
                    new CareEventChoiceDefinition(
                        "arrange_spring_blossoms",
                        "꽃잎을 도감에 정리한다",
                        "봄빛 도감 한 장",
                        "꽃잎을 조심스레 정리하자 밀크룸의 봄 향기가 기록으로 남았어요.",
                        new CareEventChoiceEffect(
                            milkCoins: 3,
                            collectionFragments: 1,
                            mood: 5,
                            affection: 3,
                            followUpAction: CareEventFollowUpAction.OpenCollection,
                            followUpHint: "도감에서 새 계절 기록을 확인해 보세요.")),
                    new CareEventChoiceDefinition(
                        "water_spring_sprout",
                        "우유방울로 새싹을 적신다",
                        "반짝이는 봄 새싹",
                        "우유방울이 닿자 새싹이 반짝이고 치즈타마도 기분 좋게 몸을 흔들었어요.",
                        new CareEventChoiceEffect(
                            milkDrops: 3,
                            hunger: 6,
                            mood: 4,
                            affection: 2,
                            followUpAction: CareEventFollowUpAction.FeedMilk,
                            followUpHint: "우유주기에서 봄날의 돌봄을 이어가 보세요."))),
                "우유빛 새싹이 피어난 날",
                "봄 창가의 우유병에서 작은 새싹과 꽃잎을 발견한 계절 기록입니다."),
            new SeasonalCareEventDefinition(
                MilkroomSeason.Summer,
                new CareEventDefinition(
                    "season_summer_milk_breeze",
                    "시원한 우유빛 바람",
                    "한낮의 창문이 살짝 열리며 차갑고 달콤한 우유빛 바람이 불어왔어요.",
                    CareEventCondition.Ambient,
                    DefaultOccurrenceChance,
                    new CareEventChoiceDefinition(
                        "rest_in_summer_breeze",
                        "커튼 아래에서 함께 쉰다",
                        "한여름의 짧은 휴식",
                        "선선한 바람을 맞으며 쉬자 치즈타마의 몸과 마음이 편안해졌어요.",
                        new CareEventChoiceEffect(
                            mood: 4,
                            sleepiness: -8,
                            health: 4,
                            affection: 2,
                            followUpAction: CareEventFollowUpAction.Rest,
                            followUpHint: "휴식하기에서 여름 낮잠을 마무리해 주세요.")),
                    new CareEventChoiceDefinition(
                        "catch_summer_drops",
                        "바람 속 우유방울을 잡는다",
                        "햇빛을 머금은 우유방울",
                        "바람을 따라 뛰어다니며 반짝이는 우유방울을 한 아름 모았어요.",
                        new CareEventChoiceEffect(
                            milkDrops: 4,
                            mood: 5,
                            cleanliness: -2,
                            affection: 3,
                            followUpAction: CareEventFollowUpAction.Play,
                            followUpHint: "놀아주기에서 신나는 기분을 이어가 보세요."))),
                "시원한 우유빛 바람",
                "여름 한낮의 창문으로 들어온 시원한 바람과 우유방울을 함께 즐긴 기록입니다."),
            new SeasonalCareEventDefinition(
                MilkroomSeason.Autumn,
                new CareEventDefinition(
                    "season_autumn_aging_aroma",
                    "숙성 향이 머문 오후",
                    "나무 선반 사이로 고소한 숙성 향과 금빛 낙엽 한 장이 흘러들어왔어요.",
                    CareEventCondition.Ambient,
                    DefaultOccurrenceChance,
                    new CareEventChoiceDefinition(
                        "record_autumn_aroma",
                        "향을 연구 노트에 적는다",
                        "금빛 숙성 기록",
                        "차분히 향을 기록하자 치즈타마의 성장에 작은 숙성의 결이 더해졌어요.",
                        new CareEventChoiceEffect(
                            collectionFragments: 1,
                            mood: 3,
                            maturation: 3,
                            affection: 2,
                            followUpAction: CareEventFollowUpAction.OpenCollection,
                            followUpHint: "도감에서 금빛 계절 기록을 읽어 보세요.")),
                    new CareEventChoiceDefinition(
                        "cook_autumn_snack",
                        "따뜻한 간식을 준비한다",
                        "가을 오후의 따뜻한 간식",
                        "고소한 향을 따라 간식을 만들자 밀크룸이 한층 더 포근해졌어요.",
                        new CareEventChoiceEffect(
                            milkCoins: 4,
                            hunger: 10,
                            mood: 4,
                            cleanliness: -3,
                            followUpAction: CareEventFollowUpAction.Cook,
                            followUpHint: "요리하기에서 다음 따뜻한 간식도 준비해 보세요."))),
                "숙성 향이 머문 오후",
                "가을의 금빛 낙엽과 고소한 숙성 향을 밀크룸에서 함께 느낀 기록입니다."),
            new SeasonalCareEventDefinition(
                MilkroomSeason.Winter,
                new CareEventDefinition(
                    "season_winter_milk_star",
                    "창문에 맺힌 우유별",
                    "차가운 창문 위에 우유방울 모양의 작은 별들이 하나둘 맺혔어요.",
                    CareEventCondition.Ambient,
                    DefaultOccurrenceChance,
                    new CareEventChoiceDefinition(
                        "warm_winter_window",
                        "담요를 두르고 별을 본다",
                        "포근한 겨울 별구경",
                        "담요 속에서 별을 바라보자 차가운 밤에도 따뜻한 온기가 오래 남았어요.",
                        new CareEventChoiceEffect(
                            mood: 4,
                            sleepiness: -10,
                            health: 7,
                            affection: 4,
                            followUpAction: CareEventFollowUpAction.Rest,
                            followUpHint: "휴식하기에서 따뜻한 겨울밤을 이어가 주세요.")),
                    new CareEventChoiceDefinition(
                        "collect_winter_stars",
                        "우유별을 작은 병에 모은다",
                        "병 속의 겨울 별빛",
                        "사라지기 전 우유별을 모으자 별방울 하나가 병 안에서 은은하게 빛났어요.",
                        new CareEventChoiceEffect(
                            milkDrops: 2,
                            starDrops: 1,
                            mood: 3,
                            sleepiness: 3,
                            followUpAction: CareEventFollowUpAction.OpenCollection,
                            followUpHint: "도감에서 병 속의 겨울 별빛을 확인해 보세요."))),
                "창문에 맺힌 우유별",
                "겨울밤 창문에 맺힌 우유방울 모양의 별빛을 발견한 계절 기록입니다.")
        };

        public static IReadOnlyList<SeasonalCareEventDefinition> All => Definitions;

        public static MilkroomSeason ResolveSeason(DateTimeOffset localTime)
        {
            return localTime.Month switch
            {
                3 or 4 or 5 => MilkroomSeason.Spring,
                6 or 7 or 8 => MilkroomSeason.Summer,
                9 or 10 or 11 => MilkroomSeason.Autumn,
                _ => MilkroomSeason.Winter
            };
        }

        public static SeasonalCareEventDefinition Find(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return null;
            }

            for (var index = 0; index < Definitions.Length; index += 1)
            {
                var definition = Definitions[index];
                if (string.Equals(definition.CareEvent.id, eventId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static bool TryGetCareEventDefinition(
            string eventId,
            out CareEventDefinition definition)
        {
            var seasonal = Find(eventId);
            definition = seasonal?.CareEvent;
            return definition != null;
        }

        public static int CountForSeason(MilkroomSeason season)
        {
            var count = 0;
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (Definitions[index].Season == season)
                {
                    count += 1;
                }
            }

            return count;
        }

        public static SeasonalCareEventDefinition GetForSeasonAt(
            MilkroomSeason season,
            int targetIndex)
        {
            if (targetIndex < 0)
            {
                return null;
            }

            var currentIndex = 0;
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (Definitions[index].Season != season)
                {
                    continue;
                }

                if (currentIndex == targetIndex)
                {
                    return Definitions[index];
                }

                currentIndex += 1;
            }

            return null;
        }
    }

    public sealed class SeasonalCareEventSystem
    {
        public CareEventResult Roll(
            DateTimeOffset localTime,
            float selectionRoll,
            float chanceRoll,
            bool force = false,
            int randomEventWeightPercent = 0)
        {
            var season = SeasonalCareEventCatalog.ResolveSeason(localTime);
            var count = SeasonalCareEventCatalog.CountForSeason(season);
            if (count <= 0 || float.IsNaN(selectionRoll) || float.IsInfinity(selectionRoll))
            {
                return CareEventResult.None();
            }

            var normalizedSelection = Mathf.Clamp01(selectionRoll);
            var selectedIndex = Mathf.Min(
                count - 1,
                Mathf.FloorToInt(normalizedSelection * count));
            var seasonal = SeasonalCareEventCatalog.GetForSeasonAt(season, selectedIndex);
            if (seasonal?.CareEvent == null)
            {
                return CareEventResult.None();
            }

            var chance = RandomEventSystem.ApplyWeightPercent(
                seasonal.CareEvent.chance,
                randomEventWeightPercent);
            if (!force && !RandomEventSystem.PassesChance(chanceRoll, chance))
            {
                return CareEventResult.None();
            }

            return new CareEventResult(
                true,
                string.Empty,
                seasonal.CareEvent.id,
                seasonal.CareEvent.title,
                seasonal.CareEvent.message);
        }
    }
}
