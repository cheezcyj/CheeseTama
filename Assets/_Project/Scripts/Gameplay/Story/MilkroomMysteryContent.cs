using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTama.Gameplay.Story
{
    [Serializable]
    public sealed class MilkroomMysteryContentDocument
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<MilkroomMysteryChapterContent> chapters =
            new List<MilkroomMysteryChapterContent>();
    }

    [Serializable]
    public sealed class MilkroomMysteryChapterContent
    {
        public string id = string.Empty;
        public string title = string.Empty;
        public string description = string.Empty;
        public string prerequisiteChapterId = string.Empty;
        public MilkroomMysteryTriggerContent trigger = new MilkroomMysteryTriggerContent();
        public List<MilkroomMysteryChoiceContent> choices =
            new List<MilkroomMysteryChoiceContent>();
    }

    [Serializable]
    public sealed class MilkroomMysteryTriggerContent
    {
        public string kind = string.Empty;
        public int minimumLevel;
    }

    [Serializable]
    public sealed class MilkroomMysteryChoiceContent
    {
        public string id = string.Empty;
        public string label = string.Empty;
        public string resultMessage = string.Empty;
        public MilkroomMysteryRecordContent memory = new MilkroomMysteryRecordContent();
        public MilkroomMysteryRecordContent codex = new MilkroomMysteryRecordContent();
        public MilkroomMysteryRewardContent reward = new MilkroomMysteryRewardContent();
    }

    [Serializable]
    public sealed class MilkroomMysteryRecordContent
    {
        public string title = string.Empty;
        public string detail = string.Empty;
    }

    [Serializable]
    public sealed class MilkroomMysteryRewardContent
    {
        public string kind = string.Empty;
        public string id = string.Empty;
        public int amount;
    }

    public enum MilkroomMysteryTriggerKind
    {
        LevelAtLeast = 0,
        StarRoute = 1
    }

    public enum MilkroomMysteryRewardKind
    {
        Coin = 0,
        MilkDrop = 1,
        StarDrop = 2,
        CollectionFragment = 3,
        Decoration = 4
    }

    public readonly struct MilkroomMysteryTriggerDefinition
    {
        internal MilkroomMysteryTriggerDefinition(
            MilkroomMysteryTriggerKind kind,
            int minimumLevel)
        {
            Kind = kind;
            MinimumLevel = Math.Max(0, minimumLevel);
        }

        public MilkroomMysteryTriggerKind Kind { get; }
        public int MinimumLevel { get; }

        public bool IsSatisfied(MilkroomMysteryProgress progress)
        {
            switch (Kind)
            {
                case MilkroomMysteryTriggerKind.LevelAtLeast:
                    return progress.Level >= MinimumLevel;
                case MilkroomMysteryTriggerKind.StarRoute:
                    return progress.StarRouteUnlocked;
                default:
                    return false;
            }
        }
    }

    public sealed class MilkroomMysteryRecordDefinition
    {
        internal MilkroomMysteryRecordDefinition(string title, string detail)
        {
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Title { get; }
        public string Detail { get; }
    }

    public readonly struct MilkroomMysteryRewardDefinition
    {
        internal MilkroomMysteryRewardDefinition(
            MilkroomMysteryRewardKind kind,
            string id,
            int amount)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Amount = Math.Max(0, amount);
        }

        public MilkroomMysteryRewardKind Kind { get; }
        public string Id { get; }
        public int Amount { get; }
    }

    public sealed class MilkroomMysteryChoiceDefinition
    {
        internal MilkroomMysteryChoiceDefinition(
            string id,
            string label,
            string resultMessage,
            MilkroomMysteryRecordDefinition memory,
            MilkroomMysteryRecordDefinition codex,
            MilkroomMysteryRewardDefinition reward)
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
        public MilkroomMysteryRecordDefinition Memory { get; }
        public MilkroomMysteryRecordDefinition Codex { get; }
        public MilkroomMysteryRewardDefinition Reward { get; }
    }

    public sealed class MilkroomMysteryChapterDefinition
    {
        private readonly MilkroomMysteryChoiceDefinition[] choices;

        internal MilkroomMysteryChapterDefinition(
            string id,
            string title,
            string description,
            string prerequisiteChapterId,
            MilkroomMysteryTriggerDefinition trigger,
            MilkroomMysteryChoiceDefinition[] choices)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            PrerequisiteChapterId = prerequisiteChapterId ?? string.Empty;
            Trigger = trigger;
            this.choices = choices ?? Array.Empty<MilkroomMysteryChoiceDefinition>();
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string PrerequisiteChapterId { get; }
        public MilkroomMysteryTriggerDefinition Trigger { get; }
        public IReadOnlyList<MilkroomMysteryChoiceDefinition> Choices => choices;

        public MilkroomMysteryChoiceDefinition FindChoice(string choiceId)
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

    public sealed class MilkroomMysteryCatalog
    {
        private readonly MilkroomMysteryChapterDefinition[] chapters;

        internal MilkroomMysteryCatalog(MilkroomMysteryChapterDefinition[] chapters)
        {
            this.chapters = chapters ?? Array.Empty<MilkroomMysteryChapterDefinition>();
        }

        public IReadOnlyList<MilkroomMysteryChapterDefinition> Chapters => chapters;
        public bool HasContent => chapters.Length > 0;

        public MilkroomMysteryChapterDefinition Find(string chapterId)
        {
            var normalized = (chapterId ?? string.Empty).Trim();
            for (var index = 0; index < chapters.Length; index += 1)
            {
                if (string.Equals(chapters[index].Id, normalized, StringComparison.Ordinal))
                {
                    return chapters[index];
                }
            }

            return null;
        }

        public static MilkroomMysteryCatalog Empty { get; } =
            new MilkroomMysteryCatalog(Array.Empty<MilkroomMysteryChapterDefinition>());
    }

    public static class MilkroomMysteryContentLoader
    {
        public const string DefaultResourcePath = "Content/MilkroomMysteryChapters";
        public const string CodexRecordPrefix = "milkroom_mystery_codex:";
        public const int MaximumJsonCharacters = 262144;
        public const int MaximumChapters = 32;
        public const int ChoicesPerChapter = 2;
        public const int MaximumRewardAmount = 1000000;

        public static bool TryReloadDefault(
            out MilkroomMysteryCatalog catalog,
            out IReadOnlyList<string> errors)
        {
            var asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                catalog = MilkroomMysteryCatalog.Empty;
                errors = new[] { "미스터리 콘텐츠 JSON을 찾을 수 없습니다." };
                return false;
            }

            return TryLoadFromJson(asset.text, out catalog, out errors);
        }

        public static string BuildCodexRecordId(string chapterId, string choiceId)
        {
            return CodexRecordPrefix
                + (chapterId ?? string.Empty).Trim()
                + ":"
                + (choiceId ?? string.Empty).Trim();
        }

        public static bool TryResolveCodexRecord(
            string recordId,
            out MilkroomMysteryRecordDefinition record)
        {
            record = null;
            var normalized = (recordId ?? string.Empty).Trim();
            if (!normalized.StartsWith(CodexRecordPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var payload = normalized.Substring(CodexRecordPrefix.Length);
            var separator = payload.IndexOf(':');
            if (separator <= 0 || separator + 1 >= payload.Length)
            {
                return false;
            }

            var chapterId = payload.Substring(0, separator);
            var choiceId = payload.Substring(separator + 1);
            if (!TryReloadDefault(out var catalog, out _))
            {
                return false;
            }

            var choice = catalog.Find(chapterId)?.FindChoice(choiceId);
            record = choice?.Codex;
            return record != null;
        }

        public static bool TryLoadFromJson(
            string json,
            out MilkroomMysteryCatalog catalog,
            out IReadOnlyList<string> errors)
        {
            var validationErrors = new List<string>();
            catalog = MilkroomMysteryCatalog.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                validationErrors.Add("미스터리 콘텐츠 JSON이 비어 있습니다.");
                errors = validationErrors;
                return false;
            }

            if (json.Length > MaximumJsonCharacters)
            {
                validationErrors.Add(
                    $"미스터리 콘텐츠 JSON은 {MaximumJsonCharacters}자를 초과할 수 없습니다.");
                errors = validationErrors;
                return false;
            }

            MilkroomMysteryContentDocument document;
            try
            {
                document = JsonUtility.FromJson<MilkroomMysteryContentDocument>(json);
            }
            catch (Exception exception)
            {
                validationErrors.Add($"미스터리 콘텐츠 JSON 해석 실패: {exception.Message}");
                errors = validationErrors;
                return false;
            }

            validationErrors.AddRange(Validate(document));
            if (validationErrors.Count > 0)
            {
                errors = validationErrors;
                return false;
            }

            catalog = BuildCatalog(document);
            errors = Array.Empty<string>();
            return true;
        }

        public static IReadOnlyList<string> Validate(MilkroomMysteryContentDocument document)
        {
            var errors = new List<string>();
            if (document == null)
            {
                errors.Add("미스터리 콘텐츠 문서가 없습니다.");
                return errors;
            }

            if (document.schemaVersion != MilkroomMysteryContentDocument.CurrentSchemaVersion)
            {
                errors.Add(
                    $"지원하지 않는 미스터리 콘텐츠 스키마 버전입니다: {document.schemaVersion}");
            }

            if (document.chapters == null || document.chapters.Count == 0)
            {
                errors.Add("미스터리 장을 하나 이상 정의해야 합니다.");
                return errors;
            }

            if (document.chapters.Count > MaximumChapters)
            {
                errors.Add($"미스터리 장은 최대 {MaximumChapters}개까지 정의할 수 있습니다.");
            }

            var inspectedCount = Math.Min(document.chapters.Count, MaximumChapters);
            var chapterIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < inspectedCount; index += 1)
            {
                var chapter = document.chapters[index];
                var path = $"chapters[{index}]";
                if (chapter == null)
                {
                    errors.Add($"{path} 항목이 없습니다.");
                    continue;
                }

                ValidateId(chapter.id, $"{path}.id", required: true, errors);
                if (IsValidId(chapter.id))
                {
                    if (chapterIndexes.ContainsKey(chapter.id))
                    {
                        errors.Add($"중복된 미스터리 장 ID입니다: {chapter.id}");
                    }
                    else
                    {
                        chapterIndexes.Add(chapter.id, index);
                    }
                }

                ValidateText(chapter.title, $"{path}.title", 80, errors);
                ValidateText(chapter.description, $"{path}.description", 500, errors);
                ValidateId(
                    chapter.prerequisiteChapterId,
                    $"{path}.prerequisiteChapterId",
                    required: false,
                    errors);
                ValidateTrigger(chapter.trigger, $"{path}.trigger", errors);
                ValidateChoices(chapter.choices, $"{path}.choices", errors);
            }

            for (var index = 0; index < inspectedCount; index += 1)
            {
                var chapter = document.chapters[index];
                if (chapter == null || string.IsNullOrEmpty(chapter.prerequisiteChapterId))
                {
                    continue;
                }

                if (!chapterIndexes.TryGetValue(chapter.prerequisiteChapterId, out var prerequisiteIndex))
                {
                    errors.Add(
                        $"{chapter.id}의 선행 장을 찾을 수 없습니다: {chapter.prerequisiteChapterId}");
                }
                else if (prerequisiteIndex >= index)
                {
                    errors.Add($"{chapter.id}의 선행 장은 현재 장보다 앞에 있어야 합니다.");
                }
            }

            return errors;
        }

        private static void ValidateTrigger(
            MilkroomMysteryTriggerContent trigger,
            string path,
            ICollection<string> errors)
        {
            if (trigger == null)
            {
                errors.Add($"{path} 항목이 없습니다.");
                return;
            }

            if (string.Equals(trigger.kind, "level_at_least", StringComparison.Ordinal))
            {
                if (trigger.minimumLevel < 1 || trigger.minimumLevel > 999)
                {
                    errors.Add($"{path}.minimumLevel은 1~999 범위여야 합니다.");
                }

                return;
            }

            if (string.Equals(trigger.kind, "star_route", StringComparison.Ordinal))
            {
                if (trigger.minimumLevel != 0)
                {
                    errors.Add($"{path}.star_route에는 minimumLevel을 지정할 수 없습니다.");
                }

                return;
            }

            errors.Add($"{path}.kind 값이 올바르지 않습니다: {trigger.kind ?? string.Empty}");
        }

        private static void ValidateChoices(
            List<MilkroomMysteryChoiceContent> choices,
            string path,
            ICollection<string> errors)
        {
            if (choices == null || choices.Count != ChoicesPerChapter)
            {
                errors.Add($"{path}에는 선택지를 정확히 {ChoicesPerChapter}개 정의해야 합니다.");
            }

            if (choices == null)
            {
                return;
            }

            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            var inspectedCount = Math.Min(choices.Count, ChoicesPerChapter);
            for (var index = 0; index < inspectedCount; index += 1)
            {
                var choice = choices[index];
                var choicePath = $"{path}[{index}]";
                if (choice == null)
                {
                    errors.Add($"{choicePath} 항목이 없습니다.");
                    continue;
                }

                ValidateId(choice.id, $"{choicePath}.id", required: true, errors);
                if (IsValidId(choice.id) && !knownIds.Add(choice.id))
                {
                    errors.Add($"{path}에 중복 선택지 ID가 있습니다: {choice.id}");
                }

                ValidateText(choice.label, $"{choicePath}.label", 80, errors);
                ValidateText(choice.resultMessage, $"{choicePath}.resultMessage", 400, errors);
                ValidateRecord(choice.memory, $"{choicePath}.memory", 400, errors);
                ValidateRecord(choice.codex, $"{choicePath}.codex", 600, errors);
                ValidateReward(choice.reward, $"{choicePath}.reward", errors);
            }
        }

        private static void ValidateRecord(
            MilkroomMysteryRecordContent record,
            string path,
            int detailLength,
            ICollection<string> errors)
        {
            if (record == null)
            {
                errors.Add($"{path} 항목이 없습니다.");
                return;
            }

            ValidateText(record.title, $"{path}.title", 80, errors);
            ValidateText(record.detail, $"{path}.detail", detailLength, errors);
        }

        private static void ValidateReward(
            MilkroomMysteryRewardContent reward,
            string path,
            ICollection<string> errors)
        {
            if (reward == null)
            {
                errors.Add($"{path} 항목이 없습니다.");
                return;
            }

            if (!TryParseRewardKind(reward.kind, out var kind))
            {
                errors.Add($"{path}.kind 값이 올바르지 않습니다: {reward.kind ?? string.Empty}");
                return;
            }

            if (reward.amount < 1 || reward.amount > MaximumRewardAmount)
            {
                errors.Add($"{path}.amount는 1~{MaximumRewardAmount} 범위여야 합니다.");
            }

            if (kind == MilkroomMysteryRewardKind.Decoration)
            {
                ValidateId(reward.id, $"{path}.id", required: true, errors);
                if (reward.amount != 1)
                {
                    errors.Add($"{path}의 장식 보상 수량은 1이어야 합니다.");
                }
            }
            else if (!string.IsNullOrEmpty(reward.id))
            {
                errors.Add($"{path}의 재화 보상에는 id를 지정할 수 없습니다.");
            }
        }

        private static void ValidateText(
            string value,
            string path,
            int maximumLength,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{path} 값이 비어 있습니다.");
                return;
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                errors.Add($"{path} 값 앞뒤에 공백을 둘 수 없습니다.");
            }

            if (value.Length > maximumLength)
            {
                errors.Add($"{path} 값은 {maximumLength}자를 초과할 수 없습니다.");
            }
        }

        private static void ValidateId(
            string value,
            string path,
            bool required,
            ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (required)
                {
                    errors.Add($"{path} 값이 비어 있습니다.");
                }

                return;
            }

            if (!IsValidId(value))
            {
                errors.Add($"{path} 값은 영문 소문자, 숫자, 점, 밑줄, 하이픈만 사용할 수 있습니다.");
            }
        }

        private static bool IsValidId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                var character = value[index];
                var valid = (character >= 'a' && character <= 'z')
                    || (character >= '0' && character <= '9')
                    || (index > 0 && (character == '.' || character == '_' || character == '-'));
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private static MilkroomMysteryCatalog BuildCatalog(
            MilkroomMysteryContentDocument document)
        {
            var definitions = new MilkroomMysteryChapterDefinition[document.chapters.Count];
            for (var chapterIndex = 0; chapterIndex < document.chapters.Count; chapterIndex += 1)
            {
                var source = document.chapters[chapterIndex];
                var choices = new MilkroomMysteryChoiceDefinition[source.choices.Count];
                for (var choiceIndex = 0; choiceIndex < source.choices.Count; choiceIndex += 1)
                {
                    var choice = source.choices[choiceIndex];
                    TryParseRewardKind(choice.reward.kind, out var rewardKind);
                    choices[choiceIndex] = new MilkroomMysteryChoiceDefinition(
                        choice.id,
                        choice.label,
                        choice.resultMessage,
                        new MilkroomMysteryRecordDefinition(
                            choice.memory.title,
                            choice.memory.detail),
                        new MilkroomMysteryRecordDefinition(
                            choice.codex.title,
                            choice.codex.detail),
                        new MilkroomMysteryRewardDefinition(
                            rewardKind,
                            choice.reward.id,
                            choice.reward.amount));
                }

                definitions[chapterIndex] = new MilkroomMysteryChapterDefinition(
                    source.id,
                    source.title,
                    source.description,
                    source.prerequisiteChapterId,
                    new MilkroomMysteryTriggerDefinition(
                        string.Equals(
                            source.trigger.kind,
                            "star_route",
                            StringComparison.Ordinal)
                            ? MilkroomMysteryTriggerKind.StarRoute
                            : MilkroomMysteryTriggerKind.LevelAtLeast,
                        source.trigger.minimumLevel),
                    choices);
            }

            return new MilkroomMysteryCatalog(definitions);
        }

        private static bool TryParseRewardKind(
            string value,
            out MilkroomMysteryRewardKind kind)
        {
            switch (value)
            {
                case "coin":
                    kind = MilkroomMysteryRewardKind.Coin;
                    return true;
                case "milk_drop":
                    kind = MilkroomMysteryRewardKind.MilkDrop;
                    return true;
                case "star_drop":
                    kind = MilkroomMysteryRewardKind.StarDrop;
                    return true;
                case "collection_fragment":
                    kind = MilkroomMysteryRewardKind.CollectionFragment;
                    return true;
                case "decoration":
                    kind = MilkroomMysteryRewardKind.Decoration;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }
    }
}
