using System;

namespace CheeseTama.Gameplay.MiniGames
{
    public readonly struct CleaningMiniGameCompletionResult
    {
        public CleaningMiniGameCompletionResult(
            int score,
            int cleanedSpots,
            int missedSpots,
            int cleanlinessGain,
            string message,
            bool success)
        {
            this.cleanedSpots = Math.Max(0, cleanedSpots);
            this.missedSpots = Math.Max(0, missedSpots);
            this.score = CleaningMiniGameRules.ClampReportedScore(this.cleanedSpots, score);
            this.cleanlinessGain = Math.Max(0, cleanlinessGain);
            this.message = message ?? string.Empty;
            this.success = success;
        }

        public readonly int score;
        public readonly int cleanedSpots;
        public readonly int missedSpots;
        public readonly int cleanlinessGain;
        public readonly string message;
        public readonly bool success;
    }

    public static class CleaningMiniGameRules
    {
        public const float DurationSeconds = 24f;
        public const int PointsPerClean = 100;
        public const float SpawnIntervalSeconds = 0.62f;
        public const float SpotLifetimeSeconds = 2.15f;
        public const float SpotSizePixels = 68f;
        public const float MinimumSpotScale = 0.78f;
        public const float MaximumSpotScale = 1.08f;
        public const int InitialPoolSize = 8;
        public const int MaximumPoolSize = 14;
        public const int MinimumCleanedSpotsForCareReward = 6;

        public static int CalculateScore(int cleanedSpots)
        {
            if (cleanedSpots <= 0)
            {
                return 0;
            }

            var score = (long)cleanedSpots * PointsPerClean;
            return score > int.MaxValue ? int.MaxValue : (int)score;
        }

        public static int ClampReportedScore(int cleanedSpots, int reportedScore)
        {
            return Math.Max(0, Math.Min(reportedScore, CalculateScore(cleanedSpots)));
        }

        public static float GetRemainingSeconds(float elapsedSeconds)
        {
            return Math.Max(0f, DurationSeconds - Math.Max(0f, elapsedSeconds));
        }

        public static bool IsComplete(float elapsedSeconds)
        {
            return elapsedSeconds >= DurationSeconds;
        }

        public static bool QualifiesForCareReward(int cleanedSpots)
        {
            return cleanedSpots >= MinimumCleanedSpotsForCareReward;
        }

        public static string GetGrade(int cleanedSpots, int missedSpots)
        {
            var safeCleaned = Math.Max(0, cleanedSpots);
            var safeMissed = Math.Max(0, missedSpots);
            var total = safeCleaned + safeMissed;
            if (safeCleaned == 0 || total == 0)
            {
                return "연습 필요";
            }

            var cleanRatio = safeCleaned / (float)total;
            if (safeCleaned >= 20 && cleanRatio >= 0.85f)
            {
                return "반짝반짝";
            }

            if (safeCleaned >= 12 && cleanRatio >= 0.65f)
            {
                return "깨끗해요";
            }

            return "조금 더 닦기";
        }
    }
}
