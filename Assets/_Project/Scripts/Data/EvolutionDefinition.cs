using System;
using UnityEngine;

namespace CheeseTama.Data
{
    [CreateAssetMenu(menuName = "CheeseTama/데이터/진화", fileName = "Evolution_")]
    public sealed class EvolutionDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Rarity rarity = Rarity.Common;
        public bool isStarRouteEvolution;
        public bool visibleInCollectionBeforeUnlock = true;
        public EvolutionRequirement requirements;
        public EvolutionVisualNotes visualNotes;
    }

    [Serializable]
    public sealed class EvolutionRequirement
    {
        public bool starEggUnlocked;
        public bool starMilkUnlocked;
        public int cheeseTamaLevel = 1;
        public bool allMainMilkGrowthMax;
    }

    [Serializable]
    public sealed class EvolutionVisualNotes
    {
        public int holeCount;
        public string pattern;
    }
}
