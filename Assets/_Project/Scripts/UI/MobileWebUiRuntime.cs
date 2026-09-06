using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public enum ResponsiveUiProfileKind
    {
        WideLandscape,
        CompactLandscape,
        PortraitBlocked
    }

    public readonly struct ResponsiveUiProfile
    {
        public ResponsiveUiProfile(
            ResponsiveUiProfileKind kind,
            float canvasMatchWidthOrHeight,
            float preferredTouchTargetPixels,
            bool showLandscapePrompt)
        {
            Kind = kind;
            CanvasMatchWidthOrHeight = canvasMatchWidthOrHeight;
            PreferredTouchTargetPixels = preferredTouchTargetPixels;
            ShowLandscapePrompt = showLandscapePrompt;
        }

        public ResponsiveUiProfileKind Kind { get; }
        public float CanvasMatchWidthOrHeight { get; }
        public float PreferredTouchTargetPixels { get; }
        public bool ShowLandscapePrompt { get; }
    }

    public static class ResponsiveUiLayoutPolicy
    {
        public const float PreferredTouchTargetPixels = 48f;
        private const float ReferenceLandscapeAspect = 16f / 9f;
        private const float AspectTolerance = 0.001f;

        public static ResponsiveUiProfile Resolve(
            int screenWidth,
            int screenHeight,
            bool isBrowser,
            bool touchPreferred)
        {
            var width = Mathf.Max(1, screenWidth);
            var height = Mathf.Max(1, screenHeight);
            var portrait = height > width;
            if (touchPreferred && portrait)
            {
                return new ResponsiveUiProfile(
                    ResponsiveUiProfileKind.PortraitBlocked,
                    1f,
                    PreferredTouchTargetPixels,
                    true);
            }

            var aspect = width / (float)height;
            var compactViewport = width < 1100 || height < 620 || aspect < 1.6f;
            var compactBrowser = isBrowser && compactViewport;
            var compactTouch = touchPreferred && compactViewport;
            if (compactTouch || compactBrowser)
            {
                // Pick the scaling axis from the actual aspect instead of treating every small
                // browser canvas as wide. Narrow tablets need width matching; only genuinely
                // wide-and-short canvases benefit from height matching.
                return new ResponsiveUiProfile(
                    ResponsiveUiProfileKind.CompactLandscape,
                    ResolveCompactCanvasMatch(aspect),
                    touchPreferred ? PreferredTouchTargetPixels : 0f,
                    false);
            }

            return new ResponsiveUiProfile(
                ResponsiveUiProfileKind.WideLandscape,
                0.5f,
                touchPreferred ? PreferredTouchTargetPixels : 0f,
                false);
        }

        private static float ResolveCompactCanvasMatch(float aspect)
        {
            if (aspect < ReferenceLandscapeAspect - AspectTolerance)
            {
                // A narrower-than-16:9 canvas needs width matching so the left and right UI
                // edges remain visible. The additional logical height is safe vertical room.
                return 0f;
            }

            if (aspect > ReferenceLandscapeAspect + AspectTolerance)
            {
                // Height matching is reserved for genuinely wide-and-short browser canvases.
                return 1f;
            }

            return 0.5f;
        }
    }

    public readonly struct SafeAreaInsets
    {
        public static SafeAreaInsets Zero => new SafeAreaInsets(0f, 0f, 0f, 0f);

        public SafeAreaInsets(float left, float right, float bottom, float top)
        {
            Left = Mathf.Max(0f, left);
            Right = Mathf.Max(0f, right);
            Bottom = Mathf.Max(0f, bottom);
            Top = Mathf.Max(0f, top);
        }

        public float Left { get; }
        public float Right { get; }
        public float Bottom { get; }
        public float Top { get; }

        public static SafeAreaInsets FromScreenPixels(
            Rect safeArea,
            int screenWidth,
            int screenHeight,
            float canvasScaleFactor)
        {
            var width = Mathf.Max(1, screenWidth);
            var height = Mathf.Max(1, screenHeight);
            var scale = Mathf.Max(0.0001f, canvasScaleFactor);
            return new SafeAreaInsets(
                Mathf.Clamp(safeArea.xMin, 0f, width) / scale,
                Mathf.Clamp(width - safeArea.xMax, 0f, width) / scale,
                Mathf.Clamp(safeArea.yMin, 0f, height) / scale,
                Mathf.Clamp(height - safeArea.yMax, 0f, height) / scale);
        }

        public bool Approximately(SafeAreaInsets other)
        {
            return Mathf.Approximately(Left, other.Left)
                && Mathf.Approximately(Right, other.Right)
                && Mathf.Approximately(Bottom, other.Bottom)
                && Mathf.Approximately(Top, other.Top);
        }
    }

    public static class SafeAreaRectLayout
    {
        private const float EdgeTolerance = 0.001f;

        public static bool IsFullStretch(RectTransform target)
        {
            return target != null
                && target.anchorMin.x <= EdgeTolerance
                && target.anchorMin.y <= EdgeTolerance
                && target.anchorMax.x >= 1f - EdgeTolerance
                && target.anchorMax.y >= 1f - EdgeTolerance;
        }

        public static bool UsesScreenEdge(RectTransform target)
        {
            if (target == null || IsFullStretch(target))
            {
                return false;
            }

            return target.anchorMin.x <= EdgeTolerance
                || target.anchorMax.x >= 1f - EdgeTolerance
                || target.anchorMin.y <= EdgeTolerance
                || target.anchorMax.y >= 1f - EdgeTolerance;
        }

        public static void ApplyDelta(
            RectTransform target,
            SafeAreaInsets previous,
            SafeAreaInsets current)
        {
            if (target == null || previous.Approximately(current))
            {
                return;
            }

            var left = current.Left - previous.Left;
            var right = current.Right - previous.Right;
            var bottom = current.Bottom - previous.Bottom;
            var top = current.Top - previous.Top;
            var anchorMin = target.anchorMin;
            var anchorMax = target.anchorMax;
            var positionShift = Vector2.zero;
            var adjustOffsetMinX = false;
            var adjustOffsetMaxX = false;
            var adjustOffsetMinY = false;
            var adjustOffsetMaxY = false;

            if (anchorMin.x <= EdgeTolerance && anchorMax.x >= 1f - EdgeTolerance)
            {
                adjustOffsetMinX = true;
                adjustOffsetMaxX = true;
            }
            else if (anchorMax.x <= EdgeTolerance)
            {
                positionShift.x += left;
            }
            else if (anchorMin.x >= 1f - EdgeTolerance)
            {
                positionShift.x -= right;
            }
            else if (anchorMin.x <= EdgeTolerance)
            {
                adjustOffsetMinX = true;
            }
            else if (anchorMax.x >= 1f - EdgeTolerance)
            {
                adjustOffsetMaxX = true;
            }

            if (anchorMin.y <= EdgeTolerance && anchorMax.y >= 1f - EdgeTolerance)
            {
                adjustOffsetMinY = true;
                adjustOffsetMaxY = true;
            }
            else if (anchorMax.y <= EdgeTolerance)
            {
                positionShift.y += bottom;
            }
            else if (anchorMin.y >= 1f - EdgeTolerance)
            {
                positionShift.y -= top;
            }
            else if (anchorMin.y <= EdgeTolerance)
            {
                adjustOffsetMinY = true;
            }
            else if (anchorMax.y >= 1f - EdgeTolerance)
            {
                adjustOffsetMaxY = true;
            }

            target.anchoredPosition += positionShift;
            if (adjustOffsetMinX || adjustOffsetMinY)
            {
                var offsetMin = target.offsetMin;
                if (adjustOffsetMinX)
                {
                    offsetMin.x += left;
                }

                if (adjustOffsetMinY)
                {
                    offsetMin.y += bottom;
                }

                target.offsetMin = offsetMin;
            }

            if (adjustOffsetMaxX || adjustOffsetMaxY)
            {
                var offsetMax = target.offsetMax;
                if (adjustOffsetMaxX)
                {
                    offsetMax.x -= right;
                }

                if (adjustOffsetMaxY)
                {
                    offsetMax.y -= top;
                }

                target.offsetMax = offsetMax;
            }
        }
    }

    public readonly struct TouchHitAreaInsets
    {
        public static TouchHitAreaInsets Zero => new TouchHitAreaInsets(0f, 0f, 0f, 0f);

        public TouchHitAreaInsets(float left, float right, float bottom, float top)
        {
            Left = Mathf.Max(0f, left);
            Right = Mathf.Max(0f, right);
            Bottom = Mathf.Max(0f, bottom);
            Top = Mathf.Max(0f, top);
        }

        public float Left { get; }
        public float Right { get; }
        public float Bottom { get; }
        public float Top { get; }
        public bool HasExpansion => Left > 0.01f
            || Right > 0.01f
            || Bottom > 0.01f
            || Top > 0.01f;

        public Rect Expand(Rect source)
        {
            return Rect.MinMaxRect(
                source.xMin - Left,
                source.yMin - Bottom,
                source.xMax + Right,
                source.yMax + Top);
        }

        public TouchHitAreaInsets WithLeft(float value)
        {
            return new TouchHitAreaInsets(value, Right, Bottom, Top);
        }

        public TouchHitAreaInsets WithRight(float value)
        {
            return new TouchHitAreaInsets(Left, value, Bottom, Top);
        }

        public TouchHitAreaInsets WithBottom(float value)
        {
            return new TouchHitAreaInsets(Left, Right, value, Top);
        }

        public TouchHitAreaInsets WithTop(float value)
        {
            return new TouchHitAreaInsets(Left, Right, Bottom, value);
        }
    }

    public static class TouchHitAreaLayout
    {
        private const float SeparationEpsilon = 0.01f;

        public static TouchHitAreaInsets Resolve(
            Rect targetBounds,
            IReadOnlyList<Rect> sameParentSiblingBounds,
            float preferredCanvasUnits)
        {
            if (preferredCanvasUnits <= 0f)
            {
                return TouchHitAreaInsets.Zero;
            }

            var targetHorizontal = Mathf.Max(0f, preferredCanvasUnits - targetBounds.width) * 0.5f;
            var targetVertical = Mathf.Max(0f, preferredCanvasUnits - targetBounds.height) * 0.5f;
            var resolved = new TouchHitAreaInsets(
                targetHorizontal,
                targetHorizontal,
                targetVertical,
                targetVertical);
            if (sameParentSiblingBounds == null)
            {
                return resolved;
            }

            for (var index = 0; index < sameParentSiblingBounds.Count; index += 1)
            {
                var sibling = sameParentSiblingBounds[index];
                var targetIsLeft = targetBounds.xMax <= sibling.xMin;
                var targetIsRight = sibling.xMax <= targetBounds.xMin;
                var targetIsBelow = targetBounds.yMax <= sibling.yMin;
                var targetIsAbove = sibling.yMax <= targetBounds.yMin;
                var separatedHorizontally = targetIsLeft || targetIsRight;
                var separatedVertically = targetIsBelow || targetIsAbove;

                if (!separatedHorizontally && !separatedVertically)
                {
                    // The authored controls already overlap. Do not enlarge this target further;
                    // runtime hit-area policy must never make an existing ambiguity worse.
                    return TouchHitAreaInsets.Zero;
                }

                var siblingHorizontal = Mathf.Max(0f, preferredCanvasUnits - sibling.width) * 0.5f;
                var siblingVertical = Mathf.Max(0f, preferredCanvasUnits - sibling.height) * 0.5f;
                var horizontalGap = separatedHorizontally
                    ? targetIsLeft
                        ? sibling.xMin - targetBounds.xMax
                        : targetBounds.xMin - sibling.xMax
                    : -1f;
                var verticalGap = separatedVertically
                    ? targetIsBelow
                        ? sibling.yMin - targetBounds.yMax
                        : targetBounds.yMin - sibling.yMax
                    : -1f;
                var horizontalExpansion = targetHorizontal + siblingHorizontal;
                var verticalExpansion = targetVertical + siblingVertical;
                var remainsSeparatedHorizontally = separatedHorizontally
                    && horizontalExpansion <= horizontalGap;
                var remainsSeparatedVertically = separatedVertically
                    && verticalExpansion <= verticalGap;
                if (remainsSeparatedHorizontally || remainsSeparatedVertically)
                {
                    continue;
                }

                var separateOnHorizontalAxis = separatedHorizontally
                    && (!separatedVertically
                        || horizontalExpansion - horizontalGap
                        <= verticalExpansion - verticalGap);
                if (separateOnHorizontalAxis)
                {
                    var allocation = AllocateGap(
                        horizontalGap,
                        targetHorizontal,
                        siblingHorizontal);
                    resolved = targetIsLeft
                        ? resolved.WithRight(Mathf.Min(resolved.Right, allocation))
                        : resolved.WithLeft(Mathf.Min(resolved.Left, allocation));
                }
                else
                {
                    var allocation = AllocateGap(
                        verticalGap,
                        targetVertical,
                        siblingVertical);
                    resolved = targetIsBelow
                        ? resolved.WithTop(Mathf.Min(resolved.Top, allocation))
                        : resolved.WithBottom(Mathf.Min(resolved.Bottom, allocation));
                }
            }

            return resolved;
        }

        private static float AllocateGap(float gap, float targetDesired, float siblingDesired)
        {
            var combined = targetDesired + siblingDesired;
            if (combined <= 0f)
            {
                return 0f;
            }

            return Mathf.Max(0f, gap - SeparationEpsilon) * targetDesired / combined;
        }
    }

    public static class MobileWebUiRuntime
    {
        private const string CoordinatorObjectName = "[CheeseTama] Mobile Web UI";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var isBrowser = Application.platform == RuntimePlatform.WebGLPlayer;
            var touchPreferred = Application.isMobilePlatform || UnityEngine.Input.touchSupported;
            if (!isBrowser && !touchPreferred)
            {
                return;
            }

            var coordinator = UnityEngine.Object.FindFirstObjectByType<MobileWebUiCoordinator>();
            if (coordinator == null)
            {
                var coordinatorObject = new GameObject(CoordinatorObjectName);
                UnityEngine.Object.DontDestroyOnLoad(coordinatorObject);
                coordinator = coordinatorObject.AddComponent<MobileWebUiCoordinator>();
            }

            coordinator.Configure(isBrowser, touchPreferred);
        }
    }
}
