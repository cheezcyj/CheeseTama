using System;
using UnityEngine;

namespace CheeseTama.Data
{
    [CreateAssetMenu(menuName = "CheeseTama/데이터/숨겨진 도감", fileName = "HiddenCollection_")]
    public sealed class HiddenCollectionDefinition : ScriptableObject
    {
        public string id;
        public string internalCategory;
        public string displayNameAfterUnlock;
        public Rarity rarity = Rarity.Epic;
        public bool isHiddenUntilUnlocked = true;
        public HiddenDisplayPolicy displayPolicy = HiddenDisplayPolicy.CreateDefault();
    }

    [Serializable]
    public sealed class HiddenDisplayPolicy
    {
        public bool showSlotBeforeUnlock;
        public bool showNameBeforeUnlock;
        public bool showRarityBeforeUnlock;
        public bool showCategoryBeforeUnlock;
        public bool showConditionBeforeUnlock;
        public bool showConditionAfterUnlock;
        public bool showNameAfterUnlock = true;
        public bool showRarityAfterUnlock = true;
        public bool showCardImageAfterUnlock = true;
        public bool showQuoteAfterUnlock = true;
        public bool showDeepTextAfterUnlock = true;
        public bool showAcquiredDateAfterUnlock = true;

        public static HiddenDisplayPolicy CreateDefault()
        {
            return new HiddenDisplayPolicy
            {
                showSlotBeforeUnlock = false,
                showNameBeforeUnlock = false,
                showRarityBeforeUnlock = false,
                showCategoryBeforeUnlock = false,
                showConditionBeforeUnlock = false,
                showConditionAfterUnlock = false
            };
        }
    }
}
