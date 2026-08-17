using System;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class FollowUpFeatureBuilderTests
    {
        [Test]
        public void CheeseStarDeliveryBuilderCreatesOneClosedAccessibleModalWhenRepeated()
        {
            var canvas = CreateBlankCanvas("Cheese Star Delivery Builder Test Canvas");
            try
            {
                Assert.That(Application.isPlaying, Is.False);

                InvokeBuilderTwice("EnsureCheeseStarDelivery", canvas.transform);

                AssertSingleNamedObject(canvas.transform, "Cheese Star Delivery Overlay");
                AssertSingleNamedObject(canvas.transform, "Cheese Star Delivery Card");
                AssertSingleNamedObject(canvas.transform, "Open Delivery Button");
                AssertSingleComponent<CheeseStarDeliveryCardController>(canvas);
                AssertSingleComponent<CheeseStarDeliveryBridge>(canvas);

                var overlay = Require(canvas.transform, "Cheese Star Delivery Overlay");
                AssertClosedFullScreenRaycastOverlay(overlay);
                AssertText(overlay.transform, "Delivery Title Text", "오늘의 배달");
                AssertText(overlay.transform, "Delivery Reward Text", "우유 코인 +20\n우유방울 +3");
                AssertButtonText(canvas.transform, "Open Delivery Button", "오늘배달");
                AssertButtonText(overlay.transform, "Delivery Claim Button", "선물 받기");
                AssertButtonText(overlay.transform, "Delivery Later Button", "나중에");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void MemoryJournalBuilderCreatesOneClosedAccessibleModalAndRecallBridgeWhenRepeated()
        {
            var canvas = CreateBlankCanvas("Memory Journal Builder Test Canvas");
            try
            {
                Assert.That(Application.isPlaying, Is.False);

                // The recall bridge is optional by design and is installed when the Milkroom's
                // speech-bubble controller is present. No GameManager or save data is created.
                canvas.AddComponent<CheeseTamaSpeechBubbleController>();
                InvokeBuilderTwice("EnsureMemoryJournal", canvas.transform);

                AssertSingleNamedObject(canvas.transform, "Memory Journal Overlay");
                AssertSingleNamedObject(canvas.transform, "Memory Journal Card");
                AssertSingleNamedObject(canvas.transform, "Open Memory Journal Button");
                AssertSingleComponent<MemoryJournalPanelController>(canvas);
                AssertSingleComponent<MemoryJournalRecallBridge>(canvas);

                var overlay = Require(canvas.transform, "Memory Journal Overlay");
                AssertClosedFullScreenRaycastOverlay(overlay);
                AssertText(overlay.transform, "Memory Journal Title Text", "치즈타마 추억일기");
                AssertText(
                    overlay.transform,
                    "Memory Journal Empty Text",
                    "아직 기록된 추억이 없어요.\n함께 돌보고 놀아주며 첫 장을 채워보세요.");
                AssertButtonText(canvas.transform, "Open Memory Journal Button", "추억일기");
                AssertButtonText(overlay.transform, "Memory Journal Mark Read Button", "모두 읽음");
                AssertButtonText(overlay.transform, "Memory Journal Close Button", "닫기");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void FantasyPowderBuilderCreatesOneClosedAccessibleModalWhenRepeated()
        {
            var canvas = CreateBlankCanvas("Fantasy Powder Builder Test Canvas");
            try
            {
                Assert.That(Application.isPlaying, Is.False);

                InvokeBuilderTwice("EnsureFantasyPowderHiddenRecipes", canvas.transform);

                AssertSingleNamedObject(canvas.transform, "Fantasy Powder Overlay");
                AssertSingleNamedObject(canvas.transform, "Fantasy Powder Card");
                AssertSingleNamedObject(canvas.transform, "Open Fantasy Powder Button");
                AssertSingleComponent<FantasyPowderHiddenRecipePanelController>(canvas);

                var overlay = Require(canvas.transform, "Fantasy Powder Overlay");
                AssertClosedFullScreenRaycastOverlay(overlay);
                AssertText(overlay.transform, "Fantasy Powder Title Text", "환상가루 비밀 조합");
                AssertText(overlay.transform, "Fantasy Powder Hint Text", string.Empty);
                AssertButtonText(canvas.transform, "Open Fantasy Powder Button", "비밀조합");
                AssertButtonText(overlay.transform, "Fantasy Powder Attempt Button", "가루 1개로 시도");
                AssertButtonText(overlay.transform, "Fantasy Powder Close Button", "닫기");

                for (var index = 0; index < 3; index += 1)
                {
                    AssertSingleNamedObject(canvas.transform, $"Fantasy Recipe Button {index}");
                    AssertText(overlay.transform, $"Fantasy Recipe Name Text {index}", string.Empty);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void FirstDayJourneyBuilderCreatesOneClosedAccessibleModalWhenRepeated()
        {
            var canvas = CreateBlankCanvas("First Day Journey Builder Test Canvas");
            try
            {
                Assert.That(Application.isPlaying, Is.False);

                InvokeBuilderTwice("EnsureFirstDayJourney", canvas.transform);

                AssertSingleNamedObject(canvas.transform, FirstDayJourneyController.OverlayObjectName);
                AssertSingleNamedObject(canvas.transform, FirstDayJourneyController.CardObjectName);
                AssertSingleNamedObject(canvas.transform, "Open First Day Journey Button");
                AssertSingleComponent<FirstDayJourneyController>(canvas);

                var overlay = Require(canvas.transform, FirstDayJourneyController.OverlayObjectName);
                AssertClosedFullScreenRaycastOverlay(overlay);
                AssertText(overlay.transform, "First Day Journey Title Text", "첫날 여정");
                AssertText(overlay.transform, "First Day Journey Progress Text", "첫날 여정  0/6");
                AssertButtonText(canvas.transform, "Open First Day Journey Button", "첫날 여정");
                AssertButtonText(overlay.transform, "First Day Journey Claim Button", "첫날 선물 받기");
                AssertButtonText(overlay.transform, "First Day Journey Close Button", "확인");
                var closeRect = Require(overlay.transform, "First Day Journey Close Button").GetComponent<RectTransform>();
                var cardRect = Require(overlay.transform, FirstDayJourneyController.CardObjectName).GetComponent<RectTransform>();
                Assert.That(closeRect, Is.Not.Null);
                Assert.That(cardRect, Is.Not.Null);
                Assert.That(
                    closeRect.anchoredPosition.x + closeRect.rect.width * 0.5f,
                    Is.EqualTo(cardRect.rect.width * 0.5f).Within(0.5f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private static GameObject CreateBlankCanvas(string name)
        {
            return new GameObject(name, typeof(RectTransform), typeof(Canvas));
        }

        private static void InvokeBuilderTwice(string methodName, Transform canvasTransform)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Builder method not found: {methodName}");

            InvokeBuilder(method, canvasTransform);
            InvokeBuilder(method, canvasTransform);
        }

        private static void InvokeBuilder(MethodInfo method, Transform canvasTransform)
        {
            try
            {
                method.Invoke(null, new object[] { canvasTransform });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static void AssertClosedFullScreenRaycastOverlay(GameObject overlay)
        {
            Assert.That(overlay.activeSelf, Is.False, "Feature overlay must start closed.");

            var rect = overlay.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));

            var image = overlay.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.raycastTarget, Is.True);

            var canvasGroup = overlay.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(canvasGroup.interactable, Is.True);
            Assert.That(canvasGroup.blocksRaycasts, Is.True);
        }

        private static void AssertButtonText(Transform root, string objectName, string expected)
        {
            var buttonObject = Require(root, objectName);
            Assert.That(buttonObject.GetComponent<Button>(), Is.Not.Null, objectName);

            var label = buttonObject.GetComponentInChildren<Text>(true);
            Assert.That(label, Is.Not.Null, $"Button label missing: {objectName}");
            Assert.That(label.text, Is.EqualTo(expected), objectName);
        }

        private static void AssertText(Transform root, string objectName, string expected)
        {
            var textObject = Require(root, objectName);
            var label = textObject.GetComponent<Text>();
            Assert.That(label, Is.Not.Null, objectName);
            Assert.That(label.text, Is.EqualTo(expected), objectName);
        }

        private static GameObject Require(Transform root, string objectName)
        {
            var found = FindRecursively(root, objectName);
            Assert.That(found, Is.Not.Null, objectName);
            return found.gameObject;
        }

        private static Transform FindRecursively(Transform parent, string objectName)
        {
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = parent.GetChild(index);
                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    return child;
                }

                var descendant = FindRecursively(child, objectName);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static void AssertSingleNamedObject(Transform root, string objectName)
        {
            Assert.That(CountRecursively(root, objectName), Is.EqualTo(1), objectName);
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

        private static void AssertSingleComponent<T>(GameObject root) where T : Component
        {
            Assert.That(root.GetComponents<T>(), Has.Length.EqualTo(1), typeof(T).Name);
        }
    }
}
