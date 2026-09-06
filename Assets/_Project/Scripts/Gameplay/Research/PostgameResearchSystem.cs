using System;
using System.Collections.Generic;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Research
{
    public sealed class PostgameResearchNodeDefinition
    {
        public PostgameResearchNodeDefinition(
            string id,
            string title,
            string detail,
            int cost,
            string prerequisiteNodeId,
            string profileId)
        {
            Id = Normalize(id);
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            Cost = Math.Max(1, cost);
            PrerequisiteNodeId = Normalize(prerequisiteNodeId);
            ProfileId = Normalize(profileId);
        }

        public string Id { get; }
        public string Title { get; }
        public string Detail { get; }
        public int Cost { get; }
        public string PrerequisiteNodeId { get; }
        public string ProfileId { get; }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public static class PostgameResearchCatalog
    {
        private static readonly PostgameResearchNodeDefinition[] Definitions =
        {
            new PostgameResearchNodeDefinition(
                "postgame_research_soft_glow",
                "최종 숙성 살펴보기",
                "레벨 33 뒤에도 계속 돌보며 달라진 점을 첫 연구 기록으로 남겨요.",
                3,
                string.Empty,
                "postgame_profile_maturation_notes"),
            new PostgameResearchNodeDefinition(
                "postgame_research_memory_shelf",
                "우유 섞기 비교표",
                "어떤 재료로 어떤 음식이 나왔는지 비교하는 두 번째 연구 기록을 열어요.",
                5,
                "postgame_research_soft_glow",
                "postgame_profile_blending_chart"),
            new PostgameResearchNodeDefinition(
                "postgame_research_star_hum",
                "손님과 기억 지도",
                "손님 이야기와 생활 기록이 어떻게 이어지는지 정리한 연구 기록을 열어요.",
                7,
                "postgame_research_memory_shelf",
                "postgame_profile_memory_map"),
            new PostgameResearchNodeDefinition(
                "postgame_research_deep_dialogue",
                "별빛 성장 연구",
                "여러 번 자란 기록과 별빛 기록을 모아 마지막 연구를 완성해요.",
                9,
                "postgame_research_star_hum",
                "postgame_profile_star_thesis")
        };

        public static IReadOnlyList<PostgameResearchNodeDefinition> All => Definitions;

        public static PostgameResearchNodeDefinition Find(string nodeId)
        {
            var normalized = Normalize(nodeId);
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (string.Equals(Definitions[index].Id, normalized, StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PostgameResearchSourceSnapshot
    {
        public PostgameResearchSourceSnapshot(
            int maturationCycles,
            int blendingMasteryRecords,
            int claimedAlbumSets,
            int miniGameSuccesses,
            int completedNpcEpisodes)
        {
            MaturationCycles = Math.Max(0, maturationCycles);
            BlendingMasteryRecords = Math.Max(0, blendingMasteryRecords);
            ClaimedAlbumSets = Math.Max(0, claimedAlbumSets);
            MiniGameSuccesses = Math.Max(0, miniGameSuccesses);
            CompletedNpcEpisodes = Math.Max(0, completedNpcEpisodes);
        }

        public int MaturationCycles { get; }
        public int BlendingMasteryRecords { get; }
        public int ClaimedAlbumSets { get; }
        public int MiniGameSuccesses { get; }
        public int CompletedNpcEpisodes { get; }

        public int CalculateEarnedPoints()
        {
            return Math.Min(12, MaturationCycles)
                + Math.Min(8, BlendingMasteryRecords / 2)
                + Math.Min(10, ClaimedAlbumSets * 2)
                + Math.Min(6, MiniGameSuccesses / 5)
                + Math.Min(6, CompletedNpcEpisodes);
        }
    }

    public sealed class PostgameResearchNodeSnapshot
    {
        public PostgameResearchNodeSnapshot(
            PostgameResearchNodeDefinition definition,
            bool unlocked,
            bool prerequisiteMet,
            bool affordable)
        {
            Definition = definition;
            Unlocked = unlocked;
            PrerequisiteMet = prerequisiteMet;
            Affordable = affordable;
        }

        public PostgameResearchNodeDefinition Definition { get; }
        public bool Unlocked { get; }
        public bool PrerequisiteMet { get; }
        public bool Affordable { get; }
        public bool CanUnlock => !Unlocked && PrerequisiteMet && Affordable;
    }

    public sealed class PostgameResearchSnapshot
    {
        public PostgameResearchSnapshot(
            bool available,
            int earnedPoints,
            int spentPoints,
            string activeProfileId,
            IReadOnlyList<PostgameResearchNodeSnapshot> nodes)
        {
            Available = available;
            EarnedPoints = Math.Max(0, earnedPoints);
            SpentPoints = Math.Max(0, Math.Min(EarnedPoints, spentPoints));
            ActiveProfileId = activeProfileId ?? string.Empty;
            Nodes = nodes ?? Array.Empty<PostgameResearchNodeSnapshot>();
        }

        public bool Available { get; }
        public int EarnedPoints { get; }
        public int SpentPoints { get; }
        public int AvailablePoints => Math.Max(0, EarnedPoints - SpentPoints);
        public string ActiveProfileId { get; }
        public IReadOnlyList<PostgameResearchNodeSnapshot> Nodes { get; }
    }

    public enum PostgameResearchUnlockStatus
    {
        Applied,
        MissingState,
        InvalidReceipt,
        AlreadyApplied,
        NotFinalLevel,
        UnknownNode,
        AlreadyUnlocked,
        MissingPrerequisite,
        InsufficientPoints,
        TrackingCapacityFull
    }

    public sealed class PostgameResearchUnlockResult
    {
        public PostgameResearchUnlockResult(
            PostgameResearchUnlockStatus status,
            string nodeId,
            string receiptKey,
            string profileId)
        {
            Status = status;
            NodeId = Normalize(nodeId);
            ReceiptKey = Normalize(receiptKey);
            ProfileId = status == PostgameResearchUnlockStatus.Applied
                ? Normalize(profileId)
                : string.Empty;
        }

        public PostgameResearchUnlockStatus Status { get; }
        public string NodeId { get; }
        public string ReceiptKey { get; }
        public string ProfileId { get; }
        public bool Applied => Status == PostgameResearchUnlockStatus.Applied;

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class PostgameResearchSystem
    {
        public const int RequiredLevel = 33;

        public PostgameResearchSnapshot BuildSnapshot(
            int currentLevel,
            PostgameResearchSaveData state,
            PostgameResearchSourceSnapshot sources)
        {
            var available = currentLevel >= RequiredLevel;
            var effectiveState = state ?? new PostgameResearchSaveData();
            effectiveState.EnsureRuntimeDefaults();
            var earned = sources.CalculateEarnedPoints();
            var spent = CalculateSpentPoints(effectiveState);
            var remaining = Math.Max(0, earned - spent);
            var nodes = new List<PostgameResearchNodeSnapshot>();

            if (available)
            {
                foreach (var definition in PostgameResearchCatalog.All)
                {
                    var unlocked = effectiveState.IsUnlocked(definition.Id);
                    var prerequisiteMet = string.IsNullOrEmpty(definition.PrerequisiteNodeId)
                        || effectiveState.IsUnlocked(definition.PrerequisiteNodeId);
                    nodes.Add(new PostgameResearchNodeSnapshot(
                        definition,
                        unlocked,
                        prerequisiteMet,
                        remaining >= definition.Cost));
                }
            }

            return new PostgameResearchSnapshot(
                available,
                earned,
                spent,
                effectiveState.activeProfileId,
                nodes);
        }

        public PostgameResearchUnlockResult TryUnlock(
            int currentLevel,
            PostgameResearchSaveData state,
            PostgameResearchSourceSnapshot sources,
            string nodeId,
            string receiptKey)
        {
            var normalizedReceipt = Normalize(receiptKey);
            if (state == null)
            {
                return Failure(PostgameResearchUnlockStatus.MissingState, nodeId, normalizedReceipt);
            }

            state.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return Failure(PostgameResearchUnlockStatus.InvalidReceipt, nodeId, normalizedReceipt);
            }

            if (state.HasAppliedReceipt(normalizedReceipt))
            {
                return Failure(PostgameResearchUnlockStatus.AlreadyApplied, nodeId, normalizedReceipt);
            }

            if (currentLevel < RequiredLevel)
            {
                return Failure(PostgameResearchUnlockStatus.NotFinalLevel, nodeId, normalizedReceipt);
            }

            var definition = PostgameResearchCatalog.Find(nodeId);
            if (definition == null)
            {
                return Failure(PostgameResearchUnlockStatus.UnknownNode, nodeId, normalizedReceipt);
            }

            if (state.IsUnlocked(definition.Id))
            {
                return Failure(PostgameResearchUnlockStatus.AlreadyUnlocked, definition.Id, normalizedReceipt);
            }

            if (!string.IsNullOrEmpty(definition.PrerequisiteNodeId)
                && !state.IsUnlocked(definition.PrerequisiteNodeId))
            {
                return Failure(PostgameResearchUnlockStatus.MissingPrerequisite, definition.Id, normalizedReceipt);
            }

            var remaining = sources.CalculateEarnedPoints() - CalculateSpentPoints(state);
            if (remaining < definition.Cost)
            {
                return Failure(PostgameResearchUnlockStatus.InsufficientPoints, definition.Id, normalizedReceipt);
            }

            if (!state.CanUnlock(definition.Id, normalizedReceipt)
                || !state.AddUnlock(definition.Id, normalizedReceipt, definition.ProfileId))
            {
                return Failure(PostgameResearchUnlockStatus.TrackingCapacityFull, definition.Id, normalizedReceipt);
            }

            return new PostgameResearchUnlockResult(
                PostgameResearchUnlockStatus.Applied,
                definition.Id,
                normalizedReceipt,
                definition.ProfileId);
        }

        public bool TrySelectProfile(
            int currentLevel,
            PostgameResearchSaveData state,
            string profileId)
        {
            return currentLevel >= RequiredLevel
                && state != null
                && state.TrySelectProfile(profileId);
        }

        public int CalculateSpentPoints(PostgameResearchSaveData state)
        {
            if (state == null)
            {
                return 0;
            }

            state.EnsureRuntimeDefaults();
            var spent = 0;
            foreach (var definition in PostgameResearchCatalog.All)
            {
                if (state.IsUnlocked(definition.Id))
                {
                    spent += definition.Cost;
                }
            }

            return spent;
        }

        private static PostgameResearchUnlockResult Failure(
            PostgameResearchUnlockStatus status,
            string nodeId,
            string receiptKey)
        {
            return new PostgameResearchUnlockResult(status, nodeId, receiptKey, string.Empty);
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
