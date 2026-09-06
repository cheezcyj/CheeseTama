using System;
using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Growth
{
    public sealed class LifeChapterObjectiveView
    {
        internal LifeChapterObjectiveView(string id, string displayName, bool completed)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Completed = completed;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public bool Completed { get; }
    }

    public sealed class LifeChapterChoiceView
    {
        internal LifeChapterChoiceView(string id, string label)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
    }

    /// <summary>
    /// Spoiler-safe view of only the current eligible chapter. A hidden snapshot contains no
    /// chapter ID, level, title, body, objective, choice, or future chapter count metadata.
    /// </summary>
    public sealed class LifeChapterSnapshot
    {
        private readonly LifeChapterObjectiveView[] objectives;
        private readonly LifeChapterChoiceView[] choices;

        private LifeChapterSnapshot(
            bool visible,
            string chapterId,
            string title,
            string body,
            LifeChapterObjectiveView[] objectives,
            LifeChapterChoiceView[] choices)
        {
            Visible = visible;
            ChapterId = visible ? chapterId ?? string.Empty : string.Empty;
            Title = visible ? title ?? string.Empty : string.Empty;
            Body = visible ? body ?? string.Empty : string.Empty;
            this.objectives = visible
                ? objectives ?? Array.Empty<LifeChapterObjectiveView>()
                : Array.Empty<LifeChapterObjectiveView>();
            this.choices = visible
                ? choices ?? Array.Empty<LifeChapterChoiceView>()
                : Array.Empty<LifeChapterChoiceView>();

            var completedCount = 0;
            for (var index = 0; index < this.objectives.Length; index += 1)
            {
                if (this.objectives[index]?.Completed == true)
                {
                    completedCount += 1;
                }
            }

            CompletedObjectiveCount = visible ? completedCount : 0;
        }

        public bool Visible { get; }
        public string ChapterId { get; }
        public string Title { get; }
        public string Body { get; }
        public IReadOnlyList<LifeChapterObjectiveView> Objectives => objectives;
        public IReadOnlyList<LifeChapterChoiceView> Choices => choices;
        public int CompletedObjectiveCount { get; }
        public bool ObjectivesComplete => Visible
            && objectives.Length == LifeChapterCatalog.ObjectivesPerChapter
            && CompletedObjectiveCount == objectives.Length;
        public bool CanChoose => ObjectivesComplete
            && choices.Length == LifeChapterCatalog.ChoicesPerChapter;

        internal static LifeChapterSnapshot CreatePending(
            LifeChapterDefinition chapter,
            LifeChapterProgressSaveData progress)
        {
            if (chapter == null)
            {
                return CreateHidden();
            }

            var objectiveViews = new LifeChapterObjectiveView[chapter.Objectives.Count];
            var allCompleted = chapter.Objectives.Count == LifeChapterCatalog.ObjectivesPerChapter;
            for (var index = 0; index < chapter.Objectives.Count; index += 1)
            {
                var objective = chapter.Objectives[index];
                var completed = progress?.HasCompletedObjective(objective.Id) == true;
                allCompleted &= completed;
                objectiveViews[index] = new LifeChapterObjectiveView(
                    objective.Id,
                    objective.DisplayName,
                    completed);
            }

            var choiceViews = Array.Empty<LifeChapterChoiceView>();
            if (allCompleted)
            {
                choiceViews = new LifeChapterChoiceView[chapter.Choices.Count];
                for (var index = 0; index < chapter.Choices.Count; index += 1)
                {
                    choiceViews[index] = new LifeChapterChoiceView(
                        chapter.Choices[index].Id,
                        chapter.Choices[index].Label);
                }
            }

            return new LifeChapterSnapshot(
                true,
                chapter.Id,
                chapter.Title,
                chapter.Body,
                objectiveViews,
                choiceViews);
        }

        public static LifeChapterSnapshot CreateHidden()
        {
            return new LifeChapterSnapshot(
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<LifeChapterObjectiveView>(),
                Array.Empty<LifeChapterChoiceView>());
        }
    }

    public sealed class LifeChapterRecordPayload
    {
        internal LifeChapterRecordPayload(
            string recordId,
            string sourceId,
            string detailId,
            string title,
            string detail)
        {
            RecordId = recordId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            DetailId = detailId ?? string.Empty;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string RecordId { get; }
        public string SourceId { get; }
        public string DetailId { get; }
        public string Title { get; }
        public string Detail { get; }
    }

    public sealed class LifeChapterRewardPayload
    {
        internal LifeChapterRewardPayload(
            string receiptId,
            LifeChapterRewardKind kind,
            int amount)
        {
            ReceiptId = receiptId ?? string.Empty;
            Kind = kind;
            Amount = Math.Max(0, amount);
        }

        public string ReceiptId { get; }
        public LifeChapterRewardKind Kind { get; }
        public int Amount { get; }
        public bool HasReward => Kind != LifeChapterRewardKind.None && Amount > 0;
    }

    public sealed class LifeChapterChoicePayloadBundle
    {
        internal LifeChapterChoicePayloadBundle(
            string chapterId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt,
            string userMessage,
            LifeChapterRecordPayload memory,
            LifeChapterRecordPayload codex,
            LifeChapterRewardPayload reward)
        {
            ChapterId = chapterId ?? string.Empty;
            ChoiceId = choiceId ?? string.Empty;
            ReceiptId = receiptId ?? string.Empty;
            CompletedAt = completedAt;
            UserMessage = userMessage ?? string.Empty;
            Memory = memory;
            Codex = codex;
            Reward = reward;
        }

        public string ChapterId { get; }
        public string ChoiceId { get; }
        public string ReceiptId { get; }
        public DateTimeOffset CompletedAt { get; }
        public string UserMessage { get; }
        public LifeChapterRecordPayload Memory { get; }
        public LifeChapterRecordPayload Codex { get; }
        public LifeChapterRewardPayload Reward { get; }
    }

    public enum LifeChapterActionStatus
    {
        Recorded = 0,
        MissingState = 1,
        ContentUnavailable = 2,
        InvalidClock = 3,
        ClockRollback = 4,
        UnknownAction = 5,
        NoPendingChapter = 6,
        ActionNotRequired = 7,
        AlreadyRecorded = 8,
        StateCapacityFull = 9
    }

    public sealed class LifeChapterActionResult
    {
        internal LifeChapterActionResult(
            LifeChapterActionStatus status,
            bool stateChanged,
            string chapterId = "",
            string objectiveId = "",
            int completedObjectiveCount = 0,
            bool chapterReady = false)
        {
            Status = status;
            StateChanged = stateChanged;
            ChapterId = chapterId ?? string.Empty;
            ObjectiveId = objectiveId ?? string.Empty;
            CompletedObjectiveCount = Math.Max(0, completedObjectiveCount);
            ChapterReady = status == LifeChapterActionStatus.Recorded && chapterReady;
        }

        public LifeChapterActionStatus Status { get; }
        public bool Recorded => Status == LifeChapterActionStatus.Recorded;
        public bool StateChanged { get; }
        public string ChapterId { get; }
        public string ObjectiveId { get; }
        public int CompletedObjectiveCount { get; }
        public bool ChapterReady { get; }
    }

    public enum LifeChapterChoiceStatus
    {
        Applied = 0,
        MissingState = 1,
        ContentUnavailable = 2,
        InvalidClock = 3,
        ClockRollback = 4,
        InvalidReceipt = 5,
        DuplicateReceipt = 6,
        UnknownChapter = 7,
        Locked = 8,
        AlreadyCompleted = 9,
        ObjectivesIncomplete = 10,
        UnknownChoice = 11,
        StateCapacityFull = 12,
        MissingPayloadCommitter = 13,
        PayloadRejected = 14,
        PersistenceRejected = 15
    }

    public sealed class LifeChapterChoiceResult
    {
        internal LifeChapterChoiceResult(
            LifeChapterChoiceStatus status,
            bool stateChanged,
            LifeChapterChoicePayloadBundle payloads = null)
        {
            Status = status;
            StateChanged = stateChanged;
            Applied = status == LifeChapterChoiceStatus.Applied;
            Payloads = Applied ? payloads : null;
        }

        public LifeChapterChoiceStatus Status { get; }
        public bool Applied { get; }
        public bool StateChanged { get; }
        public LifeChapterChoicePayloadBundle Payloads { get; }
        public string ChapterId => Payloads?.ChapterId ?? string.Empty;
        public string ChoiceId => Payloads?.ChoiceId ?? string.Empty;
        public string ReceiptId => Payloads?.ReceiptId ?? string.Empty;
        public string UserMessage => Payloads?.UserMessage ?? string.Empty;
        public LifeChapterRecordPayload Memory => Payloads?.Memory;
        public LifeChapterRecordPayload Codex => Payloads?.Codex;
        public LifeChapterRewardPayload Reward => Payloads?.Reward;
    }

    /// <summary>
    /// Deterministic milestone story rules. The caller remains authoritative for the tama level,
    /// care action stream, memory journal, collection codex, economy, and final save write.
    /// </summary>
    public sealed class LifeChapterSystem
    {
        public const string MemoryRecordPrefix = "life_chapter_memory:";
        public const string CodexRecordPrefix = "life_chapter_codex:";
        public const string ReceiptPrefix = "life-chapter:";

        private readonly LifeChapterCatalog catalog;

        public LifeChapterSystem()
            : this(LifeChapterCatalog.Default)
        {
        }

        public LifeChapterSystem(LifeChapterCatalog catalog)
        {
            this.catalog = catalog ?? LifeChapterCatalog.Empty;
        }

        public bool ContentAvailable => catalog.HasContent;
        public IReadOnlyList<LifeChapterDefinition> Chapters => catalog.All;

        public static string BuildReceiptId(string chapterId)
        {
            return ReceiptPrefix + (chapterId ?? string.Empty).Trim();
        }

        public static bool IsSupportedCareAction(string actionId)
        {
            return TryResolveCareAction(actionId, out _);
        }

        public static bool TryResolveCodexRecord(
            string recordId,
            out LifeChapterRecordDefinition record)
        {
            record = null;
            var normalized = (recordId ?? string.Empty).Trim();
            if (!normalized.StartsWith(CodexRecordPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var payload = normalized.Substring(CodexRecordPrefix.Length);
            var separator = payload.IndexOf(':');
            if (separator <= 0 || separator + 1 >= payload.Length)
            {
                return false;
            }

            var chapterId = payload.Substring(0, separator);
            var choiceId = payload.Substring(separator + 1);
            record = LifeChapterCatalog.Default.Find(chapterId)?.FindChoice(choiceId)?.Codex;
            return record != null;
        }

        public bool NormalizeState(LifeChapterSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            var changed = false;
            if (state.completions == null)
            {
                state.completions = new List<LifeChapterCompletionSaveData>();
                changed = true;
            }

            if (state.progress == null)
            {
                state.progress = new List<LifeChapterProgressSaveData>();
                changed = true;
            }

            if (!catalog.HasContent)
            {
                return state.EnsureRuntimeDefaults() || changed;
            }

            changed |= NormalizeKnownCompletions(state);
            changed |= NormalizeKnownProgress(state);
            changed |= state.EnsureRuntimeDefaults();
            return changed;
        }

        public LifeChapterActionResult TryRecordCareAction(
            LifeChapterSaveData state,
            int currentLevel,
            string actionId,
            DateTimeOffset occurredAt)
        {
            if (state == null)
            {
                return ActionFailure(LifeChapterActionStatus.MissingState, false);
            }

            if (!catalog.HasContent)
            {
                return ActionFailure(
                    LifeChapterActionStatus.ContentUnavailable,
                    state.EnsureRuntimeDefaults());
            }

            var stateChanged = NormalizeState(state);
            var clockStatus = ValidateAndObserveKnownClock(
                state,
                occurredAt,
                out var clockStateChanged);
            stateChanged |= clockStateChanged;
            if (clockStatus == LifeChapterClockValidationStatus.InvalidNow)
            {
                return ActionFailure(LifeChapterActionStatus.InvalidClock, stateChanged);
            }

            if (clockStatus == LifeChapterClockValidationStatus.RollbackDetected)
            {
                return ActionFailure(LifeChapterActionStatus.ClockRollback, stateChanged);
            }

            if (!TryResolveCareAction(actionId, out var actionKind))
            {
                return ActionFailure(LifeChapterActionStatus.UnknownAction, stateChanged);
            }

            var chapter = FindPendingChapter(state, currentLevel);
            if (chapter == null)
            {
                return ActionFailure(LifeChapterActionStatus.NoPendingChapter, stateChanged);
            }

            var objective = chapter.FindObjective(actionKind);
            if (objective == null)
            {
                return ActionFailure(
                    LifeChapterActionStatus.ActionNotRequired,
                    stateChanged,
                    chapter.Id);
            }

            var progress = FindKnownProgress(state, chapter);
            if (progress?.HasCompletedObjective(objective.Id) == true)
            {
                return new LifeChapterActionResult(
                    LifeChapterActionStatus.AlreadyRecorded,
                    stateChanged,
                    chapter.Id,
                    objective.Id,
                    CountCompletedObjectives(chapter, progress),
                    HasCompletedEveryObjective(chapter, progress));
            }

            progress ??= GetOrCreateKnownProgress(state, chapter);
            if (progress == null)
            {
                return ActionFailure(
                    LifeChapterActionStatus.StateCapacityFull,
                    stateChanged,
                    chapter.Id);
            }

            progress.completedObjectiveIds.Add(objective.Id);
            OrderKnownObjectives(chapter, progress);
            stateChanged = true;
            var completedCount = CountCompletedObjectives(chapter, progress);
            return new LifeChapterActionResult(
                LifeChapterActionStatus.Recorded,
                stateChanged,
                chapter.Id,
                objective.Id,
                completedCount,
                completedCount == chapter.Objectives.Count);
        }

        public LifeChapterSnapshot BuildPendingSnapshot(
            LifeChapterSaveData state,
            int currentLevel,
            DateTimeOffset now)
        {
            return BuildPendingSnapshot(state, currentLevel, now, out _);
        }

        public LifeChapterSnapshot BuildPendingSnapshot(
            LifeChapterSaveData state,
            int currentLevel,
            DateTimeOffset now,
            out bool stateChanged)
        {
            stateChanged = false;
            if (state == null || !catalog.HasContent)
            {
                return LifeChapterSnapshot.CreateHidden();
            }

            stateChanged = NormalizeState(state);
            var clockStatus = ValidateAndObserveKnownClock(
                state,
                now,
                out var clockStateChanged);
            stateChanged |= clockStateChanged;
            if (clockStatus != LifeChapterClockValidationStatus.Valid)
            {
                return LifeChapterSnapshot.CreateHidden();
            }

            var chapter = FindPendingChapter(state, currentLevel);
            return LifeChapterSnapshot.CreatePending(
                chapter,
                FindKnownProgress(state, chapter));
        }

        /// <summary>
        /// Builds the spoiler-safe entry state without normalizing data or advancing the
        /// anti-rollback watermark. Use this for passive UI refreshes that may run while a
        /// larger authoritative action is still being committed.
        /// </summary>
        public LifeChapterSnapshot BuildPendingSnapshotReadOnly(
            LifeChapterSaveData state,
            int currentLevel)
        {
            if (state == null || !catalog.HasContent)
            {
                return LifeChapterSnapshot.CreateHidden();
            }

            var chapter = FindPendingChapter(state, currentLevel);
            return LifeChapterSnapshot.CreatePending(
                chapter,
                FindKnownProgress(state, chapter));
        }

        public LifeChapterChoiceResult TryChoose(
            LifeChapterSaveData state,
            int currentLevel,
            string chapterId,
            string choiceId,
            DateTimeOffset completedAt,
            Func<LifeChapterChoicePayloadBundle, bool> tryCommitPayloads)
        {
            return TryChoose(
                state,
                currentLevel,
                chapterId,
                choiceId,
                BuildReceiptId(chapterId),
                completedAt,
                tryCommitPayloads);
        }

        public LifeChapterChoiceResult TryChoose(
            LifeChapterSaveData state,
            int currentLevel,
            string chapterId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt,
            Func<LifeChapterChoicePayloadBundle, bool> tryCommitPayloads)
        {
            if (state == null)
            {
                return ChoiceFailure(LifeChapterChoiceStatus.MissingState, false);
            }

            if (!catalog.HasContent)
            {
                return ChoiceFailure(
                    LifeChapterChoiceStatus.ContentUnavailable,
                    state.EnsureRuntimeDefaults());
            }

            var stateChanged = NormalizeState(state);
            var clockStatus = ValidateAndObserveKnownClock(
                state,
                completedAt,
                out var clockStateChanged);
            stateChanged |= clockStateChanged;
            if (clockStatus == LifeChapterClockValidationStatus.InvalidNow)
            {
                return ChoiceFailure(LifeChapterChoiceStatus.InvalidClock, stateChanged);
            }

            if (clockStatus == LifeChapterClockValidationStatus.RollbackDetected)
            {
                return ChoiceFailure(LifeChapterChoiceStatus.ClockRollback, stateChanged);
            }

            var normalizedReceiptId = (receiptId ?? string.Empty).Trim();
            if (!LifeChapterSaveData.IsReceiptToken(normalizedReceiptId))
            {
                return ChoiceFailure(LifeChapterChoiceStatus.InvalidReceipt, stateChanged);
            }

            if (HasKnownReceipt(state, normalizedReceiptId))
            {
                return ChoiceFailure(LifeChapterChoiceStatus.DuplicateReceipt, stateChanged);
            }

            var chapter = catalog.Find(chapterId);
            if (chapter == null)
            {
                return ChoiceFailure(LifeChapterChoiceStatus.UnknownChapter, stateChanged);
            }

            if (HasKnownCompletedChapter(state, chapter.Id))
            {
                return ChoiceFailure(LifeChapterChoiceStatus.AlreadyCompleted, stateChanged);
            }

            var pendingChapter = FindPendingChapter(state, currentLevel);
            if (pendingChapter == null
                || !string.Equals(pendingChapter.Id, chapter.Id, StringComparison.Ordinal))
            {
                return ChoiceFailure(LifeChapterChoiceStatus.Locked, stateChanged);
            }

            var progress = FindKnownProgress(state, chapter);
            if (!HasCompletedEveryObjective(chapter, progress))
            {
                return ChoiceFailure(
                    LifeChapterChoiceStatus.ObjectivesIncomplete,
                    stateChanged);
            }

            var choice = chapter.FindChoice(choiceId);
            if (choice == null)
            {
                return ChoiceFailure(LifeChapterChoiceStatus.UnknownChoice, stateChanged);
            }

            if (!CanRecordKnownCompletion(state))
            {
                return ChoiceFailure(
                    LifeChapterChoiceStatus.StateCapacityFull,
                    stateChanged);
            }

            if (tryCommitPayloads == null)
            {
                return ChoiceFailure(
                    LifeChapterChoiceStatus.MissingPayloadCommitter,
                    stateChanged);
            }

            var memory = new LifeChapterRecordPayload(
                MemoryRecordPrefix + chapter.Id,
                chapter.Id,
                choice.Id,
                choice.Memory.Title,
                choice.Memory.Detail);
            var codex = new LifeChapterRecordPayload(
                CodexRecordPrefix + chapter.Id + ":" + choice.Id,
                chapter.Id,
                choice.Id,
                choice.Codex.Title,
                choice.Codex.Detail);
            var reward = new LifeChapterRewardPayload(
                normalizedReceiptId,
                choice.Reward.Kind,
                choice.Reward.Amount);
            var payloads = new LifeChapterChoicePayloadBundle(
                chapter.Id,
                choice.Id,
                normalizedReceiptId,
                completedAt,
                choice.ResultMessage,
                memory,
                codex,
                reward);

            // The callback owns the cross-store transaction. Completion and its receipt are not
            // persisted until memory, codex, and economy stores accept the same payload bundle.
            if (!tryCommitPayloads(payloads))
            {
                return ChoiceFailure(LifeChapterChoiceStatus.PayloadRejected, stateChanged);
            }

            if (!TryRecordKnownCompletion(
                state,
                chapter,
                new LifeChapterCompletionSaveData
                {
                    chapterId = chapter.Id,
                    choiceId = choice.Id,
                    receiptId = normalizedReceiptId,
                    completedAtIso = LifeChapterSaveData.FormatTimestamp(completedAt)
                }))
            {
                return ChoiceFailure(
                    LifeChapterChoiceStatus.PersistenceRejected,
                    stateChanged);
            }

            stateChanged = true;
            return new LifeChapterChoiceResult(
                LifeChapterChoiceStatus.Applied,
                stateChanged,
                payloads);
        }

        private bool NormalizeKnownCompletions(LifeChapterSaveData state)
        {
            var source = state.completions;
            var unknownNewestFirst = new List<LifeChapterCompletionSaveData>();
            var knownNewestFirst = new List<LifeChapterCompletionSaveData>();
            var knownChapterIds = new HashSet<string>(StringComparer.Ordinal);
            var knownReceiptIds = new HashSet<string>(StringComparer.Ordinal);
            var changed = false;

            for (var index = source.Count - 1; index >= 0; index -= 1)
            {
                var completion = source[index];
                if (completion == null)
                {
                    changed = true;
                    continue;
                }

                changed |= completion.EnsureRuntimeDefaults();
                if (!completion.HasValue
                    || !LifeChapterSaveData.IsStableToken(completion.chapterId)
                    || !LifeChapterSaveData.IsStableToken(completion.choiceId)
                    || !LifeChapterSaveData.IsReceiptToken(completion.receiptId))
                {
                    changed = true;
                    continue;
                }

                var chapter = catalog.Find(completion.chapterId);
                if (chapter == null)
                {
                    unknownNewestFirst.Add(completion);
                    continue;
                }

                if (chapter.FindChoice(completion.choiceId) == null
                    || !knownChapterIds.Add(chapter.Id)
                    || !knownReceiptIds.Add(completion.receiptId))
                {
                    changed = true;
                    continue;
                }

                knownNewestFirst.Add(completion);
            }

            unknownNewestFirst.Reverse();
            knownNewestFirst.Reverse();
            unknownNewestFirst.AddRange(knownNewestFirst);
            changed |= !HasSameReferences(source, unknownNewestFirst);
            state.completions = unknownNewestFirst;
            return changed;
        }

        private bool NormalizeKnownProgress(LifeChapterSaveData state)
        {
            var source = state.progress;
            var unknownEntries = new List<LifeChapterProgressSaveData>();
            var knownByChapter = new Dictionary<string, LifeChapterProgressSaveData>(
                StringComparer.Ordinal);
            var changed = false;

            for (var index = source.Count - 1; index >= 0; index -= 1)
            {
                var entry = source[index];
                if (entry == null)
                {
                    changed = true;
                    continue;
                }

                changed |= entry.EnsureRuntimeDefaults();
                if (!entry.HasValue || !LifeChapterSaveData.IsStableToken(entry.chapterId))
                {
                    changed = true;
                    continue;
                }

                var chapter = catalog.Find(entry.chapterId);
                if (chapter == null)
                {
                    unknownEntries.Add(entry);
                    continue;
                }

                if (HasKnownCompletedChapter(state, chapter.Id))
                {
                    changed = true;
                    continue;
                }

                changed |= FilterKnownObjectives(chapter, entry);
                if (!knownByChapter.TryGetValue(chapter.Id, out var retained))
                {
                    knownByChapter.Add(chapter.Id, entry);
                    continue;
                }

                for (var objectiveIndex = 0;
                    objectiveIndex < entry.completedObjectiveIds.Count;
                    objectiveIndex += 1)
                {
                    var objectiveId = entry.completedObjectiveIds[objectiveIndex];
                    if (!retained.HasCompletedObjective(objectiveId))
                    {
                        retained.completedObjectiveIds.Add(objectiveId);
                    }
                }

                changed = true;
            }

            unknownEntries.Reverse();
            for (var index = 0; index < catalog.All.Count; index += 1)
            {
                var chapter = catalog.All[index];
                if (knownByChapter.TryGetValue(chapter.Id, out var entry))
                {
                    changed |= OrderKnownObjectives(chapter, entry);
                    unknownEntries.Add(entry);
                }
            }

            changed |= !HasSameReferences(source, unknownEntries);
            state.progress = unknownEntries;
            return changed;
        }

        private LifeChapterDefinition FindPendingChapter(
            LifeChapterSaveData state,
            int currentLevel)
        {
            var safeLevel = Math.Max(0, currentLevel);
            for (var index = 0; index < catalog.All.Count; index += 1)
            {
                var chapter = catalog.All[index];
                if (HasKnownCompletedChapter(state, chapter.Id))
                {
                    continue;
                }

                return safeLevel >= chapter.MinimumLevel ? chapter : null;
            }

            return null;
        }

        private LifeChapterProgressSaveData FindKnownProgress(
            LifeChapterSaveData state,
            LifeChapterDefinition chapter)
        {
            if (state?.progress == null || chapter == null)
            {
                return null;
            }

            for (var index = state.progress.Count - 1; index >= 0; index -= 1)
            {
                var entry = state.progress[index];
                if (entry != null
                    && string.Equals(entry.chapterId, chapter.Id, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private LifeChapterProgressSaveData GetOrCreateKnownProgress(
            LifeChapterSaveData state,
            LifeChapterDefinition chapter)
        {
            var existing = FindKnownProgress(state, chapter);
            if (existing != null)
            {
                return existing;
            }

            while (state.progress.Count >= LifeChapterSaveData.MaximumProgressEntries)
            {
                var removableIndex = FindFirstUnknownProgressIndex(state);
                if (removableIndex < 0)
                {
                    return null;
                }

                state.progress.RemoveAt(removableIndex);
            }

            var created = new LifeChapterProgressSaveData { chapterId = chapter.Id };
            state.progress.Add(created);
            return created;
        }

        private int FindFirstUnknownProgressIndex(LifeChapterSaveData state)
        {
            for (var index = 0; index < state.progress.Count; index += 1)
            {
                var entry = state.progress[index];
                if (catalog.Find(entry?.chapterId) == null)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindFirstUnknownCompletionIndex(LifeChapterSaveData state)
        {
            for (var index = 0; index < state.completions.Count; index += 1)
            {
                var completion = state.completions[index];
                var chapter = catalog.Find(completion?.chapterId);
                if (chapter?.FindChoice(completion?.choiceId) == null)
                {
                    return index;
                }
            }

            return -1;
        }

        private LifeChapterCompletionSaveData FindKnownCompletion(
            LifeChapterSaveData state,
            LifeChapterDefinition chapter)
        {
            if (state?.completions == null || chapter == null)
            {
                return null;
            }

            for (var index = state.completions.Count - 1; index >= 0; index -= 1)
            {
                var completion = state.completions[index];
                if (completion != null
                    && string.Equals(completion.chapterId, chapter.Id, StringComparison.Ordinal)
                    && chapter.FindChoice(completion.choiceId) != null)
                {
                    return completion;
                }
            }

            return null;
        }

        private bool HasKnownCompletedChapter(
            LifeChapterSaveData state,
            string chapterId)
        {
            return FindKnownCompletion(state, catalog.Find(chapterId)) != null;
        }

        private bool HasKnownReceipt(LifeChapterSaveData state, string receiptId)
        {
            if (state?.completions == null || string.IsNullOrEmpty(receiptId))
            {
                return false;
            }

            for (var index = state.completions.Count - 1; index >= 0; index -= 1)
            {
                var completion = state.completions[index];
                var chapter = catalog.Find(completion?.chapterId);
                if (chapter?.FindChoice(completion?.choiceId) != null
                    && string.Equals(
                        completion.receiptId,
                        receiptId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanRecordKnownCompletion(LifeChapterSaveData state)
        {
            if (state?.completions == null)
            {
                return false;
            }

            var knownCount = 0;
            for (var index = 0; index < state.completions.Count; index += 1)
            {
                var completion = state.completions[index];
                var chapter = catalog.Find(completion?.chapterId);
                if (chapter?.FindChoice(completion?.choiceId) != null)
                {
                    knownCount += 1;
                }
            }

            return knownCount < LifeChapterSaveData.MaximumCompletionEntries;
        }

        private bool TryRecordKnownCompletion(
            LifeChapterSaveData state,
            LifeChapterDefinition chapter,
            LifeChapterCompletionSaveData completion)
        {
            if (state?.completions == null
                || chapter == null
                || completion == null
                || chapter.FindChoice(completion.choiceId) == null
                || HasKnownCompletedChapter(state, chapter.Id)
                || HasKnownReceipt(state, completion.receiptId))
            {
                return false;
            }

            for (var index = state.completions.Count - 1; index >= 0; index -= 1)
            {
                var existing = state.completions[index];
                var existingChapter = catalog.Find(existing?.chapterId);
                var existingIsKnown = existingChapter?.FindChoice(existing?.choiceId) != null;
                if (!existingIsKnown
                    && (string.Equals(
                            existing?.chapterId,
                            chapter.Id,
                            StringComparison.Ordinal)
                        || string.Equals(
                            existing?.receiptId,
                            completion.receiptId,
                            StringComparison.Ordinal)))
                {
                    state.completions.RemoveAt(index);
                }
            }

            while (state.completions.Count >= LifeChapterSaveData.MaximumCompletionEntries)
            {
                var removableIndex = FindFirstUnknownCompletionIndex(state);
                if (removableIndex < 0)
                {
                    return false;
                }

                state.completions.RemoveAt(removableIndex);
            }

            completion.EnsureRuntimeDefaults();
            state.completions.Add(completion);
            RemoveKnownProgress(state, chapter.Id);
            return true;
        }

        private static void RemoveKnownProgress(
            LifeChapterSaveData state,
            string chapterId)
        {
            for (var index = state.progress.Count - 1; index >= 0; index -= 1)
            {
                if (string.Equals(
                    state.progress[index]?.chapterId,
                    chapterId,
                    StringComparison.Ordinal))
                {
                    state.progress.RemoveAt(index);
                }
            }
        }

        private LifeChapterClockValidationStatus ValidateAndObserveKnownClock(
            LifeChapterSaveData state,
            DateTimeOffset now,
            out bool stateChanged)
        {
            return state.ValidateAndObserveClock(
                now,
                completion =>
                {
                    var chapter = catalog.Find(completion?.chapterId);
                    return chapter?.FindChoice(completion?.choiceId) != null;
                },
                out stateChanged);
        }

        private static int CountCompletedObjectives(
            LifeChapterDefinition chapter,
            LifeChapterProgressSaveData progress)
        {
            if (chapter == null || progress?.completedObjectiveIds == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < chapter.Objectives.Count; index += 1)
            {
                if (progress.HasCompletedObjective(chapter.Objectives[index].Id))
                {
                    count += 1;
                }
            }

            return count;
        }

        private static bool HasCompletedEveryObjective(
            LifeChapterDefinition chapter,
            LifeChapterProgressSaveData progress)
        {
            return chapter != null
                && chapter.Objectives.Count == LifeChapterCatalog.ObjectivesPerChapter
                && CountCompletedObjectives(chapter, progress) == chapter.Objectives.Count;
        }

        private static bool FilterKnownObjectives(
            LifeChapterDefinition chapter,
            LifeChapterProgressSaveData progress)
        {
            return OrderKnownObjectives(chapter, progress);
        }

        private static bool OrderKnownObjectives(
            LifeChapterDefinition chapter,
            LifeChapterProgressSaveData progress)
        {
            if (chapter == null || progress == null)
            {
                return false;
            }

            var ordered = new List<string>(chapter.Objectives.Count);
            for (var index = 0; index < chapter.Objectives.Count; index += 1)
            {
                var objectiveId = chapter.Objectives[index].Id;
                if (progress.HasCompletedObjective(objectiveId))
                {
                    ordered.Add(objectiveId);
                }
            }

            var changed = !HasSameValues(progress.completedObjectiveIds, ordered);
            progress.completedObjectiveIds = ordered;
            return changed;
        }

        private static bool HasSameReferences<T>(
            IReadOnlyList<T> left,
            IReadOnlyList<T> right)
            where T : class
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index += 1)
            {
                if (!ReferenceEquals(left[index], right[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasSameValues(
            IReadOnlyList<string> left,
            IReadOnlyList<string> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index += 1)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryResolveCareAction(
            string actionId,
            out LifeChapterCareActionKind actionKind)
        {
            switch ((actionId ?? string.Empty).Trim())
            {
                case "feed_milk":
                case "feed_warm_milk":
                case "feed_cold_milk":
                case "feed_nutty_milk":
                case "feed_rich_milk":
                case "feed_fermented_milk":
                case "feed_coffee_milk":
                case "feed_star_milk":
                case "feed_snack":
                    actionKind = LifeChapterCareActionKind.Feed;
                    return true;
                case "cook":
                case "blend":
                    actionKind = LifeChapterCareActionKind.Cook;
                    return true;
                case "play":
                case "pet":
                    actionKind = LifeChapterCareActionKind.Play;
                    return true;
                case "clean":
                    actionKind = LifeChapterCareActionKind.Clean;
                    return true;
                case "rest":
                    actionKind = LifeChapterCareActionKind.Rest;
                    return true;
                default:
                    actionKind = default;
                    return false;
            }
        }

        private static LifeChapterActionResult ActionFailure(
            LifeChapterActionStatus status,
            bool stateChanged,
            string chapterId = "")
        {
            return new LifeChapterActionResult(
                status,
                stateChanged,
                chapterId);
        }

        private static LifeChapterChoiceResult ChoiceFailure(
            LifeChapterChoiceStatus status,
            bool stateChanged)
        {
            return new LifeChapterChoiceResult(status, stateChanged);
        }
    }
}
