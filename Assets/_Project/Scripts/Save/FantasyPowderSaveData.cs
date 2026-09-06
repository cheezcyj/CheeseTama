using System;
using System.Collections.Generic;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class FantasyPowderSaveData
    {
        public const int CurrentSchemaVersion = 2;
        public const int MaximumPityHintLevel = 3;
        public const int MaximumDiscoveredRecipeIds = 16;
        public const int MaximumReceiptKeys = 128;

        public int schemaVersion = CurrentSchemaVersion;
        public int powderQuantity;
        public int attemptCount;
        public int pityHintLevel;
        public bool starterGrantClaimed;
        public List<string> discoveredHiddenRecipeIds = new List<string>();
        public List<string> appliedReceiptKeys = new List<string>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            changed |= ClampNonNegative(ref powderQuantity);
            changed |= ClampNonNegative(ref attemptCount);

            var normalizedHintLevel = Math.Max(
                0,
                Math.Min(MaximumPityHintLevel, pityHintLevel));
            if (pityHintLevel != normalizedHintLevel)
            {
                pityHintLevel = normalizedHintLevel;
                changed = true;
            }

            changed |= NormalizeIds(
                ref discoveredHiddenRecipeIds,
                MaximumDiscoveredRecipeIds,
                keepNewest: false);
            changed |= NormalizeIds(
                ref appliedReceiptKeys,
                MaximumReceiptKeys,
                keepNewest: true);
            return changed;
        }

        public bool HasDiscovered(string recipeId)
        {
            return ContainsOrdinal(discoveredHiddenRecipeIds, recipeId);
        }

        public bool HasAppliedReceipt(string receiptKey)
        {
            return ContainsOrdinal(appliedReceiptKeys, receiptKey);
        }

        public bool AddDiscoveredRecipe(string recipeId)
        {
            EnsureRuntimeDefaults();
            var normalizedId = NormalizeId(recipeId);
            if (string.IsNullOrEmpty(normalizedId) || HasDiscovered(normalizedId))
            {
                return false;
            }

            if (discoveredHiddenRecipeIds.Count >= MaximumDiscoveredRecipeIds)
            {
                return false;
            }

            discoveredHiddenRecipeIds.Add(normalizedId);
            return true;
        }

        public bool AddAppliedReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            var normalizedKey = NormalizeId(receiptKey);
            if (string.IsNullOrEmpty(normalizedKey) || HasAppliedReceipt(normalizedKey))
            {
                return false;
            }

            appliedReceiptKeys.Add(normalizedKey);
            if (appliedReceiptKeys.Count > MaximumReceiptKeys)
            {
                appliedReceiptKeys.RemoveRange(
                    0,
                    appliedReceiptKeys.Count - MaximumReceiptKeys);
            }

            return true;
        }

        private static bool ClampNonNegative(ref int value)
        {
            if (value >= 0)
            {
                return false;
            }

            value = 0;
            return true;
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
                var normalizedValue = NormalizeId(source[index]);
                if (string.IsNullOrEmpty(normalizedValue) || !seen.Add(normalizedValue))
                {
                    changed = true;
                    continue;
                }

                if (normalized.Count >= maximumCount)
                {
                    changed = true;
                    break;
                }

                normalized.Add(normalizedValue);
                changed |= !string.Equals(
                    source[index],
                    normalizedValue,
                    StringComparison.Ordinal);
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

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
