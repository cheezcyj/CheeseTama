using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CheeseTama.Core;
using CheeseTama.Save;
using CheeseTama.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace CheeseTama.Environment
{
    public sealed class PerformanceSoakConfiguration
    {
        public const string EnableArgument = "-cheesetama-performance-soak";
        public const string DurationArgument = "-cheesetama-soak-seconds";
        public const string OutputArgument = "-cheesetama-performance-output";
        public const int DefaultDurationSeconds = 1800;
        public const int MinimumDurationSeconds = 30;
        public const int MaximumDurationSeconds = 7200;

        public int DurationSeconds { get; private set; }
        public string RequestedOutputPath { get; private set; }

        public static bool TryParse(IReadOnlyList<string> arguments, out PerformanceSoakConfiguration configuration)
        {
            configuration = null;
            if (arguments == null || IndexOf(arguments, EnableArgument) < 0)
            {
                return false;
            }

            var duration = DefaultDurationSeconds;
            var durationIndex = IndexOf(arguments, DurationArgument);
            if (durationIndex >= 0
                && durationIndex + 1 < arguments.Count
                && int.TryParse(
                    arguments[durationIndex + 1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedDuration))
            {
                duration = Mathf.Clamp(
                    parsedDuration,
                    MinimumDurationSeconds,
                    MaximumDurationSeconds);
            }

            var outputPath = string.Empty;
            var outputIndex = IndexOf(arguments, OutputArgument);
            if (outputIndex >= 0 && outputIndex + 1 < arguments.Count)
            {
                outputPath = arguments[outputIndex + 1]?.Trim() ?? string.Empty;
            }

            configuration = new PerformanceSoakConfiguration
            {
                DurationSeconds = duration,
                RequestedOutputPath = outputPath
            };
            return true;
        }

        public string ResolveOutputPath(string defaultDirectory)
        {
            if (!string.IsNullOrWhiteSpace(RequestedOutputPath)
                && string.Equals(
                    Path.GetExtension(RequestedOutputPath),
                    ".json",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(RequestedOutputPath);
            }

            var safeDirectory = string.IsNullOrWhiteSpace(defaultDirectory)
                ? Path.GetTempPath()
                : Path.GetFullPath(defaultDirectory);
            return Path.Combine(
                safeDirectory,
                $"CheeseTama_Performance_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        }

        private static int IndexOf(IReadOnlyList<string> arguments, string expected)
        {
            for (var index = 0; index < arguments.Count; index += 1)
            {
                if (string.Equals(arguments[index], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }
    }

    [Serializable]
    public sealed class PerformanceSoakSample
    {
        public string preset = string.Empty;
        public string uiScenario = string.Empty;
        public double elapsedSeconds;
        public double cpuFrameTimeMs;
        public double cpuMainThreadFrameTimeMs;
        public double cpuRenderThreadFrameTimeMs;
        public double gpuFrameTimeMs;
        public long totalAllocatedMemoryBytes;
        public long monoUsedMemoryBytes;
        public long gcAllocatedInFrameBytes;
        public long batches;
        public long drawCalls;
        public long triangles;
        public long setPassCalls;
    }

    [Serializable]
    public sealed class PerformanceSoakPhaseSummary
    {
        public string preset = string.Empty;
        public int sampleCount;
        public double durationSeconds;
        public double cpuFrameP50Ms;
        public double cpuFrameP95Ms;
        public double mainThreadP95Ms;
        public double renderThreadP95Ms;
        public double gpuP95Ms;
        public double averageBatches;
        public double averageDrawCalls;
        public double averageTriangles;
        public double averageSetPassCalls;
        public double averageGcAllocatedPerSampleBytes;
        public double memoryGrowthMegabytesPerMinute;
        public long peakAllocatedMemoryBytes;
    }

    [Serializable]
    public sealed class PerformanceSoakReport
    {
        public string schema = "cheesetama-performance-soak-v2";
        public string startedAtUtc = string.Empty;
        public string completedAtUtc = string.Empty;
        public string unityVersion = string.Empty;
        public string platform = string.Empty;
        public string operatingSystem = string.Empty;
        public string processor = string.Empty;
        public string graphicsDevice = string.Empty;
        public int processorCount;
        public int systemMemoryMegabytes;
        public int graphicsMemoryMegabytes;
        public int screenWidth;
        public int screenHeight;
        public int targetFrameRate;
        public int requestedDurationSeconds;
        public int attemptedFrameTimingSamples;
        public int droppedFrameTimingSamples;
        public bool representativeStateSeeded;
        public string representativeState = string.Empty;
        public int blockingOverlaysClosed;
        public bool completed;
        public string failure = string.Empty;
        public List<PerformanceSoakPhaseSummary> phases = new List<PerformanceSoakPhaseSummary>();
        public List<PerformanceSoakSample> samples = new List<PerformanceSoakSample>();
    }

    public static class PerformanceSoakStatistics
    {
        public static bool IsUsableFrameTiming(int timingCount, double cpuFrameTimeMilliseconds)
        {
            return timingCount > 0
                && !double.IsNaN(cpuFrameTimeMilliseconds)
                && !double.IsInfinity(cpuFrameTimeMilliseconds)
                && cpuFrameTimeMilliseconds > 0d;
        }

        public static double NormalizeOptionalFrameTiming(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d
                ? value
                : -1d;
        }

        public static PerformanceSoakPhaseSummary Summarize(
            string preset,
            IReadOnlyList<PerformanceSoakSample> samples)
        {
            var summary = new PerformanceSoakPhaseSummary
            {
                preset = preset ?? string.Empty
            };
            if (samples == null || samples.Count == 0)
            {
                return summary;
            }

            var frame = new List<double>(samples.Count);
            var main = new List<double>(samples.Count);
            var render = new List<double>(samples.Count);
            var gpu = new List<double>(samples.Count);
            long peakMemory = 0;
            double batches = 0;
            double drawCalls = 0;
            double triangles = 0;
            double setPass = 0;
            double gcAllocated = 0;
            for (var index = 0; index < samples.Count; index += 1)
            {
                var sample = samples[index];
                AddSupportedTiming(frame, sample.cpuFrameTimeMs);
                AddSupportedTiming(main, sample.cpuMainThreadFrameTimeMs);
                AddSupportedTiming(render, sample.cpuRenderThreadFrameTimeMs);
                AddSupportedTiming(gpu, sample.gpuFrameTimeMs);
                batches += Math.Max(0, sample.batches);
                drawCalls += Math.Max(0, sample.drawCalls);
                triangles += Math.Max(0, sample.triangles);
                setPass += Math.Max(0, sample.setPassCalls);
                gcAllocated += Math.Max(0, sample.gcAllocatedInFrameBytes);
                peakMemory = Math.Max(peakMemory, sample.totalAllocatedMemoryBytes);
            }

            var first = samples[0];
            var last = samples[samples.Count - 1];
            var duration = Math.Max(0d, last.elapsedSeconds - first.elapsedSeconds);
            summary.sampleCount = samples.Count;
            summary.durationSeconds = duration;
            summary.cpuFrameP50Ms = Percentile(frame, 0.50d);
            summary.cpuFrameP95Ms = Percentile(frame, 0.95d);
            summary.mainThreadP95Ms = Percentile(main, 0.95d);
            summary.renderThreadP95Ms = Percentile(render, 0.95d);
            summary.gpuP95Ms = Percentile(gpu, 0.95d);
            summary.averageBatches = batches / samples.Count;
            summary.averageDrawCalls = drawCalls / samples.Count;
            summary.averageTriangles = triangles / samples.Count;
            summary.averageSetPassCalls = setPass / samples.Count;
            summary.averageGcAllocatedPerSampleBytes = gcAllocated / samples.Count;
            summary.peakAllocatedMemoryBytes = peakMemory;
            if (duration > 0d)
            {
                var memoryDelta = last.totalAllocatedMemoryBytes - first.totalAllocatedMemoryBytes;
                summary.memoryGrowthMegabytesPerMinute =
                    memoryDelta / (1024d * 1024d) / (duration / 60d);
            }

            return summary;
        }

        private static void AddSupportedTiming(List<double> destination, double value)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value) && value > 0d)
            {
                destination.Add(value);
            }
        }

        public static double Percentile(IReadOnlyList<double> values, double percentile)
        {
            if (values == null || values.Count == 0)
            {
                return 0d;
            }

            var sorted = new List<double>(values);
            sorted.Sort();
            var clamped = Math.Max(0d, Math.Min(1d, percentile));
            var position = (sorted.Count - 1) * clamped;
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);
            if (lower == upper)
            {
                return sorted[lower];
            }

            var weight = position - lower;
            return sorted[lower] + ((sorted[upper] - sorted[lower]) * weight);
        }
    }

    public static class PerformanceSoakRepresentativeState
    {
        public const string Description = "representative-post-onboarding-milkroom";

        private static readonly string[] BlockingOverlayObjectNames =
        {
            "New Game Setup Overlay",
            "First Meeting Onboarding Overlay",
            "Return Summary Overlay",
            "Growth Milestone Overlay",
            "Evolution Milestone Overlay",
            "Care Event Overlay",
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Confirm Reset Dialog",
            "Decorate Overlay",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "First Day Journey Overlay",
            "Cheese Star Delivery Overlay",
            "Memory Journal Overlay",
            "Fantasy Powder Overlay",
            "Save Recovery Notice Overlay",
            "CheeseTama Profile Overlay",
            "Input Bindings Overlay",
            "Milk Blending Overlay",
            "Cooking Choice Overlay",
            "Npc Visit Overlay",
            "Journey Hub Overlay",
            "Life Records Overlay",
            "Sleep Schedule Overlay",
            "Growth Journey Overlay",
            "Play Choice Overlay",
            "Bouncy Jump Overlay",
            "Cleaning Mini Game Overlay",
            "Star Legacy Overlay",
            "Hidden Career Card Overlay",
            "Accessibility Panel",
            "Cloud Save Overlay"
        };

        public static IReadOnlyList<string> BlockingOverlayNames => BlockingOverlayObjectNames;

        public static bool Seed(CheeseTamaSaveData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.EnsureRuntimeDefaults();
            saveData.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
            saveData.newGameSetup = NewGameSetupSaveData.CreateCompletedForLegacySave();
            saveData.firstDayJourney = FirstDayJourneySaveData.CreateCompletedForLegacySave();
            saveData.EnsureRuntimeDefaults();
            return IsSeeded(saveData);
        }

        public static bool IsSeeded(CheeseTamaSaveData saveData)
        {
            return saveData?.onboarding?.completed == true
                && saveData.onboarding.currentStep == FirstMeetingOnboardingStep.Complete
                && !saveData.onboarding.replaying
                && saveData.newGameSetup?.completed == true
                && saveData.newGameSetup.outcomeApplied
                && saveData.firstDayJourney?.completed == true
                && saveData.firstDayJourney.introShown
                && saveData.firstDayJourney.rewardClaimed;
        }
    }

    public static class PerformanceSoakReadiness
    {
        public const float DependencyTimeoutSeconds = 30f;

        public static string DescribeMissingDependencies(
            bool hasPropDetailController,
            bool hasGameManager,
            bool hasCurrentSave)
        {
            var missing = new List<string>();
            if (!hasPropDetailController)
            {
                missing.Add(nameof(MilkroomPropDetailController));
            }

            if (!hasGameManager)
            {
                missing.Add(nameof(GameManager));
            }
            else if (!hasCurrentSave)
            {
                missing.Add("GameManager.CurrentSave");
            }

            return missing.Count == 0
                ? string.Empty
                : "Timed out waiting for required runtime dependencies: "
                    + string.Join(", ", missing)
                    + ".";
        }
    }

    public static class PerformanceSoakDiagnosticIsolation
    {
        public static bool TryClearOwnedRuntimeOverride()
        {
            var currentOverride = SaveManager.RuntimeDiagnosticSaveFileNameOverride;
            if (!SaveManager.IsValidRuntimeDiagnosticSaveFileName(currentOverride))
            {
                return false;
            }

            SaveManager.ClearRuntimeDiagnosticSaveFileNameOverride(currentOverride);
            return string.IsNullOrEmpty(SaveManager.RuntimeDiagnosticSaveFileNameOverride);
        }
    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    public sealed class PerformanceSoakRunner : MonoBehaviour
    {
        private const int WarmupSeconds = 30;
        private const float SampleIntervalSeconds = 1f;
        private const float UiScenarioIntervalSeconds = 15f;

        private static readonly GraphicsQualityPreset[] PhasePresets =
        {
            GraphicsQualityPreset.High,
            GraphicsQualityPreset.Balanced,
            GraphicsQualityPreset.Low
        };

        private static PerformanceSoakConfiguration pendingConfiguration;
        private static string diagnosticSaveFileName;

        private PerformanceSoakReport report;
        private string outputPath;
        private float startedAt;
        private float warmupDuration;
        private float nextSampleAt;
        private float nextUiScenarioAt;
        private int uiScenarioIndex;
        private GraphicsQualityPreset activePreset;
        private string currentUiScenario = "MilkroomIdle";
        private readonly FrameTiming[] frameTimings = new FrameTiming[1];
        private ProfilerRecorder batchesRecorder;
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder trianglesRecorder;
        private ProfilerRecorder setPassRecorder;
        private ProfilerRecorder gcAllocatedRecorder;
        private bool finalizationStarted;
        private bool reportFinalized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void PrepareDiagnosticSaveIsolation()
        {
            // SubsystemRegistration also runs when entering play mode without a domain reload.
            // Release whichever owned diagnostic slot is actually active before GameManager
            // can load a normal play session; do not rely on this runner's cached name.
            PerformanceSoakDiagnosticIsolation.TryClearOwnedRuntimeOverride();
            pendingConfiguration = null;
            diagnosticSaveFileName = null;
            if (!PerformanceSoakConfiguration.TryParse(
                    System.Environment.GetCommandLineArgs(),
                    out pendingConfiguration))
            {
                return;
            }

            diagnosticSaveFileName = SaveManager.CreateRuntimeDiagnosticSaveFileName();
            SaveManager.SetRuntimeDiagnosticSaveFileNameOverride(diagnosticSaveFileName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (pendingConfiguration == null
                || UnityEngine.Object.FindFirstObjectByType<PerformanceSoakRunner>() != null)
            {
                return;
            }

            var root = new GameObject("CheeseTama Performance Soak");
            DontDestroyOnLoad(root);
            root.AddComponent<PerformanceSoakRunner>();
        }

        private IEnumerator Start()
        {
            yield return null;
            InitializeReport();

            if (!string.Equals(
                    SceneManager.GetActiveScene().name,
                    "Milkroom",
                    StringComparison.Ordinal))
            {
                AsyncOperation loadOperation = null;
                string sceneLoadFailure = null;
                try
                {
                    loadOperation = SceneManager.LoadSceneAsync("Milkroom", LoadSceneMode.Single);
                    if (loadOperation == null)
                    {
                        sceneLoadFailure = "Failed to start loading the Milkroom scene.";
                    }
                }
                catch (Exception exception)
                {
                    sceneLoadFailure = "Milkroom scene loading failed ("
                        + exception.GetType().Name
                        + ").";
                }

                if (!string.IsNullOrEmpty(sceneLoadFailure))
                {
                    yield return FinishAndExit(false, sceneLoadFailure);
                    yield break;
                }

                while (!loadOperation.isDone)
                {
                    yield return null;
                }

                // Let the runtime builder bind the newly loaded scene before dependency polling.
                yield return null;
            }

            MilkroomPropDetailController propDetailController = null;
            GameManager manager = null;
            var dependencyDeadline = Time.realtimeSinceStartup
                + PerformanceSoakReadiness.DependencyTimeoutSeconds;
            while (Time.realtimeSinceStartup < dependencyDeadline)
            {
                propDetailController = UnityEngine.Object.FindFirstObjectByType<
                    MilkroomPropDetailController>(FindObjectsInactive.Include);
                manager = GameManager.Instance
                    ?? UnityEngine.Object.FindFirstObjectByType<GameManager>(
                        FindObjectsInactive.Include);
                if (propDetailController != null && manager?.CurrentSave != null)
                {
                    break;
                }

                yield return null;
            }

            var dependencyFailure = PerformanceSoakReadiness.DescribeMissingDependencies(
                propDetailController != null,
                manager != null,
                manager?.CurrentSave != null);
            if (!string.IsNullOrEmpty(dependencyFailure))
            {
                yield return FinishAndExit(false, dependencyFailure);
                yield break;
            }

            string setupFailure = null;
            try
            {
                report.representativeStateSeeded =
                    PerformanceSoakRepresentativeState.Seed(manager.CurrentSave);
                if (!report.representativeStateSeeded)
                {
                    setupFailure = "Failed to seed the representative post-onboarding state.";
                }
                else
                {
                    manager.SaveGame();
                    report.blockingOverlaysClosed = CloseBlockingOverlays();
                    BeginCapture();
                }
            }
            catch (Exception exception)
            {
                setupFailure = "Representative-state setup failed ("
                    + exception.GetType().Name
                    + ").";
            }

            if (!string.IsNullOrEmpty(setupFailure))
            {
                yield return FinishAndExit(false, setupFailure);
                yield break;
            }

            var capture = RunCapture();
            string captureFailure = null;
            while (true)
            {
                bool hasNext;
                object yielded = null;
                try
                {
                    hasNext = capture.MoveNext();
                    if (hasNext)
                    {
                        yielded = capture.Current;
                    }
                }
                catch (Exception exception)
                {
                    captureFailure = "Capture interrupted ("
                        + exception.GetType().Name
                        + ").";
                    break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return yielded;
            }

            if (string.IsNullOrEmpty(captureFailure) && report.samples.Count == 0)
            {
                captureFailure = "No supported frame-timing samples were captured.";
            }

            yield return FinishAndExit(string.IsNullOrEmpty(captureFailure), captureFailure);
        }

        private void InitializeReport()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            var defaultOutputDirectory = Path.Combine(
                Application.temporaryCachePath,
                "CheeseTamaPerformance");
            try
            {
                outputPath = pendingConfiguration.ResolveOutputPath(defaultOutputDirectory);
            }
            catch (Exception)
            {
                outputPath = Path.Combine(
                    defaultOutputDirectory,
                    $"CheeseTama_Performance_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            }

            report = new PerformanceSoakReport
            {
                startedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                processor = SystemInfo.processorType,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                processorCount = SystemInfo.processorCount,
                systemMemoryMegabytes = SystemInfo.systemMemorySize,
                graphicsMemoryMegabytes = SystemInfo.graphicsMemorySize,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                targetFrameRate = Application.targetFrameRate,
                requestedDurationSeconds = pendingConfiguration.DurationSeconds,
                representativeState = PerformanceSoakRepresentativeState.Description
            };
        }

        private void BeginCapture()
        {
            startedAt = Time.realtimeSinceStartup;
            warmupDuration = Mathf.Min(
                WarmupSeconds,
                pendingConfiguration.DurationSeconds * 0.1f);
            nextSampleAt = startedAt + warmupDuration;
            nextUiScenarioAt = startedAt + warmupDuration;

            batchesRecorder = StartRecorder(ProfilerCategory.Render, "Batches Count");
            drawCallsRecorder = StartRecorder(ProfilerCategory.Render, "Draw Calls Count");
            trianglesRecorder = StartRecorder(ProfilerCategory.Render, "Triangles Count");
            setPassRecorder = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
            gcAllocatedRecorder = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
            activePreset = GraphicsQualityPreset.High;
            ApplyMeasuredPreset(activePreset);
            Debug.Log(
                $"CheeseTama performance soak started ({pendingConfiguration.DurationSeconds}s). "
                + "The gameplay save is isolated from the normal player save.");
        }

        private IEnumerator RunCapture()
        {
            var duration = pendingConfiguration.DurationSeconds;
            var measurableDuration = Mathf.Max(3f, duration - warmupDuration);
            var phaseDuration = measurableDuration / (float)PhasePresets.Length;
            while (Time.realtimeSinceStartup - startedAt < duration)
            {
                FrameTimingManager.CaptureFrameTimings();
                var elapsed = Time.realtimeSinceStartup - startedAt;
                if (elapsed >= warmupDuration)
                {
                    var phaseIndex = Mathf.Min(
                        PhasePresets.Length - 1,
                        Mathf.FloorToInt((elapsed - warmupDuration) / phaseDuration));
                    var desiredPreset = PhasePresets[phaseIndex];
                    if (activePreset != desiredPreset)
                    {
                        activePreset = desiredPreset;
                        ApplyMeasuredPreset(desiredPreset);
                    }

                    if (Time.realtimeSinceStartup >= nextUiScenarioAt)
                    {
                        ExerciseRepresentativeUi();
                        nextUiScenarioAt += UiScenarioIntervalSeconds;
                    }

                    if (Time.realtimeSinceStartup >= nextSampleAt)
                    {
                        CaptureSample(desiredPreset, elapsed);
                        nextSampleAt += SampleIntervalSeconds;
                    }
                }

                yield return null;
            }
        }

        private static void ApplyMeasuredPreset(GraphicsQualityPreset preset)
        {
            GraphicsQualityRuntime.Apply(preset);
            // QualitySettings.SetQualityLevel also applies each tier's vSync value. Keep the
            // measurement frame policy identical so the comparison reflects rendering cost.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        private void CaptureSample(GraphicsQualityPreset preset, double elapsed)
        {
            report.attemptedFrameTimingSamples += 1;
            var timingCount = FrameTimingManager.GetLatestTimings(1, frameTimings);
            var timing = timingCount > 0 ? frameTimings[0] : default;
            if (!PerformanceSoakStatistics.IsUsableFrameTiming(
                    (int)timingCount,
                    timing.cpuFrameTime))
            {
                report.droppedFrameTimingSamples += 1;
                return;
            }

            report.samples.Add(new PerformanceSoakSample
            {
                preset = preset.ToString(),
                uiScenario = currentUiScenario,
                elapsedSeconds = elapsed,
                cpuFrameTimeMs = timing.cpuFrameTime,
                cpuMainThreadFrameTimeMs = PerformanceSoakStatistics.NormalizeOptionalFrameTiming(
                    timing.cpuMainThreadFrameTime),
                cpuRenderThreadFrameTimeMs = PerformanceSoakStatistics.NormalizeOptionalFrameTiming(
                    timing.cpuRenderThreadFrameTime),
                gpuFrameTimeMs = PerformanceSoakStatistics.NormalizeOptionalFrameTiming(
                    timing.gpuFrameTime),
                totalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong(),
                monoUsedMemoryBytes = Profiler.GetMonoUsedSizeLong(),
                gcAllocatedInFrameBytes = ReadRecorder(gcAllocatedRecorder),
                batches = ReadRecorder(batchesRecorder),
                drawCalls = ReadRecorder(drawCallsRecorder),
                triangles = ReadRecorder(trianglesRecorder),
                setPassCalls = ReadRecorder(setPassRecorder)
            });
        }

        private static int CloseBlockingOverlays()
        {
            RefreshFirstRunControllers();
            UnityEngine.Object.FindFirstObjectByType<FirstDayJourneyController>(
                FindObjectsInactive.Include)?.Close();
            UnityEngine.Object.FindFirstObjectByType<MilkPanelController>(
                FindObjectsInactive.Include)?.Close();
            UnityEngine.Object.FindFirstObjectByType<SnackPanelController>(
                FindObjectsInactive.Include)?.Close();
            UnityEngine.Object.FindFirstObjectByType<JourneyHubPanelController>(
                FindObjectsInactive.Include)?.Close();
            UnityEngine.Object.FindFirstObjectByType<LifeRecordsPanelController>(
                FindObjectsInactive.Include)?.Close();
            UnityEngine.Object.FindFirstObjectByType<SleepSchedulePanelController>(
                FindObjectsInactive.Include)?.Close();

            var names = new HashSet<string>(
                PerformanceSoakRepresentativeState.BlockingOverlayNames,
                StringComparer.Ordinal);
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var closed = 0;
            for (var index = 0; index < transforms.Length; index += 1)
            {
                var candidate = transforms[index];
                if (candidate == null
                    || !candidate.gameObject.activeSelf
                    || !names.Contains(candidate.name))
                {
                    continue;
                }

                candidate.gameObject.SetActive(false);
                closed += 1;
            }

            return closed;
        }

        private static void RefreshFirstRunControllers()
        {
            var newGameSetup = UnityEngine.Object.FindFirstObjectByType<NewGameSetupController>(
                FindObjectsInactive.Include);
            newGameSetup?.Refresh();

            var onboarding = UnityEngine.Object.FindFirstObjectByType<
                FirstMeetingOnboardingController>(FindObjectsInactive.Include);
            if (onboarding != null && onboarding.enabled)
            {
                // The controller's disable path restores suspended controls and its enable
                // path re-reads the now-completed representative save.
                onboarding.enabled = false;
                onboarding.enabled = true;
            }
        }

        private void ExerciseRepresentativeUi()
        {
            var milk = UnityEngine.Object.FindFirstObjectByType<MilkPanelController>(
                FindObjectsInactive.Include);
            var snack = UnityEngine.Object.FindFirstObjectByType<SnackPanelController>(
                FindObjectsInactive.Include);
            var journey = UnityEngine.Object.FindFirstObjectByType<JourneyHubPanelController>(
                FindObjectsInactive.Include);
            milk?.Close();
            snack?.Close();
            journey?.Close();

            uiScenarioIndex = (uiScenarioIndex + 1) % 4;
            switch (uiScenarioIndex)
            {
                case 1:
                    milk?.Open();
                    currentUiScenario = "MilkPanel";
                    break;
                case 2:
                    snack?.Open();
                    currentUiScenario = "SnackPanel";
                    break;
                case 3:
                    journey?.Open();
                    currentUiScenario = "JourneyHub";
                    break;
                default:
                    currentUiScenario = "MilkroomIdle";
                    break;
            }
        }

        private IEnumerator FinishAndExit(bool completed, string failure)
        {
            if (finalizationStarted)
            {
                yield break;
            }

            finalizationStarted = true;
            DisposeRecorders();

            // GameManager saves from OnApplicationQuit. Remove only that component before
            // releasing the diagnostic override so the representative save can never be
            // written into the normal player slot during shutdown.
            var manager = GameManager.Instance
                ?? UnityEngine.Object.FindFirstObjectByType<GameManager>(
                    FindObjectsInactive.Include);
            if (manager != null)
            {
                manager.enabled = false;
                Destroy(manager);
                yield return null;
            }

            var cleanupFailure = CleanupDiagnosticSave(clearOverride: true);
            var finalFailure = CombineFailures(failure, cleanupFailure);
            CompleteCapture(completed && string.IsNullOrEmpty(cleanupFailure), finalFailure);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(report != null && report.completed ? 0 : 2);
#endif
        }

        private void CompleteCapture(bool completed, string failure)
        {
            if (report == null || reportFinalized)
            {
                return;
            }

            reportFinalized = true;
            report.completed = completed;
            report.failure = failure ?? string.Empty;
            report.completedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            report.phases.Clear();
            try
            {
                for (var presetIndex = 0; presetIndex < PhasePresets.Length; presetIndex += 1)
                {
                    var presetName = PhasePresets[presetIndex].ToString();
                    var phaseSamples = new List<PerformanceSoakSample>();
                    for (var sampleIndex = 0; sampleIndex < report.samples.Count; sampleIndex += 1)
                    {
                        if (string.Equals(
                                report.samples[sampleIndex].preset,
                                presetName,
                                StringComparison.Ordinal))
                        {
                            phaseSamples.Add(report.samples[sampleIndex]);
                        }
                    }

                    report.phases.Add(PerformanceSoakStatistics.Summarize(presetName, phaseSamples));
                }

                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
                Debug.Log($"CheeseTama performance soak report: {outputPath}");
            }
            catch (Exception exception)
            {
                report.completed = false;
                report.failure = CombineFailures(
                    report.failure,
                    "Report finalization failed (" + exception.GetType().Name + ").");
                Debug.LogError("CheeseTama performance soak report finalization failed ("
                    + exception.GetType().Name
                    + ").");
            }
            finally
            {
                DisposeRecorders();
            }
        }

        private string CleanupDiagnosticSave(bool clearOverride)
        {
            var failures = new List<string>();
            try
            {
                if (SaveManager.IsValidRuntimeDiagnosticSaveFileName(diagnosticSaveFileName)
                    && string.Equals(
                        SaveManager.RuntimeDiagnosticSaveFileNameOverride,
                        diagnosticSaveFileName,
                        StringComparison.Ordinal))
                {
                    var saveManager = UnityEngine.Object.FindFirstObjectByType<SaveManager>(
                        FindObjectsInactive.Include);
                    saveManager?.DeleteSave();
                }
            }
            catch (Exception exception)
            {
                failures.Add("Diagnostic save-manager cleanup failed ("
                    + exception.GetType().Name
                    + ").");
            }

            DeleteOwnedDiagnosticFiles(failures);
            if (clearOverride)
            {
                try
                {
                    PerformanceSoakDiagnosticIsolation.TryClearOwnedRuntimeOverride();
                }
                catch (Exception exception)
                {
                    failures.Add("Diagnostic save override cleanup failed ("
                        + exception.GetType().Name
                        + ").");
                }
            }

            return failures.Count == 0 ? string.Empty : string.Join(" ", failures);
        }

        private static void DeleteOwnedDiagnosticFiles(List<string> failures)
        {
            if (!SaveManager.IsValidRuntimeDiagnosticSaveFileName(diagnosticSaveFileName))
            {
                return;
            }

            try
            {
                var directory = Path.GetFullPath(Application.persistentDataPath);
                var primaryPath = Path.GetFullPath(Path.Combine(directory, diagnosticSaveFileName));
                if (!string.Equals(
                        Path.GetDirectoryName(primaryPath),
                        directory,
                        StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add("Diagnostic save cleanup rejected an unexpected path.");
                    return;
                }

                DeleteFileIfPresent(primaryPath);
                DeleteFileIfPresent(primaryPath + ".bak");
                DeleteFileIfPresent(primaryPath + ".tmp");
                if (Directory.Exists(directory))
                {
                    var corruptFiles = Directory.GetFiles(
                        directory,
                        diagnosticSaveFileName + "*.corrupt.*");
                    for (var index = 0; index < corruptFiles.Length; index += 1)
                    {
                        var corruptPath = Path.GetFullPath(corruptFiles[index]);
                        if (string.Equals(
                                Path.GetDirectoryName(corruptPath),
                                directory,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            DeleteFileIfPresent(corruptPath);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                failures.Add("Owned diagnostic-file cleanup failed ("
                    + exception.GetType().Name
                    + ").");
            }
        }

        private static void DeleteFileIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void OnApplicationQuit()
        {
            var cleanupFailure = CleanupDiagnosticSave(clearOverride: false);
            if (!reportFinalized)
            {
                CompleteCapture(
                    false,
                    CombineFailures("Application quit before capture completed.", cleanupFailure));
            }
        }

        private void OnDestroy()
        {
            var canReleaseOverride = !Application.isPlaying || GameManager.Instance == null;
            var cleanupFailure = CleanupDiagnosticSave(clearOverride: canReleaseOverride);
            if (!reportFinalized)
            {
                CompleteCapture(
                    false,
                    CombineFailures("Performance soak runner was destroyed before completion.", cleanupFailure));
            }

            DisposeRecorders();
        }

        private static string CombineFailures(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first))
            {
                return second ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(second) ? first : first + " " + second;
        }

        private void DisposeRecorders()
        {
            DisposeRecorder(ref batchesRecorder);
            DisposeRecorder(ref drawCallsRecorder);
            DisposeRecorder(ref trianglesRecorder);
            DisposeRecorder(ref setPassRecorder);
            DisposeRecorder(ref gcAllocatedRecorder);
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            try
            {
                if (recorder.Valid)
                {
                    recorder.Dispose();
                }
            }
            catch (Exception)
            {
                // Cleanup is best-effort and must not obscure the report or block shutdown.
            }

            recorder = default;
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string counterName)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, counterName);
            }
            catch (Exception)
            {
                return default;
            }
        }

        private static long ReadRecorder(ProfilerRecorder recorder)
        {
            try
            {
                return recorder.Valid ? Math.Max(0, recorder.LastValue) : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
#endif
}
