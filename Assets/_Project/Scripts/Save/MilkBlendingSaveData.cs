using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class MilkBlendUsageSaveEntry
    {
        public string ingredientId = string.Empty;
        public string resultSnackId = string.Empty;
        public int blendCount;
        public string firstBlendedAtIso = string.Empty;
        public string lastBlendedAtIso = string.Empty;

        public bool EnsureRuntimeDefaults()
        {
            var changed = NormalizeId(ref ingredientId);
            changed |= NormalizeId(ref resultSnackId);

            var normalizedCount = Math.Max(0, blendCount);
            if (normalizedCount != blendCount)
            {
                blendCount = normalizedCount;
                changed = true;
            }

            changed |= NormalizeTimestamp(ref firstBlendedAtIso);
            changed |= NormalizeTimestamp(ref lastBlendedAtIso);
            if (string.IsNullOrEmpty(firstBlendedAtIso)
                && !string.IsNullOrEmpty(lastBlendedAtIso))
            {
                firstBlendedAtIso = lastBlendedAtIso;
                changed = true;
            }
            else if (!string.IsNullOrEmpty(firstBlendedAtIso)
                && string.IsNullOrEmpty(lastBlendedAtIso))
            {
                lastBlendedAtIso = firstBlendedAtIso;
                changed = true;
            }

            if (TryParseTimestamp(firstBlendedAtIso, out var first)
                && TryParseTimestamp(lastBlendedAtIso, out var last)
                && last < first)
            {
                lastBlendedAtIso = firstBlendedAtIso;
                changed = true;
            }

            return changed;
        }

        internal static bool TryParseTimestamp(string value, out DateTimeOffset parsed)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed);
        }

        private static bool NormalizeId(ref string value)
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
            if (string.Equals(value, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            value = normalized;
            return true;
        }

        private static bool NormalizeTimestamp(ref string value)
        {
            if (!TryParseTimestamp(value, out var parsed))
            {
                if (value == string.Empty)
                {
                    return false;
                }

                value = string.Empty;
                return true;
            }

            var normalized = parsed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            if (string.Equals(value, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            value = normalized;
            return true;
        }
    }

    [Serializable]
    public sealed class MilkBlendingSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumUsageEntries = 64;
        public const int MaximumDiscoveredResultIds = 64;
        public const int MaximumReceiptKeys = 128;

        public int schemaVersion = CurrentSchemaVersion;
        public List<MilkBlendUsageSaveEntry> ingredientUsage =
            new List<MilkBlendUsageSaveEntry>();
        public List<string> discoveredResultIds = new List<string>();
        public List<string> appliedReceiptKeys = new List<string>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            changed |= NormalizeUsageEntries(ref ingredientUsage);
            changed |= NormalizeIds(
                ref discoveredResultIds,
                MaximumDiscoveredResultIds,
                keepNewest: false);
            for (var index = 0; index < ingredientUsage.Count; index += 1)
            {
                var resultSnackId = ingredientUsage[index]?.resultSnackId;
                if (string.IsNullOrEmpty(resultSnackId)
                    || ContainsOrdinal(discoveredResultIds, resultSnackId))
                {
                    continue;
                }

                if (discoveredResultIds.Count >= MaximumDiscoveredResultIds)
                {
                    break;
                }

                discoveredResultIds.Add(resultSnackId);
                changed = true;
            }

            changed |= NormalizeIds(
                ref appliedReceiptKeys,
                MaximumReceiptKeys,
                keepNewest: true);
            return changed;
        }

        public bool HasDiscovered(string resultSnackId)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(discoveredResultIds, resultSnackId);
        }

        public bool HasAppliedReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(appliedReceiptKeys, receiptKey);
        }

        public int GetBlendCount(string ingredientId, string resultSnackId)
        {
            EnsureRuntimeDefaults();
            var entry = FindUsage(ingredientId, resultSnackId);
            return Math.Max(0, entry?.blendCount ?? 0);
        }

        public int GetIngredientBlendCount(string ingredientId)
        {
            EnsureRuntimeDefaults();
            var normalizedIngredientId = NormalizeId(ingredientId);
            if (string.IsNullOrEmpty(normalizedIngredientId))
            {
                return 0;
            }

            var total = 0;
            for (var index = 0; index < ingredientUsage.Count; index += 1)
            {
                var entry = ingredientUsage[index];
                if (entry == null
                    || !string.Equals(
                        entry.ingredientId,
                        normalizedIngredientId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                total = SaturatingAdd(total, entry.blendCount);
            }

            return total;
        }

        public bool CanRecordBlend(string ingredientId, string resultSnackId)
        {
            EnsureRuntimeDefaults();
            var normalizedIngredientId = NormalizeId(ingredientId);
            var normalizedResultId = NormalizeId(resultSnackId);
            if (string.IsNullOrEmpty(normalizedIngredientId)
                || string.IsNullOrEmpty(normalizedResultId))
            {
                return false;
            }

            var existing = FindUsage(normalizedIngredientId, normalizedResultId);
            if (existing == null && ingredientUsage.Count >= MaximumUsageEntries)
            {
                return false;
            }

            if (existing != null && existing.blendCount >= int.MaxValue)
            {
                return false;
            }

            return HasDiscovered(normalizedResultId)
                || discoveredResultIds.Count < MaximumDiscoveredResultIds;
        }

        public MilkBlendUsageSaveEntry RecordBlend(
            string ingredientId,
            string resultSnackId,
            DateTimeOffset blendedAt)
        {
            if (!CanRecordBlend(ingredientId, resultSnackId))
            {
                return null;
            }

            var normalizedIngredientId = NormalizeId(ingredientId);
            var normalizedResultId = NormalizeId(resultSnackId);
            var timestamp = blendedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            var entry = FindUsage(normalizedIngredientId, normalizedResultId);
            if (entry == null)
            {
                entry = new MilkBlendUsageSaveEntry
                {
                    ingredientId = normalizedIngredientId,
                    resultSnackId = normalizedResultId,
                    blendCount = 1,
                    firstBlendedAtIso = timestamp,
                    lastBlendedAtIso = timestamp
                };
                ingredientUsage.Add(entry);
            }
            else
            {
                entry.blendCount = SaturatingAdd(entry.blendCount, 1);
                if (string.IsNullOrEmpty(entry.firstBlendedAtIso))
                {
                    entry.firstBlendedAtIso = timestamp;
                }

                entry.lastBlendedAtIso = timestamp;
            }

            AddDiscoveredResult(normalizedResultId);
            return entry;
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

        private bool AddDiscoveredResult(string resultSnackId)
        {
            var normalizedId = NormalizeId(resultSnackId);
            if (string.IsNullOrEmpty(normalizedId) || HasDiscovered(normalizedId))
            {
                return false;
            }

            if (discoveredResultIds.Count >= MaximumDiscoveredResultIds)
            {
                return false;
            }

            discoveredResultIds.Add(normalizedId);
            return true;
        }

        private MilkBlendUsageSaveEntry FindUsage(string ingredientId, string resultSnackId)
        {
            var normalizedIngredientId = NormalizeId(ingredientId);
            var normalizedResultId = NormalizeId(resultSnackId);
            if (string.IsNullOrEmpty(normalizedIngredientId)
                || string.IsNullOrEmpty(normalizedResultId)
                || ingredientUsage == null)
            {
                return null;
            }

            for (var index = 0; index < ingredientUsage.Count; index += 1)
            {
                var entry = ingredientUsage[index];
                if (entry != null
                    && string.Equals(
                        entry.ingredientId,
                        normalizedIngredientId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        entry.resultSnackId,
                        normalizedResultId,
                        StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool NormalizeUsageEntries(
            ref List<MilkBlendUsageSaveEntry> values)
        {
            var changed = values == null;
            var source = values ?? new List<MilkBlendUsageSaveEntry>();
            var normalized = new List<MilkBlendUsageSaveEntry>(
                Math.Min(source.Count, MaximumUsageEntries));
            var byKey = new Dictionary<string, MilkBlendUsageSaveEntry>(StringComparer.Ordinal);

            for (var index = 0; index < source.Count; index += 1)
            {
                var entry = source[index];
                if (entry == null)
                {
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (string.IsNullOrEmpty(entry.ingredientId)
                    || string.IsNullOrEmpty(entry.resultSnackId)
                    || entry.blendCount <= 0)
                {
                    changed = true;
                    continue;
                }

                var key = BuildUsageKey(entry.ingredientId, entry.resultSnackId);
                if (byKey.TryGetValue(key, out var existing))
                {
                    existing.blendCount = SaturatingAdd(existing.blendCount, entry.blendCount);
                    existing.firstBlendedAtIso = PickEarlier(
                        existing.firstBlendedAtIso,
                        entry.firstBlendedAtIso);
                    existing.lastBlendedAtIso = PickLater(
                        existing.lastBlendedAtIso,
                        entry.lastBlendedAtIso);
                    changed = true;
                    continue;
                }

                if (normalized.Count >= MaximumUsageEntries)
                {
                    changed = true;
                    continue;
                }

                byKey.Add(key, entry);
                normalized.Add(entry);
            }

            changed |= normalized.Count != source.Count;
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

        private static string BuildUsageKey(string ingredientId, string resultSnackId)
        {
            return ingredientId + "\n" + resultSnackId;
        }

        private static string PickEarlier(string first, string second)
        {
            if (!MilkBlendUsageSaveEntry.TryParseTimestamp(first, out var firstValue))
            {
                return second ?? string.Empty;
            }

            if (!MilkBlendUsageSaveEntry.TryParseTimestamp(second, out var secondValue))
            {
                return first;
            }

            return firstValue <= secondValue ? first : second;
        }

        private static string PickLater(string first, string second)
        {
            if (!MilkBlendUsageSaveEntry.TryParseTimestamp(first, out var firstValue))
            {
                return second ?? string.Empty;
            }

            if (!MilkBlendUsageSaveEntry.TryParseTimestamp(second, out var secondValue))
            {
                return first;
            }

            return firstValue >= secondValue ? first : second;
        }

        private static int SaturatingAdd(int current, int amount)
        {
            var result = (long)Math.Max(0, current) + Math.Max(0, amount);
            return result > int.MaxValue ? int.MaxValue : (int)result;
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
