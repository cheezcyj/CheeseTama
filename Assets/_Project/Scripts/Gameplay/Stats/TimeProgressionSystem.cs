using System;
using CheeseTama.Gameplay.Feeding;
using CheeseTama.Utilities;

namespace CheeseTama.Gameplay.Stats
{
    public readonly struct TimeProgressionResult
    {
        public readonly bool applied;
        public readonly int hours;
        public readonly int hungerDelta;
        public readonly int moodDelta;
        public readonly int cleanlinessDelta;
        public readonly int sleepinessDelta;
        public readonly int healthDelta;
        public readonly int overfullnessDelta;
        public readonly bool overfullnessRecovered;
        public readonly bool bodyChillRecovered;
        public readonly bool fermentedAftertasteRecovered;
        public readonly bool sleepRhythmRecovered;

        public TimeProgressionResult(
            bool applied,
            int hours,
            int hungerDelta,
            int moodDelta,
            int cleanlinessDelta,
            int sleepinessDelta,
            int healthDelta)
            : this(
                applied,
                hours,
                hungerDelta,
                moodDelta,
                cleanlinessDelta,
                sleepinessDelta,
                healthDelta,
                0,
                false)
        {
        }

        public TimeProgressionResult(
            bool applied,
            int hours,
            int hungerDelta,
            int moodDelta,
            int cleanlinessDelta,
            int sleepinessDelta,
            int healthDelta,
            int overfullnessDelta,
            bool overfullnessRecovered)
            : this(
                applied,
                hours,
                hungerDelta,
                moodDelta,
                cleanlinessDelta,
                sleepinessDelta,
                healthDelta,
                overfullnessDelta,
                overfullnessRecovered,
                false,
                false,
                false)
        {
        }

        public TimeProgressionResult(
            bool applied,
            int hours,
            int hungerDelta,
            int moodDelta,
            int cleanlinessDelta,
            int sleepinessDelta,
            int healthDelta,
            int overfullnessDelta,
            bool overfullnessRecovered,
            bool bodyChillRecovered,
            bool fermentedAftertasteRecovered,
            bool sleepRhythmRecovered)
        {
            this.applied = applied;
            this.hours = hours;
            this.hungerDelta = hungerDelta;
            this.moodDelta = moodDelta;
            this.cleanlinessDelta = cleanlinessDelta;
            this.sleepinessDelta = sleepinessDelta;
            this.healthDelta = healthDelta;
            this.overfullnessDelta = overfullnessDelta;
            this.overfullnessRecovered = overfullnessRecovered;
            this.bodyChillRecovered = bodyChillRecovered;
            this.fermentedAftertasteRecovered = fermentedAftertasteRecovered;
            this.sleepRhythmRecovered = sleepRhythmRecovered;
        }

        public static TimeProgressionResult None()
        {
            return new TimeProgressionResult(false, 0, 0, 0, 0, 0, 0);
        }

        public string ToSummary(string prefix)
        {
            if (!applied)
            {
                return "아직 시간 경과에 따른 변화가 없습니다.";
            }

            var healthText = healthDelta == 0 ? string.Empty : $", 건강 {healthDelta}";
            var overfullnessText = overfullnessDelta >= 0
                ? string.Empty
                : overfullnessRecovered
                    ? ", 과포만 회복"
                    : $", 과포만 {overfullnessDelta}";
            var aftereffectText = string.Empty;
            if (bodyChillRecovered)
            {
                aftereffectText += ", 몸 떨림 회복";
            }

            if (fermentedAftertasteRecovered)
            {
                aftereffectText += ", 발효 뒷맛 회복";
            }

            if (sleepRhythmRecovered)
            {
                aftereffectText += ", 수면 리듬 회복";
            }

            return $"{prefix} {hours}시간이 지났습니다. 포만감 {hungerDelta}, 기분 {moodDelta}, 청결 {cleanlinessDelta}, 졸림 +{sleepinessDelta}{healthText}{overfullnessText}{aftereffectText}.";
        }
    }

    public sealed class TimeProgressionSystem
    {
        private readonly FeedingStatusSystem feedingStatusSystem = new FeedingStatusSystem();

        public TimeProgressionResult ApplyOfflineProgress(CheeseTamaModel tama, DateTimeOffset now)
        {
            if (tama == null || tama.stats == null)
            {
                return TimeProgressionResult.None();
            }

            var lastSaved = TimeUtility.ParseOrDefault(tama.lastSavedAtIso, now);
            var minutes = Math.Max(0, (now - lastSaved).TotalMinutes);
            var careTicks = (int)(minutes / 60);

            if (careTicks <= 0)
            {
                return TimeProgressionResult.None();
            }

            var result = ApplyCareTicks(tama, careTicks);
            tama.lastSavedAtIso = now.ToString("O");
            return result;
        }

        public TimeProgressionResult ApplyCareTicks(CheeseTamaModel tama, int careTicks)
        {
            if (tama == null || tama.stats == null || careTicks <= 0)
            {
                return TimeProgressionResult.None();
            }

            var hungerDelta = -careTicks * 4;
            var moodDelta = -careTicks * 2;
            var cleanlinessDelta = -careTicks * 2;
            var sleepinessDelta = careTicks * 3;
            var healthDelta = 0;

            tama.stats.hunger += hungerDelta;
            tama.stats.mood += moodDelta;
            tama.stats.cleanliness += cleanlinessDelta;
            tama.stats.sleepiness += sleepinessDelta;

            if (tama.stats.hunger <= 10 || tama.stats.cleanliness <= 10)
            {
                healthDelta = -careTicks * 2;
                tama.stats.health += healthDelta;
            }

            var feedingStatus = feedingStatusSystem.RecoverByTime(tama, careTicks);
            tama.stats.ClampAll();
            return new TimeProgressionResult(
                true,
                careTicks,
                hungerDelta,
                moodDelta,
                cleanlinessDelta,
                sleepinessDelta,
                healthDelta,
                feedingStatus.OverfullnessDelta,
                feedingStatus.overfullnessRecovered,
                feedingStatus.bodyChillRecovered,
                feedingStatus.fermentedAftertasteRecovered,
                feedingStatus.sleepRhythmDisruptionRecovered);
        }
    }
}
