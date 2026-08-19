using System;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.Input;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class OneToSixUiIntegrationTests
    {
        [Test]
        public void JourneyHubBuildsLifeRecordsModalAndRestoresBlockedControlsOnClose()
        {
            var canvas = CreateCanvasWithBlockingControls(
                "Life Records Integration Canvas",
                out var topMenu,
                out var actionBar);

            try
            {
                InvokeBuilder("EnsureJourneyHub", canvas.transform);
                InvokeBuilder("EnsureAccessibleInputScopes", canvas.transform);

                var journeyCard = FindRecursively(canvas.transform, "Journey Hub Card");
                var openButton = FindRecursively(
                        journeyCard,
                        "Life Records Open Button")
                    ?.GetComponent<Button>();
                var overlay = canvas.transform.Find(LifeRecordsPanelController.OverlayObjectName);
                var controller = canvas.GetComponent<LifeRecordsPanelController>();

                Assert.That(journeyCard, Is.Not.Null);
                Assert.That(openButton, Is.Not.Null);
                Assert.That(overlay, Is.Not.Null);
                Assert.That(controller, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                AssertBlockingOverlay(overlay);
                Assert.That(canvas.GetComponent<KeyboardFocusScope>(), Is.Not.Null);
                Assert.That(canvas.GetComponent<KeyboardFocusScope>().IsModalScope, Is.False);
                AssertModalFocusScope(overlay);

                Assert.That(topMenu.enabled, Is.True);
                Assert.That(actionBar.enabled, Is.True);
                openButton.onClick.Invoke();

                Assert.That(controller.IsOpen, Is.True);
                Assert.That(overlay.gameObject.activeSelf, Is.True);
                Assert.That(topMenu.enabled, Is.False);
                Assert.That(actionBar.enabled, Is.False);

                var closeButton = FindRecursively(
                        overlay,
                        "Life Records Close Button")
                    ?.GetComponent<Button>();
                Assert.That(closeButton, Is.Not.Null);
                closeButton.onClick.Invoke();

                Assert.That(controller.IsOpen, Is.False);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                Assert.That(topMenu.enabled, Is.True);
                Assert.That(actionBar.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void SettingsBuildsSplitResetAndCloudModalsWithBlockingAndFocusScopes()
        {
            var managerField = typeof(GameManager).GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(managerField, Is.Not.Null);
            var previousManager = managerField.GetValue(null) as GameManager;

            var coreRoot = new GameObject("Settings Integration Isolated Core");
            coreRoot.SetActive(false);
            var saveManager = coreRoot.AddComponent<SaveManager>();
            SetPrivateField(
                saveManager,
                "saveFileName",
                $"cheesetama_one_to_six_ui_{Guid.NewGuid():N}.json");
            var manager = coreRoot.AddComponent<GameManager>();
            SetPrivateField(manager, "saveManager", saveManager);
            manager.LoadOrCreateGame();
            managerField.SetValue(null, manager);

            var canvas = CreateCanvasWithBlockingControls(
                "Settings Integration Canvas",
                out var topMenu,
                out var actionBar);
            var settingsButton = CreateButton(canvas.transform, "Settings Button");
            var milkroomUiRoot = new GameObject("Milkroom UI", typeof(RectTransform));
            milkroomUiRoot.transform.SetParent(canvas.transform, false);
            var milkroomUi = milkroomUiRoot.AddComponent<MilkroomUIController>();

            try
            {
                InvokeBuilder(
                    "BuildMilkroomSettings",
                    canvas.transform,
                    settingsButton,
                    milkroomUi,
                    null,
                    null);
                InvokeBuilder("EnsureAccessibleInputScopes", canvas.transform);

                var settings = canvas.transform.Find("Settings Modal");
                var confirm = canvas.transform.Find("Confirm Reset Dialog");
                var cloud = canvas.transform.Find(CloudSavePanelController.OverlayObjectName);
                Assert.That(settings, Is.Not.Null);
                Assert.That(confirm, Is.Not.Null);
                Assert.That(cloud, Is.Not.Null);

                AssertBlockingOverlay(confirm);
                AssertBlockingOverlay(cloud);
                Assert.That(
                    FindRecursively(confirm, "Care Progress Reset Mode Button")
                        ?.GetComponent<Button>(),
                    Is.Not.Null);
                Assert.That(
                    FindRecursively(confirm, "Full Local Reset Mode Button")
                        ?.GetComponent<Button>(),
                    Is.Not.Null);
                Assert.That(confirm.GetComponent<ConfirmResetDialog>(), Is.Not.Null);
                Assert.That(settings.Find("Open Cloud Save Button")?.GetComponent<Button>(),
                    Is.Not.Null);
                Assert.That(canvas.GetComponent<CloudSavePanelController>(), Is.Not.Null);

                var pageScope = canvas.GetComponent<KeyboardFocusScope>();
                Assert.That(pageScope, Is.Not.Null);
                Assert.That(pageScope.IsModalScope, Is.False);
                AssertModalFocusScope(confirm);
                AssertModalFocusScope(cloud);

                settingsButton.onClick.Invoke();
                Assert.That(settings.gameObject.activeSelf, Is.True);
                var openReset = settings.Find("Open Reset Button")?.GetComponent<Button>();
                var cancelReset = FindRecursively(confirm, "Cancel Reset Button")
                    ?.GetComponent<Button>();
                Assert.That(openReset, Is.Not.Null);
                Assert.That(cancelReset, Is.Not.Null);
                openReset.onClick.Invoke();
                Assert.That(confirm.gameObject.activeSelf, Is.True);
                Assert.That(topMenu.enabled, Is.False);
                Assert.That(actionBar.enabled, Is.False);
                cancelReset.onClick.Invoke();
                Assert.That(confirm.gameObject.activeSelf, Is.False);
                Assert.That(topMenu.enabled, Is.True);
                Assert.That(actionBar.enabled, Is.True);

                var openCloud = settings.Find("Open Cloud Save Button")?.GetComponent<Button>();
                var closeCloud = FindRecursively(cloud, "Cloud Save Close Button")
                    ?.GetComponent<Button>();
                Assert.That(openCloud, Is.Not.Null);
                Assert.That(closeCloud, Is.Not.Null);
                openCloud.onClick.Invoke();
                Assert.That(cloud.gameObject.activeSelf, Is.True);
                Assert.That(topMenu.enabled, Is.False);
                Assert.That(actionBar.enabled, Is.False);
                closeCloud.onClick.Invoke();
                Assert.That(cloud.gameObject.activeSelf, Is.False);
                Assert.That(topMenu.enabled, Is.True);
                Assert.That(actionBar.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvas);
                saveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(coreRoot);
                managerField.SetValue(null, previousManager);
            }
        }

        [Test]
        public void CollectionRecordCardRightClickRequestsDetailsAndUpdatesMessage()
        {
            var host = new GameObject("Collection Details Integration", typeof(RectTransform));
            var content = new GameObject(
                "Collection Scroll Content",
                typeof(RectTransform));
            content.transform.SetParent(host.transform, false);
            var milkLabel = CreateText(content.transform, "Milk Records");
            var message = CreateText(host.transform, "Collection Message");
            var controller = host.AddComponent<CollectionUIController>();
            var eventSystemRoot = new GameObject("Collection Details Event System");
            var eventSystem = eventSystemRoot.AddComponent<EventSystem>();

            try
            {
                controller.Configure(milkLabel, null, null, null, message);
                var save = SaveManager.CreateDefaultSave();
                save.collections.milk.Clear();
                save.collections.milk.Add("basic_milk");
                controller.Bind(save);

                var cardRoot = content.transform.Find("Milk Records Card Root");
                var card = cardRoot?.Find("Collection Record Card 01");
                var target = card?.GetComponent<ItemDetailsInputTarget>();
                var cardImage = card?.GetComponent<Image>();
                Assert.That(cardRoot, Is.Not.Null);
                Assert.That(card, Is.Not.Null);
                Assert.That(target, Is.Not.Null);
                Assert.That(cardImage, Is.Not.Null);
                Assert.That(cardImage.raycastTarget, Is.True);

                var category = card.Find("Category Text")?.GetComponent<Text>()?.text;
                var title = card.Find("Title Text")?.GetComponent<Text>()?.text;
                var detail = card.Find("Detail Text")?.GetComponent<Text>()?.text;
                Assert.That(category, Is.Not.Null.And.Not.Empty);
                Assert.That(title, Is.Not.Null.And.Not.Empty);
                Assert.That(detail, Is.Not.Null.And.Not.Empty);

                message.text = "unchanged";
                target.OnPointerClick(new PointerEventData(eventSystem)
                {
                    button = PointerEventData.InputButton.Left
                });
                Assert.That(message.text, Is.EqualTo("unchanged"));

                target.OnPointerClick(new PointerEventData(eventSystem)
                {
                    button = PointerEventData.InputButton.Right
                });
                Assert.That(message.text, Does.Contain($"{category} · {title}"));
                Assert.That(message.text, Does.Contain(detail));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(eventSystemRoot);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static GameObject CreateCanvasWithBlockingControls(
            string name,
            out TopMenuController topMenu,
            out BottomActionBarController actionBar)
        {
            var canvas = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            topMenu = canvas.AddComponent<TopMenuController>();
            var actionBarRoot = new GameObject("Bottom Action Bar", typeof(RectTransform));
            actionBarRoot.transform.SetParent(canvas.transform, false);
            actionBar = actionBarRoot.AddComponent<BottomActionBarController>();
            return canvas;
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            return button;
        }

        private static Text CreateText(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.AddComponent<Text>();
        }

        private static void AssertBlockingOverlay(Transform overlay)
        {
            var rect = overlay.GetComponent<RectTransform>();
            var image = overlay.GetComponent<Image>();
            var group = overlay.GetComponent<CanvasGroup>();
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(image, Is.Not.Null);
            Assert.That(image.raycastTarget, Is.True);
            Assert.That(group, Is.Not.Null);
            Assert.That(group.interactable, Is.True);
            Assert.That(group.blocksRaycasts, Is.True);
        }

        private static void AssertModalFocusScope(Transform modal)
        {
            var scope = modal.GetComponent<KeyboardFocusScope>();
            Assert.That(scope, Is.Not.Null, modal.name);
            Assert.That(scope.IsModalScope, Is.True, modal.name);
            Assert.That(scope.FocusRoot, Is.SameAs(modal), modal.name);
        }

        private static Transform FindRecursively(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            var descendants = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < descendants.Length; index += 1)
            {
                if (string.Equals(descendants[index].name, name, StringComparison.Ordinal))
                {
                    return descendants[index];
                }
            }

            return null;
        }

        private static void InvokeBuilder(string methodName, params object[] arguments)
        {
            var method = typeof(StarterSceneBuilder).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(null, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
