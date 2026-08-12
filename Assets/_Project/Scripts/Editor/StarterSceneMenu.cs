using CheeseTama.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CheeseTama.Editor
{
    public static class StarterSceneMenu
    {
        [MenuItem("CheeseTama/시작 씬 빌드")]
        public static void BuildStarterScenes()
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

        private static void BuildScene(string sceneName, System.Action build)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build.Invoke();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"Assets/_Project/Scenes/{sceneName}.unity");
        }
    }
}
