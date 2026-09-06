using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class NpcRelationshipEpisodeSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumCompletedEpisodeIds = 64;
        public const int MaximumKeepsakeIds = 64;
        public const int MaximumReceipts = 64;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> completedEpisodeIds = new List<string>();
        public List<string> keepsakeIds = new List<string>();
        public List<NpcRelationshipEpisodeReceiptSaveData> receipts =
            new List<NpcRelationshipEpisodeReceiptSaveData>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            if (completedEpisodeIds == null)
            {
                completedEpisodeIds = new List<string>();
                changed = true;
            }

            if (keepsakeIds == null)
            {
                keepsakeIds = new List<string>();
                changed = true;
            }

            if (receipts == null)
            {
                receipts = new List<NpcRelationshipEpisodeReceiptSaveData>();
                changed = true;
            }

            changed |= NormalizeTokens(completedEpisodeIds, MaximumCompletedEpisodeIds);
            changed |= NormalizeTokens(keepsakeIds, MaximumKeepsakeIds);
            changed |= NormalizeReceipts();

            // A receipt is the durable proof of an applied choice. Repair a partial legacy save
            // whose completed-id list was absent without discarding forward-version episode IDs.
            for (var index = 0; index < receipts.Count; index += 1)
            {
                var episodeId = receipts[index].episodeId;
                if (!ContainsOrdinal(completedEpisodeIds, episodeId))
                {
                    completedEpisodeIds.Add(episodeId);
                    changed = true;
                }
            }

            while (completedEpisodeIds.Count > MaximumCompletedEpisodeIds)
            {
                completedEpisodeIds.RemoveAt(0);
                changed = true;
            }

            return changed;
        }

        public bool HasCompletedEpisode(string episodeId)
        {
            return ContainsOrdinal(completedEpisodeIds, NormalizeToken(episodeId));
        }

        public bool HasReceipt(string receiptId)
        {
            var normalized = NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalized) || receipts == null)
            {
                return false;
            }

            for (var index = 0; index < receipts.Count; index += 1)
            {
                if (string.Equals(receipts[index]?.receiptId, normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasKeepsake(string keepsakeId)
        {
            return ContainsOrdinal(keepsakeIds, NormalizeToken(keepsakeId));
        }

        internal bool CanRecordCompletion(string episodeId)
        {
            var normalized = NormalizeToken(episodeId);
            return !string.IsNullOrEmpty(normalized)
                && (HasCompletedEpisode(normalized)
                    || (completedEpisodeIds?.Count ?? 0) < MaximumCompletedEpisodeIds);
        }

        internal bool CanAddKeepsake(string keepsakeId)
        {
            var normalized = NormalizeToken(keepsakeId);
            return string.IsNullOrEmpty(normalized)
                || HasKeepsake(normalized)
                || (keepsakeIds?.Count ?? 0) < MaximumKeepsakeIds;
        }

        internal void RecordCompletion(
            string episodeId,
            string keepsakeId,
            NpcRelationshipEpisodeReceiptSaveData receipt)
        {
            var normalizedEpisode = NormalizeToken(episodeId);
            var normalizedKeepsake = NormalizeToken(keepsakeId);
            if (!HasCompletedEpisode(normalizedEpisode))
            {
                completedEpisodeIds.Add(normalizedEpisode);
            }

            if (!string.IsNullOrEmpty(normalizedKeepsake) && !HasKeepsake(normalizedKeepsake))
            {
                keepsakeIds.Add(normalizedKeepsake);
            }

            receipts.Add(receipt);
            while (receipts.Count > MaximumReceipts)
            {
                receipts.RemoveAt(0);
            }
        }

        internal static string NormalizeToken(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private bool NormalizeReceipts()
        {
            var changed = false;
            var knownReceiptIds = new HashSet<string>(StringComparer.Ordinal);
            var knownEpisodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = receipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = receipts[index];
                if (receipt == null)
                {
                    receipts.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= receipt.EnsureRuntimeDefaults();
                if (!receipt.HasValue
                    || !knownReceiptIds.Add(receipt.receiptId)
                    || !knownEpisodeIds.Add(receipt.episodeId))
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

        private static bool NormalizeTokens(List<string> values, int maximumCount)
        {
            var changed = false;
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = values.Count - 1; index >= 0; index -= 1)
            {
                var normalized = NormalizeToken(values[index]);
                if (string.IsNullOrEmpty(normalized) || !known.Add(normalized))
                {
                    values.RemoveAt(index);
                    changed = true;
                    continue;
                }

                if (!string.Equals(values[index], normalized, StringComparison.Ordinal))
                {
                    values[index] = normalized;
                    changed = true;
                }
            }

            while (values.Count > maximumCount)
            {
                values.RemoveAt(0);
                changed = true;
            }

            return changed;
        }

        private static bool ContainsOrdinal(IList<string> values, string expected)
        {
            if (values == null || string.IsNullOrEmpty(expected))
            {
                return false;
            }

            for (var index = 0; index < values.Count; index += 1)
            {
                if (string.Equals(values[index], expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class NpcRelationshipEpisodeReceiptSaveData
    {
        public string receiptId = string.Empty;
        public string episodeId = string.Empty;
        public string npcId = string.Empty;
        public string choiceId = string.Empty;
        public string completedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrWhiteSpace(receiptId)
            && !string.IsNullOrWhiteSpace(episodeId)
            && !string.IsNullOrWhiteSpace(npcId)
            && !string.IsNullOrWhiteSpace(choiceId)
            && !string.IsNullOrWhiteSpace(completedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= Normalize(ref receiptId);
            changed |= Normalize(ref episodeId);
            changed |= Normalize(ref npcId);
            changed |= Normalize(ref choiceId);
            changed |= Normalize(ref completedAtIso);
            if (!string.IsNullOrEmpty(completedAtIso)
                && !DateTimeOffset.TryParse(
                    completedAtIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _))
            {
                completedAtIso = string.Empty;
                changed = true;
            }

            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = NpcRelationshipEpisodeSaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
