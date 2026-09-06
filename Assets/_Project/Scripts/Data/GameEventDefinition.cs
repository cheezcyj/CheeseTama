using UnityEngine;

namespace CheeseTama.Data
{
    [CreateAssetMenu(menuName = "CheeseTama/Data/Game Event", fileName = "GameEvent_")]
    public sealed class GameEventDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Rarity rarity = Rarity.Common;
        public bool isHiddenUntilUnlocked;
        public float baseChance = 0.05f;
    }
}

