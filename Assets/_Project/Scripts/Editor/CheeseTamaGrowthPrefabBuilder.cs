using System;
using System.Collections.Generic;
using System.IO;
using CheeseTama.Data;
using CheeseTama.Gameplay.Growth;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CheeseTama.Editor
{
    [InitializeOnLoad]
    public static class CheeseTamaGrowthPrefabBuilder
    {
        private const int BuilderVersion = 27;
        private const string BuilderVersionKey = "CheeseTama.GrowthPrefabBuilder.Version";
        private const string GrowthPaletteShaderName = "CheeseTama/Growth Palette";
        private const string CharacterRoot = "Assets/Characters/CheeseTama";
        private const string GrowthRoot = CharacterRoot + "/GrowthStages";
        private const string SourceRoot = GrowthRoot + "/SourceModels";
        private const string MaterialRoot = GrowthRoot + "/Materials";
        private const string ThumbnailRoot = GrowthRoot + "/Thumbnails";
        private const string VisualSetPath = "Assets/_Project/Resources/CheeseTamaGrowthVisualSet.asset";
        private const float CharacterGroundY = -0.53f;
        private const float CleanCheeseYellowHue = 0.1054054f;
        private const float CleanCheeseYellowSaturation = 0.925f;
        private const float CleanCheeseYellowValueScale = 1.05f;
        private const float CleanCheeseYellowValueOffset = 0.05f;

        private static readonly StageSpec[] StageSpecs =
        {
            new StageSpec(
                CheeseTamaGrowthStage.Egg,
                "CheeseTama_Egg",
                SourceRoot + "/Stage01/CheeseTama_Stage01.fbx",
                SourceRoot + "/Stage01/CheeseTama_Stage01_BaseColor.jpg",
                0.78f,
                false,
                -90f,
                0.122f,
                0.12f,
                1.08f,
                0.1f,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Hatchling,
                "CheeseTama_Hatchling",
                SourceRoot + "/Stage02/CheeseTama_Stage02_Optimized.obj",
                SourceRoot + "/Stage02/CheeseTama_Stage02_BaseColor.jpg",
                0.84f,
                false,
                -90f,
                0.126f,
                0.25f,
                1.12f,
                0.18f,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Soft,
                "CheeseTama_Soft",
                SourceRoot + "/Stage03/CheeseTama_Stage03_Optimized.obj",
                SourceRoot + "/Stage03/CheeseTama_Stage03_BaseColor.jpg",
                0.98f,
                false,
                -90f,
                CleanCheeseYellowHue,
                CleanCheeseYellowSaturation,
                CleanCheeseYellowValueScale,
                CleanCheeseYellowValueOffset,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Grown,
                "CheeseTama_Grown",
                SourceRoot + "/Stage04/CheeseTama_Stage04_Optimized.obj",
                SourceRoot + "/Stage04/CheeseTama_Stage04_BaseColor.jpg",
                1.11f,
                false,
                -90f,
                CleanCheeseYellowHue,
                CleanCheeseYellowSaturation,
                CleanCheeseYellowValueScale,
                CleanCheeseYellowValueOffset,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Mature,
                "CheeseTama_Mature",
                SourceRoot + "/Stage05/CheeseTama_Stage05_Optimized.obj",
                SourceRoot + "/Stage05/CheeseTama_Stage05_BaseColor.jpg",
                1.14f,
                false,
                -90f,
                CleanCheeseYellowHue,
                CleanCheeseYellowSaturation,
                CleanCheeseYellowValueScale,
                CleanCheeseYellowValueOffset,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Final,
                "CheeseTama_Final",
                SourceRoot + "/Stage06/CheeseTama_Stage06_Optimized.obj",
                SourceRoot + "/Stage06/CheeseTama_Stage06_BaseColor.jpg",
                1.22f,
                false,
                -90f,
                CleanCheeseYellowHue,
                CleanCheeseYellowSaturation,
                CleanCheeseYellowValueScale,
                CleanCheeseYellowValueOffset,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None)
        };

        static CheeseTamaGrowthPrefabBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        [MenuItem("CheeseTama/성장 외형 프리팹 생성")]
        public static void BuildGrowthPrefabs()
        {
            EnsureFolders();
            ConfigureSourceImporters();

            var prefabs = new Dictionary<CheeseTamaGrowthStage, GameObject>();
            for (var i = 0; i < StageSpecs.Length; i++)
            {
                var spec = StageSpecs[i];
                var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.SourcePrefabPath);
                if (sourcePrefab == null)
                {
                    throw new InvalidOperationException($"Growth stage source model is missing: {spec.SourcePrefabPath}");
                }

                var material = spec.UsesCustomMaterial ? CreateOrUpdateStageMaterial(spec) : null;
                var root = BuildStagePrefab(spec, sourcePrefab, material);
                var path = GetPrefabPath(spec);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                prefabs[spec.Stage] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var thumbnails = new Dictionary<CheeseTamaGrowthStage, Sprite>();
            for (var i = 0; i < StageSpecs.Length; i++)
            {
                var spec = StageSpecs[i];
                thumbnails[spec.Stage] = RenderThumbnail(prefabs[spec.Stage], spec);
            }

            ConfigureVisualSet(prefabs, thumbnails);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorPrefs.SetInt(BuilderVersionKey, BuilderVersion);
            Debug.Log("CheeseTama growth visuals generated: Egg, Hatchling, Soft, Grown, Mature, Final");
        }

        private static void TryAutoBuild()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || Application.isPlaying)
            {
                EditorApplication.delayCall += TryAutoBuild;
                return;
            }

            if (EditorPrefs.GetInt(BuilderVersionKey, 0) >= BuilderVersion && GeneratedAssetsExist())
            {
                return;
            }

            try
            {
                BuildGrowthPrefabs();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static bool GeneratedAssetsExist()
        {
            if (AssetDatabase.LoadAssetAtPath<CheeseTamaGrowthVisualSet>(VisualSetPath) == null)
            {
                return false;
            }

            for (var i = 0; i < StageSpecs.Length; i++)
            {
                var spec = StageSpecs[i];
                if (AssetDatabase.LoadAssetAtPath<GameObject>(spec.SourcePrefabPath) == null ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(spec)) == null ||
                    AssetDatabase.LoadAssetAtPath<Sprite>(GetThumbnailPath(spec)) == null)
                {
                    return false;
                }

                if (spec.UsesCustomMaterial && AssetDatabase.LoadAssetAtPath<Material>(GetMaterialPath(spec)) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConfigureSourceImporters()
        {
            for (var i = 0; i < StageSpecs.Length; i++)
            {
                var spec = StageSpecs[i];
                if (!spec.UsesCustomMaterial)
                {
                    continue;
                }

                ConfigureModelImporter(spec);
                ConfigureTextureImporter(spec);
            }
        }

        private static void ConfigureModelImporter(StageSpec spec)
        {
            var assetPath = spec.SourcePrefabPath;
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Growth stage model importer is missing: {assetPath}");
            }

            var changed = false;
            changed |= SetIfDifferent(importer.importAnimation, false, value => importer.importAnimation = value);
            changed |= SetIfDifferent(importer.animationType, ModelImporterAnimationType.None, value => importer.animationType = value);
            changed |= SetIfDifferent(importer.importCameras, false, value => importer.importCameras = value);
            changed |= SetIfDifferent(importer.importLights, false, value => importer.importLights = value);
            changed |= SetIfDifferent(importer.importBlendShapes, false, value => importer.importBlendShapes = value);
            changed |= SetIfDifferent(importer.addCollider, false, value => importer.addCollider = value);
            changed |= SetIfDifferent(importer.isReadable, false, value => importer.isReadable = value);
            changed |= SetIfDifferent(importer.generateSecondaryUV, false, value => importer.generateSecondaryUV = value);
            changed |= SetIfDifferent(importer.optimizeMeshPolygons, true, value => importer.optimizeMeshPolygons = value);
            changed |= SetIfDifferent(importer.optimizeMeshVertices, true, value => importer.optimizeMeshVertices = value);
            changed |= SetIfDifferent(importer.meshCompression, ModelImporterMeshCompression.Medium, value => importer.meshCompression = value);
            changed |= SetIfDifferent(importer.importNormals, ModelImporterNormals.Import, value => importer.importNormals = value);
            changed |= SetIfDifferent(importer.importTangents, spec.TangentImportMode, value => importer.importTangents = value);
            changed |= SetIfDifferent(importer.materialImportMode, ModelImporterMaterialImportMode.None, value => importer.materialImportMode = value);

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureTextureImporter(StageSpec spec)
        {
            var assetPath = spec.TexturePath;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Growth stage texture importer is missing: {assetPath}");
            }

            var changed = false;
            changed |= SetIfDifferent(importer.textureType, TextureImporterType.Default, value => importer.textureType = value);
            changed |= SetIfDifferent(importer.sRGBTexture, true, value => importer.sRGBTexture = value);
            changed |= SetIfDifferent(importer.alphaSource, TextureImporterAlphaSource.None, value => importer.alphaSource = value);
            changed |= SetIfDifferent(importer.alphaIsTransparency, false, value => importer.alphaIsTransparency = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, true, value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(importer.filterMode, FilterMode.Bilinear, value => importer.filterMode = value);
            changed |= SetIfDifferent(importer.maxTextureSize, spec.TextureMaxSize, value => importer.maxTextureSize = value);
            changed |= SetIfDifferent(importer.textureCompression, TextureImporterCompression.CompressedHQ, value => importer.textureCompression = value);

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static bool SetIfDifferent<T>(T currentValue, T targetValue, Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, targetValue))
            {
                return false;
            }

            setter(targetValue);
            return true;
        }

        private static Material CreateOrUpdateStageMaterial(StageSpec spec)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.TexturePath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Growth stage texture is missing: {spec.TexturePath}");
            }

            var shader = Shader.Find(GrowthPaletteShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Growth palette shader is unavailable: {GrowthPaletteShaderName}");
            }

            var materialPath = GetMaterialPath(spec);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = spec.PrefabName + " Material"
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_PaletteHue", spec.PaletteHue);
            material.SetFloat("_PaletteSaturation", spec.PaletteSaturation);
            material.SetFloat("_PaletteValueScale", spec.PaletteValueScale);
            material.SetFloat("_PaletteValueOffset", spec.PaletteValueOffset);
            material.SetFloat("_PaletteStrength", 1f);
            material.SetFloat("_PaletteEmission", spec.Stage == CheeseTamaGrowthStage.Egg ? 0.1f : 0f);
            material.SetFloat("_EraseBlush", spec.Stage == CheeseTamaGrowthStage.Egg ? 1f : 0f);
            ConfigureFaceCleanup(material, spec);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.25f);
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureFaceCleanup(Material material, StageSpec spec)
        {
            if (!TryGetFaceCleanupSettings(spec.Stage, out var settings))
            {
                material.SetVector("_FaceCleanupRegion", Vector4.zero);
                material.SetVector("_FaceCleanupSurface0", Vector4.zero);
                material.SetVector("_FaceCleanupSurface1", Vector4.zero);
                return;
            }

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.SourcePrefabPath);
            var meshFilter = sourcePrefab != null
                ? sourcePrefab.GetComponentInChildren<MeshFilter>(true)
                : null;
            var sourceMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (sourceMesh == null || !TryFitFaceSurface(sourceMesh, settings, out var coefficients))
            {
                throw new InvalidOperationException(
                    $"Unable to fit the clean forehead surface for {spec.PrefabName}.");
            }

            material.SetVector(
                "_FaceCleanupRegion",
                new Vector4(
                    settings.CenterX,
                    settings.CenterY,
                    settings.RadiusX,
                    settings.RadiusY));
            material.SetVector(
                "_FaceCleanupSurface0",
                new Vector4(
                    coefficients[0],
                    coefficients[1],
                    coefficients[2],
                    coefficients[3]));
            material.SetVector(
                "_FaceCleanupSurface1",
                new Vector4(
                    coefficients[4],
                    coefficients[5],
                    settings.MinimumPaletteValue,
                    1f));
        }

        private static bool TryGetFaceCleanupSettings(
            CheeseTamaGrowthStage stage,
            out FaceCleanupSettings settings)
        {
            // The previous migration interpreted an authored cheese indentation as
            // the reported facial decoration. The actual unwanted decoration was
            // the separate normal-evolution accent presenter, so keep the source
            // growth meshes intact and clear the mistaken deformation on rebuild.
            settings = default;
            return false;
        }

        private static bool TryFitFaceSurface(
            Mesh sourceMesh,
            FaceCleanupSettings settings,
            out float[] coefficients)
        {
            coefficients = null;
            var vertices = sourceMesh.vertices;
            var normals = sourceMesh.normals;
            if (vertices == null || vertices.Length < 6)
            {
                return false;
            }

            var samples = new List<Vector3>();
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var normalFacesForward = normals == null
                    || normals.Length != vertices.Length
                    || normals[i].z > 0f;
                if (vertex.z <= 0f || !normalFacesForward)
                {
                    continue;
                }

                var deltaX = vertex.x - settings.CenterX;
                var deltaY = vertex.y - settings.CenterY;
                var normalizedX = deltaX / settings.RadiusX;
                var normalizedY = deltaY / settings.RadiusY;
                var ellipseRadius = Mathf.Sqrt(
                    normalizedX * normalizedX + normalizedY * normalizedY);
                if (ellipseRadius >= 1.1f && ellipseRadius <= 1.6f)
                {
                    samples.Add(new Vector3(deltaX, deltaY, vertex.z));
                }
            }

            if (!TrySolveQuadraticSurface(samples, null, out var firstFit))
            {
                return false;
            }

            var absoluteResiduals = new float[samples.Count];
            for (var i = 0; i < samples.Count; i++)
            {
                absoluteResiduals[i] = Mathf.Abs(
                    samples[i].z - EvaluateQuadraticSurface(firstFit, samples[i].x, samples[i].y));
            }

            Array.Sort(absoluteResiduals);
            var medianResidual = absoluteResiduals[absoluteResiduals.Length / 2];
            var maximumResidual = Mathf.Max(0.0025f, medianResidual * 3f);
            var accepted = new bool[samples.Count];
            var acceptedCount = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                accepted[i] = Mathf.Abs(
                    samples[i].z - EvaluateQuadraticSurface(firstFit, samples[i].x, samples[i].y))
                    <= maximumResidual;
                if (accepted[i])
                {
                    acceptedCount++;
                }
            }

            if (acceptedCount < 6 || !TrySolveQuadraticSurface(samples, accepted, out coefficients))
            {
                coefficients = firstFit;
            }

            var fittedCenterZ = coefficients[0];
            return float.IsFinite(fittedCenterZ) && fittedCenterZ > 0f;
        }

        private static bool TrySolveQuadraticSurface(
            IReadOnlyList<Vector3> samples,
            IReadOnlyList<bool> accepted,
            out float[] coefficients)
        {
            const int coefficientCount = 6;
            coefficients = null;
            if (samples == null || samples.Count < coefficientCount)
            {
                return false;
            }

            var augmented = new double[coefficientCount, coefficientCount + 1];
            var includedCount = 0;
            for (var sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
            {
                if (accepted != null && !accepted[sampleIndex])
                {
                    continue;
                }

                includedCount++;
                var sample = samples[sampleIndex];
                var basis = new[]
                {
                    1d,
                    (double)sample.x,
                    (double)sample.y,
                    (double)sample.x * sample.x,
                    (double)sample.x * sample.y,
                    (double)sample.y * sample.y
                };
                for (var row = 0; row < coefficientCount; row++)
                {
                    for (var column = 0; column < coefficientCount; column++)
                    {
                        augmented[row, column] += basis[row] * basis[column];
                    }

                    augmented[row, coefficientCount] += basis[row] * sample.z;
                }
            }

            if (includedCount < coefficientCount)
            {
                return false;
            }

            for (var pivotColumn = 0; pivotColumn < coefficientCount; pivotColumn++)
            {
                var pivotRow = pivotColumn;
                var pivotMagnitude = Math.Abs(augmented[pivotRow, pivotColumn]);
                for (var candidateRow = pivotColumn + 1;
                     candidateRow < coefficientCount;
                     candidateRow++)
                {
                    var candidateMagnitude = Math.Abs(augmented[candidateRow, pivotColumn]);
                    if (candidateMagnitude > pivotMagnitude)
                    {
                        pivotRow = candidateRow;
                        pivotMagnitude = candidateMagnitude;
                    }
                }

                if (pivotMagnitude < 1e-12d)
                {
                    return false;
                }

                if (pivotRow != pivotColumn)
                {
                    for (var column = pivotColumn; column <= coefficientCount; column++)
                    {
                        var temporary = augmented[pivotColumn, column];
                        augmented[pivotColumn, column] = augmented[pivotRow, column];
                        augmented[pivotRow, column] = temporary;
                    }
                }

                var divisor = augmented[pivotColumn, pivotColumn];
                for (var column = pivotColumn; column <= coefficientCount; column++)
                {
                    augmented[pivotColumn, column] /= divisor;
                }

                for (var row = 0; row < coefficientCount; row++)
                {
                    if (row == pivotColumn)
                    {
                        continue;
                    }

                    var factor = augmented[row, pivotColumn];
                    for (var column = pivotColumn; column <= coefficientCount; column++)
                    {
                        augmented[row, column] -= factor * augmented[pivotColumn, column];
                    }
                }
            }

            coefficients = new float[coefficientCount];
            for (var i = 0; i < coefficientCount; i++)
            {
                coefficients[i] = (float)augmented[i, coefficientCount];
                if (!float.IsFinite(coefficients[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static float EvaluateQuadraticSurface(
            IReadOnlyList<float> coefficients,
            float deltaX,
            float deltaY)
        {
            return coefficients[0]
                + coefficients[1] * deltaX
                + coefficients[2] * deltaY
                + coefficients[3] * deltaX * deltaX
                + coefficients[4] * deltaX * deltaY
                + coefficients[5] * deltaY * deltaY;
        }

        private static GameObject BuildStagePrefab(StageSpec spec, GameObject sourcePrefab, Material material)
        {
            var root = new GameObject(spec.PrefabName);
            var model = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (model == null)
            {
                model = Object.Instantiate(sourcePrefab);
            }

            model.name = "StageModel";
            model.transform.SetParent(root.transform, false);
            if (spec.ResetSourceRotation)
            {
                model.transform.localRotation = Quaternion.identity;
            }
            else if (!Mathf.Approximately(spec.WorldYawOffset, 0f))
            {
                model.transform.rotation = Quaternion.Euler(0f, spec.WorldYawOffset, 0f) * model.transform.rotation;
            }

            if (material != null)
            {
                AssignMaterial(model, material);
            }

            NormalizeModel(model, spec.TargetHeight);
            return root;
        }

        private static void AssignMaterial(GameObject model, Material material)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void NormalizeModel(GameObject model, float targetHeight)
        {
            var bounds = CalculateBounds(model);
            if (bounds.size.y <= 0.0001f)
            {
                throw new InvalidOperationException($"Growth stage model has no renderable height: {model.name}");
            }

            var uniformScale = targetHeight / bounds.size.y;
            model.transform.localScale *= uniformScale;
            bounds = CalculateBounds(model);
            model.transform.position += new Vector3(
                -bounds.center.x,
                CharacterGroundY - bounds.min.y,
                -bounds.center.z);
        }

        private static Sprite RenderThumbnail(GameObject prefab, StageSpec spec)
        {
            if (prefab == null)
            {
                return null;
            }

            var preview = new PreviewRenderUtility();
            Texture2D texture = null;
            try
            {
                var instance = Object.Instantiate(prefab);
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                instance.transform.localScale = Vector3.one * 1.7f;
                preview.AddSingleGO(instance);

                var bounds = CalculateBounds(instance);
                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.orthographic = true;
                camera.orthographicSize = 1.35f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;
                camera.transform.position = bounds.center + new Vector3(0f, 0.02f, -5f);
                camera.transform.LookAt(bounds.center + Vector3.up * 0.02f);

                preview.lights[0].intensity = 1.25f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
                preview.lights[1].intensity = 0.65f;
                preview.lights[1].transform.rotation = Quaternion.Euler(340f, 215f, 0f);
                preview.ambientColor = new Color(0.48f, 0.42f, 0.32f);

                var previousActive = RenderTexture.active;
                var renderTexture = RenderTexture.GetTemporary(
                    256,
                    256,
                    24,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default);
                try
                {
                    camera.targetTexture = renderTexture;
                    RenderTexture.active = renderTexture;
                    GL.Clear(true, true, Color.clear);
                    camera.clearFlags = CameraClearFlags.Depth;
                    camera.Render();

                    texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, false);
                    texture.ReadPixels(new Rect(0f, 0f, 256f, 256f), 0, 0);
                    texture.Apply(false, false);
                    ApplyThumbnailColorCorrection(
                        texture,
                        spec.UsesCustomMaterial ? 1.08f : 1f,
                        spec.UsesCustomMaterial ? 4f : 0f);
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previousActive;
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                var thumbnailPath = GetThumbnailPath(spec);
                File.WriteAllBytes(Path.GetFullPath(thumbnailPath), texture.EncodeToPNG());
                AssetDatabase.ImportAsset(thumbnailPath, ImportAssetOptions.ForceSynchronousImport);

                var importer = AssetImporter.GetAtPath(thumbnailPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }

                return AssetDatabase.LoadAssetAtPath<Sprite>(thumbnailPath);
            }
            finally
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }

                preview.Cleanup();
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void ApplyThumbnailColorCorrection(Texture2D texture, float multiplier, float offset)
        {
            var pixels = texture.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.a == 0)
                {
                    continue;
                }

                pixel.r = BrightenThumbnailChannel(pixel.r, multiplier, offset);
                pixel.g = BrightenThumbnailChannel(pixel.g, multiplier, offset);
                pixel.b = BrightenThumbnailChannel(pixel.b, multiplier, offset);
                pixels[i] = pixel;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static byte BrightenThumbnailChannel(byte value, float multiplier, float offset)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * multiplier + offset), 0, 255);
        }

        private static void ConfigureVisualSet(
            IReadOnlyDictionary<CheeseTamaGrowthStage, GameObject> prefabs,
            IReadOnlyDictionary<CheeseTamaGrowthStage, Sprite> thumbnails)
        {
            var visualSet = AssetDatabase.LoadAssetAtPath<CheeseTamaGrowthVisualSet>(VisualSetPath);
            if (visualSet == null)
            {
                visualSet = ScriptableObject.CreateInstance<CheeseTamaGrowthVisualSet>();
                AssetDatabase.CreateAsset(visualSet, VisualSetPath);
            }

            var serialized = new SerializedObject(visualSet);
            var entries = serialized.FindProperty("entries");
            entries.arraySize = StageSpecs.Length;
            for (var i = 0; i < StageSpecs.Length; i++)
            {
                var spec = StageSpecs[i];
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("stage").enumValueIndex = (int)spec.Stage;
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefabs[spec.Stage];
                entry.FindPropertyRelative("thumbnail").objectReferenceValue = thumbnails[spec.Stage];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visualSet);
        }

        private static string GetPrefabPath(StageSpec spec)
        {
            return $"{GrowthRoot}/{spec.PrefabName}.prefab";
        }

        private static string GetThumbnailPath(StageSpec spec)
        {
            return $"{ThumbnailRoot}/{spec.PrefabName}_Thumb.png";
        }

        private static string GetMaterialPath(StageSpec spec)
        {
            return $"{MaterialRoot}/{spec.PrefabName}.mat";
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Characters", "CheeseTama");
            EnsureFolder(CharacterRoot, "GrowthStages");
            EnsureFolder(GrowthRoot, "Materials");
            EnsureFolder(GrowthRoot, "Thumbnails");
            EnsureFolder("Assets/_Project", "Resources");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private readonly struct StageSpec
        {
            public StageSpec(
                CheeseTamaGrowthStage stage,
                string prefabName,
                string sourcePrefabPath,
                string texturePath,
                float targetHeight,
                bool resetSourceRotation,
                float worldYawOffset,
                float paletteHue,
                float paletteSaturation,
                float paletteValueScale,
                float paletteValueOffset,
                int textureMaxSize = 2048,
                ModelImporterTangents tangentImportMode = ModelImporterTangents.CalculateMikk)
            {
                Stage = stage;
                PrefabName = prefabName;
                SourcePrefabPath = sourcePrefabPath;
                TexturePath = texturePath;
                TargetHeight = targetHeight;
                ResetSourceRotation = resetSourceRotation;
                WorldYawOffset = worldYawOffset;
                PaletteHue = paletteHue;
                PaletteSaturation = paletteSaturation;
                PaletteValueScale = paletteValueScale;
                PaletteValueOffset = paletteValueOffset;
                TextureMaxSize = textureMaxSize;
                TangentImportMode = tangentImportMode;
            }

            public CheeseTamaGrowthStage Stage { get; }
            public string PrefabName { get; }
            public string SourcePrefabPath { get; }
            public string TexturePath { get; }
            public float TargetHeight { get; }
            public bool ResetSourceRotation { get; }
            public float WorldYawOffset { get; }
            public float PaletteHue { get; }
            public float PaletteSaturation { get; }
            public float PaletteValueScale { get; }
            public float PaletteValueOffset { get; }
            public int TextureMaxSize { get; }
            public ModelImporterTangents TangentImportMode { get; }
            public bool UsesCustomMaterial => !string.IsNullOrEmpty(TexturePath);
        }

        private readonly struct FaceCleanupSettings
        {
            public FaceCleanupSettings(
                float centerX,
                float centerY,
                float radiusX,
                float radiusY,
                float minimumPaletteValue)
            {
                CenterX = centerX;
                CenterY = centerY;
                RadiusX = radiusX;
                RadiusY = radiusY;
                MinimumPaletteValue = minimumPaletteValue;
            }

            public float CenterX { get; }
            public float CenterY { get; }
            public float RadiusX { get; }
            public float RadiusY { get; }
            public float MinimumPaletteValue { get; }
        }
    }
}
