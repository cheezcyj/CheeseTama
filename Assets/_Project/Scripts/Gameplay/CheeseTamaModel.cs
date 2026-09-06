using System;
using CheeseTama.Gameplay.Stats;

namespace CheeseTama.Gameplay
{
    [Serializable]
    public sealed class CheeseTamaModel
    {
        public const string DefaultName = "치즈타마";
        public const string DefaultSoftName = "말랑 치즈타마";
        private const string LegacyDefaultName = "CheeseTama";
        private const string LegacyDefaultSoftName = "Soft CheeseTama";

        public string id = "ct_001";
        public string name = DefaultName;
        public bool hasCustomName;
        public string eggType = "cream_egg";
        public bool isHatched;
        public int level = 1;
        public int levelProgress;
        public int maxLevel = 33;
        public string form = "egg";
        public string evolutionId = string.Empty;
        public string createdAtIso;
        public string lastSavedAtIso;
        public StatBlock stats = StatBlock.CreateDefault();
        public GrowthHistory growthHistory = new GrowthHistory();

        public void EnsureRuntimeDefaults()
        {
            stats ??= StatBlock.CreateDefault();
            growthHistory ??= new GrowthHistory();
            stats.ClampFeedingStatuses();
            growthHistory.lastFedMilkId ??= string.Empty;
            growthHistory.sameMilkFeedStreak = Math.Max(0, growthHistory.sameMilkFeedStreak);
            if (string.IsNullOrWhiteSpace(growthHistory.lastFedMilkId))
            {
                growthHistory.sameMilkFeedStreak = 0;
            }
            else if (growthHistory.sameMilkFeedStreak == 0)
            {
                // Legacy saves remember the last milk but predate the streak counter.
                // Treat that saved milk as one taste without activating a penalty.
                growthHistory.sameMilkFeedStreak = 1;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = ResolveDefaultName();
                hasCustomName = false;
            }
            else if (!hasCustomName
                && (string.Equals(name, LegacyDefaultName, StringComparison.Ordinal)
                    || string.Equals(name, LegacyDefaultSoftName, StringComparison.Ordinal)
                    || string.Equals(name, DefaultName, StringComparison.Ordinal)
                    || string.Equals(name, DefaultSoftName, StringComparison.Ordinal)))
            {
                name = ResolveDefaultName();
            }
            else if (!hasCustomName)
            {
                // Older saves predate hasCustomName. Preserve any non-default name they already carry.
                hasCustomName = true;
            }

            if (isHatched
                && form == "soft_cheesetama"
                && string.IsNullOrWhiteSpace(evolutionId)
                && !hasCustomName)
            {
                name = DefaultSoftName;
            }
        }

        private string ResolveDefaultName()
        {
            return isHatched && string.Equals(form, "soft_cheesetama", StringComparison.Ordinal)
                ? DefaultSoftName
                : DefaultName;
        }
    }

    [Serializable]
    public sealed class GrowthHistory
    {
        public string mostUsedMilkId = "basic_milk";
        public string mostUsedIngredientId = "none";
        public string careStyle = "gentle";
        public string lastFedMilkId = string.Empty;
        public int sameMilkFeedStreak;
    }
}
