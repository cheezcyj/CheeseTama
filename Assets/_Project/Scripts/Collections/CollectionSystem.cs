using System.Collections.Generic;

namespace CheeseTama.Collections
{
    public sealed class CollectionSystem
    {
        public void RegisterMilk(CollectionSaveData collections, string milkId)
        {
            if (collections == null)
            {
                return;
            }

            collections.EnsureRuntimeDefaults();
            AddUnique(collections.milk, milkId);
        }

        public void RegisterEvolution(CollectionSaveData collections, string evolutionId)
        {
            if (collections == null)
            {
                return;
            }

            collections.EnsureRuntimeDefaults();
            AddUnique(collections.evolution, evolutionId);
        }

        public void RegisterEvent(CollectionSaveData collections, string eventId)
        {
            if (collections == null)
            {
                return;
            }

            collections.EnsureRuntimeDefaults();
            AddUnique(collections.events, eventId);
        }

        private static void AddUnique(ICollection<string> target, string id)
        {
            if (target == null || string.IsNullOrWhiteSpace(id) || target.Contains(id))
            {
                return;
            }

            target.Add(id);
        }
    }
}
