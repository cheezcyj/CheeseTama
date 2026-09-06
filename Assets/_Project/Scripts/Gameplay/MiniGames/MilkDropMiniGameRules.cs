using System;

namespace CheeseTama.Gameplay.MiniGames
{
    public readonly struct MilkDropMiniGameRewardResult
    {
        public MilkDropMiniGameRewardResult(
            int score,
            int caught,
            int missed,
            int milkCoins,
            int milkDrops,
            string message)
            : this(
                score,
                caught,
                missed,
                milkCoins,
                milkDrops,
                message,
                milkCoins > 0 || milkDrops > 0,
                0)
        {
        }

        public MilkDropMiniGameRewardResult(
            int score,
            int caught,
            int missed,
            int milkCoins,
            int milkDrops,
            string message,
            bool currencyRewardGranted,
            int rewardCooldownRemainingSeconds)
        {
            this.score = Math.Max(0, score);
            this.caught = Math.Max(0, caught);
            this.missed = Math.Max(0, missed);
            this.milkCoins = Math.Max(0, milkCoins);
            this.milkDrops = Math.Max(0, milkDrops);
            this.message = message ?? string.Empty;
            this.currencyRewardGranted = currencyRewardGranted
                && (this.milkCoins > 0 || this.milkDrops > 0);
            this.rewardCooldownRemainingSeconds = Math.Max(0, rewardCooldownRemainingSeconds);
        }

        public readonly int score;
        public readonly int caught;
        public readonly int missed;
        public readonly int milkCoins;
        public readonly int milkDrops;
        public readonly string message;
        public readonly bool currencyRewardGranted;
        public readonly int rewardCooldownRemainingSeconds;

        public bool HasReward => milkCoins > 0 || milkDrops > 0;
        public bool IsRewardOnCooldown => !currencyRewardGranted
            && rewardCooldownRemainingSeconds > 0;
    }

    public readonly struct MilkDropMiniGameRewardStatus
    {
        public MilkDropMiniGameRewardStatus(
            bool isAvailable,
            int remainingSeconds,
            bool shouldRepairTimestamp)
        {
            this.isAvailable = isAvailable;
            this.remainingSeconds = Math.Max(0, remainingSeconds);
            this.shouldRepairTimestamp = shouldRepairTimestamp;
        }

        public readonly bool isAvailable;
        public readonly int remainingSeconds;
        public readonly bool shouldRepairTimestamp;
    }

    public static class MilkDropMiniGameRules
    {
        public const float DurationSeconds = 15f;
        public const int PointsPerCatch = 100;
        public const int RewardCooldownMinutes = 30;
        public const int RewardCooldownSeconds = RewardCooldownMinutes * 60;

        // The original 72 px / 0.72 s / 180-300 speed setup was too forgiving.
        public const float DropSizePixels = 56f;
        public const float SpawnIntervalSeconds = 0.48f;
        public const float MinimumFallSpeed = 300f;
        public const float MaximumFallSpeed = 500f;
        public const int InitialPoolSize = 12;
        public const int MaximumPoolSize = 20;
        public const float BasketWidthPixels = 170f;
        public const float BasketHeightPixels = 54f;
        public const float BasketBottomPaddingPixels = 18f;
        public const float BasketKeyboardSpeedPixelsPerSecond = 620f;

        private const int MaximumMilkCoinReward = 30;
        private const int MaximumMilkDropReward = 8;
        private const int ScorePerBonusDrop = 500;

        public static int CalculateScore(int caught)
        {
            if (caught <= 0)
            {
                return 0;
            }

            var score = (long)caught * PointsPerCatch;
            return score > int.MaxValue ? int.MaxValue : (int)score;
        }

        public static float GetRemainingSeconds(float elapsedSeconds)
        {
            return Math.Max(0f, DurationSeconds - Math.Max(0f, elapsedSeconds));
        }

        public static bool IsComplete(float elapsedSeconds)
        {
            return elapsedSeconds >= DurationSeconds;
        }

        public static float ClampBasketCenterX(
            float playAreaWidth,
            float basketWidth,
            float requestedCenterX)
        {
            var safeAreaWidth = IsFinite(playAreaWidth) ? Math.Max(0f, playAreaWidth) : 0f;
            if (safeAreaWidth <= 0f)
            {
                return 0f;
            }

            var safeBasketWidth = IsFinite(basketWidth) ? Math.Max(0f, basketWidth) : 0f;
            var halfBasketWidth = Math.Min(safeAreaWidth * 0.5f, safeBasketWidth * 0.5f);
            var requested = IsFinite(requestedCenterX)
                ? requestedCenterX
                : safeAreaWidth * 0.5f;
            return Math.Max(
                halfBasketWidth,
                Math.Min(safeAreaWidth - halfBasketWidth, requested));
        }

