using System;
using System.IO;
using System.Text;
using CheeseTama.Platform;
using CheeseTama.Platform.Accounts;
using CheeseTama.Utilities;
using UnityEngine;

namespace CheeseTama.Save
{
    public sealed class SaveManager : MonoBehaviour
    {
        private const string BackupFileSuffix = ".bak";
        private const string TemporaryFileSuffix = ".tmp";
        private const string CorruptFileSuffix = ".corrupt";
        private const string DefaultSaveFileName = "cheesetama_save.json";
        private const string LocalAccountSaveFileNamePrefix = "cheesetama_account_";
        private const string RuntimeDiagnosticSaveFileNamePrefix = "cheesetama_diagnostic_";
        private const int MaximumCloudSavePayloadBytes = 8 * 1024 * 1024;

        private static string runtimeDiagnosticSaveFileNameOverride;

        [SerializeField] private string saveFileName = DefaultSaveFileName;

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, ResolveSaveFileName());
        public string BackupFilePath => SaveFilePath + BackupFileSuffix;
        public string TemporaryFilePath => SaveFilePath + TemporaryFileSuffix;
        public bool HasSaveFile => File.Exists(SaveFilePath);
        public bool LastLoadMigratedData { get; private set; }
        public SaveRecoveryReport LastRecoveryReport { get; private set; } = SaveRecoveryReport.NoRecovery;

        public static string RuntimeDiagnosticSaveFileNameOverride =>
            runtimeDiagnosticSaveFileNameOverride;

        public static string CreateRuntimeDiagnosticSaveFileName()
        {
            return $"{RuntimeDiagnosticSaveFileNamePrefix}{Guid.NewGuid():N}.json";
        }

