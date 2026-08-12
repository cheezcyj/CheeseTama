using System;
using CheeseTama.Gameplay.Growth;
using UnityEngine;

namespace CheeseTama.Data
{
    [Serializable]
    public sealed class CheeseTamaGrowthVisualEntry
    {
        public CheeseTamaGrowthStage stage;
        public GameObject prefab;
        public Sprite thumbnail;
    }

    [CreateAssetMenu(fileName = "CheeseTamaGrowthVisualSet", menuName = "CheeseTama/Growth Visual Set")]
    public sealed class CheeseTamaGrowthVisualSet : ScriptableObject
    {
        [SerializeField] private CheeseTamaGrowthVisualEntry[] entries = Array.Empty<CheeseTamaGrowthVisualEntry>();

        public GameObject GetPrefab(CheeseTamaGrowthStage stage)
        {
            var entry = Find(stage);
            return entry != null ? entry.prefab : null;
        }

        public Sprite GetThumbnail(CheeseTamaGrowthStage stage)
        {
            var entry = Find(stage);
            return entry != null ? entry.thumbnail : null;
        }

        private CheeseTamaGrowthVisualEntry Find(CheeseTamaGrowthStage stage)
        {
            if (entries == null)
            {
                return null;
            }

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].stage == stage)
                {
                    return entries[i];
                }
            }

            return null;
        }
    }
}
