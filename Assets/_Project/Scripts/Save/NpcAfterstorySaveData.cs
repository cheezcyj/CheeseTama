using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class NpcAfterstorySaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumAfterstoryCompletions = 16;
        public const int MaximumWeeklyProgressEntries = 24;
        public const int MaximumWeeklyReceipts = 160;
        public const int MaximumFutureObservationDays = 7;

        public int schemaVersion = CurrentSchemaVersion;
        public List<NpcAfterstoryCompletionSaveData> completions =
            new List<NpcAfterstoryCompletionSaveData>();
        public List<NpcWeeklyQuestProgressSaveData> weeklyProgress =
            new List<NpcWeeklyQuestProgressSaveData>();
        public List<NpcWeeklyQuestReceiptSaveData> weeklyReceipts =
            new List<NpcWeeklyQuestReceiptSaveData>();
        public string displayedKeepsakeId = string.Empty;
        public long latestObservedUtcTicks;
        public string latestObservedWeekKey = string.Empty;

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            if (completions == null)
            {
                completions = new List<NpcAfterstoryCompletionSaveData>();
                changed = true;
            }

            if (weeklyProgress == null)
            {
                weeklyProgress = new List<NpcWeeklyQuestProgressSaveData>();
                changed = true;
            }

            if (weeklyReceipts == null)
            {
                weeklyReceipts = new List<NpcWeeklyQuestReceiptSaveData>();
                changed = true;
            }

            changed |= Normalize(ref displayedKeepsakeId);
            changed |= Normalize(ref latestObservedWeekKey);
            var now = DateTimeOffset.UtcNow;
            if (!IsReasonableObservedUtcTicks(latestObservedUtcTicks, now))
            {
                latestObservedUtcTicks = 0L;
                changed = true;
            }

            if (!string.IsNullOrEmpty(latestObservedWeekKey)
                && !IsReasonableObservedWeekKey(latestObservedWeekKey, now))
            {
                latestObservedWeekKey = string.Empty;
                changed = true;
            }

            changed |= NormalizeCompletions();
            changed |= NormalizeWeeklyProgress();
            changed |= NormalizeWeeklyReceipts();
            return changed;
        }

        public static bool IsReasonableObservedUtcTicks(long ticks, DateTimeOffset now)
        {
            if (ticks == 0L)
            {
                return true;
            }

            if (ticks < 0L)
            {
                return false;
            }

            var maximum = now.ToUniversalTime().AddDays(MaximumFutureObservationDays)
                .UtcDateTime.Ticks;
            return ticks <= maximum;
        }

        public static bool IsReasonableObservedWeekKey(string weekKey, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(weekKey))
            {
                return true;
            }

            return NpcAfterstorySaveNormalization.TryParseDateKey(weekKey, out var parsed)
                && parsed.Date <= now.UtcDateTime.Date.AddDays(MaximumFutureObservationDays + 7);
        }

        public bool HasCompletedAfterstory(string afterstoryId)
        {
            return FindCompletion(afterstoryId) != null;
        }

        public NpcAfterstoryCompletionSaveData FindCompletion(string afterstoryId)
        {
            var normalized = NpcAfterstorySaveNormalization.NormalizeToken(afterstoryId);
            if (string.IsNullOrEmpty(normalized) || completions == null)
            {
                return null;
            }

            for (var index = 0; index < completions.Count; index += 1)
            {
                var completion = completions[index];
                if (string.Equals(completion?.afterstoryId, normalized, StringComparison.Ordinal))
                {
                    return completion;
                }
            }

            return null;
        }

        public bool HasAfterstoryReceipt(string receiptId)
        {
            var normalized = NpcAfterstorySaveNormalization.NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalized) || completions == null)
            {
                return false;
            }

            for (var index = 0; index < completions.Count; index += 1)
            {
                if (string.Equals(completions[index]?.receiptId, normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasKeepsake(string keepsakeId)
        {
            var normalized = NpcAfterstorySaveNormalization.NormalizeToken(keepsakeId);
            if (string.IsNullOrEmpty(normalized) || completions == null)
            {
                return false;
            }

            for (var index = 0; index < completions.Count; index += 1)
            {
                if (string.Equals(completions[index]?.keepsakeId, normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasWeeklyReceipt(string receiptId)
        {
            var normalized = NpcAfterstorySaveNormalization.NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalized) || weeklyReceipts == null)
            {
                return false;
            }

            for (var index = 0; index < weeklyReceipts.Count; index += 1)
            {
                if (string.Equals(weeklyReceipts[index]?.receiptId, normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasClaimedWeeklyQuest(string npcId, string weekKey)
        {
            var normalizedNpc = NpcAfterstorySaveNormalization.NormalizeToken(npcId);
            var normalizedWeek = NpcAfterstorySaveNormalization.NormalizeToken(weekKey);
            if (string.IsNullOrEmpty(normalizedNpc)
                || string.IsNullOrEmpty(normalizedWeek)
                || weeklyReceipts == null)
            {
                return false;
            }

            for (var index = 0; index < weeklyReceipts.Count; index += 1)
            {
                var receipt = weeklyReceipts[index];
                if (receipt != null
                    && string.Equals(receipt.npcId, normalizedNpc, StringComparison.Ordinal)
                    && string.Equals(receipt.weekKey, normalizedWeek, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool CanRecordAfterstory()
        {
            return completions != null && completions.Count < MaximumAfterstoryCompletions;
        }

        internal bool CanRecordWeeklyReceipt()
        {
            return weeklyReceipts != null && weeklyReceipts.Count < MaximumWeeklyReceipts;
        }

        internal NpcWeeklyQuestProgressSaveData FindWeeklyProgress(string npcId, string weekKey)
        {
            var normalizedNpc = NpcAfterstorySaveNormalization.NormalizeToken(npcId);
            var normalizedWeek = NpcAfterstorySaveNormalization.NormalizeToken(weekKey);
            if (weeklyProgress == null)
            {
                return null;
            }

            for (var index = 0; index < weeklyProgress.Count; index += 1)
            {
                var entry = weeklyProgress[index];
                if (entry != null
                    && string.Equals(entry.npcId, normalizedNpc, StringComparison.Ordinal)
                    && string.Equals(entry.weekKey, normalizedWeek, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        internal NpcWeeklyQuestProgressSaveData GetOrCreateWeeklyProgress(
            string npcId,
            string questId,
            string weekKey)
        {
            var existing = FindWeeklyProgress(npcId, weekKey);
            if (existing != null)
            {
                return existing;
            }

            var created = new NpcWeeklyQuestProgressSaveData
            {
                npcId = NpcAfterstorySaveNormalization.NormalizeToken(npcId),
                questId = NpcAfterstorySaveNormalization.NormalizeToken(questId),
                weekKey = NpcAfterstorySaveNormalization.NormalizeToken(weekKey)
            };
            weeklyProgress.Add(created);
            while (weeklyProgress.Count > MaximumWeeklyProgressEntries)
            {
                weeklyProgress.RemoveAt(0);
            }

            return created;
        }

        internal void RecordAfterstory(NpcAfterstoryCompletionSaveData completion)
        {
            completions.Add(completion);
        }

        internal void RecordWeeklyReceipt(NpcWeeklyQuestReceiptSaveData receipt)
        {
            weeklyReceipts.Add(receipt);
        }

        internal void RecordClockObservation(DateTimeOffset observedAt, string weekKey)
        {
            latestObservedUtcTicks = Math.Max(
                latestObservedUtcTicks,
                observedAt.UtcDateTime.Ticks);
            if (string.CompareOrdinal(weekKey, latestObservedWeekKey) > 0)
            {
                latestObservedWeekKey = weekKey;
            }
        }

        private bool NormalizeCompletions()
        {
            var changed = false;
            var knownAfterstories = new HashSet<string>(StringComparer.Ordinal);
            var knownReceipts = new HashSet<string>(StringComparer.Ordinal);
            for (var index = completions.Count - 1; index >= 0; index -= 1)
            {
                var completion = completions[index];
                if (completion == null)
                {
                    completions.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= completion.EnsureRuntimeDefaults();
                if (!completion.HasValue
                    || !knownAfterstories.Add(completion.afterstoryId)
                    || !knownReceipts.Add(completion.receiptId))
                {
                    completions.RemoveAt(index);
                    changed = true;
                }
            }

            while (completions.Count > MaximumAfterstoryCompletions)
            {
                completions.RemoveAt(0);
                changed = true;
            }

            return changed;
        }

        private bool NormalizeWeeklyProgress()
        {
            var changed = false;
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = weeklyProgress.Count - 1; index >= 0; index -= 1)
            {
                var progress = weeklyProgress[index];
                if (progress == null)
                {
                    weeklyProgress.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= progress.EnsureRuntimeDefaults();
                var key = progress.npcId + "\n" + progress.weekKey;
                if (!progress.HasValue || !known.Add(key))
                {
                    weeklyProgress.RemoveAt(index);
                    changed = true;
                }
            }

            while (weeklyProgress.Count > MaximumWeeklyProgressEntries)
            {
                weeklyProgress.RemoveAt(0);
                changed = true;
            }

            return changed;
        }

        private bool NormalizeWeeklyReceipts()
        {
            var changed = false;
            var knownReceipts = new HashSet<string>(StringComparer.Ordinal);
            var knownNpcWeeks = new HashSet<string>(StringComparer.Ordinal);
            for (var index = weeklyReceipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = weeklyReceipts[index];
                if (receipt == null)
                {
                    weeklyReceipts.RemoveAt(index);
                    changed = true;
                    continue;
                }

                changed |= receipt.EnsureRuntimeDefaults();
                var npcWeek = receipt.npcId + "\n" + receipt.weekKey;
                if (!receipt.HasValue
                    || !knownReceipts.Add(receipt.receiptId)
                    || !knownNpcWeeks.Add(npcWeek))
                {
                    weeklyReceipts.RemoveAt(index);
                    changed = true;
                }
            }

            while (weeklyReceipts.Count > MaximumWeeklyReceipts)
            {
                weeklyReceipts.RemoveAt(0);
                changed = true;
            }

            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = NpcAfterstorySaveNormalization.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }

    [Serializable]
    public sealed class NpcAfterstoryCompletionSaveData
    {
        public string afterstoryId = string.Empty;
        public string npcId = string.Empty;
        public string choiceId = string.Empty;
        public string receiptId = string.Empty;
        public string keepsakeId = string.Empty;
        public string completedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrEmpty(afterstoryId)
            && !string.IsNullOrEmpty(npcId)
            && !string.IsNullOrEmpty(choiceId)
            && !string.IsNullOrEmpty(receiptId)
            && !string.IsNullOrEmpty(keepsakeId)
            && !string.IsNullOrEmpty(completedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= NpcAfterstorySaveNormalization.Normalize(ref afterstoryId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref npcId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref choiceId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref receiptId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref keepsakeId);
            changed |= NpcAfterstorySaveNormalization.NormalizeIso(ref completedAtIso);
            return changed;
        }
    }

    [Serializable]
    public sealed class NpcWeeklyQuestProgressSaveData
    {
        public string npcId = string.Empty;
        public string questId = string.Empty;
        public string weekKey = string.Empty;
        public int progress;
        public long observedUtcTicks;

        public bool HasValue => !string.IsNullOrEmpty(npcId)
            && !string.IsNullOrEmpty(questId)
            && NpcAfterstorySaveNormalization.TryParseDateKey(weekKey, out _);

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= NpcAfterstorySaveNormalization.Normalize(ref npcId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref questId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref weekKey);
            var safeProgress = Math.Max(0, Math.Min(1000000, progress));
            if (safeProgress != progress)
            {
                progress = safeProgress;
                changed = true;
            }

            if (!NpcAfterstorySaveData.IsReasonableObservedUtcTicks(
                    observedUtcTicks,
                    DateTimeOffset.UtcNow))
            {
                observedUtcTicks = 0L;
                changed = true;
            }

            return changed;
        }
    }

    [Serializable]
    public sealed class NpcWeeklyQuestReceiptSaveData
    {
        public string receiptId = string.Empty;
        public string npcId = string.Empty;
        public string questId = string.Empty;
        public string weekKey = string.Empty;
        public string claimedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrEmpty(receiptId)
            && !string.IsNullOrEmpty(npcId)
            && !string.IsNullOrEmpty(questId)
            && NpcAfterstorySaveNormalization.TryParseDateKey(weekKey, out _)
            && !string.IsNullOrEmpty(claimedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= NpcAfterstorySaveNormalization.Normalize(ref receiptId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref npcId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref questId);
            changed |= NpcAfterstorySaveNormalization.Normalize(ref weekKey);
            changed |= NpcAfterstorySaveNormalization.NormalizeIso(ref claimedAtIso);
            return changed;
        }
    }

    internal static class NpcAfterstorySaveNormalization
    {
        public static string NormalizeToken(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        public static bool Normalize(ref string value)
        {
            var normalized = NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }

        public static bool NormalizeIso(ref string value)
        {
            var changed = Normalize(ref value);
            if (!string.IsNullOrEmpty(value)
                && !DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _))
            {
                value = string.Empty;
                return true;
            }

            return changed;
        }

        public static bool TryParseDateKey(string value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }
    }
}
