using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTama.Gameplay.Story
{
    [Serializable]
    public sealed class DreamStorySeasonContentDocument
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string seasonId = string.Empty;
        public List<DreamStoryEpisodeContent> episodes =
            new List<DreamStoryEpisodeContent>();
    }

    [Serializable]
    public sealed class DreamStoryEpisodeContent
    {
        public string id = string.Empty;
        public string kind = string.Empty;
        public string title = string.Empty;
        public string body = string.Empty;
        public string prerequisiteEpisodeId = string.Empty;
        public DreamStoryTriggerContent trigger = new DreamStoryTriggerContent();
        public List<DreamStoryChoiceContent> choices = new List<DreamStoryChoiceContent>();
    }

    [Serializable]
    public sealed class DreamStoryTriggerContent
    {
        public string kind = string.Empty;
        public int minimumCount;
        public string requiredId = string.Empty;
    }

    [Serializable]
    public sealed class DreamStoryChoiceContent
    {
        public string id = string.Empty;
        public string label = string.Empty;
        public string resultMessage = string.Empty;
        public DreamStoryRecordContent memory = new DreamStoryRecordContent();
        public DreamStoryRecordContent codex = new DreamStoryRecordContent();
        public DreamStoryRewardContent reward = new DreamStoryRewardContent();
    }

    [Serializable]
    public sealed class DreamStoryRecordContent
    {
        public string title = string.Empty;
        public string detail = string.Empty;
    }

    [Serializable]
    public sealed class DreamStoryRewardContent
    {
        public string kind = "none";
        public int amount;
    }

    public enum DreamStoryEpisodeKind
    {
        Dream = 0,
        StainGuest = 1,
        Drawer = 2
    }

    public enum DreamStoryTriggerKind
    {
        ExternalStoryCompleted = 0,
        SleepCompletionsAtLeast = 1,
        NpcVisitsAtLeast = 2,
        Signal = 3,
        DreamEpisodeCompleted = 4
    }

    public enum DreamStoryRewardKind
    {
        None = 0,
        Coin = 1,
        MilkDrop = 2,
        CollectionFragment = 3
    }

    public readonly struct DreamStoryTriggerDefinition
    {
        internal DreamStoryTriggerDefinition(
            DreamStoryTriggerKind kind,
            int minimumCount,
            string requiredId)
        {
            Kind = kind;
            MinimumCount = Math.Max(0, minimumCount);
            RequiredId = requiredId ?? string.Empty;
        }

        public DreamStoryTriggerKind Kind { get; }
        public int MinimumCount { get; }
        public string RequiredId { get; }

        public bool IsSatisfied(DreamStoryProgress progress)
        {
            if (progress == null)
            {
                return false;
            }

            switch (Kind)
            {
                case DreamStoryTriggerKind.ExternalStoryCompleted:
                    return progress.HasExternalStoryCompletion(RequiredId);
                case DreamStoryTriggerKind.SleepCompletionsAtLeast:
                    return progress.CompletedSleepReceipts >= MinimumCount;
                case DreamStoryTriggerKind.NpcVisitsAtLeast:
                    return progress.CompletedNpcVisitReceipts >= MinimumCount;
                case DreamStoryTriggerKind.Signal:
                    return progress.HasSignal(RequiredId);
                case DreamStoryTriggerKind.DreamEpisodeCompleted:
                    return progress.HasDreamEpisodeCompletion(RequiredId);
                default:
                    return false;
            }
        }
    }

    public sealed class DreamStoryRecordDefinition
    {
        internal DreamStoryRecordDefinition(string title, string detail)
        {
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Title { get; }
        public string Detail { get; }
    }

    public readonly struct DreamStoryRewardDefinition
    {
        internal DreamStoryRewardDefinition(DreamStoryRewardKind kind, int amount)
        {
            Kind = kind;
            Amount = Math.Max(0, amount);
        }

        public DreamStoryRewardKind Kind { get; }
        public int Amount { get; }
    }

    public sealed class DreamStoryChoiceDefinition
    {
        internal DreamStoryChoiceDefinition(
            string id,
            string label,
            string resultMessage,
            DreamStoryRecordDefinition memory,
            DreamStoryRecordDefinition codex,
            DreamStoryRewardDefinition reward)
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
        public DreamStoryRecordDefinition Memory { get; }
        public DreamStoryRecordDefinition Codex { get; }
        public DreamStoryRewardDefinition Reward { get; }
    }

    public sealed class DreamStoryEpisodeDefinition
    {
        private readonly DreamStoryChoiceDefinition[] choices;

        internal DreamStoryEpisodeDefinition(
            string id,
            DreamStoryEpisodeKind kind,
            string title,
            string body,
            string prerequisiteEpisodeId,
            DreamStoryTriggerDefinition trigger,
            DreamStoryChoiceDefinition[] choices)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            PrerequisiteEpisodeId = prerequisiteEpisodeId ?? string.Empty;
            Trigger = trigger;
            this.choices = choices ?? Array.Empty<DreamStoryChoiceDefinition>();
        }

        public string Id { get; }
        public DreamStoryEpisodeKind Kind { get; }
        public string Title { get; }
        public string Body { get; }
        public string PrerequisiteEpisodeId { get; }
        public DreamStoryTriggerDefinition Trigger { get; }
        public IReadOnlyList<DreamStoryChoiceDefinition> Choices => choices;

        public DreamStoryChoiceDefinition FindChoice(string choiceId)
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

    public sealed class DreamStorySeasonCatalog
    {
        private readonly DreamStoryEpisodeDefinition[] episodes;

        internal DreamStorySeasonCatalog(
            string seasonId,
            DreamStoryEpisodeDefinition[] episodes)
        {
            SeasonId = seasonId ?? string.Empty;
            this.episodes = episodes ?? Array.Empty<DreamStoryEpisodeDefinition>();
        }

        public string SeasonId { get; }
        public IReadOnlyList<DreamStoryEpisodeDefinition> Episodes => episodes;
        public bool HasContent => episodes.Length > 0;

        public DreamStoryEpisodeDefinition Find(string episodeId)
        {
            var normalized = (episodeId ?? string.Empty).Trim();
            for (var index = 0; index < episodes.Length; index += 1)
            {
                if (string.Equals(episodes[index].Id, normalized, StringComparison.Ordinal))
                {
                    return episodes[index];
                }
            }

            return null;
        }

        public static DreamStorySeasonCatalog Empty { get; } =
            new DreamStorySeasonCatalog(
                string.Empty,
                Array.Empty<DreamStoryEpisodeDefinition>());
    }

    public static class DreamStorySeasonContentLoader
    {
        public const string DefaultResourcePath = "Content/DreamStorySeasonOne";
        public const string SeasonTwoResourcePath = "Content/DreamStorySeasonTwo";
        public const string CodexRecordPrefix = "dream_story_codex:";
        public const int ChoicesPerEpisode = 2;
        public const int MaximumEpisodes = 32;
        public const int MaximumJsonCharacters = 524288;
        public const int MaximumRewardAmount = 1000000;

        public static bool TryReloadDefault(
            out DreamStorySeasonCatalog catalog,
            out IReadOnlyList<string> errors)
        {
            var asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                catalog = DreamStorySeasonCatalog.Empty;
                errors = new[] { "꿈 이야기 시즌 콘텐츠 JSON을 찾을 수 없습니다." };
                return false;
            }

            return TryLoadFromJson(asset.text, out catalog, out errors);
        }

        public static bool TryReloadAll(
            out DreamStorySeasonCatalog catalog,
            out IReadOnlyList<string> errors)
        {
            var resourcePaths = new[] { DefaultResourcePath, SeasonTwoResourcePath };
            var catalogs = new List<DreamStorySeasonCatalog>(resourcePaths.Length);
            var combinedErrors = new List<string>();
            for (var index = 0; index < resourcePaths.Length; index += 1)
            {
                var asset = Resources.Load<TextAsset>(resourcePaths[index]);
                if (asset == null)
                {
                    combinedErrors.Add(
                        $"꿈 이야기 시즌 콘텐츠 JSON을 찾을 수 없습니다: {resourcePaths[index]}");
                    continue;
                }

                if (TryLoadFromJson(
                        asset.text,
                        allowExternalPrerequisites: true,
                        out var seasonCatalog,
                        out var seasonErrors))
                {
                    catalogs.Add(seasonCatalog);
                    continue;
                }

                for (var errorIndex = 0; errorIndex < seasonErrors.Count; errorIndex += 1)
                {
                    combinedErrors.Add(resourcePaths[index] + ": " + seasonErrors[errorIndex]);
                }
            }

            if (combinedErrors.Count > 0)
            {
                catalog = DreamStorySeasonCatalog.Empty;
                errors = combinedErrors;
                return false;
            }

            if (!TryMergeCatalogs(catalogs, out catalog, out var mergeErrors))
            {
                combinedErrors.AddRange(mergeErrors);
                catalog = DreamStorySeasonCatalog.Empty;
                errors = combinedErrors;
                return false;
            }

            errors = Array.Empty<string>();
            return true;
        }

        public static bool TryMergeCatalogs(
            IReadOnlyList<DreamStorySeasonCatalog> catalogs,
            out DreamStorySeasonCatalog merged,
            out IReadOnlyList<string> errors)
        {
            var validationErrors = new List<string>();
            var seasonIds = new HashSet<string>(StringComparer.Ordinal);
            var episodeIds = new HashSet<string>(StringComparer.Ordinal);
            var episodes = new List<DreamStoryEpisodeDefinition>();
            if (catalogs == null || catalogs.Count == 0)
            {
                validationErrors.Add("꿈 이야기 시즌 레지스트리가 비어 있습니다.");
            }
            else
            {
                for (var catalogIndex = 0; catalogIndex < catalogs.Count; catalogIndex += 1)
                {
                    var season = catalogs[catalogIndex];
                    if (season == null || !season.HasContent || !seasonIds.Add(season.SeasonId))
                    {
                        validationErrors.Add(
                            $"비어 있거나 중복된 꿈 이야기 시즌입니다: {season?.SeasonId ?? string.Empty}");
                        continue;
                    }

                    for (var episodeIndex = 0;
                        episodeIndex < season.Episodes.Count;
                        episodeIndex += 1)
                    {
                        var episode = season.Episodes[episodeIndex];
                        if (episode == null || !episodeIds.Add(episode.Id))
                        {
                            validationErrors.Add(
                                $"시즌 간 중복 꿈 이야기 에피소드 ID입니다: {episode?.Id ?? string.Empty}");
                            continue;
                        }

                        episodes.Add(episode);
                    }
                }
            }

            if (validationErrors.Count == 0)
            {
                for (var index = 0; index < episodes.Count; index += 1)
                {
                    var prerequisiteId = episodes[index].PrerequisiteEpisodeId;
                    if (!string.IsNullOrEmpty(prerequisiteId)
                        && !episodeIds.Contains(prerequisiteId))
                    {
                        validationErrors.Add(
                            $"통합 시즌에서 선행 에피소드를 찾을 수 없습니다: {prerequisiteId}");
                    }
                }
            }

            if (validationErrors.Count > 0)
            {
                merged = DreamStorySeasonCatalog.Empty;
                errors = validationErrors;
                return false;
            }

            merged = new DreamStorySeasonCatalog("dream_story_registry", episodes.ToArray());
            errors = Array.Empty<string>();
            return true;
        }

        public static string BuildCodexRecordId(string episodeId, string choiceId)
        {
            return CodexRecordPrefix
                + (episodeId ?? string.Empty).Trim()
                + ":"
                + (choiceId ?? string.Empty).Trim();
        }

        public static bool TryResolveCodexRecord(
            string recordId,
            out DreamStoryRecordDefinition record)
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

            if (!TryReloadAll(out var catalog, out _))
            {
                return false;
            }

            var episodeId = payload.Substring(0, separator);
            var choiceId = payload.Substring(separator + 1);
            record = catalog.Find(episodeId)?.FindChoice(choiceId)?.Codex;
            return record != null;
        }

        public static bool TryLoadFromJson(
            string json,
            out DreamStorySeasonCatalog catalog,
            out IReadOnlyList<string> errors)
        {
            return TryLoadFromJson(
                json,
                allowExternalPrerequisites: false,
                out catalog,
                out errors);
        }

        private static bool TryLoadFromJson(
            string json,
            bool allowExternalPrerequisites,
            out DreamStorySeasonCatalog catalog,
            out IReadOnlyList<string> errors)
        {
            catalog = DreamStorySeasonCatalog.Empty;
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                validationErrors.Add("꿈 이야기 시즌 콘텐츠 JSON이 비어 있습니다.");
                errors = validationErrors;
                return false;
            }

            if (json.Length > MaximumJsonCharacters)
            {
                validationErrors.Add(
                    $"꿈 이야기 시즌 콘텐츠 JSON은 {MaximumJsonCharacters}자를 초과할 수 없습니다.");
                errors = validationErrors;
                return false;
            }

            DreamStorySeasonContentDocument document;
            try
            {
                document = JsonUtility.FromJson<DreamStorySeasonContentDocument>(json);
            }
            catch (Exception exception)
            {
                validationErrors.Add($"꿈 이야기 시즌 콘텐츠 JSON 해석 실패: {exception.Message}");
                errors = validationErrors;
                return false;
            }

            validationErrors.AddRange(Validate(document, allowExternalPrerequisites));
            if (validationErrors.Count > 0)
            {
                errors = validationErrors;
                return false;
            }

            catalog = BuildCatalog(document);
            errors = Array.Empty<string>();
            return true;
        }

        public static IReadOnlyList<string> Validate(
            DreamStorySeasonContentDocument document)
        {
            return Validate(document, allowExternalPrerequisites: false);
        }

        private static IReadOnlyList<string> Validate(
            DreamStorySeasonContentDocument document,
            bool allowExternalPrerequisites)
        {
            var errors = new List<string>();
            if (document == null)
            {
                errors.Add("꿈 이야기 시즌 콘텐츠 문서가 없습니다.");
                return errors;
            }

            if (document.schemaVersion != DreamStorySeasonContentDocument.CurrentSchemaVersion)
            {
                errors.Add(
                    $"지원하지 않는 꿈 이야기 시즌 스키마입니다: {document.schemaVersion}");
            }

            ValidateId(document.seasonId, "seasonId", required: true, errors);
            if (document.episodes == null || document.episodes.Count == 0)
            {
                errors.Add("꿈 이야기 에피소드를 하나 이상 정의해야 합니다.");
                return errors;
            }

            if (document.episodes.Count > MaximumEpisodes)
            {
                errors.Add($"꿈 이야기 에피소드는 최대 {MaximumEpisodes}개까지 정의할 수 있습니다.");
            }

            var inspectedCount = Math.Min(document.episodes.Count, MaximumEpisodes);
            var episodeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < inspectedCount; index += 1)
            {
                var episode = document.episodes[index];
                var path = $"episodes[{index}]";
                if (episode == null)
                {
                    errors.Add($"{path} 항목이 없습니다.");
                    continue;
                }

                ValidateId(episode.id, $"{path}.id", required: true, errors);
                if (IsValidId(episode.id))
                {
                    if (episodeIndexes.ContainsKey(episode.id))
                    {
                        errors.Add($"중복된 꿈 이야기 에피소드 ID입니다: {episode.id}");
                    }
                    else
                    {
                        episodeIndexes.Add(episode.id, index);
                    }
                }

                if (!TryParseEpisodeKind(episode.kind, out _))
                {
                    errors.Add($"{path}.kind 값이 올바르지 않습니다: {episode.kind ?? string.Empty}");
                }

                ValidateText(episode.title, $"{path}.title", 80, errors);
                ValidateText(episode.body, $"{path}.body", 800, errors);
                ValidateId(
                    episode.prerequisiteEpisodeId,
                    $"{path}.prerequisiteEpisodeId",
                    required: false,
                    errors);
                ValidateTrigger(episode.trigger, $"{path}.trigger", errors);
                ValidateChoices(episode.choices, $"{path}.choices", errors);
            }

            for (var index = 0; index < inspectedCount; index += 1)
            {
                var episode = document.episodes[index];
                if (episode == null || string.IsNullOrEmpty(episode.prerequisiteEpisodeId))
                {
                    continue;
                }

                if (!episodeIndexes.TryGetValue(
                    episode.prerequisiteEpisodeId,
                    out var prerequisiteIndex))
                {
                    if (!allowExternalPrerequisites)
                    {
                        errors.Add(
                            $"{episode.id}의 선행 에피소드를 찾을 수 없습니다: "
                            + episode.prerequisiteEpisodeId);
                    }
                }
                else if (prerequisiteIndex >= index)
                {
                    errors.Add($"{episode.id}의 선행 에피소드는 현재 에피소드보다 앞에 있어야 합니다.");
                }
            }

            return errors;
        }

        private static void ValidateTrigger(
            DreamStoryTriggerContent trigger,
            string path,
            ICollection<string> errors)
        {
            if (trigger == null)
            {
                errors.Add($"{path} 항목이 없습니다.");
                return;
            }

            if (!TryParseTriggerKind(trigger.kind, out var kind))
            {
                errors.Add($"{path}.kind 값이 올바르지 않습니다: {trigger.kind ?? string.Empty}");
                return;
            }

            var countTrigger = kind == DreamStoryTriggerKind.SleepCompletionsAtLeast
                || kind == DreamStoryTriggerKind.NpcVisitsAtLeast;
            if (countTrigger)
            {
                if (trigger.minimumCount < 1 || trigger.minimumCount > 10000)
                {
                    errors.Add($"{path}.minimumCount는 1~10000 범위여야 합니다.");
                }

                if (!string.IsNullOrEmpty(trigger.requiredId))
                {
                    errors.Add($"{path}의 횟수 조건에는 requiredId를 지정할 수 없습니다.");
                }

                return;
            }

            if (trigger.minimumCount != 0)
            {
                errors.Add($"{path}의 ID 조건에는 minimumCount를 지정할 수 없습니다.");
            }

            ValidateId(trigger.requiredId, $"{path}.requiredId", required: true, errors);
        }

        private static void ValidateChoices(
            List<DreamStoryChoiceContent> choices,
            string path,
            ICollection<string> errors)
        {
            if (choices == null || choices.Count != ChoicesPerEpisode)
            {
                errors.Add($"{path}에는 선택지를 정확히 {ChoicesPerEpisode}개 정의해야 합니다.");
            }

            if (choices == null)
            {
                return;
            }

            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            var inspectedCount = Math.Min(choices.Count, ChoicesPerEpisode);
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
                ValidateText(choice.resultMessage, $"{choicePath}.resultMessage", 500, errors);
                ValidateRecord(choice.memory, $"{choicePath}.memory", 300, errors);
                ValidateRecord(choice.codex, $"{choicePath}.codex", 600, errors);
                ValidateReward(choice.reward, $"{choicePath}.reward", errors);
            }
        }

        private static void ValidateRecord(
            DreamStoryRecordContent record,
            string path,
            int maximumDetailLength,
            ICollection<string> errors)
        {
            if (record == null)
            {
                errors.Add($"{path} 항목이 없습니다.");
                return;
            }

            ValidateText(record.title, $"{path}.title", 80, errors);
            ValidateText(record.detail, $"{path}.detail", maximumDetailLength, errors);
        }

        private static void ValidateReward(
            DreamStoryRewardContent reward,
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

            if (kind == DreamStoryRewardKind.None)
            {
                if (reward.amount != 0)
                {
                    errors.Add($"{path}의 무보상 항목 수량은 0이어야 합니다.");
                }

                return;
            }

            if (reward.amount < 1 || reward.amount > MaximumRewardAmount)
            {
                errors.Add($"{path}.amount는 1~{MaximumRewardAmount} 범위여야 합니다.");
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

            if (value.Length > maximumLength)
            {
                errors.Add($"{path} 값은 {maximumLength}자를 초과할 수 없습니다.");
            }

            if (value.IndexOf('\0') >= 0)
            {
                errors.Add($"{path} 값에 허용되지 않는 문자가 있습니다.");
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
                errors.Add($"{path} 값은 소문자 영문, 숫자, _, -, .만 사용할 수 있습니다.");
            }
        }

        private static bool IsValidId(string value)
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

        private static bool TryParseEpisodeKind(
            string value,
            out DreamStoryEpisodeKind kind)
        {
            if (string.Equals(value, "dream", StringComparison.Ordinal))
            {
                kind = DreamStoryEpisodeKind.Dream;
                return true;
            }

            if (string.Equals(value, "stain_guest", StringComparison.Ordinal))
            {
                kind = DreamStoryEpisodeKind.StainGuest;
                return true;
            }

            if (string.Equals(value, "drawer", StringComparison.Ordinal))
            {
                kind = DreamStoryEpisodeKind.Drawer;
                return true;
            }

            kind = default;
            return false;
        }

        private static bool TryParseTriggerKind(
            string value,
            out DreamStoryTriggerKind kind)
        {
            if (string.Equals(value, "external_story_completed", StringComparison.Ordinal))
            {
                kind = DreamStoryTriggerKind.ExternalStoryCompleted;
                return true;
            }

            if (string.Equals(value, "sleep_completions_at_least", StringComparison.Ordinal))
            {
                kind = DreamStoryTriggerKind.SleepCompletionsAtLeast;
                return true;
            }

            if (string.Equals(value, "npc_visits_at_least", StringComparison.Ordinal))
            {
                kind = DreamStoryTriggerKind.NpcVisitsAtLeast;
                return true;
            }

            if (string.Equals(value, "signal", StringComparison.Ordinal))
            {
                kind = DreamStoryTriggerKind.Signal;
                return true;
            }

            if (string.Equals(value, "dream_episode_completed", StringComparison.Ordinal))
            {
                kind = DreamStoryTriggerKind.DreamEpisodeCompleted;
                return true;
            }

            kind = default;
            return false;
        }

        private static bool TryParseRewardKind(
            string value,
            out DreamStoryRewardKind kind)
        {
            if (string.Equals(value, "none", StringComparison.Ordinal))
            {
                kind = DreamStoryRewardKind.None;
                return true;
            }

            if (string.Equals(value, "coin", StringComparison.Ordinal))
            {
                kind = DreamStoryRewardKind.Coin;
                return true;
            }

            if (string.Equals(value, "milk_drop", StringComparison.Ordinal))
            {
                kind = DreamStoryRewardKind.MilkDrop;
                return true;
            }

            if (string.Equals(value, "collection_fragment", StringComparison.Ordinal))
            {
                kind = DreamStoryRewardKind.CollectionFragment;
                return true;
            }

            kind = default;
            return false;
        }

        private static DreamStorySeasonCatalog BuildCatalog(
            DreamStorySeasonContentDocument document)
        {
            var episodes = new DreamStoryEpisodeDefinition[document.episodes.Count];
            for (var episodeIndex = 0;
                episodeIndex < document.episodes.Count;
                episodeIndex += 1)
            {
                var source = document.episodes[episodeIndex];
                TryParseEpisodeKind(source.kind, out var episodeKind);
                TryParseTriggerKind(source.trigger.kind, out var triggerKind);
                var choices = new DreamStoryChoiceDefinition[source.choices.Count];
                for (var choiceIndex = 0;
                    choiceIndex < source.choices.Count;
                    choiceIndex += 1)
                {
                    var choice = source.choices[choiceIndex];
                    TryParseRewardKind(choice.reward.kind, out var rewardKind);
                    choices[choiceIndex] = new DreamStoryChoiceDefinition(
                        choice.id,
                        choice.label,
                        choice.resultMessage,
                        new DreamStoryRecordDefinition(
                            choice.memory.title,
                            choice.memory.detail),
                        new DreamStoryRecordDefinition(
                            choice.codex.title,
                            choice.codex.detail),
                        new DreamStoryRewardDefinition(
                            rewardKind,
                            choice.reward.amount));
                }

                episodes[episodeIndex] = new DreamStoryEpisodeDefinition(
                    source.id,
                    episodeKind,
                    source.title,
                    source.body,
                    source.prerequisiteEpisodeId,
                    new DreamStoryTriggerDefinition(
                        triggerKind,
                        source.trigger.minimumCount,
                        source.trigger.requiredId),
                    choices);
            }

            return new DreamStorySeasonCatalog(document.seasonId, episodes);
        }
    }
}
