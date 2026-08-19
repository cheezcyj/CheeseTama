using System.Collections.Generic;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Save;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests
{
    public sealed class MilkroomThemeFeatureTests
    {
        [Test]
        public void LegacySaveOwnsFourFreeThemesAndRejectsUnownedSelection()
        {
            var save = new CheeseTamaSaveData
            {
                decorations = new DecorationSaveData
                {
                    ownedThemeIds = new List<string>
                    {
                        MilkroomThemeController.MorningThemeId,
                        MilkroomThemeController.MorningThemeId,
                        "unknown_theme"
                    }
                },
                milkroomThemeId = MilkroomThemeController.StarlightThemeId
            };

            save.EnsureRuntimeDefaults();

            Assert.That(save.decorations.ownedThemeIds, Has.Count.EqualTo(4));
            Assert.That(save.decorations.ContainsOwnedTheme(MilkroomThemeController.MorningThemeId), Is.True);
            Assert.That(save.decorations.ContainsOwnedTheme(MilkroomThemeController.EveningThemeId), Is.True);
            Assert.That(save.decorations.ContainsOwnedTheme(MilkroomThemeController.NightThemeId), Is.True);
            Assert.That(save.decorations.ContainsOwnedTheme(MilkroomThemeController.RainyThemeId), Is.True);
            Assert.That(save.milkroomThemeId, Is.EqualTo(MilkroomThemeController.MorningThemeId));
        }

        [Test]
        public void UnlockChargesExactlyOnceAndRoundTripsOwnership()
        {
            var save = CreateStarRouteSave(3);
            var system = new MilkroomThemeUnlockSystem();

            var unlocked = system.TryUnlock(save, MilkroomThemeController.StarlightThemeId);
            var duplicate = system.TryUnlock(save, MilkroomThemeController.StarlightThemeId);
            save.milkroomThemeId = unlocked.ThemeId;

            Assert.That(unlocked.Succeeded, Is.True);
            Assert.That(unlocked.SpentStarDrops, Is.EqualTo(3));
            Assert.That(save.economy.starDrops, Is.Zero);
            Assert.That(duplicate.Failure, Is.EqualTo(MilkroomThemeUnlockFailure.AlreadyOwned));
            Assert.That(save.economy.starDrops, Is.Zero);

            var restored = JsonUtility.FromJson<CheeseTamaSaveData>(JsonUtility.ToJson(save));
            restored.EnsureRuntimeDefaults();
            Assert.That(restored.milkroomThemeId, Is.EqualTo(MilkroomThemeController.StarlightThemeId));
            Assert.That(system.IsOwned(restored, MilkroomThemeController.StarlightThemeId), Is.True);
        }

        [Test]
        public void LockedRouteAndInsufficientBalanceLeaveStateUnchanged()
        {
            var system = new MilkroomThemeUnlockSystem();
            var routeLocked = new CheeseTamaSaveData();
            routeLocked.EnsureRuntimeDefaults();
            routeLocked.economy.starDrops = 10;

            var lockedResult = system.TryUnlock(routeLocked, MilkroomThemeController.WinterThemeId);
            Assert.That(lockedResult.Failure, Is.EqualTo(MilkroomThemeUnlockFailure.RouteLocked));
            Assert.That(routeLocked.economy.starDrops, Is.EqualTo(10));
            Assert.That(system.IsOwned(routeLocked, MilkroomThemeController.WinterThemeId), Is.False);

            var insufficient = CreateStarRouteSave(1);
            var insufficientResult = system.TryUnlock(
                insufficient,
                MilkroomThemeController.WinterThemeId);
            Assert.That(
                insufficientResult.Failure,
                Is.EqualTo(MilkroomThemeUnlockFailure.InsufficientStarDrops));
            Assert.That(insufficient.economy.starDrops, Is.EqualTo(1));
            Assert.That(system.IsOwned(insufficient, MilkroomThemeController.WinterThemeId), Is.False);
        }

        [Test]
        public void PremiumThemesHaveDistinctPalettesAndCatalogCosts()
        {
            var morning = MilkroomThemePalette.For(MilkroomThemeController.MorningThemeId);
            Assert.That(MilkroomThemeCatalog.All.Count, Is.EqualTo(7));

            AssertPremiumTheme(MilkroomThemeController.StarlightThemeId, 3, morning);
            AssertPremiumTheme(MilkroomThemeController.WinterThemeId, 2, morning);
            AssertPremiumTheme(MilkroomThemeController.VintageThemeId, 4, morning);
        }

        [Test]
        public void ManagerRejectsUnknownThemeWithoutChangingSelection()
        {
            var root = new GameObject("Milkroom Theme Unknown Selection Fixture");
            root.SetActive(false);

            try
            {
                var manager = root.AddComponent<GameManager>();
                var save = new CheeseTamaSaveData();
                save.EnsureRuntimeDefaults();
                save.milkroomThemeId = MilkroomThemeController.NightThemeId;

                var currentSaveProperty = typeof(GameManager).GetProperty(
                    nameof(GameManager.CurrentSave),
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.That(currentSaveProperty, Is.Not.Null);
                currentSaveProperty.SetValue(manager, save);

                Assert.That(manager.TrySelectMilkroomTheme("unknown_theme"), Is.False);
                Assert.That(
                    save.milkroomThemeId,
                    Is.EqualTo(MilkroomThemeController.NightThemeId));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static CheeseTamaSaveData CreateStarRouteSave(int starDrops)
        {
            var save = new CheeseTamaSaveData();
            save.EnsureRuntimeDefaults();
            save.unlocks.starMilkUnlocked = true;
            save.economy.starDrops = starDrops;
            return save;
        }

        private static void AssertPremiumTheme(
            string themeId,
            int expectedCost,
            MilkroomThemePalette morning)
        {
            var definition = MilkroomThemeCatalog.Find(themeId);
            var palette = MilkroomThemePalette.For(themeId);
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.StarDropCost, Is.EqualTo(expectedCost));
            Assert.That(definition.RequiresStarRoute, Is.True);
            Assert.That(palette.Wall, Is.Not.EqualTo(morning.Wall));
            Assert.That(palette.CameraBackground, Is.Not.EqualTo(morning.CameraBackground));
        }
    }
}
