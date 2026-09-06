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
        public int heavinessIntensity;
        public int heavinessHoursRemaining;
        public int sugarOverloadIntensity;
        public int sugarOverloadHoursRemaining;
        public int fantasyEchoIntensity;
        public int fantasyEchoHoursRemaining;
        public int lethargyIntensity;
        public int lethargyHoursRemaining;
        public int stomachRiskIntensity;
        public int stomachRiskHoursRemaining;
        public int traitDistortionIntensity;
        public int traitDistortionHoursRemaining;

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
                sleepRhythmDisruptionHoursRemaining = 0,
                heavinessIntensity = 0,
                heavinessHoursRemaining = 0,
                sugarOverloadIntensity = 0,
                sugarOverloadHoursRemaining = 0,
                fantasyEchoIntensity = 0,
                fantasyEchoHoursRemaining = 0,
                lethargyIntensity = 0,
                lethargyHoursRemaining = 0,
                stomachRiskIntensity = 0,
                stomachRiskHoursRemaining = 0,
                traitDistortionIntensity = 0,
                traitDistortionHoursRemaining = 0
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
            heavinessIntensity = Mathf.Clamp(heavinessIntensity, 0, 100);
            heavinessHoursRemaining = Mathf.Clamp(heavinessHoursRemaining, 0, 12);
            sugarOverloadIntensity = Mathf.Clamp(sugarOverloadIntensity, 0, 100);
            sugarOverloadHoursRemaining = Mathf.Clamp(sugarOverloadHoursRemaining, 0, 12);
            fantasyEchoIntensity = Mathf.Clamp(fantasyEchoIntensity, 0, 100);
            fantasyEchoHoursRemaining = Mathf.Clamp(fantasyEchoHoursRemaining, 0, 12);
            lethargyIntensity = Mathf.Clamp(lethargyIntensity, 0, 100);
            lethargyHoursRemaining = Mathf.Clamp(lethargyHoursRemaining, 0, 12);
            stomachRiskIntensity = Mathf.Clamp(stomachRiskIntensity, 0, 100);
            stomachRiskHoursRemaining = Mathf.Clamp(stomachRiskHoursRemaining, 0, 12);
            traitDistortionIntensity = Mathf.Clamp(traitDistortionIntensity, 0, 100);
            traitDistortionHoursRemaining = Mathf.Clamp(traitDistortionHoursRemaining, 0, 12);

            NormalizeAftereffect(ref bodyChillIntensity, ref bodyChillHoursRemaining);
            NormalizeAftereffect(ref fermentedAftertasteIntensity, ref fermentedAftertasteHoursRemaining);
            NormalizeAftereffect(ref sleepRhythmDisruptionIntensity, ref sleepRhythmDisruptionHoursRemaining);
            NormalizeAftereffect(ref heavinessIntensity, ref heavinessHoursRemaining);
            NormalizeAftereffect(ref sugarOverloadIntensity, ref sugarOverloadHoursRemaining);
            NormalizeAftereffect(ref fantasyEchoIntensity, ref fantasyEchoHoursRemaining);
            NormalizeAftereffect(ref lethargyIntensity, ref lethargyHoursRemaining);
            NormalizeAftereffect(ref stomachRiskIntensity, ref stomachRiskHoursRemaining);
            NormalizeAftereffect(ref traitDistortionIntensity, ref traitDistortionHoursRemaining);
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
