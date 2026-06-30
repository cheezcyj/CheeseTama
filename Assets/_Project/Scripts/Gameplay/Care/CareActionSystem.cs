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

            return AddCareProgress(tama, 8, "치즈타마가 우유를 마셨습니다.");
        }

        public CareActionResult FeedStarMilk(CheeseTamaModel tama)
        {
            if (tama == null)
            {
                return MissingTama();
            }

            tama.stats.hunger += 24;
            tama.stats.mood += 8;
            tama.stats.sleepiness += 3;
            tama.stats.affection += 5;
            tama.stats.milkSatisfaction += 8;
            tama.stats.ClampAll();

            return AddCareProgress(tama, 12, "치즈타마가 별빛 우유를 마셨습니다.");
        }

        public CareActionResult FeedSnack(CheeseTamaModel tama)
        {
            if (tama == null)
            {
                return MissingTama();
            }

            tama.stats.hunger += 10;
            tama.stats.mood += 9;
            tama.stats.cleanliness -= 5;
            tama.stats.sleepiness += 3;
            tama.stats.affection += 3;
            tama.stats.milkSatisfaction -= 2;
            tama.stats.ClampAll();

            var message = tama.stats.cleanliness < 45
                ? "치즈타마가 부스러지는 치즈 간식을 먹었습니다. 밀크룸 청소가 필요합니다."
                : "치즈타마가 치즈 간식을 조금 먹었습니다.";
            return AddCareProgress(tama, 5, message);
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

            return AddCareProgress(tama, 6, "치즈타마가 잠깐 놀았습니다.");
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

            return AddCareProgress(tama, 4, "밀크룸이 깨끗해졌습니다.");
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

            return AddCareProgress(tama, 3, "치즈타마가 따뜻한 빛 아래에서 쉬었습니다.");
        }

        private CareActionResult AddCareProgress(CheeseTamaModel tama, int progress, string message)
        {
            var levelBefore = tama.level;
            levelSystem.AddProgress(tama, Mathf.Max(0, progress));
            var hatched = hatchingSystem.TryHatch(tama);

            if (hatched)
            {
                return new CareActionResult(true, true, "껍질이 열리고 말랑한 치즈타마가 깨어났습니다.");
            }

            if (tama.level > levelBefore)
            {
                return new CareActionResult(true, false, $"{message} 레벨이 올랐습니다. 부화 {HatchingSystem.GetHatchProgressPercent(tama)}%.");
            }

            var hatchProgress = HatchingSystem.GetHatchProgressPercent(tama);
            if (!tama.isHatched && hatchProgress >= 75)
            {
                return new CareActionResult(true, false, $"{message} 껍질이 따뜻해졌습니다. 부화 {hatchProgress}%.");
            }

            return new CareActionResult(true, false, message);
        }

        private static CareActionResult MissingTama()
        {
            return new CareActionResult(false, false, "치즈타마 데이터를 불러오지 못했습니다.");
        }
    }
}
