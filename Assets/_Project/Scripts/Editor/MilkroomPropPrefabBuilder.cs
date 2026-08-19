using CheeseTama.Utilities;
using CheeseTama.Core;
using CheeseTama.Environment;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CheeseTama.Editor
{
    public static class MilkroomPropPrefabBuilder
    {
        private const string PropsRoot = "Assets/Environments/Milkroom/Props";
        private const string WindowReplacementRoot = PropsRoot + "/Window_Assets/Replacement";
        private const string WindowModelPath = WindowReplacementRoot + "/WindowReplacement.fbx";
        private const string WindowTexturePath = WindowReplacementRoot + "/window.JPEG";
        private const string WindowMaterialPath = WindowReplacementRoot + "/WindowReplacement.mat";
        private const string RugReplacementRoot = PropsRoot + "/Rug_Assets/Replacement";
        private const string RugModelPath = RugReplacementRoot + "/RugReplacement.fbx";
        private const string RugTexturePath = RugReplacementRoot + "/Rug.png";
        private const string RugMaterialPath = RugReplacementRoot + "/RugReplacement.mat";
        private const float RugPlanarScale = 1.575f;
        private const string MilkShelfReplacementRoot = PropsRoot + "/MilkShelf_Assets/Replacement";
        private const string MilkShelfModelPath = MilkShelfReplacementRoot + "/MilkShelfReplacement.fbx";
        private const string MilkShelfTexturePath = MilkShelfReplacementRoot + "/shelf.JPEG";
        private const string MilkShelfMaterialPath = MilkShelfReplacementRoot + "/MilkShelfReplacement.mat";
        private const string DresserReplacementRoot = PropsRoot + "/DresserTable_Assets/Replacement";
        private const string DresserTableModelPath = DresserReplacementRoot + "/MilkCabinetReplacement.fbx";
        private const string DresserTableTexturePath = DresserReplacementRoot + "/milkcabinet.JPEG";
        private const string DresserTableMaterialPath = DresserReplacementRoot + "/MilkCabinetReplacement.mat";
        private const string ChalkboardModelPath = PropsRoot + "/Chalkboard_Assets/selected.glb";
        private const string CozyChairModelPath = PropsRoot + "/CozyChair_Assets/selected.glb";
        private const string CozyChairTexturePath = PropsRoot + "/CozyChair_Assets/CozyChairWhite.png";
        private const string CozyChairMaterialPath = PropsRoot + "/CozyChair_Assets/CozyChairWhite.mat";
        private const string CleanCheeseCushionMeshPath = PropsRoot + "/CozyChair_Assets/CleanCheeseCushion.asset";
        private const string CleanCheeseCushionMaterialPath = PropsRoot + "/CozyChair_Assets/CleanCheeseCushion.mat";
        private const string CleanCheeseCushionOverlayName = "Clean Cheese Cushion Overlay";
        private const string CozyChairPrefabPath = PropsRoot + "/CozyChair.prefab";
        private const string FridgeModelPath = PropsRoot + "/Fridge_Assets/selected.glb";
        private const string FridgeTexturePath = PropsRoot + "/Fridge_Assets/FridgeWhite.png";
        private const string FridgeMaterialPath = PropsRoot + "/Fridge_Assets/FridgeWhite.mat";
        private const string FridgePrefabPath = PropsRoot + "/Fridge.prefab";
        private const string ChalkboardTexturePath = PropsRoot + "/Chalkboard_Assets/ChalkboardCrisp.png";
        private const string ChalkboardMaterialPath = PropsRoot + "/Chalkboard_Assets/ChalkboardCrisp.mat";
        private const string ChalkboardPrefabPath = PropsRoot + "/Chalkboard.prefab";

        [MenuItem("CheeseTama/밀크룸 소품 프리팹 생성")]
        public static void BuildMilkroomPropPrefabs()
        {
            EnsurePropsFolder();
            SavePrefab(BuildWindow(), $"{PropsRoot}/Window.prefab");
            SavePrefab(BuildRug(), $"{PropsRoot}/Rug.prefab");
            SavePrefab(BuildMilkShelf(), $"{PropsRoot}/MilkShelf.prefab");
            SavePrefab(BuildDresserTable(), $"{PropsRoot}/DresserTable.prefab");
            SavePrefab(BuildChalkboard(), $"{PropsRoot}/Chalkboard.prefab");
            ApplyNaturalMilkroomMaterialsInternal();
            MilkroomBuildAssetOptimizer.ApplyOptimization(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milkroom prop prefabs generated: Window, Rug, MilkShelf, DresserTable, Chalkboard");
        }

        [MenuItem("CheeseTama/Build Rug Prefab")]
        public static void BuildRugPrefab()
        {
            EnsurePropsFolder();
            SavePrefab(BuildRug(), $"{PropsRoot}/Rug.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milkroom rug prefab rebuilt from RugReplacement.fbx and Rug.png");
        }

        [MenuItem("CheeseTama/Build Window Prefab")]
        public static void BuildWindowPrefab()
        {
            EnsurePropsFolder();
            SavePrefab(BuildWindow(), $"{PropsRoot}/Window.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milkroom window prefab rebuilt from WindowReplacement.fbx and window.JPEG");
        }

        [MenuItem("CheeseTama/Build Dresser Table Prefab")]
        public static void BuildDresserTablePrefab()
        {
            EnsurePropsFolder();
            SavePrefab(BuildDresserTable(), $"{PropsRoot}/DresserTable.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milkroom dresser prefab rebuilt from MilkCabinetReplacement.fbx and milkcabinet.JPEG");
        }

        [MenuItem("CheeseTama/Build Milk Shelf Prefab")]
        public static void BuildMilkShelfPrefab()
        {
            EnsurePropsFolder();
            SavePrefab(BuildMilkShelf(), $"{PropsRoot}/MilkShelf.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milkroom shelf prefab rebuilt from MilkShelfReplacement.fbx and shelf.JPEG");
        }

        [MenuItem("CheeseTama/Apply Natural Milkroom Materials")]
        public static void ApplyNaturalMilkroomMaterials()
        {
            ApplyNaturalMilkroomMaterialsInternal();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Natural Milkroom materials applied to the chair, fridge, and chalkboard.");
        }

        [MenuItem("CheeseTama/Apply Current Milkroom Visual Placements")]
        public static void ApplyCurrentMilkroomVisualPlacements()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                throw new System.InvalidOperationException(
                    "Save or discard the active scene changes before applying Milkroom visual placements.");
            }

            var originalScenePath = activeScene.path;
            ApplyNaturalMilkroomMaterialsInternal();
            var placeMethod = typeof(StarterSceneBuilder).GetMethod(
                "PlaceGeneratedProp",
                BindingFlags.Static | BindingFlags.NonPublic);
            var ensureLightMethod = typeof(StarterSceneBuilder).GetMethod(
                "EnsureLight",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (placeMethod == null || ensureLightMethod == null)
            {
                throw new System.MissingMethodException("StarterSceneBuilder visual placement helpers were not found.");
            }

            foreach (var scenePath in new[]
                     {
                         "Assets/_Project/Scenes/Milkroom.unity",
                         "Assets/_Project/Scenes/Debug.unity"
                     })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var background = GameObject.Find("Milkroom Background")?.transform;
                if (background == null)
                {
                    throw new MissingReferenceException($"Milkroom Background was not found in {scenePath}.");
                }

                placeMethod.Invoke(null, new object[]
                {
                    background,
                    $"{PropsRoot}/Window.prefab",
                    "Window_Model",
                    new Vector3(0.45f, 0f, 2.366f),
                    1.72f,
                    180f,
                    false,
                    -0.15f,
                    -2.13f
                });
                placeMethod.Invoke(null, new object[]
                {
                    background,
                    $"{PropsRoot}/MilkShelf.prefab",
                    "MilkShelf_Model",
                    new Vector3(2.65f, 0f, 2.295f),
                    1.3f,
                    180f,
                    false,
                    -0.15f,
                    -2.13f
                });
                placeMethod.Invoke(null, new object[]
                {
                    background,
                    $"{PropsRoot}/DresserTable.prefab",
                    "DresserTable_Model",
                    new Vector3(2.807f, 0f, 1.18f),
                    1.5f,
                    200f,
                    true,
                    0f,
                    -2.13f
                });
                ensureLightMethod.Invoke(null, null);
                var themeController = Object.FindFirstObjectByType<MilkroomThemeController>();
                if (themeController != null)
                {
                    themeController.ApplyCurrentTheme();
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrWhiteSpace(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        private static GameObject BuildWindow()
        {
            EnsureImportedModelSettings(WindowModelPath);
            var imported = BuildImportedGlbProp("Window", WindowModelPath, Quaternion.Euler(-90f, 0f, 0f));
            if (imported != null)
            {
                ApplyImportedMaterial(
                    imported,
                    WindowTexturePath,
                    WindowMaterialPath,
                    "WindowReplacement",
                    0.16f);
                return imported;
            }

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
            EnsureImportedModelSettings(RugModelPath);
            var imported = BuildImportedGlbProp("Rug", RugModelPath, Quaternion.Euler(-90f, 0f, 0f));
            if (imported != null)
            {
                var model = imported.transform.Find("Rug_ImportedModel");
                if (model != null)
                {
                    // The replacement mesh is proportionally thick. Preserve its authored
                    // vertical axis while widening only the floor plane so a thin placed
                    // rug still occupies the intended footprint.
                    model.localScale = new Vector3(RugPlanarScale, RugPlanarScale, 1f);
                }

                ApplyImportedMaterial(
                    imported,
                    RugTexturePath,
                    RugMaterialPath,
                    "RugReplacement",
                    0.2f,
                    new Color(0.74f, 0.72f, 0.68f, 1f));
                return imported;
            }

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

        private static void EnsureImportedModelSettings(string modelPath)
        {
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer))
            {
                return;
            }

            var changed = false;
            changed |= SetIfDifferent(importer.globalScale, 1f, value => importer.globalScale = value);
            changed |= SetIfDifferent(importer.importAnimation, false, value => importer.importAnimation = value);
            changed |= SetIfDifferent(importer.isReadable, false, value => importer.isReadable = value);
            changed |= SetIfDifferent(importer.addCollider, false, value => importer.addCollider = value);
            changed |= SetIfDifferent(importer.generateSecondaryUV, false, value => importer.generateSecondaryUV = value);
            changed |= SetIfDifferent(importer.importBlendShapes, false, value => importer.importBlendShapes = value);
            changed |= SetIfDifferent(importer.importCameras, false, value => importer.importCameras = value);
            changed |= SetIfDifferent(importer.importLights, false, value => importer.importLights = value);
            changed |= SetIfDifferent(importer.importTangents, ModelImporterTangents.None, value => importer.importTangents = value);
            changed |= SetIfDifferent(importer.materialImportMode, ModelImporterMaterialImportMode.None, value => importer.materialImportMode = value);
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static bool SetIfDifferent<T>(T current, T desired, System.Action<T> assign)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(current, desired))
            {
                return false;
            }

            assign(desired);
            return true;
        }

        private static void ApplyImportedMaterial(
            GameObject root,
            string texturePath,
            string materialPath,
            string materialName,
            float smoothness,
            Color? tint = null)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogWarning($"Imported prop texture is missing at {texturePath}; keeping the imported material.");
                return;
            }

            var shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")
                : Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("No compatible Lit shader was found for the replacement rug.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            var baseColor = tint ?? Color.white;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            EditorUtility.SetDirty(material);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index += 1)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static GameObject BuildDresserTable()
        {
            EnsureImportedModelSettings(DresserTableModelPath);
            var imported = BuildImportedGlbProp("DresserTable", DresserTableModelPath, Quaternion.Euler(-90f, 0f, 0f));
            if (imported != null)
            {
                ApplyImportedMaterial(
                    imported,
                    DresserTableTexturePath,
                    DresserTableMaterialPath,
                    "MilkCabinetReplacement",
                    0.18f);
                return imported;
            }

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

        private static GameObject BuildMilkShelf()
        {
            EnsureImportedModelSettings(MilkShelfModelPath);
            var imported = BuildImportedGlbProp("MilkShelf", MilkShelfModelPath, Quaternion.Euler(-90f, 0f, 0f));
            if (imported != null)
            {
                ApplyImportedMaterial(
                    imported,
                    MilkShelfTexturePath,
                    MilkShelfMaterialPath,
                    "MilkShelfReplacement",
                    0.18f);
                return imported;
            }

            return new GameObject("MilkShelf");
        }

        private static GameObject BuildChalkboard()
        {
            var imported = BuildImportedGlbProp("Chalkboard", ChalkboardModelPath, Quaternion.Euler(0f, -90f, 0f));
            if (imported != null)
            {
                var material = EnsureNaturalImportedMaterial(
                    ChalkboardModelPath,
                    ChalkboardTexturePath,
                    ChalkboardMaterialPath,
                    "ChalkboardCrisp",
                    0f,
                    0.5f,
                    false);
                AssignMaterial(imported, material);
                return imported;
            }

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
            CreateStarDoodle(root.transform, "Chalkboard Star Doodle A", new Vector3(-0.42f, -0.22f, -0.11f), 0.075f, new Color(1f, 0.86f, 0.3f));
            CreateStarDoodle(root.transform, "Chalkboard Star Doodle B", new Vector3(0.42f, 0.27f, -0.11f), 0.06f, new Color(1f, 0.92f, 0.48f));
            CreateCheeseBlock(root.transform, "Chalkboard Tiny Cheese Doodle", new Vector3(0.39f, -0.28f, -0.12f), 0.13f);
            CreatePart(root.transform, "Chalkboard Chalk Stick", PrimitiveType.Cube, new Vector3(0.08f, -0.36f, -0.12f), new Vector3(0.36f, 0.025f, 0.025f), new Color(0.96f, 0.9f, 0.72f));
            return root;
        }

        private static void ApplyNaturalMilkroomMaterialsInternal()
        {
            var chairMaterial = EnsureNaturalImportedMaterial(
                CozyChairModelPath,
                CozyChairTexturePath,
                CozyChairMaterialPath,
                "CozyChairWhite",
                0f,
                0.72f,
                false);
            var fridgeMaterial = EnsureNaturalImportedMaterial(
                FridgeModelPath,
                FridgeTexturePath,
                FridgeMaterialPath,
                "FridgeWhite",
                0f,
                0.62f,
                false);
            var chalkboardMaterial = EnsureNaturalImportedMaterial(
                ChalkboardModelPath,
                ChalkboardTexturePath,
                ChalkboardMaterialPath,
                "ChalkboardCrisp",
                0f,
                0.5f,
                false);

            AssignMaterialToPrefab(CozyChairPrefabPath, chairMaterial);
            AssignMaterialToPrefab(FridgePrefabPath, fridgeMaterial);
            AssignMaterialToPrefab(ChalkboardPrefabPath, chalkboardMaterial);
        }

        private static Material EnsureNaturalImportedMaterial(
            string sourceModelPath,
            string texturePath,
            string materialPath,
            string materialName,
            float metallic,
            float roughness,
            bool preserveSurfaceMaps)
        {
            EnsureNaturalTextureSettings(texturePath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var sourceMaterial = FindFirstMaterial(sourceModelPath);
            if (texture == null || sourceMaterial == null)
            {
                Debug.LogWarning($"Natural Milkroom material inputs are missing: {sourceModelPath}, {texturePath}");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(sourceMaterial);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                EditorUtility.CopySerialized(sourceMaterial, material);
            }

            material.name = materialName;
            SetTextureIfPresent(material, "baseColorTexture", texture);
            SetTextureIfPresent(material, "_BaseMap", texture);
            SetTextureIfPresent(material, "_MainTex", texture);
            SetColorIfPresent(material, "baseColorFactor", Color.white);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "metallicFactor", metallic);
            SetFloatIfPresent(material, "roughnessFactor", roughness);
            SetFloatIfPresent(material, "_Metallic", metallic);
            SetFloatIfPresent(material, "_Smoothness", 1f - roughness);
            SetFloatIfPresent(material, "_Glossiness", 1f - roughness);
            SetColorIfPresent(material, "emissiveFactor", Color.black);
            SetColorIfPresent(material, "_EmissionColor", Color.black);
            if (!preserveSurfaceMaps)
            {
                SetTextureIfPresent(material, "metallicRoughnessTexture", null);
                SetTextureIfPresent(material, "normalTexture", null);
                SetTextureIfPresent(material, "_MetallicGlossMap", null);
                SetTextureIfPresent(material, "_BumpMap", null);
                material.DisableKeyword("_METALLICGLOSSMAP");
                material.DisableKeyword("_NORMALMAP");
            }
            material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material FindFirstMaterial(string modelPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Material material)
                {
                    return material;
                }
            }

            return null;
        }

        private static void EnsureNaturalTextureSettings(string texturePath)
        {
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(texturePath) is TextureImporter importer))
            {
                return;
            }

            var changed = false;
            changed |= SetIfDifferent(importer.textureType, TextureImporterType.Default, value => importer.textureType = value);
            changed |= SetIfDifferent(importer.sRGBTexture, true, value => importer.sRGBTexture = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, true, value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(importer.isReadable, false, value => importer.isReadable = value);
            changed |= SetIfDifferent(importer.maxTextureSize, 2048, value => importer.maxTextureSize = value);
            changed |= SetIfDifferent(importer.textureCompression, TextureImporterCompression.Compressed, value => importer.textureCompression = value);
            changed |= SetIfDifferent(importer.wrapMode, TextureWrapMode.Repeat, value => importer.wrapMode = value);
            changed |= SetIfDifferent(importer.alphaSource, TextureImporterAlphaSource.None, value => importer.alphaSource = value);
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void AssignMaterialToPrefab(string prefabPath, Material material)
        {
            if (material == null || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                AssignMaterial(root, material);
                if (prefabPath == CozyChairPrefabPath)
                {
                    EnsureCleanCheeseCushionOverlay(root, true);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureCleanCheeseCushionOverlay(GameObject prefabRoot)
        {
            EnsureCleanCheeseCushionOverlay(prefabRoot, false);
        }

        private static void EnsureCleanCheeseCushionOverlay(GameObject prefabRoot, bool regenerateAssets)
        {
            if (prefabRoot == null)
            {
                return;
            }

            var mesh = EnsureCleanCheeseCushionMesh(regenerateAssets);
            var material = EnsureCleanCheeseCushionMaterial(regenerateAssets);
            if (mesh == null || material == null)
            {
                return;
            }

            var parent = prefabRoot.transform.Find("scene") ?? prefabRoot.transform;
            Transform overlay = null;
            for (var index = parent.childCount - 1; index >= 0; index -= 1)
            {
                var child = parent.GetChild(index);
                if (child.name != CleanCheeseCushionOverlayName)
                {
                    continue;
                }

                if (overlay == null)
                {
                    overlay = child;
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            if (overlay == null)
            {
                overlay = new GameObject(CleanCheeseCushionOverlayName).transform;
                overlay.SetParent(parent, false);
            }

            overlay.localPosition = Vector3.zero;
            overlay.localRotation = Quaternion.identity;
            overlay.localScale = Vector3.one;

            var filter = overlay.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = overlay.gameObject.AddComponent<MeshFilter>();
            }

            var renderer = overlay.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = overlay.gameObject.AddComponent<MeshRenderer>();
            }

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            foreach (var collider in overlay.GetComponents<Collider>())
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Mesh EnsureCleanCheeseCushionMesh(bool regenerateAsset)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(CleanCheeseCushionMeshPath);
            if (existing != null && !regenerateAsset)
            {
                return existing;
            }

            const int columns = 64;
            const int rows = 44;
            const float centerX = 0.172f;
            const float centerY = 0.078f;
            const float centerZ = -0.05f;
            const float halfWidth = 0.292f;
            const float halfHeight = 0.168f;
            const float backX = 0.132f;

            var vertices = new List<Vector3>((columns + 1) * (rows + 1) + ((columns + rows) * 4));
            var uvs = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(columns * rows * 6 + ((columns + rows) * 12));

            for (var row = 0; row <= rows; row += 1)
            {
                var v = (row / (float)rows * 2f) - 1f;
                var absoluteV = Mathf.Abs(v);
                var widthFactor = 1f;
                if (absoluteV > 0.76f)
                {
                    var cornerT = Mathf.Clamp01((absoluteV - 0.76f) / 0.24f);
                    widthFactor = 0.76f + (0.24f * Mathf.Sqrt(Mathf.Max(0f, 1f - (cornerT * cornerT))));
                }

                for (var column = 0; column <= columns; column += 1)
                {
                    var u = (column / (float)columns * 2f) - 1f;
                    var y = centerY + (v * halfHeight);
                    var z = centerZ + (u * halfWidth * widthFactor);
                    var puff = 0.022f * (1f - (u * u)) * (1f - (v * v));
                    var dimple = ResolveCheeseDimpleDepth(u, v);
                    vertices.Add(new Vector3(centerX + puff - dimple, y, z));
                    uvs.Add(new Vector2((u + 1f) * 0.5f, (v + 1f) * 0.5f));
                }
            }

            for (var row = 0; row < rows; row += 1)
            {
                for (var column = 0; column < columns; column += 1)
                {
                    var a = (row * (columns + 1)) + column;
                    var b = a + 1;
                    var c = a + columns + 1;
                    var d = c + 1;
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            var boundary = BuildCushionBoundaryIndices(columns, rows);
            for (var index = 0; index < boundary.Count; index += 1)
            {
                var next = (index + 1) % boundary.Count;
                var frontA = vertices[boundary[index]];
                var frontB = vertices[boundary[next]];
                var backAIndex = vertices.Count;
                vertices.Add(new Vector3(backX, centerY + ((frontA.y - centerY) * 0.96f), centerZ + ((frontA.z - centerZ) * 0.96f)));
                uvs.Add(Vector2.zero);
                var backBIndex = vertices.Count;
                vertices.Add(new Vector3(backX, centerY + ((frontB.y - centerY) * 0.96f), centerZ + ((frontB.z - centerZ) * 0.96f)));
                uvs.Add(Vector2.zero);

                triangles.Add(boundary[index]);
                triangles.Add(backAIndex);
                triangles.Add(boundary[next]);
                triangles.Add(boundary[next]);
                triangles.Add(backAIndex);
                triangles.Add(backBIndex);
            }

            var generated = new Mesh
            {
                name = "CleanCheeseCushion"
            };
            generated.SetVertices(vertices);
            generated.SetUVs(0, uvs);
            generated.SetTriangles(triangles, 0, true);
            generated.RecalculateNormals();
            generated.RecalculateTangents();
            generated.RecalculateBounds();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, CleanCheeseCushionMeshPath);
                return generated;
            }

            EditorUtility.CopySerialized(generated, existing);
            existing.name = "CleanCheeseCushion";
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(generated);
            return existing;
        }

        private static List<int> BuildCushionBoundaryIndices(int columns, int rows)
        {
            var boundary = new List<int>((columns + rows) * 2);
            for (var column = 0; column <= columns; column += 1)
            {
                boundary.Add(column);
            }

            for (var row = 1; row <= rows; row += 1)
            {
                boundary.Add((row * (columns + 1)) + columns);
            }

            for (var column = columns - 1; column >= 0; column -= 1)
            {
                boundary.Add((rows * (columns + 1)) + column);
            }

            for (var row = rows - 1; row > 0; row -= 1)
            {
                boundary.Add(row * (columns + 1));
            }

            return boundary;
        }

        private static float ResolveCheeseDimpleDepth(float u, float v)
        {
            var depth = 0f;
            depth += ResolveRoundDimple(u, v, -0.54f, 0.32f, 0.19f, 0.020f);
            depth += ResolveRoundDimple(u, v, 0.32f, 0.20f, 0.17f, 0.018f);
            depth += ResolveRoundDimple(u, v, -0.10f, -0.38f, 0.21f, 0.022f);
            depth += ResolveRoundDimple(u, v, 0.54f, -0.24f, 0.14f, 0.014f);
            return Mathf.Min(depth, 0.025f);
        }

        private static float ResolveRoundDimple(float u, float v, float centerU, float centerV, float radius, float depth)
        {
            var normalizedDistance = Vector2.Distance(new Vector2(u, v), new Vector2(centerU, centerV)) / radius;
            if (normalizedDistance >= 1f)
            {
                return 0f;
            }

            var falloff = 0.5f + (0.5f * Mathf.Cos(normalizedDistance * Mathf.PI));
            return depth * falloff;
        }

        private static Material EnsureCleanCheeseCushionMaterial(bool regenerateAsset)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(CleanCheeseCushionMaterialPath);
            if (material != null && !regenerateAsset)
            {
                return material;
            }

            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("Standard shader is unavailable; clean cheese cushion material was not created.");
                return null;
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, CleanCheeseCushionMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.name = "CleanCheeseCushion";
            material.color = new Color(1f, 0.66f, 0.075f, 1f);
            SetColorIfPresent(material, "_BaseColor", material.color);
            SetColorIfPresent(material, "_EmissionColor", Color.black);
            SetFloatIfPresent(material, "_Metallic", 0f);
            SetFloatIfPresent(material, "_Glossiness", 0.28f);
            SetFloatIfPresent(material, "_Smoothness", 0.28f);
            material.DisableKeyword("_EMISSION");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_METALLICGLOSSMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignMaterial(GameObject root, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index += 1)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetColorIfPresent(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static GameObject BuildImportedGlbProp(string rootName, string assetPath, Quaternion modelRotation)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (modelAsset == null)
            {
                return null;
            }

            var root = new GameObject(rootName);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            if (model == null)
            {
                model = Object.Instantiate(modelAsset);
            }

            model.name = $"{rootName}_ImportedModel";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = modelRotation;
            model.transform.localScale = Vector3.one;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

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
