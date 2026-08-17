using System;
using System.IO;
using System.Text;
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

        [SerializeField] private string saveFileName = DefaultSaveFileName;

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, ResolveSaveFileName());
        public string BackupFilePath => SaveFilePath + BackupFileSuffix;
        public string TemporaryFilePath => SaveFilePath + TemporaryFileSuffix;
        public bool HasSaveFile => File.Exists(SaveFilePath);
        public bool LastLoadMigratedData { get; private set; }
        public SaveRecoveryReport LastRecoveryReport { get; private set; } = SaveRecoveryReport.NoRecovery;

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
            return saveFileName;
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
            var hasSerializedOnboarding = HasSerializedOnboardingField(json);
            var hasSerializedGrowthMilestone = HasSerializedField(json, "growthMilestone");
            var hasSerializedEvolutionMilestone = HasSerializedField(json, "evolutionMilestone");
            var hasSerializedMilkGrowthRewardKeys = HasSerializedField(json, "claimedMilkGrowthRewardKeys");
            var hasSerializedDecorations = HasSerializedField(json, "decorations");
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
            var hasSerializedMusicVolume = HasSerializedField(json, "musicVolume");
            var hasSerializedEffectVolume = HasSerializedField(json, "effectVolume");
            var hasSerializedInputBindings = HasSerializedField(json, "inputBindings");

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
            SaveInternal(saveData, false);
        }

        private void SaveInternal(CheeseTamaSaveData saveData, bool updateLastSavedAt)
        {
            if (saveData == null)
            {
                return;
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
            return deletedAnyFile;
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

        private void CommitTemporaryFile()
        {
            if (!File.Exists(SaveFilePath))
            {
                File.Move(TemporaryFilePath, SaveFilePath);
                return;
            }

            try
            {
                File.Replace(TemporaryFilePath, SaveFilePath, BackupFilePath, true);
            }
            catch (PlatformNotSupportedException)
            {
                CommitTemporaryFileWithoutReplace();
            }
        }

        private void CommitTemporaryFileWithoutReplace()
        {
            File.Copy(SaveFilePath, BackupFilePath, true);
            File.Delete(SaveFilePath);
            File.Move(TemporaryFilePath, SaveFilePath);
        }

        private void RestorePrimaryFromJson(string json)
        {
            WriteTextDurably(TemporaryFilePath, json);
            File.Move(TemporaryFilePath, SaveFilePath);
        }

        private static void WriteTextDurably(string filePath, string contents)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true);
            writer.Write(contents);
            writer.Flush();
            stream.Flush(true);
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
                DeleteFileIfPresent(TemporaryFilePath);
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
    }
}
