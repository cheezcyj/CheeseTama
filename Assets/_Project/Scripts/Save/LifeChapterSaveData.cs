using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    public enum LifeChapterClockValidationStatus
    {
        Valid = 0,
        InvalidNow = 1,
        RollbackDetected = 2
    }

    /// <summary>
    /// Standalone persistence DTO for milestone life chapters. A root save owner can add this as
    /// a nullable field; both a missing legacy field and an explicit null value normalize to an
    /// empty, playable chapter state rather than suppressing retroactive story content.
    /// </summary>
    [Serializable]
    public sealed class LifeChapterSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumProgressEntries = 16;
        public const int MaximumCompletionEntries = 32;
        public const int MaximumObjectiveIdsPerChapter = 8;
        public const int MaximumTokenLength = 100;

        public int schemaVersion = CurrentSchemaVersion;
        public List<LifeChapterProgressSaveData> progress =
            new List<LifeChapterProgressSaveData>();
        public List<LifeChapterCompletionSaveData> completions =
            new List<LifeChapterCompletionSaveData>();
        public string lastObservedAtIso = string.Empty;

        public static LifeChapterSaveData CreateForNewPlayer()
        {
            return new LifeChapterSaveData();
        }

        public static LifeChapterSaveData CreateForLegacyPlayer()
        {
            return new LifeChapterSaveData();
        }

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            changed |= NormalizeTimestamp(ref lastObservedAtIso);
            changed |= NormalizeProgress();
            changed |= NormalizeCompletions();
            return changed;
        }

        public LifeChapterClockValidationStatus ValidateAndObserveClock(
            DateTimeOffset now,
            out bool stateChanged)
        {
            return ValidateAndObserveClock(now, null, out stateChanged);
        }

        internal LifeChapterClockValidationStatus ValidateAndObserveClock(
            DateTimeOffset now,
            Predicate<LifeChapterCompletionSaveData> shouldInspectCompletion,
            out bool stateChanged)
        {
            stateChanged = EnsureRuntimeDefaults();
            if (now == default)
            {
                return LifeChapterClockValidationStatus.InvalidNow;
            }

            var utcNow = now.ToUniversalTime();
            if (TryParseTimestamp(lastObservedAtIso, out var lastObserved)
                && utcNow < lastObserved)
            {
                return LifeChapterClockValidationStatus.RollbackDetected;
            }

            for (var index = 0; index < completions.Count; index += 1)
            {
                var completion = completions[index];
                if ((shouldInspectCompletion == null || shouldInspectCompletion(completion))
                    && TryParseTimestamp(completion?.completedAtIso, out var completedAt)
                    && utcNow < completedAt)
                {
                    return LifeChapterClockValidationStatus.RollbackDetected;
                }
            }

            var normalizedNow = FormatTimestamp(utcNow);
            if (!string.Equals(lastObservedAtIso, normalizedNow, StringComparison.Ordinal))
            {
                lastObservedAtIso = normalizedNow;
                stateChanged = true;
            }

            return LifeChapterClockValidationStatus.Valid;
        }

        public LifeChapterProgressSaveData FindProgress(string chapterId)
        {
            var normalized = NormalizeToken(chapterId);
            if (string.IsNullOrEmpty(normalized) || progress == null)
            {
                return null;
            }

            for (var index = progress.Count - 1; index >= 0; index -= 1)
            {
                var entry = progress[index];
                if (entry != null
                    && string.Equals(entry.chapterId, normalized, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        public LifeChapterCompletionSaveData FindCompletion(string chapterId)
        {
            var normalized = NormalizeToken(chapterId);
            if (string.IsNullOrEmpty(normalized) || completions == null)
            {
                return null;
            }

            for (var index = completions.Count - 1; index >= 0; index -= 1)
            {
                var entry = completions[index];
                if (entry != null
                    && string.Equals(entry.chapterId, normalized, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        public bool HasCompletedChapter(string chapterId)
        {
            return FindCompletion(chapterId) != null;
        }

        public bool HasReceipt(string receiptId)
        {
            var normalized = NormalizeToken(receiptId);
            if (string.IsNullOrEmpty(normalized) || completions == null)
            {
                return false;
            }

            for (var index = completions.Count - 1; index >= 0; index -= 1)
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

        internal static string NormalizeToken(
            string value,
            int maximumLength = MaximumTokenLength)
        {
            var normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maximumLength
                ? normalized
                : normalized.Substring(0, maximumLength);
        }

        internal static bool IsStableToken(string value)
        {
            var normalized = NormalizeToken(value);
            if (string.IsNullOrEmpty(normalized)
                || !string.Equals(normalized, value, StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = 0; index < normalized.Length; index += 1)
            {
                var character = normalized[index];
                var valid = (character >= 'a' && character <= 'z')
                    || (character >= '0' && character <= '9')
                    || character == '_'
                    || character == '-'
                    || character == '.';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsReceiptToken(string value)
        {
            var normalized = NormalizeToken(value);
            if (string.IsNullOrEmpty(normalized)
                || !string.Equals(normalized, value, StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = 0; index < normalized.Length; index += 1)
            {
                var character = normalized[index];
                var valid = (character >= 'a' && character <= 'z')
                    || (character >= 'A' && character <= 'Z')
                    || (character >= '0' && character <= '9')
                    || character == '_'
                    || character == '-'
                    || character == '.'
                    || character == ':';
                if (!valid)
                {
                    return false;
                }
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
            return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }

        private bool NormalizeProgress()
        {
            var changed = progress == null;
            var source = progress ?? new List<LifeChapterProgressSaveData>();
            var normalizedNewestFirst = new List<LifeChapterProgressSaveData>(
                Math.Min(source.Count, MaximumProgressEntries));
            var knownChapterIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = source.Count - 1;
                index >= 0 && normalizedNewestFirst.Count < MaximumProgressEntries;
                index -= 1)
            {
                var entry = source[index];
                if (entry == null)
                {
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (!entry.HasValue
                    || !IsStableToken(entry.chapterId)
                    || !knownChapterIds.Add(entry.chapterId))
                {
                    changed = true;
                    continue;
                }

                normalizedNewestFirst.Add(entry);
            }

            if (normalizedNewestFirst.Count != source.Count)
            {
                changed = true;
            }

            normalizedNewestFirst.Reverse();
            progress = normalizedNewestFirst;
            return changed;
        }

        private bool NormalizeCompletions()
        {
            var changed = completions == null;
            var source = completions ?? new List<LifeChapterCompletionSaveData>();
            var normalizedNewestFirst = new List<LifeChapterCompletionSaveData>(
                Math.Min(source.Count, MaximumCompletionEntries));
            var knownChapterIds = new HashSet<string>(StringComparer.Ordinal);
            var knownReceiptIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = source.Count - 1;
                index >= 0 && normalizedNewestFirst.Count < MaximumCompletionEntries;
                index -= 1)
            {
                var entry = source[index];
                if (entry == null)
                {
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (!entry.HasValue
                    || !IsStableToken(entry.chapterId)
                    || !IsStableToken(entry.choiceId)
                    || !IsReceiptToken(entry.receiptId)
                    || !knownChapterIds.Add(entry.chapterId)
                    || !knownReceiptIds.Add(entry.receiptId))
                {
                    changed = true;
                    continue;
                }

                normalizedNewestFirst.Add(entry);
            }

            if (normalizedNewestFirst.Count != source.Count)
            {
                changed = true;
            }

            normalizedNewestFirst.Reverse();
            completions = normalizedNewestFirst;
            return changed;
        }

        private static bool NormalizeTimestamp(ref string value)
        {
            var original = value;
            if (!TryParseTimestamp(value, out var parsed))
            {
                value = string.Empty;
            }
            else
            {
                value = FormatTimestamp(parsed);
            }

            return !string.Equals(original, value, StringComparison.Ordinal);
        }
    }

    [Serializable]
    public sealed class LifeChapterProgressSaveData
    {
        public string chapterId = string.Empty;
        public List<string> completedObjectiveIds = new List<string>();

        public bool HasValue => !string.IsNullOrEmpty(chapterId);

        public bool EnsureRuntimeDefaults()
        {
            var changed = Normalize(ref chapterId);
            var source = completedObjectiveIds;
            if (source == null)
            {
                completedObjectiveIds = new List<string>();
                return true;
            }

            var normalized = new List<string>(
                Math.Min(source.Count, LifeChapterSaveData.MaximumObjectiveIdsPerChapter));
            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                index < source.Count
                    && normalized.Count < LifeChapterSaveData.MaximumObjectiveIdsPerChapter;
                index += 1)
            {
                var value = LifeChapterSaveData.NormalizeToken(source[index]);
                if (!LifeChapterSaveData.IsStableToken(value) || !knownIds.Add(value))
                {
                    changed = true;
                    continue;
                }

                if (!string.Equals(source[index], value, StringComparison.Ordinal))
                {
                    changed = true;
                }

                normalized.Add(value);
            }

            if (normalized.Count != source.Count)
            {
                changed = true;
            }

            completedObjectiveIds = normalized;
            return changed;
        }

        public bool HasCompletedObjective(string objectiveId)
        {
            var normalized = LifeChapterSaveData.NormalizeToken(objectiveId);
            if (string.IsNullOrEmpty(normalized) || completedObjectiveIds == null)
            {
                return false;
            }

            for (var index = 0; index < completedObjectiveIds.Count; index += 1)
            {
                if (string.Equals(
                    completedObjectiveIds[index],
                    normalized,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = LifeChapterSaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }

    [Serializable]
    public sealed class LifeChapterCompletionSaveData
    {
        public string chapterId = string.Empty;
        public string choiceId = string.Empty;
        public string receiptId = string.Empty;
        public string completedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrEmpty(chapterId)
            && !string.IsNullOrEmpty(choiceId)
            && !string.IsNullOrEmpty(receiptId)
            && !string.IsNullOrEmpty(completedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = Normalize(ref chapterId);
            changed |= Normalize(ref choiceId);
            changed |= Normalize(ref receiptId);
            var originalTimestamp = completedAtIso;
            if (!LifeChapterSaveData.TryParseTimestamp(
                completedAtIso,
                out var completedAt))
            {
                completedAtIso = string.Empty;
            }
            else
            {
                completedAtIso = LifeChapterSaveData.FormatTimestamp(completedAt);
            }

            changed |= !string.Equals(
                originalTimestamp,
                completedAtIso,
                StringComparison.Ordinal);
            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = LifeChapterSaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
