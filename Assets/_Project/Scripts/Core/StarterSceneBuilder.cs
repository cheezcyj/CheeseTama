using CheeseTama.Data;
using CheeseTama.Save;
using CheeseTama.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.Core
{
    public static class StarterSceneBuilder
    {
        public static GameManager EnsureCoreSystems()
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentSave == null)
                {
                    GameManager.Instance.LoadOrCreateGame();
                }

                return GameManager.Instance;
            }

            var existing = Object.FindFirstObjectByType<GameManager>();
            if (existing != null)
            {
                if (existing.CurrentSave == null)
                {
                    existing.LoadOrCreateGame();
                }

                return existing;
            }

            var core = new GameObject("CoreSystems");
            core.AddComponent<DataRegistry>();
            core.AddComponent<SaveManager>();
            return core.AddComponent<GameManager>();
        }

        public static void BuildForScene(string sceneName)
        {
            if (sceneName == SceneNames.Boot)
            {
                BuildBootScene();
            }
            else if (sceneName == SceneNames.Milkroom)
            {
                BuildMilkroomScene();
            }
            else if (sceneName == SceneNames.Collection)
            {
                BuildCollectionScene();
            }
            else if (sceneName == SceneNames.Debug)
            {
                BuildDebugScene();
            }
        }

        public static void BuildBootScene()
        {
            EnsureCoreSystems();
            EnsureCamera("Boot Camera");
            EnsureEventSystem();
            EnsureCanvas("Boot Canvas");
            EnsureTitle("Boot Canvas", "CheeseTama", "Loading core systems");

            var canvas = EnsureCanvas("Boot Canvas");
            var startButton = GetOrCreateButton(canvas.transform, "Start Button", "Start", new Vector2(0, 120));
            ConfigureNavigationButton(startButton, SceneNames.Milkroom, false);
        }

        public static void BuildMilkroomScene()
        {
            var manager = EnsureCoreSystems();
            EnsureCamera("Milkroom Camera");
            EnsureLight();
            EnsureEventSystem();
            var visualController = EnsureCheeseTamaPlaceholder();

            var canvas = EnsureCanvas("Milkroom Canvas");
            var controller = Object.FindFirstObjectByType<MilkroomUIController>();
            if (controller == null)
            {
                controller = canvas.gameObject.AddComponent<MilkroomUIController>();
            }

            RemoveMilkroomPrototypeButtons(canvas.transform);

            var topBar = GetOrCreatePanel(canvas.transform, "Top Status Bar", new Vector2(24, -18), new Vector2(1872, 78));
            var topBarTransform = topBar.transform;
            var nameText = GetOrCreateText(topBarTransform, "Name Text", "CheeseTama", 22, TextAnchor.MiddleLeft, new Vector2(22, -14), new Vector2(250, 34));
            var levelText = GetOrCreateText(topBarTransform, "Level Text", "Lv. 1 (0%)", 18, TextAnchor.MiddleLeft, new Vector2(300, -14), new Vector2(230, 34));
            var sessionText = GetOrCreateText(topBarTransform, "Session Text", "Session: 00:00 | Today 00:00", 15, TextAnchor.MiddleLeft, new Vector2(560, -14), new Vector2(390, 34));
            var economyText = GetOrCreateText(topBarTransform, "Economy Text", "Items: Coins 0 Drops 0 Frags 0", 15, TextAnchor.MiddleLeft, new Vector2(980, -14), new Vector2(520, 34));
            var topCollectionButton = GetOrCreateButton(topBarTransform, "Top Collection Button", "Collection", new Vector2(638, 17));
            ConfigureNavigationButton(topCollectionButton, SceneNames.Collection, true);
            var settingsButton = GetOrCreateButton(topBarTransform, "Settings Button", "Settings", new Vector2(790, 17));

            var panel = GetOrCreatePanel(canvas.transform, "Status Panel", new Vector2(24, -116), new Vector2(350, 620));
            var panelTransform = panel.transform;
            RemoveChildIfExists(panelTransform, "Milk Growth Text");
            RemoveChildIfExists(panelTransform, "Name Text");
            RemoveChildIfExists(panelTransform, "Level Text");
            RemoveChildIfExists(panelTransform, "Hunger Text");
            RemoveChildIfExists(panelTransform, "Mood Text");
            RemoveChildIfExists(panelTransform, "Cleanliness Text");
            RemoveChildIfExists(panelTransform, "Sleepiness Text");
            RemoveChildIfExists(panelTransform, "Health Text");
            RemoveChildIfExists(panelTransform, "Session Text");
            RemoveChildIfExists(panelTransform, "Economy Text");

            GetOrCreateText(panelTransform, "Detail Title Text", "Milkroom Notes", 18, TextAnchor.UpperLeft, new Vector2(18, -16), new Vector2(300, 28));
            var formText = GetOrCreateText(panelTransform, "Form Text", "Form: egg", 15, TextAnchor.UpperLeft, new Vector2(18, -58), new Vector2(300, 24));
            var conditionText = GetOrCreateText(panelTransform, "Condition Text", "Condition: warm", 15, TextAnchor.UpperLeft, new Vector2(18, -86), new Vector2(300, 24));
            var affectionText = GetOrCreateText(panelTransform, "Affection Text", "Affection: 10", 15, TextAnchor.UpperLeft, new Vector2(18, -124), new Vector2(300, 24));
            var maturationText = GetOrCreateText(panelTransform, "Maturation Text", "Maturation: 0", 15, TextAnchor.UpperLeft, new Vector2(18, -152), new Vector2(300, 24));
            var hatchProgressText = GetOrCreateText(panelTransform, "Hatch Progress Text", "Hatch: 0%", 15, TextAnchor.UpperLeft, new Vector2(18, -180), new Vector2(300, 24));
            var basicMilkGrowthText = GetOrCreateText(panelTransform, "Basic Milk Growth Text", "Basic Milk: Lv. 0 (0 pts)", 15, TextAnchor.UpperLeft, new Vector2(18, -220), new Vector2(310, 24));
            var starMilkGrowthText = GetOrCreateText(panelTransform, "Star Milk Growth Text", "Star Milk: locked", 15, TextAnchor.UpperLeft, new Vector2(18, -248), new Vector2(310, 24));
            var unlockText = GetOrCreateText(panelTransform, "Unlock Text", "Unlocks: Star Milk locked", 15, TextAnchor.UpperLeft, new Vector2(18, -276), new Vector2(310, 24));
            var careSummaryText = GetOrCreateText(panelTransform, "Care Summary Text", "Care: 0 | Play 0 Clean 0 Rest 0", 13, TextAnchor.UpperLeft, new Vector2(18, -318), new Vector2(310, 24));
            var dailyRoutineText = GetOrCreateText(panelTransform, "Daily Routine Text", "Today: M 0/1 P 0/1 C 0/1 R 0/1", 13, TextAnchor.UpperLeft, new Vector2(18, -346), new Vector2(310, 24));
            var careTipText = GetOrCreateText(panelTransform, "Care Tip Text", "Care Tip: Feed Milk to grow.", 13, TextAnchor.UpperLeft, new Vector2(18, -388), new Vector2(310, 48));
            var lastSavedText = GetOrCreateText(panelTransform, "Last Saved Text", "Last Saved: Never", 13, TextAnchor.UpperLeft, new Vector2(18, -450), new Vector2(310, 24));
            var messageText = GetOrCreateText(panelTransform, "Message Text", "Ready for care.", 13, TextAnchor.UpperLeft, new Vector2(18, -492), new Vector2(310, 98));

            var statBar = GetOrCreatePanel(canvas.transform, "Stat Bar", new Vector2(470, -846), new Vector2(980, 74));
            var statBarTransform = statBar.transform;
            var hungerText = GetOrCreateText(statBarTransform, "Hunger Text", "Hunger: 80", 15, TextAnchor.MiddleCenter, new Vector2(24, -20), new Vector2(170, 32));
            var moodText = GetOrCreateText(statBarTransform, "Mood Text", "Mood: 70", 15, TextAnchor.MiddleCenter, new Vector2(214, -20), new Vector2(170, 32));
            var cleanlinessText = GetOrCreateText(statBarTransform, "Cleanliness Text", "Cleanliness: 90", 15, TextAnchor.MiddleCenter, new Vector2(404, -20), new Vector2(170, 32));
            var sleepinessText = GetOrCreateText(statBarTransform, "Sleepiness Text", "Sleepiness: 20", 15, TextAnchor.MiddleCenter, new Vector2(594, -20), new Vector2(170, 32));
            var healthText = GetOrCreateText(statBarTransform, "Health Text", "Health: 100", 15, TextAnchor.MiddleCenter, new Vector2(784, -20), new Vector2(170, 32));

            controller.Configure(
                nameText,
                levelText,
                formText,
                conditionText,
                hungerText,
                moodText,
                cleanlinessText,
                sleepinessText,
                healthText,
                affectionText,
                maturationText,
                hatchProgressText,
                basicMilkGrowthText,
                starMilkGrowthText,
                unlockText,
                careSummaryText,
                dailyRoutineText,
                sessionText,
                economyText,
                careTipText,
                lastSavedText,
                messageText);
            manager.RefreshDerivedCollectionRecords();
            controller.Bind(manager.CurrentSave);
            controller.ShowMessage("Ready for care.");
            visualController.Bind(manager.CurrentTama);

            var actionBar = GetOrCreatePanel(canvas.transform, "Bottom Action Bar", new Vector2(350, -968), new Vector2(1220, 84));
            var actionBarTransform = actionBar.transform;
            RemoveChildIfExists(actionBarTransform, "Collection Button");

            var milkButton = GetOrCreateButton(actionBarTransform, "Milk Button", "Milk", new Vector2(-425, 20));
            ConfigureCareButton(milkButton, MilkroomCareAction.FeedMilk, controller, visualController);

            var blendButton = GetOrCreateButton(actionBarTransform, "Blend Button", "Blend", new Vector2(-255, 20));
            ConfigureCareButton(blendButton, MilkroomCareAction.Blend, controller, visualController);

            var snackButton = GetOrCreateButton(actionBarTransform, "Snack Button", "Snack", new Vector2(-85, 20));
            ConfigureCareButton(snackButton, MilkroomCareAction.FeedSnack, controller, visualController);

            var playButton = GetOrCreateButton(actionBarTransform, "Play Button", "Play", new Vector2(85, 20));
            ConfigureCareButton(playButton, MilkroomCareAction.Play, controller, visualController);

            var cleanButton = GetOrCreateButton(actionBarTransform, "Clean Button", "Clean", new Vector2(255, 20));
            ConfigureCareButton(cleanButton, MilkroomCareAction.Clean, controller, visualController);

            var sleepButton = GetOrCreateButton(actionBarTransform, "Sleep Button", "Sleep", new Vector2(425, 20));
            ConfigureCareButton(sleepButton, MilkroomCareAction.Rest, controller, visualController);

            var actionBarController = actionBar.GetComponent<BottomActionBarController>();
            if (actionBarController == null)
            {
                actionBarController = actionBar.AddComponent<BottomActionBarController>();
            }

            actionBarController.Configure(milkButton, blendButton, snackButton, playButton, cleanButton, sleepButton);
            BuildMilkroomSettings(canvas.transform, settingsButton, controller, visualController);
        }

        public static void BuildCollectionScene()
        {
            var manager = EnsureCoreSystems();
            manager.RefreshDerivedCollectionRecords();
            EnsureCamera("Collection Camera");
            EnsureEventSystem();
            var canvas = EnsureCanvas("Collection Canvas");

            var controller = Object.FindFirstObjectByType<CollectionUIController>();
            if (controller == null)
            {
                controller = canvas.gameObject.AddComponent<CollectionUIController>();
            }

            EnsureTitle("Collection Canvas", "Collection", "Only discovered records appear here");

            var panel = GetOrCreatePanel(canvas.transform, "Collection Records Panel", new Vector2(24, -180), new Vector2(640, 620));
            var panelTransform = panel.transform;
            var milkText = GetOrCreateText(panelTransform, "Milk Records Text", "Milk Records: 0", 16, TextAnchor.UpperLeft, new Vector2(16, -16), new Vector2(600, 72));
            var evolutionText = GetOrCreateText(panelTransform, "Evolution Records Text", "Evolution Records: 0", 16, TextAnchor.UpperLeft, new Vector2(16, -96), new Vector2(600, 72));
            var eventText = GetOrCreateText(panelTransform, "Event Records Text", "Event Records: 0", 16, TextAnchor.UpperLeft, new Vector2(16, -176), new Vector2(600, 210));
            var hiddenText = GetOrCreateText(panelTransform, "Hidden Records Text", "Hidden Records: 0", 16, TextAnchor.UpperLeft, new Vector2(16, -396), new Vector2(600, 92));
            var messageText = GetOrCreateText(panelTransform, "Collection Message Text", "Feed milk and hatch CheeseTama to add records here.", 14, TextAnchor.UpperLeft, new Vector2(16, -496), new Vector2(600, 64));

            controller.Configure(milkText, evolutionText, eventText, hiddenText, messageText);
            controller.Bind(manager.CurrentSave);

            var backButton = GetOrCreateButton(canvas.transform, "Milkroom Button", "Milkroom", new Vector2(0, 36));
            ConfigureNavigationButton(backButton, SceneNames.Milkroom, false);
        }

        public static void BuildDebugScene()
        {
            var manager = EnsureCoreSystems();
            manager.RefreshDerivedCollectionRecords();
            EnsureCamera("Debug Camera");
            EnsureLight();
            EnsureEventSystem();
            var visualController = EnsureCheeseTamaPlaceholder();
            var canvas = EnsureCanvas("Debug Canvas");

            var controller = Object.FindFirstObjectByType<DebugUIController>();
            if (controller == null)
            {
                controller = canvas.gameObject.AddComponent<DebugUIController>();
            }

            EnsureTitle("Debug Canvas", "Debug", "Developer test surface");

            var panel = GetOrCreatePanel(canvas.transform, "Debug State Panel", new Vector2(24, -180), new Vector2(500, 600));
            var panelTransform = panel.transform;
            var stateText = GetOrCreateText(panelTransform, "Debug State Text", "Debug State", 16, TextAnchor.UpperLeft, new Vector2(16, -16), new Vector2(460, 430));
            var messageText = GetOrCreateText(panelTransform, "Debug Message Text", "Pick a preset.", 14, TextAnchor.UpperLeft, new Vector2(16, -480), new Vector2(460, 80));

            controller.Configure(stateText, messageText);
            controller.Bind(manager.CurrentSave);
            controller.ShowMessage("Pick a preset to check values and CheeseTama expressions.");
            visualController.Bind(manager.CurrentTama);

            var hungryButton = GetOrCreateButton(canvas.transform, "Hungry Preset Button", "Hungry", new Vector2(-490, 96));
            ConfigureDebugButton(hungryButton, DebugAction.SetHungry, controller, visualController);

            var sleepyButton = GetOrCreateButton(canvas.transform, "Sleepy Preset Button", "Sleepy", new Vector2(-350, 96));
            ConfigureDebugButton(sleepyButton, DebugAction.SetSleepy, controller, visualController);

            var messyButton = GetOrCreateButton(canvas.transform, "Messy Preset Button", "Messy", new Vector2(-210, 96));
            ConfigureDebugButton(messyButton, DebugAction.SetMessy, controller, visualController);

            var unwellButton = GetOrCreateButton(canvas.transform, "Unwell Preset Button", "Unwell", new Vector2(-70, 96));
            ConfigureDebugButton(unwellButton, DebugAction.SetUnwell, controller, visualController);

            var cheerfulButton = GetOrCreateButton(canvas.transform, "Cheerful Preset Button", "Cheerful", new Vector2(70, 96));
            ConfigureDebugButton(cheerfulButton, DebugAction.SetCheerful, controller, visualController);

            var hatchButton = GetOrCreateButton(canvas.transform, "Hatch Preset Button", "Hatch", new Vector2(210, 96));
            ConfigureDebugButton(hatchButton, DebugAction.HatchNow, controller, visualController);

            var unlockStarButton = GetOrCreateButton(canvas.transform, "Unlock Star Preset Button", "Unlock Star", new Vector2(-210, 36));
            ConfigureDebugButton(unlockStarButton, DebugAction.UnlockStarMilk, controller, visualController);

            var resetButton = GetOrCreateButton(canvas.transform, "Debug Reset Button", "Reset", new Vector2(-70, 36));
            ConfigureDebugButton(resetButton, DebugAction.ResetSave, controller, visualController);

            var forceEventButton = GetOrCreateButton(canvas.transform, "Force Event Button", "Force Event", new Vector2(70, 36));
            ConfigureDebugButton(forceEventButton, DebugAction.ForceEvent, controller, visualController);

            var stayButton = GetOrCreateButton(canvas.transform, "Stay Five Minutes Button", "Stay +5m", new Vector2(350, 96));
            ConfigureDebugButton(stayButton, DebugAction.AddSessionFiveMinutes, controller, visualController);

            var milkroomButton = GetOrCreateButton(canvas.transform, "Milkroom Button", "Milkroom", new Vector2(210, 36));
            ConfigureNavigationButton(milkroomButton, SceneNames.Milkroom, true);
        }

        private static void BuildMilkroomSettings(
            Transform canvasTransform,
            Button settingsButton,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController)
        {
            var settingsModal = GetOrCreatePanel(canvasTransform, "Settings Modal", new Vector2(1320, -116), new Vector2(560, 620));
            var settingsTransform = settingsModal.transform;
            GetOrCreateText(settingsTransform, "Settings Title Text", "Settings", 22, TextAnchor.UpperLeft, new Vector2(28, -24), new Vector2(280, 34));
            GetOrCreateText(settingsTransform, "Settings Data Title Text", "Data Management", 18, TextAnchor.UpperLeft, new Vector2(28, -92), new Vector2(300, 30));
            GetOrCreateText(settingsTransform, "Settings Sound Title Text", "Sound", 16, TextAnchor.UpperLeft, new Vector2(28, -294), new Vector2(220, 26));
            GetOrCreateText(settingsTransform, "Settings Display Title Text", "Display", 16, TextAnchor.UpperLeft, new Vector2(28, -344), new Vector2(220, 26));
            GetOrCreateText(settingsTransform, "Settings Controls Title Text", "Controls", 16, TextAnchor.UpperLeft, new Vector2(28, -394), new Vector2(220, 26));

            var closeSettingsButton = GetOrCreateTopLeftButton(settingsTransform, "Close Settings Button", "Close", new Vector2(424, -20), new Vector2(108, 40));
            var manualSaveButton = GetOrCreateTopLeftButton(settingsTransform, "Manual Save Button", "Save", new Vector2(28, -190), new Vector2(120, 42));
            var manualLoadButton = GetOrCreateTopLeftButton(settingsTransform, "Manual Load Button", "Load", new Vector2(166, -190), new Vector2(120, 42));
            var openResetButton = GetOrCreateTopLeftButton(settingsTransform, "Open Reset Button", "Reset", new Vector2(304, -190), new Vector2(120, 42));
            ApplyDangerButtonStyle(openResetButton);
            var dataStatusText = GetOrCreateText(settingsTransform, "Data Status Text", "Auto-save runs after care actions. Manual tools are below.", 13, TextAnchor.UpperLeft, new Vector2(28, -134), new Vector2(500, 42));

            var confirmRoot = GetOrCreatePanel(canvasTransform, "Confirm Reset Dialog", new Vector2(640, -300), new Vector2(640, 360));
            if (confirmRoot.TryGetComponent(out Image confirmImage))
            {
                confirmImage.color = new Color(1f, 0.98f, 0.9f, 1f);
            }

            var confirmTransform = confirmRoot.transform;
            GetOrCreateText(confirmTransform, "Confirm Reset Title Text", "Reset Data", 22, TextAnchor.UpperLeft, new Vector2(24, -24), new Vector2(300, 34));
            var confirmMessageText = GetOrCreateText(
                confirmTransform,
                "Confirm Reset Message Text",
                "Type RESET to clear all local CheeseTama progress.",
                15,
                TextAnchor.UpperLeft,
                new Vector2(24, -82),
                new Vector2(580, 70));
            GetOrCreateText(confirmTransform, "Reset Input Label Text", "Enter RESET to unlock the button.", 14, TextAnchor.UpperLeft, new Vector2(24, -152), new Vector2(420, 24));
            var resetInput = GetOrCreateInputField(confirmTransform, "Reset Input Field", "RESET", new Vector2(24, -184), new Vector2(360, 52));
            var confirmResetButton = GetOrCreateTopLeftButton(confirmTransform, "Confirm Reset Button", "Reset", new Vector2(344, -284), new Vector2(120, 42));
            ApplyDangerButtonStyle(confirmResetButton);
            var cancelResetButton = GetOrCreateTopLeftButton(confirmTransform, "Cancel Reset Button", "Cancel", new Vector2(480, -284), new Vector2(120, 42));

            var confirmResetDialog = confirmRoot.GetComponent<ConfirmResetDialog>();
            if (confirmResetDialog == null)
            {
                confirmResetDialog = confirmRoot.AddComponent<ConfirmResetDialog>();
            }

            confirmResetDialog.Configure(
                confirmRoot,
                resetInput,
                confirmMessageText,
                confirmResetButton,
                cancelResetButton,
                controller,
                visualController);

            var dataPanel = settingsModal.GetComponent<DataManagementPanelController>();
            if (dataPanel == null)
            {
                dataPanel = settingsModal.AddComponent<DataManagementPanelController>();
            }

            dataPanel.Configure(
                manualSaveButton,
                manualLoadButton,
                openResetButton,
                dataStatusText,
                confirmResetDialog,
                controller,
                visualController);

            var settingsController = settingsModal.GetComponent<SettingsMenuController>();
            if (settingsController == null)
            {
                settingsController = settingsModal.AddComponent<SettingsMenuController>();
            }

            settingsController.Configure(settingsButton, closeSettingsButton, settingsModal);

            var devPanel = GetOrCreatePanel(canvasTransform, "Dev Panel", new Vector2(1276, -746), new Vector2(300, 164));
            var devPanelTransform = devPanel.transform;
            GetOrCreateText(devPanelTransform, "Dev Panel Title Text", "Dev Panel", 17, TextAnchor.UpperLeft, new Vector2(18, -18), new Vector2(240, 28));
            var debugSceneButton = GetOrCreateTopLeftButton(devPanelTransform, "Debug Scene Button", "Debug Scene", new Vector2(18, -82), new Vector2(150, 42));
            ConfigureNavigationButton(debugSceneButton, SceneNames.Debug, true);

            var devPanelController = canvasTransform.GetComponent<DevPanelController>();
            if (devPanelController == null)
            {
                devPanelController = canvasTransform.gameObject.AddComponent<DevPanelController>();
            }

            devPanelController.Configure(devPanel);
        }

        private static Camera EnsureCamera(string name)
        {
            var existing = Object.FindFirstObjectByType<Camera>();
            if (existing != null)
            {
                return existing;
            }

            var cameraObject = new GameObject(name);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.96f, 0.92f, 0.84f);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            return camera;
        }

        private static void EnsureLight()
        {
            var keyObject = GameObject.Find("Milkroom Key Light");
            if (keyObject == null)
            {
                keyObject = new GameObject("Milkroom Key Light");
            }

            var keyLight = keyObject.GetComponent<Light>();
            if (keyLight == null)
            {
                keyLight = keyObject.AddComponent<Light>();
            }

            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.96f, 0.86f);
            keyLight.intensity = 1.45f;
            keyObject.transform.rotation = Quaternion.Euler(48, -32, 0);

            var rimObject = GameObject.Find("Milkroom Rim Light");
            if (rimObject == null)
            {
                rimObject = new GameObject("Milkroom Rim Light");
            }

            var rimLight = rimObject.GetComponent<Light>();
            if (rimLight == null)
            {
                rimLight = rimObject.AddComponent<Light>();
            }

            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.76f, 0.92f, 1f);
            rimLight.intensity = 0.35f;
            rimObject.transform.rotation = Quaternion.Euler(25, 145, 0);
        }

        private static CheeseTamaVisualController EnsureCheeseTamaPlaceholder()
        {
            var existing = GameObject.Find("CheeseTama Egg Placeholder");
            if (existing != null)
            {
                return GetOrCreateVisualController(existing);
            }

            var egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            egg.name = "CheeseTama Egg Placeholder";
            egg.transform.position = new Vector3(0, -0.3f, 0);
            egg.transform.localScale = new Vector3(1.25f, 1.55f, 1.25f);

            return GetOrCreateVisualController(egg);
        }

        private static CheeseTamaVisualController GetOrCreateVisualController(GameObject target)
        {
            var controller = target.GetComponent<CheeseTamaVisualController>();
            if (controller == null)
            {
                controller = target.AddComponent<CheeseTamaVisualController>();
            }

            return controller;
        }

        private static Canvas EnsureCanvas(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
            {
                return existingCanvas;
            }

            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureTitle(string canvasName, string title, string subtitle)
        {
            var canvas = EnsureCanvas(canvasName);
            if (canvas.transform.Find("Title Text") != null)
            {
                return;
            }

            CreateText(canvas.transform, "Title Text", title, 34, TextAnchor.MiddleCenter, new Vector2(0, -82), new Vector2(520, 44), true);
            CreateText(canvas.transform, "Subtitle Text", subtitle, 18, TextAnchor.MiddleCenter, new Vector2(0, -128), new Vector2(520, 32), true);
        }

        private static void RemoveMilkroomPrototypeButtons(Transform canvasTransform)
        {
            RemoveChildIfExists(canvasTransform, "Catch Drops Button");
            RemoveChildIfExists(canvasTransform, "Snack Button");
            RemoveChildIfExists(canvasTransform, "Feed Milk Button");
            RemoveChildIfExists(canvasTransform, "Star Milk Button");
            RemoveChildIfExists(canvasTransform, "Play Button");
            RemoveChildIfExists(canvasTransform, "Clean Button");
            RemoveChildIfExists(canvasTransform, "Rest Button");
            RemoveChildIfExists(canvasTransform, "Wait Hour Button");
            RemoveChildIfExists(canvasTransform, "Save Button");
            RemoveChildIfExists(canvasTransform, "Reload Button");
            RemoveChildIfExists(canvasTransform, "Reset Button");
            RemoveChildIfExists(canvasTransform, "Collection Button");
            RemoveChildIfExists(canvasTransform, "Debug Button");
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            ConfigurePanelRect(rect, anchoredPosition, size);

            var image = panel.AddComponent<Image>();
            image.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            return panel;
        }

        private static GameObject GetOrCreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.TryGetComponent(out RectTransform rect))
                {
                    ConfigurePanelRect(rect, anchoredPosition, size);
                }

                if (!existing.TryGetComponent(out Image image))
                {
                    image = existing.gameObject.AddComponent<Image>();
                }

                image.color = new Color(1f, 0.98f, 0.9f, 0.92f);
                return existing.gameObject;
            }

            return CreatePanel(parent, name, anchoredPosition, size);
        }

        private static void ConfigurePanelRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void RemoveChildIfExists(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(child.gameObject);
            }
            else
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            bool centered = false)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            textObject.AddComponent<RectTransform>();
            var label = textObject.AddComponent<Text>();
            ConfigureText(label, text, fontSize, alignment, anchoredPosition, size, centered);
            return label;
        }

        private static Text GetOrCreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            bool centered = false)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Text existingText))
            {
                ConfigureText(existingText, text, fontSize, alignment, anchoredPosition, size, centered);
                return existingText;
            }

            return CreateText(parent, name, text, fontSize, alignment, anchoredPosition, size, centered);
        }

        private static InputField GetOrCreateInputField(
            Transform parent,
            string name,
            string placeholder,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out InputField existingInput))
            {
                ConfigureInputField(existingInput, placeholder, anchoredPosition, size);
                return existingInput;
            }

            var inputObject = new GameObject(name);
            inputObject.transform.SetParent(parent, false);
            inputObject.AddComponent<RectTransform>();
            var image = inputObject.AddComponent<Image>();
            image.color = new Color(1f, 0.98f, 0.9f);

            var input = inputObject.AddComponent<InputField>();
            var text = CreateText(inputObject.transform, "Text", string.Empty, 18, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(size.x - 24, size.y), false);
            var placeholderText = CreateText(inputObject.transform, "Placeholder", placeholder, 18, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(size.x - 24, size.y), false);
            placeholderText.color = new Color(0.45f, 0.34f, 0.24f, 0.45f);
            input.textComponent = text;
            input.placeholder = placeholderText;
            ConfigureInputField(input, placeholder, anchoredPosition, size);
            return input;
        }

        private static void ConfigureInputField(
            InputField input,
            string placeholder,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var rect = input.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = input.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1f, 0.98f, 0.9f);
            }

            if (input.placeholder is Text placeholderText)
            {
                ConfigureText(placeholderText, placeholder, 18, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(size.x - 24, size.y), false);
                placeholderText.color = new Color(0.45f, 0.34f, 0.24f, 0.45f);
            }

            if (input.textComponent != null)
            {
                ConfigureText(input.textComponent, input.text, 18, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(size.x - 24, size.y), false);
            }
        }

        private static void ConfigureText(
            Text label,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            bool centered)
        {
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.anchorMax = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.pivot = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            label.font = GetDefaultFont();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.22f, 0.17f, 0.12f);
            label.raycastTarget = false;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(136, 44);

            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ConfigureButton(button, label, anchoredPosition);
            return button;
        }

        private static Button GetOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                ConfigureButton(existingButton, label, anchoredPosition);
                return existingButton;
            }

            return CreateButton(parent, name, label, anchoredPosition);
        }

        private static Button CreateTopLeftButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            buttonObject.AddComponent<RectTransform>();
            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ConfigureTopLeftButton(button, label, anchoredPosition, size);
            return button;
        }

        private static Button GetOrCreateTopLeftButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                ConfigureTopLeftButton(existingButton, label, anchoredPosition, size);
                return existingButton;
            }

            return CreateTopLeftButton(parent, name, label, anchoredPosition, size);
        }

        private static void ConfigureTopLeftButton(Button button, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            ConfigureButtonVisuals(button, label, size);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController)
        {
            button.onClick.RemoveAllListeners();
            var careButton = button.GetComponent<MilkroomCareButton>();
            if (careButton == null)
            {
                careButton = button.gameObject.AddComponent<MilkroomCareButton>();
            }

            careButton.Configure(action, controller, visualController);
        }

        private static void ConfigureDebugButton(
            Button button,
            DebugAction action,
            DebugUIController controller,
            CheeseTamaVisualController visualController)
        {
            button.onClick.RemoveAllListeners();
            var debugButton = button.GetComponent<DebugActionButton>();
            if (debugButton == null)
            {
                debugButton = button.gameObject.AddComponent<DebugActionButton>();
            }

            debugButton.Configure(action, controller, visualController);
        }

        private static void ConfigureNavigationButton(Button button, string targetSceneName, bool saveBeforeLoad)
        {
            button.onClick.RemoveAllListeners();
            var navigationButton = button.GetComponent<SceneNavigationButton>();
            if (navigationButton == null)
            {
                navigationButton = button.gameObject.AddComponent<SceneNavigationButton>();
            }

            navigationButton.Configure(targetSceneName, saveBeforeLoad);
        }

        private static void ConfigureButton(Button button, string label, Vector2 anchoredPosition)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(136, 44);

            ConfigureButtonVisuals(button, label, rect.sizeDelta);
        }

        private static void ConfigureButtonVisuals(Button button, string label, Vector2 size)
        {
            if (!button.TryGetComponent(out Image image))
            {
                image = button.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.96f, 0.78f, 0.35f);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(0.96f, 0.78f, 0.35f);
            colors.highlightedColor = new Color(1f, 0.86f, 0.46f);
            colors.pressedColor = new Color(0.91f, 0.61f, 0.2f);
            colors.selectedColor = new Color(1f, 0.86f, 0.46f);
            colors.disabledColor = new Color(0.72f, 0.66f, 0.56f, 0.72f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null)
            {
                var createdLabel = CreateText(button.transform, "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero, size, true);
                ConfigureButtonLabel(createdLabel);
                return;
            }

            if (!labelTransform.TryGetComponent(out Text labelText))
            {
                labelText = labelTransform.gameObject.AddComponent<Text>();
            }

            ConfigureText(labelText, label, 16, TextAnchor.MiddleCenter, Vector2.zero, size, true);
            ConfigureButtonLabel(labelText);
        }

        private static void ConfigureButtonLabel(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.color = new Color(0.31f, 0.22f, 0.14f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 16;
        }

        private static void ApplyDangerButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.84f, 0.35f, 0.29f);
            }

            var colors = button.colors;
            colors.normalColor = new Color(0.84f, 0.35f, 0.29f);
            colors.highlightedColor = new Color(0.95f, 0.45f, 0.38f);
            colors.pressedColor = new Color(0.68f, 0.24f, 0.2f);
            colors.selectedColor = new Color(0.95f, 0.45f, 0.38f);
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform != null && labelTransform.TryGetComponent(out Text label))
            {
                label.color = new Color(1f, 0.96f, 0.9f);
            }
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}
