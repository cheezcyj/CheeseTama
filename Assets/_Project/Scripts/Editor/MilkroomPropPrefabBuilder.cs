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
            CreatePart(root.transform, "Window Warm Back Glow", PrimitiveType.Sphere, new Vector3(0f, 0.03f, 0.06f), new Vector3(2.75f, 1.75f, 0.05f), new Color(1f, 0.82f, 0.42f));
            CreatePart(root.transform, "Window Sky Panel", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(2.05f, 1.16f, 0.055f), new Color(0.62f, 0.82f, 0.95f));
            CreatePart(root.transform, "Window Sky Arch Glow", PrimitiveType.Sphere, new Vector3(0f, 0.52f, -0.005f), new Vector3(2.1f, 0.68f, 0.045f), new Color(0.72f, 0.88f, 0.98f));
            CreatePart(root.transform, "Window Sun Patch", PrimitiveType.Sphere, new Vector3(0.55f, 0.34f, -0.04f), new Vector3(0.32f, 0.32f, 0.03f), new Color(1f, 0.83f, 0.34f));
            CreatePart(root.transform, "Window Cloud Left", PrimitiveType.Sphere, new Vector3(-0.42f, 0.22f, -0.045f), new Vector3(0.42f, 0.15f, 0.03f), new Color(0.96f, 0.98f, 1f));
            CreatePart(root.transform, "Window Cloud Right", PrimitiveType.Sphere, new Vector3(-0.12f, 0.1f, -0.045f), new Vector3(0.32f, 0.12f, 0.03f), new Color(0.96f, 0.98f, 1f));

            CreatePart(root.transform, "Window Frame Top", PrimitiveType.Cube, new Vector3(0f, 0.68f, -0.08f), new Vector3(2.35f, 0.09f, 0.12f), new Color(0.9f, 0.55f, 0.24f));
            CreatePart(root.transform, "Window Frame Bottom", PrimitiveType.Cube, new Vector3(0f, -0.66f, -0.08f), new Vector3(2.38f, 0.11f, 0.13f), new Color(0.82f, 0.48f, 0.22f));
            CreatePart(root.transform, "Window Frame Left", PrimitiveType.Cube, new Vector3(-1.12f, 0f, -0.08f), new Vector3(0.11f, 1.35f, 0.12f), new Color(0.86f, 0.5f, 0.23f));
            CreatePart(root.transform, "Window Frame Right", PrimitiveType.Cube, new Vector3(1.12f, 0f, -0.08f), new Vector3(0.11f, 1.35f, 0.12f), new Color(0.86f, 0.5f, 0.23f));
            CreatePart(root.transform, "Window Frame Center", PrimitiveType.Cube, new Vector3(0f, 0f, -0.1f), new Vector3(0.08f, 1.22f, 0.11f), new Color(0.93f, 0.6f, 0.3f));
            CreatePart(root.transform, "Window Frame Cross", PrimitiveType.Cube, new Vector3(0f, 0.03f, -0.1f), new Vector3(2.08f, 0.075f, 0.11f), new Color(0.93f, 0.6f, 0.3f));
            CreatePart(root.transform, "Window Rounded Arch", PrimitiveType.Sphere, new Vector3(0f, 0.68f, -0.1f), new Vector3(2.36f, 0.42f, 0.12f), new Color(0.9f, 0.55f, 0.24f));
            CreatePart(root.transform, "Window Sill", PrimitiveType.Cube, new Vector3(0f, -0.82f, -0.12f), new Vector3(2.68f, 0.12f, 0.22f), new Color(0.74f, 0.42f, 0.18f));

            CreateCurtain(root.transform, "Window Curtain Left", -1.43f, false);
            CreateCurtain(root.transform, "Window Curtain Right", 1.43f, true);
            CreateStarDoodle(root.transform, "Window Star Decal Left", new Vector3(-0.72f, 0.36f, -0.09f), 0.09f, new Color(1f, 0.86f, 0.32f));
            CreateStarDoodle(root.transform, "Window Star Decal Right", new Vector3(0.82f, 0.08f, -0.09f), 0.075f, new Color(1f, 0.9f, 0.42f));
            return root;
        }

        private static GameObject BuildRug()
        {
            var root = new GameObject("Rug");
            CreatePart(root.transform, "Rug Outer Fluff", PrimitiveType.Sphere, Vector3.zero, new Vector3(2.6f, 0.16f, 1.18f), new Color(0.92f, 0.8f, 0.6f));
            CreatePart(root.transform, "Rug Inner Cream", PrimitiveType.Sphere, new Vector3(0f, 0.035f, -0.02f), new Vector3(2.08f, 0.12f, 0.86f), new Color(1f, 0.92f, 0.73f));
            CreatePart(root.transform, "Rug Paw Center", PrimitiveType.Sphere, new Vector3(0f, 0.09f, -0.08f), new Vector3(0.44f, 0.055f, 0.18f), new Color(0.84f, 0.68f, 0.46f));
            CreatePart(root.transform, "Rug Paw Toe Left", PrimitiveType.Sphere, new Vector3(-0.34f, 0.095f, 0.17f), new Vector3(0.18f, 0.045f, 0.13f), new Color(0.86f, 0.72f, 0.52f));
            CreatePart(root.transform, "Rug Paw Toe Mid", PrimitiveType.Sphere, new Vector3(0f, 0.105f, 0.23f), new Vector3(0.18f, 0.045f, 0.13f), new Color(0.86f, 0.72f, 0.52f));
            CreatePart(root.transform, "Rug Paw Toe Right", PrimitiveType.Sphere, new Vector3(0.34f, 0.095f, 0.17f), new Vector3(0.18f, 0.045f, 0.13f), new Color(0.86f, 0.72f, 0.52f));

            for (var i = 0; i < 24; i += 1)
            {
                var angle = i / 24f * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * 1.28f;
                var z = Mathf.Sin(angle) * 0.58f;
                var tuftScale = new Vector3(0.18f + (i % 3) * 0.018f, 0.055f, 0.095f);
                CreatePart(root.transform, $"Rug Tuft Rim {i + 1}", PrimitiveType.Sphere, new Vector3(x, 0.08f, z), tuftScale, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f), new Color(0.98f, 0.87f, 0.68f));
            }

            for (var i = 0; i < 9; i += 1)
            {
                var x = -0.84f + i * 0.21f;
                var z = -0.38f + (i % 3) * 0.08f;
                CreatePart(root.transform, $"Rug Soft Stitch {i + 1}", PrimitiveType.Cube, new Vector3(x, 0.12f, z), new Vector3(0.1f, 0.018f, 0.025f), Quaternion.Euler(0f, 20f, 0f), new Color(0.9f, 0.75f, 0.55f));
            }

            return root;
        }

        private static GameObject BuildDresserTable()
        {
            var root = new GameObject("DresserTable");
            CreatePart(root.transform, "DresserTable Body", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(1.55f, 0.78f, 0.48f), new Color(0.58f, 0.34f, 0.17f));
            CreatePart(root.transform, "DresserTable Top", PrimitiveType.Cube, new Vector3(0f, 0.86f, -0.01f), new Vector3(1.72f, 0.14f, 0.56f), new Color(0.74f, 0.46f, 0.24f));
            CreatePart(root.transform, "DresserTable Cloth", PrimitiveType.Cube, new Vector3(0f, 0.91f, -0.31f), new Vector3(1.44f, 0.09f, 0.11f), new Color(1f, 0.9f, 0.68f));
            CreatePart(root.transform, "DresserTable Cloth Fold", PrimitiveType.Cube, new Vector3(0.36f, 0.78f, -0.34f), new Vector3(0.22f, 0.26f, 0.06f), new Color(0.96f, 0.82f, 0.58f));

            for (var i = 0; i < 3; i += 1)
            {
                var x = -0.48f + i * 0.48f;
                CreatePart(root.transform, $"DresserTable Drawer {i + 1}", PrimitiveType.Cube, new Vector3(x, 0.43f, -0.285f), new Vector3(0.38f, 0.24f, 0.05f), new Color(0.72f, 0.44f, 0.22f));
                CreatePart(root.transform, $"DresserTable Drawer Pull {i + 1}", PrimitiveType.Sphere, new Vector3(x, 0.43f, -0.335f), new Vector3(0.055f, 0.055f, 0.025f), new Color(0.98f, 0.7f, 0.32f));
            }

            CreatePart(root.transform, "DresserTable Leg Left", PrimitiveType.Cube, new Vector3(-0.62f, 0.03f, -0.12f), new Vector3(0.12f, 0.28f, 0.12f), new Color(0.44f, 0.25f, 0.12f));
            CreatePart(root.transform, "DresserTable Leg Right", PrimitiveType.Cube, new Vector3(0.62f, 0.03f, -0.12f), new Vector3(0.12f, 0.28f, 0.12f), new Color(0.44f, 0.25f, 0.12f));
            CreateMilkBottle(root.transform, "DresserTable Milk Bottle A", new Vector3(-0.42f, 1.12f, -0.08f), 0.42f);
            CreateMilkBottle(root.transform, "DresserTable Milk Bottle B", new Vector3(-0.12f, 1.15f, -0.08f), 0.48f);
            CreateMilkBottle(root.transform, "DresserTable Milk Bottle C", new Vector3(0.18f, 1.1f, -0.08f), 0.38f);
            CreatePart(root.transform, "DresserTable Blender Base", PrimitiveType.Cube, new Vector3(0.57f, 1.02f, -0.08f), new Vector3(0.28f, 0.18f, 0.18f), new Color(0.9f, 0.78f, 0.58f));
            CreatePart(root.transform, "DresserTable Blender Jar", PrimitiveType.Capsule, new Vector3(0.57f, 1.27f, -0.08f), new Vector3(0.15f, 0.22f, 0.1f), new Color(0.75f, 0.9f, 0.98f));
            CreatePart(root.transform, "DresserTable Blender Milk Fill", PrimitiveType.Sphere, new Vector3(0.57f, 1.19f, -0.16f), new Vector3(0.12f, 0.06f, 0.03f), new Color(0.98f, 0.94f, 0.78f));
            CreateCheeseBlock(root.transform, "DresserTable Cheese Sample", new Vector3(0.84f, 1.02f, -0.08f), 0.18f);
            return root;
        }

        private static GameObject BuildChalkboard()
        {
            var root = new GameObject("Chalkboard");
            CreatePart(root.transform, "Chalkboard Board", PrimitiveType.Cube, Vector3.zero, new Vector3(1.05f, 0.78f, 0.08f), new Color(0.14f, 0.24f, 0.18f));
            CreatePart(root.transform, "Chalkboard Frame Top", PrimitiveType.Cube, new Vector3(0f, 0.43f, -0.045f), new Vector3(1.22f, 0.09f, 0.1f), new Color(0.56f, 0.33f, 0.16f));
            CreatePart(root.transform, "Chalkboard Frame Bottom", PrimitiveType.Cube, new Vector3(0f, -0.43f, -0.045f), new Vector3(1.22f, 0.09f, 0.1f), new Color(0.56f, 0.33f, 0.16f));
            CreatePart(root.transform, "Chalkboard Frame Left", PrimitiveType.Cube, new Vector3(-0.61f, 0f, -0.045f), new Vector3(0.09f, 0.88f, 0.1f), new Color(0.56f, 0.33f, 0.16f));
            CreatePart(root.transform, "Chalkboard Frame Right", PrimitiveType.Cube, new Vector3(0.61f, 0f, -0.045f), new Vector3(0.09f, 0.88f, 0.1f), new Color(0.56f, 0.33f, 0.16f));
            CreatePart(root.transform, "Chalkboard Hanger Left", PrimitiveType.Cube, new Vector3(-0.32f, 0.64f, -0.03f), new Vector3(0.035f, 0.38f, 0.035f), Quaternion.Euler(0f, 0f, -35f), new Color(0.64f, 0.42f, 0.2f));
            CreatePart(root.transform, "Chalkboard Hanger Right", PrimitiveType.Cube, new Vector3(0.32f, 0.64f, -0.03f), new Vector3(0.035f, 0.38f, 0.035f), Quaternion.Euler(0f, 0f, 35f), new Color(0.64f, 0.42f, 0.2f));
            CreateWorldLabel(root.transform, "Chalkboard Milk Text", "Milk\nis\nMagic", new Vector3(0f, 0.05f, -0.095f), 0.12f, new Color(1f, 0.9f, 0.62f));
            CreateStarDoodle(root.transform, "Chalkboard Star Doodle A", new Vector3(-0.4f, -0.22f, -0.1f), 0.075f, new Color(1f, 0.86f, 0.3f));
            CreateStarDoodle(root.transform, "Chalkboard Star Doodle B", new Vector3(0.42f, 0.26f, -0.1f), 0.06f, new Color(1f, 0.92f, 0.48f));
            CreatePart(root.transform, "Chalkboard Chalk Stick", PrimitiveType.Cube, new Vector3(0.32f, -0.35f, -0.11f), new Vector3(0.26f, 0.025f, 0.025f), new Color(0.96f, 0.9f, 0.72f));
            return root;
        }

        private static void CreateCurtain(Transform parent, string name, float x, bool flip)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = Vector3.zero;
            CreatePart(root, $"{name} Panel", PrimitiveType.Cube, new Vector3(x, -0.02f, -0.16f), new Vector3(0.34f, 1.46f, 0.075f), new Color(1f, 0.9f, 0.73f));
            CreatePart(root, $"{name} Inner Fold", PrimitiveType.Cube, new Vector3(x + (flip ? -0.08f : 0.08f), -0.02f, -0.2f), new Vector3(0.055f, 1.38f, 0.055f), new Color(0.95f, 0.8f, 0.6f));
            CreatePart(root, $"{name} Outer Fold", PrimitiveType.Cube, new Vector3(x + (flip ? 0.09f : -0.09f), 0.02f, -0.2f), new Vector3(0.05f, 1.28f, 0.055f), new Color(1f, 0.94f, 0.78f));
            CreatePart(root, $"{name} Tie", PrimitiveType.Cube, new Vector3(x + (flip ? -0.05f : 0.05f), -0.18f, -0.24f), new Vector3(0.38f, 0.085f, 0.06f), new Color(0.8f, 0.48f, 0.24f));
            CreatePart(root, $"{name} Bottom Puff", PrimitiveType.Sphere, new Vector3(x, -0.78f, -0.18f), new Vector3(0.36f, 0.12f, 0.06f), new Color(1f, 0.92f, 0.76f));
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
