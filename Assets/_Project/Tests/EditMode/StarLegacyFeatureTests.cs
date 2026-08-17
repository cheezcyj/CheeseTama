using System;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class StarLegacyFeatureTests
    {
        private static readonly DateTimeOffset FixedNow =
            new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.FromHours(9));

        [Test]
        public void StarEggCanOnlyBeSelectedForAnUnlockedUnstartedJourney()
        {
            var system = new StarEggEmmentalEvolutionSystem();
            var tama = new CheeseTamaModel { level = 1, isHatched = false, form = "egg" };

            Assert.That(
                system.TrySelectStarEgg(tama, new UnlockSaveData()),
                Is.EqualTo(StarEggSelectionStatus.StarEggLocked));
            Assert.That(tama.eggType, Is.Not.EqualTo(StarEggEmmentalEvolutionSystem.StarEggTypeId));

            Assert.That(
                system.TrySelectStarEgg(tama, CreateUnlockedRoute()),
                Is.EqualTo(StarEggSelectionStatus.Applied));
            Assert.That(tama.eggType, Is.EqualTo(StarEggEmmentalEvolutionSystem.StarEggTypeId));
            Assert.That(
                system.TrySelectStarEgg(tama, CreateUnlockedRoute()),
                Is.EqualTo(StarEggSelectionStatus.AlreadySelected));

            var started = CreateFinalTama("egg_cream");
            Assert.That(
                system.TrySelectStarEgg(started, CreateUnlockedRoute()),
                Is.EqualTo(StarEggSelectionStatus.JourneyAlreadyStarted));
            Assert.That(started.eggType, Is.EqualTo("egg_cream"));
        }

        [Test]
        public void StarEggFantasyResonanceUsesSevenTimesInfluence()
        {
            var system = new StarEggEmmentalEvolutionSystem();
            var state = new StarLegacySaveData();
            var unlocks = CreateUnlockedRoute();

            var regular = CreateFinalTama("egg_cream");
            var star = CreateFinalTama(StarEggEmmentalEvolutionSystem.StarEggTypeId);

            Assert.That(system.RecordFantasyResonance(regular, unlocks, state), Is.EqualTo(1));
            Assert.That(system.RecordFantasyResonance(star, unlocks, state), Is.EqualTo(7));
            Assert.That(state.fantasyResonance, Is.EqualTo(8));
        }

        [Test]
        public void EmmentalEvolutionRequiresAllSignalsAndAppliesOnce()
        {
            var system = new StarEggEmmentalEvolutionSystem();
            var tama = CreateFinalTama(StarEggEmmentalEvolutionSystem.StarEggTypeId);
            tama.evolutionId = EvolutionSystem.CreamEvolutionId;
            tama.form = EvolutionSystem.CreamEvolutionId;
            var state = new StarLegacySaveData
            {
                starMilkCareCount = StarEggEmmentalEvolutionSystem.RequiredStarMilkCareCount,
                fantasyResonance = StarEggEmmentalEvolutionSystem.RequiredFantasyResonance
            };

            var first = system.TryApplyEvolution(
                tama,
                CreateUnlockedRoute(),
                state,
                "emmental_001",
                FixedNow);

            Assert.That(first.status, Is.EqualTo(EmmentalEvolutionAttemptStatus.Applied));
            Assert.That(first.evolution.EvolutionId, Is.EqualTo(StarEggEmmentalEvolutionSystem.EmmentalEvolutionId));
            Assert.That(first.evolution.DisplayName, Is.EqualTo("에멘탈치즈타마"));
            Assert.That(tama.evolutionId, Is.EqualTo(StarEggEmmentalEvolutionSystem.EmmentalEvolutionId));
            Assert.That(tama.form, Is.EqualTo(StarEggEmmentalEvolutionSystem.EmmentalEvolutionId));
            Assert.That(state.emmentalEvolutionUnlocked, Is.True);
            Assert.That(state.emmentalEvolutionAtIso, Is.EqualTo(FixedNow.ToString("O")));
            Assert.That(first.CreateMilestone("star_special_001", tama.level), Is.Not.Null);

            var duplicate = system.TryApplyEvolution(
                tama,
                CreateUnlockedRoute(),
                state,
                "emmental_001",
                FixedNow.AddHours(1));
            Assert.That(duplicate.status, Is.EqualTo(EmmentalEvolutionAttemptStatus.AlreadyApplied));
            Assert.That(state.emmentalEvolutionAtIso, Is.EqualTo(FixedNow.ToString("O")));
        }

        [Test]
        public void EmmentalEvolutionRejectsNormalEggAndIncompleteSignalsWithoutMutation()
        {
            var system = new StarEggEmmentalEvolutionSystem();
            var regular = CreateFinalTama("egg_cream");
            var state = new StarLegacySaveData
            {
                starMilkCareCount = StarEggEmmentalEvolutionSystem.RequiredStarMilkCareCount,
                fantasyResonance = StarEggEmmentalEvolutionSystem.RequiredFantasyResonance
            };

            var wrongEgg = system.TryApplyEvolution(
                regular,
                CreateUnlockedRoute(),
                state,
                "regular_egg_attempt",
                FixedNow);
            Assert.That(wrongEgg.status, Is.EqualTo(EmmentalEvolutionAttemptStatus.NotStarEggOrigin));
            Assert.That(regular.evolutionId, Is.Empty);
            Assert.That(state.emmentalEvolutionUnlocked, Is.False);

            var star = CreateFinalTama(StarEggEmmentalEvolutionSystem.StarEggTypeId);
            state.fantasyResonance = 0;
            var noFantasy = system.TryApplyEvolution(
                star,
                CreateUnlockedRoute(),
                state,
                "no_fantasy_attempt",
                FixedNow);
            Assert.That(noFantasy.status, Is.EqualTo(EmmentalEvolutionAttemptStatus.FantasySignalIncomplete));
            Assert.That(star.evolutionId, Is.Empty);
            Assert.That(state.HasAppliedEvolutionReceipt("no_fantasy_attempt"), Is.False);
        }

        [Test]
        public void FinalMaturationCarriesOverflowAndQueuesEveryCompletedCycle()
        {
            var system = new FinalMaturationCycleSystem();
            var state = new FinalMaturationCycleSaveData { progress = 80 };
            var result = system.AddProgress(
                CreateFinalTama("egg_cream"),
                state,
                225,
                "maturation_001",
                starRouteUnlocked: true,
                fantasyPowderEnabled: true);

            Assert.That(result.status, Is.EqualTo(FinalMaturationProgressStatus.Applied));
            Assert.That(result.previousProgress, Is.EqualTo(80));
            Assert.That(result.currentProgress, Is.EqualTo(5));
            Assert.That(result.completedCycles, Is.EqualTo(3));
            Assert.That(result.generatedRewards.Count, Is.EqualTo(3));
            Assert.That(state.completedCycles, Is.EqualTo(3));
            Assert.That(state.pendingRewards, Has.Count.EqualTo(3));
            Assert.That(state.pendingRewards[2].starDrops, Is.EqualTo(1));
            Assert.That(state.pendingRewards[2].fantasyPowder, Is.Zero);

            var duplicate = system.AddProgress(
                CreateFinalTama("egg_cream"),
                state,
                225,
                "maturation_001",
                true,
                true);
            Assert.That(duplicate.status, Is.EqualTo(FinalMaturationProgressStatus.AlreadyApplied));
            Assert.That(state.progress, Is.EqualTo(5));
            Assert.That(state.pendingRewards, Has.Count.EqualTo(3));
        }

        [Test]
        public void SeventhMaturationCycleAddsFantasyPowderAndClaimIsTransactional()
        {
            var system = new FinalMaturationCycleSystem();
            var state = new FinalMaturationCycleSaveData { completedCycles = 6 };
            var economy = new EconomySaveData();
            var powder = new FantasyPowderSaveData();

            var progression = system.AddProgress(
                CreateFinalTama("egg_cream"),
                state,
                100,
                "cycle_seven",
                starRouteUnlocked: true,
                fantasyPowderEnabled: true);
            Assert.That(progression.generatedRewards[0].cycleNumber, Is.EqualTo(7));
            Assert.That(progression.generatedRewards[0].fantasyPowder, Is.EqualTo(1));

            var claim = system.TryClaimNext(state, economy, powder, "claim_seven");
            Assert.That(claim.status, Is.EqualTo(FinalMaturationClaimStatus.Applied));
            Assert.That(economy.milkCoins, Is.EqualTo(FinalMaturationCycleSystem.BaseMilkCoins));
            Assert.That(economy.milkDrops, Is.EqualTo(FinalMaturationCycleSystem.BaseMilkDrops));
            Assert.That(powder.powderQuantity, Is.EqualTo(1));
            Assert.That(state.claimedCycles, Is.EqualTo(7));
            Assert.That(state.pendingRewards, Is.Empty);

            var duplicate = system.TryClaimNext(state, economy, powder, "claim_seven");
            Assert.That(duplicate.status, Is.EqualTo(FinalMaturationClaimStatus.AlreadyApplied));
            Assert.That(economy.milkCoins, Is.EqualTo(FinalMaturationCycleSystem.BaseMilkCoins));
            Assert.That(powder.powderQuantity, Is.EqualTo(1));
        }

        [Test]
        public void SaveDtosNormalizeCorruptedAndDuplicateValues()
        {
            var state = new StarLegacySaveData
            {
                schemaVersion = -10,
                starMilkCareCount = -2,
                fantasyResonance = int.MaxValue,
                emmentalEvolutionUnlocked = false,
                emmentalEvolutionAtIso = FixedNow.ToString("O"),
                appliedEvolutionReceiptKeys = new System.Collections.Generic.List<string>
                {
                    " alpha ",
                    "alpha",
                    null
                },
                maturationCycle = new FinalMaturationCycleSaveData
                {
                    progress = 180,
                    completedCycles = -1,
                    claimedCycles = 99
                }
            };

            Assert.That(state.EnsureRuntimeDefaults(), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(StarLegacySaveData.CurrentSchemaVersion));
            Assert.That(state.starMilkCareCount, Is.Zero);
            Assert.That(state.fantasyResonance, Is.EqualTo(StarLegacySaveData.MaximumSignalCount));
            Assert.That(state.emmentalEvolutionAtIso, Is.Empty);
            Assert.That(state.appliedEvolutionReceiptKeys, Is.EqualTo(new[] { "alpha" }));
            Assert.That(state.maturationCycle.progress, Is.EqualTo(99));
            Assert.That(state.maturationCycle.completedCycles, Is.Zero);
            Assert.That(state.maturationCycle.claimedCycles, Is.Zero);
        }

        [Test]
        public void EmmentalPresenterBuildsExactlySevenHolesAndHidesForOtherForms()
        {
            var root = new GameObject("Emmental Presenter Test");
            try
            {
                var presenter = root.AddComponent<EmmentalConstellationPresenter>();
                presenter.Configure(root.transform);
                presenter.Bind(CreateFinalTama("egg_cream"));
                Assert.That(presenter.IsVisible, Is.False);
                Assert.That(presenter.VisibleHoleCount, Is.Zero);

                var emmental = CreateFinalTama(StarEggEmmentalEvolutionSystem.StarEggTypeId);
                emmental.evolutionId = StarEggEmmentalEvolutionSystem.EmmentalEvolutionId;
                presenter.Bind(emmental);

                Assert.That(presenter.IsVisible, Is.True);
                Assert.That(presenter.VisibleHoleCount, Is.EqualTo(EmmentalConstellationPresenter.HoleCount));
                Assert.That(
                    root.transform.Find("Emmental Constellation Visual").childCount,
                    Is.EqualTo(EmmentalConstellationPresenter.HoleCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PanelUsesIndirectHintAndDelegatesCommandsWithoutOwningState()
        {
            var root = new GameObject("Star Legacy Panel Test");
            var panel = new GameObject(StarLegacyPanelController.OverlayObjectName);
            panel.transform.SetParent(root.transform, false);
            var open = CreateButton(root.transform, "Open");
            var close = CreateButton(panel.transform, "Close");
            var evolve = CreateButton(panel.transform, "Evolve");
            var claim = CreateButton(panel.transform, "Claim");
            var slider = new GameObject("Slider").AddComponent<Slider>();
            slider.transform.SetParent(panel.transform, false);
            var routeText = CreateText(panel.transform, "Route");
            var progressText = CreateText(panel.transform, "Progress");
            var rewardText = CreateText(panel.transform, "Reward");
            var statusText = CreateText(panel.transform, "Status");
            var titleText = CreateText(panel.transform, "Title");
            var evolveCalls = 0;
            var claimCalls = 0;

            try
            {
                var controller = root.AddComponent<StarLegacyPanelController>();
                controller.Configure(
                    panel,
                    titleText,
                    routeText,
                    slider,
                    progressText,
                    rewardText,
                    statusText,
                    evolve,
                    claim,
                    close,
                    open,
                    () => new StarLegacyPanelViewModel(
                        true,
                        false,
                        true,
                        "일곱 개의 빛이 서로를 찾았어요.",
                        44,
                        100,
                        2,
                        1,
                        new FinalMaturationCycleSystem().CreateReward(3, true, false)),
                    () =>
                    {
                        evolveCalls += 1;
                        return new EmmentalEvolutionAttemptResult(
                            EmmentalEvolutionAttemptStatus.Applied,
                            "ui_evolve",
                            new NormalEvolutionResult(
                                StarEggEmmentalEvolutionSystem.Profile,
                                14),
                            FixedNow.ToString("O"));
                    },
                    () =>
                    {
                        claimCalls += 1;
                        return new FinalMaturationClaimResult(
                            FinalMaturationClaimStatus.Applied,
                            "ui_claim",
                            new FinalMaturationCycleSystem().CreateReward(3, true, false));
                    });

                Assert.That(open.gameObject.activeSelf, Is.True);
                Assert.That(routeText.text, Is.EqualTo("일곱 개의 빛이 서로를 찾았어요."));
                Assert.That(routeText.text, Does.Not.Contain("7/7"));
                Assert.That(slider.value, Is.EqualTo(44));
                Assert.That(evolve.interactable, Is.True);
                Assert.That(claim.interactable, Is.True);

                evolve.onClick.Invoke();
                claim.onClick.Invoke();
                Assert.That(evolveCalls, Is.EqualTo(1));
                Assert.That(claimCalls, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static CheeseTamaModel CreateFinalTama(string eggType)
        {
            return new CheeseTamaModel
            {
                eggType = eggType,
                isHatched = true,
                level = UnlockSystem.MaxLevel,
                maxLevel = UnlockSystem.MaxLevel,
                evolutionId = string.Empty,
                form = "growth_stage_final"
            };
        }

        private static UnlockSaveData CreateUnlockedRoute()
        {
            return new UnlockSaveData
            {
                starEggUnlocked = true,
                starMilkUnlocked = true,
                fantasyPowderEnabled = true
            };
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<RectTransform>();
            return gameObject.AddComponent<Button>();
        }

        private static Text CreateText(Transform parent, string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<RectTransform>();
            return gameObject.AddComponent<Text>();
        }
    }
}
