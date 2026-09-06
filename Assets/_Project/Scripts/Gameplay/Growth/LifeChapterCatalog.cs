using System;
using System.Collections.Generic;

namespace CheeseTama.Gameplay.Growth
{
    public enum LifeChapterCareActionKind
    {
        Feed = 0,
        Cook = 1,
        Play = 2,
        Clean = 3,
        Rest = 4
    }

    public enum LifeChapterRewardKind
    {
        None = 0,
        MilkCoins = 1,
        MilkDrops = 2,
        CollectionFragments = 3
    }

    public sealed class LifeChapterObjectiveDefinition
    {
        internal LifeChapterObjectiveDefinition(
            string id,
            LifeChapterCareActionKind actionKind,
            string displayName)
        {
            Id = id ?? string.Empty;
            ActionKind = actionKind;
            DisplayName = displayName ?? string.Empty;
        }

        public string Id { get; }
        public LifeChapterCareActionKind ActionKind { get; }
        public string DisplayName { get; }
    }

    public sealed class LifeChapterRecordDefinition
    {
        internal LifeChapterRecordDefinition(string title, string detail)
        {
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Title { get; }
        public string Detail { get; }
    }

    public readonly struct LifeChapterRewardDefinition
    {
        internal LifeChapterRewardDefinition(LifeChapterRewardKind kind, int amount)
        {
            Kind = kind;
            Amount = Math.Max(0, amount);
        }

        public LifeChapterRewardKind Kind { get; }
        public int Amount { get; }
        public bool HasReward => Kind != LifeChapterRewardKind.None && Amount > 0;
    }

    public sealed class LifeChapterChoiceDefinition
    {
        internal LifeChapterChoiceDefinition(
            string id,
            string label,
            string resultMessage,
            LifeChapterRecordDefinition memory,
            LifeChapterRecordDefinition codex,
            LifeChapterRewardDefinition reward)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            ResultMessage = resultMessage ?? string.Empty;
            Memory = memory;
            Codex = codex;
            Reward = reward;
        }

        public string Id { get; }
        public string Label { get; }
        public string ResultMessage { get; }
        public LifeChapterRecordDefinition Memory { get; }
        public LifeChapterRecordDefinition Codex { get; }
        public LifeChapterRewardDefinition Reward { get; }
    }

    public sealed class LifeChapterDefinition
    {
        private readonly LifeChapterObjectiveDefinition[] objectives;
        private readonly LifeChapterChoiceDefinition[] choices;

        internal LifeChapterDefinition(
            string id,
            int minimumLevel,
            string title,
            string body,
            LifeChapterObjectiveDefinition[] objectives,
            LifeChapterChoiceDefinition[] choices)
        {
            Id = id ?? string.Empty;
            MinimumLevel = Math.Max(1, minimumLevel);
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            this.objectives = objectives ?? Array.Empty<LifeChapterObjectiveDefinition>();
            this.choices = choices ?? Array.Empty<LifeChapterChoiceDefinition>();
        }

        public string Id { get; }
        public int MinimumLevel { get; }
        public string Title { get; }
        public string Body { get; }
        public IReadOnlyList<LifeChapterObjectiveDefinition> Objectives => objectives;
        public IReadOnlyList<LifeChapterChoiceDefinition> Choices => choices;

        public LifeChapterObjectiveDefinition FindObjective(string objectiveId)
        {
            var normalized = (objectiveId ?? string.Empty).Trim();
            for (var index = 0; index < objectives.Length; index += 1)
            {
                if (string.Equals(objectives[index].Id, normalized, StringComparison.Ordinal))
                {
                    return objectives[index];
                }
            }

            return null;
        }

        public LifeChapterObjectiveDefinition FindObjective(
            LifeChapterCareActionKind actionKind)
        {
            for (var index = 0; index < objectives.Length; index += 1)
            {
                if (objectives[index].ActionKind == actionKind)
                {
                    return objectives[index];
                }
            }

            return null;
        }

