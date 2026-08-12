namespace CheeseTama.Gameplay.Growth
{
    public sealed class LevelSystem
    {
        private const int ProgressPerLevel = 100;

        public void AddProgress(CheeseTamaModel tama, int amount)
        {
            if (tama == null || amount <= 0)
            {
                return;
            }

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

