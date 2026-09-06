using System;
using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Story
{
    /// <summary>
    /// Read-only trigger context copied from the existing authoritative sleep, NPC visit, and
    /// milkroom mystery receipt stores. Signal IDs are explicit one-way discoveries such as a
    /// label-error observation; they are never inferred from UI state.
    /// </summary>
    public sealed class DreamStoryProgress
    {
        private readonly HashSet<string> externalStoryCompletionIds;
        private readonly HashSet<string> signalIds;
        private readonly HashSet<string> dreamEpisodeCompletionIds;

        public DreamStoryProgress(
            int completedSleepReceipts,
            int completedNpcVisitReceipts,
            IEnumerable<string> completedExternalStoryIds,
            IEnumerable<string> discoveredSignalIds)
            : this(
                completedSleepReceipts,
                completedNpcVisitReceipts,
                completedExternalStoryIds,
                discoveredSignalIds,
                null)
        {
        }

        public DreamStoryProgress(
            int completedSleepReceipts,
            int completedNpcVisitReceipts,
            IEnumerable<string> completedExternalStoryIds,
            IEnumerable<string> discoveredSignalIds,
            IEnumerable<string> completedDreamEpisodeIds)
        {
            CompletedSleepReceipts = Math.Max(0, completedSleepReceipts);
            CompletedNpcVisitReceipts = Math.Max(0, completedNpcVisitReceipts);
            externalStoryCompletionIds = CopyStableIds(completedExternalStoryIds);
            signalIds = CopyStableIds(discoveredSignalIds);
            dreamEpisodeCompletionIds = CopyStableIds(completedDreamEpisodeIds);
        }

        public int CompletedSleepReceipts { get; }
        public int CompletedNpcVisitReceipts { get; }

        public bool HasExternalStoryCompletion(string storyId)
        {
            return externalStoryCompletionIds.Contains((storyId ?? string.Empty).Trim());
        }

        public bool HasSignal(string signalId)
        {
            return signalIds.Contains((signalId ?? string.Empty).Trim());
        }

        public bool HasDreamEpisodeCompletion(string episodeId)
        {
            return dreamEpisodeCompletionIds.Contains((episodeId ?? string.Empty).Trim());
        }

        public static DreamStoryProgress FromAuthoritativeSaves(
            SleepScheduleSaveData sleep,
            NpcVisitSaveData npcVisits,
            MilkroomMysterySaveData milkroomMystery,
            IEnumerable<string> discoveredSignalIds = null)
        {
            sleep?.EnsureRuntimeDefaults();
            npcVisits?.EnsureRuntimeDefaults();
            milkroomMystery?.EnsureRuntimeDefaults();

            var completedExternalStoryIds = new List<string>();
            if (milkroomMystery?.completions != null)
            {
                for (var index = 0; index < milkroomMystery.completions.Count; index += 1)
                {
                    var episodeId = milkroomMystery.completions[index]?.chapterId;
                    if (!string.IsNullOrWhiteSpace(episodeId))
                    {
                        completedExternalStoryIds.Add(episodeId);
                    }
                }
            }

            return new DreamStoryProgress(
                sleep?.recoveryReceipts?.Count ?? 0,
                npcVisits?.receipts?.Count ?? 0,
                completedExternalStoryIds,
                discoveredSignalIds);
        }

        public static DreamStoryProgress FromAuthoritativeSavesAndSeasonState(
            SleepScheduleSaveData sleep,
            NpcVisitSaveData npcVisits,
            MilkroomMysterySaveData milkroomMystery,
            DreamStorySeasonSaveData seasonState)
        {
            seasonState?.EnsureRuntimeDefaults();
            var completedDreamEpisodes = new List<string>();
            if (seasonState?.completions != null)
            {
                for (var index = 0; index < seasonState.completions.Count; index += 1)
                {
                    var episodeId = seasonState.completions[index]?.episodeId;
                    if (!string.IsNullOrWhiteSpace(episodeId))
                    {
                        completedDreamEpisodes.Add(episodeId);
                    }
                }
            }

            var baseProgress = FromAuthoritativeSaves(
                sleep,
                npcVisits,
                milkroomMystery,
                seasonState?.discoveredSignalIds);
            return new DreamStoryProgress(
                baseProgress.CompletedSleepReceipts,
                baseProgress.CompletedNpcVisitReceipts,
                completedExternalStoryIds: GetExternalStoryCompletionIds(milkroomMystery),
                discoveredSignalIds: seasonState?.discoveredSignalIds,
                completedDreamEpisodeIds: completedDreamEpisodes);
        }

        private static IEnumerable<string> GetExternalStoryCompletionIds(
            MilkroomMysterySaveData milkroomMystery)
        {
            if (milkroomMystery?.completions == null)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            for (var index = 0; index < milkroomMystery.completions.Count; index += 1)
            {
                var chapterId = milkroomMystery.completions[index]?.chapterId;
                if (!string.IsNullOrWhiteSpace(chapterId))
                {
                    result.Add(chapterId);
                }
            }

            return result;
        }

        private static HashSet<string> CopyStableIds(IEnumerable<string> source)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (source == null)
            {
                return result;
            }

            foreach (var value in source)
            {
                var normalized = (value ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(normalized))
                {
                    result.Add(normalized);
                }
            }

            return result;
        }
    }

    public sealed class DreamStoryChoiceView
    {
        internal DreamStoryChoiceView(string id, string label)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
    }

    /// <summary>
    /// Spoiler-safe public presentation. Hidden snapshots contain no title, prerequisite,
    /// selection, or season-size metadata. Recall snapshots contain only the recorded branch.
    /// </summary>
    public sealed class DreamStorySnapshot
    {
        private readonly DreamStoryChoiceView[] choices;

        private DreamStorySnapshot(
            bool visible,
            bool isRecall,
            string episodeId,
            DreamStoryEpisodeKind episodeKind,
            string title,
            string body,
            string resultMessage,
            DreamStoryChoiceView[] choices)
        {
            Visible = visible;
            IsRecall = visible && isRecall;
            EpisodeId = visible ? episodeId ?? string.Empty : string.Empty;
            EpisodeKind = visible ? episodeKind : default;
            Title = visible ? title ?? string.Empty : string.Empty;
            Body = visible ? body ?? string.Empty : string.Empty;
            ResultMessage = visible && isRecall
                ? resultMessage ?? string.Empty
                : string.Empty;
            this.choices = visible && !isRecall
                ? choices ?? Array.Empty<DreamStoryChoiceView>()
                : Array.Empty<DreamStoryChoiceView>();
        }

        public bool Visible { get; }
        public bool IsRecall { get; }
        public string EpisodeId { get; }
        public DreamStoryEpisodeKind EpisodeKind { get; }
        public string Title { get; }
        public string Body { get; }
        public string ResultMessage { get; }
        public IReadOnlyList<DreamStoryChoiceView> Choices => choices;
        public bool CanChoose => Visible
            && !IsRecall
            && choices.Length == DreamStorySeasonContentLoader.ChoicesPerEpisode;

        internal static DreamStorySnapshot CreatePending(
            DreamStoryEpisodeDefinition episode)
        {
            if (episode == null)
            {
                return CreateHidden();
            }

            var choiceViews = new DreamStoryChoiceView[episode.Choices.Count];
            for (var index = 0; index < episode.Choices.Count; index += 1)
            {
                choiceViews[index] = new DreamStoryChoiceView(
                    episode.Choices[index].Id,
                    episode.Choices[index].Label);
            }

            return new DreamStorySnapshot(
                true,
                false,
                episode.Id,
                episode.Kind,
                episode.Title,
                episode.Body,
                string.Empty,
                choiceViews);
        }

        internal static DreamStorySnapshot CreateRecall(
            DreamStoryEpisodeDefinition episode,
            DreamStoryChoiceDefinition selectedChoice)
        {
            if (episode == null || selectedChoice == null)
            {
                return CreateHidden();
            }

            return new DreamStorySnapshot(
                true,
                true,
                episode.Id,
                episode.Kind,
                episode.Title,
                episode.Body,
                selectedChoice.ResultMessage,
                Array.Empty<DreamStoryChoiceView>());
        }

        public static DreamStorySnapshot CreateHidden()
        {
            return new DreamStorySnapshot(
                false,
                false,
                string.Empty,
                default,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<DreamStoryChoiceView>());
        }
    }

    public sealed class DreamStoryRecallEntry
    {
        internal DreamStoryRecallEntry(
            string episodeId,
            DreamStoryEpisodeKind episodeKind,
            string title)
        {
            EpisodeId = episodeId ?? string.Empty;
            EpisodeKind = episodeKind;
            Title = title ?? string.Empty;
        }

        public string EpisodeId { get; }
        public DreamStoryEpisodeKind EpisodeKind { get; }
        public string Title { get; }
    }

    public sealed class DreamStoryRecordPayload
    {
        internal DreamStoryRecordPayload(
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

    public sealed class DreamStoryRewardPayload
    {
        internal DreamStoryRewardPayload(
            string receiptId,
            DreamStoryRewardKind kind,
            int amount)
        {
            ReceiptId = receiptId ?? string.Empty;
            Kind = kind;
            Amount = Math.Max(0, amount);
        }

        public string ReceiptId { get; }
        public DreamStoryRewardKind Kind { get; }
        public int Amount { get; }
        public bool HasReward => Kind != DreamStoryRewardKind.None && Amount > 0;
    }

    public sealed class DreamStoryChoicePayloadBundle
    {
        internal DreamStoryChoicePayloadBundle(
            string episodeId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt,
            string userMessage,
            DreamStoryRecordPayload memory,
            DreamStoryRecordPayload codex,
            DreamStoryRewardPayload reward)
        {
            EpisodeId = episodeId ?? string.Empty;
            ChoiceId = choiceId ?? string.Empty;
            ReceiptId = receiptId ?? string.Empty;
            CompletedAt = completedAt;
            UserMessage = userMessage ?? string.Empty;
            Memory = memory;
            Codex = codex;
            Reward = reward;
        }

        public string EpisodeId { get; }
        public string ChoiceId { get; }
        public string ReceiptId { get; }
        public DateTimeOffset CompletedAt { get; }
        public string UserMessage { get; }
        public DreamStoryRecordPayload Memory { get; }
        public DreamStoryRecordPayload Codex { get; }
        public DreamStoryRewardPayload Reward { get; }
    }

    public enum DreamStoryChoiceStatus
    {
        Applied = 0,
        MissingState = 1,
        ContentUnavailable = 2,
        InvalidReceipt = 3,
        DuplicateReceipt = 4,
        InvalidClock = 5,
        ClockRollback = 6,
        UnknownEpisode = 7,
        Locked = 8,
        AlreadyCompleted = 9,
        UnknownChoice = 10,
        StateCapacityFull = 11,
        MissingPayloadCommitter = 12,
        PayloadRejected = 13,
        PersistenceRejected = 14
    }

    public sealed class DreamStoryChoiceResult
    {
        internal DreamStoryChoiceResult(
            DreamStoryChoiceStatus status,
            DreamStoryChoicePayloadBundle payloads = null)
        {
            Status = status;
            Applied = status == DreamStoryChoiceStatus.Applied;
            Payloads = Applied ? payloads : null;
        }

        public DreamStoryChoiceStatus Status { get; }
        public bool Applied { get; }
        public DreamStoryChoicePayloadBundle Payloads { get; }
        public string EpisodeId => Payloads?.EpisodeId ?? string.Empty;
        public string ChoiceId => Payloads?.ChoiceId ?? string.Empty;
        public string ReceiptId => Payloads?.ReceiptId ?? string.Empty;
        public string UserMessage => Payloads?.UserMessage ?? string.Empty;
        public DreamStoryRecordPayload Memory => Payloads?.Memory;
        public DreamStoryRecordPayload Codex => Payloads?.Codex;
        public DreamStoryRewardPayload Reward => Payloads?.Reward;
    }

    /// <summary>
    /// Owns deterministic visibility, branch completion, recall, monotonic-clock checks, and
    /// one-time receipts. The caller commits memory, collection, and economy payloads to the
    /// existing authoritative stores before this system records completion.
    /// </summary>
    public sealed class DreamStorySeasonSystem
    {
        private const string MemoryRecordPrefix = "dream_story_memory:";
        private readonly DreamStorySeasonCatalog catalog;

        public DreamStorySeasonSystem()
        {
            if (!DreamStorySeasonContentLoader.TryReloadAll(
                out var loadedCatalog,
                out _))
            {
                loadedCatalog = DreamStorySeasonCatalog.Empty;
            }

            catalog = loadedCatalog;
        }

        public DreamStorySeasonSystem(DreamStorySeasonCatalog catalog)
        {
            this.catalog = catalog ?? DreamStorySeasonCatalog.Empty;
        }

        public bool ContentAvailable => catalog.HasContent;

        /// <summary>
        /// Repairs entries that claim a current-season episode with a choice that does not exist.
        /// Truly unknown episode and signal identifiers are kept for forward-compatible round trips,
        /// but current-season runtime decisions ignore them.
        /// </summary>
        public bool NormalizeState(DreamStorySeasonSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            var changed = false;
            if (!catalog.HasContent)
            {
                return state.EnsureRuntimeDefaults();
            }

            if (state.completions != null)
            {
                var unknownCompletions = new List<DreamStoryCompletionSaveData>();
                var knownNewestFirst = new List<DreamStoryCompletionSaveData>();
                var knownEpisodeIds = new HashSet<string>(StringComparer.Ordinal);
                for (var index = state.completions.Count - 1; index >= 0; index -= 1)
                {
                    var completion = state.completions[index];
                    var episode = catalog.Find(completion?.episodeId);
                    if (episode == null)
                    {
                        continue;
                    }

                    if (completion != null
                        && completion.HasValue
                        && DreamStorySeasonSaveData.IsReceiptToken(
                            completion.receiptId)
                        && DreamStorySeasonSaveData.TryParseTimestamp(
                            completion.completedAtIso,
                            out _)
                        && episode.FindChoice(completion.choiceId) != null
                        && knownEpisodeIds.Add(episode.Id))
                    {
                        knownNewestFirst.Add(completion);
                    }
                    else
                    {
                        changed = true;
                    }
                }

                for (var index = 0; index < state.completions.Count; index += 1)
                {
                    var completion = state.completions[index];
                    if (catalog.Find(completion?.episodeId) == null)
                    {
                        unknownCompletions.Add(completion);
                    }
                }

                // Generic bounded normalization keeps the newest entries. Put current-season valid
                // completions last so a newer unknown item cannot evict or deduplicate them first.
                knownNewestFirst.Reverse();
                unknownCompletions.AddRange(knownNewestFirst);
                changed |= !HasSameReferences(state.completions, unknownCompletions);
                state.completions = unknownCompletions;
            }

            if (state.discoveredSignalIds != null)
            {
                var knownSignals = new List<string>();
                var unknownSignals = new List<string>();
                for (var index = 0; index < state.discoveredSignalIds.Count; index += 1)
                {
                    var normalized = DreamStorySeasonSaveData.NormalizeToken(
                        state.discoveredSignalIds[index]);
                    if (IsKnownSignal(normalized))
                    {
                        knownSignals.Add(state.discoveredSignalIds[index]);
                    }
                    else
                    {
                        unknownSignals.Add(state.discoveredSignalIds[index]);
                    }
                }

                // Signal normalization keeps the oldest entries, so reserve current signals first.
                knownSignals.AddRange(unknownSignals);
                changed |= !HasSameValues(state.discoveredSignalIds, knownSignals);
                state.discoveredSignalIds = knownSignals;
            }

            changed |= state.EnsureRuntimeDefaults();
            return changed;
        }

        public bool TryDiscoverSignal(
            DreamStorySeasonSaveData state,
            string signalId)
        {
            if (state == null || !catalog.HasContent)
            {
                return false;
            }

            NormalizeState(state);
            var normalizedSignalId = DreamStorySeasonSaveData.NormalizeToken(signalId);
            if (!IsKnownSignal(normalizedSignalId) || state.HasSignal(normalizedSignalId))
            {
                return false;
            }

            while (state.discoveredSignalIds.Count >= DreamStorySeasonSaveData.MaximumSignals)
            {
                var removableIndex = FindFirstUnknownSignalIndex(state);
                if (removableIndex < 0)
                {
                    return false;
                }

                state.discoveredSignalIds.RemoveAt(removableIndex);
            }

            state.discoveredSignalIds.Add(normalizedSignalId);
            return true;
        }

        public DreamStorySnapshot BuildPendingSnapshot(
            DreamStorySeasonSaveData state,
            DreamStoryProgress progress,
            DateTimeOffset now)
        {
            return BuildPendingSnapshot(state, progress, now, out _);
        }

        public DreamStorySnapshot BuildPendingSnapshot(
            DreamStorySeasonSaveData state,
            DreamStoryProgress progress,
            DateTimeOffset now,
            out bool stateChanged)
        {
            stateChanged = false;
            if (state == null || progress == null || !catalog.HasContent)
            {
                return DreamStorySnapshot.CreateHidden();
            }

            stateChanged = NormalizeState(state);
            if (ValidateAndObserveKnownClock(state, now, out var clockStateChanged)
                != DreamStoryClockValidationStatus.Valid)
            {
                stateChanged |= clockStateChanged;
                return DreamStorySnapshot.CreateHidden();
            }

            stateChanged |= clockStateChanged;

            for (var index = 0; index < catalog.Episodes.Count; index += 1)
            {
                var episode = catalog.Episodes[index];
                if (HasKnownCompletedEpisode(state, episode.Id))
                {
                    continue;
                }

                if (IsUnlocked(episode, state, progress))
                {
                    return DreamStorySnapshot.CreatePending(episode);
                }
            }

            return DreamStorySnapshot.CreateHidden();
        }

        public IReadOnlyList<DreamStoryRecallEntry> BuildRecallIndex(
            DreamStorySeasonSaveData state,
            DateTimeOffset now)
        {
            return BuildRecallIndex(state, now, out _);
        }

        public IReadOnlyList<DreamStoryRecallEntry> BuildRecallIndex(
            DreamStorySeasonSaveData state,
            DateTimeOffset now,
            out bool stateChanged)
        {
            var result = new List<DreamStoryRecallEntry>();
            stateChanged = false;
            if (state == null || !catalog.HasContent)
            {
                return result;
            }

            stateChanged = NormalizeState(state);
            if (ValidateAndObserveKnownClock(state, now, out var clockStateChanged)
                != DreamStoryClockValidationStatus.Valid)
            {
                stateChanged |= clockStateChanged;
                return result;
            }

            stateChanged |= clockStateChanged;

            for (var index = 0; index < catalog.Episodes.Count; index += 1)
            {
                var episode = catalog.Episodes[index];
                if (HasKnownCompletedEpisode(state, episode.Id))
                {
                    result.Add(new DreamStoryRecallEntry(
                        episode.Id,
                        episode.Kind,
                        episode.Title));
                }
            }

            return result;
        }

        public DreamStorySnapshot BuildRecallSnapshot(
            DreamStorySeasonSaveData state,
            string episodeId,
            DateTimeOffset now)
        {
            return BuildRecallSnapshot(state, episodeId, now, out _);
        }

        public DreamStorySnapshot BuildRecallSnapshot(
            DreamStorySeasonSaveData state,
            string episodeId,
            DateTimeOffset now,
            out bool stateChanged)
        {
            stateChanged = false;
            if (state == null || !catalog.HasContent)
            {
                return DreamStorySnapshot.CreateHidden();
            }

            stateChanged = NormalizeState(state);
            if (ValidateAndObserveKnownClock(state, now, out var clockStateChanged)
                != DreamStoryClockValidationStatus.Valid)
            {
                stateChanged |= clockStateChanged;
                return DreamStorySnapshot.CreateHidden();
            }

            stateChanged |= clockStateChanged;

            var episode = catalog.Find(episodeId);
            var completion = FindKnownCompletion(state, episode);
            var selectedChoice = episode?.FindChoice(completion?.choiceId);
            return DreamStorySnapshot.CreateRecall(episode, selectedChoice);
        }

        public DreamStoryChoiceResult TryChoose(
            DreamStorySeasonSaveData state,
            DreamStoryProgress progress,
            string episodeId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt,
            Func<DreamStoryChoicePayloadBundle, bool> tryCommitPayloads)
        {
            if (state == null)
            {
                return Failure(DreamStoryChoiceStatus.MissingState);
            }

            if (!catalog.HasContent)
            {
                return Failure(DreamStoryChoiceStatus.ContentUnavailable);
            }

            NormalizeState(state);
            var clockStatus = ValidateAndObserveKnownClock(state, completedAt, out _);
            if (clockStatus == DreamStoryClockValidationStatus.InvalidNow)
            {
                return Failure(DreamStoryChoiceStatus.InvalidClock);
            }

            if (clockStatus == DreamStoryClockValidationStatus.RollbackDetected)
            {
                return Failure(DreamStoryChoiceStatus.ClockRollback);
            }

            var normalizedReceiptId = (receiptId ?? string.Empty).Trim();
            if (!DreamStorySeasonSaveData.IsReceiptToken(normalizedReceiptId))
            {
                return Failure(DreamStoryChoiceStatus.InvalidReceipt);
            }

            if (HasKnownReceipt(state, normalizedReceiptId))
            {
                return Failure(DreamStoryChoiceStatus.DuplicateReceipt);
            }

            var episode = catalog.Find(episodeId);
            if (episode == null)
            {
                return Failure(DreamStoryChoiceStatus.UnknownEpisode);
            }

            if (HasKnownCompletedEpisode(state, episode.Id))
            {
                return Failure(DreamStoryChoiceStatus.AlreadyCompleted);
            }

            if (progress == null || !IsUnlocked(episode, state, progress))
            {
                return Failure(DreamStoryChoiceStatus.Locked);
            }

            var choice = episode.FindChoice(choiceId);
            if (choice == null)
            {
                return Failure(DreamStoryChoiceStatus.UnknownChoice);
            }

            if (!CanRecordKnownCompletion(state))
            {
                return Failure(DreamStoryChoiceStatus.StateCapacityFull);
            }

            if (tryCommitPayloads == null)
            {
                return Failure(DreamStoryChoiceStatus.MissingPayloadCommitter);
            }

            var memory = new DreamStoryRecordPayload(
                MemoryRecordPrefix + episode.Id,
                episode.Id,
                choice.Id,
                choice.Memory.Title,
                choice.Memory.Detail);
            var codex = new DreamStoryRecordPayload(
                DreamStorySeasonContentLoader.BuildCodexRecordId(
                    episode.Id,
                    choice.Id),
                episode.Id,
                choice.Id,
                choice.Codex.Title,
                choice.Codex.Detail);
            var reward = new DreamStoryRewardPayload(
                normalizedReceiptId,
                choice.Reward.Kind,
                choice.Reward.Amount);
            var payloads = new DreamStoryChoicePayloadBundle(
                episode.Id,
                choice.Id,
                normalizedReceiptId,
                completedAt,
                choice.ResultMessage,
                memory,
                codex,
                reward);

            // The callback owns the cross-store transaction. No receipt is persisted until memory,
            // collection, and optional reward stores all accept this bundle exactly once.
            if (!tryCommitPayloads(payloads))
            {
                return Failure(DreamStoryChoiceStatus.PayloadRejected);
            }

            if (!TryRecordKnownCompletion(state, new DreamStoryCompletionSaveData
            {
                episodeId = episode.Id,
                choiceId = choice.Id,
                receiptId = normalizedReceiptId,
                completedAtIso = DreamStorySeasonSaveData.FormatTimestamp(completedAt)
            }))
            {
                return Failure(DreamStoryChoiceStatus.PersistenceRejected);
            }

            return new DreamStoryChoiceResult(DreamStoryChoiceStatus.Applied, payloads);
        }

        private bool IsUnlocked(
            DreamStoryEpisodeDefinition episode,
            DreamStorySeasonSaveData state,
            DreamStoryProgress progress)
        {
            if (episode == null || state == null || progress == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(episode.PrerequisiteEpisodeId)
                && !HasKnownCompletedEpisode(state, episode.PrerequisiteEpisodeId))
            {
                return false;
            }

            return episode.Trigger.IsSatisfied(progress);
        }

        private bool IsKnownSignal(string signalId)
        {
            if (string.IsNullOrEmpty(signalId))
            {
                return false;
            }

            for (var index = 0; index < catalog.Episodes.Count; index += 1)
            {
                var trigger = catalog.Episodes[index].Trigger;
                if (trigger.Kind == DreamStoryTriggerKind.Signal
                    && string.Equals(trigger.RequiredId, signalId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private int FindFirstUnknownSignalIndex(DreamStorySeasonSaveData state)
        {
            for (var index = 0; index < state.discoveredSignalIds.Count; index += 1)
            {
                if (!IsKnownSignal(state.discoveredSignalIds[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool HasSameReferences(
            IReadOnlyList<DreamStoryCompletionSaveData> left,
            IReadOnlyList<DreamStoryCompletionSaveData> right)
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

        private DreamStoryCompletionSaveData FindKnownCompletion(
            DreamStorySeasonSaveData state,
            DreamStoryEpisodeDefinition episode)
        {
            if (state?.completions == null || episode == null)
            {
                return null;
            }

            for (var index = state.completions.Count - 1; index >= 0; index -= 1)
            {
                var completion = state.completions[index];
                if (completion != null
                    && string.Equals(completion.episodeId, episode.Id, StringComparison.Ordinal)
                    && episode.FindChoice(completion.choiceId) != null)
                {
                    return completion;
                }
            }

            return null;
        }

        private bool HasKnownCompletedEpisode(
            DreamStorySeasonSaveData state,
            string episodeId)
        {
            var episode = catalog.Find(episodeId);
            return FindKnownCompletion(state, episode) != null;
        }

        private bool HasKnownReceipt(
            DreamStorySeasonSaveData state,
            string receiptId)
        {
            if (state?.completions == null || string.IsNullOrEmpty(receiptId))
            {
                return false;
            }

            for (var index = state.completions.Count - 1; index >= 0; index -= 1)
            {
                var completion = state.completions[index];
                var episode = catalog.Find(completion?.episodeId);
                if (FindKnownCompletion(state, episode) == completion
                    && string.Equals(completion.receiptId, receiptId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanRecordKnownCompletion(DreamStorySeasonSaveData state)
        {
            if (state?.completions == null)
            {
                return false;
            }

            var knownEpisodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < state.completions.Count; index += 1)
            {
                var completion = state.completions[index];
                var episode = catalog.Find(completion?.episodeId);
                if (episode != null && episode.FindChoice(completion.choiceId) != null)
                {
                    knownEpisodeIds.Add(episode.Id);
                }
            }

            return knownEpisodeIds.Count < DreamStorySeasonSaveData.MaximumCompletions;
        }

        private DreamStoryClockValidationStatus ValidateAndObserveKnownClock(
            DreamStorySeasonSaveData state,
            DateTimeOffset now,
            out bool stateChanged)
        {
            return state.ValidateAndObserveClock(
                now,
                completion =>
                {
                    var episode = catalog.Find(completion?.episodeId);
                    return episode?.FindChoice(completion?.choiceId) != null;
                },
                out stateChanged);
        }

        private bool TryRecordKnownCompletion(
            DreamStorySeasonSaveData state,
            DreamStoryCompletionSaveData completion)
        {
            if (state?.completions == null || completion == null)
            {
                return false;
            }

            completion.EnsureRuntimeDefaults();
            var episode = catalog.Find(completion.episodeId);
            if (episode?.FindChoice(completion.choiceId) == null
                || HasKnownCompletedEpisode(state, completion.episodeId)
                || HasKnownReceipt(state, completion.receiptId))
            {
                return false;
            }

            // A forward-version entry is preserved until it conflicts with current content or the
            // bounded list needs one slot. Current content must never be soft-locked by that entry.
            for (var index = state.completions.Count - 1; index >= 0; index -= 1)
            {
                var existing = state.completions[index];
                var existingEpisode = catalog.Find(existing?.episodeId);
                var existingIsKnown = existingEpisode?.FindChoice(existing?.choiceId) != null;
                if (!existingIsKnown
                    && (string.Equals(existing?.receiptId, completion.receiptId, StringComparison.Ordinal)
                        || string.Equals(existing?.episodeId, completion.episodeId, StringComparison.Ordinal)))
                {
                    state.completions.RemoveAt(index);
                }
            }

            while (state.completions.Count >= DreamStorySeasonSaveData.MaximumCompletions)
            {
                var removableIndex = -1;
                for (var index = 0; index < state.completions.Count; index += 1)
                {
                    var existing = state.completions[index];
                    var existingEpisode = catalog.Find(existing?.episodeId);
                    if (existingEpisode?.FindChoice(existing?.choiceId) == null)
                    {
                        removableIndex = index;
                        break;
                    }
                }

                if (removableIndex < 0)
                {
                    return false;
                }

                state.completions.RemoveAt(removableIndex);
            }

            state.completions.Add(completion);
            return true;
        }

        private static DreamStoryChoiceResult Failure(DreamStoryChoiceStatus status)
        {
            return new DreamStoryChoiceResult(status);
        }
    }
}
