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
        public const string StarlightThemeId = "milkroom_starlight";
        public const string WinterThemeId = "milkroom_winter";
        public const string VintageThemeId = "milkroom_vintage";

        internal const float RoomWallValueScale = 1f;
        internal const float RoomFloorValueScale = 0.55f / 0.5f;

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

        public void ApplyCurrentThemeToRenderer(Renderer renderer)
        {
            if (renderer == null || ShouldPreserveImportedRenderer(renderer))
            {
                return;
            }

            var palette = MilkroomThemePalette.For(currentThemeId);
            var color = ResolveColor(renderer.name, palette);
            PaintRenderer(renderer, AdjustRoomShellColor(renderer, color));
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
                if (ShouldPreserveImportedRenderer(renderer))
                {
                    continue;
                }

                var color = ResolveColor(renderer.name, palette);
                PaintRenderer(renderer, AdjustRoomShellColor(renderer, color));
            }
        }

        private static Color AdjustRoomShellColor(Renderer renderer, Color color)
        {
            if (!IsUnderNamedAncestor(renderer, "RoomShell"))
            {
                return color;
            }

            var objectName = renderer.name;
            if (objectName.Contains("Wall") && !objectName.Contains("Wall Wash"))
            {
                return ScaleValuePreservingHueAndSaturation(color, RoomWallValueScale);
            }

            if (objectName.Contains("Floor") || objectName.Contains("Plank") || objectName.Contains("Seam"))
            {
                return ScaleValuePreservingHueAndSaturation(color, RoomFloorValueScale);
            }

            return color;
        }

        private static bool IsUnderNamedAncestor(Renderer renderer, string ancestorName)
        {
            var current = renderer != null ? renderer.transform : null;
            while (current != null)
            {
                if (current.name == ancestorName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Color ScaleValuePreservingHueAndSaturation(Color color, float scale)
        {
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);
            var adjusted = Color.HSVToRGB(hue, saturation, Mathf.Clamp01(value * scale));
            adjusted.a = color.a;
            return adjusted;
        }

        private static bool ShouldPreserveImportedRenderer(Renderer renderer)
        {
            var current = renderer != null ? renderer.transform : null;
            while (current != null)
            {
                var objectName = current.name;
                if (objectName == "GeneratedModel"
                    || objectName == "Fridge_Model"
                    || objectName == "MilkShelf_Model"
                    || objectName == "CozyChair_Model"
                    || objectName == "Window_Model"
                    || objectName == "Rug_Model"
                    || objectName == "DresserTable_Model"
                    || objectName == "Chalkboard_Model")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
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

            if (objectName.Contains("Window Sky") || objectName.Contains("WindowGlass") || objectName.Contains("Window Arch Inner Cut"))
            {
                return palette.WindowSky;
            }

            if (objectName.Contains("Window Sun") || objectName.Contains("Moon"))
            {
                return palette.Celestial;
            }

            if (objectName.Contains("Cloud") || objectName.Contains("Rain") || objectName.Contains("Snow"))
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

            if (objectName.Contains("Star") || objectName.Contains("Sparkle") || objectName.Contains("Dust"))
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
            var showStarlight = themeId == StarlightThemeId;
            var showWinter = themeId == WinterThemeId;
            var showVintage = themeId == VintageThemeId;

            foreach (Transform child in themeVfxRoot)
            {
                var childName = child.name;
                var active =
                    childName.Contains("Rain") && showRain ||
                    childName.Contains("Night") && (showNight || showStarlight) ||
                    childName.Contains("Starlight") && showStarlight ||
                    childName.Contains("Evening") && showEvening ||
                    childName.Contains("Winter") && showWinter ||
                    childName.Contains("Vintage") && showVintage;

                child.gameObject.SetActive(active);
            }
        }

        private static string NormalizeThemeId(string themeId)
        {
            return MilkroomThemeCatalog.Normalize(themeId);
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
                MilkroomThemeController.StarlightThemeId => new MilkroomThemePalette(
                    new Color(0.26f, 0.2f, 0.46f),
                    new Color(0.72f, 0.52f, 1f),
                    new Color(0.18f, 0.15f, 0.3f),
                    new Color(0.1f, 0.08f, 0.18f),
                    new Color(0.5f, 0.42f, 0.72f),
                    new Color(0.72f, 0.62f, 0.92f),
                    new Color(0.03f, 0.05f, 0.2f),
                    new Color(0.86f, 0.78f, 1f),
                    new Color(0.58f, 0.62f, 0.88f),
                    new Color(0.5f, 0.42f, 0.7f),
                    new Color(0.68f, 0.78f, 0.94f),
                    new Color(0.46f, 0.62f, 0.96f),
                    new Color(0.24f, 0.38f, 0.36f),
                    new Color(0.88f, 0.66f, 0.28f),
                    new Color(0.58f, 0.34f, 0.12f),
                    new Color(0.34f, 0.22f, 0.28f),
                    new Color(0.08f, 0.1f, 0.22f),
                    new Color(0.66f, 0.68f, 0.82f),
                    new Color(0.16f, 0.1f, 0.18f),
                    new Color(0.24f, 0.2f, 0.46f),
                    new Color(0.11f, 0.09f, 0.25f)),
                MilkroomThemeController.WinterThemeId => new MilkroomThemePalette(
                    new Color(0.72f, 0.8f, 0.88f),
                    new Color(1f, 0.78f, 0.38f),
                    new Color(0.38f, 0.45f, 0.52f),
                    new Color(0.24f, 0.31f, 0.38f),
                    new Color(0.8f, 0.84f, 0.86f),
                    new Color(0.56f, 0.68f, 0.78f),
                    new Color(0.46f, 0.68f, 0.86f),
                    new Color(1f, 0.9f, 0.62f),
                    new Color(0.9f, 0.95f, 1f),
                    new Color(0.86f, 0.9f, 0.94f),
                    new Color(0.82f, 0.93f, 0.98f),
                    new Color(0.4f, 0.67f, 0.9f),
                    new Color(0.26f, 0.48f, 0.38f),
                    new Color(1f, 0.7f, 0.22f),
                    new Color(0.78f, 0.42f, 0.08f),
                    new Color(0.48f, 0.32f, 0.24f),
                    new Color(0.1f, 0.2f, 0.2f),
                    new Color(0.9f, 0.92f, 0.9f),
                    new Color(0.2f, 0.16f, 0.14f),
                    new Color(0.54f, 0.64f, 0.72f),
                    new Color(0.68f, 0.76f, 0.84f)),
                MilkroomThemeController.VintageThemeId => new MilkroomThemePalette(
                    new Color(0.52f, 0.38f, 0.25f),
                    new Color(0.86f, 0.6f, 0.28f),
                    new Color(0.29f, 0.2f, 0.14f),
                    new Color(0.18f, 0.12f, 0.08f),
                    new Color(0.58f, 0.44f, 0.3f),
                    new Color(0.72f, 0.56f, 0.36f),
                    new Color(0.26f, 0.2f, 0.2f),
                    new Color(0.86f, 0.7f, 0.38f),
                    new Color(0.58f, 0.48f, 0.38f),
                    new Color(0.62f, 0.5f, 0.38f),
                    new Color(0.7f, 0.72f, 0.66f),
                    new Color(0.38f, 0.54f, 0.62f),
                    new Color(0.3f, 0.42f, 0.28f),
                    new Color(0.9f, 0.62f, 0.2f),
                    new Color(0.68f, 0.36f, 0.08f),
                    new Color(0.42f, 0.25f, 0.14f),
                    new Color(0.12f, 0.18f, 0.14f),
                    new Color(0.7f, 0.64f, 0.5f),
                    new Color(0.22f, 0.14f, 0.08f),
                    new Color(0.44f, 0.34f, 0.24f),
                    new Color(0.34f, 0.28f, 0.22f)),
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
                    new Color(0.78f, 0.58f, 0.38f),
                    new Color(0.98f, 0.68f, 0.34f),
                    new Color(0.5f, 0.29f, 0.15f),
                    new Color(0.32f, 0.18f, 0.1f),
                    new Color(0.86f, 0.74f, 0.54f),
                    new Color(0.68f, 0.54f, 0.36f),
                    new Color(0.58f, 0.78f, 0.92f),
                    new Color(1f, 0.72f, 0.28f),
                    new Color(0.88f, 0.94f, 0.98f),
                    new Color(0.92f, 0.8f, 0.62f),
                    new Color(0.8f, 0.9f, 0.94f),
                    new Color(0.38f, 0.65f, 0.82f),
                    new Color(0.32f, 0.58f, 0.32f),
                    new Color(1f, 0.64f, 0.14f),
                    new Color(0.78f, 0.4f, 0.07f),
                    new Color(0.58f, 0.34f, 0.17f),
                    new Color(0.12f, 0.22f, 0.17f),
                    new Color(0.88f, 0.82f, 0.68f),
                    new Color(0.28f, 0.16f, 0.08f),
                    new Color(0.66f, 0.54f, 0.38f),
                    new Color(0.94f, 0.88f, 0.78f))
            };
        }
    }
}
