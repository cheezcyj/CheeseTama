using CheeseTama.Data;
using CheeseTama.Environment;
using CheeseTama.Save;
using CheeseTama.UI;
using CheeseTama.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
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
            RemoveChildIfExists(topBarTransform, "Top Collection Button");
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

            var actionBar = GetOrCreatePanel(canvas.transform, "Bottom Action Bar", new Vector2(280, -968), new Vector2(1360, 84));
            var actionBarTransform = actionBar.transform;
            RemoveChildIfExists(actionBarTransform, "Collection Button");

            var milkButton = GetOrCreateButton(actionBarTransform, "Milk Button", "우유", new Vector2(-510, 20));
            ConfigureCareButton(milkButton, MilkroomCareAction.FeedMilk, controller, visualController);

            var blendButton = GetOrCreateButton(actionBarTransform, "Blend Button", "조합", new Vector2(-340, 20));
            ConfigureCareButton(blendButton, MilkroomCareAction.Blend, controller, visualController);

            var snackButton = GetOrCreateButton(actionBarTransform, "Snack Button", "간식", new Vector2(-170, 20));
            ConfigureCareButton(snackButton, MilkroomCareAction.FeedSnack, controller, visualController);

            var playButton = GetOrCreateButton(actionBarTransform, "Play Button", "놀이", new Vector2(0, 20));
            ConfigureCareButton(playButton, MilkroomCareAction.Play, controller, visualController);

            var cleanButton = GetOrCreateButton(actionBarTransform, "Clean Button", "청소", new Vector2(170, 20));
            ConfigureCareButton(cleanButton, MilkroomCareAction.Clean, controller, visualController);

            var sleepButton = GetOrCreateButton(actionBarTransform, "Sleep Button", "수면", new Vector2(340, 20));
            ConfigureCareButton(sleepButton, MilkroomCareAction.Rest, controller, visualController);

            var collectionButton = GetOrCreateButton(actionBarTransform, "Collection Button", "도감", new Vector2(510, 20));
            ConfigureNavigationButton(collectionButton, SceneNames.Collection, true);

            var actionBarController = actionBar.GetComponent<BottomActionBarController>();
            if (actionBarController == null)
            {
                actionBarController = actionBar.AddComponent<BottomActionBarController>();
            }

            actionBarController.Configure(milkButton, blendButton, snackButton, playButton, cleanButton, sleepButton, collectionButton);
            BuildMilkroomSettings(canvas.transform, settingsButton, controller, visualController);
            OrganizeMilkroomSceneHierarchy();
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
#else
            RemoveChildIfExists(canvasTransform, "Dev Panel");
