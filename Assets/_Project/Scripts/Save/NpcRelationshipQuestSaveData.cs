using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class NpcRelationshipQuestSaveData
    {
        public const int CurrentSchemaVersion = 2;
        public const int MaximumClaimReceipts = 64;

        public int schemaVersion = CurrentSchemaVersion;
        public ActiveNpcRelationshipQuestSaveData activeQuest =
            new ActiveNpcRelationshipQuestSaveData();
        public List<NpcRelationshipQuestClaimReceiptSaveData> claimReceipts =
            new List<NpcRelationshipQuestClaimReceiptSaveData>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            if (activeQuest == null)
            {
                activeQuest = new ActiveNpcRelationshipQuestSaveData();
                changed = true;
            }

            changed |= activeQuest.EnsureRuntimeDefaults();
            if (claimReceipts == null)
            {
                claimReceipts = new List<NpcRelationshipQuestClaimReceiptSaveData>();
                changed = true;
            }

            var knownReceiptIds = new HashSet<string>(StringComparer.Ordinal);
            var knownOfferIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = claimReceipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = claimReceipts[index];
                if (receipt == null)
                {
                    claimReceipts.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= receipt.EnsureRuntimeDefaults();
                if (!receipt.HasValue
                    || !knownReceiptIds.Add(receipt.claimReceiptId)
                    || !knownOfferIds.Add(receipt.offerId))
                {
                    claimReceipts.RemoveAt(index);
                    changed = true;
                }
            }

            while (claimReceipts.Count > MaximumClaimReceipts)
            {
                claimReceipts.RemoveAt(0);
                changed = true;
            }

            if (activeQuest.HasValue && HasClaimedOffer(activeQuest.offerId))
            {
                activeQuest.Clear();
                changed = true;
            }

            return changed;
        }

        public bool HasClaimReceipt(string claimReceiptId)
        {
            var normalized = NormalizeToken(claimReceiptId);
            if (string.IsNullOrEmpty(normalized) || claimReceipts == null)
            {
                return false;
            }

            for (var index = 0; index < claimReceipts.Count; index += 1)
            {
                if (string.Equals(
                    claimReceipts[index]?.claimReceiptId,
                    normalized,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasClaimedOffer(string offerId)
        {
            var normalized = NormalizeToken(offerId);
            if (string.IsNullOrEmpty(normalized) || claimReceipts == null)
            {
                return false;
            }

            for (var index = 0; index < claimReceipts.Count; index += 1)
            {
                if (string.Equals(
                    claimReceipts[index]?.offerId,
                    normalized,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string NormalizeToken(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }

    [Serializable]
    public sealed class ActiveNpcRelationshipQuestSaveData
    {
        public string offerId = string.Empty;
        public string npcId = string.Empty;
        public string questId = string.Empty;
        public string startedAtIso = string.Empty;
        public string expiresAtIso = string.Empty;
        public string graceEndsAtIso = string.Empty;
        public bool terminalExpired;

        public bool HasValue => !string.IsNullOrWhiteSpace(offerId)
            && !string.IsNullOrWhiteSpace(npcId)
            && !string.IsNullOrWhiteSpace(questId);

        public void Set(
            string offer,
            string npc,
            string quest,
            DateTimeOffset startedAt,
            DateTimeOffset expiresAt,
            DateTimeOffset graceEndsAt)
        {
            offerId = NpcRelationshipQuestSaveData.NormalizeToken(offer);
            npcId = NpcRelationshipQuestSaveData.NormalizeToken(npc);
            questId = NpcRelationshipQuestSaveData.NormalizeToken(quest);
            startedAtIso = ToIso(startedAt);
            expiresAtIso = ToIso(expiresAt);
            graceEndsAtIso = ToIso(graceEndsAt);
            terminalExpired = false;
        }

        public void Clear()
        {
            offerId = string.Empty;
            npcId = string.Empty;
            questId = string.Empty;
            startedAtIso = string.Empty;
            expiresAtIso = string.Empty;
            graceEndsAtIso = string.Empty;
            terminalExpired = false;
        }

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= Normalize(ref offerId);
            changed |= Normalize(ref npcId);
            changed |= Normalize(ref questId);
            changed |= Normalize(ref startedAtIso);
            changed |= Normalize(ref expiresAtIso);
            changed |= Normalize(ref graceEndsAtIso);

            if (!HasValue)
            {
                if (!IsEmpty())
                {
                    Clear();
                    return true;
                }

                return changed;
            }

            if (!TryParse(startedAtIso, out var startedAt)
                || !TryParse(expiresAtIso, out var expiresAt)
                || !TryParse(graceEndsAtIso, out var graceEndsAt)
                || expiresAt < startedAt
                || graceEndsAt < expiresAt)
            {
                Clear();
                return true;
            }

            return changed;
        }

        public bool TryGetTimes(
            out DateTimeOffset startedAt,
            out DateTimeOffset expiresAt,
            out DateTimeOffset graceEndsAt)
        {
            var hasStartedAt = TryParse(startedAtIso, out startedAt);
            var hasExpiresAt = TryParse(expiresAtIso, out expiresAt);
            var hasGraceEndsAt = TryParse(graceEndsAtIso, out graceEndsAt);
            return hasStartedAt && hasExpiresAt && hasGraceEndsAt;
        }

        private bool IsEmpty()
        {
            return string.IsNullOrEmpty(offerId)
                && string.IsNullOrEmpty(npcId)
                && string.IsNullOrEmpty(questId)
                && string.IsNullOrEmpty(startedAtIso)
                && string.IsNullOrEmpty(expiresAtIso)
                && string.IsNullOrEmpty(graceEndsAtIso)
                && !terminalExpired;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = NpcRelationshipQuestSaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }

        private static bool TryParse(string value, out DateTimeOffset parsed)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed);
        }

        private static string ToIso(DateTimeOffset value)
        {
            return value.ToString("O", CultureInfo.InvariantCulture);
        }
    }

    [Serializable]
    public sealed class NpcRelationshipQuestClaimReceiptSaveData
    {
        public string claimReceiptId = string.Empty;
        public string offerId = string.Empty;
        public string npcId = string.Empty;
        public string questId = string.Empty;
        public string claimedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrWhiteSpace(claimReceiptId)
            && !string.IsNullOrWhiteSpace(offerId)
            && !string.IsNullOrWhiteSpace(npcId)
            && !string.IsNullOrWhiteSpace(questId)
            && !string.IsNullOrWhiteSpace(claimedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= Normalize(ref claimReceiptId);
            changed |= Normalize(ref offerId);
            changed |= Normalize(ref npcId);
            changed |= Normalize(ref questId);
            changed |= Normalize(ref claimedAtIso);
            if (!string.IsNullOrEmpty(claimedAtIso)
                && !DateTimeOffset.TryParse(
                    claimedAtIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _))
            {
                claimedAtIso = string.Empty;
                changed = true;
            }

            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = NpcRelationshipQuestSaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
