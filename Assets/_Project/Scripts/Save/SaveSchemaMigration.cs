using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Decorations;

namespace CheeseTama.Save
{
    public sealed class SaveSchemaMigrationException : InvalidOperationException
    {
        public SaveSchemaMigrationException(SaveSchemaMigrationResult result)
            : base($"Unsupported CheeseTama save schema revision: {result.SourceRevision}")
        {
            Result = result;
        }

        public SaveSchemaMigrationResult Result { get; }
    }

    public enum SaveSchemaMigrationStatus
    {
        Migrated = 0,
        AlreadyCurrent = 1,
        MissingSave = 2,
        UnsupportedPastRevision = 3,
        UnsupportedFutureRevision = 4
    }

    public readonly struct SaveSchemaMigrationResult
    {
        internal SaveSchemaMigrationResult(
            SaveSchemaMigrationStatus status,
            int sourceRevision,
            int targetRevision)
        {
            Status = status;
            SourceRevision = sourceRevision;
            TargetRevision = targetRevision;
        }

        public SaveSchemaMigrationStatus Status { get; }
        public int SourceRevision { get; }
        public int TargetRevision { get; }
        public bool Succeeded => Status == SaveSchemaMigrationStatus.Migrated
            || Status == SaveSchemaMigrationStatus.AlreadyCurrent;
        public bool Changed => Status == SaveSchemaMigrationStatus.Migrated;
    }

    /// <summary>
    /// Single sequential migration boundary shared by local loads, browser imports, and cloud
    /// replacement. Product version remains a display/build contract; schemaRevision owns save
    /// compatibility and advances one deterministic step at a time.
    /// </summary>
    public static class SaveSchemaMigrationHub
    {
        public const int EarliestSupportedRevision = 0;
        public const int CurrentRevision = 4;
        public const string CurrentProductVersion = "0.1.0";

        private const string SchemaRevisionFieldName = "schemaRevision";

        public static int ResolveSerializedRevision(
            string serializedJson,
            CheeseTamaSaveData deserialized)
        {
            return HasSerializedField(serializedJson, SchemaRevisionFieldName)
                ? deserialized?.schemaRevision ?? -1
                : EarliestSupportedRevision;
        }

        public static SaveSchemaMigrationResult Migrate(
            CheeseTamaSaveData save,
            int sourceRevision)
        {
            if (save == null)
            {
                return new SaveSchemaMigrationResult(
                    SaveSchemaMigrationStatus.MissingSave,
                    sourceRevision,
                    CurrentRevision);
            }

            if (sourceRevision < EarliestSupportedRevision)
            {
                return new SaveSchemaMigrationResult(
                    SaveSchemaMigrationStatus.UnsupportedPastRevision,
                    sourceRevision,
                    CurrentRevision);
            }

            if (sourceRevision > CurrentRevision)
            {
                return new SaveSchemaMigrationResult(
                    SaveSchemaMigrationStatus.UnsupportedFutureRevision,
                    sourceRevision,
                    CurrentRevision);
            }

            var revision = sourceRevision;
            while (revision < CurrentRevision)
            {
                switch (revision)
                {
                    case 0:
                        MigrateUnversionedToRevisionOne(save);
                        break;
                    case 1:
                        MigrateRevisionOneToTwo(save);
                        break;
                    case 2:
                        MigrateRevisionTwoToThree(save);
                        break;
                    case 3:
                        MigrateRevisionThreeToFour(save);
                        break;
                    default:
                        return new SaveSchemaMigrationResult(
                            SaveSchemaMigrationStatus.UnsupportedPastRevision,
                            sourceRevision,
                            CurrentRevision);
                }

                revision += 1;
                save.schemaRevision = revision;
            }

            save.schemaRevision = CurrentRevision;
            return new SaveSchemaMigrationResult(
                sourceRevision == CurrentRevision
                    ? SaveSchemaMigrationStatus.AlreadyCurrent
                    : SaveSchemaMigrationStatus.Migrated,
                sourceRevision,
                CurrentRevision);
        }

        private static void MigrateUnversionedToRevisionOne(CheeseTamaSaveData save)
        {
            if (string.IsNullOrWhiteSpace(save.version))
            {
                save.version = CurrentProductVersion;
            }

            if (string.IsNullOrWhiteSpace(save.playerId))
            {
                save.playerId = "local_player";
            }
        }

        private static void MigrateRevisionOneToTwo(CheeseTamaSaveData save)
        {
            save.milkroomThemeId = ContentIdLifecycle.Normalize(
                ContentIdCategory.Theme,
                save.milkroomThemeId);

            save.decorations ??= new DecorationSaveData();
            save.decorationPlacement ??= DecorationPlacementSaveData.MigrateLegacySlots(
                save.decorations.equippedAccentId,
                save.decorations.equippedShelfId,
                save.decorations.equippedBedsideId);
            save.decorationPlacement.EnsureRuntimeDefaults(
                save.decorations.equippedAccentId,
                save.decorations.equippedShelfId,
                save.decorations.equippedBedsideId);

            var ownedThemes = save.decorations?.ownedThemeIds;
            if (ownedThemes == null)
            {
                return;
            }

            for (var index = 0; index < ownedThemes.Count; index += 1)
            {
                ownedThemes[index] = ContentIdLifecycle.Normalize(
                    ContentIdCategory.Theme,
                    ownedThemes[index]);
            }
        }

