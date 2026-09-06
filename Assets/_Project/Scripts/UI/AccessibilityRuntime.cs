using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public static class AccessibilityRuntime
    {
        private static readonly Regex AbsoluteSizeTagPattern = new Regex(
            @"(?<prefix><size\s*=\s*)(?<size>\d+)(?<suffix>\s*>)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static event Action SettingsChanged;

        public static float TextScale { get; private set; } = GameSettingsSaveData.DefaultTextScale;
        public static bool HighContrast { get; private set; }
        public static bool ReducedMotion { get; private set; }
        public static float MotionScale => ReducedMotion ? 0f : 1f;

        public static void Apply(Transform root, GameSettingsSaveData settings)
        {
            settings ??= GameSettingsSaveData.CreateDefault();
            settings.EnsureRuntimeDefaults();

            TextScale = settings.textScale;
            HighContrast = settings.highContrastUi;
            ReducedMotion = settings.reduceMotion;

            if (root != null)
            {
                var shouldCreateProfiles = !Mathf.Approximately(
                        TextScale,
                        GameSettingsSaveData.DefaultTextScale)
                    || HighContrast;
                var labels = root.GetComponentsInChildren<Text>(true);
                foreach (var label in labels)
                {
                    if (label == null)
                    {
                        continue;
                    }

                    ApplyCurrent(label, shouldCreateProfiles);
                }
            }

            SettingsChanged?.Invoke();
        }

        public static void ApplyCurrent(Text label)
        {
            ApplyCurrent(
                label,
                !Mathf.Approximately(TextScale, GameSettingsSaveData.DefaultTextScale)
                || HighContrast);
        }

        public static void SetTextAndApply(Text label, string authoredRichText)
        {
            if (label == null)
            {
                return;
            }

            label.text = authoredRichText ?? string.Empty;
            ApplyCurrent(label);
        }

        public static string ScaleAbsoluteRichTextSizes(string authoredRichText, float textScale)
        {
            if (string.IsNullOrEmpty(authoredRichText))
            {
                return authoredRichText ?? string.Empty;
            }

            var scale = GameSettingsSaveData.NormalizeTextScale(textScale);
            if (Mathf.Approximately(scale, GameSettingsSaveData.DefaultTextScale))
            {
                return authoredRichText;
            }

            return AbsoluteSizeTagPattern.Replace(authoredRichText, match =>
            {
                if (!int.TryParse(
                        match.Groups["size"].Value,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var authoredSize))
                {
                    return match.Value;
                }

                var scaledSize = Mathf.Clamp(Mathf.RoundToInt(authoredSize * scale), 1, 1024);
                return match.Groups["prefix"].Value
                    + scaledSize.ToString(CultureInfo.InvariantCulture)
                    + match.Groups["suffix"].Value;
            });
        }

        private static void ApplyCurrent(Text label, bool shouldCreateProfile)
        {
            if (label == null)
            {
                return;
            }

            var profile = label.GetComponent<AccessibilityTextProfile>();
            if (profile == null)
            {
                if (!shouldCreateProfile)
                {
                    return;
                }

                profile = label.gameObject.AddComponent<AccessibilityTextProfile>();
            }

            profile.Apply(label, TextScale, HighContrast);
        }
    }
}
