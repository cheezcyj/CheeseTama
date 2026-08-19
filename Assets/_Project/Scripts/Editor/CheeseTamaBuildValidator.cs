using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;

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

        public static BuildReport BuildWindowsPlayer(bool development, string outputPath)
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

            var buildOptions = development ? BuildOptions.Development : BuildOptions.CleanBuildCache;
            if (!development)
            {
                ValidateReleaseBuildConfiguration(scenes, buildOptions);
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

            Debug.Log($"CheeseTama Windows {(development ? "개발" : "릴리스")} 빌드 완료: {report.summary.outputPath}");
            return report;
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

        public static void BuildDevelopmentFromCommandLine()
        {
            BuildWindowsPlayer(true, ResolveCommandLineOutput());
        }

        public static void BuildReleaseFromCommandLine()
        {
            BuildWindowsPlayer(false, ResolveCommandLineOutput());
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
