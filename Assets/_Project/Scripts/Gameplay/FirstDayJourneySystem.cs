using System;
using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Journey
{
    public sealed class FirstDayJourneyTaskDefinition
    {
        internal FirstDayJourneyTaskDefinition(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }
        public string DisplayName { get; }
    }

    public readonly struct FirstDayJourneyRewardResult
    {
        public FirstDayJourneyRewardResult(
            bool granted,
            int milkCoins,
            int milkDrops,
            int collectionFragments,
            string message)
        {
            Granted = granted;
            MilkCoins = milkCoins;
            MilkDrops = milkDrops;
            CollectionFragments = collectionFragments;
            Message = message ?? string.Empty;
        }

        public bool Granted { get; }
        public int MilkCoins { get; }
        public int MilkDrops { get; }
        public int CollectionFragments { get; }
        public string Message { get; }
    }

    public static class FirstDayJourneySystem
    {
        public const string FeedTaskId = "first_day_feed";
        public const string CookTaskId = "first_day_cook";
        public const string PlayTaskId = "first_day_play";
        public const string CleanTaskId = "first_day_clean";
        public const string RestTaskId = "first_day_rest";
        public const string CollectionTaskId = "first_day_collection";

        public const int RewardMilkCoins = 20;
        public const int RewardMilkDrops = 5;
        public const int RewardCollectionFragments = 1;

        private static readonly FirstDayJourneyTaskDefinition[] Definitions =
        {
            new FirstDayJourneyTaskDefinition(FeedTaskId, "우유나 간식 먹이기"),
            new FirstDayJourneyTaskDefinition(CookTaskId, "간식 한 번 요리하기"),
            new FirstDayJourneyTaskDefinition(PlayTaskId, "함께 놀아주기"),
            new FirstDayJourneyTaskDefinition(CleanTaskId, "밀크룸 청소하기"),
            new FirstDayJourneyTaskDefinition(RestTaskId, "함께 쉬기"),
            new FirstDayJourneyTaskDefinition(CollectionTaskId, "밀크룸 기록 열어보기")
        };

        public static IReadOnlyList<FirstDayJourneyTaskDefinition> Tasks => Definitions;

        public static bool IsKnownTaskId(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                return false;
            }

            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (string.Equals(Definitions[index].Id, taskId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryRecordCareAction(
            FirstDayJourneySaveData state,
            string actionId,
            DateTimeOffset now)
        {
            var taskId = ResolveTaskId(actionId);
            return TryCompleteTask(state, taskId, now);
        }

        public static bool TryRecordCollectionOpened(
            FirstDayJourneySaveData state,
            DateTimeOffset now)
        {
            return TryCompleteTask(state, CollectionTaskId, now);
        }

        public static bool TryCompleteTask(
            FirstDayJourneySaveData state,
            string taskId,
            DateTimeOffset now)
        {
            if (state == null || state.legacySuppressed || state.rewardClaimed
                || !IsKnownTaskId(taskId))
            {
                return false;
            }

            state.EnsureRuntimeDefaults();
            if (state.completedTaskIds.Contains(taskId))
            {
                return false;
            }

            state.completedTaskIds.Add(taskId);
            if (HasCompletedEveryTask(state))
            {
                state.completed = true;
                state.completedAtIso = now.ToString("O");
            }

            return true;
        }

        public static bool MarkIntroShown(FirstDayJourneySaveData state)
        {
            if (state == null || state.legacySuppressed || state.introShown)
            {
                return false;
            }

            state.introShown = true;
            return true;
        }

        public static FirstDayJourneyRewardResult ClaimCompletionReward(
            FirstDayJourneySaveData state)
        {
            if (state == null || state.legacySuppressed || !state.completed)
            {
                return new FirstDayJourneyRewardResult(
                    false, 0, 0, 0, "첫날 여정을 모두 완료해 주세요.");
            }

            if (state.rewardClaimed)
            {
                return new FirstDayJourneyRewardResult(
                    false, 0, 0, 0, "첫날 여정 선물은 이미 받았습니다.");
            }

            state.rewardClaimed = true;
            return new FirstDayJourneyRewardResult(
                true,
                RewardMilkCoins,
                RewardMilkDrops,
                RewardCollectionFragments,
                "첫날 여정 완료! 코인 +20, 우유방울 +5, 도감조각 +1.");
        }

        public static bool HasCompletedEveryTask(FirstDayJourneySaveData state)
        {
            if (state?.completedTaskIds == null)
            {
                return false;
            }

            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (!state.completedTaskIds.Contains(Definitions[index].Id))
                {
                    return false;
                }
            }

            return true;
        }

        public static int CountCompletedTasks(FirstDayJourneySaveData state)
        {
            if (state?.completedTaskIds == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (state.completedTaskIds.Contains(Definitions[index].Id))
                {
                    count += 1;
                }
            }

            return count;
        }

        private static string ResolveTaskId(string actionId)
        {
            switch (actionId)
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
                    return FeedTaskId;
                case "cook":
                case "blend":
                    return CookTaskId;
                case "play":
                    return PlayTaskId;
                case "clean":
                    return CleanTaskId;
                case "rest":
                    return RestTaskId;
                default:
                    return string.Empty;
            }
        }
    }
}
