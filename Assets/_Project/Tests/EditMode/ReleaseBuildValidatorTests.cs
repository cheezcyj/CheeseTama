using System;
using System.IO;
using CheeseTama.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace CheeseTama.Tests.EditMode
{
    public sealed class ReleaseBuildValidatorTests
    {
        [Test]
        public void ReleaseSceneResolutionExcludesDebugAndDoNotShipEntries()
        {
            var configured = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene(CheeseTamaBuildValidator.DebugScenePath, true),
                new EditorBuildSettingsScene("Assets/_Project/DoNotShip/Internal.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Collection.unity", false)
            };

            var releaseScenes = CheeseTamaBuildValidator.ResolveScenePathsForBuild(false, configured);
            var developmentScenes = CheeseTamaBuildValidator.ResolveScenePathsForBuild(true, configured);

            Assert.That(releaseScenes, Is.EqualTo(new[] { "Assets/_Project/Scenes/Boot.unity" }));
            Assert.That(developmentScenes, Has.Length.EqualTo(3));
        }

        [Test]
        public void ReleaseConfigurationRejectsDebugScene()
        {
            Assert.Throws<BuildFailedException>(() =>
                CheeseTamaBuildValidator.ValidateReleaseBuildConfiguration(
                    new[]
                    {
                        "Assets/_Project/Scenes/Boot.unity",
                        CheeseTamaBuildValidator.DebugScenePath
                    },
                    BuildOptions.CleanBuildCache));
        }

        [TestCase(BuildOptions.Development)]
        [TestCase(BuildOptions.AllowDebugging)]
        [TestCase(BuildOptions.ConnectWithProfiler)]
        [TestCase(BuildOptions.EnableDeepProfilingSupport)]
        public void ReleaseConfigurationRejectsDevelopmentOnlyOptions(BuildOptions option)
        {
            Assert.Throws<BuildFailedException>(() =>
                CheeseTamaBuildValidator.ValidateReleaseBuildConfiguration(
                    new[] { "Assets/_Project/Scenes/Boot.unity" },
                    option));
        }

        [Test]
        public void CleanNonDevelopmentConfigurationIsAccepted()
        {
            Assert.DoesNotThrow(() =>
                CheeseTamaBuildValidator.ValidateReleaseBuildConfiguration(
                    new[]
                    {
                        "Assets/_Project/Scenes/Boot.unity",
                        "Assets/_Project/Scenes/Milkroom.unity",
                        "Assets/_Project/Scenes/Collection.unity"
                    },
                    BuildOptions.CleanBuildCache));
        }

        [TestCase("Assets/_Project/DoNotShip/Internal.prefab")]
        [TestCase("Assets/DoNotShip/Internal.asset")]
        public void DoNotShipFolderMarkerIsDetected(string assetPath)
        {
            Assert.That(CheeseTamaBuildValidator.IsDoNotShipPath(assetPath), Is.True);
        }

        [TestCase("Assets/_Project/Scripts/UI/DevPanelController.cs")]
        [TestCase("Assets/_Project/Scenes/Milkroom.unity")]
        public void OrdinaryRuntimeDevelopmentGuardFilesAreNotPathBlocked(string assetPath)
        {
            Assert.That(CheeseTamaBuildValidator.IsDoNotShipPath(assetPath), Is.False);
        }

        [Test]
        public void ReleaseFinalizationRemovesOnlyExpectedBurstDoNotShipSidecar()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"cheesetama_release_gate_{Guid.NewGuid():N}");
            var outputPath = Path.Combine(root, "CheeseTama.exe");
            var sidecar = Path.Combine(
                root,
                "CheeseTama" + CheeseTamaBuildValidator.BurstDebugInformationSuffix);
            try
            {
                Directory.CreateDirectory(Path.Combine(sidecar, "Data", "Plugins"));
                File.WriteAllText(outputPath, "release");
                File.WriteAllText(Path.Combine(sidecar, "Data", "Plugins", "burst.txt"), "debug");

                CheeseTamaBuildValidator.FinalizeReleaseOutput(outputPath);

                Assert.That(File.Exists(outputPath), Is.True);
                Assert.That(Directory.Exists(sidecar), Is.False);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Test]
        public void ReleaseFinalizationRejectsUnexpectedDoNotShipOutput()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"cheesetama_release_gate_{Guid.NewGuid():N}");
            var outputPath = Path.Combine(root, "CheeseTama.exe");
            var unexpected = Path.Combine(
                root,
                "Other" + CheeseTamaBuildValidator.BurstDebugInformationSuffix);
            try
            {
                Directory.CreateDirectory(unexpected);
                File.WriteAllText(outputPath, "release");

                Assert.Throws<UnityEditor.Build.BuildFailedException>(() =>
                    CheeseTamaBuildValidator.FinalizeReleaseOutput(outputPath));
                Assert.That(Directory.Exists(unexpected), Is.True);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
