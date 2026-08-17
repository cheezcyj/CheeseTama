using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Data;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class GrowthStageAppearanceRegressionTests
    {
        private const string GrowthRoot = "Assets/Characters/CheeseTama/GrowthStages";
        private const string MaterialRoot = GrowthRoot + "/Materials";
        private const string ThumbnailRoot = GrowthRoot + "/Thumbnails";
        private const string SourceRoot = GrowthRoot + "/SourceModels";
        private const string VisualSetPath = "Assets/_Project/Resources/CheeseTamaGrowthVisualSet.asset";
        private const string CleanCushionMaterialPath =
            "Assets/Environments/Milkroom/Props/CozyChair_Assets/CleanCheeseCushion.mat";
        private const string GrowthPaletteShaderName = "CheeseTama/Growth Palette";
        private const float FloatTolerance = 0.0001f;

        private static readonly StageAssetContract[] StageAssets =
        {
            new StageAssetContract(
                CheeseTamaGrowthStage.Egg,
                "CheeseTama_Egg",
                SourceRoot + "/Stage01/CheeseTama_Stage01.fbx",
                SourceRoot + "/Stage01/CheeseTama_Stage01_BaseColor.jpg"),
            new StageAssetContract(
                CheeseTamaGrowthStage.Hatchling,
                "CheeseTama_Hatchling",
                SourceRoot + "/Stage02/CheeseTama_Stage02_Optimized.obj",
                SourceRoot + "/Stage02/CheeseTama_Stage02_BaseColor.jpg"),
            new StageAssetContract(
                CheeseTamaGrowthStage.Soft,
                "CheeseTama_Soft",
                SourceRoot + "/Stage03/CheeseTama_Stage03_Optimized.obj",
                SourceRoot + "/Stage03/CheeseTama_Stage03_BaseColor.jpg"),
            new StageAssetContract(
                CheeseTamaGrowthStage.Grown,
                "CheeseTama_Grown",
                SourceRoot + "/Stage04/CheeseTama_Stage04_Optimized.obj",
                SourceRoot + "/Stage04/CheeseTama_Stage04_BaseColor.jpg"),
            new StageAssetContract(
                CheeseTamaGrowthStage.Mature,
                "CheeseTama_Mature",
                SourceRoot + "/Stage05/CheeseTama_Stage05_Optimized.obj",
                SourceRoot + "/Stage05/CheeseTama_Stage05_BaseColor.jpg"),
            new StageAssetContract(
                CheeseTamaGrowthStage.Final,
                "CheeseTama_Final",
                SourceRoot + "/Stage06/CheeseTama_Stage06_Optimized.obj",
                SourceRoot + "/Stage06/CheeseTama_Stage06_BaseColor.jpg")
        };

        private static IEnumerable<TestCaseData> AllStageCases()
        {
            for (var index = 0; index < StageAssets.Length; index += 1)
            {
                var stage = StageAssets[index].Stage;
                yield return new TestCaseData(stage).SetName(
                    $"GeneratedGrowthAssetReferencesAreComplete({stage})");
            }
        }

        private static IEnumerable<TestCaseData> FaceCleanupDisabledStageCases()
        {
            for (var index = 0; index < StageAssets.Length; index += 1)
            {
                var stage = StageAssets[index].Stage;
                yield return new TestCaseData(stage).SetName(
                    $"AllGrowthStagesDisableModelFaceCleanup({stage})");
            }
        }

        private static IEnumerable<TestCaseData> NormalEvolutionAccentRemovalCases()
        {
            foreach (var profile in EvolutionSystem.NormalEvolutions)
            {
                yield return new TestCaseData(profile.Id).SetName(
                    $"StarterBuilderRemovesNormalEvolutionRuntimeAccents({profile.Id})");
            }
        }

        [Test]
        public void StagesThreeThroughSixMatchCleanCushionPaletteAndShareValueTransform()
        {
            var cushionMaterial = AssetDatabase.LoadAssetAtPath<Material>(CleanCushionMaterialPath);
            Assert.That(cushionMaterial, Is.Not.Null, CleanCushionMaterialPath);

            var cushionColorProperty = ResolvePrimaryColorProperty(cushionMaterial);
            var cushionColor = cushionMaterial.GetColor(cushionColorProperty);
            Color.RGBToHSV(
                cushionColor,
                out var expectedHue,
                out var expectedSaturation,
                out _);

            var hasCommonTransform = false;
            var commonValueScale = 0f;
            var commonValueOffset = 0f;
            for (var index = 2; index < StageAssets.Length; index += 1)
            {
                var contract = StageAssets[index];
                var material = LoadStageMaterial(contract);
                Assert.That(material.shader, Is.Not.Null, $"{contract.Stage} shader");
                Assert.That(material.shader.name, Is.EqualTo(GrowthPaletteShaderName), contract.Stage.ToString());
                AssertFloatProperty(material, "_PaletteHue", expectedHue, contract.Stage.ToString());
                AssertFloatProperty(
                    material,
                    "_PaletteSaturation",
                    expectedSaturation,
                    contract.Stage.ToString());

                Assert.That(material.HasProperty("_PaletteValueScale"), Is.True, contract.Stage.ToString());
                Assert.That(material.HasProperty("_PaletteValueOffset"), Is.True, contract.Stage.ToString());
                var valueScale = material.GetFloat("_PaletteValueScale");
                var valueOffset = material.GetFloat("_PaletteValueOffset");
                if (!hasCommonTransform)
                {
                    commonValueScale = valueScale;
                    commonValueOffset = valueOffset;
                    hasCommonTransform = true;
                }
                else
                {
                    Assert.That(
                        valueScale,
                        Is.EqualTo(commonValueScale).Within(FloatTolerance),
                        $"{contract.Stage} value scale");
                    Assert.That(
                        valueOffset,
                        Is.EqualTo(commonValueOffset).Within(FloatTolerance),
                        $"{contract.Stage} value offset");
                }

                AssertColor(material.GetColor("_Color"), Color.white, $"{contract.Stage} reaction tint");
            }

            Assert.That(hasCommonTransform, Is.True);
            Assert.That(IsFinite(commonValueScale), Is.True, "common value scale");
            Assert.That(IsFinite(commonValueOffset), Is.True, "common value offset");
        }

        [TestCaseSource(nameof(FaceCleanupDisabledStageCases))]
        public void AllGrowthStagesDisableModelFaceCleanup(CheeseTamaGrowthStage stage)
        {
            var material = LoadStageMaterial(FindContract(stage));
            Assert.That(material.HasProperty("_FaceCleanupSurface1"), Is.True, stage.ToString());

            var surface1 = material.GetVector("_FaceCleanupSurface1");
            Assert.That(
                surface1.w,
                Is.EqualTo(0f).Within(FloatTolerance),
                $"{stage} cleanup strength");
        }

        [TestCaseSource(nameof(NormalEvolutionAccentRemovalCases))]
        public void StarterBuilderRemovesNormalEvolutionRuntimeAccents(string evolutionId)
        {
            var host = new GameObject($"Normal Evolution Cleanup Test - {evolutionId}");
            host.SetActive(false);
            try
            {
                var model = new GameObject("GeneratedModel");
                model.transform.SetParent(host.transform, false);
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.name = "Body";
                body.transform.SetParent(model.transform, false);
                var bodyCollider = body.GetComponent<Collider>();
                if (bodyCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(bodyCollider);
                }

                var visualController = host.AddComponent<CheeseTamaVisualController>();
                SetModelInstance(visualController, model.transform);
                var owningPresenter = host.AddComponent<NormalEvolutionVisualPresenter>();
                host.AddComponent<NormalEvolutionVisualPresenter>();
                host.AddComponent<NormalEvolutionVisualBridge>();

                var tama = new CheeseTamaModel
                {
                    isHatched = true,
                    evolutionId = evolutionId,
                    form = evolutionId
                };
                owningPresenter.Bind(tama, model.transform);
                var profile = NormalEvolutionVisualCatalog.Find(evolutionId);
                Assert.That(profile, Is.Not.Null, $"Missing visual profile for {evolutionId}.");
                Assert.That(owningPresenter.GeneratedRoot, Is.Not.Null, "Fixture must reproduce runtime accents.");
                Assert.That(
                    owningPresenter.GeneratedAccentCount,
                    Is.EqualTo(profile.Accents.Count),
                    "Fixture must reproduce every authored runtime accent.");

                var duplicateStaleRoot = new GameObject(
                    NormalEvolutionVisualPresenter.GeneratedRootName);
                duplicateStaleRoot.transform.SetParent(model.transform, false);
                new GameObject("Stale Accent").transform.SetParent(
                    duplicateStaleRoot.transform,
                    false);
                Assert.That(CountNormalEvolutionAccentRoots(host.transform), Is.EqualTo(2));

                var cleanup = GetNormalEvolutionAccentCleanupMethod();
                cleanup.Invoke(null, new object[] { visualController });
                AssertNormalEvolutionIntegrationRemoved(host, visualController, model, body);

                cleanup.Invoke(null, new object[] { visualController });
                AssertNormalEvolutionIntegrationRemoved(host, visualController, model, body);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [TestCaseSource(nameof(AllStageCases))]
        public void GeneratedGrowthAssetReferencesAreComplete(CheeseTamaGrowthStage stage)
        {
            var contract = FindContract(stage);
            var visualSet = AssetDatabase.LoadAssetAtPath<CheeseTamaGrowthVisualSet>(VisualSetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(contract.PrefabPath);
            var thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(contract.ThumbnailPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(contract.MaterialPath);
            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(contract.SourceModelPath);
            var sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(contract.SourceTexturePath);

            Assert.That(visualSet, Is.Not.Null, VisualSetPath);
            Assert.That(prefab, Is.Not.Null, contract.PrefabPath);
            Assert.That(thumbnail, Is.Not.Null, contract.ThumbnailPath);
            Assert.That(material, Is.Not.Null, contract.MaterialPath);
            Assert.That(sourceModel, Is.Not.Null, contract.SourceModelPath);
            Assert.That(sourceTexture, Is.Not.Null, contract.SourceTexturePath);
            Assert.That(visualSet.GetPrefab(stage), Is.SameAs(prefab), $"{stage} visual-set prefab");
            Assert.That(visualSet.GetThumbnail(stage), Is.SameAs(thumbnail), $"{stage} visual-set thumbnail");
            Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(contract.PrefabPath));
            Assert.That(AssetDatabase.GetAssetPath(thumbnail), Is.EqualTo(contract.ThumbnailPath));
            Assert.That(thumbnail.texture, Is.Not.Null, $"{stage} thumbnail texture");
            Assert.That(material.shader, Is.Not.Null, $"{stage} material shader");
            Assert.That(material.GetTexture("_MainTex"), Is.SameAs(sourceTexture), $"{stage} base texture");
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab),
                Is.Zero,
                $"{stage} missing scripts");

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, $"{stage} renderers");
            foreach (var renderer in renderers)
            {
                Assert.That(renderer, Is.Not.Null, $"{stage} renderer");
                Assert.That(renderer.sharedMaterials, Is.Not.Empty, $"{stage}/{renderer.name} materials");
                foreach (var assignedMaterial in renderer.sharedMaterials)
                {
                    Assert.That(
                        assignedMaterial,
                        Is.SameAs(material),
                        $"{stage}/{renderer.name} assigned material");
                }
            }

            var prefabDependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(contract.PrefabPath, true),
                StringComparer.OrdinalIgnoreCase);
            Assert.That(prefabDependencies, Does.Contain(contract.SourceModelPath));
            Assert.That(prefabDependencies, Does.Contain(contract.MaterialPath));
            Assert.That(prefabDependencies, Does.Contain(contract.SourceTexturePath));

            var visualSetDependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(VisualSetPath, true),
                StringComparer.OrdinalIgnoreCase);
            Assert.That(visualSetDependencies, Does.Contain(contract.PrefabPath));
            Assert.That(visualSetDependencies, Does.Contain(contract.ThumbnailPath));

            AssertAssetGuid(contract.PrefabPath);
            AssertAssetGuid(contract.ThumbnailPath);
            AssertAssetGuid(contract.MaterialPath);
            AssertAssetGuid(contract.SourceModelPath);
            AssertAssetGuid(contract.SourceTexturePath);
        }

        [Test]
        public void HatchedNormalEvolutionIdsKeepNeutralWhiteBaseTint()
        {
            var getBaseTint = GetBaseTintMethod();
            var normalEvolutionIds = EvolutionSystem.NormalEvolutions
                .Select(profile => profile.Id)
                .ToArray();

            Assert.That(normalEvolutionIds, Is.Not.Empty);
            Assert.That(
                normalEvolutionIds.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(normalEvolutionIds.Length),
                "Normal evolution ids must be unique.");
            foreach (var evolutionId in normalEvolutionIds)
            {
                var tama = new CheeseTamaModel
                {
                    isHatched = true,
                    evolutionId = evolutionId,
                    eggType = "egg_strawberry"
                };

                var tint = InvokeBaseTint(getBaseTint, tama);
                AssertColor(tint, Color.white, evolutionId);
            }
        }

        [TestCase("egg_butter", 1f, 0.8f, 0.3f, 1f)]
        [TestCase("egg_strawberry", 1f, 0.62f, 0.72f, 1f)]
        [TestCase("egg_mint", 0.58f, 0.9f, 0.76f, 1f)]
        [TestCase("egg_coffee", 0.58f, 0.38f, 0.24f, 1f)]
        [TestCase("egg_cream", 1f, 0.94f, 0.72f, 1f)]
        [TestCase("cream_egg", 1f, 0.94f, 0.72f, 1f)]
        [TestCase(StarEggEmmentalEvolutionSystem.StarEggTypeId, 0.82f, 0.77f, 1f, 1f)]
        [TestCase("unknown_egg", 1f, 1f, 1f, 1f)]
        public void UnhatchedEggTypesRetainAuthoredBaseTint(
            string eggType,
            float red,
            float green,
            float blue,
            float alpha)
        {
            var tama = new CheeseTamaModel
            {
                isHatched = false,
                evolutionId = EvolutionSystem.CreamEvolutionId,
                eggType = eggType
            };

            var tint = InvokeBaseTint(GetBaseTintMethod(), tama);
            AssertColor(tint, new Color(red, green, blue, alpha), eggType);
        }

        [Test]
        public void GrowthBuilderVersionIncludesLatestAppearanceMigration()
        {
            var builderType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("CheeseTama.Editor.CheeseTamaGrowthPrefabBuilder"))
                .FirstOrDefault(type => type != null);
            Assert.That(builderType, Is.Not.Null, "Growth prefab builder type was not loaded.");

            var versionField = builderType.GetField(
                "BuilderVersion",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(versionField, Is.Not.Null, "Missing CheeseTamaGrowthPrefabBuilder.BuilderVersion.");
            Assert.That(versionField.IsLiteral, Is.True, "BuilderVersion must remain a const migration marker.");
            Assert.That(
                (int)versionField.GetRawConstantValue(),
                Is.GreaterThanOrEqualTo(27),
                "Disabling model-face cleanup requires builder version 27 or newer.");
        }

        private static StageAssetContract FindContract(CheeseTamaGrowthStage stage)
        {
            var contract = StageAssets.FirstOrDefault(candidate => candidate.Stage == stage);
            Assert.That(contract, Is.Not.Null, $"Missing asset contract for {stage}.");
            return contract;
        }

        private static Material LoadStageMaterial(StageAssetContract contract)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(contract.MaterialPath);
            Assert.That(material, Is.Not.Null, contract.MaterialPath);
            return material;
        }

        private static MethodInfo GetNormalEvolutionAccentCleanupMethod()
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                "RemoveNormalEvolutionVisualAccents",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(CheeseTamaVisualController) },
                null);
            Assert.That(
                method,
                Is.Not.Null,
                "StarterSceneBuilder must own the normal-evolution accent cleanup integration.");
            return method;
        }

        private static void SetModelInstance(
            CheeseTamaVisualController visualController,
            Transform model)
        {
            var field = typeof(CheeseTamaVisualController).GetField(
                "modelInstance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing CheeseTamaVisualController.modelInstance.");
            field.SetValue(visualController, model);
            Assert.That(visualController.ModelInstance, Is.SameAs(model));
        }

        private static void AssertNormalEvolutionIntegrationRemoved(
            GameObject host,
            CheeseTamaVisualController visualController,
            GameObject model,
            GameObject body)
        {
            Assert.That(
                host.GetComponentsInChildren<NormalEvolutionVisualPresenter>(true),
                Is.Empty,
                "Runtime evolution presenters must not remain on the character hierarchy.");
            Assert.That(
                host.GetComponentsInChildren<NormalEvolutionVisualBridge>(true),
                Is.Empty,
                "Runtime evolution bridges must not remain on the character hierarchy.");
            Assert.That(
                CountNormalEvolutionAccentRoots(host.transform),
                Is.Zero,
                "Generated and duplicate stale evolution accent roots must be removed.");

            Assert.That(host.GetComponent<CheeseTamaVisualController>(), Is.SameAs(visualController));
            Assert.That(visualController.ModelInstance, Is.SameAs(model.transform));
            Assert.That(model.transform.parent, Is.SameAs(host.transform));
            Assert.That(body.transform.parent, Is.SameAs(model.transform));
            Assert.That(body.GetComponent<Renderer>(), Is.Not.Null, "The authored stage model must be preserved.");
        }

        private static int CountNormalEvolutionAccentRoots(Transform root)
        {
            var count = 0;
            for (var index = 0; index < root.childCount; index += 1)
            {
                var child = root.GetChild(index);
                if (child.name.StartsWith(
                        NormalEvolutionVisualPresenter.GeneratedRootName,
                        StringComparison.Ordinal))
                {
                    count += 1;
                }

                count += CountNormalEvolutionAccentRoots(child);
            }

            return count;
        }

        private static MethodInfo GetBaseTintMethod()
        {
            var method = typeof(CheeseTamaVisualController).GetMethod(
                "GetBaseTint",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing CheeseTamaVisualController.GetBaseTint.");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(Color)));
            Assert.That(
                method.GetParameters().Select(parameter => parameter.ParameterType).ToArray(),
                Is.EqualTo(new[] { typeof(CheeseTamaModel) }));
            return method;
        }

        private static Color InvokeBaseTint(MethodInfo method, CheeseTamaModel tama)
        {
            return (Color)method.Invoke(null, new object[] { tama });
        }

        private static string ResolvePrimaryColorProperty(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                return "_BaseColor";
            }

            Assert.That(material.HasProperty("_Color"), Is.True, $"{material.name} color property");
            return "_Color";
        }

        private static void AssertFloatProperty(
            Material material,
            string propertyName,
            float expected,
            string label)
        {
            Assert.That(material.HasProperty(propertyName), Is.True, $"{label} {propertyName}");
            Assert.That(
                material.GetFloat(propertyName),
                Is.EqualTo(expected).Within(FloatTolerance),
                $"{label} {propertyName}");
        }

        private static void AssertAssetGuid(string assetPath)
        {
            Assert.That(
                AssetDatabase.AssetPathToGUID(assetPath),
                Is.Not.Empty,
                $"Missing GUID for {assetPath}.");
        }

        private static void AssertColor(Color actual, Color expected, string label)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(FloatTolerance), $"{label}.r");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(FloatTolerance), $"{label}.g");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(FloatTolerance), $"{label}.b");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(FloatTolerance), $"{label}.a");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class StageAssetContract
        {
            public StageAssetContract(
                CheeseTamaGrowthStage stage,
                string assetName,
                string sourceModelPath,
                string sourceTexturePath)
            {
                Stage = stage;
                AssetName = assetName;
                SourceModelPath = sourceModelPath;
                SourceTexturePath = sourceTexturePath;
            }

            public CheeseTamaGrowthStage Stage { get; }
            public string AssetName { get; }
            public string SourceModelPath { get; }
            public string SourceTexturePath { get; }
            public string PrefabPath => $"{GrowthRoot}/{AssetName}.prefab";
            public string MaterialPath => $"{MaterialRoot}/{AssetName}.mat";
            public string ThumbnailPath => $"{ThumbnailRoot}/{AssetName}_Thumb.png";
        }
    }
}
