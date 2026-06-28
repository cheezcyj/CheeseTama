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
                milkSatisfaction = 50
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
        }
    }
}

