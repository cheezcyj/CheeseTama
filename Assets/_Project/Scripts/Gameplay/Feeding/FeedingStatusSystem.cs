using System;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Snacks;

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
        public readonly bool heavinessActivated;
        public readonly bool heavinessRecovered;
        public readonly bool sugarOverloadActivated;
        public readonly bool sugarOverloadRecovered;
        public readonly bool fantasyEchoActivated;
        public readonly bool fantasyEchoRecovered;
        public readonly bool lethargyActivated;
        public readonly bool lethargyRecovered;
        public readonly bool stomachRiskActivated;
        public readonly bool stomachRiskRecovered;
        public readonly bool traitDistortionActivated;
        public readonly bool traitDistortionRecovered;
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
            : this(
                milkAversionActivated,
                milkAversionRecovered,
                overfullnessActivated,
                overfullnessRecovered,
                bodyChillActivated,
                bodyChillRecovered,
                fermentedAftertasteActivated,
                fermentedAftertasteRecovered,
                sleepRhythmDisruptionActivated,
                sleepRhythmDisruptionRecovered,
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
            bool heavinessActivated,
            bool heavinessRecovered,
            bool sugarOverloadActivated,
            bool sugarOverloadRecovered,
            bool fantasyEchoActivated,
            bool fantasyEchoRecovered,
            int overfullnessBefore,
            int overfullnessAfter,
            string message)
            : this(
                milkAversionActivated,
                milkAversionRecovered,
                overfullnessActivated,
                overfullnessRecovered,
                bodyChillActivated,
                bodyChillRecovered,
                fermentedAftertasteActivated,
                fermentedAftertasteRecovered,
                sleepRhythmDisruptionActivated,
                sleepRhythmDisruptionRecovered,
                heavinessActivated,
                heavinessRecovered,
                sugarOverloadActivated,
                sugarOverloadRecovered,
                fantasyEchoActivated,
                fantasyEchoRecovered,
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
            bool heavinessActivated,
            bool heavinessRecovered,
            bool sugarOverloadActivated,
            bool sugarOverloadRecovered,
            bool fantasyEchoActivated,
            bool fantasyEchoRecovered,
            bool lethargyActivated,
            bool lethargyRecovered,
            bool stomachRiskActivated,
            bool stomachRiskRecovered,
            bool traitDistortionActivated,
            bool traitDistortionRecovered,
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
            this.heavinessActivated = heavinessActivated;
            this.heavinessRecovered = heavinessRecovered;
            this.sugarOverloadActivated = sugarOverloadActivated;
            this.sugarOverloadRecovered = sugarOverloadRecovered;
            this.fantasyEchoActivated = fantasyEchoActivated;
            this.fantasyEchoRecovered = fantasyEchoRecovered;
            this.lethargyActivated = lethargyActivated;
            this.lethargyRecovered = lethargyRecovered;
            this.stomachRiskActivated = stomachRiskActivated;
            this.stomachRiskRecovered = stomachRiskRecovered;
            this.traitDistortionActivated = traitDistortionActivated;
            this.traitDistortionRecovered = traitDistortionRecovered;
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
        public const int HeavinessIntensityPerFeed = 30;
        public const int HeavinessDurationPerFeed = 5;
        public const int SugarOverloadIntensityPerFeed = 35;
        public const int SugarOverloadDurationPerFeed = 5;
        public const int FantasyEchoIntensityPerFeed = 40;
        public const int FantasyEchoDurationPerFeed = 8;
        public const int LethargyIntensityPerFeed = 30;
        public const int LethargyDurationPerFeed = 5;
        public const int StomachRiskIntensityPerFeed = 35;
        public const int StomachRiskDurationPerFeed = 6;
        public const int TraitDistortionIntensityPerFeed = 40;
        public const int TraitDistortionDurationPerFeed = 8;
        public const int HeavinessRecoveryPerHour = 8;
        public const int SugarOverloadRecoveryPerHour = 10;
        public const int FantasyEchoRecoveryPerHour = 6;
        public const int LethargyRecoveryPerHour = 10;
        public const int StomachRiskRecoveryPerHour = 8;
        public const int TraitDistortionRecoveryPerHour = 6;
        public const int MaximumHeavinessCarePenaltyPercent = 30;
        public const int MaximumSugarCarePenaltyPercent = 20;
        public const int MaximumFantasyCarePenaltyPercent = 15;
        public const int MaximumLethargyCarePenaltyPercent = 15;
        public const int MaximumStomachRiskCarePenaltyPercent = 20;
        public const int MaximumTraitDistortionCarePenaltyPercent = 10;
        public const int StomachRiskHealthPenaltyPerOvereating = 5;
        public const int MinimumCareEfficiencyPercent = 40;
        public const string DefaultSweetSnackId = "default_sweet_snack";

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

        public bool IsHeavinessActive(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.heavinessIntensity > 0
                && tama.stats.heavinessHoursRemaining > 0;
        }

        public bool IsSugarOverloadActive(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.sugarOverloadIntensity > 0
                && tama.stats.sugarOverloadHoursRemaining > 0;
        }

        public bool IsFantasyEchoActive(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.fantasyEchoIntensity > 0
                && tama.stats.fantasyEchoHoursRemaining > 0;
        }

        public bool IsLethargyActive(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.lethargyIntensity > 0
                && tama.stats.lethargyHoursRemaining > 0;
        }

        public bool IsStomachRiskActive(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.stomachRiskIntensity > 0
                && tama.stats.stomachRiskHoursRemaining > 0;
        }

        public bool IsTraitDistortionActive(CheeseTamaModel tama)
        {
            return tama?.stats != null
                && tama.stats.traitDistortionIntensity > 0
                && tama.stats.traitDistortionHoursRemaining > 0;
        }

        public static bool IsNight(DateTimeOffset localTime)
        {
            return localTime.Hour >= NightStartHour || localTime.Hour < NightEndHour;
        }

        public int CalculateCareProgress(CheeseTamaModel tama, int baseProgress)
        {
            var safeProgress = Math.Max(0, baseProgress);
            if (safeProgress == 0 || tama?.stats == null)
            {
                return safeProgress;
            }

            var penaltyPercent = 0;
            if (IsHeavinessActive(tama))
            {
                penaltyPercent += CalculateScaledPenalty(
                    tama.stats.heavinessIntensity,
                    MaximumHeavinessCarePenaltyPercent);
            }

            if (IsSugarOverloadActive(tama))
            {
                penaltyPercent += CalculateScaledPenalty(
                    tama.stats.sugarOverloadIntensity,
                    MaximumSugarCarePenaltyPercent);
            }

            if (IsFantasyEchoActive(tama))
            {
                penaltyPercent += CalculateScaledPenalty(
                    tama.stats.fantasyEchoIntensity,
                    MaximumFantasyCarePenaltyPercent);
            }

            if (IsLethargyActive(tama))
            {
                penaltyPercent += CalculateScaledPenalty(
                    tama.stats.lethargyIntensity,
                    MaximumLethargyCarePenaltyPercent);
            }

            if (IsStomachRiskActive(tama))
            {
                penaltyPercent += CalculateScaledPenalty(
                    tama.stats.stomachRiskIntensity,
                    MaximumStomachRiskCarePenaltyPercent);
            }

            if (IsTraitDistortionActive(tama))
            {
                penaltyPercent += CalculateScaledPenalty(
                    tama.stats.traitDistortionIntensity,
                    MaximumTraitDistortionCarePenaltyPercent);
            }

            var efficiencyPercent = Math.Max(MinimumCareEfficiencyPercent, 100 - penaltyPercent);
            return Math.Max(1, (int)((long)safeProgress * efficiencyPercent / 100L));
        }

        public string BuildCareEfficiencyMessage(
            CheeseTamaModel tama,
            int baseProgress,
            int appliedProgress)
        {
            var safeBase = Math.Max(0, baseProgress);
            var safeApplied = Math.Max(0, Math.Min(safeBase, appliedProgress));
            if (safeApplied >= safeBase || tama?.stats == null)
            {
                return string.Empty;
            }

            var active = new System.Collections.Generic.List<string>(6);
            if (IsHeavinessActive(tama))
            {
                active.Add("무거움");
            }

            if (IsSugarOverloadActive(tama))
            {
                active.Add("단것 과다");
            }

            if (IsFantasyEchoActive(tama))
            {
                active.Add("환상 잔향");
            }

            if (IsLethargyActive(tama))
            {
                active.Add("나른함");
            }

            if (IsStomachRiskActive(tama))
            {
                active.Add("배탈 위험");
            }

            if (IsTraitDistortionActive(tama))
            {
                active.Add("성격 흔들림");
            }

            return active.Count == 0
                ? string.Empty
                : $"{string.Join("·", active)} 후유증으로 돌봄 효율이 낮아 성장 진행도가 {safeApplied}만큼 올랐어요.";
        }

        public string BuildTemporaryPreferenceMessage(CheeseTamaModel tama)
        {
            if (!IsTraitDistortionActive(tama))
            {
                return string.Empty;
            }

            var intensity = Clamp(tama.stats.traitDistortionIntensity, 0, MaximumAftereffectIntensity);
            if (intensity >= 70)
            {
                return "성격이 잠깐 달라져서 조용한 휴식을 가장 편안해해요.";
            }

            return intensity >= 40
                ? "성격이 잠깐 달라져서 청소 소리에 관심을 보여요."
                : "성격이 잠깐 달라져서 평소와 다른 놀이를 좋아해요.";
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
                hungerBefore,
                hungerGain,
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
            var overfullness = AccumulateOverfullness(tama, hungerBefore, hungerGain);
            var aftereffects = ApplyIdentifiedAftereffects(
                tama,
                string.Empty,
                DefaultSweetSnackId,
                hungerBefore,
                hungerGain,
                null);
            return Merge(overfullness, aftereffects);
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
                hungerBefore,
                hungerGain,
                localTime);
            return Merge(overfullness, aftereffects);
        }

        public FeedingStatusResult RecoverByPlay(CheeseTamaModel tama)
        {
            if (tama?.stats == null)
            {
                return FeedingStatusResult.None();
            }

            tama.EnsureRuntimeDefaults();
            var result = RecoverOverfullness(
                tama,
                OverfullnessRecoveryPerPlay,
                "가볍게 놀아서 너무 배부른 느낌이 조금 나아졌어요.",
                "가볍게 놀고 소화해서 편안해졌어요.");
            result = Merge(
                result,
                RecoverLethargy(tama, 55, 5, "몸을 움직이며 나른함이 걷혔어요."));
            return Merge(
                result,
                RecoverTraitDistortion(tama, 15, 1, "익숙한 놀이를 하며 뒤틀린 취향이 조금 돌아왔어요."));
        }

        public FeedingStatusResult RecoverByClean(CheeseTamaModel tama)
        {
            if (tama?.stats == null)
            {
                return FeedingStatusResult.None();
            }

            tama.EnsureRuntimeDefaults();
            var result = Merge(
                FeedingStatusResult.None(tama.stats.overfullness),
                RecoverFermentedAftertaste(tama, 60, 6, "입가와 방을 닦아 발효 뒷맛이 옅어졌어요."));
            result = Merge(
                result,
                RecoverSugarOverload(tama, 60, 6, "가볍게 정리하고 물을 마셔서 단것 과다가 나아졌어요."));
            result = Merge(
                result,
                RecoverStomachRisk(tama, 20, 2, "주변을 정돈하고 천천히 움직여 배탈 위험이 조금 낮아졌어요."));
            result = Merge(
                result,
                RecoverTraitDistortion(tama, 25, 2, "익숙한 밀크룸을 정리하며 뒤틀린 취향이 차분해졌어요."));
            return Merge(
                result,
                RecoverFantasyEcho(tama, 10, 1, "밀크룸을 정돈하자 환상 잔향이 조금 맑아졌어요."));
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
            result = Merge(
                result,
                RecoverSleepRhythm(tama, 50, 6, "차분히 쉬며 흐트러진 수면 리듬을 되찾았어요."));
            result = Merge(
                result,
                RecoverHeaviness(tama, 60, 6, "천천히 쉬며 몸의 무거움이 풀렸어요."));
            result = Merge(
                result,
                RecoverSugarOverload(tama, 20, 2, "차분히 쉬며 들뜬 당기운이 조금 가라앉았어요."));
            result = Merge(
                result,
                RecoverLethargy(tama, 25, 2, "편안히 쉬어 남은 나른함이 조금 옅어졌어요."));
            result = Merge(
                result,
                RecoverStomachRisk(tama, 60, 6, "배를 편안히 하고 쉬며 배탈 위험이 가라앉았어요."));
            result = Merge(
                result,
                RecoverTraitDistortion(tama, 45, 4, "조용히 쉬며 뒤틀린 취향이 제자리로 돌아왔어요."));
            return Merge(
                result,
                RecoverFantasyEcho(tama, 20, 2, "조용히 쉬며 환상 잔향이 조금 잦아들었어요."));
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
            result = Merge(
                result,
                RecoverSleepRhythm(tama, 10, 2, "따뜻한 우유가 흐트러진 수면 리듬을 조금 진정시켰어요."));
            result = Merge(
                result,
                RecoverHeaviness(tama, 45, 4, "따뜻한 우유를 천천히 마시며 무거움이 풀렸어요."));
            result = Merge(
                result,
                RecoverStomachRisk(tama, 50, 5, "따뜻한 우유로 속을 달래 배탈 위험이 낮아졌어요."));
            result = Merge(
                result,
                RecoverTraitDistortion(tama, 60, 6, "익숙한 온기가 뒤틀린 취향을 되돌렸어요."));
            return Merge(
                result,
                RecoverFantasyEcho(tama, 60, 6, "따뜻한 우유의 온기가 환상 잔향을 씻어 냈어요."));
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
                "시간이 지나 너무 배부른 느낌이 조금 나아졌어요.",
                "시간이 지나 배가 편안해졌어요.");
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
            result = Merge(
                result,
                RecoverSleepRhythm(
                    tama,
                    SaturatingMultiply(SleepRhythmRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 수면 리듬이 조금씩 돌아왔어요."));
            result = Merge(
                result,
                RecoverHeaviness(
                    tama,
                    SaturatingMultiply(HeavinessRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 몸의 무거움이 가벼워졌어요."));
            result = Merge(
                result,
                RecoverSugarOverload(
                    tama,
                    SaturatingMultiply(SugarOverloadRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 단것 과다가 나아졌어요."));
            result = Merge(
                result,
                RecoverFantasyEcho(
                    tama,
                    SaturatingMultiply(FantasyEchoRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 환상 잔향이 옅어졌어요."));
            result = Merge(
                result,
                RecoverLethargy(
                    tama,
                    SaturatingMultiply(LethargyRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 나른함이 걷혔어요."));
            result = Merge(
                result,
                RecoverStomachRisk(
                    tama,
                    SaturatingMultiply(StomachRiskRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 배탈 위험이 낮아졌어요."));
            return Merge(
                result,
                RecoverTraitDistortion(
                    tama,
                    SaturatingMultiply(TraitDistortionRecoveryPerHour, hours),
                    hours,
                    "시간이 지나 뒤틀린 취향이 제자리로 돌아왔어요."));
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
                ? "배가 너무 불러요. 잠시 기다리거나 가볍게 놀아 주세요."
                : "먹이를 더 먹어서 배가 더 불러졌어요. 잠시 기다리거나 가볍게 놀아 주세요.";
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
            int hungerBefore,
            int hungerGain,
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
                result = Merge(
                    result,
                    RecoverHeaviness(tama, 45, 4, "따뜻한 우유를 천천히 마시며 무거움이 풀렸어요."));
                result = Merge(
                    result,
                    RecoverFantasyEcho(tama, 60, 6, "따뜻한 우유의 온기가 환상 잔향을 씻어 냈어요."));
                result = Merge(
                    result,
                    RecoverStomachRisk(tama, 50, 5, "따뜻한 우유로 속을 달래 배탈 위험이 낮아졌어요."));
                result = Merge(
                    result,
                    RecoverTraitDistortion(tama, 60, 6, "익숙한 온기가 뒤틀린 취향을 되돌렸어요."));
            }

            if (IsFamily(relatedMilkId, foodId, MilkCatalog.CoffeeMilkId, "coffee"))
            {
                result = Merge(
                    result,
                    RecoverLethargy(tama, 60, 6, "커피 향에 정신이 맑아져 나른함이 가라앉았어요."));
            }

            if (IsFamily(relatedMilkId, foodId, MilkCatalog.FermentedMilkId, "fermented", "yogurt"))
            {
                result = Merge(result, ActivateFermentedAftertaste(tama));
            }

            if (IsRichOrHighFatFood(relatedMilkId, foodId))
            {
                result = Merge(result, ActivateHeaviness(tama));
            }

            if (IsSweetSnack(foodId))
            {
                result = Merge(result, ActivateSugarOverload(tama));
            }

            if (IsFantasyPowderFood(foodId))
            {
                result = Merge(result, ActivateFantasyEcho(tama));
            }

            var overeating = IsOvereating(hungerBefore, hungerGain);
            if (localTime.HasValue
                && !IsNight(localTime.Value)
                && overeating
                && IsLethargyFood(relatedMilkId, foodId))
            {
                result = Merge(result, ActivateLethargy(tama));
            }

            if (overeating && !string.IsNullOrWhiteSpace(foodId))
            {
                result = Merge(result, ActivateStomachRisk(tama));
            }

            if (IsTraitDistortionFood(foodId))
            {
                result = Merge(result, ActivateTraitDistortion(tama));
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

        private static FeedingStatusResult ActivateHeaviness(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.heavinessIntensity > 0
                && tama.stats.heavinessHoursRemaining > 0;
            tama.stats.heavinessIntensity = SaturatingAddAndClamp(
                tama.stats.heavinessIntensity,
                HeavinessIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.heavinessHoursRemaining = SaturatingAddAndClamp(
                tama.stats.heavinessHoursRemaining,
                HeavinessDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.Heaviness,
                !wasActive,
                false,
                wasActive
                    ? "진한 먹이가 겹쳐 몸의 무거움이 더 오래가요."
                    : "진한·고지방 먹이로 몸이 무거워졌어요. 휴식이나 따뜻한 우유가 좋아요.");
        }

        private static FeedingStatusResult ActivateSugarOverload(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.sugarOverloadIntensity > 0
                && tama.stats.sugarOverloadHoursRemaining > 0;
            tama.stats.sugarOverloadIntensity = SaturatingAddAndClamp(
                tama.stats.sugarOverloadIntensity,
                SugarOverloadIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.sugarOverloadHoursRemaining = SaturatingAddAndClamp(
                tama.stats.sugarOverloadHoursRemaining,
                SugarOverloadDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.SugarOverload,
                !wasActive,
                false,
                wasActive
                    ? "단 간식을 또 먹어서 단것 과다가 더 오래가요."
                    : "단 간식을 많이 먹었어요. 청소하거나 차분히 쉬어 주세요.");
        }

        private static FeedingStatusResult ActivateFantasyEcho(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.fantasyEchoIntensity > 0
                && tama.stats.fantasyEchoHoursRemaining > 0;
            tama.stats.fantasyEchoIntensity = SaturatingAddAndClamp(
                tama.stats.fantasyEchoIntensity,
                FantasyEchoIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.fantasyEchoHoursRemaining = SaturatingAddAndClamp(
                tama.stats.fantasyEchoHoursRemaining,
                FantasyEchoDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.FantasyEcho,
                !wasActive,
                false,
                wasActive
                    ? "환상가루의 잔향이 겹쳐 감각이 더 오래 몽글거려요."
                    : "환상가루 계열 음식의 잔향이 남았어요. 따뜻한 우유나 휴식으로 가라앉혀 주세요.");
        }

        private static FeedingStatusResult ActivateLethargy(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.lethargyIntensity > 0
                && tama.stats.lethargyHoursRemaining > 0;
            tama.stats.lethargyIntensity = SaturatingAddAndClamp(
                tama.stats.lethargyIntensity,
                LethargyIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.lethargyHoursRemaining = SaturatingAddAndClamp(
                tama.stats.lethargyHoursRemaining,
                LethargyDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.Lethargy,
                !wasActive,
                false,
                wasActive
                    ? "낮에 포근한 먹이를 또 과하게 먹어 나른함이 더 오래가요."
                    : "낮에 포근한 먹이를 과하게 먹어 몸이 나른해졌어요. 가볍게 놀거나 시간이 지나면 회복해요.");
        }

        private static FeedingStatusResult ActivateStomachRisk(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.stomachRiskIntensity > 0
                && tama.stats.stomachRiskHoursRemaining > 0;
            tama.stats.stomachRiskIntensity = SaturatingAddAndClamp(
                tama.stats.stomachRiskIntensity,
                StomachRiskIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.stomachRiskHoursRemaining = SaturatingAddAndClamp(
                tama.stats.stomachRiskHoursRemaining,
                StomachRiskDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.StomachRisk,
                !wasActive,
                false,
                wasActive
                    ? "간식을 또 과하게 먹어 배탈 위험이 더 오래가요."
                    : "간식을 과하게 먹어 속이 예민해졌어요. 따뜻한 우유나 휴식으로 달래 주세요.");
        }

        private static FeedingStatusResult ActivateTraitDistortion(CheeseTamaModel tama)
        {
            var wasActive = tama.stats.traitDistortionIntensity > 0
                && tama.stats.traitDistortionHoursRemaining > 0;
            tama.stats.traitDistortionIntensity = SaturatingAddAndClamp(
                tama.stats.traitDistortionIntensity,
                TraitDistortionIntensityPerFeed,
                MaximumAftereffectIntensity);
            tama.stats.traitDistortionHoursRemaining = SaturatingAddAndClamp(
                tama.stats.traitDistortionHoursRemaining,
                TraitDistortionDurationPerFeed,
                MaximumAftereffectDurationHours);
            return CreateAftereffectResult(
                tama,
                AftereffectKind.TraitDistortion,
                !wasActive,
                false,
                wasActive
                    ? "환상가루의 기운이 겹쳐 평소 취향이 더 오래 흔들려요."
                    : "환상가루 계열 먹이로 평소 성향이 잠시 뒤틀렸어요. 익숙한 돌봄으로 되돌릴 수 있어요.");
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

        private static FeedingStatusResult RecoverHeaviness(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.heavinessIntensity,
                ref tama.stats.heavinessHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.Heaviness, false, recovered, message)
                : FeedingStatusResult.None(tama.stats.overfullness);
        }

        private static FeedingStatusResult RecoverSugarOverload(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.sugarOverloadIntensity,
                ref tama.stats.sugarOverloadHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.SugarOverload, false, recovered, message)
                : FeedingStatusResult.None(tama.stats.overfullness);
        }

        private static FeedingStatusResult RecoverFantasyEcho(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.fantasyEchoIntensity,
                ref tama.stats.fantasyEchoHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.FantasyEcho, false, recovered, message)
                : FeedingStatusResult.None(tama.stats.overfullness);
        }

        private static FeedingStatusResult RecoverLethargy(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.lethargyIntensity,
                ref tama.stats.lethargyHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.Lethargy, false, recovered, message)
                : FeedingStatusResult.None(tama.stats.overfullness);
        }

        private static FeedingStatusResult RecoverStomachRisk(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.stomachRiskIntensity,
                ref tama.stats.stomachRiskHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.StomachRisk, false, recovered, message)
                : FeedingStatusResult.None(tama.stats.overfullness);
        }

        private static FeedingStatusResult RecoverTraitDistortion(
            CheeseTamaModel tama,
            int intensityRecovery,
            int durationRecovery,
            string message)
        {
            var changed = RecoverAftereffect(
                ref tama.stats.traitDistortionIntensity,
                ref tama.stats.traitDistortionHoursRemaining,
                intensityRecovery,
                durationRecovery,
                out var recovered);
            return changed
                ? CreateAftereffectResult(tama, AftereffectKind.TraitDistortion, false, recovered, message)
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
                kind == AftereffectKind.Heaviness && activated,
                kind == AftereffectKind.Heaviness && recovered,
                kind == AftereffectKind.SugarOverload && activated,
                kind == AftereffectKind.SugarOverload && recovered,
                kind == AftereffectKind.FantasyEcho && activated,
                kind == AftereffectKind.FantasyEcho && recovered,
                kind == AftereffectKind.Lethargy && activated,
                kind == AftereffectKind.Lethargy && recovered,
                kind == AftereffectKind.StomachRisk && activated,
                kind == AftereffectKind.StomachRisk && recovered,
                kind == AftereffectKind.TraitDistortion && activated,
                kind == AftereffectKind.TraitDistortion && recovered,
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
                primary.heavinessActivated || secondary.heavinessActivated,
                primary.heavinessRecovered || secondary.heavinessRecovered,
                primary.sugarOverloadActivated || secondary.sugarOverloadActivated,
                primary.sugarOverloadRecovered || secondary.sugarOverloadRecovered,
                primary.fantasyEchoActivated || secondary.fantasyEchoActivated,
                primary.fantasyEchoRecovered || secondary.fantasyEchoRecovered,
                primary.lethargyActivated || secondary.lethargyActivated,
                primary.lethargyRecovered || secondary.lethargyRecovered,
                primary.stomachRiskActivated || secondary.stomachRiskActivated,
                primary.stomachRiskRecovered || secondary.stomachRiskRecovered,
                primary.traitDistortionActivated || secondary.traitDistortionActivated,
                primary.traitDistortionRecovered || secondary.traitDistortionRecovered,
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

        private static bool IsRichOrHighFatFood(string relatedMilkId, string foodId)
        {
            return IsFamily(
                relatedMilkId,
                foodId,
                MilkCatalog.RichMilkId,
                "rich",
                "high_fat",
                "high-fat",
                "full_fat",
                "full-fat");
        }

        private static bool IsSweetSnack(string foodId)
        {
            if (string.IsNullOrWhiteSpace(foodId))
            {
                return false;
            }

            return string.Equals(foodId, DefaultSweetSnackId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.SoftSnackDoughId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.ColdMilkPuddingId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.CoffeeMilkJellyId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.CreamSoupId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.StarCreamId, StringComparison.Ordinal)
                || ContainsAnyMarker(foodId, "sweet", "sugar", "candy", "pudding", "jelly", "cream");
        }

        private static bool IsFantasyPowderFood(string foodId)
        {
            if (string.IsNullOrWhiteSpace(foodId))
            {
                return false;
            }

            // These three snacks are the successful outputs of the current
            // fantasy-powder hidden recipes. Their origin is intentionally
            // represented by the catalog family rather than a second inventory.
            return string.Equals(foodId, SnackCatalog.SoftSnackDoughId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.FermentedYogurtBowlId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.CoffeeMilkJellyId, StringComparison.Ordinal)
                || ContainsAnyMarker(foodId, "fantasy", "powder", "hidden_recipe");
        }

        private static bool IsLethargyFood(string relatedMilkId, string foodId)
        {
            return string.Equals(relatedMilkId, MilkCatalog.WarmMilkId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.WarmMilkSoupId, StringComparison.Ordinal)
                || string.Equals(foodId, SnackCatalog.CreamSoupId, StringComparison.Ordinal)
                || ContainsAnyMarker(foodId, "sleepy", "drowsy", "cozy");
        }

        public static bool IsOvereating(int hungerBefore, int hungerGain)
        {
            return (long)Math.Max(0, hungerBefore) + Math.Max(0, hungerGain)
                > OverfullnessRawHungerThreshold;
        }

        private static bool IsTraitDistortionFood(string foodId)
        {
            return IsFantasyPowderFood(foodId)
                || ContainsAnyMarker(foodId, "trait_shift", "personality_shift");
        }

        private static bool ContainsAnyMarker(string value, params string[] markers)
        {
            foreach (var marker in markers)
            {
                if (value.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalculateScaledPenalty(int intensity, int maximumPenaltyPercent)
        {
            var safeIntensity = Clamp(intensity, 0, MaximumAftereffectIntensity);
            var safeMaximum = Math.Max(0, maximumPenaltyPercent);
            if (safeIntensity == 0 || safeMaximum == 0)
            {
                return 0;
            }

            return (int)(((long)safeIntensity * safeMaximum + MaximumAftereffectIntensity - 1L)
                / MaximumAftereffectIntensity);
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
            SleepRhythm,
            Heaviness,
            SugarOverload,
            FantasyEcho,
            Lethargy,
            StomachRisk,
            TraitDistortion
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