        private static void MigrateRevisionTwoToThree(CheeseTamaSaveData save)
        {
            // Life chapters and investigations need catalog-aware, known-first repair. The load
            // and import boundaries run root EnsureRuntimeDefaults immediately after migration;
            // do not truncate these lists generically before that repair can reserve shipped data.
            save.lifeChapters ??= LifeChapterSaveData.CreateForLegacyPlayer();
            save.miniGameMastery ??= new MiniGameMasterySaveData();
            save.miniGameMastery.EnsureRuntimeDefaults();
            save.milkroomInvestigation ??= new MilkroomInvestigationSaveData();
            save.starLineage ??= new StarLineageSaveData();
            save.starLineage.EnsureRuntimeDefaults();
        }

        private static void MigrateRevisionThreeToFour(CheeseTamaSaveData save)
        {
            save.decorations ??= new DecorationSaveData();
            var decorations = save.decorations;
            decorations.ownedItemIds ??= new List<string>();

            RemoveFormerDefaultOwnership(decorations.ownedItemIds, DecorationCatalog.CreamRugId);
            RemoveFormerDefaultOwnership(decorations.ownedItemIds, DecorationCatalog.MilkBottleId);
            RemoveFormerDefaultOwnership(decorations.ownedItemIds, DecorationCatalog.CreamCurtainId);
            RemoveFormerDefaultOwnership(decorations.ownedItemIds, DecorationCatalog.CheeseClockId);
            RemoveFormerDefaultOwnership(decorations.ownedItemIds, DecorationCatalog.MilkCushionId);

            ClearFormerDefaultEquipment(ref decorations.equippedFloorId, DecorationCatalog.CreamRugId);
            ClearFormerDefaultEquipment(ref decorations.equippedAccentId, DecorationCatalog.MilkBottleId);
            ClearFormerDefaultEquipment(ref decorations.equippedWindowId, DecorationCatalog.CreamCurtainId);
            ClearFormerDefaultEquipment(ref decorations.equippedShelfId, DecorationCatalog.CheeseClockId);
            ClearFormerDefaultEquipment(ref decorations.equippedBedsideId, DecorationCatalog.MilkCushionId);

            save.decorationPlacement?.EnsureRuntimeDefaults(
                decorations.equippedAccentId,
                decorations.equippedShelfId,
                decorations.equippedBedsideId);
        }

        private static void RemoveFormerDefaultOwnership(List<string> ownedItemIds, string itemId)
        {
            ownedItemIds.RemoveAll(value =>
                string.Equals(value?.Trim(), itemId, System.StringComparison.Ordinal));
        }

        private static void ClearFormerDefaultEquipment(ref string equippedItemId, string itemId)
        {
            if (string.Equals(equippedItemId?.Trim(), itemId, System.StringComparison.Ordinal))
            {
                equippedItemId = string.Empty;
            }
        }

        private static bool HasSerializedField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            var token = $"\"{fieldName}\"";
            var searchIndex = 0;
            while (searchIndex < json.Length)
            {
                var fieldIndex = json.IndexOf(token, searchIndex, StringComparison.Ordinal);
                if (fieldIndex < 0)
                {
                    return false;
                }

                var separatorIndex = fieldIndex + token.Length;
                while (separatorIndex < json.Length && char.IsWhiteSpace(json[separatorIndex]))
                {
                    separatorIndex += 1;
                }

                if (separatorIndex < json.Length && json[separatorIndex] == ':')
                {
                    return true;
                }

                searchIndex = fieldIndex + token.Length;
            }

            return false;
        }
    }

    public static class ContentIdCategory
    {
        public const string Theme = "theme";
    }

    /// <summary>
    /// Stable alias/tombstone seam for renamed authored IDs. Aliases are intentionally small and
    /// explicit so migrations never guess at unknown future content.
    /// </summary>
    public static class ContentIdLifecycle
    {
        private static readonly IReadOnlyDictionary<string, string> Aliases =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { BuildKey(ContentIdCategory.Theme, "milkroom_day"), "milkroom_morning" },
                { BuildKey(ContentIdCategory.Theme, "milkroom_starry"), "milkroom_starlight" }
            };

        public static string Normalize(string category, string contentId)
        {
            var normalizedCategory = (category ?? string.Empty).Trim();
            var normalizedId = (contentId ?? string.Empty).Trim();
            return Aliases.TryGetValue(BuildKey(normalizedCategory, normalizedId), out var currentId)
                ? currentId
                : normalizedId;
        }

        private static string BuildKey(string category, string contentId)
        {
            return (category ?? string.Empty) + ":" + (contentId ?? string.Empty);
        }
    }
}
