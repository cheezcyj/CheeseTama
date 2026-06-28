using CheeseTama.Collections;
using CheeseTama.Data;
using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class CollectionUIController : MonoBehaviour
    {
        private readonly HiddenCollectionSystem hiddenCollectionSystem = new HiddenCollectionSystem();

        public HiddenCollectionDefinition[] GetVisibleHiddenCards(
            HiddenCollectionDefinition[] definitions,
            CollectionSaveData collections)
        {
            var visible = hiddenCollectionSystem.GetVisibleUnlockedCards(definitions, collections);
            var result = new HiddenCollectionDefinition[visible.Count];
            for (var i = 0; i < visible.Count; i++)
            {
                result[i] = visible[i];
            }

            return result;
        }
    }
}
