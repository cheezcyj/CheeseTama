using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;

namespace CheeseTama.Collections
{
    public enum CollectionSetAlbumRecordCategory
    {
        Milk = 0,
        Evolution = 1,
        Event = 2
    }

    internal sealed class CollectionSetAlbumRequirement
    {
        public CollectionSetAlbumRequirement(
            CollectionSetAlbumRecordCategory category,
            string recordId)
        {
            Category = category;
            RecordId = Normalize(recordId);
        }

        public CollectionSetAlbumRecordCategory Category { get; }
        public string RecordId { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct CollectionSetAlbumReward
    {
        public CollectionSetAlbumReward(
            int coins,
            int milkDrops,
            int collectionFragments)
        {
            Coins = Math.Max(0, coins);
            MilkDrops = Math.Max(0, milkDrops);
            CollectionFragments = Math.Max(0, collectionFragments);
        }

        public int Coins { get; }
        public int MilkDrops { get; }
        public int CollectionFragments { get; }
    }

    internal sealed class CollectionSetAlbumDefinition
    {
        private readonly CollectionSetAlbumRequirement[] requirements;

        public CollectionSetAlbumDefinition(
            string id,
            string displayName,
            string description,
            bool hiddenUntilComplete,
            CollectionSetAlbumReward reward,
            params CollectionSetAlbumRequirement[] requirements)
        {
            Id = Normalize(id);
            DisplayName = Normalize(displayName);
            Description = Normalize(description);
            HiddenUntilComplete = hiddenUntilComplete;
            Reward = reward;
            this.requirements = NormalizeRequirements(requirements);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public bool HiddenUntilComplete { get; }
        public CollectionSetAlbumReward Reward { get; }
        public IReadOnlyList<CollectionSetAlbumRequirement> Requirements => requirements;

        private static CollectionSetAlbumRequirement[] NormalizeRequirements(
            IEnumerable<CollectionSetAlbumRequirement> source)
        {
            var normalized = new List<CollectionSetAlbumRequirement>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (source != null)
            {
                foreach (var requirement in source)
                {
                    if (requirement == null || string.IsNullOrEmpty(requirement.RecordId))
                    {
                        continue;
                    }

                    var key = ((int)requirement.Category) + ":" + requirement.RecordId;
                    if (seen.Add(key))
                    {
                        normalized.Add(requirement);
                    }
                }
            }

            return normalized.ToArray();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal static class CollectionSetAlbumCatalog
    {
        internal const string MilkFirstStepsId = "album_milk_first_steps";
        internal const string DeepFlavorTrailId = "album_deep_flavor_trail";
        internal const string MilkroomDailyMomentsId = "album_milkroom_daily_moments";
        internal const string NormalEvolutionCircleId = "album_normal_evolution_circle";
        internal const string MainMilkMasteryId = "album_main_milk_mastery";

        private static readonly CollectionSetAlbumDefinition[] Definitions =
        {
            new CollectionSetAlbumDefinition(
                MilkFirstStepsId,
                "우유 첫걸음",
                "처음 만나는 세 가지 우유 기록을 모아요.",
                hiddenUntilComplete: false,
                new CollectionSetAlbumReward(40, 2, 1),
                Milk(MilkCatalog.BasicMilkId),
                Milk(MilkCatalog.WarmMilkId),
                Milk(MilkCatalog.ColdMilkId)),
            new CollectionSetAlbumDefinition(
                DeepFlavorTrailId,
                "깊은 맛의 길",
                "고소함부터 커피 향까지 이어지는 우유 기록이에요.",
                hiddenUntilComplete: false,
                new CollectionSetAlbumReward(70, 4, 2),
                Milk(MilkCatalog.NuttyMilkId),
                Milk(MilkCatalog.RichMilkId),
                Milk(MilkCatalog.FermentedMilkId),
                Milk(MilkCatalog.CoffeeMilkId)),
            new CollectionSetAlbumDefinition(
                MilkroomDailyMomentsId,
                "밀크룸의 하루",
                "돌봄과 놀이로 남긴 평범하고 소중한 순간들이에요.",
                hiddenUntilComplete: false,
                new CollectionSetAlbumReward(50, 5, 1),
                Event("daily_routine_complete"),
                Event("milk_drop_catch"),
                Event("bouncy_jump")),
            new CollectionSetAlbumDefinition(
                NormalEvolutionCircleId,
                "여섯 갈래의 성장",
                "서로 다른 돌봄의 결이 여섯 모습으로 이어졌어요.",
                hiddenUntilComplete: true,
                new CollectionSetAlbumReward(120, 7, 3),
                Evolution(EvolutionSystem.CreamEvolutionId),
                Evolution(EvolutionSystem.CheddarEvolutionId),
                Evolution(EvolutionSystem.RicottaEvolutionId),
                Evolution(EvolutionSystem.MozzarellaEvolutionId),
                Evolution(EvolutionSystem.BlueEvolutionId),
                Evolution(EvolutionSystem.CoffeeEvolutionId)),
            new CollectionSetAlbumDefinition(
                MainMilkMasteryId,
                "일곱 우유의 기억",
                "밀크룸의 주요 우유 기록이 하나의 긴 여정이 되었어요.",
                hiddenUntilComplete: true,
                new CollectionSetAlbumReward(100, 7, 3),
                Milk(MilkCatalog.BasicMilkId),
                Milk(MilkCatalog.WarmMilkId),
                Milk(MilkCatalog.ColdMilkId),
                Milk(MilkCatalog.NuttyMilkId),
                Milk(MilkCatalog.RichMilkId),
                Milk(MilkCatalog.FermentedMilkId),
                Milk(MilkCatalog.CoffeeMilkId))
        };

        internal static IReadOnlyList<CollectionSetAlbumDefinition> All => Definitions;

        internal static CollectionSetAlbumDefinition Find(string setId)
        {
            var normalizedId = Normalize(setId);
            if (string.IsNullOrEmpty(normalizedId))
            {
                return null;
            }

            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (string.Equals(
                        Definitions[index].Id,
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }

        private static CollectionSetAlbumRequirement Milk(string recordId)
        {
            return new CollectionSetAlbumRequirement(
                CollectionSetAlbumRecordCategory.Milk,
                recordId);
        }

        private static CollectionSetAlbumRequirement Evolution(string recordId)
        {
            return new CollectionSetAlbumRequirement(
                CollectionSetAlbumRecordCategory.Evolution,
                recordId);
        }

        private static CollectionSetAlbumRequirement Event(string recordId)
        {
            return new CollectionSetAlbumRequirement(
                CollectionSetAlbumRecordCategory.Event,
                recordId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class CollectionSetAlbumRecordProgress
    {
        public CollectionSetAlbumRecordProgress(
            CollectionSetAlbumRecordCategory category,
            string recordId,
            bool discovered)
        {
            Category = category;
            RecordId = string.IsNullOrWhiteSpace(recordId)
                ? string.Empty
                : recordId.Trim();
            Discovered = discovered;
        }

        public CollectionSetAlbumRecordCategory Category { get; }
        public string RecordId { get; }
        public bool Discovered { get; }
    }

    public sealed class CollectionSetAlbumSetProgress
    {
        private readonly CollectionSetAlbumRecordProgress[] records;

        internal CollectionSetAlbumSetProgress(
            CollectionSetAlbumDefinition definition,
            IEnumerable<CollectionSetAlbumRecordProgress> records,
            bool rewardClaimed)
        {
            SetId = definition?.Id ?? string.Empty;
            DisplayName = definition?.DisplayName ?? string.Empty;
            Description = definition?.Description ?? string.Empty;
            Reward = definition?.Reward ?? default;
            var normalized = new List<CollectionSetAlbumRecordProgress>();
            if (records != null)
            {
                foreach (var record in records)
                {
                    if (record != null && !string.IsNullOrEmpty(record.RecordId))
                    {
                        normalized.Add(record);
                    }
                }
            }

            this.records = normalized.ToArray();
            var discoveredCount = 0;
            for (var index = 0; index < this.records.Length; index += 1)
            {
                if (this.records[index].Discovered)
                {
                    discoveredCount += 1;
                }
            }

            DiscoveredCount = discoveredCount;
            RequiredCount = this.records.Length;
            Complete = RequiredCount > 0 && DiscoveredCount == RequiredCount;
            RewardClaimed = rewardClaimed && Complete;
        }

        public string SetId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public CollectionSetAlbumReward Reward { get; }
        public IReadOnlyList<CollectionSetAlbumRecordProgress> Records => records;
        public int DiscoveredCount { get; }
        public int RequiredCount { get; }
        public bool Complete { get; }
        public bool RewardClaimed { get; }
        public bool CanClaimReward => Complete && !RewardClaimed;
    }

    public sealed class CollectionSetAlbumPublicSnapshot
    {
        private readonly CollectionSetAlbumSetProgress[] sets;

        public CollectionSetAlbumPublicSnapshot(
            IEnumerable<CollectionSetAlbumSetProgress> sets)
        {
            var normalized = new List<CollectionSetAlbumSetProgress>();
            if (sets != null)
            {
                foreach (var set in sets)
                {
                    if (set != null && !string.IsNullOrEmpty(set.SetId))
                    {
                        normalized.Add(set);
                    }
                }
            }

            this.sets = normalized.ToArray();
        }

        public IReadOnlyList<CollectionSetAlbumSetProgress> Sets => sets;

        public CollectionSetAlbumSetProgress Find(string setId)
        {
            var normalizedId = string.IsNullOrWhiteSpace(setId)
                ? string.Empty
                : setId.Trim();
            for (var index = 0; index < sets.Length; index += 1)
            {
                if (string.Equals(sets[index].SetId, normalizedId, StringComparison.Ordinal))
                {
                    return sets[index];
                }
            }

            return null;
        }
    }

    public enum CollectionSetAlbumClaimStatus
    {
        Applied = 0,
        MissingState = 1,
        InvalidReceipt = 2,
        AlreadyApplied = 3,
        UnknownSet = 4,
        NotVisible = 5,
        Incomplete = 6,
        AlreadyClaimed = 7,
        TrackingCapacityFull = 8
    }

    public sealed class CollectionSetAlbumClaimResult
    {
        public CollectionSetAlbumClaimResult(
            CollectionSetAlbumClaimStatus status,
            string setId,
            string receiptKey,
            CollectionSetAlbumReward reward)
        {
            Status = status;
            SetId = Normalize(setId);
            ReceiptKey = Normalize(receiptKey);
            Reward = status == CollectionSetAlbumClaimStatus.Applied
                ? reward
                : default;
        }

        public CollectionSetAlbumClaimStatus Status { get; }
        public string SetId { get; }
        public string ReceiptKey { get; }
        public CollectionSetAlbumReward Reward { get; }
        public bool Applied => Status == CollectionSetAlbumClaimStatus.Applied;
        public bool DuplicateReceipt => Status == CollectionSetAlbumClaimStatus.AlreadyApplied;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class CollectionSetAlbumSystem
    {
        public const string MilkFirstStepsSetId = "album_milk_first_steps";
        public const string DeepFlavorTrailSetId = "album_deep_flavor_trail";
        public const string MilkroomDailyMomentsSetId = "album_milkroom_daily_moments";
        public const string NormalEvolutionCircleSetId = "album_normal_evolution_circle";
        public const string MainMilkMasterySetId = "album_main_milk_mastery";

        public int RecalculateProgress(
            CollectionSetAlbumSaveData state,
            CollectionSaveData collections)
        {
            if (state == null)
            {
                return 0;
            }

            state.EnsureRuntimeDefaults();
            var index = new PublicRecordIndex(collections);
            var revealedCount = 0;
            foreach (var definition in CollectionSetAlbumCatalog.All)
            {
                if (!definition.HiddenUntilComplete
                    || state.IsHiddenSetRevealed(definition.Id)
                    || !IsComplete(definition, index)
                    || !state.CanReveal(definition.Id))
                {
                    continue;
                }

                if (state.AddRevealedHiddenSet(definition.Id))
                {
                    revealedCount += 1;
                }
            }

            return revealedCount;
        }

        public CollectionSetAlbumPublicSnapshot BuildPublicProgressSnapshot(
            CollectionSetAlbumSaveData state,
            CollectionSaveData collections)
        {
            var effectiveState = state ?? new CollectionSetAlbumSaveData();
            RecalculateProgress(effectiveState, collections);
            var index = new PublicRecordIndex(collections);
            var visibleSets = new List<CollectionSetAlbumSetProgress>();
            foreach (var definition in CollectionSetAlbumCatalog.All)
            {
                if (definition.HiddenUntilComplete
                    && !effectiveState.IsHiddenSetRevealed(definition.Id))
                {
                    continue;
                }

                visibleSets.Add(BuildProgress(
                    definition,
                    index,
                    effectiveState.IsRewardClaimed(definition.Id)));
            }

            return new CollectionSetAlbumPublicSnapshot(visibleSets);
        }

        public CollectionSetAlbumClaimResult TryClaimReward(
            CollectionSetAlbumSaveData state,
            CollectionSaveData collections,
            string setId,
            string receiptKey)
        {
            var normalizedReceipt = Normalize(receiptKey);
            if (state == null)
            {
                return ClaimFailure(
                    CollectionSetAlbumClaimStatus.MissingState,
                    string.Empty,
                    normalizedReceipt);
            }

            state.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return ClaimFailure(
                    CollectionSetAlbumClaimStatus.InvalidReceipt,
                    string.Empty,
                    string.Empty);
            }

            if (state.HasAppliedClaimReceipt(normalizedReceipt))
            {
                return ClaimFailure(
                    CollectionSetAlbumClaimStatus.AlreadyApplied,
                    string.Empty,
                    normalizedReceipt);
            }

            var definition = CollectionSetAlbumCatalog.Find(setId);
            if (definition == null)
            {
                return ClaimFailure(
                    CollectionSetAlbumClaimStatus.UnknownSet,
                    string.Empty,
                    normalizedReceipt);
            }

            RecalculateProgress(state, collections);
            if (definition.HiddenUntilComplete
                && !state.IsHiddenSetRevealed(definition.Id))
            {
                return ClaimFailure(
                    CollectionSetAlbumClaimStatus.NotVisible,
                    string.Empty,
                    normalizedReceipt);
            }

            var index = new PublicRecordIndex(collections);
            if (!IsComplete(definition, index))
            {
                return ClaimFailure(
                    CollectionSetAlbumClaimStatus.Incomplete,
                    definition.Id,
                    normalizedReceipt);
            }

            if (state.IsRewardClaimed(definition.Id))
            {
                return ClaimFailure(
                    CollectionSetAlbumClaimStatus.AlreadyClaimed,
                    definition.Id,
                    normalizedReceipt);
            }

            if (!state.CanClaim(definition.Id, normalizedReceipt))
            {
                return ClaimFailure(
                    CollectionSetAlbumClaimStatus.TrackingCapacityFull,
                    definition.Id,
                    normalizedReceipt);
            }

            state.AddClaim(definition.Id, normalizedReceipt);
            return new CollectionSetAlbumClaimResult(
                CollectionSetAlbumClaimStatus.Applied,
                definition.Id,
                normalizedReceipt,
                definition.Reward);
        }

        private static CollectionSetAlbumSetProgress BuildProgress(
            CollectionSetAlbumDefinition definition,
            PublicRecordIndex index,
            bool rewardClaimed)
        {
            var records = new List<CollectionSetAlbumRecordProgress>(
                definition.Requirements.Count);
            for (var requirementIndex = 0;
                requirementIndex < definition.Requirements.Count;
                requirementIndex += 1)
            {
                var requirement = definition.Requirements[requirementIndex];
                records.Add(new CollectionSetAlbumRecordProgress(
                    requirement.Category,
                    requirement.RecordId,
                    index.Contains(requirement.Category, requirement.RecordId)));
            }

            return new CollectionSetAlbumSetProgress(
                definition,
                records,
                rewardClaimed);
        }

        private static bool IsComplete(
            CollectionSetAlbumDefinition definition,
            PublicRecordIndex index)
        {
            if (definition == null || definition.Requirements.Count == 0)
            {
                return false;
            }

            for (var requirementIndex = 0;
                requirementIndex < definition.Requirements.Count;
                requirementIndex += 1)
            {
                var requirement = definition.Requirements[requirementIndex];
                if (!index.Contains(requirement.Category, requirement.RecordId))
                {
                    return false;
                }
            }

            return true;
        }

        private static CollectionSetAlbumClaimResult ClaimFailure(
            CollectionSetAlbumClaimStatus status,
            string setId,
            string receiptKey)
        {
            return new CollectionSetAlbumClaimResult(
                status,
                setId,
                receiptKey,
                default);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private sealed class PublicRecordIndex
        {
            private readonly HashSet<string> milkIds;
            private readonly HashSet<string> evolutionIds;
            private readonly HashSet<string> eventIds;

            public PublicRecordIndex(CollectionSaveData collections)
            {
                milkIds = NormalizeRecords(collections?.milk);
                evolutionIds = NormalizeRecords(collections?.evolution);
                eventIds = NormalizeRecords(collections?.events);
            }

            public bool Contains(
                CollectionSetAlbumRecordCategory category,
                string recordId)
            {
                var normalizedId = Normalize(recordId);
                if (string.IsNullOrEmpty(normalizedId))
                {
                    return false;
                }

                return category switch
                {
                    CollectionSetAlbumRecordCategory.Evolution =>
                        evolutionIds.Contains(normalizedId),
                    CollectionSetAlbumRecordCategory.Event =>
                        eventIds.Contains(normalizedId),
                    _ => milkIds.Contains(normalizedId)
                };
            }

            private static HashSet<string> NormalizeRecords(IEnumerable<string> records)
            {
                var normalized = new HashSet<string>(StringComparer.Ordinal);
                if (records == null)
                {
                    return normalized;
                }

                foreach (var recordId in records)
                {
                    var value = Normalize(recordId);
                    if (!string.IsNullOrEmpty(value))
                    {
                        normalized.Add(value);
                    }
                }

                return normalized;
            }
        }
    }
}
