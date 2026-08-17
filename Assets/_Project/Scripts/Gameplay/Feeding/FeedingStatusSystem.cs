using System;

namespace CheeseTama.Gameplay.Feeding
{
    public readonly struct FeedingStatusResult
    {
        public readonly bool milkAversionActivated;
        public readonly bool milkAversionRecovered;
        public readonly bool overfullnessActivated;
        public readonly bool overfullnessRecovered;
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
        {
            this.milkAversionActivated = milkAversionActivated;
            this.milkAversionRecovered = milkAversionRecovered;
            this.overfullnessActivated = overfullnessActivated;
            this.overfullnessRecovered = overfullnessRecovered;
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

        public bool IsMilkAversionActive(CheeseTamaModel tama)
        {
            return tama?.growthHistory != null
                && tama.growthHistory.sameMilkFeedStreak >= MilkAversionStreakThreshold;
        }

        public bool IsOverfull(CheeseTamaModel tama)
        {
            return tama?.stats != null && tama.stats.overfullness > 0;
        }

        public FeedingStatusResult ApplyMilk(
            CheeseTamaModel tama,
            string milkId,
            int hungerBefore,
            int hungerGain)
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
            return new FeedingStatusResult(
                milkAversionActivated,
                milkAversionRecovered,
                overfullness.overfullnessActivated,
                false,
                overfullness.overfullnessBefore,
                overfullness.overfullnessAfter,
                CombineMessages(message, overfullness.message));
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

        public FeedingStatusResult RecoverByPlay(CheeseTamaModel tama)
        {
            return RecoverOverfullness(
                tama,
                OverfullnessRecoveryPerPlay,
                "가볍게 놀며 과포만이 조금 가라앉았어요.",
                "가볍게 놀며 소화해 과포만에서 회복했어요.");
        }

        public FeedingStatusResult RecoverByTime(CheeseTamaModel tama, int hours)
        {
            if (hours <= 0)
            {
                return FeedingStatusResult.None(tama?.stats?.overfullness ?? 0);
            }

            var recovery = (long)OverfullnessRecoveryPerHour * hours;
            return RecoverOverfullness(
                tama,
                recovery > int.MaxValue ? int.MaxValue : (int)recovery,
                "시간이 지나 과포만이 조금 가라앉았어요.",
                "시간이 지나 과포만에서 회복했어요.");
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
