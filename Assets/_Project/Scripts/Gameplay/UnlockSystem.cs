using System.Collections.Generic;

namespace CheeseTama.Gameplay
{
    public sealed class UnlockSystem
    {
        public const int MaxLevel = 33;
        public const int MaxMilkGrowthLevel = 5;

        public void RefreshUnlocks(CheeseTamaModel tama, IReadOnlyDictionary<string, int> milkGrowth, UnlockSaveData unlocks)
        {
            if (tama == null || milkGrowth == null || unlocks == null)
            {
                return;
            }

            var allMainMilkMaxed = milkGrowth.Count > 0;
            foreach (var entry in milkGrowth)
            {
                if (entry.Value < MaxMilkGrowthLevel)
                {
                    allMainMilkMaxed = false;
                    break;
                }
            }

            if (tama.level >= MaxLevel && allMainMilkMaxed)
            {
                unlocks.starEggUnlocked = true;
                unlocks.starMilkUnlocked = true;
            }

            unlocks.fantasyPowderEnabled = unlocks.starEggUnlocked && unlocks.starMilkUnlocked;
        }
    }
}

