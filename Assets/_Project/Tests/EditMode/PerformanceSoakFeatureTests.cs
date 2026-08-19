using System;
using System.Collections.Generic;
using System.IO;
using CheeseTama.Environment;
using CheeseTama.Save;
using NUnit.Framework;

namespace CheeseTama.Tests.EditMode
{
    public sealed class PerformanceSoakFeatureTests
    {
        [Test]
        public void ConfigurationRequiresExplicitSwitchAndUsesThirtyMinuteDefault()
        {
            Assert.That(
                PerformanceSoakConfiguration.TryParse(Array.Empty<string>(), out _),
                Is.False);
            Assert.That(
                PerformanceSoakConfiguration.TryParse(
                    new[] { PerformanceSoakConfiguration.EnableArgument },
                    out var configuration),
                Is.True);
            Assert.That(
                configuration.DurationSeconds,
                Is.EqualTo(PerformanceSoakConfiguration.DefaultDurationSeconds));
        }

        [TestCase("1", PerformanceSoakConfiguration.MinimumDurationSeconds)]
        [TestCase("300", 300)]
        [TestCase("99999", PerformanceSoakConfiguration.MaximumDurationSeconds)]
        public void ConfigurationClampsDuration(string requested, int expected)
        {
            PerformanceSoakConfiguration.TryParse(
                new[]
                {
                    PerformanceSoakConfiguration.EnableArgument,
                    PerformanceSoakConfiguration.DurationArgument,
                    requested
                },
                out var configuration);

            Assert.That(configuration.DurationSeconds, Is.EqualTo(expected));
        }

        [Test]
        public void OutputRequiresJsonAndFallsBackInsideDefaultDirectory()
        {
            PerformanceSoakConfiguration.TryParse(
                new[]
                {
                    PerformanceSoakConfiguration.EnableArgument,
                    PerformanceSoakConfiguration.OutputArgument,
                    "report.txt"
                },
                out var configuration);

            var defaultDirectory = Path.Combine(Path.GetTempPath(), "CheeseTamaPerfTests");
            var output = configuration.ResolveOutputPath(defaultDirectory);
            Assert.That(Path.GetDirectoryName(output), Is.EqualTo(Path.GetFullPath(defaultDirectory)));
            Assert.That(Path.GetExtension(output), Is.EqualTo(".json"));
        }

        [Test]
        public void StatisticsCalculatePercentilesAndMemorySlopeWithoutMutatingSamples()
        {
            var samples = new List<PerformanceSoakSample>
            {
                CreateSample(0, 10, 100 * 1024 * 1024),
                CreateSample(30, 20, 110 * 1024 * 1024),
                CreateSample(60, 30, 120 * 1024 * 1024)
            };

            var summary = PerformanceSoakStatistics.Summarize("Low", samples);

            Assert.That(summary.sampleCount, Is.EqualTo(3));
            Assert.That(summary.cpuFrameP50Ms, Is.EqualTo(20d));
            Assert.That(summary.cpuFrameP95Ms, Is.EqualTo(29d).Within(0.0001d));
            Assert.That(summary.memoryGrowthMegabytesPerMinute, Is.EqualTo(20d).Within(0.0001d));
            Assert.That(samples[0].cpuFrameTimeMs, Is.EqualTo(10d));
        }

        [Test]
        public void StatisticsIgnoreUnsupportedOptionalTimingValues()
        {
            var samples = new List<PerformanceSoakSample>
            {
                CreateSample(0, 10, 100),
                CreateSample(1, 20, 110),
                CreateSample(2, 30, 120)
            };
            samples[0].cpuMainThreadFrameTimeMs = 0d;
            samples[1].cpuMainThreadFrameTimeMs = double.NaN;
            samples[2].cpuMainThreadFrameTimeMs = 12d;
            samples[0].gpuFrameTimeMs = double.PositiveInfinity;
            samples[1].gpuFrameTimeMs = 0d;
            samples[2].gpuFrameTimeMs = 6d;

            var summary = PerformanceSoakStatistics.Summarize("High", samples);

            Assert.That(summary.sampleCount, Is.EqualTo(3));
            Assert.That(summary.mainThreadP95Ms, Is.EqualTo(12d));
            Assert.That(summary.gpuP95Ms, Is.EqualTo(6d));
        }

        [TestCase(0, 16.6d, false)]
        [TestCase(1, 0d, false)]
        [TestCase(1, double.NaN, false)]
        [TestCase(1, double.PositiveInfinity, false)]
        [TestCase(1, 16.6d, true)]
        public void FrameTimingFilterRejectsUnsupportedSamples(
            int timingCount,
            double cpuFrameMilliseconds,
            bool expected)
        {
            Assert.That(
                PerformanceSoakStatistics.IsUsableFrameTiming(
                    timingCount,
                    cpuFrameMilliseconds),
                Is.EqualTo(expected));
        }

        [TestCase(0d, -1d)]
        [TestCase(double.NaN, -1d)]
        [TestCase(double.PositiveInfinity, -1d)]
        [TestCase(4.25d, 4.25d)]
        public void OptionalTimingNormalizationUsesNegativeUnavailableSentinel(
            double requested,
            double expected)
        {
            Assert.That(
                PerformanceSoakStatistics.NormalizeOptionalFrameTiming(requested),
                Is.EqualTo(expected));
        }

