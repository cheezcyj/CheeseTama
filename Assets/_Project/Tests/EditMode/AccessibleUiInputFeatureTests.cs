using System;
using CheeseTama.Gameplay.Input;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheeseTama.Tests.EditMode
{
    public sealed class AccessibleUiInputFeatureTests
    {
        [Test]
        public void CommonUiKeysStayOutsideSerializedBindingsAndRemainReserved()
        {
            var state = new GameInputBindingSaveData();
            GameInputBindingSystem.EnsureDefaults(state);
            var serializedActionCount = state.bindings.Count;

            Assert.That(GameInputRouter.IsSubmitKey(KeyCode.Return), Is.True);
            Assert.That(GameInputRouter.IsSubmitKey(KeyCode.KeypadEnter), Is.True);
            Assert.That(GameInputRouter.IsSubmitKey(KeyCode.Space), Is.True);
            Assert.That(GameInputRouter.IsNextPanelKey(KeyCode.Tab), Is.True);
            Assert.That(GameInputBindingSystem.IsReservedUiKey(KeyCode.Space), Is.True);
            Assert.That(GameInputBindingSystem.IsReservedUiKey(KeyCode.Tab), Is.True);
            Assert.That(
                GameInputBindingSystem.TryRebind(
                    state,
                    GameInputActionIds.Collection,
                    KeyCode.Space,
                    out var error),
                Is.False);
            Assert.That(error, Does.Contain("확인·취소"));
            Assert.That(state.bindings.Count, Is.EqualTo(serializedActionCount));
            Assert.That(state.bindings.Count, Is.EqualTo(GameInputBindingSystem.All.Count));
        }

        [Test]
        public void ExistingLegacySpaceBindingMigratesToSafeDefaultDuringRepair()
        {
            var state = new GameInputBindingSaveData();
            GameInputBindingSystem.EnsureDefaults(state);
            state.schemaVersion = GameInputBindingSaveData.CurrentSchemaVersion - 1;
            foreach (var entry in state.bindings)
            {
                if (entry.actionId == GameInputActionIds.Collection)
                {
                    entry.primaryKey = KeyCode.Space.ToString();
                }
            }

            Assert.That(GameInputBindingSystem.EnsureDefaults(state), Is.True);
            Assert.That(
                GameInputBindingSystem.TryResolve(
                    state,
                    GameInputActionIds.Collection,
                    out var primary,
                    out _),
                Is.True);
            Assert.That(primary, Is.EqualTo(KeyCode.C));
            Assert.That(GameInputBindingSystem.IsReservedUiKey(primary), Is.False);
            Assert.That(GameInputBindingSystem.EnsureDefaults(state), Is.False);
        }

        [Test]
        public void FocusCycleSkipsDisabledControlsAndWrapsWithinScope()
        {
            using var eventSystemLease = new EventSystemLease("Accessible Input EventSystem");
            var scopeRoot = new GameObject("Focus Scope", typeof(RectTransform));
            var first = CreateButton(scopeRoot.transform, "First", true);
            CreateButton(scopeRoot.transform, "Disabled", false);
            var third = CreateButton(scopeRoot.transform, "Third", true);
            var scope = scopeRoot.AddComponent<KeyboardFocusScope>();

            try
            {
                scope.Configure(scopeRoot.transform, true, false, eventSystemLease.Current);
                eventSystemLease.Current.SetSelectedGameObject(first.gameObject);

                Assert.That(scope.isActiveAndEnabled, Is.True, "scope must be active");
                Assert.That(
                    KeyboardFocusNavigation.TryCycle(
                        scopeRoot.transform,
                        eventSystemLease.Current,
                        backwards: false),
                    Is.True,
                    "static focus navigation must find active Selectables");
                eventSystemLease.Current.SetSelectedGameObject(first.gameObject);
                Assert.That(scope.CycleFocus(), Is.True, "registered modal scope must own focus");
                Assert.That(eventSystemLease.Current.currentSelectedGameObject, Is.EqualTo(third.gameObject));
                Assert.That(scope.CycleFocus(), Is.True);
                Assert.That(eventSystemLease.Current.currentSelectedGameObject, Is.EqualTo(first.gameObject));
                Assert.That(scope.CycleFocus(backwards: true), Is.True);
                Assert.That(eventSystemLease.Current.currentSelectedGameObject, Is.EqualTo(third.gameObject));
            }
            finally
            {
                Object.DestroyImmediate(scopeRoot);
            }
        }

        [Test]
        public void ModalScopeRecapturesFocusAndBlocksOutsideItemDetails()
        {
            using var eventSystemLease = new EventSystemLease("Modal Input EventSystem");
            var canvas = new GameObject("Modal Input Canvas", typeof(RectTransform));
            var background = CreateButton(canvas.transform, "Background", true);
            var modalRoot = new GameObject("Modal", typeof(RectTransform));
            modalRoot.transform.SetParent(canvas.transform, false);
            var modalButton = CreateButton(modalRoot.transform, "Modal Button", true);
            var outsideDetails = background.gameObject.AddComponent<ItemDetailsInputTarget>();
            var insideDetails = modalButton.gameObject.AddComponent<ItemDetailsInputTarget>();
            var outsideRequests = 0;
            var insideRequests = 0;
            outsideDetails.Configure(_ => outsideRequests += 1);
            insideDetails.Configure(_ => insideRequests += 1);
            var scope = modalRoot.AddComponent<KeyboardFocusScope>();

            try
            {
                scope.Configure(modalRoot.transform, true, false, eventSystemLease.Current);
                eventSystemLease.Current.SetSelectedGameObject(background.gameObject);

                Assert.That(
                    KeyboardFocusNavigation.EnsureFocusWithin(
                        modalRoot.transform,
                        eventSystemLease.Current),
                    Is.True,
                    "static focus navigation must recapture outside selection");
                eventSystemLease.Current.SetSelectedGameObject(background.gameObject);
                Assert.That(scope.EnsureFocusWithinScope(), Is.True, "modal scope must own and recapture focus");
                Assert.That(eventSystemLease.Current.currentSelectedGameObject, Is.EqualTo(modalButton.gameObject));
                Assert.That(KeyboardFocusScope.IsInteractionAllowed(background.gameObject), Is.False);
                Assert.That(KeyboardFocusScope.IsInteractionAllowed(modalButton.gameObject), Is.True);
                Assert.That(outsideDetails.RequestDetails(), Is.False);
                Assert.That(insideDetails.RequestDetails(), Is.True);
                Assert.That(outsideRequests, Is.Zero);
                Assert.That(insideRequests, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void ItemDetailsTargetAcceptsRightClickOnly()
        {
            using var eventSystemLease = new EventSystemLease("Details EventSystem");
            var targetObject = new GameObject("Details Target", typeof(RectTransform));
            var target = targetObject.AddComponent<ItemDetailsInputTarget>();
            var requestCount = 0;
            target.Configure(_ => requestCount += 1);

            try
            {
                target.OnPointerClick(new PointerEventData(eventSystemLease.Current)
                {
                    button = PointerEventData.InputButton.Left
                });
                target.OnPointerClick(new PointerEventData(eventSystemLease.Current)
                {
                    button = PointerEventData.InputButton.Right
                });

                Assert.That(requestCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void RichTextSizesTrackScaleWithoutCompounding()
        {
            var root = new GameObject("Rich Text Accessibility", typeof(RectTransform), typeof(Text));
            var label = root.GetComponent<Text>();
            label.supportRichText = true;
            label.fontSize = 20;
            label.text = "<size=14>보조</size>\n<SIZE = 3 > </SIZE>";

            try
            {
                AccessibilityRuntime.Apply(
                    root.transform,
                    new GameSettingsSaveData { textScale = GameSettingsSaveData.LargeTextScale });
                AccessibilityRuntime.Apply(
                    root.transform,
                    new GameSettingsSaveData { textScale = GameSettingsSaveData.LargeTextScale });

                Assert.That(label.text, Is.EqualTo("<size=20>보조</size>\n<SIZE = 4 > </SIZE>"));
                Assert.That(label.fontSize, Is.EqualTo(28));

                AccessibilityRuntime.Apply(root.transform, GameSettingsSaveData.CreateDefault());
                Assert.That(label.text, Is.EqualTo("<size=14>보조</size>\n<SIZE = 3 > </SIZE>"));
                Assert.That(label.fontSize, Is.EqualTo(20));
            }
            finally
            {
                AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DynamicRichTextRebasesFromAuthoredTextAndKeepsOtherTags()
        {
            var root = new GameObject("Dynamic Rich Text Accessibility", typeof(RectTransform), typeof(Text));
            var label = root.GetComponent<Text>();
            label.fontSize = 18;

            try
            {
                AccessibilityRuntime.Apply(
                    root.transform,
                    new GameSettingsSaveData { textScale = GameSettingsSaveData.LargeTextScale });
                AccessibilityRuntime.SetTextAndApply(
                    label,
                    "<b>제목</b> <color=#FFFFFF><size=10>설명</size></color> <size=auto>유지</size>");
                AccessibilityRuntime.SetTextAndApply(
                    label,
                    "<b>제목</b> <color=#FFFFFF><size=10>설명</size></color> <size=auto>유지</size>");

                Assert.That(
                    label.text,
                    Is.EqualTo("<b>제목</b> <color=#FFFFFF><size=14>설명</size></color> <size=auto>유지</size>"));

                AccessibilityRuntime.Apply(root.transform, GameSettingsSaveData.CreateDefault());
                Assert.That(
                    label.text,
                    Is.EqualTo("<b>제목</b> <color=#FFFFFF><size=10>설명</size></color> <size=auto>유지</size>"));
            }
            finally
            {
                AccessibilityRuntime.Apply(null, GameSettingsSaveData.CreateDefault());
                Object.DestroyImmediate(root);
            }
        }

        private static Button CreateButton(Transform parent, string name, bool interactable)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var button = buttonObject.GetComponent<Button>();
            button.interactable = interactable;
            return button;
        }

        private sealed class EventSystemLease : IDisposable
        {
            private readonly GameObject ownedObject;
            private readonly GameObject previousSelection;

            public EventSystem Current { get; }

            public EventSystemLease(string objectName)
            {
                Current = EventSystem.current;
                if (Current == null)
                {
                    ownedObject = new GameObject(objectName, typeof(EventSystem));
                    Current = ownedObject.GetComponent<EventSystem>();
                }

                previousSelection = Current.currentSelectedGameObject;
            }

            public void Dispose()
            {
                if (Current != null)
                {
                    Current.SetSelectedGameObject(
                        previousSelection != null && previousSelection.activeInHierarchy
                            ? previousSelection
                            : null);
                }

                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }
        }
    }
}