#endif
        }

        private static void OrganizeMilkroomSceneHierarchy()
        {
            var sceneRoot = GameObject.Find("MilkroomSceneRoot");
            if (sceneRoot == null)
            {
                sceneRoot = new GameObject("MilkroomSceneRoot");
            }

            var cameraRig = GetOrCreateSceneGroup(sceneRoot.transform, "CameraRig");
            var lighting = GetOrCreateSceneGroup(sceneRoot.transform, "Lighting");
            var environment = GetOrCreateSceneGroup(sceneRoot.transform, "Environment");
            var character = GetOrCreateSceneGroup(sceneRoot.transform, "Character");
            var vfx = GetOrCreateSceneGroup(sceneRoot.transform, "VFX");
            var ui = GetOrCreateSceneGroup(sceneRoot.transform, "UI");

            ReparentIfFound("MainCamera", cameraRig);
            ReparentIfFound("Milkroom Camera", cameraRig);
            ReparentIfFound("Milkroom Key Light", lighting);
            ReparentIfFound("Milkroom Fill Light", lighting);
            ReparentIfFound("Milkroom Rim Light", lighting);
            ReparentIfFound("GlobalVolume", lighting);
            ReparentIfFound("Milkroom Background", environment);
            ReparentIfFound("CheeseTamaRoot", character);
            ReparentIfFound("CheeseTama Egg Placeholder", character);
            ReparentIfFound("Milkroom Canvas", ui);
            ReparentIfFound("EventSystem", ui);

            var milkDrops = GetOrCreateSceneGroup(vfx, "MilkDrops");
            var softSparkles = GetOrCreateSceneGroup(vfx, "SoftSparkles");
            var cameraTarget = GetOrCreateSceneGroup(cameraRig, "CameraTarget");
            cameraTarget.localPosition = new Vector3(0f, -0.55f, 0.55f);
            milkDrops.localPosition = Vector3.zero;
            softSparkles.localPosition = Vector3.zero;
        }

        private static Transform GetOrCreateSceneGroup(Transform parent, string name)
        {
            var group = parent.Find(name);
            if (group != null)
            {
                return group;
            }

            var groupObject = new GameObject(name);
            groupObject.transform.SetParent(parent, false);
            return groupObject.transform;
        }

        private static void ReparentIfFound(string objectName, Transform parent)
        {
            var target = GameObject.Find(objectName);
            if (target == null || target.transform == parent || target.transform.IsChildOf(parent))
            {
                return;
            }

            target.transform.SetParent(parent, true);
        }

        private static Camera EnsureCamera(string name)
        {
            var existing = Object.FindFirstObjectByType<Camera>();
            if (existing != null)
            {
                ConfigureMilkroomCamera(existing, name);
                return existing;
            }

            var cameraObject = new GameObject(name);
            var camera = cameraObject.AddComponent<Camera>();
            ConfigureMilkroomCamera(camera, name);
            return camera;
        }

        private static void ConfigureMilkroomCamera(Camera camera, string name)
        {
            camera.gameObject.name = name == "Milkroom Camera" ? "MainCamera" : name;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.96f, 0.92f, 0.84f);
            if (name == "Milkroom Camera")
            {
                camera.orthographic = false;
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 40f;
                camera.transform.position = new Vector3(0f, 0.2f, -9.4f);
                camera.transform.rotation = Quaternion.identity;
            }
            else
            {
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.transform.rotation = Quaternion.identity;
            }

            if (camera.gameObject.CompareTag("Untagged"))
            {
                camera.gameObject.tag = "MainCamera";
            }
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

            var fillObject = GameObject.Find("Milkroom Fill Light");
            if (fillObject == null)
            {
                fillObject = new GameObject("Milkroom Fill Light");
            }

            var fillLight = fillObject.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = fillObject.AddComponent<Light>();
            }

            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.76f, 0.92f, 1f);
            fillLight.intensity = 0.45f;
            fillObject.transform.rotation = Quaternion.Euler(25, 145, 0);

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
            rimLight.color = new Color(1f, 0.78f, 0.34f);
            rimLight.intensity = 0.62f;
            rimObject.transform.rotation = Quaternion.Euler(32f, 208f, 0f);

            var volumeObject = GameObject.Find("GlobalVolume");
            if (volumeObject == null)
            {
                volumeObject = new GameObject("GlobalVolume");
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.86f, 0.74f, 0.56f);
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

            var roomShell = CreateLayerRoot(root, "RoomShell");
            var windowSet = CreateLayerRoot(root, "WindowSet");
            var fridgeSet = CreateLayerRoot(root, "FridgeSet");
            var milkShelfSet = CreateLayerRoot(root, "MilkShelfSet");
            var blendingTableSet = CreateLayerRoot(root, "BlendingTableSet");
            var chalkboardSet = CreateLayerRoot(root, "ChalkboardSet");
            var rug = CreateLayerRoot(root, "Rug");
            var cozyChair = CreateLayerRoot(root, "CozyChair");
            var lamps = CreateLayerRoot(root, "Lamps");
            var props = CreateLayerRoot(root, "Props");
            var themeVfxRoot = CreateLayerRoot(root, "ThemeVFXRoot");

            CreateDioramaRoomShell(roomShell);
            CreateDioramaWindowSet(windowSet);
            CreateDioramaFridgeSet(fridgeSet);
            CreateDioramaMilkShelfSet(milkShelfSet);
            CreateDioramaBlendingTableSet(blendingTableSet);
            CreateDioramaChalkboardSet(chalkboardSet);
            CreateDioramaRug(rug);
            CreateDioramaCozyChair(cozyChair);
            CreateDioramaLamps(lamps);
            CreateDioramaProps(props);
            CreateAmbientThemeVfx(themeVfxRoot);
            CreateLayerRoot(rug, "CheeseTamaAnchor").localPosition = new Vector3(0f, -0.28f, 0.05f);
            AddMilkroomControllers(root, root, root, rug, props, themeVfxRoot);
        }

        private static Transform CreateLayerRoot(Transform parent, string name)
        {
            var layer = new GameObject(name).transform;
            layer.SetParent(parent, false);
            layer.localPosition = Vector3.zero;
            layer.localRotation = Quaternion.identity;
            layer.localScale = Vector3.one;
            return layer;
        }

        private static void AddMilkroomControllers(
            Transform root,
            Transform backgroundRoot,
            Transform midgroundRoot,
            Transform playAreaRoot,
            Transform foregroundRoot,
            Transform themeVfxRoot)
        {
            var propController = root.GetComponent<MilkroomPropController>();
            if (propController == null)
            {
                propController = root.gameObject.AddComponent<MilkroomPropController>();
            }

            propController.Configure(backgroundRoot, midgroundRoot, playAreaRoot, foregroundRoot, themeVfxRoot);

            var ambientController = root.GetComponent<MilkroomAmbientEventController>();
            if (ambientController == null)
            {
                ambientController = root.gameObject.AddComponent<MilkroomAmbientEventController>();
            }

            ambientController.Configure(themeVfxRoot);

            var themeController = root.GetComponent<MilkroomThemeController>();
            if (themeController == null)
            {
                themeController = root.gameObject.AddComponent<MilkroomThemeController>();
            }

            themeController.Configure(backgroundRoot, midgroundRoot, playAreaRoot, foregroundRoot, themeVfxRoot);
            themeController.ApplyTheme(MilkroomThemeController.MorningThemeId);

            var lightingController = root.GetComponent<MilkroomLightingController>();
            if (lightingController == null)
            {
                lightingController = root.gameObject.AddComponent<MilkroomLightingController>();
            }

            lightingController.ApplyTheme(MilkroomThemeController.MorningThemeId);
        }

        private static void CreateDioramaRoomShell(Transform root)
        {
            CreateDecorPart(root, "BackWall", PrimitiveType.Cube, new Vector3(0f, 0.42f, 3.1f), new Vector3(8.9f, 4.6f, 0.28f), new Color(0.78f, 0.58f, 0.39f));
            CreateDecorPart(root, "LeftWall", PrimitiveType.Cube, new Vector3(-4.58f, 0.18f, 1.55f), new Vector3(0.3f, 4.25f, 3.35f), new Color(0.7f, 0.49f, 0.33f));
            CreateDecorPart(root, "RightWall", PrimitiveType.Cube, new Vector3(4.58f, 0.18f, 1.55f), new Vector3(0.3f, 4.25f, 3.35f), new Color(0.7f, 0.49f, 0.33f));
            CreateDecorPart(root, "Floor", PrimitiveType.Cube, new Vector3(0f, -2.28f, 1.2f), new Vector3(9.25f, 0.26f, 4.6f), new Color(0.56f, 0.33f, 0.18f));
            CreateDecorPart(root, "BackWall Baseboard", PrimitiveType.Cube, new Vector3(0f, -1.75f, 2.88f), new Vector3(8.6f, 0.16f, 0.12f), new Color(0.46f, 0.27f, 0.14f));
            CreateDecorPart(root, "LeftWall Baseboard", PrimitiveType.Cube, new Vector3(-4.36f, -1.75f, 1.35f), new Vector3(0.12f, 0.16f, 2.9f), new Color(0.46f, 0.27f, 0.14f));
            CreateDecorPart(root, "RightWall Baseboard", PrimitiveType.Cube, new Vector3(4.36f, -1.75f, 1.35f), new Vector3(0.12f, 0.16f, 2.9f), new Color(0.46f, 0.27f, 0.14f));
            CreateDecorPart(root, "Ceiling Wood Beam", PrimitiveType.Cube, new Vector3(0f, 2.68f, 1.64f), new Vector3(8.85f, 0.22f, 0.22f), new Color(0.48f, 0.29f, 0.16f));
            CreateDecorPart(root, "Warm Morning Wall Light", PrimitiveType.Sphere, new Vector3(-0.25f, 1.0f, 2.72f), new Vector3(4.8f, 2.5f, 0.12f), new Color(1f, 0.76f, 0.42f));

            for (var i = 0; i < 7; i += 1)
            {
                var z = -0.82f + i * 0.48f;
                CreateDecorPart(root, $"Floor Depth Plank {i + 1}", PrimitiveType.Cube, new Vector3(0f, -2.12f, z), new Vector3(8.65f, 0.028f, 0.035f), new Color(0.42f, 0.25f, 0.14f));
            }

            for (var i = 0; i < 9; i += 1)
            {
                var x = -4f + i;
                CreateDecorPart(root, $"Floor Width Plank {i + 1}", PrimitiveType.Cube, new Vector3(x, -2.11f, 1.04f), new Vector3(0.03f, 0.03f, 3.35f), new Color(0.47f, 0.28f, 0.15f));
            }
        }

        private static void CreateDioramaWindowSet(Transform root)
        {
            CreateDecorPart(root, "WindowGlass", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.84f), new Vector3(2.35f, 1.45f, 0.08f), new Color(0.58f, 0.78f, 0.92f));
            CreateDecorPart(root, "Window Sun Glow", PrimitiveType.Sphere, new Vector3(-0.1f, 1.55f, 2.74f), new Vector3(0.5f, 0.5f, 0.08f), new Color(1f, 0.8f, 0.34f));
            CreateDecorPart(root, "Window Cloud Left", PrimitiveType.Sphere, new Vector3(-1.3f, 1.34f, 2.68f), new Vector3(0.46f, 0.16f, 0.05f), new Color(0.94f, 0.98f, 1f));
            CreateDecorPart(root, "Window Cloud Right", PrimitiveType.Sphere, new Vector3(-0.62f, 0.98f, 2.68f), new Vector3(0.42f, 0.14f, 0.05f), new Color(0.94f, 0.98f, 1f));

            var frameColor = new Color(0.96f, 0.83f, 0.58f);
            CreateDecorPart(root, "WindowFrame Top", PrimitiveType.Cube, new Vector3(-0.75f, 1.92f, 2.62f), new Vector3(2.62f, 0.11f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Bottom", PrimitiveType.Cube, new Vector3(-0.75f, 0.38f, 2.62f), new Vector3(2.62f, 0.13f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Left", PrimitiveType.Cube, new Vector3(-2.06f, 1.15f, 2.62f), new Vector3(0.13f, 1.62f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Right", PrimitiveType.Cube, new Vector3(0.56f, 1.15f, 2.62f), new Vector3(0.13f, 1.62f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Vertical", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.56f), new Vector3(0.09f, 1.5f, 0.14f), frameColor);
            CreateDecorPart(root, "WindowFrame Horizontal", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.55f), new Vector3(2.42f, 0.09f, 0.14f), frameColor);

            CreateDecorPart(root, "Curtains Left", PrimitiveType.Cube, new Vector3(-2.38f, 1.12f, 2.46f), new Vector3(0.42f, 1.78f, 0.16f), new Color(0.98f, 0.82f, 0.64f));
            CreateDecorPart(root, "Curtains Right", PrimitiveType.Cube, new Vector3(0.88f, 1.12f, 2.46f), new Vector3(0.42f, 1.78f, 0.16f), new Color(0.98f, 0.82f, 0.64f));
            CreateDecorPart(root, "Curtains Left Tie", PrimitiveType.Cube, new Vector3(-2.24f, 0.85f, 2.3f), new Vector3(0.36f, 0.09f, 0.09f), new Color(0.78f, 0.48f, 0.24f));
            CreateDecorPart(root, "Curtains Right Tie", PrimitiveType.Cube, new Vector3(0.74f, 0.85f, 2.3f), new Vector3(0.36f, 0.09f, 0.09f), new Color(0.78f, 0.48f, 0.24f));
        }

        private static void CreateDioramaFridgeSet(Transform root)
        {
            CreateDecorPart(root, "Fridge Body Rounded", PrimitiveType.Cube, new Vector3(-3.35f, -0.55f, 1.72f), new Vector3(0.9f, 1.65f, 0.62f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Fridge Top Round", PrimitiveType.Sphere, new Vector3(-3.35f, 0.32f, 1.72f), new Vector3(0.46f, 0.18f, 0.32f), new Color(0.98f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Door Split", PrimitiveType.Cube, new Vector3(-3.35f, -0.32f, 1.36f), new Vector3(0.76f, 0.035f, 0.06f), new Color(0.74f, 0.58f, 0.4f));
            CreateDecorPart(root, "Fridge Handle", PrimitiveType.Cube, new Vector3(-2.94f, -0.36f, 1.32f), new Vector3(0.055f, 0.52f, 0.06f), new Color(0.64f, 0.4f, 0.22f));
            CreateDecorPart(root, "Fridge Face Eye L", PrimitiveType.Sphere, new Vector3(-3.48f, -0.7f, 1.29f), new Vector3(0.048f, 0.048f, 0.032f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Face Eye R", PrimitiveType.Sphere, new Vector3(-3.22f, -0.7f, 1.29f), new Vector3(0.048f, 0.048f, 0.032f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Smile", PrimitiveType.Cube, new Vector3(-3.35f, -0.84f, 1.26f), new Vector3(0.16f, 0.025f, 0.025f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Milk Memo", PrimitiveType.Cube, new Vector3(-3.12f, 0.02f, 1.27f), new Vector3(0.22f, 0.18f, 0.035f), new Color(1f, 0.76f, 0.34f));
        }

        private static void CreateDioramaMilkShelfSet(Transform root)
        {
            CreateDecorPart(root, "MilkShelf Back", PrimitiveType.Cube, new Vector3(1.75f, 0f, 2.38f), new Vector3(1.65f, 1.18f, 0.16f), new Color(0.5f, 0.3f, 0.16f));
            CreateDecorPart(root, "MilkShelf Top", PrimitiveType.Cube, new Vector3(1.75f, 0.56f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));
            CreateDecorPart(root, "MilkShelf Middle", PrimitiveType.Cube, new Vector3(1.75f, 0.05f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));
            CreateDecorPart(root, "MilkShelf Bottom", PrimitiveType.Cube, new Vector3(1.75f, -0.48f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));

            for (var i = 0; i < 4; i += 1)
            {
                CreateMilkBottle(root, $"MilkShelf Bottle Top {i + 1}", new Vector3(1.13f + i * 0.38f, 0.82f, 1.94f), 0.32f);
            }

            for (var i = 0; i < 3; i += 1)
            {
                CreateMilkBottle(root, $"MilkShelf Bottle Lower {i + 1}", new Vector3(1.32f + i * 0.42f, 0.3f, 1.94f), 0.28f);
            }
        }

        private static void CreateDioramaBlendingTableSet(Transform root)
        {
            CreateDecorPart(root, "BlendingTable Top", PrimitiveType.Cube, new Vector3(2.9f, -1.15f, 1.0f), new Vector3(1.25f, 0.18f, 0.58f), new Color(0.66f, 0.39f, 0.2f));
            CreateDecorPart(root, "BlendingTable Cloth", PrimitiveType.Cube, new Vector3(2.9f, -1.03f, 0.7f), new Vector3(1.36f, 0.09f, 0.12f), new Color(1f, 0.88f, 0.64f));
            CreateDecorPart(root, "BlendingTable Leg L", PrimitiveType.Cube, new Vector3(2.42f, -1.62f, 1.02f), new Vector3(0.09f, 0.76f, 0.09f), new Color(0.48f, 0.27f, 0.13f));
            CreateDecorPart(root, "BlendingTable Leg R", PrimitiveType.Cube, new Vector3(3.38f, -1.62f, 1.02f), new Vector3(0.09f, 0.76f, 0.09f), new Color(0.48f, 0.27f, 0.13f));
            CreateDecorPart(root, "Blending Bowl", PrimitiveType.Sphere, new Vector3(2.72f, -0.92f, 0.66f), new Vector3(0.28f, 0.12f, 0.12f), new Color(0.82f, 0.94f, 0.98f));
            CreateDecorPart(root, "Blending Spoon", PrimitiveType.Cube, new Vector3(3.1f, -0.86f, 0.62f), new Vector3(0.45f, 0.035f, 0.03f), new Color(0.82f, 0.6f, 0.34f));
            CreateMilkBottle(root, "Blending Milk Bottle", new Vector3(3.32f, -0.78f, 0.58f), 0.34f);
        }

        private static void CreateDioramaChalkboardSet(Transform root)
        {
            CreateDecorPart(root, "Chalkboard", PrimitiveType.Cube, new Vector3(-2.52f, 1.15f, 2.54f), new Vector3(0.92f, 0.72f, 0.08f), new Color(0.15f, 0.25f, 0.2f));
            CreateDecorPart(root, "Chalkboard Frame Top", PrimitiveType.Cube, new Vector3(-2.52f, 1.55f, 2.47f), new Vector3(1.08f, 0.08f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Bottom", PrimitiveType.Cube, new Vector3(-2.52f, 0.75f, 2.47f), new Vector3(1.08f, 0.08f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Left", PrimitiveType.Cube, new Vector3(-3.06f, 1.15f, 2.47f), new Vector3(0.08f, 0.82f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Right", PrimitiveType.Cube, new Vector3(-1.98f, 1.15f, 2.47f), new Vector3(0.08f, 0.82f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateWorldLabel(root, "Chalkboard Text", "Milk\nis\nMagic", new Vector3(-2.52f, 1.16f, 2.39f), 0.075f, new Color(1f, 0.9f, 0.62f));
        }

        private static void CreateDioramaRug(Transform root)
        {
            CreateDecorPart(root, "Rug Base", PrimitiveType.Sphere, new Vector3(0f, -2.05f, 0.62f), new Vector3(1.92f, 0.13f, 0.82f), new Color(0.9f, 0.78f, 0.56f));
            CreateDecorPart(root, "Rug Soft Center", PrimitiveType.Sphere, new Vector3(0f, -2.0f, 0.52f), new Vector3(1.55f, 0.08f, 0.62f), new Color(1f, 0.9f, 0.68f));
            CreateDecorPart(root, "Rug Paw Center", PrimitiveType.Sphere, new Vector3(0f, -1.95f, 0.42f), new Vector3(0.32f, 0.04f, 0.08f), new Color(0.78f, 0.62f, 0.42f));
            CreateDecorPart(root, "Rug Paw Toe L", PrimitiveType.Sphere, new Vector3(-0.34f, -1.9f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
            CreateDecorPart(root, "Rug Paw Toe C", PrimitiveType.Sphere, new Vector3(0f, -1.86f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
            CreateDecorPart(root, "Rug Paw Toe R", PrimitiveType.Sphere, new Vector3(0.34f, -1.9f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
        }

        private static void CreateDioramaCozyChair(Transform root)
        {
            CreateDecorPart(root, "CozyChair Back", PrimitiveType.Cube, new Vector3(-3.55f, -1.1f, 0.42f), new Vector3(0.92f, 0.8f, 0.34f), new Color(0.58f, 0.4f, 0.28f));
            CreateDecorPart(root, "CozyChair Seat", PrimitiveType.Cube, new Vector3(-3.55f, -1.62f, 0.12f), new Vector3(1.02f, 0.26f, 0.58f), new Color(0.72f, 0.52f, 0.36f));
            CreateDecorPart(root, "CozyChair Arm L", PrimitiveType.Cube, new Vector3(-4.12f, -1.38f, 0.22f), new Vector3(0.16f, 0.52f, 0.48f), new Color(0.5f, 0.31f, 0.18f));
            CreateDecorPart(root, "CozyChair Arm R", PrimitiveType.Cube, new Vector3(-2.98f, -1.38f, 0.22f), new Vector3(0.16f, 0.52f, 0.48f), new Color(0.5f, 0.31f, 0.18f));
            CreateDecorPart(root, "CozyChair Butter Cushion", PrimitiveType.Cube, new Vector3(-3.55f, -1.22f, -0.04f), new Vector3(0.46f, 0.32f, 0.12f), new Color(1f, 0.72f, 0.28f));
        }

        private static void CreateDioramaLamps(Transform root)
        {
            CreateDecorPart(root, "Pendant Cord", PrimitiveType.Cube, new Vector3(0.2f, 2.55f, 1.55f), new Vector3(0.035f, 0.58f, 0.035f), new Color(0.34f, 0.2f, 0.1f));
            CreateDecorPart(root, "Pendant Warm Shade", PrimitiveType.Sphere, new Vector3(0.2f, 2.16f, 1.55f), new Vector3(0.36f, 0.2f, 0.24f), new Color(1f, 0.74f, 0.32f));
            CreateDecorPart(root, "Pendant Warm Glow", PrimitiveType.Sphere, new Vector3(0.2f, 1.94f, 1.48f), new Vector3(0.54f, 0.22f, 0.28f), new Color(1f, 0.8f, 0.42f));
            CreateStarLamp(root, "Left Star Lamp", new Vector3(-3.86f, 1.68f, 2.34f), 0.24f);
        }

        private static void CreateDioramaProps(Transform root)
        {
            CreateDecorPart(root, "Plant Pot", PrimitiveType.Cube, new Vector3(0.84f, -1.68f, 2.12f), new Vector3(0.34f, 0.24f, 0.28f), new Color(0.56f, 0.31f, 0.17f));
            CreateDecorPart(root, "Plant Leaf L", PrimitiveType.Sphere, new Vector3(0.68f, -1.38f, 2.04f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.32f, 0.58f, 0.32f));
            CreateDecorPart(root, "Plant Leaf R", PrimitiveType.Sphere, new Vector3(1.0f, -1.36f, 2.04f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.36f, 0.64f, 0.36f));
            CreateDecorPart(root, "Wall Memo A", PrimitiveType.Cube, new Vector3(3.75f, 0.78f, 2.52f), new Vector3(0.28f, 0.22f, 0.035f), new Color(1f, 0.86f, 0.52f));
            CreateDecorPart(root, "Wall Memo B", PrimitiveType.Cube, new Vector3(3.42f, 0.42f, 2.52f), new Vector3(0.22f, 0.18f, 0.035f), new Color(0.78f, 0.92f, 1f));
            CreateMilkBottle(root, "Loose Milk Bottle", new Vector3(-0.96f, -1.66f, 0.12f), 0.3f);
            CreateDecorPart(root, "Foreground Soft Milk Drop L", PrimitiveType.Sphere, new Vector3(-1.8f, -2.02f, -0.68f), new Vector3(0.22f, 0.045f, 0.08f), new Color(0.92f, 0.86f, 0.74f));
            CreateDecorPart(root, "Foreground Soft Milk Drop R", PrimitiveType.Sphere, new Vector3(2.1f, -2.02f, -0.62f), new Vector3(0.26f, 0.05f, 0.09f), new Color(0.92f, 0.86f, 0.74f));
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

        private static void CreateBlendingTable(Transform root)
        {
            var tableRoot = new GameObject("BlendingTable").transform;
            tableRoot.SetParent(root, false);
            tableRoot.localPosition = Vector3.zero;

            CreateDecorPart(tableRoot, "BlendingTable Top", PrimitiveType.Cube, new Vector3(-0.12f, -1.02f, 1.08f), new Vector3(1.18f, 0.16f, 0.16f), new Color(0.72f, 0.43f, 0.22f));
            CreateDecorPart(tableRoot, "BlendingTable Cloth", PrimitiveType.Cube, new Vector3(-0.12f, -0.92f, 0.94f), new Vector3(1.28f, 0.08f, 0.08f), new Color(1f, 0.9f, 0.68f));
            CreateDecorPart(tableRoot, "BlendingTable Leg L", PrimitiveType.Cube, new Vector3(-0.58f, -1.42f, 1.12f), new Vector3(0.08f, 0.72f, 0.08f), new Color(0.52f, 0.29f, 0.14f));
            CreateDecorPart(tableRoot, "BlendingTable Leg R", PrimitiveType.Cube, new Vector3(0.34f, -1.42f, 1.12f), new Vector3(0.08f, 0.72f, 0.08f), new Color(0.52f, 0.29f, 0.14f));
            CreateDecorPart(tableRoot, "Blending Bowl", PrimitiveType.Sphere, new Vector3(-0.26f, -0.72f, 0.9f), new Vector3(0.28f, 0.12f, 0.08f), new Color(0.84f, 0.94f, 0.98f));
            CreateDecorPart(tableRoot, "Blending Spoon", PrimitiveType.Cube, new Vector3(0.14f, -0.66f, 0.86f), new Vector3(0.42f, 0.035f, 0.025f), new Color(0.86f, 0.64f, 0.36f));
            CreateMilkBottle(tableRoot, "Blending Milk Bottle", new Vector3(0.42f, -0.6f, 0.88f), 0.34f);
        }

        private static void CreateShelfGroup(Transform root)
        {
            var shelfRoot = new GameObject("MilkShelf").transform;
            shelfRoot.SetParent(root, false);
            shelfRoot.localPosition = Vector3.zero;

            CreateDecorPart(shelfRoot, "Left Shelf Back Rail", PrimitiveType.Cube, new Vector3(-1.98f, 0.1f, 1.42f), new Vector3(1.28f, 0.9f, 0.08f), new Color(0.55f, 0.32f, 0.17f));
            CreateDecorPart(shelfRoot, "Left Shelf Top", PrimitiveType.Cube, new Vector3(-1.98f, 0.48f, 1.16f), new Vector3(1.42f, 0.08f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            CreateDecorPart(shelfRoot, "Left Shelf Bottom", PrimitiveType.Cube, new Vector3(-1.98f, -0.16f, 1.16f), new Vector3(1.42f, 0.08f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                CreateMilkBottle(shelfRoot, $"Left Shelf Bottle {i + 1}", new Vector3(-2.48f + i * 0.25f, 0.68f, 1.04f), 0.3f);
                CreateMilkBottle(shelfRoot, $"Left Shelf Jar {i + 1}", new Vector3(-2.48f + i * 0.25f, 0.02f, 1.04f), 0.25f);
            }

            CreateDecorPart(shelfRoot, "Right Wall Shelf", PrimitiveType.Cube, new Vector3(3.28f, 1.06f, 1.2f), new Vector3(1.48f, 0.09f, 0.08f), new Color(0.7f, 0.43f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                CreateMilkBottle(shelfRoot, $"Right Shelf Bottle {i + 1}", new Vector3(2.72f + i * 0.28f, 1.32f, 1.04f), 0.28f);
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

        private static void CreateMilkroomForeground(Transform root)
        {
            CreateDecorPart(root, "Foreground Soft Milk Drop L", PrimitiveType.Sphere, new Vector3(-3.38f, -2.12f, 0.48f), new Vector3(0.28f, 0.055f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Foreground Soft Milk Drop C", PrimitiveType.Sphere, new Vector3(2.2f, -2.1f, 0.48f), new Vector3(0.22f, 0.05f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Foreground Soft Milk Drop R", PrimitiveType.Sphere, new Vector3(4.05f, -2.0f, 0.48f), new Vector3(0.34f, 0.06f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Foreground Warm Vignette", PrimitiveType.Cube, new Vector3(0f, -2.98f, 0.36f), new Vector3(10.8f, 0.18f, 0.04f), new Color(0.35f, 0.2f, 0.12f));
        }

        private static void CreateAmbientThemeVfx(Transform root)
        {
            for (var i = 0; i < 12; i += 1)
            {
                var x = -4.6f + i * 0.84f;
                var y = 2.38f - (i % 4) * 0.34f;
                CreateDecorPart(root, $"Rain Streak {i + 1}", PrimitiveType.Cube, new Vector3(x, y, 0.62f), new Vector3(0.025f, 0.34f, 0.025f), new Color(0.62f, 0.74f, 0.82f));
            }

            for (var i = 0; i < 14; i += 1)
            {
                var x = -1.05f + (i % 7) * 0.36f;
                var y = 1.28f + (i / 7) * 0.38f;
                CreateDecorPart(root, $"Night Star Speckle {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 0.58f), new Vector3(0.035f, 0.035f, 0.018f), new Color(0.78f, 0.88f, 1f));
            }

            CreateDecorPart(root, "Evening Window Beam L", PrimitiveType.Cube, new Vector3(-0.7f, 0.08f, 0.54f), new Vector3(0.12f, 2.4f, 0.02f), new Color(1f, 0.62f, 0.25f));
            CreateDecorPart(root, "Evening Window Beam R", PrimitiveType.Cube, new Vector3(0.7f, 0.08f, 0.54f), new Vector3(0.12f, 2.4f, 0.02f), new Color(1f, 0.62f, 0.25f));
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
                renderer.shadowCastingMode = ShouldCastDecorShadow(name) ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = !name.Contains("Glow") && !name.Contains("Sparkle") && !name.Contains("Rain");
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
            ToonMaterialUtility.Apply(renderer, ToonMaterialUtility.InferProfile(renderer), color);
        }

        private static bool ShouldCastDecorShadow(string objectName)
        {
            return !objectName.Contains("Glow")
                && !objectName.Contains("Window Sky")
                && !objectName.Contains("Cloud")
                && !objectName.Contains("Rain")
                && !objectName.Contains("Star Speckle")
                && !objectName.Contains("Vignette");
        }

        private static CheeseTamaVisualController EnsureCheeseTamaPlaceholder()
        {
            var existing = GameObject.Find("CheeseTamaRoot");
            if (existing == null)
            {
                existing = GameObject.Find("CheeseTama Egg Placeholder");
            }

            if (existing != null)
            {
                existing.name = "CheeseTamaRoot";
                existing.transform.position = new Vector3(0f, -0.28f, 0.08f);
                existing.transform.localScale = new Vector3(1.2f, 1.2f, 1.08f);
                return GetOrCreateVisualController(existing);
            }

            var egg = new GameObject("CheeseTamaRoot");
            egg.transform.position = new Vector3(0f, -0.28f, 0.08f);
            egg.transform.localScale = new Vector3(1.2f, 1.2f, 1.08f);

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
