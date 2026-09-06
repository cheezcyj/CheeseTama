using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CheeseTama.Editor
{
    public static class CheeseTamaWebGlBuildValidator
    {
        public const string DefaultDevelopmentBuildFolder = "Builds/WebGL/Development";
        public const string DefaultReleaseBuildFolder = "Builds/WebGL/Release";
        public const string KoreanFontAssetPath =
            "Assets/_Project/Resources/Fonts/NanumGothic-Regular.ttf";
        public const string ReleaseTemplateName = "PROJECT:CheeseTama";
        public const string ReleaseTemplateAssetFolder =
            "Assets/WebGLTemplates/CheeseTama";
        public const string PublicBasePath = "/play/";
        public const string FaviconRelativePath =
            "TemplateData/cheesetama-game-start-cheese-mark-v003.svg";
        public const string PreviousFaviconRelativePath =
            "TemplateData/cheesetama-tiny-cheese-life-mark-v002.svg";
        public const string EarlierFaviconRelativePath =
            "TemplateData/cheesetama-tiny-cheese-life-mark-v001.svg";
        public const string SupersededFaviconRelativePath =
            "TemplateData/cheesetama-start-symbol-v001.svg";

        private const string IndexFileName = "index.html";
        private const string BuildFolderName = "Build";
        private const string NanumGothicLicenseRelativePath =
            "_Project/ThirdParty/Fonts/NanumGothic/OFL.txt";
        private const string NanumGothicNoticeRelativePath =
            "ThirdPartyNotices/NanumGothic-OFL.txt";
        private const string UnityGzipFallbackMarker =
            "UnityWeb Compressed Content (gzip)";
        private static readonly string[] PerformanceTestReleaseMetadataFileNames =
        {
            "PerformanceTestRunInfo.json",
            "PerformanceTestRunSettings.json"
        };
        private static readonly string[] InactiveFaviconRelativePaths =
        {
            PreviousFaviconRelativePath,
            EarlierFaviconRelativePath,
            SupersededFaviconRelativePath
        };
        private static readonly string[] KnownToolchainAbsolutePathPrefixes =
        {
            "C:" + "/Program Files/Unity/Hub/Editor/",
            "C:" + @"\dev\dots\"
        };

        [MenuItem("CheeseTama/검증/WebGL 개발 빌드")]
        public static void BuildDevelopmentPlayer()
        {
            BuildWebGlPlayer(true, null);
        }

        [MenuItem("CheeseTama/검증/WebGL 릴리스 빌드")]
        public static void BuildReleasePlayer()
        {
            BuildWebGlPlayer(false, null);
        }

        [MenuItem("CheeseTama/검증/WebGL 릴리스 클린 빌드 (최종 검증)")]
        public static void BuildCleanReleasePlayer()
        {
            BuildWebGlPlayer(false, null, true);
        }

        public static BuildReport BuildWebGlPlayer(bool development, string outputDirectory)
        {
            return BuildWebGlPlayer(development, outputDirectory, false);
        }

        public static BuildReport BuildWebGlPlayer(
            bool development,
            string outputDirectory,
            bool cleanBuildCache)
        {
            if (!IsWebGlBuildSupportInstalled())
            {
                throw new BuildFailedException(
                    "WebGL Build Support 모듈이 설치되어 있지 않습니다. "
                    + "Unity Hub에서 현재 에디터 버전에 WebGL Build Support를 추가하세요.");
            }

            ValidateKoreanFontAsset();

            var scenes = CheeseTamaBuildValidator.ResolveScenePathsForBuild(
                development,
                EditorBuildSettings.scenes);
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("WebGL 빌드에 포함된 씬이 없습니다.");
            }

            var buildOptions = CheeseTamaBuildValidator.ResolveBuildOptions(
                development,
                cleanBuildCache);
            if (!development)
            {
                CheeseTamaBuildValidator.ValidateReleaseBuildConfiguration(scenes, buildOptions);
                CheeseTamaBuildValidator.ValidateReleaseSceneScripts(scenes);
                ValidateReleaseTemplateAssets();
            }

            outputDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                ? (development ? DefaultDevelopmentBuildFolder : DefaultReleaseBuildFolder)
                : outputDirectory;
            var fullOutputDirectory = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(fullOutputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullOutputDirectory,
                target = BuildTarget.WebGL,
                options = buildOptions
            };

            var hostingRequirements = development
                ? DescribeCurrentHostingRequirements()
                : DescribeHostingRequirements(
                    WebGLCompressionFormat.Gzip,
                    true,
                    false);
            IDisposable releaseSettings = null;
            BuildReport report;
            try
            {
                if (!development)
                {
                    releaseSettings = ApplyPortableReleaseSettingsTemporarily();
                }

                CheeseTamaBuildValidator.SetActiveReleaseScenePaths(development ? null : scenes);
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                CheeseTamaBuildValidator.SetActiveReleaseScenePaths(null);
                releaseSettings?.Dispose();
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"CheeseTama WebGL 빌드 실패: {report.summary.result} / 오류 {report.summary.totalErrors}");
            }

            // The WebGL postprocessor owns license copying, path sanitization, and the
            // embedded host-path gate for every BuildPipeline entry point.
            ValidateWebGlOutput(fullOutputDirectory);
            if (!development)
            {
                ValidateReleaseShellOutput(fullOutputDirectory);
            }

            var buildKind = development
                ? "개발"
                : cleanBuildCache ? "릴리스 클린" : "릴리스 증분";
            Debug.Log(
                $"CheeseTama WebGL {buildKind} 빌드 완료: "
                + $"{report.summary.outputPath}\n{hostingRequirements} "
                + $"공개 경로는 {PublicBasePath}로 고정합니다.");
            return report;
        }

        public static bool IsWebGlBuildSupportInstalled()
        {
            return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        public static void ValidateKoreanFontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontAssetPath);
            if (font == null || !font.HasCharacter('한'))
            {
                throw new BuildFailedException(
                    $"WebGL 한국어 UI 글꼴이 없거나 한국어 글리프를 포함하지 않습니다: "
                    + KoreanFontAssetPath);
            }
        }

        public static void ValidateReleaseTemplateAssets()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new BuildFailedException("Unity 프로젝트 루트 경로를 확인할 수 없습니다.");
            }

            var templateDirectory = Path.Combine(
                projectRoot,
                ReleaseTemplateAssetFolder.Replace('/', Path.DirectorySeparatorChar));
            var requiredFiles = new[]
            {
                IndexFileName,
                "TemplateData/style.css",
                FaviconRelativePath
            };
            var missingFiles = requiredFiles
                .Where(relativePath => !File.Exists(Path.Combine(
                    templateDirectory,
                    relativePath.Replace('/', Path.DirectorySeparatorChar))))
                .ToArray();
            if (missingFiles.Length > 0)
            {
                throw new BuildFailedException(
                    "CheeseTama WebGL 릴리스 템플릿이 불완전합니다: "
                    + string.Join(", ", missingFiles));
            }

            var supersededFaviconPath = InactiveFaviconRelativePaths
                .Select(relativePath => Path.Combine(
                    templateDirectory,
                    relativePath.Replace('/', Path.DirectorySeparatorChar)))
                .FirstOrDefault(File.Exists);
            if (!string.IsNullOrEmpty(supersededFaviconPath))
            {
                throw new BuildFailedException(
                    $"교체 전 CheeseTama WebGL 파비콘이 남아 있습니다: {supersededFaviconPath}");
            }

            var templateIndex = File.ReadAllText(Path.Combine(templateDirectory, IndexFileName));
            var issues = FindReleaseShellIssues(templateIndex, allowUnityTemplateDirectives: true);
            if (issues.Length > 0)
            {
                throw new BuildFailedException(
                    "CheeseTama WebGL 릴리스 템플릿 계약을 충족하지 못했습니다: "
                    + string.Join(", ", issues));
            }

            var faviconPath = Path.Combine(
                templateDirectory,
                FaviconRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var faviconContents = File.ReadAllText(faviconPath);
            var faviconIssues = FindFaviconSvgIssues(faviconContents)
                .Concat(FindBrandFaviconIssues(faviconContents))
                .Distinct()
                .ToArray();
            if (faviconIssues.Length > 0)
            {
                throw new BuildFailedException(
                    "CheeseTama WebGL SVG 파비콘 계약을 충족하지 못했습니다: "
                    + string.Join(", ", faviconIssues));
            }
        }

        public static string CopyNanumGothicLicenseToOutput(
            string assetsDirectory,
            string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(assetsDirectory))
            {
                throw new ArgumentException("Assets 경로가 비어 있습니다.", nameof(assetsDirectory));
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("출력 경로가 비어 있습니다.", nameof(outputDirectory));
            }

            var sourcePath = Path.Combine(
                Path.GetFullPath(assetsDirectory),
                NanumGothicLicenseRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sourcePath))
            {
                throw new BuildFailedException(
                    $"Nanum Gothic 라이선스 원문을 찾을 수 없습니다: {sourcePath}");
            }

            var destinationPath = Path.Combine(
                Path.GetFullPath(outputDirectory),
                NanumGothicNoticeRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new BuildFailedException("Nanum Gothic 라이선스 출력 경로가 유효하지 않습니다.");
            }

            Directory.CreateDirectory(destinationDirectory);
            File.Copy(sourcePath, destinationPath, true);
            return destinationPath;
        }

        public static int SanitizeEmbeddedToolchainAbsolutePaths(string outputDirectory)
        {
            return ProcessEmbeddedArtifactPaths(
                    outputDirectory,
                    sanitizeKnownPrefixes: true,
                    validateHostPaths: false,
                    requireBuildDirectory: true)
                .SanitizedCount;
        }

        public static int SanitizeAndValidateEmbeddedHostPaths(string outputDirectory)
        {
            var result = ProcessEmbeddedArtifactPaths(
                outputDirectory,
                sanitizeKnownPrefixes: true,
                validateHostPaths: true,
                requireBuildDirectory: true);
            ThrowIfEmbeddedHostPathsRemain(result.Issues);
            return result.SanitizedCount;
        }

        private static int ReplaceToolchainPathPrefixes(byte[] payload)
        {
            var sanitizedCount = 0;
            foreach (var prefix in ResolveToolchainAbsolutePathPrefixes())
            {
                for (var offset = 0; offset <= payload.Length - prefix.Length; offset++)
                {
                    var matches = true;
                    for (var index = 0; index < prefix.Length; index++)
                    {
                        if (payload[offset + index] == prefix[index])
                        {
                            continue;
                        }

                        matches = false;
                        break;
                    }

                    if (!matches)
                    {
                        continue;
                    }

                    for (var index = 0; index < prefix.Length; index++)
                    {
                        payload[offset + index] = (byte)'_';
                    }

                    sanitizedCount++;
                    offset += prefix.Length - 1;
                }
            }

            return sanitizedCount;
        }

        private static byte[][] ResolveToolchainAbsolutePathPrefixes()
        {
            var pathPrefixes = new HashSet<string>(
                KnownToolchainAbsolutePathPrefixes,
                StringComparer.Ordinal);
            AddAbsolutePathPrefixVariants(
                pathPrefixes,
                Directory.GetParent(Application.dataPath)?.FullName);
            AddAbsolutePathPrefixVariants(
                pathPrefixes,
                Path.GetDirectoryName(EditorApplication.applicationPath));
            AddAbsolutePathPrefixVariants(
                pathPrefixes,
                System.Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"));

            return pathPrefixes
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .OrderByDescending(path => path.Length)
                .Select(Encoding.UTF8.GetBytes)
                .ToArray();
        }

        private static void AddAbsolutePathPrefixVariants(
            ISet<string> pathPrefixes,
            string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath)
                || !Path.IsPathRooted(absolutePath))
            {
                return;
            }

            var trimmedPath = absolutePath.TrimEnd('/', '\\');
            if (trimmedPath.Length == 0)
            {
                return;
            }

            pathPrefixes.Add(trimmedPath.Replace('\\', '/') + "/");
            pathPrefixes.Add(trimmedPath.Replace('/', '\\') + "\\");
        }

        private static byte[] DecompressGzip(byte[] encodedBytes)
        {
            using var input = new MemoryStream(encodedBytes, false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] CompressGzip(byte[] payload)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(
                       output,
                       System.IO.Compression.CompressionLevel.Optimal,
                       true))
            {
                gzip.Write(payload, 0, payload.Length);
            }

            var compressed = output.ToArray();
            if (compressed.Length < 10
                || compressed[0] != 0x1f
                || compressed[1] != 0x8b)
            {
                throw new InvalidDataException("생성된 WebGL gzip 헤더가 올바르지 않습니다.");
            }

            // Unity's decompression fallback detects this gzip comment when a
            // static host cannot provide Content-Encoding. GZipStream does not
            // preserve the original comment, so re-add it after sanitization.
            var marker = Encoding.ASCII.GetBytes(UnityGzipFallbackMarker);
            var marked = new byte[compressed.Length + marker.Length + 1];
            Buffer.BlockCopy(compressed, 0, marked, 0, 10);
            marked[3] |= 0x10; // FCOMMENT
            Buffer.BlockCopy(marker, 0, marked, 10, marker.Length);
            marked[10 + marker.Length] = 0;
            Buffer.BlockCopy(
                compressed,
                10,
                marked,
                11 + marker.Length,
                compressed.Length - 10);
            return marked;
        }

        public static int RemovePerformanceTestMetadataFromReleaseContent(string assetsDirectory)
        {
            if (string.IsNullOrWhiteSpace(assetsDirectory))
            {
                throw new ArgumentException("Assets 경로가 비어 있습니다.", nameof(assetsDirectory));
            }

            var resourcesDirectory = Path.Combine(
                Path.GetFullPath(assetsDirectory),
                "Resources");
            var removedFileCount = 0;
            foreach (var fileName in PerformanceTestReleaseMetadataFileNames)
            {
                var assetPath = Path.Combine(resourcesDirectory, fileName);
                removedFileCount += DeleteFileIfPresent(assetPath);
                removedFileCount += DeleteFileIfPresent(assetPath + ".meta");
            }

            return removedFileCount;
        }

        private static int DeleteFileIfPresent(string path)
        {
            if (!File.Exists(path))
            {
                return 0;
            }

            File.Delete(path);
            return 1;
        }

        public static void ValidateWebGlOutput(string outputDirectory)
        {
            var missingArtifacts = FindMissingWebGlArtifacts(outputDirectory);
            if (missingArtifacts.Length > 0)
            {
                throw new BuildFailedException(
                    "WebGL 산출물이 불완전합니다: " + string.Join(", ", missingArtifacts));
            }

            var blockedOutputs = Directory.EnumerateFileSystemEntries(
                    Path.GetFullPath(outputDirectory),
                    "*",
                    SearchOption.AllDirectories)
                .Where(CheeseTamaBuildValidator.IsDoNotShipOutputPath)
                .Take(10)
                .ToArray();
            if (blockedOutputs.Length > 0)
            {
                throw new BuildFailedException(
                    "WebGL 산출물에 DoNotShip 파일이 남아 있습니다: "
                    + string.Join(", ", blockedOutputs.Select(Path.GetFileName)));
            }
        }

        public static void ValidateNoEmbeddedHostPaths(string outputDirectory)
        {
            var embeddedPathIssues = FindEmbeddedHostPathIssues(outputDirectory);
            ThrowIfEmbeddedHostPathsRemain(embeddedPathIssues);
        }

        public static string[] FindEmbeddedHostPathIssues(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return new[] { "출력 폴더" };
            }

            return ProcessEmbeddedArtifactPaths(
                    outputDirectory,
                    sanitizeKnownPrefixes: false,
                    validateHostPaths: true,
                    requireBuildDirectory: false)
                .Issues;
        }

        private static EmbeddedArtifactPathResult ProcessEmbeddedArtifactPaths(
            string outputDirectory,
            bool sanitizeKnownPrefixes,
            bool validateHostPaths,
            bool requireBuildDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("출력 경로가 비어 있습니다.", nameof(outputDirectory));
            }

            var buildDirectory = Path.Combine(
                Path.GetFullPath(outputDirectory),
                BuildFolderName);
            if (!Directory.Exists(buildDirectory))
            {
                if (requireBuildDirectory)
                {
                    throw new BuildFailedException(
                        $"WebGL Build 폴더를 찾을 수 없습니다: {buildDirectory}");
                }

                return new EmbeddedArtifactPathResult(0, Array.Empty<string>());
            }

            var sanitizedCount = 0;
            var issues = new List<string>();
            foreach (var filePath in Directory.EnumerateFiles(
                         buildDirectory,
                         "*.unityweb",
                         SearchOption.AllDirectories))
            {
                var encodedBytes = File.ReadAllBytes(filePath);
                var isGzip = encodedBytes.Length >= 2
                    && encodedBytes[0] == 0x1f
                    && encodedBytes[1] == 0x8b;
                byte[] payload;
                try
                {
                    payload = isGzip ? DecompressGzip(encodedBytes) : encodedBytes;
                }
                catch (InvalidDataException)
                {
                    if (!validateHostPaths)
                    {
                        throw;
                    }

                    issues.Add(Path.GetFileName(filePath) + " (손상된 gzip)");
                    continue;
                }

                var fileSanitizedCount = sanitizeKnownPrefixes
                    ? ReplaceToolchainPathPrefixes(payload)
                    : 0;
                if (validateHostPaths)
                {
                    AddEmbeddedHostPathIssues(issues, Path.GetFileName(filePath), payload);
                }

                if (fileSanitizedCount > 0)
                {
                    File.WriteAllBytes(
                        filePath,
                        isGzip ? CompressGzip(payload) : payload);
                    sanitizedCount += fileSanitizedCount;
                }
            }

            return new EmbeddedArtifactPathResult(
                sanitizedCount,
                issues.Distinct(StringComparer.Ordinal).ToArray());
        }

        private static void AddEmbeddedHostPathIssues(
            ICollection<string> issues,
            string fileName,
            byte[] payload)
        {
            if (ContainsWindowsAbsolutePath(payload))
            {
                issues.Add(fileName + " (Windows 절대 경로)");
            }

            if (ContainsAscii(payload, "/Users/"))
            {
                issues.Add(fileName + " (macOS 사용자 경로)");
            }

            if (ContainsLinuxUserHome(payload))
            {
                issues.Add(fileName + " (Linux 사용자 경로)");
            }
        }

        private static void ThrowIfEmbeddedHostPathsRemain(string[] embeddedPathIssues)
        {
            if (embeddedPathIssues.Length <= 0)
            {
                return;
            }

            throw new BuildFailedException(
                "WebGL 산출물에 게시할 수 없는 호스트 절대 경로가 남아 있습니다: "
                + string.Join(", ", embeddedPathIssues));
        }

        private sealed class EmbeddedArtifactPathResult
        {
            public EmbeddedArtifactPathResult(int sanitizedCount, string[] issues)
            {
                SanitizedCount = sanitizedCount;
                Issues = issues ?? Array.Empty<string>();
            }

            public int SanitizedCount { get; }

            public string[] Issues { get; }
        }

        private static bool ContainsWindowsAbsolutePath(byte[] payload)
        {
            if (payload == null || payload.Length < 3)
            {
                return false;
            }

            for (var index = 0; index <= payload.Length - 3; index++)
            {
                var drive = payload[index];
                var hasTokenBoundary = index == 0
                    || !IsAsciiPathWordCharacter(payload[index - 1]);
                if (hasTokenBoundary
                    && ((drive >= (byte)'A' && drive <= (byte)'Z')
                     || (drive >= (byte)'a' && drive <= (byte)'z'))
                    && payload[index + 1] == (byte)':'
                    && (payload[index + 2] == (byte)'/' || payload[index + 2] == (byte)'\\')
                    && HasPlausibleWindowsPathBody(payload, index + 3))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPlausibleWindowsPathBody(byte[] payload, int offset)
        {
            if (offset >= payload.Length
                || (!IsAsciiPathWordCharacter(payload[offset])
                    && payload[offset] != (byte)'.'
                    && payload[offset] != (byte)'$'
                    && payload[offset] != (byte)'-'))
            {
                return false;
            }

            var printablePathCharacters = 0;
            var containsNameCharacter = false;
            var containsNestedSeparator = false;
            for (var index = offset; index < payload.Length; index++)
            {
                var value = payload[index];
                if (value < 0x20
                    || value >= 0x7f
                    || value == (byte)'"'
                    || value == (byte)'\''
                    || value == (byte)'<'
                    || value == (byte)'>'
                    || value == (byte)'|'
                    || value == (byte)'?'
                    || value == (byte)'*'
                    || value == (byte)':')
                {
                    break;
                }

                printablePathCharacters++;
                containsNameCharacter |= IsAsciiPathWordCharacter(value);
                containsNestedSeparator |= index > offset
                    && (value == (byte)'/' || value == (byte)'\\');
                if (printablePathCharacters >= 3
                    && containsNameCharacter
                    && containsNestedSeparator)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAsciiPathWordCharacter(byte value)
        {
            return (value >= (byte)'A' && value <= (byte)'Z')
                || (value >= (byte)'a' && value <= (byte)'z')
                || (value >= (byte)'0' && value <= (byte)'9')
                || value == (byte)'_';
        }

        private static bool ContainsLinuxUserHome(byte[] payload)
        {
            var homePrefix = Encoding.ASCII.GetBytes("/home/");
            var allowedVirtualHome = Encoding.ASCII.GetBytes("/home/web_user");
            for (var offset = 0; offset <= payload.Length - homePrefix.Length; offset++)
            {
                if (!MatchesAt(payload, homePrefix, offset))
                {
                    continue;
                }

                if (!MatchesAt(payload, allowedVirtualHome, offset)
                    || !HasPathBoundaryAfter(payload, offset + allowedVirtualHome.Length))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPathBoundaryAfter(byte[] payload, int offset)
        {
            if (offset >= payload.Length)
            {
                return true;
            }

            var value = payload[offset];
            return value == 0
                || value == (byte)'/'
                || value == (byte)'"'
                || value == (byte)'\'';
        }

        private static bool ContainsAscii(byte[] payload, string value)
        {
            var pattern = Encoding.ASCII.GetBytes(value);
            for (var offset = 0; offset <= payload.Length - pattern.Length; offset++)
            {
                if (MatchesAt(payload, pattern, offset))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesAt(byte[] payload, byte[] pattern, int offset)
        {
            if (payload == null
                || pattern == null
                || offset < 0
                || offset > payload.Length - pattern.Length)
            {
                return false;
            }

            for (var index = 0; index < pattern.Length; index++)
            {
                if (payload[offset + index] != pattern[index])
                {
                    return false;
                }
            }

            return true;
        }

        public static void ValidateReleaseShellOutput(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("출력 경로가 비어 있습니다.", nameof(outputDirectory));
            }

            var indexPath = Path.Combine(Path.GetFullPath(outputDirectory), IndexFileName);
            if (!File.Exists(indexPath))
            {
                throw new BuildFailedException(
                    $"CheeseTama WebGL 릴리스 셸을 찾을 수 없습니다: {indexPath}");
            }

            var faviconPath = Path.Combine(
                Path.GetFullPath(outputDirectory),
                FaviconRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(faviconPath))
            {
                throw new BuildFailedException(
                    $"CheeseTama WebGL 파비콘을 찾을 수 없습니다: {faviconPath}");
            }

            var supersededFaviconPath = InactiveFaviconRelativePaths
                .Select(relativePath => Path.Combine(
                    Path.GetFullPath(outputDirectory),
                    relativePath.Replace('/', Path.DirectorySeparatorChar)))
                .FirstOrDefault(File.Exists);
            if (!string.IsNullOrEmpty(supersededFaviconPath))
            {
                throw new BuildFailedException(
                    $"교체 전 CheeseTama WebGL 파비콘이 남아 있습니다: {supersededFaviconPath}");
            }

            var faviconContents = File.ReadAllText(faviconPath);
            var faviconIssues = FindFaviconSvgIssues(faviconContents)
                .Concat(FindBrandFaviconIssues(faviconContents))
                .Distinct()
                .ToArray();
            if (faviconIssues.Length > 0)
            {
                throw new BuildFailedException(
                    "CheeseTama WebGL SVG 파비콘 계약을 충족하지 못했습니다: "
                    + string.Join(", ", faviconIssues));
            }

            var issues = FindReleaseShellIssues(
                File.ReadAllText(indexPath),
                allowUnityTemplateDirectives: false);
            if (issues.Length > 0)
            {
                throw new BuildFailedException(
                    "CheeseTama WebGL 릴리스 셸 계약을 충족하지 못했습니다: "
                    + string.Join(", ", issues));
            }
        }

        public static string[] FindFaviconSvgIssues(string svgContents)
        {
            if (string.IsNullOrWhiteSpace(svgContents))
            {
                return new[] { "SVG 내용" };
            }

            var document = new XmlDocument
            {
                XmlResolver = null
            };
            try
            {
                document.LoadXml(svgContents);
            }
            catch (XmlException)
            {
                return new[] { "유효한 SVG XML" };
            }

            var issues = new List<string>();
            var root = document.DocumentElement;
            if (root == null
                || !string.Equals(root.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("SVG 루트 요소");
                return issues.ToArray();
            }

            if (!string.Equals(root.GetAttribute("viewBox"), "0 0 64 64", StringComparison.Ordinal))
            {
                issues.Add("SVG viewBox 0 0 64 64");
            }

            var forbiddenElementNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "script",
                "image",
                "foreignObject",
                "use"
            };
            var nodes = document.SelectNodes("//*");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    if (forbiddenElementNames.Contains(node.LocalName))
                    {
                        issues.Add($"금지된 SVG 요소 <{node.LocalName}>");
                    }

                    if (node.Attributes == null)
                    {
                        continue;
                    }

                    foreach (XmlAttribute attribute in node.Attributes)
                    {
                        if (!string.Equals(attribute.LocalName, "href", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var href = attribute.Value?.Trim();
                        if (!string.IsNullOrEmpty(href)
                            && !href.StartsWith("#", StringComparison.Ordinal))
                        {
                            issues.Add("외부 SVG href");
                        }
                    }
                }
            }

            return issues.Distinct().ToArray();
        }

        public static string[] FindBrandFaviconIssues(string svgContents)
        {
            if (string.IsNullOrWhiteSpace(svgContents))
            {
                return new[] { "브랜드 SVG 내용" };
            }

            var issues = new List<string>();
            var requiredMarkers = new Dictionary<string, string>
            {
                {
                    "id=\"cheesetama-game-start-cheese-mark-v003\"",
                    "게임 시작 치즈 SVG ID"
                },
                {
                    "id=\"game-start-cheese-art\"",
                    "게임 시작 치즈 아트 그룹"
                },
                {
                    "id=\"game-start-cheese-piece\"",
                    "게임 시작 치즈 본체 그룹"
                },
                {
                    "transform=\"rotate(-8 32 32)\"",
                    "게임 시작 치즈 본체 회전"
                },
                {
                    "transform=\"matrix(1.35 0 0 1.35 -13.02 -9.81)\"",
                    "게임 시작 치즈 확대·중앙 정렬 변환"
                },
                {
                    "id=\"game-start-cheese-fill\" x1=\"15.44\" y1=\"8.34\" x2=\"48.56\" y2=\"55.66\" gradientUnits=\"userSpaceOnUse\"",
                    "게임 시작 치즈 145도 본체 색상"
                },
                {
                    "<stop offset=\"0\" stop-color=\"#fff2a2\"/>",
                    "게임 시작 치즈 본체 첫 색상"
                },
                {
                    "<stop offset=\"0.3\" stop-color=\"#fff2a2\"/>",
                    "게임 시작 치즈 본체 첫 색상 범위"
                },
                {
                    "<stop offset=\"0.31\" stop-color=\"#f4c95d\"/>",
                    "게임 시작 치즈 본체 둘째 색상 시작"
                },
                {
                    "<stop offset=\"0.72\" stop-color=\"#f4c95d\"/>",
                    "게임 시작 치즈 본체 둘째 색상 범위"
                },
                {
                    "<stop offset=\"0.73\" stop-color=\"#d8952e\"/>",
                    "게임 시작 치즈 본체 셋째 색상 시작"
                },
                {
                    "<stop offset=\"1\" stop-color=\"#d8952e\"/>",
                    "게임 시작 치즈 본체 셋째 색상 범위"
                },
                {
                    "id=\"game-start-cheese-shine\" x1=\"5.94\" y1=\"18.14\" x2=\"58.06\" y2=\"45.86\" gradientUnits=\"userSpaceOnUse\"",
                    "게임 시작 치즈 118도 면 광택"
                },
                {
                    "<stop offset=\"0\" stop-color=\"#ffffdd\" stop-opacity=\"0.58\"/>",
                    "게임 시작 치즈 광택 시작"
                },
                {
                    "<stop offset=\"0.34\" stop-color=\"#ffffdd\" stop-opacity=\"0\"/>",
                    "게임 시작 치즈 밝은 광택 범위"
                },
                {
                    "<stop offset=\"0.72\" stop-color=\"#784217\" stop-opacity=\"0\"/>",
                    "게임 시작 치즈 어두운 광택 시작"
                },
                {
                    "<stop offset=\"1\" stop-color=\"#784217\" stop-opacity=\"0.25\"/>",
                    "게임 시작 치즈 어두운 광택 범위"
                },
                {
                    "<path id=\"game-start-cheese-body\" d=\"M10.94 42.57 52.12 47.47 43.7 18.03 17.96 24.83Z\" fill=\"url(#game-start-cheese-fill)\"/>",
                    "게임 시작 치즈 본체 레이어·색상 결합"
                },
                {
                    "d=\"M10.94 42.57 52.12 47.47 43.7 18.03 17.96 24.83Z\"",
                    "게임 시작 치즈 본체 도형"
                },
                {
                    "id=\"game-start-cheese-hole-one\" cx=\"0.5\" cy=\"0.5\" r=\"0.5\"",
                    "게임 시작 치즈 첫 번째 구멍 페더"
                },
                {
                    "id=\"game-start-cheese-hole-two\" cx=\"0.5\" cy=\"0.5\" r=\"0.5\"",
                    "게임 시작 치즈 두 번째 구멍 페더"
                },
                {
                    "id=\"game-start-cheese-hole-three\" cx=\"0.5\" cy=\"0.5\" r=\"0.5\"",
                    "게임 시작 치즈 세 번째 구멍 페더"
                },
                {
                    "<circle cx=\"24.51\" cy=\"34.64\" r=\"3.01\" fill=\"url(#game-start-cheese-hole-one)\"/>",
                    "게임 시작 치즈 첫 번째 구멍 위치·크기"
                },
                {
                    "<circle cx=\"38.55\" cy=\"39.17\" r=\"2.78\" fill=\"url(#game-start-cheese-hole-two)\"/>",
                    "게임 시작 치즈 두 번째 구멍 위치·크기"
                },
                {
                    "<circle cx=\"35.74\" cy=\"26.72\" r=\"2.18\" fill=\"url(#game-start-cheese-hole-three)\"/>",
                    "게임 시작 치즈 세 번째 구멍 위치·크기"
                },
                {
                    "offset=\"87.5%\" stop-color=\"#bd7624\"",
                    "게임 시작 치즈 첫 번째 구멍 색상"
                },
                {
                    "<stop offset=\"100%\" stop-color=\"#bd7624\" stop-opacity=\"0\"/>",
                    "게임 시작 치즈 첫 번째 구멍 투명 가장자리"
                },
                {
                    "offset=\"85.7143%\" stop-color=\"#b66d21\"",
                    "게임 시작 치즈 두 번째 구멍 색상"
                },
                {
                    "<stop offset=\"100%\" stop-color=\"#b66d21\" stop-opacity=\"0\"/>",
                    "게임 시작 치즈 두 번째 구멍 투명 가장자리"
                },
                {
                    "offset=\"83.3333%\" stop-color=\"#c68128\"",
                    "게임 시작 치즈 세 번째 구멍 색상"
                },
                {
                    "<stop offset=\"100%\" stop-color=\"#c68128\" stop-opacity=\"0\"/>",
                    "게임 시작 치즈 세 번째 구멍 투명 가장자리"
                },
                {
                    "<path id=\"game-start-cheese-gloss\" d=\"M10.94 42.57 52.12 47.47 43.7 18.03 17.96 24.83Z\" fill=\"url(#game-start-cheese-shine)\"/>",
                    "게임 시작 치즈 광택 레이어·색상 결합"
                }
            };
            issues.AddRange(requiredMarkers
                .Where(pair => svgContents.IndexOf(
                    pair.Key,
                    StringComparison.Ordinal) < 0)
                .Select(pair => pair.Value));

            var layerMarkers = new[]
            {
                "id=\"game-start-cheese-body\"",
                "fill=\"url(#game-start-cheese-hole-one)\"",
                "fill=\"url(#game-start-cheese-hole-two)\"",
                "fill=\"url(#game-start-cheese-hole-three)\"",
                "id=\"game-start-cheese-gloss\""
            };
            var layerIndexes = layerMarkers
                .Select(marker => svgContents.IndexOf(marker, StringComparison.Ordinal))
                .ToArray();
            if (layerIndexes.All(index => index >= 0) &&
                layerIndexes.Zip(layerIndexes.Skip(1), (current, next) => current < next)
                    .Any(isOrdered => !isOrdered))
            {
                issues.Add("게임 시작 치즈 레이어 순서");
            }

            var hasExpectedVisibleElements =
                Regex.Matches(svgContents, @"<path\b", RegexOptions.IgnoreCase).Count == 2 &&
                Regex.Matches(svgContents, @"<circle\b", RegexOptions.IgnoreCase).Count == 3;
            var forbiddenVisibleMarkers = new[]
            {
                "<rect",
                "<ellipse",
                "<polygon",
                "<polyline",
                "<line ",
                "<line>",
                "<line/",
                "<image",
                "<use",
                "<text",
                "<style",
                "<animate",
                "<set",
                "<clipPath",
                "<mask",
                "stroke="
            };
            if (!hasExpectedVisibleElements || forbiddenVisibleMarkers.Any(marker =>
                    svgContents.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                issues.Add("게임 시작 치즈 가시 요소 구성");
            }

            var shadowMarkers = new[]
            {
                "fill=\"#190e08\"",
                "opacity=\"0.32\"",
                "translate(0 4)",
                "<filter",
                "filter="
            };
            if (shadowMarkers.Any(marker => svgContents.IndexOf(
                marker,
                StringComparison.OrdinalIgnoreCase) >= 0))
            {
                issues.Add("그림자 없는 게임 시작 치즈 SVG");
            }

            var sparkleMarkers = new[]
            {
                "id=\"spark",
                "class=\"spark",
                "class=\"hero-cheese__spark",
                "#fff0a6"
            };
            if (sparkleMarkers.Any(marker => svgContents.IndexOf(
                marker,
                StringComparison.OrdinalIgnoreCase) >= 0))
            {
                issues.Add("별빛 없는 게임 시작 치즈 SVG");
            }

            return issues.Distinct().ToArray();
        }

        public static string[] FindReleaseShellIssues(
            string indexContents,
            bool allowUnityTemplateDirectives)
        {
            if (string.IsNullOrWhiteSpace(indexContents))
            {
                return new[] { "index.html 내용" };
            }

            var requiredMarkers = new Dictionary<string, string>
            {
                { "lang=\"ko\"", "한국어 문서 언어" },
                { "name=\"cheesetama-public-base\" content=\"/play/\"", "고정 /play/ 경로 표식" },
                {
                    $"rel=\"icon\" type=\"image/svg+xml\" sizes=\"any\" href=\"{FaviconRelativePath}\"",
                    "게임 시작 치즈 본체 SVG 파비콘 선언"
                },
                {
                    "id=\"cheesetama-brand-symbol\"",
                    "헤더 브랜드 이미지 컨테이너"
                },
                {
                    "class=\"cheese-symbol__image\"",
                    "공유 SVG 헤더 브랜드 이미지"
                },
                {
                    $"src=\"{FaviconRelativePath}\"",
                    "파비콘과 헤더 브랜드 이미지의 공유 경로"
                },
                { "class=\"hero-cheese\"", "원래 CSS 게임 시작 심볼" },
                { "id=\"cheesetama-start\"", "첫 클릭 시작 화면" },
                { "id=\"cheesetama-progress\"", "로딩 진행률" },
                { "id=\"cheesetama-error\"", "오류 화면" },
                { "id=\"cheesetama-retry\"", "오류 재시도" },
                { "id=\"cheesetama-version\"", "버전 표시" },
                { "id=\"cheesetama-portrait-guide\"", "세로 화면 회전 안내" },
                { "autoSyncPersistentDataPath: true", "브라우저 저장 자동 동기화" },
                { "window.CheeseTamaReturnToGameStart", "게임 시작 화면 복귀 연결" },
                { "id=\"cheesetama-compatibility\"", "브라우저 호환성 경고 영역" }
            };
            var issues = requiredMarkers
                .Where(pair => indexContents.IndexOf(
                    pair.Key,
                    StringComparison.Ordinal) < 0)
                .Select(pair => pair.Value)
                .ToList();

            var forbiddenRoutineCopy = new Dictionary<string, string>
            {
                {
                    "게임 기록은 현재 주소와 브라우저 저장공간에 보관됩니다.",
                    "게임 시작 화면의 저장공간 안내 문구"
                },
                {
                    "시크릿 모드나 브라우저 데이터 삭제 시 기록이 사라질 수 있습니다.",
                    "게임 시작 화면의 기록 소실 안내 문구"
                },
                {
                    "최신 데스크톱 Chrome · Edge · Firefox · Safari 권장",
                    "게임 시작 화면의 브라우저 권장 문구"
                },
                {
                    "브라우저 실행 환경을 확인했습니다.",
                    "게임 시작 화면의 정상 환경 확인 문구"
                },
                {
                    "저장 데이터는 이 사이트의 /play/ 주소를 기준으로 구분됩니다.",
                    "게임 시작 화면의 푸터 저장 안내 문구"
                }
            };
            issues.AddRange(forbiddenRoutineCopy
                .Where(pair => indexContents.IndexOf(
                    pair.Key,
                    StringComparison.Ordinal) >= 0)
                .Select(pair => pair.Value));

            if (indexContents.IndexOf("class=\"game-footer\"", StringComparison.Ordinal) >= 0)
            {
                issues.Add("게임 시작 화면의 푸터 영역");
            }

            if (indexContents.IndexOf("class=\"hero-symbol\"", StringComparison.Ordinal) >= 0)
            {
                issues.Add("파비콘을 재사용한 게임 시작 이미지");
            }

            if (!allowUnityTemplateDirectives
                && (indexContents.Contains("{{{")
                    || Regex.IsMatch(indexContents, @"(?m)^\s*#(?:if|else|endif)\b")))
            {
                issues.Add("처리되지 않은 Unity 템플릿 지시문");
            }

            return issues.ToArray();
        }

        public static string[] FindMissingWebGlArtifacts(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return new[] { "출력 폴더" };
            }

            var fullOutputDirectory = Path.GetFullPath(outputDirectory);
            if (!Directory.Exists(fullOutputDirectory))
            {
                return new[] { "출력 폴더" };
            }

            var missing = new List<string>();
            if (!File.Exists(Path.Combine(fullOutputDirectory, IndexFileName)))
            {
                missing.Add(IndexFileName);
            }

            var buildDirectory = Path.Combine(fullOutputDirectory, BuildFolderName);
            if (!Directory.Exists(buildDirectory))
            {
                missing.Add(BuildFolderName + " 폴더");
                return missing.ToArray();
            }

            var buildFiles = Directory.EnumerateFiles(buildDirectory, "*", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
                .ToArray();
            var indexContents = File.Exists(Path.Combine(fullOutputDirectory, IndexFileName))
                ? File.ReadAllText(Path.Combine(fullOutputDirectory, IndexFileName))
                : string.Empty;
            AddMissingArtifact(
                missing,
                buildFiles,
                indexContents,
                "loaderUrl",
                ".loader.js",
                "loader JavaScript");
            AddMissingArtifact(
                missing,
                buildFiles,
                indexContents,
                "frameworkUrl",
                ".framework.js",
                "framework JavaScript");
            AddMissingArtifact(
                missing,
                buildFiles,
                indexContents,
                "codeUrl",
                ".wasm",
                "WebAssembly 코드");
            AddMissingArtifact(
                missing,
                buildFiles,
                indexContents,
                "dataUrl",
                ".data",
                "게임 데이터");
            return missing.ToArray();
        }

        public static string DescribeCurrentHostingRequirements()
        {
            return DescribeHostingRequirements(
                PlayerSettings.WebGL.compressionFormat,
                PlayerSettings.WebGL.decompressionFallback,
                PlayerSettings.WebGL.threadsSupport);
        }

        public static string DescribeHostingRequirements(
            WebGLCompressionFormat compressionFormat,
            bool decompressionFallback,
            bool threadsSupport)
        {
            var requirements = new List<string>();
            if (compressionFormat == WebGLCompressionFormat.Gzip && !decompressionFallback)
            {
                requirements.Add(
                    "호스트는 압축된 WebGL 파일에 Content-Encoding: gzip과 올바른 Content-Type을 제공해야 합니다.");
            }
            else if (compressionFormat == WebGLCompressionFormat.Brotli && !decompressionFallback)
            {
                requirements.Add(
                    "호스트는 압축된 WebGL 파일에 Content-Encoding: br과 올바른 Content-Type을 제공해야 합니다.");
            }
            else if (compressionFormat == WebGLCompressionFormat.Disabled)
            {
                requirements.Add("압축 전송은 비활성화되어 있으며 호스트의 Content-Encoding 설정이 필요하지 않습니다.");
            }
            else
            {
                requirements.Add("브라우저 측 압축 해제 대체 경로가 포함되어 있습니다.");
            }

            if (threadsSupport)
            {
                requirements.Add(
                    "스레드 빌드는 Cross-Origin-Opener-Policy: same-origin과 "
                    + "Cross-Origin-Embedder-Policy: require-corp 헤더가 필요합니다.");
            }
            else
            {
                requirements.Add("스레드 지원은 비활성화되어 COOP/COEP 헤더가 필수는 아닙니다.");
            }

            return string.Join(" ", requirements);
        }

        public static IDisposable ApplyPortableReleaseSettingsTemporarily()
        {
            ValidateReleaseTemplateAssets();
            var snapshot = CaptureWebGlSettings();
            try
            {
                ApplyPortableReleaseSettings();
                return snapshot;
            }
            catch
            {
                snapshot.Dispose();
                throw;
            }
        }

        private static void ApplyPortableReleaseSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.template = ReleaseTemplateName;
        }

        public static string EnableAutoSyncPersistentDataPath(string indexContents)
        {
            if (string.IsNullOrWhiteSpace(indexContents))
            {
                throw new BuildFailedException("WebGL index.html 내용이 비어 있습니다.");
            }

            const string enabledProperty = "autoSyncPersistentDataPath: true,";
            const string commentedPattern =
                @"(?m)^(?<indent>[ \t]*)//[ \t]*autoSyncPersistentDataPath\s*:\s*(?:true|false)\s*,?";
            if (Regex.IsMatch(indexContents, commentedPattern))
            {
                return Regex.Replace(
                    indexContents,
                    commentedPattern,
                    "${indent}" + enabledProperty,
                    RegexOptions.None,
                    TimeSpan.FromSeconds(1));
            }

            const string activePattern =
                @"(?m)^(?<indent>[ \t]*)autoSyncPersistentDataPath\s*:\s*(?:true|false)\s*,?";
            if (Regex.IsMatch(indexContents, activePattern))
            {
                return Regex.Replace(
                    indexContents,
                    activePattern,
                    "${indent}" + enabledProperty,
                    RegexOptions.None,
                    TimeSpan.FromSeconds(1));
            }

            var configMatch = Regex.Match(
                indexContents,
                @"\b(?:var|let|const)\s+config\s*=\s*\{",
                RegexOptions.None,
                TimeSpan.FromSeconds(1));
            if (!configMatch.Success)
            {
                throw new BuildFailedException(
                    "WebGL index.html에서 Unity config 객체를 찾지 못해 저장 자동 동기화를 활성화할 수 없습니다.");
            }

            var lineStart = indexContents.LastIndexOf('\n', configMatch.Index);
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            var leadingWhitespaceLength = 0;
            while (lineStart + leadingWhitespaceLength < indexContents.Length)
            {
                var character = indexContents[lineStart + leadingWhitespaceLength];
                if (character != ' ' && character != '\t')
                {
                    break;
                }

                leadingWhitespaceLength += 1;
            }

            var indent = indexContents.Substring(lineStart, leadingWhitespaceLength) + "  ";
            var lineEnding = indexContents.Contains("\r\n") ? "\r\n" : "\n";
            return indexContents.Insert(
                configMatch.Index + configMatch.Length,
                lineEnding + indent + enabledProperty);
        }

        public static void BuildDevelopmentFromCommandLine()
        {
            BuildWebGlPlayer(true, ResolveCommandLineOutput());
        }

        public static void BuildReleaseFromCommandLine()
        {
            BuildWebGlPlayer(false, ResolveCommandLineOutput());
        }

        public static void BuildCleanReleaseFromCommandLine()
        {
            BuildWebGlPlayer(false, ResolveCommandLineOutput(), true);
        }

        private static void AddMissingArtifact(
            ICollection<string> missing,
            IEnumerable<string> fileNames,
            string indexContents,
            string urlProperty,
            string marker,
            string displayName)
        {
            var names = fileNames.ToArray();
            var referencedFileName = FindReferencedFileName(indexContents, urlProperty);
            var referencedFileExists = !string.IsNullOrWhiteSpace(referencedFileName)
                && names.Any(fileName => string.Equals(
                    fileName,
                    referencedFileName,
                    StringComparison.OrdinalIgnoreCase));
            var conventionalFileExists = names.Any(fileName =>
                fileName.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!referencedFileExists && !conventionalFileExists)
            {
                missing.Add(displayName);
            }
        }

        private static string FindReferencedFileName(string indexContents, string urlProperty)
        {
            if (string.IsNullOrWhiteSpace(indexContents))
            {
                return null;
            }

            var pattern = $@"\b{Regex.Escape(urlProperty)}\s*(?::|=)\s*"
                + @"(?:buildUrl\s*\+\s*)?[""']/?(?<path>[^""']+)[""']";
            var match = Regex.Match(indexContents, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            var relativePath = match.Groups["path"].Value
                .Split(new[] { '?', '#' }, 2)[0]
                .Replace('\\', '/');
            return Path.GetFileName(relativePath);
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

        private static WebGlSettingsSnapshot CaptureWebGlSettings()
        {
            return new WebGlSettingsSnapshot(
                PlayerSettings.WebGL.compressionFormat,
                PlayerSettings.WebGL.decompressionFallback,
                PlayerSettings.WebGL.dataCaching,
                PlayerSettings.WebGL.threadsSupport,
                PlayerSettings.WebGL.debugSymbolMode,
                PlayerSettings.WebGL.template);
        }

        private sealed class WebGlSettingsSnapshot : IDisposable
        {
            private readonly WebGLCompressionFormat compressionFormat;
            private readonly bool decompressionFallback;
            private readonly bool dataCaching;
            private readonly bool threadsSupport;
            private readonly WebGLDebugSymbolMode debugSymbolMode;
            private readonly string template;
            private bool disposed;

            public WebGlSettingsSnapshot(
                WebGLCompressionFormat compressionFormat,
                bool decompressionFallback,
                bool dataCaching,
                bool threadsSupport,
                WebGLDebugSymbolMode debugSymbolMode,
                string template)
            {
                this.compressionFormat = compressionFormat;
                this.decompressionFallback = decompressionFallback;
                this.dataCaching = dataCaching;
                this.threadsSupport = threadsSupport;
                this.debugSymbolMode = debugSymbolMode;
                this.template = template;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                PlayerSettings.WebGL.compressionFormat = compressionFormat;
                PlayerSettings.WebGL.decompressionFallback = decompressionFallback;
                PlayerSettings.WebGL.dataCaching = dataCaching;
                PlayerSettings.WebGL.threadsSupport = threadsSupport;
                PlayerSettings.WebGL.debugSymbolMode = debugSymbolMode;
                PlayerSettings.WebGL.template = template;
                disposed = true;
            }
        }
    }

    public sealed class CheeseTamaWebGlReleaseContentSanitizer : IPreprocessBuildWithReport
    {
        // Unity Performance Testing의 callbackOrder(0) 뒤에 실행해 임시 Resources JSON을
        // 플레이어 콘텐츠 직렬화 전에 제거한다.
        public int callbackOrder => 1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null
                || report.summary.platform != BuildTarget.WebGL
                || (report.summary.options & BuildOptions.Development) != 0)
            {
                return;
            }

            var removedFileCount =
                CheeseTamaWebGlBuildValidator.RemovePerformanceTestMetadataFromReleaseContent(
                    Application.dataPath);
            if (removedFileCount <= 0)
            {
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log(
                $"CheeseTama WebGL 릴리스에서 성능 테스트 임시 메타데이터 "
                + $"{removedFileCount}개를 제외했습니다.");
        }
    }

    public sealed class CheeseTamaWebGlBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.WebGL)
            {
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var outputDirectory = Path.GetFullPath(report.summary.outputPath);
            var indexPath = Path.Combine(outputDirectory, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new BuildFailedException(
                    $"WebGL 저장 자동 동기화를 설정할 index.html을 찾을 수 없습니다: {report.summary.outputPath}");
            }

            var original = File.ReadAllText(indexPath);
            var updated = CheeseTamaWebGlBuildValidator.EnableAutoSyncPersistentDataPath(original);
            if (!string.Equals(original, updated, StringComparison.Ordinal))
            {
                File.WriteAllText(indexPath, updated, new UTF8Encoding(false));
            }

            CheeseTamaWebGlBuildValidator.CopyNanumGothicLicenseToOutput(
                Application.dataPath,
                outputDirectory);

            if ((report.summary.options & BuildOptions.Development) == 0)
            {
                var sanitizedCount =
                    CheeseTamaWebGlBuildValidator.SanitizeAndValidateEmbeddedHostPaths(
                        outputDirectory);
                if (sanitizedCount > 0)
                {
                    Debug.Log(
                        $"CheeseTama WebGL 릴리스에서 도구체인 절대 경로 "
                        + $"{sanitizedCount}건을 정제했습니다.");
                }
            }

            stopwatch.Stop();
            Debug.Log(
                $"CheeseTama WebGL 산출물 후처리 완료: "
                + $"{stopwatch.Elapsed.TotalSeconds:F2}초");
        }
    }
}
