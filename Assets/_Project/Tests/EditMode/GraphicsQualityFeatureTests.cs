using System;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class GraphicsQualityFeatureTests
    {
        [Test]
        public void PresetsResolveToExistingQualityNamesAndConservativeCullOrder()
        {
            var names = new[] { "Very Low", "Low", "Medium", "High", "Very High", "Ultra" };

            Assert.That(
                GraphicsQualityCatalog.ResolveQualityLevelIndex(GraphicsQualityPreset.Low, names),
                Is.EqualTo(1));
            Assert.That(
                GraphicsQualityCatalog.ResolveQualityLevelIndex(GraphicsQualityPreset.Balanced, names),
                Is.EqualTo(3));
            Assert.That(
                GraphicsQualityCatalog.ResolveQualityLevelIndex(GraphicsQualityPreset.High, names),
                Is.EqualTo(5));
            Assert.That(
                GraphicsQualityCatalog.Get(GraphicsQualityPreset.Low).PropCullHeight,
                Is.GreaterThan(GraphicsQualityCatalog.Get(GraphicsQualityPreset.Balanced).PropCullHeight));
            Assert.That(
                GraphicsQualityCatalog.Get(GraphicsQualityPreset.Balanced).PropCullHeight,
                Is.GreaterThan(GraphicsQualityCatalog.Get(GraphicsQualityPreset.High).PropCullHeight));
            Assert.That(
                GraphicsQualityCatalog.Get(GraphicsQualityPreset.Low).PropLowDetailHeight,
                Is.GreaterThan(GraphicsQualityCatalog.Get(GraphicsQualityPreset.Balanced).PropLowDetailHeight));
            Assert.That(
                GraphicsQualityCatalog.Get(GraphicsQualityPreset.Balanced).PropLowDetailHeight,
                Is.GreaterThan(GraphicsQualityCatalog.Get(GraphicsQualityPreset.High).PropLowDetailHeight));
            foreach (GraphicsQualityPreset preset in Enum.GetValues(typeof(GraphicsQualityPreset)))
            {
                Assert.That(
                    GraphicsQualityCatalog.Get(preset).PropLowDetailHeight,
                    Is.GreaterThan(GraphicsQualityCatalog.Get(preset).PropCullHeight));
            }
        }

        [Test]
        public void QualitySettingNormalizesAndSurvivesJsonRoundTrip()
        {
            var settings = new GameSettingsSaveData
            {
                graphicsQualityPreset = (int)GraphicsQualityPreset.Low
            };

            var restored = JsonUtility.FromJson<GameSettingsSaveData>(JsonUtility.ToJson(settings));
            restored.EnsureRuntimeDefaults();
            Assert.That(restored.graphicsQualityPreset, Is.EqualTo((int)GraphicsQualityPreset.Low));

            restored.graphicsQualityPreset = 99;
            restored.EnsureRuntimeDefaults();
            Assert.That(restored.graphicsQualityPreset, Is.EqualTo((int)GraphicsQualityPreset.High));
        }

        [Test]
        public void PropDetailControllerCreatesIdempotentTwoStageLodsAndAppliesShadowPolicy()
        {
            var root = new GameObject("Milkroom Background");
            try
            {
                CreateProp(root.transform, "Fridge_Model");
                CreateProp(root.transform, "Chalkboard_Model");
                var controller = root.AddComponent<MilkroomPropDetailController>();

                controller.Configure(root.transform);
                controller.Configure(root.transform);
                controller.ApplyPreset(GraphicsQualityPreset.Low);

                Assert.That(controller.ManagedGroupCount, Is.EqualTo(2));
                Assert.That(controller.ManagedProxyCount, Is.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<LODGroup>(true), Has.Length.EqualTo(2));
                foreach (var group in root.GetComponentsInChildren<LODGroup>(true))
                {
                    var lods = group.GetLODs();
                    Assert.That(lods, Has.Length.EqualTo(2));
                    Assert.That(
                        lods[0].screenRelativeTransitionHeight,
                        Is.EqualTo(GraphicsQualityCatalog.Get(GraphicsQualityPreset.Low).PropLowDetailHeight)
                            .Within(0.0001f));
                    Assert.That(lods[0].renderers, Has.Length.EqualTo(1));
                    Assert.That(
                        lods[1].screenRelativeTransitionHeight,
                        Is.EqualTo(GraphicsQualityCatalog.Get(GraphicsQualityPreset.Low).PropCullHeight)
                            .Within(0.0001f));
                    Assert.That(lods[1].renderers, Has.Length.EqualTo(1));
                    Assert.That(lods[1].renderers[0].GetComponent<MeshFilter>().sharedMesh.triangles, Has.Length.EqualTo(36));
                    Assert.That(
                        lods[0].renderers[0].shadowCastingMode,
                        Is.EqualTo(ShadowCastingMode.Off));
                    Assert.That(lods[1].renderers[0].shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                }

                controller.ApplyPreset(GraphicsQualityPreset.High);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.transform.name == "__CheeseTama Detail Proxy")
                    {
                        Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                        Assert.That(renderer.receiveShadows, Is.False);
                    }
                    else
                    {
                        Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
                        Assert.That(renderer.receiveShadows, Is.True);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AtmosphereBuilderCreatesOneNonBlockingThemeSafeOverlay()
        {
            var canvas = new GameObject("Milkroom Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var malformed = new GameObject("Milkroom Atmosphere Overlay");
                malformed.transform.SetParent(canvas.transform, false);
                var method = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureMilkroomAtmosphere",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);

                method.Invoke(null, new object[] { canvas.transform, null });
                method.Invoke(null, new object[] { canvas.transform, null });

                var overlay = canvas.transform.Find("Milkroom Atmosphere Overlay");
                Assert.That(overlay, Is.Not.Null);
                Assert.That(malformed == null, Is.True);
                Assert.That(overlay, Is.TypeOf<RectTransform>());
                Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.False);
                Assert.That(overlay.GetComponent<MilkroomAtmosphereLayerController>(), Is.Not.Null);
                Assert.That(CountChildren(canvas.transform, "Milkroom Atmosphere Overlay"), Is.EqualTo(1));
                Assert.That(GameObject.Find("Milkroom Atmosphere Light"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private static void CreateProp(Transform parent, string name)
        {
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = name;
            prop.transform.SetParent(parent, false);
        }

        private static int CountChildren(Transform root, string name)
        {
            var count = 0;
            foreach (Transform child in root)
            {
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    count += 1;
                }
            }

            return count;
        }
    }
}
