using System;
using System.Collections.Generic;
using CheeseTama.Data;

namespace CheeseTama.Collections
{
    public sealed class HiddenCollectionSystem
    {
        public IReadOnlyList<HiddenCollectionDefinition> GetVisibleUnlockedCards(
            HiddenCollectionDefinition[] definitions,
            CollectionSaveData collections)
        {
            var visible = new List<HiddenCollectionDefinition>();
            if (definitions == null || collections == null)
            {
                return visible;
            }

            collections.EnsureRuntimeDefaults();
            foreach (var definition in definitions)
            {
                if (definition == null || !IsUnlocked(collections, definition.id))
                {
                    continue;
                }

                visible.Add(definition);
            }

            return visible;
        }

        public bool Unlock(CollectionSaveData collections, string hiddenId, DateTimeOffset acquiredAt)
        {
            if (collections == null || string.IsNullOrWhiteSpace(hiddenId))
            {
                return false;
            }

            collections.EnsureRuntimeDefaults();
            if (IsUnlocked(collections, hiddenId))
            {
                return false;
            }

            collections.hiddenUnlockedOnly.Add(new HiddenCollectionSaveEntry
            {
                id = hiddenId,
                acquiredAtIso = acquiredAt.ToString("O")
            });
            return true;
        }

        private static bool IsUnlocked(CollectionSaveData collections, string id)
        {
            foreach (var entry in collections.hiddenUnlockedOnly)
            {
                if (entry != null && entry.id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
