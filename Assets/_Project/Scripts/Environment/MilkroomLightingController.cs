using UnityEngine;

namespace CheeseTama.Environment
{
    public sealed class MilkroomLightingController : MonoBehaviour
    {
        [SerializeField] private string currentThemeId = MilkroomThemeController.MorningThemeId;
        [SerializeField] private Light keyLight;
        [SerializeField] private Light fillLight;
        [SerializeField] private Light rimLight;
        [SerializeField] private Camera targetCamera;

        private void Awake()
        {
            CacheSceneReferences();
            ApplyTheme(currentThemeId);
        }

        public void ApplyTheme(string themeId)
        {
            currentThemeId = themeId;
            var palette = MilkroomThemePalette.For(themeId);
            CacheSceneReferences();

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(palette.Ambient, Color.white, 0.28f);

            if (keyLight != null)
            {
                keyLight.color = Color.Lerp(palette.Glow, Color.white, 0.12f);
                keyLight.intensity = themeId == MilkroomThemeController.NightThemeId ? 0.95f : 1.18f;
                keyLight.transform.position = new Vector3(-2.2f, 3.2f, -2.8f);
                keyLight.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            }

            if (fillLight != null)
            {
                fillLight.color = Color.Lerp(palette.WindowSky, Color.white, 0.12f);
                fillLight.intensity = themeId == MilkroomThemeController.NightThemeId ? 0.42f : 0.52f;
            }

            if (rimLight != null)
            {
                rimLight.color = Color.Lerp(palette.Celestial, new Color(1f, 0.82f, 0.38f), 0.35f);
                rimLight.intensity = themeId == MilkroomThemeController.NightThemeId ? 0.48f : 0.44f;
                rimLight.transform.rotation = Quaternion.Euler(32f, 208f, 0f);
            }

            if (targetCamera != null)
            {
                targetCamera.backgroundColor = palette.CameraBackground;
            }
        }

        private void CacheSceneReferences()
        {
            keyLight ??= GameObject.Find("Milkroom Key Light")?.GetComponent<Light>();
            fillLight ??= GameObject.Find("Milkroom Fill Light")?.GetComponent<Light>();
            rimLight ??= GameObject.Find("Milkroom Rim Light")?.GetComponent<Light>();
            targetCamera ??= Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        }
    }
}
