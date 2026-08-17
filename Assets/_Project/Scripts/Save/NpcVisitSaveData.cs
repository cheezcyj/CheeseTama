using System;
using System.Collections.Generic;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class NpcVisitSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumReceipts = 64;

        public int schemaVersion = CurrentSchemaVersion;
        public string dateKey = string.Empty;
        public int visitsToday;
        public string nextAllowedAtIso = string.Empty;
        public PendingNpcVisitSaveData pending = new PendingNpcVisitSaveData();
        public List<NpcRelationshipSaveEntry> relationships = new List<NpcRelationshipSaveEntry>();
        public List<NpcVisitReceiptSaveEntry> receipts = new List<NpcVisitReceiptSaveEntry>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            changed |= EnsureString(ref dateKey);
            changed |= EnsureString(ref nextAllowedAtIso);
            if (!string.IsNullOrWhiteSpace(nextAllowedAtIso)
                && !DateTimeOffset.TryParse(nextAllowedAtIso, out _))
            {
                nextAllowedAtIso = string.Empty;
                changed = true;
            }
            if (visitsToday < 0)
            {
                visitsToday = 0;
                changed = true;
            }

            if (pending == null)
            {
                pending = new PendingNpcVisitSaveData();
                changed = true;
            }

            changed |= pending.EnsureRuntimeDefaults();
            relationships ??= new List<NpcRelationshipSaveEntry>();
            receipts ??= new List<NpcVisitReceiptSaveEntry>();
            changed |= NormalizeRelationships();
            changed |= NormalizeReceipts();
            if (pending.HasValue && HasReceipt(pending.occurrenceId))
            {
                pending.Clear();
                changed = true;
            }

            return changed;
        }

        private bool HasReceipt(string occurrenceId)
        {
            for (var index = 0; index < receipts.Count; index += 1)
            {
                if (string.Equals(
                        receipts[index]?.occurrenceId,
                        occurrenceId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool NormalizeRelationships()
        {
            var changed = false;
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = relationships.Count - 1; index >= 0; index -= 1)
            {
                var entry = relationships[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.npcId))
                {
                    relationships.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (!known.Add(entry.npcId))
                {
                    relationships.RemoveAt(index);
                    changed = true;
                }
            }

            return changed;
        }

        private bool NormalizeReceipts()
        {
            var changed = false;
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = receipts.Count - 1; index >= 0; index -= 1)
            {
                var entry = receipts[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.occurrenceId))
                {
                    receipts.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (!known.Add(entry.occurrenceId))
                {
                    receipts.RemoveAt(index);
                    changed = true;
                }
            }

            while (receipts.Count > MaximumReceipts)
            {
                receipts.RemoveAt(0);
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

    [Serializable]
    public sealed class PendingNpcVisitSaveData
    {
        public string occurrenceId = string.Empty;
        public string npcId = string.Empty;
        public int storyStep;
        public string queuedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrWhiteSpace(occurrenceId)
            && !string.IsNullOrWhiteSpace(npcId);

        public void Set(string occurrence, string visitorId, int step, string queuedAt)
        {
            occurrenceId = occurrence ?? string.Empty;
            npcId = visitorId ?? string.Empty;
            storyStep = Math.Max(0, Math.Min(2, step));
            queuedAtIso = queuedAt ?? string.Empty;
        }

        public void Clear()
        {
            occurrenceId = string.Empty;
            npcId = string.Empty;
            storyStep = 0;
            queuedAtIso = string.Empty;
        }

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            occurrenceId ??= string.Empty;
            npcId ??= string.Empty;
            queuedAtIso ??= string.Empty;
            var clamped = Math.Max(0, Math.Min(2, storyStep));
            if (clamped != storyStep)
            {
                storyStep = clamped;
                changed = true;
            }

            if (!HasValue && (!string.IsNullOrEmpty(occurrenceId)
                || !string.IsNullOrEmpty(npcId)
                || storyStep != 0
                || !string.IsNullOrEmpty(queuedAtIso)))
            {
                Clear();
                changed = true;
            }

            return changed;
        }
    }

    [Serializable]
    public sealed class NpcRelationshipSaveEntry
    {
        public string npcId = string.Empty;
        public int visits;
        public int affinity;
        public int storyStep;
        public string lastVisitedAtIso = string.Empty;

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            npcId = (npcId ?? string.Empty).Trim();
            lastVisitedAtIso ??= string.Empty;
            var safeVisits = Math.Max(0, visits);
            var safeAffinity = Math.Max(0, Math.Min(99, affinity));
            var safeStory = Math.Max(0, Math.Min(2, storyStep));
            changed |= safeVisits != visits || safeAffinity != affinity || safeStory != storyStep;
            visits = safeVisits;
            affinity = safeAffinity;
            storyStep = safeStory;
            return changed;
        }
    }

    [Serializable]
    public sealed class NpcVisitReceiptSaveEntry
    {
        public string occurrenceId = string.Empty;
        public string npcId = string.Empty;
        public string choiceId = string.Empty;
        public string resolvedAtIso = string.Empty;

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= Normalize(ref occurrenceId);
            changed |= Normalize(ref npcId);
            changed |= Normalize(ref choiceId);
            changed |= Normalize(ref resolvedAtIso);
            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
