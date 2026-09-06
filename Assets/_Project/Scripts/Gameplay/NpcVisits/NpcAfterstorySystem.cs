using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.NpcVisits
{
    public static class NpcAfterstoryIds
    {
        public const string Doctor = "doctor_afterstory_night_clinic";
        public const string Fairy = "fairy_afterstory_seasoned_scent";
        public const string Cat = "cat_afterstory_window_promise";
    }

    public static class NpcAfterstoryKeepsakeIds
    {
        public const string DoctorMoonlightChart = "afterstory_keepsake_doctor_moonlight_chart";
        public const string FairyAgingBottle = "afterstory_keepsake_fairy_aging_bottle";
        public const string CatWindowBell = "afterstory_keepsake_cat_window_bell";
    }

    public static class NpcWeeklyQuestIds
    {
        public const string Doctor = "doctor_weekly_wellbeing_check";
        public const string Fairy = "fairy_weekly_blending_notes";
        public const string Cat = "cat_weekly_room_patrol";
    }

    public static class NpcWeeklyObjectiveIds
    {
        public const string CareAction = "care_action";
        public const string MilkBlend = "milk_blend";
        public const string PlayOrClean = "play_or_clean";
    }

    public sealed class NpcAfterstoryChoiceDefinition
    {
        public NpcAfterstoryChoiceDefinition(
            string id,
            string label,
            string resultMessage,
            string memoryTitle,
            string memoryDetail)
        {
            Id = Normalize(id);
            Label = label ?? string.Empty;
            ResultMessage = resultMessage ?? string.Empty;
            MemoryTitle = memoryTitle ?? string.Empty;
            MemoryDetail = memoryDetail ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
        public string ResultMessage { get; }
        public string MemoryTitle { get; }
        public string MemoryDetail { get; }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }

    public sealed class NpcAfterstoryDefinition
    {
        public NpcAfterstoryDefinition(
            string id,
            string npcId,
            string npcDisplayName,
            string title,
            string description,
            string firstRequiredEpisodeId,
            string secondRequiredEpisodeId,
            string keepsakeId,
            NpcAfterstoryChoiceDefinition[] choices)
        {
            Id = Normalize(id);
            NpcId = Normalize(npcId);
            NpcDisplayName = npcDisplayName ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            FirstRequiredEpisodeId = Normalize(firstRequiredEpisodeId);
            SecondRequiredEpisodeId = Normalize(secondRequiredEpisodeId);
            KeepsakeId = Normalize(keepsakeId);
            Choices = choices ?? Array.Empty<NpcAfterstoryChoiceDefinition>();
        }

        public string Id { get; }
        public string NpcId { get; }
        public string NpcDisplayName { get; }
        public string Title { get; }
        public string Description { get; }
        public string FirstRequiredEpisodeId { get; }
        public string SecondRequiredEpisodeId { get; }
        public string KeepsakeId { get; }
        public IReadOnlyList<NpcAfterstoryChoiceDefinition> Choices { get; }

        public NpcAfterstoryChoiceDefinition FindChoice(string choiceId)
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

    public sealed class NpcWeeklyQuestReward
    {
        public NpcWeeklyQuestReward(int coins, int milkDrops)
        {
            Coins = Math.Max(0, coins);
            MilkDrops = Math.Max(0, milkDrops);
        }

        public int Coins { get; }
        public int MilkDrops { get; }
        public bool IsEmpty => Coins == 0 && MilkDrops == 0;
    }

    public sealed class NpcWeeklyQuestDefinition
    {
        public NpcWeeklyQuestDefinition(
            string id,
            string npcId,
            string title,
            string description,
            string objectiveId,
            string objectiveLabel,
            int requiredProgress,
            NpcWeeklyQuestReward reward)
        {
            Id = Normalize(id);
            NpcId = Normalize(npcId);
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            ObjectiveId = Normalize(objectiveId);
            ObjectiveLabel = objectiveLabel ?? string.Empty;
            RequiredProgress = Math.Max(1, requiredProgress);
            Reward = reward ?? new NpcWeeklyQuestReward(0, 0);
        }

        public string Id { get; }
        public string NpcId { get; }
        public string Title { get; }
        public string Description { get; }
        public string ObjectiveId { get; }
        public string ObjectiveLabel { get; }
        public int RequiredProgress { get; }
        public NpcWeeklyQuestReward Reward { get; }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }

    public sealed class NpcKeepsakeDisplayProfile
    {
        public NpcKeepsakeDisplayProfile(
            string keepsakeId,
            string npcId,
            string title,
            string ambientDialogue,
            string accentHex,
            float displayScale)
        {
            KeepsakeId = (keepsakeId ?? string.Empty).Trim();
            NpcId = (npcId ?? string.Empty).Trim();
            Title = title ?? string.Empty;
            AmbientDialogue = ambientDialogue ?? string.Empty;
            AccentHex = accentHex ?? string.Empty;
            DisplayScale = Math.Max(0.5f, Math.Min(1.5f, displayScale));
        }

        public string KeepsakeId { get; }
        public string NpcId { get; }
        public string Title { get; }
        public string AmbientDialogue { get; }
        public string AccentHex { get; }
        public float DisplayScale { get; }
    }

    public enum NpcAfterstorySnapshotStatus
    {
        Ready = 0,
        Completed = 1,
        MissingState = 2,
        MissingEpisodeState = 3,
        UnknownNpc = 4,
        Locked = 5
    }

    public readonly struct NpcAfterstorySnapshot
    {
        public NpcAfterstorySnapshot(
            NpcAfterstorySnapshotStatus status,
            string npcId,
            NpcAfterstoryDefinition definition,
            NpcAfterstoryChoiceDefinition selectedChoice,
            string completedAtIso)
        {
            Status = status;
            NpcId = npcId ?? string.Empty;
            Definition = definition;
            SelectedChoice = selectedChoice;
            CompletedAtIso = completedAtIso ?? string.Empty;
        }

        public NpcAfterstorySnapshotStatus Status { get; }
        public string NpcId { get; }
        public NpcAfterstoryDefinition Definition { get; }
        public NpcAfterstoryChoiceDefinition SelectedChoice { get; }
        public string CompletedAtIso { get; }
        public bool IsReady => Status == NpcAfterstorySnapshotStatus.Ready;
        public bool IsCompleted => Status == NpcAfterstorySnapshotStatus.Completed;
        public bool HasVisibleContent => IsReady || IsCompleted;
    }

    public enum NpcAfterstoryChoiceStatus
    {
        Applied = 0,
        MissingState = 1,
        MissingEpisodeState = 2,
        InvalidReceipt = 3,
        InvalidCompletionTime = 4,
        DuplicateReceipt = 5,
        UnknownAfterstory = 6,
        UnknownChoice = 7,
        AlreadyCompleted = 8,
        PrerequisiteIncomplete = 9,
        StateCapacityFull = 10
    }

    public sealed class NpcAfterstoryChoiceResult
    {
        public NpcAfterstoryChoiceResult(
            NpcAfterstoryChoiceStatus status,
            NpcAfterstoryDefinition definition,
            NpcAfterstoryChoiceDefinition choice,
            string receiptId,
            DateTimeOffset completedAt)
        {
            Status = status;
            Definition = definition;
            Choice = choice;
            ReceiptId = receiptId ?? string.Empty;
            CompletedAt = completedAt;
        }

        public NpcAfterstoryChoiceStatus Status { get; }
        public NpcAfterstoryDefinition Definition { get; }
        public NpcAfterstoryChoiceDefinition Choice { get; }
        public string ReceiptId { get; }
        public DateTimeOffset CompletedAt { get; }
        public bool Applied => Status == NpcAfterstoryChoiceStatus.Applied;
        public string AfterstoryId => Applied ? Definition?.Id ?? string.Empty : string.Empty;
        public string NpcId => Applied ? Definition?.NpcId ?? string.Empty : string.Empty;
        public string ChoiceId => Applied ? Choice?.Id ?? string.Empty : string.Empty;
        public string KeepsakeId => Applied ? Definition?.KeepsakeId ?? string.Empty : string.Empty;
        public string ResultMessage => Applied ? Choice?.ResultMessage ?? string.Empty : string.Empty;
        public string MemoryTitle => Applied ? Choice?.MemoryTitle ?? string.Empty : string.Empty;
        public string MemoryDetail => Applied ? Choice?.MemoryDetail ?? string.Empty : string.Empty;
    }

    public enum NpcWeeklyQuestSnapshotStatus
    {
        InProgress = 0,
        ReadyToClaim = 1,
        Claimed = 2,
        Hidden = 3,
        MissingState = 4,
        UnknownNpc = 5,
        InvalidTime = 6,
        ClockRollback = 7
    }

    public readonly struct NpcWeeklyQuestSnapshot
    {
        public NpcWeeklyQuestSnapshot(
            NpcWeeklyQuestSnapshotStatus status,
            NpcWeeklyQuestDefinition definition,
            string weekKey,
            int progress)
        {
            Status = status;
            Definition = definition;
            WeekKey = weekKey ?? string.Empty;
            Progress = Math.Max(0, progress);
        }

        public NpcWeeklyQuestSnapshotStatus Status { get; }
        public NpcWeeklyQuestDefinition Definition { get; }
        public string WeekKey { get; }
        public int Progress { get; }
        public int RequiredProgress => Definition?.RequiredProgress ?? 0;
        public bool IsVisible => Definition != null;
        public bool CanClaim => Status == NpcWeeklyQuestSnapshotStatus.ReadyToClaim;
    }

    public enum NpcWeeklyProgressStatus
    {
        Applied = 0,
        MissingState = 1,
        MissingEpisodeState = 2,
        UnknownNpc = 3,
        AfterstoryIncomplete = 4,
        UnknownObjective = 5,
        InvalidAmount = 6,
        InvalidTime = 7,
        ClockRollback = 8,
        AlreadyClaimed = 9
    }

    public readonly struct NpcWeeklyProgressResult
    {
        public NpcWeeklyProgressResult(
            NpcWeeklyProgressStatus status,
            NpcWeeklyQuestDefinition definition,
            string weekKey,
            int progressBefore,
            int progressAfter)
        {
            Status = status;
            Definition = definition;
            WeekKey = weekKey ?? string.Empty;
            ProgressBefore = Math.Max(0, progressBefore);
            ProgressAfter = Math.Max(0, progressAfter);
        }

        public NpcWeeklyProgressStatus Status { get; }
        public NpcWeeklyQuestDefinition Definition { get; }
        public string WeekKey { get; }
        public int ProgressBefore { get; }
        public int ProgressAfter { get; }
        public bool Applied => Status == NpcWeeklyProgressStatus.Applied;
        public bool Completed => Definition != null && ProgressAfter >= Definition.RequiredProgress;
    }

    public enum NpcWeeklyClaimStatus
    {
        Applied = 0,
        MissingState = 1,
        MissingEpisodeState = 2,
        MissingEconomy = 3,
        UnknownNpc = 4,
        AfterstoryIncomplete = 5,
        InvalidReceipt = 6,
        InvalidTime = 7,
        ClockRollback = 8,
        DuplicateReceipt = 9,
        AlreadyClaimedThisWeek = 10,
        ObjectiveIncomplete = 11,
        RewardOverflow = 12
    }

    public sealed class NpcWeeklyClaimResult
    {
        public NpcWeeklyClaimResult(
            NpcWeeklyClaimStatus status,
            NpcWeeklyQuestDefinition definition,
            string weekKey,
            string receiptId,
            DateTimeOffset claimedAt)
        {
            Status = status;
            Definition = definition;
            WeekKey = weekKey ?? string.Empty;
            ReceiptId = receiptId ?? string.Empty;
            ClaimedAt = claimedAt;
        }

        public NpcWeeklyClaimStatus Status { get; }
        public NpcWeeklyQuestDefinition Definition { get; }
        public string WeekKey { get; }
        public string ReceiptId { get; }
        public DateTimeOffset ClaimedAt { get; }
        public bool Applied => Status == NpcWeeklyClaimStatus.Applied;
        public NpcWeeklyQuestReward Reward => Applied
            ? Definition?.Reward ?? EmptyReward
            : EmptyReward;

        private static readonly NpcWeeklyQuestReward EmptyReward =
            new NpcWeeklyQuestReward(0, 0);
    }

    public enum NpcKeepsakeSelectionStatus
    {
        Applied = 0,
        Cleared = 1,
        MissingState = 2,
        UnknownKeepsake = 3,
        NotOwned = 4
    }

    public readonly struct NpcKeepsakeSelectionResult
    {
        public NpcKeepsakeSelectionResult(
            NpcKeepsakeSelectionStatus status,
            NpcKeepsakeDisplayProfile profile)
        {
            Status = status;
            Profile = profile;
        }

        public NpcKeepsakeSelectionStatus Status { get; }
        public NpcKeepsakeDisplayProfile Profile { get; }
        public bool Changed => Status == NpcKeepsakeSelectionStatus.Applied
            || Status == NpcKeepsakeSelectionStatus.Cleared;
    }

    public readonly struct NpcKeepsakeDisplaySnapshot
    {
        public NpcKeepsakeDisplaySnapshot(NpcKeepsakeDisplayProfile profile)
        {
            Profile = profile;
        }

        public NpcKeepsakeDisplayProfile Profile { get; }
        public bool HasDisplay => Profile != null;
        public string AmbientDialogue => Profile?.AmbientDialogue ?? string.Empty;
    }

    public sealed class NpcAfterstoryNpcPanelSnapshot
    {
        public NpcAfterstoryNpcPanelSnapshot(
            NpcAfterstorySnapshot afterstory,
            NpcWeeklyQuestSnapshot weeklyQuest)
        {
            Afterstory = afterstory;
            WeeklyQuest = weeklyQuest;
        }

        public NpcAfterstorySnapshot Afterstory { get; }
        public NpcWeeklyQuestSnapshot WeeklyQuest { get; }
        public string NpcId => Afterstory.NpcId;
    }

    public sealed class NpcAfterstoryPanelSnapshot
    {
        public static readonly NpcAfterstoryPanelSnapshot Empty =
            new NpcAfterstoryPanelSnapshot(
                Array.Empty<NpcAfterstoryNpcPanelSnapshot>(),
                Array.Empty<NpcKeepsakeDisplayProfile>(),
                new NpcKeepsakeDisplaySnapshot(null));

        public NpcAfterstoryPanelSnapshot(
            NpcAfterstoryNpcPanelSnapshot[] npcStories,
            NpcKeepsakeDisplayProfile[] ownedKeepsakes,
            NpcKeepsakeDisplaySnapshot displayedKeepsake)
        {
            NpcStories = npcStories ?? Array.Empty<NpcAfterstoryNpcPanelSnapshot>();
            OwnedKeepsakes = ownedKeepsakes ?? Array.Empty<NpcKeepsakeDisplayProfile>();
            DisplayedKeepsake = displayedKeepsake;
        }

        public IReadOnlyList<NpcAfterstoryNpcPanelSnapshot> NpcStories { get; }
        public IReadOnlyList<NpcKeepsakeDisplayProfile> OwnedKeepsakes { get; }
        public NpcKeepsakeDisplaySnapshot DisplayedKeepsake { get; }
        public bool HasVisibleContent => NpcStories.Count > 0 || OwnedKeepsakes.Count > 0;
    }

    /// <summary>
    /// Owns post-relationship story, weekly quest, and keepsake-display state. Existing NPC visit
    /// and relationship episode data is consumed only as an unlock prerequisite.
    /// </summary>
    public sealed class NpcAfterstorySystem
    {
        public const int NpcCount = 3;
        public const int ChoicesPerAfterstory = 2;

        private static readonly NpcAfterstoryDefinition[] Afterstories =
        {
            new NpcAfterstoryDefinition(
                NpcAfterstoryIds.Doctor,
                NpcVisitSystem.MilkyDoctorId,
                "밀키 박사",
                "밤에도 켜진 진료등",
                "모든 약속을 지킨 뒤, 밀키 박사가 마지막 환자 기록을 함께 정리하자고 찾아왔어요.",
                NpcRelationshipEpisodeIds.DoctorFriend,
                NpcRelationshipEpisodeIds.DoctorTrustedFriend,
                NpcAfterstoryKeepsakeIds.DoctorMoonlightChart,
                new[]
                {
                    new NpcAfterstoryChoiceDefinition(
                        "record_together",
                        "곁에서 기록을 정리하기",
                        "한 줄씩 나눈 기록이 둘만의 달빛 진료표가 되었어요.",
                        "밀키 박사와 완성한 달빛 진료표",
                        "늦은 밤까지 서로의 안부를 적고, 무리하지 않는 돌봄을 이어 가기로 했다."),
                    new NpcAfterstoryChoiceDefinition(
                        "suggest_rest_first",
                        "먼저 쉬어도 된다고 말하기",
                        "밀키 박사가 웃으며 진료등을 끄고 나란히 쉬었어요.",
                        "함께 쉰 밤의 달빛 진료표",
                        "기록보다 휴식을 먼저 골랐고, 잘 쉬는 것도 서로를 돌보는 일임을 기억했다.")
                }),
            new NpcAfterstoryDefinition(
                NpcAfterstoryIds.Fairy,
                NpcVisitSystem.FermentationFairyId,
                "발효요정",
                "계절을 건넌 향",
                "두 번의 약속을 지킨 발효요정이 한 계절을 기다린 향을 열어 보여 줬어요.",
                NpcRelationshipEpisodeIds.FairyFriend,
                NpcRelationshipEpisodeIds.FairyTrustedFriend,
                NpcAfterstoryKeepsakeIds.FairyAgingBottle,
                new[]
                {
                    new NpcAfterstoryChoiceDefinition(
                        "share_first_scent",
                        "첫 향을 함께 나누기",
                        "병을 연 순간 포근한 향이 밀크룸을 천천히 감쌌어요.",
                        "계절을 건넌 첫 향",
                        "오래 기다린 향의 첫 순간을 발효요정과 나누고 작은 숙성병을 건네받았다."),
                    new NpcAfterstoryChoiceDefinition(
                        "save_next_season",
                        "다음 계절을 위해 남겨 두기",
                        "한 방울을 남긴 병 안에서 다음 계절의 향이 반짝였어요.",
                        "다음 계절을 품은 숙성병",
                        "지금의 향을 모두 쓰지 않고 다음 만남을 위해 한 방울 남겨 두었다.")
                }),
            new NpcAfterstoryDefinition(
                NpcAfterstoryIds.Cat,
                NpcVisitSystem.MilkCatId,
                "밀크냥",
                "창가에 남은 약속",
                "비밀 지도를 완성한 밀크냥이 돌아오는 길을 알리는 작은 방울을 가져왔어요.",
                NpcRelationshipEpisodeIds.CatFriend,
                NpcRelationshipEpisodeIds.CatTrustedFriend,
                NpcAfterstoryKeepsakeIds.CatWindowBell,
                new[]
                {
                    new NpcAfterstoryChoiceDefinition(
                        "hang_bell_together",
                        "창가에 함께 방울 달기",
                        "둘이 고른 자리에 작은 별빛 소리가 머물렀어요.",
                        "밀크냥과 단 창가 방울",
                        "돌아오는 길을 잊지 않도록 창가에 방울을 달고 언제든 다시 만나기로 했다."),
                    new NpcAfterstoryChoiceDefinition(
                        "ring_secret_signal",
                        "둘만의 신호로 울리기",
                        "짧게 두 번 울리는 소리가 둘만의 인사가 되었어요.",
                        "둘만의 신호가 된 창가 방울",
                        "작은 방울에 둘만 아는 인사를 정하고 멀리서도 서로를 알아보기로 했다.")
                })
        };

        private static readonly NpcWeeklyQuestDefinition[] WeeklyQuests =
        {
            new NpcWeeklyQuestDefinition(
                NpcWeeklyQuestIds.Doctor,
                NpcVisitSystem.MilkyDoctorId,
                "이번 주 안부 기록",
                "밀키 박사와 약속한 대로 일주일 동안 몸과 마음을 돌봐 주세요.",
                NpcWeeklyObjectiveIds.CareAction,
                "돌봄 행동 3회",
                3,
                new NpcWeeklyQuestReward(35, 2)),
            new NpcWeeklyQuestDefinition(
                NpcWeeklyQuestIds.Fairy,
                NpcVisitSystem.FermentationFairyId,
                "이번 주 향의 기록",
                "발효요정에게 들려줄 새로운 향을 찾도록 우유를 섞어 주세요.",
                NpcWeeklyObjectiveIds.MilkBlend,
                "우유 2번 섞기",
                2,
                new NpcWeeklyQuestReward(30, 3)),
            new NpcWeeklyQuestDefinition(
                NpcWeeklyQuestIds.Cat,
                NpcVisitSystem.MilkCatId,
                "이번 주 밀크룸 순찰",
                "밀크냥과 함께 놀거나 밀크룸을 깨끗하게 돌봐 주세요.",
                NpcWeeklyObjectiveIds.PlayOrClean,
                "놀이 또는 청소 3회",
                3,
                new NpcWeeklyQuestReward(40, 2))
        };

        private static readonly NpcKeepsakeDisplayProfile[] DisplayProfiles =
        {
            new NpcKeepsakeDisplayProfile(
                NpcAfterstoryKeepsakeIds.DoctorMoonlightChart,
                NpcVisitSystem.MilkyDoctorId,
                "달빛 진료표",
                "무리하지 않아도 괜찮아. 오늘의 안부부터 천천히 적어 보자.",
                "#8EC5FF",
                0.94f),
            new NpcKeepsakeDisplayProfile(
                NpcAfterstoryKeepsakeIds.FairyAgingBottle,
                NpcVisitSystem.FermentationFairyId,
                "계절 숙성병",
                "좋은 향은 기다린 시간까지 함께 기억한대.",
                "#C6A7FF",
                0.88f),
            new NpcKeepsakeDisplayProfile(
                NpcAfterstoryKeepsakeIds.CatWindowBell,
                NpcVisitSystem.MilkCatId,
                "별빛 창가 방울",
                "딸랑, 딸랑. 밀크냥이 돌아오는 길을 찾았나 봐.",
                "#FFD47A",
                1.04f)
        };

        public IReadOnlyList<NpcAfterstoryDefinition> AllAfterstories => Afterstories;
        public IReadOnlyList<NpcWeeklyQuestDefinition> AllWeeklyQuests => WeeklyQuests;
        public IReadOnlyList<NpcKeepsakeDisplayProfile> AllDisplayProfiles => DisplayProfiles;

        public NpcAfterstoryDefinition FindAfterstory(string afterstoryId)
        {
            var normalized = Normalize(afterstoryId);
            for (var index = 0; index < Afterstories.Length; index += 1)
            {
                if (string.Equals(Afterstories[index].Id, normalized, StringComparison.Ordinal))
                {
                    return Afterstories[index];
                }
            }

            return null;
        }

        public NpcAfterstoryDefinition FindAfterstoryByNpc(string npcId)
        {
            var normalized = Normalize(npcId);
            for (var index = 0; index < Afterstories.Length; index += 1)
            {
                if (string.Equals(Afterstories[index].NpcId, normalized, StringComparison.Ordinal))
                {
                    return Afterstories[index];
                }
            }

            return null;
        }

        public NpcWeeklyQuestDefinition FindWeeklyQuestByNpc(string npcId)
        {
            var normalized = Normalize(npcId);
            for (var index = 0; index < WeeklyQuests.Length; index += 1)
            {
                if (string.Equals(WeeklyQuests[index].NpcId, normalized, StringComparison.Ordinal))
                {
                    return WeeklyQuests[index];
                }
            }

            return null;
        }

        public NpcKeepsakeDisplayProfile FindDisplayProfile(string keepsakeId)
        {
            var normalized = Normalize(keepsakeId);
            for (var index = 0; index < DisplayProfiles.Length; index += 1)
            {
                if (string.Equals(DisplayProfiles[index].KeepsakeId, normalized, StringComparison.Ordinal))
                {
                    return DisplayProfiles[index];
                }
            }

            return null;
        }

        public bool NormalizeState(NpcAfterstorySaveData state)
        {
            if (state == null)
            {
                return false;
            }

            var changed = state.EnsureRuntimeDefaults();
            if (!string.IsNullOrEmpty(state.displayedKeepsakeId)
                && (FindDisplayProfile(state.displayedKeepsakeId) == null
                    || !state.HasKeepsake(state.displayedKeepsakeId)))
            {
                state.displayedKeepsakeId = string.Empty;
                changed = true;
            }

            for (var index = 0; index < state.weeklyProgress.Count; index += 1)
            {
                var progress = state.weeklyProgress[index];
                var definition = FindWeeklyQuestByNpc(progress.npcId);
                if (definition != null
                    && !string.Equals(progress.questId, definition.Id, StringComparison.Ordinal))
                {
                    progress.questId = definition.Id;
                    changed = true;
                }
            }

            return changed;
        }

        public NpcAfterstorySnapshot BuildAfterstorySnapshot(
            NpcAfterstorySaveData state,
            NpcRelationshipEpisodeSaveData episodeState,
            string npcId)
        {
            var normalizedNpc = Normalize(npcId);
            var definition = FindAfterstoryByNpc(normalizedNpc);
            if (definition == null)
            {
                return new NpcAfterstorySnapshot(
                    NpcAfterstorySnapshotStatus.UnknownNpc,
                    normalizedNpc,
                    null,
                    null,
                    string.Empty);
            }

            if (state == null)
            {
                return new NpcAfterstorySnapshot(
                    NpcAfterstorySnapshotStatus.MissingState,
                    normalizedNpc,
                    null,
                    null,
                    string.Empty);
            }

            NormalizeState(state);
            var completion = state.FindCompletion(definition.Id);
            if (completion != null)
            {
                return new NpcAfterstorySnapshot(
                    NpcAfterstorySnapshotStatus.Completed,
                    normalizedNpc,
                    definition,
                    definition.FindChoice(completion.choiceId),
                    completion.completedAtIso);
            }

            if (episodeState == null)
            {
                return new NpcAfterstorySnapshot(
                    NpcAfterstorySnapshotStatus.MissingEpisodeState,
                    normalizedNpc,
                    null,
                    null,
                    string.Empty);
            }

            episodeState.EnsureRuntimeDefaults();
            if (!HasRequiredEpisodes(episodeState, definition))
            {
                // Locked content intentionally carries no title, choice, keepsake, or condition data.
                return new NpcAfterstorySnapshot(
                    NpcAfterstorySnapshotStatus.Locked,
                    normalizedNpc,
                    null,
                    null,
                    string.Empty);
            }

            return new NpcAfterstorySnapshot(
                NpcAfterstorySnapshotStatus.Ready,
                normalizedNpc,
                definition,
                null,
                string.Empty);
        }

        public NpcAfterstoryChoiceResult TryApplyAfterstoryChoice(
            NpcAfterstorySaveData state,
            NpcRelationshipEpisodeSaveData episodeState,
            string afterstoryId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt)
        {
            var normalizedReceipt = Normalize(receiptId);
            if (state == null)
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.MissingState, null, null, normalizedReceipt, completedAt);
            }

            if (episodeState == null)
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.MissingEpisodeState, null, null, normalizedReceipt, completedAt);
            }

            NormalizeState(state);
            episodeState.EnsureRuntimeDefaults();
            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.InvalidReceipt, null, null, normalizedReceipt, completedAt);
            }

            if (completedAt == default)
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.InvalidCompletionTime, null, null, normalizedReceipt, completedAt);
            }

            if (state.HasAfterstoryReceipt(normalizedReceipt))
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.DuplicateReceipt, null, null, normalizedReceipt, completedAt);
            }

            var definition = FindAfterstory(afterstoryId);
            if (definition == null)
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.UnknownAfterstory, null, null, normalizedReceipt, completedAt);
            }

            var choice = definition.FindChoice(choiceId);
            if (choice == null)
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.UnknownChoice, definition, null, normalizedReceipt, completedAt);
            }

            if (state.HasCompletedAfterstory(definition.Id))
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.AlreadyCompleted, definition, choice, normalizedReceipt, completedAt);
            }

            if (!HasRequiredEpisodes(episodeState, definition))
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.PrerequisiteIncomplete, definition, choice, normalizedReceipt, completedAt);
            }

            if (!state.CanRecordAfterstory())
            {
                return AfterstoryResult(NpcAfterstoryChoiceStatus.StateCapacityFull, definition, choice, normalizedReceipt, completedAt);
            }

            state.RecordAfterstory(new NpcAfterstoryCompletionSaveData
            {
                afterstoryId = definition.Id,
                npcId = definition.NpcId,
                choiceId = choice.Id,
                receiptId = normalizedReceipt,
                keepsakeId = definition.KeepsakeId,
                completedAtIso = completedAt.ToString("O", CultureInfo.InvariantCulture)
            });
            return AfterstoryResult(
                NpcAfterstoryChoiceStatus.Applied,
                definition,
                choice,
                normalizedReceipt,
                completedAt);
        }

        public NpcWeeklyQuestSnapshot BuildWeeklyQuestSnapshot(
            NpcAfterstorySaveData state,
            NpcRelationshipEpisodeSaveData episodeState,
            string npcId,
            DateTimeOffset now)
        {
            var definition = FindWeeklyQuestByNpc(npcId);
            if (definition == null)
            {
                return new NpcWeeklyQuestSnapshot(
                    NpcWeeklyQuestSnapshotStatus.UnknownNpc,
                    null,
                    string.Empty,
                    0);
            }

            if (state == null)
            {
                return new NpcWeeklyQuestSnapshot(
                    NpcWeeklyQuestSnapshotStatus.MissingState,
                    null,
                    string.Empty,
                    0);
            }

            NormalizeState(state);
            var afterstory = FindAfterstoryByNpc(definition.NpcId);
            if (episodeState == null || !state.HasCompletedAfterstory(afterstory.Id))
            {
                return new NpcWeeklyQuestSnapshot(
                    NpcWeeklyQuestSnapshotStatus.Hidden,
                    null,
                    string.Empty,
                    0);
            }

            var clockStatus = EvaluateClock(state, now, out var weekKey);
            if (clockStatus == ClockEvaluation.InvalidTime)
            {
                return new NpcWeeklyQuestSnapshot(
                    NpcWeeklyQuestSnapshotStatus.InvalidTime,
                    definition,
                    string.Empty,
                    0);
            }

            if (clockStatus == ClockEvaluation.Rollback)
            {
                return new NpcWeeklyQuestSnapshot(
                    NpcWeeklyQuestSnapshotStatus.ClockRollback,
                    definition,
                    weekKey,
                    0);
            }

            var progress = state.FindWeeklyProgress(definition.NpcId, weekKey)?.progress ?? 0;
            if (state.HasClaimedWeeklyQuest(definition.NpcId, weekKey))
            {
                return new NpcWeeklyQuestSnapshot(
                    NpcWeeklyQuestSnapshotStatus.Claimed,
                    definition,
                    weekKey,
                    Math.Max(progress, definition.RequiredProgress));
            }

            return new NpcWeeklyQuestSnapshot(
                progress >= definition.RequiredProgress
                    ? NpcWeeklyQuestSnapshotStatus.ReadyToClaim
                    : NpcWeeklyQuestSnapshotStatus.InProgress,
                definition,
                weekKey,
                progress);
        }

        public NpcWeeklyProgressResult TryAdvanceWeeklyQuest(
            NpcAfterstorySaveData state,
            NpcRelationshipEpisodeSaveData episodeState,
            string npcId,
            string objectiveId,
            int amount,
            DateTimeOffset now)
        {
            var definition = FindWeeklyQuestByNpc(npcId);
            if (state == null)
            {
                return WeeklyProgressResult(NpcWeeklyProgressStatus.MissingState, definition, string.Empty, 0, 0);
            }

            if (episodeState == null)
            {
                return WeeklyProgressResult(NpcWeeklyProgressStatus.MissingEpisodeState, definition, string.Empty, 0, 0);
            }

            NormalizeState(state);
            if (definition == null)
            {
                return WeeklyProgressResult(NpcWeeklyProgressStatus.UnknownNpc, null, string.Empty, 0, 0);
            }

            var afterstory = FindAfterstoryByNpc(definition.NpcId);
            if (!state.HasCompletedAfterstory(afterstory.Id))
            {
                return WeeklyProgressResult(NpcWeeklyProgressStatus.AfterstoryIncomplete, definition, string.Empty, 0, 0);
            }

            if (!string.Equals(definition.ObjectiveId, Normalize(objectiveId), StringComparison.Ordinal))
            {
                return WeeklyProgressResult(NpcWeeklyProgressStatus.UnknownObjective, definition, string.Empty, 0, 0);
            }

            if (amount <= 0)
            {
                return WeeklyProgressResult(NpcWeeklyProgressStatus.InvalidAmount, definition, string.Empty, 0, 0);
            }

            var clockStatus = EvaluateClock(state, now, out var weekKey);
            if (clockStatus == ClockEvaluation.InvalidTime)
            {
                return WeeklyProgressResult(NpcWeeklyProgressStatus.InvalidTime, definition, string.Empty, 0, 0);
            }

            if (clockStatus == ClockEvaluation.Rollback)
            {
                return WeeklyProgressResult(NpcWeeklyProgressStatus.ClockRollback, definition, weekKey, 0, 0);
            }

            if (state.HasClaimedWeeklyQuest(definition.NpcId, weekKey))
            {
                return WeeklyProgressResult(
                    NpcWeeklyProgressStatus.AlreadyClaimed,
                    definition,
                    weekKey,
                    definition.RequiredProgress,
                    definition.RequiredProgress);
            }

            var progress = state.GetOrCreateWeeklyProgress(definition.NpcId, definition.Id, weekKey);
            var before = Math.Max(0, Math.Min(definition.RequiredProgress, progress.progress));
            var after = Math.Min(definition.RequiredProgress, SaturatingAdd(before, amount));
            progress.progress = after;
            progress.observedUtcTicks = Math.Max(progress.observedUtcTicks, now.UtcDateTime.Ticks);
            state.RecordClockObservation(now, weekKey);
            return WeeklyProgressResult(
                NpcWeeklyProgressStatus.Applied,
                definition,
                weekKey,
                before,
                after);
        }

        public NpcWeeklyClaimResult TryClaimWeeklyQuest(
            NpcAfterstorySaveData state,
            NpcRelationshipEpisodeSaveData episodeState,
            EconomySaveData economy,
            string npcId,
            string receiptId,
            DateTimeOffset now)
        {
            var definition = FindWeeklyQuestByNpc(npcId);
            var normalizedReceipt = Normalize(receiptId);
            if (state == null)
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.MissingState, definition, string.Empty, normalizedReceipt, now);
            }

            if (episodeState == null)
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.MissingEpisodeState, definition, string.Empty, normalizedReceipt, now);
            }

            if (economy == null)
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.MissingEconomy, definition, string.Empty, normalizedReceipt, now);
            }

            NormalizeState(state);
            if (definition == null)
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.UnknownNpc, null, string.Empty, normalizedReceipt, now);
            }

            var afterstory = FindAfterstoryByNpc(definition.NpcId);
            if (!state.HasCompletedAfterstory(afterstory.Id))
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.AfterstoryIncomplete, definition, string.Empty, normalizedReceipt, now);
            }

            if (string.IsNullOrEmpty(normalizedReceipt))
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.InvalidReceipt, definition, string.Empty, normalizedReceipt, now);
            }

            var clockStatus = EvaluateClock(state, now, out var weekKey);
            if (clockStatus == ClockEvaluation.InvalidTime)
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.InvalidTime, definition, string.Empty, normalizedReceipt, now);
            }

            if (clockStatus == ClockEvaluation.Rollback)
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.ClockRollback, definition, weekKey, normalizedReceipt, now);
            }

            if (state.HasWeeklyReceipt(normalizedReceipt))
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.DuplicateReceipt, definition, weekKey, normalizedReceipt, now);
            }

            if (state.HasClaimedWeeklyQuest(definition.NpcId, weekKey))
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.AlreadyClaimedThisWeek, definition, weekKey, normalizedReceipt, now);
            }

            var progress = state.FindWeeklyProgress(definition.NpcId, weekKey)?.progress ?? 0;
            if (progress < definition.RequiredProgress)
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.ObjectiveIncomplete, definition, weekKey, normalizedReceipt, now);
            }

            var currentCoins = Math.Max(0, economy.milkCoins);
            var currentMilkDrops = Math.Max(0, economy.milkDrops);
            if (!CanAdd(currentCoins, definition.Reward.Coins)
                || !CanAdd(currentMilkDrops, definition.Reward.MilkDrops))
            {
                return WeeklyClaimResult(NpcWeeklyClaimStatus.RewardOverflow, definition, weekKey, normalizedReceipt, now);
            }

            economy.milkCoins = currentCoins + definition.Reward.Coins;
            economy.milkDrops = currentMilkDrops + definition.Reward.MilkDrops;
            state.RecordWeeklyReceipt(new NpcWeeklyQuestReceiptSaveData
            {
                receiptId = normalizedReceipt,
                npcId = definition.NpcId,
                questId = definition.Id,
                weekKey = weekKey,
                claimedAtIso = now.ToString("O", CultureInfo.InvariantCulture)
            });
            while (state.weeklyReceipts.Count > NpcAfterstorySaveData.MaximumWeeklyReceipts)
            {
                state.weeklyReceipts.RemoveAt(0);
            }

            state.RecordClockObservation(now, weekKey);
            return WeeklyClaimResult(
                NpcWeeklyClaimStatus.Applied,
                definition,
                weekKey,
                normalizedReceipt,
                now);
        }

        public NpcKeepsakeSelectionResult TrySelectDisplayedKeepsake(
            NpcAfterstorySaveData state,
            string keepsakeId)
        {
            if (state == null)
            {
                return new NpcKeepsakeSelectionResult(NpcKeepsakeSelectionStatus.MissingState, null);
            }

            NormalizeState(state);
            var normalized = Normalize(keepsakeId);
            if (string.IsNullOrEmpty(normalized))
            {
                state.displayedKeepsakeId = string.Empty;
                return new NpcKeepsakeSelectionResult(NpcKeepsakeSelectionStatus.Cleared, null);
            }

            var profile = FindDisplayProfile(normalized);
            if (profile == null)
            {
                return new NpcKeepsakeSelectionResult(NpcKeepsakeSelectionStatus.UnknownKeepsake, null);
            }

            if (!state.HasKeepsake(normalized))
            {
                return new NpcKeepsakeSelectionResult(NpcKeepsakeSelectionStatus.NotOwned, profile);
            }

            state.displayedKeepsakeId = normalized;
            return new NpcKeepsakeSelectionResult(NpcKeepsakeSelectionStatus.Applied, profile);
        }

        public NpcKeepsakeDisplaySnapshot BuildDisplayedKeepsakeSnapshot(NpcAfterstorySaveData state)
        {
            if (state == null)
            {
                return new NpcKeepsakeDisplaySnapshot(null);
            }

            NormalizeState(state);
            return new NpcKeepsakeDisplaySnapshot(FindDisplayProfile(state.displayedKeepsakeId));
        }

        public NpcAfterstoryPanelSnapshot BuildPanelSnapshot(
            NpcAfterstorySaveData state,
            NpcRelationshipEpisodeSaveData episodeState,
            DateTimeOffset now)
        {
            if (state == null)
            {
                return NpcAfterstoryPanelSnapshot.Empty;
            }

            NormalizeState(state);
            var stories = new List<NpcAfterstoryNpcPanelSnapshot>(NpcCount);
            var ownedKeepsakes = new List<NpcKeepsakeDisplayProfile>(NpcCount);
            for (var index = 0; index < Afterstories.Length; index += 1)
            {
                var afterstory = BuildAfterstorySnapshot(state, episodeState, Afterstories[index].NpcId);
                if (afterstory.HasVisibleContent)
                {
                    stories.Add(new NpcAfterstoryNpcPanelSnapshot(
                        afterstory,
                        BuildWeeklyQuestSnapshot(state, episodeState, Afterstories[index].NpcId, now)));
                }

                var profile = FindDisplayProfile(Afterstories[index].KeepsakeId);
                if (profile != null && state.HasKeepsake(profile.KeepsakeId))
                {
                    ownedKeepsakes.Add(profile);
                }
            }

            return new NpcAfterstoryPanelSnapshot(
                stories.ToArray(),
                ownedKeepsakes.ToArray(),
                BuildDisplayedKeepsakeSnapshot(state));
        }

        public static string GetMondayWeekKey(DateTimeOffset localTime)
        {
            if (localTime == default)
            {
                return string.Empty;
            }

            var date = localTime.Date;
            var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-daysSinceMonday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static bool HasRequiredEpisodes(
            NpcRelationshipEpisodeSaveData episodeState,
            NpcAfterstoryDefinition definition)
        {
            return episodeState != null
                && definition != null
                && episodeState.HasCompletedEpisode(definition.FirstRequiredEpisodeId)
                && episodeState.HasCompletedEpisode(definition.SecondRequiredEpisodeId);
        }

        private static NpcAfterstoryChoiceResult AfterstoryResult(
            NpcAfterstoryChoiceStatus status,
            NpcAfterstoryDefinition definition,
            NpcAfterstoryChoiceDefinition choice,
            string receiptId,
            DateTimeOffset completedAt)
        {
            return new NpcAfterstoryChoiceResult(status, definition, choice, receiptId, completedAt);
        }

        private static NpcWeeklyProgressResult WeeklyProgressResult(
            NpcWeeklyProgressStatus status,
            NpcWeeklyQuestDefinition definition,
            string weekKey,
            int before,
            int after)
        {
            return new NpcWeeklyProgressResult(status, definition, weekKey, before, after);
        }

        private static NpcWeeklyClaimResult WeeklyClaimResult(
            NpcWeeklyClaimStatus status,
            NpcWeeklyQuestDefinition definition,
            string weekKey,
            string receiptId,
            DateTimeOffset claimedAt)
        {
            return new NpcWeeklyClaimResult(status, definition, weekKey, receiptId, claimedAt);
        }

        private static ClockEvaluation EvaluateClock(
            NpcAfterstorySaveData state,
            DateTimeOffset now,
            out string weekKey)
        {
            weekKey = GetMondayWeekKey(now);
            if (state == null || now == default || string.IsNullOrEmpty(weekKey))
            {
                return ClockEvaluation.InvalidTime;
            }

            if ((state.latestObservedUtcTicks > 0L
                    && now.UtcDateTime.Ticks < state.latestObservedUtcTicks)
                || (!string.IsNullOrEmpty(state.latestObservedWeekKey)
                    && string.CompareOrdinal(weekKey, state.latestObservedWeekKey) < 0))
            {
                return ClockEvaluation.Rollback;
            }

            return ClockEvaluation.Valid;
        }

        private static bool CanAdd(int current, int added)
        {
            return current >= 0 && added >= 0 && current <= int.MaxValue - added;
        }

        private static int SaturatingAdd(int current, int added)
        {
            return current > int.MaxValue - added ? int.MaxValue : current + added;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private enum ClockEvaluation
        {
            Valid = 0,
            InvalidTime = 1,
            Rollback = 2
        }
    }
}
