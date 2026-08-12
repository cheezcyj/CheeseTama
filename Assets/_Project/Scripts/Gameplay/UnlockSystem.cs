using System.Collections.Generic;
using CheeseTama.Gameplay.Milk;

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

            var allMainMilkMaxed = true;
            foreach (var milk in MilkCatalog.MainMilks)
            {
                if (milk == null
                    || !milkGrowth.TryGetValue(milk.id, out var growthLevel)
                    || growthLevel < MaxMilkGrowthLevel)
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
