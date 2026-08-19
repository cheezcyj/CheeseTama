using System;
using System.Collections.Generic;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Events;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class SeasonalEventFeatureTests
    {
        [TestCase(3, MilkroomSeason.Spring)]
        [TestCase(5, MilkroomSeason.Spring)]
        [TestCase(6, MilkroomSeason.Summer)]
        [TestCase(8, MilkroomSeason.Summer)]
        [TestCase(9, MilkroomSeason.Autumn)]
        [TestCase(11, MilkroomSeason.Autumn)]
        [TestCase(12, MilkroomSeason.Winter)]
        [TestCase(2, MilkroomSeason.Winter)]
        public void CalendarResolvesStableNorthernSeasons(int month, MilkroomSeason expected)
        {
            var time = new DateTimeOffset(2026, month, 15, 12, 0, 0, TimeSpan.FromHours(9));
            Assert.That(SeasonalCareEventCatalog.ResolveSeason(time), Is.EqualTo(expected));
        }

        [Test]
        public void CatalogHasOneDiscoverableChoiceEventPerSeasonWithUniqueIds()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (MilkroomSeason season in Enum.GetValues(typeof(MilkroomSeason)))
            {
                Assert.That(SeasonalCareEventCatalog.CountForSeason(season), Is.EqualTo(1));
                var definition = SeasonalCareEventCatalog.GetForSeasonAt(season, 0);
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.CareEvent.RequiresChoice, Is.True);
                Assert.That(definition.CollectionTitle, Is.Not.Empty);
                Assert.That(definition.CollectionDetail, Is.Not.Empty);
                Assert.That(ids.Add(definition.CareEvent.id), Is.True);
                Assert.That(RandomEventSystem.TryGetDefinition(
                    definition.CareEvent.id,
                    out var sharedDefinition), Is.True);
                Assert.That(sharedDefinition, Is.SameAs(definition.CareEvent));
            }
        }

        [Test]
        public void SeasonalRollUsesStrictChanceBoundaryAndRejectsInvalidSelection()
        {
            var system = new SeasonalCareEventSystem();
            var summer = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.FromHours(9));

            var success = system.Roll(
                summer,
                0f,
                SeasonalCareEventCatalog.DefaultOccurrenceChance - 0.0001f);
            var boundary = system.Roll(
                summer,
                0f,
                SeasonalCareEventCatalog.DefaultOccurrenceChance);
            var nan = system.Roll(summer, float.NaN, 0f);
            var infinity = system.Roll(summer, float.PositiveInfinity, 0f);

            Assert.That(success.occurred, Is.True);
            Assert.That(success.eventId, Is.EqualTo("season_summer_milk_breeze"));
            Assert.That(boundary.occurred, Is.False);
            Assert.That(nan.occurred, Is.False);
            Assert.That(infinity.occurred, Is.False);
        }

        [Test]
        public void SeasonalChoiceUsesExistingReceiptSafeChoicePipeline()
        {
            var definition = SeasonalCareEventCatalog.Find("season_winter_milk_star");
            Assert.That(definition, Is.Not.Null);

            var occurrence = new CareEventResult(
                true,
                "seasonal_receipt_1",
                definition.CareEvent.id,
                definition.CareEvent.title,
                definition.CareEvent.message,
                true);
            var tama = new CheeseTamaModel();
            tama.EnsureRuntimeDefaults();
            var economy = new EconomySaveData();
            var system = new CareEventChoiceSystem();

            var first = system.ApplyChoice(
                occurrence,
                "collect_winter_stars",
                tama,
                economy);
            var duplicate = system.ApplyChoice(
                occurrence,
                "warm_winter_window",
                tama,
                economy);

            Assert.That(first.applied, Is.True);
            Assert.That(economy.starDrops, Is.EqualTo(1));
            Assert.That(economy.milkDrops, Is.EqualTo(2));
            Assert.That(duplicate.duplicate, Is.True);
            Assert.That(economy.starDrops, Is.EqualTo(1));
            Assert.That(economy.milkDrops, Is.EqualTo(2));
        }

        [Test]
        public void SeasonalLayerIsSubtleThemeSafeAndNonBlocking()
        {
            var root = new GameObject("Seasonal Layer Test Root", typeof(RectTransform));
            var overlayObject = new GameObject(
                "Seasonal Overlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            overlayObject.transform.SetParent(root.transform, false);

            try
            {
                var overlay = overlayObject.GetComponent<Image>();
                var controller = overlayObject.AddComponent<MilkroomSeasonalLayerController>();
                controller.Configure(overlay, null);
                controller.Refresh(new DateTimeOffset(
                    2026,
                    10,
                    1,
                    12,
                    0,
                    0,
                    TimeSpan.FromHours(9)));

                Assert.That(controller.CurrentLayer.Season, Is.EqualTo(MilkroomSeason.Autumn));
                Assert.That(controller.CurrentLayer.Opacity, Is.InRange(0f, 0.04f));
                Assert.That(overlay.raycastTarget, Is.False);
                Assert.That(overlay.color.a, Is.InRange(0f, 0.06f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuilderCreatesOneReusableSeasonalLayer()
        {
            var canvasObject = new GameObject(
                "Seasonal Builder Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            try
            {
                var method = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureMilkroomSeasonalLayer",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);

                method.Invoke(null, new object[] { canvasObject.transform, null });
                method.Invoke(null, new object[] { canvasObject.transform, null });

                var layer = canvasObject.transform.Find("Milkroom Seasonal Layer");
                Assert.That(layer, Is.Not.Null);
                Assert.That(
                    layer.GetComponents<MilkroomSeasonalLayerController>(),
                    Has.Length.EqualTo(1));
                Assert.That(layer.GetComponent<Image>().raycastTarget, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void CollectionFormatterShowsSeasonalTitleCategoryAndDetail()
        {
            const string EventId = "season_autumn_aging_aroma";
            var name = InvokeCollectionFormatter("FormatKnownRecordName", EventId);
            var category = InvokeCollectionFormatter("FormatRecordCategory", EventId);
            var detail = InvokeCollectionFormatter("FormatKnownRecordDetail", EventId);

            Assert.That(name, Is.EqualTo("숙성 향이 머문 오후"));
            Assert.That(category, Is.EqualTo("계절"));
            Assert.That(detail, Does.Contain("가을"));
            Assert.That(name, Does.Not.Contain("season_"));
        }

        private static string InvokeCollectionFormatter(string methodName, string value)
        {
            var method = typeof(CollectionUIController).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(null, new object[] { value });
        }
    }
}
