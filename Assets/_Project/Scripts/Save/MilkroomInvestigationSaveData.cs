using System;
using System.Collections.Generic;
using System.Globalization;

namespace CheeseTama.Save
{
    /// <summary>
    /// Standalone persistence contract for the Milkroom investigation prologue. The root save
    /// owner may add this DTO as a nullable field and normalize it during the ordinary load flow.
    /// Unknown stable IDs are preserved for forward compatibility but are never exposed by the
    /// current investigation catalog.
    /// </summary>
    [Serializable]
    public sealed class MilkroomInvestigationSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaximumClues = 16;
        public const int MaximumCompletions = 8;
        public const int MaximumTokenLength = 100;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> discoveredClueIds = new List<string>();
        public List<MilkroomInvestigationCompletionSaveData> completions =
            new List<MilkroomInvestigationCompletionSaveData>();

        public bool EnsureRuntimeDefaults()
        {
            var changed = schemaVersion != CurrentSchemaVersion;
            schemaVersion = CurrentSchemaVersion;
            changed |= NormalizeClues();
            changed |= NormalizeCompletions();
            return changed;
        }

        public bool HasDiscoveredClue(string clueId)
        {
            var normalized = NormalizeToken(clueId);
            if (string.IsNullOrEmpty(normalized) || discoveredClueIds == null)
            {
                return false;
            }

            for (var index = 0; index < discoveredClueIds.Count; index += 1)
            {
                if (string.Equals(
                        discoveredClueIds[index],
                        normalized,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public MilkroomInvestigationCompletionSaveData FindCompletion(string episodeId)
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
                    && string.Equals(completion.episodeId, normalized, StringComparison.Ordinal))
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

        internal bool TryRecordClue(string clueId)
        {
            EnsureRuntimeDefaults();
            var normalized = NormalizeToken(clueId);
            if (!IsStableToken(normalized)
                || HasDiscoveredClue(normalized)
                || discoveredClueIds.Count >= MaximumClues)
            {
                return false;
            }

            discoveredClueIds.Add(normalized);
            return true;
        }

        internal bool CanRecordCompletion()
        {
            return completions != null && completions.Count < MaximumCompletions;
        }

        internal bool TryRecordCompletion(
            MilkroomInvestigationCompletionSaveData completion)
        {
            if (completion == null || completions == null || !CanRecordCompletion())
            {
                return false;
            }

            completion.EnsureRuntimeDefaults();
            if (!completion.HasValue
                || !IsStableToken(completion.episodeId)
                || !IsStableToken(completion.choiceId)
                || !IsReceiptToken(completion.receiptId)
                || FindCompletion(completion.episodeId) != null
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
                || !string.Equals(value, normalized, StringComparison.Ordinal))
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
                || !string.Equals(value, normalized, StringComparison.Ordinal))
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

        private bool NormalizeClues()
        {
            var source = discoveredClueIds;
            var changed = source == null;
            source ??= new List<string>();
            var normalized = new List<string>(Math.Min(source.Count, MaximumClues));
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                index < source.Count && normalized.Count < MaximumClues;
                index += 1)
            {
                var clueId = NormalizeToken(source[index]);
                if (!IsStableToken(clueId) || !known.Add(clueId))
                {
                    changed = true;
                    continue;
                }

                if (!string.Equals(source[index], clueId, StringComparison.Ordinal))
                {
                    changed = true;
                }

                normalized.Add(clueId);
            }

            if (normalized.Count != source.Count)
            {
                changed = true;
            }

            discoveredClueIds = normalized;
            return changed;
        }

        private bool NormalizeCompletions()
        {
            var source = completions;
            var changed = source == null;
            source ??= new List<MilkroomInvestigationCompletionSaveData>();
            var normalizedNewestFirst = new List<MilkroomInvestigationCompletionSaveData>(
                Math.Min(source.Count, MaximumCompletions));
            var episodeIds = new HashSet<string>(StringComparer.Ordinal);
            var receiptIds = new HashSet<string>(StringComparer.Ordinal);
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
                    || !IsReceiptToken(completion.receiptId)
                    || !episodeIds.Add(completion.episodeId)
                    || !receiptIds.Add(completion.receiptId))
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
    }

    [Serializable]
    public sealed class MilkroomInvestigationCompletionSaveData
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
            if (!MilkroomInvestigationSaveData.TryParseTimestamp(
                    completedAtIso,
                    out var completedAt))
            {
                completedAtIso = string.Empty;
            }
            else
            {
                completedAtIso = MilkroomInvestigationSaveData.FormatTimestamp(completedAt);
            }

            changed |= !string.Equals(
                originalTimestamp,
                completedAtIso,
                StringComparison.Ordinal);
            return changed;
        }

        private static bool Normalize(ref string value)
        {
            var normalized = MilkroomInvestigationSaveData.NormalizeToken(value);
            var changed = !string.Equals(value, normalized, StringComparison.Ordinal);
            value = normalized;
            return changed;
        }
    }
}
