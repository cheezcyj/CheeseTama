using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class SaveRecoveryFeatureTests
    {
        [Test]
        public void RepeatedSaveKeepsPreviousCommittedSaveAsBackup()
        {
            using var fixture = IsolatedSaveFixture.Create();
            var first = SaveManager.CreateDefaultSave();
            first.economy.milkCoins = 17;
            fixture.Manager.Save(first);

            var second = SaveManager.CreateDefaultSave();
            second.economy.milkCoins = 29;
            fixture.Manager.Save(second);

            Assert.That(ReadSave(fixture.Manager.SaveFilePath).economy.milkCoins, Is.EqualTo(29));
            Assert.That(ReadSave(fixture.Manager.BackupFilePath).economy.milkCoins, Is.EqualTo(17));
            Assert.That(File.Exists(fixture.Manager.TemporaryFilePath), Is.False);
        }

        [Test]
        public void CorruptPrimaryLoadsBackupAndPreservesCorruptArtifact()
        {
            using var fixture = IsolatedSaveFixture.Create();
            var recoverable = SaveManager.CreateDefaultSave();
            recoverable.economy.milkCoins = 41;
            fixture.Manager.Save(recoverable);

            var latest = SaveManager.CreateDefaultSave();
            latest.economy.milkCoins = 73;
            fixture.Manager.Save(latest);
            File.WriteAllText(fixture.Manager.SaveFilePath, "{broken-primary");

            var loaded = fixture.Manager.LoadOrCreate();

            Assert.That(loaded.economy.milkCoins, Is.EqualTo(41));
            Assert.That(fixture.Manager.LastRecoveryReport.Outcome,
                Is.EqualTo(SaveRecoveryOutcome.RecoveredFromBackup));
            Assert.That(fixture.Manager.LastRecoveryReport.RecoveredExistingData, Is.True);
            Assert.That(fixture.Manager.LastRecoveryReport.QuarantinedFileCount, Is.EqualTo(1));
            Assert.That(ReadSave(fixture.Manager.SaveFilePath).economy.milkCoins, Is.EqualTo(41));
            Assert.That(ReadSave(fixture.Manager.BackupFilePath).economy.milkCoins, Is.EqualTo(41));
            Assert.That(fixture.FindCorruptArtifacts().Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidInterruptedTemporarySaveIsPreferredOverOlderBackup()
        {
            using var fixture = IsolatedSaveFixture.Create();
            var backup = SaveManager.CreateDefaultSave();
            backup.economy.milkCoins = 10;
            fixture.Manager.Save(backup);

            var current = SaveManager.CreateDefaultSave();
            current.economy.milkCoins = 20;
            fixture.Manager.Save(current);

            var interrupted = SaveManager.CreateDefaultSave();
            interrupted.economy.milkCoins = 30;
            File.WriteAllText(
                fixture.Manager.TemporaryFilePath,
                JsonUtility.ToJson(interrupted, true));
            File.WriteAllText(fixture.Manager.SaveFilePath, "{broken-primary");

            var loaded = fixture.Manager.LoadOrCreate();

            Assert.That(loaded.economy.milkCoins, Is.EqualTo(30));
            Assert.That(fixture.Manager.LastRecoveryReport.Outcome,
                Is.EqualTo(SaveRecoveryOutcome.RecoveredFromTemporaryFile));
            Assert.That(File.Exists(fixture.Manager.TemporaryFilePath), Is.False);
            Assert.That(ReadSave(fixture.Manager.SaveFilePath).economy.milkCoins, Is.EqualTo(30));
            Assert.That(ReadSave(fixture.Manager.BackupFilePath).economy.milkCoins, Is.EqualTo(10));
        }

        [Test]
        public void UnrecoverableArtifactsAreQuarantinedBeforeFreshSaveIsCreated()
        {
            using var fixture = IsolatedSaveFixture.Create();
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.Manager.SaveFilePath));
            File.WriteAllText(fixture.Manager.SaveFilePath, "{broken-primary");
            File.WriteAllText(fixture.Manager.BackupFilePath, "{broken-backup");
            File.WriteAllText(fixture.Manager.TemporaryFilePath, "{broken-temporary");

            var loaded = fixture.Manager.LoadOrCreate();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(fixture.Manager.LastRecoveryReport.Outcome,
                Is.EqualTo(SaveRecoveryOutcome.CreatedFreshSaveAfterCorruption));
            Assert.That(fixture.Manager.LastRecoveryReport.RecoveredExistingData, Is.False);
            Assert.That(fixture.Manager.LastRecoveryReport.UserNotificationRecommended, Is.True);
            Assert.That(fixture.Manager.LastRecoveryReport.QuarantinedFileCount, Is.EqualTo(3));
            Assert.That(fixture.FindCorruptArtifacts().Count, Is.EqualTo(3));
            Assert.That(ReadSave(fixture.Manager.SaveFilePath), Is.Not.Null);
        }

        [Test]
        public void DeleteSaveRemovesPrimaryBackupTemporaryAndQuarantinedArtifacts()
        {
            using var fixture = IsolatedSaveFixture.Create();
            var first = SaveManager.CreateDefaultSave();
            fixture.Manager.Save(first);
            fixture.Manager.Save(first);
            File.WriteAllText(fixture.Manager.TemporaryFilePath, "stale");
            File.WriteAllText(fixture.Manager.SaveFilePath + ".corrupt.test", "quarantined");

            Assert.That(fixture.Manager.DeleteSave(), Is.True);

            Assert.That(fixture.FindAllArtifacts(), Is.Empty);
            Assert.That(fixture.Manager.LastRecoveryReport.Outcome, Is.EqualTo(SaveRecoveryOutcome.None));
        }

        private static CheeseTamaSaveData ReadSave(string filePath)
        {
            var save = JsonUtility.FromJson<CheeseTamaSaveData>(File.ReadAllText(filePath));
            save?.EnsureRuntimeDefaults();
            return save;
        }

        private sealed class IsolatedSaveFixture : IDisposable
        {
            private const string TestFilePrefix = "cheesetama_recovery_test_";

            private readonly GameObject root;
            private readonly string fileName;

            private IsolatedSaveFixture(GameObject root, SaveManager manager, string fileName)
            {
                this.root = root;
                this.fileName = fileName;
                Manager = manager;
            }

            public SaveManager Manager { get; }

            public static IsolatedSaveFixture Create()
            {
                var root = new GameObject("Save Recovery Test Fixture");
                root.SetActive(false);
                var manager = root.AddComponent<SaveManager>();
                var fileName = TestFilePrefix + Guid.NewGuid().ToString("N") + ".json";
                var fileNameField = typeof(SaveManager).GetField(
                    "saveFileName",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(fileNameField, Is.Not.Null);
                fileNameField.SetValue(manager, fileName);
                return new IsolatedSaveFixture(root, manager, fileName);
            }

            public IReadOnlyList<string> FindCorruptArtifacts()
            {
                var directoryPath = Path.GetDirectoryName(Manager.SaveFilePath);
                return Directory.Exists(directoryPath)
                    ? Directory.GetFiles(directoryPath, fileName + "*.corrupt.*")
                    : Array.Empty<string>();
            }

            public IReadOnlyList<string> FindAllArtifacts()
            {
                var directoryPath = Path.GetDirectoryName(Manager.SaveFilePath);
                return Directory.Exists(directoryPath)
                    ? Directory.GetFiles(directoryPath, fileName + "*")
                    : Array.Empty<string>();
            }

            public void Dispose()
            {
                foreach (var artifactPath in FindAllArtifacts())
                {
                    File.Delete(artifactPath);
                }

                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
