using CheeseTama.Core;
using CheeseTama.Save;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheeseTama.Editor
{
    public static class StarterSceneMenu
    {
        [MenuItem("CheeseTama/시작 씬 빌드")]
        public static void BuildStarterScenes()
        {
            string ownedDiagnosticSave = null;
            if (string.IsNullOrWhiteSpace(SaveManager.RuntimeDiagnosticSaveFileNameOverride))
            {
                ownedDiagnosticSave = SaveManager.CreateRuntimeDiagnosticSaveFileName();
                SaveManager.SetRuntimeDiagnosticSaveFileNameOverride(ownedDiagnosticSave);
            }

            try
            {
                BuildScene(SceneNames.Boot, StarterSceneBuilder.BuildBootScene);
                BuildScene(SceneNames.Milkroom, StarterSceneBuilder.BuildMilkroomScene);
                BuildScene(SceneNames.Collection, StarterSceneBuilder.BuildCollectionScene);
                BuildScene(SceneNames.Debug, StarterSceneBuilder.BuildDebugScene);

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene("Assets/_Project/Scenes/Boot.unity", true),
                    new EditorBuildSettingsScene("Assets/_Project/Scenes/Milkroom.unity", true),
                    new EditorBuildSettingsScene("Assets/_Project/Scenes/Collection.unity", true),
                    new EditorBuildSettingsScene("Assets/_Project/Scenes/Debug.unity", true)
                };

                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(ownedDiagnosticSave))
                {
                    SaveManager.ClearRuntimeDiagnosticSaveFileNameOverride(ownedDiagnosticSave);
                    DeleteOwnedDiagnosticSaveArtifacts(ownedDiagnosticSave);
                }
            }
        }

        private static void DeleteOwnedDiagnosticSaveArtifacts(string fileName)
        {
            if (!SaveManager.IsValidRuntimeDiagnosticSaveFileName(fileName))
            {
                return;
            }

            var persistentRoot = Path.GetFullPath(Application.persistentDataPath);
            var primaryPath = Path.GetFullPath(Path.Combine(persistentRoot, fileName));
            if (!string.Equals(
                    Path.GetDirectoryName(primaryPath),
                    persistentRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Diagnostic save cleanup escaped the persistent-data root.");
            }

            DeleteFileIfPresent(primaryPath);
            DeleteFileIfPresent(primaryPath + ".bak");
            DeleteFileIfPresent(primaryPath + ".tmp");
            if (!Directory.Exists(persistentRoot))
            {
                return;
            }

            foreach (var corruptPath in Directory.GetFiles(
                         persistentRoot,
                         fileName + "*.corrupt.*",
                         SearchOption.TopDirectoryOnly))
            {
                DeleteFileIfPresent(corruptPath);
            }
        }

        private static void DeleteFileIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void BuildScene(string sceneName, System.Action build)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build.Invoke();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"Assets/_Project/Scenes/{sceneName}.unity");
        }
    }
}
