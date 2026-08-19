using System.Collections.Generic;
using System.IO;
using CheeseTama.Gameplay.Input;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheeseTama.Tests.EditMode
{
    public sealed class MobileWebUiFeatureTests
    {
        [Test]
        public void LayoutPolicyUsesLandscapePromptOnlyForPortraitTouch()
        {
            var portraitTouch = ResponsiveUiLayoutPolicy.Resolve(
                390,
                844,
                isBrowser: true,
                touchPreferred: true);
            var landscapeTouch = ResponsiveUiLayoutPolicy.Resolve(
                844,
                390,
                isBrowser: true,
                touchPreferred: true);
            var fourByThreeTablet = ResponsiveUiLayoutPolicy.Resolve(
                1024,
                768,
                isBrowser: true,
                touchPreferred: true);
            var narrowLandscape = ResponsiveUiLayoutPolicy.Resolve(
                900,
                700,
                isBrowser: true,
                touchPreferred: false);
            var desktop = ResponsiveUiLayoutPolicy.Resolve(
                1920,
                1080,
                isBrowser: true,
                touchPreferred: false);

            Assert.That(portraitTouch.Kind, Is.EqualTo(ResponsiveUiProfileKind.PortraitBlocked));
            Assert.That(portraitTouch.ShowLandscapePrompt, Is.True);
            Assert.That(portraitTouch.CanvasMatchWidthOrHeight, Is.EqualTo(1f));
            Assert.That(portraitTouch.PreferredTouchTargetPixels, Is.EqualTo(48f));
            Assert.That(landscapeTouch.Kind, Is.EqualTo(ResponsiveUiProfileKind.CompactLandscape));
            Assert.That(landscapeTouch.ShowLandscapePrompt, Is.False);
            Assert.That(
                landscapeTouch.CanvasMatchWidthOrHeight,
                Is.EqualTo(1f),
                "wide-and-short canvases should match height");
            Assert.That(fourByThreeTablet.Kind, Is.EqualTo(ResponsiveUiProfileKind.CompactLandscape));
            Assert.That(
                fourByThreeTablet.CanvasMatchWidthOrHeight,
                Is.Zero,
                "1024x768 must keep both horizontal UI edges visible");
            Assert.That(narrowLandscape.Kind, Is.EqualTo(ResponsiveUiProfileKind.CompactLandscape));
            Assert.That(
                narrowLandscape.CanvasMatchWidthOrHeight,
                Is.Zero,
                "900x700 browser canvas must use width matching");
            Assert.That(desktop.Kind, Is.EqualTo(ResponsiveUiProfileKind.WideLandscape));
            Assert.That(desktop.CanvasMatchWidthOrHeight, Is.EqualTo(0.5f));
            Assert.That(desktop.PreferredTouchTargetPixels, Is.Zero);
        }

        [Test]
        public void SafeAreaInsetsShiftEdgeControlsAndRestoreExactly()
        {
            var targetObject = new GameObject("Top Left Control", typeof(RectTransform));
            var target = targetObject.GetComponent<RectTransform>();
            target.anchorMin = new Vector2(0f, 1f);
            target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0f, 1f);
            target.anchoredPosition = new Vector2(20f, -30f);
            target.sizeDelta = new Vector2(120f, 48f);
            var insets = SafeAreaInsets.FromScreenPixels(
                new Rect(12f, 0f, 988f, 570f),
                1000,
                600,
                0.5f);

            try
            {
                Assert.That(insets.Left, Is.EqualTo(24f));
                Assert.That(insets.Top, Is.EqualTo(60f));
                SafeAreaRectLayout.ApplyDelta(target, SafeAreaInsets.Zero, insets);
                Assert.That(target.anchoredPosition, Is.EqualTo(new Vector2(44f, -90f)));

                SafeAreaRectLayout.ApplyDelta(target, insets, SafeAreaInsets.Zero);
                Assert.That(target.anchoredPosition, Is.EqualTo(new Vector2(20f, -30f)));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void AdaptiveTouchHitAreaExpandsOnlyInvisibleRaycastArea()
        {
            var buttonObject = new GameObject(
                "Compact Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(32f, 40f);
            var target = buttonObject.AddComponent<AdaptiveTouchHitArea>();

            try
            {
                target.Configure(80f);

                Assert.That(target.HitAreaRect, Is.Not.Null);
                Assert.That(target.HitAreaRect.gameObject.activeSelf, Is.True);
                Assert.That(target.HitAreaRect.offsetMin, Is.EqualTo(new Vector2(-24f, -20f)));
                Assert.That(target.HitAreaRect.offsetMax, Is.EqualTo(new Vector2(24f, 20f)));
                Assert.That(target.HitAreaRect.GetComponent<Image>().color.a, Is.Zero);
                Assert.That(target.HitAreaRect.GetComponent<Image>().raycastTarget, Is.True);
                Assert.That(target.HitAreaRect.GetComponent<LayoutElement>().ignoreLayout, Is.True);
                Assert.That(target.HitAreaRect.GetComponent<Selectable>(), Is.Null);

                target.Configure(30f);
                Assert.That(target.HitAreaRect.gameObject.activeSelf, Is.False);
                Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(32f, 40f)));
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
            }
        }

        [Test]
        public void DataManagementSeventySixPixelButtonRowCapsHitAreasBeforeSiblings()
        {
            const float buttonWidth = 76f;
            const float buttonHeight = 42f;
            const float buttonStep = 84f;
            const float preferredTarget = 96f;
            var authoredButtons = new Rect[6];
            for (var index = 0; index < authoredButtons.Length; index += 1)
            {
                authoredButtons[index] = new Rect(
                    28f + buttonStep * index,
                    0f,
                    buttonWidth,
                    buttonHeight);
            }

            var expandedButtons = new Rect[authoredButtons.Length];
            for (var index = 0; index < authoredButtons.Length; index += 1)
            {
                var siblings = new List<Rect>();
                for (var siblingIndex = 0; siblingIndex < authoredButtons.Length; siblingIndex += 1)
                {
                    if (siblingIndex != index)
                    {
                        siblings.Add(authoredButtons[siblingIndex]);
                    }
                }

                var expansion = TouchHitAreaLayout.Resolve(
                    authoredButtons[index],
                    siblings,
                    preferredTarget);
                expandedButtons[index] = expansion.Expand(authoredButtons[index]);
                Assert.That(expandedButtons[index].width, Is.GreaterThan(buttonWidth));
                Assert.That(
                    expandedButtons[index].width,
                    Is.LessThan(preferredTarget),
                    "the 8px authored gap cannot safely fit two full 96-unit targets");
            }

            for (var index = 0; index < expandedButtons.Length - 1; index += 1)
            {
                Assert.That(
                    expandedButtons[index].Overlaps(expandedButtons[index + 1]),
                    Is.False,
                    $"data-management buttons {index} and {index + 1} must not share a raycast area");
                Assert.That(
                    expandedButtons[index].xMax,
                    Is.LessThan(expandedButtons[index + 1].xMin));
            }
        }

        [Test]
        public void GestureTrackerMakesLongPressAndSwipeMutuallyExclusive()
        {
            var tracker = new UiPointerGestureTracker();
            tracker.Begin(7, Vector2.zero, 10f);
            tracker.Track(7, new Vector2(10f, 6f));

            Assert.That(tracker.TryConsumeLongPress(10.54f), Is.False);
            Assert.That(tracker.TryConsumeLongPress(10.56f), Is.True);
            Assert.That(tracker.TryConsumeLongPress(11f), Is.False);
            Assert.That(
                tracker.End(7, new Vector2(120f, 0f), out var directionAfterHold),
                Is.EqualTo(UiPointerGesture.LongPress));
            Assert.That(directionAfterHold, Is.EqualTo(UiSwipeDirection.None));

            tracker.Begin(8, Vector2.zero, 20f);
            tracker.Track(8, new Vector2(-80f, 4f));
            Assert.That(tracker.TryConsumeLongPress(21f), Is.False);
            Assert.That(
                tracker.End(8, new Vector2(-90f, 5f), out var swipeDirection),
                Is.EqualTo(UiPointerGesture.Swipe));
            Assert.That(swipeDirection, Is.EqualTo(UiSwipeDirection.Left));
        }

        [Test]
        public void SwipeTargetSuppressesReleaseClickAndInvokesOnlySwipeCallback()
        {
            using var eventSystemLease = new EventSystemLease();
            var targetObject = new GameObject("Swipe Details Target", typeof(RectTransform));
            var target = targetObject.AddComponent<ItemDetailsInputTarget>();
            var detailRequests = 0;
            var swipeRequests = 0;
            var swipeDirection = UiSwipeDirection.None;
            target.Configure(
                _ => detailRequests += 1,
                (_, direction) =>
                {
                    swipeRequests += 1;
                    swipeDirection = direction;
                });
            var pointer = new PointerEventData(eventSystemLease.Current)
            {
                pointerId = 3,
                button = PointerEventData.InputButton.Left,
                position = Vector2.zero,
                eligibleForClick = true
            };

            try
            {
                target.OnPointerDown(pointer);
                pointer.position = new Vector2(90f, 3f);
                target.OnPointerMove(pointer);
                target.OnPointerUp(pointer);

                Assert.That(detailRequests, Is.Zero);
                Assert.That(swipeRequests, Is.EqualTo(1));
                Assert.That(swipeDirection, Is.EqualTo(UiSwipeDirection.Right));
                Assert.That(pointer.eligibleForClick, Is.False);
                Assert.That(pointer.used, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void ResponsiveCanvasAppliesPromptSafeAreaAndTouchHitProfile()
        {
            var canvasObject = new GameObject(
                "Responsive Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var buttonObject = new GameObject(
                "Top Left Touch Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(12f, -12f);
            buttonRect.sizeDelta = new Vector2(36f, 36f);
            var runtime = canvasObject.AddComponent<ResponsiveCanvasRuntime>();

            try
            {
                runtime.ApplyLayout(
                    390,
                    844,
                    new Rect(0f, 30f, 390f, 780f),
                    0.5f,
                    browser: true,
                    preferTouch: true);

                Assert.That(runtime.CurrentProfile.Kind, Is.EqualTo(ResponsiveUiProfileKind.PortraitBlocked));
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(1f));
                Assert.That(
                    canvasObject.transform.Find(ResponsiveCanvasRuntime.LandscapePromptObjectName).gameObject.activeSelf,
                    Is.True);
                Assert.That(buttonRect.anchoredPosition, Is.EqualTo(new Vector2(12f, -80f)));
                Assert.That(buttonObject.GetComponent<AdaptiveTouchHitArea>(), Is.Not.Null);
                Assert.That(buttonObject.GetComponent<AdaptiveTouchHitArea>().HitAreaRect.gameObject.activeSelf, Is.True);

                runtime.ApplyLayout(
                    844,
                    390,
                    new Rect(24f, 0f, 796f, 390f),
                    0.5f,
                    browser: true,
                    preferTouch: true);

                Assert.That(runtime.CurrentProfile.Kind, Is.EqualTo(ResponsiveUiProfileKind.CompactLandscape));
                Assert.That(
                    canvasObject.transform.Find(ResponsiveCanvasRuntime.LandscapePromptObjectName).gameObject.activeSelf,
                    Is.False);
                Assert.That(buttonRect.anchoredPosition, Is.EqualTo(new Vector2(60f, -12f)));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void WebGlShellUsesReferenceAspectAndOwnsTouchGestures()
        {
            var cssPath = Path.Combine(
                Application.dataPath,
                "WebGLTemplates",
                "CheeseTama",
                "TemplateData",
                "style.css");
            var css = File.ReadAllText(cssPath);

            Assert.That(
                css,
                Does.Match(@"(?s)\.page-shell\s*\{[^}]*width:\s*min\(100%,\s*1040px\);"));
            Assert.That(
                css,
                Does.Match(
                    @"(?s)\.game-stage\s*\{[^}]*width:\s*100%;[^}]*aspect-ratio:\s*16\s*/\s*9;[^}]*touch-action:\s*none;[^}]*overscroll-behavior:\s*contain;"));
            Assert.That(
                css,
                Does.Match(
                    @"(?s)#unity-canvas\s*\{[^}]*touch-action:\s*none;[^}]*overscroll-behavior:\s*contain;"));
            Assert.That(
                css,
                Does.Match(
                    @"(?s)@media\s*\(orientation:\s*landscape\)\s*and\s*\(max-height:\s*600px\)\s*\{.*?\.page-shell\s*\{[^}]*height:\s*100dvh;[^}]*overflow:\s*hidden;.*?\.brand-header,\s*\.game-footer\s*\{[^}]*display:\s*none;.*?\.game-stage\s*\{[^}]*width:\s*min\(100%,\s*177\.7778dvh\);[^}]*max-height:\s*100%;"));
            Assert.That(css, Does.Not.Contain("aspect-ratio: auto"));
        }

        private sealed class EventSystemLease : System.IDisposable
        {
            private readonly GameObject ownedObject;

            public EventSystem Current { get; }

            public EventSystemLease()
            {
                Current = EventSystem.current;
                if (Current == null)
                {
                    ownedObject = new GameObject("Mobile Web UI EventSystem", typeof(EventSystem));
                    Current = ownedObject.GetComponent<EventSystem>();
                }
            }

            public void Dispose()
            {
                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }
        }
    }
}