        public LifeChapterChoiceDefinition FindChoice(string choiceId)
        {
            var normalized = (choiceId ?? string.Empty).Trim();
            for (var index = 0; index < choices.Length; index += 1)
            {
                if (string.Equals(choices[index].Id, normalized, StringComparison.Ordinal))
                {
                    return choices[index];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Data-only milestone chapters. Runtime progression consumes these definitions in order;
    /// adding presentation logic or save fields is not required when chapter text changes.
    /// </summary>
    public sealed class LifeChapterCatalog
    {
        public const int ObjectivesPerChapter = 3;
        public const int ChoicesPerChapter = 2;

        private static readonly int[] RequiredLevels = { 15, 20, 28, 33 };
        private static readonly LifeChapterDefinition[] ShippedDefinitions =
        {
            new LifeChapterDefinition(
                "life_chapter_15_soft_steps",
                15,
                "말랑한 하루의 첫걸음",
                "몸이 한층 말랑하게 자란 치즈타마가 밀크룸 곳곳을 새롭게 바라봅니다. 익숙한 돌봄을 함께하며 첫 성장의 리듬을 찾아 주세요.",
                new[]
                {
                    Objective("life15_feed", LifeChapterCareActionKind.Feed, "좋아하는 것을 천천히 먹여 보기"),
                    Objective("life15_play", LifeChapterCareActionKind.Play, "가까이에서 함께 놀아 보기"),
                    Objective("life15_rest", LifeChapterCareActionKind.Rest, "편안한 자리에서 함께 쉬기")
                },
                new[]
                {
                    Choice(
                        "reach_out_first",
                        "먼저 손 내밀기",
                        "치즈타마가 내민 손끝에 살며시 기대며 새 하루를 시작했어요.",
                        "먼저 건넨 손",
                        "말랑하게 자란 날, 먼저 손을 내밀자 치즈타마가 조심스럽게 기대 왔다.",
                        "말랑기의 첫 인사",
                        "레벨 15의 치즈타마는 익숙한 돌봄 속에서 먼저 다가오는 용기를 배웠다.",
                        LifeChapterRewardKind.MilkCoins,
                        30),
                    Choice(
                        "wait_side_by_side",
                        "곁에서 기다리기",
                        "서두르지 않고 기다리자 치즈타마가 스스로 한 걸음 다가왔어요.",
                        "기다림 끝의 한 걸음",
                        "말랑하게 자란 날, 곁을 지키며 기다리자 치즈타마가 먼저 다가왔다.",
                        "말랑기의 느린 인사",
                        "레벨 15의 치즈타마는 편안한 기다림 속에서 자기만의 속도로 마음을 열었다.",
                        LifeChapterRewardKind.MilkCoins,
                        30)
                }),
            new LifeChapterDefinition(
                "life_chapter_20_room_rhythm",
                20,
                "함께 만든 방의 리듬",
                "성장한 치즈타마는 요리 소리와 정돈된 방, 놀이의 박자를 기억합니다. 둘만의 생활 리듬을 한 번 완성해 보세요.",
                new[]
                {
                    Objective("life20_cook", LifeChapterCareActionKind.Cook, "함께 먹을 간식을 만들기"),
                    Objective("life20_clean", LifeChapterCareActionKind.Clean, "밀크룸을 산뜻하게 정돈하기"),
                    Objective("life20_play", LifeChapterCareActionKind.Play, "둘만의 놀이 박자를 맞추기")
                },
                new[]
                {
                    Choice(
                        "keep_daily_rhythm",
                        "익숙한 리듬 이어가기",
                        "익숙한 소리들이 이어지자 치즈타마가 편안하게 방 한가운데 자리를 잡았어요.",
                        "우리 방의 박자",
                        "요리와 청소, 놀이가 이어지는 익숙한 순서가 둘만의 생활 리듬이 되었다.",
                        "성장기의 생활 리듬",
                        "레벨 20의 치즈타마는 반복되는 돌봄을 안전한 하루의 신호로 기억한다.",
                        LifeChapterRewardKind.MilkDrops,
                        5),
                    Choice(
                        "follow_tama_rhythm",
                        "치즈타마의 박자 따르기",
                        "치즈타마가 고른 순서대로 움직이자 방 안에 새로운 박자가 생겼어요.",
                        "새로 맞춘 박자",
                        "치즈타마가 이끄는 순서에 맞춰 둘만의 새로운 생활 리듬을 만들었다.",
                        "성장기의 주도성",
                        "레벨 20의 치즈타마는 돌봄을 받는 데서 나아가 하루의 순서를 함께 정하기 시작한다.",
                        LifeChapterRewardKind.MilkDrops,
                        5)
                }),
            new LifeChapterDefinition(
                "life_chapter_28_mature_flavor",
                28,
                "천천히 익는 마음",
                "숙성기에 들어선 치즈타마의 표정과 향에는 지나온 돌봄이 차분히 배어 있습니다. 오늘의 상태를 살피며 오래 남을 기억을 골라 주세요.",
                new[]
                {
                    Objective("life28_feed", LifeChapterCareActionKind.Feed, "상태에 맞는 먹이를 챙기기"),
                    Objective("life28_clean", LifeChapterCareActionKind.Clean, "쉬어 갈 공간을 정돈하기"),
                    Objective("life28_rest", LifeChapterCareActionKind.Rest, "조용히 숙성의 시간을 보내기")
                },
                new[]
                {
                    Choice(
                        "write_care_note",
                        "오늘의 돌봄을 적어 두기",
                        "짧은 돌봄 기록을 읽어 주자 치즈타마가 지난 시간을 알아본 듯 고개를 끄덕였어요.",
                        "숙성기의 돌봄 기록",
                        "무엇을 먹고 어디서 쉬었는지 적은 짧은 기록이 오래 함께한 시간을 선명하게 만들었다.",
                        "숙성 기록법",
                        "레벨 28의 치즈타마는 일상의 작은 돌봄이 쌓인 기록에 안정감을 느낀다.",
                        LifeChapterRewardKind.CollectionFragments,
                        2),
                    Choice(
                        "remember_room_scent",
                        "방의 향을 기억하기",
                        "따뜻한 우유와 깨끗한 천의 향이 오늘을 기억하는 둘만의 표지가 되었어요.",
                        "밀크룸의 익숙한 향",
                        "말보다 먼저 떠오르는 방의 향을 오늘의 기억으로 남겼다.",
                        "숙성기의 감각 기억",
                        "레벨 28의 치즈타마는 익숙한 향과 온도를 오래된 돌봄의 표지로 받아들인다.",
                        LifeChapterRewardKind.CollectionFragments,
                        2)
                }),
            new LifeChapterDefinition(
                "life_chapter_33_shared_table",
                33,
                "완성된 하루의 식탁",
                "완성된 모습의 치즈타마와 지금까지의 하루를 한자리에 펼쳐 봅니다. 마지막 성장 장을 닫기보다 다음 시간을 위한 자리를 정해 주세요.",
                new[]
                {
                    Objective("life33_cook", LifeChapterCareActionKind.Cook, "함께 나눌 한 접시 준비하기"),
                    Objective("life33_play", LifeChapterCareActionKind.Play, "가장 익숙한 놀이를 다시 하기"),
                    Objective("life33_rest", LifeChapterCareActionKind.Rest, "하루 끝에 나란히 쉬기")
                },
                new[]
                {
                    Choice(
                        "set_shared_table",
                        "지나온 시간을 식탁에 놓기",
                        "작은 접시마다 지나온 날의 이야기가 놓이고 치즈타마가 가장 좋아한 자리를 골랐어요.",
                        "함께 차린 성장 식탁",
                        "첫 돌봄부터 완성된 모습까지의 시간을 작은 식탁 위에 하나씩 펼쳐 보았다.",
                        "완성형의 생활 기록",
                        "레벨 33은 돌봄의 끝이 아니라 함께 쌓은 생활을 돌아보는 첫 완성점이다.",
                        LifeChapterRewardKind.MilkCoins,
                        100),
                    Choice(
                        "leave_next_seat_open",
                        "다음 하루의 자리 남기기",
                        "빈 자리 하나를 남겨 두자 치즈타마가 내일의 접시를 상상하듯 오래 바라보았어요.",
                        "내일을 위한 빈 자리",
                        "완성된 하루 옆에 아직 오지 않은 다음 시간을 위한 자리를 비워 두었다.",
                        "완성 뒤의 열린 자리",
                        "레벨 33 뒤에도 돌봄과 이야기는 이어지며, 빈 자리는 다음 성장 길을 받아들인다.",
                        LifeChapterRewardKind.MilkCoins,
                        100)
                })
        };

        private readonly LifeChapterDefinition[] definitions;

        internal LifeChapterCatalog(LifeChapterDefinition[] definitions)
        {
            this.definitions = definitions ?? Array.Empty<LifeChapterDefinition>();
        }

        public IReadOnlyList<LifeChapterDefinition> All => definitions;
        public bool HasContent => definitions.Length > 0;
        public static LifeChapterCatalog Default { get; } =
            new LifeChapterCatalog(ShippedDefinitions);
        public static LifeChapterCatalog Empty { get; } =
            new LifeChapterCatalog(Array.Empty<LifeChapterDefinition>());

        public LifeChapterDefinition Find(string chapterId)
        {
            var normalized = (chapterId ?? string.Empty).Trim();
            for (var index = 0; index < definitions.Length; index += 1)
            {
                if (string.Equals(definitions[index].Id, normalized, StringComparison.Ordinal))
                {
                    return definitions[index];
                }
            }

            return null;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (definitions.Length != RequiredLevels.Length)
            {
                errors.Add($"생활 챕터는 정확히 {RequiredLevels.Length}편이어야 합니다.");
            }

            var knownChapterIds = new HashSet<string>(StringComparer.Ordinal);
            var inspectedCount = Math.Min(definitions.Length, RequiredLevels.Length);
            for (var index = 0; index < definitions.Length; index += 1)
            {
                var chapter = definitions[index];
                if (chapter == null)
                {
                    errors.Add($"생활 챕터 {index} 정의가 없습니다.");
                    continue;
                }

                if (!IsStableId(chapter.Id) || !knownChapterIds.Add(chapter.Id))
                {
                    errors.Add($"생활 챕터 ID가 올바르지 않거나 중복됩니다: {chapter.Id}");
                }

                if (index < inspectedCount && chapter.MinimumLevel != RequiredLevels[index])
                {
                    errors.Add(
                        $"생활 챕터 {chapter.Id}의 레벨은 {RequiredLevels[index]}여야 합니다.");
                }

                if (string.IsNullOrWhiteSpace(chapter.Title)
                    || string.IsNullOrWhiteSpace(chapter.Body))
                {
                    errors.Add($"생활 챕터 {chapter.Id}의 표시 문구가 비어 있습니다.");
                }

                ValidateObjectives(chapter, errors);
                ValidateChoices(chapter, errors);
            }

            return errors;
        }

        private static LifeChapterObjectiveDefinition Objective(
            string id,
            LifeChapterCareActionKind actionKind,
            string displayName)
        {
            return new LifeChapterObjectiveDefinition(id, actionKind, displayName);
        }

        private static LifeChapterChoiceDefinition Choice(
            string id,
            string label,
            string resultMessage,
            string memoryTitle,
            string memoryDetail,
            string codexTitle,
            string codexDetail,
            LifeChapterRewardKind rewardKind,
            int rewardAmount)
        {
            return new LifeChapterChoiceDefinition(
                id,
                label,
                resultMessage,
                new LifeChapterRecordDefinition(memoryTitle, memoryDetail),
                new LifeChapterRecordDefinition(codexTitle, codexDetail),
                new LifeChapterRewardDefinition(rewardKind, rewardAmount));
        }

        private static void ValidateObjectives(
            LifeChapterDefinition chapter,
            ICollection<string> errors)
        {
            if (chapter.Objectives.Count != ObjectivesPerChapter)
            {
                errors.Add(
                    $"생활 챕터 {chapter.Id}에는 목표가 정확히 {ObjectivesPerChapter}개여야 합니다.");
            }

            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            var knownKinds = new HashSet<LifeChapterCareActionKind>();
            for (var index = 0; index < chapter.Objectives.Count; index += 1)
            {
                var objective = chapter.Objectives[index];
                if (objective == null
                    || !IsStableId(objective.Id)
                    || !knownIds.Add(objective.Id)
                    || !knownKinds.Add(objective.ActionKind)
                    || string.IsNullOrWhiteSpace(objective.DisplayName))
                {
                    errors.Add($"생활 챕터 {chapter.Id}의 목표 {index} 정의가 올바르지 않습니다.");
                }
            }
        }

        private static void ValidateChoices(
            LifeChapterDefinition chapter,
            ICollection<string> errors)
        {
            if (chapter.Choices.Count != ChoicesPerChapter)
            {
                errors.Add(
                    $"생활 챕터 {chapter.Id}에는 선택지가 정확히 {ChoicesPerChapter}개여야 합니다.");
            }

            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < chapter.Choices.Count; index += 1)
            {
                var choice = chapter.Choices[index];
                if (choice == null
                    || !IsStableId(choice.Id)
                    || !knownIds.Add(choice.Id)
                    || string.IsNullOrWhiteSpace(choice.Label)
                    || string.IsNullOrWhiteSpace(choice.ResultMessage)
                    || choice.Memory == null
                    || string.IsNullOrWhiteSpace(choice.Memory.Title)
                    || string.IsNullOrWhiteSpace(choice.Memory.Detail)
                    || choice.Codex == null
                    || string.IsNullOrWhiteSpace(choice.Codex.Title)
                    || string.IsNullOrWhiteSpace(choice.Codex.Detail)
                    || !choice.Reward.HasReward)
                {
                    errors.Add($"생활 챕터 {chapter.Id}의 선택지 {index} 정의가 올바르지 않습니다.");
                }
            }
        }

        internal static bool IsStableId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 100)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                var character = value[index];
                var valid = (character >= 'a' && character <= 'z')
                    || (character >= '0' && character <= '9')
                    || character == '_'
                    || character == '-'
                    || character == '.';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
