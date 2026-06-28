using CheeseTama.Data;
using UnityEngine;

namespace CheeseTama.Gameplay.Events
{
    public sealed class RandomEventSystem
    {
        public GameEventDefinition Roll(GameEventDefinition[] candidates)
        {
            if (candidates == null)
            {
                return null;
            }

            foreach (var candidate in candidates)
            {
                if (candidate == null || candidate.isHiddenUntilUnlocked)
                {
                    continue;
                }

                if (Random.value <= candidate.baseChance)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}

