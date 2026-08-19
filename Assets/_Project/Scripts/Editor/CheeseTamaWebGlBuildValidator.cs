using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public static BuildReport BuildWebGlPlayer(bool development, string outputDirectory)
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

            var buildOptions = development ? BuildOptions.Development : BuildOptions.CleanBuildCache;
            if (!development)
            {
                CheeseTamaBuildValidator.ValidateReleaseBuildConfiguration(scenes, buildOptions);
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

            CopyNanumGothicLicenseToOutput(Application.dataPath, fullOutputDirectory);
            ValidateWebGlOutput(fullOutputDirectory);
            if (!development)
            {
                ValidateReleaseShellOutput(fullOutputDirectory);
                ValidateNoEmbeddedHostPaths(fullOutputDirectory);
            }

            Debug.Log(
                $"CheeseTama WebGL {(development ? "개발" : "릴리스")} 빌드 완료: "
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
                "TemplateData/style.css"
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

            var templateIndex = File.ReadAllText(Path.Combine(templateDirectory, IndexFileName));
            var issues = FindReleaseShellIssues(templateIndex, allowUnityTemplateDirectives: true);
            if (issues.Length > 0)
            {
                throw new BuildFailedException(
                    "CheeseTama WebGL 릴리스 템플릿 계약을 충족하지 못했습니다: "
                    + string.Join(", ", issues));
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
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("출력 경로가 비어 있습니다.", nameof(outputDirectory));
            }

            var buildDirectory = Path.Combine(
                Path.GetFullPath(outputDirectory),
                BuildFolderName);
            if (!Directory.Exists(buildDirectory))
            {
                throw new BuildFailedException(
                    $"WebGL Build 폴더를 찾을 수 없습니다: {buildDirectory}");
            }

            var sanitizedCount = 0;
            foreach (var filePath in Directory.EnumerateFiles(
                         buildDirectory,
                         "*.unityweb",
                         SearchOption.AllDirectories))
            {
                var encodedBytes = File.ReadAllBytes(filePath);
                var isGzip = encodedBytes.Length >= 2
                    && encodedBytes[0] == 0x1f
                    && encodedBytes[1] == 0x8b;
                var payload = isGzip
                    ? DecompressGzip(encodedBytes)
                    : encodedBytes;
                var fileSanitizedCount = ReplaceToolchainPathPrefixes(payload);
                if (fileSanitizedCount <= 0)
                {
                    continue;
                }

                File.WriteAllBytes(
                    filePath,
                    isGzip ? CompressGzip(payload) : payload);
                sanitizedCount += fileSanitizedCount;
            }

            return sanitizedCount;
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
            if (embeddedPathIssues.Length > 0)
            {
                throw new BuildFailedException(
                    "WebGL 산출물에 게시할 수 없는 호스트 절대 경로가 남아 있습니다: "
                    + string.Join(", ", embeddedPathIssues));
            }
        }

        public static string[] FindEmbeddedHostPathIssues(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return new[] { "출력 폴더" };
            }

            var buildDirectory = Path.Combine(
                Path.GetFullPath(outputDirectory),
                BuildFolderName);
            if (!Directory.Exists(buildDirectory))
            {
                return Array.Empty<string>();
            }

            var issues = new List<string>();
            foreach (var filePath in Directory.EnumerateFiles(
                         buildDirectory,
                         "*.unityweb",
                         SearchOption.AllDirectories))
            {
                byte[] payload;
                try
                {
                    var encodedBytes = File.ReadAllBytes(filePath);
                    payload = encodedBytes.Length >= 2
                              && encodedBytes[0] == 0x1f
                              && encodedBytes[1] == 0x8b
                        ? DecompressGzip(encodedBytes)
                        : encodedBytes;
                }
                catch (InvalidDataException)
                {
                    issues.Add(Path.GetFileName(filePath) + " (손상된 gzip)");
                    continue;
                }

                var fileName = Path.GetFileName(filePath);
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

            return issues.Distinct(StringComparer.Ordinal).ToArray();
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
                { "id=\"cheesetama-start\"", "첫 클릭 시작 화면" },
                { "id=\"cheesetama-progress\"", "로딩 진행률" },
                { "id=\"cheesetama-error\"", "오류 화면" },
                { "id=\"cheesetama-retry\"", "오류 재시도" },
                { "id=\"cheesetama-version\"", "버전 표시" },
                { "autoSyncPersistentDataPath: true", "브라우저 저장 자동 동기화" },
                { "브라우저 저장공간", "저장 위치 안내" },
                { "Chrome", "지원 브라우저 안내" }
            };
            var issues = requiredMarkers
                .Where(pair => indexContents.IndexOf(
                    pair.Key,
                    StringComparison.Ordinal) < 0)
                .Select(pair => pair.Value)
                .ToList();

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
                    CheeseTamaWebGlBuildValidator.SanitizeEmbeddedToolchainAbsolutePaths(
                        outputDirectory);
                if (sanitizedCount > 0)
                {
                    Debug.Log(
                        $"CheeseTama WebGL 릴리스에서 도구체인 절대 경로 "
                        + $"{sanitizedCount}건을 정제했습니다.");
                }

                CheeseTamaWebGlBuildValidator.ValidateNoEmbeddedHostPaths(
                    outputDirectory);
            }
        }
    }
}