        public static bool IsValidRuntimeDiagnosticSaveFileName(string isolatedFileName)
        {
            if (string.IsNullOrWhiteSpace(isolatedFileName)
                || isolatedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || !string.Equals(Path.GetFileName(isolatedFileName), isolatedFileName, StringComparison.Ordinal))
            {
                return false;
            }

            var extension = Path.GetExtension(isolatedFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(isolatedFileName);
            var identifier = fileNameWithoutExtension.StartsWith(
                RuntimeDiagnosticSaveFileNamePrefix,
                StringComparison.Ordinal)
                ? fileNameWithoutExtension.Substring(RuntimeDiagnosticSaveFileNamePrefix.Length)
                : string.Empty;
            return string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParseExact(identifier, "N", out _);
        }

        public static bool TryCreateLocalAccountSaveFileName(
            string accountId,
            out string accountSaveFileName)
        {
            accountSaveFileName = string.Empty;
            if (!LocalAccountId.IsValid(accountId))
            {
                return false;
            }

            accountSaveFileName = $"{LocalAccountSaveFileNamePrefix}{accountId}.json";
            return string.Equals(
                Path.GetFileName(accountSaveFileName),
                accountSaveFileName,
                StringComparison.Ordinal);
        }

        public static void SetRuntimeDiagnosticSaveFileNameOverride(string isolatedFileName)
        {
            if (!IsValidRuntimeDiagnosticSaveFileName(isolatedFileName))
            {
                throw new ArgumentException(
                    "A GUID-based diagnostic save file name without path segments is required.",
                    nameof(isolatedFileName));
            }

            runtimeDiagnosticSaveFileNameOverride = isolatedFileName;
        }

        public static void ClearRuntimeDiagnosticSaveFileNameOverride(string expectedIsolatedFileName)
        {
            if (string.Equals(
                    runtimeDiagnosticSaveFileNameOverride,
                    expectedIsolatedFileName,
                    StringComparison.Ordinal))
            {
                runtimeDiagnosticSaveFileNameOverride = null;
            }
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public const string PlayModeTestSaveFileNamePrefix = "cheesetama_playmode_test_";
        public const string PlayModeTestSaveFileNameEnvironmentVariable =
            "CHEESETAMA_PLAYMODE_TEST_SAVE_FILE";

        private static string playModeTestSaveFileNameOverride;

        public static string PlayModeTestSaveFileNameOverride => playModeTestSaveFileNameOverride;

        public static void SetPlayModeTestSaveFileNameOverride(string isolatedFileName)
        {
            ValidateIsolatedSaveFileName(isolatedFileName, nameof(isolatedFileName));
            if (!IsValidPlayModeTestSaveFileName(isolatedFileName))
            {
                throw new ArgumentException(
                    $"A GUID-based file name beginning with {PlayModeTestSaveFileNamePrefix} is required.",
                    nameof(isolatedFileName));
            }

            playModeTestSaveFileNameOverride = isolatedFileName;
        }

        public static bool IsValidPlayModeTestSaveFileName(string isolatedFileName)
        {
            if (string.IsNullOrWhiteSpace(isolatedFileName)
                || isolatedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || !string.Equals(Path.GetFileName(isolatedFileName), isolatedFileName, StringComparison.Ordinal))
            {
                return false;
            }

            var extension = Path.GetExtension(isolatedFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(isolatedFileName);
            var identifier = fileNameWithoutExtension.StartsWith(
                PlayModeTestSaveFileNamePrefix,
                StringComparison.Ordinal)
                ? fileNameWithoutExtension.Substring(PlayModeTestSaveFileNamePrefix.Length)
                : string.Empty;
            return string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParseExact(identifier, "N", out _);
        }

        public static void ClearPlayModeTestSaveFileNameOverride(string expectedIsolatedFileName)
        {
            if (string.Equals(
                    playModeTestSaveFileNameOverride,
                    expectedIsolatedFileName,
                    StringComparison.Ordinal))
            {
                playModeTestSaveFileNameOverride = null;
            }
        }
#endif

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetIsolatedSaveFileNameForTests(string isolatedFileName)
        {
            ValidateIsolatedSaveFileName(isolatedFileName, nameof(isolatedFileName));
            saveFileName = isolatedFileName;
        }

        private static void ValidateIsolatedSaveFileName(string isolatedFileName, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(isolatedFileName)
                || isolatedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || !string.Equals(Path.GetFileName(isolatedFileName), isolatedFileName, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "An isolated file name without path segments is required.",
                    parameterName);
            }
        }
#endif

        private string ResolveSaveFileName()
        {
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
            // An explicitly isolated manager must remain independent from the process-wide
            // PlayMode bootstrap override used by the default runtime SaveManager.
            if (!string.IsNullOrWhiteSpace(saveFileName)
                && !string.Equals(saveFileName, DefaultSaveFileName, StringComparison.Ordinal))
            {
                return saveFileName;
            }

            var environmentOverride = System.Environment.GetEnvironmentVariable(
                PlayModeTestSaveFileNameEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentOverride))
            {
                if (!IsValidPlayModeTestSaveFileName(environmentOverride))
                {
                    throw new InvalidOperationException(
                        $"Invalid {PlayModeTestSaveFileNameEnvironmentVariable} value; refusing to resolve a save path.");
                }

                return environmentOverride;
            }

            if (!string.IsNullOrWhiteSpace(playModeTestSaveFileNameOverride))
            {
                return playModeTestSaveFileNameOverride;
            }
#endif
            if (!string.IsNullOrWhiteSpace(runtimeDiagnosticSaveFileNameOverride))
            {
                if (!IsValidRuntimeDiagnosticSaveFileName(runtimeDiagnosticSaveFileNameOverride))
                {
                    throw new InvalidOperationException(
                        "Invalid runtime diagnostic save override; refusing to resolve a save path.");
                }

                return runtimeDiagnosticSaveFileNameOverride;
            }

            if (string.Equals(saveFileName, DefaultSaveFileName, StringComparison.Ordinal))
            {
                LocalAccountRuntime.EnsureInitialized();
                var account = LocalAccountRuntime.Snapshot;
                if (account.IsSignedIn
                    && TryCreateLocalAccountSaveFileName(
                        account.AccountId,
                        out var accountSaveFileName))
                {
                    return accountSaveFileName;
                }
            }

            return saveFileName;
        }

        public bool TryInitializeLocalAccountSave(
            string accountId,
            CheeseTamaSaveData sourceSave)
        {
            if (sourceSave == null
                || !TryResolveLocalAccountPaths(
                    accountId,
                    out var primaryPath,
                    out var backupPath,
                    out var temporaryPath))
            {
                return false;
            }

            try
            {
                if (HasLocalAccountArtifacts(primaryPath, backupPath, temporaryPath))
                {
                    return false;
                }

                var sourceJson = JsonUtility.ToJson(sourceSave, true);
                if (!SaveTransferCodec.TryValidateAndNormalizeSaveContent(
                        sourceJson,
                        out _,
                        out var normalizedJson,
                        out _,
                        out _)
                    || Encoding.UTF8.GetByteCount(normalizedJson) > MaximumCloudSavePayloadBytes)
                {
                    return false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(primaryPath));
                WriteTextDurably(temporaryPath, normalizedJson);
                CommitTemporaryFile(primaryPath, backupPath, temporaryPath);
                BrowserPersistence.RequestSync();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            finally
            {
                try
                {
                    DeleteFileIfPresent(temporaryPath);
                }
                catch (IOException)
                {
                    // A committed primary remains authoritative.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort cleanup only.
                }
            }
        }

        public bool TryPurgeLocalAccountSaveArtifacts(string accountId)
        {
            if (!TryResolveLocalAccountPaths(
                    accountId,
                    out var primaryPath,
                    out var backupPath,
                    out var temporaryPath))
            {
                return false;
            }

            try
            {
                DeleteFileIfPresent(primaryPath);
                DeleteFileIfPresent(backupPath);
                DeleteFileIfPresent(temporaryPath);
                DeleteCorruptArtifacts(primaryPath);
                BrowserPersistence.RequestSync();
                return !HasLocalAccountArtifacts(primaryPath, backupPath, temporaryPath);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        public CheeseTamaSaveData LoadOrCreate()
        {
            LastLoadMigratedData = false;
            LastRecoveryReport = SaveRecoveryReport.NoRecovery;

            var primaryExists = File.Exists(SaveFilePath);
            if (primaryExists && TryReadSave(SaveFilePath, out var primaryJson, out var primarySave))
            {
                TryDeleteStaleTemporaryFile();
                return PrepareLoadedSave(primaryJson, primarySave);
            }

            var temporaryExists = File.Exists(TemporaryFilePath);
            if (temporaryExists && TryReadSave(TemporaryFilePath, out var temporaryJson, out var temporarySave))
            {
                var quarantinedCount = primaryExists ? QuarantineFile(SaveFilePath) : 0;
                File.Move(TemporaryFilePath, SaveFilePath);
                BrowserPersistence.RequestSync();
                LastRecoveryReport = new SaveRecoveryReport(
                    SaveRecoveryOutcome.RecoveredFromTemporaryFile,
                    quarantinedCount);
                return PrepareLoadedSave(temporaryJson, temporarySave);
            }

            var backupExists = File.Exists(BackupFilePath);
            if (backupExists && TryReadSave(BackupFilePath, out var backupJson, out var backupSave))
            {
                var quarantinedCount = 0;
                if (primaryExists)
                {
                    quarantinedCount += QuarantineFile(SaveFilePath);
                }

                if (temporaryExists)
                {
                    quarantinedCount += QuarantineFile(TemporaryFilePath);
                }

                RestorePrimaryFromJson(backupJson);
                LastRecoveryReport = new SaveRecoveryReport(
                    SaveRecoveryOutcome.RecoveredFromBackup,
                    quarantinedCount);
                return PrepareLoadedSave(backupJson, backupSave);
            }

            var corruptFileCount = 0;
            if (primaryExists)
            {
                corruptFileCount += QuarantineFile(SaveFilePath);
            }

            if (temporaryExists)
            {
                corruptFileCount += QuarantineFile(TemporaryFilePath);
            }

            if (backupExists)
            {
                corruptFileCount += QuarantineFile(BackupFilePath);
            }

            var created = CreateDefaultSave();
            Save(created);
            if (corruptFileCount > 0)
            {
                LastRecoveryReport = new SaveRecoveryReport(
                    SaveRecoveryOutcome.CreatedFreshSaveAfterCorruption,
                    corruptFileCount);
            }

            return created;
        }

        private CheeseTamaSaveData PrepareLoadedSave(string json, CheeseTamaSaveData loaded)
        {
            var serializedRevision = SaveSchemaMigrationHub.ResolveSerializedRevision(json, loaded);
            var schemaMigration = SaveSchemaMigrationHub.Migrate(loaded, serializedRevision);
            if (!schemaMigration.Succeeded)
            {
                throw new SaveSchemaMigrationException(schemaMigration);
            }

            if (schemaMigration.Changed)
            {
                LastLoadMigratedData = true;
            }

            var hasSerializedOnboarding = HasSerializedOnboardingField(json);
            var hasSerializedGrowthMilestone = HasSerializedField(json, "growthMilestone");
            var hasSerializedEvolutionMilestone = HasSerializedField(json, "evolutionMilestone");
            var hasSerializedMilkGrowthRewardKeys = HasSerializedField(json, "claimedMilkGrowthRewardKeys");
            var hasSerializedDecorations = HasSerializedField(json, "decorations");
            var hasSerializedDecorationPlacement = HasSerializedField(
                json,
                "decorationPlacement");
            var hasSerializedStarRoute = HasSerializedField(json, "starRoute");
            var hasSerializedPlayMiniGames = HasSerializedField(json, "playMiniGames");
            var hasSerializedNewGameSetup = HasSerializedField(json, "newGameSetup");
            var hasSerializedFirstDayJourney = HasSerializedField(json, "firstDayJourney");
            var hasSerializedCheeseStarDelivery = HasSerializedField(json, "cheeseStarDelivery");
            var hasSerializedMemoryJournal = HasSerializedField(json, "memoryJournal");
            var hasSerializedFantasyPowder = HasSerializedField(json, "fantasyPowder");
            var hasSerializedStarLegacy = HasSerializedField(json, "starLegacy");
            var hasSerializedNpcVisits = HasSerializedField(json, "npcVisits");
            var hasSerializedMilkBlending = HasSerializedField(json, "milkBlending");
            var hasSerializedAutonomousLife = HasSerializedField(json, "autonomousLife");
            var hasSerializedLateLevelGrowth = HasSerializedField(json, "lateLevelGrowth");
            var hasSerializedSleepSchedule = HasSerializedField(json, "sleepSchedule");
            var hasSerializedNpcRelationshipQuests = HasSerializedField(json, "npcRelationshipQuests");
            var hasSerializedNpcRelationshipEpisodes = HasSerializedField(json, "npcRelationshipEpisodes");
            var hasSerializedWeeklyCareJourney = HasSerializedField(json, "weeklyCareJourney");
            var hasSerializedDecorationWorkshop = HasSerializedField(json, "decorationWorkshop");
            var hasSerializedCollectionSetAlbum = HasSerializedField(json, "collectionSetAlbum");
            var hasSerializedMusicVolume = HasSerializedField(json, "musicVolume");
            var hasSerializedEffectVolume = HasSerializedField(json, "effectVolume");
            var hasSerializedInputBindings = HasSerializedField(json, "inputBindings");
            var hasSerializedGraphicsQuality = HasSerializedField(json, "graphicsQualityPreset");
            var hasSerializedTextScale = HasSerializedField(json, "textScale");
            var hasSerializedHighContrastUi = HasSerializedField(json, "highContrastUi");
            var hasSerializedReduceMotion = HasSerializedField(json, "reduceMotion");

            var migratedOnboarding = !hasSerializedOnboarding || loaded.onboarding == null;
            if (migratedOnboarding)
            {
                loaded.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
                LastLoadMigratedData = true;
            }

            if (loaded.onboarding != null && loaded.onboarding.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedGrowthMilestone || loaded.growthMilestone == null)
            {
                loaded.cheeseTama ??= new CheeseTama.Gameplay.CheeseTamaModel();
                loaded.cheeseTama.EnsureRuntimeDefaults();
                loaded.growthMilestone = GrowthMilestoneSaveData.CreateAcknowledged(
                    CheeseTama.Gameplay.Growth.CheeseTamaGrowthStageCatalog.Resolve(loaded.cheeseTama));
                LastLoadMigratedData = true;
            }

            if (!hasSerializedEvolutionMilestone || loaded.evolutionMilestone == null)
            {
                loaded.cheeseTama ??= new CheeseTama.Gameplay.CheeseTamaModel();
                loaded.cheeseTama.EnsureRuntimeDefaults();
                loaded.evolutionMilestone = EvolutionMilestoneSaveData.CreateAcknowledged(
                    loaded.cheeseTama.evolutionId);
                LastLoadMigratedData = true;
            }

            if (!hasSerializedStarRoute || loaded.starRoute == null)
            {
                loaded.unlocks ??= new CheeseTama.Gameplay.UnlockSaveData();
                loaded.starRoute = StarRouteSaveData.CreateAcknowledged(loaded.unlocks.starMilkUnlocked);
                LastLoadMigratedData = true;
            }

            if (!hasSerializedNewGameSetup || loaded.newGameSetup == null)
            {
                loaded.newGameSetup = NewGameSetupSaveData.CreateCompletedForLegacySave();
                LastLoadMigratedData = true;
            }
            else if (loaded.newGameSetup.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedFirstDayJourney || loaded.firstDayJourney == null)
            {
                loaded.firstDayJourney = FirstDayJourneySaveData.CreateCompletedForLegacySave();
                LastLoadMigratedData = true;
            }
            else if (loaded.firstDayJourney.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedCheeseStarDelivery || loaded.cheeseStarDelivery == null)
            {
                loaded.cheeseStarDelivery = new CheeseStarDeliverySaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.cheeseStarDelivery.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedMemoryJournal || loaded.memoryJournal == null)
            {
                loaded.memoryJournal = new MemoryJournalSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.memoryJournal.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedFantasyPowder || loaded.fantasyPowder == null)
            {
                loaded.fantasyPowder = new FantasyPowderSaveData();
                LastLoadMigratedData = true;
            }

            if (!hasSerializedStarLegacy || loaded.starLegacy == null)
            {
                loaded.starLegacy = new StarLegacySaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.starLegacy.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedNpcVisits || loaded.npcVisits == null)
            {
                loaded.npcVisits = new NpcVisitSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.npcVisits.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedMilkBlending || loaded.milkBlending == null)
            {
                loaded.milkBlending = new MilkBlendingSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.milkBlending.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedAutonomousLife || loaded.autonomousLife == null)
            {
                loaded.autonomousLife = new AutonomousLifeSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.autonomousLife.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedLateLevelGrowth || loaded.lateLevelGrowth == null)
            {
                loaded.lateLevelGrowth = new LateLevelGrowthSaveData();
                LastLoadMigratedData = true;
            }

            loaded.cheeseTama ??= new CheeseTama.Gameplay.CheeseTamaModel();
            loaded.cheeseTama.EnsureRuntimeDefaults();
            var lateLevelMigration = CheeseTama.Gameplay.Growth.LateLevelProgressMigration.EnsureCurrent(
                loaded.cheeseTama,
                loaded.lateLevelGrowth);
            if (lateLevelMigration.Changed)
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedSleepSchedule || loaded.sleepSchedule == null)
            {
                loaded.sleepSchedule = new SleepScheduleSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.sleepSchedule.EnsureRuntimeDefaults(DateTimeOffset.Now))
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedNpcRelationshipQuests || loaded.npcRelationshipQuests == null)
            {
                loaded.npcRelationshipQuests = new NpcRelationshipQuestSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.npcRelationshipQuests.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedNpcRelationshipEpisodes || loaded.npcRelationshipEpisodes == null)
            {
                loaded.npcRelationshipEpisodes = new NpcRelationshipEpisodeSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.npcRelationshipEpisodes.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedWeeklyCareJourney || loaded.weeklyCareJourney == null)
            {
                loaded.weeklyCareJourney = new WeeklyCareJourneySaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.weeklyCareJourney.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedDecorationWorkshop || loaded.decorationWorkshop == null)
            {
                loaded.decorationWorkshop = new DecorationWorkshopSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.decorationWorkshop.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedCollectionSetAlbum || loaded.collectionSetAlbum == null)
            {
                loaded.collectionSetAlbum = new CollectionSetAlbumSaveData();
                LastLoadMigratedData = true;
            }
            else if (loaded.collectionSetAlbum.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            if (loaded.fantasyPowder != null && loaded.fantasyPowder.EnsureRuntimeDefaults())
            {
                LastLoadMigratedData = true;
            }

            loaded.settings ??= new GameSettingsSaveData();
            if (!hasSerializedMusicVolume)
            {
                loaded.settings.musicVolume = 1f;
                LastLoadMigratedData = true;
            }

            if (!hasSerializedEffectVolume)
            {
                loaded.settings.effectVolume = 1f;
                LastLoadMigratedData = true;
            }

            if (!hasSerializedInputBindings || loaded.settings.inputBindings == null)
            {
                loaded.settings.inputBindings = new GameInputBindingSaveData();
                LastLoadMigratedData = true;
            }
            else if (CheeseTama.Gameplay.Input.GameInputBindingSystem.EnsureDefaults(
                         loaded.settings.inputBindings))
            {
                LastLoadMigratedData = true;
            }

            if (!hasSerializedGraphicsQuality)
            {
                loaded.settings.graphicsQualityPreset =
                    (int)CheeseTama.Environment.GraphicsQualityPreset.High;
                LastLoadMigratedData = true;
            }
            else
            {
                var normalizedGraphicsQuality =
                    CheeseTama.Environment.GraphicsQualityCatalog.Normalize(
                        loaded.settings.graphicsQualityPreset);
                if (loaded.settings.graphicsQualityPreset != (int)normalizedGraphicsQuality)
                {
                    loaded.settings.graphicsQualityPreset = (int)normalizedGraphicsQuality;
                    LastLoadMigratedData = true;
                }
            }

            if (!hasSerializedTextScale)
            {
                loaded.settings.textScale = GameSettingsSaveData.DefaultTextScale;
                LastLoadMigratedData = true;
            }

            if (!hasSerializedHighContrastUi)
            {
                loaded.settings.highContrastUi = false;
                LastLoadMigratedData = true;
            }

            if (!hasSerializedReduceMotion)
            {
                loaded.settings.reduceMotion = false;
                LastLoadMigratedData = true;
            }

            loaded.decorations ??= new DecorationSaveData();
            loaded.decorations.EnsureRuntimeDefaults();
            if (!hasSerializedDecorationPlacement || loaded.decorationPlacement == null)
            {
                loaded.decorationPlacement = DecorationPlacementSaveData.MigrateLegacySlots(
                    loaded.decorations.equippedAccentId,
                    loaded.decorations.equippedShelfId,
                    loaded.decorations.equippedBedsideId);
                LastLoadMigratedData = true;
            }
            else if (loaded.decorationPlacement.EnsureRuntimeDefaults(
                         loaded.decorations.equippedAccentId,
                         loaded.decorations.equippedShelfId,
                         loaded.decorations.equippedBedsideId))
            {
                LastLoadMigratedData = true;
            }

            loaded.EnsureRuntimeDefaults();
            if (!hasSerializedMilkGrowthRewardKeys
                || !hasSerializedDecorations
                || !hasSerializedPlayMiniGames)
            {
                LastLoadMigratedData = true;
            }

            return loaded;
        }

        public void Save(CheeseTamaSaveData saveData)
        {
            SaveInternal(saveData, true);
        }

        internal void SaveMigration(CheeseTamaSaveData saveData)
        {
            SaveWithoutAdvancingTimestamp(saveData);
        }

        internal void SaveWithoutAdvancingTimestamp(CheeseTamaSaveData saveData)
        {
            SaveInternal(saveData, false);
        }

        /// <summary>
        /// Validates and atomically commits an explicitly selected cloud copy. Invalid,
        /// oversized, mismatched-slot, or unreadable payloads never touch the local files.
        /// The existing primary becomes the recovery backup only after all validation passes.
        /// </summary>
        public bool TryReplaceFromCloudPayload(
            CloudSavePayload payload,
            out CheeseTamaSaveData restored)
        {
            restored = null;
            if (payload == null
                || !payload.IsValid()
                || !string.Equals(
                    payload.slotId,
                    CloudSaveSlotRules.PrimarySlotId,
                    StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(payload.contentJson)
                || Encoding.UTF8.GetByteCount(payload.contentJson) > MaximumCloudSavePayloadBytes)
            {
                return false;
            }

            if (!SaveTransferCodec.TryValidateAndNormalizeSaveContent(
                    payload.contentJson,
                    out var candidate,
                    out var normalizedJson,
                    out _,
                    out _)
                || Encoding.UTF8.GetByteCount(normalizedJson) > MaximumCloudSavePayloadBytes)
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath));
                WriteTextDurably(TemporaryFilePath, normalizedJson);
                CommitTemporaryFile();
                BrowserPersistence.RequestSync();
                restored = candidate;
                return true;
            }
            catch (IOException)
            {
                RecoverPrimaryAfterFailedCommit();
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                RecoverPrimaryAfterFailedCommit();
                return false;
            }
            catch (ArgumentException)
            {
                RecoverPrimaryAfterFailedCommit();
                return false;
            }
            catch (NotSupportedException)
            {
                RecoverPrimaryAfterFailedCommit();
                return false;
            }
            finally
            {
                TryDeleteTemporaryFileAfterCloudCommit();
            }
        }

        private void SaveInternal(CheeseTamaSaveData saveData, bool updateLastSavedAt)
        {
            if (saveData == null)
            {
                return;
            }

            var schemaMigration = SaveSchemaMigrationHub.Migrate(
                saveData,
                saveData.schemaRevision);
            if (!schemaMigration.Succeeded)
            {
                throw new SaveSchemaMigrationException(schemaMigration);
            }

            saveData.EnsureRuntimeDefaults();
            if (updateLastSavedAt)
            {
                saveData.cheeseTama.lastSavedAtIso = TimeUtility.NowIso();
            }

            var json = JsonUtility.ToJson(saveData, true);
            Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath));
            WriteTextDurably(TemporaryFilePath, json);
            CommitTemporaryFile();
            BrowserPersistence.RequestSync();
        }

        public bool DeleteSave()
        {
            var deletedAnyFile = DeleteFileIfPresent(SaveFilePath);
            deletedAnyFile |= DeleteFileIfPresent(BackupFilePath);
            deletedAnyFile |= DeleteFileIfPresent(TemporaryFilePath);

            var directoryPath = Path.GetDirectoryName(SaveFilePath);
            if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
            {
                var searchPattern = Path.GetFileName(SaveFilePath) + "*" + CorruptFileSuffix + ".*";
                foreach (var corruptFilePath in Directory.GetFiles(directoryPath, searchPattern))
                {
                    deletedAnyFile |= DeleteFileIfPresent(corruptFilePath);
                }
            }

            LastRecoveryReport = SaveRecoveryReport.NoRecovery;
            BrowserPersistence.RequestSync();
            return deletedAnyFile;
        }

        /// <summary>
        /// Removes only recovery artifacts after a confirmed full reset. The newly
        /// committed primary is never targeted, so it remains authoritative throughout.
        /// </summary>
        public bool TryPurgeRecoveryArtifacts()
        {
            try
            {
                DeleteFileIfPresent(BackupFilePath);
                DeleteFileIfPresent(TemporaryFilePath);

                var directoryPath = Path.GetDirectoryName(SaveFilePath);
                if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
                {
                    var searchPattern = Path.GetFileName(SaveFilePath)
                        + "*"
                        + CorruptFileSuffix
                        + ".*";
                    foreach (var corruptFilePath in Directory.GetFiles(directoryPath, searchPattern))
                    {
                        DeleteFileIfPresent(corruptFilePath);
                    }
                }

                LastRecoveryReport = SaveRecoveryReport.NoRecovery;
                return !File.Exists(BackupFilePath)
                    && !File.Exists(TemporaryFilePath)
                    && !HasCorruptRecoveryArtifacts();
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            finally
            {
                BrowserPersistence.RequestSync();
            }
        }

        public static CheeseTamaSaveData CreateDefaultSave()
        {
            var now = DateTimeOffset.Now.ToString("O");
            var save = new CheeseTamaSaveData();
            save.onboarding = OnboardingSaveData.CreateForNewPlayer();
            save.growthMilestone = GrowthMilestoneSaveData.CreateAcknowledged(
                CheeseTama.Gameplay.Growth.CheeseTamaGrowthStage.Egg);
            save.evolutionMilestone = EvolutionMilestoneSaveData.CreateAcknowledged(string.Empty);
            save.starRoute = StarRouteSaveData.CreateAcknowledged(false);
            save.newGameSetup = NewGameSetupSaveData.CreateForNewPlayer();
            save.firstDayJourney = FirstDayJourneySaveData.CreateForNewPlayer();
            save.EnsureRuntimeDefaults();
            save.cheeseTama.createdAtIso = now;
            save.cheeseTama.lastSavedAtIso = now;
            return save;
        }

        private static bool HasSerializedOnboardingField(string json)
        {
            return HasSerializedField(json, "onboarding");
        }

        private bool HasCorruptRecoveryArtifacts()
        {
            var directoryPath = Path.GetDirectoryName(SaveFilePath);
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return false;
            }

            var searchPattern = Path.GetFileName(SaveFilePath)
                + "*"
                + CorruptFileSuffix
                + ".*";
            return Directory.GetFiles(directoryPath, searchPattern).Length > 0;
        }

        private static bool HasSerializedField(string json, string fieldName)
        {
            var fieldToken = $"\"{fieldName}\"";
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            var searchIndex = 0;
            while (searchIndex < json.Length)
            {
                var fieldIndex = json.IndexOf(fieldToken, searchIndex, StringComparison.Ordinal);
                if (fieldIndex < 0)
                {
                    return false;
                }

                var separatorIndex = fieldIndex + fieldToken.Length;
                while (separatorIndex < json.Length && char.IsWhiteSpace(json[separatorIndex]))
                {
                    separatorIndex++;
                }

                if (separatorIndex < json.Length && json[separatorIndex] == ':')
                {
                    return true;
                }

                searchIndex = fieldIndex + fieldToken.Length;
            }

            return false;
        }

        private static bool TryReadSave(
            string filePath,
            out string json,
            out CheeseTamaSaveData saveData)
        {
            json = string.Empty;
            saveData = null;
            try
            {
                json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                if (!HasMinimumSerializedSaveSignature(json))
                {
                    return false;
                }

                saveData = JsonUtility.FromJson<CheeseTamaSaveData>(json);
                return saveData != null;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        private static bool HasMinimumSerializedSaveSignature(string json)
        {
            if (HasSerializedField(json, "cheeseTama"))
            {
                return HasSerializedObjectField(json, "cheeseTama");
            }

            // Revision-zero saves predate the numeric schema field, but every supported
            // historical save without a Tama field identifies both product and local player.
            // A present-but-null Tama and current schema without one stay on recovery.
            return !HasSerializedField(json, "schemaRevision")
                && HasSerializedField(json, "version")
                && HasSerializedField(json, "playerId");
        }

        private static bool HasSerializedObjectField(string json, string fieldName)
        {
            var fieldToken = $"\"{fieldName}\"";
            var fieldIndex = json?.IndexOf(fieldToken, StringComparison.Ordinal) ?? -1;
            if (fieldIndex < 0)
            {
                return false;
            }

            var valueIndex = fieldIndex + fieldToken.Length;
            while (valueIndex < json.Length && char.IsWhiteSpace(json[valueIndex]))
            {
                valueIndex += 1;
            }

            if (valueIndex >= json.Length || json[valueIndex] != ':')
            {
                return false;
            }

            valueIndex += 1;
            while (valueIndex < json.Length && char.IsWhiteSpace(json[valueIndex]))
            {
                valueIndex += 1;
            }

            return valueIndex < json.Length && json[valueIndex] == '{';
        }

        private void CommitTemporaryFile()
        {
            CommitTemporaryFile(SaveFilePath, BackupFilePath, TemporaryFilePath);
        }

        private static void CommitTemporaryFile(
            string primaryPath,
            string backupPath,
            string temporaryPath)
        {
            if (!File.Exists(primaryPath))
            {
                File.Move(temporaryPath, primaryPath);
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // Web uses Emscripten's virtual file system. File.Replace has no portable
            // atomic-replace contract there, so keep the existing backup-first fallback.
            CommitTemporaryFileWithoutReplace(primaryPath, backupPath, temporaryPath);
            return;
#else
            try
            {
                File.Replace(temporaryPath, primaryPath, backupPath, true);
            }
            catch (PlatformNotSupportedException)
            {
                CommitTemporaryFileWithoutReplace(primaryPath, backupPath, temporaryPath);
            }
#endif
        }

        private static void CommitTemporaryFileWithoutReplace(
            string primaryPath,
            string backupPath,
            string temporaryPath)
        {
            File.Copy(primaryPath, backupPath, true);
            File.Delete(primaryPath);
            File.Move(temporaryPath, primaryPath);
        }

        private void RecoverPrimaryAfterFailedCommit()
        {
            try
            {
                if (!File.Exists(SaveFilePath) && File.Exists(BackupFilePath))
                {
                    File.Copy(BackupFilePath, SaveFilePath, false);
                }
            }
            catch (IOException)
            {
                // The intact backup remains available to LoadOrCreate recovery.
            }
            catch (UnauthorizedAccessException)
            {
                // Recovery is best-effort; never replace an existing primary here.
            }
        }

        private void TryDeleteTemporaryFileAfterCloudCommit()
        {
            try
            {
                if (DeleteFileIfPresent(TemporaryFilePath))
                {
                    BrowserPersistence.RequestSync();
                }
            }
            catch (IOException)
            {
                // A valid primary or backup remains authoritative.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup must not change the cloud apply result.
            }
        }

        private void RestorePrimaryFromJson(string json)
        {
            WriteTextDurably(TemporaryFilePath, json);
            File.Move(TemporaryFilePath, SaveFilePath);
            BrowserPersistence.RequestSync();
        }

        private static void WriteTextDurably(string filePath, string contents)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true);
            writer.Write(contents);
            writer.Flush();
#if UNITY_WEBGL && !UNITY_EDITOR
            stream.Flush();
#else
            stream.Flush(true);
#endif
        }

        private static int QuarantineFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return 0;
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var candidatePath = filePath + CorruptFileSuffix + "." + timestamp;
            var collisionIndex = 1;
            while (File.Exists(candidatePath))
            {
                candidatePath = filePath + CorruptFileSuffix + "." + timestamp + "." + collisionIndex;
                collisionIndex += 1;
            }

            File.Move(filePath, candidatePath);
            return 1;
        }

        private void TryDeleteStaleTemporaryFile()
        {
            try
            {
                if (DeleteFileIfPresent(TemporaryFilePath))
                {
                    BrowserPersistence.RequestSync();
                }
            }
            catch (IOException)
            {
                // A committed primary save remains authoritative. A locked stale temp can be retried next load.
            }
            catch (UnauthorizedAccessException)
            {
                // Cleanup is best-effort and must not block a valid primary load.
            }
        }

        private static bool DeleteFileIfPresent(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            return true;
        }

        private static bool TryResolveLocalAccountPaths(
            string accountId,
            out string primaryPath,
            out string backupPath,
            out string temporaryPath)
        {
            primaryPath = string.Empty;
            backupPath = string.Empty;
            temporaryPath = string.Empty;
            if (!TryCreateLocalAccountSaveFileName(accountId, out var fileName))
            {
                return false;
            }

            var persistentRoot = Path.GetFullPath(Application.persistentDataPath);
            var candidate = Path.GetFullPath(Path.Combine(persistentRoot, fileName));
            var rootWithSeparator = persistentRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            primaryPath = candidate;
            backupPath = primaryPath + BackupFileSuffix;
            temporaryPath = primaryPath + TemporaryFileSuffix;
            return true;
        }

        private static bool HasLocalAccountArtifacts(
            string primaryPath,
            string backupPath,
            string temporaryPath)
        {
            return File.Exists(primaryPath)
                || File.Exists(backupPath)
                || File.Exists(temporaryPath)
                || HasCorruptArtifacts(primaryPath);
        }

        private static bool HasCorruptArtifacts(string primaryPath)
        {
            var directoryPath = Path.GetDirectoryName(primaryPath);
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return false;
            }

            var searchPattern = Path.GetFileName(primaryPath) + "*" + CorruptFileSuffix + ".*";
            return Directory.GetFiles(directoryPath, searchPattern).Length > 0;
        }

        private static void DeleteCorruptArtifacts(string primaryPath)
        {
            var directoryPath = Path.GetDirectoryName(primaryPath);
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return;
            }

            var searchPattern = Path.GetFileName(primaryPath) + "*" + CorruptFileSuffix + ".*";
            foreach (var corruptPath in Directory.GetFiles(directoryPath, searchPattern))
            {
                DeleteFileIfPresent(corruptPath);
            }
        }
    }
}
