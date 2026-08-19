using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Data;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.NpcVisits
{
    public static class NpcRelationshipEpisodeIds
    {
        public const string DoctorFriend = "doctor_friend_health_notebook";
        public const string DoctorTrustedFriend = "doctor_trusted_care_promise";
        public const string FairyFriend = "fairy_friend_scent_memory";
        public const string FairyTrustedFriend = "fairy_trusted_fermentation_promise";
        public const string CatFriend = "cat_friend_paw_map";
        public const string CatTrustedFriend = "cat_trusted_secret_route";
    }

    public static class NpcRelationshipKeepsakeIds
    {
        public const string DoctorHealthNotebook = "keepsake_doctor_health_notebook";
        public const string DoctorSmallStethoscope = "keepsake_doctor_small_stethoscope";
        public const string FairyScentSachet = "keepsake_fairy_scent_sachet";
        public const string FairyFermentationBell = "keepsake_fairy_fermentation_bell";
        public const string CatPawMap = "keepsake_cat_paw_map";
        public const string CatStarCompass = "keepsake_cat_star_compass";
    }

    public sealed class NpcRelationshipEpisodeChoiceDefinition
    {
        public NpcRelationshipEpisodeChoiceDefinition(
            string id,
            string label,
            string resultMessage,
            int affinity,
            StatEffect statEffect,
            string memoryTitle,
            string memoryDetail,
            string rewardDecorationId = "",
            string rewardKeepsakeId = "")
        {
            Id = Normalize(id);
            Label = label ?? string.Empty;
            ResultMessage = resultMessage ?? string.Empty;
            Affinity = Math.Max(0, affinity);
            StatEffect = statEffect;
            MemoryTitle = memoryTitle ?? string.Empty;
            MemoryDetail = memoryDetail ?? string.Empty;
            RewardDecorationId = Normalize(rewardDecorationId);
            RewardKeepsakeId = Normalize(rewardKeepsakeId);
        }

        public string Id { get; }
        public string Label { get; }
        public string ResultMessage { get; }
        public int Affinity { get; }
        public StatEffect StatEffect { get; }
        public string MemoryTitle { get; }
        public string MemoryDetail { get; }
        public string RewardDecorationId { get; }
        public string RewardKeepsakeId { get; }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }

    public sealed class NpcRelationshipEpisodeDefinition
    {
        public NpcRelationshipEpisodeDefinition(
            string id,
            string npcId,
            string title,
            string description,
            NpcRelationshipTier requiredTier,
            int minimumAffinity,
            string prerequisiteEpisodeId,
            NpcRelationshipEpisodeChoiceDefinition[] choices)
        {
            Id = Normalize(id);
            NpcId = Normalize(npcId);
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            RequiredTier = requiredTier;
            MinimumAffinity = Math.Max(0, Math.Min(99, minimumAffinity));
            PrerequisiteEpisodeId = Normalize(prerequisiteEpisodeId);
            Choices = choices ?? Array.Empty<NpcRelationshipEpisodeChoiceDefinition>();
        }

        public string Id { get; }
        public string NpcId { get; }
        public string Title { get; }
        public string Description { get; }
        public NpcRelationshipTier RequiredTier { get; }
        public int MinimumAffinity { get; }
        public string PrerequisiteEpisodeId { get; }
        public IReadOnlyList<NpcRelationshipEpisodeChoiceDefinition> Choices { get; }

        public NpcRelationshipEpisodeChoiceDefinition FindChoice(string choiceId)
        {
            var normalized = Normalize(choiceId);
            for (var index = 0; index < Choices.Count; index += 1)
            {
                var choice = Choices[index];
                if (choice != null && string.Equals(choice.Id, normalized, StringComparison.Ordinal))
                {
                    return choice;
                }
            }

            return null;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }

    public enum NpcRelationshipEpisodeSnapshotStatus
    {
        Eligible = 0,
        MissingState = 1,
        MissingRelationshipState = 2,
        UnknownNpc = 3,
        RelationshipNotStarted = 4,
        AffinityLocked = 5,
        PrerequisiteIncomplete = 6,
        AllCompleted = 7
    }

    public readonly struct NpcRelationshipEpisodeSnapshot
    {
        public NpcRelationshipEpisodeSnapshot(
            NpcRelationshipEpisodeSnapshotStatus status,
            string npcId,
            NpcRelationshipEpisodeDefinition episode,
            int currentAffinity,
            NpcRelationshipTier currentTier)
        {
            Status = status;
            NpcId = npcId ?? string.Empty;
            Episode = episode;
            CurrentAffinity = Math.Max(0, Math.Min(99, currentAffinity));
            CurrentTier = currentTier;
        }

        public NpcRelationshipEpisodeSnapshotStatus Status { get; }
        public string NpcId { get; }
        public NpcRelationshipEpisodeDefinition Episode { get; }
        public int CurrentAffinity { get; }
        public NpcRelationshipTier CurrentTier { get; }
        public bool IsEligible => Status == NpcRelationshipEpisodeSnapshotStatus.Eligible;
        public bool HasEpisode => Episode != null;
        public int RequiredAffinity => Episode?.MinimumAffinity ?? 0;
        public NpcRelationshipTier RequiredTier => Episode?.RequiredTier ?? NpcRelationshipTier.NewFace;
    }

    public enum NpcRelationshipEpisodeChoiceStatus
    {
        Applied = 0,
        MissingState = 1,
        MissingRelationshipState = 2,
        MissingTama = 3,
        InvalidReceiptId = 4,
        InvalidCompletionTime = 5,
        DuplicateReceipt = 6,
        UnknownEpisode = 7,
        UnknownChoice = 8,
        AlreadyCompleted = 9,
        RelationshipNotStarted = 10,
        EarlierEpisodeIncomplete = 11,
        PrerequisiteIncomplete = 12,
        AffinityLocked = 13,
        StateCapacityFull = 14
    }

    public sealed class NpcRelationshipEpisodeChoiceResult
    {
        public NpcRelationshipEpisodeChoiceResult(
            NpcRelationshipEpisodeChoiceStatus status,
            NpcRelationshipEpisodeDefinition episode,
            NpcRelationshipEpisodeChoiceDefinition choice,
            string receiptId,
            int affinityBefore,
            int affinityAfter,
            DateTimeOffset completedAt)
        {
            Status = status;
            Episode = episode;
            Choice = choice;
            ReceiptId = receiptId ?? string.Empty;
            AffinityBefore = Math.Max(0, Math.Min(99, affinityBefore));
            AffinityAfter = Math.Max(0, Math.Min(99, affinityAfter));
            CompletedAt = completedAt;
        }

        public NpcRelationshipEpisodeChoiceStatus Status { get; }
        public NpcRelationshipEpisodeDefinition Episode { get; }
        public NpcRelationshipEpisodeChoiceDefinition Choice { get; }
        public string ReceiptId { get; }
        public int AffinityBefore { get; }
        public int AffinityAfter { get; }
        public DateTimeOffset CompletedAt { get; }
        public bool Applied => Status == NpcRelationshipEpisodeChoiceStatus.Applied;
        public string CompletionId => Applied ? Episode?.Id ?? string.Empty : string.Empty;
        public string NpcId => Episode?.NpcId ?? string.Empty;
        public string ChoiceId => Choice?.Id ?? string.Empty;
        public int AffinityGained => Applied ? Math.Max(0, AffinityAfter - AffinityBefore) : 0;
        public StatEffect StatEffect => Applied && Choice != null ? Choice.StatEffect : default;
        public string RewardDecorationId => Applied ? Choice?.RewardDecorationId ?? string.Empty : string.Empty;
        public string RewardKeepsakeId => Applied ? Choice?.RewardKeepsakeId ?? string.Empty : string.Empty;
        public string MemoryTitle => Applied ? Choice?.MemoryTitle ?? string.Empty : string.Empty;
        public string MemoryDetail => Applied ? Choice?.MemoryDetail ?? string.Empty : string.Empty;
        public string MemorySourceId => Applied ? Episode?.Id ?? string.Empty : string.Empty;
        public string MemoryDetailId => Applied ? Choice?.Id ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Owns deterministic relationship-promotion episode rules. UI presentation, memory-journal
    /// recording, persistence writes, and keepsake decoration rendering remain with the caller.
    /// </summary>
    public sealed class NpcRelationshipEpisodeSystem
    {
        public const int EpisodeCount = 6;
        public const int ChoicesPerEpisode = 2;

        private static readonly string[] NpcIds =
        {
            NpcVisitSystem.MilkyDoctorId,
            NpcVisitSystem.FermentationFairyId,
            NpcVisitSystem.MilkCatId
        };

        private static readonly NpcRelationshipEpisodeDefinition[] Definitions =
        {
            new NpcRelationshipEpisodeDefinition(
                NpcRelationshipEpisodeIds.DoctorFriend,
                NpcVisitSystem.MilkyDoctorId,
                "친구가 된 날의 건강 수첩",
                "밀키 박사가 함께 돌본 날들을 한 권의 수첩으로 묶어 왔어요.",
                NpcRelationshipTier.Friend,
                NpcRelationshipQuestSystem.FriendAffinityThreshold,
                string.Empty,
                new[]
                {
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "write_warm_records",
                        "따뜻했던 날을 적기",
                        "서로 기억하는 돌봄 순간이 수첩의 첫 장을 채웠어요.",
                        3,
                        new StatEffect { health = 5, mood = 2 },
                        "밀키 박사와 채운 건강 수첩",
                        "따뜻했던 돌봄의 순간을 함께 적고, 다음에도 서로의 안부를 살피기로 했다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.DoctorHealthNotebook),
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "draw_smile_chart",
                        "웃는 얼굴 표 만들기",
                        "진찰표 옆에 웃는 얼굴이 하나씩 늘어났어요.",
                        4,
                        new StatEffect { affection = 3, sleepiness = -4 },
                        "웃는 얼굴로 완성한 건강 수첩",
                        "밀키 박사와 몸뿐 아니라 마음의 상태도 기록하는 특별한 표를 만들었다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.DoctorHealthNotebook)
                }),
            new NpcRelationshipEpisodeDefinition(
                NpcRelationshipEpisodeIds.DoctorTrustedFriend,
                NpcVisitSystem.MilkyDoctorId,
                "믿는 친구의 돌봄 약속",
                "밀키 박사가 오래 간직한 작은 청진기를 맡기며 약속을 건네요.",
                NpcRelationshipTier.TrustedFriend,
                NpcRelationshipQuestSystem.TrustedFriendAffinityThreshold,
                NpcRelationshipEpisodeIds.DoctorFriend,
                new[]
                {
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "promise_daily_check",
                        "매일 안부 묻기",
                        "짧은 안부라도 꾸준히 나누자는 약속을 했어요.",
                        4,
                        new StatEffect { health = 4, affection = 4 },
                        "매일의 안부를 위한 청진기",
                        "밀키 박사의 작은 청진기를 맡아, 힘든 날에도 서로의 상태를 먼저 묻기로 약속했다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.DoctorSmallStethoscope),
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "promise_rest_signal",
                        "쉬어도 된다는 신호 정하기",
                        "말없이도 휴식이 필요하다는 걸 알아볼 신호가 생겼어요.",
                        5,
                        new StatEffect { mood = 6, sleepiness = -5 },
                        "쉬어도 된다는 약속의 청진기",
                        "서두르지 않아도 된다는 신호를 정하고, 작은 청진기를 그 약속의 표식으로 받았다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.DoctorSmallStethoscope)
                }),
            new NpcRelationshipEpisodeDefinition(
                NpcRelationshipEpisodeIds.FairyFriend,
                NpcVisitSystem.FermentationFairyId,
                "친구의 향을 담은 주머니",
                "발효요정이 함께 보낸 시간의 향을 작은 주머니에 담아 왔어요.",
                NpcRelationshipTier.Friend,
                NpcRelationshipQuestSystem.FriendAffinityThreshold,
                string.Empty,
                new[]
                {
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "choose_soft_aroma",
                        "포근한 향 고르기",
                        "천천히 익은 포근한 향이 밀크룸에 머물렀어요.",
                        3,
                        new StatEffect { maturation = 4, mood = 3 },
                        "포근한 향을 담은 우정 주머니",
                        "발효요정과 가장 편안했던 날의 향을 골라 작은 주머니에 오래 간직했다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.FairyScentSachet),
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "choose_bright_aroma",
                        "산뜻한 향 고르기",
                        "기분 좋은 향이 반짝이며 둘의 주변을 한 바퀴 돌았어요.",
                        4,
                        new StatEffect { affection = 4, cleanliness = 2 },
                        "산뜻한 향을 담은 우정 주머니",
                        "발효요정과 처음 웃었던 순간의 향을 골라, 다시 만날 때마다 꺼내 보기로 했다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.FairyScentSachet)
                }),
            new NpcRelationshipEpisodeDefinition(
                NpcRelationshipEpisodeIds.FairyTrustedFriend,
                NpcVisitSystem.FermentationFairyId,
                "오래 기다린 약속의 종",
                "발효요정이 가장 깊은 향이 완성될 때 울리는 작은 종을 건네요.",
                NpcRelationshipTier.TrustedFriend,
                NpcRelationshipQuestSystem.TrustedFriendAffinityThreshold,
                NpcRelationshipEpisodeIds.FairyFriend,
                new[]
                {
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "wait_for_deep_aroma",
                        "깊은 향을 함께 기다리기",
                        "서두르지 않는 시간이 둘만의 믿음으로 익어 갔어요.",
                        4,
                        new StatEffect { maturation = 6, affection = 3 },
                        "기다림 끝에 울린 발효 종",
                        "가장 깊은 향이 완성될 때까지 함께 기다렸고, 작은 종이 믿음의 시간을 알렸다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.FairyFermentationBell),
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "ring_for_new_aroma",
                        "새로운 향을 위해 종 울리기",
                        "맑은 종소리와 함께 다음 계절의 향을 찾아 나서기로 했어요.",
                        5,
                        new StatEffect { mood = 5, milkSatisfaction = 4 },
                        "새 계절을 부르는 발효 종",
                        "발효요정과 새 향을 만날 때마다 종을 울리며 서로를 가장 먼저 부르기로 했다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.FairyFermentationBell)
                }),
            new NpcRelationshipEpisodeDefinition(
                NpcRelationshipEpisodeIds.CatFriend,
                NpcVisitSystem.MilkCatId,
                "나란히 그린 발자국 지도",
                "밀크냥이 둘만 아는 장소를 표시할 빈 지도를 펼쳤어요.",
                NpcRelationshipTier.Friend,
                NpcRelationshipQuestSystem.FriendAffinityThreshold,
                string.Empty,
                new[]
                {
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "mark_favorite_corner",
                        "좋아하는 구석 표시하기",
                        "밀크룸의 포근한 구석이 지도에 첫 번째 별로 남았어요.",
                        3,
                        new StatEffect { mood = 5, affection = 3 },
                        "밀크냥과 그린 발자국 지도",
                        "함께 쉬기 좋은 구석을 지도에 표시하고, 언제든 그곳에서 만나기로 했다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.CatPawMap),
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "mark_hidden_path",
                        "비밀 지름길 표시하기",
                        "남들에게는 보이지 않는 작은 지름길이 지도에 이어졌어요.",
                        4,
                        new StatEffect { cleanliness = 3, mood = 4 },
                        "비밀 지름길이 담긴 발자국 지도",
                        "밀크냥과 둘만 아는 길을 지도에 그리고, 길 끝에 작은 발자국을 나란히 남겼다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.CatPawMap)
                }),
            new NpcRelationshipEpisodeDefinition(
                NpcRelationshipEpisodeIds.CatTrustedFriend,
                NpcVisitSystem.MilkCatId,
                "비밀 친구의 별 나침반",
                "밀크냥이 가장 믿는 친구에게만 보여 주는 별 나침반을 꺼냈어요.",
                NpcRelationshipTier.TrustedFriend,
                NpcRelationshipQuestSystem.TrustedFriendAffinityThreshold,
                NpcRelationshipEpisodeIds.CatFriend,
                new[]
                {
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "follow_home_star",
                        "집으로 가는 별 고르기",
                        "멀리 가도 다시 만날 수 있는 별 하나를 함께 골랐어요.",
                        4,
                        new StatEffect { mood = 6, affection = 4 },
                        "다시 만나는 길의 별 나침반",
                        "밀크냥과 집으로 돌아오는 별을 정하고, 어느 길에서도 다시 만나기로 약속했다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.CatStarCompass),
                    new NpcRelationshipEpisodeChoiceDefinition(
                        "follow_adventure_star",
                        "새 모험의 별 고르기",
                        "나침반이 아직 가 보지 않은 작은 모험을 가리켰어요.",
                        5,
                        new StatEffect { sleepiness = -5, affection = 5 },
                        "새 모험을 가리키는 별 나침반",
                        "밀크냥과 다음 모험의 별을 골랐고, 가장 먼저 서로를 길동무로 부르기로 했다.",
                        rewardKeepsakeId: NpcRelationshipKeepsakeIds.CatStarCompass)
                })
        };

        public IReadOnlyList<NpcRelationshipEpisodeDefinition> All => Definitions;

        public NpcRelationshipEpisodeDefinition Find(string episodeId)
        {
            var normalized = Normalize(episodeId);
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                if (string.Equals(Definitions[index].Id, normalized, StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }

        public NpcRelationshipEpisodeSnapshot BuildNextEpisodeSnapshot(
            NpcRelationshipEpisodeSaveData state,
            NpcVisitSaveData relationships,
            string npcId)
        {
            var normalizedNpc = Normalize(npcId);
            if (state == null)
            {
                return SnapshotFailure(
                    NpcRelationshipEpisodeSnapshotStatus.MissingState,
                    normalizedNpc);
            }

            state.EnsureRuntimeDefaults();
            if (relationships == null)
            {
                return SnapshotFailure(
                    NpcRelationshipEpisodeSnapshotStatus.MissingRelationshipState,
                    normalizedNpc);
            }

            relationships.EnsureRuntimeDefaults();
            if (!IsKnownNpc(normalizedNpc))
            {
                return SnapshotFailure(
                    NpcRelationshipEpisodeSnapshotStatus.UnknownNpc,
                    normalizedNpc);
            }

            var relationship = FindRelationship(relationships, normalizedNpc);
            var affinity = relationship?.affinity ?? 0;
            var tier = NpcRelationshipQuestSystem.ResolveTier(affinity);
            var next = FindNextIncomplete(state, normalizedNpc);
            if (next == null)
            {
                return new NpcRelationshipEpisodeSnapshot(
                    NpcRelationshipEpisodeSnapshotStatus.AllCompleted,
                    normalizedNpc,
                    null,
                    affinity,
                    tier);
            }

            if (relationship == null)
            {
                return new NpcRelationshipEpisodeSnapshot(
                    NpcRelationshipEpisodeSnapshotStatus.RelationshipNotStarted,
                    normalizedNpc,
                    next,
                    0,
                    NpcRelationshipTier.NewFace);
            }

            if (!string.IsNullOrEmpty(next.PrerequisiteEpisodeId)
                && !state.HasCompletedEpisode(next.PrerequisiteEpisodeId))
            {
                return new NpcRelationshipEpisodeSnapshot(
                    NpcRelationshipEpisodeSnapshotStatus.PrerequisiteIncomplete,
                    normalizedNpc,
                    next,
                    affinity,
                    tier);
            }

            var status = affinity < next.MinimumAffinity || (int)tier < (int)next.RequiredTier
                ? NpcRelationshipEpisodeSnapshotStatus.AffinityLocked
                : NpcRelationshipEpisodeSnapshotStatus.Eligible;
            return new NpcRelationshipEpisodeSnapshot(
                status,
                normalizedNpc,
                next,
                affinity,
                tier);
        }

        public IReadOnlyList<NpcRelationshipEpisodeSnapshot> BuildNextEpisodeSnapshots(
            NpcRelationshipEpisodeSaveData state,
            NpcVisitSaveData relationships)
        {
            var result = new NpcRelationshipEpisodeSnapshot[NpcIds.Length];
            for (var index = 0; index < NpcIds.Length; index += 1)
            {
                result[index] = BuildNextEpisodeSnapshot(state, relationships, NpcIds[index]);
            }

            return result;
        }

        public bool TryGetNextEligibleEpisode(
            NpcRelationshipEpisodeSaveData state,
            NpcVisitSaveData relationships,
            string npcId,
            out NpcRelationshipEpisodeSnapshot snapshot)
        {
            snapshot = BuildNextEpisodeSnapshot(state, relationships, npcId);
            return snapshot.IsEligible;
        }

        public NpcRelationshipEpisodeChoiceResult TryApplyChoice(
            NpcRelationshipEpisodeSaveData state,
            NpcVisitSaveData relationships,
            CheeseTamaModel tama,
            string episodeId,
            string choiceId,
            string receiptId,
            DateTimeOffset now)
        {
            var normalizedReceipt = Normalize(receiptId);
            if (state == null)
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.MissingState,
                    normalizedReceipt);
            }

            state.EnsureRuntimeDefaults();
            if (relationships == null)
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.MissingRelationshipState,
                    normalizedReceipt);
            }

            if (tama == null)
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.MissingTama,
                    normalizedReceipt);
            }

            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.InvalidReceiptId,
                    normalizedReceipt);
            }

            if (now == default)
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.InvalidCompletionTime,
                    normalizedReceipt);
            }

            if (state.HasReceipt(normalizedReceipt))
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.DuplicateReceipt,
                    normalizedReceipt);
            }

            var episode = Find(episodeId);
            if (episode == null)
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.UnknownEpisode,
                    normalizedReceipt);
            }

            var choice = episode.FindChoice(choiceId);
            if (choice == null)
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.UnknownChoice,
                    normalizedReceipt,
                    episode);
            }

            if (state.HasCompletedEpisode(episode.Id))
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.AlreadyCompleted,
                    normalizedReceipt,
                    episode,
                    choice);
            }

            relationships.EnsureRuntimeDefaults();
            tama.EnsureRuntimeDefaults();
            var relationship = FindRelationship(relationships, episode.NpcId);
            if (relationship == null)
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.RelationshipNotStarted,
                    normalizedReceipt,
                    episode,
                    choice);
            }

            var expectedEpisode = FindNextIncomplete(state, episode.NpcId);
            if (expectedEpisode == null
                || !string.Equals(expectedEpisode.Id, episode.Id, StringComparison.Ordinal))
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.EarlierEpisodeIncomplete,
                    normalizedReceipt,
                    episode,
                    choice);
            }

            if (!string.IsNullOrEmpty(episode.PrerequisiteEpisodeId)
                && !state.HasCompletedEpisode(episode.PrerequisiteEpisodeId))
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.PrerequisiteIncomplete,
                    normalizedReceipt,
                    episode,
                    choice);
            }

            var affinityBefore = Math.Max(0, Math.Min(99, relationship.affinity));
            var tierBefore = NpcRelationshipQuestSystem.ResolveTier(affinityBefore);
            if (affinityBefore < episode.MinimumAffinity
                || (int)tierBefore < (int)episode.RequiredTier)
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.AffinityLocked,
                    normalizedReceipt,
                    episode,
                    choice,
                    affinityBefore);
            }

            if (!state.CanRecordCompletion(episode.Id)
                || !state.CanAddKeepsake(choice.RewardKeepsakeId))
            {
                return ChoiceFailure(
                    NpcRelationshipEpisodeChoiceStatus.StateCapacityFull,
                    normalizedReceipt,
                    episode,
                    choice,
                    affinityBefore);
            }

            var affinityAfter = Math.Min(99, SaturatingAdd(affinityBefore, choice.Affinity));
            var tierAfter = NpcRelationshipQuestSystem.ResolveTier(affinityAfter);

            // Validation is complete. Apply the relationship, stat, and durable completion as one block.
            relationship.affinity = affinityAfter;
            relationship.storyStep = Math.Max(
                relationship.storyStep,
                tierAfter == NpcRelationshipTier.TrustedFriend
                    ? 2
                    : tierAfter == NpcRelationshipTier.Friend ? 1 : 0);
            tama.stats.Apply(choice.StatEffect);
            state.RecordCompletion(
                episode.Id,
                choice.RewardKeepsakeId,
                new NpcRelationshipEpisodeReceiptSaveData
                {
                    receiptId = normalizedReceipt,
                    episodeId = episode.Id,
                    npcId = episode.NpcId,
                    choiceId = choice.Id,
                    completedAtIso = now.ToString("O", CultureInfo.InvariantCulture)
                });

            return new NpcRelationshipEpisodeChoiceResult(
                NpcRelationshipEpisodeChoiceStatus.Applied,
                episode,
                choice,
                normalizedReceipt,
                affinityBefore,
                affinityAfter,
                now);
        }

        private static NpcRelationshipEpisodeDefinition FindNextIncomplete(
            NpcRelationshipEpisodeSaveData state,
            string npcId)
        {
            for (var index = 0; index < Definitions.Length; index += 1)
            {
                var episode = Definitions[index];
                if (string.Equals(episode.NpcId, npcId, StringComparison.Ordinal)
                    && !state.HasCompletedEpisode(episode.Id))
                {
                    return episode;
                }
            }

            return null;
        }

        private static NpcRelationshipSaveEntry FindRelationship(
            NpcVisitSaveData relationships,
            string npcId)
        {
            if (relationships?.relationships == null)
            {
                return null;
            }

            for (var index = 0; index < relationships.relationships.Count; index += 1)
            {
                var relationship = relationships.relationships[index];
                if (relationship != null
                    && string.Equals(relationship.npcId, npcId, StringComparison.Ordinal))
                {
                    return relationship;
                }
            }

            return null;
        }

        private static bool IsKnownNpc(string npcId)
        {
            for (var index = 0; index < NpcIds.Length; index += 1)
            {
                if (string.Equals(NpcIds[index], npcId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static NpcRelationshipEpisodeSnapshot SnapshotFailure(
            NpcRelationshipEpisodeSnapshotStatus status,
            string npcId)
        {
            return new NpcRelationshipEpisodeSnapshot(
                status,
                npcId,
                null,
                0,
                NpcRelationshipTier.NewFace);
        }

        private static NpcRelationshipEpisodeChoiceResult ChoiceFailure(
            NpcRelationshipEpisodeChoiceStatus status,
            string receiptId,
            NpcRelationshipEpisodeDefinition episode = null,
            NpcRelationshipEpisodeChoiceDefinition choice = null,
            int affinity = 0)
        {
            return new NpcRelationshipEpisodeChoiceResult(
                status,
                episode,
                choice,
                receiptId,
                affinity,
                affinity,
                default);
        }

        private static int SaturatingAdd(int current, int amount)
        {
            var result = (long)Math.Max(0, current) + Math.Max(0, amount);
            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }
}
