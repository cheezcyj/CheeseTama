using System;

namespace CheeseTama.Gameplay.Stats
{
    public readonly struct ReturnSummaryStatsSnapshot
    {
        public readonly int hunger;
        public readonly int mood;
        public readonly int cleanliness;
        public readonly int sleepiness;
        public readonly int health;
        public readonly int overfullness;

        public ReturnSummaryStatsSnapshot(
            int hunger,
            int mood,
            int cleanliness,
            int sleepiness,
            int health)
            : this(hunger, mood, cleanliness, sleepiness, health, 0)
        {
        }

        public ReturnSummaryStatsSnapshot(
            int hunger,
            int mood,
            int cleanliness,
            int sleepiness,
            int health,
            int overfullness)
        {
            this.hunger = hunger;
            this.mood = mood;
            this.cleanliness = cleanliness;
            this.sleepiness = sleepiness;
            this.health = health;
            this.overfullness = overfullness;
        }

        public static ReturnSummaryStatsSnapshot Capture(CheeseTamaModel tama)
        {
            var stats = tama?.stats;
            return stats == null
                ? new ReturnSummaryStatsSnapshot(0, 0, 0, 0, 0)
                : new ReturnSummaryStatsSnapshot(
                    stats.hunger,
                    stats.mood,
                    stats.cleanliness,
                    stats.sleepiness,
                    stats.health,
                    stats.overfullness);
        }
    }

    public sealed class ReturnSummaryData
    {
        public readonly string id;
        public readonly int elapsedMinutes;
        public readonly int appliedHours;
        public readonly ReturnSummaryStatsSnapshot before;
        public readonly ReturnSummaryStatsSnapshot after;
        public readonly int milkCoinsDelta;
        public readonly int milkDropsDelta;
        public readonly int collectionFragmentsDelta;

        public ReturnSummaryData(
            string id,
            int elapsedMinutes,
            int appliedHours,
            ReturnSummaryStatsSnapshot before,
            ReturnSummaryStatsSnapshot after,
            int milkCoinsDelta,
            int milkDropsDelta,
            int collectionFragmentsDelta)
        {
            this.id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            this.elapsedMinutes = Math.Max(0, elapsedMinutes);
            this.appliedHours = Math.Max(0, appliedHours);
            this.before = before;
            this.after = after;
            this.milkCoinsDelta = milkCoinsDelta;
            this.milkDropsDelta = milkDropsDelta;
            this.collectionFragmentsDelta = collectionFragmentsDelta;
        }

        public bool HasRewards => milkCoinsDelta != 0
            || milkDropsDelta != 0
            || collectionFragmentsDelta != 0;
    }
}
