using System;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Memories;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class NpcVisitFeatureTests
    {
        [Test]
        public void ConditionSelectsDoctorAndChoiceAppliesExactlyOnce()
        {
            var system = new NpcVisitSystem();
            var state = new NpcVisitSaveData();
            var tama = new CheeseTamaModel();
            tama.EnsureRuntimeDefaults();
            tama.stats.health = 40;
            var history = new CareHistorySaveData { totalCareActions = 5 };
            var economy = new EconomySaveData();
            var now = new DateTimeOffset(2026, 8, 14, 10, 0, 0, TimeSpan.FromHours(9));

            Assert.That(system.TryQueueVisit(
                state, tama, history, now, 0.99d, 0.5d, "visit_1", true, out var offer), Is.True);
            Assert.That(offer.Visitor.Id, Is.EqualTo(NpcVisitSystem.MilkyDoctorId));
            Assert.That(system.TryResolve(
                state, tama, economy, "visit_1", "gentle_checkup", now, out var result), Is.True);
            Assert.That(result.Applied, Is.True);
            Assert.That(tama.stats.health, Is.EqualTo(46));
            Assert.That(state.receipts.Count, Is.EqualTo(1));
            Assert.That(system.TryResolve(
                state, tama, economy, "visit_1", "gentle_checkup", now, out _), Is.False);
            Assert.That(tama.stats.health, Is.EqualTo(46));
        }

        [Test]
        public void SameDayAndCooldownPreventRepeatedVisit()
        {
            var system = new NpcVisitSystem();
            var state = new NpcVisitSaveData();
            var tama = new CheeseTamaModel();
            tama.EnsureRuntimeDefaults();
            var history = new CareHistorySaveData { totalCareActions = 5 };
            var economy = new EconomySaveData();
            var now = new DateTimeOffset(2026, 8, 14, 10, 0, 0, TimeSpan.FromHours(9));
            system.TryQueueVisit(state, tama, history, now, 0d, 0d, "visit_1", true, out var offer);
            system.TryResolve(
                state,
                tama,
                economy,
                offer.OccurrenceId,
                offer.Visitor.Choices[0].Id,
                now,
                out _);

            Assert.That(system.TryQueueVisit(
                state, tama, history, now.AddHours(7), 0d, 0d, "visit_2", true, out _), Is.False);
            Assert.That(system.TryQueueVisit(
                state, tama, history, now.AddDays(1), 0d, 0d, "visit_3", true, out _), Is.True);
        }

        [Test]
        public void PendingVisitAndReceiptSurviveJsonRoundTrip()
        {
            var system = new NpcVisitSystem();
            var state = new NpcVisitSaveData();
            var tama = new CheeseTamaModel();
            tama.EnsureRuntimeDefaults();
            var now = new DateTimeOffset(2026, 8, 14, 10, 0, 0, TimeSpan.FromHours(9));
            system.TryQueueVisit(
                state,
                tama,
                new CareHistorySaveData { totalCareActions = 10 },
                now,
                0d,
                0.5d,
                "persisted_visit",
                true,
                out _);

            var loaded = JsonUtility.FromJson<NpcVisitSaveData>(JsonUtility.ToJson(state));
            Assert.That(system.TryGetPending(loaded, out var pending), Is.True);
            Assert.That(pending.OccurrenceId, Is.EqualTo("persisted_visit"));
            Assert.That(system.TryResolve(
                loaded,
                tama,
                new EconomySaveData(),
                pending.OccurrenceId,
                pending.Visitor.Choices[0].Id,
                now,
                out _), Is.True);
            var reloaded = JsonUtility.FromJson<NpcVisitSaveData>(JsonUtility.ToJson(loaded));
            reloaded.EnsureRuntimeDefaults();
            Assert.That(reloaded.pending.HasValue, Is.False);
            Assert.That(reloaded.receipts.Count, Is.EqualTo(1));
        }

        [Test]
        public void ResolvingPreviousDayPendingVisitConsumesCurrentDayQuota()
        {
            var system = new NpcVisitSystem();
            var state = new NpcVisitSaveData();
            var tama = new CheeseTamaModel();
            tama.EnsureRuntimeDefaults();
            var history = new CareHistorySaveData { totalCareActions = 10 };
            var economy = new EconomySaveData();
            var queuedAt = new DateTimeOffset(2026, 8, 14, 23, 30, 0, TimeSpan.FromHours(9));
            var resolvedAt = queuedAt.AddHours(1);

            Assert.That(system.TryQueueVisit(
                state, tama, history, queuedAt, 0d, 0d, "overnight_visit", true, out var offer), Is.True);
            Assert.That(system.TryResolve(
                state,
                tama,
                economy,
                offer.OccurrenceId,
                offer.Visitor.Choices[0].Id,
                resolvedAt,
                out _), Is.True);
            Assert.That(state.dateKey, Is.EqualTo("2026-08-15"));
            Assert.That(state.visitsToday, Is.EqualTo(NpcVisitSystem.MaximumVisitsPerDay));
            Assert.That(system.TryQueueVisit(
                state,
                tama,
                history,
                resolvedAt.AddHours(NpcVisitSystem.VisitCooldownHours + 1),
                0d,
                0d,
                "same_day_second_visit",
                true,
                out _), Is.False);
        }

        [TestCase("Settings Modal")]
        [TestCase("Decoration Shop Overlay")]
        [TestCase(StarLegacyPanelController.OverlayObjectName)]
        public void VisitBridgeTreatsMajorPanelsAsBlockingModals(string modalName)
        {
            var canvasObject = new GameObject("Npc Blocking Test Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var bridge = canvasObject.AddComponent<NpcVisitBridge>();
                var modal = new GameObject(modalName, typeof(RectTransform));
                modal.transform.SetParent(canvasObject.transform, false);
                modal.SetActive(true);
                bridge.Configure(null, null, canvasObject.transform);
                var isBlocked = typeof(NpcVisitBridge).GetMethod(
                    "IsAnotherModalBlocking",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.That(isBlocked, Is.Not.Null);
                Assert.That(isBlocked.Invoke(bridge, null), Is.EqualTo(true), modalName);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void MemoryJournalRendersStoryKindAsKoreanStoryLabel()
        {
            var presentation = new MemoryJournalPresentation(
                "story_memory",
                MemoryJournalKind.Story,
                "2026-08-14",
                "모짜",
                "soft_cheesetama",
                "밀크룸의 손님",
                "새로운 이야기가 시작됐다.",
                true,
                false,
                false);
            var build = typeof(MemoryJournalPanelController).GetMethod(
                "BuildEntryText",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(build, Is.Not.Null);
            var rendered = build.Invoke(null, new object[] { new[] { presentation } }) as string;
            Assert.That(rendered, Does.Contain("이야기"));
        }

        [Test]
        public void BuilderCreatesSingleInitiallyHiddenVisitCard()
        {
            var canvasObject = new GameObject("Npc Visit Test Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var ensure = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureNpcVisitCard",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(ensure, Is.Not.Null);
                ensure.Invoke(null, new object[] { canvasObject.transform });
                ensure.Invoke(null, new object[] { canvasObject.transform });

                var overlay = canvasObject.transform.Find(NpcVisitCardController.OverlayObjectName);
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.True);
                Assert.That(overlay.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                Assert.That(canvasObject.GetComponents<NpcVisitCardController>().Length, Is.EqualTo(1));
                Assert.That(canvasObject.GetComponents<NpcVisitBridge>().Length, Is.EqualTo(1));
                Assert.That(
                    overlay.Find("Npc Visit Card/Npc Visit First Choice Button"),
                    Is.Not.Null);
                Assert.That(
                    overlay.Find("Npc Visit Card/Npc Visit Second Choice Button"),
                    Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void BuilderCentersMilkCatLaterButtonIdempotently()
        {
            var canvasObject = new GameObject(
                "Npc Visit Later Button Layout Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                var ensure = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureNpcVisitCard",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(ensure, Is.Not.Null);

                ensure.Invoke(null, new object[] { canvasObject.transform });
                ensure.Invoke(null, new object[] { canvasObject.transform });

                var overlay = canvasObject.transform.Find(NpcVisitCardController.OverlayObjectName);
                Assert.That(overlay, Is.Not.Null);
                var card = overlay.Find("Npc Visit Card") as RectTransform;
                var later = card?.Find("Npc Visit Later Button") as RectTransform;
                Assert.That(card, Is.Not.Null);
                Assert.That(later, Is.Not.Null);
                Assert.That(
                    CountNamedChildren(overlay, "Npc Visit Later Button"),
                    Is.EqualTo(1),
                    "Repeated EnsureNpcVisitCard must reuse the NPC later button.");

                Canvas.ForceUpdateCanvases();
                var laterBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    card,
                    later);
                Assert.That(
                    laterBounds.center.x,
                    Is.EqualTo(card.rect.center.x).Within(0.001f),
                    "The NPC later button must be horizontally centered in its card.");

                var laterLabel = later.Find("Label")?.GetComponent<Text>();
                Assert.That(laterLabel, Is.Not.Null);
                Assert.That(laterLabel.text, Is.EqualTo("나중에"));
                Assert.That(laterLabel.alignment, Is.EqualTo(TextAnchor.MiddleCenter));

                var visitor = new NpcVisitSystem().Find(NpcVisitSystem.MilkCatId);
                Assert.That(visitor, Is.Not.Null);
                Assert.That(visitor.DisplayName, Is.EqualTo("밀크냥"));
                var controller = canvasObject.GetComponent<NpcVisitCardController>();
                Assert.That(controller, Is.Not.Null);
                Assert.That(
                    controller.Show(
                        new NpcVisitOffer("milk_cat_later_layout", visitor, 0, false),
                        null),
                    Is.True);
                Assert.That(later.gameObject.activeSelf, Is.True);
                Assert.That(
                    card.Find("Npc Visit Title Text")?.GetComponent<Text>()?.text,
                    Is.EqualTo("밀크냥"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static int CountNamedChildren(Transform root, string objectName)
        {
            var count = 0;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    count += 1;
                }
            }

            return count;
        }
    }
}
