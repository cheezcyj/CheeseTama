using CheeseTama.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CheeseTama.Editor
{
    public static class MilkroomPropPrefabBuilder
    {
        private const string PropsRoot = "Assets/Environments/Milkroom/Props";

        [MenuItem("CheeseTama/밀크룸 소품 프리팹 생성")]
        public static void BuildMilkroomPropPrefabs()
        {
            EnsurePropsFolder();
            SavePrefab(BuildWindow(), $"{PropsRoot}/Window.prefab");
            SavePrefab(BuildRug(), $"{PropsRoot}/Rug.prefab");
            SavePrefab(BuildDresserTable(), $"{PropsRoot}/DresserTable.prefab");
            SavePrefab(BuildChalkboard(), $"{PropsRoot}/Chalkboard.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milkroom prop prefabs generated: Window, Rug, DresserTable, Chalkboard");
        }

        private static GameObject BuildWindow()
        {
            var root = new GameObject("Window");
            CreatePart(root.transform, "Window Back Warm Halo", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0.08f), new Vector3(3.15f, 2.05f, 0.045f), new Color(1f, 0.82f, 0.42f));
            CreatePart(root.transform, "Window Wall Cut Dark Plate", PrimitiveType.Cube, new Vector3(0f, -0.06f, 0.02f), new Vector3(2.55f, 1.55f, 0.055f), new Color(0.62f, 0.36f, 0.17f));
            CreatePart(root.transform, "Window Sky Lower Pane", PrimitiveType.Cube, new Vector3(0f, -0.12f, -0.02f), new Vector3(1.98f, 1.18f, 0.05f), new Color(0.58f, 0.78f, 0.94f));
            CreatePart(root.transform, "Window Sky Rounded Top", PrimitiveType.Sphere, new Vector3(0f, 0.54f, -0.025f), new Vector3(1.98f, 0.58f, 0.045f), new Color(0.68f, 0.86f, 0.98f));
            CreatePart(root.transform, "Window Sun Glow", PrimitiveType.Sphere, new Vector3(0.52f, 0.34f, -0.065f), new Vector3(0.42f, 0.42f, 0.035f), new Color(1f, 0.84f, 0.36f));
            CreatePart(root.transform, "Window Sun Core", PrimitiveType.Sphere, new Vector3(0.52f, 0.34f, -0.09f), new Vector3(0.18f, 0.18f, 0.026f), new Color(1f, 0.94f, 0.52f));
            CreateCloud(root.transform, "Window Soft Cloud Left", new Vector3(-0.48f, 0.26f, -0.08f), 0.32f);
            CreateCloud(root.transform, "Window Soft Cloud Right", new Vector3(0.28f, 0.04f, -0.08f), 0.26f);

            CreatePart(root.transform, "Window Outer Arch Top", PrimitiveType.Sphere, new Vector3(0f, 0.68f, -0.13f), new Vector3(2.44f, 0.5f, 0.14f), new Color(0.84f, 0.48f, 0.2f));
            CreatePart(root.transform, "Window Inner Arch Cut", PrimitiveType.Sphere, new Vector3(0f, 0.61f, -0.155f), new Vector3(1.92f, 0.38f, 0.12f), new Color(0.68f, 0.86f, 0.98f));
            CreatePart(root.transform, "Window Lower Frame", PrimitiveType.Cube, new Vector3(0f, -0.72f, -0.13f), new Vector3(2.48f, 0.14f, 0.16f), new Color(0.73f, 0.4f, 0.16f));
            CreatePart(root.transform, "Window Left Frame", PrimitiveType.Cube, new Vector3(-1.14f, -0.06f, -0.13f), new Vector3(0.13f, 1.36f, 0.15f), new Color(0.78f, 0.44f, 0.19f));
            CreatePart(root.transform, "Window Right Frame", PrimitiveType.Cube, new Vector3(1.14f, -0.06f, -0.13f), new Vector3(0.13f, 1.36f, 0.15f), new Color(0.78f, 0.44f, 0.19f));
            CreatePart(root.transform, "Window Center Mullion", PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.18f), new Vector3(0.085f, 1.36f, 0.13f), new Color(0.92f, 0.58f, 0.28f));
            CreatePart(root.transform, "Window Cross Mullion", PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.18f), new Vector3(2.04f, 0.075f, 0.13f), new Color(0.92f, 0.58f, 0.28f));
            CreatePart(root.transform, "Window Inner Left Rail", PrimitiveType.Cube, new Vector3(-0.55f, 0.31f, -0.18f), new Vector3(0.055f, 0.68f, 0.11f), new Color(0.96f, 0.66f, 0.34f));
            CreatePart(root.transform, "Window Inner Right Rail", PrimitiveType.Cube, new Vector3(0.55f, 0.31f, -0.18f), new Vector3(0.055f, 0.68f, 0.11f), new Color(0.96f, 0.66f, 0.34f));
            CreatePart(root.transform, "Window Chunky Sill", PrimitiveType.Cube, new Vector3(0f, -0.88f, -0.17f), new Vector3(2.78f, 0.14f, 0.28f), new Color(0.66f, 0.36f, 0.14f));
            CreatePart(root.transform, "Window Sill Sheen", PrimitiveType.Cube, new Vector3(-0.22f, -0.79f, -0.32f), new Vector3(1.82f, 0.035f, 0.035f), new Color(0.98f, 0.72f, 0.38f));

            CreateCurtain(root.transform, "Window Curtain Left", -1.45f, false);
            CreateCurtain(root.transform, "Window Curtain Right", 1.45f, true);
            CreatePart(root.transform, "Window Curtain Rod", PrimitiveType.Cylinder, new Vector3(0f, 0.94f, -0.23f), new Vector3(0.04f, 1.75f, 0.04f), Quaternion.Euler(0f, 0f, 90f), new Color(0.58f, 0.32f, 0.13f));
            for (var i = 0; i < 7; i += 1)
            {
                CreatePart(root.transform, $"Window Curtain Ring {i + 1}", PrimitiveType.Cylinder, new Vector3(-1.14f + i * 0.38f, 0.94f, -0.24f), new Vector3(0.055f, 0.012f, 0.055f), Quaternion.Euler(90f, 0f, 0f), new Color(0.72f, 0.46f, 0.22f));
            }

            CreateStarDoodle(root.transform, "Window Star Decal Left", new Vector3(-0.72f, 0.34f, -0.2f), 0.09f, new Color(1f, 0.86f, 0.32f));
            CreateStarDoodle(root.transform, "Window Star Decal Right", new Vector3(0.82f, 0.08f, -0.2f), 0.075f, new Color(1f, 0.9f, 0.42f));
            CreatePottedPlant(root.transform, "Window Sill Plant", new Vector3(0.72f, -0.67f, -0.34f), 0.24f);
            return root;
        }

        private static GameObject BuildRug()
        {
            var root = new GameObject("Rug");
            CreatePart(root.transform, "Rug Soft Ground Dark Plate", PrimitiveType.Sphere, new Vector3(0f, -0.035f, 0.02f), new Vector3(2.85f, 0.055f, 1.32f), new Color(0.58f, 0.38f, 0.2f));
            CreatePart(root.transform, "Rug Outer Fluff Base", PrimitiveType.Sphere, Vector3.zero, new Vector3(2.7f, 0.18f, 1.22f), new Color(0.92f, 0.8f, 0.6f));
            CreatePart(root.transform, "Rug Raised Inner Cushion", PrimitiveType.Sphere, new Vector3(0f, 0.055f, -0.015f), new Vector3(2.12f, 0.12f, 0.88f), new Color(1f, 0.92f, 0.73f));
            CreatePart(root.transform, "Rug Warm Center", PrimitiveType.Sphere, new Vector3(0f, 0.09f, -0.02f), new Vector3(1.28f, 0.08f, 0.52f), new Color(0.96f, 0.84f, 0.62f));
            CreatePart(root.transform, "Rug Paw Center Pad", PrimitiveType.Sphere, new Vector3(0f, 0.14f, -0.11f), new Vector3(0.48f, 0.055f, 0.19f), new Color(0.82f, 0.64f, 0.42f));
            CreatePart(root.transform, "Rug Paw Toe Left", PrimitiveType.Sphere, new Vector3(-0.36f, 0.145f, 0.17f), new Vector3(0.18f, 0.045f, 0.13f), new Color(0.86f, 0.72f, 0.52f));
            CreatePart(root.transform, "Rug Paw Toe Left Center", PrimitiveType.Sphere, new Vector3(-0.12f, 0.15f, 0.24f), new Vector3(0.17f, 0.045f, 0.125f), new Color(0.86f, 0.72f, 0.52f));
            CreatePart(root.transform, "Rug Paw Toe Right Center", PrimitiveType.Sphere, new Vector3(0.12f, 0.15f, 0.24f), new Vector3(0.17f, 0.045f, 0.125f), new Color(0.86f, 0.72f, 0.52f));
            CreatePart(root.transform, "Rug Paw Toe Right", PrimitiveType.Sphere, new Vector3(0.36f, 0.145f, 0.17f), new Vector3(0.18f, 0.045f, 0.13f), new Color(0.86f, 0.72f, 0.52f));

            for (var i = 0; i < 40; i += 1)
            {
                var angle = i / 40f * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * 1.34f;
                var z = Mathf.Sin(angle) * 0.62f;
                var tuftScale = new Vector3(0.16f + (i % 4) * 0.014f, 0.062f, 0.09f + (i % 2) * 0.012f);
                CreatePart(root.transform, $"Rug Braided Tuft {i + 1}", PrimitiveType.Sphere, new Vector3(x, 0.115f, z), tuftScale, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f), new Color(0.98f, 0.87f, 0.68f));
            }

            for (var i = 0; i < 14; i += 1)
            {
                var angle = i / 14f * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * 0.86f;
                var z = Mathf.Sin(angle) * 0.38f;
                CreatePart(root.transform, $"Rug Inner Stitch {i + 1}", PrimitiveType.Cube, new Vector3(x, 0.155f, z), new Vector3(0.13f, 0.018f, 0.022f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f), new Color(0.86f, 0.7f, 0.48f));
            }

            for (var i = 0; i < 10; i += 1)
            {
                var x = -0.98f + i * 0.22f;
                var z = -0.44f + (i % 2) * 0.1f;
                CreatePart(root.transform, $"Rug Loose Thread {i + 1}", PrimitiveType.Cube, new Vector3(x, 0.165f, z), new Vector3(0.08f, 0.015f, 0.018f), Quaternion.Euler(0f, 25f, 0f), new Color(1f, 0.9f, 0.72f));
            }

            return root;
        }

        private static GameObject BuildDresserTable()
        {
            var root = new GameObject("DresserTable");
            CreatePart(root.transform, "DresserTable Back Dark Plate", PrimitiveType.Cube, new Vector3(0f, 0.46f, 0.08f), new Vector3(1.74f, 0.9f, 0.08f), new Color(0.32f, 0.18f, 0.08f));
            CreatePart(root.transform, "DresserTable Body", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(1.58f, 0.8f, 0.5f), new Color(0.58f, 0.34f, 0.17f));
            CreatePart(root.transform, "DresserTable Rounded Left", PrimitiveType.Sphere, new Vector3(-0.8f, 0.43f, -0.02f), new Vector3(0.14f, 0.78f, 0.46f), new Color(0.64f, 0.39f, 0.2f));
            CreatePart(root.transform, "DresserTable Rounded Right", PrimitiveType.Sphere, new Vector3(0.8f, 0.43f, -0.02f), new Vector3(0.14f, 0.78f, 0.46f), new Color(0.64f, 0.39f, 0.2f));
            CreatePart(root.transform, "DresserTable Top Slab", PrimitiveType.Cube, new Vector3(0f, 0.9f, -0.02f), new Vector3(1.82f, 0.16f, 0.62f), new Color(0.74f, 0.46f, 0.24f));
            CreatePart(root.transform, "DresserTable Top Sheen", PrimitiveType.Cube, new Vector3(-0.18f, 0.99f, -0.28f), new Vector3(1.25f, 0.035f, 0.04f), new Color(0.96f, 0.66f, 0.36f));
            CreatePart(root.transform, "DresserTable Cream Runner", PrimitiveType.Cube, new Vector3(0.24f, 1f, -0.32f), new Vector3(1.14f, 0.075f, 0.16f), new Color(1f, 0.9f, 0.68f));
            CreatePart(root.transform, "DresserTable Runner Drop", PrimitiveType.Cube, new Vector3(0.38f, 0.78f, -0.37f), new Vector3(0.36f, 0.34f, 0.06f), new Color(0.96f, 0.82f, 0.58f));
            for (var i = 0; i < 4; i += 1)
            {
                CreatePart(root.transform, $"DresserTable Cloth Scallop {i + 1}", PrimitiveType.Sphere, new Vector3(-0.12f + i * 0.22f, 0.73f, -0.4f), new Vector3(0.11f, 0.06f, 0.03f), new Color(1f, 0.9f, 0.68f));
            }

            for (var i = 0; i < 3; i += 1)
            {
                var x = -0.48f + i * 0.48f;
                CreatePart(root.transform, $"DresserTable Drawer Panel {i + 1}", PrimitiveType.Cube, new Vector3(x, 0.47f, -0.295f), new Vector3(0.39f, 0.24f, 0.055f), new Color(0.72f, 0.44f, 0.22f));
                CreatePart(root.transform, $"DresserTable Drawer Inset {i + 1}", PrimitiveType.Cube, new Vector3(x, 0.47f, -0.33f), new Vector3(0.28f, 0.15f, 0.022f), new Color(0.56f, 0.32f, 0.15f));
                CreatePart(root.transform, $"DresserTable Drawer Pull {i + 1}", PrimitiveType.Sphere, new Vector3(x, 0.47f, -0.365f), new Vector3(0.055f, 0.055f, 0.025f), new Color(0.98f, 0.7f, 0.32f));
            }

            CreatePart(root.transform, "DresserTable Leg Left", PrimitiveType.Cube, new Vector3(-0.64f, 0.04f, -0.12f), new Vector3(0.13f, 0.3f, 0.13f), new Color(0.44f, 0.25f, 0.12f));
            CreatePart(root.transform, "DresserTable Leg Right", PrimitiveType.Cube, new Vector3(0.64f, 0.04f, -0.12f), new Vector3(0.13f, 0.3f, 0.13f), new Color(0.44f, 0.25f, 0.12f));
            CreatePart(root.transform, "DresserTable Bottom Rail", PrimitiveType.Cube, new Vector3(0f, 0.17f, -0.28f), new Vector3(1.44f, 0.08f, 0.055f), new Color(0.46f, 0.26f, 0.12f));
            CreateMilkBottle(root.transform, "DresserTable Milk Bottle Tall", new Vector3(-0.5f, 1.2f, -0.14f), 0.48f);
            CreateMilkBottle(root.transform, "DresserTable Milk Bottle Middle", new Vector3(-0.2f, 1.15f, -0.14f), 0.4f);
            CreateMilkBottle(root.transform, "DresserTable Milk Bottle Small", new Vector3(0.06f, 1.1f, -0.14f), 0.32f);
            CreatePart(root.transform, "DresserTable Blender Base", PrimitiveType.Cube, new Vector3(0.48f, 1.04f, -0.12f), new Vector3(0.3f, 0.18f, 0.18f), new Color(0.9f, 0.78f, 0.58f));
            CreatePart(root.transform, "DresserTable Blender Dial", PrimitiveType.Sphere, new Vector3(0.48f, 1.05f, -0.23f), new Vector3(0.045f, 0.045f, 0.018f), new Color(0.54f, 0.34f, 0.18f));
            CreatePart(root.transform, "DresserTable Blender Jar", PrimitiveType.Capsule, new Vector3(0.48f, 1.3f, -0.12f), new Vector3(0.15f, 0.22f, 0.1f), new Color(0.75f, 0.9f, 0.98f));
            CreatePart(root.transform, "DresserTable Blender Milk Fill", PrimitiveType.Sphere, new Vector3(0.48f, 1.2f, -0.2f), new Vector3(0.12f, 0.06f, 0.03f), new Color(0.98f, 0.94f, 0.78f));
            CreateCheeseBlock(root.transform, "DresserTable Cheese Sample", new Vector3(0.82f, 1.04f, -0.14f), 0.18f);
            CreatePart(root.transform, "DresserTable Star Cookie", PrimitiveType.Sphere, new Vector3(0.75f, 1.22f, -0.16f), new Vector3(0.08f, 0.08f, 0.035f), new Color(1f, 0.78f, 0.34f));
            return root;
        }

        private static GameObject BuildChalkboard()
        {
            var root = new GameObject("Chalkboard");
            CreatePart(root.transform, "Chalkboard Back Plate", PrimitiveType.Cube, new Vector3(0f, 0f, 0.04f), new Vector3(1.36f, 1.02f, 0.06f), new Color(0.34f, 0.2f, 0.1f));
            CreatePart(root.transform, "Chalkboard Board", PrimitiveType.Cube, Vector3.zero, new Vector3(1.08f, 0.78f, 0.08f), new Color(0.12f, 0.22f, 0.16f));
            CreatePart(root.transform, "Chalkboard Soft Smudge", PrimitiveType.Sphere, new Vector3(-0.12f, 0.08f, -0.05f), new Vector3(0.82f, 0.42f, 0.018f), new Color(0.2f, 0.32f, 0.24f));
            CreatePart(root.transform, "Chalkboard Frame Top", PrimitiveType.Cube, new Vector3(0f, 0.45f, -0.055f), new Vector3(1.28f, 0.1f, 0.12f), new Color(0.58f, 0.34f, 0.17f));
            CreatePart(root.transform, "Chalkboard Frame Bottom", PrimitiveType.Cube, new Vector3(0f, -0.45f, -0.055f), new Vector3(1.28f, 0.1f, 0.12f), new Color(0.58f, 0.34f, 0.17f));
            CreatePart(root.transform, "Chalkboard Frame Left", PrimitiveType.Cube, new Vector3(-0.64f, 0f, -0.055f), new Vector3(0.1f, 0.94f, 0.12f), new Color(0.58f, 0.34f, 0.17f));
            CreatePart(root.transform, "Chalkboard Frame Right", PrimitiveType.Cube, new Vector3(0.64f, 0f, -0.055f), new Vector3(0.1f, 0.94f, 0.12f), new Color(0.58f, 0.34f, 0.17f));
            CreatePart(root.transform, "Chalkboard Top Cap Sheen", PrimitiveType.Cube, new Vector3(-0.1f, 0.51f, -0.12f), new Vector3(0.86f, 0.035f, 0.035f), new Color(0.9f, 0.6f, 0.3f));
            CreatePart(root.transform, "Chalkboard Hanger Left", PrimitiveType.Cube, new Vector3(-0.34f, 0.68f, -0.03f), new Vector3(0.035f, 0.42f, 0.035f), Quaternion.Euler(0f, 0f, -35f), new Color(0.64f, 0.42f, 0.2f));
            CreatePart(root.transform, "Chalkboard Hanger Right", PrimitiveType.Cube, new Vector3(0.34f, 0.68f, -0.03f), new Vector3(0.035f, 0.42f, 0.035f), Quaternion.Euler(0f, 0f, 35f), new Color(0.64f, 0.42f, 0.2f));
            CreatePart(root.transform, "Chalkboard Hanging Peg", PrimitiveType.Sphere, new Vector3(0f, 0.84f, -0.04f), new Vector3(0.07f, 0.07f, 0.04f), new Color(0.82f, 0.55f, 0.28f));
            CreateWorldLabel(root.transform, "Chalkboard Milk Text", "Milk\nis\nMagic", new Vector3(0f, 0.08f, -0.105f), 0.125f, new Color(1f, 0.9f, 0.62f));
            CreateStarDoodle(root.transform, "Chalkboard Star Doodle A", new Vector3(-0.42f, -0.22f, -0.11f), 0.075f, new Color(1f, 0.86f, 0.3f));
            CreateStarDoodle(root.transform, "Chalkboard Star Doodle B", new Vector3(0.42f, 0.27f, -0.11f), 0.06f, new Color(1f, 0.92f, 0.48f));
            CreateCheeseBlock(root.transform, "Chalkboard Tiny Cheese Doodle", new Vector3(0.39f, -0.28f, -0.12f), 0.13f);
            CreatePart(root.transform, "Chalkboard Chalk Stick", PrimitiveType.Cube, new Vector3(0.08f, -0.36f, -0.12f), new Vector3(0.36f, 0.025f, 0.025f), new Color(0.96f, 0.9f, 0.72f));
            return root;
        }

        private static void CreateCurtain(Transform parent, string name, float x, bool flip)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = Vector3.zero;
            var sign = flip ? -1f : 1f;
            CreatePart(root, $"{name} Draped Panel", PrimitiveType.Sphere, new Vector3(x, -0.04f, -0.16f), new Vector3(0.34f, 0.86f, 0.075f), new Color(1f, 0.9f, 0.73f));
            CreatePart(root, $"{name} Top Pleat", PrimitiveType.Cube, new Vector3(x, 0.66f, -0.18f), new Vector3(0.42f, 0.12f, 0.08f), new Color(1f, 0.95f, 0.78f));
            for (var i = 0; i < 4; i += 1)
            {
                var foldX = x + sign * (-0.13f + i * 0.085f);
                var foldHeight = 1.2f - i * 0.08f;
                CreatePart(root, $"{name} Vertical Fold {i + 1}", PrimitiveType.Cube, new Vector3(foldX, -0.05f + i * 0.015f, -0.22f), new Vector3(0.035f, foldHeight, 0.052f), new Color(0.96f + i * 0.01f, 0.82f + i * 0.02f, 0.62f + i * 0.02f));
            }

            CreatePart(root, $"{name} Tie Band", PrimitiveType.Cube, new Vector3(x - sign * 0.04f, -0.19f, -0.25f), new Vector3(0.38f, 0.085f, 0.06f), new Color(0.8f, 0.48f, 0.24f));
            CreatePart(root, $"{name} Tie Knot", PrimitiveType.Sphere, new Vector3(x - sign * 0.2f, -0.19f, -0.28f), new Vector3(0.08f, 0.08f, 0.035f), new Color(0.88f, 0.58f, 0.3f));
            CreatePart(root, $"{name} Bottom Puff", PrimitiveType.Sphere, new Vector3(x, -0.78f, -0.18f), new Vector3(0.36f, 0.12f, 0.06f), new Color(1f, 0.92f, 0.76f));
        }

        private static void CreateCloud(Transform parent, string name, Vector3 position, float size)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            CreatePart(root, "Cloud Body", PrimitiveType.Sphere, Vector3.zero, new Vector3(size * 1.05f, size * 0.32f, size * 0.08f), new Color(0.96f, 0.98f, 1f));
            CreatePart(root, "Cloud Puff Left", PrimitiveType.Sphere, new Vector3(-size * 0.32f, size * 0.06f, -size * 0.02f), new Vector3(size * 0.38f, size * 0.28f, size * 0.07f), new Color(1f, 1f, 1f));
            CreatePart(root, "Cloud Puff Right", PrimitiveType.Sphere, new Vector3(size * 0.3f, size * 0.08f, -size * 0.02f), new Vector3(size * 0.42f, size * 0.3f, size * 0.07f), new Color(1f, 1f, 1f));
        }

        private static void CreatePottedPlant(Transform parent, string name, Vector3 position, float size)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            CreatePart(root, "Plant Pot", PrimitiveType.Cube, Vector3.zero, new Vector3(size * 0.65f, size * 0.45f, size * 0.28f), new Color(0.58f, 0.32f, 0.17f));
            CreatePart(root, "Plant Pot Rim", PrimitiveType.Cube, new Vector3(0f, size * 0.23f, -size * 0.02f), new Vector3(size * 0.76f, size * 0.08f, size * 0.32f), new Color(0.72f, 0.42f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                var angle = -44f + i * 22f;
                var x = Mathf.Sin(angle * Mathf.Deg2Rad) * size * 0.28f;
                var y = size * 0.38f + (i % 2) * size * 0.06f;
                CreatePart(root, $"Plant Leaf {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, -size * 0.08f), new Vector3(size * 0.3f, size * 0.14f, size * 0.045f), Quaternion.Euler(0f, 0f, angle), new Color(0.33f, 0.6f + i * 0.02f, 0.32f));
            }
        }

        private static void CreateMilkBottle(Transform parent, string name, Vector3 position, float size)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            CreatePart(root, "Bottle Body", PrimitiveType.Capsule, Vector3.zero, new Vector3(size * 0.25f, size * 0.44f, size * 0.13f), new Color(0.84f, 0.94f, 0.98f));
            CreatePart(root, "Bottle Milk Fill", PrimitiveType.Capsule, new Vector3(0f, -size * 0.08f, -size * 0.012f), new Vector3(size * 0.2f, size * 0.32f, size * 0.1f), new Color(0.98f, 0.95f, 0.78f));
            CreatePart(root, "Bottle Neck", PrimitiveType.Cylinder, new Vector3(0f, size * 0.3f, 0f), new Vector3(size * 0.08f, size * 0.1f, size * 0.08f), new Color(0.86f, 0.95f, 1f));
            CreatePart(root, "Bottle Cap", PrimitiveType.Cube, new Vector3(0f, size * 0.42f, -size * 0.02f), new Vector3(size * 0.18f, size * 0.07f, size * 0.08f), new Color(0.47f, 0.72f, 0.9f));
            CreatePart(root, "Bottle Label", PrimitiveType.Cube, new Vector3(0f, -size * 0.02f, -size * 0.14f), new Vector3(size * 0.22f, size * 0.13f, size * 0.028f), new Color(1f, 0.86f, 0.56f));
            CreatePart(root, "Bottle Shine", PrimitiveType.Cube, new Vector3(-size * 0.075f, size * 0.08f, -size * 0.15f), new Vector3(size * 0.025f, size * 0.19f, size * 0.012f), new Color(1f, 1f, 0.94f));
        }

        private static void CreateCheeseBlock(Transform parent, string name, Vector3 position, float size)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            CreatePart(root, "Cheese Block Body", PrimitiveType.Cube, Vector3.zero, new Vector3(size, size * 0.72f, size * 0.7f), new Color(1f, 0.7f, 0.2f));
            CreatePart(root, "Cheese Hole A", PrimitiveType.Sphere, new Vector3(-size * 0.22f, size * 0.1f, -size * 0.36f), new Vector3(size * 0.12f, size * 0.09f, size * 0.025f), new Color(0.82f, 0.48f, 0.1f));
            CreatePart(root, "Cheese Hole B", PrimitiveType.Sphere, new Vector3(size * 0.12f, -size * 0.11f, -size * 0.36f), new Vector3(size * 0.08f, size * 0.07f, size * 0.025f), new Color(0.82f, 0.48f, 0.1f));
        }

        private static void CreateStarDoodle(Transform parent, string name, Vector3 position, float size, Color color)
        {
            CreatePart(parent, name, PrimitiveType.Sphere, position, new Vector3(size, size, size * 0.28f), color);
            CreatePart(parent, $"{name} Horizontal", PrimitiveType.Cube, position + new Vector3(0f, 0f, -size * 0.14f), new Vector3(size * 2.1f, size * 0.22f, size * 0.18f), color);
            CreatePart(parent, $"{name} Vertical", PrimitiveType.Cube, position + new Vector3(0f, 0f, -size * 0.15f), new Vector3(size * 0.22f, size * 2.1f, size * 0.18f), color);
        }

        private static Transform CreatePart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
        {
            return CreatePart(parent, name, primitive, localPosition, localScale, Quaternion.identity, color);
        }

        private static Transform CreatePart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            if (part.TryGetComponent(out Renderer renderer))
            {
                renderer.shadowCastingMode = name.Contains("Glow") || name.Contains("Sky") ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = !name.Contains("Glow") && !name.Contains("Sky");
                ToonMaterialUtility.Apply(renderer, ToonMaterialUtility.InferProfile(renderer), color);
            }

            return part.transform;
        }

        private static void CreateWorldLabel(Transform parent, string name, string text, Vector3 localPosition, float characterSize, Color color)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.identity;

            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = 96;
            label.color = color;

            var renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void EnsurePropsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Environments"))
            {
                AssetDatabase.CreateFolder("Assets", "Environments");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Environments/Milkroom"))
            {
                AssetDatabase.CreateFolder("Assets/Environments", "Milkroom");
            }

            if (!AssetDatabase.IsValidFolder(PropsRoot))
            {
                AssetDatabase.CreateFolder("Assets/Environments/Milkroom", "Props");
            }
        }
    }
}
