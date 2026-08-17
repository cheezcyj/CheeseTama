using System;
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
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("빌드에 포함된 씬이 없습니다.");
            }

            outputPath = string.IsNullOrWhiteSpace(outputPath)
                ? Path.Combine(DefaultBuildFolder, development ? "CheeseTama_Development.exe" : "CheeseTama.exe")
                : outputPath;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? DefaultBuildFolder);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"CheeseTama Windows 빌드 실패: {report.summary.result} / 오류 {report.summary.totalErrors}");
            }

            Debug.Log($"CheeseTama Windows {(development ? "개발" : "릴리스")} 빌드 완료: {report.summary.outputPath}");
            return report;
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
}
