using UnityEngine;

namespace CheeseTama.Environment
{
    public sealed class MilkroomLightingController : MonoBehaviour
    {
        internal const float DayAmbientWhiteBlend = 0.32f;
        internal const float NightAmbientWhiteBlend = 0.12f;
        internal const float DayKeyIntensity = 0.36f;
        internal const float DayFillIntensity = 0.18f;
        internal const float DayRimIntensity = 0.07f;
        internal const float NightKeyIntensity = 0.28f;
        internal const float NightFillIntensity = 0.16f;
        internal const float NightRimIntensity = 0.1f;
        internal const float KeyWhiteBlend = 0.52f;
        internal const float FillWhiteBlend = 0.42f;
        internal const float KeyShadowStrength = 0.18f;
        internal const float KeyShadowBias = 0.05f;
        internal const float KeyShadowNormalBias = 0.35f;
        internal static readonly Vector3 KeyRotationEuler = new(52f, -28f, 0f);
        internal static readonly Vector3 FillRotationEuler = new(25f, 32f, 0f);
        internal static readonly Vector3 RimRotationEuler = new(32f, 208f, 0f);

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
            RenderSettings.ambientLight = ResolveAmbientColor(themeId, palette);

            if (keyLight != null)
            {
                keyLight.color = Color.Lerp(palette.Glow, Color.white, KeyWhiteBlend);
                keyLight.intensity = themeId == MilkroomThemeController.NightThemeId
                    ? NightKeyIntensity
                    : DayKeyIntensity;
                keyLight.shadows = LightShadows.Soft;
                keyLight.shadowStrength = KeyShadowStrength;
                keyLight.shadowBias = KeyShadowBias;
                keyLight.shadowNormalBias = KeyShadowNormalBias;
                keyLight.transform.position = new Vector3(-2.2f, 3.2f, -2.8f);
                keyLight.transform.rotation = Quaternion.Euler(KeyRotationEuler);
            }

            if (fillLight != null)
            {
                fillLight.color = Color.Lerp(palette.WindowSky, Color.white, FillWhiteBlend);
                fillLight.intensity = themeId == MilkroomThemeController.NightThemeId
                    ? NightFillIntensity
                    : DayFillIntensity;
                fillLight.shadows = LightShadows.None;
                fillLight.transform.rotation = Quaternion.Euler(FillRotationEuler);
            }

            if (rimLight != null)
            {
                rimLight.color = Color.Lerp(palette.Celestial, new Color(1f, 0.82f, 0.38f), 0.35f);
                rimLight.intensity = themeId == MilkroomThemeController.NightThemeId
                    ? NightRimIntensity
                    : DayRimIntensity;
                rimLight.shadows = LightShadows.None;
                rimLight.transform.rotation = Quaternion.Euler(RimRotationEuler);
            }

            if (targetCamera != null)
            {
                targetCamera.backgroundColor = palette.CameraBackground;
            }
        }

        internal static Color ResolveAmbientColor(string themeId, MilkroomThemePalette palette)
        {
            var whiteBlend = themeId == MilkroomThemeController.NightThemeId
                ? NightAmbientWhiteBlend
                : DayAmbientWhiteBlend;
            return Color.Lerp(palette.Ambient, Color.white, whiteBlend);
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
