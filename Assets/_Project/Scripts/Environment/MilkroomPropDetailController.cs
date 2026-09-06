using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CheeseTama.Environment
{
    /// <summary>
    /// Keeps the authored meshes at close range and swaps distant Milkroom props to a
    /// twelve-triangle silhouette proxy before culling. The high preset keeps the
    /// transition deliberately late so the normal room view remains unchanged.
    /// </summary>
    public sealed class MilkroomPropDetailController : MonoBehaviour
    {
        private const string DetailProxyObjectName = "__CheeseTama Detail Proxy";

        private static readonly string[] ManagedPropNames =
        {
            "Fridge_Model",
            "MilkShelf_Model",
            "CozyChair_Model",
            "Window_Model",
            "Rug_Model",
            "DresserTable_Model",
            "Chalkboard_Model"
        };

        [SerializeField] private Transform propRoot;
        [SerializeField] private GraphicsQualityPreset currentPreset = GraphicsQualityPreset.High;

        private readonly List<LODGroup> managedGroups = new List<LODGroup>();

        private static Mesh detailProxyMesh;

        public GraphicsQualityPreset CurrentPreset => currentPreset;
        public int ManagedGroupCount => managedGroups.Count;
        public int ManagedProxyCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < managedGroups.Count; index += 1)
                {
                    if (managedGroups[index] != null
                        && managedGroups[index].transform.Find(DetailProxyObjectName) != null)
                    {
                        count += 1;
                    }
                }

                return count;
            }
        }

        public void Configure(Transform root)
        {
            propRoot = root;
            RebuildManagedGroups();
            ApplyPreset(currentPreset);
        }

        public void ApplyPreset(GraphicsQualityPreset preset)
        {
            currentPreset = GraphicsQualityCatalog.Normalize((int)preset);
            if (managedGroups.Count == 0)
            {
                RebuildManagedGroups();
            }

            var profile = GraphicsQualityCatalog.Get(currentPreset);
            for (var index = managedGroups.Count - 1; index >= 0; index -= 1)
            {
                var group = managedGroups[index];
                if (group == null)
                {
                    managedGroups.RemoveAt(index);
                    continue;
                }

                var renderers = GetAuthoredRenderers(group.transform);
                if (renderers.Count == 0)
                {
                    continue;
                }

                var keepsSourceWindowColor = group.name == "Window_Model";
                for (var rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex += 1)
                {
                    var renderer = renderers[rendererIndex];
                    renderer.shadowCastingMode = profile.PropShadows && !keepsSourceWindowColor
                        ? ShadowCastingMode.On
                        : ShadowCastingMode.Off;
                    renderer.receiveShadows = profile.PropShadows && !keepsSourceWindowColor;
                }

                var proxyRenderer = EnsureDetailProxy(group.transform, renderers);
                if (proxyRenderer == null)
                {
                    group.SetLODs(new[]
                    {
                        new LOD(profile.PropCullHeight, renderers.ToArray())
                    });
                    group.RecalculateBounds();
                    group.enabled = true;
                    continue;
                }

                proxyRenderer.shadowCastingMode = ShadowCastingMode.Off;
                proxyRenderer.receiveShadows = false;
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.SetLODs(new[]
                {
                    new LOD(profile.PropLowDetailHeight, renderers.ToArray())
                    {
                        fadeTransitionWidth = 0.2f
                    },
                    new LOD(profile.PropCullHeight, new Renderer[] { proxyRenderer })
                    {
                        fadeTransitionWidth = 0.2f
                    }
                });
                group.RecalculateBounds();
                group.enabled = true;
            }
        }

        private void RebuildManagedGroups()
        {
            managedGroups.Clear();
            if (propRoot == null)
            {
                return;
            }

            for (var index = 0; index < ManagedPropNames.Length; index += 1)
            {
                var prop = propRoot.Find(ManagedPropNames[index]);
                if (prop == null || prop.GetComponentsInChildren<Renderer>(true).Length == 0)
                {
                    continue;
                }

                var group = prop.GetComponent<LODGroup>();
                if (group == null)
                {
                    group = prop.gameObject.AddComponent<LODGroup>();
                }

                managedGroups.Add(group);
            }
        }

        private static List<Renderer> GetAuthoredRenderers(Transform root)
        {
            var result = new List<Renderer>();
            if (root == null)
            {
                return result;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index += 1)
            {
                var renderer = renderers[index];
                if (renderer != null
                    && !string.Equals(
                        renderer.transform.name,
                        DetailProxyObjectName,
                        StringComparison.Ordinal))
                {
                    result.Add(renderer);
                }
            }

            return result;
        }

        private static MeshRenderer EnsureDetailProxy(
            Transform groupRoot,
            IReadOnlyList<Renderer> authoredRenderers)
        {
            if (groupRoot == null || authoredRenderers == null || authoredRenderers.Count == 0)
            {
                return null;
            }

            var proxy = groupRoot.Find(DetailProxyObjectName);
            if (proxy == null)
            {
                var proxyObject = new GameObject(DetailProxyObjectName);
                proxyObject.hideFlags = HideFlags.DontSave;
                proxyObject.layer = groupRoot.gameObject.layer;
                proxyObject.transform.SetParent(groupRoot, false);
                proxy = proxyObject.transform;
            }

            var filter = proxy.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = proxy.gameObject.AddComponent<MeshFilter>();
            }

            var proxyRenderer = proxy.GetComponent<MeshRenderer>();
            if (proxyRenderer == null)
            {
                proxyRenderer = proxy.gameObject.AddComponent<MeshRenderer>();
            }

            filter.sharedMesh = GetOrCreateDetailProxyMesh();

            Material proxyMaterial = null;
            for (var index = 0; index < authoredRenderers.Count && proxyMaterial == null; index += 1)
            {
                var materials = authoredRenderers[index].sharedMaterials;
                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex += 1)
                {
                    if (materials[materialIndex] != null)
                    {
                        proxyMaterial = materials[materialIndex];
                        break;
                    }
                }
            }

            proxyRenderer.sharedMaterial = proxyMaterial;
            FitProxyToRendererBounds(groupRoot, proxy, authoredRenderers);
            return proxyRenderer;
        }

        private static void FitProxyToRendererBounds(
            Transform groupRoot,
            Transform proxy,
            IReadOnlyList<Renderer> renderers)
        {
            var worldBounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Count; index += 1)
            {
                worldBounds.Encapsulate(renderers[index].bounds);
            }

            var worldMin = worldBounds.min;
            var worldMax = worldBounds.max;
            var localMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var localMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (var corner = 0; corner < 8; corner += 1)
            {
                var worldCorner = new Vector3(
                    (corner & 1) == 0 ? worldMin.x : worldMax.x,
                    (corner & 2) == 0 ? worldMin.y : worldMax.y,
                    (corner & 4) == 0 ? worldMin.z : worldMax.z);
                var localCorner = groupRoot.InverseTransformPoint(worldCorner);
                localMin = Vector3.Min(localMin, localCorner);
                localMax = Vector3.Max(localMax, localCorner);
            }

            proxy.localPosition = (localMin + localMax) * 0.5f;
            proxy.localRotation = Quaternion.identity;
            proxy.localScale = localMax - localMin;
        }

        private static Mesh GetOrCreateDetailProxyMesh()
        {
            if (detailProxyMesh != null)
            {
                return detailProxyMesh;
            }

            detailProxyMesh = new Mesh
            {
                name = "CheeseTama Detail Proxy Cube",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
                    new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
                    new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f),
                    new Vector3(-0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f),
                    new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
                    new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(-0.5f, -0.5f,  0.5f),
                    new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
                    new Vector3( 0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f,  0.5f),
                    new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f,  0.5f),
                    new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f, -0.5f)
                },
                normals = new[]
                {
                    Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                    Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                    Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                    Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                    Vector3.right, Vector3.right, Vector3.right, Vector3.right,
                    Vector3.left, Vector3.left, Vector3.left, Vector3.left
                },
                uv = new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                },
                triangles = new[]
                {
                     0,  1,  2,  0,  2,  3,
                     4,  5,  6,  4,  6,  7,
                     8,  9, 10,  8, 10, 11,
                    12, 13, 14, 12, 14, 15,
                    16, 17, 18, 16, 18, 19,
                    20, 21, 22, 20, 22, 23
                }
            };
            detailProxyMesh.RecalculateBounds();
            detailProxyMesh.UploadMeshData(true);
            return detailProxyMesh;
        }
    }
}
