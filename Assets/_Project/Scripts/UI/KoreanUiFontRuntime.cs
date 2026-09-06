using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public static class KoreanUiFontRuntime
    {
        public const string ResourcePath = "Fonts/NanumGothic-Regular";

        private static Font cachedFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            cachedFont = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyAfterInitialSceneLoad()
        {
            ApplyToLoadedTextComponents();
        }

        public static Font GetDefaultFont()
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            cachedFont = Resources.Load<Font>(ResourcePath);
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return cachedFont;
        }

        public static int ApplyToLoadedTextComponents()
        {
            var font = GetDefaultFont();
            if (font == null)
            {
                return 0;
            }

            var changed = 0;
            var labels = Resources.FindObjectsOfTypeAll<Text>();
            for (var index = 0; index < labels.Length; index++)
            {
                var label = labels[index];
                if (label == null
                    || !label.gameObject.scene.IsValid()
                    || label.font == font)
                {
                    continue;
                }

                label.font = font;
                label.SetAllDirty();
                changed++;
            }

            return changed;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToLoadedTextComponents();
        }
    }
}
