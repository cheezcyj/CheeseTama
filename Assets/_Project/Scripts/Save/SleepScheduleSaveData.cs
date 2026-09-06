using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class SleepSessionSaveData
    {
        public string receiptKey = string.Empty;
        public string sleepStartedAtIso = string.Empty;
        public string plannedWakeAtIso = string.Empty;
        public int scheduledHours;

        internal bool TryEnsureRuntimeDefaults(out bool changed)
        {
            changed = NormalizeText(ref receiptKey);
            changed |= NormalizeTimestamp(ref sleepStartedAtIso);
            changed |= NormalizeTimestamp(ref plannedWakeAtIso);

            if (string.IsNullOrEmpty(receiptKey)
                || !TryParseTimestamp(sleepStartedAtIso, out var startedAt)
                || !TryParseTimestamp(plannedWakeAtIso, out var plannedWakeAt)
                || plannedWakeAt <= startedAt)
            {
                return false;
            }

            var durationHours = (plannedWakeAt - startedAt).TotalHours;
            var normalizedHours = (int)Math.Round(durationHours);
            if (normalizedHours < SleepScheduleSaveData.MinimumScheduledHours
                || normalizedHours > SleepScheduleSaveData.MaximumScheduledHours
                || Math.Abs(durationHours - normalizedHours) > 0.000001d)
            {
                return false;
            }

            if (scheduledHours != normalizedHours)
            {
                scheduledHours = normalizedHours;
                changed = true;
            }

            return true;
        }

        internal static bool TryParseTimestamp(
            string value,
            out DateTimeOffset parsed)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed);
        }

        internal static string FormatTimestamp(DateTimeOffset value)
        {
            return value
                .ToUniversalTime()
                .ToString("O", CultureInfo.InvariantCulture);
        }

        internal static bool NormalizeText(ref string value)
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

        internal static bool NormalizeTimestamp(ref string value)
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

            var normalized = FormatTimestamp(parsed);
            if (string.Equals(value, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            value = normalized;
            return true;
        }
    }

    [Serializable]
    public sealed class SleepRecoveryReceiptSaveEntry
    {
        public string receiptKey = string.Empty;
        public string sleepStartedAtIso = string.Empty;
        public string plannedWakeAtIso = string.Empty;
        public string wokeAtIso = string.Empty;
        public string claimedAtIso = string.Empty;
        public int scheduledHours;
        public int elapsedMinutes;
        public int sleepinessDelta;
        public int healthDelta;
        public int moodDelta;
        public bool wasEarlyWake;

        public bool EnsureRuntimeDefaults()
        {
            var changed = SleepSessionSaveData.NormalizeText(ref receiptKey);
            changed |= SleepSessionSaveData.NormalizeTimestamp(ref sleepStartedAtIso);
            changed |= SleepSessionSaveData.NormalizeTimestamp(ref plannedWakeAtIso);
            changed |= SleepSessionSaveData.NormalizeTimestamp(ref wokeAtIso);
            changed |= SleepSessionSaveData.NormalizeTimestamp(ref claimedAtIso);

            changed |= Clamp(ref scheduledHours, 0, SleepScheduleSaveData.MaximumScheduledHours);
            changed |= Clamp(
                ref elapsedMinutes,
                0,
                SleepScheduleSaveData.MaximumScheduledHours * 60);
            changed |= Clamp(ref sleepinessDelta, -100, 0);
            changed |= Clamp(ref healthDelta, 0, 100);
            changed |= Clamp(ref moodDelta, 0, 100);
            return changed;
        }

        private static bool Clamp(ref int value, int minimum, int maximum)
        {
            var normalized = Math.Max(minimum, Math.Min(maximum, value));
            if (normalized == value)
            {
                return false;
            }

            value = normalized;
            return true;
        }
    }

    /// <summary>
    /// Standalone persistence contract for a scheduled sleep session. The root
    /// save owner may add this DTO as a nullable field and call either overload
    /// of EnsureRuntimeDefaults during its normal migration/load flow.
    /// </summary>
    [Serializable]
    public sealed class SleepScheduleSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MinimumScheduledHours = 1;
        public const int MaximumScheduledHours = 8;
        public const int MaximumRecoveryReceipts = 128;

        public int schemaVersion = CurrentSchemaVersion;
        public SleepSessionSaveData activeSession;
        public List<SleepRecoveryReceiptSaveEntry> recoveryReceipts =
            new List<SleepRecoveryReceiptSaveEntry>();
        public string lastWakeAtIso = string.Empty;

        public bool HasActiveSession => activeSession != null;

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            if (activeSession != null)
            {
                var valid = activeSession.TryEnsureRuntimeDefaults(
                    out var sessionChanged);
                changed |= sessionChanged;
                if (!valid)
                {
                    activeSession = null;
                    changed = true;
                }
            }

            changed |= NormalizeReceipts(ref recoveryReceipts);
            changed |= SleepSessionSaveData.NormalizeTimestamp(ref lastWakeAtIso);

            if (activeSession != null
                && ContainsReceipt(recoveryReceipts, activeSession.receiptKey))
            {
                // A persisted receipt is authoritative. Clearing a duplicated
                // active session makes crash/reload recovery idempotent.
                activeSession = null;
                changed = true;
            }

            return changed;
        }

        public bool EnsureRuntimeDefaults(DateTimeOffset now)
        {
            var changed = EnsureRuntimeDefaults();
            if (activeSession != null
                && SleepSessionSaveData.TryParseTimestamp(
                    activeSession.sleepStartedAtIso,
                    out var startedAt)
                && startedAt > now)
            {
                // A start time in the caller's future cannot earn recovery.
                // Fail closed by discarding only the invalid active session.
                activeSession = null;
                changed = true;
            }

            if (SleepSessionSaveData.TryParseTimestamp(lastWakeAtIso, out var lastWakeAt)
                && lastWakeAt > now)
            {
                lastWakeAtIso = string.Empty;
                changed = true;
            }

            return changed;
        }

        public bool HasAppliedReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            return ContainsReceipt(recoveryReceipts, receiptKey);
        }

        public SleepRecoveryReceiptSaveEntry FindReceipt(string receiptKey)
        {
            EnsureRuntimeDefaults();
            var normalized = NormalizeKey(receiptKey);
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            for (var index = recoveryReceipts.Count - 1; index >= 0; index -= 1)
            {
                var entry = recoveryReceipts[index];
                if (entry != null
                    && string.Equals(
                        entry.receiptKey,
                        normalized,
                        StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        internal bool TryBeginSession(
            string receiptKey,
            DateTimeOffset startedAt,
            DateTimeOffset plannedWakeAt,
            int scheduledHours)
        {
            EnsureRuntimeDefaults(startedAt);
            var normalizedKey = NormalizeKey(receiptKey);
            if (activeSession != null
                || string.IsNullOrEmpty(normalizedKey)
                || ContainsReceipt(recoveryReceipts, normalizedKey))
            {
                return false;
            }

            activeSession = new SleepSessionSaveData
            {
                receiptKey = normalizedKey,
                sleepStartedAtIso = SleepSessionSaveData.FormatTimestamp(startedAt),
                plannedWakeAtIso = SleepSessionSaveData.FormatTimestamp(plannedWakeAt),
                scheduledHours = scheduledHours
            };
            return true;
        }

        internal bool TryAddRecoveryReceipt(SleepRecoveryReceiptSaveEntry receipt)
        {
            EnsureRuntimeDefaults();
            if (receipt == null)
            {
                return false;
            }

            receipt.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(receipt.receiptKey)
                || ContainsReceipt(recoveryReceipts, receipt.receiptKey))
            {
                return false;
            }

            recoveryReceipts.Add(receipt);
            if (recoveryReceipts.Count > MaximumRecoveryReceipts)
            {
                recoveryReceipts.RemoveRange(
                    0,
                    recoveryReceipts.Count - MaximumRecoveryReceipts);
            }

            return true;
        }

        internal void ClearActiveSession()
        {
            activeSession = null;
        }

        internal void RecordLastWake(DateTimeOffset wokeAt)
        {
            lastWakeAtIso = SleepSessionSaveData.FormatTimestamp(wokeAt);
        }

        private static bool NormalizeReceipts(
            ref List<SleepRecoveryReceiptSaveEntry> values)
        {
            var changed = values == null;
            var source = values ?? new List<SleepRecoveryReceiptSaveEntry>();
            var normalized = new List<SleepRecoveryReceiptSaveEntry>(
                Math.Min(source.Count, MaximumRecoveryReceipts));
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // Iterate newest-first so an idempotency key cannot be displaced by
            // an older duplicate when an oversized or partially corrupt save loads.
            for (var index = source.Count - 1;
                index >= 0 && normalized.Count < MaximumRecoveryReceipts;
                index -= 1)
            {
                var entry = source[index];
                if (entry == null)
                {
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (string.IsNullOrEmpty(entry.receiptKey)
                    || !seen.Add(entry.receiptKey))
                {
                    changed = true;
                    continue;
                }

                normalized.Insert(0, entry);
            }

            changed |= normalized.Count != source.Count;
            values = normalized;
            return changed;
        }

        private static bool ContainsReceipt(
            IReadOnlyList<SleepRecoveryReceiptSaveEntry> receipts,
            string receiptKey)
        {
            var normalized = NormalizeKey(receiptKey);
            if (receipts == null || string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            for (var index = 0; index < receipts.Count; index += 1)
            {
                var entry = receipts[index];
                if (entry != null
                    && string.Equals(
                        entry.receiptKey,
                        normalized,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
