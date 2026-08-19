using System;
using System.IO;
using System.Reflection;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Save;
using CheeseTama.Core;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests
{
    public sealed class FirstMeetingOnboardingTests
    {
        [Test]
        public void NewSaveStartsAtWelcome()
        {
            var saveData = SaveManager.CreateDefaultSave();

            Assert.That(saveData.onboarding, Is.Not.Null);
            Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.Welcome));
            Assert.That(saveData.onboarding.completed, Is.False);
            Assert.That(saveData.onboarding.skipped, Is.False);
        }

        [Test]
        public void LegacySaveWithoutOnboardingIsTreatedAsCompleted()
        {
            var lastSavedAtIso = DateTimeOffset.Now.AddHours(-3).ToString("O");
            var legacyJson = $"{{\"playerId\":\"legacy\",\"cheeseTama\":{{\"name\":\"Legacy Tama\",\"lastSavedAtIso\":\"{lastSavedAtIso}\"}}}}";
            var testFileName = $"cheesetama_onboarding_test_{Guid.NewGuid():N}.json";
            var managerObject = new GameObject("SaveManager Test");
            managerObject.SetActive(false);
            var saveManager = managerObject.AddComponent<SaveManager>();
            var fileNameField = typeof(SaveManager).GetField(
                "saveFileName",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fileNameField, Is.Not.Null);
            fileNameField.SetValue(saveManager, testFileName);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveManager.SaveFilePath));
                File.WriteAllText(saveManager.SaveFilePath, legacyJson);

                var saveData = saveManager.LoadOrCreate();

                Assert.That(saveData.playerId, Is.EqualTo("legacy"));
                Assert.That(saveData.cheeseTama.name, Is.EqualTo("Legacy Tama"));
                Assert.That(saveData.cheeseTama.lastSavedAtIso, Is.EqualTo(lastSavedAtIso));
                Assert.That(saveData.onboarding.completed, Is.True);
                Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.Complete));
                Assert.That(saveManager.LastLoadMigratedData, Is.True);
                Assert.That(File.ReadAllText(saveManager.SaveFilePath), Does.Not.Contain("\"onboarding\""));
            }
            finally
            {
                saveManager.DeleteSave();

                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void GameLoadAppliesOfflineProgressBeforePersistingLegacyMigration()
        {
            var lastSavedAtIso = DateTimeOffset.Now.AddHours(-3).ToString("O");
            var legacyJson = $"{{\"playerId\":\"legacy\",\"cheeseTama\":{{\"name\":\"Legacy Tama\",\"lastSavedAtIso\":\"{lastSavedAtIso}\"}}}}";
            var testFileName = $"cheesetama_onboarding_test_{Guid.NewGuid():N}.json";
            var managerObject = new GameObject("GameManager Migration Test");
            managerObject.SetActive(false);
            var saveManager = managerObject.AddComponent<SaveManager>();
            var gameManager = managerObject.AddComponent<GameManager>();
            var fileNameField = typeof(SaveManager).GetField(
                "saveFileName",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var saveManagerField = typeof(GameManager).GetField(
                "saveManager",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fileNameField, Is.Not.Null);
            Assert.That(saveManagerField, Is.Not.Null);
            fileNameField.SetValue(saveManager, testFileName);
            saveManagerField.SetValue(gameManager, saveManager);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveManager.SaveFilePath));
                File.WriteAllText(saveManager.SaveFilePath, legacyJson);

                gameManager.LoadOrCreateGame();

                Assert.That(gameManager.LastTimeProgression.applied, Is.True);
                Assert.That(gameManager.LastTimeProgression.hours, Is.GreaterThanOrEqualTo(2));
                Assert.That(gameManager.CurrentSave.onboarding.completed, Is.True);
                Assert.That(File.ReadAllText(saveManager.SaveFilePath), Does.Contain("\"onboarding\""));
            }
            finally
            {
                saveManager.DeleteSave();

                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void VersionOneNamingStepMigratesToFeedMilkWithoutChangingName()
        {
            const string legacyName = "기존 이름";
            var legacyJson = "{\"playerId\":\"mid_tutorial\","
                + "\"cheeseTama\":{\"name\":\"기존 이름\",\"hasCustomName\":true},"
                + "\"onboarding\":{\"schemaVersion\":1,\"currentStep\":1,"
                + "\"completed\":false,\"firstCollectionRewardGranted\":true}}";
            var testFileName = $"cheesetama_onboarding_test_{Guid.NewGuid():N}.json";
            var managerObject = new GameObject("Onboarding Step Migration Test");
            managerObject.SetActive(false);
            var saveManager = managerObject.AddComponent<SaveManager>();
            var fileNameField = typeof(SaveManager).GetField(
                "saveFileName",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fileNameField, Is.Not.Null);
            fileNameField.SetValue(saveManager, testFileName);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveManager.SaveFilePath));
                File.WriteAllText(saveManager.SaveFilePath, legacyJson);

                var saveData = saveManager.LoadOrCreate();

                Assert.That(saveManager.LastLoadMigratedData, Is.True);
                Assert.That(saveData.onboarding.schemaVersion, Is.EqualTo(OnboardingSaveData.CurrentSchemaVersion));
                Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.FeedMilk));
                Assert.That(saveData.onboarding.firstCollectionRewardGranted, Is.True);
                Assert.That(saveData.cheeseTama.name, Is.EqualTo(legacyName));
                Assert.That(saveData.cheeseTama.hasCustomName, Is.True);
            }
            finally
            {
                saveManager.DeleteSave();

                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [TestCase(0, FirstMeetingOnboardingStep.Welcome)]
        [TestCase(1, FirstMeetingOnboardingStep.FeedMilk)]
        [TestCase(2, FirstMeetingOnboardingStep.FeedMilk)]
        [TestCase(3, FirstMeetingOnboardingStep.Care)]
        [TestCase(4, FirstMeetingOnboardingStep.Collection)]
        [TestCase(5, FirstMeetingOnboardingStep.Complete)]
        public void VersionOneStepNumbersRemainCompatible(
            int serializedStep,
            FirstMeetingOnboardingStep expectedStep)
        {
            var onboarding = new OnboardingSaveData
            {
                schemaVersion = 1,
                currentStep = (FirstMeetingOnboardingStep)serializedStep
            };

            Assert.That(onboarding.EnsureRuntimeDefaults(), Is.True);
            Assert.That(onboarding.currentStep, Is.EqualTo(expectedStep));
            Assert.That(
                onboarding.completed,
                Is.EqualTo(expectedStep == FirstMeetingOnboardingStep.Complete));
        }

        [Test]
        public void ResetProgressPreservesCompletedOnboardingAndSettings()
        {
            var testFileName = $"cheesetama_onboarding_test_{Guid.NewGuid():N}.json";
            var managerObject = new GameObject("GameManager Reset Test");
            managerObject.SetActive(false);
            var saveManager = managerObject.AddComponent<SaveManager>();
            var gameManager = managerObject.AddComponent<GameManager>();
            var fileNameField = typeof(SaveManager).GetField(
                "saveFileName",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var saveManagerField = typeof(GameManager).GetField(
                "saveManager",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fileNameField, Is.Not.Null);
            Assert.That(saveManagerField, Is.Not.Null);
            fileNameField.SetValue(saveManager, testFileName);
            saveManagerField.SetValue(gameManager, saveManager);

            try
            {
                gameManager.LoadOrCreateGame();
                gameManager.CurrentSave.settings.muteAudio = true;
                gameManager.CurrentSave.onboarding.currentStep = FirstMeetingOnboardingStep.Complete;
                gameManager.CurrentSave.onboarding.completed = true;
                gameManager.SaveGame();

                var saveDataReplacedCount = 0;
                var preservedMuteWasVisibleToListeners = false;
                gameManager.SaveDataReplaced += () =>
                {
                    saveDataReplacedCount++;
                    preservedMuteWasVisibleToListeners = gameManager.CurrentSave.settings.muteAudio;
                };
                gameManager.ResetProgress();

                Assert.That(saveDataReplacedCount, Is.EqualTo(1));
                Assert.That(preservedMuteWasVisibleToListeners, Is.True);
                Assert.That(gameManager.CurrentSave.settings.muteAudio, Is.True);
                Assert.That(gameManager.CurrentSave.onboarding.completed, Is.True);
                Assert.That(
                    gameManager.CurrentSave.onboarding.currentStep,
                    Is.EqualTo(FirstMeetingOnboardingStep.Complete));
            }
            finally
            {
                saveManager.DeleteSave();

                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void ExpectedSignalsCompleteTheTutorialWithoutChangingName()
        {
            var saveData = SaveManager.CreateDefaultSave();
            var originalName = saveData.cheeseTama.name;
            var originalCustomNameFlag = saveData.cheeseTama.hasCustomName;

            AssertSignal(saveData, FirstMeetingOnboardingSignal.Continue);
            Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.FeedMilk));

            AssertSignal(saveData, FirstMeetingOnboardingSignal.MilkFeedSucceeded);
            Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.Care));

            AssertSignal(saveData, FirstMeetingOnboardingSignal.CareSucceeded);
            Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.Collection));

            AssertSignal(saveData, FirstMeetingOnboardingSignal.CollectionOpened);
            Assert.That(saveData.onboarding.completed, Is.True);
            Assert.That(saveData.onboarding.skipped, Is.False);
            Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.Complete));
            Assert.That(saveData.cheeseTama.name, Is.EqualTo(originalName));
            Assert.That(saveData.cheeseTama.hasCustomName, Is.EqualTo(originalCustomNameFlag));
        }

        [Test]
        public void UnexpectedSignalDoesNotAdvance()
        {
            var saveData = SaveManager.CreateDefaultSave();

            var changed = FirstMeetingOnboardingSystem.TryApply(
                saveData,
                FirstMeetingOnboardingSignal.CareSucceeded,
                out var errorMessage);

            Assert.That(changed, Is.False);
            Assert.That(errorMessage, Is.Empty);
            Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.Welcome));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("1234567890123")]
        [TestCase("몽글\n치즈")]
        public void InvalidNameIsRejected(string requestedName)
        {
            var changed = CheeseTamaNameSystem.TryNormalize(
                requestedName,
                out var normalizedName,
                out var errorMessage);

            Assert.That(changed, Is.False);
            Assert.That(normalizedName, Is.Not.Null);
            Assert.That(errorMessage, Is.Not.Empty);
        }

        [Test]
        public void RenameIsTrimmedSavedAndReloaded()
        {
            var testFileName = $"cheesetama_onboarding_test_{Guid.NewGuid():N}.json";
            var managerObject = new GameObject("GameManager Rename Test");
            managerObject.SetActive(false);
            var saveManager = managerObject.AddComponent<SaveManager>();
            var gameManager = managerObject.AddComponent<GameManager>();
            var fileNameField = typeof(SaveManager).GetField(
                "saveFileName",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var saveManagerField = typeof(GameManager).GetField(
                "saveManager",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fileNameField, Is.Not.Null);
            Assert.That(saveManagerField, Is.Not.Null);
            fileNameField.SetValue(saveManager, testFileName);
            saveManagerField.SetValue(gameManager, saveManager);

            try
            {
                gameManager.LoadOrCreateGame();

                Assert.That(gameManager.TryRenameCurrentTama("  몽글이  ", out var errorMessage), Is.True);
                Assert.That(errorMessage, Is.Empty);
                Assert.That(gameManager.CurrentTama.name, Is.EqualTo("몽글이"));
                Assert.That(gameManager.CurrentTama.hasCustomName, Is.True);

                var reloaded = saveManager.LoadOrCreate();
                Assert.That(reloaded.cheeseTama.name, Is.EqualTo("몽글이"));
                Assert.That(reloaded.cheeseTama.hasCustomName, Is.True);
            }
            finally
            {
                saveManager.DeleteSave();

                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void SkipAndReplayPreserveRewardGuard()
        {
            var saveData = SaveManager.CreateDefaultSave();
            saveData.onboarding.firstCollectionRewardGranted = true;

            AssertSignal(saveData, FirstMeetingOnboardingSignal.Skip);
            Assert.That(saveData.onboarding.completed, Is.True);
            Assert.That(saveData.onboarding.skipped, Is.True);

            Assert.That(FirstMeetingOnboardingSystem.StartReplay(saveData), Is.True);
            Assert.That(saveData.onboarding.completed, Is.False);
            Assert.That(saveData.onboarding.skipped, Is.False);
            Assert.That(saveData.onboarding.replaying, Is.True);
            Assert.That(saveData.onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.Welcome));
            Assert.That(saveData.onboarding.firstCollectionRewardGranted, Is.True);
        }

        [Test]
        public void InvalidSerializedStepFailsClosed()
        {
            var onboarding = OnboardingSaveData.CreateForNewPlayer();
            onboarding.currentStep = (FirstMeetingOnboardingStep)999;

            onboarding.EnsureRuntimeDefaults();

            Assert.That(onboarding.completed, Is.True);
            Assert.That(onboarding.currentStep, Is.EqualTo(FirstMeetingOnboardingStep.Complete));
        }

        [Test]
        public void HatchingPreservesCustomName()
        {
            var tama = new CheeseTamaModel
            {
                level = HatchingSystem.HatchLevel,
                name = "몽글이",
                hasCustomName = true
            };

            Assert.That(new HatchingSystem().TryHatch(tama), Is.True);
            Assert.That(tama.name, Is.EqualTo("몽글이"));
        }

        [Test]
        public void LegacyCustomNameIsProtectedAtHatch()
        {
            var tama = new CheeseTamaModel
            {
                level = HatchingSystem.HatchLevel,
                name = "Legacy Tama",
                hasCustomName = false
            };

            tama.EnsureRuntimeDefaults();

            Assert.That(tama.hasCustomName, Is.True);
            Assert.That(new HatchingSystem().TryHatch(tama), Is.True);
            Assert.That(tama.name, Is.EqualTo("Legacy Tama"));
        }

        [Test]
        public void BuilderCreatesOnboardingOverlayAndReplayControl()
        {
            var canvasObject = new GameObject(
                "Onboarding Builder Test Canvas",
                typeof(RectTransform),
                typeof(Canvas));

            try
            {
                var milkroomUi = canvasObject.AddComponent<MilkroomUIController>();
                canvasObject.AddComponent<TopMenuController>();
                canvasObject.AddComponent<MilkPanelController>();
                canvasObject.AddComponent<CookingPanelController>();
                canvasObject.AddComponent<SnackPanelController>();

                CreateRectChild(canvasObject.transform, "Settings Modal");
                var bottomBar = CreateRectChild(canvasObject.transform, "Bottom Action Bar");
                CreateButton(bottomBar.transform, "Milk Button");
                CreateButton(bottomBar.transform, "Blend Button");
                CreateButton(bottomBar.transform, "Snack Button");
                CreateButton(bottomBar.transform, "Play Button");
                CreateButton(bottomBar.transform, "Clean Button");
                CreateButton(bottomBar.transform, "Sleep Button");

                var topMenu = CreateRectChild(canvasObject.transform, "Top Menu");
                CreateButton(topMenu.transform, "Top Collection Button");
                CreateButton(topMenu.transform, "Top Decorate Button");
                CreateButton(topMenu.transform, "Settings Button");
                CreateButton(canvasObject.transform, "Dev Mode Toggle Button");

                var ensureMethod = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureFirstMeetingOnboarding",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(ensureMethod, Is.Not.Null);
                ensureMethod.Invoke(null, new object[] { canvasObject.transform, milkroomUi, null });

                var ensureNameDialogMethod = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureCheeseTamaNameDialog",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(ensureNameDialogMethod, Is.Not.Null);
                var ensureProfileShellMethod = typeof(StarterSceneBuilder).GetMethod(
                    "EnsureCheeseTamaProfileMenuShell",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(ensureProfileShellMethod, Is.Not.Null);
                ensureProfileShellMethod.Invoke(null, new object[] { canvasObject.transform });
                ensureNameDialogMethod.Invoke(null, new object[] { canvasObject.transform, milkroomUi });

                var overlay = canvasObject.transform.Find("First Meeting Onboarding Overlay");
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                var onboardingController = canvasObject.GetComponent<FirstMeetingOnboardingController>();
                Assert.That(onboardingController, Is.Not.Null);
                Assert.That(
                    canvasObject.transform.Find("Settings Modal/Replay First Meeting Button"),
                    Is.Not.Null);
                Assert.That(
                    canvasObject.transform.Find("Settings Modal/Replay First Meeting Button/Label")
                        ?.GetComponent<Text>()?.text,
                    Is.EqualTo("튜토리얼"));

                var stepText = overlay.Find("First Meeting Card/First Meeting Step Text")
                    ?.GetComponent<Text>();
                Assert.That(stepText, Is.Not.Null);
                Assert.That(stepText.text, Is.EqualTo("튜토리얼 · 1/4"));
                var bodyText = overlay.Find("First Meeting Card/First Meeting Body Text")
                    ?.GetComponent<Text>();
                Assert.That(bodyText, Is.Not.Null);
                Assert.That(bodyText.alignment, Is.EqualTo(TextAnchor.MiddleCenter));

                var skipConfirmation = overlay.Find("Skip Tutorial Confirmation");
                Assert.That(skipConfirmation, Is.Not.Null);
                Assert.That(skipConfirmation.gameObject.activeSelf, Is.False);
                Assert.That(skipConfirmation.GetComponent<Image>()?.raycastTarget, Is.True);
                Assert.That(skipConfirmation.GetComponent<CanvasGroup>()?.blocksRaycasts, Is.True);
                Assert.That(
                    skipConfirmation.Find(
                            "Skip Tutorial Confirmation Card/Skip Tutorial Confirmation Title Text")
                        ?.GetComponent<Text>()?.text,
                    Is.EqualTo("튜토리얼을 건너뛰시겠습니까?"));
                var continueTutorialButton = skipConfirmation.Find(
                        "Skip Tutorial Confirmation Card/Continue Tutorial Button")
                    ?.GetComponent<Button>();
                var confirmSkipButton = skipConfirmation.Find(
                        "Skip Tutorial Confirmation Card/Confirm Skip Tutorial Button")
                    ?.GetComponent<Button>();
                Assert.That(continueTutorialButton, Is.Not.Null);
                Assert.That(confirmSkipButton, Is.Not.Null);
                Assert.That(
                    continueTutorialButton.transform.Find("Label")?.GetComponent<Text>()?.text,
                    Is.EqualTo("계속 진행"));
                Assert.That(
                    confirmSkipButton.transform.Find("Label")?.GetComponent<Text>()?.text,
                    Is.EqualTo("건너뛰기"));

                var skipButton = overlay.Find("First Meeting Card/First Meeting Skip Button")
                    ?.GetComponent<Button>();
                Assert.That(skipButton, Is.Not.Null);
                skipButton.onClick.Invoke();
                Assert.That(skipConfirmation.gameObject.activeSelf, Is.True);
                continueTutorialButton.onClick.Invoke();
                Assert.That(skipConfirmation.gameObject.activeSelf, Is.False);

                Assert.That(overlay.Find("First Meeting Card/First Meeting Name Input"), Is.Null);
                Assert.That(canvasObject.GetComponent<CheeseTamaNameDialogController>(), Is.Not.Null);
                Assert.That(
                    canvasObject.transform.Find(
                            "CheeseTama Profile Overlay/Profile Card/Open Name Change Button/Label")
                        ?.GetComponent<Text>()?.text,
                    Is.EqualTo("이름 변경"));
                Assert.That(
                    canvasObject.transform.Find("Settings Modal/Open Name Change Button"),
                    Is.Null);
                var nameDialog = canvasObject.transform.Find("CheeseTama Name Dialog");
                Assert.That(nameDialog, Is.Not.Null);
                Assert.That(nameDialog.gameObject.activeSelf, Is.False);
                var nameInput = nameDialog.Find("Name Change Card/Name Change Input")
                    ?.GetComponent<InputField>();
                Assert.That(nameInput, Is.Not.Null);
                Assert.That(nameInput.characterLimit, Is.EqualTo(CheeseTamaNameSystem.MaximumNameLength));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static GameObject CreateRectChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Button>();
        }

        private static void AssertSignal(
            CheeseTamaSaveData saveData,
            FirstMeetingOnboardingSignal signal)
        {
            var changed = FirstMeetingOnboardingSystem.TryApply(
                saveData,
                signal,
                out var errorMessage);

            Assert.That(changed, Is.True, errorMessage);
            Assert.That(errorMessage, Is.Empty);
        }
    }
}
