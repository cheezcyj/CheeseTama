using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public sealed class LevelSystem
    {
        private const int ProgressPerLevel = 100;
        private readonly LateLevelGrowthSystem lateLevelGrowthSystem = new LateLevelGrowthSystem();

        public LateLevelGrowthResult LastLateLevelResult { get; private set; }

        public void AddProgress(CheeseTamaModel tama, int amount)
        {
            AddProgress(tama, amount, null, null);
        }

        public void AddProgress(
            CheeseTamaModel tama,
            int amount,
            LateLevelGrowthSaveData lateLevelState,
            IList<MilkGrowthSaveEntry> milkGrowth)
        {
            if (tama == null || amount <= 0)
            {
                return;
            }

            if (lateLevelState != null
                && LateLevelGrowthCatalog.TryGetForCurrentLevel(tama.level, out _))
            {
                LastLateLevelResult = lateLevelGrowthSystem.AddProgress(
                    tama,
                    lateLevelState,
                    milkGrowth,
                    amount);
                return;
            }

            LastLateLevelResult = default;

            if (tama.level >= tama.maxLevel)
            {
                tama.stats.maturation += amount;
                tama.stats.ClampAll();
                return;
            }

            tama.levelProgress += amount;

            while (tama.levelProgress >= ProgressPerLevel && tama.level < tama.maxLevel)
            {
                tama.levelProgress -= ProgressPerLevel;
                tama.level++;
            }

            if (tama.level >= tama.maxLevel)
            {
                tama.level = tama.maxLevel;
                tama.levelProgress = 0;
            }
        }
    }
}