        [Test]
        public void RepresentativeStateCompletesBlockingFirstRunFlows()
        {
            var save = SaveManager.CreateDefaultSave();

            Assert.That(PerformanceSoakRepresentativeState.IsSeeded(save), Is.False);
            Assert.That(PerformanceSoakRepresentativeState.Seed(save), Is.True);
            Assert.That(PerformanceSoakRepresentativeState.IsSeeded(save), Is.True);
            Assert.That(save.onboarding.replaying, Is.False);
            Assert.That(save.newGameSetup.outcomeApplied, Is.True);
            Assert.That(save.firstDayJourney.introShown, Is.True);
            Assert.That(save.firstDayJourney.rewardClaimed, Is.True);
            Assert.That(
                PerformanceSoakRepresentativeState.Description,
                Is.EqualTo("representative-post-onboarding-milkroom"));
        }

        [Test]
        public void RepresentativeOverlayInventoryIncludesFirstRunAndEverydayModals()
        {
            Assert.That(
                PerformanceSoakRepresentativeState.BlockingOverlayNames,
                Does.Contain("First Meeting Onboarding Overlay"));
            Assert.That(
                PerformanceSoakRepresentativeState.BlockingOverlayNames,
                Does.Contain("New Game Setup Overlay"));
            Assert.That(
                PerformanceSoakRepresentativeState.BlockingOverlayNames,
                Does.Contain("First Day Journey Overlay"));
            Assert.That(
                PerformanceSoakRepresentativeState.BlockingOverlayNames,
                Does.Contain("Settings Modal"));
        }

        [Test]
        public void ReadinessReportsEveryMissingDependencyWithoutLocalPaths()
        {
            var failure = PerformanceSoakReadiness.DescribeMissingDependencies(
                hasPropDetailController: false,
                hasGameManager: true,
                hasCurrentSave: false);

            Assert.That(failure, Does.Contain(nameof(MilkroomPropDetailController)));
            Assert.That(failure, Does.Contain("GameManager.CurrentSave"));
            Assert.That(failure, Does.Not.Contain(":\\"));
            Assert.That(
                PerformanceSoakReadiness.DescribeMissingDependencies(true, true, true),
                Is.Empty);
            Assert.That(
                PerformanceSoakReadiness.DependencyTimeoutSeconds,
                Is.EqualTo(30f));
        }

        [Test]
        public void DiagnosticSaveOverrideAcceptsOnlyOwnedGuidFileNames()
        {
            var valid = SaveManager.CreateRuntimeDiagnosticSaveFileName();
            Assert.That(SaveManager.IsValidRuntimeDiagnosticSaveFileName(valid), Is.True);
            Assert.That(SaveManager.IsValidRuntimeDiagnosticSaveFileName("cheesetama_save.json"), Is.False);
            Assert.That(SaveManager.IsValidRuntimeDiagnosticSaveFileName("../" + valid), Is.False);
        }

        [Test]
        public void StaleOwnedRuntimeOverrideIsClearedFromCurrentSaveManagerState()
        {
            var previousOverride = SaveManager.RuntimeDiagnosticSaveFileNameOverride;
            var staleOverride = SaveManager.CreateRuntimeDiagnosticSaveFileName();
            try
            {
                PerformanceSoakDiagnosticIsolation.TryClearOwnedRuntimeOverride();
                SaveManager.SetRuntimeDiagnosticSaveFileNameOverride(staleOverride);

                Assert.That(
                    SaveManager.RuntimeDiagnosticSaveFileNameOverride,
                    Is.EqualTo(staleOverride));
                Assert.That(
                    PerformanceSoakDiagnosticIsolation.TryClearOwnedRuntimeOverride(),
                    Is.True);
                Assert.That(SaveManager.RuntimeDiagnosticSaveFileNameOverride, Is.Null);
                Assert.That(
                    PerformanceSoakDiagnosticIsolation.TryClearOwnedRuntimeOverride(),
                    Is.False);
            }
            finally
            {
                PerformanceSoakDiagnosticIsolation.TryClearOwnedRuntimeOverride();
                if (SaveManager.IsValidRuntimeDiagnosticSaveFileName(previousOverride))
                {
                    SaveManager.SetRuntimeDiagnosticSaveFileNameOverride(previousOverride);
                }
            }
        }

        private static PerformanceSoakSample CreateSample(
            double elapsed,
            double frameMilliseconds,
            long memoryBytes)
        {
            return new PerformanceSoakSample
            {
                elapsedSeconds = elapsed,
                cpuFrameTimeMs = frameMilliseconds,
                cpuMainThreadFrameTimeMs = frameMilliseconds * 0.5d,
                cpuRenderThreadFrameTimeMs = frameMilliseconds * 0.25d,
                gpuFrameTimeMs = frameMilliseconds * 0.125d,
                totalAllocatedMemoryBytes = memoryBytes,
                batches = 10,
                drawCalls = 11,
                triangles = 12,
                setPassCalls = 13,
                gcAllocatedInFrameBytes = 14
            };
        }
    }
}
