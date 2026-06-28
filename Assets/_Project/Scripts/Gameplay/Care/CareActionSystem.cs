using CheeseTama.Gameplay.Growth;
using UnityEngine;

namespace CheeseTama.Gameplay.Care
{
    public sealed class CareActionSystem
    {
        private readonly LevelSystem levelSystem = new LevelSystem();
        private readonly HatchingSystem hatchingSystem = new HatchingSystem();

        public CareActionResult FeedMilk(CheeseTamaModel tama)
        {
            if (tama == null)
            {
                return MissingTama();
            }

            tama.stats.hunger += 18;
            tama.stats.mood += 3;
            tama.stats.sleepiness += 2;
            tama.stats.affection += 2;
            tama.stats.milkSatisfaction += 4;
            tama.stats.ClampAll();

            return AddCareProgress(tama, 8, "CheeseTama drank milk.");
        }

        public CareActionResult Play(CheeseTamaModel tama)
        {
            if (tama == null)
            {
                return MissingTama();
            }

            tama.stats.hunger -= 5;
            tama.stats.mood += 12;
            tama.stats.sleepiness += 8;
            tama.stats.affection += 4;
            tama.stats.ClampAll();

            return AddCareProgress(tama, 6, "CheeseTama played for a bit.");
        }

        public CareActionResult Clean(CheeseTamaModel tama)
        {
            if (tama == null)
            {
                return MissingTama();
            }

            tama.stats.cleanliness += 25;
            tama.stats.mood += 1;
            tama.stats.health += 2;
            tama.stats.ClampAll();

            return AddCareProgress(tama, 4, "The milkroom is clean.");
        }

        public CareActionResult Rest(CheeseTamaModel tama)
        {
            if (tama == null)
            {
                return MissingTama();
            }

            tama.stats.hunger -= 2;
            tama.stats.sleepiness -= 20;
            tama.stats.health += 4;
            tama.stats.mood += 2;
            tama.stats.ClampAll();

            return AddCareProgress(tama, 3, "CheeseTama rested under warm light.");
        }

        private CareActionResult AddCareProgress(CheeseTamaModel tama, int progress, string message)
        {
            var levelBefore = tama.level;
            levelSystem.AddProgress(tama, Mathf.Max(0, progress));
            var hatched = hatchingSystem.TryHatch(tama);

            if (hatched)
            {
                return new CareActionResult(true, true, "The egg hatched into a soft CheeseTama.");
            }

            if (tama.level > levelBefore)
            {
                return new CareActionResult(true, false, $"{message} Level up!");
            }

            return new CareActionResult(true, false, message);
        }

        private static CareActionResult MissingTama()
        {
            return new CareActionResult(false, false, "No CheeseTama data is loaded.");
        }
    }
}

