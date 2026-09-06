using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    public enum MemoryJournalKind
    {
        Care = 0,
        Return = 1,
        Growth = 2,
        Evolution = 3,
        Story = 4
    }

    [Serializable]
    public sealed class MemoryJournalSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumEntries = 60;

        public int schemaVersion = CurrentSchemaVersion;
        public List<MemoryJournalEntrySaveData> entries = new List<MemoryJournalEntrySaveData>();
        public string lastRecalledMemoryId = string.Empty;

        /// <summary>
        /// Repairs fields missing from older or partial JSON saves. Legacy entries are treated as read,
        /// because JsonUtility cannot distinguish a missing bool from an explicitly false bool.
        /// </summary>
        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            if (entries == null)
            {
                entries = new List<MemoryJournalEntrySaveData>();
                changed = true;
            }

            if (lastRecalledMemoryId == null)
            {
                lastRecalledMemoryId = string.Empty;
                changed = true;
            }

            var knownKeys = new HashSet<string>(StringComparer.Ordinal);
            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = entries.Count - 1; index >= 0; index -= 1)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    entries.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults(index);
                if (!knownKeys.Add(entry.idempotencyKey) || !knownIds.Add(entry.id))
                {
                    // Keep the most recently appended copy when a partial write produced duplicates.
                    entries.RemoveAt(index);
                    changed = true;
                }
            }

            changed |= TrimToCapacity();
            return changed;
        }

        public bool TrimToCapacity()
        {
            var changed = false;
            while (entries != null && entries.Count > MaximumEntries)
            {
                var removableIndex = -1;
                for (var index = 0; index < entries.Count; index += 1)
                {
                    if (entries[index] == null || !entries[index].important)
                    {
                        removableIndex = index;
                        break;
                    }
                }

                // A journal made entirely of milestones still remains bounded.
                entries.RemoveAt(removableIndex >= 0 ? removableIndex : 0);
                changed = true;
            }

            return changed;
        }
    }

    [Serializable]
    public sealed class MemoryJournalEntrySaveData
    {
        public string id = string.Empty;
        public string idempotencyKey = string.Empty;
        public MemoryJournalKind kind = MemoryJournalKind.Care;
        public string sourceId = string.Empty;
        public string occurrenceId = string.Empty;
        public string detailId = string.Empty;
        public string dateKey = string.Empty;
        public string occurredAtIso = string.Empty;
        public string tamaName = string.Empty;
        public string formId = string.Empty;
        public string title = string.Empty;
        public string quote = string.Empty;
        public bool unread;
        public bool important;
        public bool isHiddenContent;
        public string hiddenUnlockId = string.Empty;

        public bool EnsureRuntimeDefaults(int fallbackIndex = 0)
        {
            var changed = false;
            changed |= EnsureString(ref sourceId);
            changed |= EnsureString(ref occurrenceId);
            changed |= EnsureString(ref detailId);
            changed |= EnsureString(ref dateKey);
            changed |= EnsureString(ref occurredAtIso);
            changed |= EnsureString(ref tamaName);
            changed |= EnsureString(ref formId);
            changed |= EnsureString(ref title);
            changed |= EnsureString(ref quote);
            changed |= EnsureString(ref hiddenUnlockId);

            var kindValue = (int)kind;
            if (kindValue < (int)MemoryJournalKind.Care || kindValue > (int)MemoryJournalKind.Story)
            {
                kind = MemoryJournalKind.Care;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(dateKey)
                && DateTimeOffset.TryParse(
                    occurredAtIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var occurredAt))
            {
                dateKey = occurredAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(tamaName))
            {
                tamaName = "치즈타마";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = "소중한 추억";
                changed = true;
            }

            if ((kind == MemoryJournalKind.Growth || kind == MemoryJournalKind.Evolution) && !important)
            {
                important = true;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                idempotencyKey = MemoryJournalSaveKey.Build(
                    kind,
                    sourceId,
                    dateKey,
                    occurrenceId,
                    fallbackIndex);
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                id = MemoryJournalSaveKey.CreateStableLegacyId(idempotencyKey, fallbackIndex);
                changed = true;
            }

            return changed;
        }

        private static bool EnsureString(ref string value)
        {
            if (value != null)
            {
                return false;
            }

            value = string.Empty;
            return true;
        }
    }

    internal static class MemoryJournalSaveKey
    {
        public static string Build(
            MemoryJournalKind kind,
            string sourceId,
            string dateKey,
            string occurrenceId,
            int fallbackIndex = 0)
        {
            var safeSource = NormalizeToken(sourceId, "unknown");
            var scope = !string.IsNullOrWhiteSpace(occurrenceId)
                ? occurrenceId
                : !string.IsNullOrWhiteSpace(dateKey)
                    ? dateKey
                    : fallbackIndex.ToString(CultureInfo.InvariantCulture);
            return $"{kind.ToString().ToLowerInvariant()}:{safeSource}:{NormalizeToken(scope, "unknown")}";
        }

        public static string CreateStableLegacyId(string key, int fallbackIndex)
        {
            unchecked
            {
                var hash = 2166136261u;
                var value = $"{key}:{fallbackIndex.ToString(CultureInfo.InvariantCulture)}";
                for (var index = 0; index < value.Length; index += 1)
                {
                    hash ^= value[index];
                    hash *= 16777619u;
                }

                return $"memory_legacy_{hash:x8}";
            }
        }

        private static string NormalizeToken(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return value.Trim().Replace(":", "_").Replace("\r", string.Empty).Replace("\n", "_");
        }
    }
}
