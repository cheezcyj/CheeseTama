using System;
using System.IO;
using System.Linq;
using CheeseTama.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CheeseTama.Editor
{
    /// <summary>Imports the original seven-prop set without depending on retired source assets.</summary>
    public static class OriginalMilkroomPropAssets
    {
        public const string Root = "Assets/Environments/Milkroom/Props/OriginalModels";
        private const string PrefabRoot = "Assets/Environments/Milkroom/Props";
        private const string LegacyOverlay = "Clean Cheese Cushion Overlay";

        private sealed class Prop
        {
            public readonly string Prefab;
            public readonly string Model;
            public readonly Vector3 Size;
            public readonly int Triangles;
            public string Folder => Root + "/" + Model;
            public string SourcePath => Folder + "/" + Model + ".fbx";
            public string MeshPath => Folder + "/" + Model + "_Mesh.asset";
            public string MaterialPath => Folder + "/" + Model + ".mat";
            public Prop(string prefab, string model, Vector3 size, int triangles)
            {
                Prefab = prefab; Model = model; Size = size; Triangles = triangles;
            }
        }

        private static readonly Prop[] Props =
        {
            new Prop("CozyChair", "CozyChair", new Vector3(.916f, 1.061f, .82f), 7606),
            new Prop("Fridge", "Fridge", new Vector3(.74f, 1.38f, .6465f), 9968),
            new Prop("Window", "Window", new Vector3(1.35f, 1.499f, .3995382f), 23780),
            new Prop("MilkShelf", "MilkShelf", new Vector3(1.02f, 1.4f, .3815f), 19884),
            new Prop("DresserTable", "MilkCabinet", new Vector3(1.18f, .95f, .5f), 15380),
            new Prop("Chalkboard", "Chalkboard", new Vector3(1.05f, .85f, .234f), 7552),
            new Prop("Rug", "Rug", new Vector3(2.2f, .06f, 2.2f), 6240)
        };

        public static void ConfigureAllAssets()
        {
            foreach (var prop in Props) Configure(prop);
            AssetDatabase.SaveAssets();
        }

        public static void ApplyAll()
        {
            ConfigureAllAssets();
            foreach (var prop in Props) UpdatePrefabContents(prop);
            AssetDatabase.SaveAssets();
        }

        public static void UpdatePrefab(string prefabName)
        {
            var prop = Find(prefabName);
            Configure(prop);
            UpdatePrefabContents(prop);
            AssetDatabase.SaveAssets();
        }

        public static GameObject BuildProp(string prefabName)
        {
            var prop = Find(prefabName);
            Configure(prop);
            var root = new GameObject(prefabName);
            try { ApplyToRoot(root, prefabName); return root; }
            catch { Object.DestroyImmediate(root); throw; }
        }

        /// <summary>Also supports an in-memory clone for repeatability checks.</summary>
        public static void ApplyToRoot(GameObject root, string prefabName)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var prop = Find(prefabName);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(prop.MeshPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(prop.MaterialPath);
            if (mesh == null || material == null)
                throw new InvalidOperationException("Configure original model assets before replacing a prop.");

            var nested = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t != root.transform && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                .OrderByDescending(t => Depth(t)).ToArray();
            foreach (var t in nested)
                if (t != null && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                    PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);

            var filter = root.GetComponentsInChildren<MeshFilter>(true)
                .FirstOrDefault(f => f.name != LegacyOverlay);
            if (filter == null)
            {
                var model = new GameObject(prefabName + "_ImportedModel");
                model.transform.SetParent(root.transform, false);
                filter = model.AddComponent<MeshFilter>();
            }
            // Keep the existing renderer component: saved scene LODs reference its local ID.
            var renderer = filter.GetComponent<MeshRenderer>() ?? filter.gameObject.AddComponent<MeshRenderer>();
            foreach (var other in root.GetComponentsInChildren<MeshFilter>(true))
                if (other != filter) Object.DestroyImmediate(other.gameObject);
            filter.name = prefabName + "_ImportedModel";
            for (var t = filter.transform.parent; t != null && t != root.transform; t = t.parent)
            {
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }
            filter.transform.localPosition = Vector3.zero;
            filter.transform.localRotation = prefabName == "Chalkboard"
                ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            filter.transform.localScale = Vector3.one;
            filter.sharedMesh = mesh;
            renderer.sharedMaterials = new[] { material };
            renderer.shadowCastingMode = prefabName == "Window" ? ShadowCastingMode.Off : ShadowCastingMode.On;
            renderer.receiveShadows = prefabName != "Window";
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
        }

        private static int Depth(Transform t)
        {
            var count = 0;
            while (t != null) { count++; t = t.parent; }
            return count;
        }

        private static Prop Find(string prefabName)
        {
            var prop = Props.FirstOrDefault(p => p.Prefab == prefabName);
            if (prop == null) throw new ArgumentException("Unknown original prop: " + prefabName);
            return prop;
        }

        private static void UpdatePrefabContents(Prop prop)
        {
            var path = PrefabRoot + "/" + prop.Prefab + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new InvalidOperationException("Required functional wrapper is missing: " + path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyToRoot(root, prop.Prefab);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            AssetDatabase.SetLabels(prefab, AssetDatabase.GetLabels(prefab)
                .Where(label => label != "UnityAI").ToArray());
        }

        private static void Configure(Prop prop)
        {
            if (!File.Exists(prop.SourcePath))
                throw new FileNotFoundException("Original source FBX is required.", prop.SourcePath);
            AssetDatabase.ImportAsset(prop.SourcePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(prop.SourcePath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Expected FBX ModelImporter.");
            var dirty = importer.globalScale != 1f || !importer.useFileScale || importer.bakeAxisConversion
                || importer.importAnimation || importer.importBlendShapes || importer.importCameras
                || importer.importLights || importer.addCollider || !importer.isReadable
                || importer.generateSecondaryUV || !importer.weldVertices
                || importer.importNormals != ModelImporterNormals.Import
                || importer.importTangents != ModelImporterTangents.None
                || importer.materialImportMode != ModelImporterMaterialImportMode.None;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = true;
            importer.generateSecondaryUV = false;
            importer.weldVertices = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            if (dirty) importer.SaveAndReimport();
            CreateNormalizedMesh(prop);
            ConfigureTexture(prop.Folder + "/BaseColor.png", true, false);
            ConfigureTexture(prop.Folder + "/Roughness.png", false, true);
            CreateSmoothnessMap(prop);
            ConfigureMaterial(prop);
        }

        private static void CreateNormalizedMesh(Prop prop)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(prop.SourcePath);
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length != 1 || filters[0].sharedMesh == null)
                throw new InvalidOperationException("Original FBX must contain exactly one mesh: " + prop.SourcePath);
            var filter = filters[0];
            var mesh = Object.Instantiate(filter.sharedMesh);
            mesh.name = prop.Model + "_Mesh";
            // Bake the actual FBX import transform, including its cm-to-metre scale.
            // Hardcoded 90-degree rotations would lose the importer unit conversion.
            var matrix = filter.transform.localToWorldMatrix;
            var vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
            mesh.vertices = vertices;
            var normals = mesh.normals;
            var normalMatrix = matrix.inverse.transpose;
            for (var i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
            mesh.normals = normals;
            if (matrix.determinant < 0f)
            {
                var triangles = mesh.triangles;
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    var swap = triangles[i]; triangles[i] = triangles[i + 1]; triangles[i + 1] = swap;
                }
                mesh.triangles = triangles;
            }
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            if ((mesh.bounds.size - prop.Size).sqrMagnitude > .000001f
                || Mathf.Abs(mesh.bounds.min.y) > .0001f
                || mesh.subMeshCount != 1 || mesh.GetIndexCount(0) / 3 != prop.Triangles)
            {
                Object.DestroyImmediate(mesh);
                throw new InvalidOperationException("Original import geometry does not match its recorded source: " + prop.Model);
            }
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(prop.MeshPath);
            if (existing == null) AssetDatabase.CreateAsset(mesh, prop.MeshPath);
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
                // CopySerialized updates CPU data, but an open scene can still draw
                // the prior vertex/index buffers after a topology-changing reimport.
                existing.UploadMeshData(false);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
            }
        }

        private static void ConfigureTexture(string path, bool srgb, bool readable)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing original texture: " + path);
            var compressed = srgb ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            var alpha = path.EndsWith("MetallicSmoothness.png", StringComparison.Ordinal)
                ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            var dirty = importer.sRGBTexture != srgb || importer.isReadable != readable
                || importer.textureCompression != compressed || importer.maxTextureSize != 1024
                || importer.crunchedCompression != srgb || importer.compressionQuality != 80
                || !importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp || importer.alphaSource != alpha;
            importer.sRGBTexture = srgb;
            importer.isReadable = readable;
            importer.textureCompression = compressed;
            importer.maxTextureSize = 1024;
            importer.crunchedCompression = srgb;
            importer.compressionQuality = 80;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = alpha;
            if (dirty) importer.SaveAndReimport();
        }

        private static void CreateSmoothnessMap(Prop prop)
        {
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(prop.Folder + "/Roughness.png");
            var pixels = source.GetPixels32();
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, (byte)(255 - pixels[i].r));
            var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            try
            {
                output.SetPixels32(pixels);
                output.Apply();
                var bytes = output.EncodeToPNG();
                var path = prop.Folder + "/MetallicSmoothness.png";
                if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes)) File.WriteAllBytes(path, bytes);
                ConfigureTexture(path, false, false);
            }
            finally { Object.DestroyImmediate(output); }
        }

        private static void ConfigureMaterial(Prop prop)
        {
            var shader = prop.Prefab == "Window"
                ? AssetDatabase.LoadAssetAtPath<Shader>(prop.Folder + "/WindowSourceColor.shader")
                : Shader.Find(GraphicsSettings.currentRenderPipeline == null
                    ? "Standard" : "Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Required original-prop shader is unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(prop.MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = prop.Model };
                AssetDatabase.CreateAsset(material, prop.MaterialPath);
            }
            material.shader = shader;
            material.shaderKeywords = Array.Empty<string>();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(prop.Folder + "/BaseColor.png");
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (prop.Prefab != "Window")
            {
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prop.Folder + "/MetallicSmoothness.png"));
                material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1f);
                if (material.HasProperty("_GlossMapScale")) material.SetFloat("_GlossMapScale", 1f);
                material.SetFloat("_SmoothnessTextureChannel", 0f);
                material.SetColor("_EmissionColor", Color.black);
                material.EnableKeyword(GraphicsSettings.currentRenderPipeline == null
                    ? "_METALLICGLOSSMAP" : "_METALLICSPECGLOSSMAP");
            }
            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);
        }

        public static void ApplyScenePlacements()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before migrating scenes.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save or discard scene changes first.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in new[] { "Assets/_Project/Scenes/Milkroom.unity", "Assets/_Project/Scenes/Debug.unity" })
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var room = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                        .First(t => t.name == "Milkroom Background");
                    Place(room, "Fridge", new Vector3(-1.75f, 0f, 2.0f), 2.1f, 180f, true, 0f);
                    Place(room, "MilkShelf", new Vector3(2.65f, 0f, 2.34f), 1.3f, 180f, false, -.15f);
                    Place(room, "CozyChair", new Vector3(-2.7f, 0f, .2f), 1.5f, 150f, true, 0f);
                    Place(room, "Window", new Vector3(.45f, 0f, 2.32f), 1.72f, 180f, false, -.15f);
                    Place(room, "DresserTable", new Vector3(2.807f, 0f, 1.18f), 1.5f, 200f, true, 0f);
                    Place(room, "Chalkboard", new Vector3(-2.78f, 0f, 2.41f), 1.18f, 0f, false, .05f);
                    Place(room, "Rug", new Vector3(.005f, 0f, .28f), .06f, 0f, true, 0f);
                    PrepareAuthoredLodsForSave(room);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void Place(Transform room, string prefab, Vector3 anchor, float height, float yaw, bool floor, float center)
        {
            var target = room.Find(prefab + "_Model");
            if (target == null) throw new MissingReferenceException("Missing functional prop: " + prefab);
            var mods = PrefabUtility.GetPropertyModifications(target.gameObject);
            if (mods != null && mods.Any(m => m.target == null))
                PrefabUtility.SetPropertyModifications(target.gameObject, mods.Where(m => m.target != null).ToArray());
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.Euler(0f, yaw, 0f);
            target.localScale = Vector3.one;
            var renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name != "__CheeseTama Detail Proxy").ToArray();
            if (renderers.Length != 1) throw new InvalidOperationException("Expected one authored renderer: " + prefab);
            var bounds = renderers[0].bounds;
            target.localScale = Vector3.one * (height / bounds.size.y);
            bounds = renderers[0].bounds;
            var y = target.position.y + (floor ? -2.13f - bounds.min.y : center - bounds.center.y);
            target.position = new Vector3(anchor.x, y, anchor.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private static void PrepareAuthoredLodsForSave(Transform room)
        {
            // Runtime quality configuration creates DontSave proxies. Persisting those
            // in a scene's LOD array serializes null renderer references after reload.
            foreach (var name in Props.Select(p => p.Prefab))
            {
                var target = room.Find(name + "_Model");
                if (target == null) continue;
                var group = target.GetComponent<LODGroup>();
                if (group == null) continue;
                var levels = group.GetLODs();
                var cullHeight = levels.Length == 0 ? .001f
                    : levels[levels.Length - 1].screenRelativeTransitionHeight;
                var proxy = target.Find("__CheeseTama Detail Proxy");
                if (proxy != null) Object.DestroyImmediate(proxy.gameObject);
                var renderers = target.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("No authored LOD renderers: " + name);
                group.SetLODs(new[] { new LOD(cullHeight, renderers) });
                group.RecalculateBounds();
                EditorUtility.SetDirty(group);
            }
        }
    }
}
