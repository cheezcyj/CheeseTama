using System;
using System.Collections.Generic;

namespace CheeseTama.Collections
{
    [Serializable]
    public sealed class CollectionSaveData
    {
        public List<string> milk = new List<string>();
        public List<string> evolution = new List<string>();
        public List<string> events = new List<string>();
        public List<HiddenCollectionSaveEntry> hiddenUnlockedOnly = new List<HiddenCollectionSaveEntry>();

        public void EnsureRuntimeDefaults()
        {
            milk ??= new List<string>();
            evolution ??= new List<string>();
            events ??= new List<string>();
            hiddenUnlockedOnly ??= new List<HiddenCollectionSaveEntry>();
        }
    }

    [Serializable]
    public sealed class HiddenCollectionSaveEntry
    {
        public string id;
        public string acquiredAtIso;
    }
}
