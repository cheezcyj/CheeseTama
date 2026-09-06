using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CheeseTama.Gameplay.Records;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.MiniGames
{
    public enum MiniGameMasteryMedal
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3
    }

    public sealed class MiniGameMasteryAchievementSnapshot
    {
        internal MiniGameMasteryAchievementSnapshot(
            string gameId,
            string gameDisplayName,
            MiniGameMasteryMedal medal,
            int scoreThreshold,
            string badgeNoun,
            string titleDisplayName,
            MiniGameMasteryReceiptSaveData receipt)
        {
            GameId = gameId ?? string.Empty;
            GameDisplayName = gameDisplayName ?? string.Empty;
            Medal = medal;
            MedalId = MiniGameMasterySystem.GetMedalId(medal);
            MedalDisplayName = MiniGameMasterySystem.GetMedalDisplayName(medal);
            ScoreThreshold = Math.Max(0, scoreThreshold);
            BadgeId = "mini_game_badge."
                + GameId
                + "."
                + MedalId;
            BadgeDisplayName = MedalDisplayName
                + "빛 "
                + (badgeNoun ?? string.Empty)
                + " 배지";
            TitleId = "mini_game_title."
                + GameId
                + "."
                + MedalId;
            TitleDisplayName = titleDisplayName ?? string.Empty;
            ReceiptId = receipt?.receiptId ?? string.Empty;
            AchievedAtIso = receipt?.achievedAtIso ?? string.Empty;
        }

        public string GameId { get; }
        public string GameDisplayName { get; }
        public MiniGameMasteryMedal Medal { get; }
        public string MedalId { get; }
        public string MedalDisplayName { get; }
        public int ScoreThreshold { get; }
        public string BadgeId { get; }
        public string BadgeDisplayName { get; }
        public string TitleId { get; }
        public string TitleDisplayName { get; }
        public string ReceiptId { get; }
        public string AchievedAtIso { get; }
        public bool ReceiptRecorded => !string.IsNullOrEmpty(ReceiptId);
    }

    public sealed class MiniGameMasteryProgressSnapshot
    {
        internal MiniGameMasteryProgressSnapshot(
            string gameId,
            string displayName,
            int highestScore,
            int totalSessions,
            int totalSuccesses,
            int bronzeThreshold,
            int silverThreshold,
            int goldThreshold,
            MiniGameMasteryMedal currentMedal,
            MiniGameMasteryMedal nextMedal,
            int nextScoreThreshold,
            int scoreRemaining,
            int nextProgressPercent,
            IList<MiniGameMasteryAchievementSnapshot> unlockedAchievements)
        {
            GameId = gameId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            HighestScore = ClampScore(highestScore);
            TotalSessions = ClampCount(totalSessions);
            TotalSuccesses = ClampCount(totalSuccesses);
            BronzeScoreThreshold = Math.Max(1, bronzeThreshold);
            SilverScoreThreshold = Math.Max(BronzeScoreThreshold + 1, silverThreshold);
            GoldScoreThreshold = Math.Max(SilverScoreThreshold + 1, goldThreshold);
            CurrentMedal = currentMedal;
            NextMedal = nextMedal;
            NextScoreThreshold = Math.Max(0, nextScoreThreshold);
            ScoreRemaining = Math.Max(0, scoreRemaining);
            NextProgressPercent = Math.Max(0, Math.Min(100, nextProgressPercent));
            UnlockedAchievements = ToReadOnly(unlockedAchievements);
            CurrentAchievement = UnlockedAchievements.Count > 0
                ? UnlockedAchievements[UnlockedAchievements.Count - 1]
                : null;
        }

        public string GameId { get; }
        public string DisplayName { get; }
        public int HighestScore { get; }
        public int TotalSessions { get; }
        public int TotalSuccesses { get; }
        public int BronzeScoreThreshold { get; }
        public int SilverScoreThreshold { get; }
        public int GoldScoreThreshold { get; }
        public MiniGameMasteryMedal CurrentMedal { get; }
        public MiniGameMasteryMedal NextMedal { get; }
        public int NextScoreThreshold { get; }
        public int ScoreRemaining { get; }
        public int NextProgressPercent { get; }
        public IReadOnlyList<MiniGameMasteryAchievementSnapshot> UnlockedAchievements { get; }
        public MiniGameMasteryAchievementSnapshot CurrentAchievement { get; }
        public bool HasPlayed => TotalSessions > 0 || HighestScore > 0;
        public bool HasMedal => CurrentMedal != MiniGameMasteryMedal.None;
        public bool IsGold => CurrentMedal == MiniGameMasteryMedal.Gold;
        public int UnlockedMedalCount => (int)CurrentMedal;

        private static int ClampScore(int value)
        {
            return Math.Max(0, Math.Min(PlayMiniGameSaveData.MaximumRecordedScore, value));
        }

        private static int ClampCount(int value)
        {
            return Math.Max(0, Math.Min(PlayMiniGameSaveData.MaximumRecordedSuccesses, value));
        }

        private static IReadOnlyList<MiniGameMasteryAchievementSnapshot> ToReadOnly(
            IList<MiniGameMasteryAchievementSnapshot> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<MiniGameMasteryAchievementSnapshot>());
            }

            var copy = new MiniGameMasteryAchievementSnapshot[source.Count];
            source.CopyTo(copy, 0);
            return new ReadOnlyCollection<MiniGameMasteryAchievementSnapshot>(copy);
        }
    }

    public sealed class MiniGameMasteryBoardSnapshot
    {
        internal MiniGameMasteryBoardSnapshot(
            IList<MiniGameMasteryProgressSnapshot> games,
            IList<MiniGameMasteryAchievementSnapshot> unlockedAchievements)
        {
            Games = ToReadOnly(games);
            UnlockedAchievements = ToReadOnly(unlockedAchievements);

            var goldCount = 0;
            for (var index = 0; index < Games.Count; index += 1)
            {
                if (Games[index].IsGold)
                {
                    goldCount += 1;
                }
            }

            GoldMedalCount = goldCount;
        }

        public static MiniGameMasteryBoardSnapshot Empty =>
            new MiniGameMasterySystem().BuildSnapshot(null, null);

        public IReadOnlyList<MiniGameMasteryProgressSnapshot> Games { get; }
        public IReadOnlyList<MiniGameMasteryAchievementSnapshot> UnlockedAchievements { get; }
        public int MedalCount => UnlockedAchievements.Count;
        public int GoldMedalCount { get; }
        public int MaximumMedalCount => MiniGameMasterySystem.MaximumMedalCount;
        public int MaximumGoldMedalCount => MiniGameMasterySystem.GameCount;
        public bool HasAnyScore
        {
            get
            {
                for (var index = 0; index < Games.Count; index += 1)
                {
                    if (Games[index].HighestScore > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool AllGold => GoldMedalCount == MaximumGoldMedalCount;

        public MiniGameMasteryProgressSnapshot Find(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
            {
                return null;
            }

            var normalized = gameId.Trim();
            for (var index = 0; index < Games.Count; index += 1)
            {
                if (string.Equals(Games[index].GameId, normalized, StringComparison.Ordinal))
                {
                    return Games[index];
                }
            }

            return null;
        }

        private static IReadOnlyList<T> ToReadOnly<T>(IList<T> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<T>());
            }

            var copy = new T[source.Count];
            source.CopyTo(copy, 0);
            return new ReadOnlyCollection<T>(copy);
        }
    }

    public sealed class MiniGameMasteryReconcileResult
    {
        internal MiniGameMasteryReconcileResult(
            MiniGameMasterySaveData state,
            MiniGameMasteryBoardSnapshot snapshot,
            IList<MiniGameMasteryAchievementSnapshot> newlyUnlocked,
            bool stateChanged)
        {
            State = state ?? new MiniGameMasterySaveData();
            Snapshot = snapshot ?? MiniGameMasteryBoardSnapshot.Empty;
            NewlyUnlocked = ToReadOnly(newlyUnlocked);
            StateChanged = stateChanged;
        }

        public MiniGameMasterySaveData State { get; }
        public MiniGameMasteryBoardSnapshot Snapshot { get; }
        public IReadOnlyList<MiniGameMasteryAchievementSnapshot> NewlyUnlocked { get; }
        public bool StateChanged { get; }
        public bool HasNewUnlocks => NewlyUnlocked.Count > 0;

        private static IReadOnlyList<MiniGameMasteryAchievementSnapshot> ToReadOnly(
            IList<MiniGameMasteryAchievementSnapshot> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<MiniGameMasteryAchievementSnapshot>());
            }

            var copy = new MiniGameMasteryAchievementSnapshot[source.Count];
            source.CopyTo(copy, 0);
            return new ReadOnlyCollection<MiniGameMasteryAchievementSnapshot>(copy);
        }
    }

    /// <summary>
    /// Derives stable medal mastery exclusively from LifeRecordsSnapshot highest scores.
    /// Reconciliation writes first-achievement receipts only; it never touches the economy.
    /// </summary>
    public sealed class MiniGameMasterySystem
    {
        public const int GameCount = 4;
        public const int MedalCountPerGame = 3;
        public const int MaximumMedalCount = GameCount * MedalCountPerGame;

        public const int MilkDropBronzeScore = 500;
        public const int MilkDropSilverScore = 1500;
        public const int MilkDropGoldScore = 3000;

        public const int CleaningBronzeScore = 600;
        public const int CleaningSilverScore = 1200;
        public const int CleaningGoldScore = 2000;

        public const int BouncyJumpBronzeScore = 600;
        public const int BouncyJumpSilverScore = 1500;
        public const int BouncyJumpGoldScore = 3000;

        public const int BlueBallBronzeScore = 500;
        public const int BlueBallSilverScore = 1500;
        public const int BlueBallGoldScore = 3000;

        private static readonly MiniGameMasteryDefinition[] Definitions =
        {
            new MiniGameMasteryDefinition(
                MiniGameRecordIds.MilkDrop,
                "우유방울 받기",
                "방울",
                MilkDropBronzeScore,
                MilkDropSilverScore,
                MilkDropGoldScore,
                "방울을 따라가는 손",
                "반짝임을 모은 손",
                "우유방울의 별"),
            new MiniGameMasteryDefinition(
                MiniGameRecordIds.Cleaning,
                "청소하기",
                "거품",
                CleaningBronzeScore,
                CleaningSilverScore,
                CleaningGoldScore,
                "작은 얼룩 해결사",
                "밀크룸의 반짝임",
                "말끔한 하루의 장인"),
            new MiniGameMasteryDefinition(
                MiniGameRecordIds.BouncyJump,
                "말랑 점프",
                "말랑",
                BouncyJumpBronzeScore,
                BouncyJumpSilverScore,
                BouncyJumpGoldScore,
                "통통 튀는 새싹",
                "말랑 리듬 수집가",
                "구름 위의 점프 장인"),
            new MiniGameMasteryDefinition(
                MiniGameRecordIds.BlueBall,
                "파란 공 놀이",
                "파란 공",
                BlueBallBronzeScore,
                BlueBallSilverScore,
                BlueBallGoldScore,
                "파란 공의 친구",
                "말랑 패스의 달인",
                "푸른 궤적의 별")
        };

        public MiniGameMasteryBoardSnapshot BuildSnapshot(
            LifeRecordsSnapshot records,
            MiniGameMasterySaveData state)
        {
            var source = records ?? LifeRecordsSnapshot.Empty;
            var games = new List<MiniGameMasteryProgressSnapshot>(Definitions.Length);
            var allAchievements = new List<MiniGameMasteryAchievementSnapshot>(
                MaximumMedalCount);
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                var definition = Definitions[index];
                var record = ResolveRecord(source, definition.GameId);
                var game = BuildProgress(definition, record, state);
                games.Add(game);
                for (var achievementIndex = 0;
                    achievementIndex < game.UnlockedAchievements.Count;
                    achievementIndex += 1)
                {
                    allAchievements.Add(game.UnlockedAchievements[achievementIndex]);
                }
            }

            return new MiniGameMasteryBoardSnapshot(games, allAchievements);
        }

        public MiniGameMasteryReconcileResult Reconcile(
            LifeRecordsSnapshot records,
            MiniGameMasterySaveData state)
        {
            return Reconcile(records, state, DateTimeOffset.UtcNow);
        }

        public MiniGameMasteryReconcileResult Reconcile(
            LifeRecordsSnapshot records,
            MiniGameMasterySaveData state,
            DateTimeOffset achievedAt)
        {
            var stateChanged = state == null;
            var normalizedState = state ?? new MiniGameMasterySaveData();
            stateChanged |= normalizedState.EnsureRuntimeDefaults();
            var source = records ?? LifeRecordsSnapshot.Empty;
            var newlyUnlocked = new List<MiniGameMasteryAchievementSnapshot>();

            if (achievedAt != default)
            {
                for (var definitionIndex = 0;
                    definitionIndex < Definitions.Length;
                    definitionIndex += 1)
                {
                    var definition = Definitions[definitionIndex];
                    var record = ResolveRecord(source, definition.GameId);
                    var highestScore = ClampScore(record?.HighestScore ?? 0);
                    for (var medalValue = (int)MiniGameMasteryMedal.Bronze;
                        medalValue <= (int)MiniGameMasteryMedal.Gold;
                        medalValue += 1)
                    {
                        var medal = (MiniGameMasteryMedal)medalValue;
                        if (highestScore < definition.GetThreshold(medal)
                            || normalizedState.HasMasteryReceipt(
                                definition.GameId,
                                GetMedalId(medal)))
                        {
                            continue;
                        }

                        if (normalizedState.TryRecordMasteryReceipt(
                                definition.GameId,
                                GetMedalId(medal),
                                achievedAt,
                                out var receipt))
                        {
                            stateChanged = true;
                            newlyUnlocked.Add(CreateAchievement(
                                definition,
                                medal,
                                receipt));
                        }
                    }
                }
            }

            return new MiniGameMasteryReconcileResult(
                normalizedState,
                BuildSnapshot(source, normalizedState),
                newlyUnlocked,
                stateChanged);
        }

        public static string GetMedalId(MiniGameMasteryMedal medal)
        {
            return medal switch
            {
                MiniGameMasteryMedal.Bronze => MiniGameMasterySaveData.BronzeMedalId,
                MiniGameMasteryMedal.Silver => MiniGameMasterySaveData.SilverMedalId,
                MiniGameMasteryMedal.Gold => MiniGameMasterySaveData.GoldMedalId,
                _ => string.Empty
            };
        }

        public static string GetMedalDisplayName(MiniGameMasteryMedal medal)
        {
            return medal switch
            {
                MiniGameMasteryMedal.Bronze => "동",
                MiniGameMasteryMedal.Silver => "은",
                MiniGameMasteryMedal.Gold => "금",
                _ => "없음"
            };
        }

        private static MiniGameMasteryProgressSnapshot BuildProgress(
            MiniGameMasteryDefinition definition,
            MiniGameLifeRecord record,
            MiniGameMasterySaveData state)
        {
            var highestScore = ClampScore(record?.HighestScore ?? 0);
            var currentMedal = ResolveMedal(definition, highestScore);
            var nextMedal = ResolveNextMedal(currentMedal);
            var nextThreshold = nextMedal == MiniGameMasteryMedal.None
                ? 0
                : definition.GetThreshold(nextMedal);
            var remaining = nextThreshold <= 0
                ? 0
                : Math.Max(0, nextThreshold - highestScore);
            var progress = nextThreshold <= 0
                ? 100
                : Math.Max(0, Math.Min(100, (int)((long)highestScore * 100 / nextThreshold)));
            var achievements = new List<MiniGameMasteryAchievementSnapshot>(
                (int)currentMedal);
            for (var medalValue = (int)MiniGameMasteryMedal.Bronze;
                medalValue <= (int)currentMedal;
                medalValue += 1)
            {
                var medal = (MiniGameMasteryMedal)medalValue;
                achievements.Add(CreateAchievement(
                    definition,
                    medal,
                    state?.FindMasteryReceipt(definition.GameId, GetMedalId(medal))));
            }

            return new MiniGameMasteryProgressSnapshot(
                definition.GameId,
                definition.DisplayName,
                highestScore,
                record?.TotalSessions ?? 0,
                record?.TotalSuccesses ?? 0,
                definition.BronzeThreshold,
                definition.SilverThreshold,
                definition.GoldThreshold,
                currentMedal,
                nextMedal,
                nextThreshold,
                remaining,
                progress,
                achievements);
        }

        private static MiniGameMasteryAchievementSnapshot CreateAchievement(
            MiniGameMasteryDefinition definition,
            MiniGameMasteryMedal medal,
            MiniGameMasteryReceiptSaveData receipt)
        {
            return new MiniGameMasteryAchievementSnapshot(
                definition.GameId,
                definition.DisplayName,
                medal,
                definition.GetThreshold(medal),
                definition.BadgeNoun,
                definition.GetTitle(medal),
                receipt);
        }

        private static MiniGameMasteryMedal ResolveMedal(
            MiniGameMasteryDefinition definition,
            int score)
        {
            if (score >= definition.GoldThreshold)
            {
                return MiniGameMasteryMedal.Gold;
            }

            if (score >= definition.SilverThreshold)
            {
                return MiniGameMasteryMedal.Silver;
            }

            return score >= definition.BronzeThreshold
                ? MiniGameMasteryMedal.Bronze
                : MiniGameMasteryMedal.None;
        }

        private static MiniGameMasteryMedal ResolveNextMedal(
            MiniGameMasteryMedal current)
        {
            return current switch
            {
                MiniGameMasteryMedal.None => MiniGameMasteryMedal.Bronze,
                MiniGameMasteryMedal.Bronze => MiniGameMasteryMedal.Silver,
                MiniGameMasteryMedal.Silver => MiniGameMasteryMedal.Gold,
                _ => MiniGameMasteryMedal.None
            };
        }

        private static MiniGameLifeRecord ResolveRecord(
            LifeRecordsSnapshot records,
            string gameId)
        {
            return gameId switch
            {
                MiniGameRecordIds.MilkDrop => records.MilkDrop,
                MiniGameRecordIds.Cleaning => records.Cleaning,
                MiniGameRecordIds.BouncyJump => records.BouncyJump,
                MiniGameRecordIds.BlueBall => records.BlueBall,
                _ => null
            };
        }

        private static int ClampScore(int value)
        {
            return Math.Max(0, Math.Min(PlayMiniGameSaveData.MaximumRecordedScore, value));
        }

        private sealed class MiniGameMasteryDefinition
        {
            public MiniGameMasteryDefinition(
                string gameId,
                string displayName,
                string badgeNoun,
                int bronzeThreshold,
                int silverThreshold,
                int goldThreshold,
                string bronzeTitle,
                string silverTitle,
                string goldTitle)
            {
                GameId = gameId;
                DisplayName = displayName;
                BadgeNoun = badgeNoun;
                BronzeThreshold = bronzeThreshold;
                SilverThreshold = silverThreshold;
                GoldThreshold = goldThreshold;
                BronzeTitle = bronzeTitle;
                SilverTitle = silverTitle;
                GoldTitle = goldTitle;
            }

            public string GameId { get; }
            public string DisplayName { get; }
            public string BadgeNoun { get; }
            public int BronzeThreshold { get; }
            public int SilverThreshold { get; }
            public int GoldThreshold { get; }
            public string BronzeTitle { get; }
            public string SilverTitle { get; }
            public string GoldTitle { get; }

            public int GetThreshold(MiniGameMasteryMedal medal)
            {
                return medal switch
                {
                    MiniGameMasteryMedal.Bronze => BronzeThreshold,
                    MiniGameMasteryMedal.Silver => SilverThreshold,
                    MiniGameMasteryMedal.Gold => GoldThreshold,
                    _ => 0
                };
            }

            public string GetTitle(MiniGameMasteryMedal medal)
            {
                return medal switch
                {
                    MiniGameMasteryMedal.Bronze => BronzeTitle,
                    MiniGameMasteryMedal.Silver => SilverTitle,
                    MiniGameMasteryMedal.Gold => GoldTitle,
                    _ => string.Empty
                };
            }
        }
    }
}
