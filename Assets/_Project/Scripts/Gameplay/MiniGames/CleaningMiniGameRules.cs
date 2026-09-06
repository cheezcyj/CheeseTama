using System;

namespace CheeseTama.Gameplay.MiniGames
{
    public readonly struct CleaningMiniGameFixedSlot
    {
        public CleaningMiniGameFixedSlot(float normalizedX, float normalizedY)
        {
            this.normalizedX = normalizedX;
            this.normalizedY = normalizedY;
        }

        public readonly float normalizedX;
        public readonly float normalizedY;
    }

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
        public const float DurationSeconds = 10f;
        public const int PointsPerClean = 100;
        public const float SpotSizePixels = 68f;
        public const float RequiredScrubPathPixels = 100f;
        public const float RequiredScrubSpanPixels = 42f;
        public const int FixedSpotCount = 8;
        public const int MinimumCleanedSpotsForCareReward = 6;

        public static CleaningMiniGameFixedSlot GetFixedSlot(int index)
        {
            return index switch
            {
                0 => new CleaningMiniGameFixedSlot(0.13f, 0.20f),
                1 => new CleaningMiniGameFixedSlot(0.38f, 0.14f),
                2 => new CleaningMiniGameFixedSlot(0.67f, 0.23f),
                3 => new CleaningMiniGameFixedSlot(0.88f, 0.16f),
                4 => new CleaningMiniGameFixedSlot(0.20f, 0.58f),
                5 => new CleaningMiniGameFixedSlot(0.48f, 0.50f),
                6 => new CleaningMiniGameFixedSlot(0.75f, 0.62f),
                7 => new CleaningMiniGameFixedSlot(0.90f, 0.82f),
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        public static CleaningMiniGameFixedSlot GetFixedSlot(int index, int phaseIndex)
        {
            var slot = GetFixedSlot(index);
            return (Math.Max(0, phaseIndex) % 4) switch
            {
                1 => new CleaningMiniGameFixedSlot(1f - slot.normalizedX, slot.normalizedY),
                2 => new CleaningMiniGameFixedSlot(slot.normalizedX, 1f - slot.normalizedY),
                3 => new CleaningMiniGameFixedSlot(1f - slot.normalizedX, 1f - slot.normalizedY),
                _ => slot
            };
        }

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

            if (safeCleaned >= MinimumCleanedSpotsForCareReward && cleanRatio >= 0.65f)
            {
                return "깨끗해요";
            }

            return "조금 더 닦기";
        }

        public static string BuildResultSummary(CleaningMiniGameCompletionResult completion)
        {
            var grade = GetGrade(completion.cleanedSpots, completion.missedSpots);
            var title = grade switch
            {
                "반짝반짝" => "반짝반짝! 정말 깨끗해졌어요.",
                "깨끗해요" => "잘했어요! 밀크룸이 깨끗해졌어요.",
                "조금 더 닦기" => "조금만 더 닦아 봐요!",
                _ => "괜찮아요. 다음에 다시 도전해요!"
            };
            var countLine = completion.missedSpots > 0
                ? $"얼룩 {completion.cleanedSpots}개를 닦고 {completion.missedSpots}개를 놓쳤어요."
                : $"얼룩 {completion.cleanedSpots}개를 닦았어요.";
            string rewardLine;
            if (completion.success)
            {
                rewardLine = completion.cleanlinessGain > 0
                    ? $"점수 {completion.score} · 깨끗함 +{completion.cleanlinessGain}"
                    : $"점수 {completion.score} · 깨끗함은 이미 최고예요!";
            }
            else if (QualifiesForCareReward(completion.cleanedSpots))
            {
                rewardLine = $"점수 {completion.score} · 결과를 저장하지 못했어요.";
            }
            else
            {
                rewardLine = $"점수 {completion.score} · 얼룩 6개 이상 닦으면 깨끗함이 올라요.";
            }

            return $"{title}\n{countLine}\n{rewardLine}";
        }
    }
}
