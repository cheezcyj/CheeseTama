using System;
using System.Collections.Generic;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class StarLegacySaveData
    {
        public const int CurrentSchemaVersion = 2;
        public const int MaximumSignalCount = 9999;
        public const int MaximumEvolutionReceiptKeys = 64;

        public int schemaVersion = CurrentSchemaVersion;
        public bool starRoutePermanentlyUnlocked;
        public int starEggGenerationCount;
        public string currentGenerationTamaId = string.Empty;
        public string currentGenerationStartedAtIso = string.Empty;
        public int starMilkCareCount;
        public int fantasyResonance;
        public bool emmentalEvolutionUnlocked;
        public string emmentalEvolutionAtIso = string.Empty;
        public List<string> appliedEvolutionReceiptKeys = new List<string>();
        public FinalMaturationCycleSaveData maturationCycle = new FinalMaturationCycleSaveData();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            changed |= StarLegacySaveNormalization.Clamp(
                ref starEggGenerationCount,
                0,
                int.MaxValue);
            changed |= StarLegacySaveNormalization.EnsureString(ref currentGenerationTamaId);
            changed |= StarLegacySaveNormalization.EnsureString(ref currentGenerationStartedAtIso);
            if (starEggGenerationCount > 0 && !starRoutePermanentlyUnlocked)
            {
                starRoutePermanentlyUnlocked = true;
                changed = true;
            }

            changed |= StarLegacySaveNormalization.Clamp(
                ref starMilkCareCount,
                0,
                MaximumSignalCount);
            changed |= StarLegacySaveNormalization.Clamp(
                ref fantasyResonance,
                0,
                MaximumSignalCount);
            changed |= StarLegacySaveNormalization.EnsureString(ref emmentalEvolutionAtIso);
            changed |= StarLegacySaveNormalization.NormalizeIds(
                ref appliedEvolutionReceiptKeys,
                MaximumEvolutionReceiptKeys,
                keepNewest: true);

            if (!emmentalEvolutionUnlocked && !string.IsNullOrEmpty(emmentalEvolutionAtIso))
            {
                emmentalEvolutionAtIso = string.Empty;
                changed = true;
            }

            if (maturationCycle == null)
            {
                maturationCycle = new FinalMaturationCycleSaveData();
                changed = true;
            }

            changed |= maturationCycle.EnsureRuntimeDefaults();
            return changed;
        }

        public bool HasAppliedEvolutionReceipt(string receiptKey)
        {
            return StarLegacySaveNormalization.ContainsOrdinal(
                appliedEvolutionReceiptKeys,
                receiptKey);
        }

        public bool AddAppliedEvolutionReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            return StarLegacySaveNormalization.AddBoundedId(
                appliedEvolutionReceiptKeys,
                receiptKey,
                MaximumEvolutionReceiptKeys,
                keepNewest: true);
        }
    }

    [Serializable]
    public sealed class FinalMaturationCycleSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumPendingRewards = 64;
        public const int MaximumReceiptKeys = 128;

        public int schemaVersion = CurrentSchemaVersion;
        public int progress;
        public int completedCycles;
        public int claimedCycles;
        public List<FinalMaturationRewardSaveEntry> pendingRewards =
            new List<FinalMaturationRewardSaveEntry>();
        public List<string> appliedProgressReceiptKeys = new List<string>();
        public List<string> appliedClaimReceiptKeys = new List<string>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            changed |= StarLegacySaveNormalization.Clamp(ref progress, 0, 99);
            changed |= StarLegacySaveNormalization.Clamp(ref completedCycles, 0, int.MaxValue);
            changed |= StarLegacySaveNormalization.Clamp(ref claimedCycles, 0, completedCycles);
            changed |= NormalizePendingRewards();
            changed |= StarLegacySaveNormalization.NormalizeIds(
                ref appliedProgressReceiptKeys,
                MaximumReceiptKeys,
                keepNewest: true);
            changed |= StarLegacySaveNormalization.NormalizeIds(
                ref appliedClaimReceiptKeys,
                MaximumReceiptKeys,
                keepNewest: true);
            return changed;
        }

        public bool HasAppliedProgressReceipt(string receiptKey)
        {
            return StarLegacySaveNormalization.ContainsOrdinal(
                appliedProgressReceiptKeys,
                receiptKey);
        }

        public bool AddAppliedProgressReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            return StarLegacySaveNormalization.AddBoundedId(
                appliedProgressReceiptKeys,
                receiptKey,
                MaximumReceiptKeys,
                keepNewest: true);
        }

        public bool HasAppliedClaimReceipt(string receiptKey)
        {
            return StarLegacySaveNormalization.ContainsOrdinal(
                appliedClaimReceiptKeys,
                receiptKey);
        }

        public bool AddAppliedClaimReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            return StarLegacySaveNormalization.AddBoundedId(
                appliedClaimReceiptKeys,
                receiptKey,
                MaximumReceiptKeys,
                keepNewest: true);
        }

        private bool NormalizePendingRewards()
        {
            var changed = pendingRewards == null;
            var source = pendingRewards ?? new List<FinalMaturationRewardSaveEntry>();
            var normalized = new List<FinalMaturationRewardSaveEntry>(
                Math.Min(source.Count, MaximumPendingRewards));
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var seenCycles = new HashSet<int>();

            for (var index = 0; index < source.Count; index += 1)
            {
                var entry = source[index];
                if (entry == null)
                {
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (entry.cycleNumber <= claimedCycles
                    || string.IsNullOrEmpty(entry.rewardId)
                    || !seenIds.Add(entry.rewardId)
                    || !seenCycles.Add(entry.cycleNumber))
                {
                    changed = true;
                    continue;
                }

                if (normalized.Count >= MaximumPendingRewards)
                {
                    changed = true;
                    break;
                }

                normalized.Add(entry);
                if (entry.cycleNumber > completedCycles)
                {
                    completedCycles = entry.cycleNumber;
                    changed = true;
                }
            }

            normalized.Sort((left, right) => left.cycleNumber.CompareTo(right.cycleNumber));
            if (!changed && normalized.Count == source.Count)
            {
                for (var index = 0; index < normalized.Count; index += 1)
                {
                    if (!ReferenceEquals(normalized[index], source[index]))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            changed |= normalized.Count != source.Count;
            pendingRewards = normalized;
            return changed;
        }
    }

    [Serializable]
    public sealed class FinalMaturationRewardSaveEntry
    {
        public string rewardId = string.Empty;
        public int cycleNumber;
        public int milkCoins;
        public int milkDrops;
        public int starDrops;
        public int fantasyPowder;

        public bool EnsureRuntimeDefaults()
        {
            var changed = StarLegacySaveNormalization.Clamp(
                ref cycleNumber,
                0,
                int.MaxValue);
            changed |= StarLegacySaveNormalization.EnsureString(ref rewardId);
            if (string.IsNullOrEmpty(rewardId) && cycleNumber > 0)
            {
                rewardId = $"final_maturation_{cycleNumber:D8}";
                changed = true;
            }

            changed |= StarLegacySaveNormalization.Clamp(ref milkCoins, 0, int.MaxValue);
            changed |= StarLegacySaveNormalization.Clamp(ref milkDrops, 0, int.MaxValue);
            changed |= StarLegacySaveNormalization.Clamp(ref starDrops, 0, int.MaxValue);
            changed |= StarLegacySaveNormalization.Clamp(ref fantasyPowder, 0, int.MaxValue);
            return changed;
        }
    }

    internal static class StarLegacySaveNormalization
    {
        public static bool Clamp(ref int value, int minimum, int maximum)
        {
            var normalized = Math.Max(minimum, Math.Min(maximum, value));
            if (normalized == value)
            {
                return false;
            }

            value = normalized;
            return true;
        }

        public static bool EnsureString(ref string value)
        {
            if (value != null)
            {
                return false;
            }

            value = string.Empty;
            return true;
        }

        public static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public static bool ContainsOrdinal(IReadOnlyList<string> values, string requested)
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

        public static bool AddBoundedId(
            List<string> values,
            string requested,
            int maximumCount,
            bool keepNewest)
        {
            var normalized = NormalizeId(requested);
            if (values == null
                || string.IsNullOrEmpty(normalized)
                || ContainsOrdinal(values, normalized))
            {
                return false;
            }

            values.Add(normalized);
            if (values.Count > maximumCount)
            {
                if (keepNewest)
                {
                    values.RemoveRange(0, values.Count - maximumCount);
                }
                else
                {
                    values.RemoveRange(maximumCount, values.Count - maximumCount);
                }
            }

            return true;
        }

        public static bool NormalizeIds(
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
    }
}
