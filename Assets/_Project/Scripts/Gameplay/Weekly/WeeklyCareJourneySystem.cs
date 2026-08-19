using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Weekly
{
    public static class WeeklyCareEventIds
    {
        public const string Feed = "care.feed";
        public const string Play = "care.play";
        public const string Cook = "care.cook";
        public const string Blend = "care.blend";
        public const string Clean = "care.clean";
        public const string Rest = "care.rest";
        public const string Discovery = "collection.discovery";

        public static readonly string[] All =
        {
            Feed,
            Play,
            Cook,
            Blend,
            Clean,
            Rest,
            Discovery
        };

        public static bool IsKnown(string eventId)
        {
            var normalized = (eventId ?? string.Empty).Trim();
            for (var index = 0; index < All.Length; index += 1)
            {
                if (string.Equals(All[index], normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class WeeklyCareObjectiveDefinition
    {
        public WeeklyCareObjectiveDefinition(
            string id,
            string title,
            string description,
            int target,
            params string[] acceptedEventIds)
        {
            Id = (id ?? string.Empty).Trim();
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Target = Math.Max(1, target);
            AcceptedEventIds = acceptedEventIds ?? Array.Empty<string>();
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public int Target { get; }
        public IReadOnlyList<string> AcceptedEventIds { get; }

        public bool Accepts(string eventId)
        {
            for (var index = 0; index < AcceptedEventIds.Count; index += 1)
            {
                if (string.Equals(AcceptedEventIds[index], eventId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public readonly struct WeeklyCareObjectiveSnapshot
    {
        public WeeklyCareObjectiveSnapshot(
            WeeklyCareObjectiveDefinition definition,
            int progress)
        {
            Definition = definition;
            Progress = Math.Max(0, Math.Min(definition?.Target ?? 0, progress));
        }

        public WeeklyCareObjectiveDefinition Definition { get; }
        public int Progress { get; }
        public bool Completed => Definition != null && Progress >= Definition.Target;
    }

    public enum WeeklyCareWeekStatus
    {
        Ready,
        Initialized,
        Advanced,
        ClockRollback,
        MissingState
    }

    public readonly struct WeeklyCareWeekResult
    {
        public WeeklyCareWeekResult(
            WeeklyCareWeekStatus status,
            string weekKey,
            bool stateChanged)
        {
            Status = status;
            WeekKey = weekKey ?? string.Empty;
            StateChanged = stateChanged;
        }

        public WeeklyCareWeekStatus Status { get; }
        public string WeekKey { get; }
        public bool StateChanged { get; }
        public bool CanRecord => Status == WeeklyCareWeekStatus.Ready
            || Status == WeeklyCareWeekStatus.Initialized
            || Status == WeeklyCareWeekStatus.Advanced;
    }

    public enum WeeklyCareRecordStatus
    {
        Applied,
        DuplicateReceipt,
        MissingState,
        InvalidReceipt,
        UnknownEvent,
        InvalidAmount,
        ClockRollback
    }

    public readonly struct WeeklyCareRecordResult
    {
        public WeeklyCareRecordResult(
            WeeklyCareRecordStatus status,
            string eventId,
            string receiptId,
            bool progressChanged,
            int completedObjectives,
            int newlyCompletedObjectives)
        {
            Status = status;
            EventId = eventId ?? string.Empty;
            ReceiptId = receiptId ?? string.Empty;
            ProgressChanged = progressChanged;
            CompletedObjectives = Math.Max(0, completedObjectives);
            NewlyCompletedObjectives = Math.Max(0, newlyCompletedObjectives);
        }

        public WeeklyCareRecordStatus Status { get; }
        public string EventId { get; }
        public string ReceiptId { get; }
        public bool ProgressChanged { get; }
        public int CompletedObjectives { get; }
        public int NewlyCompletedObjectives { get; }
        public bool Applied => Status == WeeklyCareRecordStatus.Applied;
    }

    public readonly struct WeeklyCareReward
    {
        public WeeklyCareReward(int milkCoins, int milkDrops, int collectionFragments)
        {
            MilkCoins = Math.Max(0, milkCoins);
            MilkDrops = Math.Max(0, milkDrops);
            CollectionFragments = Math.Max(0, collectionFragments);
        }

        public int MilkCoins { get; }
        public int MilkDrops { get; }
        public int CollectionFragments { get; }
    }

    public enum WeeklyCareClaimStatus
    {
        Applied,
        DuplicateClaim,
        AlreadyClaimed,
        MissingState,
        MissingEconomy,
        InvalidClaimReceipt,
        NotEnoughObjectives,
        ClockRollback,
        RewardCapacityFull
    }

    public readonly struct WeeklyCareClaimResult
    {
        public WeeklyCareClaimResult(
            WeeklyCareClaimStatus status,
            string weekKey,
            string claimReceiptId,
            WeeklyCareReward reward)
        {
            Status = status;
            WeekKey = weekKey ?? string.Empty;
            ClaimReceiptId = claimReceiptId ?? string.Empty;
            Reward = reward;
        }

        public WeeklyCareClaimStatus Status { get; }
        public string WeekKey { get; }
        public string ClaimReceiptId { get; }
        public WeeklyCareReward Reward { get; }
        public bool Applied => Status == WeeklyCareClaimStatus.Applied;
    }

    public sealed class WeeklyCareJourneySnapshot
    {
        public WeeklyCareJourneySnapshot(
            WeeklyCareWeekStatus weekStatus,
            string weekKey,
            IReadOnlyList<WeeklyCareObjectiveSnapshot> objectives,
            int completedObjectives,
            bool rewardClaimed)
        {
            WeekStatus = weekStatus;
            WeekKey = weekKey ?? string.Empty;
            Objectives = objectives ?? Array.Empty<WeeklyCareObjectiveSnapshot>();
            CompletedObjectives = Math.Max(0, completedObjectives);
            RewardClaimed = rewardClaimed;
        }

        public WeeklyCareWeekStatus WeekStatus { get; }
        public string WeekKey { get; }
        public IReadOnlyList<WeeklyCareObjectiveSnapshot> Objectives { get; }
        public int CompletedObjectives { get; }
        public bool RewardClaimed { get; }
        public bool CanClaimReward => WeekStatus != WeeklyCareWeekStatus.ClockRollback
            && CompletedObjectives >= WeeklyCareJourneySystem.RequiredCompletedObjectives
            && !RewardClaimed;
    }

    public sealed class WeeklyCareJourneySystem
    {
        public const int ObjectiveCount = 5;
        public const int RequiredCompletedObjectives = 3;
        public const int RewardMilkCoins = 60;
        public const int RewardMilkDrops = 10;
        public const int RewardCollectionFragments = 3;

        private static readonly WeeklyCareObjectiveDefinition[] Definitions =
        {
            new WeeklyCareObjectiveDefinition(
                "weekly_care_12",
                "고른 돌봄",
                "먹이, 놀이, 요리, 청소, 휴식을 합쳐 12회 돌봐 주세요.",
                12,
                WeeklyCareEventIds.Feed,
                WeeklyCareEventIds.Play,
                WeeklyCareEventIds.Cook,
                WeeklyCareEventIds.Blend,
                WeeklyCareEventIds.Clean,
                WeeklyCareEventIds.Rest),
            new WeeklyCareObjectiveDefinition(
                "weekly_feed_6",
                "든든한 한 주",
                "우유나 간식을 6회 챙겨 주세요.",
                6,
                WeeklyCareEventIds.Feed),
            new WeeklyCareObjectiveDefinition(
                "weekly_play_3",
                "함께 뛰는 시간",
                "놀이를 3회 함께해 주세요.",
                3,
                WeeklyCareEventIds.Play),
            new WeeklyCareObjectiveDefinition(
                "weekly_kitchen_3",
                "밀크룸 주방",
                "요리 또는 블렌딩을 합쳐 3회 완성해 주세요.",
                3,
                WeeklyCareEventIds.Cook,
                WeeklyCareEventIds.Blend),
            new WeeklyCareObjectiveDefinition(
                "weekly_discovery_2",
                "새로운 기록",
                "새로운 도감 기록을 2개 발견해 주세요.",
                2,
                WeeklyCareEventIds.Discovery)
        };

        public IReadOnlyList<WeeklyCareObjectiveDefinition> All => Definitions;

        public WeeklyCareReward Reward => new WeeklyCareReward(
            RewardMilkCoins,
            RewardMilkDrops,
            RewardCollectionFragments);

        /// <summary>
        /// Resolves Monday 00:00 from the calendar date and offset carried by the injected value.
        /// The method never converts to UTC or a fixed regional timezone.
        /// </summary>
        public static DateTimeOffset GetWeekStart(DateTimeOffset injectedNow)
        {
            var localMidnight = new DateTimeOffset(
                injectedNow.Year,
                injectedNow.Month,
                injectedNow.Day,
                0,
                0,
                0,
                injectedNow.Offset);
            var daysSinceMonday = ((int)localMidnight.DayOfWeek + 6) % 7;
            return localMidnight.AddDays(-daysSinceMonday);
        }

        public static string GetWeekKey(DateTimeOffset injectedNow)
        {
            return GetWeekStart(injectedNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public WeeklyCareWeekResult ReconcileWeek(
            WeeklyCareJourneySaveData state,
            DateTimeOffset injectedNow)
        {
            var requestedWeekKey = GetWeekKey(injectedNow);
            if (state == null)
            {
                return new WeeklyCareWeekResult(
                    WeeklyCareWeekStatus.MissingState,
                    requestedWeekKey,
                    false);
            }

            var changed = state.EnsureRuntimeDefaults();
            changed |= NormalizeKnownReceipts(state);
            if (!TryParseWeekKey(state.weekKey, out var savedWeekStart))
            {
                state.weekKey = requestedWeekKey;
                ResetObjectives(state);
                return new WeeklyCareWeekResult(
                    WeeklyCareWeekStatus.Initialized,
                    requestedWeekKey,
                    true);
            }

            var requestedWeekStart = GetWeekStart(injectedNow).Date;
            if (requestedWeekStart < savedWeekStart)
            {
                changed |= ReconcileObjectives(state);
                return new WeeklyCareWeekResult(
                    WeeklyCareWeekStatus.ClockRollback,
                    state.weekKey,
                    changed);
            }

            if (requestedWeekStart > savedWeekStart)
            {
                state.weekKey = requestedWeekKey;
                ResetObjectives(state);
                return new WeeklyCareWeekResult(
                    WeeklyCareWeekStatus.Advanced,
                    requestedWeekKey,
                    true);
            }

            changed |= ReconcileObjectives(state);
            return new WeeklyCareWeekResult(
                WeeklyCareWeekStatus.Ready,
                state.weekKey,
                changed);
        }

        public WeeklyCareRecordResult RecordEvent(
            WeeklyCareJourneySaveData state,
            string eventId,
            int amount,
            DateTimeOffset injectedNow,
            string receiptId)
        {
            var normalizedEvent = Normalize(eventId);
            var normalizedReceipt = Normalize(receiptId);
            if (state == null)
            {
                return RecordFailure(
                    WeeklyCareRecordStatus.MissingState,
                    normalizedEvent,
                    normalizedReceipt);
            }

            state.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return RecordFailure(
                    WeeklyCareRecordStatus.InvalidReceipt,
                    normalizedEvent,
                    normalizedReceipt);
            }

            if (state.HasEventReceipt(normalizedReceipt))
            {
                return RecordFailure(
                    WeeklyCareRecordStatus.DuplicateReceipt,
                    normalizedEvent,
                    normalizedReceipt,
                    CountCompleted(state));
            }

            if (!WeeklyCareEventIds.IsKnown(normalizedEvent))
            {
                return RecordFailure(
                    WeeklyCareRecordStatus.UnknownEvent,
                    normalizedEvent,
                    normalizedReceipt,
                    CountCompleted(state));
            }

            if (amount <= 0)
            {
                return RecordFailure(
                    WeeklyCareRecordStatus.InvalidAmount,
                    normalizedEvent,
                    normalizedReceipt,
                    CountCompleted(state));
            }

            var week = ReconcileWeek(state, injectedNow);
            if (!week.CanRecord)
            {
                return RecordFailure(
                    WeeklyCareRecordStatus.ClockRollback,
                    normalizedEvent,
                    normalizedReceipt,
                    CountCompleted(state));
            }

            var completedBefore = CountCompleted(state);
            var progressChanged = false;
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                var definition = Definitions[index];
                if (!definition.Accepts(normalizedEvent))
                {
                    continue;
                }

                var progress = FindProgress(state, definition.Id);
                var before = progress.progress;
                progress.progress = SaturatingAddAndClamp(before, amount, definition.Target);
                progressChanged |= progress.progress != before;
            }

            state.eventReceipts.Add(new WeeklyCareEventReceiptSaveData
            {
                receiptId = normalizedReceipt,
                eventId = normalizedEvent,
                weekKey = state.weekKey,
                recordedAtIso = injectedNow.ToString("O", CultureInfo.InvariantCulture)
            });
            while (state.eventReceipts.Count > WeeklyCareJourneySaveData.MaximumEventReceipts)
            {
                state.eventReceipts.RemoveAt(0);
            }

            var completedAfter = CountCompleted(state);
            return new WeeklyCareRecordResult(
                WeeklyCareRecordStatus.Applied,
                normalizedEvent,
                normalizedReceipt,
                progressChanged,
                completedAfter,
                Math.Max(0, completedAfter - completedBefore));
        }

        public WeeklyCareJourneySnapshot BuildSnapshot(
            WeeklyCareJourneySaveData state,
            DateTimeOffset injectedNow)
        {
            var week = ReconcileWeek(state, injectedNow);
            if (state == null)
            {
                return new WeeklyCareJourneySnapshot(
                    week.Status,
                    week.WeekKey,
                    Array.Empty<WeeklyCareObjectiveSnapshot>(),
                    0,
                    false);
            }

            var objectives = new WeeklyCareObjectiveSnapshot[Definitions.Length];
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                var definition = Definitions[index];
                var progress = FindProgress(state, definition.Id)?.progress ?? 0;
                objectives[index] = new WeeklyCareObjectiveSnapshot(definition, progress);
            }

            return new WeeklyCareJourneySnapshot(
                week.Status,
                state.weekKey,
                objectives,
                CountCompleted(state),
                state.HasRewardReceiptForWeek(state.weekKey));
        }

        public WeeklyCareClaimResult TryClaimReward(
            WeeklyCareJourneySaveData state,
            EconomySaveData economy,
            DateTimeOffset injectedNow,
            string claimReceiptId)
        {
            var normalizedReceipt = Normalize(claimReceiptId);
            if (state == null)
            {
                return ClaimFailure(
                    WeeklyCareClaimStatus.MissingState,
                    string.Empty,
                    normalizedReceipt);
            }

            state.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return ClaimFailure(
                    WeeklyCareClaimStatus.InvalidClaimReceipt,
                    state.weekKey,
                    normalizedReceipt);
            }

            if (state.HasRewardClaimReceipt(normalizedReceipt))
            {
                return ClaimFailure(
                    WeeklyCareClaimStatus.DuplicateClaim,
                    state.weekKey,
                    normalizedReceipt);
            }

            if (economy == null)
            {
                return ClaimFailure(
                    WeeklyCareClaimStatus.MissingEconomy,
                    state.weekKey,
                    normalizedReceipt);
            }

            var week = ReconcileWeek(state, injectedNow);
            if (week.Status == WeeklyCareWeekStatus.ClockRollback)
            {
                return ClaimFailure(
                    WeeklyCareClaimStatus.ClockRollback,
                    state.weekKey,
                    normalizedReceipt);
            }

            if (state.HasRewardReceiptForWeek(state.weekKey))
            {
                return ClaimFailure(
                    WeeklyCareClaimStatus.AlreadyClaimed,
                    state.weekKey,
                    normalizedReceipt);
            }

            if (CountCompleted(state) < RequiredCompletedObjectives)
            {
                return ClaimFailure(
                    WeeklyCareClaimStatus.NotEnoughObjectives,
                    state.weekKey,
                    normalizedReceipt);
            }

            var reward = Reward;
            if (!CanAdd(economy.milkCoins, reward.MilkCoins)
                || !CanAdd(economy.milkDrops, reward.MilkDrops)
                || !CanAdd(economy.collectionFragments, reward.CollectionFragments))
            {
                return ClaimFailure(
                    WeeklyCareClaimStatus.RewardCapacityFull,
                    state.weekKey,
                    normalizedReceipt);
            }

            economy.milkCoins += reward.MilkCoins;
            economy.milkDrops += reward.MilkDrops;
            economy.collectionFragments += reward.CollectionFragments;
            state.rewardReceipts.Add(new WeeklyCareRewardReceiptSaveData
            {
                claimReceiptId = normalizedReceipt,
                weekKey = state.weekKey,
                claimedAtIso = injectedNow.ToString("O", CultureInfo.InvariantCulture)
            });
            while (state.rewardReceipts.Count > WeeklyCareJourneySaveData.MaximumRewardReceipts)
            {
                state.rewardReceipts.RemoveAt(0);
            }

            return new WeeklyCareClaimResult(
                WeeklyCareClaimStatus.Applied,
                state.weekKey,
                normalizedReceipt,
                reward);
        }

        private static bool ReconcileObjectives(WeeklyCareJourneySaveData state)
        {
            var changed = false;
            for (var index = state.objectives.Count - 1; index >= 0; index -= 1)
            {
                var entry = state.objectives[index];
                var definition = FindDefinition(entry?.objectiveId);
                if (entry == null || definition == null)
                {
                    state.objectives.RemoveAt(index);
                    changed = true;
                    continue;
                }

                var clamped = Math.Max(0, Math.Min(definition.Target, entry.progress));
                if (clamped != entry.progress)
                {
                    entry.progress = clamped;
                    changed = true;
                }
            }

            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (FindProgress(state, Definitions[index].Id) != null)
                {
                    continue;
                }

                state.objectives.Add(new WeeklyCareObjectiveProgressSaveData
                {
                    objectiveId = Definitions[index].Id
                });
                changed = true;
            }

            return changed;
        }

        private static bool NormalizeKnownReceipts(WeeklyCareJourneySaveData state)
        {
            var changed = false;
            for (var index = state.eventReceipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = state.eventReceipts[index];
                if (receipt == null
                    || !WeeklyCareEventIds.IsKnown(receipt.eventId)
                    || !TryParseWeekKey(receipt.weekKey, out _))
                {
                    state.eventReceipts.RemoveAt(index);
                    changed = true;
                }
            }

            for (var index = state.rewardReceipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = state.rewardReceipts[index];
                if (receipt == null || !TryParseWeekKey(receipt.weekKey, out _))
                {
                    state.rewardReceipts.RemoveAt(index);
                    changed = true;
                }
            }

            return changed;
        }

        private static void ResetObjectives(WeeklyCareJourneySaveData state)
        {
            state.objectives.Clear();
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                state.objectives.Add(new WeeklyCareObjectiveProgressSaveData
                {
                    objectiveId = Definitions[index].Id,
                    progress = 0
                });
            }
        }

        private static WeeklyCareObjectiveProgressSaveData FindProgress(
            WeeklyCareJourneySaveData state,
            string objectiveId)
        {
            if (state?.objectives == null)
            {
                return null;
            }

            for (var index = 0; index < state.objectives.Count; index += 1)
            {
                var entry = state.objectives[index];
                if (entry != null
                    && string.Equals(entry.objectiveId, objectiveId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static WeeklyCareObjectiveDefinition FindDefinition(string objectiveId)
        {
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (string.Equals(Definitions[index].Id, objectiveId, StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }

        private static int CountCompleted(WeeklyCareJourneySaveData state)
        {
            var count = 0;
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                var definition = Definitions[index];
                if ((FindProgress(state, definition.Id)?.progress ?? 0) >= definition.Target)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static bool TryParseWeekKey(string value, out DateTime parsed)
        {
            if (!DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed))
            {
                return false;
            }

            return parsed.DayOfWeek == DayOfWeek.Monday;
        }

        private static int SaturatingAddAndClamp(int current, int amount, int maximum)
        {
            var result = (long)Math.Max(0, current) + Math.Max(0, amount);
            return result >= maximum ? maximum : (int)result;
        }

        private static bool CanAdd(int current, int amount)
        {
            return current >= 0
                && amount >= 0
                && (long)current + amount <= int.MaxValue;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static WeeklyCareRecordResult RecordFailure(
            WeeklyCareRecordStatus status,
            string eventId,
            string receiptId,
            int completedObjectives = 0)
        {
            return new WeeklyCareRecordResult(
                status,
                eventId,
                receiptId,
                false,
                completedObjectives,
                0);
        }

        private static WeeklyCareClaimResult ClaimFailure(
            WeeklyCareClaimStatus status,
            string weekKey,
            string claimReceiptId)
        {
            return new WeeklyCareClaimResult(
                status,
                weekKey,
                claimReceiptId,
                default);
        }
    }
}
