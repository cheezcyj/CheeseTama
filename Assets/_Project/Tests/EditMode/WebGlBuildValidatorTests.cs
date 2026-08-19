using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using CheeseTama.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace CheeseTama.Tests.EditMode
{
    public sealed class WebGlBuildValidatorTests
    {
        [Test]
        public void CompressedBuildWithoutFallbackDescribesRequiredServerHeaders()
        {
            var requirements = CheeseTamaWebGlBuildValidator.DescribeHostingRequirements(
                WebGLCompressionFormat.Gzip,
                false,
                false);

            Assert.That(requirements, Does.Contain("Content-Encoding: gzip"));
            Assert.That(requirements, Does.Contain("COOP/COEP 헤더가 필수는 아닙니다"));
        }

        [Test]
        public void ThreadedBuildDescribesCrossOriginIsolationHeaders()
        {
            var requirements = CheeseTamaWebGlBuildValidator.DescribeHostingRequirements(
                WebGLCompressionFormat.Brotli,
                false,
                true);

            Assert.That(requirements, Does.Contain("Content-Encoding: br"));
            Assert.That(requirements, Does.Contain("Cross-Origin-Opener-Policy: same-origin"));
            Assert.That(requirements, Does.Contain("Cross-Origin-Embedder-Policy: require-corp"));
        }

        [Test]
        public void PortableFallbackProfileNeedsNoHostCompressionHeader()
        {
            var requirements = CheeseTamaWebGlBuildValidator.DescribeHostingRequirements(
                WebGLCompressionFormat.Gzip,
                true,
                false);

            Assert.That(requirements, Does.Contain("브라우저 측 압축 해제 대체 경로"));
            Assert.That(requirements, Does.Not.Contain("Content-Encoding: gzip"));
            Assert.That(requirements, Does.Contain("COOP/COEP 헤더가 필수는 아닙니다"));
        }

        [Test]
        public void PortableReleaseSettingsAreAppliedAndRestoredWithoutTouchingAdvancedOptions()
        {
            var originalCompression = PlayerSettings.WebGL.compressionFormat;
            var originalFallback = PlayerSettings.WebGL.decompressionFallback;
            var originalCaching = PlayerSettings.WebGL.dataCaching;
            var originalThreads = PlayerSettings.WebGL.threadsSupport;
            var originalDebugSymbols = PlayerSettings.WebGL.debugSymbolMode;
            var originalTemplate = PlayerSettings.WebGL.template;
            var originalExceptionSupport = PlayerSettings.WebGL.exceptionSupport;
            var originalWasm2023 = PlayerSettings.WebGL.wasm2023;

            using (CheeseTamaWebGlBuildValidator.ApplyPortableReleaseSettingsTemporarily())
            {
                Assert.That(PlayerSettings.WebGL.compressionFormat, Is.EqualTo(WebGLCompressionFormat.Gzip));
                Assert.That(PlayerSettings.WebGL.decompressionFallback, Is.True);
                Assert.That(PlayerSettings.WebGL.dataCaching, Is.True);
                Assert.That(PlayerSettings.WebGL.threadsSupport, Is.False);
                Assert.That(PlayerSettings.WebGL.debugSymbolMode, Is.EqualTo(WebGLDebugSymbolMode.Off));
                Assert.That(
                    PlayerSettings.WebGL.template,
                    Is.EqualTo(CheeseTamaWebGlBuildValidator.ReleaseTemplateName));
                Assert.That(PlayerSettings.WebGL.exceptionSupport, Is.EqualTo(originalExceptionSupport));
                Assert.That(PlayerSettings.WebGL.wasm2023, Is.EqualTo(originalWasm2023));
            }

            Assert.That(PlayerSettings.WebGL.compressionFormat, Is.EqualTo(originalCompression));
            Assert.That(PlayerSettings.WebGL.decompressionFallback, Is.EqualTo(originalFallback));
            Assert.That(PlayerSettings.WebGL.dataCaching, Is.EqualTo(originalCaching));
            Assert.That(PlayerSettings.WebGL.threadsSupport, Is.EqualTo(originalThreads));
            Assert.That(PlayerSettings.WebGL.debugSymbolMode, Is.EqualTo(originalDebugSymbols));
            Assert.That(PlayerSettings.WebGL.template, Is.EqualTo(originalTemplate));
        }

        [Test]
        public void ReleaseTemplateProvidesBrandedStartProgressErrorAndSaveGuidance()
        {
            Assert.DoesNotThrow(CheeseTamaWebGlBuildValidator.ValidateReleaseTemplateAssets);

            var templatePath = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath)!.FullName,
                CheeseTamaWebGlBuildValidator.ReleaseTemplateAssetFolder,
                "index.html");
            var template = File.ReadAllText(templatePath);

            Assert.That(
                CheeseTamaWebGlBuildValidator.FindReleaseShellIssues(
                    template,
                    allowUnityTemplateDirectives: true),
                Is.Empty);
            Assert.That(template, Does.Contain("{{{ PRODUCT_VERSION }}}"));
            Assert.That(template, Does.Contain("createUnityInstance"));
            Assert.That(template, Does.Contain("window.location.reload()"));
        }

        [Test]
        public void GeneratedReleaseShellRejectsUnresolvedTemplateDirectives()
        {
            const string incomplete = "<html lang=\"ko\">{{{ PRODUCT_NAME }}}</html>";

            var issues = CheeseTamaWebGlBuildValidator.FindReleaseShellIssues(
                incomplete,
                allowUnityTemplateDirectives: false);

            Assert.That(issues, Does.Contain("처리되지 않은 Unity 템플릿 지시문"));
            Assert.That(issues, Does.Contain("첫 클릭 시작 화면"));
        }

        [Test]
        public void AlreadyEnabledAutoSyncConfigurationIsIdempotent()
        {
            const string index = "var config = {\n  autoSyncPersistentDataPath: true,\n};";

            var once = CheeseTamaWebGlBuildValidator.EnableAutoSyncPersistentDataPath(index);
            var twice = CheeseTamaWebGlBuildValidator.EnableAutoSyncPersistentDataPath(once);

            Assert.That(once, Is.EqualTo(index));
            Assert.That(twice, Is.EqualTo(once));
        }

        [Test]
        public void CommentedDefaultAutoSyncConfigurationIsEnabled()
        {
            const string index = "var config = {\n  // autoSyncPersistentDataPath: true,\n};";

            var updated = CheeseTamaWebGlBuildValidator.EnableAutoSyncPersistentDataPath(index);

            Assert.That(updated, Does.Contain("\n  autoSyncPersistentDataPath: true,"));
            Assert.That(updated, Does.Not.Contain("// autoSyncPersistentDataPath"));
        }

        [Test]
        public void MissingAutoSyncConfigurationIsInsertedIntoUnityConfig()
        {
            const string index = "  const config = {\n    dataUrl: buildUrl + \"/game.data\",\n  };";

            var updated = CheeseTamaWebGlBuildValidator.EnableAutoSyncPersistentDataPath(index);

            Assert.That(
                updated,
                Does.Contain("const config = {\n    autoSyncPersistentDataPath: true,"));
        }

        [Test]
        public void CompleteCompressedOutputPassesArtifactValidation()
        {
            var root = CreateTemporaryOutput();
            try
            {
                File.WriteAllText(Path.Combine(root, "index.html"), "<html></html>");
                var build = Directory.CreateDirectory(Path.Combine(root, "Build")).FullName;
                File.WriteAllText(Path.Combine(build, "CheeseTama.loader.js"), string.Empty);
                File.WriteAllText(Path.Combine(build, "CheeseTama.framework.js.gz"), string.Empty);
                File.WriteAllText(Path.Combine(build, "CheeseTama.wasm.gz"), string.Empty);
                File.WriteAllText(Path.Combine(build, "CheeseTama.data.gz"), string.Empty);

                Assert.That(
                    CheeseTamaWebGlBuildValidator.FindMissingWebGlArtifacts(root),
                    Is.Empty);
                Assert.DoesNotThrow(() => CheeseTamaWebGlBuildValidator.ValidateWebGlOutput(root));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void HashedUnityWebArtifactsReferencedByIndexPassValidation()
        {
            var root = CreateTemporaryOutput();
            try
            {
                File.WriteAllText(
                    Path.Combine(root, "index.html"),
                    "var loaderUrl = buildUrl + \"/a1.unityweb\"; "
                    + "var config = { frameworkUrl: buildUrl + \"/b2.unityweb\", "
                    + "codeUrl: buildUrl + \"/c3.unityweb\", "
                    + "dataUrl: buildUrl + \"/d4.unityweb\" }; ");
                var build = Directory.CreateDirectory(Path.Combine(root, "Build")).FullName;
                File.WriteAllText(Path.Combine(build, "a1.unityweb"), string.Empty);
                File.WriteAllText(Path.Combine(build, "b2.unityweb"), string.Empty);
                File.WriteAllText(Path.Combine(build, "c3.unityweb"), string.Empty);
                File.WriteAllText(Path.Combine(build, "d4.unityweb"), string.Empty);

                Assert.That(
                    CheeseTamaWebGlBuildValidator.FindMissingWebGlArtifacts(root),
                    Is.Empty);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void IncompleteOutputReportsEachRequiredArtifactKind()
        {
            var root = CreateTemporaryOutput();
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "Build"));

                var missing = CheeseTamaWebGlBuildValidator.FindMissingWebGlArtifacts(root);

                Assert.That(missing, Does.Contain("index.html"));
                Assert.That(missing, Does.Contain("loader JavaScript"));
                Assert.That(missing, Does.Contain("framework JavaScript"));
                Assert.That(missing, Does.Contain("WebAssembly 코드"));
                Assert.That(missing, Does.Contain("게임 데이터"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void OutputValidationRejectsDoNotShipContent()
        {
            var root = CreateTemporaryOutput();
            try
            {
                File.WriteAllText(Path.Combine(root, "index.html"), "<html></html>");
                var build = Directory.CreateDirectory(Path.Combine(root, "Build")).FullName;
                File.WriteAllText(Path.Combine(build, "CheeseTama.loader.js"), string.Empty);
                File.WriteAllText(Path.Combine(build, "CheeseTama.framework.js"), string.Empty);
                File.WriteAllText(Path.Combine(build, "CheeseTama.wasm"), string.Empty);
                File.WriteAllText(Path.Combine(build, "CheeseTama.data"), string.Empty);
                var blocked = Directory.CreateDirectory(Path.Combine(build, "DoNotShip")).FullName;
                File.WriteAllText(Path.Combine(blocked, "internal.txt"), string.Empty);

                Assert.Throws<BuildFailedException>(() =>
                    CheeseTamaWebGlBuildValidator.ValidateWebGlOutput(root));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReleaseContentSanitizerRemovesOnlyPerformanceMetadataFiles()
        {
            var root = CreateTemporaryOutput();
            try
            {
                var resources = Directory.CreateDirectory(
                    Path.Combine(root, "Resources")).FullName;
                var expectedRemoved = new[]
                {
                    "PerformanceTestRunInfo.json",
                    "PerformanceTestRunInfo.json.meta",
                    "PerformanceTestRunSettings.json",
                    "PerformanceTestRunSettings.json.meta"
                };
                foreach (var fileName in expectedRemoved)
                {
                    File.WriteAllText(Path.Combine(resources, fileName), "temporary");
                }

                var preservedPath = Path.Combine(resources, "GameRuntimeData.json");
                File.WriteAllText(preservedPath, "preserve");

                var removed =
                    CheeseTamaWebGlBuildValidator.RemovePerformanceTestMetadataFromReleaseContent(
                        root);

                Assert.That(removed, Is.EqualTo(expectedRemoved.Length));
                foreach (var fileName in expectedRemoved)
                {
                    Assert.That(File.Exists(Path.Combine(resources, fileName)), Is.False);
                }

                Assert.That(File.ReadAllText(preservedPath), Is.EqualTo("preserve"));
                Assert.That(Directory.Exists(resources), Is.True);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void KoreanFontAssetAndLicenseAreRequiredForWebRelease()
        {
            Assert.DoesNotThrow(CheeseTamaWebGlBuildValidator.ValidateKoreanFontAsset);

            var assetsRoot = CreateTemporaryOutput();
            var outputRoot = CreateTemporaryOutput();
            try
            {
                var sourceDirectory = Directory.CreateDirectory(Path.Combine(
                    assetsRoot,
                    "_Project",
                    "ThirdParty",
                    "Fonts",
                    "NanumGothic")).FullName;
                const string licenseContents = "SIL OPEN FONT LICENSE Version 1.1";
                File.WriteAllText(Path.Combine(sourceDirectory, "OFL.txt"), licenseContents);

                var destination =
                    CheeseTamaWebGlBuildValidator.CopyNanumGothicLicenseToOutput(
                        assetsRoot,
                        outputRoot);

                Assert.That(File.Exists(destination), Is.True);
                Assert.That(File.ReadAllText(destination), Is.EqualTo(licenseContents));
                Assert.That(
                    destination.Replace('\\', '/'),
                    Does.EndWith("ThirdPartyNotices/NanumGothic-OFL.txt"));
            }
            finally
            {
                Directory.Delete(assetsRoot, true);
                Directory.Delete(outputRoot, true);
            }
        }

        [Test]
        public void ReleaseSanitizerRemovesOnlyKnownAbsoluteToolchainPrefixesFromGzip()
        {
            var root = CreateTemporaryOutput();
            try
            {
                var buildDirectory = Directory.CreateDirectory(
                    Path.Combine(root, "Build")).FullName;
                var artifactPath = Path.Combine(buildDirectory, "Release.wasm.unityweb");
                var originalPayload = Encoding.ASCII.GetBytes(
                    "before\0C:" + "/Program Files/Unity/Hub/Editor/6000.0/test.cpp(1) : after\0"
                    + "C:" + @"\dev\dots\Packages\com.unity.collections\ILSupport.csT"
                    + "\0tail");
                WriteGzip(artifactPath, originalPayload);

                var sanitized =
                    CheeseTamaWebGlBuildValidator.SanitizeEmbeddedToolchainAbsolutePaths(root);
                var actualPayload = ReadGzip(artifactPath);
                var expectedPayload = (byte[])originalPayload.Clone();
                ReplaceAsciiPrefixWithUnderscores(
                    expectedPayload,
                    "C:" + "/Program Files/Unity/Hub/Editor/");
                ReplaceAsciiPrefixWithUnderscores(expectedPayload, "C:" + @"\dev\dots\");

                Assert.That(sanitized, Is.EqualTo(2));
                Assert.That(actualPayload, Is.EqualTo(expectedPayload));
                Assert.That(
                    Encoding.ASCII.GetString(File.ReadAllBytes(artifactPath)),
                    Does.Contain("UnityWeb Compressed Content (gzip)"));
                Assert.That(
                    Encoding.ASCII.GetString(actualPayload),
                    Does.Not.Contain("C:" + "/Program Files/Unity/Hub/Editor/"));
                Assert.That(
                    Encoding.ASCII.GetString(actualPayload),
                    Does.Not.Contain("C:" + @"\dev\dots\"));

                var encodedAfterFirstPass = File.ReadAllBytes(artifactPath);
                Assert.That(
                    CheeseTamaWebGlBuildValidator.SanitizeEmbeddedToolchainAbsolutePaths(root),
                    Is.Zero);
                Assert.That(File.ReadAllBytes(artifactPath), Is.EqualTo(encodedAfterFirstPass));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReleaseSanitizerRemovesCurrentProjectPathFromGzip()
        {
            var root = CreateTemporaryOutput();
            try
            {
                var buildDirectory = Directory.CreateDirectory(
                    Path.Combine(root, "Build")).FullName;
                var artifactPath = Path.Combine(buildDirectory, "Release.data.unityweb");
                var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)!.FullName
                    .TrimEnd('/', '\\')
                    .Replace('\\', '/');
                var originalPayload = Encoding.UTF8.GetBytes(
                    "before\0" + projectRoot + "/Assets/_Project/runtime.bin\0after");
                WriteGzip(artifactPath, originalPayload);

                var sanitized =
                    CheeseTamaWebGlBuildValidator.SanitizeEmbeddedToolchainAbsolutePaths(root);
                var actualPayload = Encoding.UTF8.GetString(ReadGzip(artifactPath));

                Assert.That(sanitized, Is.EqualTo(1));
                Assert.That(actualPayload, Does.Not.Contain(projectRoot));
                Assert.That(actualPayload, Does.EndWith("Assets/_Project/runtime.bin\0after"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReleaseGateRejectsEmbeddedHostUserPathsFromGzipArtifacts()
        {
            var root = CreateTemporaryOutput();
            try
            {
                var buildDirectory = Directory.CreateDirectory(
                    Path.Combine(root, "Build")).FullName;
                var artifactPath = Path.Combine(buildDirectory, "Release.data.unityweb");
                var payload = Encoding.UTF8.GetBytes(
                    "before\0"
                    + "D:" + @"\" + "Users" + @"\build-user\project\asset.bin"
                    + "\0/" + "Users" + "/build-user/project/asset.bin"
                    + "\0/" + "home" + "/build-user/project/asset.bin\0after");
                WriteGzip(artifactPath, payload);

                var issues =
                    CheeseTamaWebGlBuildValidator.FindEmbeddedHostPathIssues(root);

                Assert.That(issues, Has.Length.EqualTo(3));
                Assert.That(issues, Has.Some.Contains("Windows 절대 경로"));
                Assert.That(issues, Has.Some.Contains("macOS 사용자 경로"));
                Assert.That(issues, Has.Some.Contains("Linux 사용자 경로"));
                Assert.Throws<BuildFailedException>(() =>
                    CheeseTamaWebGlBuildValidator.ValidateNoEmbeddedHostPaths(root));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReleaseGateAllowsEmscriptenVirtualHome()
        {
            var root = CreateTemporaryOutput();
            try
            {
                var buildDirectory = Directory.CreateDirectory(
                    Path.Combine(root, "Build")).FullName;
                var artifactPath = Path.Combine(buildDirectory, "Release.framework.js.unityweb");
                WriteGzip(
                    artifactPath,
                    Encoding.ASCII.GetBytes("virtual:/home/web_user/.config\0"));

                Assert.That(
                    CheeseTamaWebGlBuildValidator.FindEmbeddedHostPathIssues(root),
                    Is.Empty);
                Assert.DoesNotThrow(() =>
                    CheeseTamaWebGlBuildValidator.ValidateNoEmbeddedHostPaths(root));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReleaseGateAllowsQuotedEmscriptenVirtualHome()
        {
            var root = CreateTemporaryOutput();
            try
            {
                var buildDirectory = Directory.CreateDirectory(
                    Path.Combine(root, "Build")).FullName;
                var artifactPath = Path.Combine(buildDirectory, "Release.framework.js.unityweb");
                WriteGzip(
                    artifactPath,
                    Encoding.ASCII.GetBytes("HOME=\"/home/web_user\",PWD=\"/\""));

                Assert.That(
                    CheeseTamaWebGlBuildValidator.FindEmbeddedHostPathIssues(root),
                    Is.Empty);
                Assert.DoesNotThrow(() =>
                    CheeseTamaWebGlBuildValidator.ValidateNoEmbeddedHostPaths(root));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReleaseGateRejectsLinuxHomeThatOnlySharesVirtualHomePrefix()
        {
            var root = CreateTemporaryOutput();
            try
            {
                var buildDirectory = Directory.CreateDirectory(
                    Path.Combine(root, "Build")).FullName;
                var artifactPath = Path.Combine(buildDirectory, "Release.framework.js.unityweb");
                WriteGzip(
                    artifactPath,
                    Encoding.ASCII.GetBytes(
                        "host:/" + "home" + "/web_user-ci/private\0"));

                var issues =
                    CheeseTamaWebGlBuildValidator.FindEmbeddedHostPathIssues(root);

                Assert.That(issues, Has.Some.Contains("Linux 사용자 경로"));
                Assert.Throws<BuildFailedException>(() =>
                    CheeseTamaWebGlBuildValidator.ValidateNoEmbeddedHostPaths(root));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReleaseGateIgnoresBinaryDriveLikeBytesWithoutAPathBody()
        {
            var root = CreateTemporaryOutput();
            try
            {
                var buildDirectory = Directory.CreateDirectory(
                    Path.Combine(root, "Build")).FullName;
                var artifactPath = Path.Combine(buildDirectory, "Release.data.unityweb");
                WriteGzip(
                    artifactPath,
                    new byte[]
                    {
                        0, (byte)'C', (byte)':', (byte)'\\', 0xbe, 0xa0, 0xa2, 0,
                        (byte)'v', (byte)'i', (byte)'r', (byte)'t', (byte)'u', (byte)'a',
                        (byte)'l', (byte)':', (byte)'/', (byte)'/', (byte)'6', 0xb4, 0,
                        (byte)'m', (byte)':', (byte)'\\', (byte)'W', (byte)'U',
                        (byte)'U', (byte)'m', (byte)':', (byte)'L', (byte)'2', 0
                    });

                Assert.That(
                    CheeseTamaWebGlBuildValidator.FindEmbeddedHostPathIssues(root),
                    Is.Empty);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void RuntimePrimitiveComponentsArePreservedForWebGlStripping()
        {
            var linkPath = Path.Combine(UnityEngine.Application.dataPath, "_Project", "link.xml");
            var linkXml = File.ReadAllText(linkPath);

            Assert.That(linkXml, Does.Contain("UnityEngine.MeshFilter"));
            Assert.That(linkXml, Does.Contain("UnityEngine.MeshRenderer"));
            Assert.That(linkXml, Does.Contain("UnityEngine.BoxCollider"));
            Assert.That(linkXml, Does.Contain("UnityEngine.SphereCollider"));
            Assert.That(linkXml, Does.Contain("UnityEngine.CapsuleCollider"));
            Assert.That(linkXml, Does.Contain("UnityEngine.MeshCollider"));
        }

        private static void WriteGzip(string path, byte[] payload)
        {
            using var file = File.Create(path);
            using var gzip = new GZipStream(
                file,
                System.IO.Compression.CompressionLevel.Optimal);
            gzip.Write(payload, 0, payload.Length);
        }

        private static byte[] ReadGzip(string path)
        {
            using var file = File.OpenRead(path);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static void ReplaceAsciiPrefixWithUnderscores(byte[] payload, string prefix)
        {
            var needle = Encoding.ASCII.GetBytes(prefix);
            for (var offset = 0; offset <= payload.Length - needle.Length; offset++)
            {
                var matches = true;
                for (var index = 0; index < needle.Length; index++)
                {
                    if (payload[offset + index] == needle[index])
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

                for (var index = 0; index < needle.Length; index++)
                {
                    payload[offset + index] = (byte)'_';
                }

                offset += needle.Length - 1;
            }
        }

        private static string CreateTemporaryOutput()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"cheesetama_webgl_gate_{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
