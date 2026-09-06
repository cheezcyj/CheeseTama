using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class AdaptiveTouchHitArea : MonoBehaviour
    {
        public const string HitAreaObjectName = "Touch Hit Area";
        private static readonly Rect[] NoSiblingBounds = new Rect[0];

        private RectTransform sourceRect;
        private RectTransform hitAreaRect;
        private Image hitAreaImage;
        private LayoutElement hitAreaLayout;

        public RectTransform HitAreaRect => hitAreaRect;
        public TouchHitAreaInsets AppliedInsets { get; private set; }

        public void Configure(float preferredCanvasUnits)
        {
            sourceRect ??= transform as RectTransform;
            if (sourceRect == null)
            {
                return;
            }

            var sourceBounds = new Rect(
                0f,
                0f,
                sourceRect.rect.width,
                sourceRect.rect.height);
            Configure(TouchHitAreaLayout.Resolve(
                sourceBounds,
                NoSiblingBounds,
                preferredCanvasUnits));
        }

        public void Configure(TouchHitAreaInsets expansion)
        {
            AppliedInsets = expansion;
            if (!expansion.HasExpansion)
            {
                if (hitAreaRect != null)
                {
                    hitAreaRect.gameObject.SetActive(false);
                }

                return;
            }

            EnsureHitArea();
            hitAreaRect.anchorMin = Vector2.zero;
            hitAreaRect.anchorMax = Vector2.one;
            hitAreaRect.pivot = new Vector2(0.5f, 0.5f);
            hitAreaRect.offsetMin = new Vector2(-expansion.Left, -expansion.Bottom);
            hitAreaRect.offsetMax = new Vector2(expansion.Right, expansion.Top);
            hitAreaRect.SetAsFirstSibling();
            hitAreaRect.gameObject.SetActive(true);
        }

        private void EnsureHitArea()
        {
            if (hitAreaRect != null)
            {
                return;
            }

            var existing = transform.Find(HitAreaObjectName) as RectTransform;
            if (existing != null)
            {
                hitAreaRect = existing;
                hitAreaImage = existing.GetComponent<Image>();
            }
            else
            {
                var hitArea = new GameObject(
                    HitAreaObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(LayoutElement));
                hitArea.transform.SetParent(transform, false);
                hitAreaRect = hitArea.GetComponent<RectTransform>();
                hitAreaImage = hitArea.GetComponent<Image>();
            }

            if (hitAreaImage == null)
            {
                hitAreaImage = hitAreaRect.gameObject.AddComponent<Image>();
            }

            hitAreaImage.color = Color.clear;
            hitAreaImage.raycastTarget = true;
            hitAreaLayout = hitAreaRect.GetComponent<LayoutElement>();
            if (hitAreaLayout == null)
            {
                hitAreaLayout = hitAreaRect.gameObject.AddComponent<LayoutElement>();
            }

            hitAreaLayout.ignoreLayout = true;
        }
    }
}
