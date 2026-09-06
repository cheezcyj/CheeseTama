using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class MilkroomMysterySaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumCompletions = 64;

        public int schemaVersion = CurrentSchemaVersion;
        public List<MilkroomMysteryCompletionSaveData> completions =
            new List<MilkroomMysteryCompletionSaveData>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;

            if (completions == null)
            {
                completions = new List<MilkroomMysteryCompletionSaveData>();
                return true;
            }

            var knownChapterIds = new HashSet<string>(StringComparer.Ordinal);
            var knownReceiptIds = new HashSet<string>(StringComparer.Ordinal);
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
                    || !knownChapterIds.Add(completion.chapterId)
                    || !knownReceiptIds.Add(completion.receiptId))
                {
                    completions.RemoveAt(index);
                    changed = true;
                }
            }

            while (completions.Count > MaximumCompletions)
            {
                completions.RemoveAt(0);
                changed = true;
            }

            return changed;
        }

        public bool HasCompletedChapter(string chapterId)
        {
            var normalized = NormalizeToken(chapterId);
            if (string.IsNullOrEmpty(normalized) || completions == null)
            {
                return false;
            }

            for (var index = 0; index < completions.Count; index += 1)
            {
                if (string.Equals(
                    completions[index]?.chapterId,
                    normalized,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasReceipt(string receiptId)
        {
            var normalized = NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalized) || completions == null)
            {
                return false;
            }

            for (var index = 0; index < completions.Count; index += 1)
            {
                if (string.Equals(
                    completions[index]?.receiptId,
                    normalized,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool CanRecordCompletion()
        {
            return completions != null && completions.Count < MaximumCompletions;
        }

        internal void RecordCompletion(MilkroomMysteryCompletionSaveData completion)
        {
            if (completion == null || !completion.HasValue || completions == null)
            {
                return;
            }

            completions.Add(completion);
        }

        internal static string NormalizeToken(string value, int maximumLength = 100)
        {
            var normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maximumLength
                ? normalized
                : normalized.Substring(0, maximumLength);
        }
    }

    [Serializable]
    public sealed class MilkroomMysteryCompletionSaveData
    {
        public string chapterId = string.Empty;
        public string choiceId = string.Empty;
        public string receiptId = string.Empty;
        public string completedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrWhiteSpace(chapterId)
            && !string.IsNullOrWhiteSpace(choiceId)
            && !string.IsNullOrWhiteSpace(receiptId)
            && !string.IsNullOrWhiteSpace(completedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            changed |= Normalize(ref chapterId);
            changed |= Normalize(ref choiceId);
            changed |= Normalize(ref receiptId);
            changed |= Normalize(ref completedAtIso, 64);
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

        private static bool Normalize(ref string value, int maximumLength = 100)
        {
            var normalized = MilkroomMysterySaveData.NormalizeToken(value, maximumLength);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
