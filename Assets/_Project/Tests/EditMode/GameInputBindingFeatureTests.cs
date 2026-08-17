using System.Reflection;
using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Gameplay.Input;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class GameInputBindingFeatureTests
    {
        [Test]
        public void LegacyStateReceivesEverySafeDefault()
        {
            var state = new GameInputBindingSaveData { schemaVersion = 0, bindings = null };

            Assert.That(GameInputBindingSystem.EnsureDefaults(state), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(GameInputBindingSaveData.CurrentSchemaVersion));
            Assert.That(state.bindings.Count, Is.EqualTo(GameInputBindingSystem.All.Count));
            Assert.That(
                GameInputBindingSystem.FormatBinding(state, GameInputActionIds.Care1),
                Is.EqualTo("1 / Keypad1"));
        }

        [Test]
        public void RebindRejectsConflictsAndReservedNavigationKeys()
        {
            var state = new GameInputBindingSaveData();
            GameInputBindingSystem.EnsureDefaults(state);

            Assert.That(
                GameInputBindingSystem.TryRebind(state, GameInputActionIds.Collection, KeyCode.Alpha1, out var conflict),
                Is.False);
            Assert.That(conflict, Does.Contain("이미"));
            Assert.That(
                GameInputBindingSystem.TryRebind(state, GameInputActionIds.Collection, KeyCode.Escape, out var reserved),
                Is.False);
            Assert.That(reserved, Does.Contain("확인·취소"));
            Assert.That(
                GameInputBindingSystem.FormatBinding(state, GameInputActionIds.Collection),
                Is.EqualTo("C"));
        }

        [Test]
        public void ValidRebindPersistsThroughJsonRoundTrip()
        {
            var state = new GameInputBindingSaveData();
            GameInputBindingSystem.EnsureDefaults(state);

            Assert.That(
                GameInputBindingSystem.TryRebind(state, GameInputActionIds.Collection, KeyCode.Q, out _),
                Is.True);
            var loaded = JsonUtility.FromJson<GameInputBindingSaveData>(JsonUtility.ToJson(state));
            GameInputBindingSystem.EnsureDefaults(loaded);

            Assert.That(GameInputBindingSystem.FormatBinding(loaded, GameInputActionIds.Collection), Is.EqualTo("Q"));
            Assert.That(
                GameInputBindingSystem.TryResolve(loaded, GameInputActionIds.Collection, out var primary, out _),
                Is.True);
            Assert.That(primary, Is.EqualTo(KeyCode.Q));
        }

        [Test]
        public void ResetAllRestoresOriginalBindings()
        {
            var state = new GameInputBindingSaveData();
            GameInputBindingSystem.EnsureDefaults(state);
            GameInputBindingSystem.TryRebind(state, GameInputActionIds.Decorate, KeyCode.Q, out _);

            Assert.That(GameInputBindingSystem.ResetAll(state), Is.True);
            Assert.That(GameInputBindingSystem.FormatBinding(state, GameInputActionIds.Decorate), Is.EqualTo("D"));
        }

        [Test]
        public void CorruptedDuplicateBindingsRepairDeterministicallyWithUniqueKeys()
        {
            var state = new GameInputBindingSaveData
            {
                schemaVersion = 0,
                bindings = new List<GameInputBindingSaveEntry>
                {
                    new GameInputBindingSaveEntry
                    {
                        actionId = GameInputActionIds.Collection,
                        primaryKey = KeyCode.Alpha1.ToString(),
                        secondaryKey = KeyCode.Alpha1.ToString()
                    },
                    new GameInputBindingSaveEntry
                    {
                        actionId = GameInputActionIds.Care1,
                        primaryKey = KeyCode.Alpha1.ToString(),
                        secondaryKey = KeyCode.Keypad1.ToString()
                    },
                    new GameInputBindingSaveEntry
                    {
                        actionId = GameInputActionIds.Collection,
                        primaryKey = KeyCode.Alpha1.ToString(),
                        secondaryKey = KeyCode.Keypad1.ToString()
                    },
                    new GameInputBindingSaveEntry
                    {
                        actionId = GameInputActionIds.Decorate,
                        primaryKey = "NotAKey",
                        secondaryKey = null
                    }
                }
            };

            Assert.That(GameInputBindingSystem.EnsureDefaults(state), Is.True);
            var firstRepair = JsonUtility.ToJson(state);
            Assert.That(GameInputBindingSystem.EnsureDefaults(state), Is.False);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(firstRepair));
            Assert.That(state.bindings.Count, Is.EqualTo(GameInputBindingSystem.All.Count));

            var occupied = new HashSet<KeyCode>();
            foreach (var definition in GameInputBindingSystem.All)
            {
                Assert.That(
                    GameInputBindingSystem.TryResolve(state, definition.id, out var primary, out var secondary),
                    Is.True,
                    definition.id);
                Assert.That(occupied.Add(primary), Is.True, $"duplicate primary: {primary}");
                if (secondary != KeyCode.None)
                {
                    Assert.That(occupied.Add(secondary), Is.True, $"duplicate secondary: {secondary}");
                }
            }
        }

        [Test]
        public void ResetActionWithDefaultConflictLeavesStateUnchanged()
        {
            var state = new GameInputBindingSaveData();
            GameInputBindingSystem.EnsureDefaults(state);
            Assert.That(
                GameInputBindingSystem.TryRebind(state, GameInputActionIds.Care1, KeyCode.Q, out _),
                Is.True);
            Assert.That(
                GameInputBindingSystem.TryRebind(state, GameInputActionIds.Collection, KeyCode.Alpha1, out _),
                Is.True);
            var before = JsonUtility.ToJson(state);

            Assert.That(GameInputBindingSystem.ResetAction(state, GameInputActionIds.Care1), Is.False);
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
        }

        [Test]
        public void BuilderCreatesOneBlockingKeyboardSettingsOverlay()
        {
            var canvasObject = new GameObject("Input Test Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var settings = new GameObject("Settings Modal", typeof(RectTransform), typeof(Image));
                settings.transform.SetParent(canvasObject.transform, false);
                var ensure = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureInputBindingsPanel",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(ensure, Is.Not.Null);

                Assert.That(ensure.Invoke(null, new object[] { canvasObject.transform }), Is.EqualTo(true));
                Assert.That(ensure.Invoke(null, new object[] { canvasObject.transform }), Is.EqualTo(true));

                var overlay = canvasObject.transform.Find(InputBindingsPanelController.OverlayObjectName);
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.True);
                Assert.That(overlay.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                Assert.That(
                    settings.transform.Find("Open Input Bindings Button"),
                    Is.Not.Null);
                Assert.That(
                    canvasObject.GetComponents<InputBindingsPanelController>().Length,
                    Is.EqualTo(1));
                foreach (var definition in GameInputBindingSystem.All)
                {
                    Assert.That(
                        overlay.Find($"Input Bindings Card/Input Binding {definition.id} Button"),
                        Is.Not.Null,
                        definition.id);
                }
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void InputBindingsBuilderReportsMissingSettingsForFastPathFallback()
        {
            var canvasObject = new GameObject("Missing Settings Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var ensure = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureInputBindingsPanel",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(ensure, Is.Not.Null);

                Assert.That(ensure.Invoke(null, new object[] { canvasObject.transform }), Is.EqualTo(false));
                Assert.That(
                    canvasObject.transform.Find(InputBindingsPanelController.OverlayObjectName),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
