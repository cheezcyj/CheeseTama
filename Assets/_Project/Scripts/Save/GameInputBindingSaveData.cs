using System;
using System.Collections.Generic;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class GameInputBindingSaveData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public List<GameInputBindingSaveEntry> bindings = new List<GameInputBindingSaveEntry>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            if (bindings == null)
            {
                bindings = new List<GameInputBindingSaveEntry>();
                changed = true;
            }

            for (var index = bindings.Count - 1; index >= 0; index -= 1)
            {
                var entry = bindings[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.actionId))
                {
                    bindings.RemoveAt(index);
                    changed = true;
                    continue;
                }

                entry.actionId = entry.actionId.Trim();
                entry.primaryKey ??= string.Empty;
                entry.secondaryKey ??= string.Empty;
            }

            return changed;
        }
    }

    [Serializable]
    public sealed class GameInputBindingSaveEntry
    {
        public string actionId = string.Empty;
        public string primaryKey = string.Empty;
        public string secondaryKey = string.Empty;
    }
}
