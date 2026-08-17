using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Memories
{
    public sealed class MemoryJournalDraft
    {
        public MemoryJournalDraft(
            MemoryJournalKind kind,
            string sourceId,
            string occurrenceId,
            string detailId,
            DateTimeOffset occurredAt,
            string tamaName,
            string formId,
            string title,
            string quote,
            bool important = false,
            bool isHiddenContent = false,
            string hiddenUnlockId = "")
        {
            Kind = kind;
            SourceId = sourceId ?? string.Empty;
            OccurrenceId = occurrenceId ?? string.Empty;
            DetailId = detailId ?? string.Empty;
            OccurredAt = occurredAt;
            TamaName = tamaName ?? string.Empty;
            FormId = formId ?? string.Empty;
            Title = title ?? string.Empty;
            Quote = quote ?? string.Empty;
            Important = important;
            IsHiddenContent = isHiddenContent;
            HiddenUnlockId = hiddenUnlockId ?? string.Empty;
        }

        public MemoryJournalKind Kind { get; }
        public string SourceId { get; }
        public string OccurrenceId { get; }
        public string DetailId { get; }
        public DateTimeOffset OccurredAt { get; }
        public string TamaName { get; }
        public string FormId { get; }
        public string Title { get; }
        public string Quote { get; }
        public bool Important { get; }
        public bool IsHiddenContent { get; }
        public string HiddenUnlockId { get; }
    }

    public sealed class MemoryJournalPresentation
    {
        public MemoryJournalPresentation(
            string memoryId,
            MemoryJournalKind kind,
            string dateKey,
            string tamaName,
            string formId,
            string title,
            string quote,
            bool unread,
            bool important,
            bool isMasked)
        {
            MemoryId = memoryId ?? string.Empty;
            Kind = kind;
            DateKey = dateKey ?? string.Empty;
            TamaName = tamaName ?? string.Empty;
            FormId = formId ?? string.Empty;
            Title = title ?? string.Empty;
            Quote = quote ?? string.Empty;
            Unread = unread;
            Important = important;
            IsMasked = isMasked;
        }

        public string MemoryId { get; }
        public MemoryJournalKind Kind { get; }
        public string DateKey { get; }
        public string TamaName { get; }
        public string FormId { get; }
        public string Title { get; }
        public string Quote { get; }
        public bool Unread { get; }
        public bool Important { get; }
        public bool IsMasked { get; }
    }

    public sealed class MemoryJournalRecall
    {
        public MemoryJournalRecall(MemoryJournalPresentation memory)
        {
            Memory = memory;
        }

        public MemoryJournalPresentation Memory { get; }
        public string MemoryId => Memory?.MemoryId ?? string.Empty;
        public string DialogueLine => Memory == null || string.IsNullOrWhiteSpace(Memory.Quote)
            ? string.Empty
            : $"그날의 기억: {Memory.Quote}";
    }

    /// <summary>
    /// Owns deterministic journal rules. Persistence and UI timing remain with the caller.
    /// </summary>
    public sealed class MemoryJournalSystem
    {
        private const string DailyFirstCareSourceId = "daily_first_care";
        private const string HiddenTitle = "아직 비밀인 추억";
        private const string HiddenQuote = "조금 더 함께 지내면 이 기억의 의미를 알 수 있을 것 같아.";

        public bool TryRecordFirstDailyCare(
            MemoryJournalSaveData journal,
            string actionId,
            DateTimeOffset occurredAt,
            string tamaName,
            string formId,
            out MemoryJournalEntrySaveData recorded)
        {
            recorded = null;
            if (!TryResolveCareCopy(actionId, out var title, out var quote))
            {
                return false;
            }

            return TryRecord(
                journal,
                new MemoryJournalDraft(
                    MemoryJournalKind.Care,
                    DailyFirstCareSourceId,
                    string.Empty,
                    actionId,
                    occurredAt,
                    tamaName,
                    formId,
                    title,
                    quote),
                out recorded);
        }

        public bool TryRecordReturn(
            MemoryJournalSaveData journal,
            string summaryOccurrenceId,
            int elapsedMinutes,
            DateTimeOffset occurredAt,
            string tamaName,
            string formId,
            out MemoryJournalEntrySaveData recorded)
        {
            var safeMinutes = Math.Max(0, elapsedMinutes);
            var durationText = safeMinutes >= 60
                ? $"{Math.Max(1, safeMinutes / 60)}시간"
                : $"{safeMinutes}분";
            return TryRecord(
                journal,
                new MemoryJournalDraft(
                    MemoryJournalKind.Return,
                    "return_summary",
                    summaryOccurrenceId,
                    string.Empty,
                    occurredAt,
                    tamaName,
                    formId,
                    "다시 만난 순간",
                    $"{durationText} 만에 돌아와 다시 눈을 마주쳤다."),
                out recorded);
        }

        public bool TryRecordGrowth(
            MemoryJournalSaveData journal,
            string milestoneId,
            string occurrenceId,
            int level,
            string stageDisplayName,
            DateTimeOffset occurredAt,
            string tamaName,
            string formId,
            bool isHiddenContent,
            string hiddenUnlockId,
            out MemoryJournalEntrySaveData recorded)
        {
            var safeLevel = Math.Max(1, level);
            var safeStageName = string.IsNullOrWhiteSpace(stageDisplayName)
                ? "새로운 모습"
                : stageDisplayName.Trim();
            var stableMilestoneId = string.IsNullOrWhiteSpace(milestoneId)
                ? "growth_milestone"
                : milestoneId.Trim();
            if (HasRecordedSource(journal, MemoryJournalKind.Growth, stableMilestoneId))
            {
                recorded = null;
                return false;
            }

            return TryRecord(
                journal,
                new MemoryJournalDraft(
                    MemoryJournalKind.Growth,
                    stableMilestoneId,
                    occurrenceId,
                    string.Empty,
                    occurredAt,
                    tamaName,
                    formId,
                    $"Lv.{safeLevel} · {safeStageName}",
                    $"{ResolveTamaName(tamaName)}의 새로운 모습이 반짝였다.",
                    true,
                    isHiddenContent,
                    hiddenUnlockId),
                out recorded);
        }

        public bool TryRecordEvolution(
            MemoryJournalSaveData journal,
            string evolutionId,
            string occurrenceId,
            int level,
            string evolutionDisplayName,
            DateTimeOffset occurredAt,
            string tamaName,
            string formId,
            bool isHiddenContent,
            string hiddenUnlockId,
            out MemoryJournalEntrySaveData recorded)
        {
            var safeEvolutionName = string.IsNullOrWhiteSpace(evolutionDisplayName)
                ? "새로운 치즈타마"
                : evolutionDisplayName.Trim();
            var stableEvolutionId = string.IsNullOrWhiteSpace(evolutionId)
                ? "evolution"
                : evolutionId.Trim();
            if (HasRecordedSource(journal, MemoryJournalKind.Evolution, stableEvolutionId))
            {
                recorded = null;
                return false;
            }

            return TryRecord(
                journal,
                new MemoryJournalDraft(
                    MemoryJournalKind.Evolution,
                    stableEvolutionId,
                    occurrenceId,
                    string.Empty,
                    occurredAt,
                    tamaName,
                    formId,
                    $"Lv.{Math.Max(1, level)} · {safeEvolutionName}으로 진화",
                    $"함께 보낸 시간이 {safeEvolutionName}의 모습으로 피어났다.",
                    true,
                    isHiddenContent,
                    hiddenUnlockId),
                out recorded);
        }

        private static bool HasRecordedSource(
            MemoryJournalSaveData journal,
            MemoryJournalKind kind,
            string sourceId)
        {
            if (journal?.entries == null || string.IsNullOrWhiteSpace(sourceId))
            {
                return false;
            }

            for (var index = 0; index < journal.entries.Count; index += 1)
            {
                var entry = journal.entries[index];
                if (entry != null
                    && entry.kind == kind
                    && string.Equals(entry.sourceId, sourceId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryRecord(
            MemoryJournalSaveData journal,
            MemoryJournalDraft draft,
            out MemoryJournalEntrySaveData recorded)
        {
            recorded = null;
            if (journal == null || draft == null || string.IsNullOrWhiteSpace(draft.SourceId))
            {
                return false;
            }

            journal.EnsureRuntimeDefaults();
            var occurredAt = draft.OccurredAt == default ? DateTimeOffset.Now : draft.OccurredAt;
            var dateKey = occurredAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var idempotencyKey = MemoryJournalSaveKey.Build(
                draft.Kind,
                draft.SourceId,
                dateKey,
                draft.OccurrenceId);
            for (var index = 0; index < journal.entries.Count; index += 1)
            {
                if (string.Equals(
                    journal.entries[index]?.idempotencyKey,
                    idempotencyKey,
                    StringComparison.Ordinal))
                {
                    return false;
                }
            }

            var entry = new MemoryJournalEntrySaveData
            {
                id = $"memory_{Guid.NewGuid():N}",
                idempotencyKey = idempotencyKey,
                kind = draft.Kind,
                sourceId = NormalizeId(draft.SourceId),
                occurrenceId = NormalizeId(draft.OccurrenceId),
                detailId = NormalizeId(draft.DetailId),
                dateKey = dateKey,
                occurredAtIso = occurredAt.ToString("O", CultureInfo.InvariantCulture),
                tamaName = NormalizeText(ResolveTamaName(draft.TamaName), 40),
                formId = NormalizeId(draft.FormId),
                title = NormalizeText(draft.Title, 80),
                quote = NormalizeText(draft.Quote, 160),
                unread = true,
                important = draft.Important
                    || draft.Kind == MemoryJournalKind.Growth
                    || draft.Kind == MemoryJournalKind.Evolution,
                isHiddenContent = draft.IsHiddenContent,
                hiddenUnlockId = NormalizeId(draft.HiddenUnlockId)
            };
            entry.EnsureRuntimeDefaults(journal.entries.Count);
            journal.entries.Add(entry);
            journal.TrimToCapacity();

            // If all 60 slots are protected milestones, a routine entry removes itself.
            for (var index = journal.entries.Count - 1; index >= 0; index -= 1)
            {
                if (ReferenceEquals(journal.entries[index], entry))
                {
                    recorded = entry;
                    return true;
                }
            }

            return false;
        }

        public int CountUnread(MemoryJournalSaveData journal)
        {
            if (journal == null)
            {
                return 0;
            }

            journal.EnsureRuntimeDefaults();
            var count = 0;
            for (var index = 0; index < journal.entries.Count; index += 1)
            {
                if (journal.entries[index].unread)
                {
                    count += 1;
                }
            }

            return count;
        }

        public bool TryMarkRead(MemoryJournalSaveData journal, string memoryId)
        {
            if (journal == null || string.IsNullOrWhiteSpace(memoryId))
            {
                return false;
            }

            journal.EnsureRuntimeDefaults();
            for (var index = 0; index < journal.entries.Count; index += 1)
            {
                var entry = journal.entries[index];
                if (!string.Equals(entry.id, memoryId, StringComparison.Ordinal) || !entry.unread)
                {
                    continue;
                }

                entry.unread = false;
                return true;
            }

            return false;
        }

        public int MarkAllRead(MemoryJournalSaveData journal)
        {
            if (journal == null)
            {
                return 0;
            }

            journal.EnsureRuntimeDefaults();
            var changed = 0;
            for (var index = 0; index < journal.entries.Count; index += 1)
            {
                if (!journal.entries[index].unread)
                {
                    continue;
                }

                journal.entries[index].unread = false;
                changed += 1;
            }

            return changed;
        }

        public IReadOnlyList<MemoryJournalPresentation> GetNewestFirst(
            MemoryJournalSaveData journal,
            Func<string, bool> hiddenUnlockResolver = null)
        {
            var result = new List<MemoryJournalPresentation>();
            if (journal == null)
            {
                return result;
            }

            journal.EnsureRuntimeDefaults();
            result.Capacity = journal.entries.Count;
            for (var index = journal.entries.Count - 1; index >= 0; index -= 1)
            {
                result.Add(CreatePresentation(journal.entries[index], hiddenUnlockResolver));
            }

            return result;
        }

        public bool TrySelectLatestRecall(
            MemoryJournalSaveData journal,
            Func<string, bool> hiddenUnlockResolver,
            out MemoryJournalRecall recall)
        {
            recall = null;
            if (journal == null)
            {
                return false;
            }

            journal.EnsureRuntimeDefaults();
            if (journal.entries.Count == 0)
            {
                return false;
            }

            var latest = journal.entries[journal.entries.Count - 1];
            if (string.Equals(latest.id, journal.lastRecalledMemoryId, StringComparison.Ordinal))
            {
                return false;
            }

            recall = new MemoryJournalRecall(CreatePresentation(latest, hiddenUnlockResolver));
            return true;
        }

        public bool AcknowledgeRecall(MemoryJournalSaveData journal, string memoryId)
        {
            if (journal == null || string.IsNullOrWhiteSpace(memoryId))
            {
                return false;
            }

            journal.EnsureRuntimeDefaults();
            for (var index = journal.entries.Count - 1; index >= 0; index -= 1)
            {
                if (!string.Equals(journal.entries[index].id, memoryId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(journal.lastRecalledMemoryId, memoryId, StringComparison.Ordinal))
                {
                    return false;
                }

                journal.lastRecalledMemoryId = memoryId;
                return true;
            }

            return false;
        }

        public MemoryJournalPresentation CreatePresentation(
            MemoryJournalEntrySaveData entry,
            Func<string, bool> hiddenUnlockResolver = null)
        {
            if (entry == null)
            {
                return null;
            }

            entry.EnsureRuntimeDefaults();
            var hiddenUnlocked = !entry.isHiddenContent
                || (hiddenUnlockResolver != null
                    && !string.IsNullOrWhiteSpace(entry.hiddenUnlockId)
                    && hiddenUnlockResolver(entry.hiddenUnlockId));
            if (!hiddenUnlocked)
            {
                return new MemoryJournalPresentation(
                    entry.id,
                    entry.kind,
                    entry.dateKey,
                    entry.tamaName,
                    string.Empty,
                    HiddenTitle,
                    HiddenQuote,
                    entry.unread,
                    entry.important,
                    true);
            }

            return new MemoryJournalPresentation(
                entry.id,
                entry.kind,
                entry.dateKey,
                entry.tamaName,
                entry.formId,
                entry.title,
                entry.quote,
                entry.unread,
                entry.important,
                false);
        }

        private static bool TryResolveCareCopy(string actionId, out string title, out string quote)
        {
            title = string.Empty;
            quote = string.Empty;
            var safeActionId = actionId?.Trim() ?? string.Empty;
            if (string.Equals(safeActionId, "pet", StringComparison.Ordinal))
            {
                title = "포근하게 쓰다듬은 날";
                quote = "천천히 쓰다듬자 마음의 거리가 조금 더 가까워졌다.";
                return true;
            }

            if (string.Equals(safeActionId, "play", StringComparison.Ordinal))
            {
                title = "신나게 놀아준 날";
                quote = "함께 웃고 뛰며 즐거운 시간을 보냈다.";
                return true;
            }

            if (safeActionId.StartsWith("feed_", StringComparison.Ordinal))
            {
                title = "맛있는 한입을 나눈 날";
                quote = "좋아하는 먹이를 건네자 기분 좋은 표정을 지었다.";
                return true;
            }

            return false;
        }

        private static string ResolveTamaName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "치즈타마" : value.Trim();
        }

        private static string NormalizeId(string value)
        {
            return NormalizeText(value, 100).Replace(" ", "_");
        }

        private static string NormalizeText(string value, int maximumLength)
        {
            var normalized = (value ?? string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
            while (normalized.Contains("  "))
            {
                normalized = normalized.Replace("  ", " ");
            }

            return normalized.Length <= maximumLength
                ? normalized
                : normalized.Substring(0, maximumLength);
        }
    }
}
