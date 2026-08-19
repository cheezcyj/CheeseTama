using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CheeseTama.Core;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class AccessibilityFeatureTests
    {
        [Test]
        public void SettingsNormalizeToSupportedTextScalesAndSurviveJsonRoundTrip()
        {
            var settings = new GameSettingsSaveData
            {
                textScale = 1.26f,
                highContrastUi = true,
                reduceMotion = true
            };

            settings.EnsureRuntimeDefaults();
            Assert.That(settings.textScale, Is.EqualTo(GameSettingsSaveData.MediumTextScale));

            var restored = JsonUtility.FromJson<GameSettingsSaveData>(JsonUtility.ToJson(settings));
            restored.EnsureRuntimeDefaults();
            Assert.That(restored.textScale, Is.EqualTo(GameSettingsSaveData.MediumTextScale));
            Assert.That(restored.highContrastUi, Is.True);
            Assert.That(restored.reduceMotion, Is.True);

            restored.textScale = 99f;
            restored.EnsureRuntimeDefaults();
            Assert.That(restored.textScale, Is.EqualTo(GameSettingsSaveData.LargeTextScale));
        }

        [Test]
        public void RuntimeTextScaleAndContrastAreIdempotentAndReversible()
        {
            var root = new GameObject("Accessibility Runtime Test", typeof(RectTransform));
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.GetComponent<Text>();
            label.fontSize = 20;
            label.color = Color.white;

            try
            {
                var enlarged = new GameSettingsSaveData
                {
                    textScale = GameSettingsSaveData.LargeTextScale,
                    highContrastUi = true
                };
                AccessibilityRuntime.Apply(root.transform, enlarged);
                AccessibilityRuntime.Apply(root.transform, enlarged);

                Assert.That(label.fontSize, Is.EqualTo(28));
                Assert.That(label.GetComponent<AccessibilityTextProfile>().BaseFontSize, Is.EqualTo(20));
                Assert.That(label.GetComponent<Outline>(), Is.Not.Null);
                Assert.That(label.GetComponent<Outline>().enabled, Is.True);

                AccessibilityRuntime.Apply(root.transform, GameSettingsSaveData.CreateDefault());
                Assert.That(label.fontSize, Is.EqualTo(20));
                Assert.That(label.GetComponent<Outline>().enabled, Is.False);
                Assert.That(AccessibilityRuntime.ReducedMotion, Is.False);
            }
            finally
            {
                AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HighContrastPreservesAnAuthoredOutline()
        {
            var root = new GameObject("Authored Outline Test", typeof(RectTransform));
            var label = root.AddComponent<Text>();
            label.fontSize = 18;
            var authored = root.AddComponent<Outline>();
            authored.effectColor = Color.red;
            authored.effectDistance = new Vector2(3f, -3f);
            authored.enabled = false;

            try
            {
                AccessibilityRuntime.Apply(
                    root.transform,
                    new GameSettingsSaveData { highContrastUi = true });

                Assert.That(root.GetComponents<Outline>(), Has.Length.EqualTo(2));
                Assert.That(authored.effectColor, Is.EqualTo(Color.red));
                Assert.That(authored.effectDistance, Is.EqualTo(new Vector2(3f, -3f)));
                Assert.That(authored.enabled, Is.False);

                AccessibilityRuntime.Apply(root.transform, GameSettingsSaveData.CreateDefault());
                Assert.That(authored.enabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReducedMotionStateIsAppliedWithoutHidingOutcomeUi()
        {
            AccessibilityRuntime.Apply(null, new GameSettingsSaveData { reduceMotion = true });
            Assert.That(AccessibilityRuntime.ReducedMotion, Is.True);
            Assert.That(AccessibilityRuntime.MotionScale, Is.Zero);

            AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
            Assert.That(AccessibilityRuntime.ReducedMotion, Is.False);
            Assert.That(AccessibilityRuntime.MotionScale, Is.EqualTo(1f));
        }

        [Test]
        public void ReducedMotionShortensCharacterReactionWithoutDroppingActionState()
        {
            var root = new GameObject("Reduced Motion Character Test");
            try
            {
                AccessibilityRuntime.Apply(null, new GameSettingsSaveData { reduceMotion = true });
                var visual = root.AddComponent<CheeseTamaVisualController>();
                visual.ReactAction(CheeseTamaVisualAction.Play);

                var duration = (float)(typeof(CheeseTamaVisualController)
                    .GetField("reactionDuration", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(visual) ?? 0f);
                Assert.That(visual.IsReacting, Is.True);
                Assert.That(visual.ActiveAction, Is.EqualTo(CheeseTamaVisualAction.Play));
                Assert.That(duration, Is.EqualTo(0.12f).Within(0.001f));
            }
            finally
            {
                AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReducedMotionShowsCareEventCardWithoutScaleOrFadeTween()
        {
            var host = new GameObject("Reduced Care Event Presentation Test", typeof(RectTransform));
            var overlay = new GameObject("Care Event Overlay", typeof(RectTransform));
            overlay.transform.SetParent(host.transform, false);
            var card = new GameObject("Care Event Card", typeof(RectTransform));
            card.transform.SetParent(overlay.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.localScale = new Vector3(1.1f, 1.1f, 1f);

            try
            {
                AccessibilityRuntime.Apply(null, new GameSettingsSaveData { reduceMotion = true });
                var controller = host.AddComponent<CareEventCardController>();
                typeof(CareEventCardController)
                    .GetField("overlayRoot", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(controller, overlay);
                typeof(CareEventCardController)
                    .GetField("cardTransform", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(controller, cardRect);
                typeof(CareEventCardController)
                    .GetField("cardRestingScale", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(controller, cardRect.localScale);

                typeof(CareEventCardController)
                    .GetMethod("BeginPresentation", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(controller, null);

                Assert.That(overlay.GetComponent<CanvasGroup>(), Is.Not.Null);
                Assert.That(overlay.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
                Assert.That(cardRect.localScale, Is.EqualTo(new Vector3(1.1f, 1.1f, 1f)));
                Assert.That(
                    (bool)(typeof(CareEventCardController)
                        .GetField("presentationAnimating", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(controller) ?? true),
                    Is.False);
            }
            finally
            {
                AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DynamicRecordAndCareChoiceLabelsKeepCurrentTextScale()
        {
            var root = new GameObject("Dynamic Accessibility Labels", typeof(RectTransform));
            var recordObject = new GameObject("Record Label", typeof(RectTransform), typeof(Text));
            recordObject.transform.SetParent(root.transform, false);
            var recordLabel = recordObject.GetComponent<Text>();
            recordLabel.fontSize = 16;
            var choiceObject = new GameObject(
                "Choice Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            choiceObject.transform.SetParent(root.transform, false);
            var choiceLabelObject = new GameObject("Choice Label", typeof(RectTransform), typeof(Text));
            choiceLabelObject.transform.SetParent(choiceObject.transform, false);
            var choiceLabel = choiceLabelObject.GetComponent<Text>();
            choiceLabel.fontSize = 18;
            choiceLabel.resizeTextForBestFit = true;
            choiceLabel.resizeTextMinSize = 13;
            choiceLabel.resizeTextMaxSize = 18;

            try
            {
                AccessibilityRuntime.Apply(
                    root.transform,
                    new GameSettingsSaveData { textScale = GameSettingsSaveData.LargeTextScale });

                typeof(MilkroomUIController)
                    .GetMethod("PrepareRecordText", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, new object[] { recordLabel });
                typeof(CareEventCardController)
                    .GetMethod("ConfigureChoiceButtonLabel", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, new object[] { choiceObject.GetComponent<Button>(), "선택하기" });

                Assert.That(recordLabel.fontSize, Is.EqualTo(22));
                Assert.That(choiceLabel.fontSize, Is.EqualTo(25));
                Assert.That(choiceLabel.resizeTextMinSize, Is.EqualTo(18));
                Assert.That(choiceLabel.resizeTextMaxSize, Is.EqualTo(25));
            }
            finally
            {
                AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void JourneyBadgeKeepsCurrentTextScaleAcrossRefreshStyling()
        {
            var host = new GameObject("Journey Badge Accessibility", typeof(RectTransform));
            var openObject = new GameObject(
                JourneyHubPanelController.OpenButtonObjectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            openObject.transform.SetParent(host.transform, false);
            var sourceObject = new GameObject("Open Label", typeof(RectTransform), typeof(Text));
            sourceObject.transform.SetParent(openObject.transform, false);
            sourceObject.GetComponent<Text>().fontSize = 18;

            try
            {
                AccessibilityRuntime.Apply(
                    host.transform,
                    new GameSettingsSaveData { textScale = GameSettingsSaveData.LargeTextScale });
                var controller = host.AddComponent<JourneyHubPanelController>();
                typeof(JourneyHubPanelController)
                    .GetField("openButton", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(controller, openObject.GetComponent<Button>());

                var ensureBadge = typeof(JourneyHubPanelController).GetMethod(
                    "EnsureOpenButtonBadge",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(ensureBadge, Is.Not.Null);
                ensureBadge.Invoke(controller, null);
                ensureBadge.Invoke(controller, null);

                var badge = openObject.transform.Find("Journey Hub Attention Badge");
                var badgeLabel = badge?.GetComponentInChildren<Text>(true);
                Assert.That(badgeLabel, Is.Not.Null);
                Assert.That(badgeLabel.fontSize, Is.EqualTo(18));
                Assert.That(badgeLabel.resizeTextMinSize, Is.EqualTo(14));
                Assert.That(badgeLabel.resizeTextMaxSize, Is.EqualTo(18));
            }
            finally
            {
                AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void CollectionRebindAppliesAccessibilityToRecreatedCards()
        {
            var host = new GameObject("Collection Accessibility", typeof(RectTransform));
            var contentObject = new GameObject("Collection Scroll Content", typeof(RectTransform));
            contentObject.transform.SetParent(host.transform, false);
            var milkLabelObject = new GameObject("Milk Records", typeof(RectTransform), typeof(Text));
            milkLabelObject.transform.SetParent(contentObject.transform, false);
            var controller = host.AddComponent<CollectionUIController>();
            var save = SaveManager.CreateDefaultSave();
            save.settings.textScale = GameSettingsSaveData.LargeTextScale;
            save.collections.milk.Add("milk_basic");

            try
            {
                controller.Configure(milkLabelObject.GetComponent<Text>(), null, null, null, null);
                controller.Bind(save);
                controller.Bind(save);
                controller.Bind(null);

                var cardRoot = contentObject.transform.Find("Milk Records Card Root");
                var cardLabels = cardRoot?.GetComponentsInChildren<Text>(true);
                Assert.That(cardLabels, Is.Not.Null.And.Not.Empty);
                Assert.That(cardLabels, Has.All.Matches<Text>(label =>
                    label.GetComponent<AccessibilityTextProfile>() != null));
            }
            finally
            {
                AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void LegacySaveWithoutAccessibilityFieldsMigratesToSafeDefaults()
        {
            var root = new GameObject("Legacy Accessibility Migration");
            root.SetActive(false);
            var saveManager = root.AddComponent<SaveManager>();
            var isolatedName = $"cheesetama_accessibility_{Guid.NewGuid():N}.json";
            typeof(SaveManager).GetField("saveFileName", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(saveManager, isolatedName);

            try
            {
                var json = JsonUtility.ToJson(SaveManager.CreateDefaultSave(), true);
                json = Regex.Replace(json, @"^\s*""textScale""\s*:\s*[^,\r\n]+,?\r?\n", string.Empty, RegexOptions.Multiline);
                json = Regex.Replace(json, @"^\s*""highContrastUi""\s*:\s*[^,\r\n]+,?\r?\n", string.Empty, RegexOptions.Multiline);
                json = Regex.Replace(json, @"^\s*""reduceMotion""\s*:\s*[^,\r\n]+,?\r?\n", string.Empty, RegexOptions.Multiline);
                File.WriteAllText(saveManager.SaveFilePath, json);

                var restored = saveManager.LoadOrCreate();
                Assert.That(restored.settings.textScale, Is.EqualTo(GameSettingsSaveData.DefaultTextScale));
                Assert.That(restored.settings.highContrastUi, Is.False);
                Assert.That(restored.settings.reduceMotion, Is.False);
                Assert.That(saveManager.LastLoadMigratedData, Is.True);
            }
            finally
            {
                saveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SettingsBuilderAddsAccessibilityEntryAndOverlay()
        {
            var previousManager = GameManager.Instance;
            var managerField = typeof(GameManager).GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(managerField, Is.Not.Null);
            var coreRoot = new GameObject("Accessibility Settings Isolated Core");
            coreRoot.SetActive(false);
            var saveManager = coreRoot.AddComponent<SaveManager>();
            var manager = coreRoot.AddComponent<GameManager>();
            var isolatedName = $"cheesetama_accessibility_builder_{Guid.NewGuid():N}.json";
            typeof(SaveManager)
                .GetField("saveFileName", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(saveManager, isolatedName);
            typeof(GameManager)
                .GetField("saveManager", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, saveManager);
            manager.LoadOrCreateGame();
            managerField.SetValue(null, manager);

            var staleAudio = CheeseTama.Audio.CheeseTamaAudioController.Instance;
            if (staleAudio == null && !ReferenceEquals(staleAudio, null))
            {
                typeof(CheeseTama.Audio.CheeseTamaAudioController)
                    .GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.SetValue(null, null);
            }

            var canvasObject = new GameObject("Accessibility Canvas", typeof(RectTransform), typeof(Canvas));
            var topMenu = new GameObject("Top Menu", typeof(RectTransform));
            topMenu.transform.SetParent(canvasObject.transform, false);
            var settingsButtonObject = new GameObject("Settings Button", typeof(RectTransform), typeof(Image), typeof(Button));
            settingsButtonObject.transform.SetParent(topMenu.transform, false);
            var controllerObject = new GameObject("Milkroom UI", typeof(RectTransform));
            controllerObject.transform.SetParent(canvasObject.transform, false);
            var milkroomUi = controllerObject.AddComponent<MilkroomUIController>();

            try
            {
                var method = typeof(StarterSceneBuilder).GetMethod(
                    "BuildMilkroomSettings",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                method.Invoke(null, new object[]
                {
                    canvasObject.transform,
                    settingsButtonObject.GetComponent<Button>(),
                    milkroomUi,
                    null,
                    null
                });

                var settings = canvasObject.transform.Find("Settings Modal");
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings.Find("Accessibility Open Button"), Is.Not.Null);
                var overlay = settings.Find("Accessibility Panel");
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.GetComponent<AccessibilitySettingsPanelController>(), Is.Not.Null);
                Assert.That(overlay.Find("Accessibility Text 125 Button"), Is.Not.Null);
                Assert.That(overlay.Find("High Contrast Toggle"), Is.Not.Null);
                Assert.That(overlay.Find("Reduce Motion Toggle"), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
                saveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(coreRoot);
                managerField.SetValue(
                    null,
                    previousManager != null ? previousManager : null);
            }
        }
    }
}
