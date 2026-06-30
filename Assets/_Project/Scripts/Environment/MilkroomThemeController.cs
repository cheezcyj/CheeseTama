using CheeseTama.Utilities;
using UnityEngine;

namespace CheeseTama.Environment
{
    public sealed class MilkroomThemeController : MonoBehaviour
    {
        public const string MorningThemeId = "milkroom_morning";
        public const string EveningThemeId = "milkroom_evening";
        public const string NightThemeId = "milkroom_night";
        public const string RainyThemeId = "milkroom_rainy";

        [SerializeField] private string currentThemeId = MorningThemeId;
        [SerializeField] private Transform backgroundRoot;
        [SerializeField] private Transform midgroundRoot;
        [SerializeField] private Transform playAreaRoot;
        [SerializeField] private Transform foregroundRoot;
        [SerializeField] private Transform themeVfxRoot;

        public string CurrentThemeId => currentThemeId;

        private void Awake()
        {
            CacheGroupRoots();
            ApplyCurrentTheme();
        }

        public void Configure(
            Transform background,
            Transform midground,
            Transform playArea,
            Transform foreground,
            Transform themeVfx)
        {
            backgroundRoot = background;
            midgroundRoot = midground;
            playAreaRoot = playArea;
            foregroundRoot = foreground;
            themeVfxRoot = themeVfx;
            ApplyCurrentTheme();
        }

        public void ApplyCurrentTheme()
        {
            ApplyTheme(currentThemeId);
        }

        public void ApplyTheme(string themeId)
        {
            currentThemeId = NormalizeThemeId(themeId);
            var palette = MilkroomThemePalette.For(currentThemeId);

            PaintGroup(backgroundRoot, palette);
            PaintGroup(midgroundRoot, palette);
            PaintGroup(playAreaRoot, palette);
            PaintGroup(foregroundRoot, palette);
            PaintGroup(themeVfxRoot, palette);
            SetThemeVfxVisibility(currentThemeId);
        }

        private void CacheGroupRoots()
        {
            backgroundRoot ??= transform.Find("BackgroundRoot");
            midgroundRoot ??= transform.Find("MidgroundRoot");
            playAreaRoot ??= transform.Find("PlayAreaRoot");
            foregroundRoot ??= transform.Find("ForegroundRoot");
            themeVfxRoot ??= transform.Find("ThemeVFXRoot");
        }

        private void PaintGroup(Transform groupRoot, MilkroomThemePalette palette)
        {
            if (groupRoot == null)
            {
                return;
            }

            var renderers = groupRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                PaintRenderer(renderer, ResolveColor(renderer.name, palette));
            }
        }

        private Color ResolveColor(string objectName, MilkroomThemePalette palette)
        {
            if (objectName.Contains("Wall Wash") || objectName.Contains("Glow") || objectName.Contains("Lamp"))
            {
                return palette.Glow;
            }

            if (objectName.Contains("Wall") || objectName.Contains("Curtain Tie"))
            {
                return palette.Wall;
            }

            if (objectName.Contains("Floor") || objectName.Contains("Plank") || objectName.Contains("Seam"))
            {
                return objectName.Contains("Line") || objectName.Contains("Seam") ? palette.FloorLine : palette.Floor;
            }

            if (objectName.Contains("Rug"))
            {
                return objectName.Contains("Paw") ? palette.RugMark : palette.Rug;
            }

            if (objectName.Contains("Window Sky"))
            {
                return palette.WindowSky;
            }

            if (objectName.Contains("Window Sun") || objectName.Contains("Moon"))
            {
                return palette.Celestial;
            }

            if (objectName.Contains("Cloud") || objectName.Contains("Rain"))
            {
                return palette.Weather;
            }

            if (objectName.Contains("Curtain"))
            {
                return palette.Curtain;
            }

            if (objectName.Contains("Bottle") || objectName.Contains("Milk Drop") || objectName.Contains("Jar"))
            {
                return objectName.Contains("Cap") ? palette.MilkBlue : palette.MilkGlass;
            }

            if (objectName.Contains("Plant") || objectName.Contains("Leaf"))
            {
                return palette.Plant;
            }

            if (objectName.Contains("Cheese Body") || objectName.Contains("Cheese Block"))
            {
                return palette.CheeseAccent;
            }

            if (objectName.Contains("Cheese Hole"))
            {
                return palette.CheeseHole;
            }

            if (objectName.Contains("Chalkboard"))
            {
                return objectName.Contains("Frame") ? palette.Wood : palette.Chalkboard;
            }

            if (objectName.Contains("Fridge"))
            {
                return objectName.Contains("Face") || objectName.Contains("Handle") ? palette.Detail : palette.Fridge;
            }

            if (objectName.Contains("Chair") || objectName.Contains("Dresser") || objectName.Contains("Shelf") || objectName.Contains("Drawer") || objectName.Contains("Frame") || objectName.Contains("Handle"))
            {
                return palette.Wood;
            }

            if (objectName.Contains("Star") || objectName.Contains("Sparkle"))
            {
                return palette.Celestial;
            }

            return palette.Detail;
        }

