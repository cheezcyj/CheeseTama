using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class AccessibilityTextProfile : MonoBehaviour
    {
        [SerializeField] private bool initialized;
        [SerializeField] private int baseFontSize;
        [SerializeField] private int baseBestFitMinSize;
        [SerializeField] private int baseBestFitMaxSize;
        [SerializeField] private Outline generatedOutline;
        [SerializeField] private bool richTextInitialized;
        [SerializeField] private string baseRichText;
        [SerializeField] private string lastRenderedRichText;

        public int BaseFontSize => baseFontSize;

        public void Apply(Text target, float textScale, bool highContrast)
        {
            if (target == null)
            {
                return;
            }

            CaptureBaseline(target);
            var scale = GameSettingsSaveData.NormalizeTextScale(textScale);
            target.fontSize = Mathf.Max(1, Mathf.RoundToInt(baseFontSize * scale));
            if (target.resizeTextForBestFit)
            {
                target.resizeTextMinSize = Mathf.Max(1, Mathf.RoundToInt(baseBestFitMinSize * scale));
                target.resizeTextMaxSize = Mathf.Max(
                    target.resizeTextMinSize,
                    Mathf.RoundToInt(baseBestFitMaxSize * scale));
            }

            ApplyRichTextScale(target, scale);
            ApplyContrast(target, highContrast);
        }

        private void ApplyRichTextScale(Text target, float textScale)
        {
            if (!target.supportRichText)
            {
                return;
            }

            var current = target.text ?? string.Empty;
            if (!richTextInitialized || !string.Equals(current, lastRenderedRichText, StringComparison.Ordinal))
            {
                baseRichText = current;
                richTextInitialized = true;
            }

            lastRenderedRichText = AccessibilityRuntime.ScaleAbsoluteRichTextSizes(
                baseRichText,
                textScale);
            if (!string.Equals(target.text, lastRenderedRichText, StringComparison.Ordinal))
            {
                target.text = lastRenderedRichText;
            }
        }

        private void CaptureBaseline(Text target)
        {
            if (initialized)
            {
                return;
            }

            baseFontSize = Mathf.Max(1, target.fontSize);
            baseBestFitMinSize = Mathf.Max(1, target.resizeTextMinSize);
            baseBestFitMaxSize = Mathf.Max(baseBestFitMinSize, target.resizeTextMaxSize);
            initialized = true;
        }

        private void ApplyContrast(Text target, bool highContrast)
        {
            if (generatedOutline == null && highContrast)
            {
                generatedOutline = target.gameObject.AddComponent<Outline>();
                generatedOutline.useGraphicAlpha = true;
            }

            if (generatedOutline == null)
            {
                return;
            }

            generatedOutline.enabled = highContrast;
            if (!highContrast)
            {
                return;
            }

            var color = target.color;
            var luminance = (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);
            generatedOutline.effectColor = luminance >= 0.55f
                ? new Color(0.03f, 0.03f, 0.03f, 0.95f)
                : new Color(1f, 1f, 1f, 0.95f);
            generatedOutline.effectDistance = new Vector2(1.4f, -1.4f);
        }
    }

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
