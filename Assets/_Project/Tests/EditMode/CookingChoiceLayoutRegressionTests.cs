using System.Reflection;
using CheeseTama.Core;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class CookingChoiceLayoutRegressionTests
    {
        [Test]
        public void BuilderKeepsCookingChoicesInOneColumnAndExplainsSpecialMilkBlendingFood()
        {
            var canvasObject = new GameObject(
                "Cooking Choice Layout Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));

            try
            {
                var ensureChoice = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureCookingChoicePanel",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(ensureChoice, Is.Not.Null);

                ensureChoice.Invoke(null, new object[] { canvasObject.transform });
                ensureChoice.Invoke(null, new object[] { canvasObject.transform });

                var overlay = canvasObject.transform.Find(
                    CookingChoicePanelController.OverlayObjectName);
                var card = overlay?.Find("Cooking Choice Card");
                Assert.That(card, Is.Not.Null);

                var cookingButton = card.Find("Cooking Choice Cooking Button")
                    ?.GetComponent<Button>();
                var milkBlendingButton = card.Find("Cooking Choice Milk Blending Button")
                    ?.GetComponent<Button>();
                Assert.That(cookingButton, Is.Not.Null);
                Assert.That(milkBlendingButton, Is.Not.Null);
                Assert.That(card.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(2));

                var cookingNavigation = cookingButton.navigation;
                Assert.That(cookingNavigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(cookingNavigation.selectOnUp, Is.Null);
                Assert.That(cookingNavigation.selectOnDown, Is.SameAs(milkBlendingButton));
                Assert.That(cookingNavigation.selectOnLeft, Is.Null);
                Assert.That(cookingNavigation.selectOnRight, Is.Null);

                var milkBlendingNavigation = milkBlendingButton.navigation;
                Assert.That(milkBlendingNavigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(milkBlendingNavigation.selectOnUp, Is.SameAs(cookingButton));
                Assert.That(milkBlendingNavigation.selectOnDown, Is.Null);
                Assert.That(milkBlendingNavigation.selectOnLeft, Is.Null);
                Assert.That(milkBlendingNavigation.selectOnRight, Is.Null);

                var cookingRect = cookingButton.GetComponent<RectTransform>();
                var milkBlendingRect = milkBlendingButton.GetComponent<RectTransform>();
                Assert.That(cookingRect, Is.Not.Null);
                Assert.That(milkBlendingRect, Is.Not.Null);
                Assert.That(
                    cookingRect.anchoredPosition,
                    Is.EqualTo(new Vector2(64f, -150f)));
                Assert.That(
                    milkBlendingRect.anchoredPosition,
                    Is.EqualTo(new Vector2(64f, -266f)));
                Assert.That(cookingRect.sizeDelta, Is.EqualTo(new Vector2(552f, 96f)));
                Assert.That(milkBlendingRect.sizeDelta, Is.EqualTo(new Vector2(552f, 96f)));
                Assert.That(
                    cookingRect.anchoredPosition.x,
                    Is.EqualTo(milkBlendingRect.anchoredPosition.x).Within(0.01f),
                    "The two choices must remain in one vertical column.");
                Assert.That(
                    cookingRect.rect.width,
                    Is.EqualTo(milkBlendingRect.rect.width).Within(0.01f),
                    "Both rows must use the same column width.");
                Assert.That(
                    cookingRect.anchoredPosition.y,
                    Is.Not.EqualTo(milkBlendingRect.anchoredPosition.y).Within(0.01f),
                    "Each choice must occupy a different row.");

                var cardRect = card.GetComponent<RectTransform>();
                Assert.That(cardRect, Is.Not.Null);
                var cookingBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    cardRect,
                    cookingRect);
                var milkBlendingBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    cardRect,
                    milkBlendingRect);
                Assert.That(
                    cookingBounds.max.y <= milkBlendingBounds.min.y
                    || milkBlendingBounds.max.y <= cookingBounds.min.y,
                    Is.True,
                    "The two choice rows must not overlap.");
                AssertContainedWithinCard(cardRect, cookingBounds, "Cooking choice");
                AssertContainedWithinCard(cardRect, milkBlendingBounds, "Milk-blending choice");

                var cookingLabel = cookingButton.GetComponentInChildren<Text>(true);
                var milkBlendingLabel = milkBlendingButton.GetComponentInChildren<Text>(true);
                Assert.That(cookingLabel, Is.Not.Null);
                Assert.That(milkBlendingLabel, Is.Not.Null);
                Assert.That(cookingLabel.text, Is.EqualTo("요리하기"));
                Assert.That(
                    milkBlendingLabel.text,
                    Is.EqualTo(
                        "<size=21>우유 블렌딩</size>\n"
                        + "<size=14>(낮은 확률로 특별한 음식 등장)</size>"));
                Assert.That(cookingLabel.alignment, Is.EqualTo(TextAnchor.MiddleCenter));
                Assert.That(milkBlendingLabel.alignment, Is.EqualTo(TextAnchor.MiddleCenter));
                Assert.That(cookingLabel.resizeTextForBestFit, Is.False);
                Assert.That(cookingLabel.fontSize, Is.EqualTo(21));
                Assert.That(milkBlendingLabel.supportRichText, Is.True);
                Assert.That(milkBlendingLabel.resizeTextForBestFit, Is.False);
                Assert.That(milkBlendingLabel.fontSize, Is.EqualTo(21));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        private static void AssertContainedWithinCard(
            RectTransform cardRect,
            Bounds childBounds,
            string childName)
        {
            const float tolerance = 0.01f;
            Assert.That(
                childBounds.min.x,
                Is.GreaterThanOrEqualTo(cardRect.rect.xMin - tolerance),
                $"{childName} must remain inside the card's left edge.");
            Assert.That(
                childBounds.max.x,
                Is.LessThanOrEqualTo(cardRect.rect.xMax + tolerance),
                $"{childName} must remain inside the card's right edge.");
            Assert.That(
                childBounds.min.y,
                Is.GreaterThanOrEqualTo(cardRect.rect.yMin - tolerance),
                $"{childName} must remain inside the card's bottom edge.");
            Assert.That(
                childBounds.max.y,
                Is.LessThanOrEqualTo(cardRect.rect.yMax + tolerance),
                $"{childName} must remain inside the card's top edge.");
        }
    }
}
