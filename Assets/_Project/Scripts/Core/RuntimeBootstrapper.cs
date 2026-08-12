using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheeseTama.Core
{
    public static class RuntimeBootstrapper
    {
        private static int preparedSceneHandle = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            preparedSceneHandle = -1;
            StarterSceneBuilder.EnsureCoreSystems();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildActiveScene()
        {
            PrepareScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PrepareScene(scene);
        }

        private static void PrepareScene(Scene scene)
        {
            if (!scene.IsValid() || scene.handle == preparedSceneHandle)
            {
                return;
            }

            if (StarterSceneBuilder.TryBindExistingSceneForRuntime(scene.name))
            {
                preparedSceneHandle = scene.handle;
                return;
            }

            StarterSceneBuilder.BuildForScene(scene.name);
            preparedSceneHandle = scene.handle;
        }
    }
}
