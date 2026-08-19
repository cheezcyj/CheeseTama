using System;
using CheeseTama.Gameplay.Milk;

namespace CheeseTama.Gameplay.Feeding
{
    public readonly struct FeedingStatusResult
    {
        public readonly bool milkAversionActivated;
        public readonly bool milkAversionRecovered;
        public readonly bool overfullnessActivated;
        public readonly bool overfullnessRecovered;
        public readonly bool bodyChillActivated;
        public readonly bool bodyChillRecovered;
        public readonly bool fermentedAftertasteActivated;
        public readonly bool fermentedAftertasteRecovered;
        public readonly bool sleepRhythmDisruptionActivated;
        public readonly bool sleepRhythmDisruptionRecovered;
        public readonly int overfullnessBefore;
        public readonly int overfullnessAfter;
        public readonly string message;

        public FeedingStatusResult(
            bool milkAversionActivated,
            bool milkAversionRecovered,
            bool overfullnessActivated,
            bool overfullnessRecovered,
            int overfullnessBefore,
            int overfullnessAfter,
            string message)
            : this(
                milkAversionActivated,
                milkAversionRecovered,
                overfullnessActivated,
                overfullnessRecovered,
                false,
                false,
                false,
                false,
                false,
                false,
                overfullnessBefore,
                overfullnessAfter,
                message)
        {
        }

        public FeedingStatusResult(
            bool milkAversionActivated,
            bool milkAversionRecovered,
            bool overfullnessActivated,
            bool overfullnessRecovered,
            bool bodyChillActivated,
            bool bodyChillRecovered,
            bool fermentedAftertasteActivated,
            bool fermentedAftertasteRecovered,
            bool sleepRhythmDisruptionActivated,
            bool sleepRhythmDisruptionRecovered,
            int overfullnessBefore,
            int overfullnessAfter,
            string message)
        {
            this.milkAversionActivated = milkAversionActivated;
            this.milkAversionRecovered = milkAversionRecovered;
            this.overfullnessActivated = overfullnessActivated;
            this.overfullnessRecovered = overfullnessRecovered;
            this.bodyChillActivated = bodyChillActivated;
            this.bodyChillRecovered = bodyChillRecovered;
            this.fermentedAftertasteActivated = fermentedAftertasteActivated;
            this.fermentedAftertasteRecovered = fermentedAftertasteRecovered;
            this.sleepRhythmDisruptionActivated = sleepRhythmDisruptionActivated;
            this.sleepRhythmDisruptionRecovered = sleepRhythmDisruptionRecovered;
            this.overfullnessBefore = overfullnessBefore;
            this.overfullnessAfter = overfullnessAfter;
            this.message = message ?? string.Empty;
        }

        public int OverfullnessDelta => overfullnessAfter - overfullnessBefore;
        public bool HasMessage => !string.IsNullOrWhiteSpace(message);

        public static FeedingStatusResult None(int overfullness = 0)
        {
            var safeValue = Clamp(overfullness, 0, FeedingStatusSystem.MaximumOverfullness);
            return new FeedingStatusResult(false, false, false, false, safeValue, safeValue, string.Empty);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }

    public sealed class FeedingStatusSystem
    {
        public const int MilkAversionStreakThreshold = 3;
        public const int MilkAversionSatisfactionPenalty = 12;
        public const int MilkVarietySatisfactionRecovery = 12;
        public const int OverfullnessRawHungerThreshold = 110;
        public const int MaximumOverfullness = 100;
        public const int OverfullnessRecoveryPerPlay = 25;
        public const int OverfullnessRecoveryPerHour = 20;
        public const int NightStartHour = 22;
        public const int NightEndHour = 6;
        public const int MaximumAftereffectIntensity = 100;
        public const int MaximumAftereffectDurationHours = 12;
        public const int BodyChillIntensityPerFeed = 35;
        public const int BodyChillDurationPerFeed = 4;
        public const int FermentedAftertasteIntensityPerFeed = 30;
        public const int FermentedAftertasteDurationPerFeed = 6;
        public const int SleepRhythmIntensityPerFeed = 40;
        public const int SleepRhythmDurationPerFeed = 8;
        public const int BodyChillRecoveryPerHour = 10;
        public const int FermentedAftertasteRecoveryPerHour = 8;
        public const int SleepRhythmRecoveryPerHour = 6;

        public bool IsMilkAversionActive(CheeseTamaModel tama)
        {
            return tama?.growthHistory != null
                && tama.growthHistory.sameMilkFeedStreak >= MilkAversionStreakThreshold;
        }

        public bool IsOverfull(CheeseTamaModel tama)
        {
            return tama?.stats != null && tama.stats.overfullness > 0;
        }

        public bool IsBodyChillActive(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.bodyChillIntensity > 0
                && tama.stats.bodyChillHoursRemaining > 0;
        }

        public bool IsFermentedAftertasteActive(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.fermentedAftertasteIntensity > 0
                && tama.stats.fermentedAftertasteHoursRemaining > 0;
        }

        public bool IsSleepRhythmDisrupted(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.sleepRhythmDisruptionIntensity > 0
                && tama.stats.sleepRhythmDisruptionHoursRemaining > 0;
        }

        public static bool IsNight(DateTimeOffset localTime)
        {
            return localTime.Hour >= NightStartHour || localTime.Hour < NightEndHour;
        }

        public FeedingStatusResult ApplyMilk(
            CheeseTamaModel tama,
            string milkId,
            int hungerBefore,
            int hungerGain)
        {
            return ApplyMilkInternal(tama, milkId, hungerBefore, hungerGain, null);
        }

        public FeedingStatusResult ApplyMilk(
            CheeseTamaModel tama,
            string milkId,
            int hungerBefore,
            int hungerGain,
            DateTimeOffset localTime)
        {
            return ApplyMilkInternal(tama, milkId, hungerBefore, hungerGain, localTime);
        }

        public FeedingStatusResult ApplyMilk(
            CheeseTamaModel tama,
            string milkId,
            int hungerBefore,
            int hungerGain,
            int localHour)
        {
            var safeHour = Clamp(localHour, 0, 23);
            return ApplyMilkInternal(
                tama,
                milkId,
                hungerBefore,
                hungerGain,
                new DateTimeOffset(2000, 1, 1, safeHour, 0, 0, TimeSpan.Zero));
        }

        private FeedingStatusResult ApplyMilkInternal(
            CheeseTamaModel tama,
            string milkId,
            int hungerBefore,
            int hungerGain,
            DateTimeOffset? localTime)
        {
            if (tama == null || string.IsNullOrWhiteSpace(milkId))
            {
                return FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            }

            tama.EnsureRuntimeDefaults();
            var history = tama.growthHistory;
            var wasMilkAverse = IsMilkAversionActive(tama);
            var repeatedSameMilk = string.Equals(history.lastFedMilkId, milkId, StringComparison.Ordinal);

            history.sameMilkFeedStreak = repeatedSameMilk
                ? SaturatingIncrement(Math.Max(1, history.sameMilkFeedStreak))
                : 1;
            history.lastFedMilkId = milkId;

            var isMilkAverse = IsMilkAversionActive(tama);
            var milkAversionActivated = !wasMilkAverse && isMilkAverse;
            var milkAversionRecovered = wasMilkAverse && !isMilkAverse;
            var message = string.Empty;

            if (isMilkAverse)
            {
                tama.stats.milkSatisfaction = Clamp(
                    tama.stats.milkSatisfaction - MilkAversionSatisfactionPenalty,
                    0,
                    100);
                message = milkAversionActivated
                    ? "같은 우유가 반복되어 우유 질림 상태예요. 다른 우유로 회복할 수 있어요."
                    : "우유 질림으로 만족도가 낮아졌어요. 다른 우유를 주세요.";
            }
            else if (milkAversionRecovered)
            {
                tama.stats.milkSatisfaction = Clamp(
                    tama.stats.milkSatisfaction + MilkVarietySatisfactionRecovery,
                    0,
                    100);
                message = "다른 우유를 맛보고 우유 질림에서 회복했어요.";
            }

            var overfullness = AccumulateOverfullness(tama, hungerBefore, hungerGain);
            var result = new FeedingStatusResult(
                milkAversionActivated,
                milkAversionRecovered,
                overfullness.overfullnessActivated,
                false,
                overfullness.overfullnessBefore,
                overfullness.overfullnessAfter,
                CombineMessages(message, overfullness.message));
            var aftereffects = ApplyIdentifiedAftereffects(
                tama,
                milkId,
                string.Empty,
                localTime);
            return Merge(result, aftereffects);
        }

        public FeedingStatusResult ApplySnack(CheeseTamaModel tama, int hungerBefore, int hungerGain)
        {
            if (tama == null)
            {
                return FeedingStatusResult.None();
            }

            tama.EnsureRuntimeDefaults();
            return AccumulateOverfullness(tama, hungerBefore, hungerGain);
        }

        public FeedingStatusResult ApplySnack(
            CheeseTamaModel tama,
            string snackId,
            string relatedMilkId,
            int hungerBefore,
            int hungerGain,
            DateTimeOffset localTime)
        {
            if (tama == null)
            {
                return FeedingStatusResult.None();
            }

            tama.EnsureRuntimeDefaults();
            var overfullness = AccumulateOverfullness(tama, hungerBefore, hungerGain);
            var aftereffects = ApplyIdentifiedAftereffects(
                tama,
                relatedMilkId,
                snackId,
                localTime);
            return Merge(overfullness, aftereffects);
        }

        public FeedingStatusResult RecoverByPlay(CheeseTamaModel tama)
        {
            return RecoverOverfullness(
                tama,
                OverfullnessRecoveryPerPlay,
                "가볍게 놀며 과포만이 조금 가라앉았어요.",
                "가볍게 놀며 소화해 과포만에서 회복했어요.");
        }

        public FeedingStatusResult RecoverByClean(CheeseTamaModel tama)
        {
            if (tama?.stats == null)
            {
                return FeedingStatusResult.None();
            }

            tama.EnsureRuntimeDefaults();
            return Merge(
                FeedingStatusResult.None(tama.stats.overfullness),
                RecoverFermentedAftertaste(tama, 60, 6, "입가와 방을 닦아 발효 뒷맛이 옅어졌어요."));
        }

        public FeedingStatusResult RecoverByRest(CheeseTamaModel tama)
        {
            if (tama?.stats == null)
            {
                return FeedingStatusResult.None();
            }

            tama.EnsureRuntimeDefaults();
            var result = FeedingStatusResult.None(tama.stats.overfullness);
            result = Merge(
                result,
                RecoverBodyChill(tama, 30, 3, "포근하게 쉬며 몸 떨림이 가라앉았어요."));
            return Merge(
                result,
                RecoverSleepRhythm(tama, 50, 6, "차분히 쉬며 흐트러진 수면 리듬을 되찾았어요."));
        }

        public FeedingStatusResult RecoverByWarmMilk(CheeseTamaModel tama)
        {
            if (tama?.stats == null)
            {
                return FeedingStatusResult.None();
            }

            tama.EnsureRuntimeDefaults();
            var result = FeedingStatusResult.None(tama.stats.overfullness);
            result = Merge(
                result,
                RecoverBodyChill(tama, 50, 4, "따뜻한 우유로 몸 떨림이 가라앉았어요."));
            return Merge(
                result,
                RecoverSleepRhythm(tama, 10, 2, "따뜻한 우유가 흐트러진 수면 리듬을 조금 진정시켰어요."));
        }

        public FeedingStatusResult RecoverByTime(CheeseTamaModel tama, int hours)
        {
            if (hours <= 0)
            {
                return FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            }

            if (tama?.stats == null)
            {
                return FeedingStatusResult.None();
            }

            tama.EnsureRuntimeDefaults();
            var recovery = (long)OverfullnessRecoveryPerHour * hours;
            var result = RecoverOverfullness(
                tama,
                recovery > int.MaxValue ? int.MaxValue : (int)recovery,
                "시간이 지나 과포만이 조금 가라앉았어요.",
                "시간이 지나 과포만에서 회복했어요.");
            result = Merge(
                result,
                RecoverBodyChill(
                    tama,
                    SaturatingMultiply(BodyChillRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 몸 떨림이 가라앉았어요."));
            result = Merge(
                result,
                RecoverFermentedAftertaste(
                    tama,
                    SaturatingMultiply(FermentedAftertasteRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 발효 뒷맛이 옅어졌어요."));
            return Merge(
                result,
                RecoverSleepRhythm(
                    tama,
                    SaturatingMultiply(SleepRhythmRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 수면 리듬이 조금씩 돌아왔어요."));
        }

        private static FeedingStatusResult AccumulateOverfullness(
            CheeseTamaModel tama,
            int hungerBefore,
            int hungerGain)
        {
            var before = Clamp(tama.stats.overfullness, 0, MaximumOverfullness);
            var rawHunger = (long)hungerBefore + Math.Max(0, hungerGain);
            var excess = Math.Max(0L, rawHunger - OverfullnessRawHungerThreshold);
            var added = excess > int.MaxValue ? int.MaxValue : (int)excess;
            var after = SaturatingAddAndClamp(before, added, MaximumOverfullness);
            tama.stats.overfullness = after;

            if (added <= 0)
            {
                return FeedingStatusResult.None(before);
            }

            var activated = before == 0;
            var message = activated
                ? "배가 너무 불러 과포만 상태예요. 시간 경과나 놀이로 회복해요."
                : "먹이를 더 먹어 과포만이 심해졌어요. 잠시 기다리거나 가볍게 놀아 주세요.";
            return new FeedingStatusResult(false, false, activated, false, before, after, message);
        }

        private static FeedingStatusResult RecoverOverfullness(
            CheeseTamaModel tama,
            int recovery,
            string progressMessage,
            string recoveredMessage)
        {
            if (tama?.stats == null || recovery <= 0)
            {
                return FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            }

            var before = Clamp(tama.stats.overfullness, 0, MaximumOverfullness);
            if (before <= 0)
            {
                tama.stats.overfullness = 0;
                return FeedingStatusResult.None();
            }

            var after = Math.Max(0, before - recovery);
            var recovered = after == 0;
            tama.stats.overfullness = after;
            return new FeedingStatusResult(
                false,
                false,
                false,
                recovered,
                before,
                after,
                recovered ? recoveredMessage : progressMessage);
        }

        private static FeedingStatusResult ApplyIdentifiedAftereffects(
            CheeseTamaModel tama,
            string relatedMilkId,
            string foodId,
            DateTimeOffset? localTime)
        {
            var result = FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            if (tama?.stats == null)
            {
                return result;
            }

            if (string.Equals(relatedMilkId, MilkCatalog.WarmMilkId, StringComparison.Ordinal))
            {
                result = Merge(
                    result,
                    RecoverBodyChill(tama, 50, 4, "따뜻한 우유로 몸 떨림이 가라앉았어요."));
                result = Merge(
                    result,
                    RecoverSleepRhythm(tama, 10, 2, "따뜻한 우유가 흐트러진 수면 리듬을 조금 진정시켰어요."));
            }

            if (IsFamily(relatedMilkId, foodId, MilkCatalog.FermentedMilkId, "fermented", "yogurt"))
            {
                result = Merge(result, ActivateFermentedAftertaste(tama));
            }

            if (!localTime.HasValue || !IsNight(localTime.Value))
            {
                return result;
            }

            if (IsFamily(relatedMilkId, foodId, MilkCatalog.ColdMilkId, "cold"))
            {
                result = Merge(result, ActivateBodyChill(tama));
            }

            if (IsFamily(relatedMilkId, foodId, MilkCatalog.CoffeeMilkId, "coffee"))
            {
                result = Merge(result, ActivateSleepRhythm(tama));
            }

            return result;
        }

        private static FeedingStatusResult ActivateBodyChill(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.bodyChillIntensity > 0
                && tama.stats.bodyChillHoursRemaining > 0;
            tama.stats.bodyChillIntensity = SaturatingAddAndClamp(
                tama.stats.bodyChillIntensity,
                BodyChillIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.bodyChillHoursRemaining = SaturatingAddAndClamp(
                tama.stats.bodyChillHoursRemaining,
                BodyChillDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.BodyChill,
                !wasActive,
                false,
                wasActive
                    ? "늦은 시간 차가운 먹이로 몸 떨림이 더 오래가요."
                    : "늦은 시간 차가운 먹이로 몸 떨림이 생겼어요. 따뜻한 우유나 휴식으로 돌봐 주세요.");
        }

        private static FeedingStatusResult ActivateFermentedAftertaste(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.fermentedAftertasteIntensity > 0
                && tama.stats.fermentedAftertasteHoursRemaining > 0;
            tama.stats.fermentedAftertasteIntensity = SaturatingAddAndClamp(
                tama.stats.fermentedAftertasteIntensity,
                FermentedAftertasteIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.fermentedAftertasteHoursRemaining = SaturatingAddAndClamp(
                tama.stats.fermentedAftertasteHoursRemaining,
                FermentedAftertasteDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.FermentedAftertaste,
                !wasActive,
                false,
                wasActive
                    ? "발효 향이 겹쳐 발효 뒷맛이 더 오래 남아요."
                    : "발효·요거트 먹이의 뒷맛이 남았어요. 청소하거나 시간이 지나면 옅어져요.");
        }

        private static FeedingStatusResult ActivateSleepRhythm(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.sleepRhythmDisruptionIntensity > 0
                && tama.stats.sleepRhythmDisruptionHoursRemaining > 0;
            tama.stats.sleepRhythmDisruptionIntensity = SaturatingAddAndClamp(
                tama.stats.sleepRhythmDisruptionIntensity,
                SleepRhythmIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.sleepRhythmDisruptionHoursRemaining = SaturatingAddAndClamp(
                tama.stats.sleepRhythmDisruptionHoursRemaining,
                SleepRhythmDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.SleepRhythm,
                !wasActive,
                false,
                wasActive
                    ? "늦은 시간 커피 향으로 수면 리듬이 더 오래 흐트러져요."
                    : "늦은 시간 커피 계열 먹이로 수면 리듬이 흐트러졌어요. 휴식으로 진정시켜 주세요.");
        }

        private static FeedingStatusResult RecoverBodyChill(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.bodyChillIntensity,
                ref tama.stats.bodyChillHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.BodyChill, false, recovered, message)
                : FeedingStatusResult.None(tama.stats.overfullness);
        }

        private static FeedingStatusResult RecoverFermentedAftertaste(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.fermentedAftertasteIntensity,
                ref tama.stats.fermentedAftertasteHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.FermentedAftertaste, false, recovered, message)
                : FeedingStatusResult.None(tama.stats.overfullness);
        }

        private static FeedingStatusResult RecoverSleepRhythm(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.sleepRhythmDisruptionIntensity,
                ref tama.stats.sleepRhythmDisruptionHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.SleepRhythm, false, recovered, message)
                : FeedingStatusResult.None(tama.stats.overfullness);
        }

        private static bool RecoverAftereffect(
            ref int intensity,
            ref int hoursRemaining,
            int intensityRecovery,
            int durationRecovery,
            out bool recovered)
        {
            intensity = Clamp(intensity, 0, MaximumAftereffectIntensity);
            hoursRemaining = Clamp(hoursRemaining, 0, MaximumAftereffectDurationHours);
            var wasActive = intensity > 0 && hoursRemaining > 0;
            if (!wasActive)
            {
                intensity = 0;
                hoursRemaining = 0;
                recovered = false;
                return false;
            }

            intensity = Math.Max(0, intensity - Math.Max(0, intensityRecovery));
            hoursRemaining = Math.Max(0, hoursRemaining - Math.Max(0, durationRecovery));
            recovered = intensity == 0 || hoursRemaining == 0;
            if (recovered)
            {
                intensity = 0;
                hoursRemaining = 0;
            }

            return true;
        }

        private static FeedingStatusResult CreateAftereffectResult(
            CheeseTamaModel tama,
            AftereffectKind kind,
            bool activated,
            bool recovered,
            string message)
        {
            var overfullness = Clamp(tama?.stats?.overfullness ?? 0, 0, MaximumOverfullness);
            return new FeedingStatusResult(
                false,
                false,
                false,
                false,
                kind == AftereffectKind.BodyChill && activated,
                kind == AftereffectKind.BodyChill && recovered,
                kind == AftereffectKind.FermentedAftertaste && activated,
                kind == AftereffectKind.FermentedAftertaste && recovered,
                kind == AftereffectKind.SleepRhythm && activated,
                kind == AftereffectKind.SleepRhythm && recovered,
                overfullness,
                overfullness,
                message);
        }

        private static FeedingStatusResult Merge(
            FeedingStatusResult primary,
            FeedingStatusResult secondary)
        {
            return new FeedingStatusResult(
                primary.milkAversionActivated || secondary.milkAversionActivated,
                primary.milkAversionRecovered || secondary.milkAversionRecovered,
                primary.overfullnessActivated || secondary.overfullnessActivated,
                primary.overfullnessRecovered || secondary.overfullnessRecovered,
                primary.bodyChillActivated || secondary.bodyChillActivated,
                primary.bodyChillRecovered || secondary.bodyChillRecovered,
                primary.fermentedAftertasteActivated || secondary.fermentedAftertasteActivated,
                primary.fermentedAftertasteRecovered || secondary.fermentedAftertasteRecovered,
                primary.sleepRhythmDisruptionActivated || secondary.sleepRhythmDisruptionActivated,
                primary.sleepRhythmDisruptionRecovered || secondary.sleepRhythmDisruptionRecovered,
                primary.overfullnessBefore,
                primary.overfullnessAfter,
                CombineMessages(primary.message, secondary.message));
        }

        private static bool IsFamily(
            string relatedMilkId,
            string foodId,
            string exactMilkId,
            params string[] markers)
        {
            if (string.Equals(relatedMilkId, exactMilkId, StringComparison.Ordinal))
            {
                return true;
            }

            foreach (var marker in markers)
            {
                if ((!string.IsNullOrWhiteSpace(relatedMilkId)
                        && relatedMilkId.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrWhiteSpace(foodId)
                        && foodId.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }
            }

            return false;
        }

        private static int SaturatingMultiply(int value, int multiplier)
        {
            var result = (long)Math.Max(0, value) * Math.Max(0, multiplier);
            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        private enum AftereffectKind
        {
            BodyChill,
            FermentedAftertaste,
            SleepRhythm
        }

        private static int SaturatingIncrement(int value)
        {
            return value >= int.MaxValue ? int.MaxValue : value + 1;
        }

        private static int SaturatingAddAndClamp(int current, int amount, int maximum)
        {
            var result = (long)current + Math.Max(0, amount);
            return result >= maximum ? maximum : (int)result;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
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
