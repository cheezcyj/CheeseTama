using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Autonomy;

namespace CheeseTama.Save
{
    /// <summary>
    /// Standalone persistence contract. The root save owner may add this DTO as a
    /// nullable field and call EnsureRuntimeDefaults during its normal migration.
    /// </summary>
    [Serializable]
    public sealed class AutonomousLifeSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumDiscoveries = 6;

        public int schemaVersion = CurrentSchemaVersion;
        public List<AutonomousLifeDiscoverySaveEntry> firstDiscoveries =
            new List<AutonomousLifeDiscoverySaveEntry>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            var source = firstDiscoveries;
            if (source == null)
            {
                source = new List<AutonomousLifeDiscoverySaveEntry>();
                changed = true;
            }

            var normalized = new List<AutonomousLifeDiscoverySaveEntry>(
                Math.Min(source.Count, MaximumDiscoveries));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < source.Count; index += 1)
            {
                var entry = source[index];
                if (entry == null || entry.EnsureRuntimeDefaults())
                {
                    changed = true;
                }

                if (entry == null
                    || !AutonomousLifeBehaviourCatalog.TryParseId(
                        entry.behaviourId,
                        out var behaviour))
                {
                    continue;
                }

                var canonicalId = AutonomousLifeBehaviourCatalog.GetId(behaviour);
                if (!seen.Add(canonicalId))
                {
                    changed = true;
                    continue;
                }

                if (!string.Equals(entry.behaviourId, canonicalId, StringComparison.Ordinal))
                {
                    entry.behaviourId = canonicalId;
                    changed = true;
                }

                if (normalized.Count >= MaximumDiscoveries)
                {
                    changed = true;
                    break;
                }

                normalized.Add(entry);
            }

            changed |= normalized.Count != source.Count;
            firstDiscoveries = normalized;
            return changed;
        }

        public bool HasDiscovered(string behaviourId)
        {
            return Find(behaviourId) != null;
        }

        public AutonomousLifeDiscoverySaveEntry Find(string behaviourId)
        {
            EnsureRuntimeDefaults();
            if (!AutonomousLifeBehaviourCatalog.TryParseId(behaviourId, out var behaviour))
            {
                return null;
            }

            var canonicalId = AutonomousLifeBehaviourCatalog.GetId(behaviour);
            for (var index = 0; index < firstDiscoveries.Count; index += 1)
            {
                var entry = firstDiscoveries[index];
                if (entry != null
                    && string.Equals(
                        entry.behaviourId,
                        canonicalId,
                        StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        public bool TryRecordFirstDiscovery(
            string behaviourId,
            DateTimeOffset discoveredAt,
            out AutonomousLifeDiscoverySaveEntry entry)
        {
            EnsureRuntimeDefaults();
            entry = Find(behaviourId);
            if (entry != null)
            {
                return false;
            }

            if (!AutonomousLifeBehaviourCatalog.TryParseId(behaviourId, out var behaviour)
                || firstDiscoveries.Count >= MaximumDiscoveries)
            {
                return false;
            }

            entry = new AutonomousLifeDiscoverySaveEntry
            {
                behaviourId = AutonomousLifeBehaviourCatalog.GetId(behaviour),
                firstDiscoveredAtIso = discoveredAt.ToString("O")
            };
            firstDiscoveries.Add(entry);
            return true;
        }
    }

    [Serializable]
    public sealed class AutonomousLifeDiscoverySaveEntry
    {
        public string behaviourId = string.Empty;
        public string firstDiscoveredAtIso = string.Empty;

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            if (behaviourId == null)
            {
                behaviourId = string.Empty;
                changed = true;
            }
            else
            {
                var normalizedId = behaviourId.Trim();
                if (!string.Equals(behaviourId, normalizedId, StringComparison.Ordinal))
                {
                    behaviourId = normalizedId;
                    changed = true;
                }
            }

            if (firstDiscoveredAtIso == null)
            {
                firstDiscoveredAtIso = string.Empty;
                changed = true;
            }
            else
            {
                var normalizedTime = firstDiscoveredAtIso.Trim();
                if (!string.Equals(
                    firstDiscoveredAtIso,
                    normalizedTime,
                    StringComparison.Ordinal))
                {
                    firstDiscoveredAtIso = normalizedTime;
                    changed = true;
                }
            }

            return changed;
        }
    }
}
