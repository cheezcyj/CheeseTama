using System;
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

        public TimeProgressionResult(
            bool applied,
            int hours,
            int hungerDelta,
            int moodDelta,
            int cleanlinessDelta,
            int sleepinessDelta,
            int healthDelta)
        {
            this.applied = applied;
            this.hours = hours;
            this.hungerDelta = hungerDelta;
            this.moodDelta = moodDelta;
            this.cleanlinessDelta = cleanlinessDelta;
            this.sleepinessDelta = sleepinessDelta;
            this.healthDelta = healthDelta;
        }

        public static TimeProgressionResult None()
        {
            return new TimeProgressionResult(false, 0, 0, 0, 0, 0, 0);
        }

        public string ToSummary(string prefix)
        {
            if (!applied)
            {
                return "No time-based stat changes yet.";
            }

            var healthText = healthDelta == 0 ? string.Empty : $", Health {healthDelta}";
            return $"{prefix} {hours}h passed. Hunger {hungerDelta}, Mood {moodDelta}, Cleanliness {cleanlinessDelta}, Sleepiness +{sleepinessDelta}{healthText}.";
        }
    }

    public sealed class TimeProgressionSystem
    {
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

            tama.stats.ClampAll();
            return new TimeProgressionResult(true, careTicks, hungerDelta, moodDelta, cleanlinessDelta, sleepinessDelta, healthDelta);
        }
    }
}
