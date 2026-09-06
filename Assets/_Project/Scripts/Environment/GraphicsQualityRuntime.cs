using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTama.Environment
{
    public enum GraphicsQualityPreset
    {
        Low = 0,
        Balanced = 1,
        High = 2
    }

    public readonly struct GraphicsQualityProfile
    {
        public GraphicsQualityProfile(
            GraphicsQualityPreset preset,
            string qualityLevelName,
            float propLowDetailHeight,
            float propCullHeight,
            bool propShadows)
        {
            Preset = preset;
            QualityLevelName = qualityLevelName ?? string.Empty;
            PropLowDetailHeight = Mathf.Clamp(propLowDetailHeight, 0.002f, 0.5f);
            PropCullHeight = Mathf.Clamp(propCullHeight, 0.001f, 0.2f);
            PropShadows = propShadows;
        }

        public GraphicsQualityPreset Preset { get; }
        public string QualityLevelName { get; }
        public float PropLowDetailHeight { get; }
        public float PropCullHeight { get; }
        public bool PropShadows { get; }
    }

    public static class GraphicsQualityCatalog
    {
        public static GraphicsQualityPreset Normalize(int value)
        {
            return value switch
            {
                // Keep the legacy numeric value readable, but never apply the
                // visibly degraded Low profile to current players.
                (int)GraphicsQualityPreset.Low => GraphicsQualityPreset.Balanced,
                (int)GraphicsQualityPreset.Balanced => GraphicsQualityPreset.Balanced,
                _ => GraphicsQualityPreset.High
            };
        }

        public static GraphicsQualityProfile Get(GraphicsQualityPreset preset)
        {
            return Normalize((int)preset) switch
            {
                GraphicsQualityPreset.Balanced => new GraphicsQualityProfile(
                    GraphicsQualityPreset.Balanced,
                    "High",
                    0.045f,
                    0.003f,
                    true),
                _ => new GraphicsQualityProfile(
                    GraphicsQualityPreset.High,
                    "Ultra",
                    0.01f,
                    0.0015f,
                    true)
            };
        }

        public static string GetDisplayName(GraphicsQualityPreset preset)
        {
            return Normalize((int)preset) switch
            {
                GraphicsQualityPreset.Balanced => "균형",
                _ => "고화질"
            };
        }

        public static int ResolveQualityLevelIndex(
            GraphicsQualityPreset preset,
            IReadOnlyList<string> qualityNames)
        {
            if (qualityNames == null || qualityNames.Count == 0)
            {
                return 0;
            }

            var normalized = Normalize((int)preset);
            var requestedName = Get(normalized).QualityLevelName;
            for (var index = 0; index < qualityNames.Count; index += 1)
            {
                if (string.Equals(qualityNames[index], requestedName, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return qualityNames.Count - 1;
        }
    }

    public static class GraphicsQualityRuntime
    {
        public static void Apply(GraphicsQualityPreset preset)
        {
            var normalized = GraphicsQualityCatalog.Normalize((int)preset);
            if (Application.isPlaying)
            {
                var qualityIndex = GraphicsQualityCatalog.ResolveQualityLevelIndex(
                    normalized,
                    QualitySettings.names);
                if (qualityIndex != QualitySettings.GetQualityLevel())
                {
                    QualitySettings.SetQualityLevel(qualityIndex, true);
                }
            }

            var controllers = UnityEngine.Object.FindObjectsByType<MilkroomPropDetailController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var index = 0; index < controllers.Length; index += 1)
            {
                controllers[index]?.ApplyPreset(normalized);
            }
        }
    }

}
