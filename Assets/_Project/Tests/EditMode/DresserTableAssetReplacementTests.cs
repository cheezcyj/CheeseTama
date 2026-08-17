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
    public sealed class DresserTableAssetReplacementTests
    {
        private const string PrefabPath = "Assets/Environments/Milkroom/Props/DresserTable.prefab";
        private const string PrefabGuid = "99e2115091dceaa49b345959610721d9";
        private const string ModelPath =
            "Assets/Environments/Milkroom/Props/DresserTable_Assets/Replacement/MilkCabinetReplacement.fbx";
        private const string TexturePath =
            "Assets/Environments/Milkroom/Props/DresserTable_Assets/Replacement/milkcabinet.JPEG";
        private const string MaterialPath =
            "Assets/Environments/Milkroom/Props/DresserTable_Assets/Replacement/MilkCabinetReplacement.mat";
        private const float FloorTop = -2.13f;
        private const float PlacedHeight = 1.50f;
        private const float PositionTolerance = 0.0005f;
        private const int MaxVertexCount = 65535;
        private const ulong MaxTriangleCount = 90000UL;
        private const float LocalBoundsTolerance = 0.001f;
        private static readonly Bounds ExpectedLocalBounds = new Bounds(
            new Vector3(0f, 0f, 0.49998474f),
            new Vector3(0.7830812f, 0.51724243f, 0.9999695f));

        [Test]
        public void PrefabPreservesGuidAndUsesSuppliedModelTextureAndMaterial()
        {
            Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath), Is.EqualTo(PrefabGuid));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            Assert.That(prefab, Is.Not.Null, $"Missing dresser prefab at {PrefabPath}.");
            Assert.That(model, Is.Not.Null, $"Missing replacement model at {ModelPath}.");
            Assert.That(texture, Is.Not.Null, $"Missing supplied texture at {TexturePath}.");
            Assert.That(material, Is.Not.Null, $"Missing dedicated material at {MaterialPath}.");
            Assert.That(prefab.transform.Find("DresserTable_ImportedModel"), Is.Not.Null);

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            var uniqueMeshes = renderers.Select(GetRendererMesh)
                .Where(mesh => mesh != null)
                .Distinct()
                .ToArray();
            Assert.That(
                uniqueMeshes.Select(AssetDatabase.GetAssetPath).ToArray(),
                Is.EqualTo(new[] { ModelPath }));
            Assert.That(uniqueMeshes, Has.Length.EqualTo(1));
            AssertOptimizedReplacementMesh(uniqueMeshes[0]);

            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Is.Not.Empty);
                Assert.That(renderer.sharedMaterials, Is.All.SameAs(material));
            }

            var baseTexture = material.HasProperty("_BaseMap")
                ? material.GetTexture("_BaseMap")
                : material.GetTexture("_MainTex");
            Assert.That(AssetDatabase.GetAssetPath(baseTexture), Is.EqualTo(TexturePath));
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);
        }

        [Test]
        public void ReplacementImporterIsStaticAndLightweightAtRuntime()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.globalScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(importer.addCollider, Is.False);
            Assert.That(importer.importAnimation, Is.False);
            Assert.That(importer.importTangents, Is.EqualTo(ModelImporterTangents.None));
            Assert.That(importer.isReadable, Is.False);
        }

        [Test]
        public void PlaceGeneratedPropSizesAndBottomAlignsReplacementDresser()
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                "PlaceGeneratedProp",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing StarterSceneBuilder.PlaceGeneratedProp.");

            var parent = new GameObject("Dresser Placement Regression Test Root");
            try
            {
                method.Invoke(
                    null,
                    new object[]
                    {
                        parent.transform,
                        PrefabPath,
                        "DresserTable_Model",
                        new Vector3(2.807f, 0f, 1.18f),
                        PlacedHeight,
                        200f,
                        true,
                        0f,
                        FloorTop
                    });

                var dresser = parent.transform.Find("DresserTable_Model");
                Assert.That(dresser, Is.Not.Null);
                AssertPlacedContract(dresser);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [TestCase("Assets/_Project/Scenes/Milkroom.unity")]
        [TestCase("Assets/_Project/Scenes/Debug.unity")]
        public void SavedSceneDresserKeepsPrefabAndPlacementContract(string scenePath)
        {
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var matches = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Where(transform => transform.name == "DresserTable_Model")
                    .ToArray();

                Assert.That(matches, Has.Length.EqualTo(1));
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(matches[0].gameObject),
                    Is.EqualTo(PrefabPath));
                AssertPlacedContract(matches[0]);
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
            return renderer is SkinnedMeshRenderer skinned
                ? skinned.sharedMesh
                : renderer.GetComponent<MeshFilter>()?.sharedMesh;
        }

        private static void AssertOptimizedReplacementMesh(Mesh mesh)
        {
            Assert.That(mesh.vertexCount, Is.InRange(1, MaxVertexCount), $"{ModelPath} vertices");
            Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt16), $"{ModelPath} index format");
            Assert.That(mesh.subMeshCount, Is.EqualTo(1), $"{ModelPath} submesh count");
            Assert.That(mesh.GetTopology(0), Is.EqualTo(MeshTopology.Triangles), $"{ModelPath} topology");

            var indexCount = mesh.GetIndexCount(0);
            Assert.That(indexCount % 3u, Is.EqualTo(0u), $"{ModelPath} triangle indices");
            Assert.That(indexCount / 3UL, Is.InRange(1UL, MaxTriangleCount), $"{ModelPath} triangles");

            var normals = mesh.normals;
            Assert.That(normals, Has.Length.EqualTo(mesh.vertexCount), $"{ModelPath} normals");
            Assert.That(
                normals.All(normal => IsFinite(normal) && normal.sqrMagnitude > 0.25f),
                Is.True,
                $"{ModelPath} contains a zero or non-finite normal");

            var uv = mesh.uv;
            Assert.That(uv, Has.Length.EqualTo(mesh.vertexCount), $"{ModelPath} UV0");
            Assert.That(uv.All(IsFinite), Is.True, $"{ModelPath} contains a non-finite UV0 coordinate");
            var minU = uv.Min(coordinate => coordinate.x);
            var maxU = uv.Max(coordinate => coordinate.x);
            var minV = uv.Min(coordinate => coordinate.y);
            var maxV = uv.Max(coordinate => coordinate.y);
            Assert.That(minU, Is.GreaterThanOrEqualTo(-0.01f), $"{ModelPath} UV0 min U");
            Assert.That(maxU, Is.LessThanOrEqualTo(1.01f), $"{ModelPath} UV0 max U");
            Assert.That(minV, Is.GreaterThanOrEqualTo(-0.01f), $"{ModelPath} UV0 min V");
            Assert.That(maxV, Is.LessThanOrEqualTo(1.01f), $"{ModelPath} UV0 max V");
            Assert.That(maxU - minU, Is.GreaterThanOrEqualTo(0.95f), $"{ModelPath} UV0 U span");
            Assert.That(maxV - minV, Is.GreaterThanOrEqualTo(0.95f), $"{ModelPath} UV0 V span");

            AssertVectorWithin(mesh.bounds.center, ExpectedLocalBounds.center, $"{ModelPath} bounds center");
            AssertVectorWithin(mesh.bounds.size, ExpectedLocalBounds.size, $"{ModelPath} bounds size");
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

        private static void AssertPlacedContract(Transform dresser)
        {
            Assert.That(dresser.position.x, Is.EqualTo(2.807f).Within(PositionTolerance));
            Assert.That(dresser.position.z, Is.EqualTo(1.18f).Within(PositionTolerance));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(dresser.eulerAngles.y, 200f)), Is.LessThan(0.01f));
            Assert.That(dresser.localScale.x, Is.EqualTo(dresser.localScale.y).Within(PositionTolerance));
            Assert.That(dresser.localScale.x, Is.EqualTo(dresser.localScale.z).Within(PositionTolerance));

            var renderers = dresser.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index += 1)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            Assert.That(bounds.size.y, Is.EqualTo(PlacedHeight).Within(PositionTolerance));
            Assert.That(bounds.min.y, Is.EqualTo(FloorTop).Within(PositionTolerance));
            Assert.That(bounds.max.y, Is.EqualTo(-0.63f).Within(PositionTolerance));
            Assert.That(bounds.size.x, Is.EqualTo(1.36919f).Within(0.015f));
            Assert.That(bounds.size.z, Is.EqualTo(1.13085f).Within(0.015f));
            Assert.That(dresser.GetComponentsInChildren<Collider>(true), Is.Empty);
        }
    }
}
