using System;
using System.Collections.Generic;
using CheeseTama.Data;
using CheeseTama.Gameplay;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Gameplay.Events
{
    public enum CareEventCondition
    {
        LowHealth,
        LowHunger,
        LowCleanliness,
        HighSleepiness,
        HighMood,
        Ambient
    }

    public enum CareEventFollowUpAction
    {
        None,
        FeedMilk,
        Cook,
        Clean,
        Rest,
        Play,
        OpenCollection
    }

    public readonly struct CareEventChoiceEffect
    {
        public CareEventChoiceEffect(
            int milkCoins = 0,
            int milkDrops = 0,
            int starDrops = 0,
            int collectionFragments = 0,
            int hunger = 0,
            int mood = 0,
            int cleanliness = 0,
            int sleepiness = 0,
            int health = 0,
            int maturation = 0,
            int affection = 0,
            CareEventFollowUpAction followUpAction = CareEventFollowUpAction.None,
            string followUpHint = "")
        {
            this.milkCoins = milkCoins;
            this.milkDrops = milkDrops;
            this.starDrops = starDrops;
            this.collectionFragments = collectionFragments;
            this.hunger = hunger;
            this.mood = mood;
            this.cleanliness = cleanliness;
            this.sleepiness = sleepiness;
            this.health = health;
            this.maturation = maturation;
            this.affection = affection;
            this.followUpAction = followUpAction;
            this.followUpHint = followUpHint ?? string.Empty;
        }

        public readonly int milkCoins;
        public readonly int milkDrops;
        public readonly int starDrops;
        public readonly int collectionFragments;
        public readonly int hunger;
        public readonly int mood;
        public readonly int cleanliness;
        public readonly int sleepiness;
        public readonly int health;
        public readonly int maturation;
        public readonly int affection;
        public readonly CareEventFollowUpAction followUpAction;
        public readonly string followUpHint;

        public string BuildSummary()
        {
            var lines = new List<string>();
            var currencyChanges = new List<string>();
            AddDelta(currencyChanges, "코인", milkCoins);
            AddDelta(currencyChanges, "우유방울", milkDrops);
            AddDelta(currencyChanges, "별방울", starDrops);
            AddDelta(currencyChanges, "도감조각", collectionFragments);
            if (currencyChanges.Count > 0)
            {
                lines.Add($"재화: {string.Join(" · ", currencyChanges)}");
            }

            var statChanges = new List<string>();
            AddDelta(statChanges, "포만감", hunger);
            AddDelta(statChanges, "기분", mood);
            AddDelta(statChanges, "청결", cleanliness);
            AddDelta(statChanges, "졸림", sleepiness);
            AddDelta(statChanges, "건강", health);
            AddDelta(statChanges, "성숙도", maturation);
            AddDelta(statChanges, "애정", affection);
            if (statChanges.Count > 0)
            {
                lines.Add($"상태: {string.Join(" · ", statChanges)}");
            }

            if (!string.IsNullOrWhiteSpace(followUpHint))
            {
                lines.Add($"다음 행동: {followUpHint}");
            }

            return string.Join("\n", lines);
        }

        private static void AddDelta(ICollection<string> target, string label, int value)
        {
            if (target == null || value == 0)
            {
                return;
            }

            target.Add($"{label} {(value > 0 ? "+" : string.Empty)}{value}");
        }
    }

    public sealed class CareEventChoiceDefinition
    {
        public CareEventChoiceDefinition(
            string id,
            string label,
            string resultTitle,
            string resultMessage,
            CareEventChoiceEffect effect)
        {
            this.id = id ?? string.Empty;
            this.label = label ?? string.Empty;
            this.resultTitle = resultTitle ?? string.Empty;
            this.resultMessage = resultMessage ?? string.Empty;
            this.effect = effect;
        }

        public readonly string id;
        public readonly string label;
        public readonly string resultTitle;
        public readonly string resultMessage;
        public readonly CareEventChoiceEffect effect;
    }

    public sealed class CareEventDefinition
    {
        private static readonly CareEventChoiceDefinition[] NoChoices = Array.Empty<CareEventChoiceDefinition>();
        private readonly CareEventChoiceDefinition[] choices;

        public readonly string id;
        public readonly string title;
        public readonly string message;
        public readonly CareEventCondition condition;
        public readonly float chance;

        public CareEventDefinition(
            string id,
            string title,
            string message,
            CareEventCondition condition,
            float chance)
            : this(id, title, message, condition, chance, null, null)
        {
        }

        public CareEventDefinition(
            string id,
            string title,
            string message,
            CareEventCondition condition,
            float chance,
            CareEventChoiceDefinition firstChoice,
            CareEventChoiceDefinition secondChoice)
        {
            this.id = id ?? string.Empty;
            this.title = title ?? string.Empty;
            this.message = message ?? string.Empty;
            this.condition = condition;
            this.chance = Mathf.Clamp01(chance);
            choices = firstChoice != null && secondChoice != null
                ? new[] { firstChoice, secondChoice }
                : NoChoices;
        }

        public IReadOnlyList<CareEventChoiceDefinition> Choices => choices;
        public bool RequiresChoice => choices.Length == 2;

        public bool TryGetChoice(string choiceId, out CareEventChoiceDefinition choice)
        {
            if (!string.IsNullOrWhiteSpace(choiceId))
            {
                for (var index = 0; index < choices.Length; index += 1)
                {
                    if (string.Equals(choices[index].id, choiceId, StringComparison.Ordinal))
                    {
                        choice = choices[index];
                        return true;
                    }
                }
            }

            choice = null;
            return false;
        }

        public bool Matches(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return false;
            }

            return condition switch
            {
                CareEventCondition.LowHealth => tama.stats.health < RandomEventSystem.LowHealthThreshold,
                CareEventCondition.LowHunger => tama.stats.hunger < RandomEventSystem.LowHungerThreshold,
                CareEventCondition.LowCleanliness => tama.stats.cleanliness < RandomEventSystem.LowCleanlinessThreshold,
                CareEventCondition.HighSleepiness => tama.stats.sleepiness > RandomEventSystem.HighSleepinessThreshold,
                CareEventCondition.HighMood => tama.stats.mood > RandomEventSystem.HighMoodThreshold,
                CareEventCondition.Ambient => true,
                _ => false
            };
        }
    }

    public readonly struct CareEventResult
    {
        public readonly bool occurred;
        public readonly string occurrenceId;
        public readonly string eventId;
        public readonly string title;
        public readonly string message;
        public readonly bool firstDiscovery;
        public bool RequiresChoice => occurred
            && RandomEventSystem.TryGetDefinition(eventId, out var definition)
            && definition.RequiresChoice;

        // Kept for existing callers that only need the event id and message.
        public CareEventResult(bool occurred, string eventId, string message)
            : this(occurred, string.Empty, eventId, string.Empty, message, false)
        {
        }

        public CareEventResult(
            bool occurred,
            string occurrenceId,
            string eventId,
            string title,
            string message,
            bool firstDiscovery = false)
        {
            this.occurred = occurred;
            this.occurrenceId = occurrenceId ?? string.Empty;
            this.eventId = eventId ?? string.Empty;
            this.title = title ?? string.Empty;
            this.message = message ?? string.Empty;
            this.firstDiscovery = firstDiscovery;
        }

        public CareEventResult WithOccurrence(string id, bool isFirstDiscovery)
        {
            return new CareEventResult(
                occurred,
                id,
                eventId,
                title,
                message,
                isFirstDiscovery);
        }

        public static CareEventResult None()
        {
            return new CareEventResult(false, string.Empty, string.Empty);
        }
    }

    public enum CareEventChoiceResolutionStatus
    {
        Applied,
        AlreadyApplied,
        InvalidOccurrence,
        MissingTarget,
        UnknownEvent,
        ChoiceNotRequired,
        UnknownChoice
    }

    public readonly struct CareEventChoiceResult
    {
        public CareEventChoiceResult(
            CareEventChoiceResolutionStatus status,
            string occurrenceId,
            string eventId,
            string choiceId,
            string title,
            string message,
            CareEventChoiceEffect effect)
        {
            this.status = status;
            this.occurrenceId = occurrenceId ?? string.Empty;
            this.eventId = eventId ?? string.Empty;
            this.choiceId = choiceId ?? string.Empty;
            this.title = title ?? string.Empty;
            this.message = message ?? string.Empty;
            this.effect = effect;
        }

        public readonly CareEventChoiceResolutionStatus status;
        public readonly string occurrenceId;
        public readonly string eventId;
        public readonly string choiceId;
        public readonly string title;
        public readonly string message;
        public readonly CareEventChoiceEffect effect;
        public bool applied => status == CareEventChoiceResolutionStatus.Applied;
        public bool duplicate => status == CareEventChoiceResolutionStatus.AlreadyApplied;

        public CareEventChoiceResult WithStatus(CareEventChoiceResolutionStatus nextStatus)
        {
            return new CareEventChoiceResult(
                nextStatus,
                occurrenceId,
                eventId,
                choiceId,
                title,
                message,
                effect);
        }
    }

    public sealed class CareEventChoiceSystem
    {
        private readonly Dictionary<string, CareEventChoiceResult> resolvedByOccurrence =
            new Dictionary<string, CareEventChoiceResult>(StringComparer.Ordinal);

        public CareEventChoiceResult ApplyChoice(
            CareEventResult pendingOccurrence,
            string choiceId,
            CheeseTamaModel tama,
            EconomySaveData economy,
            int negativeEffectMitigationPercent = 0)
        {
            if (!pendingOccurrence.occurred || string.IsNullOrWhiteSpace(pendingOccurrence.occurrenceId))
            {
                return Failure(
                    CareEventChoiceResolutionStatus.InvalidOccurrence,
                    pendingOccurrence,
                    choiceId);
            }

            if (resolvedByOccurrence.TryGetValue(pendingOccurrence.occurrenceId, out var resolved))
            {
                return resolved.WithStatus(CareEventChoiceResolutionStatus.AlreadyApplied);
            }

            if (tama == null || economy == null)
            {
                return Failure(
                    CareEventChoiceResolutionStatus.MissingTarget,
                    pendingOccurrence,
                    choiceId);
            }

            if (!RandomEventSystem.TryGetDefinition(pendingOccurrence.eventId, out var definition))
            {
                return Failure(
                    CareEventChoiceResolutionStatus.UnknownEvent,
                    pendingOccurrence,
                    choiceId);
            }

            if (!definition.RequiresChoice)
            {
                return Failure(
                    CareEventChoiceResolutionStatus.ChoiceNotRequired,
                    pendingOccurrence,
                    choiceId);
            }

            if (!definition.TryGetChoice(choiceId, out var choice))
            {
                return Failure(
                    CareEventChoiceResolutionStatus.UnknownChoice,
                    pendingOccurrence,
                    choiceId);
            }

            var appliedEffect = ApplyNegativeEffectMitigation(
                choice.effect,
                negativeEffectMitigationPercent);
            ApplyEffect(appliedEffect, tama, economy);
            var result = new CareEventChoiceResult(
                CareEventChoiceResolutionStatus.Applied,
                pendingOccurrence.occurrenceId,
                pendingOccurrence.eventId,
                choice.id,
                choice.resultTitle,
                choice.resultMessage,
                appliedEffect);
            resolvedByOccurrence.Add(pendingOccurrence.occurrenceId, result);
            return result;
        }

        public bool TryGetResolution(string occurrenceId, out CareEventChoiceResult result)
        {
            if (!string.IsNullOrWhiteSpace(occurrenceId)
                && resolvedByOccurrence.TryGetValue(occurrenceId, out result))
            {
                return true;
            }

            result = default;
            return false;
        }

        public void Clear()
        {
            resolvedByOccurrence.Clear();
        }

        public static CareEventChoiceEffect ApplyNegativeEffectMitigation(
            CareEventChoiceEffect effect,
            int percent)
        {
            var safePercent = Math.Max(0, Math.Min(100, percent));
            if (safePercent <= 0)
            {
                return effect;
            }

            return new CareEventChoiceEffect(
                milkCoins: MitigateDecrease(effect.milkCoins, safePercent),
                milkDrops: MitigateDecrease(effect.milkDrops, safePercent),
                starDrops: MitigateDecrease(effect.starDrops, safePercent),
                collectionFragments: MitigateDecrease(effect.collectionFragments, safePercent),
                hunger: MitigateDecrease(effect.hunger, safePercent),
                mood: MitigateDecrease(effect.mood, safePercent),
                cleanliness: MitigateDecrease(effect.cleanliness, safePercent),
                sleepiness: MitigateIncrease(effect.sleepiness, safePercent),
                health: MitigateDecrease(effect.health, safePercent),
                maturation: MitigateDecrease(effect.maturation, safePercent),
                affection: MitigateDecrease(effect.affection, safePercent),
                followUpAction: effect.followUpAction,
                followUpHint: effect.followUpHint);
        }

        private static void ApplyEffect(
            CareEventChoiceEffect effect,
            CheeseTamaModel tama,
            EconomySaveData economy)
        {
            tama.EnsureRuntimeDefaults();
            tama.stats.Apply(new StatEffect
            {
                hunger = effect.hunger,
                mood = effect.mood,
                cleanliness = effect.cleanliness,
                sleepiness = effect.sleepiness,
                health = effect.health,
                maturation = effect.maturation,
                affection = effect.affection
            });
            economy.milkCoins = AddClamped(economy.milkCoins, effect.milkCoins);
            economy.milkDrops = AddClamped(economy.milkDrops, effect.milkDrops);
            economy.starDrops = AddClamped(economy.starDrops, effect.starDrops);
            economy.collectionFragments = AddClamped(
                economy.collectionFragments,
                effect.collectionFragments);
        }

        private static int AddClamped(int current, int delta)
        {
            var value = (long)Math.Max(0, current) + delta;
            if (value <= 0L)
            {
                return 0;
            }

            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }

        private static int MitigateDecrease(int value, int percent)
        {
            return value < 0 ? ScaleTowardZero(value, percent) : value;
        }

        private static int MitigateIncrease(int value, int percent)
        {
            return value > 0 ? ScaleTowardZero(value, percent) : value;
        }

        private static int ScaleTowardZero(int value, int percent)
        {
            return (int)((long)value * (100 - percent) / 100L);
        }

        private static CareEventChoiceResult Failure(
            CareEventChoiceResolutionStatus status,
            CareEventResult occurrence,
            string choiceId)
        {
            return new CareEventChoiceResult(
                status,
                occurrence.occurrenceId,
                occurrence.eventId,
                choiceId,
                string.Empty,
                string.Empty,
                default);
        }
    }

    public sealed class RandomEventSystem
    {
        public const int LowHealthThreshold = 35;
        public const int LowHungerThreshold = 25;
        public const int LowCleanlinessThreshold = 35;
        public const int HighSleepinessThreshold = 75;
        public const int HighMoodThreshold = 80;
        public const float AmbientChance = 0.06f;

        private static readonly CareEventDefinition[] ConditionEventDefinitions =
        {
            new CareEventDefinition(
                "small_fever",
                "따뜻한 온기가 필요해요",
                "치즈타마가 추워해서 밀크룸의 조명이 조금 더 따뜻해졌어요.",
                CareEventCondition.LowHealth,
                0.36f),
            new CareEventDefinition(
                "hungry_peep",
                "꼬르륵, 작은 신호",
                "치즈타마가 배고픈 소리를 작게 냈어요.",
                CareEventCondition.LowHunger,
                0.34f),
            new CareEventDefinition(
                "dusty_corner",
                "먼지 낀 구석",
                "밀크룸 한쪽의 먼지 낀 구석이 치즈타마의 눈에 들어왔어요.",
                CareEventCondition.LowCleanliness,
                0.32f),
            new CareEventDefinition(
                "sleepy_yawn",
                "졸음이 한가득",
                "치즈타마가 눈을 비비며 길게 하품했어요.",
                CareEventCondition.HighSleepiness,
                0.32f),
            new CareEventDefinition(
                "happy_wiggle",
                "기분 좋은 흔들림",
                "치즈타마가 기분 좋게 몸을 살랑살랑 흔들었어요.",
                CareEventCondition.HighMood,
                0.22f)
        };

        private static readonly CareEventDefinition[] ChoiceEventDefinitions =
        {
            new CareEventDefinition(
                "warm_lamp_choice",
                "따뜻한 불빛 아래에서",
                "몸이 차가워진 치즈타마가 담요와 우유등을 번갈아 바라봐요.",
                CareEventCondition.LowHealth,
                0.24f,
                new CareEventChoiceDefinition(
                    "wrap_blanket",
                    "담요를 덮어준다",
                    "포근한 담요",
                    "치즈타마가 담요 속에서 천천히 온기를 되찾았어요.",
                    new CareEventChoiceEffect(
                        mood: 2,
                        sleepiness: -8,
                        health: 10,
                        followUpAction: CareEventFollowUpAction.Rest,
                        followUpHint: "휴식하기로 충분히 쉬게 해주세요.")),
                new CareEventChoiceDefinition(
                    "light_milk_lamp",
                    "우유등을 켠다",
                    "은은한 우유등",
                    "우유등이 켜지자 작은 우유방울이 빛을 머금었어요.",
                    new CareEventChoiceEffect(
                        milkDrops: 2,
                        health: 5,
                        affection: 3,
                        followUpAction: CareEventFollowUpAction.FeedMilk,
                        followUpHint: "따뜻한 우유를 챙겨주면 더 빨리 회복해요."))),
            new CareEventDefinition(
                "mystery_milk_delivery",
                "문 앞의 작은 우유 상자",
                "배고픈 치즈타마 앞에 보낸 이를 알 수 없는 우유 상자가 놓였어요.",
                CareEventCondition.LowHunger,
                0.22f,
                new CareEventChoiceDefinition(
                    "share_delivery",
                    "지금 함께 나눈다",
                    "나눠 마신 우유",
                    "치즈타마와 우유를 나누자 상자 밑에서 작은 코인이 굴러나왔어요.",
                    new CareEventChoiceEffect(
                        milkCoins: 2,
                        hunger: 18,
                        mood: 3,
                        affection: 2,
                        followUpAction: CareEventFollowUpAction.FeedMilk,
                        followUpHint: "포만감이 다시 낮아지기 전에 우유를 확인해 주세요.")),
                new CareEventChoiceDefinition(
                    "store_delivery",
                    "선반에 보관한다",
                    "선반에 모인 우유방울",
                    "상자를 정리하자 조합에 쓸 수 있는 우유방울과 도감조각이 남았어요.",
                    new CareEventChoiceEffect(
                        milkDrops: 4,
                        collectionFragments: 1,
                        hunger: -2,
                        followUpAction: CareEventFollowUpAction.FeedMilk,
                        followUpHint: "치즈타마는 아직 배고파요. 우유주기를 잊지 마세요."))),
            new CareEventDefinition(
                "moldy_footprints_choice",
                "바닥의 수상한 발자국",
                "먼지 사이로 곰팡곰팡이의 작은 발자국이 이어져 있어요.",
                CareEventCondition.LowCleanliness,
                0.24f,
                new CareEventChoiceDefinition(
                    "clean_footprints",
                    "발자국을 닦는다",
                    "반짝이는 바닥",
                    "발자국을 닦자 바닥이 반짝이고 틈새에서 코인을 찾았어요.",
                    new CareEventChoiceEffect(
                        milkCoins: 4,
                        cleanliness: 20,
                        health: 2,
                        followUpAction: CareEventFollowUpAction.Clean,
                        followUpHint: "청소하기로 남은 먼지도 정리해 주세요.")),
                new CareEventChoiceDefinition(
                    "follow_footprints",
                    "끝까지 따라간다",
                    "발자국 끝의 작은 꾸러미",
                    "발자국 끝에서 우유방울 꾸러미와 낯선 도감조각을 발견했어요.",
                    new CareEventChoiceEffect(
                        milkDrops: 3,
                        collectionFragments: 1,
                        mood: 5,
                        cleanliness: -4,
                        followUpAction: CareEventFollowUpAction.Clean,
                        followUpHint: "탐색으로 더 어질러졌으니 청소가 필요해요."))),
            new CareEventDefinition(
                "window_star_choice",
                "창가에 머문 작은 별",
                "졸린 치즈타마가 커튼 너머의 작은 별빛을 계속 바라봐요.",
                CareEventCondition.HighSleepiness,
                0.20f,
                new CareEventChoiceDefinition(
                    "close_curtain",
                    "커튼을 닫아준다",
                    "조용한 낮잠",
                    "커튼을 닫자 치즈타마가 안심하고 짧은 낮잠을 잤어요.",
                    new CareEventChoiceEffect(
                        mood: 2,
                        sleepiness: -20,
                        health: 6,
                        followUpAction: CareEventFollowUpAction.Rest,
                        followUpHint: "휴식하기로 잠을 조금 더 보충해 주세요.")),
                new CareEventChoiceDefinition(
                    "watch_star",
                    "조금 더 함께 본다",
                    "함께 본 별빛",
                    "잠은 미뤘지만 함께 본 빛이 특별한 기억과 도감조각으로 남았어요.",
                    new CareEventChoiceEffect(
                        collectionFragments: 1,
                        mood: 8,
                        sleepiness: 6,
                        affection: 4,
                        followUpAction: CareEventFollowUpAction.Rest,
                        followUpHint: "별을 본 뒤에는 꼭 쉬게 해주세요."))),
            new CareEventDefinition(
                "fridge_party_choice",
                "냉장고 속 작은 파티",
                "기분 좋은 치즈타마가 냉장고의 리듬에 맞춰 몸을 흔들기 시작했어요.",
                CareEventCondition.HighMood,
                0.18f,
                new CareEventChoiceDefinition(
                    "dance_together",
                    "함께 춤춘다",
                    "밀크룸 댄스 타임",
                    "함께 춤추자 냉장고 자석 뒤에서 파티 코인이 쏟아졌어요.",
                    new CareEventChoiceEffect(
                        milkCoins: 6,
                        mood: 5,
                        sleepiness: 5,
                        affection: 5,
                        followUpAction: CareEventFollowUpAction.Play,
                        followUpHint: "놀아주기에서 즐거운 기분을 이어가 보세요.")),
                new CareEventChoiceDefinition(
                    "prepare_party_snack",
                    "간식을 준비한다",
                    "작은 파티 간식",
                    "간식을 준비하는 동안 냉장고가 우유방울을 살짝 보태줬어요.",
                    new CareEventChoiceEffect(
                        milkDrops: 3,
                        hunger: 10,
                        cleanliness: -3,
                        followUpAction: CareEventFollowUpAction.Cook,
                        followUpHint: "요리하기에서 다음 간식도 준비할 수 있어요.")))
        };

        private static readonly CareEventDefinition AmbientEventDefinition = new CareEventDefinition(
            "quiet_hum",
            "밀크룸의 작은 울림",
            "밀크룸이 치즈타마 곁에서 부드럽게 울렸어요.",
            CareEventCondition.Ambient,
            AmbientChance);

        public static IReadOnlyList<CareEventDefinition> ConditionEvents => ConditionEventDefinitions;
        public static IReadOnlyList<CareEventDefinition> ChoiceEvents => ChoiceEventDefinitions;
        public static CareEventDefinition AmbientEvent => AmbientEventDefinition;

        public GameEventDefinition Roll(GameEventDefinition[] candidates)
        {
            if (candidates == null)
            {
                return null;
            }

            foreach (var candidate in candidates)
            {
                if (candidate == null || candidate.isHiddenUntilUnlocked)
                {
                    continue;
                }

                if (PassesChance(UnityEngine.Random.value, candidate.baseChance))
                {
                    return candidate;
                }
            }

            return null;
        }

        public CareEventResult RollCareEvent(
            CheeseTamaModel tama,
            bool force = false,
            int randomEventWeightPercent = 0)
        {
            return RollCareEvent(
                tama,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                force,
                randomEventWeightPercent);
        }

        // Deterministic overload for boundary tests and non-Unity callers.
        public CareEventResult RollCareEvent(
            CheeseTamaModel tama,
            float conditionChanceRoll,
            float ambientChanceRoll,
            bool force = false,
            int randomEventWeightPercent = 0)
        {
            return RollCareEvent(
                tama,
                0f,
                conditionChanceRoll,
                ambientChanceRoll,
                force,
                randomEventWeightPercent);
        }

        // Selection is independent from occurrence chance so multiple simultaneous
        // conditions do not permanently favor the first catalog entry.
        public CareEventResult RollCareEvent(
            CheeseTamaModel tama,
            float conditionSelectionRoll,
            float conditionChanceRoll,
            float ambientChanceRoll,
            bool force = false,
            int randomEventWeightPercent = 0)
        {
            // The legacy deterministic overload intentionally suppresses choice events.
            // Existing boundary tests and callers therefore retain their exact behavior.
            return RollCareEvent(
                tama,
                conditionSelectionRoll,
                conditionChanceRoll,
                0f,
                1f,
                ambientChanceRoll,
                force,
                randomEventWeightPercent);
        }

        public CareEventResult RollCareEvent(
            CheeseTamaModel tama,
            float conditionSelectionRoll,
            float conditionChanceRoll,
            float choiceSelectionRoll,
            float choiceChanceRoll,
            float ambientChanceRoll,
            bool force = false,
            int randomEventWeightPercent = 0)
        {
            if (tama == null || tama.stats == null)
            {
                return CareEventResult.None();
            }

            var candidate = PickConditionEvent(tama, conditionSelectionRoll);
            if (candidate != null && (force || PassesChance(
                    conditionChanceRoll,
                    ApplyWeightPercent(candidate.chance, randomEventWeightPercent))))
            {
                return CreateResult(candidate);
            }

            var choiceCandidate = PickChoiceEvent(tama, choiceSelectionRoll);
            if (!force
                && choiceCandidate != null
                && PassesChance(
                    choiceChanceRoll,
                    ApplyWeightPercent(choiceCandidate.chance, randomEventWeightPercent)))
            {
                return CreateResult(choiceCandidate);
            }

            if (force || PassesChance(
                    ambientChanceRoll,
                    ApplyWeightPercent(AmbientEventDefinition.chance, randomEventWeightPercent)))
            {
                return CreateResult(AmbientEventDefinition);
            }

            return CareEventResult.None();
        }

        // Allows development UI and tests to select an exact presentation without mutating stats.
        public CareEventResult ForceCareEvent(string eventId)
        {
            return TryGetDefinition(eventId, out var definition)
                ? CreateResult(definition)
                : CareEventResult.None();
        }

        public static bool TryGetDefinition(string eventId, out CareEventDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(eventId))
            {
                foreach (var candidate in ConditionEventDefinitions)
                {
                    if (string.Equals(candidate.id, eventId, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }

                foreach (var candidate in ChoiceEventDefinitions)
                {
                    if (string.Equals(candidate.id, eventId, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }

                if (string.Equals(AmbientEventDefinition.id, eventId, StringComparison.Ordinal))
                {
                    definition = AmbientEventDefinition;
                    return true;
                }

                if (SeasonalCareEventCatalog.TryGetCareEventDefinition(eventId, out definition))
                {
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static bool PassesChance(float roll, float chance)
        {
            if (float.IsNaN(roll) || float.IsNaN(chance) || chance <= 0f)
            {
                return false;
            }

            if (chance >= 1f)
            {
                return roll >= 0f && roll <= 1f;
            }

            return roll >= 0f && roll < chance;
        }

        public static float ApplyWeightPercent(float chance, int percent)
        {
            if (float.IsNaN(chance) || chance <= 0f)
            {
                return 0f;
            }

            var safePercent = Math.Max(0, Math.Min(1000, percent));
            return Mathf.Clamp01(chance * (100f + safePercent) / 100f);
        }

        private static CareEventDefinition PickConditionEvent(
            CheeseTamaModel tama,
            float selectionRoll)
        {
            var matches = new List<CareEventDefinition>();
            foreach (var definition in ConditionEventDefinitions)
            {
                if (definition.Matches(tama))
                {
                    matches.Add(definition);
                }
            }

            if (matches.Count == 0)
            {
                return null;
            }

            var normalizedRoll = float.IsNaN(selectionRoll)
                ? 0f
                : Mathf.Clamp01(selectionRoll);
            var selectedIndex = Mathf.Min(
                matches.Count - 1,
                Mathf.FloorToInt(normalizedRoll * matches.Count));
            return matches[selectedIndex];
        }

        private static CareEventDefinition PickChoiceEvent(
            CheeseTamaModel tama,
            float selectionRoll)
        {
            var matches = new List<CareEventDefinition>();
            foreach (var definition in ChoiceEventDefinitions)
            {
                if (definition.Matches(tama))
                {
                    matches.Add(definition);
                }
            }

            if (matches.Count == 0)
            {
                return null;
            }

            var normalizedRoll = float.IsNaN(selectionRoll)
                ? 0f
                : Mathf.Clamp01(selectionRoll);
            var selectedIndex = Mathf.Min(
                matches.Count - 1,
                Mathf.FloorToInt(normalizedRoll * matches.Count));
            return matches[selectedIndex];
        }

        private static CareEventResult CreateResult(CareEventDefinition definition)
        {
            return definition == null
                ? CareEventResult.None()
                : new CareEventResult(
                    true,
                    string.Empty,
                    definition.id,
                    definition.title,
                    definition.message);
        }
    }
}
