using CheeseTama.Data;
using CheeseTama.Save;
using CheeseTama.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CheeseTama.Core
{
    public static class StarterSceneBuilder
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

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
            EnsureMilkroomBackground();
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
            RemoveChildIfExists(panelTransform, "Message Text");

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

            var statBar = GetOrCreatePanel(canvas.transform, "Stat Bar", new Vector2(470, -846), new Vector2(980, 74));
            var statBarTransform = statBar.transform;
            var hungerText = GetOrCreateText(statBarTransform, "Hunger Text", "Hunger: 80", 15, TextAnchor.MiddleCenter, new Vector2(24, -20), new Vector2(170, 32));
            var moodText = GetOrCreateText(statBarTransform, "Mood Text", "Mood: 70", 15, TextAnchor.MiddleCenter, new Vector2(214, -20), new Vector2(170, 32));
            var cleanlinessText = GetOrCreateText(statBarTransform, "Cleanliness Text", "Cleanliness: 90", 15, TextAnchor.MiddleCenter, new Vector2(404, -20), new Vector2(170, 32));
            var sleepinessText = GetOrCreateText(statBarTransform, "Sleepiness Text", "Sleepiness: 20", 15, TextAnchor.MiddleCenter, new Vector2(594, -20), new Vector2(170, 32));
            var healthText = GetOrCreateText(statBarTransform, "Health Text", "Health: 100", 15, TextAnchor.MiddleCenter, new Vector2(784, -20), new Vector2(170, 32));

            var messageBar = GetOrCreatePanel(canvas.transform, "Message Bar", new Vector2(470, -750), new Vector2(980, 72));
            if (messageBar.TryGetComponent(out Image messageBarImage))
            {
                messageBarImage.color = new Color(1f, 0.93f, 0.68f, 0.98f);
            }

            var messageText = GetOrCreateText(messageBar.transform, "Message Text", "Ready for care.", 19, TextAnchor.MiddleLeft, new Vector2(24, -16), new Vector2(932, 40));
            messageText.fontStyle = FontStyle.Bold;
            messageText.color = new Color(0.28f, 0.18f, 0.08f);

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

            var devPanel = GetOrCreatePanel(canvasTransform, "Dev Panel", new Vector2(1570, -116), new Vector2(326, 206));
            var devPanelTransform = devPanel.transform;
            GetOrCreateText(devPanelTransform, "Dev Panel Title Text", "Dev Panel", 17, TextAnchor.UpperLeft, new Vector2(18, -18), new Vector2(240, 28));
            GetOrCreateText(devPanelTransform, "Dev Panel Help Text", "Editor test tools", 13, TextAnchor.UpperLeft, new Vector2(18, -48), new Vector2(240, 24));
            var waitHourButton = GetOrCreateTopLeftButton(devPanelTransform, "Wait Hour Dev Button", "Wait +1h", new Vector2(18, -86), new Vector2(126, 42));
            ConfigureCareButton(waitHourButton, MilkroomCareAction.WaitHour, controller, visualController);
            var debugSceneButton = GetOrCreateTopLeftButton(devPanelTransform, "Debug Scene Button", "Debug Scene", new Vector2(170, -86), new Vector2(126, 42));
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

        private static void EnsureMilkroomBackground()
        {
            var existing = GameObject.Find("Milkroom Background");
            if (existing != null && Application.isPlaying && existing.transform.childCount > 0)
            {
                return;
            }

            if (existing != null)
            {
                DestroyObjectSafely(existing);
            }

            var root = new GameObject("Milkroom Background").transform;
            root.position = Vector3.zero;

            CreateDecorPart(root, "Back Wall", PrimitiveType.Cube, new Vector3(0f, 0.55f, 2.85f), new Vector3(10.8f, 5.8f, 0.08f), new Color(0.82f, 0.61f, 0.42f));
            CreateDecorPart(root, "Warm Wall Wash", PrimitiveType.Sphere, new Vector3(0f, 1.45f, 2.45f), new Vector3(5.6f, 3.2f, 0.05f), new Color(1f, 0.82f, 0.48f));
            CreateDecorPart(root, "Floor Plane", PrimitiveType.Cube, new Vector3(0f, -2.62f, 1.72f), new Vector3(10.9f, 2.5f, 0.08f), new Color(0.58f, 0.34f, 0.18f));

            for (var i = 0; i < 7; i += 1)
            {
                var y = -1.58f - i * 0.32f;
                CreateDecorPart(root, $"Floor Plank Line {i + 1}", PrimitiveType.Cube, new Vector3(0f, y, 1.32f), new Vector3(10.2f, 0.018f, 0.04f), new Color(0.42f, 0.24f, 0.13f));
            }

            for (var i = 0; i < 9; i += 1)
            {
                var x = -4.2f + i * 1.05f;
                CreateDecorPart(root, $"Floor Vertical Seam {i + 1}", PrimitiveType.Cube, new Vector3(x, -2.65f, 1.31f), new Vector3(0.018f, 2.1f, 0.04f), new Color(0.49f, 0.29f, 0.16f));
            }

            CreateRug(root);
            CreateWindow(root);
            CreateLeftFurniture(root);
            CreateRightFurniture(root);
            CreateShelfGroup(root);
            CreateHangingLights(root);
        }

        private static void CreateRug(Transform root)
        {
            CreateDecorPart(root, "Rug Outer Rim", PrimitiveType.Sphere, new Vector3(0f, -1.96f, 0.96f), new Vector3(2.85f, 0.34f, 0.68f), new Color(0.92f, 0.82f, 0.63f));
            CreateDecorPart(root, "Rug Inner Cream", PrimitiveType.Sphere, new Vector3(0f, -1.94f, 0.86f), new Vector3(2.38f, 0.22f, 0.52f), new Color(1f, 0.92f, 0.72f));
            CreateDecorPart(root, "Rug Paw Center", PrimitiveType.Sphere, new Vector3(0f, -1.93f, 0.76f), new Vector3(0.42f, 0.07f, 0.05f), new Color(0.84f, 0.7f, 0.5f));
            CreateDecorPart(root, "Rug Paw Toe L", PrimitiveType.Sphere, new Vector3(-0.38f, -1.8f, 0.75f), new Vector3(0.18f, 0.05f, 0.04f), new Color(0.86f, 0.72f, 0.52f));
            CreateDecorPart(root, "Rug Paw Toe LC", PrimitiveType.Sphere, new Vector3(-0.14f, -1.74f, 0.75f), new Vector3(0.18f, 0.05f, 0.04f), new Color(0.86f, 0.72f, 0.52f));
            CreateDecorPart(root, "Rug Paw Toe RC", PrimitiveType.Sphere, new Vector3(0.14f, -1.74f, 0.75f), new Vector3(0.18f, 0.05f, 0.04f), new Color(0.86f, 0.72f, 0.52f));
            CreateDecorPart(root, "Rug Paw Toe R", PrimitiveType.Sphere, new Vector3(0.38f, -1.8f, 0.75f), new Vector3(0.18f, 0.05f, 0.04f), new Color(0.86f, 0.72f, 0.52f));
        }

        private static void CreateWindow(Transform root)
        {
            CreateDecorPart(root, "Window Glow", PrimitiveType.Sphere, new Vector3(0f, 1.8f, 2.02f), new Vector3(3.15f, 2.15f, 0.05f), new Color(1f, 0.86f, 0.48f));
            CreateDecorPart(root, "Window Sky", PrimitiveType.Cube, new Vector3(0f, 1.72f, 1.72f), new Vector3(2.35f, 1.58f, 0.06f), new Color(0.64f, 0.83f, 0.95f));
            CreateDecorPart(root, "Window Sun Patch", PrimitiveType.Sphere, new Vector3(0.68f, 2.08f, 1.66f), new Vector3(0.36f, 0.36f, 0.04f), new Color(1f, 0.86f, 0.38f));
            CreateDecorPart(root, "Window Cloud A", PrimitiveType.Sphere, new Vector3(-0.62f, 1.9f, 1.64f), new Vector3(0.44f, 0.14f, 0.035f), new Color(0.96f, 0.98f, 1f));
            CreateDecorPart(root, "Window Cloud B", PrimitiveType.Sphere, new Vector3(-0.22f, 1.76f, 1.64f), new Vector3(0.38f, 0.12f, 0.035f), new Color(0.96f, 0.98f, 1f));
            CreateDecorPart(root, "Window Frame Top", PrimitiveType.Cube, new Vector3(0f, 2.52f, 1.52f), new Vector3(2.65f, 0.09f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Frame Bottom", PrimitiveType.Cube, new Vector3(0f, 0.92f, 1.52f), new Vector3(2.65f, 0.11f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Frame Left", PrimitiveType.Cube, new Vector3(-1.32f, 1.72f, 1.52f), new Vector3(0.11f, 1.65f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Frame Right", PrimitiveType.Cube, new Vector3(1.32f, 1.72f, 1.52f), new Vector3(0.11f, 1.65f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Cross Vertical", PrimitiveType.Cube, new Vector3(0f, 1.72f, 1.48f), new Vector3(0.08f, 1.5f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Window Cross Horizontal", PrimitiveType.Cube, new Vector3(0f, 1.72f, 1.48f), new Vector3(2.42f, 0.08f, 0.08f), new Color(0.98f, 0.88f, 0.66f));
            CreateDecorPart(root, "Curtain Left", PrimitiveType.Cube, new Vector3(-1.62f, 1.72f, 1.35f), new Vector3(0.36f, 1.78f, 0.08f), new Color(1f, 0.91f, 0.76f));
            CreateDecorPart(root, "Curtain Right", PrimitiveType.Cube, new Vector3(1.62f, 1.72f, 1.35f), new Vector3(0.36f, 1.78f, 0.08f), new Color(1f, 0.91f, 0.76f));
            CreateDecorPart(root, "Curtain Left Tie", PrimitiveType.Cube, new Vector3(-1.5f, 1.28f, 1.28f), new Vector3(0.38f, 0.08f, 0.06f), new Color(0.84f, 0.56f, 0.3f));
            CreateDecorPart(root, "Curtain Right Tie", PrimitiveType.Cube, new Vector3(1.5f, 1.28f, 1.28f), new Vector3(0.38f, 0.08f, 0.06f), new Color(0.84f, 0.56f, 0.3f));
            CreateDecorPart(root, "Window Plant Pot", PrimitiveType.Cube, new Vector3(1.0f, 0.66f, 1.18f), new Vector3(0.34f, 0.22f, 0.08f), new Color(0.62f, 0.34f, 0.19f));
            CreateDecorPart(root, "Window Plant Leaf A", PrimitiveType.Sphere, new Vector3(0.88f, 0.88f, 1.12f), new Vector3(0.22f, 0.12f, 0.035f), new Color(0.37f, 0.63f, 0.37f));
            CreateDecorPart(root, "Window Plant Leaf B", PrimitiveType.Sphere, new Vector3(1.08f, 0.9f, 1.12f), new Vector3(0.22f, 0.12f, 0.035f), new Color(0.4f, 0.68f, 0.42f));
        }

        private static void CreateLeftFurniture(Transform root)
        {
            CreateDecorPart(root, "Left Armchair Back", PrimitiveType.Cube, new Vector3(-4.08f, -0.84f, 1.44f), new Vector3(0.72f, 0.82f, 0.14f), new Color(0.64f, 0.43f, 0.29f));
            CreateDecorPart(root, "Left Armchair Seat", PrimitiveType.Cube, new Vector3(-4.05f, -1.36f, 1.18f), new Vector3(0.9f, 0.32f, 0.16f), new Color(0.78f, 0.56f, 0.38f));
            CreateDecorPart(root, "Left Cushion", PrimitiveType.Cube, new Vector3(-3.95f, -0.98f, 1.04f), new Vector3(0.42f, 0.34f, 0.08f), new Color(1f, 0.78f, 0.36f));
            CreateDecorPart(root, "Fridge Body", PrimitiveType.Cube, new Vector3(-3.1f, -0.45f, 1.24f), new Vector3(0.82f, 1.7f, 0.16f), new Color(1f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Door Split", PrimitiveType.Cube, new Vector3(-3.1f, -0.22f, 1.1f), new Vector3(0.75f, 0.025f, 0.045f), new Color(0.82f, 0.67f, 0.48f));
            CreateDecorPart(root, "Fridge Handle", PrimitiveType.Cube, new Vector3(-2.78f, -0.25f, 1.04f), new Vector3(0.055f, 0.54f, 0.04f), new Color(0.68f, 0.43f, 0.22f));
            CreateDecorPart(root, "Fridge Face Eye L", PrimitiveType.Sphere, new Vector3(-3.22f, -0.68f, 1.0f), new Vector3(0.045f, 0.045f, 0.025f), new Color(0.32f, 0.18f, 0.1f));
            CreateDecorPart(root, "Fridge Face Eye R", PrimitiveType.Sphere, new Vector3(-2.98f, -0.68f, 1.0f), new Vector3(0.045f, 0.045f, 0.025f), new Color(0.32f, 0.18f, 0.1f));
            CreateDecorPart(root, "Fridge Smile", PrimitiveType.Cube, new Vector3(-3.1f, -0.82f, 0.98f), new Vector3(0.16f, 0.025f, 0.025f), new Color(0.32f, 0.18f, 0.1f));
            CreateCheeseBlock(root, "Floor Cheese Block", new Vector3(-2.62f, -1.66f, 0.94f), 0.34f);
        }

        private static void CreateRightFurniture(Transform root)
        {
            CreateDecorPart(root, "Right Dresser", PrimitiveType.Cube, new Vector3(3.35f, -1.08f, 1.24f), new Vector3(1.55f, 0.86f, 0.16f), new Color(0.62f, 0.37f, 0.19f));
            CreateDecorPart(root, "Right Dresser Top Cloth", PrimitiveType.Cube, new Vector3(3.35f, -0.58f, 1.08f), new Vector3(1.7f, 0.12f, 0.08f), new Color(1f, 0.92f, 0.76f));
            for (var i = 0; i < 3; i += 1)
            {
                var x = 2.86f + i * 0.48f;
                CreateDecorPart(root, $"Right Drawer {i + 1}", PrimitiveType.Cube, new Vector3(x, -1.12f, 1.02f), new Vector3(0.36f, 0.26f, 0.05f), new Color(0.74f, 0.46f, 0.24f));
                CreateDecorPart(root, $"Right Drawer Pull {i + 1}", PrimitiveType.Sphere, new Vector3(x, -1.12f, 0.96f), new Vector3(0.045f, 0.045f, 0.025f), new Color(0.95f, 0.7f, 0.32f));
            }

            CreateMilkBottle(root, "Big Bottle Table A", new Vector3(2.86f, -0.1f, 0.94f), 0.52f);
            CreateMilkBottle(root, "Big Bottle Table B", new Vector3(3.35f, -0.02f, 0.94f), 0.58f);
            CreateMilkBottle(root, "Big Bottle Table C", new Vector3(3.86f, -0.1f, 0.94f), 0.48f);
            CreateDecorPart(root, "Table Lamp Base", PrimitiveType.Cube, new Vector3(4.32f, -0.52f, 1.02f), new Vector3(0.16f, 0.34f, 0.05f), new Color(0.72f, 0.44f, 0.23f));
            CreateDecorPart(root, "Table Lamp Glow", PrimitiveType.Sphere, new Vector3(4.32f, -0.18f, 0.94f), new Vector3(0.42f, 0.34f, 0.05f), new Color(1f, 0.78f, 0.36f));
        }

        private static void CreateShelfGroup(Transform root)
        {
            CreateDecorPart(root, "Left Shelf Back Rail", PrimitiveType.Cube, new Vector3(-1.98f, 0.1f, 1.42f), new Vector3(1.28f, 0.9f, 0.08f), new Color(0.55f, 0.32f, 0.17f));
            CreateDecorPart(root, "Left Shelf Top", PrimitiveType.Cube, new Vector3(-1.98f, 0.48f, 1.16f), new Vector3(1.42f, 0.08f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            CreateDecorPart(root, "Left Shelf Bottom", PrimitiveType.Cube, new Vector3(-1.98f, -0.16f, 1.16f), new Vector3(1.42f, 0.08f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                CreateMilkBottle(root, $"Left Shelf Bottle {i + 1}", new Vector3(-2.48f + i * 0.25f, 0.68f, 1.04f), 0.3f);
                CreateMilkBottle(root, $"Left Shelf Jar {i + 1}", new Vector3(-2.48f + i * 0.25f, 0.02f, 1.04f), 0.25f);
            }

            CreateDecorPart(root, "Right Wall Shelf", PrimitiveType.Cube, new Vector3(3.28f, 1.06f, 1.2f), new Vector3(1.48f, 0.09f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                CreateMilkBottle(root, $"Right Shelf Bottle {i + 1}", new Vector3(2.72f + i * 0.28f, 1.32f, 1.04f), 0.28f);
            }

            CreateDecorPart(root, "Chalkboard", PrimitiveType.Cube, new Vector3(-2.88f, 1.48f, 1.12f), new Vector3(0.74f, 0.72f, 0.06f), new Color(0.18f, 0.28f, 0.21f));
            CreateDecorPart(root, "Chalkboard Frame Top", PrimitiveType.Cube, new Vector3(-2.88f, 1.91f, 1.08f), new Vector3(0.9f, 0.08f, 0.04f), new Color(0.64f, 0.38f, 0.18f));
            CreateDecorPart(root, "Chalkboard Frame Bottom", PrimitiveType.Cube, new Vector3(-2.88f, 1.05f, 1.08f), new Vector3(0.9f, 0.08f, 0.04f), new Color(0.64f, 0.38f, 0.18f));
            CreateDecorPart(root, "Chalkboard Frame Left", PrimitiveType.Cube, new Vector3(-3.33f, 1.48f, 1.08f), new Vector3(0.08f, 0.84f, 0.04f), new Color(0.64f, 0.38f, 0.18f));
            CreateDecorPart(root, "Chalkboard Frame Right", PrimitiveType.Cube, new Vector3(-2.43f, 1.48f, 1.08f), new Vector3(0.08f, 0.84f, 0.04f), new Color(0.64f, 0.38f, 0.18f));
            CreateWorldLabel(root, "Chalkboard Text", "Milk\nis\nMagic", new Vector3(-2.88f, 1.5f, 0.95f), 0.075f, new Color(1f, 0.9f, 0.62f));
        }

        private static void CreateHangingLights(Transform root)
        {
            CreateDecorPart(root, "Center Pendant Cord", PrimitiveType.Cube, new Vector3(0.18f, 2.88f, 1.18f), new Vector3(0.025f, 0.76f, 0.025f), new Color(0.42f, 0.25f, 0.13f));
            CreateDecorPart(root, "Center Pendant Glow", PrimitiveType.Sphere, new Vector3(0.18f, 2.36f, 1.08f), new Vector3(0.36f, 0.28f, 0.05f), new Color(1f, 0.78f, 0.34f));
            CreateStarLamp(root, "Left Star Lamp", new Vector3(-3.9f, 1.72f, 1.02f), 0.32f);
            CreateStarLamp(root, "Right Star Lamp", new Vector3(2.05f, 2.28f, 1.02f), 0.3f);
        }

        private static void CreateMilkBottle(Transform root, string name, Vector3 position, float size)
        {
            var bottleRoot = new GameObject(name).transform;
            bottleRoot.SetParent(root, false);
            bottleRoot.localPosition = position;

            CreateDecorPart(bottleRoot, "Bottle Body", PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(size * 0.22f, size * 0.46f, size * 0.08f), new Color(0.84f, 0.94f, 0.98f));
            CreateDecorPart(bottleRoot, "Bottle Cap", PrimitiveType.Cube, new Vector3(0f, size * 0.31f, -0.02f), new Vector3(size * 0.16f, size * 0.08f, size * 0.05f), new Color(0.47f, 0.72f, 0.9f));
            CreateDecorPart(bottleRoot, "Bottle Label", PrimitiveType.Cube, new Vector3(0f, -size * 0.02f, -0.05f), new Vector3(size * 0.18f, size * 0.12f, size * 0.025f), new Color(1f, 0.86f, 0.56f));
        }

        private static void CreateCheeseBlock(Transform root, string name, Vector3 position, float size)
        {
            var cheeseRoot = new GameObject(name).transform;
            cheeseRoot.SetParent(root, false);
            cheeseRoot.localPosition = position;

            CreateDecorPart(cheeseRoot, "Cheese Body", PrimitiveType.Cube, Vector3.zero, new Vector3(size, size * 0.62f, size * 0.16f), new Color(1f, 0.72f, 0.18f));
            CreateDecorPart(cheeseRoot, "Cheese Hole A", PrimitiveType.Sphere, new Vector3(-size * 0.22f, size * 0.08f, -size * 0.08f), new Vector3(size * 0.12f, size * 0.09f, size * 0.035f), new Color(0.85f, 0.48f, 0.09f));
            CreateDecorPart(cheeseRoot, "Cheese Hole B", PrimitiveType.Sphere, new Vector3(size * 0.16f, -size * 0.06f, -size * 0.08f), new Vector3(size * 0.1f, size * 0.08f, size * 0.035f), new Color(0.85f, 0.48f, 0.09f));
        }

        private static void CreateStarLamp(Transform root, string name, Vector3 position, float size)
        {
            var starRoot = new GameObject(name).transform;
            starRoot.SetParent(root, false);
            starRoot.localPosition = position;

            CreateDecorPart(starRoot, "Star Core", PrimitiveType.Sphere, Vector3.zero, new Vector3(size, size, size * 0.12f), new Color(1f, 0.86f, 0.34f));
            CreateDecorPart(starRoot, "Star Up", PrimitiveType.Cube, new Vector3(0f, size * 0.34f, 0f), new Vector3(size * 0.13f, size * 0.42f, size * 0.05f), new Color(1f, 0.86f, 0.34f));
            CreateDecorPart(starRoot, "Star Down", PrimitiveType.Cube, new Vector3(0f, -size * 0.34f, 0f), new Vector3(size * 0.13f, size * 0.42f, size * 0.05f), new Color(1f, 0.86f, 0.34f));
            CreateDecorPart(starRoot, "Star Left", PrimitiveType.Cube, new Vector3(-size * 0.34f, 0f, 0f), new Vector3(size * 0.42f, size * 0.13f, size * 0.05f), new Color(1f, 0.86f, 0.34f));
            CreateDecorPart(starRoot, "Star Right", PrimitiveType.Cube, new Vector3(size * 0.34f, 0f, 0f), new Vector3(size * 0.42f, size * 0.13f, size * 0.05f), new Color(1f, 0.86f, 0.34f));
        }

        private static Transform CreateDecorPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = Quaternion.identity;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyObjectSafely(collider);
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                PaintDecorRenderer(renderer, color);
            }

            return part.transform;
        }

        private static void CreateWorldLabel(Transform parent, string name, string text, Vector3 localPosition, float characterSize, Color color)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.identity;

            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = 64;
            label.color = color;

            var renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void PaintDecorRenderer(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);
        }

        private static CheeseTamaVisualController EnsureCheeseTamaPlaceholder()
        {
            var existing = GameObject.Find("CheeseTama Egg Placeholder");
            if (existing != null)
            {
                existing.transform.position = new Vector3(0f, -0.28f, 0f);
                existing.transform.localScale = new Vector3(1.18f, 1.24f, 1.04f);
                return GetOrCreateVisualController(existing);
            }

            var egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            egg.name = "CheeseTama Egg Placeholder";
            egg.transform.position = new Vector3(0f, -0.28f, 0f);
            egg.transform.localScale = new Vector3(1.18f, 1.24f, 1.04f);

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
                DestroyObjectSafely(child.gameObject);
            }
            else
            {
                DestroyObjectSafely(child.gameObject);
            }
        }

        private static void DestroyObjectSafely(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
                return;
            }

            Object.DestroyImmediate(target);
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
