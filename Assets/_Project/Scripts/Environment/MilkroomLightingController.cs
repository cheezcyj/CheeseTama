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
            RenderSettings.ambientLight = palette.Ambient;

            if (keyLight != null)
            {
                keyLight.color = Color.Lerp(palette.Glow, Color.white, 0.24f);
                keyLight.intensity = themeId == MilkroomThemeController.NightThemeId ? 1.15f : 1.35f;
                keyLight.transform.position = new Vector3(-2.2f, 3.2f, -2.8f);
                keyLight.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            }

            if (fillLight != null)
            {
                fillLight.color = Color.Lerp(palette.WindowSky, Color.white, 0.2f);
                fillLight.intensity = themeId == MilkroomThemeController.NightThemeId ? 0.55f : 0.72f;
            }

            if (rimLight != null)
            {
                rimLight.color = Color.Lerp(palette.Celestial, new Color(1f, 0.82f, 0.38f), 0.35f);
                rimLight.intensity = themeId == MilkroomThemeController.NightThemeId ? 0.72f : 0.62f;
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
