using System;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class MilkroomProfileLayoutRegressionTests
    {
        private const float LayoutTolerance = 0.001f;

        [Test]
        public void ProfileShellCreatesOneRoundedSquarePortraitButtonWhenRepeated()
        {
            var canvas = new GameObject(
                "Profile Shell Regression Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                Assert.That(Application.isPlaying, Is.False);
                var topBar = CreateTopLeftRect(
                    canvas.transform,
                    "Top Status Bar",
                    new Vector2(1920f, 88f));
                CreateText(topBar, "Name Text", "CheeseTama");

                InvokeBuilderTwice("EnsureCheeseTamaProfileMenuShell", canvas.transform);

                Assert.That(
                    CountRecursively(canvas.transform, "CheeseTama Profile Button"),
                    Is.EqualTo(1));
                Assert.That(
                    CountRecursively(canvas.transform, "Profile Portrait Image"),
                    Is.EqualTo(1));
                Assert.That(
                    CountRecursively(canvas.transform, CheeseTamaProfileMenuController.OverlayObjectName),
                    Is.EqualTo(1));
                Assert.That(CountRecursively(canvas.transform, "Profile Entries"), Is.EqualTo(1));

                var profileButton = RequireDirect(topBar, "CheeseTama Profile Button");
                Assert.That(profileButton.GetComponent<Button>(), Is.Not.Null);
                var buttonRect = profileButton.GetComponent<RectTransform>();
                Assert.That(buttonRect, Is.Not.Null);
                Assert.That(buttonRect.sizeDelta, Is.EqualTo(new Vector2(56f, 56f)));
                Assert.That(
                    buttonRect.sizeDelta.x,
                    Is.EqualTo(buttonRect.sizeDelta.y).Within(LayoutTolerance),
                    "The profile launcher must stay square before its rounded mask is applied.");

                var background = profileButton.GetComponent<Image>();
                Assert.That(background, Is.Not.Null);
                Assert.That(background.sprite, Is.Not.Null);
                Assert.That(background.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(background.sprite.border.x, Is.GreaterThan(0f));
                Assert.That(background.sprite.border.y, Is.GreaterThan(0f));
                Assert.That(profileButton.GetComponents<Mask>(), Has.Length.EqualTo(1));

                var portrait = RequireDirect(profileButton.transform, "Profile Portrait Image");
                var portraitRect = portrait.GetComponent<RectTransform>();
                var portraitImage = portrait.GetComponent<Image>();
                Assert.That(portraitRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(portraitRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(portraitRect.offsetMin, Is.EqualTo(new Vector2(4f, 4f)));
                Assert.That(portraitRect.offsetMax, Is.EqualTo(new Vector2(-4f, -4f)));
                Assert.That(portraitImage, Is.Not.Null);
                Assert.That(portraitImage.preserveAspect, Is.True);
                Assert.That(portraitImage.raycastTarget, Is.False);

                var overlay = RequireDirect(
                    canvas.transform,
                    CheeseTamaProfileMenuController.OverlayObjectName);
                Assert.That(overlay.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void RuntimeUiChainBuildsProfileAndUtilityEntriesWhenUtilityBarDoesNotExistYet()
        {
            var canvas = CreateMilkroomUiFixture();
            try
            {
                Assert.That(Application.isPlaying, Is.False);
                Assert.That(canvas.transform.Find("Milkroom Utility Bar"), Is.Null);

                Assert.DoesNotThrow(() =>
                {
                    InvokeBuilder("EnsureCheeseTamaProfileMenuShell", canvas.transform);
                    InvokeBuilder("EnsureMilkBlendingPanel", canvas.transform, null, null);
                    InvokeBuilder("EnsureCookingChoicePanel", canvas.transform);
                    InvokeBuilder("EnsureCheeseStarDelivery", canvas.transform);
                    InvokeBuilder("EnsureFirstDayJourney", canvas.transform);
                    InvokeBuilder("EnsureCheeseTamaProfileMenu", canvas.transform);
                });

                var utilityBar = RequireDirect(canvas.transform, "Milkroom Utility Bar");
                AssertDirectButton(utilityBar.transform, "Open First Day Journey Button", "첫날 여정");
                AssertDirectButton(utilityBar.transform, "Open Delivery Button", "오늘배달");

                var portrait = RequirePath(
                    canvas.transform,
                    "Top Status Bar/CheeseTama Profile Button/Profile Portrait Image");
                Assert.That(portrait.GetComponent<Image>()?.sprite, Is.Not.Null);
                Assert.That(
                    canvas.transform.Find(CookingChoicePanelController.OverlayObjectName),
                    Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void MilkroomFeatureBuildersKeepCareTipClearAndRouteSevenButtonsWhenRepeated()
        {
            var canvas = CreateMilkroomUiFixture();
            try
            {
                Assert.That(Application.isPlaying, Is.False);
                var settingsModal = RequireDirect(canvas.transform, "Settings Modal").transform;

                InvokeBuilderTwice("EnsureCheeseTamaProfileMenuShell", canvas.transform);
                InvokeBuilderTwice("EnsureGrowthJourney", canvas.transform);
                InvokeBuilderTwice("EnsureMemoryJournal", canvas.transform);
                InvokeBuilderTwice("EnsureLateGameFeatures", canvas.transform, null, null);
                InvokeBuilderTwice("EnsureFirstDayJourney", canvas.transform);
                InvokeBuilderTwice("EnsureCheeseStarDelivery", canvas.transform);
                InvokeBuilderTwice("EnsureFantasyPowderHiddenRecipes", canvas.transform);

                // The name dialog builder also owns save behavior. This layout regression only
                // exercises the builder's pure reparenting helper beside the profile name.
                InvokeBuilderTwice(
                    "GetOrMoveProfileRenameButton",
                    canvas.transform,
                    settingsModal);
                InvokeBuilderTwice("EnsureCheeseTamaProfileMenu", canvas.transform);

                var careTip = RequireDirect(canvas.transform, "Care Tip Panel");
                var careTipTitle = RequireDirect(careTip.transform, "Care Tip Title Text");
                Assert.That(careTipTitle.activeSelf, Is.True);
                Assert.That(careTipTitle.GetComponent<Text>()?.text, Is.EqualTo("돌봄 팁"));
                Assert.That(
                    careTip.GetComponentsInChildren<Button>(true),
                    Is.Empty,
                    "Feature launchers must not replace or cover the care tip.");

                var utilityBar = RequireDirect(canvas.transform, "Milkroom Utility Bar");
                var utilityRect = utilityBar.GetComponent<RectTransform>();
                Assert.That(utilityRect, Is.Not.Null);
                Assert.That(utilityRect.sizeDelta.y, Is.EqualTo(92f));
                Assert.That(utilityBar.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(3));
                AssertDirectButton(utilityBar.transform, "Open First Day Journey Button", "첫날 여정");
                AssertDirectButton(utilityBar.transform, "Open Delivery Button", "오늘배달");
                AssertDirectButton(utilityBar.transform, "Open Fantasy Powder Button", "비밀조합");
                Assert.That(CountRecursively(canvas.transform, "Open First Day Journey Button"), Is.EqualTo(1));
                Assert.That(CountRecursively(canvas.transform, "Open Delivery Button"), Is.EqualTo(1));
                Assert.That(CountRecursively(canvas.transform, "Open Fantasy Powder Button"), Is.EqualTo(1));
                var firstDayRect = utilityBar.transform.Find("Open First Day Journey Button") as RectTransform;
                var deliveryRect = utilityBar.transform.Find("Open Delivery Button") as RectTransform;
                Assert.That(firstDayRect, Is.Not.Null);
                Assert.That(deliveryRect, Is.Not.Null);
                Assert.That(deliveryRect.anchoredPosition.y, Is.LessThan(firstDayRect.anchoredPosition.y));
                Assert.That(
                    deliveryRect.Find(CheeseStarDeliveryBridge.EntryNotificationBadgeObjectName),
                    Is.Not.Null);

                var entries = RequirePath(
                    canvas.transform,
                    CheeseTamaProfileMenuController.OverlayObjectName + "/Profile Card/Profile Entries");
                var expectedEntries = new[]
                {
                    new[] { "Open Growth Journey Button", "성장 여정" },
                    new[] { "Open Memory Journal Button", "추억일기" },
                    new[] { "Open Bond Status Button", "우리 사이" }
                };
                Assert.That(entries.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(3));
                foreach (var expectedEntry in expectedEntries)
                {
                    AssertDirectButton(entries.transform, expectedEntry[0], expectedEntry[1]);
                    Assert.That(
                        CountRecursively(canvas.transform, expectedEntry[0]),
                        Is.EqualTo(1),
                        expectedEntry[0]);
                }

                var profileCard = RequirePath(
                    canvas.transform,
                    CheeseTamaProfileMenuController.OverlayObjectName + "/Profile Card");
                AssertDirectButton(profileCard.transform, "Open Name Change Button", "이름 변경");
                Assert.That(entries.transform.Find("Open Name Change Button"), Is.Null);
                Assert.That(CountRecursively(canvas.transform, "Open Name Change Button"), Is.EqualTo(1));
                var renameRect = profileCard.transform.Find("Open Name Change Button") as RectTransform;
                var profileNameRect = profileCard.transform.Find("Profile Name Text") as RectTransform;
                var profileCardRect = profileCard.GetComponent<RectTransform>();
                Assert.That(renameRect, Is.Not.Null);
                Assert.That(profileNameRect, Is.Not.Null);
                Assert.That(profileCardRect, Is.Not.Null);
                Assert.That(renameRect.sizeDelta, Is.EqualTo(new Vector2(100f, 36f)));
                Assert.That(
                    profileNameRect.anchoredPosition.x + profileNameRect.rect.width * 0.5f,
                    Is.EqualTo(profileCardRect.rect.width * 0.5f).Within(LayoutTolerance),
                    "The profile name must stay centered in the card.");
                Assert.That(profileNameRect.GetComponent<Text>()?.resizeTextForBestFit, Is.True);
                Assert.That(
                    renameRect.anchoredPosition.x,
                    Is.GreaterThan(profileNameRect.anchoredPosition.x + profileNameRect.rect.width),
                    "The compact rename action should sit beside the profile name.");

                Assert.That(
                    canvas.GetComponents<CheeseTamaProfileMenuController>(),
                    Has.Length.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void DecorationShopLauncherStaysBelowPreviewAtOverlayBottomWhenRepeated()
        {
            var canvas = new GameObject(
                "Decoration Shop Layout Regression Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                Assert.That(Application.isPlaying, Is.False);
                var decorateOverlay = CreateTopLeftRect(
                    canvas.transform,
                    "Decorate Overlay",
                    new Vector2(740f, 620f));
                var preview = CreateTopLeftRect(
                    decorateOverlay,
                    "Decorate Preview Panel",
                    new Vector2(692f, 448f),
                    new Vector2(24f, -122f));

                InvokeBuilderTwice("EnsureDecorationShop", canvas.transform);

                Assert.That(
                    CountRecursively(canvas.transform, "Open Decoration Shop Button"),
                    Is.EqualTo(1));
                Assert.That(
                    CountRecursively(canvas.transform, "Decoration Shop Overlay"),
                    Is.EqualTo(1));
                Assert.That(canvas.GetComponents<DecorationShopPanelController>(), Has.Length.EqualTo(1));

                var launcher = RequireDirect(decorateOverlay, "Open Decoration Shop Button");
                AssertDirectButton(decorateOverlay, "Open Decoration Shop Button", "장식 상점");
                var launcherRect = launcher.GetComponent<RectTransform>();
                var overlayRect = decorateOverlay.GetComponent<RectTransform>();
                var launcherTopInset = -launcherRect.anchoredPosition.y;
                var previewBottomInset = -preview.anchoredPosition.y + preview.rect.height;
                var bottomGap = overlayRect.rect.height - launcherTopInset - launcherRect.rect.height;

                Assert.That(
                    launcherRect.anchoredPosition.y,
                    Is.LessThanOrEqualTo(-540f),
                    "The shop launcher regressed away from the bottom action row.");
                Assert.That(
                    launcherTopInset - previewBottomInset,
                    Is.GreaterThanOrEqualTo(8f - LayoutTolerance),
                    "The shop launcher overlaps the preview panel.");
                Assert.That(
                    bottomGap,
                    Is.InRange(8f, 24f),
                    "The shop launcher must remain inside the overlay with bottom padding.");
                Assert.That(
                    launcherRect.anchoredPosition.x + launcherRect.rect.width,
                    Is.LessThanOrEqualTo(overlayRect.rect.width + LayoutTolerance));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private static GameObject CreateMilkroomUiFixture()
        {
            var canvas = new GameObject(
                "Milkroom Profile Layout Regression Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            var topBar = CreateTopLeftRect(
                canvas.transform,
                "Top Status Bar",
                new Vector2(1920f, 88f));
            CreateText(topBar, "Name Text", "CheeseTama");
            CreateTopLeftRect(
                canvas.transform,
                "Status Panel",
                new Vector2(360f, 510f));
            var careTip = CreateTopLeftRect(
                canvas.transform,
                "Care Tip Panel",
                new Vector2(350f, 104f));
            var careTipTitle = CreateText(careTip, "Care Tip Title Text", "regressed title");
            careTipTitle.gameObject.SetActive(false);
            CreateTopLeftRect(
                canvas.transform,
                "Settings Modal",
                new Vector2(600f, 480f));
            return canvas;
        }

        private static RectTransform CreateTopLeftRect(
            Transform parent,
            string name,
            Vector2 size,
            Vector2 anchoredPosition = default)
        {
            var child = new GameObject(name, typeof(RectTransform));
            var rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, string value)
        {
            var rect = CreateTopLeftRect(parent, name, new Vector2(306f, 30f));
            var label = rect.gameObject.AddComponent<Text>();
            label.text = value;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return label;
        }

        private static void InvokeBuilderTwice(string methodName, params object[] arguments)
        {
            InvokeBuilder(methodName, arguments);
            InvokeBuilder(methodName, arguments);
        }

        private static object InvokeBuilder(string methodName, params object[] arguments)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Builder method not found: {methodName}");
            try
            {
                return method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static void AssertDirectButton(
            Transform parent,
            string objectName,
            string expectedLabel)
        {
            var buttonObject = RequireDirect(parent, objectName);
            Assert.That(buttonObject.transform.parent, Is.SameAs(parent), objectName);
            Assert.That(buttonObject.GetComponent<Button>(), Is.Not.Null, objectName);
            var label = buttonObject.GetComponentInChildren<Text>(true);
            Assert.That(label, Is.Not.Null, $"Button label missing: {objectName}");
            Assert.That(label.text, Is.EqualTo(expectedLabel), objectName);
        }

        private static GameObject RequireDirect(Transform parent, string objectName)
        {
            var found = parent.Find(objectName);
            Assert.That(found, Is.Not.Null, objectName);
            Assert.That(found.parent, Is.SameAs(parent), objectName);
            return found.gameObject;
        }

        private static GameObject RequirePath(Transform root, string path)
        {
            var found = root.Find(path);
            Assert.That(found, Is.Not.Null, path);
            return found.gameObject;
        }

        private static int CountRecursively(Transform parent, string objectName)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = parent.GetChild(index);
                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    count += 1;
                }

                count += CountRecursively(child, objectName);
            }

            return count;
        }
    }
}
