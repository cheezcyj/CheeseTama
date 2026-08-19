using System;
using System.IO;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    [SetUpFixture]
    public sealed class EditModeSaveIsolationBootstrap
    {
        private static string isolatedFileName;
        private static string previousSaveFileNameOverride;
        private static string previousEnvironmentSaveFileName;

        [OneTimeSetUp]
        public void BeginIsolatedEditModeRun()
        {
            previousSaveFileNameOverride = SaveManager.PlayModeTestSaveFileNameOverride;
            previousEnvironmentSaveFileName = System.Environment.GetEnvironmentVariable(
                SaveManager.PlayModeTestSaveFileNameEnvironmentVariable);
            isolatedFileName =
                $"{SaveManager.PlayModeTestSaveFileNamePrefix}{Guid.NewGuid():N}.json";

            DeleteOwnedArtifacts();
            SaveManager.SetPlayModeTestSaveFileNameOverride(isolatedFileName);
            System.Environment.SetEnvironmentVariable(
                SaveManager.PlayModeTestSaveFileNameEnvironmentVariable,
                isolatedFileName);
        }

        [OneTimeTearDown]
        public void EndIsolatedEditModeRun()
        {
            try
            {
                DeleteOwnedArtifacts();
            }
            finally
            {
                var activeOverride = SaveManager.PlayModeTestSaveFileNameOverride;
                if (!string.IsNullOrWhiteSpace(activeOverride))
                {
                    SaveManager.ClearPlayModeTestSaveFileNameOverride(activeOverride);
                }

                if (!string.IsNullOrWhiteSpace(previousSaveFileNameOverride))
                {
                    SaveManager.SetPlayModeTestSaveFileNameOverride(
                        previousSaveFileNameOverride);
                }

                System.Environment.SetEnvironmentVariable(
                    SaveManager.PlayModeTestSaveFileNameEnvironmentVariable,
                    previousEnvironmentSaveFileName);
                isolatedFileName = null;
                previousSaveFileNameOverride = null;
                previousEnvironmentSaveFileName = null;
            }
        }

        private static void DeleteOwnedArtifacts()
        {
            if (!SaveManager.IsValidPlayModeTestSaveFileName(isolatedFileName))
            {
                return;
            }

            var basePath = Path.Combine(Application.persistentDataPath, isolatedFileName);
            DeleteFileIfPresent(basePath);
            DeleteFileIfPresent(basePath + ".bak");
            DeleteFileIfPresent(basePath + ".tmp");

            var directoryPath = Path.GetDirectoryName(basePath);
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return;
            }

            var searchPattern = Path.GetFileName(basePath) + "*.corrupt.*";
            foreach (var corruptFilePath in Directory.GetFiles(directoryPath, searchPattern))
            {
                DeleteFileIfPresent(corruptFilePath);
            }
        }

        private static void DeleteFileIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
