using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheeseTama.Editor
{
    public static class CheeseTamaBuildValidator
    {
        private const string DefaultBuildFolder = "Builds/Windows";
        public const string DebugScenePath = "Assets/_Project/Scenes/Debug.unity";

        private const string DoNotShipLabel = "DoNotShip";
        public const string BurstDebugInformationSuffix = "_BurstDebugInformation_DoNotShip";
        private const BuildOptions DevelopmentOnlyOptions = BuildOptions.Development
            | BuildOptions.AllowDebugging
            | BuildOptions.ConnectWithProfiler
            | BuildOptions.EnableDeepProfilingSupport;

        private static string[] activeReleaseScenePaths;

        [MenuItem("CheeseTama/검증/Windows 개발 빌드")]
        public static void BuildDevelopmentPlayer()
        {
            BuildWindowsPlayer(true, null);
        }

        [MenuItem("CheeseTama/검증/Windows 릴리스 빌드")]
        public static void BuildReleasePlayer()
        {
            BuildWindowsPlayer(false, null);
        }

        [MenuItem("CheeseTama/검증/Windows 릴리스 클린 빌드 (최종 검증)")]
        public static void BuildCleanReleasePlayer()
        {
            BuildWindowsPlayer(false, null, true);
        }

        public static BuildReport BuildWindowsPlayer(bool development, string outputPath)
        {
            return BuildWindowsPlayer(development, outputPath, false);
        }

        public static BuildReport BuildWindowsPlayer(
            bool development,
            string outputPath,
            bool cleanBuildCache)
        {
            var scenes = ResolveScenePathsForBuild(development, EditorBuildSettings.scenes);
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("빌드에 포함된 씬이 없습니다.");
            }

            outputPath = string.IsNullOrWhiteSpace(outputPath)
                ? Path.Combine(DefaultBuildFolder, development ? "CheeseTama_Development.exe" : "CheeseTama.exe")
                : outputPath;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? DefaultBuildFolder);

            var buildOptions = ResolveBuildOptions(development, cleanBuildCache);
            if (!development)
            {
                ValidateReleaseBuildConfiguration(scenes, buildOptions);
                ValidateReleaseSceneScripts(scenes);
                ValidateReleaseDependencies(scenes);
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = buildOptions
            };

            BuildReport report;
            try
            {
                SetActiveReleaseScenePaths(development ? null : scenes);
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                SetActiveReleaseScenePaths(null);
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"CheeseTama Windows 빌드 실패: {report.summary.result} / 오류 {report.summary.totalErrors}");
            }

            if (!development)
            {
                FinalizeReleaseOutput(report.summary.outputPath);
            }

            var buildKind = development
                ? "개발"
                : cleanBuildCache ? "릴리스 클린" : "릴리스 증분";
            Debug.Log($"CheeseTama Windows {buildKind} 빌드 완료: {report.summary.outputPath}");
            return report;
        }

        public static BuildOptions ResolveBuildOptions(bool development, bool cleanBuildCache)
        {
            var options = development ? BuildOptions.Development : BuildOptions.None;
            if (cleanBuildCache)
            {
                options |= BuildOptions.CleanBuildCache;
            }

            return options;
        }

        public static void FinalizeReleaseOutput(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new BuildFailedException("RC 빌드 출력 경로가 비어 있습니다.");
            }

            var fullOutputPath = Path.GetFullPath(outputPath);
            if (!File.Exists(fullOutputPath))
            {
                throw new BuildFailedException($"RC 실행 파일을 찾을 수 없습니다: {outputPath}");
            }

            var outputDirectory = Path.GetDirectoryName(fullOutputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new BuildFailedException("RC 빌드 출력 폴더를 확인할 수 없습니다.");
            }

            var burstSidecarName = Path.GetFileNameWithoutExtension(fullOutputPath)
                + BurstDebugInformationSuffix;
            var burstSidecarPath = Path.GetFullPath(Path.Combine(outputDirectory, burstSidecarName));
            var expectedPrefix = outputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!burstSidecarPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    Path.GetFileName(burstSidecarPath),
                    burstSidecarName,
                    StringComparison.Ordinal))
            {
                throw new BuildFailedException("Burst DoNotShip 정리 경로가 RC 출력 폴더를 벗어났습니다.");
            }

            if (Directory.Exists(burstSidecarPath))
            {
                Directory.Delete(burstSidecarPath, true);
            }

            var blockedOutputs = Directory.EnumerateFileSystemEntries(
                    outputDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .Where(IsDoNotShipOutputPath)
                .Take(10)
                .ToArray();
            if (blockedOutputs.Length > 0)
            {
                throw new BuildFailedException(
                    "RC 산출물에 DoNotShip 파일이 남아 있습니다: "
                    + string.Join(", ", blockedOutputs.Select(Path.GetFileName)));
            }
        }

        public static bool IsDoNotShipOutputPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var normalized = path.Replace('\\', '/');
            return normalized.IndexOf("/DoNotShip/", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf(BurstDebugInformationSuffix, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string[] ResolveScenePathsForBuild(
            bool development,
            IEnumerable<EditorBuildSettingsScene> configuredScenes)
        {
            if (configuredScenes == null)
            {
                return Array.Empty<string>();
            }

            return configuredScenes
                .Where(scene => scene != null && scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path.Replace('\\', '/'))
                .Where(path => development || (!IsDebugScene(path) && !IsDoNotShipAsset(path)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static void ValidateReleaseBuildConfiguration(
            IEnumerable<string> scenePaths,
            BuildOptions options)
        {
            if ((options & DevelopmentOnlyOptions) != 0)
            {
                throw new BuildFailedException(
                    "RC 빌드는 Development/디버거/Profiler/Deep Profiling 옵션을 사용할 수 없습니다.");
            }

            var scenes = scenePaths?.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray()
                ?? Array.Empty<string>();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("RC 빌드에 포함할 씬이 없습니다.");
            }

            var blockedScenes = scenes
                .Where(path => IsDebugScene(path) || IsDoNotShipAsset(path))
                .ToArray();
            if (blockedScenes.Length > 0)
            {
                throw new BuildFailedException(
                    $"RC 빌드에 개발 전용 씬 또는 DoNotShip 씬이 포함되었습니다: {string.Join(", ", blockedScenes)}");
            }
        }

        public static string[] FindMissingScriptSceneObjects(IEnumerable<string> scenePaths)
        {
            var issues = new List<string>();
            var normalizedPaths = scenePaths?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace('\\', '/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? Array.Empty<string>();

            foreach (var path in normalizedPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    issues.Add($"{path} (씬 파일 없음)");
                    continue;
                }

                var scene = SceneManager.GetSceneByPath(path);
                var openedForValidation = !scene.IsValid() || !scene.isLoaded;
                if (openedForValidation)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }

                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        {
                            var missingCount =
                                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                                    child.gameObject);
                            if (missingCount <= 0)
                            {
                                continue;
                            }

                            issues.Add(
                                $"{path}:{GetHierarchyPath(child)} (누락 {missingCount})");
                        }
                    }
                }
                finally
                {
                    if (openedForValidation && scene.IsValid() && scene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            return issues.ToArray();
        }

        public static void ValidateReleaseSceneScripts(IEnumerable<string> scenePaths)
        {
            var issues = FindMissingScriptSceneObjects(scenePaths);
            if (issues.Length > 0)
            {
                throw new BuildFailedException(
                    "RC 씬에 누락되거나 유효하지 않은 MonoBehaviour 스크립트가 있습니다: "
                    + string.Join(", ", issues));
            }
        }

        public static bool IsDebugScene(string assetPath)
        {
            return string.Equals(
                assetPath?.Replace('\\', '/'),
                DebugScenePath,
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDoNotShipPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            var normalized = $"/{assetPath.Replace('\\', '/').Trim('/')}/";
            return normalized.IndexOf("/DoNotShip/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static void ValidatePreprocessedReleaseBuild(BuildReport report)
        {
            if (report == null || (report.summary.options & BuildOptions.Development) != 0)
            {
                return;
            }

            // Our build entry point records its exact BuildPlayerOptions scene list. An external
            // release build must prove every enabled Build Settings scene is safe instead.
            var scenes = activeReleaseScenePaths ?? EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            ValidateReleaseBuildConfiguration(scenes, report.summary.options);
            ValidateReleaseSceneScripts(scenes);
            ValidateReleaseDependencies(scenes);
        }

        internal static void SetActiveReleaseScenePaths(IEnumerable<string> scenePaths)
        {
            activeReleaseScenePaths = scenePaths?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace('\\', '/'))
                .ToArray();
        }

        private static bool IsDoNotShipAsset(string assetPath)
        {
            if (IsDoNotShipPath(assetPath))
            {
                return true;
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return asset != null && AssetDatabase.GetLabels(asset)
                .Any(label => string.Equals(label, DoNotShipLabel, StringComparison.OrdinalIgnoreCase));
        }

        private static void ValidateReleaseDependencies(IEnumerable<string> scenePaths)
        {
            var paths = scenePaths.ToArray();
            var blockedDependencies = AssetDatabase.GetDependencies(paths, true)
                .Where(IsDoNotShipAsset)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (blockedDependencies.Length > 0)
            {
                throw new BuildFailedException(
                    $"RC 빌드가 DoNotShip 자산을 참조합니다: {string.Join(", ", blockedDependencies)}");
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
            }

            return string.Join("/", names);
        }

        public static void BuildDevelopmentFromCommandLine()
        {
            BuildWindowsPlayer(true, ResolveCommandLineOutput());
        }

        public static void BuildReleaseFromCommandLine()
        {
            BuildWindowsPlayer(false, ResolveCommandLineOutput());
        }

        public static void BuildCleanReleaseFromCommandLine()
        {
            BuildWindowsPlayer(false, ResolveCommandLineOutput(), true);
        }

        private static string ResolveCommandLineOutput()
        {
            var arguments = System.Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index += 1)
            {
                if (string.Equals(arguments[index], "-buildOutput", StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }

    public sealed class CheeseTamaReleaseBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            CheeseTamaBuildValidator.ValidatePreprocessedReleaseBuild(report);
        }
    }
}
