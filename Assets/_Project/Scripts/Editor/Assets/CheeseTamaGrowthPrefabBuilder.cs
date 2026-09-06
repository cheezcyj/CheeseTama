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
        private const int BuilderVersion = 30;
        private const string BuilderVersionKey = "CheeseTama.GrowthPrefabBuilder.Version";
        private const string GrowthPaletteShaderName = "CheeseTama/Growth Palette";
        private const string CharacterRoot = "Assets/Characters/CheeseTama";
        private const string GrowthRoot = CharacterRoot + "/GrowthStages";
        private const string SourceRoot = GrowthRoot + "/SourceModels";
        private const string MaterialRoot = GrowthRoot + "/Materials";
        private const string ThumbnailRoot = GrowthRoot + "/Thumbnails";
        private const string VisualSetPath = "Assets/_Project/Resources/CheeseTamaGrowthVisualSet.asset";
        private const float CharacterGroundY = -0.53f;

        private static readonly StageSpec[] StageSpecs =
        {
            new StageSpec(
                CheeseTamaGrowthStage.Egg,
                "CheeseTama_Egg",
                SourceRoot + "/UnityReady/Stage01/CheeseTama_Stage01_Unity.fbx",
                SourceRoot + "/UnityReady/Stage01/CheeseTama_Stage01_Atlas.png",
                SourceRoot + "/UnityReady/Stage01/CheeseTama_Egg_Thumb.png",
                0.42f,
                false,
                -90f,
                0f,
                0f,
                1f,
                0f,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Hatchling,
                "CheeseTama_Hatchling",
                SourceRoot + "/UnityReady/Stage02/CheeseTama_Stage02_Unity.fbx",
                SourceRoot + "/UnityReady/Stage02/CheeseTama_Stage02_Atlas.png",
                SourceRoot + "/UnityReady/Stage02/CheeseTama_Hatchling_Thumb.png",
                0.52f,
                false,
                -90f,
                0f,
                0f,
                1f,
                0f,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Soft,
                "CheeseTama_Soft",
                SourceRoot + "/UnityReady/Stage03/CheeseTama_Stage03_Unity.fbx",
                SourceRoot + "/UnityReady/Stage03/CheeseTama_Stage03_Atlas.png",
                SourceRoot + "/UnityReady/Stage03/CheeseTama_Soft_Thumb.png",
                0.62f,
                false,
                -90f,
                0f,
                0f,
                1f,
                0f,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Grown,
                "CheeseTama_Grown",
                SourceRoot + "/UnityReady/Stage04/CheeseTama_Stage04_Unity.fbx",
                SourceRoot + "/UnityReady/Stage04/CheeseTama_Stage04_Atlas.png",
                SourceRoot + "/UnityReady/Stage04/CheeseTama_Grown_Thumb.png",
                0.7f,
                false,
                -90f,
                0f,
                0f,
                1f,
                0f,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Mature,
                "CheeseTama_Mature",
                SourceRoot + "/UnityReady/Stage05/CheeseTama_Stage05_Unity.fbx",
                SourceRoot + "/UnityReady/Stage05/CheeseTama_Stage05_Atlas.png",
                SourceRoot + "/UnityReady/Stage05/CheeseTama_Mature_Thumb.png",
                0.85f,
                false,
                -90f,
                0f,
                0f,
                1f,
                0f,
                textureMaxSize: 1024,
                tangentImportMode: ModelImporterTangents.None),
            new StageSpec(
                CheeseTamaGrowthStage.Final,
                "CheeseTama_Final",
                SourceRoot + "/UnityReady/Stage06/CheeseTama_Stage06_Unity.fbx",
                SourceRoot + "/UnityReady/Stage06/CheeseTama_Stage06_Atlas.png",
                SourceRoot + "/UnityReady/Stage06/CheeseTama_Final_Thumb.png",
                1f,
                false,
                -90f,
                0f,
                0f,
                1f,
                0f,
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

                var path = GetPrefabPath(spec);
                var material = spec.UsesCustomMaterial ? CreateOrUpdateStageMaterial(spec) : null;
                BuildOrUpdateStagePrefab(spec, sourcePrefab, material, path);
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

        [MenuItem("CheeseTama/초기 성장 크기 적용 (1~3단계)")]
        public static void ApplyEarlyGrowthStageSizes()
        {
            if (EditorApplication.isCompiling || Application.isPlaying)
            {
                throw new InvalidOperationException("Apply growth sizes after compilation and outside Play Mode.");
            }

            if (!GeneratedAssetsExist())
            {
                throw new InvalidOperationException("Complete growth assets are required before applying the size-only migration.");
            }

            foreach (var spec in StageSpecs)
            {
                if (spec.Stage != CheeseTamaGrowthStage.Egg &&
                    spec.Stage != CheeseTamaGrowthStage.Hatchling &&
                    spec.Stage != CheeseTamaGrowthStage.Soft)
                {
                    continue;
                }

                var materialPath = GetMaterialPath(spec);
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    throw new InvalidOperationException($"Growth stage material is missing: {materialPath}");
                }

                BuildOrUpdateStagePrefab(
                    spec,
                    null,
                    material,
                    GetPrefabPath(spec),
                    resizeExistingModelOnly: true);
            }

            // SaveAsPrefabAsset writes only each changed compatibility prefab.
            // Source imports, later stages, thumbnails and saved scenes are untouched.
            EditorPrefs.SetInt(BuilderVersionKey, BuilderVersion);
            Debug.Log("Initial growth prefab heights applied: Egg 0.42m, Hatchling 0.52m, Soft 0.62m.");
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

            var completedVersion = EditorPrefs.GetInt(BuilderVersionKey, 0);
            var generatedAssetsExist = GeneratedAssetsExist();
            if (completedVersion >= BuilderVersion && generatedAssetsExist)
            {
                return;
            }

            // Existing projects apply the visual-size migration explicitly, even
            // when editor preferences were reset or the project moved to a new PC.
            // An assembly reload must not rewrite their prefabs before that call.
            if (generatedAssetsExist)
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
                    AssetDatabase.LoadAssetAtPath<Texture2D>(spec.TexturePath) == null ||
                    AssetDatabase.LoadAssetAtPath<Texture2D>(spec.ConditionMaskPath) == null ||
                    AssetDatabase.LoadAssetAtPath<Texture2D>(spec.ThumbnailSourcePath) == null ||
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
                ConfigureTextureImporter(spec.TexturePath, spec.TextureMaxSize, true);
                ConfigureTextureImporter(spec.ConditionMaskPath, spec.TextureMaxSize, false);
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

        private static void ConfigureTextureImporter(string assetPath, int maxTextureSize, bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Growth stage texture importer is missing: {assetPath}");
            }

            var changed = false;
            changed |= SetIfDifferent(importer.textureType, TextureImporterType.Default, value => importer.textureType = value);
            changed |= SetIfDifferent(importer.sRGBTexture, sRgb, value => importer.sRGBTexture = value);
            changed |= SetIfDifferent(importer.alphaSource, TextureImporterAlphaSource.None, value => importer.alphaSource = value);
            changed |= SetIfDifferent(importer.alphaIsTransparency, false, value => importer.alphaIsTransparency = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, true, value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(importer.filterMode, FilterMode.Bilinear, value => importer.filterMode = value);
            changed |= SetIfDifferent(importer.maxTextureSize, maxTextureSize, value => importer.maxTextureSize = value);
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

            var conditionMask = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.ConditionMaskPath);
            if (conditionMask == null)
            {
                throw new InvalidOperationException($"Growth stage condition mask is missing: {spec.ConditionMaskPath}");
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
            material.SetTexture("_ConditionMask", conditionMask);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_PaletteHue", spec.PaletteHue);
            material.SetFloat("_PaletteSaturation", spec.PaletteSaturation);
            material.SetFloat("_PaletteValueScale", spec.PaletteValueScale);
            material.SetFloat("_PaletteValueOffset", spec.PaletteValueOffset);
            material.SetFloat("_PaletteStrength", 0f);
            material.SetFloat("_PaletteEmission", 0f);
            material.SetFloat("_EraseBlush", 0f);
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

        private static void BuildOrUpdateStagePrefab(
            StageSpec spec,
            GameObject sourcePrefab,
            Material material,
            string prefabPath,
            bool resizeExistingModelOnly = false)
        {
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab == null)
            {
                throw new InvalidOperationException(
                    $"Growth stage compatibility prefab is missing: {prefabPath}");
            }

            var assetGuid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    existingPrefab,
                    out var rootGuid,
                    out long rootGameObjectFileId) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    existingPrefab.transform,
                    out var transformGuid,
                    out long rootTransformFileId) ||
                string.IsNullOrEmpty(assetGuid) ||
                rootGuid != assetGuid ||
                transformGuid != assetGuid)
            {
                throw new InvalidOperationException(
                    $"Unable to capture compatibility identity for {prefabPath}");
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform previousStageModel = null;
                var stageModelCount = 0;
                for (var childIndex = 0; childIndex < root.transform.childCount; childIndex++)
                {
                    var child = root.transform.GetChild(childIndex);
                    if (child.name == "StageModel")
                    {
                        previousStageModel = child;
                        stageModelCount++;
                    }
                }
                if (stageModelCount != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected exactly one direct StageModel child in {prefabPath}, found {stageModelCount}.");
                }

                GameObject model;
                if (resizeExistingModelOnly)
                {
                    model = previousStageModel.gameObject;
                    var currentBounds = CalculateBounds(model);
                    const float sizeTolerance = 0.0001f;
                    if (Mathf.Abs(currentBounds.size.y - spec.TargetHeight) <= sizeTolerance &&
                        Mathf.Abs(currentBounds.min.y - CharacterGroundY) <= sizeTolerance &&
                        Mathf.Abs(currentBounds.center.x) <= sizeTolerance &&
                        Mathf.Abs(currentBounds.center.z) <= sizeTolerance)
                    {
                        ValidateStageModel(model, material, spec);
                        return;
                    }
                }
                else
                {
                    model = PrefabUtility.InstantiatePrefab(sourcePrefab, root.scene) as GameObject;
                    if (model == null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to instantiate growth source model in prefab contents: {spec.SourcePrefabPath}");
                    }

                    model.name = "StageModel__Replacement";
                    model.transform.SetParent(root.transform, false);
                    model.transform.SetSiblingIndex(previousStageModel.GetSiblingIndex());
                    if (spec.ResetSourceRotation)
                    {
                        model.transform.localRotation = Quaternion.identity;
                    }
                    else if (!Mathf.Approximately(spec.WorldYawOffset, 0f))
                    {
                        model.transform.localRotation =
                            Quaternion.Euler(0f, spec.WorldYawOffset, 0f) * model.transform.localRotation;
                    }

                    if (material != null)
                    {
                        AssignMaterial(model, material);
                    }
                }

                NormalizeModel(model, spec.TargetHeight);
                ValidateStageModel(model, material, spec);

                if (!resizeExistingModelOnly)
                {
                    Object.DestroyImmediate(previousStageModel.gameObject);
                    model.name = "StageModel";
                }
                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var success);
                if (!success || savedPrefab == null)
                {
                    throw new InvalidOperationException($"Failed to save growth stage prefab: {prefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.ImportAsset(
                prefabPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (reloadedPrefab == null ||
                AssetDatabase.AssetPathToGUID(prefabPath) != assetGuid ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    reloadedPrefab,
                    out var reloadedRootGuid,
                    out long reloadedRootGameObjectFileId) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    reloadedPrefab.transform,
                    out var reloadedTransformGuid,
                    out long reloadedRootTransformFileId) ||
                reloadedRootGuid != assetGuid ||
                reloadedTransformGuid != assetGuid ||
                reloadedRootGameObjectFileId != rootGameObjectFileId ||
                reloadedRootTransformFileId != rootTransformFileId)
            {
                throw new InvalidOperationException(
                    $"Growth stage compatibility identity changed while updating {prefabPath}");
            }
        }

        private static void ValidateStageModel(GameObject model, Material material, StageSpec spec)
        {
            const float boundsTolerance = 0.002f;
            var meshFilters = model.GetComponentsInChildren<MeshFilter>(true);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (meshFilters.Length != 1 || meshFilters[0].sharedMesh == null || renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{spec.PrefabName} must contain exactly one renderable mesh.");
            }

            var assignedMaterials = renderers[0].sharedMaterials;
            if (assignedMaterials.Length != 1 || assignedMaterials[0] != material)
            {
                throw new InvalidOperationException(
                    $"{spec.PrefabName} must use exactly one generated stage material.");
            }

            var bounds = CalculateBounds(model);
            if (!float.IsFinite(bounds.size.y) ||
                !float.IsFinite(bounds.min.y) ||
                !float.IsFinite(bounds.center.x) ||
                !float.IsFinite(bounds.center.z) ||
                Mathf.Abs(bounds.size.y - spec.TargetHeight) > boundsTolerance ||
                Mathf.Abs(bounds.min.y - CharacterGroundY) > boundsTolerance ||
                Mathf.Abs(bounds.center.x) > boundsTolerance ||
                Mathf.Abs(bounds.center.z) > boundsTolerance)
            {
                throw new InvalidOperationException(
                    $"{spec.PrefabName} bounds violate the physical-height or ground-alignment contract: {bounds}");
            }
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

            var sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.ThumbnailSourcePath);
            if (sourceTexture == null)
            {
                throw new InvalidOperationException(
                    $"Growth stage thumbnail source is missing: {spec.ThumbnailSourcePath}");
            }

            var thumbnailPath = GetThumbnailPath(spec);
            File.Copy(
                Path.GetFullPath(spec.ThumbnailSourcePath),
                Path.GetFullPath(thumbnailPath),
                true);
            AssetDatabase.ImportAsset(
                thumbnailPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(thumbnailPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"Growth stage thumbnail importer is missing: {thumbnailPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(thumbnailPath);
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
                string thumbnailSourcePath,
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
                ThumbnailSourcePath = thumbnailSourcePath;
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
            public string ConditionMaskPath => TexturePath.Replace("_Atlas.png", "_ConditionMask.png");
            public string ThumbnailSourcePath { get; }
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
