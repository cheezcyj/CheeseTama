using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Feeding;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Gameplay.Care
{
    public sealed class CareActionSystem
    {
        private readonly LevelSystem levelSystem = new LevelSystem();
        private readonly HatchingSystem hatchingSystem = new HatchingSystem();
        private readonly FeedingStatusSystem feedingStatusSystem = new FeedingStatusSystem();
        private LateLevelGrowthSaveData lateLevelGrowth;
        private IList<MilkGrowthSaveEntry> milkGrowth;
        private int recoveryEffectPercent;

        public FeedingStatusResult LastFeedingStatusResult { get; private set; }
        public LateLevelGrowthResult LastLateLevelGrowthResult => levelSystem.LastLateLevelResult;

        public void ConfigureLateLevelGrowth(
            LateLevelGrowthSaveData state,
            IList<MilkGrowthSaveEntry> growthEntries)
        {
            lateLevelGrowth = state;
            milkGrowth = growthEntries;
        }

        public void ConfigureRecoveryEffectPercent(int percent)
        {
            recoveryEffectPercent = Math.Max(0, Math.Min(100, percent));
        }

        public CareActionResult FeedMilk(CheeseTamaModel tama)
        {
            return FeedMilk(tama, MilkCatalog.BasicMilk);
        }

        public CareActionResult FeedMilk(CheeseTamaModel tama, DateTimeOffset localTime)
        {
            return FeedMilk(tama, MilkCatalog.BasicMilk, localTime);
        }

        public CareActionResult FeedStarMilk(CheeseTamaModel tama)
        {
            return FeedMilk(tama, MilkCatalog.StarMilk);
        }

        public CareActionResult FeedStarMilk(CheeseTamaModel tama, DateTimeOffset localTime)
        {
            return FeedMilk(tama, MilkCatalog.StarMilk, localTime);
        }

        public CareActionResult FeedMilk(CheeseTamaModel tama, MilkDefinition milk)
        {
            return FeedMilkInternal(tama, milk, null);
        }

        public CareActionResult FeedMilk(
            CheeseTamaModel tama,
            MilkDefinition milk,
            DateTimeOffset localTime)
        {
            return FeedMilkInternal(tama, milk, localTime);
        }

        private CareActionResult FeedMilkInternal(
            CheeseTamaModel tama,
            MilkDefinition milk,
            DateTimeOffset? localTime)
        {
            LastFeedingStatusResult = FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            if (tama == null)
            {
                return MissingTama();
            }

            if (milk == null)
            {
                return new CareActionResult(false, false, "선택한 우유 데이터를 찾지 못했습니다.");
            }

            tama.EnsureRuntimeDefaults();
            var hungerBefore = tama.stats.hunger;
            tama.stats.hunger += milk.hunger;
            tama.stats.mood += milk.mood;
            tama.stats.cleanliness += milk.cleanliness;
            tama.stats.sleepiness += milk.sleepiness;
            tama.stats.health += ApplyRecoveryBonus(milk.health);
            tama.stats.maturation += milk.maturation;
            tama.stats.affection += milk.affection;
            tama.stats.milkSatisfaction += milk.milkSatisfaction;
            var feedingStatus = localTime.HasValue
                ? feedingStatusSystem.ApplyMilk(
                    tama,
                    milk.id,
                    hungerBefore,
                    milk.hunger,
                    localTime.Value)
                : feedingStatusSystem.ApplyMilk(tama, milk.id, hungerBefore, milk.hunger);
            LastFeedingStatusResult = feedingStatus;
            tama.stats.ClampAll();

            var message = CombineMessages(
                feedingStatus.message,
                $"치즈타마가 {milk.displayName}를 마셨습니다.");
            return AddCareProgress(tama, milk.careProgress, message);
        }

        public CareActionResult FeedSnack(CheeseTamaModel tama)
        {
            LastFeedingStatusResult = FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            if (tama == null)
            {
                return MissingTama();
            }

            tama.EnsureRuntimeDefaults();
            var hungerBefore = tama.stats.hunger;
            tama.stats.hunger += 10;
            tama.stats.mood += 9;
            tama.stats.cleanliness -= 5;
            tama.stats.sleepiness += 3;
            tama.stats.affection += 3;
            tama.stats.milkSatisfaction -= 2;
            var feedingStatus = feedingStatusSystem.ApplySnack(tama, hungerBefore, 10);
            LastFeedingStatusResult = feedingStatus;
            tama.stats.ClampAll();

            var actionMessage = tama.stats.cleanliness < 45
                ? "치즈타마가 부스러지는 치즈 간식을 먹었습니다. 밀크룸 청소가 필요합니다."
                : "치즈타마가 치즈 간식을 조금 먹었습니다.";
            var message = CombineMessages(feedingStatus.message, actionMessage);
            return AddCareProgress(tama, 5, message);
        }

        public CareActionResult FeedSnack(CheeseTamaModel tama, SnackDefinition snack)
        {
            return FeedSnackInternal(tama, snack, null);
        }

        public CareActionResult FeedSnack(
            CheeseTamaModel tama,
            SnackDefinition snack,
            DateTimeOffset localTime)
        {
            return FeedSnackInternal(tama, snack, localTime);
        }

        private CareActionResult FeedSnackInternal(
            CheeseTamaModel tama,
            SnackDefinition snack,
            DateTimeOffset? localTime)
        {
            LastFeedingStatusResult = FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            if (tama == null)
            {
                return MissingTama();
            }

            if (snack == null)
            {
                return new CareActionResult(false, false, "선택한 간식 데이터를 찾지 못했습니다.");
            }

            tama.EnsureRuntimeDefaults();
            var hungerBefore = tama.stats.hunger;
            tama.stats.hunger += snack.hunger;
            tama.stats.mood += snack.mood;
            tama.stats.cleanliness += snack.cleanliness;
            tama.stats.sleepiness += snack.sleepiness;
            tama.stats.health += ApplyRecoveryBonus(snack.health);
            tama.stats.affection += snack.affection;
            tama.stats.maturation += snack.maturation;
            tama.stats.milkSatisfaction += snack.milkSatisfaction;
            var feedingStatus = localTime.HasValue
                ? feedingStatusSystem.ApplySnack(
                    tama,
                    snack.id,
                    snack.growthMilkId,
                    hungerBefore,
                    snack.hunger,
                    localTime.Value)
                : ApplyIdentifiedSnackWithoutNightEffects(tama, snack, hungerBefore);
            LastFeedingStatusResult = feedingStatus;
            tama.stats.ClampAll();

            var actionMessage = snack.cleanliness < 0 && tama.stats.cleanliness < 45
                ? $"치즈타마가 {snack.displayName}을 먹었습니다. 바닥에 부스러기가 남아 청소가 필요합니다."
                : $"치즈타마가 {snack.displayName}을 먹었습니다.";
            var message = CombineMessages(feedingStatus.message, actionMessage);
            return AddCareProgress(tama, snack.careProgress, message);
        }

        public CareActionResult Play(CheeseTamaModel tama)
        {
            LastFeedingStatusResult = FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            if (tama == null)
            {
                return MissingTama();
            }

            tama.EnsureRuntimeDefaults();
            var wasOverfull = feedingStatusSystem.IsOverfull(tama);
            tama.stats.hunger -= 5;
            tama.stats.mood += wasOverfull ? 7 : 12;
            tama.stats.sleepiness += 8;
            tama.stats.affection += wasOverfull ? 2 : 4;
            var feedingStatus = wasOverfull
                ? feedingStatusSystem.RecoverByPlay(tama)
                : FeedingStatusResult.None(tama.stats.overfullness);
            LastFeedingStatusResult = feedingStatus;
            tama.stats.ClampAll();

            var actionMessage = wasOverfull
                ? "치즈타마가 무리하지 않고 가볍게 놀았습니다."
                : "치즈타마가 잠깐 놀았습니다.";
            return AddCareProgress(tama, 6, CombineMessages(feedingStatus.message, actionMessage));
        }

        public CareActionResult Clean(CheeseTamaModel tama)
        {
            LastFeedingStatusResult = FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            if (tama == null)
            {
                return MissingTama();
            }

            tama.EnsureRuntimeDefaults();
            tama.stats.cleanliness += 25;
            tama.stats.mood += 1;
            tama.stats.health += ApplyRecoveryBonus(2);
            var feedingStatus = feedingStatusSystem.RecoverByClean(tama);
            LastFeedingStatusResult = feedingStatus;
            tama.stats.ClampAll();

            return AddCareProgress(
                tama,
                4,
                CombineMessages(feedingStatus.message, "밀크룸이 깨끗해졌습니다."));
        }

        public CareActionResult Rest(CheeseTamaModel tama)
        {
            LastFeedingStatusResult = FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            if (tama == null)
            {
                return MissingTama();
            }

            tama.EnsureRuntimeDefaults();
            tama.stats.hunger -= 2;
            tama.stats.sleepiness -= 20;
            tama.stats.health += ApplyRecoveryBonus(4);
            tama.stats.mood += 2;
            var feedingStatus = feedingStatusSystem.RecoverByRest(tama);
            LastFeedingStatusResult = feedingStatus;
            tama.stats.ClampAll();

            return AddCareProgress(
                tama,
                3,
                CombineMessages(feedingStatus.message, "치즈타마가 따뜻한 빛 아래에서 쉬었습니다."));
        }

        public CareActionResult Pet(CheeseTamaModel tama)
        {
            LastFeedingStatusResult = FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            if (tama == null)
            {
                return MissingTama();
            }

            tama.stats.mood += 4;
            tama.stats.affection += 2;
            tama.stats.ClampAll();

            var message = tama.stats.mood >= 85
                ? "치즈타마가 쓰다듬을 좋아해 기분 좋게 몸을 흔들었습니다."
                : "치즈타마를 부드럽게 쓰다듬었습니다.";
            return AddCareProgress(tama, 1, message);
        }

        private CareActionResult AddCareProgress(CheeseTamaModel tama, int progress, string message)
        {
            var levelBefore = tama.level;
            levelSystem.AddProgress(
                tama,
                Mathf.Max(0, progress),
                lateLevelGrowth,
                milkGrowth);
            var hatched = hatchingSystem.TryHatch(tama);

            if (levelSystem.LastLateLevelResult.IsBlocked)
            {
                var missing = levelSystem.LastLateLevelResult.GateStatus
                    .BuildMissingRequirementsMessage();
                if (!string.IsNullOrWhiteSpace(missing))
                {
                    message = CombineMessages(message, $"다음 성장 조건: {missing}");
                }
            }

            if (hatched)
            {
                return new CareActionResult(
                    true,
                    true,
                    CombineMessages(message, "껍질이 열리고 말랑한 치즈타마가 깨어났습니다."),
                    true);
            }

            if (tama.level > levelBefore)
            {
                return new CareActionResult(true, false, $"{message} 레벨이 올랐습니다. 부화 {HatchingSystem.GetHatchProgressPercent(tama)}%.", true);
            }

            var hatchProgress = HatchingSystem.GetHatchProgressPercent(tama);
            if (!tama.isHatched && hatchProgress >= 75)
            {
                return new CareActionResult(true, false, $"{message} 껍질이 따뜻해졌습니다. 부화 {hatchProgress}%.");
            }

            return new CareActionResult(true, false, message);
        }

        private static CareActionResult MissingTama()
        {
            return new CareActionResult(false, false, "치즈타마 데이터를 불러오지 못했습니다.");
        }

        private FeedingStatusResult ApplyIdentifiedSnackWithoutNightEffects(
            CheeseTamaModel tama,
            SnackDefinition snack,
            int hungerBefore)
        {
            return feedingStatusSystem.ApplySnack(
                tama,
                snack.id,
                snack.growthMilkId,
                hungerBefore,
                snack.hunger,
                new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.Zero));
        }

        private int ApplyRecoveryBonus(int amount)
        {
            if (amount <= 0 || recoveryEffectPercent <= 0)
            {
                return amount;
            }

            var bonus = ((long)amount * recoveryEffectPercent + 99L) / 100L;
            var result = (long)amount + bonus;
            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        private static string CombineMessages(string primary, string secondary)
        {
            if (string.IsNullOrWhiteSpace(primary))
            {
                return secondary ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(secondary) ? primary : $"{primary} {secondary}";
        }
    }
}
