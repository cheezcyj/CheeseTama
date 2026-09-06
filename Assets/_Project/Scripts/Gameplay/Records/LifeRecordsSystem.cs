using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Records
{
    public sealed class MiniGameLifeRecord
    {
        internal MiniGameLifeRecord(int highestScore, int totalSessions, int totalSuccesses)
            : this(string.Empty, string.Empty, highestScore, totalSessions, totalSuccesses)
        {
        }

        internal MiniGameLifeRecord(
            string gameId,
            string displayName,
            int highestScore,
            int totalSessions,
            int totalSuccesses)
        {
            GameId = gameId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            HighestScore = Clamp(
                highestScore,
                PlayMiniGameSaveData.MaximumRecordedScore);
            TotalSessions = Clamp(
                totalSessions,
                PlayMiniGameSaveData.MaximumRecordedSessions);
            // One session can contain multiple catches, cleaned spots, jumps, or hits.
            TotalSuccesses = Clamp(
                totalSuccesses,
                PlayMiniGameSaveData.MaximumRecordedSuccesses);
        }

        public string GameId { get; }
        public string DisplayName { get; }
        public int HighestScore { get; }
        public int TotalSessions { get; }
        public int TotalSuccesses { get; }
        public bool HasPlayed => TotalSessions > 0;

        private static int Clamp(int value, int maximum)
        {
            return Math.Max(0, Math.Min(maximum, value));
        }
    }

    public sealed class MiniGameSessionLifeRecord
    {
        internal MiniGameSessionLifeRecord(
            string sessionId,
            string gameId,
            string displayName,
            int score,
            int successes,
            string completedAtIso)
        {
            SessionId = Normalize(sessionId);
            GameId = Normalize(gameId);
            DisplayName = displayName ?? string.Empty;
            Score = Clamp(score, 0, PlayMiniGameSaveData.MaximumRecordedScore);
            Successes = Clamp(
                successes,
                0,
                PlayMiniGameSaveData.MaximumRecordedSuccesses);
            CompletedAtIso = NormalizeTimestamp(completedAtIso);
        }

        public string SessionId { get; }
        public string GameId { get; }
        public string DisplayName { get; }
        public int Score { get; }
        public int Successes { get; }
        public string CompletedAtIso { get; }

        private static string NormalizeTimestamp(string value)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
                ? parsed.ToString("O", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }

    public sealed class SleepLifeRecord
    {
        internal SleepLifeRecord(SleepRecoveryReceiptSaveEntry receipt)
        {
            ReceiptKey = Normalize(receipt?.receiptKey);
            CompletedAtIso = ResolveCompletedAt(receipt);
            ScheduledHours = Clamp(
                receipt?.scheduledHours ?? 0,
                0,
                SleepScheduleSaveData.MaximumScheduledHours);
            ElapsedMinutes = Clamp(
                receipt?.elapsedMinutes ?? 0,
                0,
                SleepScheduleSaveData.MaximumScheduledHours * 60);
            SleepinessDelta = Clamp(receipt?.sleepinessDelta ?? 0, -100, 0);
            HealthDelta = Clamp(receipt?.healthDelta ?? 0, 0, 100);
            MoodDelta = Clamp(receipt?.moodDelta ?? 0, 0, 100);
            WasEarlyWake = receipt?.wasEarlyWake ?? false;
        }

        public string ReceiptKey { get; }
        public string CompletedAtIso { get; }
        public int ScheduledHours { get; }
        public int ElapsedMinutes { get; }
        public int SleepinessDelta { get; }
        public int HealthDelta { get; }
        public int MoodDelta { get; }
        public bool WasEarlyWake { get; }

        private static string ResolveCompletedAt(SleepRecoveryReceiptSaveEntry receipt)
        {
            var claimedAt = NormalizeTimestamp(receipt?.claimedAtIso);
            return !string.IsNullOrEmpty(claimedAt)
                ? claimedAt
                : NormalizeTimestamp(receipt?.wokeAtIso);
        }

        private static string NormalizeTimestamp(string value)
        {
            if (!DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                return string.Empty;
            }

            return parsed.ToString("O", CultureInfo.InvariantCulture);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class NpcKeepsakeLifeRecord
    {
        internal NpcKeepsakeLifeRecord(
            string keepsakeId,
            string title,
            string npcId,
            string npcDisplayName)
        {
            KeepsakeId = keepsakeId ?? string.Empty;
            Title = title ?? string.Empty;
            NpcId = npcId ?? string.Empty;
            NpcDisplayName = npcDisplayName ?? string.Empty;
        }

        public string KeepsakeId { get; }
        public string Title { get; }
        public string NpcId { get; }
        public string NpcDisplayName { get; }
    }

    public sealed class NpcEpisodeLifeRecord
    {
        internal NpcEpisodeLifeRecord(
            NpcRelationshipEpisodeDefinition episode,
            string npcDisplayName,
            NpcRelationshipEpisodeChoiceDefinition choice,
            string completedAtIso)
        {
            EpisodeId = episode?.Id ?? string.Empty;
            NpcId = episode?.NpcId ?? string.Empty;
            NpcDisplayName = npcDisplayName ?? string.Empty;
            Title = episode?.Title ?? string.Empty;
            Description = episode?.Description ?? string.Empty;
            ChoiceId = choice?.Id ?? string.Empty;
            ChoiceLabel = choice?.Label ?? string.Empty;
            ResultMessage = choice?.ResultMessage ?? string.Empty;
            CompletedAtIso = NormalizeTimestamp(completedAtIso);
        }

        public string EpisodeId { get; }
        public string NpcId { get; }
        public string NpcDisplayName { get; }
        public string Title { get; }
        public string Description { get; }
        public string ChoiceId { get; }
        public string ChoiceLabel { get; }
        public string ResultMessage { get; }
        public string CompletedAtIso { get; }
        public bool HasChoiceRecord => !string.IsNullOrEmpty(ChoiceId);

        private static string NormalizeTimestamp(string value)
        {
            if (!DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                return string.Empty;
            }

            return parsed.ToString("O", CultureInfo.InvariantCulture);
        }
    }

    public sealed class LifeRecordsSnapshot
    {
        private static readonly IReadOnlyList<MiniGameSessionLifeRecord> NoMiniGameSessions =
            Array.AsReadOnly(Array.Empty<MiniGameSessionLifeRecord>());
        private static readonly IReadOnlyList<SleepLifeRecord> NoSleeps =
            Array.AsReadOnly(Array.Empty<SleepLifeRecord>());
        private static readonly IReadOnlyList<NpcKeepsakeLifeRecord> NoKeepsakes =
            Array.AsReadOnly(Array.Empty<NpcKeepsakeLifeRecord>());
        private static readonly IReadOnlyList<NpcEpisodeLifeRecord> NoEpisodes =
            Array.AsReadOnly(Array.Empty<NpcEpisodeLifeRecord>());

        internal LifeRecordsSnapshot(
            MiniGameLifeRecord milkDrop,
            MiniGameLifeRecord cleaning,
            MiniGameLifeRecord bouncyJump,
            MiniGameLifeRecord blueBall,
            IList<MiniGameSessionLifeRecord> recentMiniGameSessions,
            IList<SleepLifeRecord> sleeps,
            IList<NpcKeepsakeLifeRecord> keepsakes,
            IList<NpcEpisodeLifeRecord> completedEpisodes)
        {
            MilkDrop = milkDrop ?? EmptyMiniGame(
                MiniGameRecordIds.MilkDrop,
                "우유방울 받기");
            Cleaning = cleaning ?? EmptyMiniGame(
                MiniGameRecordIds.Cleaning,
                "청소하기");
            BouncyJump = bouncyJump ?? EmptyMiniGame(
                MiniGameRecordIds.BouncyJump,
                "말랑 점프");
            BlueBall = blueBall ?? EmptyMiniGame(
                MiniGameRecordIds.BlueBall,
                "파란 공 놀이");
            MiniGames = Array.AsReadOnly(new[]
            {
                MilkDrop,
                Cleaning,
                BouncyJump,
                BlueBall
            });
            RecentMiniGameSessions = ToReadOnly(
                recentMiniGameSessions,
                NoMiniGameSessions);
            Sleeps = ToReadOnly(sleeps, NoSleeps);
            RecentSleep = Sleeps.Count > 0 ? Sleeps[0] : null;
            Keepsakes = ToReadOnly(keepsakes, NoKeepsakes);
            CompletedEpisodes = ToReadOnly(completedEpisodes, NoEpisodes);
        }

        public static LifeRecordsSnapshot Empty => new LifeRecordsSnapshot(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        public MiniGameLifeRecord MilkDrop { get; }
        public MiniGameLifeRecord Cleaning { get; }
        public MiniGameLifeRecord BouncyJump { get; }
        public MiniGameLifeRecord BlueBall { get; }
        public IReadOnlyList<MiniGameLifeRecord> MiniGames { get; }
        public IReadOnlyList<MiniGameSessionLifeRecord> RecentMiniGameSessions { get; }
        public SleepLifeRecord RecentSleep { get; }
        public IReadOnlyList<SleepLifeRecord> Sleeps { get; }
        public IReadOnlyList<NpcKeepsakeLifeRecord> Keepsakes { get; }
        public IReadOnlyList<NpcEpisodeLifeRecord> CompletedEpisodes { get; }
        public bool HasRecentSleep => RecentSleep != null;
        public bool HasAnyRecord => MilkDrop.HasPlayed
            || Cleaning.HasPlayed
            || BouncyJump.HasPlayed
            || BlueBall.HasPlayed
            || RecentMiniGameSessions.Count > 0
            || HasRecentSleep
            || Keepsakes.Count > 0
            || CompletedEpisodes.Count > 0;

        private static IReadOnlyList<T> ToReadOnly<T>(
            IList<T> source,
            IReadOnlyList<T> empty)
        {
            if (source == null || source.Count == 0)
            {
                return empty;
            }

            var copy = new T[source.Count];
            source.CopyTo(copy, 0);
            return new ReadOnlyCollection<T>(copy);
        }

        private static MiniGameLifeRecord EmptyMiniGame(string gameId, string displayName)
        {
            return new MiniGameLifeRecord(gameId, displayName, 0, 0, 0);
        }
    }

    /// <summary>
    /// Builds a public, read-only album snapshot from existing save DTOs. Unknown and
    /// unearned NPC content is intentionally omitted rather than represented by a locked card.
    /// The source save is never normalized or mutated while the album is being observed.
    /// </summary>
    public sealed class LifeRecordsSystem
    {
        private readonly NpcRelationshipEpisodeSystem episodeSystem =
            new NpcRelationshipEpisodeSystem();
        private readonly NpcVisitSystem visitSystem = new NpcVisitSystem();

        public LifeRecordsSnapshot BuildSnapshot(CheeseTamaSaveData saveData)
        {
            if (saveData == null)
            {
                return LifeRecordsSnapshot.Empty;
            }

            var miniGame = saveData.playMiniGames;
            var milkDropRecord = new MiniGameLifeRecord(
                MiniGameRecordIds.MilkDrop,
                "우유방울 받기",
                miniGame?.highestMilkDropScore ?? 0,
                miniGame?.totalMilkDropSessions ?? 0,
                miniGame?.totalMilkDropSuccesses ?? 0);
            var cleaningRecord = new MiniGameLifeRecord(
                MiniGameRecordIds.Cleaning,
                "청소하기",
                miniGame?.highestCleaningScore ?? 0,
                miniGame?.totalCleaningSessions ?? 0,
                miniGame?.totalCleaningSuccesses ?? 0);
            var bouncyJumpRecord = new MiniGameLifeRecord(
                MiniGameRecordIds.BouncyJump,
                "말랑 점프",
                miniGame?.highestBouncyJumpScore ?? 0,
                miniGame?.totalBouncyJumpSessions ?? 0,
                miniGame?.totalBouncyJumpSuccesses ?? 0);
            var blueBallRecord = new MiniGameLifeRecord(
                MiniGameRecordIds.BlueBall,
                "파란 공 놀이",
                miniGame?.highestBlueBallScore ?? 0,
                miniGame?.totalBlueBallSessions ?? 0,
                miniGame?.totalBlueBallSuccesses ?? 0);
            var recentMiniGameSessions = BuildRecentMiniGameSessions(
                miniGame?.recentSessions);
            var sleeps = BuildRecentSleeps(saveData.sleepSchedule?.recoveryReceipts);
            var earnedEpisodes = BuildEpisodeRecords(saveData.npcRelationshipEpisodes);
            var keepsakes = BuildKeepsakeRecords(
                saveData.npcRelationshipEpisodes,
                earnedEpisodes);

            return new LifeRecordsSnapshot(
                milkDropRecord,
                cleaningRecord,
                bouncyJumpRecord,
                blueBallRecord,
                recentMiniGameSessions,
                sleeps,
                keepsakes,
                earnedEpisodes);
        }

        private static List<MiniGameSessionLifeRecord> BuildRecentMiniGameSessions(
            IReadOnlyList<MiniGameSessionSaveEntry> source)
        {
            var records = new List<MiniGameSessionCandidate>();
            if (source == null)
            {
                return new List<MiniGameSessionLifeRecord>();
            }

            var knownSessionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < source.Count; index += 1)
            {
                var entry = source[index];
                var sessionId = Normalize(entry?.sessionId);
                var gameId = Normalize(entry?.gameId);
                var displayName = ResolveMiniGameDisplayName(gameId);
                if (string.IsNullOrEmpty(sessionId)
                    || string.IsNullOrEmpty(displayName)
                    || !knownSessionIds.Add(sessionId))
                {
                    continue;
                }

                var hasTimestamp = DateTimeOffset.TryParse(
                    entry?.completedAtIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var timestamp);
                records.Add(new MiniGameSessionCandidate(
                    new MiniGameSessionLifeRecord(
                        sessionId,
                        gameId,
                        displayName,
                        entry?.score ?? 0,
                        entry?.successes ?? 0,
                        entry?.completedAtIso),
                    index,
                    hasTimestamp,
                    timestamp));
            }

            records.Sort(CompareMiniGameSessions);
            var visibleCount = Math.Min(
                PlayMiniGameSaveData.MaximumRecentSessions,
                records.Count);
            var result = new List<MiniGameSessionLifeRecord>(visibleCount);
            for (var index = 0; index < visibleCount; index += 1)
            {
                result.Add(records[index].Record);
            }

            return result;
        }

        private static int CompareMiniGameSessions(
            MiniGameSessionCandidate left,
            MiniGameSessionCandidate right)
        {
            if (left.HasTimestamp != right.HasTimestamp)
            {
                return left.HasTimestamp ? -1 : 1;
            }

            if (left.HasTimestamp)
            {
                var timestampOrder = right.Timestamp.CompareTo(left.Timestamp);
                if (timestampOrder != 0)
                {
                    return timestampOrder;
                }
            }

            return left.SourceIndex.CompareTo(right.SourceIndex);
        }

        private static string ResolveMiniGameDisplayName(string gameId)
        {
            switch (gameId)
            {
                case MiniGameRecordIds.MilkDrop:
                    return "우유방울 받기";
                case MiniGameRecordIds.Cleaning:
                    return "청소하기";
                case MiniGameRecordIds.BouncyJump:
                    return "말랑 점프";
                case MiniGameRecordIds.BlueBall:
                    return "파란 공 놀이";
                default:
                    return string.Empty;
            }
        }

        private static List<SleepLifeRecord> BuildRecentSleeps(
            IReadOnlyList<SleepRecoveryReceiptSaveEntry> receipts)
        {
            const int maximumPublicHistory = 5;
            var records = new List<SleepLifeRecord>(maximumPublicHistory);
            if (receipts == null) return records;

            var knownReceiptKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var index = receipts.Count - 1;
                index >= 0 && records.Count < maximumPublicHistory;
                index -= 1)
            {
                var receipt = receipts[index];
                var receiptKey = Normalize(receipt?.receiptKey);
                if (!string.IsNullOrEmpty(receiptKey) && knownReceiptKeys.Add(receiptKey))
                {
                    records.Add(new SleepLifeRecord(receipt));
                }
            }

            return records;
        }

        private List<NpcEpisodeLifeRecord> BuildEpisodeRecords(
            NpcRelationshipEpisodeSaveData state)
        {
            var records = new List<NpcEpisodeLifeRecord>();
            if (state == null)
            {
                return records;
            }

            var earnedIds = new List<string>();
            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            AddEarnedEpisodeIds(state.completedEpisodeIds, earnedIds, knownIds);

            if (state.receipts != null)
            {
                for (var index = 0; index < state.receipts.Count; index += 1)
                {
                    AddEarnedEpisodeId(
                        state.receipts[index]?.episodeId,
                        earnedIds,
                        knownIds);
                }
            }

            for (var index = 0; index < earnedIds.Count; index += 1)
            {
                var episode = episodeSystem.Find(earnedIds[index]);
                if (episode == null)
                {
                    continue;
                }

                var receipt = FindNewestReceipt(state.receipts, episode.Id);
                var choice = episode.FindChoice(receipt?.choiceId);
                records.Add(new NpcEpisodeLifeRecord(
                    episode,
                    visitSystem.Find(episode.NpcId)?.DisplayName,
                    choice,
                    receipt?.completedAtIso));
            }

            return records;
        }

        private List<NpcKeepsakeLifeRecord> BuildKeepsakeRecords(
            NpcRelationshipEpisodeSaveData state,
            IReadOnlyList<NpcEpisodeLifeRecord> earnedEpisodes)
        {
            var ids = new List<string>();
            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            if (state?.keepsakeIds != null)
            {
                for (var index = 0; index < state.keepsakeIds.Count; index += 1)
                {
                    AddKnownKeepsakeId(state.keepsakeIds[index], ids, knownIds);
                }
            }

            // A completed known episode is durable proof that its keepsake was earned.
            // This keeps a partially migrated save presentable without modifying it.
            for (var index = 0; index < earnedEpisodes.Count; index += 1)
            {
                var episode = episodeSystem.Find(earnedEpisodes[index].EpisodeId);
                if (episode == null)
                {
                    continue;
                }

                var choice = episode.FindChoice(earnedEpisodes[index].ChoiceId);
                var keepsakeId = choice?.RewardKeepsakeId;
                if (string.IsNullOrEmpty(keepsakeId))
                {
                    for (var choiceIndex = 0;
                        choiceIndex < episode.Choices.Count && string.IsNullOrEmpty(keepsakeId);
                        choiceIndex += 1)
                    {
                        keepsakeId = episode.Choices[choiceIndex]?.RewardKeepsakeId;
                    }
                }

                AddKnownKeepsakeId(keepsakeId, ids, knownIds);
            }

            var records = new List<NpcKeepsakeLifeRecord>(ids.Count);
            for (var index = 0; index < ids.Count; index += 1)
            {
                if (!TryResolveKeepsake(
                        ids[index],
                        out var title,
                        out var npcId))
                {
                    continue;
                }

                records.Add(new NpcKeepsakeLifeRecord(
                    ids[index],
                    title,
                    npcId,
                    visitSystem.Find(npcId)?.DisplayName));
            }

            return records;
        }

        private void AddEarnedEpisodeIds(
            IReadOnlyList<string> source,
            ICollection<string> destination,
            ISet<string> knownIds)
        {
            if (source == null)
            {
                return;
            }

            for (var index = 0; index < source.Count; index += 1)
            {
                AddEarnedEpisodeId(source[index], destination, knownIds);
            }
        }

        private void AddEarnedEpisodeId(
            string episodeId,
            ICollection<string> destination,
            ISet<string> knownIds)
        {
            var normalized = Normalize(episodeId);
            if (string.IsNullOrEmpty(normalized)
                || episodeSystem.Find(normalized) == null
                || !knownIds.Add(normalized))
            {
                return;
            }

            destination.Add(normalized);
        }

        private static NpcRelationshipEpisodeReceiptSaveData FindNewestReceipt(
            IReadOnlyList<NpcRelationshipEpisodeReceiptSaveData> receipts,
            string episodeId)
        {
            if (receipts == null)
            {
                return null;
            }

            for (var index = receipts.Count - 1; index >= 0; index -= 1)
            {
                var receipt = receipts[index];
                if (receipt != null
                    && string.Equals(
                        Normalize(receipt.episodeId),
                        episodeId,
                        StringComparison.Ordinal))
                {
                    return receipt;
                }
            }

            return null;
        }

        private static void AddKnownKeepsakeId(
            string keepsakeId,
            ICollection<string> destination,
            ISet<string> knownIds)
        {
            var normalized = Normalize(keepsakeId);
            if (!TryResolveKeepsake(normalized, out _, out _)
                || !knownIds.Add(normalized))
            {
                return;
            }

            destination.Add(normalized);
        }

        private static bool TryResolveKeepsake(
            string keepsakeId,
            out string title,
            out string npcId)
        {
            switch (Normalize(keepsakeId))
            {
                case NpcRelationshipKeepsakeIds.DoctorHealthNotebook:
                    title = "건강 수첩";
                    npcId = NpcVisitSystem.MilkyDoctorId;
                    return true;
                case NpcRelationshipKeepsakeIds.DoctorSmallStethoscope:
                    title = "작은 청진기";
                    npcId = NpcVisitSystem.MilkyDoctorId;
                    return true;
                case NpcRelationshipKeepsakeIds.FairyScentSachet:
                    title = "향기 주머니";
                    npcId = NpcVisitSystem.FermentationFairyId;
                    return true;
                case NpcRelationshipKeepsakeIds.FairyFermentationBell:
                    title = "발효 종";
                    npcId = NpcVisitSystem.FermentationFairyId;
                    return true;
                case NpcRelationshipKeepsakeIds.CatPawMap:
                    title = "발자국 지도";
                    npcId = NpcVisitSystem.MilkCatId;
                    return true;
                case NpcRelationshipKeepsakeIds.CatStarCompass:
                    title = "별 나침반";
                    npcId = NpcVisitSystem.MilkCatId;
                    return true;
                default:
                    title = string.Empty;
                    npcId = string.Empty;
                    return false;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private sealed class MiniGameSessionCandidate
        {
            public MiniGameSessionCandidate(
                MiniGameSessionLifeRecord record,
                int sourceIndex,
                bool hasTimestamp,
                DateTimeOffset timestamp)
            {
                Record = record;
                SourceIndex = sourceIndex;
                HasTimestamp = hasTimestamp;
                Timestamp = timestamp;
            }

            public MiniGameSessionLifeRecord Record { get; }
            public int SourceIndex { get; }
            public bool HasTimestamp { get; }
            public DateTimeOffset Timestamp { get; }
        }
    }
}
