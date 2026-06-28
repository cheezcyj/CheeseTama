using CheeseTama.Data;
using UnityEngine;

namespace CheeseTama.Gameplay.Feeding
{
    public sealed class PenaltySystem
    {
        public string RollPenalty(CheeseTamaModel tama, PenaltyCandidate[] candidates)
        {
            if (tama == null || candidates == null)
            {
                return string.Empty;
            }

            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (!ConditionMatches(tama, candidate.condition))
                {
                    continue;
                }

                if (Random.value <= candidate.chance)
                {
                    return candidate.id;
                }
            }

            return string.Empty;
        }

        private static bool ConditionMatches(CheeseTamaModel tama, string condition)
        {
            return string.IsNullOrWhiteSpace(condition)
                || condition == "always"
                || condition == "repeated_same_milk" && !string.IsNullOrEmpty(tama.growthHistory.lastFedMilkId);
        }
    }
}

