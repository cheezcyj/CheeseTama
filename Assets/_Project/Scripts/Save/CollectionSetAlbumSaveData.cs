using System;
using System.Collections.Generic;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class CollectionSetAlbumSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumRevealedSetIds = 16;
        public const int MaximumClaimedSetIds = 16;
        public const int MaximumClaimReceiptKeys = 128;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> revealedHiddenSetIds = new List<string>();
        public List<string> claimedSetIds = new List<string>();
        public List<string> appliedClaimReceiptKeys = new List<string>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            if (schemaVersion < CurrentSchemaVersion)
            {
                schemaVersion = CurrentSchemaVersion;
                changed = true;
            }

            changed |= NormalizeIds(
                ref revealedHiddenSetIds,
                MaximumRevealedSetIds,
                keepNewest: false);
            changed |= NormalizeIds(
                ref claimedSetIds,
                MaximumClaimedSetIds,
                keepNewest: false);
            changed |= NormalizeIds(
                ref appliedClaimReceiptKeys,
                MaximumClaimReceiptKeys,
                keepNewest: true);
            return changed;
        }

        public bool IsHiddenSetRevealed(string setId)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(revealedHiddenSetIds, setId);
        }

        public bool IsRewardClaimed(string setId)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(claimedSetIds, setId);
        }

        public bool HasAppliedClaimReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(appliedClaimReceiptKeys, receiptKey);
        }

        internal bool CanReveal(string setId)
        {
            EnsureRuntimeDefaults();
            var normalizedId = NormalizeId(setId);
            return !string.IsNullOrEmpty(normalizedId)
                && (ContainsOrdinal(revealedHiddenSetIds, normalizedId)
                    || revealedHiddenSetIds.Count < MaximumRevealedSetIds);
        }

        internal bool CanClaim(string setId, string receiptKey)
        {
            EnsureRuntimeDefaults();
            var normalizedSetId = NormalizeId(setId);
            var normalizedReceipt = NormalizeId(receiptKey);
            var hasSetCapacity = ContainsOrdinal(claimedSetIds, normalizedSetId)
                || claimedSetIds.Count < MaximumClaimedSetIds;
            var hasReceiptCapacity = ContainsOrdinal(
                    appliedClaimReceiptKeys,
                    normalizedReceipt)
                || appliedClaimReceiptKeys.Count < MaximumClaimReceiptKeys;
            return !string.IsNullOrEmpty(normalizedSetId)
                && !string.IsNullOrEmpty(normalizedReceipt)
                && hasSetCapacity
                && hasReceiptCapacity;
        }

        internal bool AddRevealedHiddenSet(string setId)
        {
            if (!CanReveal(setId))
            {
                return false;
            }

            var normalizedId = NormalizeId(setId);
            if (ContainsOrdinal(revealedHiddenSetIds, normalizedId))
            {
                return false;
            }

            revealedHiddenSetIds.Add(normalizedId);
            return true;
        }

        internal bool AddClaim(string setId, string receiptKey)
        {
            if (!CanClaim(setId, receiptKey))
            {
                return false;
            }

            var normalizedSetId = NormalizeId(setId);
            var normalizedReceipt = NormalizeId(receiptKey);
            if (ContainsOrdinal(claimedSetIds, normalizedSetId)
                || ContainsOrdinal(appliedClaimReceiptKeys, normalizedReceipt))
            {
                return false;
            }

            claimedSetIds.Add(normalizedSetId);
            appliedClaimReceiptKeys.Add(normalizedReceipt);
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
                var normalizedId = NormalizeId(source[index]);
                if (string.IsNullOrEmpty(normalizedId) || !seen.Add(normalizedId))
                {
                    changed = true;
                    continue;
                }

                if (normalized.Count >= maximumCount)
                {
                    changed = true;
                    break;
                }

                normalized.Add(normalizedId);
                changed |= !string.Equals(
                    source[index],
                    normalizedId,
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
