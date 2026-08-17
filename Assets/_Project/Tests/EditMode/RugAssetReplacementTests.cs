using System.Linq;
using System.Reflection;
using CheeseTama.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CheeseTama.Tests
{
    public sealed class RugAssetReplacementTests
    {
        private const string RugPrefabPath = "Assets/Environments/Milkroom/Props/Rug.prefab";
        private const string RugPrefabGuid = "d82254ab91268c749a492f21c0a91ab4";
        private const string RugModelPath =
            "Assets/Environments/Milkroom/Props/Rug_Assets/Replacement/RugReplacement.fbx";
        private const string RugTexturePath =
            "Assets/Environments/Milkroom/Props/Rug_Assets/Replacement/Rug.png";
        private const string RugMaterialPath =
            "Assets/Environments/Milkroom/Props/Rug_Assets/Replacement/RugReplacement.mat";
        private const float FloorTop = -2.13f;
        private const float RugTop = -2.03f;
        private const float RugHeight = 0.1f;
        private const float RugFootprint = 2.45f;
        private const float PositionTolerance = 0.0005f;
        private const int MaxVertexCount = 65535;
        private const ulong MaxTriangleCount = 35000UL;
        private const float LocalBoundsTolerance = 0.001f;
        private static readonly Bounds ExpectedLocalBounds = new Bounds(
            Vector3.zero,
            new Vector3(1.999342f, 2f, 0.12864786f));

        [Test]
        public void RugPrefabPreservesGuidAndUsesReplacementAssets()
        {
            Assert.That(
                AssetDatabase.AssetPathToGUID(RugPrefabPath),
                Is.EqualTo(RugPrefabGuid),
                "Replacing the rug must preserve the existing prefab GUID used by both Milkroom scenes.");

            var rugPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RugPrefabPath);
            var replacementModel = AssetDatabase.LoadAssetAtPath<GameObject>(RugModelPath);
            var replacementTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RugTexturePath);
            var replacementMaterial = AssetDatabase.LoadAssetAtPath<Material>(RugMaterialPath);

            Assert.That(rugPrefab, Is.Not.Null, $"Missing rug prefab at {RugPrefabPath}.");
            Assert.That(replacementModel, Is.Not.Null, $"Missing replacement model at {RugModelPath}.");
            Assert.That(replacementTexture, Is.Not.Null, $"Missing replacement texture at {RugTexturePath}.");
            Assert.That(replacementMaterial, Is.Not.Null, $"Missing dedicated rug material at {RugMaterialPath}.");

            var renderers = rugPrefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, "The rug prefab must contain visible mesh renderers.");

            var meshes = renderers
                .Select(GetRendererMesh)
                .Where(mesh => mesh != null)
                .ToArray();
            Assert.That(meshes, Is.Not.Empty, "The rug renderers do not reference a mesh.");
            var uniqueMeshes = meshes.Distinct().ToArray();
            Assert.That(
                uniqueMeshes.Select(AssetDatabase.GetAssetPath).ToArray(),
                Is.EqualTo(new[] { RugModelPath }),
                "Every rendered rug mesh must come from the replacement FBX.");
            Assert.That(uniqueMeshes, Has.Length.EqualTo(1));
            AssertOptimizedReplacementMesh(uniqueMeshes[0]);

            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Is.Not.Empty, $"{renderer.name} has no material assigned.");
                Assert.That(
                    renderer.sharedMaterials,
                    Is.All.SameAs(replacementMaterial),
                    $"{renderer.name} must use the dedicated replacement rug material in every slot.");
            }

            var baseTexture = replacementMaterial.HasProperty("_BaseMap")
                ? replacementMaterial.GetTexture("_BaseMap")
                : replacementMaterial.GetTexture("_MainTex");
            Assert.That(baseTexture, Is.Not.Null, "The dedicated rug material has no base texture.");
            Assert.That(
                AssetDatabase.GetAssetPath(baseTexture),
                Is.EqualTo(RugTexturePath),
                "The dedicated rug material must use the supplied Rug.png texture.");
            Assert.That(
                rugPrefab.GetComponentsInChildren<Collider>(true),
                Is.Empty,
                "The decorative rug must not introduce physics colliders.");
        }

        [Test]
        public void RugReplacementModelImporterDoesNotGenerateCollidersOrAnimations()
        {
            var importer = AssetImporter.GetAtPath(RugModelPath) as ModelImporter;

            Assert.That(importer, Is.Not.Null, $"Missing ModelImporter for {RugModelPath}.");
            Assert.That(importer.globalScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(importer.addCollider, Is.False, "The replacement FBX must remain collider-free.");
            Assert.That(importer.importAnimation, Is.False, "The static rug must not import animation data.");
        }

        [Test]
        public void PlaceGeneratedPropSizesAndBottomAlignsReplacementRug()
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                "PlaceGeneratedProp",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing StarterSceneBuilder.PlaceGeneratedProp.");

            var parent = new GameObject("Rug Placement Regression Test Root");
            try
            {
                method.Invoke(
                    null,
                    new object[]
                    {
                        parent.transform,
                        RugPrefabPath,
                        "Rug_Model",
                        new Vector3(0.005f, 0f, 0.28f),
                        RugHeight,
                        0f,
                        true,
                        0f,
                        FloorTop
                    });

                var rug = parent.transform.Find("Rug_Model");
                Assert.That(rug, Is.Not.Null, "PlaceGeneratedProp did not instantiate Rug_Model.");
                AssertPlacedRugContract(rug);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [TestCase("Assets/_Project/Scenes/Milkroom.unity")]
        [TestCase("Assets/_Project/Scenes/Debug.unity")]
        public void SavedSceneRugInstanceKeepsPrefabAndPlacementContract(string scenePath)
        {
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);

            try
            {
                Assert.That(scene.IsValid() && scene.isLoaded, Is.True, $"Could not inspect {scenePath}.");
                var rugMatches = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Where(transform => transform.name == "Rug_Model")
                    .ToArray();

                Assert.That(rugMatches, Has.Length.EqualTo(1),
                    $"{scenePath} must contain exactly one Rug_Model instance.");
                var rug = rugMatches[0];
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(rug.gameObject),
                    Is.EqualTo(RugPrefabPath),
                    $"{scenePath} Rug_Model lost its Rug.prefab connection.");
                AssertPlacedRugContract(rug);

                var character = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Single(transform => transform.name == "CheeseTamaRoot");
                Assert.That(character.position.y, Is.EqualTo(-1.1f).Within(PositionTolerance));
                var characterRenderers = character.GetComponentsInChildren<Renderer>(true);
                Assert.That(characterRenderers, Is.Not.Empty);
                var characterBounds = characterRenderers[0].bounds;
                for (var index = 1; index < characterRenderers.Length; index += 1)
                {
                    characterBounds.Encapsulate(characterRenderers[index].bounds);
                }

                var rugRenderers = rug.GetComponentsInChildren<Renderer>(true);
                var rugBounds = rugRenderers[0].bounds;
                for (var index = 1; index < rugRenderers.Length; index += 1)
                {
                    rugBounds.Encapsulate(rugRenderers[index].bounds);
                }

                Assert.That(
                    characterBounds.min.y,
                    Is.GreaterThan(rugBounds.max.y),
                    $"{scenePath} character is embedded in the thinner rug.");
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        private static Mesh GetRendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            return renderer.GetComponent<MeshFilter>()?.sharedMesh;
        }

        private static void AssertOptimizedReplacementMesh(Mesh mesh)
        {
            Assert.That(mesh.vertexCount, Is.InRange(1, MaxVertexCount), $"{RugModelPath} vertices");
            Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt16), $"{RugModelPath} index format");
            Assert.That(mesh.subMeshCount, Is.EqualTo(1), $"{RugModelPath} submesh count");
            Assert.That(mesh.GetTopology(0), Is.EqualTo(MeshTopology.Triangles), $"{RugModelPath} topology");

            var indexCount = mesh.GetIndexCount(0);
            Assert.That(indexCount % 3u, Is.EqualTo(0u), $"{RugModelPath} triangle indices");
            Assert.That(indexCount / 3UL, Is.InRange(1UL, MaxTriangleCount), $"{RugModelPath} triangles");

            var normals = mesh.normals;
            Assert.That(normals, Has.Length.EqualTo(mesh.vertexCount), $"{RugModelPath} normals");
            Assert.That(
                normals.All(normal => IsFinite(normal) && normal.sqrMagnitude > 0.25f),
                Is.True,
                $"{RugModelPath} contains a zero or non-finite normal");

            var uv = mesh.uv;
            Assert.That(uv, Has.Length.EqualTo(mesh.vertexCount), $"{RugModelPath} UV0");
            Assert.That(uv.All(IsFinite), Is.True, $"{RugModelPath} contains a non-finite UV0 coordinate");
            var minU = uv.Min(coordinate => coordinate.x);
            var maxU = uv.Max(coordinate => coordinate.x);
            var minV = uv.Min(coordinate => coordinate.y);
            var maxV = uv.Max(coordinate => coordinate.y);
            Assert.That(minU, Is.GreaterThanOrEqualTo(-0.01f), $"{RugModelPath} UV0 min U");
            Assert.That(maxU, Is.LessThanOrEqualTo(1.01f), $"{RugModelPath} UV0 max U");
            Assert.That(minV, Is.GreaterThanOrEqualTo(-0.01f), $"{RugModelPath} UV0 min V");
            Assert.That(maxV, Is.LessThanOrEqualTo(1.01f), $"{RugModelPath} UV0 max V");
            Assert.That(maxU - minU, Is.GreaterThanOrEqualTo(0.95f), $"{RugModelPath} UV0 U span");
            Assert.That(maxV - minV, Is.GreaterThanOrEqualTo(0.95f), $"{RugModelPath} UV0 V span");

            AssertVectorWithin(mesh.bounds.center, ExpectedLocalBounds.center, $"{RugModelPath} bounds center");
            AssertVectorWithin(mesh.bounds.size, ExpectedLocalBounds.size, $"{RugModelPath} bounds size");
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

        private static void AssertVectorWithin(Vector3 actual, Vector3 expected, string label)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(LocalBoundsTolerance), $"{label} X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(LocalBoundsTolerance), $"{label} Y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(LocalBoundsTolerance), $"{label} Z");
        }

        private static void AssertPlacedRugContract(Transform rug)
        {
            Assert.That(rug.position.x, Is.EqualTo(0.005f).Within(PositionTolerance), "Rug anchor X changed.");
            Assert.That(rug.position.z, Is.EqualTo(0.28f).Within(PositionTolerance), "Rug anchor Z changed.");
            Assert.That(rug.localScale.x, Is.EqualTo(rug.localScale.y).Within(PositionTolerance));
            Assert.That(rug.localScale.x, Is.EqualTo(rug.localScale.z).Within(PositionTolerance));

            var renderers = rug.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, "Rug_Model has no renderers.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index += 1)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            Assert.That(bounds.size.y, Is.EqualTo(RugHeight).Within(PositionTolerance), "Rug height changed.");
            Assert.That(bounds.min.y, Is.EqualTo(FloorTop).Within(PositionTolerance), "Rug no longer rests on the floor.");
            Assert.That(bounds.max.y, Is.EqualTo(RugTop).Within(PositionTolerance), "Rug top contact height changed.");
            Assert.That(
                bounds.size.x,
                Is.EqualTo(RugFootprint).Within(0.08f),
                "Rug footprint X no longer stays inside the room floor.");
            Assert.That(
                bounds.size.z,
                Is.EqualTo(RugFootprint).Within(0.08f),
                "Rug footprint Z no longer stays inside the room floor.");
            Assert.That(
                rug.GetComponentsInChildren<Collider>(true),
                Is.Empty,
                "The placed decorative rug must remain collider-free.");
        }
    }
}
