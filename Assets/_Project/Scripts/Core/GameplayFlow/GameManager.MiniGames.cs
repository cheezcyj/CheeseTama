using System;
using CheeseTama.Gameplay.MiniGames;

namespace CheeseTama.Core
{
    public sealed partial class GameManager
    {
        public string PlayMilkDropCatch()
        {
            return CompleteMilkDropMiniGame(5, 0, MilkDropMiniGameRules.CalculateScore(5)).message;
        }

        public MilkDropMiniGameRewardStatus GetMilkDropMiniGameRewardStatus()
        {
            return GetMilkDropMiniGameRewardStatus(DateTimeOffset.Now);
        }

        public MilkDropMiniGameRewardResult CompleteMilkDropMiniGame(int caught, int missed, int score)
        {
            return CompleteMilkDropMiniGame(caught, missed, score, DateTimeOffset.Now, true);
        }

        public MilkDropMiniGameRewardResult CompleteMilkDropMiniGame(
            int caught,
            int missed,
            int score,
            bool allowCurrencyReward)
        {
            return CompleteMilkDropMiniGame(
                caught,
                missed,
                score,
                DateTimeOffset.Now,
                allowCurrencyReward);
        }

        public CleaningMiniGameCompletionResult CompleteCleaningMiniGame(
            int cleanedSpots,
            int missedSpots,
            int score)
        {
            var completedAt = DateTimeOffset.Now;
            var safeCleaned = Math.Max(0, cleanedSpots);
            var safeMissed = Math.Max(0, missedSpots);
            var safeScore = CleaningMiniGameRules.ClampReportedScore(safeCleaned, score);
            if (CurrentSave == null || CurrentTama == null)
            {
                return new CleaningMiniGameCompletionResult(
                    safeScore,
                    safeCleaned,
                    safeMissed,
                    0,
                    "치즈타마 저장 데이터를 불러오지 못했습니다.",
                    false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            CurrentSave.playMiniGames.RecordSession(
                $"cleaning:{Guid.NewGuid():N}",
                MiniGameRecordIds.Cleaning,
                safeScore,
                safeCleaned,
                completedAt.ToString("O"));
            if (!CleaningMiniGameRules.QualifiesForCareReward(safeCleaned))
            {
                ReconcileMiniGameMastery(completedAt);
                SaveGame();
                return new CleaningMiniGameCompletionResult(
                    safeScore,
                    safeCleaned,
                    safeMissed,
                    0,
                    $"얼룩을 {CleaningMiniGameRules.MinimumCleanedSpotsForCareReward}개 이상 닦아야 돌봄으로 기록돼요.",
                    false);
            }

            var beforeCleanliness = CurrentTama.stats != null
                ? CurrentTama.stats.cleanliness
                : 0;
            var careActions = CreateCareActionSystem();
            var careResult = careActions.Clean(CurrentTama);
            var afterCleanliness = CurrentTama.stats != null
                ? CurrentTama.stats.cleanliness
                : beforeCleanliness;
            RegisterCareAction("clean");
            var dailyCompleted = RegisterDailyCareAction("clean");
            RefreshDerivedCollectionRecords();
            ReconcileMiniGameMastery(completedAt);
            SaveGame();

            var message = careResult.message ?? string.Empty;
            if (dailyCompleted)
            {
                message = CombineMessages(message, DailyRoutineRewardMessage);
            }

            return new CleaningMiniGameCompletionResult(
                safeScore,
                safeCleaned,
                safeMissed,
                Math.Max(0, afterCleanliness - beforeCleanliness),
                message,
                careResult.success);
        }

        public BouncyJumpCompletionResult CompleteBouncyJumpMiniGame(
            int successfulJumps,
            int missedJumps,
            int score,
            int highestCombo)
        {
            var completedAt = DateTimeOffset.Now;
            var sessionResult = BouncyJumpMiniGameRules.Complete(
                successfulJumps,
                missedJumps,
                score,
                highestCombo);
            if (CurrentSave == null || CurrentTama == null)
            {
                return new BouncyJumpCompletionResult(
                    false,
                    sessionResult.successes,
                    sessionResult.misses,
                    sessionResult.score,
                    0,
                    "치즈타마 저장 데이터를 불러오지 못했습니다.");
            }

            CurrentSave.EnsureRuntimeDefaults();
            var playSave = CurrentSave.playMiniGames;
            playSave.RecordSession(
                $"bouncy-jump:{Guid.NewGuid():N}",
                MiniGameRecordIds.BouncyJump,
                sessionResult.score,
                sessionResult.successes,
                completedAt.ToString("O"));

            if (!sessionResult.qualifiesForCare)
            {
                ReconcileMiniGameMastery(completedAt);
                SaveGame();
                return new BouncyJumpCompletionResult(
                    false,
                    sessionResult.successes,
                    sessionResult.misses,
                    sessionResult.score,
                    playSave.highestBouncyJumpScore,
                    $"성공 {BouncyJumpMiniGameRules.MinimumSuccessfulJumpsForCare}회부터 놀이 돌봄으로 기록돼요. 최고 점수는 저장했어요.");
            }

            var careActions = CreateCareActionSystem();
            var careResult = careActions.Play(CurrentTama);
            RegisterCareAction("play");
            var dailyCompleted = RegisterDailyCareAction("play");
            AddUniqueRecord(CurrentSave.collections.events, BouncyJumpEventId);
            RefreshDerivedCollectionRecords();
            ReconcileMiniGameMastery(completedAt);
            SaveGame();

            var message = CombineMessages("말랑 점프 기록과 최고 점수를 저장했어요.", careResult.message);
            if (dailyCompleted)
            {
                message = CombineMessages(message, DailyRoutineRewardMessage);
            }

            return new BouncyJumpCompletionResult(
                true,
                sessionResult.successes,
                sessionResult.misses,
                sessionResult.score,
                playSave.highestBouncyJumpScore,
                message);
        }

        public BlueBallCompletionResult CompleteBlueBallMiniGame(
            int hits,
            int misses,
            int score,
            int highestCombo)
        {
            var completedAt = DateTimeOffset.Now;
            var session = BlueBallMiniGameRules.Complete(
                hits,
                misses,
                score,
                highestCombo);
            if (CurrentSave == null || CurrentTama == null)
            {
                return new BlueBallCompletionResult(
                    false,
                    session.hits,
                    session.misses,
                    session.score,
                    0,
                    "치즈타마 저장 데이터를 불러오지 못했습니다.");
            }

            CurrentSave.EnsureRuntimeDefaults();
            var playSave = CurrentSave.playMiniGames;
            playSave.RecordSession(
                $"blue-ball:{Guid.NewGuid():N}",
                MiniGameRecordIds.BlueBall,
                session.score,
                session.hits,
                completedAt.ToString("O"));

            if (!session.qualifiesForCare)
            {
                ReconcileMiniGameMastery(completedAt);
                SaveGame();
                return new BlueBallCompletionResult(
                    false,
                    session.hits,
                    session.misses,
                    session.score,
                    playSave.highestBlueBallScore,
                    $"공을 {BlueBallMiniGameRules.MinimumHitsForCare}번 이상 맞히면 놀이 돌봄으로 기록돼요. 최고 점수는 저장했어요.");
            }

            var careResult = CreateCareActionSystem().Play(CurrentTama);
            RegisterCareAction("play");
            var dailyCompleted = RegisterDailyCareAction("play");
            AddUniqueRecord(CurrentSave.collections.events, BlueBallEventId);
            RefreshDerivedCollectionRecords();
            ReconcileMiniGameMastery(completedAt);
            SaveGame();

            var message = CombineMessages("파란 공 놀이 기록과 최고 점수를 저장했어요.", careResult.message);
            if (dailyCompleted)
            {
                message = CombineMessages(message, DailyRoutineRewardMessage);
            }

            return new BlueBallCompletionResult(
                true,
                session.hits,
                session.misses,
                session.score,
                playSave.highestBlueBallScore,
                message);
        }

        public MilkDropMiniGameRewardResult CompleteMilkDropMiniGame(
            int caught,
            int missed,
            int score,
            DateTimeOffset completedAt)
        {
            return CompleteMilkDropMiniGame(caught, missed, score, completedAt, true);
        }

        private MilkDropMiniGameRewardResult CompleteMilkDropMiniGame(
            int caught,
            int missed,
            int score,
            DateTimeOffset completedAt,
            bool allowCurrencyReward)
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return new MilkDropMiniGameRewardResult(
                    0,
                    0,
                    Math.Max(0, missed),
                    0,
                    0,
                    "치즈타마 저장 데이터를 불러오지 못했습니다.");
            }

            CurrentSave.EnsureRuntimeDefaults();
            EnsureMilkroomPresenceSession();
            var rewardStatus = GetMilkDropMiniGameRewardStatus(completedAt);
            var calculatedReward = MilkDropMiniGameRules.CalculateReward(caught, missed, score);
            var reward = calculatedReward;
            var canGrantCurrencyReward = allowCurrencyReward
                && rewardStatus.isAvailable
                && calculatedReward.HasReward;
            if (canGrantCurrencyReward)
            {
                CurrentSave.milkroomSession.lastMilkDropMiniGameRewardAtIso = completedAt.ToString("O");
            }
            else if (!rewardStatus.isAvailable || !allowCurrencyReward)
            {
                if (rewardStatus.shouldRepairTimestamp)
                {
                    CurrentSave.milkroomSession.lastMilkDropMiniGameRewardAtIso = completedAt.ToString("O");
                }

                var cooldownMessageSuffix = rewardStatus.remainingSeconds > 0
                    ? $"{MilkDropMiniGameRules.FormatCooldown(rewardStatus.remainingSeconds)} 뒤에 다시 받을 수 있어요"
                    : "다음 판부터 다시 받을 수 있어요";
                var cooldownMessage = calculatedReward.caught > 0
                    ? $"우유방울 {calculatedReward.caught}개를 받았어요! 미니게임 자원 보상은 {cooldownMessageSuffix}. 이번 판은 점수와 돌봄만 기록됐어요."
                    : $"이번에는 우유방울을 받지 못했어요. 미니게임 자원 보상은 {cooldownMessageSuffix}.";
                reward = new MilkDropMiniGameRewardResult(
                    calculatedReward.score,
                    calculatedReward.caught,
                    calculatedReward.missed,
                    0,
                    0,
                    cooldownMessage,
                    false,
                    rewardStatus.remainingSeconds);
            }

            CurrentSave.economy.milkCoins = SaturatingAdd(CurrentSave.economy.milkCoins, reward.milkCoins);
            CurrentSave.economy.milkDrops = SaturatingAdd(CurrentSave.economy.milkDrops, reward.milkDrops);
            CurrentSave.playMiniGames.RecordSession(
                $"milk-drop:{Guid.NewGuid():N}",
                MiniGameRecordIds.MilkDrop,
                reward.score,
                reward.caught,
                completedAt.ToString("O"));

            if (reward.caught > 0)
            {
                CurrentSave.milkroomSession.todayMilkDropCatches = SaturatingAdd(
                    CurrentSave.milkroomSession.todayMilkDropCatches,
                    1);
                CurrentSave.milkroomSession.totalMilkDropCatches = SaturatingAdd(
                    CurrentSave.milkroomSession.totalMilkDropCatches,
                    1);
                AddUniqueRecord(CurrentSave.collections.events, MilkDropCatchEventId);
            }

            var careActions = CreateCareActionSystem();
            var careResult = careActions.Play(CurrentTama);
            RegisterCareAction("play");
            // Practice runs still count toward today's play goal, but cannot pay the
            // daily-routine bundle from the same no-reward result screen.
            var dailyCompleted = RegisterDailyCareAction(
                "play",
                allowCurrencyReward && rewardStatus.isAvailable);
            RefreshDerivedCollectionRecords();
            ReconcileMiniGameMastery(completedAt);
            SaveGame();

            var message = reward.message;
            if (!string.IsNullOrWhiteSpace(careResult.message))
            {
                message = $"{message} {careResult.message}";
            }

            if (dailyCompleted)
            {
                message = $"{message} {DailyRoutineRewardMessage}";
            }

            return new MilkDropMiniGameRewardResult(
                reward.score,
                reward.caught,
                reward.missed,
                reward.milkCoins,
                reward.milkDrops,
                message,
                reward.currencyRewardGranted,
                reward.rewardCooldownRemainingSeconds);
        }

        private MilkDropMiniGameRewardStatus GetMilkDropMiniGameRewardStatus(DateTimeOffset now)
        {
            if (CurrentSave == null)
            {
                return new MilkDropMiniGameRewardStatus(
                    false,
                    MilkDropMiniGameRules.RewardCooldownSeconds,
                    false);
            }

            CurrentSave.EnsureRuntimeDefaults();
            return MilkDropMiniGameRules.EvaluateRewardCooldown(
                CurrentSave.milkroomSession.lastMilkDropMiniGameRewardAtIso,
                now);
        }
    }
}
