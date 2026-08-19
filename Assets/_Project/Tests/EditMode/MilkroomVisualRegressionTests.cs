using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Utilities;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class MilkroomVisualRegressionTests
    {
        private const string AtmosphereOverlayName = "Milkroom Atmosphere Overlay";
        private const string AtmosphereLightName = "Milkroom Atmosphere Light";
        private const string EquippedDecorationVisualName = "Equipped Decoration Visual";
        private const string StarterSceneBuilderScriptPath =
            "Assets/_Project/Scripts/Core/StarterSceneBuilder.cs";
        private const string EggMaterialPath =
            "Assets/Characters/CheeseTama/GrowthStages/Materials/CheeseTama_Egg.mat";
        private const string RugMaterialPath =
            "Assets/Environments/Milkroom/Props/Rug_Assets/Replacement/RugReplacement.mat";
        private static readonly Color LegacyMorningWallColor = new Color(0.78f, 0.58f, 0.38f, 1f);
        private static readonly Color LegacyMorningFloorColor = new Color(0.50f, 0.29f, 0.15f, 1f);
        private const float DayAmbientWhiteBlend = 0.32f;
        private const float NightAmbientWhiteBlend = 0.12f;
        private const float KeyWhiteBlend = 0.52f;
        private const float FillWhiteBlend = 0.42f;
        private const float KeyShadowStrength = 0.18f;
        private const float KeyShadowBias = 0.05f;
        private const float KeyShadowNormalBias = 0.35f;
        private const float FloatTolerance = 0.0001f;

        [TestCase(MilkroomThemeController.MorningThemeId, 0.36f, 0.18f, 0.07f)]
        [TestCase(MilkroomThemeController.EveningThemeId, 0.36f, 0.18f, 0.07f)]
        [TestCase(MilkroomThemeController.RainyThemeId, 0.36f, 0.18f, 0.07f)]
        [TestCase(MilkroomThemeController.NightThemeId, 0.28f, 0.16f, 0.10f)]
        public void ApplyThemeUsesDimmedPaletteAmbientAndExactLightIntensities(
            string themeId,
            float expectedKeyIntensity,
            float expectedFillIntensity,
            float expectedRimIntensity)
        {
            var renderSettings = RenderSettingsSnapshot.Capture();
            var root = new GameObject("Milkroom Lighting Regression Test Root");
            root.SetActive(false);
            try
            {
                var keyLight = CreateLight(root.transform, "Test Key Light");
                var fillLight = CreateLight(root.transform, "Test Fill Light");
                var rimLight = CreateLight(root.transform, "Test Rim Light");
                var cameraObject = new GameObject("Test Milkroom Camera", typeof(Camera));
                cameraObject.SetActive(false);
                cameraObject.transform.SetParent(root.transform, false);

                var controller = root.AddComponent<MilkroomLightingController>();
                SetPrivateField(controller, "keyLight", keyLight);
                SetPrivateField(controller, "fillLight", fillLight);
                SetPrivateField(controller, "rimLight", rimLight);
                SetPrivateField(controller, "targetCamera", cameraObject.GetComponent<Camera>());

                controller.ApplyTheme(themeId);

                var palette = MilkroomThemePalette.For(themeId);
                var expectedAmbient = Color.Lerp(
                    palette.Ambient,
                    Color.white,
                    themeId == MilkroomThemeController.NightThemeId
                        ? NightAmbientWhiteBlend
                        : DayAmbientWhiteBlend);
                Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
                AssertColor(RenderSettings.ambientLight, expectedAmbient, "ambient light");
                Assert.That(keyLight.intensity, Is.EqualTo(expectedKeyIntensity).Within(FloatTolerance));
                Assert.That(fillLight.intensity, Is.EqualTo(expectedFillIntensity).Within(FloatTolerance));
                Assert.That(rimLight.intensity, Is.EqualTo(expectedRimIntensity).Within(FloatTolerance));
                AssertColor(
                    keyLight.color,
                    Color.Lerp(palette.Glow, Color.white, KeyWhiteBlend),
                    "key light color");
                AssertColor(
                    fillLight.color,
                    Color.Lerp(palette.WindowSky, Color.white, FillWhiteBlend),
                    "fill light color");
                Assert.That(keyLight.shadows, Is.EqualTo(LightShadows.Soft));
                Assert.That(
                    keyLight.shadowStrength,
                    Is.EqualTo(KeyShadowStrength).Within(FloatTolerance));
                Assert.That(
                    keyLight.shadowBias,
                    Is.EqualTo(KeyShadowBias).Within(FloatTolerance));
                Assert.That(
                    keyLight.shadowNormalBias,
                    Is.EqualTo(KeyShadowNormalBias).Within(FloatTolerance));
                Assert.That(fillLight.shadows, Is.EqualTo(LightShadows.None));
                Assert.That(rimLight.shadows, Is.EqualTo(LightShadows.None));
                AssertEulerAngles(keyLight.transform, new Vector3(52f, -28f, 0f), "key light rotation");
                AssertEulerAngles(fillLight.transform, new Vector3(25f, 32f, 0f), "fill light rotation");
                AssertEulerAngles(rimLight.transform, new Vector3(32f, 208f, 0f), "rim light rotation");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                renderSettings.Restore();
            }
        }

        [TestCase(ToonMaterialProfile.EnvironmentMatte, 0.35f)]
        [TestCase(ToonMaterialProfile.EnvironmentWood, 0.24f)]
        public void ConfigureMaterialWritesBuiltInStandardGlossiness(
            ToonMaterialProfile profile,
            float expectedGlossiness)
        {
            var shader = Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null, "The Built-in Standard shader is required for this regression test.");

            var material = new Material(shader);
            try
            {
                var configure = typeof(ToonMaterialUtility).GetMethod(
                    "ConfigureMaterial",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(configure, Is.Not.Null, "Missing ToonMaterialUtility.ConfigureMaterial.");

                configure.Invoke(null, new object[] { material, profile });

                Assert.That(material.HasProperty("_Glossiness"), Is.True);
                Assert.That(
                    material.GetFloat("_Glossiness"),
                    Is.EqualTo(expectedGlossiness).Within(FloatTolerance));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [TestCase("Assets/_Project/Scenes/Milkroom.unity")]
        [TestCase("Assets/_Project/Scenes/Debug.unity")]
        public void SavedSceneUsesNaturalThreePointLightingContract(string scenePath)
        {
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var lights = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Light>(true))
                    .ToArray();
                var key = lights.Single(light => light.name == "Milkroom Key Light");
                var fill = lights.Single(light => light.name == "Milkroom Fill Light");
                var rim = lights.Single(light => light.name == "Milkroom Rim Light");
                var palette = MilkroomThemePalette.For(MilkroomThemeController.MorningThemeId);
                var expectedAmbient = Color.Lerp(palette.Ambient, Color.white, DayAmbientWhiteBlend);

                AssertColor(ReadSerializedAmbientColor(scenePath), expectedAmbient, "saved ambient color");
                Assert.That(
                    File.ReadAllText(scenePath),
                    Does.Contain("m_AmbientMode: 3"),
                    "The saved scene must keep flat ambient lighting.");

                Assert.That(key.enabled, Is.True);
                Assert.That(fill.enabled, Is.True);
                Assert.That(rim.enabled, Is.True);
                Assert.That(key.type, Is.EqualTo(LightType.Directional));
                Assert.That(fill.type, Is.EqualTo(LightType.Directional));
                Assert.That(rim.type, Is.EqualTo(LightType.Directional));
                Assert.That(key.intensity, Is.EqualTo(0.36f).Within(FloatTolerance));
                Assert.That(fill.intensity, Is.EqualTo(0.18f).Within(FloatTolerance));
                Assert.That(rim.intensity, Is.EqualTo(0.07f).Within(FloatTolerance));
                AssertColor(key.color, Color.Lerp(palette.Glow, Color.white, KeyWhiteBlend), "saved key color");
                AssertColor(fill.color, Color.Lerp(palette.WindowSky, Color.white, FillWhiteBlend), "saved fill color");
                Assert.That(key.shadows, Is.EqualTo(LightShadows.Soft));
                Assert.That(key.shadowStrength, Is.EqualTo(KeyShadowStrength).Within(FloatTolerance));
                Assert.That(key.shadowBias, Is.EqualTo(KeyShadowBias).Within(FloatTolerance));
                Assert.That(key.shadowNormalBias, Is.EqualTo(KeyShadowNormalBias).Within(FloatTolerance));
                Assert.That(fill.shadows, Is.EqualTo(LightShadows.None));
                Assert.That(rim.shadows, Is.EqualTo(LightShadows.None));
                AssertEulerAngles(key.transform, new Vector3(52f, -28f, 0f), "saved key rotation");
                AssertEulerAngles(fill.transform, new Vector3(25f, 32f, 0f), "saved fill rotation");
                AssertEulerAngles(rim.transform, new Vector3(32f, 208f, 0f), "saved rim rotation");

                var controller = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MilkroomLightingController>(true))
                    .Single();
                Assert.That(GetPrivateField<Light>(controller, "keyLight"), Is.SameAs(key));
                Assert.That(GetPrivateField<Light>(controller, "fillLight"), Is.SameAs(fill));
                Assert.That(GetPrivateField<Light>(controller, "rimLight"), Is.SameAs(rim));
                Assert.That(GetPrivateField<Camera>(controller, "targetCamera"), Is.Not.Null);

                var themeController = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MilkroomThemeController>(true))
                    .Single();
                var roomShell = GetPrivateField<Transform>(themeController, "backgroundRoot");
                Assert.That(roomShell, Is.Not.Null);
                Assert.That(roomShell.name, Is.EqualTo("RoomShell"));
                var wallRenderers = roomShell.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.name.Contains("Wall")
                        && !renderer.name.Contains("Wall Wash"))
                    .ToArray();
                Assert.That(wallRenderers, Is.Not.Empty);
                foreach (var wallRenderer in wallRenderers)
                {
                    var wallMaterial = wallRenderer.sharedMaterial;
                    Assert.That(wallMaterial, Is.Not.Null, $"{wallRenderer.name} material");
                    var colorProperty = wallMaterial.HasProperty("_BaseColor")
                        ? "_BaseColor"
                        : "_Color";
                    Assert.That(
                        wallMaterial.HasProperty(colorProperty),
                        Is.True,
                        $"{wallRenderer.name} color property");
                    AssertColor(
                        wallMaterial.GetColor(colorProperty),
                        palette.Wall,
                        $"{wallRenderer.name} saved warm wall color");
                }
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
        public void StarterSceneBuilderReusesTheLightingControllerContract()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(StarterSceneBuilderScriptPath);
            Assert.That(script, Is.Not.Null, StarterSceneBuilderScriptPath);

            var source = script.text;
            Assert.That(source, Does.Contain("MilkroomLightingController.ResolveAmbientColor"));
            Assert.That(source, Does.Contain("MilkroomLightingController.DayKeyIntensity"));
            Assert.That(source, Does.Contain("MilkroomLightingController.DayFillIntensity"));
            Assert.That(source, Does.Contain("MilkroomLightingController.DayRimIntensity"));
            Assert.That(source, Does.Contain("MilkroomLightingController.KeyWhiteBlend"));
            Assert.That(source, Does.Contain("MilkroomLightingController.FillWhiteBlend"));
            Assert.That(source, Does.Contain("MilkroomLightingController.KeyShadowStrength"));
            Assert.That(source, Does.Contain("MilkroomLightingController.KeyShadowBias"));
            Assert.That(source, Does.Contain("MilkroomLightingController.KeyShadowNormalBias"));
            Assert.That(source, Does.Contain("MilkroomLightingController.KeyRotationEuler"));
            Assert.That(source, Does.Contain("MilkroomLightingController.FillRotationEuler"));
            Assert.That(source, Does.Contain("MilkroomLightingController.RimRotationEuler"));
        }

        [Test]
        public void MorningRoomShellKeepsOriginalWallAndRaisesOnlyFloorValue()
        {
            var roomShell = new GameObject("RoomShell");
            try
            {
                var wallObject = new GameObject("BackWall", typeof(MeshRenderer));
                wallObject.transform.SetParent(roomShell.transform, false);
                var floorObject = new GameObject("Floor", typeof(MeshRenderer));
                floorObject.transform.SetParent(roomShell.transform, false);
                var outsideObject = new GameObject("Outside Wall", typeof(MeshRenderer));

                try
                {
                    var palette = MilkroomThemePalette.For(MilkroomThemeController.MorningThemeId);
                    AssertColor(palette.Wall, LegacyMorningWallColor, "unchanged morning wall palette");
                    AssertColor(palette.Floor, LegacyMorningFloorColor, "unchanged morning floor palette");

                    var wallFirst = AdjustRoomShellColor(wallObject.GetComponent<Renderer>(), palette.Wall);
                    var wallSecond = AdjustRoomShellColor(wallObject.GetComponent<Renderer>(), palette.Wall);
                    var floorFirst = AdjustRoomShellColor(floorObject.GetComponent<Renderer>(), palette.Floor);
                    var floorSecond = AdjustRoomShellColor(floorObject.GetComponent<Renderer>(), palette.Floor);

                    AssertColor(wallFirst, palette.Wall, "morning wall original palette");
                    AssertHsvValueOnly(floorFirst, palette.Floor, 0.55f, "morning floor");
                    AssertColor(wallSecond, wallFirst, "repeated morning wall resolution");
                    AssertColor(floorSecond, floorFirst, "repeated morning floor resolution");
                    AssertColor(
                        AdjustRoomShellColor(outsideObject.GetComponent<Renderer>(), palette.Wall),
                        palette.Wall,
                        "outside RoomShell wall");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(outsideObject);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(roomShell);
            }
        }

        [Test]
        public void CharacterAndRugMaterialsKeepBalancedBrightnessInputs()
        {
            var eggMaterial = AssetDatabase.LoadAssetAtPath<Material>(EggMaterialPath);
            var rugMaterial = AssetDatabase.LoadAssetAtPath<Material>(RugMaterialPath);

            Assert.That(eggMaterial, Is.Not.Null, EggMaterialPath);
            Assert.That(rugMaterial, Is.Not.Null, RugMaterialPath);
            Assert.That(eggMaterial.shader.name, Is.EqualTo("CheeseTama/Growth Palette"));
            Assert.That(eggMaterial.GetFloat("_PaletteValueScale"), Is.EqualTo(1.08f).Within(FloatTolerance));
            Assert.That(eggMaterial.GetFloat("_PaletteValueOffset"), Is.EqualTo(0.10f).Within(FloatTolerance));
            Assert.That(eggMaterial.GetFloat("_PaletteEmission"), Is.EqualTo(0.10f).Within(FloatTolerance));
            AssertColor(eggMaterial.GetColor("_Color"), Color.white, "egg reaction tint");

            var rugColorProperty = rugMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            Assert.That(rugMaterial.HasProperty(rugColorProperty), Is.True);
            AssertColor(
                rugMaterial.GetColor(rugColorProperty),
                new Color(0.74f, 0.72f, 0.68f, 1f),
                "rug tint");
        }

        [Test]
        public void EnsureMilkroomAtmosphereReusesSubtleOverlayAndRemovesLegacyLight()
        {
            Assert.That(
                GameObject.Find(AtmosphereLightName),
                Is.Null,
                "This regression test requires no pre-existing active atmosphere light.");

            var root = new GameObject("Milkroom Atmosphere Regression Test Root");
            var canvasObject = new GameObject(
                "Test Milkroom Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            canvasObject.transform.SetParent(root.transform, false);
            var overlayObject = new GameObject(
                AtmosphereOverlayName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            overlayObject.transform.SetParent(canvasObject.transform, false);
            var lightObject = new GameObject(AtmosphereLightName, typeof(Light));
            lightObject.transform.SetParent(root.transform, false);
            lightObject.GetComponent<Light>().intensity = 0f;

            try
            {
                var method = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureMilkroomAtmosphere",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null, "Missing StarterSceneBuilder.EnsureMilkroomAtmosphere.");

                method.Invoke(null, new object[] { canvasObject.transform, null });
                method.Invoke(null, new object[] { canvasObject.transform, null });

                Assert.That(overlayObject == null, Is.False, "The atmosphere overlay was removed.");
                Assert.That(lightObject == null, Is.True, "The existing atmosphere light was not removed.");
                var retainedOverlay = canvasObject.transform.Find(AtmosphereOverlayName);
                Assert.That(retainedOverlay, Is.SameAs(overlayObject.transform));
                Assert.That(retainedOverlay.GetSiblingIndex(), Is.Zero);
                Assert.That(retainedOverlay.GetComponent<Image>()?.raycastTarget, Is.False);
                Assert.That(
                    retainedOverlay.GetComponents<MilkroomAtmosphereLayerController>(),
                    Has.Length.EqualTo(1));
                Assert.That(GameObject.Find(AtmosphereLightName), Is.Null,
                    "EnsureMilkroomAtmosphere recreated an atmosphere light.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                var remainingLight = GameObject.Find(AtmosphereLightName);
                if (remainingLight != null)
                {
                    UnityEngine.Object.DestroyImmediate(remainingLight);
                }
            }
        }

        [TestCase(MilkroomThemeController.MorningThemeId)]
        [TestCase(MilkroomThemeController.EveningThemeId)]
        [TestCase(MilkroomThemeController.NightThemeId)]
        [TestCase(MilkroomThemeController.RainyThemeId)]
        public void DefaultDecorationRefreshRestoresThemeWallAndRemovesPlaceholders(string themeId)
        {
            Assert.That(
                GameManager.Instance,
                Is.Null,
                "This regression test requires the default snapshot path without a live GameManager.");

            var root = new GameObject("Milkroom Background");
            root.SetActive(false);
            Material runtimeWhiteMaterial = null;
            try
            {
                var roomShell = new GameObject("RoomShell").transform;
                roomShell.SetParent(root.transform, false);
                var wallRenderer = CreatePrimitiveRenderer(roomShell, "BackWall");
                var floorRenderer = CreatePrimitiveRenderer(root.transform, "Test Floor");
                var themeController = root.AddComponent<MilkroomThemeController>();
                themeController.Configure(roomShell, null, null, null, null);
                themeController.ApplyTheme(themeId);
                var palette = MilkroomThemePalette.For(themeId);
                var expectedWall = AdjustRoomShellColor(wallRenderer, palette.Wall);
                runtimeWhiteMaterial = new Material(Shader.Find("Standard"));
                runtimeWhiteMaterial.color = Color.white;
                wallRenderer.sharedMaterial = runtimeWhiteMaterial;
                SeedColorOverride(wallRenderer, expectedWall);
                SeedColorOverride(floorRenderer, Color.cyan);

                var windowAnchor = CreateAnchor(root.transform, "Decoration Window Anchor");
                var shelfAnchor = CreateAnchor(root.transform, "Decoration Shelf Anchor");
                CreatePlaceholderCube(windowAnchor);
                CreatePlaceholderCube(shelfAnchor);

                var presenter = root.AddComponent<DecorationRoomPresenter>();
                presenter.Configure(
                    wallRenderer,
                    floorRenderer,
                    null,
                    windowAnchor,
                    shelfAnchor,
                    null);

                var propertyBlock = new MaterialPropertyBlock();
                wallRenderer.GetPropertyBlock(propertyBlock);
                var wallOverrideIsEmpty = propertyBlock.isEmpty;
                propertyBlock.Clear();
                floorRenderer.GetPropertyBlock(propertyBlock);
                var floorOverrideIsEmpty = propertyBlock.isEmpty;
                var wallMaterial = wallRenderer.sharedMaterial;
                Assert.That(wallMaterial, Is.Not.Null);
                var wallColorProperty = wallMaterial.HasProperty("_BaseColor")
                    ? "_BaseColor"
                    : "_Color";

                Assert.That(windowAnchor.Find(EquippedDecorationVisualName), Is.Null,
                    "Default Refresh left or recreated the window placeholder Cube.");
                Assert.That(shelfAnchor.Find(EquippedDecorationVisualName), Is.Null,
                    "Default Refresh left or recreated the shelf placeholder Cube.");
                var windowDecoration = windowAnchor.Find("Equipped Window Decoration");
                var shelfDecoration = shelfAnchor.Find("Equipped Shelf Decoration");
                Assert.That(windowDecoration, Is.Null,
                    "The default curtain should stay hidden beside the chalkboard.");
                Assert.That(shelfDecoration, Is.Null,
                    "The default cheese clock should stay hidden beside the shelf.");
                Assert.That(wallOverrideIsEmpty, Is.True,
                    "Default wall presentation retained a MaterialPropertyBlock override.");
                AssertColor(
                    wallMaterial.GetColor(wallColorProperty),
                    expectedWall,
                    "Default wall presentation lost the active theme color.");
                Assert.That(floorOverrideIsEmpty, Is.True,
                    "Default floor presentation retained a MaterialPropertyBlock override.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if (runtimeWhiteMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(runtimeWhiteMaterial);
                }
            }
        }

        private static Light CreateLight(Transform parent, string name)
        {
            var lightObject = new GameObject(name);
            lightObject.SetActive(false);
            lightObject.transform.SetParent(parent, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            return light;
        }

        private static bool ContainsCubeMesh(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (var index = 0; index < filters.Length; index += 1)
            {
                var meshName = filters[index]?.sharedMesh?.name;
                if (!string.IsNullOrWhiteSpace(meshName)
                    && meshName.IndexOf("Cube", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static Renderer CreatePrimitiveRenderer(Transform parent, string name)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            return primitive.GetComponent<Renderer>();
        }

        private static Transform CreateAnchor(Transform parent, string name)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            return anchor;
        }

        private static void CreatePlaceholderCube(Transform anchor)
        {
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = EquippedDecorationVisualName;
            placeholder.transform.SetParent(anchor, false);
        }

        private static void SeedColorOverride(Renderer renderer, Color color)
        {
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            field.SetValue(target, value);
        }

        private static void AssertColor(Color actual, Color expected, string label)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(FloatTolerance), $"{label}.r");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(FloatTolerance), $"{label}.g");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(FloatTolerance), $"{label}.b");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(FloatTolerance), $"{label}.a");
        }

        private static void AssertHsvValueOnly(
            Color actual,
            Color baseline,
            float expectedValue,
            string label)
        {
            Color.RGBToHSV(actual, out var actualHue, out var actualSaturation, out var actualValue);
            Color.RGBToHSV(baseline, out var baselineHue, out var baselineSaturation, out _);

            Assert.That(actualHue, Is.EqualTo(baselineHue).Within(FloatTolerance), $"{label} hue");
            Assert.That(
                actualSaturation,
                Is.EqualTo(baselineSaturation).Within(FloatTolerance),
                $"{label} saturation");
            Assert.That(actualValue, Is.EqualTo(expectedValue).Within(FloatTolerance), $"{label} value");
            Assert.That(actual.a, Is.EqualTo(baseline.a).Within(FloatTolerance), $"{label} alpha");
        }

        private static Color AdjustRoomShellColor(Renderer renderer, Color color)
        {
            var method = typeof(MilkroomThemeController).GetMethod(
                "AdjustRoomShellColor",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing MilkroomThemeController.AdjustRoomShellColor.");
            return (Color)method.Invoke(null, new object[] { renderer, color });
        }

        private static void AssertEulerAngles(Transform transform, Vector3 expected, string label)
        {
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.x, expected.x)),
                Is.LessThan(FloatTolerance),
                $"{label}.x");
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, expected.y)),
                Is.LessThan(FloatTolerance),
                $"{label}.y");
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, expected.z)),
                Is.LessThan(FloatTolerance),
                $"{label}.z");
        }

        private static Color ReadSerializedAmbientColor(string scenePath)
        {
            var sceneText = File.ReadAllText(scenePath);
            var match = Regex.Match(
                sceneText,
                @"m_AmbientSkyColor:\s*\{r:\s*(?<r>[-+0-9.eE]+),\s*g:\s*(?<g>[-+0-9.eE]+),\s*b:\s*(?<b>[-+0-9.eE]+),\s*a:\s*(?<a>[-+0-9.eE]+)\}",
                RegexOptions.CultureInvariant);
            Assert.That(match.Success, Is.True, $"Missing serialized ambient color in {scenePath}.");
            return new Color(
                ParseInvariant(match.Groups["r"].Value),
                ParseInvariant(match.Groups["g"].Value),
                ParseInvariant(match.Groups["b"].Value),
                ParseInvariant(match.Groups["a"].Value));
        }

        private static float ParseInvariant(string value)
        {
            return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private readonly struct RenderSettingsSnapshot
        {
            private readonly AmbientMode ambientMode;
            private readonly Color ambientLight;
            private readonly Color ambientSkyColor;
            private readonly Color ambientEquatorColor;
            private readonly Color ambientGroundColor;
            private readonly float ambientIntensity;

            private RenderSettingsSnapshot(
                AmbientMode ambientMode,
                Color ambientLight,
                Color ambientSkyColor,
                Color ambientEquatorColor,
                Color ambientGroundColor,
                float ambientIntensity)
            {
                this.ambientMode = ambientMode;
                this.ambientLight = ambientLight;
                this.ambientSkyColor = ambientSkyColor;
                this.ambientEquatorColor = ambientEquatorColor;
                this.ambientGroundColor = ambientGroundColor;
                this.ambientIntensity = ambientIntensity;
            }

            public static RenderSettingsSnapshot Capture()
            {
                return new RenderSettingsSnapshot(
                    RenderSettings.ambientMode,
                    RenderSettings.ambientLight,
                    RenderSettings.ambientSkyColor,
                    RenderSettings.ambientEquatorColor,
                    RenderSettings.ambientGroundColor,
                    RenderSettings.ambientIntensity);
            }

            public void Restore()
            {
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambientLight;
                RenderSettings.ambientSkyColor = ambientSkyColor;
                RenderSettings.ambientEquatorColor = ambientEquatorColor;
                RenderSettings.ambientGroundColor = ambientGroundColor;
                RenderSettings.ambientIntensity = ambientIntensity;
            }
        }
    }
}
