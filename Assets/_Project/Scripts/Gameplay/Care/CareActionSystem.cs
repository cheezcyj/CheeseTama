using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Snacks;
using UnityEngine;

namespace CheeseTama.Gameplay.Care
{
    public sealed class CareActionSystem
    {
        private readonly LevelSystem levelSystem = new LevelSystem();
        private readonly HatchingSystem hatchingSystem = new HatchingSystem();

        public CareActionResult FeedMilk(CheeseTamaModel tama)
        {
            return FeedMilk(tama, MilkCatalog.BasicMilk);
        }

        public CareActionResult FeedStarMilk(CheeseTamaModel tama)
        {
            return FeedMilk(tama, MilkCatalog.StarMilk);
        }

        public CareActionResult FeedMilk(CheeseTamaModel tama, MilkDefinition milk)
        {
            if (tama == null)
            {
                return MissingTama();
            }

            if (milk == null)
            {
                return new CareActionResult(false, false, "선택한 우유 데이터를 찾지 못했습니다.");
            }

            tama.stats.hunger += milk.hunger;
            tama.stats.mood += milk.mood;
            tama.stats.cleanliness += milk.cleanliness;
            tama.stats.sleepiness += milk.sleepiness;
            tama.stats.health += milk.health;
            tama.stats.maturation += milk.maturation;
            tama.stats.affection += milk.affection;
            tama.stats.milkSatisfaction += milk.milkSatisfaction;
            tama.stats.ClampAll();

            return AddCareProgress(tama, milk.careProgress, $"치즈타마가 {milk.displayName}를 마셨습니다.");
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

        public CareActionResult FeedSnack(CheeseTamaModel tama, SnackDefinition snack)
        {
            if (tama == null)
            {
                return MissingTama();
            }

            if (snack == null)
            {
                return new CareActionResult(false, false, "선택한 간식 데이터를 찾지 못했습니다.");
            }

            tama.stats.hunger += snack.hunger;
            tama.stats.mood += snack.mood;
            tama.stats.cleanliness += snack.cleanliness;
            tama.stats.sleepiness += snack.sleepiness;
            tama.stats.health += snack.health;
            tama.stats.affection += snack.affection;
            tama.stats.maturation += snack.maturation;
            tama.stats.milkSatisfaction += snack.milkSatisfaction;
            tama.stats.ClampAll();

            var message = snack.cleanliness < 0 && tama.stats.cleanliness < 45
                ? $"치즈타마가 {snack.displayName}을 먹었습니다. 바닥에 부스러기가 남아 청소가 필요합니다."
                : $"치즈타마가 {snack.displayName}을 먹었습니다.";
            return AddCareProgress(tama, snack.careProgress, message);
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
                return new CareActionResult(true, true, "껍질이 열리고 말랑한 치즈타마가 깨어났습니다.", true);
            }

            if (tama.level > levelBefore)
            {
                return new CareActionResult(true, false, $"{message} 레벨이 올랐습니다. 부화 {HatchingSystem.GetHatchProgressPercent(tama)}%.", true);
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
