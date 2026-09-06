using CheeseTama.Data;
using CheeseTama.Gameplay.Growth;

namespace CheeseTama.Gameplay.Feeding
{
    public sealed class FeedingSystem
    {
        private readonly PenaltySystem penaltySystem = new PenaltySystem();

        public FeedingResult FeedMilk(CheeseTamaModel tama, MilkItemDefinition milk, MilkGrowthSystem milkGrowthSystem)
        {
            if (tama == null || milk == null)
            {
                return new FeedingResult(false, string.Empty, string.Empty);
            }

            tama.stats.Apply(milk.effects);
            milkGrowthSystem?.AddGrowthPoints(milk.id, milk.growthPointGain);

            var penaltyId = penaltySystem.RollPenalty(tama, milk.penaltyCandidates);
            tama.growthHistory.lastFedMilkId = milk.id;

            return new FeedingResult(true, milk.id, penaltyId);
        }
    }
}