        private void PaintRenderer(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            ToonMaterialUtility.Apply(renderer, ToonMaterialUtility.InferProfile(renderer), color);
        }

        private void SetThemeVfxVisibility(string themeId)
        {
            if (themeVfxRoot == null)
            {
                return;
            }

            var showNight = themeId == NightThemeId;
            var showRain = themeId == RainyThemeId;
            var showEvening = themeId == EveningThemeId;

            foreach (Transform child in themeVfxRoot)
            {
                var childName = child.name;
                var active =
                    childName.Contains("Rain") && showRain ||
                    childName.Contains("Night") && showNight ||
                    childName.Contains("Star") && showNight ||
                    childName.Contains("Evening") && showEvening;

                child.gameObject.SetActive(active);
            }
        }

        private static string NormalizeThemeId(string themeId)
        {
            return themeId switch
            {
                EveningThemeId => EveningThemeId,
                NightThemeId => NightThemeId,
                RainyThemeId => RainyThemeId,
                _ => MorningThemeId
            };
        }
    }

    public readonly struct MilkroomThemePalette
    {
        public readonly Color Wall;
        public readonly Color Glow;
        public readonly Color Floor;
        public readonly Color FloorLine;
        public readonly Color Rug;
        public readonly Color RugMark;
        public readonly Color WindowSky;
        public readonly Color Celestial;
        public readonly Color Weather;
        public readonly Color Curtain;
        public readonly Color MilkGlass;
        public readonly Color MilkBlue;
        public readonly Color Plant;
        public readonly Color CheeseAccent;
        public readonly Color CheeseHole;
        public readonly Color Wood;
        public readonly Color Chalkboard;
        public readonly Color Fridge;
        public readonly Color Detail;
        public readonly Color Ambient;
        public readonly Color CameraBackground;

        private MilkroomThemePalette(
            Color wall,
            Color glow,
            Color floor,
            Color floorLine,
            Color rug,
            Color rugMark,
            Color windowSky,
            Color celestial,
            Color weather,
            Color curtain,
            Color milkGlass,
            Color milkBlue,
            Color plant,
            Color cheeseAccent,
            Color cheeseHole,
            Color wood,
            Color chalkboard,
            Color fridge,
            Color detail,
            Color ambient,
            Color cameraBackground)
        {
            Wall = wall;
            Glow = glow;
            Floor = floor;
            FloorLine = floorLine;
            Rug = rug;
            RugMark = rugMark;
            WindowSky = windowSky;
            Celestial = celestial;
            Weather = weather;
            Curtain = curtain;
            MilkGlass = milkGlass;
            MilkBlue = milkBlue;
            Plant = plant;
            CheeseAccent = cheeseAccent;
            CheeseHole = cheeseHole;
            Wood = wood;
            Chalkboard = chalkboard;
            Fridge = fridge;
            Detail = detail;
            Ambient = ambient;
            CameraBackground = cameraBackground;
        }

        public static MilkroomThemePalette For(string themeId)
        {
            return themeId switch
            {
                MilkroomThemeController.EveningThemeId => new MilkroomThemePalette(
                    new Color(0.72f, 0.48f, 0.32f),
                    new Color(1f, 0.57f, 0.24f),
                    new Color(0.5f, 0.28f, 0.16f),
                    new Color(0.36f, 0.2f, 0.12f),
                    new Color(0.9f, 0.68f, 0.45f),
                    new Color(0.72f, 0.47f, 0.28f),
                    new Color(0.72f, 0.36f, 0.62f),
                    new Color(1f, 0.62f, 0.25f),
                    new Color(0.95f, 0.52f, 0.42f),
                    new Color(0.92f, 0.7f, 0.58f),
                    new Color(0.83f, 0.92f, 0.94f),
                    new Color(0.44f, 0.64f, 0.82f),
                    new Color(0.36f, 0.58f, 0.34f),
                    new Color(1f, 0.67f, 0.2f),
                    new Color(0.86f, 0.44f, 0.08f),
                    new Color(0.58f, 0.32f, 0.18f),
                    new Color(0.15f, 0.23f, 0.18f),
                    new Color(0.94f, 0.82f, 0.66f),
                    new Color(0.39f, 0.22f, 0.12f),
                    new Color(0.78f, 0.48f, 0.28f),
                    new Color(0.88f, 0.72f, 0.54f)),
                MilkroomThemeController.NightThemeId => new MilkroomThemePalette(
                    new Color(0.22f, 0.3f, 0.48f),
                    new Color(0.54f, 0.72f, 1f),
                    new Color(0.19f, 0.22f, 0.32f),
                    new Color(0.12f, 0.14f, 0.22f),
                    new Color(0.58f, 0.62f, 0.82f),
                    new Color(0.42f, 0.46f, 0.66f),
                    new Color(0.04f, 0.12f, 0.34f),
                    new Color(0.78f, 0.88f, 1f),
                    new Color(0.7f, 0.78f, 0.95f),
                    new Color(0.62f, 0.7f, 0.9f),
                    new Color(0.65f, 0.8f, 0.92f),
                    new Color(0.36f, 0.58f, 0.92f),
                    new Color(0.25f, 0.43f, 0.36f),
                    new Color(0.8f, 0.55f, 0.22f),
                    new Color(0.54f, 0.28f, 0.1f),
                    new Color(0.32f, 0.2f, 0.18f),
                    new Color(0.07f, 0.12f, 0.2f),
                    new Color(0.7f, 0.72f, 0.78f),
                    new Color(0.12f, 0.08f, 0.06f),
                    new Color(0.22f, 0.27f, 0.45f),
                    new Color(0.16f, 0.2f, 0.32f)),
                MilkroomThemeController.RainyThemeId => new MilkroomThemePalette(
                    new Color(0.5f, 0.58f, 0.64f),
                    new Color(1f, 0.74f, 0.42f),
                    new Color(0.36f, 0.38f, 0.36f),
                    new Color(0.23f, 0.25f, 0.25f),
                    new Color(0.72f, 0.7f, 0.62f),
                    new Color(0.52f, 0.52f, 0.48f),
                    new Color(0.28f, 0.45f, 0.6f),
                    new Color(0.86f, 0.86f, 0.78f),
                    new Color(0.62f, 0.74f, 0.82f),
                    new Color(0.72f, 0.76f, 0.78f),
                    new Color(0.74f, 0.88f, 0.94f),
                    new Color(0.38f, 0.62f, 0.82f),
                    new Color(0.29f, 0.5f, 0.36f),
                    new Color(0.95f, 0.6f, 0.18f),
                    new Color(0.7f, 0.34f, 0.08f),
                    new Color(0.42f, 0.31f, 0.24f),
                    new Color(0.12f, 0.2f, 0.18f),
                    new Color(0.76f, 0.8f, 0.76f),
                    new Color(0.24f, 0.16f, 0.1f),
                    new Color(0.44f, 0.5f, 0.56f),
                    new Color(0.68f, 0.74f, 0.78f)),
                _ => new MilkroomThemePalette(
                    new Color(0.82f, 0.61f, 0.42f),
                    new Color(1f, 0.82f, 0.48f),
                    new Color(0.58f, 0.34f, 0.18f),
                    new Color(0.42f, 0.24f, 0.13f),
                    new Color(0.92f, 0.82f, 0.63f),
                    new Color(0.84f, 0.7f, 0.5f),
                    new Color(0.64f, 0.83f, 0.95f),
                    new Color(1f, 0.86f, 0.38f),
                    new Color(0.96f, 0.98f, 1f),
                    new Color(1f, 0.91f, 0.76f),
                    new Color(0.84f, 0.94f, 0.98f),
                    new Color(0.47f, 0.72f, 0.9f),
                    new Color(0.38f, 0.64f, 0.38f),
                    new Color(1f, 0.72f, 0.18f),
                    new Color(0.85f, 0.48f, 0.09f),
                    new Color(0.68f, 0.42f, 0.22f),
                    new Color(0.18f, 0.28f, 0.21f),
                    new Color(1f, 0.95f, 0.84f),
                    new Color(0.32f, 0.18f, 0.1f),
                    new Color(0.86f, 0.74f, 0.56f),
                    new Color(0.96f, 0.92f, 0.84f))
            };
        }
    }
}
