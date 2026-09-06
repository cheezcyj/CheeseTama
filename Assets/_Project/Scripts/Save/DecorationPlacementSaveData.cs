using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Decorations;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class DecorationPlacementSaveEntry
    {
        public int slot;
        public string decorationId = string.Empty;
        public float normalizedX;
        public float normalizedY;
        public int rotationStep;
    }

    [Serializable]
    public sealed class DecorationPlacementSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumPlacementEntries = 3;

        public int schemaVersion = CurrentSchemaVersion;
        public List<DecorationPlacementSaveEntry> entries =
            new List<DecorationPlacementSaveEntry>();

        public static DecorationPlacementSaveData MigrateLegacySlots(
            string equippedAccentId,
            string equippedShelfId,
            string equippedBedsideId)
        {
            var migrated = new DecorationPlacementSaveData();
            migrated.ReplaceSnapshot(DecorationPlacementSystem.MigrateFromLegacySlots(
                equippedAccentId,
                equippedShelfId,
                equippedBedsideId));
            return migrated;
        }

        public bool EnsureRuntimeDefaults(
            string equippedAccentId,
            string equippedShelfId,
            string equippedBedsideId)
        {
            var changed = false;
            if (schemaVersion < CurrentSchemaVersion)
            {
                schemaVersion = CurrentSchemaVersion;
                changed = true;
            }

            var normalized = DecorationPlacementSystem.ReconcileWithEquippedSlots(
                ToSnapshot(),
                equippedAccentId,
                equippedShelfId,
                equippedBedsideId);
            changed |= ReplaceSnapshot(normalized);
            return changed;
        }

        public DecorationPlacementSnapshot GetSnapshot(
            string equippedAccentId,
            string equippedShelfId,
            string equippedBedsideId)
        {
            EnsureRuntimeDefaults(
                equippedAccentId,
                equippedShelfId,
                equippedBedsideId);
            return ToSnapshot();
        }

        public DecorationPlacementSnapshot ToSnapshot()
        {
            var source = entries ?? new List<DecorationPlacementSaveEntry>();
            var placements = new List<DecorationPlacementEntry>(
                Math.Min(source.Count, MaximumPlacementEntries));
            for (var index = 0; index < source.Count; index += 1)
            {
                var entry = source[index];
                if (entry == null || !Enum.IsDefined(typeof(DecorationSlot), entry.slot))
                {
                    continue;
                }

                placements.Add(new DecorationPlacementEntry(
                    (DecorationSlot)entry.slot,
                    entry.decorationId,
                    entry.normalizedX,
                    entry.normalizedY,
                    entry.rotationStep));
            }

            return new DecorationPlacementSnapshot(placements);
        }

        public bool ReplaceSnapshot(DecorationPlacementSnapshot snapshot)
        {
            snapshot ??= new DecorationPlacementSnapshot(null);
            var replacements = new List<DecorationPlacementSaveEntry>(
                Math.Min(snapshot.Entries.Count, MaximumPlacementEntries));
            for (var index = 0;
                 index < snapshot.Entries.Count && replacements.Count < MaximumPlacementEntries;
                 index += 1)
            {
                var entry = snapshot.Entries[index];
                replacements.Add(new DecorationPlacementSaveEntry
                {
                    slot = (int)entry.Slot,
                    decorationId = entry.DecorationId,
                    normalizedX = entry.NormalizedX,
                    normalizedY = entry.NormalizedY,
                    rotationStep = entry.RotationStep
                });
            }

            var changed = !EntriesEqual(entries, replacements);
            entries = replacements;
            return changed;
        }

        private static bool EntriesEqual(
            IReadOnlyList<DecorationPlacementSaveEntry> left,
            IReadOnlyList<DecorationPlacementSaveEntry> right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index += 1)
            {
                var leftEntry = left[index];
                var rightEntry = right[index];
                if (leftEntry == null
                    || rightEntry == null
                    || leftEntry.slot != rightEntry.slot
                    || !string.Equals(
                        leftEntry.decorationId,
                        rightEntry.decorationId,
                        StringComparison.Ordinal)
                    || Math.Abs(leftEntry.normalizedX - rightEntry.normalizedX) >= 0.0001f
                    || Math.Abs(leftEntry.normalizedY - rightEntry.normalizedY) >= 0.0001f
                    || leftEntry.rotationStep != rightEntry.rotationStep)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
