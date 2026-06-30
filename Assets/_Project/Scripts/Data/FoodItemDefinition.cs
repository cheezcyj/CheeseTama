using UnityEngine;

namespace CheeseTama.Data
{
    [CreateAssetMenu(menuName = "CheeseTama/데이터/간식 아이템", fileName = "FoodItem_")]
    public sealed class FoodItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Rarity rarity = Rarity.Common;
        public ItemCategory category = ItemCategory.DairySnack;
        public StatEffect effects;
        public PenaltyCandidate[] penaltyCandidates;
        public bool isHiddenUntilUnlocked;
    }
}
