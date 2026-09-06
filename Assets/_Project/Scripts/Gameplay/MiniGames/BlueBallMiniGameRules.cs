using System;

namespace CheeseTama.Gameplay.MiniGames
{
    public static class MiniGameRecordIds
    {
        public const string MilkDrop = "milk_drop";
        public const string Cleaning = "cleaning";
        public const string BouncyJump = "bouncy_jump";
        public const string BlueBall = "blue_ball";
    }

    public sealed class BlueBallSessionResult
    {
        public BlueBallSessionResult(int hits, int misses, int score, int highestCombo)
        {
            this.hits = Math.Max(0, hits);
            this.misses = Math.Max(0, misses);
            this.score = Math.Max(0, score);
            this.highestCombo = Math.Max(0, highestCombo);
        }

        public int hits { get; }
        public int misses { get; }
        public int score { get; }
        public int highestCombo { get; }
        public bool qualifiesForCare => hits >= BlueBallMiniGameRules.MinimumHitsForCare;
    }

    public sealed class BlueBallCompletionResult
    {
        public BlueBallCompletionResult(
            bool success,
            int hits,
            int misses,
            int score,
            int bestScore,
            string message)
        {
            this.success = success;
            this.hits = Math.Max(0, hits);
            this.misses = Math.Max(0, misses);
            this.score = Math.Max(0, score);
            this.bestScore = Math.Max(0, bestScore);
            this.message = message ?? string.Empty;
        }

        public bool success { get; }
        public int hits { get; }
        public int misses { get; }
        public int score { get; }
        public int bestScore { get; }
        public string message { get; }
    }

    public static class BlueBallMiniGameRules
    {
        public const float SessionSeconds = 15f;
        public const int MinimumHitsForCare = 5;
        public const int BaseHitScore = 80;
        public const int ComboStepScore = 15;
        public const int MaximumComboBonus = 240;

        public static int CalculateHitScore(int combo)
        {
            var safeCombo = Math.Max(1, combo);
            return BaseHitScore + Math.Min(MaximumComboBonus, (safeCombo - 1) * ComboStepScore);
        }

        public static BlueBallSessionResult Complete(
            int hits,
            int misses,
            int score,
            int highestCombo)
        {
            return new BlueBallSessionResult(hits, misses, score, highestCombo);
        }
    }
}
