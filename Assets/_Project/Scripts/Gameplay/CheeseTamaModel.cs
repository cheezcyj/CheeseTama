using System;
using CheeseTama.Gameplay.Stats;

namespace CheeseTama.Gameplay
{
    [Serializable]
    public sealed class CheeseTamaModel
    {
        public string id = "ct_001";
        public string name = "CheeseTama";
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
        }
    }

    [Serializable]
    public sealed class GrowthHistory
    {
        public string mostUsedMilkId = "basic_milk";
        public string mostUsedIngredientId = "none";
        public string careStyle = "gentle";
        public string lastFedMilkId = string.Empty;
    }
}
