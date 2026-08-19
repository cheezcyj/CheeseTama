using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Events
{
    public sealed class RandomEventJournalEntrySnapshot
    {
        internal RandomEventJournalEntrySnapshot(
            string eventId,
            string title,
            int totalOccurrences,
            string lastOccurredAtIso,
            string lastOccurredDate,
            int daysSinceLastOccurrence,
            string latestChoiceLabel,
            string latestChoiceSummary,
            string latestChoiceResolvedAtIso)
        {
            EventId = eventId ?? string.Empty;
            Title = title ?? string.Empty;
            TotalOccurrences = Math.Max(0, totalOccurrences);
            LastOccurredAtIso = lastOccurredAtIso ?? string.Empty;
            LastOccurredDate = lastOccurredDate ?? string.Empty;
            DaysSinceLastOccurrence = Math.Max(-1, daysSinceLastOccurrence);
            LatestChoiceLabel = latestChoiceLabel ?? string.Empty;
            LatestChoiceSummary = latestChoiceSummary ?? string.Empty;
            LatestChoiceResolvedAtIso = latestChoiceResolvedAtIso ?? string.Empty;
        }

        public string EventId { get; }
        public string Title { get; }
        public int TotalOccurrences { get; }
        public bool HasLastOccurrenceTime => !string.IsNullOrEmpty(LastOccurredAtIso);
        public string LastOccurredAtIso { get; }
        public string LastOccurredDate { get; }
        public int DaysSinceLastOccurrence { get; }
        public bool HasLatestChoice => !string.IsNullOrEmpty(LatestChoiceLabel);
        public string LatestChoiceLabel { get; }
        public string LatestChoiceSummary { get; }
        public string LatestChoiceResolvedAtIso { get; }
    }

    public sealed class RandomEventJournalSnapshot
    {
        internal RandomEventJournalSnapshot(
            string generatedAtIso,
            IReadOnlyList<RandomEventJournalEntrySnapshot> entries,
            int totalOccurrences)
        {
            GeneratedAtIso = generatedAtIso ?? string.Empty;
            Entries = entries ?? Array.Empty<RandomEventJournalEntrySnapshot>();
            TotalOccurrences = Math.Max(0, totalOccurrences);
        }

        public string GeneratedAtIso { get; }
        public IReadOnlyList<RandomEventJournalEntrySnapshot> Entries { get; }
        public int TotalOccurrences { get; }
    }

    /// <summary>
    /// Builds a presentation-only journal from persisted event history. Unknown
    /// event and choice ids are omitted, receipts are optional, and the source DTO
    /// is never normalized or mutated.
    /// </summary>
    public static class RandomEventJournalSystem
    {
        public static RandomEventJournalSnapshot Build(
            RandomEventSaveData saveData,
            DateTimeOffset now)
        {
            var aggregates = AggregateKnownHistory(saveData?.history);
            var entries = new List<RandomEventJournalEntrySnapshot>(aggregates.Count);
            var totalOccurrences = 0;

            for (var index = 0; index < aggregates.Count; index += 1)
            {
                var aggregate = aggregates[index];
                var latestChoice = FindLatestChoiceReadOnly(
                    saveData?.choiceReceipts,
                    aggregate.Definition);
                totalOccurrences = SaturatingAdd(
                    totalOccurrences,
                    aggregate.TotalOccurrences);
                entries.Add(BuildEntry(aggregate, latestChoice, now));
            }

            entries.Sort(CompareEntries);
            return new RandomEventJournalSnapshot(
                now.ToString("O", CultureInfo.InvariantCulture),
                entries.ToArray(),
                totalOccurrences);
        }

        private static List<HistoryAggregate> AggregateKnownHistory(
            IList<RandomEventHistorySaveEntry> history)
        {
            var aggregates = new List<HistoryAggregate>();
            if (history == null)
            {
                return aggregates;
            }

            for (var index = 0; index < history.Count; index += 1)
            {
                var source = history[index];
                var eventId = source?.eventId?.Trim();
                if (source == null
                    || source.totalOccurrences <= 0
                    || !RandomEventSystem.TryGetDefinition(eventId, out var definition))
                {
                    continue;
                }

                var aggregate = FindAggregate(aggregates, definition.id);
                if (aggregate == null)
                {
                    aggregate = new HistoryAggregate(definition);
                    aggregates.Add(aggregate);
                }

                aggregate.TotalOccurrences = SaturatingAdd(
                    aggregate.TotalOccurrences,
                    source.totalOccurrences);
                if (TryParseTimestamp(source.lastOccurredAtIso, out var occurredAt)
                    && (!aggregate.HasOccurredAt || occurredAt > aggregate.OccurredAt))
                {
                    aggregate.HasOccurredAt = true;
                    aggregate.OccurredAt = occurredAt;
                }
            }

            return aggregates;
        }

        private static HistoryAggregate FindAggregate(
            IList<HistoryAggregate> aggregates,
            string eventId)
        {
            for (var index = 0; index < aggregates.Count; index += 1)
            {
                if (string.Equals(
                        aggregates[index].Definition.id,
                        eventId,
                        StringComparison.Ordinal))
                {
                    return aggregates[index];
                }
            }

            return null;
        }

        private static ChoiceSnapshot FindLatestChoiceReadOnly(
            IList<CareEventChoiceReceiptSaveEntry> receipts,
            CareEventDefinition definition)
        {
            if (receipts == null || definition == null || !definition.RequiresChoice)
            {
                return default;
            }

            var found = false;
            var selected = default(ChoiceSnapshot);
            for (var index = 0; index < receipts.Count; index += 1)
            {
                var receipt = receipts[index];
                if (receipt == null
                    || string.IsNullOrWhiteSpace(receipt.occurrenceId)
                    || !string.Equals(
                        receipt.eventId?.Trim(),
                        definition.id,
                        StringComparison.Ordinal)
                    || !definition.TryGetChoice(
                        receipt.choiceId?.Trim(),
                        out var choice))
                {
                    continue;
                }

                var hasResolvedAt = TryParseTimestamp(
                    receipt.resolvedAtIso,
                    out var resolvedAt);
                var candidate = new ChoiceSnapshot(
                    choice,
                    hasResolvedAt,
                    resolvedAt,
                    index);
                if (!found || IsLater(candidate, selected))
                {
                    selected = candidate;
                    found = true;
                }
            }

            return found ? selected : default;
        }

        private static bool IsLater(ChoiceSnapshot candidate, ChoiceSnapshot current)
        {
            if (candidate.HasResolvedAt != current.HasResolvedAt)
            {
                return candidate.HasResolvedAt;
            }

            if (candidate.HasResolvedAt && candidate.ResolvedAt != current.ResolvedAt)
            {
                return candidate.ResolvedAt > current.ResolvedAt;
            }

            return candidate.SourceIndex > current.SourceIndex;
        }

        private static RandomEventJournalEntrySnapshot BuildEntry(
            HistoryAggregate aggregate,
            ChoiceSnapshot latestChoice,
            DateTimeOffset now)
        {
            var lastOccurredAtIso = aggregate.HasOccurredAt
                ? aggregate.OccurredAt.ToString("O", CultureInfo.InvariantCulture)
                : string.Empty;
            var lastOccurredDate = aggregate.HasOccurredAt
                ? aggregate.OccurredAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;
            var daysSince = aggregate.HasOccurredAt
                ? WholeDaysSince(aggregate.OccurredAt, now)
                : -1;

            var latestChoiceLabel = latestChoice.Choice?.label ?? string.Empty;
            var latestChoiceSummary = latestChoice.Choice == null
                ? string.Empty
                : BuildChoiceSummary(latestChoice.Choice);
            var latestChoiceResolvedAtIso = latestChoice.Choice != null
                && latestChoice.HasResolvedAt
                    ? latestChoice.ResolvedAt.ToString("O", CultureInfo.InvariantCulture)
                    : string.Empty;

            return new RandomEventJournalEntrySnapshot(
                aggregate.Definition.id,
                aggregate.Definition.title,
                aggregate.TotalOccurrences,
                lastOccurredAtIso,
                lastOccurredDate,
                daysSince,
                latestChoiceLabel,
                latestChoiceSummary,
                latestChoiceResolvedAtIso);
        }

        private static string BuildChoiceSummary(CareEventChoiceDefinition choice)
        {
            var heading = string.IsNullOrWhiteSpace(choice.resultTitle)
                ? choice.label
                : $"{choice.label}: {choice.resultTitle}";
            var effect = choice.effect.BuildSummary();
            if (string.IsNullOrWhiteSpace(effect))
            {
                return heading ?? string.Empty;
            }

            return $"{heading} · {effect.Replace("\n", " · ")}";
        }

        private static int WholeDaysSince(DateTimeOffset occurredAt, DateTimeOffset now)
        {
            var elapsed = now.ToUniversalTime() - occurredAt.ToUniversalTime();
            if (elapsed <= TimeSpan.Zero)
            {
                return 0;
            }

            return elapsed.TotalDays >= int.MaxValue
                ? int.MaxValue
                : (int)Math.Floor(elapsed.TotalDays);
        }

        private static int CompareEntries(
            RandomEventJournalEntrySnapshot left,
            RandomEventJournalEntrySnapshot right)
        {
            if (left.HasLastOccurrenceTime != right.HasLastOccurrenceTime)
            {
                return left.HasLastOccurrenceTime ? -1 : 1;
            }

            if (left.HasLastOccurrenceTime
                && TryParseTimestamp(left.LastOccurredAtIso, out var leftAt)
                && TryParseTimestamp(right.LastOccurredAtIso, out var rightAt)
                && leftAt != rightAt)
            {
                return rightAt.CompareTo(leftAt);
            }

            return string.Compare(left.EventId, right.EventId, StringComparison.Ordinal);
        }

        private static bool TryParseTimestamp(string value, out DateTimeOffset parsed)
        {
            return DateTimeOffset.TryParse(
                value?.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed);
        }

        private static int SaturatingAdd(int current, int amount)
        {
            if (current >= int.MaxValue || amount >= int.MaxValue - current)
            {
                return int.MaxValue;
            }

            return Math.Max(0, current + Math.Max(0, amount));
        }

        private sealed class HistoryAggregate
        {
            public HistoryAggregate(CareEventDefinition definition)
            {
                Definition = definition;
            }

            public CareEventDefinition Definition { get; }
            public int TotalOccurrences { get; set; }
            public bool HasOccurredAt { get; set; }
            public DateTimeOffset OccurredAt { get; set; }
        }

        private readonly struct ChoiceSnapshot
        {
            public ChoiceSnapshot(
                CareEventChoiceDefinition choice,
                bool hasResolvedAt,
                DateTimeOffset resolvedAt,
                int sourceIndex)
            {
                Choice = choice;
                HasResolvedAt = hasResolvedAt;
                ResolvedAt = resolvedAt;
                SourceIndex = sourceIndex;
            }

            public CareEventChoiceDefinition Choice { get; }
            public bool HasResolvedAt { get; }
            public DateTimeOffset ResolvedAt { get; }
            public int SourceIndex { get; }
        }
    }
}
