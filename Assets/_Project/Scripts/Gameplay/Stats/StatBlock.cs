using System;
using CheeseTama.Data;
using UnityEngine;

namespace CheeseTama.Gameplay.Stats
{
    [Serializable]
    public sealed class StatBlock
    {
        public int hunger;
        public int mood;
        public int cleanliness;
        public int sleepiness;
        public int health;
        public int maturation;
        public int affection;
        public int milkSatisfaction;
        public int overfullness;
        public int bodyChillIntensity;
        public int bodyChillHoursRemaining;
        public int fermentedAftertasteIntensity;
        public int fermentedAftertasteHoursRemaining;
        public int sleepRhythmDisruptionIntensity;
        public int sleepRhythmDisruptionHoursRemaining;

        public static StatBlock CreateDefault()
        {
            return new StatBlock
            {
                hunger = 80,
                mood = 70,
                cleanliness = 90,
                sleepiness = 20,
                health = 100,
                maturation = 0,
                affection = 10,
                milkSatisfaction = 50,
                overfullness = 0,
                bodyChillIntensity = 0,
                bodyChillHoursRemaining = 0,
                fermentedAftertasteIntensity = 0,
                fermentedAftertasteHoursRemaining = 0,
                sleepRhythmDisruptionIntensity = 0,
                sleepRhythmDisruptionHoursRemaining = 0
            };
        }

        public void Apply(StatEffect effect)
        {
            hunger += effect.hunger;
            mood += effect.mood;
            cleanliness += effect.cleanliness;
            sleepiness += effect.sleepiness;
            health += effect.health;
            maturation += effect.maturation;
            affection += effect.affection;
            milkSatisfaction += effect.milkSatisfaction;
            ClampAll();
        }

        public void ClampAll()
        {
            hunger = Mathf.Clamp(hunger, 0, 100);
            mood = Mathf.Clamp(mood, 0, 100);
            cleanliness = Mathf.Clamp(cleanliness, 0, 100);
            sleepiness = Mathf.Clamp(sleepiness, 0, 100);
            health = Mathf.Clamp(health, 0, 100);
            maturation = Mathf.Clamp(maturation, 0, 100);
            affection = Mathf.Clamp(affection, 0, 100);
            milkSatisfaction = Mathf.Clamp(milkSatisfaction, 0, 100);
            ClampFeedingStatuses();
        }

        public void ClampFeedingStatuses()
        {
            overfullness = Mathf.Clamp(overfullness, 0, 100);
            bodyChillIntensity = Mathf.Clamp(bodyChillIntensity, 0, 100);
            bodyChillHoursRemaining = Mathf.Clamp(bodyChillHoursRemaining, 0, 12);
            fermentedAftertasteIntensity = Mathf.Clamp(fermentedAftertasteIntensity, 0, 100);
            fermentedAftertasteHoursRemaining = Mathf.Clamp(fermentedAftertasteHoursRemaining, 0, 12);
            sleepRhythmDisruptionIntensity = Mathf.Clamp(sleepRhythmDisruptionIntensity, 0, 100);
            sleepRhythmDisruptionHoursRemaining = Mathf.Clamp(sleepRhythmDisruptionHoursRemaining, 0, 12);

            NormalizeAftereffect(ref bodyChillIntensity, ref bodyChillHoursRemaining);
            NormalizeAftereffect(ref fermentedAftertasteIntensity, ref fermentedAftertasteHoursRemaining);
            NormalizeAftereffect(ref sleepRhythmDisruptionIntensity, ref sleepRhythmDisruptionHoursRemaining);
        }

        private static void NormalizeAftereffect(ref int intensity, ref int hoursRemaining)
        {
            if (intensity <= 0 || hoursRemaining <= 0)
            {
                intensity = 0;
                hoursRemaining = 0;
            }
        }
    }
}
