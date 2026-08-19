using System.Collections;
using System.IO;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CheeseTama.Tests.PlayMode
{
    internal static class PlayModeSaveIsolationBootstrap
    {
        internal static string ActiveSaveFileName { get; private set; }

        internal static string ActiveSaveFilePath => string.IsNullOrWhiteSpace(ActiveSaveFileName)
            ? string.Empty
            : Path.Combine(Application.persistentDataPath, ActiveSaveFileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ConfigureBeforeRuntimeBootstrap()
        {
            ActiveSaveFileName = System.Environment.GetEnvironmentVariable(
                SaveManager.PlayModeTestSaveFileNameEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(ActiveSaveFileName))
            {
                ActiveSaveFileName = CreateIsolatedSaveFileName();
                System.Environment.SetEnvironmentVariable(
                    SaveManager.PlayModeTestSaveFileNameEnvironmentVariable,
                    ActiveSaveFileName);
            }

            SaveManager.SetPlayModeTestSaveFileNameOverride(ActiveSaveFileName);
        }

        internal static void PrepareBeforePlayModeEntry()
        {
            CleanupAfterPlayModeRun();
            ActiveSaveFileName = CreateIsolatedSaveFileName();
            System.Environment.SetEnvironmentVariable(
                SaveManager.PlayModeTestSaveFileNameEnvironmentVariable,
                ActiveSaveFileName);
        }

        internal static void CleanupAfterPlayModeRun()
        {
            var environmentFileName = System.Environment.GetEnvironmentVariable(
                SaveManager.PlayModeTestSaveFileNameEnvironmentVariable);
            if (SaveManager.IsValidPlayModeTestSaveFileName(environmentFileName))
            {
                ActiveSaveFileName = environmentFileName;
                DeleteOwnedArtifacts(Path.Combine(Application.persistentDataPath, environmentFileName));
                SaveManager.ClearPlayModeTestSaveFileNameOverride(environmentFileName);
            }

            System.Environment.SetEnvironmentVariable(
                SaveManager.PlayModeTestSaveFileNameEnvironmentVariable,
                null);
            ActiveSaveFileName = null;
        }

        internal static void RestoreActiveFileNameFromEnvironment()
        {
            var environmentFileName = System.Environment.GetEnvironmentVariable(
                SaveManager.PlayModeTestSaveFileNameEnvironmentVariable);
            if (!SaveManager.IsValidPlayModeTestSaveFileName(environmentFileName))
            {
                throw new System.InvalidOperationException(
                    "PlayMode save isolation was not configured before entering PlayMode.");
            }

            ActiveSaveFileName = environmentFileName;
        }

        internal static bool OwnsExactPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && !string.IsNullOrWhiteSpace(ActiveSaveFilePath)
                && string.Equals(
                    Path.GetFullPath(path),
                    Path.GetFullPath(ActiveSaveFilePath),
                    System.StringComparison.OrdinalIgnoreCase);
        }

        internal static void DeleteOwnedArtifacts(string primaryPath)
        {
            if (!OwnsExactPath(primaryPath))
            {
                throw new System.InvalidOperationException(
                    "Refusing to clean a save path not owned by the active PlayMode test run.");
            }

            DeleteIfPresent(primaryPath);
            DeleteIfPresent(primaryPath + ".bak");
            DeleteIfPresent(primaryPath + ".tmp");

            var directoryPath = Path.GetDirectoryName(primaryPath);
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return;
            }

            var primaryFileName = Path.GetFileName(primaryPath);
            foreach (var corruptArtifact in Directory.GetFiles(
                         directoryPath,
                         primaryFileName + "*.corrupt.*"))
            {
                DeleteIfPresent(corruptArtifact);
            }
        }

        private static string CreateIsolatedSaveFileName()
        {
            return $"{SaveManager.PlayModeTestSaveFileNamePrefix}{System.Guid.NewGuid():N}.json";
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public sealed class RuntimeSmokeTests : IPrebuildSetup, IPostBuildCleanup
    {
        private string isolatedSavePath;

        public void Setup()
        {
            PlayModeSaveIsolationBootstrap.PrepareBeforePlayModeEntry();
        }

        public void Cleanup()
        {
            PlayModeSaveIsolationBootstrap.CleanupAfterPlayModeRun();
        }

        [UnitySetUp]
        public IEnumerator SetUpTest()
        {
            yield return null;

            PlayModeSaveIsolationBootstrap.RestoreActiveFileNameFromEnvironment();
            isolatedSavePath = PlayModeSaveIsolationBootstrap.ActiveSaveFilePath;
            Assert.That(isolatedSavePath, Is.Not.Empty);
            Assert.That(PlayModeSaveIsolationBootstrap.OwnsExactPath(isolatedSavePath), Is.True);
            Assert.That(
                Path.GetFileName(isolatedSavePath),
                Does.StartWith(SaveManager.PlayModeTestSaveFileNamePrefix));

            var manager = GameManager.Instance;
            if (manager == null)
            {
                manager = StarterSceneBuilder.EnsureCoreSystems();
                yield return null;
            }

            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.SaveFilePath, Is.EqualTo(isolatedSavePath));
            Assert.That(PlayModeSaveIsolationBootstrap.OwnsExactPath(manager.SaveFilePath), Is.True);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var manager = GameManager.Instance;
            if (manager != null)
            {
                Object.Destroy(manager.gameObject);
                yield return null;
            }

            if (!string.IsNullOrWhiteSpace(isolatedSavePath))
            {
                PlayModeSaveIsolationBootstrap.DeleteOwnedArtifacts(isolatedSavePath);
            }

            isolatedSavePath = null;
        }

        [UnityTest]
        public IEnumerator MilkroomRuntimeBootstrapCreatesCoreUiAndPersistsIsolatedCareState()
        {
            SceneManager.LoadScene("Milkroom");
            yield return null;
            yield return null;

            var manager = GameManager.Instance;
            Assert.That(manager, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<Canvas>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<MilkroomUIController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<CheeseTamaVisualController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<SleepSchedulePanelController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<SleepScheduleBridge>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<MilkBlendingPanelController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<NpcVisitBridge>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<InputBindingsPanelController>(), Is.Not.Null);

            var canvasTransform = GameObject.Find("Milkroom Canvas")?.transform;
            var roomRoot = GameObject.Find("Milkroom Background")?.transform;
            var propController = roomRoot?.GetComponent<MilkroomPropController>();
            Assert.That(canvasTransform, Is.Not.Null);
            Assert.That(roomRoot, Is.Not.Null);
            Assert.That(propController, Is.Not.Null);
            Assert.That(propController.RegisteredInteractionCount, Is.EqualTo(4));
            foreach (var routeMapping in new[]
                     {
                         (MilkroomPropRoute.SnackPanel, "Fridge_Model"),
                         (MilkroomPropRoute.MilkPanel, "MilkShelf_Model"),
                         (MilkroomPropRoute.CookingChoice, "DresserTable_Model"),
                         (MilkroomPropRoute.SleepSchedule, "CozyChair_Model")
                     })
            {
                var interaction = propController.GetInteraction(routeMapping.Item1);
                Assert.That(interaction, Is.Not.Null, routeMapping.Item1.ToString());
                Assert.That(
                    interaction.transform,
                    Is.SameAs(roomRoot.Find(routeMapping.Item2)),
                    routeMapping.Item1.ToString());
                Assert.That(interaction.InteractionCollider, Is.Not.Null);
            }

            var routeMethod = typeof(StarterSceneBuilder).GetMethod(
                "TryOpenMilkroomPropRoute",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(routeMethod, Is.Not.Null);
            var blockerNames = (string[])typeof(StarterSceneBuilder)
                .GetField(
                    "MilkroomPropBlockingSurfaceNames",
                    BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null);
            Assert.That(blockerNames, Is.Not.Null.And.Not.Empty);
            foreach (var blockerName in blockerNames)
            {
                var blocker = canvasTransform.Find(blockerName);
                if (blocker != null)
                {
                    blocker.gameObject.SetActive(false);
                }
            }

            var onboardingOverlay = canvasTransform.Find("First Meeting Onboarding Overlay");
            if (onboardingOverlay != null)
            {
                onboardingOverlay.gameObject.SetActive(false);
            }

            foreach (var routeTarget in new[]
                     {
                         (MilkroomPropRoute.SnackPanel, "Snack Panel"),
                         (MilkroomPropRoute.MilkPanel, "Milk Panel"),
                         (MilkroomPropRoute.CookingChoice, CookingChoicePanelController.OverlayObjectName),
                         (MilkroomPropRoute.SleepSchedule, SleepSchedulePanelController.OverlayObjectName)
                     })
            {
                var target = canvasTransform.Find(routeTarget.Item2);
                Assert.That(target, Is.Not.Null, routeTarget.Item1.ToString());
                target.gameObject.SetActive(false);
                Assert.That(
                    (bool)routeMethod.Invoke(
                        null,
                        new object[] { canvasTransform, routeTarget.Item1 }),
                    Is.True,
                    routeTarget.Item1.ToString());
                Assert.That(target.gameObject.activeSelf, Is.True, routeTarget.Item1.ToString());
                target.gameObject.SetActive(false);
            }

            var settingsModal = canvasTransform.Find("Settings Modal");
            Assert.That(settingsModal, Is.Not.Null);
            settingsModal.gameObject.SetActive(true);
            propController.RefreshInteractionBlockingState();
            foreach (var route in new[]
                     {
                         MilkroomPropRoute.SnackPanel,
                         MilkroomPropRoute.MilkPanel,
                         MilkroomPropRoute.CookingChoice,
                         MilkroomPropRoute.SleepSchedule
                     })
            {
                Assert.That(propController.TryActivateRoute(route), Is.False, route.ToString());
            }

            settingsModal.gameObject.SetActive(false);
            propController.RefreshInteractionBlockingState();

            var snackController = Object.FindFirstObjectByType<SnackPanelController>();
            Assert.That(snackController, Is.Not.Null);
            var snackContent = GameObject.Find("Milkroom Canvas")?.transform.Find(
                "Snack Panel/Snack Inventory Scroll View/Snack Inventory Scroll Content");
            Assert.That(snackContent, Is.Not.Null);
            for (var i = 0; i < SnackCatalog.VisibleSnackItems.Length; i += 1)
            {
                Assert.That(snackContent.Find($"Snack Inventory Row {i}"), Is.Not.Null, $"snack row {i}");
            }

            var specialRow = snackContent.Find(
                $"Snack Inventory Row {SnackCatalog.VisibleSnackItems.Length - 1}");
            Assert.That(
                specialRow?.Find($"Snack Row Title Text {SnackCatalog.VisibleSnackItems.Length - 1}")
                    ?.GetComponent<Text>()?.text,
                Is.EqualTo("특별 음식 ???"));
            Assert.That(
                specialRow?.Find($"Feed Snack Item Button {SnackCatalog.VisibleSnackItems.Length - 1}")
                    ?.GetComponent<Button>()?.interactable,
                Is.False);

            const string SpecialBlendReceipt = "playmode-special-cream-soup";
            manager.CurrentSave.economy.milkCoins = 20;
            var specialBlend = manager.TryBlendMilk(
                MilkCatalog.BasicMilkId,
                MilkBlendingCatalog.SoftDoughIngredientId,
                SpecialBlendReceipt,
                System.DateTimeOffset.UtcNow,
                0d);
            Assert.That(specialBlend.applied, Is.True);
            Assert.That(specialBlend.specialResult, Is.True);
            Assert.That(specialBlend.resultSnackId, Is.EqualTo(SnackCatalog.CreamSoupId));
            snackController.Refresh();
            Assert.That(
                specialRow.Find($"Snack Row Title Text {SnackCatalog.VisibleSnackItems.Length - 1}")
                    ?.GetComponent<Text>()?.text,
                Is.EqualTo("크림 수프"));
            Assert.That(
                specialRow.Find($"Snack Row Quantity Text {SnackCatalog.VisibleSnackItems.Length - 1}")
                    ?.GetComponent<Text>()?.text,
                Is.EqualTo("수량 1"));
            Assert.That(
                specialRow.Find($"Feed Snack Item Button {SnackCatalog.VisibleSnackItems.Length - 1}")
                    ?.GetComponent<Button>()?.interactable,
                Is.True);

            var before = manager.CurrentSave.careHistory.totalCareActions;
            manager.RegisterCareAction("pet");
            manager.SaveGame();
            Assert.That(manager.CurrentSave.careHistory.totalCareActions, Is.EqualTo(before + 1));

            var activeSaveManager = manager.GetComponent<SaveManager>();
            Assert.That(activeSaveManager, Is.Not.Null);
            var restored = activeSaveManager.LoadOrCreate();
            Assert.That(restored.careHistory.totalCareActions, Is.EqualTo(before + 1));
            Assert.That(restored.milkBlending.HasDiscovered(SnackCatalog.CreamSoupId), Is.True);
            Assert.That(restored.milkBlending.HasAppliedReceipt(SpecialBlendReceipt), Is.True);
            Assert.That(
                restored.milkBlending.GetBlendCount(
                    MilkBlendingCatalog.SoftDoughIngredientId,
                    SnackCatalog.CreamSoupId),
                Is.EqualTo(1));
            Assert.That(
                restored.snackInventory.Find(entry => entry?.snackId == SnackCatalog.CreamSoupId)
                    ?.quantity,
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MilkroomRuntimeProfileLayoutKeepsPersonalEntriesBehindPortraitMenu()
        {
            var manager = GameManager.Instance;
            Assert.That(manager, Is.Not.Null);
            manager.CurrentSave.newGameSetup = NewGameSetupSaveData.CreateCompletedForLegacySave();
            manager.CurrentSave.onboarding = OnboardingSaveData.CreateCompletedForLegacySave();
            manager.CurrentSave.firstDayJourney = FirstDayJourneySaveData.CreateCompletedForLegacySave();
            manager.SaveGame();

            SceneManager.LoadScene("Milkroom");
            yield return null;
            yield return null;

            var canvas = GameObject.Find("Milkroom Canvas")?.transform;
            Assert.That(canvas, Is.Not.Null);

            var careTip = canvas.Find("Care Tip Panel");
            Assert.That(careTip, Is.Not.Null);
            var careTipTitle = careTip.Find("Care Tip Title Text")?.GetComponent<Text>();
            Assert.That(careTipTitle, Is.Not.Null);
            Assert.That(careTipTitle.gameObject.activeSelf, Is.True);
            Assert.That(careTipTitle.text, Is.EqualTo("돌봄 팁"));
            Assert.That(careTip.Find("Open Delivery Button"), Is.Null);
            Assert.That(careTip.Find("Open First Day Journey Button"), Is.Null);

            var utilityBar = canvas.Find("Milkroom Utility Bar");
            var firstDayEntry = utilityBar?.Find("Open First Day Journey Button") as RectTransform;
            var deliveryEntry = utilityBar?.Find("Open Delivery Button") as RectTransform;
            Assert.That(firstDayEntry, Is.Not.Null);
            Assert.That(deliveryEntry, Is.Not.Null);
            Assert.That(deliveryEntry.anchoredPosition.y, Is.LessThan(firstDayEntry.anchoredPosition.y));
            Assert.That(
                deliveryEntry.Find(CheeseStarDeliveryBridge.EntryNotificationBadgeObjectName),
                Is.Not.Null);
            Assert.That(utilityBar?.Find("Open Fantasy Powder Button"), Is.Not.Null);
            var journeyEntry = utilityBar?.Find(JourneyHubPanelController.OpenButtonObjectName)
                as RectTransform;
            Assert.That(journeyEntry, Is.Not.Null);
            Assert.That(journeyEntry.anchoredPosition, Is.EqualTo(new Vector2(112f, -48f)));
            Assert.That(utilityBar?.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(4));

            var statBar = canvas.Find("Stat Bar");
            foreach (var gaugeName in new[]
                     {
                         "Hunger Gauge",
                         "Mood Gauge",
                         "Cleanliness Gauge",
                         "Sleepiness Gauge",
                         "Health Gauge"
                     })
            {
                var fill = statBar?.Find(gaugeName + "/Fill")?.GetComponent<Image>();
                Assert.That(fill, Is.Not.Null, gaugeName);
                Assert.That(fill.type, Is.EqualTo(Image.Type.Filled), gaugeName);
                Assert.That(fill.fillAmount, Is.InRange(0f, 1f), gaugeName);
            }

            var sleepOverlay = canvas.Find(SleepSchedulePanelController.OverlayObjectName);
            Assert.That(sleepOverlay, Is.Not.Null);
            Assert.That(sleepOverlay.gameObject.activeSelf, Is.False);
            var sleepCard = sleepOverlay.Find("Sleep Schedule Card");
            Assert.That(sleepCard, Is.Not.Null);
            for (var hour = 1; hour <= 8; hour += 1)
            {
                Assert.That(sleepCard.Find($"Sleep Duration Button {hour}"), Is.Not.Null);
            }

            var sleepButtonLabel = canvas.Find("Bottom Action Bar/Sleep Button/Label")
                ?.GetComponent<Text>();
            Assert.That(sleepButtonLabel, Is.Not.Null);
            Assert.That(sleepButtonLabel.text, Is.EqualTo("수면 예약"));

            var firstDayCard = canvas.Find(
                FirstDayJourneyController.OverlayObjectName + "/" + FirstDayJourneyController.CardObjectName);
            var firstDayConfirm = firstDayCard?.Find("First Day Journey Close Button")?.GetComponent<Button>();
            Assert.That(firstDayConfirm, Is.Not.Null);
            Assert.That(firstDayConfirm.GetComponentInChildren<Text>(true)?.text, Is.EqualTo("확인"));
            Assert.That(
                firstDayConfirm.GetComponent<RectTransform>().anchoredPosition.x
                + firstDayConfirm.GetComponent<RectTransform>().rect.width * 0.5f,
                Is.EqualTo(firstDayCard.GetComponent<RectTransform>().rect.width * 0.5f).Within(0.5f));

            var topBar = canvas.Find("Top Status Bar");
            var portraitButton = topBar?.Find("CheeseTama Profile Button")?.GetComponent<Button>();
            Assert.That(portraitButton, Is.Not.Null);
            Assert.That(portraitButton.GetComponent<Mask>(), Is.Not.Null);
            Assert.That(portraitButton.transform.Find("Profile Portrait Image")?.GetComponent<Image>(), Is.Not.Null);

            var profileOverlay = canvas.Find(CheeseTamaProfileMenuController.OverlayObjectName);
            var entries = profileOverlay?.Find("Profile Card/Profile Entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.Find("Open First Day Journey Button"), Is.Null);
            Assert.That(entries.Find("Open Growth Journey Button"), Is.Not.Null);
            Assert.That(entries.Find("Open Memory Journal Button"), Is.Not.Null);
            Assert.That(entries.Find("Open Bond Status Button"), Is.Not.Null);
            Assert.That(entries.Find("Open Name Change Button"), Is.Null);
            Assert.That(profileOverlay.Find("Profile Card/Open Name Change Button"), Is.Not.Null);
            Assert.That(canvas.Find("Settings Modal/Open Name Change Button"), Is.Null);
            Assert.That(canvas.Find("Status Panel/Open Growth Journey Button"), Is.Null);
            Assert.That(canvas.Find("Status Panel/Open Memory Journal Button"), Is.Null);
            Assert.That(canvas.Find("Status Panel/Open Bond Status Button"), Is.Null);

            var decorationShop = canvas.Find("Decorate Overlay/Open Decoration Shop Button") as RectTransform;
            Assert.That(decorationShop, Is.Not.Null);
            Assert.That(decorationShop.anchoredPosition.y, Is.LessThanOrEqualTo(-540f));

            var atmosphereOverlay = canvas.Find("Milkroom Atmosphere Overlay");
            Assert.That(atmosphereOverlay, Is.Not.Null);
            Assert.That(atmosphereOverlay.GetSiblingIndex(), Is.Zero);
            Assert.That(atmosphereOverlay.GetComponent<Image>()?.raycastTarget, Is.False);
            Assert.That(
                atmosphereOverlay.GetComponent<MilkroomAtmosphereLayerController>(),
                Is.Not.Null);
            Assert.That(GameObject.Find("Milkroom Atmosphere Light"), Is.Null);
            Assert.That(
                GameObject.Find("Decoration Window Anchor")?.transform.Find("Equipped Decoration Visual"),
                Is.Null);
            Assert.That(
                GameObject.Find("Decoration Shelf Anchor")?.transform.Find("Equipped Decoration Visual"),
                Is.Null);
            Assert.That(
                GameObject.Find("Decoration Window Anchor")?.transform.Find("Equipped Window Decoration"),
                Is.Null);
            Assert.That(
                GameObject.Find("Decoration Shelf Anchor")?.transform.Find("Equipped Shelf Decoration"),
                Is.Null);
            Assert.That(GameObject.Find("CheeseTama Autonomous Motion Root"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<AutonomousLifePresenter>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<NormalEvolutionVisualPresenter>(), Is.Null);
            Assert.That(Object.FindFirstObjectByType<NormalEvolutionVisualBridge>(), Is.Null);
            Assert.That(GameObject.Find(NormalEvolutionVisualPresenter.GeneratedRootName), Is.Null);

            foreach (var blockerName in new[]
                     {
                         "New Game Setup Overlay",
                         "First Meeting Onboarding Overlay",
                         "Save Recovery Notice Overlay",
                         "Return Summary Overlay",
                         "Growth Achievement Overlay",
                         "Evolution Achievement Overlay",
                         "Cheese Star Delivery Overlay",
                         "First Day Journey Overlay"
                     })
            {
                var blocker = canvas.Find(blockerName);
                if (blocker != null)
                {
                    blocker.gameObject.SetActive(false);
                }
            }

            portraitButton.onClick.Invoke();
            yield return null;
            Assert.That(profileOverlay.gameObject.activeSelf, Is.True);

            var renameButton = profileOverlay.Find("Profile Card/Open Name Change Button")
                ?.GetComponent<Button>();
            Assert.That(renameButton, Is.Not.Null);
            renameButton.onClick.Invoke();
            yield return null;
            Assert.That(profileOverlay.gameObject.activeSelf, Is.False);
            Assert.That(canvas.Find("CheeseTama Name Dialog")?.gameObject.activeSelf, Is.True);
        }
    }
}
