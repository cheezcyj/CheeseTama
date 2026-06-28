using UnityEngine;

namespace CheeseTama.Data
{
    [CreateAssetMenu(menuName = "CheeseTama/Data/Milk Item", fileName = "MilkItem_")]
    public sealed class MilkItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Rarity rarity = Rarity.Common;
        public ItemCategory category = ItemCategory.Milk;
        public StatEffect effects;
        public PenaltyCandidate[] penaltyCandidates;
        public int growthPointGain = 1;
        public bool isHiddenUntilUnlocked;
    }
}

