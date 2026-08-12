using CheeseTama.Data;

namespace CheeseTama.Gameplay.Growth
{
    public sealed class EvolutionSystem
    {
        public bool CanUseEvolution(CheeseTamaModel tama, UnlockSaveData unlocks, EvolutionDefinition evolution)
        {
            if (tama == null || unlocks == null || evolution == null || evolution.requirements == null)
            {
                return false;
            }

            var requirements = evolution.requirements;
            return tama.level >= requirements.cheeseTamaLevel
                && (!requirements.starEggUnlocked || unlocks.starEggUnlocked)
                && (!requirements.starMilkUnlocked || unlocks.starMilkUnlocked);
        }

        public bool TryApplyEvolution(CheeseTamaModel tama, UnlockSaveData unlocks, EvolutionDefinition evolution)
        {
            if (!CanUseEvolution(tama, unlocks, evolution))
            {
                return false;
            }

            tama.evolutionId = evolution.id;
            tama.form = evolution.id;
            return true;
        }
    }
}

