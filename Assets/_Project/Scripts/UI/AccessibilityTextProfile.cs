using System;
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
        public int BaseBestFitMinSize => baseBestFitMinSize;
        public int BaseBestFitMaxSize => baseBestFitMaxSize;

        public void Rebase(Text target)
        {
            if (target == null)
            {
                return;
            }

            baseFontSize = Mathf.Max(1, target.fontSize);
            baseBestFitMinSize = Mathf.Max(1, target.resizeTextMinSize);
            baseBestFitMaxSize = Mathf.Max(baseBestFitMinSize, target.resizeTextMaxSize);
            initialized = true;
            richTextInitialized = false;
            baseRichText = target.text ?? string.Empty;
            lastRenderedRichText = string.Empty;
        }

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
}
