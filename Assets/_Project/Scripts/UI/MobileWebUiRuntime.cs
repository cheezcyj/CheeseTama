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
            var compactBrowser = isBrowser
                && (width < 1100 || height < 620 || aspect < 1.6f);
            if (touchPreferred || compactBrowser)
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
                0f,
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

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class ResponsiveCanvasRuntime : MonoBehaviour
    {
        public const string LandscapePromptObjectName = "Mobile Landscape Prompt";

        private readonly Dictionary<RectTransform, SafeAreaInsets> appliedInsets =
            new Dictionary<RectTransform, SafeAreaInsets>();
        private readonly HashSet<RectTransform> safeAreaTargets = new HashSet<RectTransform>();
        private readonly List<RectTransform> staleTargets = new List<RectTransform>();
        private readonly List<Selectable> selectables = new List<Selectable>();
        private readonly List<Rect> siblingSelectableBounds = new List<Rect>();
        private readonly Vector3[] rectWorldCorners = new Vector3[4];

        private Canvas targetCanvas;
        private CanvasScaler canvasScaler;
        private RectTransform landscapePrompt;
        private bool isBrowser;
        private bool touchPreferred;
        private bool configured;
        private float nextHierarchyRefreshAt;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private Rect lastSafeArea;
        private float lastCanvasScale = -1f;

        public ResponsiveUiProfile CurrentProfile { get; private set; }

        public void Configure(bool browser, bool preferTouch)
        {
            var configurationChanged = !configured
                || isBrowser != browser
                || touchPreferred != preferTouch;
            isBrowser = browser;
            touchPreferred = preferTouch;
            ResolveComponents();
            configured = true;
            if (configurationChanged)
            {
                ApplyCurrentLayout(forceHierarchyRefresh: true);
            }
        }

        public void ApplyLayout(
            int screenWidth,
            int screenHeight,
            Rect safeArea,
            float canvasScaleFactor,
            bool browser,
            bool preferTouch,
            bool forceHierarchyRefresh = true)
        {
            isBrowser = browser;
            touchPreferred = preferTouch;
            ResolveComponents();
            CurrentProfile = ResponsiveUiLayoutPolicy.Resolve(
                screenWidth,
                screenHeight,
                isBrowser,
                touchPreferred);
            ApplyScaleProfile();
            EnsureLandscapePrompt(CurrentProfile.ShowLandscapePrompt);

            if (forceHierarchyRefresh)
            {
                var insets = SafeAreaInsets.FromScreenPixels(
                    safeArea,
                    screenWidth,
                    screenHeight,
                    canvasScaleFactor);
                ApplySafeArea(insets);
                ApplyAdaptiveTouchHitAreas(
                    CurrentProfile.PreferredTouchTargetPixels <= 0f
                        ? 0f
                        : CurrentProfile.PreferredTouchTargetPixels
                          / Mathf.Max(0.0001f, canvasScaleFactor));
            }
        }

        private void Awake()
        {
            ResolveComponents();
        }

        private void LateUpdate()
        {
            ApplyCurrentLayout(forceHierarchyRefresh: Time.unscaledTime >= nextHierarchyRefreshAt);
        }

        private void OnEnable()
        {
            if (configured)
            {
                ApplyCurrentLayout(forceHierarchyRefresh: true);
            }
        }

        private void OnDisable()
        {
            RestoreAppliedInsets();
        }

        private void ApplyCurrentLayout(bool forceHierarchyRefresh)
        {
            ResolveComponents();
            if (targetCanvas == null || !targetCanvas.isRootCanvas)
            {
                return;
            }

            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);
            var safeArea = Screen.safeArea;
            var scale = Mathf.Max(0.0001f, targetCanvas.scaleFactor);
            var metricsChanged = width != lastScreenWidth
                || height != lastScreenHeight
                || safeArea != lastSafeArea
                || !Mathf.Approximately(scale, lastCanvasScale);

            CurrentProfile = ResponsiveUiLayoutPolicy.Resolve(
                width,
                height,
                isBrowser,
                touchPreferred);
            ApplyScaleProfile();
            EnsureLandscapePrompt(CurrentProfile.ShowLandscapePrompt);

            if (metricsChanged || forceHierarchyRefresh)
            {
                ApplySafeArea(SafeAreaInsets.FromScreenPixels(safeArea, width, height, scale));
                ApplyAdaptiveTouchHitAreas(
                    CurrentProfile.PreferredTouchTargetPixels <= 0f
                        ? 0f
                        : CurrentProfile.PreferredTouchTargetPixels / scale);
                nextHierarchyRefreshAt = Time.unscaledTime + 0.75f;
                lastScreenWidth = width;
                lastScreenHeight = height;
                lastSafeArea = safeArea;
                lastCanvasScale = scale;
            }
        }

        private void ResolveComponents()
        {
            targetCanvas ??= GetComponent<Canvas>();
            canvasScaler ??= GetComponent<CanvasScaler>();
        }

        private void ApplyScaleProfile()
        {
            if (canvasScaler == null
                || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return;
            }

            if (!Mathf.Approximately(
                    canvasScaler.matchWidthOrHeight,
                    CurrentProfile.CanvasMatchWidthOrHeight))
            {
                canvasScaler.matchWidthOrHeight = CurrentProfile.CanvasMatchWidthOrHeight;
            }
        }

        private void ApplySafeArea(SafeAreaInsets currentInsets)
        {
            if (targetCanvas == null || !targetCanvas.isRootCanvas)
            {
                return;
            }

            safeAreaTargets.Clear();
            CollectSafeAreaTargets(targetCanvas.transform);
            foreach (var target in safeAreaTargets)
            {
                var previous = appliedInsets.TryGetValue(target, out var existing)
                    ? existing
                    : SafeAreaInsets.Zero;
                SafeAreaRectLayout.ApplyDelta(target, previous, currentInsets);
                appliedInsets[target] = currentInsets;
            }

            staleTargets.Clear();
            foreach (var pair in appliedInsets)
            {
                if (pair.Key == null || !safeAreaTargets.Contains(pair.Key))
                {
                    if (pair.Key != null)
                    {
                        SafeAreaRectLayout.ApplyDelta(pair.Key, pair.Value, SafeAreaInsets.Zero);
                    }

                    staleTargets.Add(pair.Key);
                }
            }

            for (var index = 0; index < staleTargets.Count; index += 1)
            {
                appliedInsets.Remove(staleTargets[index]);
            }
        }

        private void CollectSafeAreaTargets(Transform parent)
        {
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = parent.GetChild(index) as RectTransform;
                if (child == null || child == landscapePrompt)
                {
                    continue;
                }

                if (SafeAreaRectLayout.IsFullStretch(child))
                {
                    // Full-bleed backdrops keep covering the whole browser canvas. Their direct
                    // edge-aligned content is inset instead, avoiding a double safe-area offset.
                    CollectSafeAreaTargets(child);
                }
                else if (SafeAreaRectLayout.UsesScreenEdge(child))
                {
                    safeAreaTargets.Add(child);
                }
            }
        }

        private void ApplyAdaptiveTouchHitAreas(float preferredCanvasUnits)
        {
            if (targetCanvas == null)
            {
                return;
            }

            selectables.Clear();
            targetCanvas.GetComponentsInChildren(false, selectables);
            for (var index = 0; index < selectables.Count; index += 1)
            {
                var selectable = selectables[index];
                if (selectable == null || selectable.transform == landscapePrompt)
                {
                    continue;
                }

                var hitArea = selectable.GetComponent<AdaptiveTouchHitArea>();
                if (preferredCanvasUnits <= 0f)
                {
                    hitArea?.Configure(TouchHitAreaInsets.Zero);
                    continue;
                }

                if (hitArea == null)
                {
                    hitArea = selectable.gameObject.AddComponent<AdaptiveTouchHitArea>();
                }

                var targetRect = selectable.transform as RectTransform;
                if (targetRect == null)
                {
                    hitArea.Configure(TouchHitAreaInsets.Zero);
                    continue;
                }

                siblingSelectableBounds.Clear();
                for (var siblingIndex = 0; siblingIndex < selectables.Count; siblingIndex += 1)
                {
                    var sibling = selectables[siblingIndex];
                    if (sibling == null
                        || ReferenceEquals(sibling, selectable)
                        || sibling.transform.parent != selectable.transform.parent
                        || sibling.transform is not RectTransform siblingRect)
                    {
                        continue;
                    }

                    siblingSelectableBounds.Add(CalculateRectInParentSpace(siblingRect));
                }

                var expansion = TouchHitAreaLayout.Resolve(
                    CalculateRectInParentSpace(targetRect),
                    siblingSelectableBounds,
                    preferredCanvasUnits);
                hitArea.Configure(expansion);
            }
        }

        private Rect CalculateRectInParentSpace(RectTransform target)
        {
            target.GetWorldCorners(rectWorldCorners);
            var parent = target.parent;
            var first = parent != null
                ? parent.InverseTransformPoint(rectWorldCorners[0])
                : rectWorldCorners[0];
            var xMin = first.x;
            var xMax = first.x;
            var yMin = first.y;
            var yMax = first.y;
            for (var index = 1; index < rectWorldCorners.Length; index += 1)
            {
                var corner = parent != null
                    ? parent.InverseTransformPoint(rectWorldCorners[index])
                    : rectWorldCorners[index];
                xMin = Mathf.Min(xMin, corner.x);
                xMax = Mathf.Max(xMax, corner.x);
                yMin = Mathf.Min(yMin, corner.y);
                yMax = Mathf.Max(yMax, corner.y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void EnsureLandscapePrompt(bool visible)
        {
            if (landscapePrompt == null)
            {
                var existing = transform.Find(LandscapePromptObjectName) as RectTransform;
                landscapePrompt = existing != null ? existing : CreateLandscapePrompt();
            }

            if (landscapePrompt == null)
            {
                return;
            }

            landscapePrompt.gameObject.SetActive(visible);
            if (visible)
            {
                landscapePrompt.SetAsLastSibling();
            }
        }

        private RectTransform CreateLandscapePrompt()
        {
            var promptObject = new GameObject(
                LandscapePromptObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            promptObject.transform.SetParent(transform, false);
            var promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = Vector2.zero;
            promptRect.anchorMax = Vector2.one;
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;
            var background = promptObject.GetComponent<Image>();
            background.color = new Color(0.16f, 0.11f, 0.06f, 0.97f);
            background.raycastTarget = true;

            var labelObject = new GameObject(
                "Landscape Prompt Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(promptRect, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.08f, 0.15f);
            labelRect.anchorMax = new Vector2(0.92f, 0.85f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<Text>();
            label.text = "가로 화면으로 돌려 주세요\n치즈타마는 가로 화면에 맞춰져 있어요.";
            label.font = KoreanUiFontRuntime.GetDefaultFont();
            label.fontSize = 30;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.91f, 0.62f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return promptRect;
        }

        private void RestoreAppliedInsets()
        {
            foreach (var pair in appliedInsets)
            {
                if (pair.Key != null)
                {
                    SafeAreaRectLayout.ApplyDelta(pair.Key, pair.Value, SafeAreaInsets.Zero);
                }
            }

            appliedInsets.Clear();
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

            var coordinator = Object.FindFirstObjectByType<MobileWebUiCoordinator>();
            if (coordinator == null)
            {
                var coordinatorObject = new GameObject(CoordinatorObjectName);
                Object.DontDestroyOnLoad(coordinatorObject);
                coordinator = coordinatorObject.AddComponent<MobileWebUiCoordinator>();
            }

            coordinator.Configure(isBrowser, touchPreferred);
        }
    }

    [DisallowMultipleComponent]
    public sealed class MobileWebUiCoordinator : MonoBehaviour
    {
        private bool isBrowser;
        private bool touchPreferred;
        private float nextCanvasDiscoveryAt;

        public void Configure(bool browser, bool preferTouch)
        {
            isBrowser = browser;
            touchPreferred = preferTouch;
            RefreshLoadedCanvases();
        }

        public int RefreshLoadedCanvases()
        {
            var configured = 0;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (var index = 0; index < canvases.Length; index += 1)
            {
                var canvas = canvases[index];
                if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                var runtime = canvas.GetComponent<ResponsiveCanvasRuntime>();
                if (runtime == null)
                {
                    runtime = canvas.gameObject.AddComponent<ResponsiveCanvasRuntime>();
                }

                runtime.Configure(isBrowser, touchPreferred);
                configured += 1;
            }

            nextCanvasDiscoveryAt = Time.unscaledTime + 0.75f;
            return configured;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextCanvasDiscoveryAt)
            {
                RefreshLoadedCanvases();
            }
        }
    }
}
