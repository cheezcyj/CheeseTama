using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Gameplay.Story
{
    [Serializable]
    public sealed class MilkroomInvestigationContentDocument
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string episodeId = string.Empty;
        public string requiredDreamEpisodeId = string.Empty;
        public string title = string.Empty;
        public string body = string.Empty;
        public List<MilkroomInvestigationHotspotContent> hotspots =
            new List<MilkroomInvestigationHotspotContent>();
        public List<MilkroomInvestigationChoiceContent> choices =
            new List<MilkroomInvestigationChoiceContent>();
    }

    [Serializable]
    public sealed class MilkroomInvestigationHotspotContent
    {
        public string id = string.Empty;
        public string kind = string.Empty;
        public string label = string.Empty;
        public string clueId = string.Empty;
        public string clueTitle = string.Empty;
        public string clueDetail = string.Empty;
        public MilkroomInvestigationRequirementContent requirement =
            new MilkroomInvestigationRequirementContent();
    }

    [Serializable]
    public sealed class MilkroomInvestigationRequirementContent
    {
        public string kind = "always";
        public float minimumZoom = 1f;
        public int startHour;
        public int endHour;
        public string hint = string.Empty;
    }

    [Serializable]
    public sealed class MilkroomInvestigationChoiceContent
    {
        public string id = string.Empty;
        public string label = string.Empty;
        public string resultMessage = string.Empty;
        public MilkroomInvestigationRecordContent memory =
            new MilkroomInvestigationRecordContent();
        public MilkroomInvestigationRecordContent codex =
            new MilkroomInvestigationRecordContent();
        public MilkroomInvestigationRewardContent reward =
            new MilkroomInvestigationRewardContent();
    }

    [Serializable]
    public sealed class MilkroomInvestigationRecordContent
    {
        public string title = string.Empty;
        public string detail = string.Empty;
    }

    [Serializable]
    public sealed class MilkroomInvestigationRewardContent
    {
        public string kind = "none";
        public int amount;
    }

    public enum MilkroomInvestigationHotspotKind
    {
        Window = 0,
        Shelf = 1,
        Drawer = 2
    }

    public enum MilkroomInvestigationRequirementKind
    {
        Always = 0,
        ZoomOrTime = 1
    }

    public enum MilkroomInvestigationRewardKind
    {
        None = 0,
        Coin = 1,
        MilkDrop = 2,
        CollectionFragment = 3
    }

    public readonly struct MilkroomInvestigationContext
    {
        public MilkroomInvestigationContext(float zoomScale, int localHour)
        {
            ZoomScale = zoomScale;
            LocalHour = localHour;
        }

        public float ZoomScale { get; }
        public int LocalHour { get; }
        public bool IsValid => !float.IsNaN(ZoomScale)
            && !float.IsInfinity(ZoomScale)
            && ZoomScale > 0f
            && LocalHour >= 0
            && LocalHour <= 23;
    }

    public readonly struct MilkroomInvestigationRequirementDefinition
    {
        internal MilkroomInvestigationRequirementDefinition(
            MilkroomInvestigationRequirementKind kind,
            float minimumZoom,
            int startHour,
            int endHour,
            string hint)
        {
            Kind = kind;
            MinimumZoom = Math.Max(0.01f, minimumZoom);
            StartHour = startHour;
            EndHour = endHour;
            Hint = hint ?? string.Empty;
        }

        public MilkroomInvestigationRequirementKind Kind { get; }
        public float MinimumZoom { get; }
        public int StartHour { get; }
        public int EndHour { get; }
        public string Hint { get; }

        public bool IsSatisfied(MilkroomInvestigationContext context)
        {
            if (!context.IsValid)
            {
                return false;
            }

            if (Kind == MilkroomInvestigationRequirementKind.Always)
            {
                return true;
            }

            var withinTime = StartHour < EndHour
                ? context.LocalHour >= StartHour && context.LocalHour < EndHour
                : context.LocalHour >= StartHour || context.LocalHour < EndHour;
            return context.ZoomScale >= MinimumZoom || withinTime;
        }
    }

    public sealed class MilkroomInvestigationHotspotDefinition
    {
        internal MilkroomInvestigationHotspotDefinition(
            string id,
            MilkroomInvestigationHotspotKind kind,
            string label,
            string clueId,
            string clueTitle,
            string clueDetail,
            MilkroomInvestigationRequirementDefinition requirement)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Label = label ?? string.Empty;
            ClueId = clueId ?? string.Empty;
            ClueTitle = clueTitle ?? string.Empty;
            ClueDetail = clueDetail ?? string.Empty;
            Requirement = requirement;
        }

        public string Id { get; }
        public MilkroomInvestigationHotspotKind Kind { get; }
        public string Label { get; }
        public string ClueId { get; }
        public string ClueTitle { get; }
        public string ClueDetail { get; }
        public MilkroomInvestigationRequirementDefinition Requirement { get; }
    }

    public sealed class MilkroomInvestigationRecordDefinition
    {
        internal MilkroomInvestigationRecordDefinition(string title, string detail)
        {
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Title { get; }
        public string Detail { get; }
    }

    public readonly struct MilkroomInvestigationRewardDefinition
    {
        internal MilkroomInvestigationRewardDefinition(
            MilkroomInvestigationRewardKind kind,
            int amount)
        {
            Kind = kind;
            Amount = Math.Max(0, amount);
        }

        public MilkroomInvestigationRewardKind Kind { get; }
        public int Amount { get; }
    }

    public sealed class MilkroomInvestigationChoiceDefinition
    {
        internal MilkroomInvestigationChoiceDefinition(
            string id,
            string label,
            string resultMessage,
            MilkroomInvestigationRecordDefinition memory,
            MilkroomInvestigationRecordDefinition codex,
            MilkroomInvestigationRewardDefinition reward)
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
        public MilkroomInvestigationRecordDefinition Memory { get; }
        public MilkroomInvestigationRecordDefinition Codex { get; }
        public MilkroomInvestigationRewardDefinition Reward { get; }
    }

    public sealed class MilkroomInvestigationEpisodeDefinition
    {
        private readonly MilkroomInvestigationHotspotDefinition[] hotspots;
        private readonly MilkroomInvestigationChoiceDefinition[] choices;

        internal MilkroomInvestigationEpisodeDefinition(
            string id,
            string requiredDreamEpisodeId,
            string title,
            string body,
            MilkroomInvestigationHotspotDefinition[] hotspots,
            MilkroomInvestigationChoiceDefinition[] choices)
        {
            Id = id ?? string.Empty;
            RequiredDreamEpisodeId = requiredDreamEpisodeId ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            this.hotspots = hotspots ?? Array.Empty<MilkroomInvestigationHotspotDefinition>();
            this.choices = choices ?? Array.Empty<MilkroomInvestigationChoiceDefinition>();
        }

        public string Id { get; }
        public string RequiredDreamEpisodeId { get; }
        public string Title { get; }
        public string Body { get; }
        public IReadOnlyList<MilkroomInvestigationHotspotDefinition> Hotspots => hotspots;
        public IReadOnlyList<MilkroomInvestigationChoiceDefinition> Choices => choices;

        public MilkroomInvestigationHotspotDefinition FindHotspot(string hotspotId)
        {
            var normalized = (hotspotId ?? string.Empty).Trim();
            for (var index = 0; index < hotspots.Length; index += 1)
            {
                if (string.Equals(hotspots[index].Id, normalized, StringComparison.Ordinal))
                {
                    return hotspots[index];
                }
            }

            return null;
        }

        public MilkroomInvestigationChoiceDefinition FindChoice(string choiceId)
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

        public bool KnowsClue(string clueId)
        {
            var normalized = (clueId ?? string.Empty).Trim();
            for (var index = 0; index < hotspots.Length; index += 1)
            {
                if (string.Equals(hotspots[index].ClueId, normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class MilkroomInvestigationCatalog
    {
        internal MilkroomInvestigationCatalog(MilkroomInvestigationEpisodeDefinition episode)
        {
            Episode = episode;
        }

        public MilkroomInvestigationEpisodeDefinition Episode { get; }
        public bool HasContent => Episode != null;
        public static MilkroomInvestigationCatalog Empty { get; } =
            new MilkroomInvestigationCatalog(null);
    }

    public static class MilkroomInvestigationContentLoader
    {
        public const string DefaultResourcePath = "Content/MilkroomInvestigationPrologue";
        public const int HotspotCount = 3;
        public const int ChoiceCount = 2;
        public const int MaximumJsonCharacters = 131072;
        public const int MaximumRewardAmount = 1000000;
        public const string CodexRecordPrefix = "milkroom_investigation_codex:";

        public static bool TryReloadDefault(
            out MilkroomInvestigationCatalog catalog,
            out IReadOnlyList<string> errors)
        {
            var asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                catalog = MilkroomInvestigationCatalog.Empty;
                errors = new[] { "밀크룸 조사 콘텐츠 JSON을 찾을 수 없습니다." };
                return false;
            }

            return TryLoadFromJson(asset.text, out catalog, out errors);
        }

        public static bool TryLoadFromJson(
            string json,
            out MilkroomInvestigationCatalog catalog,
            out IReadOnlyList<string> errors)
        {
            catalog = MilkroomInvestigationCatalog.Empty;
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                validationErrors.Add("밀크룸 조사 콘텐츠 JSON이 비어 있습니다.");
                errors = validationErrors;
                return false;
            }

            if (json.Length > MaximumJsonCharacters)
            {
                validationErrors.Add(
                    $"밀크룸 조사 콘텐츠 JSON은 {MaximumJsonCharacters}자를 초과할 수 없습니다.");
                errors = validationErrors;
                return false;
            }

            MilkroomInvestigationContentDocument document;
            try
            {
                document = JsonUtility.FromJson<MilkroomInvestigationContentDocument>(json);
            }
            catch (Exception exception)
            {
                validationErrors.Add($"밀크룸 조사 콘텐츠 JSON 해석 실패: {exception.Message}");
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

        public static IReadOnlyList<string> Validate(
            MilkroomInvestigationContentDocument document)
        {
            var errors = new List<string>();
            if (document == null)
            {
                errors.Add("밀크룸 조사 콘텐츠 문서가 없습니다.");
                return errors;
            }

            if (document.schemaVersion != MilkroomInvestigationContentDocument.CurrentSchemaVersion)
            {
                errors.Add($"지원하지 않는 밀크룸 조사 스키마입니다: {document.schemaVersion}");
            }

            ValidateId(document.episodeId, "episodeId", errors);
            if (!string.Equals(
                    document.requiredDreamEpisodeId,
                    MilkroomInvestigationSystem.SeasonTwoFinalEpisodeId,
                    StringComparison.Ordinal))
            {
                errors.Add("조사 프롤로그는 꿈 시즌 2 최종편만 선행 조건으로 사용할 수 있습니다.");
            }

            ValidateText(document.title, "title", errors);
            ValidateText(document.body, "body", errors);
            ValidateHotspots(document.hotspots, errors);
            ValidateChoices(document.choices, errors);
            return errors;
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
            out MilkroomInvestigationRecordDefinition record)
        {
            record = null;
            var normalized = (recordId ?? string.Empty).Trim();
            if (!normalized.StartsWith(CodexRecordPrefix, StringComparison.Ordinal)
                || !TryReloadDefault(out var catalog, out _)
                || !catalog.HasContent)
            {
                return false;
            }

            var payload = normalized.Substring(CodexRecordPrefix.Length);
            var separator = payload.IndexOf(':');
            if (separator <= 0 || separator >= payload.Length - 1)
            {
                return false;
            }

            var episodeId = payload.Substring(0, separator);
            var choiceId = payload.Substring(separator + 1);
            if (!string.Equals(catalog.Episode.Id, episodeId, StringComparison.Ordinal))
            {
                return false;
            }

            var choice = catalog.Episode.FindChoice(choiceId);
            record = choice?.Codex;
            return record != null;
        }

        private static void ValidateHotspots(
            List<MilkroomInvestigationHotspotContent> hotspots,
            ICollection<string> errors)
        {
            if (hotspots == null || hotspots.Count != HotspotCount)
            {
                errors.Add($"밀크룸 조사 hotspot은 정확히 {HotspotCount}개여야 합니다.");
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var clueIds = new HashSet<string>(StringComparer.Ordinal);
            var kinds = new HashSet<MilkroomInvestigationHotspotKind>();
            var conditionalCount = 0;
            for (var index = 0; index < hotspots.Count; index += 1)
            {
                var hotspot = hotspots[index];
                var path = $"hotspots[{index}]";
                if (hotspot == null)
                {
                    errors.Add($"{path} 항목이 없습니다.");
                    continue;
                }

                ValidateId(hotspot.id, $"{path}.id", errors);
                ValidateId(hotspot.clueId, $"{path}.clueId", errors);
                if (IsValidId(hotspot.id) && !ids.Add(hotspot.id))
                {
                    errors.Add($"중복 hotspot ID입니다: {hotspot.id}");
                }

                if (IsValidId(hotspot.clueId) && !clueIds.Add(hotspot.clueId))
                {
                    errors.Add($"중복 clue ID입니다: {hotspot.clueId}");
                }

                if (!TryParseHotspotKind(hotspot.kind, out var hotspotKind)
                    || !kinds.Add(hotspotKind))
                {
                    errors.Add($"중복되거나 잘못된 hotspot kind입니다: {hotspot.kind ?? string.Empty}");
                }

                ValidateText(hotspot.label, $"{path}.label", errors);
                ValidateText(hotspot.clueTitle, $"{path}.clueTitle", errors);
                ValidateText(hotspot.clueDetail, $"{path}.clueDetail", errors);
                var requirement = hotspot.requirement;
                if (requirement == null
                    || !TryParseRequirementKind(requirement.kind, out var requirementKind))
                {
                    errors.Add($"{path}.requirement.kind 값이 올바르지 않습니다.");
                    continue;
                }

                if (requirementKind == MilkroomInvestigationRequirementKind.ZoomOrTime)
                {
                    conditionalCount += 1;
                    if (requirement.minimumZoom < 1f || requirement.minimumZoom > 4f)
                    {
                        errors.Add($"{path}.requirement.minimumZoom 범위가 올바르지 않습니다.");
                    }

                    if (requirement.startHour < 0
                        || requirement.startHour > 23
                        || requirement.endHour < 0
                        || requirement.endHour > 23
                        || requirement.startHour == requirement.endHour)
                    {
                        errors.Add($"{path}.requirement 시간 범위가 올바르지 않습니다.");
                    }

                    ValidateText(requirement.hint, $"{path}.requirement.hint", errors);
                }
            }

            if (kinds.Count != HotspotCount)
            {
                errors.Add("창문, 선반, 서랍 hotspot을 각각 하나씩 정의해야 합니다.");
            }

            if (conditionalCount != 1)
            {
                errors.Add("확대 또는 시간대 조건 hotspot을 정확히 하나 정의해야 합니다.");
            }
        }

        private static void ValidateChoices(
            List<MilkroomInvestigationChoiceContent> choices,
            ICollection<string> errors)
        {
            if (choices == null || choices.Count != ChoiceCount)
            {
                errors.Add($"시즌 3 프롤로그 선택지는 정확히 {ChoiceCount}개여야 합니다.");
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < choices.Count; index += 1)
            {
                var choice = choices[index];
                var path = $"choices[{index}]";
                if (choice == null)
                {
                    errors.Add($"{path} 항목이 없습니다.");
                    continue;
                }

                ValidateId(choice.id, $"{path}.id", errors);
                if (IsValidId(choice.id) && !ids.Add(choice.id))
                {
                    errors.Add($"중복 선택지 ID입니다: {choice.id}");
                }

                ValidateText(choice.label, $"{path}.label", errors);
                ValidateText(choice.resultMessage, $"{path}.resultMessage", errors);
                ValidateRecord(choice.memory, $"{path}.memory", errors);
                ValidateRecord(choice.codex, $"{path}.codex", errors);
                if (choice.reward == null
                    || !TryParseRewardKind(choice.reward.kind, out var rewardKind))
                {
                    errors.Add($"{path}.reward.kind 값이 올바르지 않습니다.");
                    continue;
                }

                var amountValid = rewardKind == MilkroomInvestigationRewardKind.None
                    ? choice.reward.amount == 0
                    : choice.reward.amount >= 1
                        && choice.reward.amount <= MaximumRewardAmount;
                if (!amountValid)
                {
                    errors.Add($"{path}.reward.amount 값이 올바르지 않습니다.");
                }
            }
        }

        private static void ValidateRecord(
            MilkroomInvestigationRecordContent record,
            string path,
            ICollection<string> errors)
        {
            if (record == null)
            {
                errors.Add($"{path} 항목이 없습니다.");
                return;
            }

            ValidateText(record.title, $"{path}.title", errors);
            ValidateText(record.detail, $"{path}.detail", errors);
        }

        private static void ValidateId(string value, string path, ICollection<string> errors)
        {
            if (!IsValidId(value))
            {
                errors.Add($"{path} ID가 올바르지 않습니다: {value ?? string.Empty}");
            }
        }

        private static bool IsValidId(string value)
        {
            return MilkroomInvestigationSaveData.IsStableToken(value);
        }

        private static void ValidateText(
            string value,
            string path,
            ICollection<string> errors)
        {
            var length = value?.Trim().Length ?? 0;
            if (length == 0 || length > 4000)
            {
                errors.Add($"{path} 텍스트 길이가 올바르지 않습니다.");
            }
        }

        private static bool TryParseHotspotKind(
            string value,
            out MilkroomInvestigationHotspotKind kind)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "window":
                    kind = MilkroomInvestigationHotspotKind.Window;
                    return true;
                case "shelf":
                    kind = MilkroomInvestigationHotspotKind.Shelf;
                    return true;
                case "drawer":
                    kind = MilkroomInvestigationHotspotKind.Drawer;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static bool TryParseRequirementKind(
            string value,
            out MilkroomInvestigationRequirementKind kind)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "always":
                    kind = MilkroomInvestigationRequirementKind.Always;
                    return true;
                case "zoom_or_time":
                    kind = MilkroomInvestigationRequirementKind.ZoomOrTime;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static bool TryParseRewardKind(
            string value,
            out MilkroomInvestigationRewardKind kind)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "none":
                    kind = MilkroomInvestigationRewardKind.None;
                    return true;
                case "coin":
                    kind = MilkroomInvestigationRewardKind.Coin;
                    return true;
                case "milk_drop":
                    kind = MilkroomInvestigationRewardKind.MilkDrop;
                    return true;
                case "collection_fragment":
                    kind = MilkroomInvestigationRewardKind.CollectionFragment;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static MilkroomInvestigationCatalog BuildCatalog(
            MilkroomInvestigationContentDocument document)
        {
            var hotspots = new MilkroomInvestigationHotspotDefinition[document.hotspots.Count];
            for (var index = 0; index < hotspots.Length; index += 1)
            {
                var source = document.hotspots[index];
                TryParseHotspotKind(source.kind, out var hotspotKind);
                TryParseRequirementKind(source.requirement.kind, out var requirementKind);
                hotspots[index] = new MilkroomInvestigationHotspotDefinition(
                    source.id,
                    hotspotKind,
                    source.label,
                    source.clueId,
                    source.clueTitle,
                    source.clueDetail,
                    new MilkroomInvestigationRequirementDefinition(
                        requirementKind,
                        source.requirement.minimumZoom,
                        source.requirement.startHour,
                        source.requirement.endHour,
                        source.requirement.hint));
            }

            var choices = new MilkroomInvestigationChoiceDefinition[document.choices.Count];
            for (var index = 0; index < choices.Length; index += 1)
            {
                var source = document.choices[index];
                TryParseRewardKind(source.reward.kind, out var rewardKind);
                choices[index] = new MilkroomInvestigationChoiceDefinition(
                    source.id,
                    source.label,
                    source.resultMessage,
                    new MilkroomInvestigationRecordDefinition(
                        source.memory.title,
                        source.memory.detail),
                    new MilkroomInvestigationRecordDefinition(
                        source.codex.title,
                        source.codex.detail),
                    new MilkroomInvestigationRewardDefinition(
                        rewardKind,
                        source.reward.amount));
            }

            return new MilkroomInvestigationCatalog(
                new MilkroomInvestigationEpisodeDefinition(
                    document.episodeId,
                    document.requiredDreamEpisodeId,
                    document.title,
                    document.body,
                    hotspots,
                    choices));
        }
    }

    public sealed class MilkroomInvestigationHotspotView
    {
        internal MilkroomInvestigationHotspotView(
            string id,
            MilkroomInvestigationHotspotKind kind,
            string label,
            bool discovered,
            bool available,
            string requirementHint,
            string clueId,
            string clueTitle,
            string clueDetail)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Label = label ?? string.Empty;
            Discovered = discovered;
            Available = available;
            RequirementHint = requirementHint ?? string.Empty;
            ClueId = discovered ? clueId ?? string.Empty : string.Empty;
            ClueTitle = discovered ? clueTitle ?? string.Empty : string.Empty;
            ClueDetail = discovered ? clueDetail ?? string.Empty : string.Empty;
        }

        public string Id { get; }
        public MilkroomInvestigationHotspotKind Kind { get; }
        public string Label { get; }
        public bool Discovered { get; }
        public bool Available { get; }
        public string RequirementHint { get; }
        public string ClueId { get; }
        public string ClueTitle { get; }
        public string ClueDetail { get; }
    }

    public sealed class MilkroomInvestigationChoiceView
    {
        internal MilkroomInvestigationChoiceView(string id, string label)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
    }

    public sealed class MilkroomInvestigationSnapshot
    {
        private readonly MilkroomInvestigationHotspotView[] hotspots;
        private readonly MilkroomInvestigationChoiceView[] choices;

        private MilkroomInvestigationSnapshot(
            bool visible,
            bool isRecall,
            string episodeId,
            string title,
            string body,
            string resultMessage,
            int discoveredClueCount,
            int requiredClueCount,
            MilkroomInvestigationHotspotView[] hotspots,
            MilkroomInvestigationChoiceView[] choices)
        {
            Visible = visible;
            IsRecall = visible && isRecall;
            EpisodeId = visible ? episodeId ?? string.Empty : string.Empty;
            Title = visible ? title ?? string.Empty : string.Empty;
            Body = visible ? body ?? string.Empty : string.Empty;
            ResultMessage = visible ? resultMessage ?? string.Empty : string.Empty;
            DiscoveredClueCount = visible ? Math.Max(0, discoveredClueCount) : 0;
            RequiredClueCount = visible ? Math.Max(0, requiredClueCount) : 0;
            this.hotspots = visible
                ? hotspots ?? Array.Empty<MilkroomInvestigationHotspotView>()
                : Array.Empty<MilkroomInvestigationHotspotView>();
            this.choices = visible
                ? choices ?? Array.Empty<MilkroomInvestigationChoiceView>()
                : Array.Empty<MilkroomInvestigationChoiceView>();
        }

        public bool Visible { get; }
        public bool IsRecall { get; }
        public string EpisodeId { get; }
        public string Title { get; }
        public string Body { get; }
        public string ResultMessage { get; }
        public int DiscoveredClueCount { get; }
        public int RequiredClueCount { get; }
        public IReadOnlyList<MilkroomInvestigationHotspotView> Hotspots => hotspots;
        public IReadOnlyList<MilkroomInvestigationChoiceView> Choices => choices;
        public bool CanChoose => Visible
            && !IsRecall
            && RequiredClueCount > 0
            && DiscoveredClueCount == RequiredClueCount
            && choices.Length == MilkroomInvestigationContentLoader.ChoiceCount;

        internal static MilkroomInvestigationSnapshot Hidden { get; } =
            new MilkroomInvestigationSnapshot(
                false,
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0,
                Array.Empty<MilkroomInvestigationHotspotView>(),
                Array.Empty<MilkroomInvestigationChoiceView>());

        internal static MilkroomInvestigationSnapshot CreateActive(
            MilkroomInvestigationEpisodeDefinition episode,
            int discoveredClueCount,
            MilkroomInvestigationHotspotView[] hotspots)
        {
            var allCluesFound = discoveredClueCount == episode.Hotspots.Count;
            var choiceViews = allCluesFound
                ? new[]
                {
                    new MilkroomInvestigationChoiceView(
                        episode.Choices[0].Id,
                        episode.Choices[0].Label),
                    new MilkroomInvestigationChoiceView(
                        episode.Choices[1].Id,
                        episode.Choices[1].Label)
                }
                : Array.Empty<MilkroomInvestigationChoiceView>();
            return new MilkroomInvestigationSnapshot(
                true,
                false,
                episode.Id,
                episode.Title,
                episode.Body,
                string.Empty,
                discoveredClueCount,
                episode.Hotspots.Count,
                hotspots,
                choiceViews);
        }

        internal static MilkroomInvestigationSnapshot CreateRecall(
            MilkroomInvestigationEpisodeDefinition episode,
            MilkroomInvestigationChoiceDefinition choice)
        {
            if (episode == null || choice == null)
            {
                return Hidden;
            }

            return new MilkroomInvestigationSnapshot(
                true,
                true,
                episode.Id,
                episode.Title,
                episode.Body,
                choice.ResultMessage,
                episode.Hotspots.Count,
                episode.Hotspots.Count,
                Array.Empty<MilkroomInvestigationHotspotView>(),
                Array.Empty<MilkroomInvestigationChoiceView>());
        }
    }

    public enum MilkroomInvestigationInspectStatus
    {
        Discovered = 0,
        AlreadyDiscovered = 1,
        MissingState = 2,
        ContentUnavailable = 3,
        Locked = 4,
        InvalidContext = 5,
        UnknownHotspot = 6,
        ConditionNotMet = 7,
        StateCapacityFull = 8,
        PersistenceRejected = 9
    }

    public sealed class MilkroomInvestigationInspectResult
    {
        internal MilkroomInvestigationInspectResult(
            MilkroomInvestigationInspectStatus status,
            bool applied = false,
            string hotspotId = null,
            string clueId = null,
            string clueTitle = null,
            string clueDetail = null,
            string userMessage = null,
            MilkroomInvestigationSnapshot snapshot = null)
        {
            Status = status;
            Applied = applied;
            HotspotId = hotspotId ?? string.Empty;
            ClueId = clueId ?? string.Empty;
            ClueTitle = clueTitle ?? string.Empty;
            ClueDetail = clueDetail ?? string.Empty;
            UserMessage = userMessage ?? string.Empty;
            Snapshot = snapshot ?? MilkroomInvestigationSnapshot.Hidden;
        }

        public MilkroomInvestigationInspectStatus Status { get; }
        public bool Applied { get; }
        public string HotspotId { get; }
        public string ClueId { get; }
        public string ClueTitle { get; }
        public string ClueDetail { get; }
        public string UserMessage { get; }
        public MilkroomInvestigationSnapshot Snapshot { get; }
    }

    public sealed class MilkroomInvestigationRecordPayload
    {
        internal MilkroomInvestigationRecordPayload(
            string recordId,
            string sourceId,
            string detailId,
            string title,
            string detail)
        {
            RecordId = recordId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            DetailId = detailId ?? string.Empty;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string RecordId { get; }
        public string SourceId { get; }
        public string DetailId { get; }
        public string Title { get; }
        public string Detail { get; }
    }

    public sealed class MilkroomInvestigationRewardPayload
    {
        internal MilkroomInvestigationRewardPayload(
            string receiptId,
            MilkroomInvestigationRewardKind kind,
            int amount)
        {
            ReceiptId = receiptId ?? string.Empty;
            Kind = kind;
            Amount = Math.Max(0, amount);
        }

        public string ReceiptId { get; }
        public MilkroomInvestigationRewardKind Kind { get; }
        public int Amount { get; }
        public bool HasReward => Kind != MilkroomInvestigationRewardKind.None && Amount > 0;
    }

    public sealed class MilkroomInvestigationChoicePayloadBundle
    {
        internal MilkroomInvestigationChoicePayloadBundle(
            string episodeId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt,
            string userMessage,
            MilkroomInvestigationRecordPayload memory,
            MilkroomInvestigationRecordPayload codex,
            MilkroomInvestigationRewardPayload reward)
        {
            EpisodeId = episodeId ?? string.Empty;
            ChoiceId = choiceId ?? string.Empty;
            ReceiptId = receiptId ?? string.Empty;
            CompletedAt = completedAt;
            UserMessage = userMessage ?? string.Empty;
            Memory = memory;
            Codex = codex;
            Reward = reward;
        }

        public string EpisodeId { get; }
        public string ChoiceId { get; }
        public string ReceiptId { get; }
        public DateTimeOffset CompletedAt { get; }
        public string UserMessage { get; }
        public MilkroomInvestigationRecordPayload Memory { get; }
        public MilkroomInvestigationRecordPayload Codex { get; }
        public MilkroomInvestigationRewardPayload Reward { get; }
    }

    public enum MilkroomInvestigationChoiceStatus
    {
        Applied = 0,
        MissingState = 1,
        ContentUnavailable = 2,
        Locked = 3,
        CluesIncomplete = 4,
        UnknownEpisode = 5,
        UnknownChoice = 6,
        InvalidReceipt = 7,
        DuplicateReceipt = 8,
        InvalidCompletionTime = 9,
        AlreadyCompleted = 10,
        StateCapacityFull = 11,
        MissingPayloadCommitter = 12,
        PayloadRejected = 13,
        PersistenceRejected = 14
    }

    public sealed class MilkroomInvestigationChoiceResult
    {
        internal MilkroomInvestigationChoiceResult(
            MilkroomInvestigationChoiceStatus status,
            MilkroomInvestigationChoicePayloadBundle payloads = null)
        {
            Status = status;
            Payloads = payloads;
        }

        public MilkroomInvestigationChoiceStatus Status { get; }
        public bool Applied => Status == MilkroomInvestigationChoiceStatus.Applied;
        public MilkroomInvestigationChoicePayloadBundle Payloads { get; }
        public string EpisodeId => Payloads?.EpisodeId ?? string.Empty;
        public string ChoiceId => Payloads?.ChoiceId ?? string.Empty;
        public string ReceiptId => Payloads?.ReceiptId ?? string.Empty;
        public string UserMessage => Payloads?.UserMessage ?? string.Empty;
        public MilkroomInvestigationRecordPayload Memory => Payloads?.Memory;
        public MilkroomInvestigationRecordPayload Codex => Payloads?.Codex;
        public MilkroomInvestigationRewardPayload Reward => Payloads?.Reward;
    }

    /// <summary>
    /// Owns spoiler-safe investigation visibility, stable clue discovery, and the one-time season
    /// three prologue receipt. The caller applies memory, codex, and reward payloads to its existing
    /// authoritative stores as a single transaction.
    /// </summary>
    public sealed class MilkroomInvestigationSystem
    {
        public const string SeasonTwoFinalEpisodeId = "season2_final_01_open_page";
        public const string SeasonTwoFinalChoiceLeaveOpen = "leave_page_open";
        public const string SeasonTwoFinalChoiceThreadMark = "mark_page_with_thread";
        public const string MemoryRecordPrefix = "milkroom_investigation_memory:";

        private readonly MilkroomInvestigationCatalog catalog;

        public MilkroomInvestigationSystem()
        {
            if (!MilkroomInvestigationContentLoader.TryReloadDefault(
                    out var loadedCatalog,
                    out _))
            {
                loadedCatalog = MilkroomInvestigationCatalog.Empty;
            }

            catalog = loadedCatalog;
        }

        public MilkroomInvestigationSystem(MilkroomInvestigationCatalog catalog)
        {
            this.catalog = catalog ?? MilkroomInvestigationCatalog.Empty;
        }

        public bool ContentAvailable => catalog.HasContent;

        public bool NormalizeState(MilkroomInvestigationSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            if (!catalog.HasContent)
            {
                return state.EnsureRuntimeDefaults();
            }

            var changed = false;
            if (state.discoveredClueIds != null)
            {
                var knownClues = new List<string>();
                var unknownClues = new List<string>();
                for (var index = 0; index < state.discoveredClueIds.Count; index += 1)
                {
                    var clueId = state.discoveredClueIds[index];
                    if (catalog.Episode.KnowsClue(clueId))
                    {
                        knownClues.Add(clueId);
                    }
                    else
                    {
                        unknownClues.Add(clueId);
                    }
                }

                knownClues.AddRange(unknownClues);
                changed |= !HasSameValues(state.discoveredClueIds, knownClues);
                state.discoveredClueIds = knownClues;
            }

            if (state.completions != null)
            {
                var unknownCompletions = new List<MilkroomInvestigationCompletionSaveData>();
                MilkroomInvestigationCompletionSaveData knownCompletion = null;
                for (var index = state.completions.Count - 1; index >= 0; index -= 1)
                {
                    var completion = state.completions[index];
                    if (completion == null)
                    {
                        changed = true;
                        continue;
                    }

                    if (!string.Equals(
                            completion.episodeId,
                            catalog.Episode.Id,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (knownCompletion == null
                        && catalog.Episode.FindChoice(completion.choiceId) != null
                        && MilkroomInvestigationSaveData.IsReceiptToken(completion.receiptId)
                        && MilkroomInvestigationSaveData.TryParseTimestamp(
                            completion.completedAtIso,
                            out _))
                    {
                        knownCompletion = completion;
                    }
                    else
                    {
                        changed = true;
                    }
                }

                for (var index = 0; index < state.completions.Count; index += 1)
                {
                    var completion = state.completions[index];
                    if (completion != null
                        && !string.Equals(
                            completion.episodeId,
                            catalog.Episode.Id,
                            StringComparison.Ordinal))
                    {
                        unknownCompletions.Add(completion);
                    }
                }

                if (knownCompletion != null)
                {
                    unknownCompletions.Add(knownCompletion);
                }

                changed |= !HasSameReferences(state.completions, unknownCompletions);
                state.completions = unknownCompletions;
            }

            changed |= state.EnsureRuntimeDefaults();
            return changed;
        }

        public bool IsUnlocked(DreamStorySeasonSaveData dreamState)
        {
            if (!catalog.HasContent
                || !string.Equals(
                    catalog.Episode.RequiredDreamEpisodeId,
                    SeasonTwoFinalEpisodeId,
                    StringComparison.Ordinal)
                || dreamState?.completions == null)
            {
                return false;
            }

            for (var index = dreamState.completions.Count - 1; index >= 0; index -= 1)
            {
                var completion = dreamState.completions[index];
                if (completion == null
                    || !string.Equals(
                        completion.episodeId,
                        SeasonTwoFinalEpisodeId,
                        StringComparison.Ordinal)
                    || (!string.Equals(
                            completion.choiceId,
                            SeasonTwoFinalChoiceLeaveOpen,
                            StringComparison.Ordinal)
                        && !string.Equals(
                            completion.choiceId,
                            SeasonTwoFinalChoiceThreadMark,
                            StringComparison.Ordinal))
                    || !MilkroomInvestigationSaveData.IsReceiptToken(completion.receiptId)
                    || !DateTimeOffset.TryParse(
                        completion.completedAtIso,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out _))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public MilkroomInvestigationSnapshot BuildSnapshot(
            MilkroomInvestigationSaveData state,
            DreamStorySeasonSaveData dreamState,
            MilkroomInvestigationContext context)
        {
            if (state == null || !catalog.HasContent || !IsUnlocked(dreamState))
            {
                return MilkroomInvestigationSnapshot.Hidden;
            }

            var episode = catalog.Episode;
            var completion = FindKnownCompletion(state);
            if (completion != null)
            {
                return MilkroomInvestigationSnapshot.CreateRecall(
                    episode,
                    episode.FindChoice(completion.choiceId));
            }

            if (!context.IsValid)
            {
                return MilkroomInvestigationSnapshot.Hidden;
            }

            return BuildActiveSnapshot(state, context);
        }

        public MilkroomInvestigationSnapshot BuildRecallSnapshot(
            MilkroomInvestigationSaveData state,
            DreamStorySeasonSaveData dreamState)
        {
            if (state == null || !catalog.HasContent || !IsUnlocked(dreamState))
            {
                return MilkroomInvestigationSnapshot.Hidden;
            }

            var completion = FindKnownCompletion(state);
            return MilkroomInvestigationSnapshot.CreateRecall(
                catalog.Episode,
                catalog.Episode.FindChoice(completion?.choiceId));
        }

        public MilkroomInvestigationInspectResult TryInspect(
            MilkroomInvestigationSaveData state,
            DreamStorySeasonSaveData dreamState,
            string hotspotId,
            MilkroomInvestigationContext context)
        {
            if (state == null)
            {
                return InspectFailure(MilkroomInvestigationInspectStatus.MissingState);
            }

            if (!catalog.HasContent)
            {
                return InspectFailure(MilkroomInvestigationInspectStatus.ContentUnavailable);
            }

            if (!IsUnlocked(dreamState) || FindKnownCompletion(state) != null)
            {
                return InspectFailure(MilkroomInvestigationInspectStatus.Locked);
            }

            if (!context.IsValid)
            {
                return InspectFailure(MilkroomInvestigationInspectStatus.InvalidContext);
            }

            var hotspot = catalog.Episode.FindHotspot(hotspotId);
            if (hotspot == null)
            {
                return InspectFailure(MilkroomInvestigationInspectStatus.UnknownHotspot);
            }

            if (!hotspot.Requirement.IsSatisfied(context))
            {
                return new MilkroomInvestigationInspectResult(
                    MilkroomInvestigationInspectStatus.ConditionNotMet,
                    hotspotId: hotspot.Id,
                    userMessage: hotspot.Requirement.Hint,
                    snapshot: BuildActiveSnapshot(state, context));
            }

            if (state.HasDiscoveredClue(hotspot.ClueId))
            {
                return new MilkroomInvestigationInspectResult(
                    MilkroomInvestigationInspectStatus.AlreadyDiscovered,
                    hotspotId: hotspot.Id,
                    clueId: hotspot.ClueId,
                    clueTitle: hotspot.ClueTitle,
                    clueDetail: hotspot.ClueDetail,
                    userMessage: "이미 기록한 단서예요.",
                    snapshot: BuildActiveSnapshot(state, context));
            }

            state.EnsureRuntimeDefaults();
            while (state.discoveredClueIds.Count >= MilkroomInvestigationSaveData.MaximumClues)
            {
                var removableIndex = FindFirstUnknownClueIndex(state);
                if (removableIndex < 0)
                {
                    return InspectFailure(MilkroomInvestigationInspectStatus.StateCapacityFull);
                }

                state.discoveredClueIds.RemoveAt(removableIndex);
            }

            if (!state.TryRecordClue(hotspot.ClueId))
            {
                return InspectFailure(MilkroomInvestigationInspectStatus.PersistenceRejected);
            }

            return new MilkroomInvestigationInspectResult(
                MilkroomInvestigationInspectStatus.Discovered,
                applied: true,
                hotspotId: hotspot.Id,
                clueId: hotspot.ClueId,
                clueTitle: hotspot.ClueTitle,
                clueDetail: hotspot.ClueDetail,
                userMessage: "새 조사 단서를 기록했어요.",
                snapshot: BuildActiveSnapshot(state, context));
        }

        public MilkroomInvestigationChoiceResult TryChoose(
            MilkroomInvestigationSaveData state,
            DreamStorySeasonSaveData dreamState,
            string episodeId,
            string choiceId,
            string receiptId,
            DateTimeOffset completedAt,
            Func<MilkroomInvestigationChoicePayloadBundle, bool> tryCommitPayloads)
        {
            if (state == null)
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.MissingState);
            }

            if (!catalog.HasContent)
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.ContentUnavailable);
            }

            if (!IsUnlocked(dreamState))
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.Locked);
            }

            var episode = catalog.Episode;
            if (!string.Equals(episode.Id, (episodeId ?? string.Empty).Trim(), StringComparison.Ordinal))
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.UnknownEpisode);
            }

            if (FindKnownCompletion(state) != null)
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.AlreadyCompleted);
            }

            if (!HasAllKnownClues(state))
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.CluesIncomplete);
            }

            var choice = episode.FindChoice(choiceId);
            if (choice == null)
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.UnknownChoice);
            }

            var normalizedReceipt = (receiptId ?? string.Empty).Trim();
            if (!MilkroomInvestigationSaveData.IsReceiptToken(normalizedReceipt))
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.InvalidReceipt);
            }

            if (HasKnownReceipt(state, normalizedReceipt))
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.DuplicateReceipt);
            }

            if (completedAt == default)
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.InvalidCompletionTime);
            }

            if (!CanRecordKnownCompletion(state))
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.StateCapacityFull);
            }

            if (tryCommitPayloads == null)
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.MissingPayloadCommitter);
            }

            var memory = new MilkroomInvestigationRecordPayload(
                MemoryRecordPrefix + episode.Id,
                episode.Id,
                choice.Id,
                choice.Memory.Title,
                choice.Memory.Detail);
            var codex = new MilkroomInvestigationRecordPayload(
                MilkroomInvestigationContentLoader.BuildCodexRecordId(
                    episode.Id,
                    choice.Id),
                episode.Id,
                choice.Id,
                choice.Codex.Title,
                choice.Codex.Detail);
            var reward = new MilkroomInvestigationRewardPayload(
                normalizedReceipt,
                choice.Reward.Kind,
                choice.Reward.Amount);
            var payloads = new MilkroomInvestigationChoicePayloadBundle(
                episode.Id,
                choice.Id,
                normalizedReceipt,
                completedAt,
                choice.ResultMessage,
                memory,
                codex,
                reward);

            if (!tryCommitPayloads(payloads))
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.PayloadRejected);
            }

            if (!TryRecordKnownCompletion(
                    state,
                    new MilkroomInvestigationCompletionSaveData
                    {
                        episodeId = episode.Id,
                        choiceId = choice.Id,
                        receiptId = normalizedReceipt,
                        completedAtIso = MilkroomInvestigationSaveData.FormatTimestamp(completedAt)
                    }))
            {
                return ChoiceFailure(MilkroomInvestigationChoiceStatus.PersistenceRejected);
            }

            return new MilkroomInvestigationChoiceResult(
                MilkroomInvestigationChoiceStatus.Applied,
                payloads);
        }

        private MilkroomInvestigationSnapshot BuildActiveSnapshot(
            MilkroomInvestigationSaveData state,
            MilkroomInvestigationContext context)
        {
            var episode = catalog.Episode;
            var views = new MilkroomInvestigationHotspotView[episode.Hotspots.Count];
            var discoveredCount = 0;
            for (var index = 0; index < episode.Hotspots.Count; index += 1)
            {
                var hotspot = episode.Hotspots[index];
                var discovered = state.HasDiscoveredClue(hotspot.ClueId);
                if (discovered)
                {
                    discoveredCount += 1;
                }

                var available = hotspot.Requirement.IsSatisfied(context);
                views[index] = new MilkroomInvestigationHotspotView(
                    hotspot.Id,
                    hotspot.Kind,
                    hotspot.Label,
                    discovered,
                    available,
                    available ? string.Empty : hotspot.Requirement.Hint,
                    hotspot.ClueId,
                    hotspot.ClueTitle,
                    hotspot.ClueDetail);
            }

            return MilkroomInvestigationSnapshot.CreateActive(
                episode,
                discoveredCount,
                views);
        }

        private bool HasAllKnownClues(MilkroomInvestigationSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            for (var index = 0; index < catalog.Episode.Hotspots.Count; index += 1)
            {
                if (!state.HasDiscoveredClue(catalog.Episode.Hotspots[index].ClueId))
                {
                    return false;
                }
            }

            return true;
        }

        private int FindFirstUnknownClueIndex(MilkroomInvestigationSaveData state)
        {
            for (var index = 0; index < state.discoveredClueIds.Count; index += 1)
            {
                if (!catalog.Episode.KnowsClue(state.discoveredClueIds[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private MilkroomInvestigationCompletionSaveData FindKnownCompletion(
            MilkroomInvestigationSaveData state)
        {
            if (state?.completions == null)
            {
                return null;
            }

            for (var index = state.completions.Count - 1; index >= 0; index -= 1)
            {
                var completion = state.completions[index];
                if (completion != null
                    && string.Equals(
                        completion.episodeId,
                        catalog.Episode.Id,
                        StringComparison.Ordinal)
                    && catalog.Episode.FindChoice(completion.choiceId) != null
                    && MilkroomInvestigationSaveData.IsReceiptToken(completion.receiptId)
                    && MilkroomInvestigationSaveData.TryParseTimestamp(
                        completion.completedAtIso,
                        out _))
                {
                    return completion;
                }
            }

            return null;
        }

        private bool HasKnownReceipt(
            MilkroomInvestigationSaveData state,
            string receiptId)
        {
            if (string.IsNullOrEmpty(receiptId))
            {
                return false;
            }

            var completion = FindKnownCompletion(state);
            return completion != null
                && string.Equals(completion.receiptId, receiptId, StringComparison.Ordinal);
        }

        private bool CanRecordKnownCompletion(MilkroomInvestigationSaveData state)
        {
            if (state?.completions == null)
            {
                return false;
            }

            var knownCount = 0;
            for (var index = 0; index < state.completions.Count; index += 1)
            {
                var completion = state.completions[index];
                if (completion != null
                    && string.Equals(
                        completion.episodeId,
                        catalog.Episode.Id,
                        StringComparison.Ordinal)
                    && catalog.Episode.FindChoice(completion.choiceId) != null)
                {
                    knownCount += 1;
                }
            }

            return knownCount < MilkroomInvestigationSaveData.MaximumCompletions;
        }

        private bool TryRecordKnownCompletion(
            MilkroomInvestigationSaveData state,
            MilkroomInvestigationCompletionSaveData completion)
        {
            if (state?.completions == null || completion == null)
            {
                return false;
            }

            completion.EnsureRuntimeDefaults();
            if (!completion.HasValue
                || catalog.Episode.FindChoice(completion.choiceId) == null
                || FindKnownCompletion(state) != null)
            {
                return false;
            }

            for (var index = state.completions.Count - 1; index >= 0; index -= 1)
            {
                var existing = state.completions[index];
                var existingIsKnown = existing != null
                    && string.Equals(
                        existing.episodeId,
                        catalog.Episode.Id,
                        StringComparison.Ordinal)
                    && catalog.Episode.FindChoice(existing.choiceId) != null;
                if (!existingIsKnown
                    && (string.Equals(
                            existing?.episodeId,
                            completion.episodeId,
                            StringComparison.Ordinal)
                        || string.Equals(
                            existing?.receiptId,
                            completion.receiptId,
                            StringComparison.Ordinal)))
                {
                    state.completions.RemoveAt(index);
                }
            }

            while (state.completions.Count >= MilkroomInvestigationSaveData.MaximumCompletions)
            {
                var removableIndex = -1;
                for (var index = 0; index < state.completions.Count; index += 1)
                {
                    var existing = state.completions[index];
                    var existingIsKnown = existing != null
                        && string.Equals(
                            existing.episodeId,
                            catalog.Episode.Id,
                            StringComparison.Ordinal)
                        && catalog.Episode.FindChoice(existing.choiceId) != null;
                    if (!existingIsKnown)
                    {
                        removableIndex = index;
                        break;
                    }
                }

                if (removableIndex < 0)
                {
                    return false;
                }

                state.completions.RemoveAt(removableIndex);
            }

            return state.TryRecordCompletion(completion);
        }

        private static bool HasSameReferences(
            IReadOnlyList<MilkroomInvestigationCompletionSaveData> left,
            IReadOnlyList<MilkroomInvestigationCompletionSaveData> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index += 1)
            {
                if (!ReferenceEquals(left[index], right[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasSameValues(
            IReadOnlyList<string> left,
            IReadOnlyList<string> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index += 1)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static MilkroomInvestigationInspectResult InspectFailure(
            MilkroomInvestigationInspectStatus status)
        {
            return new MilkroomInvestigationInspectResult(status);
        }

        private static MilkroomInvestigationChoiceResult ChoiceFailure(
            MilkroomInvestigationChoiceStatus status)
        {
            return new MilkroomInvestigationChoiceResult(status);
        }
    }
}
