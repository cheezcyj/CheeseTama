using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheeseTama.Core
{
    public static class RuntimeBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            StarterSceneBuilder.EnsureCoreSystems();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildActiveScene()
        {
            StarterSceneBuilder.BuildForScene(SceneManager.GetActiveScene().name);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StarterSceneBuilder.BuildForScene(scene.name);
        }
    }
}
