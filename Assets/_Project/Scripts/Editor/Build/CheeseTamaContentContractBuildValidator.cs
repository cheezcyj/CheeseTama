using System;
using System.Collections.Generic;
using System.IO;
using CheeseTama.Data;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.HiddenRecipes;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Research;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Gameplay.Story;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CheeseTama.Editor
{
    public sealed class CheeseTamaContentContractBuildValidator : IPreprocessBuildWithReport
    {
        private const string ContentRoot = "Assets/_Project/Resources/Content";
        private const string DreamAssetPrefix = "DreamStorySeason";

        private const string MilkCategory = "milk";
        private const string IngredientCategory = "blend_ingredient";
        private const string SnackCategory = "snack";
        private const string BlendRecipeCategory = "blend_recipe";
        private const string HiddenRecipeCategory = "hidden_recipe";
        private const string DecorationCategory = "decoration";
        private const string WorkshopCategory = "decoration_workshop";
        private const string ResearchCategory = "research";
        private const string SeasonalEventCategory = "seasonal_event";
        private const string DreamEpisodeCategory = "dream_episode";
        private const string DreamChoiceCategory = "dream_choice";
        private const string MysteryChapterCategory = "mystery_chapter";
        private const string MysteryChoiceCategory = "mystery_choice";

        public int callbackOrder => -950;

        [MenuItem("Tools/CheeseTama/Validate All Content Contracts")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log("CheeseTama content contract validation passed.");
        }

        public static IReadOnlyList<string> ValidateProjectContent()
        {
            var errors = new List<string>();
            var entries = new List<ContentContractEntry>();
            CollectStaticCatalogEntries(entries);
            CollectMysteryEntries(entries, errors);
            CollectDreamEntries(entries, errors);

            var report = ContentContractValidator.Validate(entries);
            for (var index = 0; index < report.Issues.Count; index += 1)
            {
                var issue = report.Issues[index];
                errors.Add(string.IsNullOrEmpty(issue.Source)
                    ? issue.Message
                    : issue.Source + ": " + issue.Message);
            }

            return errors;
        }

        public static void ValidateOrThrow()
        {
            var errors = ValidateProjectContent();
            if (errors.Count == 0)
            {
                return;
            }

            throw new BuildFailedException(
                "CheeseTama content contract validation failed:\n- "
                + string.Join("\n- ", errors));
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow();
        }

        private static void CollectStaticCatalogEntries(ICollection<ContentContractEntry> entries)
        {
            for (var index = 0; index < MilkBlendingCatalog.AllMilkIds.Length; index += 1)
            {
                entries.Add(new ContentContractEntry(
                    "MilkBlendingCatalog.AllMilkIds",
                    MilkCategory,
                    MilkBlendingCatalog.AllMilkIds[index]));
            }

            for (var index = 0; index < MilkBlendingCatalog.AllIngredients.Length; index += 1)
            {
                entries.Add(new ContentContractEntry(
                    "MilkBlendingCatalog.AllIngredients",
                    IngredientCategory,
                    MilkBlendingCatalog.AllIngredients[index]?.id));
            }

            for (var index = 0; index < SnackCatalog.All.Length; index += 1)
            {
                entries.Add(new ContentContractEntry(
                    "SnackCatalog.All",
                    SnackCategory,
                    SnackCatalog.All[index]?.id));
            }

            for (var index = 0; index < MilkBlendingCatalog.AllRecipes.Length; index += 1)
            {
                var recipe = MilkBlendingCatalog.AllRecipes[index];
                var recipeId = recipe?.milkId + "__" + recipe?.ingredientId;
                entries.Add(new ContentContractEntry(
                    "MilkBlendingCatalog.AllRecipes",
                    BlendRecipeCategory,
                    recipeId,
                    new ContentContractReference(MilkCategory, recipe?.milkId),
                    new ContentContractReference(IngredientCategory, recipe?.ingredientId),
                    new ContentContractReference(SnackCategory, recipe?.resultSnackId),
                    new ContentContractReference(
                        SnackCategory,
                        recipe?.specialResultSnackId,
                        required: false)));
            }

            for (var index = 0; index < FantasyPowderHiddenRecipeCatalog.All.Length; index += 1)
            {
                var recipe = FantasyPowderHiddenRecipeCatalog.All[index];
                entries.Add(new ContentContractEntry(
                    "FantasyPowderHiddenRecipeCatalog.All",
                    HiddenRecipeCategory,
                    recipe?.id,
                    new ContentContractReference(SnackCategory, recipe?.resultSnackId),
                    new ContentContractReference(
                        SnackCategory,
                        recipe?.byproductSnackId,
                        required: false)));
            }

            for (var index = 0; index < DecorationCatalog.All.Length; index += 1)
            {
                entries.Add(new ContentContractEntry(
                    "DecorationCatalog.All",
                    DecorationCategory,
                    DecorationCatalog.All[index]?.id));
            }

            var workshop = DecorationWorkshopCatalog.All;
            for (var index = 0; index < workshop.Count; index += 1)
            {
                entries.Add(new ContentContractEntry(
                    "DecorationWorkshopCatalog.All",
                    WorkshopCategory,
                    workshop[index]?.Id));
            }

            var research = PostgameResearchCatalog.All;
            for (var index = 0; index < research.Count; index += 1)
            {
                var node = research[index];
                entries.Add(new ContentContractEntry(
                    "PostgameResearchCatalog.All",
                    ResearchCategory,
                    node?.Id,
                    new ContentContractReference(
                        ResearchCategory,
                        node?.PrerequisiteNodeId,
                        required: false)));
            }

            var events = SeasonalCareEventCatalog.All;
            for (var index = 0; index < events.Count; index += 1)
            {
                entries.Add(new ContentContractEntry(
                    "SeasonalCareEventCatalog.All",
                    SeasonalEventCategory,
                    events[index]?.CareEvent?.id));
            }
        }

        private static void CollectMysteryEntries(
            ICollection<ContentContractEntry> entries,
            ICollection<string> errors)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                MilkroomMysteryContentBuildValidator.ContentAssetPath);
            if (asset == null)
            {
                errors.Add("밀크룸 미스터리 콘텐츠 파일을 찾을 수 없습니다.");
                return;
            }

            if (!MilkroomMysteryContentLoader.TryLoadFromJson(
                    asset.text,
                    out var catalog,
                    out var validationErrors))
            {
                AddErrors(errors, MilkroomMysteryContentBuildValidator.ContentAssetPath, validationErrors);
                return;
            }

            for (var chapterIndex = 0; chapterIndex < catalog.Chapters.Count; chapterIndex += 1)
            {
                var chapter = catalog.Chapters[chapterIndex];
                entries.Add(new ContentContractEntry(
                    MilkroomMysteryContentBuildValidator.ContentAssetPath,
                    MysteryChapterCategory,
                    chapter.Id,
                    new ContentContractReference(
                        MysteryChapterCategory,
                        chapter.PrerequisiteChapterId,
                        required: false)));
                for (var choiceIndex = 0; choiceIndex < chapter.Choices.Count; choiceIndex += 1)
                {
                    var choice = chapter.Choices[choiceIndex];
                    var references = choice.Reward.Kind == MilkroomMysteryRewardKind.Decoration
                        ? new[]
                        {
                            new ContentContractReference(
                                DecorationCategory,
                                choice.Reward.Id)
                        }
                        : Array.Empty<ContentContractReference>();
                    entries.Add(new ContentContractEntry(
                        MilkroomMysteryContentBuildValidator.ContentAssetPath,
                        MysteryChoiceCategory,
                        chapter.Id + ":" + choice.Id,
                        references));
                }
            }
        }

        private static void CollectDreamEntries(
            ICollection<ContentContractEntry> entries,
            ICollection<string> errors)
        {
            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { ContentRoot });
            Array.Sort(guids, StringComparer.Ordinal);
            var foundDreamAsset = false;
            for (var index = 0; index < guids.Length; index += 1)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(fileName)
                    || !fileName.StartsWith(DreamAssetPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                foundDreamAsset = true;
            }

            if (!foundDreamAsset)
            {
                errors.Add("꿈 이야기 시즌 콘텐츠 파일을 하나 이상 정의해야 합니다.");
                return;
            }

            if (!DreamStorySeasonContentLoader.TryReloadAll(
                    out var mergedCatalog,
                    out var validationErrors))
            {
                AddErrors(errors, "DreamStorySeasonContentLoader.All", validationErrors);
                return;
            }

            for (var episodeIndex = 0;
                episodeIndex < mergedCatalog.Episodes.Count;
                episodeIndex += 1)
            {
                var episode = mergedCatalog.Episodes[episodeIndex];
                var references = new List<ContentContractReference>
                {
                    new ContentContractReference(
                        DreamEpisodeCategory,
                        episode.PrerequisiteEpisodeId,
                        required: false)
                };
                if (episode.Trigger.Kind == DreamStoryTriggerKind.ExternalStoryCompleted)
                {
                    references.Add(new ContentContractReference(
                        MysteryChapterCategory,
                        episode.Trigger.RequiredId));
                }
                else if (episode.Trigger.Kind == DreamStoryTriggerKind.DreamEpisodeCompleted)
                {
                    references.Add(new ContentContractReference(
                        DreamEpisodeCategory,
                        episode.Trigger.RequiredId));
                }

                entries.Add(new ContentContractEntry(
                    "DreamStorySeasonContentLoader.All",
                    DreamEpisodeCategory,
                    episode.Id,
                    references.ToArray()));
                for (var choiceIndex = 0;
                    choiceIndex < episode.Choices.Count;
                    choiceIndex += 1)
                {
                    entries.Add(new ContentContractEntry(
                        "DreamStorySeasonContentLoader.All",
                        DreamChoiceCategory,
                        episode.Id + ":" + episode.Choices[choiceIndex].Id));
                }
            }
        }

        private static void AddErrors(
            ICollection<string> destination,
            string source,
            IReadOnlyList<string> sourceErrors)
        {
            if (sourceErrors == null || sourceErrors.Count == 0)
            {
                destination.Add(source + ": 콘텐츠를 읽지 못했습니다.");
                return;
            }

            for (var index = 0; index < sourceErrors.Count; index += 1)
            {
                destination.Add(source + ": " + sourceErrors[index]);
            }
        }
    }
}
