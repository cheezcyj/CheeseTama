using System;
using System.Collections.Generic;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class NextFeatureBuilderTests
    {
        [Test]
        public void BuilderCreatesNewFeatureOverlaysIdempotentlyAndSpeechIsNonBlocking()
        {
            var root = new GameObject("Next Feature Builder Test", typeof(RectTransform), typeof(Canvas));
            try
            {
                Invoke("EnsureNewGameSetup", root.transform, null, null);
                Invoke("EnsureNewGameSetup", root.transform, null, null);
                Invoke("EnsureCheeseTamaSpeechBubble", root.transform, null);
                Invoke("EnsureCheeseTamaSpeechBubble", root.transform, null);
                Invoke("EnsureBouncyJumpMiniGame", root.transform, null, null);
                Invoke("EnsureBouncyJumpMiniGame", root.transform, null, null);
                Invoke("EnsurePlayChoicePanel", root.transform);
                Invoke("EnsurePlayChoicePanel", root.transform);
                Invoke("EnsureGrowthJourney", root.transform);
                Invoke("EnsureGrowthJourney", root.transform);

                AssertSingleChild(root.transform, NewGameSetupController.OverlayObjectName);
                AssertSingleChild(root.transform, "CheeseTama Speech Bubble");
                AssertSingleChild(root.transform, BouncyJumpMiniGameController.OverlayObjectName);
                AssertSingleChild(root.transform, PlayChoicePanelController.OverlayObjectName);
                AssertSingleChild(root.transform, GrowthJourneyController.OverlayObjectName);

                Assert.That(root.GetComponents<NewGameSetupController>().Length, Is.EqualTo(1));
                Assert.That(root.GetComponents<CheeseTamaSpeechBubbleController>().Length, Is.EqualTo(1));
                Assert.That(root.GetComponents<CheeseTamaDialogueBridge>().Length, Is.EqualTo(1));
                Assert.That(root.GetComponents<BouncyJumpMiniGameController>().Length, Is.EqualTo(1));
                Assert.That(root.GetComponents<PlayChoicePanelController>().Length, Is.EqualTo(1));
                Assert.That(root.GetComponents<GrowthJourneyController>().Length, Is.EqualTo(1));

                var bubble = root.transform.Find("CheeseTama Speech Bubble");
                Assert.That(bubble.GetComponent<Image>().raycastTarget, Is.False);
                Assert.That(bubble.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(
                    bubble.GetComponentInChildren<Text>(true).raycastTarget,
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LockedGrowthJourneyHidesStarRouteNameAndExactRequirements()
        {
            var root = new GameObject("Locked Growth Journey Test");
            try
            {
                var controller = root.AddComponent<GrowthJourneyController>();
                var labels = new[]
                {
                    CreateText(root.transform, "Title"),
                    CreateText(root.transform, "Level"),
                    CreateText(root.transform, "Milk"),
                    CreateText(root.transform, "Goal"),
                    CreateText(root.transform, "Unlock")
                };
                var progress = new StarRouteProgress(32, 33, 6, 7, false, "별빛 비밀 조건");

                SetPrivateField(controller, "titleText", labels[0]);
                SetPrivateField(controller, "levelText", labels[1]);
                SetPrivateField(controller, "milkProgressText", labels[2]);
                SetPrivateField(controller, "nextGoalText", labels[3]);
                SetPrivateField(controller, "unlockText", labels[4]);
                InvokePrivate(controller, "ApplyProgress", progress);

                var visibleText = string.Join("\n", Array.ConvertAll(labels, label => label.text));
                Assert.That(visibleText, Does.Not.Contain("별빛"));
                Assert.That(visibleText, Does.Not.Contain("Lv.33"));
                Assert.That(visibleText, Does.Not.Contain("6/7"));
                Assert.That(visibleText, Does.Contain("다음 성장 목표"));
                Assert.That(visibleText, Does.Contain("새로운 성장 길은 조건 달성 후 발견"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MilkroomRecordRevealsStarRouteOnlyAfterUnlock()
        {
            var lockedSave = SaveManager.CreateDefaultSave();
            lockedSave.cheeseTama.level = 32;
            lockedSave.milkGrowth = new List<MilkGrowthSaveEntry>();
            for (var index = 0; index < MilkCatalog.MainMilks.Length - 1; index += 1)
            {
                lockedSave.milkGrowth.Add(new MilkGrowthSaveEntry
                {
                    milkId = MilkCatalog.MainMilks[index].id,
                    growthLevel = MilkCatalog.MainMilkMaxGrowthLevel
                });
            }

            var lockedMilkLine = (string)InvokePrivateStatic(
                typeof(MilkroomUIController),
                "FormatStarMilkGrowthLine",
                lockedSave);
            var lockedGoalLine = (string)InvokePrivateStatic(
                typeof(MilkroomUIController),
                "FormatUnlocks",
                lockedSave);
            var lockedVisibleText = lockedMilkLine + "\n" + lockedGoalLine;

            Assert.That(lockedVisibleText, Does.Not.Contain("별빛"));
            Assert.That(lockedVisibleText, Does.Not.Contain("Lv.5"));
            Assert.That(lockedVisibleText, Does.Not.Contain("/33"));
            Assert.That(lockedVisibleText, Does.Contain("숨겨진 기록"));
            Assert.That(lockedVisibleText, Does.Contain("새로운 성장 길은 조건 달성 후 발견"));

            lockedSave.unlocks.starMilkUnlocked = true;
            var unlockedMilkLine = (string)InvokePrivateStatic(
                typeof(MilkroomUIController),
                "FormatStarMilkGrowthLine",
                lockedSave);
            var unlockedGoalLine = (string)InvokePrivateStatic(
                typeof(MilkroomUIController),
                "FormatUnlocks",
                lockedSave);

            Assert.That(unlockedMilkLine, Does.Contain("별빛 우유"));
            Assert.That(unlockedGoalLine, Does.Contain("별빛 알 / 별빛 우유 해금"));
        }

        private static void Invoke(string methodName, params object[] arguments)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Builder method not found: {methodName}");
            try
            {
                method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, arguments);
        }

        private static object InvokePrivateStatic(Type type, string methodName, params object[] arguments)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(null, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static Text CreateText(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Text>();
        }

        private static void AssertSingleChild(Transform parent, string childName)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                if (string.Equals(parent.GetChild(index).name, childName, StringComparison.Ordinal))
                {
                    count += 1;
                }
            }

            Assert.That(count, Is.EqualTo(1), childName);
        }
    }
}
