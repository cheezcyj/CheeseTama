using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.NewGameSetup;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class NewGameSetupFeatureTests
    {
        [Test]
        public void CatalogContainsFiveRegularEggsWithoutStarEgg()
        {
            Assert.That(NewGameSetupCatalog.EggChoices.Count, Is.EqualTo(5));

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var egg in NewGameSetupCatalog.EggChoices)
            {
                Assert.That(ids.Add(egg.Id), Is.True, $"Duplicate egg id: {egg.Id}");
                Assert.That(egg.Id, Does.Not.Contain("star"));
                Assert.That(egg.DisplayName, Is.Not.Empty);
                Assert.That(egg.Description, Is.Not.Empty);
            }

            CollectionAssert.AreEquivalent(
                new[]
                {
                    NewGameSetupCatalog.CreamEggId,
                    NewGameSetupCatalog.ButterEggId,
                    NewGameSetupCatalog.StrawberryEggId,
                    NewGameSetupCatalog.MintEggId,
                    NewGameSetupCatalog.CoffeeEggId
                },
                ids);
        }

        [Test]
        public void EggAndFirstMilkCompleteWithDeterministicTemperamentSeed()
        {
            var state = NewGameSetupSaveData.CreateForNewPlayer();

            Assert.That(
                NewGameSetupSystem.TrySelectEgg(
                    state,
                    NewGameSetupCatalog.ButterEggId,
                    out var errorMessage),
                Is.True,
                errorMessage);
            Assert.That(NewGameSetupSystem.TryAdvance(state, out errorMessage), Is.True, errorMessage);
            Assert.That(
                NewGameSetupSystem.TrySelectFirstMilk(
                    state,
                    NewGameSetupCatalog.WarmFirstMilkId,
                    out errorMessage),
                Is.True,
                errorMessage);
            Assert.That(NewGameSetupSystem.TryAdvance(state, out errorMessage), Is.True, errorMessage);

            Assert.That(state.completed, Is.True);
            Assert.That(state.skipped, Is.False);
            Assert.That(state.legacySuppressed, Is.False);
            Assert.That(state.currentStep, Is.EqualTo(NewGameSetupStep.Complete));
            Assert.That(state.temperamentSeed.seedKey, Is.EqualTo(
                "setup:v1:egg_butter:warm_milk"));
            Assert.That(state.temperamentSeed.dominantTraitId, Is.EqualTo(
                NewGameSetupCatalog.LivelyTraitId));
            Assert.That(state.temperamentSeed.activity, Is.EqualTo(40));
            Assert.That(state.temperamentSeed.expressiveness, Is.EqualTo(30));
            Assert.That(SumScores(state.temperamentSeed), Is.EqualTo(100));

            Assert.That(
                NewGameSetupCatalog.TryCreateTemperamentSeed(
                    NewGameSetupCatalog.ButterEggId,
                    NewGameSetupCatalog.WarmFirstMilkId,
                    out var repeatedSeed),
                Is.True);
            Assert.That(repeatedSeed.HasSameValues(state.temperamentSeed), Is.True);
        }

        [Test]
        public void EveryEggAndMilkPairHasAUniqueStableSeedKey()
        {
            Assert.That(NewGameSetupCatalog.FirstMilkChoices.Count, Is.EqualTo(5));
            var seedKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var egg in NewGameSetupCatalog.EggChoices)
            {
                foreach (var milk in NewGameSetupCatalog.FirstMilkChoices)
                {
                    Assert.That(
                        NewGameSetupCatalog.TryCreateTemperamentSeed(
                            egg.Id,
                            milk.Id,
                            out var seed),
                        Is.True);
                    Assert.That(seedKeys.Add(seed.seedKey), Is.True, seed.seedKey);
                    Assert.That(SumScores(seed), Is.EqualTo(100));
                    Assert.That(seed.dominantTraitId, Is.Not.Empty);
                }
            }

            Assert.That(seedKeys.Count, Is.EqualTo(25));
        }

        [Test]
        public void ProgressCannotAdvanceWithoutTheRequiredSelection()
        {
            var state = NewGameSetupSaveData.CreateForNewPlayer();

            Assert.That(NewGameSetupSystem.TryAdvance(state, out var errorMessage), Is.False);
            Assert.That(errorMessage, Is.Not.Empty);
            Assert.That(state.currentStep, Is.EqualTo(NewGameSetupStep.EggSelection));

            Assert.That(
                NewGameSetupSystem.TrySelectEgg(state, "egg_unknown", out errorMessage),
                Is.False);
            Assert.That(errorMessage, Is.Not.Empty);

            Assert.That(
                NewGameSetupSystem.TrySelectEgg(
                    state,
                    NewGameSetupCatalog.CreamEggId,
                    out errorMessage),
                Is.True);
            Assert.That(NewGameSetupSystem.TryAdvance(state, out errorMessage), Is.True);
            Assert.That(NewGameSetupSystem.TryAdvance(state, out errorMessage), Is.False);
            Assert.That(errorMessage, Is.Not.Empty);
            Assert.That(state.completed, Is.False);
        }

        [Test]
        public void JsonRoundTripResumesAtFirstMilkWithoutLosingEgg()
        {
            var state = NewGameSetupSaveData.CreateForNewPlayer();
            Assert.That(
                NewGameSetupSystem.TrySelectEgg(
                    state,
                    NewGameSetupCatalog.MintEggId,
                    out var errorMessage),
                Is.True,
                errorMessage);
            Assert.That(NewGameSetupSystem.TryAdvance(state, out errorMessage), Is.True, errorMessage);

            var json = JsonUtility.ToJson(state);
            var resumed = JsonUtility.FromJson<NewGameSetupSaveData>(json);

            Assert.That(resumed.EnsureRuntimeDefaults(), Is.False);
            Assert.That(resumed.completed, Is.False);
            Assert.That(resumed.currentStep, Is.EqualTo(NewGameSetupStep.FirstMilkSelection));
            Assert.That(resumed.selectedEggId, Is.EqualTo(NewGameSetupCatalog.MintEggId));
            Assert.That(resumed.selectedFirstMilkId, Is.Empty);
        }

        [Test]
        public void SkipUsesNeutralSeedAndLegacyFactorySuppressesTheOverlayContract()
        {
            var skipped = NewGameSetupSaveData.CreateForNewPlayer();
            Assert.That(
                NewGameSetupSystem.TrySelectEgg(
                    skipped,
                    NewGameSetupCatalog.CoffeeEggId,
                    out var errorMessage),
                Is.True,
                errorMessage);
            Assert.That(NewGameSetupSystem.TrySkip(skipped, out errorMessage), Is.True, errorMessage);

            Assert.That(skipped.completed, Is.True);
            Assert.That(skipped.skipped, Is.True);
            Assert.That(skipped.selectedEggId, Is.Empty);
            Assert.That(skipped.selectedFirstMilkId, Is.Empty);
            Assert.That(skipped.temperamentSeed.seedKey, Is.EqualTo(
                NewGameSetupCatalog.SkippedSeedKey));
            Assert.That(SumScores(skipped.temperamentSeed), Is.EqualTo(100));

            var legacy = NewGameSetupSaveData.CreateCompletedForLegacySave();
            Assert.That(legacy.EnsureRuntimeDefaults(), Is.False);
            Assert.That(legacy.completed, Is.True);
            Assert.That(legacy.skipped, Is.False);
            Assert.That(legacy.legacySuppressed, Is.True);
            Assert.That(legacy.temperamentSeed.seedKey, Is.EqualTo(
                NewGameSetupCatalog.LegacySeedKey));
            Assert.That(
                NewGameSetupSystem.TrySelectEgg(
                    legacy,
                    NewGameSetupCatalog.CreamEggId,
                    out errorMessage),
                Is.False);
        }

        [Test]
        public void InvalidIncompleteDataRecoversToEggSelection()
        {
            var state = new NewGameSetupSaveData
            {
                schemaVersion = 0,
                currentStep = (NewGameSetupStep)999,
                selectedEggId = "removed_egg",
                selectedFirstMilkId = "removed_milk",
                temperamentSeed = new InitialTemperamentSeedSaveData
                {
                    seedKey = "stale",
                    balance = 999
                }
            };

            Assert.That(state.EnsureRuntimeDefaults(), Is.True);
            Assert.That(state.schemaVersion, Is.EqualTo(NewGameSetupSaveData.CurrentSchemaVersion));
            Assert.That(state.currentStep, Is.EqualTo(NewGameSetupStep.EggSelection));
            Assert.That(state.selectedEggId, Is.Empty);
            Assert.That(state.selectedFirstMilkId, Is.Empty);
            Assert.That(state.temperamentSeed.HasAnyValue(), Is.False);
            Assert.That(state.completed, Is.False);
        }

        [Test]
        public void ControllerResumesAndCompletesThroughPersistCallbacks()
        {
            using var fixture = UiFixture.Create(NewGameSetupSaveData.CreateForNewPlayer());

            Assert.That(fixture.Overlay.activeSelf, Is.True);
            Assert.That(fixture.EggStep.activeSelf, Is.True);
            Assert.That(fixture.MilkStep.activeSelf, Is.False);
            Assert.That(fixture.BodyText.alignment, Is.EqualTo(TextAnchor.MiddleCenter));

            fixture.EggButtons[1].onClick.Invoke();
            fixture.AdvanceButton.onClick.Invoke();

            Assert.That(fixture.State.currentStep, Is.EqualTo(
                NewGameSetupStep.FirstMilkSelection));
            Assert.That(fixture.State.selectedEggId, Is.EqualTo(
                NewGameSetupCatalog.ButterEggId));
            Assert.That(fixture.EggStep.activeSelf, Is.False);
            Assert.That(fixture.MilkStep.activeSelf, Is.True);

            fixture.Controller.Configure(
                fixture.Overlay,
                fixture.EggStep,
                fixture.MilkStep,
                fixture.ProgressText,
                fixture.TitleText,
                fixture.BodyText,
                fixture.SelectionText,
                fixture.StatusText,
                fixture.EggButtons,
                fixture.EggLabels,
                fixture.MilkButtons,
                fixture.MilkLabels,
                fixture.BackButton,
                fixture.AdvanceButton,
                fixture.SkipButton,
                fixture.SkipConfirmation,
                fixture.ContinueButton,
                fixture.ConfirmSkipButton,
                () => fixture.State,
                _ => fixture.PersistCount++,
                _ => fixture.CompletionCount++);

            fixture.MilkButtons[1].onClick.Invoke();
            fixture.AdvanceButton.onClick.Invoke();

            Assert.That(fixture.State.completed, Is.True);
            Assert.That(fixture.Overlay.activeSelf, Is.False);
            Assert.That(fixture.PersistCount, Is.EqualTo(4));
            Assert.That(fixture.CompletionCount, Is.EqualTo(1));
        }

        [Test]
        public void ControllerRequiresConfirmationToSkipAndHidesLegacyState()
        {
            using (var fixture = UiFixture.Create(NewGameSetupSaveData.CreateForNewPlayer()))
            {
                fixture.SkipButton.onClick.Invoke();
                Assert.That(fixture.SkipConfirmation.activeSelf, Is.True);
                Assert.That(fixture.State.completed, Is.False);

                fixture.ContinueButton.onClick.Invoke();
                Assert.That(fixture.SkipConfirmation.activeSelf, Is.False);
                Assert.That(fixture.State.completed, Is.False);

                fixture.SkipButton.onClick.Invoke();
                fixture.ConfirmSkipButton.onClick.Invoke();
                Assert.That(fixture.State.completed, Is.True);
                Assert.That(fixture.State.skipped, Is.True);
                Assert.That(fixture.Overlay.activeSelf, Is.False);
                Assert.That(fixture.CompletionCount, Is.EqualTo(1));
            }

            using (var legacyFixture = UiFixture.Create(
                       NewGameSetupSaveData.CreateCompletedForLegacySave()))
            {
                Assert.That(legacyFixture.Overlay.activeSelf, Is.False);
                Assert.That(legacyFixture.PersistCount, Is.Zero);
                Assert.That(legacyFixture.CompletionCount, Is.Zero);
            }
        }

        private static int SumScores(InitialTemperamentSeedSaveData seed)
        {
            return seed.balance
                + seed.activity
                + seed.expressiveness
                + seed.composure
                + seed.focus;
        }

        private sealed class UiFixture : IDisposable
        {
            private readonly GameObject host;

            private UiFixture(GameObject host)
            {
                this.host = host;
            }

            public NewGameSetupController Controller { get; private set; }
            public NewGameSetupSaveData State { get; private set; }
            public GameObject Overlay { get; private set; }
            public GameObject EggStep { get; private set; }
            public GameObject MilkStep { get; private set; }
            public GameObject SkipConfirmation { get; private set; }
            public Text ProgressText { get; private set; }
            public Text TitleText { get; private set; }
            public Text BodyText { get; private set; }
            public Text SelectionText { get; private set; }
            public Text StatusText { get; private set; }
            public Button[] EggButtons { get; private set; }
            public Text[] EggLabels { get; private set; }
            public Button[] MilkButtons { get; private set; }
            public Text[] MilkLabels { get; private set; }
            public Button BackButton { get; private set; }
            public Button AdvanceButton { get; private set; }
            public Button SkipButton { get; private set; }
            public Button ContinueButton { get; private set; }
            public Button ConfirmSkipButton { get; private set; }
            public int PersistCount { get; set; }
            public int CompletionCount { get; set; }

            public static UiFixture Create(NewGameSetupSaveData state)
            {
                var host = new GameObject("New Game Setup Test Host");
                var fixture = new UiFixture(host)
                {
                    Controller = host.AddComponent<NewGameSetupController>(),
                    State = state,
                    Overlay = CreateObject("New Game Setup Overlay", host.transform),
                    ProgressText = CreateText("Progress", host.transform),
                    TitleText = CreateText("Title", host.transform),
                    BodyText = CreateText("Body", host.transform),
                    SelectionText = CreateText("Selection", host.transform),
                    StatusText = CreateText("Status", host.transform)
                };

                fixture.EggStep = CreateObject("Egg Step", fixture.Overlay.transform);
                fixture.MilkStep = CreateObject("Milk Step", fixture.Overlay.transform);
                fixture.SkipConfirmation = CreateObject(
                    "Skip Confirmation",
                    fixture.Overlay.transform);
                fixture.EggButtons = CreateButtons(
                    "Egg",
                    fixture.EggStep.transform,
                    NewGameSetupCatalog.EggChoices.Count,
                    out var eggLabels);
                fixture.EggLabels = eggLabels;
                fixture.MilkButtons = CreateButtons(
                    "Milk",
                    fixture.MilkStep.transform,
                    NewGameSetupCatalog.FirstMilkChoices.Count,
                    out var milkLabels);
                fixture.MilkLabels = milkLabels;
                fixture.BackButton = CreateButton("Back", fixture.Overlay.transform, out _);
                fixture.AdvanceButton = CreateButton("Advance", fixture.Overlay.transform, out _);
                fixture.SkipButton = CreateButton("Skip", fixture.Overlay.transform, out _);
                fixture.ContinueButton = CreateButton(
                    "Continue Setup",
                    fixture.SkipConfirmation.transform,
                    out _);
                fixture.ConfirmSkipButton = CreateButton(
                    "Confirm Skip",
                    fixture.SkipConfirmation.transform,
                    out _);

                fixture.Controller.Configure(
                    fixture.Overlay,
                    fixture.EggStep,
                    fixture.MilkStep,
                    fixture.ProgressText,
                    fixture.TitleText,
                    fixture.BodyText,
                    fixture.SelectionText,
                    fixture.StatusText,
                    fixture.EggButtons,
                    fixture.EggLabels,
                    fixture.MilkButtons,
                    fixture.MilkLabels,
                    fixture.BackButton,
                    fixture.AdvanceButton,
                    fixture.SkipButton,
                    fixture.SkipConfirmation,
                    fixture.ContinueButton,
                    fixture.ConfirmSkipButton,
                    () => fixture.State,
                    _ => fixture.PersistCount++,
                    _ => fixture.CompletionCount++);
                return fixture;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            private static Button[] CreateButtons(
                string prefix,
                Transform parent,
                int count,
                out Text[] labels)
            {
                var buttons = new Button[count];
                labels = new Text[count];
                for (var index = 0; index < count; index++)
                {
                    buttons[index] = CreateButton(
                        $"{prefix} {index}",
                        parent,
                        out labels[index]);
                }

                return buttons;
            }

            private static Button CreateButton(string name, Transform parent, out Text label)
            {
                var buttonObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(parent, false);
                label = CreateText("Label", buttonObject.transform);
                return buttonObject.GetComponent<Button>();
            }

            private static Text CreateText(string name, Transform parent)
            {
                var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(parent, false);
                return textObject.GetComponent<Text>();
            }

            private static GameObject CreateObject(string name, Transform parent)
            {
                var result = new GameObject(name, typeof(RectTransform));
                result.transform.SetParent(parent, false);
                return result;
            }
        }
    }
}
