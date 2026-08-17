using System;

namespace CheeseTama.Gameplay.MiniGames
{
    public sealed class BouncyJumpSessionResult
    {
        public BouncyJumpSessionResult(int successes, int misses, int score, int highestCombo)
        {
            this.successes = Math.Max(0, successes);
            this.misses = Math.Max(0, misses);
            this.score = Math.Max(0, score);
            this.highestCombo = Math.Max(0, highestCombo);
        }

        public int successes { get; }
        public int misses { get; }
        public int score { get; }
        public int highestCombo { get; }
        public bool qualifiesForCare => successes >= BouncyJumpMiniGameRules.MinimumSuccessfulJumpsForCare;
    }

    public sealed class BouncyJumpCompletionResult
    {
        public BouncyJumpCompletionResult(
            bool success,
            int successes,
            int misses,
            int score,
            int bestScore,
            string message)
        {
            this.success = success;
            this.successes = Math.Max(0, successes);
            this.misses = Math.Max(0, misses);
            this.score = Math.Max(0, score);
            this.bestScore = Math.Max(0, bestScore);
            this.message = message ?? string.Empty;
        }

        public bool success { get; }
        public int successes { get; }
        public int misses { get; }
        public int score { get; }
        public int bestScore { get; }
        public string message { get; }
    }

    public static class BouncyJumpMiniGameRules
    {
        public const float SessionSeconds = 25f;
        public const float MarkerTravelSeconds = 1.85f;
        public const float MinimumTargetWidthRatio = 0.12f;
        public const float MaximumTargetWidthRatio = 0.22f;
        public const int MinimumSuccessfulJumpsForCare = 3;
        public const int BaseSuccessScore = 100;
        public const int MaximumComboBonus = 200;

        public static int CalculateAttemptScore(float normalizedDistance, int combo)
        {
            var safeDistance = Math.Max(0f, Math.Min(1f, normalizedDistance));
            if (safeDistance >= 1f)
            {
                return 0;
            }

            var accuracyBonus = (int)Math.Round((1f - safeDistance) * 100f);
            var comboBonus = Math.Min(MaximumComboBonus, Math.Max(0, combo - 1) * 20);
            return BaseSuccessScore + accuracyBonus + comboBonus;
        }

        public static BouncyJumpSessionResult Complete(
            int successes,
            int misses,
            int score,
            int highestCombo)
        {
            return new BouncyJumpSessionResult(successes, misses, score, highestCombo);
        }
    }
}
