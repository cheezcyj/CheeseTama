using System;
using System.Collections.Generic;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class DecorationWorkshopSelectionSaveEntry
    {
        public int slot;
        public string variantId = string.Empty;
    }

    [Serializable]
    public sealed class DecorationWorkshopSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumOwnedVariantIds = 64;
        public const int MaximumCraftReceiptKeys = 256;
        public const int MaximumSelectedVariants = 6;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> ownedVariantIds = new List<string>();
        public List<string> appliedCraftReceiptKeys = new List<string>();
        public List<DecorationWorkshopSelectionSaveEntry> selectedVariants =
            new List<DecorationWorkshopSelectionSaveEntry>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            if (schemaVersion < CurrentSchemaVersion)
            {
                schemaVersion = CurrentSchemaVersion;
                changed = true;
            }

            changed |= NormalizeIds(
                ref ownedVariantIds,
                MaximumOwnedVariantIds,
                keepNewest: false);
            changed |= NormalizeIds(
                ref appliedCraftReceiptKeys,
                MaximumCraftReceiptKeys,
                keepNewest: true);
            changed |= NormalizeSelections(ref selectedVariants);
            return changed;
        }

        public bool Owns(string variantId)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(ownedVariantIds, variantId);
        }

        public bool HasAppliedCraftReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(appliedCraftReceiptKeys, receiptKey);
        }

        public string GetSelectedVariantId(int slot)
        {
            EnsureRuntimeDefaults();
            for (var index = 0; index < selectedVariants.Count; index += 1)
            {
                var entry = selectedVariants[index];
                if (entry != null && entry.slot == slot)
                {
                    return entry.variantId ?? string.Empty;
                }
            }

            return string.Empty;
        }

        internal bool CanAddOwnedVariant(string variantId)
        {
            EnsureRuntimeDefaults();
            var normalizedId = NormalizeId(variantId);
            return !string.IsNullOrEmpty(normalizedId)
                && (ContainsOrdinal(ownedVariantIds, normalizedId)
                    || ownedVariantIds.Count < MaximumOwnedVariantIds);
        }

        internal bool CanAddCraftReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            var normalizedKey = NormalizeId(receiptKey);
            return !string.IsNullOrEmpty(normalizedKey)
                && (ContainsOrdinal(appliedCraftReceiptKeys, normalizedKey)
                    || appliedCraftReceiptKeys.Count < MaximumCraftReceiptKeys);
        }

        internal bool AddOwnedVariant(string variantId)
        {
            if (!CanAddOwnedVariant(variantId))
            {
                return false;
            }

            var normalizedId = NormalizeId(variantId);
            if (ContainsOrdinal(ownedVariantIds, normalizedId))
            {
                return false;
            }

            ownedVariantIds.Add(normalizedId);
            return true;
        }

        internal bool AddCraftReceipt(string receiptKey)
        {
            if (!CanAddCraftReceipt(receiptKey))
            {
                return false;
            }

            var normalizedKey = NormalizeId(receiptKey);
            if (ContainsOrdinal(appliedCraftReceiptKeys, normalizedKey))
            {
                return false;
            }

            appliedCraftReceiptKeys.Add(normalizedKey);
            return true;
        }

        internal bool SetSelectedVariant(int slot, string variantId)
        {
            EnsureRuntimeDefaults();
            var normalizedId = NormalizeId(variantId);
            var existingIndex = -1;
            for (var index = 0; index < selectedVariants.Count; index += 1)
            {
                if (selectedVariants[index] != null
                    && selectedVariants[index].slot == slot)
                {
                    existingIndex = index;
                    break;
                }
            }

            if (string.IsNullOrEmpty(normalizedId))
            {
                if (existingIndex < 0)
                {
                    return false;
                }

                selectedVariants.RemoveAt(existingIndex);
                return true;
            }

            if (existingIndex >= 0)
            {
                if (string.Equals(
                        selectedVariants[existingIndex].variantId,
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    return false;
                }

                selectedVariants[existingIndex].variantId = normalizedId;
                return true;
            }

            if (selectedVariants.Count >= MaximumSelectedVariants)
            {
                return false;
            }

            selectedVariants.Add(new DecorationWorkshopSelectionSaveEntry
            {
                slot = slot,
                variantId = normalizedId
            });
            selectedVariants.Sort((left, right) => left.slot.CompareTo(right.slot));
            return true;
        }

        internal bool ReplaceSelections(
            IEnumerable<DecorationWorkshopSelectionSaveEntry> replacements)
        {
            var next = replacements == null
                ? new List<DecorationWorkshopSelectionSaveEntry>()
                : new List<DecorationWorkshopSelectionSaveEntry>(replacements);
            var changed = !SelectionsEqual(selectedVariants, next);
            selectedVariants = next;
            changed |= NormalizeSelections(ref selectedVariants);
            return changed;
        }

        private static bool NormalizeSelections(
            ref List<DecorationWorkshopSelectionSaveEntry> values)
        {
            var changed = values == null;
            var source = values ?? new List<DecorationWorkshopSelectionSaveEntry>();
            var bySlot = new Dictionary<int, string>();
            for (var index = 0; index < source.Count; index += 1)
            {
                var entry = source[index];
                if (entry == null
                    || entry.slot < 0
                    || entry.slot >= MaximumSelectedVariants)
                {
                    changed = true;
                    continue;
                }

                var normalizedId = NormalizeId(entry.variantId);
                if (string.IsNullOrEmpty(normalizedId))
                {
                    changed = true;
                    continue;
                }

                if (bySlot.ContainsKey(entry.slot))
                {
                    changed = true;
                }

                bySlot[entry.slot] = normalizedId;
                changed |= !string.Equals(
                    entry.variantId,
                    normalizedId,
                    StringComparison.Ordinal);
            }

            var normalized = new List<DecorationWorkshopSelectionSaveEntry>(bySlot.Count);
            for (var slot = 0; slot < MaximumSelectedVariants; slot += 1)
            {
                if (!bySlot.TryGetValue(slot, out var variantId))
                {
                    continue;
                }

                normalized.Add(new DecorationWorkshopSelectionSaveEntry
                {
                    slot = slot,
                    variantId = variantId
                });
            }

            changed |= !SelectionsEqual(source, normalized);
            values = normalized;
            return changed;
        }

        private static bool NormalizeIds(
            ref List<string> values,
            int maximumCount,
            bool keepNewest)
        {
            var changed = values == null;
            var source = values ?? new List<string>();
            var normalized = new List<string>(Math.Min(source.Count, maximumCount));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var startIndex = keepNewest
                ? Math.Max(0, source.Count - maximumCount)
                : 0;

            for (var index = startIndex; index < source.Count; index += 1)
            {
                var value = NormalizeId(source[index]);
                if (string.IsNullOrEmpty(value) || !seen.Add(value))
                {
                    changed = true;
                    continue;
                }

                if (normalized.Count >= maximumCount)
                {
                    changed = true;
                    break;
                }

                normalized.Add(value);
                changed |= !string.Equals(source[index], value, StringComparison.Ordinal);
            }

            changed |= normalized.Count != source.Count;
            values = normalized;
            return changed;
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string requested)
        {
            var normalized = NormalizeId(requested);
            if (values == null || string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            for (var index = 0; index < values.Count; index += 1)
            {
                if (string.Equals(values[index], normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SelectionsEqual(
            IReadOnlyList<DecorationWorkshopSelectionSaveEntry> left,
            IReadOnlyList<DecorationWorkshopSelectionSaveEntry> right)
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
                        leftEntry.variantId,
                        rightEntry.variantId,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
