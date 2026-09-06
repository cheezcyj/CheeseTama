using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    public enum DreamStoryClockValidationStatus
    {
        Valid = 0,
        InvalidNow = 1,
        RollbackDetected = 2
    }

    /// <summary>
    /// Standalone persistence contract for the first deep-story season. The root save owner may
    /// add this DTO as a nullable field and call EnsureRuntimeDefaults during its normal load flow.
    /// </summary>
    [Serializable]
    public sealed class DreamStorySeasonSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumCompletions = 64;
        public const int MaximumSignals = 32;
        public const int MaximumTokenLength = 100;

        public int schemaVersion = CurrentSchemaVersion;
        public List<DreamStoryCompletionSaveData> completions =
            new List<DreamStoryCompletionSaveData>();
        public List<string> discoveredSignalIds = new List<string>();
        public string lastObservedAtIso = string.Empty;

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            changed |= NormalizeTimestamp(ref lastObservedAtIso);
            changed |= NormalizeSignals(ref discoveredSignalIds);

            var source = completions;
            if (source == null)
            {
                completions = new List<DreamStoryCompletionSaveData>();
                return true;
            }

            var normalizedNewestFirst = new List<DreamStoryCompletionSaveData>(
                Math.Min(source.Count, MaximumCompletions));
            for (var index = source.Count - 1;
                index >= 0 && normalizedNewestFirst.Count < MaximumCompletions;
                index -= 1)
            {
                var completion = source[index];
                if (completion == null)
                {
                    changed = true;
                    continue;
                }

                changed |= completion.EnsureRuntimeDefaults();
                if (!completion.HasValue
                    || !IsStableToken(completion.episodeId)
                    || !IsStableToken(completion.choiceId)
                    || !IsReceiptToken(completion.receiptId))
                {
                    changed = true;
                    continue;
                }

                normalizedNewestFirst.Add(completion);
            }

            if (normalizedNewestFirst.Count != source.Count)
            {
                changed = true;
            }

            normalizedNewestFirst.Reverse();
            completions = normalizedNewestFirst;
            return changed;
        }

        /// <summary>
        /// Validates a caller-owned wall clock monotonically. A rollback never clears the stored
        /// watermark or future completion, so reloading cannot turn the same rollback into progress.
        /// </summary>
        public DreamStoryClockValidationStatus ValidateAndObserveClock(
            DateTimeOffset now,
            out bool stateChanged)
        {
            return ValidateAndObserveClock(now, null, out stateChanged);
        }

        internal DreamStoryClockValidationStatus ValidateAndObserveClock(
            DateTimeOffset now,
            Predicate<DreamStoryCompletionSaveData> shouldInspectCompletion,
            out bool stateChanged)
        {
            stateChanged = EnsureRuntimeDefaults();
            if (now == default)
            {
                return DreamStoryClockValidationStatus.InvalidNow;
            }

            var utcNow = now.ToUniversalTime();
            if (TryParseTimestamp(lastObservedAtIso, out var lastObserved)
                && utcNow < lastObserved)
            {
                return DreamStoryClockValidationStatus.RollbackDetected;
            }

            for (var index = 0; index < completions.Count; index += 1)
            {
                var completion = completions[index];
                if ((shouldInspectCompletion == null || shouldInspectCompletion(completion))
                    && TryParseTimestamp(completion.completedAtIso, out var completedAt)
                    && utcNow < completedAt)
                {
                    return DreamStoryClockValidationStatus.RollbackDetected;
                }
            }

            var normalizedNow = FormatTimestamp(utcNow);
            if (!string.Equals(lastObservedAtIso, normalizedNow, StringComparison.Ordinal))
            {
                lastObservedAtIso = normalizedNow;
                stateChanged = true;
            }

            return DreamStoryClockValidationStatus.Valid;
        }

        public bool HasCompletedEpisode(string episodeId)
        {
            return FindCompletion(episodeId) != null;
        }

        public bool HasSignal(string signalId)
        {
            var normalized = NormalizeToken(signalId);
            if (string.IsNullOrEmpty(normalized) || discoveredSignalIds == null)
            {
                return false;
            }

            for (var index = 0; index < discoveredSignalIds.Count; index += 1)
            {
                if (string.Equals(
                    discoveredSignalIds[index],
                    normalized,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryDiscoverSignal(string signalId)
        {
            EnsureRuntimeDefaults();
            var normalized = NormalizeToken(signalId);
            if (!IsStableToken(normalized)
                || HasSignal(normalized)
                || discoveredSignalIds.Count >= MaximumSignals)
            {
                return false;
            }

            discoveredSignalIds.Add(normalized);
            return true;
        }

        public DreamStoryCompletionSaveData FindCompletion(string episodeId)
        {
            var normalized = NormalizeToken(episodeId);
            if (string.IsNullOrEmpty(normalized) || completions == null)
            {
                return null;
            }

            for (var index = completions.Count - 1; index >= 0; index -= 1)
            {
                var completion = completions[index];
                if (completion != null
                    && string.Equals(
                        completion.episodeId,
                        normalized,
                        StringComparison.Ordinal))
                {
                    return completion;
                }
            }

            return null;
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

        internal bool CanRecordCompletion()
        {
            return completions != null && completions.Count < MaximumCompletions;
        }

        internal bool TryRecordCompletion(DreamStoryCompletionSaveData completion)
        {
            if (completion == null
                || completions == null
                || !CanRecordCompletion())
            {
                return false;
            }

            completion.EnsureRuntimeDefaults();
            if (!completion.HasValue
                || !IsStableToken(completion.episodeId)
                || !IsStableToken(completion.choiceId)
                || !IsReceiptToken(completion.receiptId)
                || HasCompletedEpisode(completion.episodeId)
                || HasReceipt(completion.receiptId))
            {
                return false;
            }

            completions.Add(completion);
            return true;
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

        private static bool NormalizeSignals(ref List<string> values)
        {
            var changed = values == null;
            var source = values ?? new List<string>();
            var normalized = new List<string>(Math.Min(source.Count, MaximumSignals));
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                index < source.Count && normalized.Count < MaximumSignals;
                index += 1)
            {
                var value = NormalizeToken(source[index]);
                if (!IsStableToken(value) || !known.Add(value))
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

            values = normalized;
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
    public sealed class DreamStoryCompletionSaveData
    {
        public string episodeId = string.Empty;
        public string choiceId = string.Empty;
        public string receiptId = string.Empty;
        public string completedAtIso = string.Empty;

        public bool HasValue => !string.IsNullOrEmpty(episodeId)
            && !string.IsNullOrEmpty(choiceId)
            && !string.IsNullOrEmpty(receiptId)
            && !string.IsNullOrEmpty(completedAtIso);

        public bool EnsureRuntimeDefaults()
        {
            var changed = Normalize(ref episodeId);
            changed |= Normalize(ref choiceId);
            changed |= Normalize(ref receiptId);
            var originalTimestamp = completedAtIso;
            if (!DreamStorySeasonSaveData.TryParseTimestamp(
                completedAtIso,
                out var completedAt))
            {
                completedAtIso = string.Empty;
            }
            else
            {
                completedAtIso = DreamStorySeasonSaveData.FormatTimestamp(completedAt);
            }

            changed |= !string.Equals(
                originalTimestamp,
                completedAtIso,
                StringComparison.Ordinal);
            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = DreamStorySeasonSaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
