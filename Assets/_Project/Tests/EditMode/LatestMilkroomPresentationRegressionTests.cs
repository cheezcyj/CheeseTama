using System.Linq;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class LatestMilkroomPresentationRegressionTests
    {
        private const string SpeechBubbleName = "CheeseTama Speech Bubble";
        private const string SpeechTailName = "CheeseTama Speech Tail";
        private const string LegacyDecorationName = "Equipped Decoration Visual";
        private const string ShelfPrefabPath = "Assets/Environments/Milkroom/Props/MilkShelf.prefab";
        private const string ShelfPrefabGuid = "dda687a6d86898546b582470c9262632";
        private const string ShelfModelPath =
            "Assets/Environments/Milkroom/Props/MilkShelf_Assets/Replacement/MilkShelfReplacement.fbx";
        private const string ShelfTexturePath =
            "Assets/Environments/Milkroom/Props/MilkShelf_Assets/Replacement/shelf.JPEG";
        private const string ShelfMaterialPath =
            "Assets/Environments/Milkroom/Props/MilkShelf_Assets/Replacement/MilkShelfReplacement.mat";
        private const string DresserPrefabPath = "Assets/Environments/Milkroom/Props/DresserTable.prefab";
        private const string ChairPrefabPath = "Assets/Environments/Milkroom/Props/CozyChair.prefab";
        private const string ChairTexturePath =
            "Assets/Environments/Milkroom/Props/CozyChair_Assets/CozyChairWhite.png";
        private const string ChairMaterialPath =
            "Assets/Environments/Milkroom/Props/CozyChair_Assets/CozyChairWhite.mat";
        private const string CleanCushionName = "Clean Cheese Cushion Overlay";
        private const string CleanCushionMeshPath =
            "Assets/Environments/Milkroom/Props/CozyChair_Assets/CleanCheeseCushion.asset";
        private const string CleanCushionMaterialPath =
            "Assets/Environments/Milkroom/Props/CozyChair_Assets/CleanCheeseCushion.mat";
        private const string FridgePrefabPath = "Assets/Environments/Milkroom/Props/Fridge.prefab";
        private const string FridgeTexturePath =
            "Assets/Environments/Milkroom/Props/Fridge_Assets/FridgeWhite.png";
        private const string FridgeMaterialPath =
            "Assets/Environments/Milkroom/Props/Fridge_Assets/FridgeWhite.mat";
        private const string ChalkboardPrefabPath = "Assets/Environments/Milkroom/Props/Chalkboard.prefab";
        private const string ChalkboardTexturePath =
            "Assets/Environments/Milkroom/Props/Chalkboard_Assets/ChalkboardCrisp.png";
        private const string ChalkboardMaterialPath =
            "Assets/Environments/Milkroom/Props/Chalkboard_Assets/ChalkboardCrisp.mat";
        private const string WindowPrefabPath = "Assets/Environments/Milkroom/Props/Window.prefab";
        private const string WindowPrefabGuid = "70764bbb87803b44c85315a6865a6f8a";
        private const string WindowModelPath =
            "Assets/Environments/Milkroom/Props/Window_Assets/Replacement/WindowReplacement.fbx";
        private const string WindowTexturePath =
            "Assets/Environments/Milkroom/Props/Window_Assets/Replacement/window.JPEG";
        private const string WindowMaterialPath =
            "Assets/Environments/Milkroom/Props/Window_Assets/Replacement/WindowReplacement.mat";
        private const int OptimizedMeshMaxVertexCount = 65535;
        private const ulong ShelfMaxTriangleCount = 75000UL;
        private const ulong WindowMaxTriangleCount = 75000UL;
        private const float LocalBoundsTolerance = 0.001f;
        private static readonly Bounds ShelfExpectedLocalBounds = new Bounds(
            new Vector3(0f, 0f, 0.46694946f),
            new Vector3(1.0000001f, 0.32385266f, 0.9338989f));
        private static readonly Bounds WindowExpectedLocalBounds = new Bounds(
            new Vector3(0f, 0f, 0.44726563f),
            new Vector3(0.9999696f, 0.15559378f, 0.89453125f));
        private const float FloatTolerance = 0.0005f;

        [Test]
        public void SpeechBubbleBuilderCreatesOneBottomNonBlockingTailAndLowerOffsets()
        {
            var canvasObject = new GameObject(
                "Speech Bubble Regression Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                var ensure = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureCheeseTamaSpeechBubble",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(ensure, Is.Not.Null);

                ensure.Invoke(null, new object[] { canvasObject.transform, null });
                ensure.Invoke(null, new object[] { canvasObject.transform, null });

                var bubble = canvasObject.transform.Find(SpeechBubbleName);
                Assert.That(bubble, Is.Not.Null);
                var tails = bubble.GetComponentsInChildren<Transform>(true)
                    .Where(candidate => candidate.name == SpeechTailName)
                    .ToArray();
                Assert.That(tails, Has.Length.EqualTo(1));

                var bubbleRect = bubble.GetComponent<RectTransform>();
                var tailRect = tails[0].GetComponent<RectTransform>();
                Assert.That(bubbleRect, Is.Not.Null);
                Assert.That(tailRect, Is.Not.Null);
                var tailBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    bubbleRect,
                    tailRect);
                Assert.That(
                    tailBounds.center.y,
                    Is.LessThanOrEqualTo(bubbleRect.rect.yMin + 0.01f),
                    "The pointer must remain attached to the bottom edge of the bubble.");

                var canvasGroup = bubble.GetComponent<CanvasGroup>();
                Assert.That(canvasGroup, Is.Not.Null);
                Assert.That(bubble.gameObject.activeSelf, Is.False);
                Assert.That(canvasGroup.alpha, Is.Zero);
                Assert.That(canvasGroup.interactable, Is.False);
                Assert.That(canvasGroup.blocksRaycasts, Is.False);
                foreach (var graphic in bubble.GetComponentsInChildren<Graphic>(true))
                {
                    Assert.That(graphic.raycastTarget, Is.False, graphic.name);
                }

                var controller = canvasObject.GetComponent<CheeseTamaSpeechBubbleController>();
                Assert.That(controller, Is.Not.Null);
                Assert.That(
                    GetPrivateField<Vector3>(controller, "worldOffset"),
                    Is.EqualTo(new Vector3(0f, 1.45f, 0f)));
                Assert.That(
                    GetPrivateField<Vector2>(controller, "screenOffset"),
                    Is.EqualTo(new Vector2(0f, 4f)));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void DefaultDecorationRefreshRemovesLegacyWindowAndShelfWithoutCreatingDefaults()
        {
            Assert.That(
                GameManager.Instance,
                Is.Null,
                "This test requires DecorationRoomPresenter's default-snapshot path.");

            var root = new GameObject("Default Decoration Regression Root");
            root.SetActive(false);
            try
            {
                var windowAnchor = CreateAnchor(root.transform, "Decoration Window Anchor");
                var shelfAnchor = CreateAnchor(root.transform, "Decoration Shelf Anchor");
                CreateLegacyPlaceholder(windowAnchor);
                CreateLegacyPlaceholder(shelfAnchor);

                var presenter = root.AddComponent<DecorationRoomPresenter>();
                presenter.Configure(null, null, null, windowAnchor, shelfAnchor, null);

                Assert.That(windowAnchor.Find(LegacyDecorationName), Is.Null);
                Assert.That(shelfAnchor.Find(LegacyDecorationName), Is.Null);
                Assert.That(
                    windowAnchor.Find("Equipped Window Decoration"),
                    Is.Null,
                    "The default cream curtain duplicates the window art and must stay hidden.");
                Assert.That(
                    shelfAnchor.Find("Equipped Shelf Decoration"),
                    Is.Null,
                    "The default cheese clock duplicates the supplied shelf and must stay hidden.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(ChairTexturePath, ChairMaterialPath, 0f, 0.72f, false)]
        [TestCase(FridgeTexturePath, FridgeMaterialPath, 0f, 0.62f, false)]
        [TestCase(ChalkboardTexturePath, ChalkboardMaterialPath, 0f, 0.50f, false)]
        public void BrightenedPropMaterialsUseExactTextureAndNaturalPbrContract(
            string texturePath,
            string materialPath,
            float expectedMetallic,
            float expectedRoughness,
            bool expectedSurfaceMaps)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            Assert.That(texture, Is.Not.Null, texturePath);
            Assert.That(material, Is.Not.Null, materialPath);
            Assert.That(material.shader, Is.Not.Null, materialPath);
            Assert.That(material.shader.name, Is.EqualTo("glTF/PbrMetallicRoughness"), materialPath);
            Assert.That(material.HasProperty("baseColorTexture"), Is.True, materialPath);
            Assert.That(
                AssetDatabase.GetAssetPath(material.GetTexture("baseColorTexture")),
                Is.EqualTo(texturePath),
                materialPath);
            Assert.That(material.HasProperty("baseColorFactor"), Is.True, materialPath);
            AssertColor(material.GetColor("baseColorFactor"), Color.white, $"{materialPath} base color");
            Assert.That(material.HasProperty("metallicFactor"), Is.True, materialPath);
            Assert.That(
                material.GetFloat("metallicFactor"),
                Is.EqualTo(expectedMetallic).Within(FloatTolerance),
                materialPath);
            Assert.That(material.HasProperty("roughnessFactor"), Is.True, materialPath);
            Assert.That(
                material.GetFloat("roughnessFactor"),
                Is.EqualTo(expectedRoughness).Within(FloatTolerance),
                materialPath);
            Assert.That(material.HasProperty("emissiveFactor"), Is.True, materialPath);
            AssertColor(material.GetColor("emissiveFactor"), Color.black, $"{materialPath} emission");
            Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.False, materialPath);

            Assert.That(material.HasProperty("normalTexture"), Is.True, materialPath);
            Assert.That(material.HasProperty("metallicRoughnessTexture"), Is.True, materialPath);
            Assert.That(
                material.GetTexture("normalTexture") != null,
                Is.EqualTo(expectedSurfaceMaps),
                $"{materialPath} normal map binding");
            Assert.That(
                material.GetTexture("metallicRoughnessTexture") != null,
                Is.EqualTo(expectedSurfaceMaps),
                $"{materialPath} metallic-roughness map binding");
            Assert.That(
                material.IsKeywordEnabled("_NORMALMAP"),
                Is.EqualTo(expectedSurfaceMaps),
                $"{materialPath} normal map keyword");
            Assert.That(
                material.IsKeywordEnabled("_METALLICGLOSSMAP"),
                Is.EqualTo(expectedSurfaceMaps),
                $"{materialPath} metallic-roughness map keyword");
        }

        [TestCase(ChairPrefabPath, ChairMaterialPath)]
        [TestCase(FridgePrefabPath, FridgeMaterialPath)]
        [TestCase(ChalkboardPrefabPath, ChalkboardMaterialPath)]
        public void BrightenedPropPrefabRenderersUseOnlyTheirExternalMaterial(
            string prefabPath,
            string materialPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(material, Is.Not.Null, materialPath);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    prefabPath != ChairPrefabPath
                    || !IsUnderNamedAncestor(renderer.transform, CleanCushionName))
                .ToArray();
            Assert.That(renderers, Is.Not.Empty, prefabPath);
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Is.Not.Empty, $"{prefabPath}: {renderer.name}");
                Assert.That(
                    renderer.sharedMaterials,
                    Is.All.SameAs(material),
                    $"{prefabPath}: {renderer.name}");
            }
        }

        [Test]
        public void ChairPrefabHasOneCleanCheeseCushionWithValidDedicatedAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChairPrefabPath);
            var expectedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(CleanCushionMeshPath);
            var expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(CleanCushionMaterialPath);

            Assert.That(prefab, Is.Not.Null, ChairPrefabPath);
            Assert.That(expectedMesh, Is.Not.Null, CleanCushionMeshPath);
            Assert.That(expectedMaterial, Is.Not.Null, CleanCushionMaterialPath);

            var overlay = FindSingleCleanCushion(prefab.transform);
            var meshFilter = overlay.GetComponent<MeshFilter>();
            var meshRenderer = overlay.GetComponent<MeshRenderer>();
            Assert.That(meshFilter, Is.Not.Null, $"{CleanCushionName} MeshFilter");
            Assert.That(meshRenderer, Is.Not.Null, $"{CleanCushionName} MeshRenderer");
            Assert.That(meshFilter.sharedMesh, Is.SameAs(expectedMesh));
            Assert.That(meshRenderer.sharedMaterials, Is.EqualTo(new[] { expectedMaterial }));
            Assert.That(overlay.GetComponentsInChildren<Collider>(true), Is.Empty);

            var nonOverlayMaterials = prefab.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !IsUnderNamedAncestor(renderer.transform, CleanCushionName))
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .ToArray();
            Assert.That(
                nonOverlayMaterials.Contains(expectedMaterial),
                Is.False,
                "The clean cushion material must remain dedicated to the overlay.");

            AssertValidTriangleMesh(expectedMesh, CleanCushionMeshPath);
            AssertYellowNonEmissiveMaterial(expectedMaterial, CleanCushionMaterialPath);
        }

        [Test]
        public void CleanCheeseCushionBuilderIsIdempotentOnAnInMemoryChairClone()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChairPrefabPath);
            var cleanMesh = AssetDatabase.LoadAssetAtPath<Mesh>(CleanCushionMeshPath);
            var cleanMaterial = AssetDatabase.LoadAssetAtPath<Material>(CleanCushionMaterialPath);
            Assert.That(prefab, Is.Not.Null, ChairPrefabPath);
            Assert.That(cleanMesh, Is.Not.Null, CleanCushionMeshPath);
            Assert.That(cleanMaterial, Is.Not.Null, CleanCushionMaterialPath);
            var meshWasDirty = EditorUtility.IsDirty(cleanMesh);
            var materialWasDirty = EditorUtility.IsDirty(cleanMaterial);
            var clone = Object.Instantiate(prefab);
            clone.name = "Clean Cushion Idempotence Test Chair";
            try
            {
                var existingOverlays = clone.GetComponentsInChildren<Transform>(true)
                    .Where(candidate => candidate.name == CleanCushionName)
                    .ToArray();
                foreach (var existing in existingOverlays)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                var builderType = System.AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("CheeseTama.Editor.MilkroomPropPrefabBuilder"))
                    .Single(type => type != null);
                var ensure = builderType.GetMethod(
                    "EnsureCleanCheeseCushionOverlay",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(GameObject) },
                    null);
                Assert.That(ensure, Is.Not.Null, "Missing clean cushion builder helper.");
                Assert.That(
                    ensure.GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
                    Is.EqualTo(new[] { typeof(GameObject) }));

                ensure.Invoke(null, new object[] { clone });
                var first = FindSingleCleanCushion(clone.transform);
                ensure.Invoke(null, new object[] { clone });
                var second = FindSingleCleanCushion(clone.transform);

                Assert.That(second, Is.SameAs(first), "A second builder pass recreated the overlay.");
                Assert.That(
                    EditorUtility.IsDirty(cleanMesh),
                    Is.EqualTo(meshWasDirty),
                    "The in-memory builder changed the mesh asset dirty state.");
                Assert.That(
                    EditorUtility.IsDirty(cleanMaterial),
                    Is.EqualTo(materialWasDirty),
                    "The in-memory builder changed the material asset dirty state.");
            }
            finally
            {
                Object.DestroyImmediate(clone);
                if (!meshWasDirty)
                {
                    EditorUtility.ClearDirty(cleanMesh);
                }

                if (!materialWasDirty)
                {
                    EditorUtility.ClearDirty(cleanMaterial);
                }
            }
        }

        [TestCase("Assets/_Project/Scenes/Milkroom.unity")]
        [TestCase("Assets/_Project/Scenes/Debug.unity")]
        public void SavedScenesKeepBrightenedPropPrefabMaterialsWithoutRendererOverrides(string scenePath)
        {
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                AssertScenePropMaterial(
                    scene,
                    scenePath,
                    "CozyChair_Model",
                    ChairPrefabPath,
                    ChairMaterialPath,
                    CleanCushionName);
                AssertScenePropMaterial(scene, scenePath, "Fridge_Model", FridgePrefabPath, FridgeMaterialPath);
                AssertScenePropMaterial(
                    scene,
                    scenePath,
                    "Chalkboard_Model",
                    ChalkboardPrefabPath,
                    ChalkboardMaterialPath);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        [Test]
        public void ShelfPrefabPreservesGuidAndUsesReplacementAssetChainWithoutColliders()
        {
            Assert.That(AssetDatabase.AssetPathToGUID(ShelfPrefabPath), Is.EqualTo(ShelfPrefabGuid));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShelfPrefabPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ShelfModelPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ShelfTexturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ShelfMaterialPath);
            Assert.That(prefab, Is.Not.Null, ShelfPrefabPath);
            Assert.That(model, Is.Not.Null, ShelfModelPath);
            Assert.That(texture, Is.Not.Null, ShelfTexturePath);
            Assert.That(material, Is.Not.Null, ShelfMaterialPath);
            var importedModel = prefab.transform.Find("MilkShelf_ImportedModel");
            Assert.That(importedModel, Is.Not.Null);
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(importedModel.localEulerAngles.x, 270f)),
                Is.LessThan(0.01f),
                "The supplied shelf needs the audited -90 degree X-axis correction.");

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            Assert.That(
                renderers.Select(GetRendererMesh)
                    .Where(mesh => mesh != null)
                    .Select(AssetDatabase.GetAssetPath)
                    .Distinct()
                    .ToArray(),
                Is.EqualTo(new[] { ShelfModelPath }));
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Is.Not.Empty, renderer.name);
                Assert.That(renderer.sharedMaterials, Is.All.SameAs(material), renderer.name);
            }

            var baseTexture = material.HasProperty("_BaseMap")
                ? material.GetTexture("_BaseMap")
                : material.GetTexture("_MainTex");
            Assert.That(AssetDatabase.GetAssetPath(baseTexture), Is.EqualTo(ShelfTexturePath));
            if (material.HasProperty("_Metallic"))
            {
                Assert.That(material.GetFloat("_Metallic"), Is.EqualTo(0f).Within(0.0001f));
            }

            var smoothnessProperty = material.HasProperty("_Smoothness")
                ? "_Smoothness"
                : "_Glossiness";
            Assert.That(material.HasProperty(smoothnessProperty), Is.True);
            Assert.That(material.GetFloat(smoothnessProperty), Is.EqualTo(0.18f).Within(0.0001f));
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);

            var uniqueMeshes = renderers.Select(GetRendererMesh)
                .Where(mesh => mesh != null)
                .Distinct()
                .ToArray();
            Assert.That(uniqueMeshes, Has.Length.EqualTo(1));
            AssertOptimizedReplacementMesh(
                uniqueMeshes[0],
                ShelfMaxTriangleCount,
                ShelfExpectedLocalBounds,
                ShelfModelPath);
        }

        [Test]
        public void ShelfReplacementImporterStaysStaticAndRuntimeLightweight()
        {
            var importer = AssetImporter.GetAtPath(ShelfModelPath) as ModelImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.globalScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(importer.addCollider, Is.False);
            Assert.That(importer.importAnimation, Is.False);
            Assert.That(importer.importTangents, Is.EqualTo(ModelImporterTangents.None));
            Assert.That(importer.importBlendShapes, Is.False);
            Assert.That(importer.importCameras, Is.False);
            Assert.That(importer.importLights, Is.False);
            Assert.That(importer.generateSecondaryUV, Is.False);
            Assert.That(importer.materialImportMode, Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(importer.importNormals, Is.EqualTo(ModelImporterNormals.Import));
            Assert.That(importer.weldVertices, Is.True);
            Assert.That(importer.isReadable, Is.False);

            var textureImporter = AssetImporter.GetAtPath(ShelfTexturePath) as TextureImporter;
            Assert.That(textureImporter, Is.Not.Null);
            Assert.That(textureImporter.sRGBTexture, Is.True);
            Assert.That(textureImporter.mipmapEnabled, Is.True);
            Assert.That(textureImporter.maxTextureSize, Is.EqualTo(2048));
            Assert.That(textureImporter.textureCompression, Is.EqualTo(TextureImporterCompression.Compressed));
            Assert.That(textureImporter.isReadable, Is.False);
        }

        [Test]
        public void WindowPrefabPreservesGuidAndUsesReplacementAssetChainWithoutColliders()
        {
            Assert.That(AssetDatabase.AssetPathToGUID(WindowPrefabPath), Is.EqualTo(WindowPrefabGuid));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefabPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(WindowModelPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WindowTexturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(WindowMaterialPath);
            Assert.That(prefab, Is.Not.Null, WindowPrefabPath);
            Assert.That(model, Is.Not.Null, WindowModelPath);
            Assert.That(texture, Is.Not.Null, WindowTexturePath);
            Assert.That(material, Is.Not.Null, WindowMaterialPath);

            var importedModel = prefab.transform.Find("Window_ImportedModel");
            Assert.That(importedModel, Is.Not.Null);
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(importedModel.localEulerAngles.x, 270f)),
                Is.LessThan(0.01f),
                "The supplied window needs the audited -90 degree X-axis correction.");

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            Assert.That(
                renderers.Select(GetRendererMesh)
                    .Where(mesh => mesh != null)
                    .Select(AssetDatabase.GetAssetPath)
                    .Distinct()
                    .ToArray(),
                Is.EqualTo(new[] { WindowModelPath }));
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Is.Not.Empty, renderer.name);
                Assert.That(renderer.sharedMaterials, Is.All.SameAs(material), renderer.name);
            }

            var baseTexture = material.HasProperty("_BaseMap")
                ? material.GetTexture("_BaseMap")
                : material.GetTexture("_MainTex");
            Assert.That(AssetDatabase.GetAssetPath(baseTexture), Is.EqualTo(WindowTexturePath));
            if (material.HasProperty("_Metallic"))
            {
                Assert.That(material.GetFloat("_Metallic"), Is.EqualTo(0f).Within(0.0001f));
            }

            var smoothnessProperty = material.HasProperty("_Smoothness")
                ? "_Smoothness"
                : "_Glossiness";
            Assert.That(material.HasProperty(smoothnessProperty), Is.True);
            Assert.That(material.GetFloat(smoothnessProperty), Is.EqualTo(0.16f).Within(0.0001f));
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);

            var uniqueMeshes = renderers.Select(GetRendererMesh)
                .Where(mesh => mesh != null)
                .Distinct()
                .ToArray();
            Assert.That(uniqueMeshes, Has.Length.EqualTo(1));
            AssertOptimizedReplacementMesh(
                uniqueMeshes[0],
                WindowMaxTriangleCount,
                WindowExpectedLocalBounds,
                WindowModelPath);
        }

        [Test]
        public void WindowReplacementImporterStaysStaticAndRuntimeLightweight()
        {
            var importer = AssetImporter.GetAtPath(WindowModelPath) as ModelImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.globalScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(importer.addCollider, Is.False);
            Assert.That(importer.importAnimation, Is.False);
            Assert.That(importer.importTangents, Is.EqualTo(ModelImporterTangents.None));
            Assert.That(importer.importBlendShapes, Is.False);
            Assert.That(importer.importCameras, Is.False);
            Assert.That(importer.importLights, Is.False);
            Assert.That(importer.generateSecondaryUV, Is.False);
            Assert.That(importer.materialImportMode, Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(importer.importNormals, Is.EqualTo(ModelImporterNormals.Import));
            Assert.That(importer.weldVertices, Is.True);
            Assert.That(importer.isReadable, Is.False);

            var textureImporter = AssetImporter.GetAtPath(WindowTexturePath) as TextureImporter;
            Assert.That(textureImporter, Is.Not.Null);
            Assert.That(textureImporter.sRGBTexture, Is.True);
            Assert.That(textureImporter.mipmapEnabled, Is.True);
            Assert.That(textureImporter.maxTextureSize, Is.EqualTo(2048));
            Assert.That(textureImporter.textureCompression, Is.EqualTo(TextureImporterCompression.Compressed));
            Assert.That(textureImporter.isReadable, Is.False);
        }

        [TestCase("Assets/_Project/Scenes/Milkroom.unity")]
        [TestCase("Assets/_Project/Scenes/Debug.unity")]
        public void SavedSceneShelfKeepsReplacementBoundsAndPlacement(string scenePath)
        {
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var matches = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Where(candidate => candidate.name == "MilkShelf_Model")
                    .ToArray();
                Assert.That(matches, Has.Length.EqualTo(1), scenePath);
                var shelf = matches[0];
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(shelf.gameObject),
                    Is.EqualTo(ShelfPrefabPath));
                Assert.That(shelf.position.x, Is.EqualTo(2.65f).Within(FloatTolerance));
                Assert.That(shelf.position.z, Is.EqualTo(2.295f).Within(FloatTolerance));
                Assert.That(
                    Mathf.Abs(Mathf.DeltaAngle(shelf.eulerAngles.y, 180f)),
                    Is.LessThan(0.01f));
                Assert.That(shelf.localScale.x, Is.EqualTo(shelf.localScale.y).Within(FloatTolerance));
                Assert.That(shelf.localScale.x, Is.EqualTo(shelf.localScale.z).Within(FloatTolerance));

                var bounds = CalculateRendererBounds(shelf);
                Assert.That(bounds.size.x, Is.EqualTo(1.392014f).Within(0.015f));
                Assert.That(bounds.size.y, Is.EqualTo(1.3f).Within(0.001f));
                Assert.That(bounds.size.z, Is.EqualTo(0.45080f).Within(0.015f));
                Assert.That(bounds.center.y, Is.EqualTo(-0.15f).Within(0.001f));
                Assert.That(bounds.min.y, Is.EqualTo(-0.8f).Within(0.001f));
                Assert.That(bounds.max.y, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(bounds.max.z, Is.EqualTo(2.5204f).Within(0.015f));
                Assert.That(shelf.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        [TestCase("Assets/_Project/Scenes/Milkroom.unity")]
        [TestCase("Assets/_Project/Scenes/Debug.unity")]
        public void SavedSceneWindowKeepsReplacementBoundsAndBackWallPlacement(string scenePath)
        {
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var matches = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Where(candidate => candidate.name == "Window_Model")
                    .ToArray();
                Assert.That(matches, Has.Length.EqualTo(1), scenePath);
                var window = matches[0];
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(window.gameObject),
                    Is.EqualTo(WindowPrefabPath));
                Assert.That(window.position.x, Is.EqualTo(0.45f).Within(FloatTolerance));
                Assert.That(window.position.z, Is.EqualTo(2.366f).Within(FloatTolerance));
                Assert.That(
                    Mathf.Abs(Mathf.DeltaAngle(window.eulerAngles.y, 180f)),
                    Is.LessThan(0.01f));
                Assert.That(window.localScale.x, Is.EqualTo(1.926f).Within(0.015f));
                Assert.That(window.localScale.x, Is.EqualTo(window.localScale.y).Within(FloatTolerance));
                Assert.That(window.localScale.x, Is.EqualTo(window.localScale.z).Within(FloatTolerance));

                var bounds = CalculateRendererBounds(window);
                Assert.That(bounds.size.x, Is.EqualTo(1.926f).Within(0.02f));
                Assert.That(bounds.size.y, Is.EqualTo(1.72f).Within(0.001f));
                Assert.That(bounds.size.z, Is.EqualTo(0.308f).Within(0.02f));
                Assert.That(bounds.min.y, Is.EqualTo(-1.01f).Within(0.015f));
                Assert.That(bounds.max.y, Is.EqualTo(0.71f).Within(0.015f));
                Assert.That(bounds.max.z, Is.EqualTo(2.52f).Within(0.015f));
                Assert.That(window.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        [TestCase("Assets/_Project/Scenes/Milkroom.unity")]
        [TestCase("Assets/_Project/Scenes/Debug.unity")]
        public void SavedSceneDresserUsesNewFacingYaw(string scenePath)
        {
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var matches = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Where(candidate => candidate.name == "DresserTable_Model")
                    .ToArray();
                Assert.That(matches, Has.Length.EqualTo(1), scenePath);
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(matches[0].gameObject),
                    Is.EqualTo(DresserPrefabPath));
                Assert.That(
                    Mathf.Abs(Mathf.DeltaAngle(matches[0].eulerAngles.y, 200f)),
                    Is.LessThan(0.01f),
                    scenePath);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        [TestCase(MilkroomThemeController.MorningThemeId, 0.36f, 0.18f, 0.07f)]
        [TestCase(MilkroomThemeController.EveningThemeId, 0.36f, 0.18f, 0.07f)]
        [TestCase(MilkroomThemeController.RainyThemeId, 0.36f, 0.18f, 0.07f)]
        [TestCase(MilkroomThemeController.NightThemeId, 0.28f, 0.16f, 0.10f)]
        public void LightingControllerUsesAdjustedNaturalPreset(
            string themeId,
            float expectedKey,
            float expectedFill,
            float expectedRim)
        {
            var previousAmbientMode = RenderSettings.ambientMode;
            var previousAmbientLight = RenderSettings.ambientLight;
            var root = new GameObject("Adjusted Lighting Regression Root");
            root.SetActive(false);
            try
            {
                var key = CreateDirectionalLight(root.transform, "Key");
                var fill = CreateDirectionalLight(root.transform, "Fill");
                var rim = CreateDirectionalLight(root.transform, "Rim");
                var cameraObject = new GameObject("Lighting Test Camera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform, false);
                var controller = root.AddComponent<MilkroomLightingController>();
                SetPrivateField(controller, "keyLight", key);
                SetPrivateField(controller, "fillLight", fill);
                SetPrivateField(controller, "rimLight", rim);
                SetPrivateField(controller, "targetCamera", cameraObject.GetComponent<Camera>());

                controller.ApplyTheme(themeId);

                var palette = MilkroomThemePalette.For(themeId);
                Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
                AssertColor(
                    RenderSettings.ambientLight,
                    Color.Lerp(
                        palette.Ambient,
                        Color.white,
                        themeId == MilkroomThemeController.NightThemeId ? 0.12f : 0.32f));
                Assert.That(key.intensity, Is.EqualTo(expectedKey).Within(0.0001f));
                Assert.That(fill.intensity, Is.EqualTo(expectedFill).Within(0.0001f));
                Assert.That(rim.intensity, Is.EqualTo(expectedRim).Within(0.0001f));
                AssertColor(key.color, Color.Lerp(palette.Glow, Color.white, 0.52f));
                AssertColor(fill.color, Color.Lerp(palette.WindowSky, Color.white, 0.42f));
                Assert.That(key.shadows, Is.EqualTo(LightShadows.Soft));
                Assert.That(key.shadowStrength, Is.EqualTo(0.18f).Within(0.0001f));
                Assert.That(key.shadowBias, Is.EqualTo(0.05f).Within(0.0001f));
                Assert.That(key.shadowNormalBias, Is.EqualTo(0.35f).Within(0.0001f));
                Assert.That(fill.shadows, Is.EqualTo(LightShadows.None));
                Assert.That(rim.shadows, Is.EqualTo(LightShadows.None));
                AssertEulerAngles(key.transform, new Vector3(52f, -28f, 0f));
                AssertEulerAngles(fill.transform, new Vector3(25f, 32f, 0f));
                AssertEulerAngles(rim.transform, new Vector3(32f, 208f, 0f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
            }
        }

        private static Transform CreateAnchor(Transform parent, string name)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            return anchor;
        }

        private static void CreateLegacyPlaceholder(Transform parent)
        {
            var placeholder = new GameObject(LegacyDecorationName);
            placeholder.transform.SetParent(parent, false);
        }

        private static Mesh GetRendererMesh(Renderer renderer)
        {
            return renderer is SkinnedMeshRenderer skinned
                ? skinned.sharedMesh
                : renderer.GetComponent<MeshFilter>()?.sharedMesh;
        }

        private static Bounds CalculateRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, root.name);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index += 1)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static Transform FindSingleCleanCushion(Transform root)
        {
            var overlays = root.GetComponentsInChildren<Transform>(true)
                .Where(candidate => candidate.name == CleanCushionName)
                .ToArray();
            Assert.That(overlays, Has.Length.EqualTo(1), root.name);
            return overlays[0];
        }

        private static bool IsUnderNamedAncestor(Transform transform, string ancestorName)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                if (current.name == ancestorName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertValidTriangleMesh(Mesh mesh, string label)
        {
            Assert.That(mesh.vertexCount, Is.GreaterThan(0), $"{label} vertices");
            var normals = mesh.normals;
            Assert.That(normals, Has.Length.EqualTo(mesh.vertexCount), $"{label} normals");
            Assert.That(
                normals.All(normal => normal.sqrMagnitude > 0.25f),
                Is.True,
                $"{label} contains a zero or invalid normal");
            Assert.That(mesh.subMeshCount, Is.GreaterThan(0), $"{label} submeshes");

            ulong totalTriangleCount = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh += 1)
            {
                Assert.That(mesh.GetTopology(subMesh), Is.EqualTo(MeshTopology.Triangles), label);
                var indexCount = mesh.GetIndexCount(subMesh);
                Assert.That(indexCount, Is.GreaterThanOrEqualTo(3u), $"{label} submesh {subMesh}");
                Assert.That(indexCount % 3u, Is.EqualTo(0u), $"{label} submesh {subMesh} indices");
                totalTriangleCount += indexCount / 3;
            }

            Assert.That(totalTriangleCount, Is.GreaterThan(0), $"{label} triangles");
        }

        private static void AssertOptimizedReplacementMesh(
            Mesh mesh,
            ulong maxTriangleCount,
            Bounds expectedLocalBounds,
            string label)
        {
            Assert.That(mesh.vertexCount, Is.InRange(1, OptimizedMeshMaxVertexCount), $"{label} vertices");
            Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt16), $"{label} index format");
            Assert.That(mesh.subMeshCount, Is.EqualTo(1), $"{label} submesh count");
            Assert.That(mesh.GetTopology(0), Is.EqualTo(MeshTopology.Triangles), $"{label} topology");

            var indexCount = mesh.GetIndexCount(0);
            Assert.That(indexCount % 3u, Is.EqualTo(0u), $"{label} triangle indices");
            var triangleCount = indexCount / 3UL;
            Assert.That(triangleCount, Is.InRange(1UL, maxTriangleCount), $"{label} triangles");

            var normals = mesh.normals;
            Assert.That(normals, Has.Length.EqualTo(mesh.vertexCount), $"{label} normals");
            Assert.That(
                normals.All(normal => IsFinite(normal) && normal.sqrMagnitude > 0.25f),
                Is.True,
                $"{label} contains a zero or non-finite normal");

            var uv = mesh.uv;
            Assert.That(uv, Has.Length.EqualTo(mesh.vertexCount), $"{label} UV0");
            Assert.That(uv.All(IsFinite), Is.True, $"{label} contains a non-finite UV0 coordinate");
            var minU = uv.Min(coordinate => coordinate.x);
            var maxU = uv.Max(coordinate => coordinate.x);
            var minV = uv.Min(coordinate => coordinate.y);
            var maxV = uv.Max(coordinate => coordinate.y);
            Assert.That(minU, Is.GreaterThanOrEqualTo(-0.01f), $"{label} UV0 min U");
            Assert.That(maxU, Is.LessThanOrEqualTo(1.01f), $"{label} UV0 max U");
            Assert.That(minV, Is.GreaterThanOrEqualTo(-0.01f), $"{label} UV0 min V");
            Assert.That(maxV, Is.LessThanOrEqualTo(1.01f), $"{label} UV0 max V");
            Assert.That(maxU - minU, Is.GreaterThanOrEqualTo(0.95f), $"{label} UV0 U span");
            Assert.That(maxV - minV, Is.GreaterThanOrEqualTo(0.95f), $"{label} UV0 V span");

            AssertVectorWithin(mesh.bounds.center, expectedLocalBounds.center, LocalBoundsTolerance, $"{label} bounds center");
            AssertVectorWithin(mesh.bounds.size, expectedLocalBounds.size, LocalBoundsTolerance, $"{label} bounds size");
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void AssertVectorWithin(
            Vector3 actual,
            Vector3 expected,
            float tolerance,
            string label)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance), $"{label} X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance), $"{label} Y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(tolerance), $"{label} Z");
        }

        private static void AssertYellowNonEmissiveMaterial(Material material, string label)
        {
            var colorProperty = material.HasProperty("baseColorFactor")
                ? "baseColorFactor"
                : material.HasProperty("_BaseColor")
                    ? "_BaseColor"
                    : "_Color";
            Assert.That(material.HasProperty(colorProperty), Is.True, $"{label} color property");
            var color = material.GetColor(colorProperty);
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);
            Assert.That(hue, Is.InRange(0.09f, 0.19f), $"{label} yellow hue");
            Assert.That(saturation, Is.GreaterThanOrEqualTo(0.35f), $"{label} saturation");
            Assert.That(value, Is.GreaterThanOrEqualTo(0.70f), $"{label} value");

            if (material.HasProperty("emissiveFactor"))
            {
                AssertColor(material.GetColor("emissiveFactor"), Color.black, $"{label} emissiveFactor");
            }

            if (material.HasProperty("_EmissionColor"))
            {
                AssertColor(material.GetColor("_EmissionColor"), Color.black, $"{label} emission");
            }

            Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.False, label);
        }

        private static void AssertScenePropMaterial(
            UnityEngine.SceneManagement.Scene scene,
            string scenePath,
            string instanceName,
            string prefabPath,
            string materialPath,
            string excludedRendererAncestorName = null)
        {
            var matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(candidate => candidate.name == instanceName)
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1), $"{scenePath}: {instanceName}");

            var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(matches[0].gameObject);
            Assert.That(instanceRoot, Is.Not.Null, $"{scenePath}: {instanceName} is not a prefab instance");
            Assert.That(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot),
                Is.EqualTo(prefabPath),
                $"{scenePath}: {instanceName}");

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(material, Is.Not.Null, materialPath);
            var renderers = matches[0].GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    excludedRendererAncestorName == null
                    || !IsUnderNamedAncestor(renderer.transform, excludedRendererAncestorName))
                .ToArray();
            Assert.That(renderers, Is.Not.Empty, $"{scenePath}: {instanceName}");
            var propertyBlock = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Is.Not.Empty, $"{scenePath}: {renderer.name}");
                Assert.That(
                    renderer.sharedMaterials,
                    Is.All.SameAs(material),
                    $"{scenePath}: {renderer.name}");
                renderer.GetPropertyBlock(propertyBlock);
                Assert.That(propertyBlock.isEmpty, Is.True, $"{scenePath}: {renderer.name} property block");
                propertyBlock.Clear();
            }

            var modifications = PrefabUtility.GetPropertyModifications(instanceRoot);
            if (modifications != null)
            {
                var materialOverrides = modifications
                    .Where(modification =>
                        modification.target is Renderer
                        && modification.propertyPath != null
                        && modification.propertyPath.StartsWith(
                            "m_Materials.Array.data",
                            System.StringComparison.Ordinal))
                    .ToArray();
                Assert.That(
                    materialOverrides,
                    Is.Empty,
                    $"{scenePath}: {instanceName} has a serialized renderer material override");
            }
        }

        private static Light CreateDirectionalLight(Transform parent, string name)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            return light;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void AssertColor(Color actual, Color expected, string label = null)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f), $"{label}.r");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f), $"{label}.g");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f), $"{label}.b");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f), $"{label}.a");
        }

        private static void AssertEulerAngles(Transform transform, Vector3 expected)
        {
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.x, expected.x)), Is.LessThan(0.0001f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, expected.y)), Is.LessThan(0.0001f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, expected.z)), Is.LessThan(0.0001f));
        }
    }
}
