using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class WeeklyCareJourneySaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumEventReceipts = 256;
        public const int MaximumRewardReceipts = 104;

        public int schemaVersion = CurrentSchemaVersion;
        public string weekKey = string.Empty;
        public List<WeeklyCareObjectiveProgressSaveData> objectives =
            new List<WeeklyCareObjectiveProgressSaveData>();
        public List<WeeklyCareEventReceiptSaveData> eventReceipts =
            new List<WeeklyCareEventReceiptSaveData>();
        public List<WeeklyCareRewardReceiptSaveData> rewardReceipts =
            new List<WeeklyCareRewardReceiptSaveData>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            changed |= Normalize(ref weekKey);

            if (objectives == null)
            {
                objectives = new List<WeeklyCareObjectiveProgressSaveData>();
                changed = true;
            }

            if (eventReceipts == null)
            {
                eventReceipts = new List<WeeklyCareEventReceiptSaveData>();
                changed = true;
            }

            if (rewardReceipts == null)
            {
                rewardReceipts = new List<WeeklyCareRewardReceiptSaveData>();
                changed = true;
            }

            changed |= NormalizeObjectives();
            changed |= NormalizeEventReceipts();
            changed |= NormalizeRewardReceipts();
            return changed;
        }

        public bool HasEventReceipt(string receiptId)
        {
            var normalized = NormalizeToken(receiptId);
            for (var index = 0; index < (eventReceipts?.Count ?? 0); index += 1)
            {
                if (string.Equals(
                    eventReceipts[index]?.receiptId,
                    normalized,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasRewardReceiptForWeek(string requestedWeekKey)
        {
            var normalized = NormalizeToken(requestedWeekKey);
            for (var index = 0; index < (rewardReceipts?.Count ?? 0); index += 1)
            {
                if (string.Equals(
                    rewardReceipts[index]?.weekKey,
                    normalized,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasRewardClaimReceipt(string claimReceiptId)
        {
            var normalized = NormalizeToken(claimReceiptId);
            for (var index = 0; index < (rewardReceipts?.Count ?? 0); index += 1)
            {
                if (string.Equals(
                    rewardReceipts[index]?.claimReceiptId,
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

        private bool NormalizeObjectives()
        {
            var changed = false;
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = objectives.Count - 1; index >= 0; index -= 1)
            {
                var entry = objectives[index];
                if (entry == null)
                {
                    objectives.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (!entry.HasValue || !known.Add(entry.objectiveId))
                {
                    objectives.RemoveAt(index);
                    changed = true;
                }
            }

            return changed;
        }

        private bool NormalizeEventReceipts()
        {
            var changed = false;
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = eventReceipts.Count - 1; index >= 0; index -= 1)
            {
                var entry = eventReceipts[index];
                if (entry == null)
                {
                    eventReceipts.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (!entry.HasValue || !known.Add(entry.receiptId))
                {
                    eventReceipts.RemoveAt(index);
                    changed = true;
                }
            }

            while (eventReceipts.Count > MaximumEventReceipts)
            {
                eventReceipts.RemoveAt(0);
                changed = true;
            }

            return changed;
        }

        private bool NormalizeRewardReceipts()
        {
            var changed = false;
            var knownClaims = new HashSet<string>(StringComparer.Ordinal);
            var knownWeeks = new HashSet<string>(StringComparer.Ordinal);
            for (var index = rewardReceipts.Count - 1; index >= 0; index -= 1)
            {
                var entry = rewardReceipts[index];
                if (entry == null)
                {
                    rewardReceipts.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (!entry.HasValue
                    || !knownClaims.Add(entry.claimReceiptId)
                    || !knownWeeks.Add(entry.weekKey))
                {
                    rewardReceipts.RemoveAt(index);
                    changed = true;
                }
            }

            while (rewardReceipts.Count > MaximumRewardReceipts)
            {
                rewardReceipts.RemoveAt(0);
                changed = true;
            }

            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }

    [Serializable]
    public sealed class WeeklyCareObjectiveProgressSaveData
    {
        public string objectiveId = string.Empty;
        public int progress;

        public bool HasValue => !string.IsNullOrWhiteSpace(objectiveId);

        public bool EnsureRuntimeDefaults()
        {
            var normalizedId = WeeklyCareJourneySaveData.NormalizeToken(objectiveId);
            var safeProgress = Math.Max(0, progress);
            var changed = !string.Equals(normalizedId, objectiveId, StringComparison.Ordinal)
                || safeProgress != progress;
            objectiveId = normalizedId;
            progress = safeProgress;
            return changed;
        }
    }

    [Serializable]
    public sealed class WeeklyCareEventReceiptSaveData
    {
        public string receiptId = string.Empty;
        public string eventId = string.Empty;
        public string weekKey = string.Empty;
        public string recordedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrWhiteSpace(receiptId)
            && !string.IsNullOrWhiteSpace(eventId)
            && !string.IsNullOrWhiteSpace(weekKey)
            && IsIso(recordedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= Normalize(ref receiptId);
            changed |= Normalize(ref eventId);
            changed |= Normalize(ref weekKey);
            changed |= Normalize(ref recordedAtIso);
            if (!string.IsNullOrEmpty(recordedAtIso) && !IsIso(recordedAtIso))
            {
                recordedAtIso = string.Empty;
                changed = true;
            }

            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = WeeklyCareJourneySaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }

        private static bool IsIso(string value)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out _);
        }
    }

    [Serializable]
    public sealed class WeeklyCareRewardReceiptSaveData
    {
        public string claimReceiptId = string.Empty;
        public string weekKey = string.Empty;
        public string claimedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrWhiteSpace(claimReceiptId)
            && !string.IsNullOrWhiteSpace(weekKey)
            && DateTimeOffset.TryParse(
                claimedAtIso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out _);

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= Normalize(ref claimReceiptId);
            changed |= Normalize(ref weekKey);
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
            var normalized = WeeklyCareJourneySaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