        public static bool DoesDropIntersectBasket(
            float dropCenterX,
            float previousDropCenterY,
            float currentDropCenterY,
            float dropHalfWidth,
            float dropHalfHeight,
            float basketCenterX,
            float basketCenterY,
            float basketHalfWidth,
            float basketHalfHeight)
        {
            if (!IsFinite(dropCenterX)
                || !IsFinite(previousDropCenterY)
                || !IsFinite(currentDropCenterY)
                || !IsFinite(dropHalfWidth)
                || !IsFinite(dropHalfHeight)
                || !IsFinite(basketCenterX)
                || !IsFinite(basketCenterY)
                || !IsFinite(basketHalfWidth)
                || !IsFinite(basketHalfHeight))
            {
                return false;
            }

            var safeDropHalfWidth = Math.Max(0f, dropHalfWidth);
            var safeDropHalfHeight = Math.Max(0f, dropHalfHeight);
            var safeBasketHalfWidth = Math.Max(0f, basketHalfWidth);
            var safeBasketHalfHeight = Math.Max(0f, basketHalfHeight);
            var horizontalDistance = Math.Abs(dropCenterX - basketCenterX);
            if (horizontalDistance > safeDropHalfWidth + safeBasketHalfWidth)
            {
                return false;
            }

            // Use the swept vertical bounds so a fast drop cannot skip through the basket
            // during one long frame.
            var sweptBottom = Math.Min(previousDropCenterY, currentDropCenterY)
                - safeDropHalfHeight;
            var sweptTop = Math.Max(previousDropCenterY, currentDropCenterY)
                + safeDropHalfHeight;
            var basketBottom = basketCenterY - safeBasketHalfHeight;
            var basketTop = basketCenterY + safeBasketHalfHeight;
            return sweptBottom <= basketTop && sweptTop >= basketBottom;
        }

        public static MilkDropMiniGameRewardStatus EvaluateRewardCooldown(
            string lastRewardAtIso,
            DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(lastRewardAtIso)
                || !DateTimeOffset.TryParse(lastRewardAtIso, out var lastRewardAt))
            {
                return new MilkDropMiniGameRewardStatus(true, 0, false);
            }

            if (lastRewardAt > now)
            {
                return new MilkDropMiniGameRewardStatus(
                    false,
                    RewardCooldownSeconds,
                    true);
            }

            var elapsedSeconds = Math.Max(0d, (now - lastRewardAt).TotalSeconds);
            var remainingSeconds = (int)Math.Ceiling(RewardCooldownSeconds - elapsedSeconds);
            return remainingSeconds <= 0
                ? new MilkDropMiniGameRewardStatus(true, 0, false)
                : new MilkDropMiniGameRewardStatus(false, remainingSeconds, false);
        }

        public static string FormatCooldown(int remainingSeconds)
        {
            var safeSeconds = Math.Max(0, remainingSeconds);
            var minutes = safeSeconds / 60;
            var seconds = safeSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        public static MilkDropMiniGameRewardResult CalculateReward(
            int caught,
            int missed,
            int reportedScore)
        {
            var safeCaught = Math.Max(0, caught);
            var safeMissed = Math.Max(0, missed);
            var maximumValidScore = CalculateScore(safeCaught);
            var safeScore = Math.Max(0, Math.Min(reportedScore, maximumValidScore));

            var milkCoins = Math.Min(MaximumMilkCoinReward, safeScore / PointsPerCatch);
            var milkDrops = safeScore <= 0
                ? 0
                : Math.Min(MaximumMilkDropReward, 1 + safeScore / ScorePerBonusDrop);
            var message = safeCaught > 0
                ? $"우유방울 {safeCaught}개를 받았어요! 코인 +{milkCoins}, 우유방울 +{milkDrops}."
                : "이번에는 우유방울을 받지 못했어요. 다시 도전해 봐요!";

            return new MilkDropMiniGameRewardResult(
                safeScore,
                safeCaught,
                safeMissed,
                milkCoins,
                milkDrops,
                message);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
