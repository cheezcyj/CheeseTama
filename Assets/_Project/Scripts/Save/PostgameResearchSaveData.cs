using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Research;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class PostgameResearchSaveData
    {
        public const int CurrentSchemaVersion = 2;
        public const int MaximumUnlockedNodeIds = 16;
        public const int MaximumReceiptKeys = 64;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> unlockedNodeIds = new List<string>();
        public List<string> appliedReceiptKeys = new List<string>();
        public string activeProfileId = string.Empty;

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            changed |= NormalizeIds(ref unlockedNodeIds, MaximumUnlockedNodeIds, false);
            changed |= NormalizeIds(ref appliedReceiptKeys, MaximumReceiptKeys, true);
            changed |= NormalizeCatalogState();

            return changed;
        }

        private bool NormalizeCatalogState()
        {
            var changed = false;
            var requested = new HashSet<string>(unlockedNodeIds, StringComparer.Ordinal);
            var normalized = new List<string>(PostgameResearchCatalog.All.Count);
            var accepted = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < PostgameResearchCatalog.All.Count; index += 1)
            {
                var definition = PostgameResearchCatalog.All[index];
                if (!requested.Contains(definition.Id)
                    || (!string.IsNullOrEmpty(definition.PrerequisiteNodeId)
                        && !accepted.Contains(definition.PrerequisiteNodeId)))
                {
                    continue;
                }

                normalized.Add(definition.Id);
                accepted.Add(definition.Id);
            }

            if (!SequenceEqual(unlockedNodeIds, normalized))
            {
                unlockedNodeIds = normalized;
                changed = true;
            }

            if (appliedReceiptKeys.Count > unlockedNodeIds.Count)
            {
                appliedReceiptKeys.RemoveRange(
                    unlockedNodeIds.Count,
                    appliedReceiptKeys.Count - unlockedNodeIds.Count);
                changed = true;
            }

            var requestedProfile = NormalizeId(activeProfileId);
            var expectedProfile = ResolveUnlockedProfile(requestedProfile);
            if (string.IsNullOrEmpty(expectedProfile) && unlockedNodeIds.Count > 0)
            {
                expectedProfile = PostgameResearchCatalog.Find(unlockedNodeIds[unlockedNodeIds.Count - 1])
                    ?.ProfileId ?? string.Empty;
            }
            if (!string.Equals(activeProfileId, expectedProfile, StringComparison.Ordinal))
            {
                activeProfileId = expectedProfile;
                changed = true;
            }

            return changed;
        }

        public bool TrySelectProfile(string profileId)
        {
            EnsureRuntimeDefaults();
            var normalized = NormalizeId(profileId);
            if (string.IsNullOrEmpty(normalized)
                || string.Equals(activeProfileId, normalized, StringComparison.Ordinal)
                || string.IsNullOrEmpty(ResolveUnlockedProfile(normalized)))
            {
                return false;
            }

            activeProfileId = normalized;
            return true;
        }

        public bool IsUnlocked(string nodeId)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(unlockedNodeIds, NormalizeId(nodeId));
        }

        public bool HasAppliedReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            return ContainsOrdinal(appliedReceiptKeys, NormalizeId(receiptKey));
        }

        internal bool CanUnlock(string nodeId, string receiptKey)
        {
            EnsureRuntimeDefaults();
            var normalizedNode = NormalizeId(nodeId);
            var normalizedReceipt = NormalizeId(receiptKey);
            return !string.IsNullOrEmpty(normalizedNode)
                && !string.IsNullOrEmpty(normalizedReceipt)
                && (ContainsOrdinal(unlockedNodeIds, normalizedNode)
                    || unlockedNodeIds.Count < MaximumUnlockedNodeIds)
                && (ContainsOrdinal(appliedReceiptKeys, normalizedReceipt)
                    || appliedReceiptKeys.Count < MaximumReceiptKeys);
        }

        internal bool AddUnlock(string nodeId, string receiptKey, string profileId)
        {
            if (!CanUnlock(nodeId, receiptKey))
            {
                return false;
            }

            var normalizedNode = NormalizeId(nodeId);
            var normalizedReceipt = NormalizeId(receiptKey);
            if (ContainsOrdinal(unlockedNodeIds, normalizedNode)
                || ContainsOrdinal(appliedReceiptKeys, normalizedReceipt))
            {
                return false;
            }

            unlockedNodeIds.Add(normalizedNode);
            appliedReceiptKeys.Add(normalizedReceipt);
            activeProfileId = NormalizeId(profileId);
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
            var start = keepNewest ? Math.Max(0, source.Count - maximumCount) : 0;
            var end = keepNewest ? source.Count : Math.Min(source.Count, maximumCount);
            for (var index = start; index < end; index += 1)
            {
                var id = NormalizeId(source[index]);
                if (!string.IsNullOrEmpty(id) && seen.Add(id))
                {
                    normalized.Add(id);
                }
            }

            if (normalized.Count != source.Count)
            {
                changed = true;
            }
            else
            {
                for (var index = 0; index < normalized.Count; index += 1)
                {
                    if (!string.Equals(normalized[index], source[index], StringComparison.Ordinal))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            values = normalized;
            return changed;
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
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

        private static bool SequenceEqual(
            IReadOnlyList<string> left,
            IReadOnlyList<string> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index += 1)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private string ResolveUnlockedProfile(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return string.Empty;
            }

            for (var index = 0; index < unlockedNodeIds.Count; index += 1)
            {
                var definition = PostgameResearchCatalog.Find(unlockedNodeIds[index]);
                if (definition != null
                    && string.Equals(definition.ProfileId, profileId, StringComparison.Ordinal))
                {
                    return definition.ProfileId;
                }
            }

            return string.Empty;
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
