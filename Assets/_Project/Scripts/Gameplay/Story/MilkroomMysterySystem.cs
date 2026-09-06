using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Story
{
    public readonly struct MilkroomMysteryProgress
    {
        public MilkroomMysteryProgress(int level, bool starRouteUnlocked)
        {
            Level = Math.Max(1, level);
            StarRouteUnlocked = starRouteUnlocked;
        }

        public int Level { get; }
        public bool StarRouteUnlocked { get; }
    }

    public sealed class MilkroomMysteryChoiceView
    {
        internal MilkroomMysteryChoiceView(string id, string label)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
    }

    public sealed class MilkroomMysterySnapshot
    {
        private readonly MilkroomMysteryChoiceView[] choices;

        private MilkroomMysterySnapshot(
            bool visible,
            string chapterId,
            string title,
            string description,
            MilkroomMysteryChoiceView[] choices)
        {
            Visible = visible;
            ChapterId = visible ? chapterId ?? string.Empty : string.Empty;
            Title = visible ? title ?? string.Empty : string.Empty;
            Description = visible ? description ?? string.Empty : string.Empty;
            this.choices = visible
                ? choices ?? Array.Empty<MilkroomMysteryChoiceView>()
                : Array.Empty<MilkroomMysteryChoiceView>();
        }

        public bool Visible { get; }
        public string ChapterId { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<MilkroomMysteryChoiceView> Choices => choices;
        public bool CanChoose => Visible
            && choices.Length == MilkroomMysteryContentLoader.ChoicesPerChapter;

        internal static MilkroomMysterySnapshot CreateVisible(
            MilkroomMysteryChapterDefinition chapter)
        {
            if (chapter == null)
            {
                return CreateHidden();
            }

            var choiceViews = new MilkroomMysteryChoiceView[chapter.Choices.Count];
            for (var index = 0; index < chapter.Choices.Count; index += 1)
            {
                choiceViews[index] = new MilkroomMysteryChoiceView(
                    chapter.Choices[index].Id,
                    chapter.Choices[index].Label);
            }

            return new MilkroomMysterySnapshot(
                true,
                chapter.Id,
                chapter.Title,
                chapter.Description,
                choiceViews);
        }

        public static MilkroomMysterySnapshot CreateHidden()
        {
            return new MilkroomMysterySnapshot(
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<MilkroomMysteryChoiceView>());
        }
    }

    public sealed class MilkroomMysteryRecordPayload
    {
        internal MilkroomMysteryRecordPayload(
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

    public sealed class MilkroomMysteryRewardPayload
    {
        internal MilkroomMysteryRewardPayload(
            string receiptId,
            MilkroomMysteryRewardKind kind,
            string rewardId,
            int amount)
        {
            ReceiptId = receiptId ?? string.Empty;
            Kind = kind;
            RewardId = rewardId ?? string.Empty;
            Amount = Math.Max(0, amount);
        }

        public string ReceiptId { get; }
        public MilkroomMysteryRewardKind Kind { get; }
        public string RewardId { get; }
        public int Amount { get; }
    }

    public sealed class MilkroomMysteryChoicePayloadBundle
    {
        internal MilkroomMysteryChoicePayloadBundle(
            string chapterId,
            string choiceId,
            string receiptId,
            string userMessage,
            MilkroomMysteryRecordPayload memory,
            MilkroomMysteryRecordPayload codex,
            MilkroomMysteryRewardPayload reward)
        {
            ChapterId = chapterId ?? string.Empty;
            ChoiceId = choiceId ?? string.Empty;
            ReceiptId = receiptId ?? string.Empty;
            UserMessage = userMessage ?? string.Empty;
            Memory = memory;
            Codex = codex;
            Reward = reward;
        }

        public string ChapterId { get; }
        public string ChoiceId { get; }
        public string ReceiptId { get; }
        public string UserMessage { get; }
        public MilkroomMysteryRecordPayload Memory { get; }
        public MilkroomMysteryRecordPayload Codex { get; }
        public MilkroomMysteryRewardPayload Reward { get; }
    }

    public enum MilkroomMysteryChoiceStatus
    {
        Applied = 0,
        MissingState = 1,
        ContentUnavailable = 2,
        InvalidReceipt = 3,
        DuplicateReceipt = 4,
        InvalidCompletionTime = 5,
        UnknownChapter = 6,
        Locked = 7,
        AlreadyCompleted = 8,
        UnknownChoice = 9,
        StateCapacityFull = 10,
        MissingPayloadCommitter = 11,
        PayloadRejected = 12
    }

    public sealed class MilkroomMysteryChoiceResult
    {
        internal MilkroomMysteryChoiceResult(
            MilkroomMysteryChoiceStatus status,
            MilkroomMysteryChoicePayloadBundle payloads = null)
        {
            Status = status;
            Applied = status == MilkroomMysteryChoiceStatus.Applied;
            Payloads = Applied ? payloads : null;
        }

        public MilkroomMysteryChoiceStatus Status { get; }
        public bool Applied { get; }
        public MilkroomMysteryChoicePayloadBundle Payloads { get; }
        public string ChapterId => Payloads?.ChapterId ?? string.Empty;
        public string ChoiceId => Payloads?.ChoiceId ?? string.Empty;
        public string ReceiptId => Payloads?.ReceiptId ?? string.Empty;
        public string UserMessage => Payloads?.UserMessage ?? string.Empty;
        public MilkroomMysteryRecordPayload Memory => Payloads?.Memory;
        public MilkroomMysteryRecordPayload Codex => Payloads?.Codex;
        public MilkroomMysteryRewardPayload Reward => Payloads?.Reward;
    }

    /// <summary>
    /// Owns deterministic chapter visibility and one-time completion receipts. Callers apply
    /// returned memory, codex, and reward payloads to their existing authoritative systems.
    /// </summary>
    public sealed class MilkroomMysterySystem
    {
        private readonly MilkroomMysteryCatalog catalog;

        public MilkroomMysterySystem()
        {
            MilkroomMysteryCatalog loadedCatalog;
            if (!MilkroomMysteryContentLoader.TryReloadDefault(
                out loadedCatalog,
                out _))
            {
                loadedCatalog = MilkroomMysteryCatalog.Empty;
            }

            catalog = loadedCatalog;
        }

        public MilkroomMysterySystem(MilkroomMysteryCatalog catalog)
        {
            this.catalog = catalog ?? MilkroomMysteryCatalog.Empty;
        }

        public bool ContentAvailable => catalog.HasContent;

        public MilkroomMysterySnapshot BuildSnapshot(
            MilkroomMysterySaveData state,
            MilkroomMysteryProgress progress)
        {
            if (state == null || !catalog.HasContent)
            {
                return MilkroomMysterySnapshot.CreateHidden();
            }

            state.EnsureRuntimeDefaults();
            for (var index = 0; index < catalog.Chapters.Count; index += 1)
            {
                var chapter = catalog.Chapters[index];
                if (state.HasCompletedChapter(chapter.Id))
                {
                    continue;
                }

                if (IsUnlocked(chapter, state, progress))
                {
                    return MilkroomMysterySnapshot.CreateVisible(chapter);
                }
            }

            return MilkroomMysterySnapshot.CreateHidden();
        }

        public MilkroomMysteryChoiceResult TryChoose(
            MilkroomMysterySaveData state,
            MilkroomMysteryProgress progress,
            string chapterId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt,
            Func<MilkroomMysteryChoicePayloadBundle, bool> tryCommitPayloads)
        {
            if (state == null)
            {
                return Failure(MilkroomMysteryChoiceStatus.MissingState);
            }

            state.EnsureRuntimeDefaults();
            if (!catalog.HasContent)
            {
                return Failure(MilkroomMysteryChoiceStatus.ContentUnavailable);
            }

            var normalizedReceipt = (receiptId ?? string.Empty).Trim();
            if (!IsValidReceipt(normalizedReceipt))
            {
                return Failure(MilkroomMysteryChoiceStatus.InvalidReceipt);
            }

            if (state.HasReceipt(normalizedReceipt))
            {
                return Failure(MilkroomMysteryChoiceStatus.DuplicateReceipt);
            }

            if (completedAt == default)
            {
                return Failure(MilkroomMysteryChoiceStatus.InvalidCompletionTime);
            }

            var chapter = catalog.Find(chapterId);
            if (chapter == null)
            {
                return Failure(MilkroomMysteryChoiceStatus.UnknownChapter);
            }

            if (state.HasCompletedChapter(chapter.Id))
            {
                return Failure(MilkroomMysteryChoiceStatus.AlreadyCompleted);
            }

            if (!IsUnlocked(chapter, state, progress))
            {
                return Failure(MilkroomMysteryChoiceStatus.Locked);
            }

            var choice = chapter.FindChoice(choiceId);
            if (choice == null)
            {
                return Failure(MilkroomMysteryChoiceStatus.UnknownChoice);
            }

            if (!state.CanRecordCompletion())
            {
                return Failure(MilkroomMysteryChoiceStatus.StateCapacityFull);
            }

            var memory = new MilkroomMysteryRecordPayload(
                $"milkroom_mystery_memory:{chapter.Id}",
                chapter.Id,
                choice.Id,
                choice.Memory.Title,
                choice.Memory.Detail);
            var codex = new MilkroomMysteryRecordPayload(
                MilkroomMysteryContentLoader.BuildCodexRecordId(chapter.Id, choice.Id),
                chapter.Id,
                choice.Id,
                choice.Codex.Title,
                choice.Codex.Detail);
            var reward = new MilkroomMysteryRewardPayload(
                normalizedReceipt,
                choice.Reward.Kind,
                choice.Reward.Id,
                choice.Reward.Amount);
            var payloads = new MilkroomMysteryChoicePayloadBundle(
                chapter.Id,
                choice.Id,
                normalizedReceipt,
                choice.ResultMessage,
                memory,
                codex,
                reward);
            if (tryCommitPayloads == null)
            {
                return Failure(MilkroomMysteryChoiceStatus.MissingPayloadCommitter);
            }

            // The caller owns the other authoritative stores and must accept all payloads as one
            // transaction. Completion is recorded only after that transaction succeeds.
            if (!tryCommitPayloads(payloads))
            {
                return Failure(MilkroomMysteryChoiceStatus.PayloadRejected);
            }

            state.RecordCompletion(new MilkroomMysteryCompletionSaveData
            {
                chapterId = chapter.Id,
                choiceId = choice.Id,
                receiptId = normalizedReceipt,
                completedAtIso = completedAt.ToString("O", CultureInfo.InvariantCulture)
            });
            return new MilkroomMysteryChoiceResult(
                MilkroomMysteryChoiceStatus.Applied,
                payloads);
        }

        private static bool IsUnlocked(
            MilkroomMysteryChapterDefinition chapter,
            MilkroomMysterySaveData state,
            MilkroomMysteryProgress progress)
        {
            if (chapter == null || state == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(chapter.PrerequisiteChapterId)
                && !state.HasCompletedChapter(chapter.PrerequisiteChapterId))
            {
                return false;
            }

            return chapter.Trigger.IsSatisfied(progress);
        }

        private static bool IsValidReceipt(string receiptId)
        {
            if (string.IsNullOrEmpty(receiptId) || receiptId.Length > 100)
            {
                return false;
            }

            for (var index = 0; index < receiptId.Length; index += 1)
            {
                var character = receiptId[index];
                var valid = (character >= 'a' && character <= 'z')
                    || (character >= 'A' && character <= 'Z')
                    || (character >= '0' && character <= '9')
                    || character == '.'
                    || character == '_'
                    || character == '-'
                    || character == ':';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private static MilkroomMysteryChoiceResult Failure(
            MilkroomMysteryChoiceStatus status)
        {
            return new MilkroomMysteryChoiceResult(status);
        }
    }
}
