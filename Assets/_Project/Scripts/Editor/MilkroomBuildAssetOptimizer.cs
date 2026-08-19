using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CheeseTama.Editor
{
    public static class MilkroomBuildAssetOptimizer
    {
        private const string OptimizedMeshRoot =
            "Assets/Environments/Milkroom/Props/Optimized";

        private static readonly string[] PrefabPaths =
        {
            "Assets/Environments/Milkroom/Props/Chalkboard.prefab",
            "Assets/Environments/Milkroom/Props/CozyChair.prefab",
            "Assets/Environments/Milkroom/Props/Fridge.prefab"
        };

        private static readonly string[] PropTexturePaths =
        {
            "Assets/Environments/Milkroom/Props/Chalkboard_Assets/ChalkboardCrisp.png",
            "Assets/Environments/Milkroom/Props/CozyChair_Assets/CozyChairWhite.png",
            "Assets/Environments/Milkroom/Props/Fridge_Assets/FridgeWhite.png"
        };

        private static readonly string[] TopBarIconPaths =
        {
            "Assets/_Project/Resources/UI/TopBarIcons/coin.png",
            "Assets/_Project/Resources/UI/TopBarIcons/milkdrop.png",
            "Assets/_Project/Resources/UI/TopBarIcons/collectionpuzzle.png"
        };

        [MenuItem("CheeseTama/빌드 자산 최적화 적용")]
        public static void OptimizeMilkroomBuildAssets()
        {
            ApplyOptimization(true);
        }

        public static void ApplyOptimization(bool logCompletion)
        {
            EnsureFolder(OptimizedMeshRoot);
            for (var index = 0; index < PrefabPaths.Length; index += 1)
            {
                OptimizePrefabMeshes(PrefabPaths[index]);
            }

            for (var index = 0; index < PropTexturePaths.Length; index += 1)
            {
                ConfigureTexture(PropTexturePaths[index], 1024, false);
            }

            for (var index = 0; index < TopBarIconPaths.Length; index += 1)
            {
                ConfigureTexture(TopBarIconPaths[index], 256, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (logCompletion)
            {
                Debug.Log(
                    "Milkroom build assets optimized: embedded GLB mesh references removed, "
                    + "prop textures capped at 1024, top-bar icons capped at 256.");
            }
        }

        private static void OptimizePrefabMeshes(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                throw new InvalidOperationException($"Prefab could not be loaded: {prefabPath}");
            }

            try
            {
                UnpackNestedModelInstances(root);
                var meshAssets = new Dictionary<int, Mesh>();
                var meshIndex = 0;
                foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!ReferencesGlb(meshFilter.sharedMesh))
                    {
                        continue;
                    }

                    meshFilter.sharedMesh = CopyMesh(
                        meshFilter.sharedMesh,
                        prefabPath,
                        ref meshIndex,
                        meshAssets);
                }

                foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!ReferencesGlb(renderer.sharedMesh))
                    {
                        continue;
                    }

                    renderer.sharedMesh = CopyMesh(
                        renderer.sharedMesh,
                        prefabPath,
                        ref meshIndex,
                        meshAssets);
                }

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var dependencies = AssetDatabase.GetDependencies(prefabPath, true);
            var glbDependency = dependencies.FirstOrDefault(
                path => path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(glbDependency))
            {
                throw new InvalidOperationException(
                    $"GLB dependency remains after optimizing {prefabPath}: {glbDependency}");
            }
        }

        private static void UnpackNestedModelInstances(GameObject root)
        {
            var nestedRoots = root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != root.transform
                    && PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject))
                .OrderByDescending(GetDepth)
                .ToArray();
            for (var index = 0; index < nestedRoots.Length; index += 1)
            {
                if (nestedRoots[index] == null
                    || !PrefabUtility.IsAnyPrefabInstanceRoot(nestedRoots[index].gameObject))
                {
                    continue;
                }

                PrefabUtility.UnpackPrefabInstance(
                    nestedRoots[index].gameObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }
        }

        private static int GetDepth(Transform transform)
        {
            var depth = 0;
            while (transform != null)
            {
                depth += 1;
                transform = transform.parent;
            }

            return depth;
        }

        private static bool ReferencesGlb(Object asset)
        {
            return asset != null
                && AssetDatabase.GetAssetPath(asset)
                    .EndsWith(".glb", StringComparison.OrdinalIgnoreCase);
        }

        private static Mesh CopyMesh(
            Mesh source,
            string prefabPath,
            ref int meshIndex,
            Dictionary<int, Mesh> meshAssets)
        {
            if (source == null)
            {
                return null;
            }

            var sourceId = source.GetInstanceID();
            if (meshAssets.TryGetValue(sourceId, out var reused))
            {
                return reused;
            }

            meshIndex += 1;
            var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            var safeMeshName = SanitizeFileName(
                string.IsNullOrWhiteSpace(source.name) ? "Mesh" : source.name);
            var assetPath = $"{OptimizedMeshRoot}/{prefabName}_{meshIndex:D2}_{safeMeshName}.asset";
            var copy = Object.Instantiate(source);
            copy.name = $"{prefabName}_{safeMeshName}";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(copy, assetPath);
                existing = copy;
            }
            else
            {
                EditorUtility.CopySerialized(copy, existing);
                Object.DestroyImmediate(copy);
                EditorUtility.SetDirty(existing);
            }

            meshAssets[sourceId] = existing;
            return existing;
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var result = value;
            for (var index = 0; index < invalid.Length; index += 1)
            {
                result = result.Replace(invalid[index], '_');
            }

            return result.Replace(' ', '_');
        }

        private static void ConfigureTexture(string path, int maxSize, bool isUi)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                throw new InvalidOperationException($"Texture importer not found: {path}");
            }

            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = true;
            importer.compressionQuality = 80;
            importer.mipmapEnabled = !isUi;
            if (isUi)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
            }

            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index += 1)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
