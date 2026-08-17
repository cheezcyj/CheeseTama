using CheeseTama.Audio;
using CheeseTama.Data;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.Snacks;
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
        private const int RoundedUiSpriteSize = 32;
        private const int RoundedUiCornerRadius = 8;
        private const float MilkroomSideMargin = 24f;
        private const float MilkroomRightPanelWidth = 360f;
        private const float TopHudTop = 16f;
        private const float TopHudHeight = 82f;
        private const float TopHudGap = 18f;
        private const float TopMenuPadding = 12f;
        private const float TopMenuButtonGap = 10f;
        private const float TopMenuButtonTop = 14f;
        private const float TopMenuButtonHeight = 54f;
        private const string RecordDetailVerticalGap = "\n<size=3> </size>\n";
        private static readonly Vector2 MilkroomToolPanelPosition = new Vector2(420f, -184f);
        private static readonly Vector2 MilkroomToolPanelSize = new Vector2(680f, 590f);

        private static Texture2D roundedUiTexture;
        private static Sprite roundedUiSprite;
        private static Texture2D circleUiTexture;
        private static Sprite circleUiSprite;

        public static GameManager EnsureCoreSystems()
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentSave == null)
                {
                    GameManager.Instance.LoadOrCreateGame();
                }

                EnsureAudioController(GameManager.Instance);
                return GameManager.Instance;
            }

            var existing = Object.FindFirstObjectByType<GameManager>();
            if (existing != null)
            {
                if (existing.CurrentSave == null)
                {
                    existing.LoadOrCreateGame();
                }

                EnsureAudioController(existing);
                return existing;
            }

            var core = new GameObject("CoreSystems");
            core.AddComponent<DataRegistry>();
            core.AddComponent<SaveManager>();
            var manager = core.AddComponent<GameManager>();
            EnsureAudioController(manager);
            return manager;
        }

        private static void EnsureAudioController(GameManager manager)
        {
            if (manager == null)
            {
                return;
            }

            var audioController = manager.GetComponent<CheeseTamaAudioController>();
            if (audioController == null)
            {
                audioController = manager.gameObject.AddComponent<CheeseTamaAudioController>();
            }

            audioController.BindManager(manager);
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

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                EnsureUiButtonSounds(canvas.transform);
            }
        }

        public static bool TryBindExistingSceneForRuntime(string sceneName)
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            if (sceneName == SceneNames.Debug)
            {
                return TryBindExistingDebugSceneForRuntime();
            }

            if (sceneName != SceneNames.Milkroom)
            {
                return false;
            }

            var canvas = GameObject.Find("Milkroom Canvas");
            var controller = Object.FindFirstObjectByType<MilkroomUIController>();
            if (canvas == null || controller == null)
            {
                return false;
            }

            RemoveLegacyMilkroomPanelObjects(canvas.transform);
            ReapplyRoundedImages(canvas.transform);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            RemoveChildIfExists(canvas.transform, "Dev Panel");
            RemoveChildIfExists(canvas.transform, "Dev Mode Toggle Button");
#endif

            var manager = EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            ApplySavedMilkroomTheme(manager);
            controller.Bind(manager.CurrentSave);
            controller.ShowMessage("돌봄 준비 완료.");

            var visualController = Object.FindFirstObjectByType<CheeseTamaVisualController>();
            if (visualController != null)
            {
                AlignCheeseTamaRestingPosition(visualController);
                visualController.Bind(manager.CurrentTama);
            }

            var existingSettingsButton = canvas.transform.Find("Top Menu/Settings Button")?.GetComponent<Button>();
            if (existingSettingsButton != null)
            {
                BuildMilkroomSettings(
                    canvas.transform,
                    existingSettingsButton,
                    controller,
                    visualController,
                    out var settingsLastSavedText);
                controller.SetLastSavedText(settingsLastSavedText);
            }
            else
            {
                return false;
            }

            Object.FindFirstObjectByType<MilkPanelController>()?.Close();
            Object.FindFirstObjectByType<CookingPanelController>()?.Close();
            Object.FindFirstObjectByType<SnackPanelController>()?.Close();
            EnsureCheeseTamaProfileMenuShell(canvas.transform);
            EnsureMilkroomStatGauges(canvas.transform, controller);
            EnsureCheeseTamaNameDialog(canvas.transform, controller);
            EnsureFirstMeetingOnboarding(canvas.transform, controller, visualController);
            EnsureNewGameSetup(canvas.transform, controller, visualController);
            EnsureMilkBlendingPanel(canvas.transform, controller, visualController);
            EnsureCookingChoicePanel(canvas.transform);
            EnsureSaveRecoveryNotice(canvas.transform);
            EnsureReturnSummary(canvas.transform);
            EnsureGrowthMilestone(canvas.transform, controller, visualController);
            EnsureEvolutionMilestone(canvas.transform, controller, visualController);
            EnsureGrowthJourney(canvas.transform);
            EnsureMilkDropMiniGame(canvas.transform, controller, visualController);
            EnsureBouncyJumpMiniGame(canvas.transform, controller, visualController);
            EnsurePlayChoicePanel(canvas.transform);
            EnsureCleaningMiniGame(canvas.transform, controller, visualController);
            EnsureCareEventCard(canvas.transform, visualController);
            EnsureNpcVisitCard(canvas.transform);
            EnsureSleepSchedulePanel(canvas.transform, controller, visualController);
            EnsureDecorationShop(canvas.transform);
            EnsureDecorationRoomPresenter();
            EnsureMilkroomAtmosphere(canvas.transform, manager.CurrentTama);
            EnsureCheeseTamaPetInteraction(canvas.transform, controller, visualController);
            EnsureCheeseTamaSpeechBubble(canvas.transform, visualController);
            EnsureAutonomousLife(canvas.transform, visualController);
            RemoveNormalEvolutionVisualAccents(visualController);
            EnsureLateGameFeatures(canvas.transform, controller, visualController);
            EnsureCheeseStarDelivery(canvas.transform);
            EnsureMemoryJournal(canvas.transform);
            EnsureFantasyPowderHiddenRecipes(canvas.transform);
            EnsureFirstDayJourney(canvas.transform);
            EnsureCheeseTamaProfileMenu(canvas.transform);
            if (!EnsureInputBindingsPanel(canvas.transform))
            {
                return false;
            }
            EnsureUiButtonSounds(canvas.transform);
            return true;
        }

        private static bool TryBindExistingDebugSceneForRuntime()
        {
            var canvas = GameObject.Find("Debug Canvas");
            var controller = Object.FindFirstObjectByType<DebugUIController>();
            if (canvas == null || controller == null)
            {
                return false;
            }

            var manager = EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            manager.RefreshDerivedCollectionRecords();
            controller.Bind(manager.CurrentSave);

            var visualController = Object.FindFirstObjectByType<CheeseTamaVisualController>();
            if (visualController != null)
            {
                AlignCheeseTamaRestingPosition(visualController);
                visualController.Bind(manager.CurrentTama);
            }

            EnsureUiButtonSounds(canvas.transform);

            return true;
        }

        private static CheeseTamaSaveData ResolveSceneDisplaySave(GameManager manager)
        {
            if (manager != null && manager.CurrentSave != null)
            {
                return manager.CurrentSave;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return LoadEditorPreviewSave();
            }
#endif

            return null;
        }

#if UNITY_EDITOR
        public static bool SyncEditorScenePreview()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || (scene.name != SceneNames.Milkroom && scene.name != SceneNames.Debug))
            {
                return false;
            }

            var root = GameObject.Find("CheeseTamaRoot");
            var visualController = root != null ? root.GetComponent<CheeseTamaVisualController>() : null;
            if (root == null || visualController == null)
            {
                return false;
            }

            AlignCheeseTamaRestingPosition(visualController);
            EnsureGeneratedCharacterModel(root, visualController);
            var previewSave = LoadEditorPreviewSave();
            if (previewSave == null)
            {
                return true;
            }

            if (scene.name == SceneNames.Milkroom)
            {
                Object.FindFirstObjectByType<MilkroomUIController>()?.Bind(previewSave);
            }
            else
            {
                Object.FindFirstObjectByType<DebugUIController>()?.Bind(previewSave);
            }

            var themeId = string.IsNullOrWhiteSpace(previewSave.milkroomThemeId)
                ? MilkroomThemeController.MorningThemeId
                : previewSave.milkroomThemeId;
            Object.FindFirstObjectByType<MilkroomThemeController>()?.ApplyTheme(themeId);
            Object.FindFirstObjectByType<MilkroomLightingController>()?.ApplyTheme(themeId);
            Object.FindFirstObjectByType<MilkroomAmbientEventController>()?.SetTheme(themeId);
            return true;
        }

        private static CheeseTamaSaveData LoadEditorPreviewSave()
        {
            var savePath = System.IO.Path.Combine(Application.persistentDataPath, "cheesetama_save.json");
            if (!System.IO.File.Exists(savePath))
            {
                return null;
            }

            try
            {
                var json = System.IO.File.ReadAllText(savePath);
                var save = JsonUtility.FromJson<CheeseTamaSaveData>(json);
                save?.EnsureRuntimeDefaults();
                return save;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Could not load the editor CheeseTama preview save: {exception.Message}");
                return null;
            }
        }
#endif

        private static void RefreshExistingGameSettingsPanels()
        {
            var settingsPanels = Object.FindObjectsByType<GameSettingsPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var settingsPanel in settingsPanels)
            {
                settingsPanel.RefreshFromSave(true);
            }
        }

        private static void RemoveLegacyMilkroomPanelObjects(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            RemoveLegacyChildIfExists(canvasTransform.Find("Cooking Panel"), "Cooking Recipe Button Background");
            RemoveLegacyChildIfExists(canvasTransform.Find("Snack Panel"), "Snack Inventory Scroll Background");
        }

        private static void RemoveLegacyChildIfExists(Transform parent, string childName)
        {
            var child = parent != null ? parent.Find(childName) : null;
            if (child == null)
            {
                return;
            }

            child.gameObject.SetActive(false);
            DestroyObjectSafely(child.gameObject);
        }

        public static void BuildBootScene()
        {
            EnsureCoreSystems();
            EnsureCamera("Boot Camera");
            EnsureEventSystem();
            EnsureCanvas("Boot Canvas");
            EnsureTitle("Boot Canvas", "CheeseTama", "핵심 시스템을 준비하는 중");

            var canvas = EnsureCanvas("Boot Canvas");
            var startButton = GetOrCreateButton(canvas.transform, "Start Button", "시작", new Vector2(0, 120));
            ConfigureNavigationButton(startButton, SceneNames.Milkroom, false);
        }

        public static void BuildMilkroomScene()
        {
            var manager = EnsureCoreSystems();
            RemoveRootObjectIfExists("Debug Canvas");
            EnsureCamera("Milkroom Camera");
            EnsureLight();
            EnsureMilkroomBackground();
            ApplySavedMilkroomTheme(manager);
            EnsureEventSystem();
            var visualController = EnsureCheeseTamaPlaceholder();

            var canvas = EnsureCanvas("Milkroom Canvas");
            var controller = Object.FindFirstObjectByType<MilkroomUIController>();
            if (controller == null)
            {
                controller = canvas.gameObject.AddComponent<MilkroomUIController>();
            }

            RemoveMilkroomPrototypeButtons(canvas.transform);

            var topBar = GetOrCreatePanel(canvas.transform, "Top Status Bar", new Vector2(24, -16), new Vector2(1348, 82));
            if (topBar.TryGetComponent(out Image topBarImage))
            {
                topBarImage.color = new Color(1f, 0.96f, 0.82f, 0.94f);
            }

            var topBarTransform = topBar.transform;
            var nameText = GetOrCreateText(topBarTransform, "Name Text", "CheeseTama", 28, TextAnchor.MiddleLeft, new Vector2(24, -17), new Vector2(280, 48));
            var levelText = GetOrCreateText(topBarTransform, "Level Text", "레벨 1 (0%)", 22, TextAnchor.MiddleLeft, new Vector2(330, -17), new Vector2(190, 48));
            var sessionText = GetOrCreateText(topBarTransform, "Session Text", "밀크룸에 머문 시간 00:00\n오늘 총 플레이 시간 00:00", 15, TextAnchor.MiddleLeft, new Vector2(548, -10), new Vector2(300, 64));
            RemoveChildIfExists(topBarTransform, "Economy Text");
            var coinEconomyText = GetOrCreateText(topBarTransform, "Coin Economy Text", "코인 0", 17, TextAnchor.MiddleLeft, new Vector2(0, -17), new Vector2(112, 48));
            var milkDropEconomyText = GetOrCreateText(topBarTransform, "Milk Drop Economy Text", "우유방울 0", 17, TextAnchor.MiddleLeft, new Vector2(0, -17), new Vector2(152, 48));
            var collectionFragmentEconomyText = GetOrCreateText(topBarTransform, "Collection Fragment Economy Text", "도감조각 0", 17, TextAnchor.MiddleLeft, new Vector2(0, -17), new Vector2(136, 48));
            ApplyTopInfoTextStyle(nameText, 28);
            ApplyTopInfoTextStyle(levelText, 22);
            ApplyTopSessionTextStyle(sessionText);
            ApplyTopInfoTextStyle(coinEconomyText, 17);
            ApplyTopInfoTextStyle(milkDropEconomyText, 17);
            ApplyTopInfoTextStyle(collectionFragmentEconomyText, 17);
            ConfigureTopBarResourceIcons(topBarTransform);
            RemoveChildIfExists(topBarTransform, "Top Collection Button");
            RemoveChildIfExists(topBarTransform, "Top Decorate Button");
            RemoveChildIfExists(topBarTransform, "Settings Button");

            var topMenu = GetOrCreatePanel(canvas.transform, "Top Menu", new Vector2(1390, -16), new Vector2(486, 82));
            if (topMenu.TryGetComponent(out Image topMenuImage))
            {
                topMenuImage.color = new Color(1f, 0.95f, 0.78f, 0.28f);
            }

            var topMenuTransform = topMenu.transform;
            var topCollectionButton = GetOrCreateTopLeftButton(topMenuTransform, "Top Collection Button", "도감", new Vector2(16, -14), new Vector2(142, 54));
            var topDecorateButton = GetOrCreateTopLeftButton(topMenuTransform, "Top Decorate Button", "꾸미기", new Vector2(172, -14), new Vector2(142, 54));
            var settingsButton = GetOrCreateTopLeftButton(topMenuTransform, "Settings Button", "설정", new Vector2(328, -14), new Vector2(142, 54));

            SetButtonLabel(topCollectionButton, "도감");
            SetButtonLabel(topDecorateButton, "꾸미기");
            SetButtonLabel(settingsButton, "설정");
            ApplyTopMenuButtonStyle(topCollectionButton);
            ApplyTopMenuButtonStyle(topDecorateButton);
            ApplyTopMenuButtonStyle(settingsButton);
            ApplyMilkroomTopHudLayout(
                topBar,
                topMenu,
                nameText,
                levelText,
                sessionText,
                coinEconomyText,
                milkDropEconomyText,
                collectionFragmentEconomyText,
                topCollectionButton,
                topDecorateButton,
                settingsButton);
            SetButtonIcon(topCollectionButton, "collection");
            SetButtonIcon(topDecorateButton, "decorate");
            SetButtonIcon(settingsButton, "settings");

            var panel = GetOrCreateRightPanel(canvas.transform, "Status Panel", new Vector2(-24, -116), new Vector2(360, 524));
            if (panel.TryGetComponent(out Image panelImage))
            {
                panelImage.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            }

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
            RemoveChildIfExists(panelTransform, "Care Tip Text");
            RemoveChildIfExists(panelTransform, "Record Milk Section");
            RemoveChildIfExists(panelTransform, "Basic Milk Growth Text");
            RemoveChildIfExists(panelTransform, "Star Milk Growth Text");
            RemoveChildIfExists(panelTransform, "Unlock Text");
            RemoveChildIfExists(panelTransform, "Record Routine Section");
            RemoveChildIfExists(panelTransform, "Record Save Section");
            RemoveChildIfExists(panelTransform, "Form Text");
            RemoveChildIfExists(panelTransform, "Condition Text");
            RemoveChildIfExists(panelTransform, "Affection Text");
            RemoveChildIfExists(panelTransform, "Maturation Text");
            RemoveChildIfExists(panelTransform, "Hatch Progress Text");
            RemoveChildIfExists(panelTransform, "Care Summary Text");
            RemoveChildIfExists(panelTransform, "Daily Routine Text");
            RemoveChildIfExists(panelTransform, "Last Saved Text");

            var recordPrimaryColor = new Color(1f, 0.9f, 0.62f, 0.66f);
            var recordSecondaryColor = new Color(0.92f, 0.84f, 0.66f, 0.64f);
            var identitySection = GetOrCreatePanel(panelTransform, "Record Identity Section", new Vector2(12, -58), new Vector2(336, 88));
            ApplyRecordSectionStyle(identitySection, recordPrimaryColor);
            var growthSection = GetOrCreatePanel(panelTransform, "Record Growth Section", new Vector2(12, -158), new Vector2(336, 126));
            ApplyRecordSectionStyle(growthSection, recordSecondaryColor);
            var careSummarySection = GetOrCreatePanel(panelTransform, "Record Care Summary Section", new Vector2(12, -296), new Vector2(336, 80));
            ApplyRecordSectionStyle(careSummarySection, recordPrimaryColor);
            var dailyRoutineSection = GetOrCreatePanel(panelTransform, "Record Daily Routine Section", new Vector2(12, -388), new Vector2(336, 124));
            ApplyRecordSectionStyle(dailyRoutineSection, recordSecondaryColor);

            var detailTitleText = GetOrCreateText(panelTransform, "Detail Title Text", "밀크룸 기록", 22, TextAnchor.UpperLeft, new Vector2(22, -20), new Vector2(316, 34));
            detailTitleText.fontStyle = FontStyle.Bold;
            var formText = GetOrCreateText(identitySection.transform, "Form Text", "<b>형태</b>  알", 17, TextAnchor.MiddleLeft, new Vector2(10, -10), new Vector2(316, 30));
            var conditionText = GetOrCreateText(identitySection.transform, "Condition Text", "<b>상태</b>  따뜻함", 17, TextAnchor.MiddleLeft, new Vector2(10, -48), new Vector2(316, 30));
            var affectionText = GetOrCreateText(growthSection.transform, "Affection Text", "<b>애정</b>  10", 17, TextAnchor.MiddleLeft, new Vector2(10, -10), new Vector2(316, 30));
            var maturationText = GetOrCreateText(growthSection.transform, "Maturation Text", "<b>성숙도</b>  0", 17, TextAnchor.MiddleLeft, new Vector2(10, -48), new Vector2(316, 30));
            var hatchProgressText = GetOrCreateText(growthSection.transform, "Hatch Progress Text", "<b>부화 진행</b>  0%", 17, TextAnchor.MiddleLeft, new Vector2(10, -86), new Vector2(316, 30));
            var careSummaryText = GetOrCreateText(careSummarySection.transform, "Care Summary Text", "<b>돌봄 누적</b>  0회" + RecordDetailVerticalGap + "놀이 0  청소 0  휴식 0", 16, TextAnchor.MiddleLeft, new Vector2(10, -13), new Vector2(316, 54));
            var dailyRoutineText = GetOrCreateText(dailyRoutineSection.transform, "Daily Routine Text", "<b>오늘 루틴</b>" + RecordDetailVerticalGap + "먹기 0/3  요리 0/2\n놀이 0/3  청소 0/2  휴식 0/2\n<size=14>완료 보상  코인 20 · 우유방울 5 · 도감조각 1</size>", 16, TextAnchor.MiddleLeft, new Vector2(10, -13), new Vector2(316, 98));
            ApplyRecordLineStyle(formText);
            ApplyRecordLineStyle(conditionText);
            ApplyRecordLineStyle(affectionText);
            ApplyRecordLineStyle(maturationText);
            ApplyRecordLineStyle(hatchProgressText);
            ApplyRecordLineStyle(careSummaryText);
            ApplyRecordLineStyle(dailyRoutineText);

            var statBar = GetOrCreatePanel(canvas.transform, "Stat Bar", new Vector2(24, -116), new Vector2(350, 396));
            if (statBar.TryGetComponent(out Image statBarImage))
            {
                statBarImage.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            }

            var statBarTransform = statBar.transform;
            var statTitleText = GetOrCreateText(statBarTransform, "Stat Title Text", "상태 수치", 22, TextAnchor.UpperLeft, new Vector2(22, -27), new Vector2(306, 34));
            statTitleText.fontStyle = FontStyle.Bold;
            var hungerText = GetOrCreateText(statBarTransform, "Hunger Text", "포만감  80/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -72), new Vector2(306, 30));
            var moodText = GetOrCreateText(statBarTransform, "Mood Text", "기분  70/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -132), new Vector2(306, 30));
            var cleanlinessText = GetOrCreateText(statBarTransform, "Cleanliness Text", "청결  90/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -192), new Vector2(306, 30));
            var sleepinessText = GetOrCreateText(statBarTransform, "Sleepiness Text", "졸림  20/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -252), new Vector2(306, 30));
            var healthText = GetOrCreateText(statBarTransform, "Health Text", "건강  100/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -312), new Vector2(306, 30));

            var careTipPanel = GetOrCreatePanel(canvas.transform, "Care Tip Panel", new Vector2(24, -532), new Vector2(350, 104));
            if (careTipPanel.TryGetComponent(out Image careTipPanelImage))
            {
                careTipPanelImage.color = new Color(1f, 0.96f, 0.8f, 0.92f);
            }

            var careTipTitleText = GetOrCreateText(careTipPanel.transform, "Care Tip Title Text", "돌봄 팁", 22, TextAnchor.UpperLeft, new Vector2(22, -16), new Vector2(306, 30));
            careTipTitleText.fontStyle = FontStyle.Bold;
            var careTipText = GetOrCreateText(careTipPanel.transform, "Care Tip Text", "우유를 먹여 성장시켜 주세요.", 20, TextAnchor.MiddleLeft, new Vector2(22, -58), new Vector2(306, 38));
            careTipText.color = new Color(0.28f, 0.18f, 0.08f);
            careTipText.resizeTextForBestFit = true;
            careTipText.resizeTextMinSize = 14;
            careTipText.resizeTextMaxSize = 20;
            careTipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            careTipText.verticalOverflow = VerticalWrapMode.Truncate;

            var messageBar = GetOrCreateBottomPanel(canvas.transform, "Message Bar", new Vector2(0, 146), new Vector2(980, 72));
            if (messageBar.TryGetComponent(out Image messageBarImage))
            {
                messageBarImage.color = new Color(1f, 0.93f, 0.68f, 0.98f);
            }

            var messageText = GetOrCreateText(messageBar.transform, "Message Text", "돌봄 준비 완료.", 24, TextAnchor.MiddleLeft, new Vector2(24, -14), new Vector2(932, 44));
            messageText.fontStyle = FontStyle.Bold;
            messageText.color = new Color(0.28f, 0.18f, 0.08f);

            // 도감 이벤트 메시지 바 — 상태메시지 바(Message Bar) 바로 위에 배치.
            var eventMessageBar = GetOrCreateBottomPanel(canvas.transform, "Event Message Bar", new Vector2(0, 226), new Vector2(980, 64));
            if (eventMessageBar.TryGetComponent(out Image eventMessageBarImage))
            {
                eventMessageBarImage.color = new Color(0.86f, 0.92f, 1f, 0.98f);
            }

            var eventMessageText = GetOrCreateText(eventMessageBar.transform, "Event Message Text", "이벤트 대기 중.", 20, TextAnchor.MiddleLeft, new Vector2(24, -12), new Vector2(932, 40));
            eventMessageText.fontStyle = FontStyle.Bold;
            eventMessageText.color = new Color(0.16f, 0.24f, 0.42f);
            eventMessageBar.SetActive(false);

            var milkPanelController = BuildMilkPanel(
                canvas.transform,
                controller,
                visualController,
                out var basicMilkGrowthText,
                out var starMilkGrowthText,
                out var unlockText);

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
                null,
                null,
                null,
                careSummaryText,
                dailyRoutineText,
                sessionText,
                null,
                careTipText,
                null,
                messageText,
                eventMessageText,
                coinEconomyText,
                milkDropEconomyText,
                collectionFragmentEconomyText);
            manager.RefreshDerivedCollectionRecords();
            controller.Bind(ResolveSceneDisplaySave(manager));
            controller.ShowMessage("돌봄 준비 완료.");
            if (Application.isPlaying)
            {
                visualController.Bind(manager.CurrentTama);
            }
            var cookingPanelController = BuildCookingPanel(canvas.transform, controller, visualController);
            var snackPanelController = BuildSnackPanel(canvas.transform, controller, visualController);

            var actionBar = GetOrCreateBottomPanel(canvas.transform, "Bottom Action Bar", new Vector2(0, 24), new Vector2(1240, 108));
            if (actionBar.TryGetComponent(out Image actionBarImage))
            {
                actionBarImage.color = new Color(1f, 1f, 1f, 0f);
                actionBarImage.raycastTarget = false;
            }

            var actionBarTransform = actionBar.transform;
            RemoveChildIfExists(actionBarTransform, "Collection Button");

            var milkButton = GetOrCreateButton(actionBarTransform, "Milk Button", "우유주기", new Vector2(-460, 25), new Vector2(156, 58));
            ConfigureCareButton(milkButton, MilkroomCareAction.OpenMilkPanel, controller, visualController, milkPanelController);

            var blendButton = GetOrCreateButton(actionBarTransform, "Blend Button", "요리하기", new Vector2(-276, 25), new Vector2(156, 58));
            ConfigureCareButton(blendButton, MilkroomCareAction.Blend, controller, visualController, cookingPanelController);

            var snackButton = GetOrCreateButton(actionBarTransform, "Snack Button", "간식가방", new Vector2(-92, 25), new Vector2(156, 58));
            ConfigureCareButton(snackButton, MilkroomCareAction.OpenSnackPanel, controller, visualController, snackPanelController);

            var playButton = GetOrCreateButton(actionBarTransform, "Play Button", "놀아주기", new Vector2(92, 25), new Vector2(156, 58));
            ConfigureCareButton(playButton, MilkroomCareAction.Play, controller, visualController);

            var cleanButton = GetOrCreateButton(actionBarTransform, "Clean Button", "청소하기", new Vector2(276, 25), new Vector2(156, 58));
            ConfigureCareButton(cleanButton, MilkroomCareAction.Clean, controller, visualController);

            var sleepButton = GetOrCreateButton(actionBarTransform, "Sleep Button", "휴식하기", new Vector2(460, 25), new Vector2(156, 58));
            ConfigureCareButton(sleepButton, MilkroomCareAction.SleepSchedule, controller, visualController);

            SetButtonLabel(milkButton, "우유주기");
            SetButtonLabel(blendButton, "요리하기");
            SetButtonLabel(snackButton, "간식가방");
            SetButtonLabel(playButton, "놀아주기");
            SetButtonLabel(cleanButton, "청소하기");
            SetButtonLabel(sleepButton, "휴식하기");
            ApplyCareButtonStyle(milkButton);
            ApplyCareButtonStyle(blendButton);
            ApplyCareButtonStyle(snackButton);
            ApplyCareButtonStyle(playButton);
            ApplyCareButtonStyle(cleanButton);
            ApplyCareButtonStyle(sleepButton);
            SetButtonIcon(milkButton, "milk");
            SetButtonIcon(blendButton, "cook");
            SetButtonIcon(snackButton, "snack");
            SetButtonIcon(playButton, "play");
            SetButtonIcon(cleanButton, "clean");
            SetButtonIcon(sleepButton, "rest");

            var actionBarController = actionBar.GetComponent<BottomActionBarController>();
            if (actionBarController == null)
            {
                actionBarController = actionBar.AddComponent<BottomActionBarController>();
            }

            actionBarController.Configure(milkButton, blendButton, snackButton, playButton, cleanButton, sleepButton);
            RemoveChildIfExists(canvas.transform, "Collection Overlay");
            var decorateOverlay = BuildDecorateOverlay(canvas.transform, out var decorateCloseButton);
            BuildMilkroomSettings(canvas.transform, settingsButton, controller, visualController, out var settingsLastSavedText);
            controller.SetLastSavedText(settingsLastSavedText);
            var settingsModal = canvas.transform.Find("Settings Modal")?.gameObject;
            var settingsCloseButton = settingsModal != null
                ? settingsModal.transform.Find("Close Settings Button")?.GetComponent<Button>()
                : null;
            ConfigureTopMenu(
                canvas.transform,
                topCollectionButton,
                topDecorateButton,
                settingsButton,
                null,
                decorateCloseButton,
                settingsCloseButton,
                null,
                decorateOverlay,
                settingsModal,
                null);
            EnsureCheeseTamaProfileMenuShell(canvas.transform);
            EnsureMilkroomStatGauges(canvas.transform, controller);
            EnsureCheeseTamaNameDialog(canvas.transform, controller);
            EnsureFirstMeetingOnboarding(canvas.transform, controller, visualController);
            EnsureNewGameSetup(canvas.transform, controller, visualController);
            EnsureMilkBlendingPanel(canvas.transform, controller, visualController);
            EnsureCookingChoicePanel(canvas.transform);
            EnsureSaveRecoveryNotice(canvas.transform);
            EnsureReturnSummary(canvas.transform);
            EnsureGrowthMilestone(canvas.transform, controller, visualController);
            EnsureEvolutionMilestone(canvas.transform, controller, visualController);
            EnsureGrowthJourney(canvas.transform);
            EnsureMilkDropMiniGame(canvas.transform, controller, visualController);
            EnsureBouncyJumpMiniGame(canvas.transform, controller, visualController);
            EnsurePlayChoicePanel(canvas.transform);
            EnsureCleaningMiniGame(canvas.transform, controller, visualController);
            EnsureCareEventCard(canvas.transform, visualController);
            EnsureNpcVisitCard(canvas.transform);
            EnsureSleepSchedulePanel(canvas.transform, controller, visualController);
            EnsureDecorationShop(canvas.transform);
            EnsureDecorationRoomPresenter();
            EnsureMilkroomAtmosphere(canvas.transform, manager.CurrentTama);
            EnsureCheeseTamaPetInteraction(canvas.transform, controller, visualController);
            EnsureCheeseTamaSpeechBubble(canvas.transform, visualController);
            EnsureAutonomousLife(canvas.transform, visualController);
            RemoveNormalEvolutionVisualAccents(visualController);
            EnsureLateGameFeatures(canvas.transform, controller, visualController);
            EnsureCheeseStarDelivery(canvas.transform);
            EnsureMemoryJournal(canvas.transform);
            EnsureFantasyPowderHiddenRecipes(canvas.transform);
            EnsureFirstDayJourney(canvas.transform);
            EnsureCheeseTamaProfileMenu(canvas.transform);
            EnsureInputBindingsPanel(canvas.transform);
            EnsureUiButtonSounds(canvas.transform);
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

            EnsureTitle("Collection Canvas", "도감", "발견한 기록을 한눈에 확인하세요");

            var panel = GetOrCreatePanel(canvas.transform, "Collection Records Panel", new Vector2(260, -178), new Vector2(1400, 720));
            if (panel.TryGetComponent(out Image panelImage))
            {
                panelImage.color = new Color(1f, 0.98f, 0.9f, 0.96f);
            }

            var panelTransform = panel.transform;
            RemoveChildIfExists(panelTransform, "Milk Records Text");
            RemoveChildIfExists(panelTransform, "Evolution Records Text");
            RemoveChildIfExists(panelTransform, "Event Records Text");
            RemoveChildIfExists(panelTransform, "Hidden Records Text");

            var milkTabButton = GetOrCreateTopLeftButton(panelTransform, "Milk Records Tab Button", "우유", new Vector2(24, -24), new Vector2(326, 52));
            var evolutionTabButton = GetOrCreateTopLeftButton(panelTransform, "Evolution Records Tab Button", "진화", new Vector2(358, -24), new Vector2(326, 52));
            var eventTabButton = GetOrCreateTopLeftButton(panelTransform, "Event Records Tab Button", "이벤트", new Vector2(692, -24), new Vector2(326, 52));
            var hiddenTabButton = GetOrCreateTopLeftButton(panelTransform, "Hidden Records Tab Button", "특별", new Vector2(1026, -24), new Vector2(326, 52));
            ApplyCollectionTabButtonStyle(milkTabButton, evolutionTabButton, eventTabButton, hiddenTabButton);

            var recordHeaderText = GetOrCreateText(panelTransform, "Collection Records Header Text", "<b>우유 기록</b>  <size=15>0개 발견</size>", 20, TextAnchor.MiddleLeft, new Vector2(30, -88), new Vector2(1300, 34));
            recordHeaderText.supportRichText = true;
            recordHeaderText.color = new Color(0.25f, 0.17f, 0.09f);

            var scrollContent = GetOrCreateCollectionScrollContent(panelTransform, new Vector2(24, -134), new Vector2(1352, 482));
            RemoveChildIfExists(scrollContent, "Milk Records Card Root");
            RemoveChildIfExists(scrollContent, "Evolution Records Card Root");
            RemoveChildIfExists(scrollContent, "Event Records Card Root");
            RemoveChildIfExists(scrollContent, "Hidden Records Card Root");
            var milkText = GetOrCreateCollectionRecordText(scrollContent, "Milk Records Text", "우유 기록: 0", 18);
            var evolutionText = GetOrCreateCollectionRecordText(scrollContent, "Evolution Records Text", "진화 기록: 0", 18);
            var eventText = GetOrCreateCollectionRecordText(scrollContent, "Event Records Text", "이벤트 기록: 0", 18);
            var hiddenText = GetOrCreateCollectionRecordText(scrollContent, "Hidden Records Text", "특별 기록: 0", 18);
            var messageText = GetOrCreateText(panelTransform, "Collection Message Text", "발견한 기록만 표시됩니다. 밀크룸에서 돌봄을 이어가면 새 기록이 추가됩니다.", 15, TextAnchor.MiddleLeft, new Vector2(30, -650), new Vector2(980, 42));
            messageText.color = new Color(0.38f, 0.28f, 0.17f);

            controller.Configure(
                milkText,
                evolutionText,
                eventText,
                hiddenText,
                messageText,
                recordHeaderText,
                milkTabButton,
                evolutionTabButton,
                eventTabButton,
                hiddenTabButton);
            controller.Bind(manager.CurrentSave);

            var backButton = GetOrCreateButton(canvas.transform, "Milkroom Button", "밀크룸", new Vector2(0, 36), new Vector2(164, 50));
            ConfigureNavigationButton(backButton, SceneNames.Milkroom, false);
        }

        public static void BuildDebugScene()
        {
            var manager = EnsureCoreSystems();
            manager.RefreshDerivedCollectionRecords();
            EnsureCamera("Debug Camera");
            EnsureLight();
            EnsureMilkroomBackground();
            EnsureEventSystem();
            var visualController = EnsureCheeseTamaPlaceholder();
            var canvas = EnsureCanvas("Debug Canvas");

            var controller = Object.FindFirstObjectByType<DebugUIController>();
            if (controller == null)
            {
                controller = canvas.gameObject.AddComponent<DebugUIController>();
            }

            EnsureTitle("Debug Canvas", "개발자", "테스트용 화면");

            var panel = GetOrCreatePanel(canvas.transform, "Debug State Panel", new Vector2(24, -180), new Vector2(500, 600));
            var panelTransform = panel.transform;
            var stateText = GetOrCreateText(panelTransform, "Debug State Text", "개발자 상태", 16, TextAnchor.UpperLeft, new Vector2(16, -16), new Vector2(460, 430));
            var messageText = GetOrCreateText(panelTransform, "Debug Message Text", "프리셋을 선택하세요.", 14, TextAnchor.UpperLeft, new Vector2(16, -480), new Vector2(460, 80));

            controller.Configure(stateText, messageText);
            controller.Bind(ResolveSceneDisplaySave(manager));
            controller.ShowMessage("프리셋을 선택해 수치와 CheeseTama 표정을 확인하세요.");
            if (Application.isPlaying)
            {
                visualController.Bind(manager.CurrentTama);
            }

            var hungryButton = GetOrCreateButton(canvas.transform, "Hungry Preset Button", "배고픔", new Vector2(-490, 96));
            ConfigureDebugButton(hungryButton, DebugAction.SetHungry, controller, visualController);

            var sleepyButton = GetOrCreateButton(canvas.transform, "Sleepy Preset Button", "졸림", new Vector2(-350, 96));
            ConfigureDebugButton(sleepyButton, DebugAction.SetSleepy, controller, visualController);

            var messyButton = GetOrCreateButton(canvas.transform, "Messy Preset Button", "지저분함", new Vector2(-210, 96));
            ConfigureDebugButton(messyButton, DebugAction.SetMessy, controller, visualController);

            var unwellButton = GetOrCreateButton(canvas.transform, "Unwell Preset Button", "아픔", new Vector2(-70, 96));
            ConfigureDebugButton(unwellButton, DebugAction.SetUnwell, controller, visualController);

            var cheerfulButton = GetOrCreateButton(canvas.transform, "Cheerful Preset Button", "신남", new Vector2(70, 96));
            ConfigureDebugButton(cheerfulButton, DebugAction.SetCheerful, controller, visualController);

            var hatchButton = GetOrCreateButton(canvas.transform, "Hatch Preset Button", "부화", new Vector2(210, 96));
            ConfigureDebugButton(hatchButton, DebugAction.HatchNow, controller, visualController);

            var levelOneButton = GetOrCreateButton(canvas.transform, "Add Level One Button", "레벨 +1", new Vector2(-490, 36));
            ConfigureDebugButton(levelOneButton, DebugAction.AddLevelOne, controller, visualController);

            var levelTwoButton = GetOrCreateButton(canvas.transform, "Add Level Two Button", "레벨 +2", new Vector2(-350, 36));
            ConfigureDebugButton(levelTwoButton, DebugAction.AddLevelTwo, controller, visualController);

            var levelFiveButton = GetOrCreateButton(canvas.transform, "Add Level Five Button", "레벨 +5", new Vector2(-210, 36));
            ConfigureDebugButton(levelFiveButton, DebugAction.AddLevelFive, controller, visualController);

            var unlockStarButton = GetOrCreateButton(canvas.transform, "Unlock Star Preset Button", "별빛 해금", new Vector2(-70, 36));
            ConfigureDebugButton(unlockStarButton, DebugAction.UnlockStarMilk, controller, visualController);

            var resetButton = GetOrCreateButton(canvas.transform, "Debug Reset Button", "초기화", new Vector2(70, 36));
            ConfigureDebugButton(resetButton, DebugAction.ResetSave, controller, visualController);

            var forceEventButton = GetOrCreateButton(canvas.transform, "Force Event Button", "이벤트 발생", new Vector2(210, 36));
            ConfigureDebugButton(forceEventButton, DebugAction.ForceEvent, controller, visualController);

            var stayButton = GetOrCreateButton(canvas.transform, "Stay Five Minutes Button", "5분 체류", new Vector2(350, 96));
            ConfigureDebugButton(stayButton, DebugAction.AddSessionFiveMinutes, controller, visualController);

            var milkroomButton = GetOrCreateButton(canvas.transform, "Milkroom Button", "밀크룸", new Vector2(350, 36));
            ConfigureNavigationButton(milkroomButton, SceneNames.Milkroom, true);
        }

        private static GameObject BuildCollectionOverlay(
            Transform canvasTransform,
            CheeseTamaSaveData saveData,
            out Button closeButton,
            out CollectionUIController collectionController)
        {
            var overlay = GetOrCreatePanel(canvasTransform, "Collection Overlay", new Vector2(1136, -116), new Vector2(740, 620));
            if (overlay.TryGetComponent(out Image overlayImage))
            {
                overlayImage.color = new Color(1f, 0.98f, 0.9f, 0.98f);
            }

            var overlayTransform = overlay.transform;
            GetOrCreateText(overlayTransform, "Collection Overlay Title Text", "도감", 24, TextAnchor.UpperLeft, new Vector2(28, -24), new Vector2(300, 36));
            GetOrCreateText(overlayTransform, "Collection Overlay Help Text", "발견한 기록만 표시됩니다.", 14, TextAnchor.UpperLeft, new Vector2(28, -64), new Vector2(380, 24));
            closeButton = GetOrCreateTopLeftButton(overlayTransform, "Close Collection Button", "닫기", new Vector2(596, -20), new Vector2(116, 40));

            var recordsPanel = GetOrCreatePanel(overlayTransform, "Collection Overlay Records Panel", new Vector2(24, -104), new Vector2(692, 486));
            if (recordsPanel.TryGetComponent(out Image recordsImage))
            {
                recordsImage.color = new Color(1f, 0.94f, 0.78f, 0.42f);
            }

            var recordsTransform = recordsPanel.transform;
            var milkTabButton = GetOrCreateTopLeftButton(recordsTransform, "Milk Records Tab Button", "우유", new Vector2(18, -18), new Vector2(154, 38));
            var evolutionTabButton = GetOrCreateTopLeftButton(recordsTransform, "Evolution Records Tab Button", "진화", new Vector2(182, -18), new Vector2(154, 38));
            var eventTabButton = GetOrCreateTopLeftButton(recordsTransform, "Event Records Tab Button", "이벤트", new Vector2(346, -18), new Vector2(154, 38));
            var hiddenTabButton = GetOrCreateTopLeftButton(recordsTransform, "Hidden Records Tab Button", "특별", new Vector2(510, -18), new Vector2(154, 38));
            ApplyCollectionTabButtonStyle(milkTabButton, evolutionTabButton, eventTabButton, hiddenTabButton);

            var milkText = GetOrCreateText(recordsTransform, "Milk Records Text", "우유 기록: 0", 16, TextAnchor.UpperLeft, new Vector2(18, -72), new Vector2(646, 350));
            var evolutionText = GetOrCreateText(recordsTransform, "Evolution Records Text", "진화 기록: 0", 16, TextAnchor.UpperLeft, new Vector2(18, -72), new Vector2(646, 350));
            var eventText = GetOrCreateText(recordsTransform, "Event Records Text", "이벤트 기록: 0", 16, TextAnchor.UpperLeft, new Vector2(18, -72), new Vector2(646, 350));
            var hiddenText = GetOrCreateText(recordsTransform, "Hidden Records Text", "특별 기록: 0", 16, TextAnchor.UpperLeft, new Vector2(18, -72), new Vector2(646, 350));
            var messageText = GetOrCreateText(recordsTransform, "Collection Message Text", "우유를 먹이고 부화시키면 이곳에 기록이 추가됩니다.", 14, TextAnchor.UpperLeft, new Vector2(18, -444), new Vector2(646, 28));

            collectionController = overlay.GetComponent<CollectionUIController>();
            if (collectionController == null)
            {
                collectionController = overlay.AddComponent<CollectionUIController>();
            }

            collectionController.Configure(
                milkText,
                evolutionText,
                eventText,
                hiddenText,
                messageText,
                milkTabButton,
                evolutionTabButton,
                eventTabButton,
                hiddenTabButton);
            collectionController.Bind(saveData);
            overlay.SetActive(false);
            return overlay;
        }

        private static GameObject BuildDecorateOverlay(Transform canvasTransform, out Button closeButton)
        {
            var overlay = GetOrCreateRightPanel(canvasTransform, "Decorate Overlay", new Vector2(-44, -116), new Vector2(740, 620));
            if (overlay.TryGetComponent(out Image overlayImage))
            {
                overlayImage.color = new Color(1f, 0.98f, 0.9f, 0.98f);
            }

            var overlayTransform = overlay.transform;
            GetOrCreateText(overlayTransform, "Decorate Overlay Title Text", "꾸미기", 24, TextAnchor.UpperLeft, new Vector2(28, -24), new Vector2(300, 36));
            var stateText = GetOrCreateText(overlayTransform, "Decorate Overlay State Text", "현재 테마: 따뜻한 아침 밀크룸", 15, TextAnchor.UpperLeft, new Vector2(28, -72), new Vector2(420, 28));
            closeButton = GetOrCreateTopLeftButton(overlayTransform, "Close Decorate Button", "닫기", new Vector2(596, -20), new Vector2(116, 40));

            var previewPanel = GetOrCreatePanel(overlayTransform, "Decorate Preview Panel", new Vector2(24, -122), new Vector2(692, 432));
            if (previewPanel.TryGetComponent(out Image previewImage))
            {
                previewImage.color = new Color(1f, 0.94f, 0.78f, 0.42f);
            }

            var previewTransform = previewPanel.transform;
            var themeText = GetOrCreateText(previewTransform, "Decorate Theme Text", "따뜻한 아침 밀크룸", 18, TextAnchor.UpperLeft, new Vector2(22, -22), new Vector2(420, 32));
            var detailText = GetOrCreateText(previewTransform, "Decorate Theme Detail Text", "크림색 벽 / 정돈된 바닥 / 냉장고 / 원목 의자 / 포근한 아침빛", 14, TextAnchor.UpperLeft, new Vector2(22, -64), new Vector2(620, 32));
            GetOrCreateText(previewTransform, "Decorate Slot A Text", "조명", 16, TextAnchor.UpperLeft, new Vector2(22, -132), new Vector2(120, 26));
            var lightingText = GetOrCreateText(previewTransform, "Decorate Slot A Value Text", "따뜻한 햇살 + 부드러운 림라이트", 14, TextAnchor.UpperLeft, new Vector2(142, -132), new Vector2(460, 26));
            GetOrCreateText(previewTransform, "Decorate Slot B Text", "가구", 16, TextAnchor.UpperLeft, new Vector2(22, -188), new Vector2(120, 26));
            var furnitureText = GetOrCreateText(previewTransform, "Decorate Slot B Value Text", "GLB 소품 재질 유지 / 벽과 바닥 팔레트만 전환", 14, TextAnchor.UpperLeft, new Vector2(142, -188), new Vector2(500, 26));
            GetOrCreateText(previewTransform, "Decorate Slot C Text", "소품", 16, TextAnchor.UpperLeft, new Vector2(22, -244), new Vector2(120, 26));
            var propsText = GetOrCreateText(previewTransform, "Decorate Slot C Value Text", "기본 소품 배치 유지", 14, TextAnchor.UpperLeft, new Vector2(142, -244), new Vector2(500, 26));
            GetOrCreateText(previewTransform, "Decorate Help Text", "테마를 고르면 방 색감, 조명, 창밖 분위기가 즉시 바뀌고 저장됩니다.", 14, TextAnchor.UpperLeft, new Vector2(22, -350), new Vector2(620, 42));

            var morningButton = GetOrCreateTopLeftButton(previewTransform, "Morning Theme Button", "아침", new Vector2(22, -292), new Vector2(148, 42));
            var eveningButton = GetOrCreateTopLeftButton(previewTransform, "Evening Theme Button", "오후", new Vector2(184, -292), new Vector2(148, 42));
            var nightButton = GetOrCreateTopLeftButton(previewTransform, "Night Theme Button", "밤", new Vector2(346, -292), new Vector2(148, 42));
            var rainyButton = GetOrCreateTopLeftButton(previewTransform, "Rainy Theme Button", "비", new Vector2(508, -292), new Vector2(148, 42));
            ApplyCollectionTabButtonStyle(morningButton, eveningButton, nightButton, rainyButton);

            var decorateController = overlay.GetComponent<DecorateThemePanelController>();
            if (decorateController == null)
            {
                decorateController = overlay.AddComponent<DecorateThemePanelController>();
            }

            decorateController.Configure(
                stateText,
                themeText,
                detailText,
                lightingText,
                furnitureText,
                propsText,
                morningButton,
                eveningButton,
                nightButton,
                rainyButton);

            overlay.SetActive(false);
            return overlay;
        }

        private static MilkPanelController BuildMilkPanel(
            Transform canvasTransform,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController,
            out Text basicMilkGrowthText,
            out Text starMilkGrowthText,
            out Text unlockText)
        {
            var panel = GetOrCreatePanel(canvasTransform, "Milk Panel", MilkroomToolPanelPosition, MilkroomToolPanelSize);
            if (panel.TryGetComponent(out Image panelImage))
            {
                panelImage.color = new Color(1f, 0.98f, 0.9f, 0.98f);
            }

            var panelTransform = panel.transform;
            RemoveChildIfExists(panelTransform, "Basic Milk Tab Button");
            RemoveChildIfExists(panelTransform, "Star Milk Tab Button");
            RemoveChildIfExists(panelTransform, "Feed Basic Milk Button");
            RemoveChildIfExists(panelTransform, "Feed Star Milk Button");
            RemoveChildIfExists(panelTransform, "Milk Detail Text");

            GetOrCreateText(panelTransform, "Milk Panel Header Text", "우유", 24, TextAnchor.UpperLeft, new Vector2(28, -24), new Vector2(220, 36));
            GetOrCreateText(panelTransform, "Milk Panel Help Text", "먹일 우유를 고르고 성장 기록을 확인합니다.", 14, TextAnchor.UpperLeft, new Vector2(28, -64), new Vector2(460, 28));

            var closeButton = GetOrCreateTopLeftButton(panelTransform, "Close Milk Panel Button", "닫기", new Vector2(536, -20), new Vector2(116, 40));

            var milks = MilkCatalog.VisibleMilks;
            var tabButtons = new Button[milks.Length];
            var feedButtons = new Button[milks.Length];
            for (var i = 0; i < milks.Length; i++)
            {
                var milk = milks[i];
                var row = i / 4;
                var column = i % 4;
                tabButtons[i] = GetOrCreateTopLeftButton(
                    panelTransform,
                    $"{milk.id} Milk Tab Button",
                    milk.displayName,
                    new Vector2(28 + column * 158, -116 - row * 50),
                    new Vector2(146, 42));
            }

            ApplyCollectionTabButtonStyle(tabButtons);

            var listPanel = GetOrCreatePanel(panelTransform, "Milk Growth List Panel", new Vector2(28, -232), new Vector2(624, 260));
            if (listPanel.TryGetComponent(out Image listPanelImage))
            {
                listPanelImage.color = new Color(1f, 0.94f, 0.78f, 0.46f);
            }

            var listTransform = listPanel.transform;
            RemoveChildIfExists(listTransform, "Milk Detail Label Text");
            RemoveChildIfExists(listTransform, "Milk Detail Text");
            var listTitleText = GetOrCreateText(listTransform, "Milk Growth List Title Text", "선택한 우유", 20, TextAnchor.UpperLeft, new Vector2(22, -16), new Vector2(580, 30));
            listTitleText.fontStyle = FontStyle.Bold;
            basicMilkGrowthText = GetOrCreateText(listTransform, "Basic Milk Growth Text", "<b>희귀도</b>  common", 15, TextAnchor.UpperLeft, new Vector2(22, -54), new Vector2(580, 24));
            starMilkGrowthText = GetOrCreateText(listTransform, "Star Milk Growth Text", "<b>설명</b>\n<size=4> </size>\n우유를 선택하세요.\n\n<b>효과</b>  포만감 +25", 14, TextAnchor.UpperLeft, new Vector2(22, -86), new Vector2(580, 118));
            unlockText = GetOrCreateText(listTransform, "Unlock Text", "<b>해금</b>  완료 · 바로 줄 수 있습니다.", 15, TextAnchor.UpperLeft, new Vector2(22, -216), new Vector2(580, 30));
            ApplyRecordLineStyle(basicMilkGrowthText);
            ApplyRecordLineStyle(starMilkGrowthText);
            ApplyRecordLineStyle(unlockText);
            starMilkGrowthText.lineSpacing = 1.06f;

            var statusText = GetOrCreateText(panelTransform, "Milk Panel Status Text", "기본 우유를 먹일 수 있습니다.", 15, TextAnchor.UpperLeft, new Vector2(28, -508), new Vector2(430, 28));
            statusText.fontStyle = FontStyle.Bold;
            statusText.color = new Color(0.34f, 0.22f, 0.1f);

            for (var i = 0; i < milks.Length; i++)
            {
                var milk = milks[i];
                feedButtons[i] = GetOrCreateTopLeftButton(
                    panelTransform,
                    $"Feed {milk.id} Button",
                    $"{milk.displayName} 주기",
                    new Vector2(500, -504),
                    new Vector2(152, 48));
                ApplyCareButtonStyle(feedButtons[i]);
            }

            var tipText = GetOrCreateText(panelTransform, "Milk Panel Tip Text", "우유 성장은 도감 기록과 해금 조건에 반영됩니다.", 14, TextAnchor.UpperLeft, new Vector2(28, -544), new Vector2(440, 24));
            tipText.color = new Color(0.38f, 0.28f, 0.17f);

            var milkController = canvasTransform.GetComponent<MilkPanelController>();
            if (milkController == null)
            {
                milkController = canvasTransform.gameObject.AddComponent<MilkPanelController>();
            }

            for (var i = 0; i < feedButtons.Length; i++)
            {
                ConfigureCareButton(feedButtons[i], GetMilkCareAction(milks[i].id), controller, visualController, milkController);
            }

            milkController.Configure(
                panel,
                listTitleText,
                null,
                basicMilkGrowthText,
                starMilkGrowthText,
                unlockText,
                statusText,
                tabButtons,
                feedButtons,
                closeButton,
                controller);
            return milkController;
        }

        private static MilkroomCareAction GetMilkCareAction(string milkId)
        {
            return milkId switch
            {
                MilkCatalog.BasicMilkId => MilkroomCareAction.FeedMilk,
                MilkCatalog.WarmMilkId => MilkroomCareAction.FeedWarmMilk,
                MilkCatalog.ColdMilkId => MilkroomCareAction.FeedColdMilk,
                MilkCatalog.NuttyMilkId => MilkroomCareAction.FeedNuttyMilk,
                MilkCatalog.RichMilkId => MilkroomCareAction.FeedRichMilk,
                MilkCatalog.FermentedMilkId => MilkroomCareAction.FeedFermentedMilk,
                MilkCatalog.CoffeeMilkId => MilkroomCareAction.FeedCoffeeMilk,
                MilkCatalog.StarMilkId => MilkroomCareAction.FeedStarMilk,
                _ => MilkroomCareAction.FeedMilk
            };
        }

        private static CookingPanelController BuildCookingPanel(
            Transform canvasTransform,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController)
        {
            var panel = GetOrCreatePanel(canvasTransform, "Cooking Panel", MilkroomToolPanelPosition, MilkroomToolPanelSize);
            if (panel.TryGetComponent(out Image panelImage))
            {
                panelImage.color = new Color(1f, 0.98f, 0.9f, 0.98f);
            }

            var panelTransform = panel.transform;
            GetOrCreateText(panelTransform, "Cooking Panel Header Text", "요리", 24, TextAnchor.UpperLeft, new Vector2(28, -24), new Vector2(220, 36));
            GetOrCreateText(panelTransform, "Cooking Panel Help Text", "재료를 고르고 CheeseTama에게 줄 작은 요리를 만듭니다.", 14, TextAnchor.UpperLeft, new Vector2(28, -64), new Vector2(460, 28));

            var closeButton = GetOrCreateTopLeftButton(panelTransform, "Close Cooking Button", "닫기", new Vector2(536, -20), new Vector2(116, 40));
            RemoveChildIfExists(panelTransform, "Warm Milk Soup Button");
            RemoveChildIfExists(panelTransform, "Soft Snack Dough Button");
            RemoveChildIfExists(panelTransform, "Star Cream Button");
            RemoveChildIfExists(panelTransform, "Cooking Recipe Menu Panel");
            RemoveChildIfExists(panelTransform, "Cooking Recipe Button Background");
            for (var i = 0; i < 12; i++)
            {
                RemoveChildIfExists(panelTransform, $"Cooking Recipe Button {i}");
                RemoveChildIfExists(panelTransform, $"Cooking Recipe Visible Label {i}");
            }
            RemoveChildIfExists(panelTransform, "Cooking Recipe List Text");

            var visibleRecipes = SnackCatalog.VisibleCookingRecipes;
            var recipeButtons = new Button[visibleRecipes.Length];
            for (var i = 0; i < visibleRecipes.Length; i++)
            {
                var row = i / 4;
                var column = i % 4;
                var position = new Vector2(28 + (column * 158), -116 - (row * 50));
                var size = new Vector2(146, 42);
                recipeButtons[i] = GetOrCreateTopLeftButton(
                    panelTransform,
                    $"Cooking Recipe Button {i}",
                    visibleRecipes[i].displayName,
                    position,
                    size);
            }

            ApplyCookingRecipeButtonStyle(recipeButtons);

            var recipePanel = GetOrCreatePanel(panelTransform, "Cooking Recipe Detail Panel", new Vector2(28, -232), new Vector2(624, 260));
            if (recipePanel.TryGetComponent(out Image recipePanelImage))
            {
                recipePanelImage.color = new Color(1f, 0.94f, 0.78f, 0.42f);
            }

            var recipeTransform = recipePanel.transform;
            var titleText = GetOrCreateText(recipeTransform, "Recipe Title Text", "따뜻한 우유 수프", 20, TextAnchor.UpperLeft, new Vector2(22, -16), new Vector2(580, 30));
            titleText.fontStyle = FontStyle.Bold;
            var detailText = GetOrCreateText(recipeTransform, "Recipe Detail Text", "레시피를 선택하세요.", 15, TextAnchor.UpperLeft, new Vector2(22, -56), new Vector2(580, 116));
            detailText.lineSpacing = 1.1f;
            var statusText = GetOrCreateText(recipeTransform, "Recipe Status Text", "만들 수 있습니다.", 15, TextAnchor.UpperLeft, new Vector2(22, -216), new Vector2(360, 30));
            statusText.fontStyle = FontStyle.Bold;
            statusText.color = new Color(0.34f, 0.22f, 0.1f);

            var cookButton = GetOrCreateTopLeftButton(panelTransform, "Cook Recipe Button", "만들기", new Vector2(500, -504), new Vector2(152, 48));
            ApplyCareButtonStyle(cookButton);
            var tipText = GetOrCreateText(panelTransform, "Cooking Tip Text", "요리는 돌봄 기록과 도감 이벤트에 남고 자동 저장됩니다.", 14, TextAnchor.UpperLeft, new Vector2(28, -544), new Vector2(440, 24));
            tipText.color = new Color(0.38f, 0.28f, 0.17f);

            foreach (var recipeButton in recipeButtons)
            {
                if (recipeButton != null)
                {
                    recipeButton.transform.SetAsLastSibling();
                }
            }

            var cookingController = canvasTransform.GetComponent<CookingPanelController>();
            if (cookingController == null)
            {
                cookingController = canvasTransform.gameObject.AddComponent<CookingPanelController>();
            }

            cookingController.Configure(
                panel,
                titleText,
                detailText,
                statusText,
                null,
                recipeButtons,
                cookButton,
                closeButton,
                controller,
                visualController);
            return cookingController;
        }

        private static SnackPanelController BuildSnackPanel(
            Transform canvasTransform,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController)
        {
            var panel = GetOrCreatePanel(canvasTransform, "Snack Panel", MilkroomToolPanelPosition, MilkroomToolPanelSize);
            if (panel.TryGetComponent(out Image panelImage))
            {
                panelImage.color = new Color(1f, 0.98f, 0.9f, 0.98f);
            }

            var panelTransform = panel.transform;
            GetOrCreateText(panelTransform, "Snack Panel Header Text", "간식", 24, TextAnchor.UpperLeft, new Vector2(28, -24), new Vector2(220, 36));
            GetOrCreateText(panelTransform, "Snack Panel Help Text", "요리한 음식을 보관하고 수량을 확인한 뒤 먹입니다.", 14, TextAnchor.UpperLeft, new Vector2(28, -64), new Vector2(460, 28));

            var closeButton = GetOrCreateTopLeftButton(panelTransform, "Close Snack Panel Button", "닫기", new Vector2(536, -20), new Vector2(116, 40));
            for (var i = 0; i < 12; i++)
            {
                RemoveChildIfExists(panelTransform, $"Snack Inventory Row {i}");
                RemoveChildIfExists(panelTransform, $"Snack Row Title Text {i}");
                RemoveChildIfExists(panelTransform, $"Snack Row Detail Text {i}");
                RemoveChildIfExists(panelTransform, $"Snack Row Quantity Text {i}");
                RemoveChildIfExists(panelTransform, $"Feed Snack Item Button {i}");
                RemoveChildIfExists(panelTransform, $"Snack Feed Visible Label {i}");
            }
            RemoveChildIfExists(panelTransform, "Snack Inventory List Text");
            RemoveChildIfExists(panelTransform, "Snack Inventory Scroll View Viewport");
            RemoveChildIfExists(panelTransform, "Snack Inventory Scroll Background");

            var snacks = SnackCatalog.VisibleSnackItems;
            var titleTexts = new Text[snacks.Length];
            var detailTexts = new Text[snacks.Length];
            var quantityTexts = new Text[snacks.Length];
            var feedButtons = new Button[snacks.Length];
            const float snackRowStep = 132f;
            const float snackRowTopPadding = 12f;
            const float snackRowHeight = 116f;
            var contentHeight = Mathf.Max(366f, snackRowTopPadding + snacks.Length * snackRowStep + 12f);
            var scrollContent = GetOrCreateVerticalScrollContent(
                panelTransform,
                "Snack Inventory Scroll View",
                "Snack Inventory Scroll Content",
                new Vector2(28, -116),
                new Vector2(624, 366),
                contentHeight,
                new Color(1f, 0.96f, 0.82f, 0.38f));

            for (var i = 0; i < 12; i++)
            {
                RemoveChildIfExists(scrollContent, $"Snack Inventory Row {i}");
            }

            for (var i = 0; i < snacks.Length; i++)
            {
                var snack = snacks[i];
                var rowPanel = GetOrCreatePanel(
                    scrollContent,
                    $"Snack Inventory Row {i}",
                    new Vector2(10, -snackRowTopPadding - (i * snackRowStep)),
                    new Vector2(580, snackRowHeight));
                if (rowPanel.TryGetComponent(out Image rowImage))
                {
                    rowImage.color = new Color(1f, 0.93f, 0.68f, 0.98f);
                }

                var rowTransform = rowPanel.transform;
                titleTexts[i] = GetOrCreateText(rowTransform, $"Snack Row Title Text {i}", snack.displayName, 18, TextAnchor.UpperLeft, new Vector2(18, -14), new Vector2(360, 28));
                titleTexts[i].fontStyle = FontStyle.Bold;
                titleTexts[i].color = new Color(0.16f, 0.09f, 0.04f);
                titleTexts[i].raycastTarget = false;
                detailTexts[i] = GetOrCreateText(rowTransform, $"Snack Row Detail Text {i}", $"{snack.description}\n효과: 포만감 +0", 13, TextAnchor.UpperLeft, new Vector2(18, -48), new Vector2(404, 56));
                detailTexts[i].lineSpacing = 1.08f;
                detailTexts[i].color = new Color(0.20f, 0.12f, 0.05f);
                detailTexts[i].raycastTarget = false;
                quantityTexts[i] = GetOrCreateText(rowTransform, $"Snack Row Quantity Text {i}", "수량 0", 15, TextAnchor.MiddleLeft, new Vector2(444, -18), new Vector2(100, 28));
                quantityTexts[i].fontStyle = FontStyle.Bold;
                quantityTexts[i].color = new Color(0.16f, 0.09f, 0.04f);
                quantityTexts[i].raycastTarget = false;
                feedButtons[i] = GetOrCreateTopLeftButton(rowTransform, $"Feed Snack Item Button {i}", "먹이기", new Vector2(442, -66), new Vector2(108, 38));
                ApplyCareButtonStyle(feedButtons[i]);
                rowPanel.transform.SetAsLastSibling();
            }

            var statusText = GetOrCreateText(panelTransform, "Snack Panel Status Text", "요리한 간식이 없습니다. 요리에서 먼저 만들어 주세요.", 15, TextAnchor.UpperLeft, new Vector2(28, -508), new Vector2(430, 28));
            statusText.fontStyle = FontStyle.Bold;
            statusText.color = new Color(0.34f, 0.22f, 0.1f);
            var tipText = GetOrCreateText(panelTransform, "Snack Panel Tip Text", "간식 수량은 요리 패널에서 음식을 만들 때 증가합니다.", 14, TextAnchor.UpperLeft, new Vector2(28, -544), new Vector2(440, 24));
            tipText.color = new Color(0.38f, 0.28f, 0.17f);

            var snackController = canvasTransform.GetComponent<SnackPanelController>();
            if (snackController == null)
            {
                snackController = canvasTransform.gameObject.AddComponent<SnackPanelController>();
            }

            snackController.Configure(
                panel,
                titleTexts,
                detailTexts,
                quantityTexts,
                feedButtons,
                null,
                statusText,
                closeButton,
                controller,
                visualController);
            return snackController;
        }

        private static void ConfigureTopMenu(
            Transform canvasTransform,
            Button collectionButton,
            Button decorateButton,
            Button settingsButton,
            Button collectionCloseButton,
            Button decorateCloseButton,
            Button settingsCloseButton,
            GameObject collectionOverlay,
            GameObject decorateOverlay,
            GameObject settingsModal,
            CollectionUIController collectionController)
        {
            var topMenuController = canvasTransform.GetComponent<TopMenuController>();
            if (topMenuController == null)
            {
                topMenuController = canvasTransform.gameObject.AddComponent<TopMenuController>();
            }

            topMenuController.Configure(
                collectionButton,
                decorateButton,
                settingsButton,
                collectionCloseButton,
                decorateCloseButton,
                settingsCloseButton,
                collectionOverlay,
                decorateOverlay,
                settingsModal,
                collectionController);
        }

        private static void EnsureCheeseTamaProfileMenuShell(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var topBar = canvasTransform.Find("Top Status Bar");
            if (topBar != null)
            {
                var profileButton = GetOrCreateTopLeftButton(
                    topBar,
                    "CheeseTama Profile Button",
                    string.Empty,
                    new Vector2(18f, -13f),
                    new Vector2(56f, 56f));
                ApplyCareButtonStyle(profileButton);
                var profileBackground = profileButton.targetGraphic as Image
                    ?? profileButton.GetComponent<Image>();
                if (profileBackground != null)
                {
                    ApplyRoundedImage(profileBackground);
                    profileBackground.color = new Color(1f, 0.67f, 0.12f, 1f);
                    profileBackground.raycastTarget = true;
                }

                var mask = profileButton.GetComponent<Mask>() ?? profileButton.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;
                var portraitTransform = profileButton.transform.Find("Profile Portrait Image");
                Image portraitImage;
                if (portraitTransform == null)
                {
                    var portraitObject = new GameObject("Profile Portrait Image", typeof(RectTransform));
                    portraitObject.transform.SetParent(profileButton.transform, false);
                    portraitTransform = portraitObject.transform;
                    portraitImage = portraitObject.AddComponent<Image>();
                }
                else
                {
                    portraitImage = portraitTransform.GetComponent<Image>()
                        ?? portraitTransform.gameObject.AddComponent<Image>();
                }

                var portraitRect = portraitTransform.GetComponent<RectTransform>();
                portraitRect.anchorMin = Vector2.zero;
                portraitRect.anchorMax = Vector2.one;
                portraitRect.offsetMin = new Vector2(4f, 4f);
                portraitRect.offsetMax = new Vector2(-4f, -4f);
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;
                portraitTransform.SetAsLastSibling();

                var nameRect = topBar.Find("Name Text") as RectTransform;
                ConfigureTopLeftRect(nameRect, 92f, 17f, 212f, 48f);
                if (nameRect != null && nameRect.TryGetComponent(out Text nameText))
                {
                    nameText.resizeTextForBestFit = true;
                    nameText.resizeTextMinSize = 18;
                    nameText.resizeTextMaxSize = 28;
                }
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                CheeseTamaProfileMenuController.OverlayObjectName,
                new Color(0.08f, 0.055f, 0.025f, 0.72f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Profile Card",
                Vector2.zero,
                new Vector2(600f, 610f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(600f, 610f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.84f, 1f);
                cardImage.raycastTarget = true;
            }

            var heading = GetOrCreateText(
                card.transform,
                "Profile Heading Text",
                "프로필",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(50f, -30f),
                new Vector2(500f, 48f));
            heading.fontStyle = FontStyle.Bold;
            var profileName = GetOrCreateText(
                card.transform,
                "Profile Name Text",
                "CheeseTama",
                26,
                TextAnchor.MiddleCenter,
                new Vector2(150f, -82f),
                new Vector2(300f, 42f));
            profileName.fontStyle = FontStyle.Bold;
            profileName.resizeTextForBestFit = true;
            profileName.resizeTextMinSize = 18;
            profileName.resizeTextMaxSize = 26;
            var profileDetail = GetOrCreateText(
                card.transform,
                "Profile Detail Text",
                "Lv. 1 · 치즈타마 알",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(50f, -124f),
                new Vector2(500f, 34f));
            profileDetail.color = new Color(0.55f, 0.34f, 0.14f, 1f);

            var entries = GetOrCreatePanel(
                card.transform,
                "Profile Entries",
                new Vector2(90f, -184f),
                new Vector2(420f, 288f));
            if (entries.TryGetComponent(out Image entriesImage))
            {
                entriesImage.color = Color.clear;
                entriesImage.raycastTarget = false;
            }

            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Profile Close Button",
                "닫기",
                new Vector2(212f, -530f),
                new Vector2(176f, 50f));
            ApplyCareButtonStyle(close);
            overlay.SetActive(false);
        }

        private static void EnsureCheeseTamaProfileMenu(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            EnsureCheeseTamaProfileMenuShell(canvasTransform);
            var overlay = canvasTransform.Find(CheeseTamaProfileMenuController.OverlayObjectName)?.gameObject;
            var card = overlay != null ? overlay.transform.Find("Profile Card") : null;
            var topBar = canvasTransform.Find("Top Status Bar");
            var profileButton = topBar != null
                ? topBar.Find("CheeseTama Profile Button")?.GetComponent<Button>()
                : null;
            var portraitImage = profileButton != null
                ? profileButton.transform.Find("Profile Portrait Image")?.GetComponent<Image>()
                : null;
            var entries = GetProfileMenuEntryParent(canvasTransform);
            var controller = canvasTransform.GetComponent<CheeseTamaProfileMenuController>()
                ?? canvasTransform.gameObject.AddComponent<CheeseTamaProfileMenuController>();
            controller.Configure(
                overlay,
                profileButton,
                portraitImage,
                card != null ? card.Find("Profile Name Text")?.GetComponent<Text>() : null,
                card != null ? card.Find("Profile Detail Text")?.GetComponent<Text>() : null,
                entries != null ? entries.Find("Open First Day Journey Button")?.GetComponent<Button>() : null,
                entries != null ? entries.Find("Open Growth Journey Button")?.GetComponent<Button>() : null,
                entries != null ? entries.Find("Open Memory Journal Button")?.GetComponent<Button>() : null,
                entries != null ? entries.Find("Open Bond Status Button")?.GetComponent<Button>() : null,
                card != null ? card.Find("Open Name Change Button")?.GetComponent<Button>() : null,
                card != null ? card.Find("Profile Close Button")?.GetComponent<Button>() : null,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay?.transform.SetAsLastSibling();
        }

        private static Transform GetProfileMenuEntryParent(Transform canvasTransform)
        {
            return canvasTransform?.Find(
                CheeseTamaProfileMenuController.OverlayObjectName + "/Profile Card/Profile Entries");
        }

        private static Button GetOrMoveProfileEntryButton(
            Transform canvasTransform,
            Transform legacyParent,
            string name,
            string label,
            int entryIndex)
        {
            var entryParent = GetProfileMenuEntryParent(canvasTransform);
            if (entryParent == null)
            {
                return GetOrCreateTopLeftButton(
                    legacyParent != null ? legacyParent : canvasTransform,
                    name,
                    label,
                    Vector2.zero,
                    new Vector2(420f, 48f));
            }

            var current = entryParent.Find(name);
            var legacy = legacyParent != null ? legacyParent.Find(name) : null;
            if (current == null && legacy != null)
            {
                legacy.SetParent(entryParent, false);
                current = legacy;
            }
            else if (current != null && legacy != null && legacy != current)
            {
                DestroyObjectSafely(legacy.gameObject);
            }

            var button = current != null ? current.GetComponent<Button>() : null;
            if (button == null)
            {
                button = GetOrCreateTopLeftButton(
                    entryParent,
                    name,
                    label,
                    Vector2.zero,
                    new Vector2(420f, 48f));
            }

            ConfigureTopLeftButton(
                button,
                label,
                new Vector2(0f, -entryIndex * 58f),
                new Vector2(420f, 48f));
            return button;
        }

        private static Button GetOrMoveProfileRenameButton(
            Transform canvasTransform,
            Transform legacyParent)
        {
            var card = canvasTransform?.Find(
                CheeseTamaProfileMenuController.OverlayObjectName + "/Profile Card");
            if (card == null)
            {
                return GetOrCreateTopLeftButton(
                    legacyParent != null ? legacyParent : canvasTransform,
                    "Open Name Change Button",
                    "이름 변경",
                    Vector2.zero,
                    new Vector2(100f, 36f));
            }

            var entries = GetProfileMenuEntryParent(canvasTransform);
            var current = card.Find("Open Name Change Button");
            var legacy = legacyParent != null
                ? legacyParent.Find("Open Name Change Button")
                : null;
            var oldEntry = entries != null
                ? entries.Find("Open Name Change Button")
                : null;
            if (current == null)
            {
                var source = legacy != null ? legacy : oldEntry;
                if (source != null)
                {
                    source.SetParent(card, false);
                    current = source;
                }
            }

            if (legacy != null && legacy != current)
            {
                DestroyObjectSafely(legacy.gameObject);
            }

            if (oldEntry != null && oldEntry != current)
            {
                DestroyObjectSafely(oldEntry.gameObject);
            }

            var button = current != null ? current.GetComponent<Button>() : null;
            if (button == null)
            {
                button = GetOrCreateTopLeftButton(
                    card,
                    "Open Name Change Button",
                    "이름 변경",
                    Vector2.zero,
                    new Vector2(100f, 36f));
            }

            ConfigureTopLeftButton(
                button,
                "이름 변경",
                new Vector2(456f, -85f),
                new Vector2(100f, 36f));
            return button;
        }

        private static void EnsureMilkroomStatGauges(
            Transform canvasTransform,
            MilkroomUIController controller)
        {
            var statBar = canvasTransform?.Find("Stat Bar");
            if (statBar == null || controller == null)
            {
                return;
            }

            ConfigureTopLeftRect(statBar.Find("Hunger Text") as RectTransform, 22f, 72f, 306f, 30f);
            ConfigureTopLeftRect(statBar.Find("Mood Text") as RectTransform, 22f, 132f, 306f, 30f);
            ConfigureTopLeftRect(statBar.Find("Cleanliness Text") as RectTransform, 22f, 192f, 306f, 30f);
            ConfigureTopLeftRect(statBar.Find("Sleepiness Text") as RectTransform, 22f, 252f, 306f, 30f);
            ConfigureTopLeftRect(statBar.Find("Health Text") as RectTransform, 22f, 312f, 306f, 30f);

            controller.ConfigureStatGauges(
                GetOrCreateStatGauge(statBar, "Hunger Gauge", 108f),
                GetOrCreateStatGauge(statBar, "Mood Gauge", 168f),
                GetOrCreateStatGauge(statBar, "Cleanliness Gauge", 228f),
                GetOrCreateStatGauge(statBar, "Sleepiness Gauge", 288f),
                GetOrCreateStatGauge(statBar, "Health Gauge", 348f));
        }

        private static Image GetOrCreateStatGauge(
            Transform statBar,
            string gaugeName,
            float top)
        {
            var track = GetOrCreatePanel(
                statBar,
                gaugeName,
                new Vector2(22f, -top),
                new Vector2(306f, 10f));
            var trackImage = track.GetComponent<Image>();
            if (trackImage != null)
            {
                ApplyRoundedImage(trackImage);
                trackImage.color = new Color(0.32f, 0.25f, 0.17f, 0.16f);
                trackImage.raycastTarget = false;
            }

            var fillTransform = track.transform.Find("Fill");
            Image fill;
            if (fillTransform == null)
            {
                var fillObject = new GameObject(
                    "Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                fillObject.transform.SetParent(track.transform, false);
                fillTransform = fillObject.transform;
                fill = fillObject.GetComponent<Image>();
            }
            else
            {
                fill = fillTransform.GetComponent<Image>()
                    ?? fillTransform.gameObject.AddComponent<Image>();
            }

            var fillRect = fillTransform.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);
            fill.sprite = GetRoundedUiSprite();
            fill.raycastTarget = false;
            return fill;
        }

        private static Transform GetOrCreateMilkroomUtilityBar(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return null;
            }

            var utilityBar = GetOrCreatePanel(
                canvasTransform,
                "Milkroom Utility Bar",
                new Vector2(24f, -650f),
                new Vector2(350f, 92f));
            if (utilityBar.TryGetComponent(out Image image))
            {
                image.color = Color.clear;
                image.raycastTarget = false;
            }

            return utilityBar.transform;
        }

        private static Button GetOrMoveUtilityButton(
            Transform canvasTransform,
            Transform legacyParent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var utilityParent = GetOrCreateMilkroomUtilityBar(canvasTransform);
            if (utilityParent == null)
            {
                return null;
            }

            var current = utilityParent.Find(name);
            var legacy = legacyParent != null ? legacyParent.Find(name) : null;
            if (current == null && legacy != null)
            {
                legacy.SetParent(utilityParent, false);
                current = legacy;
            }
            else if (current != null && legacy != null && current != legacy)
            {
                DestroyObjectSafely(legacy.gameObject);
            }

            var button = current != null ? current.GetComponent<Button>() : null;
            if (button == null)
            {
                button = GetOrCreateTopLeftButton(
                    utilityParent,
                    name,
                    label,
                    Vector2.zero,
                    size);
            }

            ConfigureTopLeftButton(
                button,
                label,
                anchoredPosition,
                size);
            return button;
        }

        private static void EnsureCheeseTamaNameDialog(
            Transform canvasTransform,
            MilkroomUIController milkroomUi)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var settingsModal = canvasTransform.Find("Settings Modal");
            if (settingsModal == null)
            {
                return;
            }

            var openButton = GetOrMoveProfileRenameButton(canvasTransform, settingsModal);
            ApplyCareButtonStyle(openButton);

            var dialogTransform = canvasTransform.Find("CheeseTama Name Dialog");
            GameObject dialogRoot;
            RectTransform dialogRect;
            if (dialogTransform == null)
            {
                dialogRoot = new GameObject("CheeseTama Name Dialog", typeof(RectTransform));
                dialogRoot.transform.SetParent(canvasTransform, false);
                dialogRect = dialogRoot.GetComponent<RectTransform>();
            }
            else
            {
                dialogRoot = dialogTransform.gameObject;
                dialogRect = dialogRoot.GetComponent<RectTransform>();
                if (dialogRect == null)
                {
                    dialogRect = dialogRoot.AddComponent<RectTransform>();
                }
            }

            dialogRect.anchorMin = Vector2.zero;
            dialogRect.anchorMax = Vector2.one;
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;

            var dimImage = dialogRoot.GetComponent<Image>();
            if (dimImage == null)
            {
                dimImage = dialogRoot.AddComponent<Image>();
            }

            dimImage.color = new Color(0.08f, 0.05f, 0.02f, 0.62f);
            dimImage.raycastTarget = true;
            var canvasGroup = dialogRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dialogRoot.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var card = GetOrCreatePanel(
                dialogRoot.transform,
                "Name Change Card",
                Vector2.zero,
                new Vector2(600f, 320f));
            var cardRect = card.GetComponent<RectTransform>();
            ConfigureCenteredRect(cardRect, new Vector2(600f, 320f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.98f, 0.9f, 1f);
                cardImage.raycastTarget = true;
            }

            var titleText = GetOrCreateText(
                card.transform,
                "Name Change Title Text",
                "이름 변경",
                28,
                TextAnchor.MiddleLeft,
                new Vector2(36f, -28f),
                new Vector2(528f, 44f));
            titleText.fontStyle = FontStyle.Bold;
            var guideText = GetOrCreateText(
                card.transform,
                "Name Change Guide Text",
                "새 이름을 1자부터 12자 사이로 입력해 주세요.",
                17,
                TextAnchor.MiddleLeft,
                new Vector2(36f, -78f),
                new Vector2(528f, 30f));
            guideText.color = new Color(0.38f, 0.28f, 0.17f);
            var nameInput = GetOrCreateInputField(
                card.transform,
                "Name Change Input",
                "이름을 입력하세요 (최대 12자)",
                new Vector2(36f, -120f),
                new Vector2(528f, 56f));
            var statusText = GetOrCreateText(
                card.transform,
                "Name Change Status Text",
                string.Empty,
                15,
                TextAnchor.MiddleLeft,
                new Vector2(36f, -188f),
                new Vector2(528f, 30f));
            statusText.color = new Color(0.72f, 0.16f, 0.1f);

            var saveButton = GetOrCreateTopLeftButton(
                card.transform,
                "Save Name Change Button",
                "변경하기",
                new Vector2(304f, -244f),
                new Vector2(120f, 48f));
            var cancelButton = GetOrCreateTopLeftButton(
                card.transform,
                "Cancel Name Change Button",
                "취소",
                new Vector2(440f, -244f),
                new Vector2(124f, 48f));
            ApplyCareButtonStyle(saveButton);
            ApplyCareButtonStyle(cancelButton);

            var controller = canvasTransform.GetComponent<CheeseTamaNameDialogController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<CheeseTamaNameDialogController>();
            }

            controller.Configure(
                openButton,
                dialogRoot,
                nameInput,
                statusText,
                saveButton,
                cancelButton,
                milkroomUi,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            dialogRoot.transform.SetAsLastSibling();
        }

        private static void EnsureFirstMeetingOnboarding(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            Button FindButton(string path)
            {
                var target = canvasTransform.Find(path);
                return target != null ? target.GetComponent<Button>() : null;
            }

            var settingsModal = canvasTransform.Find("Settings Modal")?.gameObject;
            Button replayButton = null;
            if (settingsModal != null)
            {
                replayButton = GetOrCreateTopLeftButton(
                    settingsModal.transform,
                    "Replay First Meeting Button",
                    "튜토리얼",
                    new Vector2(442f, -170f),
                    new Vector2(90f, 42f));
                ApplyCareButtonStyle(replayButton);
            }

            var overlayTransform = canvasTransform.Find("First Meeting Onboarding Overlay");
            GameObject overlayRoot;
            RectTransform overlayRect;
            if (overlayTransform == null)
            {
                overlayRoot = new GameObject("First Meeting Onboarding Overlay", typeof(RectTransform));
                overlayRoot.transform.SetParent(canvasTransform, false);
                overlayRect = overlayRoot.GetComponent<RectTransform>();
            }
            else
            {
                overlayRoot = overlayTransform.gameObject;
                overlayRect = overlayRoot.GetComponent<RectTransform>();
                if (overlayRect == null)
                {
                    overlayRect = overlayRoot.AddComponent<RectTransform>();
                }
            }

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var dimImage = overlayRoot.GetComponent<Image>();
            if (dimImage == null)
            {
                dimImage = overlayRoot.AddComponent<Image>();
            }

            dimImage.color = new Color(0.08f, 0.05f, 0.02f, 0.62f);
            dimImage.raycastTarget = false;
            var overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
            {
                overlayCanvasGroup = overlayRoot.AddComponent<CanvasGroup>();
            }

            overlayCanvasGroup.alpha = 1f;
            overlayCanvasGroup.interactable = true;
            overlayCanvasGroup.blocksRaycasts = true;

            var card = GetOrCreatePanel(overlayRoot.transform, "First Meeting Card", Vector2.zero, new Vector2(760f, 380f));
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(760f, 380f);
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.98f, 0.9f, 0.99f);
                cardImage.raycastTarget = true;
            }

            var stepText = GetOrCreateText(
                card.transform,
                "First Meeting Step Text",
                "튜토리얼 · 1/4",
                18,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -34f),
                new Vector2(664f, 28f));
            stepText.fontStyle = FontStyle.Bold;
            stepText.color = new Color(0.74f, 0.38f, 0.08f);

            var titleText = GetOrCreateText(
                card.transform,
                "First Meeting Title Text",
                "밀크룸에 온 걸 환영해요",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -82f),
                new Vector2(664f, 52f));
            titleText.fontStyle = FontStyle.Bold;

            var bodyText = GetOrCreateText(
                card.transform,
                "First Meeting Body Text",
                "작은 치즈 생명체가 당신을 기다리고 있어요.",
                21,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -154f),
                new Vector2(664f, 112f));
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 16;
            bodyText.resizeTextMaxSize = 21;

            RemoveChildIfExists(card.transform, "First Meeting Name Input");
            RemoveChildIfExists(card.transform, "First Meeting Status Text");

            var primaryButton = GetOrCreateTopLeftButton(
                card.transform,
                "First Meeting Primary Button",
                "시작하기",
                new Vector2(428f, -300f),
                new Vector2(174f, 52f));
            var skipButton = GetOrCreateTopLeftButton(
                card.transform,
                "First Meeting Skip Button",
                "건너뛰기",
                new Vector2(616f, -300f),
                new Vector2(112f, 52f));
            ApplyCareButtonStyle(primaryButton);
            ApplyCareButtonStyle(skipButton);

            var skipConfirmationTransform = overlayRoot.transform.Find("Skip Tutorial Confirmation");
            GameObject skipConfirmationRoot;
            RectTransform skipConfirmationRect;
            if (skipConfirmationTransform == null)
            {
                skipConfirmationRoot = new GameObject("Skip Tutorial Confirmation");
                skipConfirmationRoot.transform.SetParent(overlayRoot.transform, false);
                skipConfirmationRect = skipConfirmationRoot.AddComponent<RectTransform>();
            }
            else
            {
                skipConfirmationRoot = skipConfirmationTransform.gameObject;
                skipConfirmationRect = skipConfirmationRoot.GetComponent<RectTransform>();
                if (skipConfirmationRect == null)
                {
                    skipConfirmationRect = skipConfirmationRoot.AddComponent<RectTransform>();
                }
            }

            skipConfirmationRect.anchorMin = Vector2.zero;
            skipConfirmationRect.anchorMax = Vector2.one;
            skipConfirmationRect.pivot = new Vector2(0.5f, 0.5f);
            skipConfirmationRect.anchoredPosition = Vector2.zero;
            skipConfirmationRect.offsetMin = Vector2.zero;
            skipConfirmationRect.offsetMax = Vector2.zero;
            var skipConfirmationDim = skipConfirmationRoot.GetComponent<Image>();
            if (skipConfirmationDim == null)
            {
                skipConfirmationDim = skipConfirmationRoot.AddComponent<Image>();
            }

            skipConfirmationDim.color = new Color(0.08f, 0.05f, 0.02f, 0.72f);
            skipConfirmationDim.raycastTarget = true;
            var skipConfirmationGroup = skipConfirmationRoot.GetComponent<CanvasGroup>();
            if (skipConfirmationGroup == null)
            {
                skipConfirmationGroup = skipConfirmationRoot.AddComponent<CanvasGroup>();
            }

            skipConfirmationGroup.alpha = 1f;
            skipConfirmationGroup.interactable = true;
            skipConfirmationGroup.blocksRaycasts = true;

            var skipConfirmationCard = GetOrCreatePanel(
                skipConfirmationRoot.transform,
                "Skip Tutorial Confirmation Card",
                Vector2.zero,
                new Vector2(560f, 280f));
            ConfigureCenteredRect(
                skipConfirmationCard.GetComponent<RectTransform>(),
                new Vector2(560f, 280f));
            if (skipConfirmationCard.TryGetComponent(out Image skipConfirmationCardImage))
            {
                skipConfirmationCardImage.color = new Color(1f, 0.98f, 0.9f, 1f);
                skipConfirmationCardImage.raycastTarget = true;
            }

            var skipConfirmationTitle = GetOrCreateText(
                skipConfirmationCard.transform,
                "Skip Tutorial Confirmation Title Text",
                "튜토리얼을 건너뛰시겠습니까?",
                26,
                TextAnchor.MiddleCenter,
                new Vector2(36f, -40f),
                new Vector2(488f, 48f));
            skipConfirmationTitle.fontStyle = FontStyle.Bold;
            var skipConfirmationBody = GetOrCreateText(
                skipConfirmationCard.transform,
                "Skip Tutorial Confirmation Body Text",
                "건너뛴 뒤에도 설정에서 튜토리얼을 다시 볼 수 있습니다.",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(36f, -106f),
                new Vector2(488f, 56f));
            skipConfirmationBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            skipConfirmationBody.verticalOverflow = VerticalWrapMode.Truncate;

            var continueTutorialButton = GetOrCreateTopLeftButton(
                skipConfirmationCard.transform,
                "Continue Tutorial Button",
                "계속 진행",
                new Vector2(250f, -202f),
                new Vector2(126f, 48f));
            var confirmSkipButton = GetOrCreateTopLeftButton(
                skipConfirmationCard.transform,
                "Confirm Skip Tutorial Button",
                "건너뛰기",
                new Vector2(392f, -202f),
                new Vector2(132f, 48f));
            ApplyCareButtonStyle(continueTutorialButton);
            ApplyDangerButtonStyle(confirmSkipButton);

            var actionButtons = new[]
            {
                FindButton("Bottom Action Bar/Milk Button"),
                FindButton("Bottom Action Bar/Blend Button"),
                FindButton("Bottom Action Bar/Snack Button"),
                FindButton("Bottom Action Bar/Play Button"),
                FindButton("Bottom Action Bar/Clean Button"),
                FindButton("Bottom Action Bar/Sleep Button")
            };
            var topMenuController = canvasTransform.GetComponent<TopMenuController>();
            var onboardingController = canvasTransform.GetComponent<FirstMeetingOnboardingController>();
            if (onboardingController == null)
            {
                onboardingController = canvasTransform.gameObject.AddComponent<FirstMeetingOnboardingController>();
            }

            onboardingController.Configure(
                overlayRoot,
                cardRect,
                dimImage,
                stepText,
                titleText,
                bodyText,
                primaryButton,
                skipButton,
                skipConfirmationRoot,
                confirmSkipButton,
                continueTutorialButton,
                replayButton,
                actionButtons,
                actionButtons[0],
                actionButtons[3],
                actionButtons[4],
                FindButton("Top Menu/Top Collection Button"),
                FindButton("Top Menu/Top Decorate Button"),
                FindButton("Top Menu/Settings Button"),
                FindButton("Dev Mode Toggle Button"),
                topMenuController,
                settingsModal,
                canvasTransform.GetComponent<MilkPanelController>(),
                canvasTransform.GetComponent<CookingPanelController>(),
                canvasTransform.GetComponent<SnackPanelController>(),
                milkroomUi,
                visualController);
            overlayRoot.transform.SetAsLastSibling();
        }

        private static void EnsureSaveRecoveryNotice(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                SaveRecoveryNoticeController.OverlayObjectName,
                new Color(0.04f, 0.045f, 0.07f, 0.8f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Save Recovery Notice Card",
                Vector2.zero,
                new Vector2(680f, 390f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(680f, 390f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.96f, 0.97f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Save Recovery Notice Title Text",
                "저장 복구 완료",
                31,
                TextAnchor.MiddleCenter,
                new Vector2(52f, -42f),
                new Vector2(576f, 54f));
            title.fontStyle = FontStyle.Bold;
            var message = GetOrCreateText(
                card.transform,
                "Save Recovery Notice Message Text",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                new Vector2(72f, -126f),
                new Vector2(536f, 126f));
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Overflow;
            var confirm = GetOrCreateTopLeftButton(
                card.transform,
                "Save Recovery Notice Confirm Button",
                "확인",
                new Vector2(250f, -302f),
                new Vector2(180f, 54f));
            ApplyCareButtonStyle(confirm);

            var controller = canvasTransform.GetComponent<SaveRecoveryNoticeController>()
                ?? canvasTransform.gameObject.AddComponent<SaveRecoveryNoticeController>();
            controller.Configure(overlay, title, message, confirm);
            var manager = Application.isPlaying ? GameManager.Instance : null;
            var bridge = canvasTransform.GetComponent<SaveRecoveryNoticeBridge>()
                ?? canvasTransform.gameObject.AddComponent<SaveRecoveryNoticeBridge>();
            bridge.Configure(
                controller,
                () => manager?.LastSaveRecoveryReport ?? SaveRecoveryReport.NoRecovery,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureReturnSummary(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlayTransform = canvasTransform.Find("Return Summary Overlay");
            GameObject overlayRoot;
            RectTransform overlayRect;
            if (overlayTransform == null)
            {
                overlayRoot = new GameObject("Return Summary Overlay", typeof(RectTransform));
                overlayRoot.transform.SetParent(canvasTransform, false);
                overlayRect = overlayRoot.GetComponent<RectTransform>();
            }
            else
            {
                overlayRoot = overlayTransform.gameObject;
                overlayRect = overlayRoot.GetComponent<RectTransform>();
                if (overlayRect == null)
                {
                    overlayRect = overlayRoot.AddComponent<RectTransform>();
                }
            }

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var dimImage = overlayRoot.GetComponent<Image>();
            if (dimImage == null)
            {
                dimImage = overlayRoot.AddComponent<Image>();
            }

            dimImage.color = new Color(0.08f, 0.05f, 0.02f, 0.66f);
            dimImage.raycastTarget = true;
            var canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = overlayRoot.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var card = GetOrCreatePanel(
                overlayRoot.transform,
                "Return Summary Card",
                Vector2.zero,
                new Vector2(680f, 500f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(680f, 500f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.98f, 0.9f, 1f);
                cardImage.raycastTarget = true;
            }

            var titleText = GetOrCreateText(
                card.transform,
                "Return Summary Title Text",
                "다시 만나서 반가워요",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -38f),
                new Vector2(584f, 48f));
            titleText.fontStyle = FontStyle.Bold;

            var elapsedText = GetOrCreateText(
                card.transform,
                "Return Summary Elapsed Text",
                "잠시 자리를 비운 동안의 기록이에요.",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -96f),
                new Vector2(584f, 36f));
            elapsedText.color = new Color(0.55f, 0.32f, 0.12f);

            var changesPanel = GetOrCreatePanel(
                card.transform,
                "Return Summary Changes Panel",
                new Vector2(48f, -150f),
                new Vector2(584f, 202f));
            if (changesPanel.TryGetComponent(out Image changesPanelImage))
            {
                changesPanelImage.color = new Color(1f, 0.92f, 0.68f, 0.52f);
            }

            var changesText = GetOrCreateText(
                changesPanel.transform,
                "Return Summary Changes Text",
                "상태 변화를 확인하고 있어요.",
                19,
                TextAnchor.MiddleCenter,
                new Vector2(24f, -18f),
                new Vector2(536f, 166f));
            changesText.horizontalOverflow = HorizontalWrapMode.Wrap;
            changesText.verticalOverflow = VerticalWrapMode.Truncate;
            changesText.resizeTextForBestFit = true;
            changesText.resizeTextMinSize = 15;
            changesText.resizeTextMaxSize = 19;

            var rewardsText = GetOrCreateText(
                card.transform,
                "Return Summary Rewards Text",
                string.Empty,
                17,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -370f),
                new Vector2(584f, 38f));
            rewardsText.fontStyle = FontStyle.Bold;
            rewardsText.color = new Color(0.72f, 0.34f, 0.08f);

            var confirmButton = GetOrCreateTopLeftButton(
                card.transform,
                "Return Summary Confirm Button",
                "확인",
                new Vector2(476f, -424f),
                new Vector2(156f, 52f));
            ApplyCareButtonStyle(confirmButton);

            var controller = canvasTransform.GetComponent<ReturnSummaryController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<ReturnSummaryController>();
            }

            controller.Configure(
                overlayRoot,
                elapsedText,
                changesText,
                rewardsText,
                confirmButton,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlayRoot.transform.SetAsLastSibling();
        }

        private static void EnsureCareEventCard(
            Transform canvasTransform,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Care Event Overlay",
                new Color(0.12f, 0.08f, 0.04f, 0.64f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Care Event Card",
                Vector2.zero,
                new Vector2(680f, 440f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(680f, 440f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.84f, 1f);
                cardImage.raycastTarget = true;
            }

            var titleText = GetOrCreateText(
                card.transform,
                "Care Event Title Text",
                "밀크룸의 작은 순간",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -48f),
                new Vector2(584f, 52f));
            titleText.fontStyle = FontStyle.Bold;
            var badge = GetOrCreatePanel(
                card.transform,
                "First Discovery Badge",
                new Vector2(236f, -116f),
                new Vector2(208f, 38f));
            if (badge.TryGetComponent(out Image badgeImage))
            {
                badgeImage.color = new Color(1f, 0.74f, 0.22f, 0.94f);
            }

            var badgeText = GetOrCreateText(
                badge.transform,
                "First Discovery Badge Text",
                "새 도감 기록",
                17,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(184f, 30f));
            ConfigureCenteredRect(badgeText.rectTransform, new Vector2(184f, 30f));
            badgeText.fontStyle = FontStyle.Bold;
            var bodyText = GetOrCreateText(
                card.transform,
                "Care Event Body Text",
                "치즈타마와 밀크룸에서 발견한 순간이에요.",
                21,
                TextAnchor.MiddleCenter,
                new Vector2(58f, -170f),
                new Vector2(564f, 128f));
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 16;
            bodyText.resizeTextMaxSize = 21;
            var confirmButton = GetOrCreateTopLeftButton(
                card.transform,
                "Care Event Confirm Button",
                "확인",
                new Vector2(476f, -356f),
                new Vector2(156f, 52f));
            ApplyCareButtonStyle(confirmButton);

            var controller = canvasTransform.GetComponent<CareEventCardController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<CareEventCardController>();
            }

            controller.Configure(
                overlay,
                card.GetComponent<RectTransform>(),
                titleText,
                bodyText,
                badge,
                confirmButton,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>(),
                visualController);
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureGrowthMilestone(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Growth Achievement Overlay",
                new Color(0.12f, 0.07f, 0.02f, 0.72f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Growth Achievement Card",
                Vector2.zero,
                new Vector2(760f, 570f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(760f, 570f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.96f, 0.78f, 1f);
                cardImage.raycastTarget = true;
            }

            var titleText = GetOrCreateText(
                card.transform,
                "Growth Achievement Title Text",
                "새로운 성장 단계 달성!",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(54f, -36f),
                new Vector2(652f, 54f));
            titleText.fontStyle = FontStyle.Bold;
            var thumbnailPanel = GetOrCreatePanel(
                card.transform,
                "Growth Achievement Thumbnail",
                new Vector2(250f, -110f),
                new Vector2(260f, 230f));
            var thumbnail = thumbnailPanel.GetComponent<Image>();
            thumbnail.color = new Color(1f, 0.86f, 0.48f, 0.42f);
            thumbnail.raycastTarget = false;
            var levelText = GetOrCreateText(
                card.transform,
                "Growth Achievement Level Text",
                "Lv.10 · 새로운 성장 단계",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(60f, -354f),
                new Vector2(640f, 34f));
            levelText.fontStyle = FontStyle.Bold;
            var descriptionText = GetOrCreateText(
                card.transform,
                "Growth Achievement Description Text",
                "치즈타마가 한 단계 더 성장했어요.",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(72f, -398f),
                new Vector2(616f, 78f));
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.resizeTextForBestFit = true;
            descriptionText.resizeTextMinSize = 15;
            descriptionText.resizeTextMaxSize = 20;
            var confirmButton = GetOrCreateTopLeftButton(
                card.transform,
                "Growth Achievement Confirm Button",
                "새 모습 만나기",
                new Vector2(520f, -492f),
                new Vector2(186f, 52f));
            ApplyCareButtonStyle(confirmButton);

            var controller = canvasTransform.GetComponent<GrowthMilestoneController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<GrowthMilestoneController>();
            }

            controller.Configure(
                overlay,
                thumbnail,
                titleText,
                levelText,
                descriptionText,
                confirmButton,
                milkroomUi,
                visualController,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureMilkDropMiniGame(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Milk Drop Catch Overlay",
                new Color(0.04f, 0.09f, 0.16f, 0.78f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Milk Drop Catch Card",
                Vector2.zero,
                new Vector2(980f, 760f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(980f, 760f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.98f, 0.97f, 0.84f, 1f);
                cardImage.raycastTarget = true;
            }

            var titleText = GetOrCreateText(
                card.transform,
                "Milk Drop Catch Title Text",
                "우유방울 받기",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(44f, -28f),
                new Vector2(892f, 48f));
            titleText.fontStyle = FontStyle.Bold;
            var timeText = GetOrCreateText(
                card.transform,
                "Milk Drop Catch Time Text",
                "남은 시간  30초",
                21,
                TextAnchor.MiddleLeft,
                new Vector2(60f, -82f),
                new Vector2(340f, 36f));
            var scoreText = GetOrCreateText(
                card.transform,
                "Milk Drop Catch Score Text",
                "점수  0 · 성공 0 · 놓침 0",
                21,
                TextAnchor.MiddleRight,
                new Vector2(430f, -82f),
                new Vector2(490f, 36f));
            var playAreaObject = GetOrCreatePanel(
                card.transform,
                "Milk Drop Catch Play Area",
                new Vector2(60f, -132f),
                new Vector2(860f, 470f));
            var playArea = playAreaObject.GetComponent<RectTransform>();
            if (playAreaObject.TryGetComponent(out Image playAreaImage))
            {
                playAreaImage.color = new Color(0.72f, 0.9f, 1f, 0.58f);
                playAreaImage.raycastTarget = true;
            }

            if (playAreaObject.GetComponent<RectMask2D>() == null)
            {
                playAreaObject.AddComponent<RectMask2D>();
            }

            var dropTemplate = GetOrCreateButton(
                playArea,
                "Milk Drop Template",
                "●",
                Vector2.zero,
                Vector2.one * MilkDropMiniGameRules.DropSizePixels);
            ApplyCareButtonStyle(dropTemplate);
            if (dropTemplate.TryGetComponent(out Image dropImage))
            {
                dropImage.sprite = Resources.Load<Sprite>("UI/TopBarIcons/milkdrop");
                dropImage.type = Image.Type.Simple;
                dropImage.preserveAspect = true;
                dropImage.color = Color.white;
            }

            var dropLabel = dropTemplate.transform.Find("Label")?.GetComponent<Text>();
            if (dropLabel != null)
            {
                dropLabel.gameObject.SetActive(false);
            }

            var resultText = GetOrCreateText(
                card.transform,
                "Milk Drop Catch Result Text",
                "떨어지는 우유방울을 눌러서 받아 보세요!",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(60f, -618f),
                new Vector2(650f, 88f));
            resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultText.resizeTextForBestFit = true;
            resultText.resizeTextMinSize = 14;
            resultText.resizeTextMaxSize = 18;
            var cancelButton = GetOrCreateTopLeftButton(
                card.transform,
                "Milk Drop Catch Cancel Button",
                "그만하기",
                new Vector2(754f, -634f),
                new Vector2(166f, 52f));
            var confirmButton = GetOrCreateTopLeftButton(
                card.transform,
                "Milk Drop Catch Confirm Button",
                "확인",
                new Vector2(754f, -634f),
                new Vector2(166f, 52f));
            ApplyCareButtonStyle(cancelButton);
            ApplyCareButtonStyle(confirmButton);

            var controller = canvasTransform.GetComponent<MilkDropMiniGameController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<MilkDropMiniGameController>();
            }

            controller.Configure(
                overlay,
                playArea,
                dropTemplate,
                timeText,
                scoreText,
                resultText,
                cancelButton,
                confirmButton,
                milkroomUi,
                visualController,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());

            var playButton = canvasTransform.Find("Bottom Action Bar/Play Button")?.GetComponent<Button>();
            var careButton = playButton != null ? playButton.GetComponent<MilkroomCareButton>() : null;
            careButton?.Configure(MilkroomCareAction.CatchMilkDrops, milkroomUi, visualController);
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureBouncyJumpMiniGame(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                BouncyJumpMiniGameController.OverlayObjectName,
                new Color(0.08f, 0.07f, 0.18f, 0.8f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Bouncy Jump Card",
                Vector2.zero,
                new Vector2(940f, 730f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(940f, 730f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.96f, 0.91f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Bouncy Jump Title Text",
                "말랑 점프",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(44f, -24f),
                new Vector2(852f, 50f));
            title.fontStyle = FontStyle.Bold;
            var time = GetOrCreateText(
                card.transform,
                "Bouncy Jump Time Text",
                "남은 시간  25초",
                20,
                TextAnchor.MiddleLeft,
                new Vector2(58f, -82f),
                new Vector2(250f, 36f));
            var score = GetOrCreateText(
                card.transform,
                "Bouncy Jump Score Text",
                "점수  0",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(326f, -82f),
                new Vector2(270f, 36f));
            var combo = GetOrCreateText(
                card.transform,
                "Bouncy Jump Combo Text",
                "콤보  -",
                20,
                TextAnchor.MiddleRight,
                new Vector2(614f, -82f),
                new Vector2(268f, 36f));

            var playAreaObject = GetOrCreatePanel(
                card.transform,
                "Bouncy Jump Play Area",
                new Vector2(58f, -132f),
                new Vector2(824f, 414f));
            var playArea = playAreaObject.GetComponent<RectTransform>();
            if (playAreaObject.TryGetComponent(out Image playAreaImage))
            {
                playAreaImage.color = new Color(0.67f, 0.82f, 1f, 0.48f);
                playAreaImage.raycastTarget = true;
            }

            if (playAreaObject.GetComponent<RectMask2D>() == null)
            {
                playAreaObject.AddComponent<RectMask2D>();
            }

            var targetObject = GetOrCreatePanel(
                playArea,
                "Bouncy Jump Target Zone",
                Vector2.zero,
                new Vector2(150f, 28f));
            var target = targetObject.GetComponent<RectTransform>();
            ConfigureCenteredRect(target, new Vector2(150f, 28f));
            target.anchoredPosition = new Vector2(0f, -112f);
            if (targetObject.TryGetComponent(out Image targetImage))
            {
                targetImage.color = new Color(1f, 0.83f, 0.24f, 0.92f);
                targetImage.raycastTarget = false;
            }

            var markerObject = GetOrCreatePanel(
                playArea,
                "Bouncy Jump Tama Marker",
                Vector2.zero,
                new Vector2(86f, 86f));
            var marker = markerObject.GetComponent<RectTransform>();
            ConfigureCenteredRect(marker, new Vector2(86f, 86f));
            marker.anchoredPosition = new Vector2(0f, -112f);
            if (markerObject.TryGetComponent(out Image markerImage))
            {
                markerImage.color = new Color(1f, 0.72f, 0.25f, 1f);
                markerImage.raycastTarget = false;
                ApplyCircleImage(markerImage);
            }

            var face = GetOrCreateText(
                marker,
                "Bouncy Jump Tama Face",
                "•ᴗ•",
                24,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(78f, 52f));
            ConfigureCenteredRect(face.rectTransform, new Vector2(78f, 52f));
            face.raycastTarget = false;

            var jumpButton = GetOrCreateButton(
                playArea,
                "Bouncy Jump Input Button",
                "점프!",
                new Vector2(0f, 154f),
                new Vector2(216f, 60f));
            ApplyCareButtonStyle(jumpButton);
            var jumpRect = jumpButton.GetComponent<RectTransform>();
            jumpRect.anchorMin = new Vector2(0.5f, 0.5f);
            jumpRect.anchorMax = new Vector2(0.5f, 0.5f);
            jumpRect.pivot = new Vector2(0.5f, 0.5f);
            jumpRect.anchoredPosition = new Vector2(0f, 154f);
            jumpRect.sizeDelta = new Vector2(216f, 60f);

            var result = GetOrCreateText(
                card.transform,
                "Bouncy Jump Result Text",
                "빛나는 착지 구역과 겹칠 때 점프하세요!",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(58f, -564f),
                new Vector2(640f, 104f));
            result.horizontalOverflow = HorizontalWrapMode.Wrap;
            result.resizeTextForBestFit = true;
            result.resizeTextMinSize = 14;
            result.resizeTextMaxSize = 18;
            var cancel = GetOrCreateTopLeftButton(
                card.transform,
                "Bouncy Jump Cancel Button",
                "그만하기",
                new Vector2(718f, -594f),
                new Vector2(164f, 52f));
            var confirm = GetOrCreateTopLeftButton(
                card.transform,
                "Bouncy Jump Confirm Button",
                "확인",
                new Vector2(718f, -594f),
                new Vector2(164f, 52f));
            ApplyCareButtonStyle(cancel);
            ApplyCareButtonStyle(confirm);

            var controller = canvasTransform.GetComponent<BouncyJumpMiniGameController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<BouncyJumpMiniGameController>();
            }

            controller.Configure(
                overlay,
                playArea,
                marker,
                target,
                time,
                score,
                combo,
                result,
                jumpButton,
                cancel,
                confirm,
                milkroomUi,
                visualController,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsurePlayChoicePanel(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                PlayChoicePanelController.OverlayObjectName,
                new Color(0.08f, 0.06f, 0.12f, 0.72f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Play Choice Card",
                Vector2.zero,
                new Vector2(680f, 430f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(680f, 430f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.95f, 0.82f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Play Choice Title Text",
                "어떻게 놀아줄까요?",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(40f, -30f),
                new Vector2(600f, 50f));
            title.fontStyle = FontStyle.Bold;
            var status = GetOrCreateText(
                card.transform,
                "Play Choice Status Text",
                "놀이를 선택해 주세요.",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(60f, -94f),
                new Vector2(560f, 66f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            var milkDrop = GetOrCreateTopLeftButton(
                card.transform,
                "Play Choice Milk Drop Button",
                "우유방울 받기\n30초 반응 게임",
                new Vector2(64f, -184f),
                new Vector2(260f, 108f));
            var bouncy = GetOrCreateTopLeftButton(
                card.transform,
                "Play Choice Bouncy Jump Button",
                "말랑 점프\n25초 타이밍 게임",
                new Vector2(356f, -184f),
                new Vector2(260f, 108f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Play Choice Close Button",
                "닫기",
                new Vector2(252f, -330f),
                new Vector2(176f, 50f));
            ApplyCareButtonStyle(milkDrop);
            ApplyCareButtonStyle(bouncy);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<PlayChoicePanelController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<PlayChoicePanelController>();
            }

            controller.Configure(
                overlay,
                status,
                milkDrop,
                bouncy,
                close,
                canvasTransform.GetComponent<MilkDropMiniGameController>(),
                canvasTransform.GetComponent<BouncyJumpMiniGameController>(),
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureNewGameSetup(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                NewGameSetupController.OverlayObjectName,
                new Color(0.08f, 0.05f, 0.02f, 0.76f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "New Game Setup Card",
                Vector2.zero,
                new Vector2(980f, 650f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(980f, 650f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.85f, 1f);
                cardImage.raycastTarget = true;
            }

            var progress = GetOrCreateText(card.transform, "New Game Setup Progress Text", "새 게임 설정 · 1/2", 18,
                TextAnchor.MiddleCenter, new Vector2(54f, -28f), new Vector2(872f, 30f));
            progress.color = new Color(0.72f, 0.38f, 0.08f);
            progress.fontStyle = FontStyle.Bold;
            var title = GetOrCreateText(card.transform, "New Game Setup Title Text", "함께할 알을 골라 주세요", 32,
                TextAnchor.MiddleCenter, new Vector2(54f, -70f), new Vector2(872f, 50f));
            title.fontStyle = FontStyle.Bold;
            var body = GetOrCreateText(card.transform, "New Game Setup Body Text", "다섯 알은 서로 다른 초기 성향의 바탕을 가지고 있어요.", 18,
                TextAnchor.MiddleCenter, new Vector2(74f, -126f), new Vector2(832f, 54f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            var selection = GetOrCreateText(card.transform, "New Game Setup Selection Text", "아직 선택하지 않음", 17,
                TextAnchor.MiddleCenter, new Vector2(74f, -184f), new Vector2(832f, 44f));
            var status = GetOrCreateText(card.transform, "New Game Setup Status Text", string.Empty, 15,
                TextAnchor.MiddleCenter, new Vector2(74f, -522f), new Vector2(832f, 30f));
            status.color = new Color(0.72f, 0.16f, 0.1f);

            GameObject EnsureStep(string name)
            {
                var found = card.transform.Find(name);
                if (found != null) return found.gameObject;
                var root = new GameObject(name, typeof(RectTransform));
                root.transform.SetParent(card.transform, false);
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return root;
            }

            Button[] CreateChoices(Transform parent, string prefix, System.Collections.Generic.IReadOnlyList<Gameplay.NewGameSetup.NewGameSetupChoiceDefinition> choices, out Text[] labels)
            {
                var buttons = new Button[choices.Count];
                labels = new Text[choices.Count];
                for (var index = 0; index < choices.Count; index += 1)
                {
                    var row = index / 3;
                    var column = index % 3;
                    var x = 90f + column * 272f + (row == 1 ? 136f : 0f);
                    var y = -256f - row * 112f;
                    var button = GetOrCreateTopLeftButton(
                        parent,
                        $"{prefix} Option Button {index}",
                        choices[index].DisplayName,
                        new Vector2(x, y),
                        new Vector2(248f, 84f));
                    ApplyCareButtonStyle(button);
                    buttons[index] = button;
                    labels[index] = button.GetComponentInChildren<Text>(true);
                }

                return buttons;
            }

            var eggStep = EnsureStep("New Game Setup Egg Step");
            var milkStep = EnsureStep("New Game Setup Milk Step");
            var eggButtons = CreateChoices(eggStep.transform, "Egg", Gameplay.NewGameSetup.NewGameSetupCatalog.EggChoices, out var eggLabels);
            var milkButtons = CreateChoices(milkStep.transform, "First Milk", Gameplay.NewGameSetup.NewGameSetupCatalog.FirstMilkChoices, out var milkLabels);

            var back = GetOrCreateTopLeftButton(card.transform, "New Game Setup Back Button", "이전", new Vector2(62f, -574f), new Vector2(118f, 48f));
            var skip = GetOrCreateTopLeftButton(card.transform, "New Game Setup Skip Button", "건너뛰기", new Vector2(620f, -574f), new Vector2(126f, 48f));
            var primary = GetOrCreateTopLeftButton(card.transform, "New Game Setup Primary Button", "다음", new Vector2(762f, -574f), new Vector2(156f, 48f));
            ApplyCareButtonStyle(back);
            ApplyDangerButtonStyle(skip);
            ApplyCareButtonStyle(primary);

            var skipOverlay = GetOrCreateFullScreenOverlay(overlay.transform, "Skip New Game Setup Confirmation", new Color(0.08f, 0.05f, 0.02f, 0.74f));
            var skipCard = GetOrCreatePanel(skipOverlay.transform, "Skip New Game Setup Card", Vector2.zero, new Vector2(580f, 290f));
            ConfigureCenteredRect(skipCard.GetComponent<RectTransform>(), new Vector2(580f, 290f));
            GetOrCreateText(skipCard.transform, "Skip New Game Setup Title", "새 게임 설정을 건너뛰시겠습니까?", 26,
                TextAnchor.MiddleCenter, new Vector2(36f, -38f), new Vector2(508f, 52f)).fontStyle = FontStyle.Bold;
            GetOrCreateText(skipCard.transform, "Skip New Game Setup Body", "알과 첫 우유는 기본 성향으로 정해집니다.", 17,
                TextAnchor.MiddleCenter, new Vector2(54f, -108f), new Vector2(472f, 54f));
            var keep = GetOrCreateTopLeftButton(skipCard.transform, "Continue New Game Setup Button", "계속 진행", new Vector2(260f, -210f), new Vector2(130f, 48f));
            var confirmSkip = GetOrCreateTopLeftButton(skipCard.transform, "Confirm Skip New Game Setup Button", "건너뛰기", new Vector2(406f, -210f), new Vector2(130f, 48f));
            ApplyCareButtonStyle(keep);
            ApplyDangerButtonStyle(confirmSkip);

            var controller = canvasTransform.GetComponent<NewGameSetupController>();
            if (controller == null) controller = canvasTransform.gameObject.AddComponent<NewGameSetupController>();
            controller.Configure(
                overlay,
                eggStep,
                milkStep,
                progress,
                title,
                body,
                selection,
                status,
                eggButtons,
                eggLabels,
                milkButtons,
                milkLabels,
                back,
                primary,
                skip,
                skipOverlay,
                keep,
                confirmSkip,
                () => GameManager.Instance?.CurrentSave?.newGameSetup,
                state =>
                {
                    var manager = GameManager.Instance;
                    manager?.PersistNewGameSetup(state);
                    if (manager != null)
                    {
                        milkroomUi?.Bind(manager.CurrentSave);
                        visualController?.Bind(manager.CurrentTama);
                    }
                },
                _ => canvasTransform.GetComponent<FirstMeetingOnboardingController>()?.Refresh(),
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureCheeseTamaSpeechBubble(
            Transform canvasTransform,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var bubble = GetOrCreatePanel(
                canvasTransform,
                "CheeseTama Speech Bubble",
                Vector2.zero,
                new Vector2(380f, 122f));
            var bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
            bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRect.pivot = new Vector2(0.5f, 0f);
            bubbleRect.anchoredPosition = Vector2.zero;
            if (bubble.TryGetComponent(out Image bubbleImage))
            {
                bubbleImage.color = new Color(1f, 0.98f, 0.9f, 0.96f);
                bubbleImage.raycastTarget = false;
            }

            var tailRect = GetOrCreateRect(bubble.transform, "CheeseTama Speech Tail");
            tailRect.anchorMin = new Vector2(0.5f, 0f);
            tailRect.anchorMax = new Vector2(0.5f, 0f);
            tailRect.pivot = new Vector2(0.5f, 0.5f);
            tailRect.anchoredPosition = new Vector2(0f, -3f);
            tailRect.sizeDelta = new Vector2(26f, 26f);
            tailRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var tailImage = tailRect.GetComponent<Image>() ?? tailRect.gameObject.AddComponent<Image>();
            tailImage.color = new Color(1f, 0.98f, 0.9f, 0.96f);
            tailImage.raycastTarget = false;
            tailRect.SetAsFirstSibling();

            var text = GetOrCreateText(
                bubble.transform,
                "CheeseTama Speech Text",
                string.Empty,
                19,
                TextAnchor.MiddleCenter,
                new Vector2(24f, -18f),
                new Vector2(332f, 86f));
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 19;
            text.raycastTarget = false;

            var controller = canvasTransform.GetComponent<CheeseTamaSpeechBubbleController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<CheeseTamaSpeechBubbleController>();
            }

            controller.Configure(
                bubble,
                bubbleRect,
                text,
                canvasTransform.GetComponent<Canvas>(),
                visualController != null ? visualController.transform : null,
                Camera.main);
            controller.SetOffsets(new Vector3(0f, 1.45f, 0f), new Vector2(0f, 4f));

            var dialogueBridge = canvasTransform.GetComponent<CheeseTamaDialogueBridge>();
            if (dialogueBridge == null)
            {
                dialogueBridge = canvasTransform.gameObject.AddComponent<CheeseTamaDialogueBridge>();
            }

            dialogueBridge.Configure(
                controller,
                Application.isPlaying ? GameManager.Instance : null,
                canvasTransform);
            bubble.transform.SetSiblingIndex(Mathf.Min(4, canvasTransform.childCount - 1));
        }

        private static void EnsureLateGameFeatures(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var statusPanel = canvasTransform.Find("Status Panel");
            var entryParent = statusPanel != null ? statusPanel : canvasTransform;
            var entryTop = statusPanel != null ? -430f : -104f;

            var starOpen = GetOrCreateTopLeftButton(
                entryParent,
                "Open Star Legacy Button",
                "별빛 숙성",
                new Vector2(statusPanel != null ? 22f : 1500f, entryTop),
                new Vector2(96f, 34f));
            var bondOpen = GetOrMoveProfileEntryButton(
                canvasTransform,
                entryParent,
                "Open Bond Status Button",
                "우리 사이",
                3);
            var careerOpen = GetOrCreateTopLeftButton(
                entryParent,
                "Open Hidden Career Button",
                "특별 기록",
                new Vector2(statusPanel != null ? 230f : 1708f, entryTop),
                new Vector2(106f, 34f));
            ApplyCareButtonStyle(starOpen);
            ApplyCareButtonStyle(bondOpen);
            ApplyCareButtonStyle(careerOpen);

            var starOverlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                StarLegacyPanelController.OverlayObjectName,
                new Color(0.04f, 0.035f, 0.16f, 0.84f));
            var starCard = GetOrCreatePanel(
                starOverlay.transform,
                "Star Legacy Card",
                Vector2.zero,
                new Vector2(780f, 620f));
            ConfigureCenteredRect(starCard.GetComponent<RectTransform>(), new Vector2(780f, 620f));
            if (starCard.TryGetComponent(out Image starCardImage))
            {
                starCardImage.color = new Color(0.94f, 0.92f, 1f, 1f);
                starCardImage.raycastTarget = true;
            }

            var starTitle = GetOrCreateText(
                starCard.transform,
                "Star Legacy Title Text",
                "별빛 숙성",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(54f, -36f),
                new Vector2(672f, 54f));
            starTitle.fontStyle = FontStyle.Bold;
            var starRoute = GetOrCreateText(
                starCard.transform,
                "Star Legacy Route Text",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                new Vector2(76f, -108f),
                new Vector2(628f, 92f));
            starRoute.horizontalOverflow = HorizontalWrapMode.Wrap;
            var starSlider = GetOrCreateSettingsSlider(
                starCard.transform,
                "Star Legacy Maturation Slider",
                new Vector2(116f, -226f),
                new Vector2(548f, 30f),
                0f,
                FinalMaturationCycleSystem.RequiredProgress,
                true);
            starSlider.interactable = false;
            var maturationText = GetOrCreateText(
                starCard.transform,
                "Star Legacy Maturation Text",
                "최종형 숙성 0/100",
                19,
                TextAnchor.MiddleCenter,
                new Vector2(80f, -270f),
                new Vector2(620f, 40f));
            var rewardText = GetOrCreateText(
                starCard.transform,
                "Star Legacy Reward Text",
                "받을 숙성 보상이 없습니다.",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(80f, -320f),
                new Vector2(620f, 72f));
            rewardText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var starStatus = GetOrCreateText(
                starCard.transform,
                "Star Legacy Status Text",
                string.Empty,
                17,
                TextAnchor.MiddleCenter,
                new Vector2(80f, -398f),
                new Vector2(620f, 58f));
            starStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            var starEgg = GetOrCreateTopLeftButton(
                starCard.transform,
                "Begin Star Egg Button",
                "별빛 알 만나기",
                new Vector2(64f, -494f),
                new Vector2(176f, 54f));
            var evolve = GetOrCreateTopLeftButton(
                starCard.transform,
                "Emmental Evolution Button",
                "빛 이어보기",
                new Vector2(256f, -494f),
                new Vector2(154f, 54f));
            var claim = GetOrCreateTopLeftButton(
                starCard.transform,
                "Final Maturation Claim Button",
                "숙성 보상",
                new Vector2(426f, -494f),
                new Vector2(140f, 54f));
            var starClose = GetOrCreateTopLeftButton(
                starCard.transform,
                "Star Legacy Close Button",
                "닫기",
                new Vector2(582f, -494f),
                new Vector2(134f, 54f));
            ApplyCareButtonStyle(starEgg);
            ApplyCareButtonStyle(evolve);
            ApplyCareButtonStyle(claim);
            ApplyCareButtonStyle(starClose);

            var starController = canvasTransform.GetComponent<StarLegacyPanelController>()
                ?? canvasTransform.gameObject.AddComponent<StarLegacyPanelController>();
            var lateBridge = canvasTransform.GetComponent<LateGameFeatureBridge>()
                ?? canvasTransform.gameObject.AddComponent<LateGameFeatureBridge>();
            starController.Configure(
                starOverlay,
                starTitle,
                starRoute,
                starSlider,
                maturationText,
                rewardText,
                starStatus,
                evolve,
                claim,
                starClose,
                starOpen,
                () => GameManager.Instance?.GetStarLegacyViewModel()
                    ?? StarLegacyPanelViewModel.Hidden(),
                () => GameManager.Instance != null
                    ? GameManager.Instance.TryEvolveEmmental()
                    : default,
                () => GameManager.Instance != null
                    ? GameManager.Instance.ClaimFinalMaturationReward()
                    : default,
                lateBridge.SetStarPanelBlocking);

            var bondOverlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Bond Status Overlay",
                new Color(0.09f, 0.04f, 0.06f, 0.78f));
            var bondCard = GetOrCreatePanel(
                bondOverlay.transform,
                "Bond Status Card",
                Vector2.zero,
                new Vector2(680f, 470f));
            ConfigureCenteredRect(bondCard.GetComponent<RectTransform>(), new Vector2(680f, 470f));
            if (bondCard.TryGetComponent(out Image bondCardImage))
            {
                bondCardImage.color = new Color(1f, 0.94f, 0.93f, 1f);
                bondCardImage.raycastTarget = true;
            }

            var bondTitle = GetOrCreateText(
                bondCard.transform,
                "Bond Status Title Text",
                "치즈타마와 우리 사이",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -38f),
                new Vector2(584f, 54f));
            bondTitle.fontStyle = FontStyle.Bold;
            var relationship = GetOrCreateText(
                bondCard.transform,
                "Bond Relationship Text",
                string.Empty,
                23,
                TextAnchor.MiddleCenter,
                new Vector2(70f, -122f),
                new Vector2(540f, 48f));
            var trait = GetOrCreateText(
                bondCard.transform,
                "Bond Trait Text",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                new Vector2(70f, -190f),
                new Vector2(540f, 44f));
            var preference = GetOrCreateText(
                bondCard.transform,
                "Bond Preference Text",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                new Vector2(76f, -250f),
                new Vector2(528f, 84f));
            preference.horizontalOverflow = HorizontalWrapMode.Wrap;
            var bondClose = GetOrCreateTopLeftButton(
                bondCard.transform,
                "Bond Status Close Button",
                "닫기",
                new Vector2(250f, -382f),
                new Vector2(180f, 54f));
            ApplyCareButtonStyle(bondClose);
            var bondController = canvasTransform.GetComponent<BondStatusPanelController>()
                ?? canvasTransform.gameObject.AddComponent<BondStatusPanelController>();
            bondController.Configure(
                bondOverlay,
                relationship,
                trait,
                preference,
                bondOpen,
                bondClose);

            var careerOverlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Hidden Career Card Overlay",
                new Color(0.035f, 0.05f, 0.09f, 0.82f));
            var careerCard = GetOrCreatePanel(
                careerOverlay.transform,
                "Hidden Career Card",
                Vector2.zero,
                new Vector2(860f, 700f));
            ConfigureCenteredRect(careerCard.GetComponent<RectTransform>(), new Vector2(860f, 700f));
            if (careerCard.TryGetComponent(out Image careerCardImage))
            {
                careerCardImage.color = new Color(0.92f, 0.96f, 1f, 1f);
                careerCardImage.raycastTarget = true;
            }

            var careerTitle = GetOrCreateText(
                careerCard.transform,
                "Hidden Career Title Text",
                string.Empty,
                31,
                TextAnchor.MiddleCenter,
                new Vector2(54f, -34f),
                new Vector2(752f, 54f));
            careerTitle.fontStyle = FontStyle.Bold;
            var careerViewport = GetOrCreatePanel(
                careerCard.transform,
                "Hidden Career Viewport",
                new Vector2(52f, -104f),
                new Vector2(756f, 490f));
            if (careerViewport.TryGetComponent(out Image careerViewportImage))
            {
                careerViewportImage.color = new Color(0.975f, 0.99f, 1f, 0.96f);
            }

            if (careerViewport.GetComponent<RectMask2D>() == null)
            {
                careerViewport.AddComponent<RectMask2D>();
            }

            var careerContent = GetOrCreateRect(careerViewport.transform, "Hidden Career Content");
            careerContent.anchorMin = new Vector2(0f, 1f);
            careerContent.anchorMax = new Vector2(1f, 1f);
            careerContent.pivot = new Vector2(0.5f, 1f);
            careerContent.anchoredPosition = new Vector2(0f, -20f);
            careerContent.sizeDelta = new Vector2(-48f, 0f);
            var careerText = careerContent.GetComponent<Text>()
                ?? careerContent.gameObject.AddComponent<Text>();
            careerText.font = GetDefaultFont();
            careerText.fontSize = 18;
            careerText.alignment = TextAnchor.UpperLeft;
            careerText.color = new Color(0.16f, 0.2f, 0.3f);
            careerText.raycastTarget = false;
            careerText.horizontalOverflow = HorizontalWrapMode.Wrap;
            careerText.verticalOverflow = VerticalWrapMode.Overflow;
            var careerFitter = careerContent.GetComponent<ContentSizeFitter>()
                ?? careerContent.gameObject.AddComponent<ContentSizeFitter>();
            careerFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            careerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var careerScroll = careerViewport.GetComponent<ScrollRect>()
                ?? careerViewport.AddComponent<ScrollRect>();
            careerScroll.viewport = careerViewport.GetComponent<RectTransform>();
            careerScroll.content = careerContent;
            careerScroll.horizontal = false;
            careerScroll.vertical = true;
            careerScroll.movementType = ScrollRect.MovementType.Clamped;
            careerScroll.scrollSensitivity = 34f;
            var careerClose = GetOrCreateTopLeftButton(
                careerCard.transform,
                "Hidden Career Close Button",
                "닫기",
                new Vector2(340f, -624f),
                new Vector2(180f, 54f));
            ApplyCareButtonStyle(careerClose);
            var careerController = canvasTransform.GetComponent<HiddenCareerCardPanelController>()
                ?? canvasTransform.gameObject.AddComponent<HiddenCareerCardPanelController>();
            careerController.Configure(
                careerOverlay,
                careerTitle,
                careerText,
                careerOpen,
                careerClose);

            var bubble = canvasTransform.GetComponent<CheeseTamaSpeechBubbleController>();
            var reactionPresenter = canvasTransform.GetComponent<BondReactionPresenter>()
                ?? canvasTransform.gameObject.AddComponent<BondReactionPresenter>();
            reactionPresenter.Configure(bubble, visualController);

            EmmentalConstellationPresenter constellation = null;
            if (visualController != null)
            {
                constellation = visualController.GetComponent<EmmentalConstellationPresenter>()
                    ?? visualController.gameObject.AddComponent<EmmentalConstellationPresenter>();
                constellation.Configure(visualController.transform, 1f);
            }

            lateBridge.Configure(
                Application.isPlaying ? GameManager.Instance : null,
                starController,
                bondController,
                careerController,
                reactionPresenter,
                constellation,
                visualController,
                milkroomUi,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>(),
                starEgg,
                starStatus,
                starOpen,
                starClose,
                bondOpen,
                bondClose,
                careerOpen,
                careerClose);

            starOverlay.transform.SetAsLastSibling();
            bondOverlay.transform.SetAsLastSibling();
            careerOverlay.transform.SetAsLastSibling();
        }

        private static void EnsureGrowthJourney(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                GrowthJourneyController.OverlayObjectName,
                new Color(0.04f, 0.04f, 0.14f, 0.82f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Growth Journey Card",
                Vector2.zero,
                new Vector2(780f, 560f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(780f, 560f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.95f, 0.93f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Growth Journey Title Text",
                "치즈타마 성장 여정",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(54f, -34f),
                new Vector2(672f, 54f));
            title.fontStyle = FontStyle.Bold;
            var level = GetOrCreateText(
                card.transform,
                "Growth Journey Level Text",
                "성장 레벨  1/33",
                24,
                TextAnchor.MiddleLeft,
                new Vector2(80f, -122f),
                new Vector2(620f, 44f));
            var milk = GetOrCreateText(
                card.transform,
                "Growth Journey Milk Text",
                "주요 우유 완전 성장  0/7",
                24,
                TextAnchor.MiddleLeft,
                new Vector2(80f, -184f),
                new Vector2(620f, 44f));
            var goal = GetOrCreateText(
                card.transform,
                "Growth Journey Goal Text",
                "다음 성장 목표를 확인하는 중입니다.",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(80f, -256f),
                new Vector2(620f, 94f));
            goal.horizontalOverflow = HorizontalWrapMode.Wrap;
            var unlock = GetOrCreateText(
                card.transform,
                "Growth Journey Unlock Text",
                "Lv.33과 주요 우유 완전 성장을 모두 달성하면 별빛 루트가 열립니다.",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(80f, -368f),
                new Vector2(620f, 74f));
            unlock.horizontalOverflow = HorizontalWrapMode.Wrap;
            unlock.fontStyle = FontStyle.Bold;
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Growth Journey Close Button",
                "확인",
                new Vector2(296f, -468f),
                new Vector2(188f, 52f));
            ApplyCareButtonStyle(close);

            var statusPanel = canvasTransform.Find("Status Panel");
            var recordTitle = statusPanel != null
                ? statusPanel.Find("Detail Title Text") as RectTransform
                : null;
            if (recordTitle != null)
            {
                recordTitle.sizeDelta = new Vector2(316f, recordTitle.sizeDelta.y);
            }
            var open = GetOrMoveProfileEntryButton(
                canvasTransform,
                statusPanel,
                "Open Growth Journey Button",
                "성장 여정",
                1);
            ApplyCareButtonStyle(open);

            var controller = canvasTransform.GetComponent<GrowthJourneyController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<GrowthJourneyController>();
            }

            controller.Configure(
                overlay,
                title,
                level,
                milk,
                goal,
                unlock,
                close,
                open,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureMemoryJournal(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            const string overlayName = "Memory Journal Overlay";
            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                overlayName,
                new Color(0.07f, 0.05f, 0.03f, 0.78f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Memory Journal Card",
                Vector2.zero,
                new Vector2(900f, 760f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(900f, 760f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.965f, 0.86f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Memory Journal Title Text",
                "치즈타마 추억일기",
                32,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -34f),
                new Vector2(530f, 52f));
            title.fontStyle = FontStyle.Bold;
            var unread = GetOrCreateText(
                card.transform,
                "Memory Journal Unread Text",
                "모두 읽음",
                18,
                TextAnchor.MiddleRight,
                new Vector2(620f, -42f),
                new Vector2(220f, 38f));
            unread.color = new Color(0.72f, 0.39f, 0.1f);

            var viewportObject = GetOrCreatePanel(
                card.transform,
                "Memory Journal Viewport",
                new Vector2(48f, -108f),
                new Vector2(804f, 500f));
            if (viewportObject.TryGetComponent(out Image viewportImage))
            {
                viewportImage.color = new Color(1f, 0.985f, 0.93f, 0.94f);
            }

            var viewportRect = viewportObject.GetComponent<RectTransform>();
            if (viewportObject.GetComponent<RectMask2D>() == null)
            {
                viewportObject.AddComponent<RectMask2D>();
            }

            var contentRect = GetOrCreateRect(viewportObject.transform, "Memory Journal Content");
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, -14f);
            contentRect.sizeDelta = new Vector2(-44f, 0f);
            var entries = contentRect.GetComponent<Text>() ?? contentRect.gameObject.AddComponent<Text>();
            entries.font = GetDefaultFont();
            entries.fontSize = 18;
            entries.alignment = TextAnchor.UpperLeft;
            entries.color = new Color(0.25f, 0.18f, 0.12f);
            entries.raycastTarget = false;
            entries.supportRichText = true;
            entries.horizontalOverflow = HorizontalWrapMode.Wrap;
            entries.verticalOverflow = VerticalWrapMode.Overflow;
            var contentFitter = contentRect.GetComponent<ContentSizeFitter>()
                ?? contentRect.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewportObject.GetComponent<ScrollRect>()
                ?? viewportObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;

            var empty = GetOrCreateText(
                viewportObject.transform,
                "Memory Journal Empty Text",
                "아직 기록된 추억이 없어요.\n함께 돌보고 놀아주며 첫 장을 채워보세요.",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(70f, -170f),
                new Vector2(664f, 140f));
            empty.horizontalOverflow = HorizontalWrapMode.Wrap;

            var markRead = GetOrCreateTopLeftButton(
                card.transform,
                "Memory Journal Mark Read Button",
                "모두 읽음",
                new Vector2(500f, -656f),
                new Vector2(160f, 54f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Memory Journal Close Button",
                "닫기",
                new Vector2(688f, -656f),
                new Vector2(164f, 54f));
            ApplyCareButtonStyle(markRead);
            ApplyCareButtonStyle(close);

            var statusPanel = canvasTransform.Find("Status Panel");
            var open = GetOrMoveProfileEntryButton(
                canvasTransform,
                statusPanel,
                "Open Memory Journal Button",
                "추억일기",
                2);
            ApplyCareButtonStyle(open);

            var manager = Application.isPlaying ? GameManager.Instance : null;
            var controller = canvasTransform.GetComponent<MemoryJournalPanelController>()
                ?? canvasTransform.gameObject.AddComponent<MemoryJournalPanelController>();
            controller.Configure(
                overlay,
                title,
                unread,
                entries,
                empty,
                markRead,
                close,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            controller.BindProvider(
                () => GameManager.Instance?.CurrentSave?.memoryJournal,
                _ => GameManager.Instance?.SaveGame(),
                unlockId => IsMemoryJournalUnlockAvailable(GameManager.Instance, unlockId));
            open.onClick.RemoveAllListeners();
            open.onClick.AddListener(controller.Open);

            var bubble = canvasTransform.GetComponent<CheeseTamaSpeechBubbleController>();
            if (bubble != null)
            {
                var bridge = canvasTransform.GetComponent<MemoryJournalRecallBridge>()
                    ?? canvasTransform.gameObject.AddComponent<MemoryJournalRecallBridge>();
                bridge.Configure(bubble, manager, canvasTransform);
            }

            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureCheeseStarDelivery(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            const string overlayName = "Cheese Star Delivery Overlay";
            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                overlayName,
                new Color(0.07f, 0.05f, 0.03f, 0.78f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Cheese Star Delivery Card",
                Vector2.zero,
                new Vector2(680f, 560f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(680f, 560f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.95f, 0.78f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Delivery Title Text",
                "오늘의 배달",
                34,
                TextAnchor.MiddleCenter,
                new Vector2(44f, -38f),
                new Vector2(592f, 58f));
            title.fontStyle = FontStyle.Bold;
            var streak = GetOrCreateText(
                card.transform,
                "Delivery Streak Text",
                "연속 1일째 · 보상 1일차",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(58f, -108f),
                new Vector2(564f, 40f));
            streak.color = new Color(0.72f, 0.4f, 0.1f);
            var rewardPanel = GetOrCreatePanel(
                card.transform,
                "Delivery Reward Panel",
                new Vector2(104f, -174f),
                new Vector2(472f, 164f));
            if (rewardPanel.TryGetComponent(out Image rewardImage))
            {
                rewardImage.color = new Color(1f, 0.985f, 0.91f, 1f);
            }

            var reward = GetOrCreateText(
                rewardPanel.transform,
                "Delivery Reward Text",
                "우유 코인 +20\n우유방울 +3",
                24,
                TextAnchor.MiddleCenter,
                new Vector2(36f, -22f),
                new Vector2(400f, 120f));
            reward.horizontalOverflow = HorizontalWrapMode.Wrap;
            var note = GetOrCreateText(
                card.transform,
                "Delivery Note Text",
                "오늘 찾아온 포근한 선물이에요.",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(62f, -360f),
                new Vector2(556f, 62f));
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            var later = GetOrCreateTopLeftButton(
                card.transform,
                "Delivery Later Button",
                "나중에",
                new Vector2(160f, -460f),
                new Vector2(160f, 54f));
            var claim = GetOrCreateTopLeftButton(
                card.transform,
                "Delivery Claim Button",
                "선물 받기",
                new Vector2(360f, -460f),
                new Vector2(160f, 54f));
            ApplyCareButtonStyle(later);
            ApplyCareButtonStyle(claim);

            var cardController = canvasTransform.GetComponent<CheeseStarDeliveryCardController>()
                ?? canvasTransform.gameObject.AddComponent<CheeseStarDeliveryCardController>();
            cardController.Configure(
                overlay,
                title,
                streak,
                reward,
                note,
                claim,
                later);

            var manager = Application.isPlaying ? GameManager.Instance : null;
            var bridge = canvasTransform.GetComponent<CheeseStarDeliveryBridge>()
                ?? canvasTransform.gameObject.AddComponent<CheeseStarDeliveryBridge>();
            bridge.Configure(
                cardController,
                manager,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>(),
                canvasTransform);

            var careTipPanel = canvasTransform.Find("Care Tip Panel");
            var open = GetOrMoveUtilityButton(
                canvasTransform,
                careTipPanel,
                "Open Delivery Button",
                "오늘배달",
                new Vector2(0f, -48f),
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);
            open.onClick.RemoveAllListeners();
            open.onClick.AddListener(() =>
            {
                var liveManager = GameManager.Instance;
                if (liveManager != null)
                {
                    bridge.TryShowOffer(liveManager.ObserveCheeseStarDelivery());
                }
            });
            bridge.BindEntryButton(open);

            if (careTipPanel != null)
            {
                var titleRect = careTipPanel.Find("Care Tip Title Text") as RectTransform;
                if (titleRect != null)
                {
                    titleRect.gameObject.SetActive(true);
                    titleRect.anchoredPosition = new Vector2(22f, -16f);
                    titleRect.sizeDelta = new Vector2(306f, 30f);
                    if (titleRect.TryGetComponent(out Text titleText))
                    {
                        titleText.text = "돌봄 팁";
                    }
                }
            }

            overlay.transform.SetAsLastSibling();
        }

        private static bool IsMemoryJournalUnlockAvailable(GameManager manager, string unlockId)
        {
            var unlocks = manager?.CurrentSave?.unlocks;
            if (unlocks == null || string.IsNullOrWhiteSpace(unlockId))
            {
                return false;
            }

            return unlockId switch
            {
                "star" or "star_route" or "star_milk" => unlocks.starMilkUnlocked,
                "fantasy_powder" => unlocks.fantasyPowderEnabled,
                _ => false
            };
        }

        private static void EnsureFantasyPowderHiddenRecipes(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            const string overlayName = "Fantasy Powder Overlay";
            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                overlayName,
                new Color(0.08f, 0.04f, 0.13f, 0.8f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Fantasy Powder Card",
                Vector2.zero,
                new Vector2(920f, 700f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(920f, 700f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.98f, 0.94f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Fantasy Powder Title Text",
                "환상가루 비밀 조합",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -30f),
                new Vector2(824f, 54f));
            title.fontStyle = FontStyle.Bold;
            var powder = GetOrCreateText(
                card.transform,
                "Fantasy Powder Quantity Text",
                "보유 수량 0",
                19,
                TextAnchor.MiddleLeft,
                new Vector2(60f, -92f),
                new Vector2(280f, 38f));
            var attempts = GetOrCreateText(
                card.transform,
                "Fantasy Powder Attempts Text",
                "시도 0회 · 단서 0/3",
                19,
                TextAnchor.MiddleRight,
                new Vector2(572f, -92f),
                new Vector2(288f, 38f));
            var hint = GetOrCreateText(
                card.transform,
                "Fantasy Powder Hint Text",
                "아직 분명한 단서는 없어요.",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(90f, -136f),
                new Vector2(740f, 52f));
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;

            var recipeNames = new Text[3];
            var recipeStates = new Text[3];
            var recipeButtons = new Button[3];
            for (var index = 0; index < 3; index += 1)
            {
                var y = -210f - index * 84f;
                recipeButtons[index] = GetOrCreateTopLeftButton(
                    card.transform,
                    $"Fantasy Recipe Button {index}",
                    "선택",
                    new Vector2(62f, y),
                    new Vector2(118f, 58f));
                ApplyCareButtonStyle(recipeButtons[index]);
                recipeNames[index] = GetOrCreateText(
                    card.transform,
                    $"Fantasy Recipe Name Text {index}",
                    $"미지의 조합 {index + 1}",
                    20,
                    TextAnchor.MiddleLeft,
                    new Vector2(202f, y + 3f),
                    new Vector2(430f, 48f));
                recipeStates[index] = GetOrCreateText(
                    card.transform,
                    $"Fantasy Recipe State Text {index}",
                    "미발견",
                    17,
                    TextAnchor.MiddleRight,
                    new Vector2(664f, y + 3f),
                    new Vector2(190f, 48f));
            }

            var detail = GetOrCreateText(
                card.transform,
                "Fantasy Powder Detail Text",
                "표시할 조합이 없어요.",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(76f, -472f),
                new Vector2(768f, 82f));
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            var status = GetOrCreateText(
                card.transform,
                "Fantasy Powder Status Text",
                string.Empty,
                17,
                TextAnchor.MiddleCenter,
                new Vector2(76f, -554f),
                new Vector2(768f, 52f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            var attempt = GetOrCreateTopLeftButton(
                card.transform,
                "Fantasy Powder Attempt Button",
                "가루 1개로 시도",
                new Vector2(494f, -622f),
                new Vector2(188f, 54f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Fantasy Powder Close Button",
                "닫기",
                new Vector2(704f, -622f),
                new Vector2(150f, 54f));
            ApplyCareButtonStyle(attempt);
            ApplyCareButtonStyle(close);

            var careTipPanel = canvasTransform.Find("Care Tip Panel");
            var open = GetOrMoveUtilityButton(
                canvasTransform,
                careTipPanel,
                "Open Fantasy Powder Button",
                "비밀조합",
                new Vector2(112f, 0f),
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);

            var controller = canvasTransform.GetComponent<FantasyPowderHiddenRecipePanelController>()
                ?? canvasTransform.gameObject.AddComponent<FantasyPowderHiddenRecipePanelController>();
            controller.Configure(
                overlay,
                powder,
                attempts,
                hint,
                detail,
                status,
                recipeNames,
                recipeStates,
                recipeButtons,
                attempt,
                close,
                () => GameManager.Instance?.GetFantasyPowderSnapshot()
                    ?? Gameplay.HiddenRecipes.FantasyPowderPanelSnapshot.CreateHidden(),
                recipeId => GameManager.Instance?.TryAttemptFantasyPowderRecipe(recipeId),
                null,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            var manager = GameManager.Instance;
            controller.BindEntryButton(open, manager);
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureFirstDayJourney(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                FirstDayJourneyController.OverlayObjectName,
                new Color(0.08f, 0.06f, 0.03f, 0.78f));
            var card = GetOrCreatePanel(
                overlay.transform,
                FirstDayJourneyController.CardObjectName,
                Vector2.zero,
                new Vector2(720f, 660f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(720f, 660f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.86f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "First Day Journey Title Text",
                "첫날 여정",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -30f),
                new Vector2(624f, 52f));
            title.fontStyle = FontStyle.Bold;
            var progress = GetOrCreateText(
                card.transform,
                "First Day Journey Progress Text",
                "첫날 여정  0/6",
                19,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -84f),
                new Vector2(624f, 36f));
            progress.color = new Color(0.67f, 0.36f, 0.08f);

            var taskTexts = new Text[Gameplay.Journey.FirstDayJourneySystem.Tasks.Count];
            for (var index = 0; index < taskTexts.Length; index += 1)
            {
                taskTexts[index] = GetOrCreateText(
                    card.transform,
                    $"First Day Journey Task Text {index}",
                    $"○ {Gameplay.Journey.FirstDayJourneySystem.Tasks[index].DisplayName}",
                    20,
                    TextAnchor.MiddleLeft,
                    new Vector2(110f, -142f - index * 58f),
                    new Vector2(500f, 44f));
            }

            var status = GetOrCreateText(
                card.transform,
                "First Day Journey Status Text",
                "정해진 순서 없이 천천히 경험해도 괜찮아요.",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(70f, -500f),
                new Vector2(580f, 58f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "First Day Journey Close Button",
                "확인",
                new Vector2(275f, -590f),
                new Vector2(170f, 52f));
            var claim = GetOrCreateTopLeftButton(
                card.transform,
                "First Day Journey Claim Button",
                "첫날 선물 받기",
                new Vector2(275f, -532f),
                new Vector2(170f, 52f));
            ApplyCareButtonStyle(close);
            ApplyCareButtonStyle(claim);

            var profileEntries = GetProfileMenuEntryParent(canvasTransform);
            var open = GetOrMoveUtilityButton(
                canvasTransform,
                profileEntries,
                "Open First Day Journey Button",
                "첫날 여정",
                Vector2.zero,
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);

            var manager = Application.isPlaying ? GameManager.Instance : null;
            var controller = canvasTransform.GetComponent<FirstDayJourneyController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<FirstDayJourneyController>();
            }

            controller.Configure(
                overlay,
                open,
                progress,
                status,
                taskTexts,
                claim,
                close,
                () => GameManager.Instance?.CurrentSave?.firstDayJourney,
                () => GameManager.Instance?.MarkFirstDayJourneyShown(),
                () => GameManager.Instance != null
                    ? GameManager.Instance.ClaimFirstDayJourneyReward()
                    : default,
                manager,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureEvolutionMilestone(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Evolution Achievement Overlay",
                new Color(0.11f, 0.05f, 0.16f, 0.78f));
            var card = GetOrCreatePanel(overlay.transform, "Evolution Achievement Card", Vector2.zero, new Vector2(760f, 530f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(760f, 530f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.98f, 0.92f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(card.transform, "Evolution Achievement Title Text", "새로운 진화!", 34,
                TextAnchor.MiddleCenter, new Vector2(54f, -42f), new Vector2(652f, 58f));
            title.fontStyle = FontStyle.Bold;
            var emblem = GetOrCreateText(card.transform, "Evolution Achievement Emblem Text", "✦", 88,
                TextAnchor.MiddleCenter, new Vector2(250f, -112f), new Vector2(260f, 130f));
            emblem.color = new Color(0.66f, 0.4f, 0.85f, 1f);
            var level = GetOrCreateText(card.transform, "Evolution Achievement Level Text", "Lv.21 · 일반 진화 달성", 21,
                TextAnchor.MiddleCenter, new Vector2(64f, -258f), new Vector2(632f, 38f));
            level.fontStyle = FontStyle.Bold;
            var description = GetOrCreateText(card.transform, "Evolution Achievement Description Text", "돌봄의 추억이 새로운 모습으로 이어졌어요.", 19,
                TextAnchor.MiddleCenter, new Vector2(70f, -314f), new Vector2(620f, 118f));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 14;
            description.resizeTextMaxSize = 19;
            var confirm = GetOrCreateTopLeftButton(card.transform, "Evolution Achievement Confirm Button", "새 모습 만나기",
                new Vector2(514f, -450f), new Vector2(192f, 54f));
            ApplyCareButtonStyle(confirm);

            var controller = canvasTransform.GetComponent<EvolutionMilestoneController>()
                ?? canvasTransform.gameObject.AddComponent<EvolutionMilestoneController>();
            controller.Configure(
                overlay,
                title,
                level,
                description,
                confirm,
                milkroomUi,
                visualController,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureCleaningMiniGame(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                CleaningMiniGameController.OverlayObjectName,
                new Color(0.07f, 0.12f, 0.11f, 0.8f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Cleaning Mini Game Card",
                Vector2.zero,
                new Vector2(940f, 730f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(940f, 730f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.94f, 0.99f, 0.94f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(card.transform, "Cleaning Title Text", "반짝반짝 청소", 32,
                TextAnchor.MiddleCenter, new Vector2(42f, -24f), new Vector2(856f, 50f));
            title.fontStyle = FontStyle.Bold;
            var timeText = GetOrCreateText(card.transform, "Cleaning Time Text", "남은 시간  24초", 20,
                TextAnchor.MiddleLeft, new Vector2(54f, -80f), new Vector2(270f, 36f));
            var scoreText = GetOrCreateText(card.transform, "Cleaning Score Text", "점수  0", 20,
                TextAnchor.MiddleCenter, new Vector2(334f, -80f), new Vector2(250f, 36f));
            var progressText = GetOrCreateText(card.transform, "Cleaning Progress Text", "닦음 0 · 놓침 0", 20,
                TextAnchor.MiddleRight, new Vector2(590f, -80f), new Vector2(296f, 36f));
            var playAreaObject = GetOrCreatePanel(card.transform, "Cleaning Play Area", new Vector2(54f, -128f), new Vector2(832f, 430f));
            var playArea = playAreaObject.GetComponent<RectTransform>();
            if (playAreaObject.TryGetComponent(out Image playAreaImage))
            {
                playAreaImage.color = new Color(0.72f, 0.88f, 0.78f, 0.58f);
                playAreaImage.raycastTarget = true;
            }

            if (playAreaObject.GetComponent<RectMask2D>() == null)
            {
                playAreaObject.AddComponent<RectMask2D>();
            }

            var spotTemplate = GetOrCreateButton(playArea, "Dirt Spot Template", "✦", Vector2.zero,
                Vector2.one * CleaningMiniGameRules.SpotSizePixels);
            ApplyCareButtonStyle(spotTemplate);
            if (spotTemplate.TryGetComponent(out Image spotImage))
            {
                spotImage.sprite = null;
                spotImage.color = new Color(0.42f, 0.27f, 0.14f, 0.94f);
            }

            var resultText = GetOrCreateText(card.transform, "Cleaning Result Text",
                "얼룩을 눌러 밀크룸을 반짝이게 닦아 주세요.", 18, TextAnchor.MiddleCenter,
                new Vector2(54f, -574f), new Vector2(626f, 96f));
            resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultText.resizeTextForBestFit = true;
            resultText.resizeTextMinSize = 14;
            resultText.resizeTextMaxSize = 18;
            var cancelButton = GetOrCreateTopLeftButton(card.transform, "Cleaning Cancel Button", "그만하기",
                new Vector2(704f, -598f), new Vector2(182f, 54f));
            var confirmButton = GetOrCreateTopLeftButton(card.transform, "Cleaning Confirm Button", "밀크룸으로",
                new Vector2(704f, -598f), new Vector2(182f, 54f));
            ApplyCareButtonStyle(cancelButton);
            ApplyCareButtonStyle(confirmButton);

            var controller = canvasTransform.GetComponent<CleaningMiniGameController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<CleaningMiniGameController>();
            }

            controller.Configure(
                overlay,
                playArea,
                spotTemplate,
                timeText,
                scoreText,
                progressText,
                resultText,
                cancelButton,
                confirmButton,
                milkroomUi,
                visualController,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureDecorationShop(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var decorateOverlay = canvasTransform.Find("Decorate Overlay");
            if (decorateOverlay == null)
            {
                return;
            }

            var shopRoot = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Decoration Shop Overlay",
                new Color(0.1f, 0.06f, 0.02f, 0.7f));
            var card = GetOrCreatePanel(shopRoot.transform, "Decoration Shop Card", Vector2.zero, new Vector2(1180f, 820f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(1180f, 820f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.85f, 1f);
            }

            var heading = GetOrCreateText(card.transform, "Decoration Shop Title Text", "밀크룸 장식 상점", 30,
                TextAnchor.MiddleLeft, new Vector2(42f, -24f), new Vector2(480f, 50f));
            heading.fontStyle = FontStyle.Bold;
            var balance = GetOrCreateText(card.transform, "Decoration Shop Balance Text", "코인 0 · 우유방울 0", 18,
                TextAnchor.MiddleRight, new Vector2(540f, -28f), new Vector2(452f, 42f));

            var itemNames = new Text[DecorationCatalog.All.Length];
            var itemStates = new Text[DecorationCatalog.All.Length];
            var itemButtons = new Button[DecorationCatalog.All.Length];
            for (var index = 0; index < DecorationCatalog.All.Length; index += 1)
            {
                var column = index % 3;
                var row = index / 3;
                var x = 42f + column * 286f;
                var y = 96f + row * 92f;
                itemButtons[index] = GetOrCreateTopLeftButton(card.transform,
                    $"Decoration Item {index + 1} Button", string.Empty, new Vector2(x, -y), new Vector2(268f, 78f));
                itemNames[index] = GetOrCreateText(itemButtons[index].transform, "Item Name Text",
                    DecorationCatalog.All[index].displayName, 17, TextAnchor.UpperLeft,
                    new Vector2(14f, -8f), new Vector2(238f, 28f));
                itemStates[index] = GetOrCreateText(itemButtons[index].transform, "Item State Text", "보유 상태", 13,
                    TextAnchor.LowerLeft, new Vector2(14f, -40f), new Vector2(238f, 25f));
            }

            var detail = GetOrCreateText(card.transform, "Decoration Detail Text", "장식을 선택해 주세요.", 18,
                TextAnchor.UpperLeft, new Vector2(910f, -112f), new Vector2(228f, 240f));
            detail.supportRichText = true;
            var status = GetOrCreateText(card.transform, "Decoration Status Text", string.Empty, 16,
                TextAnchor.UpperLeft, new Vector2(910f, -366f), new Vector2(228f, 94f));
            var purchase = GetOrCreateTopLeftButton(card.transform, "Decoration Purchase Button", "구매",
                new Vector2(910f, -488f), new Vector2(104f, 52f));
            var equip = GetOrCreateTopLeftButton(card.transform, "Decoration Equip Button", "장착",
                new Vector2(1030f, -488f), new Vector2(104f, 52f));
            var close = GetOrCreateTopLeftButton(card.transform, "Decoration Shop Close Button", "닫기",
                new Vector2(990f, -718f), new Vector2(144f, 52f));
            ApplyCareButtonStyle(purchase);
            ApplyCareButtonStyle(equip);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<DecorationShopPanelController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<DecorationShopPanelController>();
            }

            controller.Configure(
                shopRoot,
                balance,
                detail,
                status,
                itemNames,
                itemStates,
                itemButtons,
                purchase,
                equip,
                close,
                () => ResolveDecorationManager()?.GetDecorationShopSnapshot()
                    ?? DecorationShopSnapshot.CreateDefault(),
                itemId =>
                {
                    var manager = ResolveDecorationManager();
                    return manager != null
                        ? manager.TryPurchaseDecoration(itemId)
                        : DecorationShopRules.Purchase(itemId, DecorationShopSnapshot.CreateDefault());
                },
                itemId =>
                {
                    var manager = ResolveDecorationManager();
                    return manager != null
                        ? manager.TryEquipDecoration(itemId)
                        : DecorationShopRules.Equip(itemId, DecorationShopSnapshot.CreateDefault());
                });

            var previewRect = decorateOverlay.Find("Decorate Preview Panel") as RectTransform;
            if (previewRect != null)
            {
                previewRect.sizeDelta = new Vector2(692f, 432f);
            }

            var openShopButton = GetOrCreateTopLeftButton(
                decorateOverlay,
                "Open Decoration Shop Button",
                "장식 상점",
                new Vector2(568f, -562f),
                new Vector2(144f, 42f));
            ApplyCareButtonStyle(openShopButton);
            openShopButton.onClick.RemoveAllListeners();
            openShopButton.onClick.AddListener(controller.Open);
        }

        private static GameManager ResolveDecorationManager()
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance;
            }

            return Application.isPlaying ? EnsureCoreSystems() : null;
        }

        private static void EnsureDecorationRoomPresenter()
        {
            var background = GameObject.Find("Milkroom Background");
            if (background == null)
            {
                return;
            }

            var presenter = background.GetComponent<DecorationRoomPresenter>();
            if (presenter == null)
            {
                presenter = background.AddComponent<DecorationRoomPresenter>();
            }

            var shell = background.transform.Find("RoomShell");
            var wall = shell != null ? shell.Find("BackWall")?.GetComponent<Renderer>() : null;
            var rug = background.transform.Find("Rug_Model");
            var floor = rug != null
                ? rug.GetComponentInChildren<Renderer>(true)
                : shell != null ? shell.Find("Floor")?.GetComponent<Renderer>() : null;
            var anchor = background.transform.Find("Decoration Accent Anchor");
            if (anchor == null)
            {
                var anchorObject = new GameObject("Decoration Accent Anchor");
                anchorObject.transform.SetParent(background.transform, false);
                anchorObject.transform.localPosition = new Vector3(3.34f, -1.93f, 1.55f);
                anchor = anchorObject.transform;
            }

            Transform EnsureDecorationAnchor(string name, Vector3 localPosition)
            {
                var found = background.transform.Find(name);
                if (found != null)
                {
                    return found;
                }

                var created = new GameObject(name).transform;
                created.SetParent(background.transform, false);
                created.localPosition = localPosition;
                return created;
            }

            var windowAnchor = EnsureDecorationAnchor("Decoration Window Anchor", new Vector3(-3.15f, 0.62f, 1.42f));
            var shelfAnchor = EnsureDecorationAnchor("Decoration Shelf Anchor", new Vector3(3.05f, 0.18f, 1.36f));
            var bedsideAnchor = EnsureDecorationAnchor("Decoration Bedside Anchor", new Vector3(-2.85f, -1.72f, 1.2f));

            presenter.Configure(wall, floor, anchor, windowAnchor, shelfAnchor, bedsideAnchor);
        }

        private static void RemoveNormalEvolutionVisualAccents(
            CheeseTamaVisualController visualController)
        {
            if (visualController == null)
            {
                return;
            }

            // Normal-evolution accents were previously drawn as primitive ribbons,
            // drops, and spots in front of the character. In particular, the
            // Mozzarella profile produced pale blue and white shapes over the face.
            // The authored growth model already carries the intended facial detail,
            // so remove both live components and any orphaned generated roots.
            foreach (var bridge in visualController.GetComponents<NormalEvolutionVisualBridge>())
            {
                bridge.enabled = false;
                DestroyObjectSafely(bridge);
            }

            foreach (var presenter in visualController.GetComponents<NormalEvolutionVisualPresenter>())
            {
                if (presenter.GeneratedRoot != null)
                {
                    presenter.GeneratedRoot.gameObject.SetActive(false);
                }

                presenter.enabled = false;
                presenter.Release();
                DestroyObjectSafely(presenter);
            }

            var modelRoot = visualController.ModelInstance;
            if (modelRoot == null)
            {
                return;
            }

            while (true)
            {
                var generatedRoot = modelRoot.Find(NormalEvolutionVisualPresenter.GeneratedRootName);
                if (generatedRoot == null)
                {
                    break;
                }

                generatedRoot.gameObject.SetActive(false);
                generatedRoot.SetParent(null, true);
                DestroyObjectSafely(generatedRoot.gameObject);
            }
        }

        private static void EnsureAutonomousLife(
            Transform canvasTransform,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null || visualController == null)
            {
                return;
            }

            var visualRoot = visualController.transform;
            var motionObject = GameObject.Find("CheeseTama Autonomous Motion Root");
            if (motionObject == null)
            {
                motionObject = new GameObject("CheeseTama Autonomous Motion Root");
                motionObject.transform.position = CheeseTamaRestingWorldPosition;
                motionObject.transform.rotation = Quaternion.identity;
                motionObject.transform.localScale = Vector3.one;
                motionObject.transform.SetParent(visualRoot.parent, true);
            }

            if (visualRoot.parent != motionObject.transform)
            {
                visualRoot.SetParent(motionObject.transform, true);
            }

            motionObject.transform.position = CheeseTamaRestingWorldPosition;
            visualController.SetRestingWorldPosition(CheeseTamaRestingWorldPosition);

            var sceneRoot = GameObject.Find("MilkroomSceneRoot");
            if (sceneRoot == null)
            {
                sceneRoot = new GameObject("MilkroomSceneRoot");
            }

            var environment = GetOrCreateSceneGroup(sceneRoot.transform, "Environment");
            var anchorRoot = GetOrCreateSceneGroup(environment, "Autonomous Life Anchors");
            var restingY = CheeseTamaRestingWorldPosition.y;
            var idle = EnsureAutonomousLifeAnchor(anchorRoot, "Idle Anchor", new Vector3(0f, restingY, 0.08f));
            var nap = EnsureAutonomousLifeAnchor(anchorRoot, "Nap Anchor", new Vector3(-2.05f, restingY, 0.25f));
            var window = EnsureAutonomousLifeAnchor(anchorRoot, "Window Anchor", new Vector3(-0.85f, restingY, 1.2f));
            var shelf = EnsureAutonomousLifeAnchor(anchorRoot, "Shelf Anchor", new Vector3(1.9f, restingY, 1.15f));
            var play = EnsureAutonomousLifeAnchor(anchorRoot, "Play Anchor", new Vector3(-1.15f, restingY, 0.15f));
            var dance = EnsureAutonomousLifeAnchor(anchorRoot, "Dance Anchor", new Vector3(1.15f, restingY, 0.15f));

            var presenter = motionObject.GetComponent<AutonomousLifePresenter>();
            if (presenter == null)
            {
                presenter = motionObject.AddComponent<AutonomousLifePresenter>();
            }

            var bridge = motionObject.GetComponent<AutonomousLifeBridge>();
            if (bridge == null)
            {
                bridge = motionObject.AddComponent<AutonomousLifeBridge>();
            }

            bridge.Configure(
                presenter,
                motionObject.transform,
                new AutonomousLifeAnchorBindings(idle, nap, window, shelf, play, dance),
                EnsureCoreSystems(),
                visualController,
                canvasTransform.GetComponent<CheeseTamaDialogueBridge>());
        }

        private static Transform EnsureAutonomousLifeAnchor(
            Transform parent,
            string name,
            Vector3 worldPosition)
        {
            var anchor = parent.Find(name);
            if (anchor == null)
            {
                anchor = new GameObject(name).transform;
                anchor.SetParent(parent, false);
            }

            anchor.position = worldPosition;
            anchor.rotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static void EnsureMilkroomAtmosphere(Transform canvasTransform, CheeseTama.Gameplay.CheeseTamaModel tama)
        {
            if (canvasTransform == null)
            {
                return;
            }

            // Restore the original Milkroom lighting. The atmosphere feature added
            // a full-screen warm tint and an extra point light on top of the theme
            // controller, making the room visibly brighter than the authored scene.
            var overlayTransform = canvasTransform.Find("Milkroom Atmosphere Overlay");
            if (overlayTransform != null)
            {
                DestroyObjectSafely(overlayTransform.gameObject);
            }

            var lightObject = GameObject.Find("Milkroom Atmosphere Light");
            if (lightObject != null)
            {
                DestroyObjectSafely(lightObject);
            }
        }

        private static void EnsureCheeseTamaPetInteraction(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null || visualController == null)
            {
                return;
            }

            var target = visualController.gameObject;
            var interactionCollider = target.GetComponent<BoxCollider>();
            if (interactionCollider == null)
            {
                interactionCollider = target.AddComponent<BoxCollider>();
            }

            interactionCollider.isTrigger = true;
            var petController = target.GetComponent<CheeseTamaPetInteractionController>();
            if (petController == null)
            {
                petController = target.AddComponent<CheeseTamaPetInteractionController>();
            }

            petController.Configure(milkroomUi, visualController, canvasTransform);
        }

        private static void EnsureUiButtonSounds(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button != null && button.GetComponent<UiButtonSound>() == null)
                {
                    button.gameObject.AddComponent<UiButtonSound>();
                }
            }
        }

        private static void BuildMilkroomSettings(
            Transform canvasTransform,
            Button settingsButton,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController,
            out Text settingsLastSavedText)
        {
            var settingsModal = GetOrCreateRightPanel(canvasTransform, "Settings Modal", new Vector2(-40, -76), new Vector2(560, 780));
            if (settingsModal.TryGetComponent(out Image settingsImage))
            {
                settingsImage.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            }

            var settingsTransform = settingsModal.transform;
            GetOrCreateText(settingsTransform, "Settings Title Text", "설정", 22, TextAnchor.UpperLeft, new Vector2(28, -24), new Vector2(280, 34));
            GetOrCreateText(settingsTransform, "Settings Data Title Text", "데이터 관리", 18, TextAnchor.UpperLeft, new Vector2(28, -82), new Vector2(300, 30));
            GetOrCreateText(settingsTransform, "Settings Sound Title Text", "소리", 18, TextAnchor.UpperLeft, new Vector2(28, -276), new Vector2(220, 28));
            GetOrCreateText(settingsTransform, "Settings Display Title Text", "화면", 18, TextAnchor.UpperLeft, new Vector2(28, -472), new Vector2(220, 28));
            GetOrCreateText(settingsTransform, "Settings Controls Title Text", "조작", 18, TextAnchor.UpperLeft, new Vector2(28, -632), new Vector2(220, 28));

            var closeSettingsButton = GetOrCreateTopLeftButton(settingsTransform, "Close Settings Button", "닫기", new Vector2(424, -20), new Vector2(108, 40));
            var manualSaveButton = GetOrCreateTopLeftButton(settingsTransform, "Manual Save Button", "저장", new Vector2(28, -170), new Vector2(120, 42));
            var manualLoadButton = GetOrCreateTopLeftButton(settingsTransform, "Manual Load Button", "불러오기", new Vector2(166, -170), new Vector2(120, 42));
            var openResetButton = GetOrCreateTopLeftButton(settingsTransform, "Open Reset Button", "초기화", new Vector2(304, -170), new Vector2(120, 42));
            ApplyDangerButtonStyle(openResetButton);
            var dataStatusText = GetOrCreateText(settingsTransform, "Data Status Text", "돌봄 행동 후 자동 저장됩니다. 아래에서 수동 관리할 수 있습니다.", 13, TextAnchor.UpperLeft, new Vector2(28, -116), new Vector2(500, 42));
            settingsLastSavedText = GetOrCreateText(settingsTransform, "Settings Last Saved Text", "<b>마지막 저장</b>  없음", 14, TextAnchor.UpperLeft, new Vector2(28, -228), new Vector2(500, 26));
            settingsLastSavedText.supportRichText = true;
            settingsLastSavedText.color = new Color(0.34f, 0.22f, 0.1f);

            GetOrCreateText(settingsTransform, "Master Volume Label Text", "전체 볼륨", 14, TextAnchor.MiddleLeft, new Vector2(28, -308), new Vector2(100, 28));
            var masterVolumeSlider = GetOrCreateSettingsSlider(settingsTransform, "Master Volume Slider", new Vector2(132, -312), new Vector2(280, 22), 0f, 1f, false);
            var masterVolumeValueText = GetOrCreateText(settingsTransform, "Master Volume Value Text", "100%", 14, TextAnchor.MiddleRight, new Vector2(430, -306), new Vector2(80, 28));
            GetOrCreateText(settingsTransform, "Music Volume Label Text", "배경음", 14, TextAnchor.MiddleLeft, new Vector2(28, -346), new Vector2(100, 28));
            var musicVolumeSlider = GetOrCreateSettingsSlider(settingsTransform, "Music Volume Slider", new Vector2(132, -350), new Vector2(280, 22), 0f, 1f, false);
            var musicVolumeValueText = GetOrCreateText(settingsTransform, "Music Volume Value Text", "100%", 14, TextAnchor.MiddleRight, new Vector2(430, -344), new Vector2(80, 28));
            GetOrCreateText(settingsTransform, "Effect Volume Label Text", "효과음", 14, TextAnchor.MiddleLeft, new Vector2(28, -384), new Vector2(100, 28));
            var effectVolumeSlider = GetOrCreateSettingsSlider(settingsTransform, "Effect Volume Slider", new Vector2(132, -388), new Vector2(280, 22), 0f, 1f, false);
            var effectVolumeValueText = GetOrCreateText(settingsTransform, "Effect Volume Value Text", "100%", 14, TextAnchor.MiddleRight, new Vector2(430, -382), new Vector2(80, 28));
            var muteToggle = GetOrCreateSettingsToggle(settingsTransform, "Mute Audio Toggle", "전체 음소거", new Vector2(28, -422), new Vector2(180, 30));

            var fullScreenToggle = GetOrCreateSettingsToggle(settingsTransform, "Fullscreen Toggle", "전체화면", new Vector2(28, -504), new Vector2(180, 30));
            GetOrCreateText(settingsTransform, "UI Scale Label Text", "UI 크기", 14, TextAnchor.MiddleLeft, new Vector2(28, -540), new Vector2(100, 28));
            RemoveChildIfExists(settingsTransform, "UI Scale Slider");
            var uiScale90Button = GetOrCreateTopLeftButton(settingsTransform, "UI Scale 90 Button", "90", new Vector2(132, -536), new Vector2(80, 34));
            var uiScale100Button = GetOrCreateTopLeftButton(settingsTransform, "UI Scale 100 Button", "100", new Vector2(226, -536), new Vector2(80, 34));
            var uiScale110Button = GetOrCreateTopLeftButton(settingsTransform, "UI Scale 110 Button", "110", new Vector2(320, -536), new Vector2(80, 34));
            ApplyCollectionTabButtonStyle(uiScale90Button, uiScale100Button, uiScale110Button);
            var uiScaleValueText = GetOrCreateText(settingsTransform, "UI Scale Value Text", "100%", 14, TextAnchor.MiddleRight, new Vector2(420, -538), new Vector2(90, 28));
            GetOrCreateText(settingsTransform, "Frame Rate Label Text", "프레임", 14, TextAnchor.MiddleLeft, new Vector2(28, -580), new Vector2(90, 28));
            var frameRate30Button = GetOrCreateTopLeftButton(settingsTransform, "Frame Rate 30 Button", "30", new Vector2(132, -576), new Vector2(80, 34));
            var frameRate60Button = GetOrCreateTopLeftButton(settingsTransform, "Frame Rate 60 Button", "60", new Vector2(226, -576), new Vector2(80, 34));
            var frameRate120Button = GetOrCreateTopLeftButton(settingsTransform, "Frame Rate 120 Button", "120", new Vector2(320, -576), new Vector2(80, 34));
            ApplyCollectionTabButtonStyle(frameRate30Button, frameRate60Button, frameRate120Button);
            var frameRateValueText = GetOrCreateText(settingsTransform, "Frame Rate Value Text", "60 FPS", 14, TextAnchor.MiddleRight, new Vector2(420, -578), new Vector2(90, 28));

            var careTipToggle = GetOrCreateSettingsToggle(settingsTransform, "Care Tip Toggle", "돌봄 팁 표시", new Vector2(28, -664), new Vector2(210, 30));
            var resetSettingsButton = GetOrCreateTopLeftButton(settingsTransform, "Reset Settings Button", "설정 초기화", new Vector2(374, -656), new Vector2(136, 40));
            ApplyCareButtonStyle(resetSettingsButton);
            var settingsStatusText = GetOrCreateText(settingsTransform, "Settings Status Text", "설정을 불러왔습니다.", 13, TextAnchor.MiddleLeft, new Vector2(28, -718), new Vector2(500, 34));
            settingsStatusText.color = new Color(0.38f, 0.28f, 0.17f);

            var confirmRoot = GetOrCreatePanel(canvasTransform, "Confirm Reset Dialog", new Vector2(640, -300), new Vector2(640, 360));
            if (confirmRoot.TryGetComponent(out Image confirmImage))
            {
                confirmImage.color = new Color(1f, 0.98f, 0.9f, 1f);
            }

            var confirmTransform = confirmRoot.transform;
            GetOrCreateText(confirmTransform, "Confirm Reset Title Text", "데이터 초기화", 22, TextAnchor.UpperLeft, new Vector2(24, -24), new Vector2(300, 34));
            var confirmMessageText = GetOrCreateText(
                confirmTransform,
                "Confirm Reset Message Text",
                "로컬 CheeseTama 진행도를 모두 지우려면 RESET을 입력하세요.",
                15,
                TextAnchor.UpperLeft,
                new Vector2(24, -82),
                new Vector2(580, 70));
            GetOrCreateText(confirmTransform, "Reset Input Label Text", "RESET을 입력하면 버튼이 활성화됩니다.", 14, TextAnchor.UpperLeft, new Vector2(24, -152), new Vector2(420, 24));
            var resetInput = GetOrCreateInputField(confirmTransform, "Reset Input Field", "RESET", new Vector2(24, -184), new Vector2(360, 52));
            var confirmResetButton = GetOrCreateTopLeftButton(confirmTransform, "Confirm Reset Button", "초기화", new Vector2(344, -284), new Vector2(120, 42));
            ApplyDangerButtonStyle(confirmResetButton);
            var cancelResetButton = GetOrCreateTopLeftButton(confirmTransform, "Cancel Reset Button", "취소", new Vector2(480, -284), new Vector2(120, 42));

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

            var gameSettingsPanel = settingsModal.GetComponent<GameSettingsPanelController>();
            if (gameSettingsPanel == null)
            {
                gameSettingsPanel = settingsModal.AddComponent<GameSettingsPanelController>();
            }

            gameSettingsPanel.Configure(
                masterVolumeSlider,
                musicVolumeSlider,
                effectVolumeSlider,
                muteToggle,
                fullScreenToggle,
                uiScale90Button,
                uiScale100Button,
                uiScale110Button,
                frameRate30Button,
                frameRate60Button,
                frameRate120Button,
                careTipToggle,
                resetSettingsButton,
                masterVolumeValueText,
                musicVolumeValueText,
                effectVolumeValueText,
                uiScaleValueText,
                frameRateValueText,
                settingsStatusText);

            var settingsController = settingsModal.GetComponent<SettingsMenuController>();
            if (settingsController == null)
            {
                settingsController = settingsModal.AddComponent<SettingsMenuController>();
            }

            settingsController.Configure(settingsButton, closeSettingsButton, settingsModal);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var devModeToggleButton = GetOrCreateButton(
                canvasTransform,
                "Dev Mode Toggle Button",
                "개발자 모드",
                new Vector2(-200, 25),
                new Vector2(156, 58));
            var devModeToggleRect = devModeToggleButton.GetComponent<RectTransform>();
            devModeToggleRect.anchorMin = new Vector2(1, 0);
            devModeToggleRect.anchorMax = new Vector2(1, 0);
            devModeToggleRect.pivot = new Vector2(0.5f, 0);
            devModeToggleRect.anchoredPosition = new Vector2(-200, 25);
            SetButtonLabel(devModeToggleButton, "개발자 모드");
            ApplyCareButtonStyle(devModeToggleButton);

            var devPanel = GetOrCreatePanel(canvasTransform, "Dev Panel", new Vector2(1570, -116), new Vector2(326, 206));
            var devPanelRect = devPanel.GetComponent<RectTransform>();
            var devPanelRight = devModeToggleRect.anchoredPosition.x + (devModeToggleRect.sizeDelta.x * 0.5f);
            var devPanelBottom = devModeToggleRect.anchoredPosition.y + devModeToggleRect.sizeDelta.y + 14f;
            devPanelRect.anchorMin = new Vector2(1, 0);
            devPanelRect.anchorMax = new Vector2(1, 0);
            devPanelRect.pivot = new Vector2(1, 0);
            devPanelRect.anchoredPosition = new Vector2(devPanelRight, devPanelBottom);
            var devPanelTransform = devPanel.transform;
            GetOrCreateText(devPanelTransform, "Dev Panel Title Text", "개발자 패널", 17, TextAnchor.UpperLeft, new Vector2(18, -18), new Vector2(240, 28));
            GetOrCreateText(devPanelTransform, "Dev Panel Help Text", "에디터 테스트 도구", 13, TextAnchor.UpperLeft, new Vector2(18, -48), new Vector2(240, 24));
            var waitHourButton = GetOrCreateTopLeftButton(devPanelTransform, "Wait Hour Dev Button", "1시간 경과", new Vector2(18, -86), new Vector2(126, 42));
            ConfigureCareButton(waitHourButton, MilkroomCareAction.WaitHour, controller, visualController);
            var debugSceneButton = GetOrCreateTopLeftButton(devPanelTransform, "Debug Scene Button", "개발자 씬", new Vector2(170, -86), new Vector2(126, 42));
            ConfigureNavigationButton(debugSceneButton, SceneNames.Debug, true);
            var levelOneDevButton = GetOrCreateTopLeftButton(devPanelTransform, "Add Level One Dev Button", "레벨 +1", new Vector2(18, -142), new Vector2(88, 42));
            ConfigureDebugButton(levelOneDevButton, DebugAction.AddLevelOne, controller, visualController);
            var levelTwoDevButton = GetOrCreateTopLeftButton(devPanelTransform, "Add Level Two Dev Button", "레벨 +2", new Vector2(119, -142), new Vector2(88, 42));
            ConfigureDebugButton(levelTwoDevButton, DebugAction.AddLevelTwo, controller, visualController);
            var levelFiveDevButton = GetOrCreateTopLeftButton(devPanelTransform, "Add Level Five Dev Button", "레벨 +5", new Vector2(220, -142), new Vector2(88, 42));
            ConfigureDebugButton(levelFiveDevButton, DebugAction.AddLevelFive, controller, visualController);

            var devPanelController = canvasTransform.GetComponent<DevPanelController>();
            if (devPanelController == null)
            {
                devPanelController = canvasTransform.gameObject.AddComponent<DevPanelController>();
            }

            devPanelController.Configure(devPanel, devModeToggleButton);
#else
            RemoveChildIfExists(canvasTransform, "Dev Panel");
            RemoveChildIfExists(canvasTransform, "Dev Mode Toggle Button");
#endif
        }

        private static bool EnsureInputBindingsPanel(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return false;
            }

            var settingsModal = canvasTransform.Find("Settings Modal");
            if (settingsModal == null)
            {
                return false;
            }

            var openButton = GetOrCreateTopLeftButton(
                settingsModal,
                "Open Input Bindings Button",
                "키 설정",
                new Vector2(246f, -656f),
                new Vector2(112f, 40f));
            ApplyCareButtonStyle(openButton);

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                InputBindingsPanelController.OverlayObjectName,
                new Color(0.06f, 0.045f, 0.03f, 0.82f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Input Bindings Card",
                Vector2.zero,
                new Vector2(820f, 650f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(820f, 650f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.975f, 0.88f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Input Bindings Title Text",
                "키보드 조작 설정",
                28,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -32f),
                new Vector2(724f, 48f));
            title.fontStyle = FontStyle.Bold;
            var help = GetOrCreateText(
                card.transform,
                "Input Bindings Help Text",
                "바꿀 항목을 누른 다음 새 키를 입력하세요. 중복 키는 저장되지 않습니다.",
                15,
                TextAnchor.MiddleCenter,
                new Vector2(54f, -78f),
                new Vector2(712f, 34f));
            help.horizontalOverflow = HorizontalWrapMode.Wrap;

            var definitions = CheeseTama.Gameplay.Input.GameInputBindingSystem.All;
            var buttons = new Button[definitions.Count];
            var valueLabels = new Text[definitions.Count];
            for (var index = 0; index < definitions.Count; index += 1)
            {
                var leftColumn = index < 5;
                var row = leftColumn ? index : index - 5;
                var x = leftColumn ? 48f : 424f;
                var y = -128f - (row * 64f);
                var button = GetOrCreateTopLeftButton(
                    card.transform,
                    $"Input Binding {definitions[index].id} Button",
                    definitions[index].displayName,
                    new Vector2(x, y),
                    new Vector2(348f, 50f));
                ApplyCareButtonStyle(button);
                var label = button.transform.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.alignment = TextAnchor.MiddleLeft;
                    var labelRect = label.rectTransform;
                    labelRect.offsetMin = new Vector2(18f, 0f);
                    labelRect.offsetMax = new Vector2(-152f, 0f);
                }

                var value = GetOrCreateText(
                    button.transform,
                    "Binding Value Text",
                    "-",
                    15,
                    TextAnchor.MiddleRight,
                    new Vector2(174f, -4f),
                    new Vector2(154f, 42f));
                value.fontStyle = FontStyle.Bold;
                value.color = new Color(0.39f, 0.22f, 0.08f, 1f);
                buttons[index] = button;
                valueLabels[index] = value;
            }

            var status = GetOrCreateText(
                card.transform,
                "Input Bindings Status Text",
                "바꿀 조작을 선택하세요.",
                15,
                TextAnchor.MiddleCenter,
                new Vector2(54f, -470f),
                new Vector2(712f, 46f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            var reset = GetOrCreateTopLeftButton(
                card.transform,
                "Reset Input Bindings Button",
                "기본 키로",
                new Vector2(444f, -558f),
                new Vector2(148f, 50f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Close Input Bindings Button",
                "확인",
                new Vector2(616f, -558f),
                new Vector2(148f, 50f));
            ApplyCareButtonStyle(reset);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<InputBindingsPanelController>()
                ?? canvasTransform.gameObject.AddComponent<InputBindingsPanelController>();
            controller.Configure(
                overlay,
                status,
                buttons,
                valueLabels,
                reset,
                close,
                () =>
                {
                    var manager = GameManager.Instance;
                    if (manager?.CurrentSave?.settings == null)
                    {
                        return null;
                    }

                    manager.CurrentSave.settings.EnsureRuntimeDefaults();
                    return manager.CurrentSave.settings.inputBindings;
                },
                state =>
                {
                    var manager = GameManager.Instance;
                    if (manager?.CurrentSave?.settings == null || state == null)
                    {
                        return;
                    }

                    manager.CurrentSave.settings.inputBindings = state;
                    manager.SaveGame();
                },
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(controller.Open);
            overlay.transform.SetAsLastSibling();
            return true;
        }

        private static void EnsureNpcVisitCard(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                NpcVisitCardController.OverlayObjectName,
                new Color(0.055f, 0.04f, 0.025f, 0.8f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Npc Visit Card",
                Vector2.zero,
                new Vector2(720f, 590f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(720f, 590f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.965f, 0.82f, 1f);
                cardImage.raycastTarget = true;
            }

            var portrait = GetOrCreatePanel(
                card.transform,
                "Npc Portrait",
                new Vector2(48f, -42f),
                new Vector2(118f, 118f));
            if (portrait.TryGetComponent(out Image portraitImage))
            {
                portraitImage.color = new Color(1f, 0.79f, 0.34f, 1f);
                ApplyCircleImage(portraitImage);
            }

            var portraitText = GetOrCreateText(
                portrait.transform,
                "Npc Portrait Text",
                "손님",
                22,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(118f, 118f));
            portraitText.fontStyle = FontStyle.Bold;
            var title = GetOrCreateText(
                card.transform,
                "Npc Visit Title Text",
                "밀크룸의 손님",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(194f, -42f),
                new Vector2(470f, 52f));
            title.fontStyle = FontStyle.Bold;
            var role = GetOrCreateText(
                card.transform,
                "Npc Visit Role Text",
                "새로운 방문자",
                17,
                TextAnchor.MiddleLeft,
                new Vector2(196f, -98f),
                new Vector2(430f, 34f));
            role.color = new Color(0.62f, 0.35f, 0.1f, 1f);
            var relationship = GetOrCreateText(
                card.transform,
                "Npc Visit Relationship Text",
                "이야기 1/3",
                15,
                TextAnchor.MiddleRight,
                new Vector2(500f, -132f),
                new Vector2(164f, 28f));
            var messagePanel = GetOrCreatePanel(
                card.transform,
                "Npc Visit Message Panel",
                new Vector2(48f, -184f),
                new Vector2(624f, 170f));
            if (messagePanel.TryGetComponent(out Image messageImage))
            {
                messageImage.color = new Color(1f, 0.99f, 0.93f, 1f);
            }

            var message = GetOrCreateText(
                messagePanel.transform,
                "Npc Visit Message Text",
                "밀크룸에 반가운 손님이 찾아왔어요.",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(28f, -18f),
                new Vector2(568f, 134f));
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Truncate;

            var firstChoice = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Visit First Choice Button",
                "첫 번째 선택",
                new Vector2(48f, -382f),
                new Vector2(296f, 58f));
            var secondChoice = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Visit Second Choice Button",
                "두 번째 선택",
                new Vector2(376f, -382f),
                new Vector2(296f, 58f));
            var later = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Visit Later Button",
                "나중에",
                new Vector2(288f, -494f),
                new Vector2(144f, 50f));
            var confirm = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Visit Confirm Button",
                "확인",
                new Vector2(528f, -494f),
                new Vector2(144f, 50f));
            ApplyCareButtonStyle(firstChoice);
            ApplyCareButtonStyle(secondChoice);
            ApplyCareButtonStyle(later);
            ApplyCareButtonStyle(confirm);
            var firstLabel = firstChoice.transform.Find("Label")?.GetComponent<Text>();
            var secondLabel = secondChoice.transform.Find("Label")?.GetComponent<Text>();

            var controller = canvasTransform.GetComponent<NpcVisitCardController>()
                ?? canvasTransform.gameObject.AddComponent<NpcVisitCardController>();
            controller.Configure(
                overlay,
                portraitText,
                title,
                role,
                message,
                relationship,
                firstChoice,
                firstLabel,
                secondChoice,
                secondLabel,
                later,
                confirm,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            var bridge = canvasTransform.GetComponent<NpcVisitBridge>()
                ?? canvasTransform.gameObject.AddComponent<NpcVisitBridge>();
            bridge.Configure(
                controller,
                Application.isPlaying ? GameManager.Instance : null,
                canvasTransform);
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureMilkBlendingPanel(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            RemoveChildIfExists(canvasTransform.Find("Cooking Panel"), "Open Milk Blending Button");
            RemoveChildIfExists(canvasTransform.Find("Milkroom Utility Bar"), "Open Milk Blending Button");

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Milk Blending Overlay",
                new Color(0.055f, 0.04f, 0.025f, 0.82f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Milk Blending Card",
                Vector2.zero,
                new Vector2(1120f, 780f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(1120f, 780f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.975f, 0.88f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Milk Blending Title Text",
                "우유 블렌딩 실험",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(42f, -26f),
                new Vector2(430f, 48f));
            title.fontStyle = FontStyle.Bold;
            var balance = GetOrCreateText(
                card.transform,
                "Milk Blending Balance Text",
                "우유코인 0 · 우유방울 0 · 수집 조각 0",
                15,
                TextAnchor.MiddleRight,
                new Vector2(512f, -30f),
                new Vector2(430f, 40f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Close Milk Blending Button",
                "닫기",
                new Vector2(962f, -24f),
                new Vector2(116f, 42f));
            ApplyCareButtonStyle(close);

            GetOrCreateText(
                card.transform,
                "Milk Blending Milk Header Text",
                "1. 우유 선택",
                19,
                TextAnchor.MiddleLeft,
                new Vector2(42f, -88f),
                new Vector2(400f, 34f));
            GetOrCreateText(
                card.transform,
                "Milk Blending Ingredient Header Text",
                "2. 재료 선택",
                19,
                TextAnchor.MiddleLeft,
                new Vector2(578f, -88f),
                new Vector2(400f, 34f));

            var milkCount = CheeseTama.Gameplay.Milk.MilkBlendingCatalog.AllMilkIds.Length;
            var ingredientCount = CheeseTama.Gameplay.Milk.MilkBlendingCatalog.AllIngredients.Length;
            var milkNames = new Text[milkCount];
            var milkStates = new Text[milkCount];
            var milkButtons = new Button[milkCount];
            var ingredientNames = new Text[ingredientCount];
            var ingredientStates = new Text[ingredientCount];
            var ingredientButtons = new Button[ingredientCount];

            for (var index = 0; index < milkCount; index += 1)
            {
                var row = index / 2;
                var column = index % 2;
                var button = GetOrCreateTopLeftButton(
                    card.transform,
                    $"Milk Blending Milk Button {index}",
                    "우유",
                    new Vector2(42f + (column * 230f), -130f - (row * 64f)),
                    new Vector2(214f, 54f));
                ApplyCareButtonStyle(button);
                var nameLabel = button.transform.Find("Label")?.GetComponent<Text>();
                if (nameLabel != null)
                {
                    nameLabel.alignment = TextAnchor.MiddleLeft;
                    nameLabel.rectTransform.offsetMin = new Vector2(14f, 0f);
                    nameLabel.rectTransform.offsetMax = new Vector2(-72f, 0f);
                }

                var stateLabel = GetOrCreateText(
                    button.transform,
                    "Option State Text",
                    "사용 가능",
                    12,
                    TextAnchor.MiddleRight,
                    new Vector2(128f, -4f),
                    new Vector2(70f, 46f));
                milkNames[index] = nameLabel;
                milkStates[index] = stateLabel;
                milkButtons[index] = button;
            }

            for (var index = 0; index < ingredientCount; index += 1)
            {
                var row = index / 2;
                var column = index % 2;
                var button = GetOrCreateTopLeftButton(
                    card.transform,
                    $"Milk Blending Ingredient Button {index}",
                    "재료",
                    new Vector2(578f + (column * 230f), -130f - (row * 64f)),
                    new Vector2(214f, 54f));
                ApplyCareButtonStyle(button);
                var nameLabel = button.transform.Find("Label")?.GetComponent<Text>();
                if (nameLabel != null)
                {
                    nameLabel.alignment = TextAnchor.MiddleLeft;
                    nameLabel.rectTransform.offsetMin = new Vector2(14f, 0f);
                    nameLabel.rectTransform.offsetMax = new Vector2(-74f, 0f);
                }

                var stateLabel = GetOrCreateText(
                    button.transform,
                    "Option State Text",
                    "사용 0회",
                    12,
                    TextAnchor.MiddleRight,
                    new Vector2(128f, -4f),
                    new Vector2(70f, 46f));
                ingredientNames[index] = nameLabel;
                ingredientStates[index] = stateLabel;
                ingredientButtons[index] = button;
            }

            var detailPanel = GetOrCreatePanel(
                card.transform,
                "Milk Blending Detail Panel",
                new Vector2(42f, -418f),
                new Vector2(1036f, 214f));
            if (detailPanel.TryGetComponent(out Image detailImage))
            {
                detailImage.color = new Color(1f, 0.99f, 0.94f, 1f);
            }

            var detail = GetOrCreateText(
                detailPanel.transform,
                "Milk Blending Detail Text",
                "우유와 재료를 하나씩 선택해 주세요.",
                17,
                TextAnchor.UpperLeft,
                new Vector2(24f, -20f),
                new Vector2(620f, 160f));
            detail.supportRichText = true;
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            var resultText = GetOrCreateText(
                detailPanel.transform,
                "Milk Blending Result Text",
                "완성 결과  ???",
                17,
                TextAnchor.UpperLeft,
                new Vector2(680f, -20f),
                new Vector2(330f, 110f));
            resultText.supportRichText = true;
            resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var status = GetOrCreateText(
                card.transform,
                "Milk Blending Status Text",
                "어울리는 조합을 찾아보세요.",
                15,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -660f),
                new Vector2(780f, 52f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            var blend = GetOrCreateTopLeftButton(
                card.transform,
                "Execute Milk Blending Button",
                "블렌딩",
                new Vector2(884f, -660f),
                new Vector2(194f, 54f));
            ApplyCareButtonStyle(blend);

            var panelController = canvasTransform.GetComponent<MilkBlendingPanelController>()
                ?? canvasTransform.gameObject.AddComponent<MilkBlendingPanelController>();
            panelController.Configure(
                overlay,
                balance,
                detail,
                resultText,
                status,
                milkNames,
                milkStates,
                milkButtons,
                ingredientNames,
                ingredientStates,
                ingredientButtons,
                blend,
                close,
                () => GameManager.Instance?.GetMilkBlendingSnapshot()
                    ?? CheeseTama.Gameplay.Milk.MilkBlendingPanelSnapshot.CreateDefault(),
                (milkId, ingredientId) =>
                {
                    var manager = GameManager.Instance;
                    var result = manager?.TryBlendMilk(milkId, ingredientId);
                    if (result != null && result.applied)
                    {
                        milkroomUi?.Bind(manager.CurrentSave);
                        visualController?.Bind(manager.CurrentTama);
                        visualController?.ReactAction(CheeseTamaVisualAction.Cook);
                    }

                    return result;
                },
                null,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureCookingChoicePanel(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            RemoveChildIfExists(canvasTransform.Find("Cooking Panel"), "Open Milk Blending Button");
            RemoveChildIfExists(canvasTransform.Find("Milkroom Utility Bar"), "Open Milk Blending Button");

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                CookingChoicePanelController.OverlayObjectName,
                new Color(0.08f, 0.055f, 0.025f, 0.76f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Cooking Choice Card",
                Vector2.zero,
                new Vector2(680f, 420f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(680f, 420f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.96f, 0.82f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Cooking Choice Title Text",
                "무엇을 만들까요?",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(40f, -34f),
                new Vector2(600f, 48f));
            title.fontStyle = FontStyle.Bold;
            GetOrCreateText(
                card.transform,
                "Cooking Choice Help Text",
                "만드는 방법을 선택해 주세요.",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(80f, -92f),
                new Vector2(520f, 38f));

            var cookingButton = GetOrCreateTopLeftButton(
                card.transform,
                "Cooking Choice Cooking Button",
                "요리하기",
                new Vector2(64f, -150f),
                new Vector2(552f, 96f));
            var milkBlendingButton = GetOrCreateTopLeftButton(
                card.transform,
                "Cooking Choice Milk Blending Button",
                "<size=21>우유 블렌딩</size>\n<size=14>(낮은 확률로 특별한 음식 등장)</size>",
                new Vector2(64f, -266f),
                new Vector2(552f, 96f));
            RemoveChildIfExists(card.transform, "Cooking Choice Close Button");
            ApplyCareButtonStyle(cookingButton);
            ApplyCareButtonStyle(milkBlendingButton);
            cookingButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnDown = milkBlendingButton
            };
            milkBlendingButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = cookingButton
            };
            var cookingLabel = cookingButton.transform.Find("Label")?.GetComponent<Text>();
            if (cookingLabel != null)
            {
                cookingLabel.fontSize = 21;
                cookingLabel.alignment = TextAnchor.MiddleCenter;
                cookingLabel.resizeTextForBestFit = false;
            }

            var milkBlendingLabel = milkBlendingButton.transform.Find("Label")?.GetComponent<Text>();
            if (milkBlendingLabel != null)
            {
                milkBlendingLabel.supportRichText = true;
                milkBlendingLabel.fontSize = 21;
                milkBlendingLabel.alignment = TextAnchor.MiddleCenter;
                milkBlendingLabel.lineSpacing = 1.15f;
                milkBlendingLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                milkBlendingLabel.verticalOverflow = VerticalWrapMode.Truncate;
                milkBlendingLabel.resizeTextForBestFit = false;
            }

            var cookingPanel = canvasTransform.GetComponent<CookingPanelController>();
            var milkBlendingPanel = canvasTransform.GetComponent<MilkBlendingPanelController>();
            var controller = canvasTransform.GetComponent<CookingChoicePanelController>()
                ?? canvasTransform.gameObject.AddComponent<CookingChoicePanelController>();
            controller.Configure(
                overlay,
                cookingButton,
                milkBlendingButton,
                () =>
                {
                    canvasTransform.GetComponent<MilkPanelController>()?.Close();
                    canvasTransform.GetComponent<SnackPanelController>()?.Close();
                    cookingPanel?.Open();
                },
                () =>
                {
                    canvasTransform.GetComponent<CookingPanelController>()?.Close();
                    canvasTransform.GetComponent<MilkPanelController>()?.Close();
                    canvasTransform.GetComponent<SnackPanelController>()?.Close();
                    return milkBlendingPanel != null && milkBlendingPanel.Open();
                },
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureSleepSchedulePanel(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var sleepButton = canvasTransform.Find("Bottom Action Bar/Sleep Button")
                ?.GetComponent<Button>();
            if (sleepButton != null)
            {
                var careButton = sleepButton.GetComponent<MilkroomCareButton>()
                    ?? sleepButton.gameObject.AddComponent<MilkroomCareButton>();
                careButton.Configure(
                    MilkroomCareAction.SleepSchedule,
                    milkroomUi,
                    visualController);
                SetButtonLabel(sleepButton, "수면 예약");
                ApplyCareButtonStyle(sleepButton);
                SetButtonIcon(sleepButton, "rest");
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                SleepSchedulePanelController.OverlayObjectName,
                new Color(0.025f, 0.035f, 0.075f, 0.84f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Sleep Schedule Card",
                Vector2.zero,
                new Vector2(760f, 700f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(760f, 700f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.97f, 0.96f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Sleep Schedule Title Text",
                "수면 예약",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(36f, -24f),
                new Vector2(420f, 48f));
            title.fontStyle = FontStyle.Bold;
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Close Sleep Schedule Button",
                "닫기",
                new Vector2(610f, -24f),
                new Vector2(114f, 44f));
            ApplyCareButtonStyle(close);
            var summary = GetOrCreateText(
                card.transform,
                "Sleep Schedule Summary Text",
                "1~8시간 중 쉴 시간을 정해 주세요.",
                20,
                TextAnchor.MiddleLeft,
                new Vector2(36f, -88f),
                new Vector2(688f, 58f));
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;

            var detailPanel = GetOrCreatePanel(
                card.transform,
                "Sleep Schedule Detail Panel",
                new Vector2(36f, -158f),
                new Vector2(688f, 132f));
            if (detailPanel.TryGetComponent(out Image detailImage))
            {
                detailImage.color = new Color(0.91f, 0.92f, 0.99f, 1f);
            }

            var detail = GetOrCreateText(
                detailPanel.transform,
                "Sleep Schedule Detail Text",
                "예약은 저장되며, 실제로 쉰 시간만큼 회복해요.",
                18,
                TextAnchor.MiddleLeft,
                new Vector2(22f, -16f),
                new Vector2(644f, 100f));
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            GetOrCreateText(
                card.transform,
                "Sleep Duration Header Text",
                "수면 시간 선택",
                20,
                TextAnchor.MiddleLeft,
                new Vector2(36f, -314f),
                new Vector2(300f, 36f));

            var durationButtons = new Button[8];
            var durationLabels = new Text[8];
            for (var index = 0; index < durationButtons.Length; index += 1)
            {
                var row = index / 4;
                var column = index % 4;
                var button = GetOrCreateTopLeftButton(
                    card.transform,
                    $"Sleep Duration Button {index + 1}",
                    $"{index + 1}시간",
                    new Vector2(36f + (column * 174f), -358f - (row * 70f)),
                    new Vector2(158f, 54f));
                ApplyCareButtonStyle(button);
                durationButtons[index] = button;
                durationLabels[index] = button.transform.Find("Label")?.GetComponent<Text>();
            }

            var status = GetOrCreateText(
                card.transform,
                "Sleep Schedule Status Text",
                "원하는 시간을 선택한 뒤 예약을 시작하세요.",
                16,
                TextAnchor.MiddleLeft,
                new Vector2(36f, -510f),
                new Vector2(688f, 54f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            var start = GetOrCreateTopLeftButton(
                card.transform,
                "Start Sleep Schedule Button",
                "수면 시작",
                new Vector2(350f, -588f),
                new Vector2(176f, 58f));
            var wake = GetOrCreateTopLeftButton(
                card.transform,
                "Wake Sleep Schedule Button",
                "지금 깨우기",
                new Vector2(548f, -588f),
                new Vector2(176f, 58f));
            ApplyCareButtonStyle(start);
            ApplyCareButtonStyle(wake);
            var wakeLabel = wake.transform.Find("Label")?.GetComponent<Text>();

            overlay.SetActive(false);
            var panelController = canvasTransform.GetComponent<SleepSchedulePanelController>()
                ?? canvasTransform.gameObject.AddComponent<SleepSchedulePanelController>();
            var bridge = canvasTransform.GetComponent<SleepScheduleBridge>()
                ?? canvasTransform.gameObject.AddComponent<SleepScheduleBridge>();
            bridge.Configure(
                panelController,
                Application.isPlaying ? GameManager.Instance : null,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>(),
                milkroomUi,
                visualController,
                canvasTransform);
            panelController.Configure(
                overlay,
                title,
                summary,
                detail,
                status,
                durationLabels,
                wakeLabel,
                durationButtons,
                start,
                wake,
                close,
                bridge.GetSnapshot,
                bridge.StartSchedule,
                bridge.WakeSchedule,
                null,
                bridge.SetBlocking);
            overlay.transform.SetAsLastSibling();
        }

        private static void ApplySavedMilkroomTheme(GameManager manager)
        {
            var themeId = MilkroomThemeController.MorningThemeId;
            if (manager != null && manager.CurrentSave != null)
            {
                manager.CurrentSave.EnsureRuntimeDefaults();
                themeId = manager.CurrentSave.milkroomThemeId;
            }

            var themeController = Object.FindFirstObjectByType<MilkroomThemeController>();
            var lightingController = Object.FindFirstObjectByType<MilkroomLightingController>();
            var ambientController = Object.FindFirstObjectByType<MilkroomAmbientEventController>();
            themeController?.ApplyTheme(themeId);
            lightingController?.ApplyTheme(themeId);
            ambientController?.SetTheme(themeId);
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
            var autonomousMotionRoot = GameObject.Find("CheeseTama Autonomous Motion Root");
            if (autonomousMotionRoot != null)
            {
                ReparentIfFound("CheeseTama Autonomous Motion Root", character);
            }
            else
            {
                ReparentIfFound("CheeseTamaRoot", character);
                ReparentIfFound("CheeseTama Egg Placeholder", character);
            }
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
            if (name == "Milkroom Camera" || name == "Debug Camera")
            {
                camera.orthographic = false;
                camera.fieldOfView = 33f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 40f;
                camera.transform.position = new Vector3(0f, -0.78f, -11.8f);
                camera.transform.rotation = Quaternion.identity;

                if (name == "Milkroom Camera" && camera.GetComponent<MilkroomCameraFramer>() == null)
                {
                    camera.gameObject.AddComponent<MilkroomCameraFramer>();
                }
                else if (name == "Debug Camera")
                {
                    var framer = camera.GetComponent<MilkroomCameraFramer>();
                    if (framer != null)
                    {
                        DestroyObjectSafely(framer);
                    }
                }
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
            var palette = MilkroomThemePalette.For(MilkroomThemeController.MorningThemeId);
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
            keyLight.color = Color.Lerp(palette.Glow, Color.white, MilkroomLightingController.KeyWhiteBlend);
            keyLight.intensity = MilkroomLightingController.DayKeyIntensity;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = MilkroomLightingController.KeyShadowStrength;
            keyLight.shadowBias = MilkroomLightingController.KeyShadowBias;
            keyLight.shadowNormalBias = MilkroomLightingController.KeyShadowNormalBias;
            keyObject.transform.rotation = Quaternion.Euler(MilkroomLightingController.KeyRotationEuler);

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
            fillLight.color = Color.Lerp(palette.WindowSky, Color.white, MilkroomLightingController.FillWhiteBlend);
            fillLight.intensity = MilkroomLightingController.DayFillIntensity;
            fillLight.shadows = LightShadows.None;
            fillObject.transform.rotation = Quaternion.Euler(MilkroomLightingController.FillRotationEuler);

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
            rimLight.color = Color.Lerp(palette.Celestial, new Color(1f, 0.82f, 0.38f), 0.35f);
            rimLight.intensity = MilkroomLightingController.DayRimIntensity;
            rimLight.shadows = LightShadows.None;
            rimObject.transform.rotation = Quaternion.Euler(MilkroomLightingController.RimRotationEuler);

            var volumeObject = GameObject.Find("GlobalVolume");
            if (volumeObject == null)
            {
                volumeObject = new GameObject("GlobalVolume");
            }

            ConfigureGlobalVolumeIfAvailable(volumeObject);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = MilkroomLightingController.ResolveAmbientColor(
                MilkroomThemeController.MorningThemeId,
                palette);
        }

        private static void ConfigureGlobalVolumeIfAvailable(GameObject volumeObject)
        {
            var volumeType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volumeType == null || volumeObject == null)
            {
                return;
            }

            var volume = volumeObject.GetComponent(volumeType);
            if (volume == null)
            {
                volume = volumeObject.AddComponent(volumeType);
            }

            SetVolumeMember(volume, "isGlobal", true);
            SetVolumeMember(volume, "priority", 0f);
            SetVolumeMember(volume, "weight", 0.35f);
        }

        private static void SetVolumeMember(Component volume, string memberName, object value)
        {
            if (volume == null)
            {
                return;
            }

            var type = volume.GetType();
            var property = type.GetProperty(memberName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(volume, value);
                return;
            }

            var field = type.GetField(memberName);
            if (field != null)
            {
                field.SetValue(volume, value);
            }
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

            var roomShell = CreateGroupRoot(root, "RoomShell");
            var fridgeSet = CreateGroupRoot(root, "FridgeSet");
            var playArea = CreateGroupRoot(root, "PlayArea");
            var cozyChair = CreateGroupRoot(root, "CozyChair");
            var foreground = CreateGroupRoot(root, "Foreground");
            var themeVfxRoot = CreateGroupRoot(root, "ThemeVFXRoot");

            CreateDioramaRoomShell(roomShell);
            CreateDioramaFridgeSet(fridgeSet);
            CreateDioramaCozyChair(cozyChair);
            CreateGroupRoot(playArea, "CheeseTamaAnchor").localPosition = new Vector3(0f, -0.28f, 0.05f);
            AddMilkroomControllers(root, roomShell, root, playArea, foreground, themeVfxRoot);
            EnsureGeneratedMilkroomProps(root);
        }

        private static void EnsureGeneratedMilkroomProps(Transform root)
        {
#if UNITY_EDITOR
            const float floorTop = -2.13f;

            // Hide the legacy primitive prop groups that the generated meshes replace.
            foreach (var groupName in new[] { "FridgeSet", "CozyChair" })
            {
                var group = root.Find(groupName);
                if (group != null)
                {
                    group.gameObject.SetActive(false);
                }
            }

            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/Fridge.prefab", "Fridge_Model",
                new Vector3(-1.75f, 0f, 2.35f), 2.1f, 180f, true, 0f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/MilkShelf.prefab", "MilkShelf_Model",
                new Vector3(2.65f, 0f, 2.295f), 1.3f, 180f, false, -0.15f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/CozyChair.prefab", "CozyChair_Model",
                new Vector3(-2.7f, 0f, 0.2f), 1.5f, 150f, true, 0f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/Window.prefab", "Window_Model",
                new Vector3(0.45f, 0f, 2.366f), 1.72f, 180f, false, -0.15f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/Rug.prefab", "Rug_Model",
                new Vector3(0.005f, 0f, 0.28f), RugPlacedHeight, 0f, true, 0f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/DresserTable.prefab", "DresserTable_Model",
                new Vector3(2.807f, 0f, 1.18f), 1.5f, 200f, true, 0f, floorTop);
            PlaceGeneratedProp(root, "Assets/Environments/Milkroom/Props/Chalkboard.prefab", "Chalkboard_Model",
                new Vector3(-2.78f, 0f, 2.41f), 1.18f, 0f, false, 0.05f, floorTop);
#endif
        }

#if UNITY_EDITOR
        private static void PlaceGeneratedProp(
            Transform parent,
            string prefabPath,
            string instanceName,
            Vector3 xzAnchor,
            float targetHeight,
            float yaw,
            bool onFloor,
            float centerY,
            float floorTop)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return;
            }

            var existing = parent.Find(instanceName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
            go.name = instanceName;
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            var scale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
            go.transform.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            var posY = onFloor
                ? go.transform.position.y + (floorTop - bounds.min.y)
                : go.transform.position.y + (centerY - bounds.center.y);
            go.transform.position = new Vector3(xzAnchor.x, posY, xzAnchor.z);
        }
#endif

        private static Transform CreateGroupRoot(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = Vector3.zero;
            group.localRotation = Quaternion.identity;
            group.localScale = Vector3.one;
            return group;
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
            CreateDecorPart(root, "BackWall", PrimitiveType.Cube, new Vector3(0f, -0.55f, 2.64f), new Vector3(7.9f, 3.15f, 0.24f), new Color(0.86f, 0.72f, 0.54f));
            CreateDecorPart(root, "LeftWall", PrimitiveType.Cube, new Vector3(-4.02f, -0.55f, 0.84f), new Vector3(0.24f, 3.15f, 3.6f), new Color(0.78f, 0.6f, 0.42f));
            CreateDecorPart(root, "RightWall", PrimitiveType.Cube, new Vector3(4.02f, -0.55f, 0.84f), new Vector3(0.24f, 3.15f, 3.6f), new Color(0.78f, 0.6f, 0.42f));
            CreateDecorPart(root, "Floor", PrimitiveType.Cube, new Vector3(0f, -2.24f, 0.84f), new Vector3(7.9f, 0.22f, 3.6f), new Color(0.5f, 0.29f, 0.14f));
            CreateDecorPart(root, "BackWall Baseboard", PrimitiveType.Cube, new Vector3(0f, -2.04f, 2.5f), new Vector3(7.65f, 0.18f, 0.1f), new Color(0.46f, 0.27f, 0.14f));
            CreateDecorPart(root, "LeftWall Baseboard", PrimitiveType.Cube, new Vector3(-3.82f, -2.04f, 0.76f), new Vector3(0.1f, 0.18f, 3.35f), new Color(0.46f, 0.27f, 0.14f));
            CreateDecorPart(root, "RightWall Baseboard", PrimitiveType.Cube, new Vector3(3.82f, -2.04f, 0.76f), new Vector3(0.1f, 0.18f, 3.35f), new Color(0.46f, 0.27f, 0.14f));
        }

        private static void CreateDioramaWindowSet(Transform root)
        {
            CreateDecorPart(root, "WindowGlass", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.84f), new Vector3(2.35f, 1.45f, 0.08f), new Color(0.58f, 0.78f, 0.92f));
            CreateDecorPart(root, "Window Sun Glow", PrimitiveType.Sphere, new Vector3(-0.1f, 1.55f, 2.74f), new Vector3(0.5f, 0.5f, 0.08f), new Color(1f, 0.8f, 0.34f));
            CreateDecorPart(root, "Window Cloud Left", PrimitiveType.Sphere, new Vector3(-1.3f, 1.34f, 2.68f), new Vector3(0.46f, 0.16f, 0.05f), new Color(0.94f, 0.98f, 1f));
            CreateDecorPart(root, "Window Cloud Right", PrimitiveType.Sphere, new Vector3(-0.62f, 0.98f, 2.68f), new Vector3(0.42f, 0.14f, 0.05f), new Color(0.94f, 0.98f, 1f));

            var frameColor = new Color(0.96f, 0.83f, 0.58f);
            CreateDecorPart(root, "Window Arch Glow", PrimitiveType.Sphere, new Vector3(-0.75f, 1.92f, 2.72f), new Vector3(1.38f, 0.5f, 0.07f), new Color(0.95f, 0.83f, 0.56f));
            CreateDecorPart(root, "Window Arch Frame", PrimitiveType.Sphere, new Vector3(-0.75f, 1.91f, 2.57f), new Vector3(1.48f, 0.55f, 0.12f), new Color(0.68f, 0.39f, 0.18f));
            CreateDecorPart(root, "Window Arch Inner Cut", PrimitiveType.Sphere, new Vector3(-0.75f, 1.86f, 2.49f), new Vector3(1.22f, 0.38f, 0.12f), new Color(0.58f, 0.78f, 0.92f));
            CreateDecorPart(root, "WindowFrame Top", PrimitiveType.Cube, new Vector3(-0.75f, 1.92f, 2.62f), new Vector3(2.62f, 0.11f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Bottom", PrimitiveType.Cube, new Vector3(-0.75f, 0.38f, 2.62f), new Vector3(2.62f, 0.13f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Left", PrimitiveType.Cube, new Vector3(-2.06f, 1.15f, 2.62f), new Vector3(0.13f, 1.62f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Right", PrimitiveType.Cube, new Vector3(0.56f, 1.15f, 2.62f), new Vector3(0.13f, 1.62f, 0.18f), frameColor);
            CreateDecorPart(root, "WindowFrame Vertical", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.56f), new Vector3(0.09f, 1.5f, 0.14f), frameColor);
            CreateDecorPart(root, "WindowFrame Horizontal", PrimitiveType.Cube, new Vector3(-0.75f, 1.15f, 2.55f), new Vector3(2.42f, 0.09f, 0.14f), frameColor);
            CreateDecorPart(root, "Window Handle Left", PrimitiveType.Sphere, new Vector3(-0.88f, 1.08f, 2.42f), new Vector3(0.035f, 0.055f, 0.025f), new Color(0.82f, 0.56f, 0.24f));
            CreateDecorPart(root, "Window Handle Right", PrimitiveType.Sphere, new Vector3(-0.62f, 1.08f, 2.42f), new Vector3(0.035f, 0.055f, 0.025f), new Color(0.82f, 0.56f, 0.24f));

            CreateDecorPart(root, "Curtain Rod", PrimitiveType.Cylinder, new Vector3(-0.75f, 2.12f, 2.42f), new Vector3(0.035f, 1.92f, 0.035f), Quaternion.Euler(0f, 0f, 90f), new Color(0.64f, 0.38f, 0.19f));
            CreateDecorPart(root, "Curtain Rod Knob L", PrimitiveType.Sphere, new Vector3(-2.55f, 2.12f, 2.42f), new Vector3(0.12f, 0.12f, 0.08f), new Color(0.72f, 0.45f, 0.24f));
            CreateDecorPart(root, "Curtain Rod Knob R", PrimitiveType.Sphere, new Vector3(1.05f, 2.12f, 2.42f), new Vector3(0.12f, 0.12f, 0.08f), new Color(0.72f, 0.45f, 0.24f));
            CreateDecorPart(root, "Curtains Left", PrimitiveType.Cube, new Vector3(-2.38f, 1.12f, 2.46f), new Vector3(0.42f, 1.78f, 0.16f), new Color(0.98f, 0.82f, 0.64f));
            CreateDecorPart(root, "Curtains Right", PrimitiveType.Cube, new Vector3(0.88f, 1.12f, 2.46f), new Vector3(0.42f, 1.78f, 0.16f), new Color(0.98f, 0.82f, 0.64f));
            for (var i = 0; i < 4; i += 1)
            {
                var leftX = -2.55f + i * 0.13f;
                var rightX = 0.74f + i * 0.13f;
                var y = 1.18f - (i % 2) * 0.04f;
                CreateDecorPart(root, $"Curtain Left Fold {i + 1}", PrimitiveType.Cube, new Vector3(leftX, y, 2.28f), new Vector3(0.038f, 1.64f, 0.07f), new Color(1f, 0.9f, 0.72f));
                CreateDecorPart(root, $"Curtain Right Fold {i + 1}", PrimitiveType.Cube, new Vector3(rightX, y, 2.28f), new Vector3(0.038f, 1.64f, 0.07f), new Color(1f, 0.9f, 0.72f));
            }
            CreateDecorPart(root, "Curtains Left Tie", PrimitiveType.Cube, new Vector3(-2.24f, 0.85f, 2.3f), new Vector3(0.36f, 0.09f, 0.09f), new Color(0.78f, 0.48f, 0.24f));
            CreateDecorPart(root, "Curtains Right Tie", PrimitiveType.Cube, new Vector3(0.74f, 0.85f, 2.3f), new Vector3(0.36f, 0.09f, 0.09f), new Color(0.78f, 0.48f, 0.24f));
            CreateDecorPart(root, "Window Sill", PrimitiveType.Cube, new Vector3(-0.75f, 0.25f, 2.42f), new Vector3(2.75f, 0.13f, 0.24f), new Color(0.7f, 0.43f, 0.22f));
            CreateDecorPart(root, "Window Tiny Plant Pot", PrimitiveType.Cube, new Vector3(0.18f, 0.56f, 2.28f), new Vector3(0.22f, 0.16f, 0.08f), new Color(0.62f, 0.34f, 0.18f));
            CreateDecorPart(root, "Window Tiny Plant Leaf A", PrimitiveType.Sphere, new Vector3(0.08f, 0.75f, 2.22f), new Vector3(0.13f, 0.08f, 0.035f), new Color(0.34f, 0.62f, 0.34f));
            CreateDecorPart(root, "Window Tiny Plant Leaf B", PrimitiveType.Sphere, new Vector3(0.25f, 0.76f, 2.22f), new Vector3(0.13f, 0.08f, 0.035f), new Color(0.38f, 0.68f, 0.38f));
        }

        private static void CreateDioramaFridgeSet(Transform root)
        {
            CreateDecorPart(root, "Fridge Body Rounded", PrimitiveType.Cube, new Vector3(-3.35f, -0.55f, 1.72f), new Vector3(0.9f, 1.65f, 0.62f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Fridge Top Round", PrimitiveType.Sphere, new Vector3(-3.35f, 0.32f, 1.72f), new Vector3(0.46f, 0.18f, 0.32f), new Color(0.98f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Top Left Corner", PrimitiveType.Sphere, new Vector3(-3.78f, 0.27f, 1.44f), new Vector3(0.12f, 0.18f, 0.12f), new Color(0.98f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Top Right Corner", PrimitiveType.Sphere, new Vector3(-2.92f, 0.27f, 1.44f), new Vector3(0.12f, 0.18f, 0.12f), new Color(0.98f, 0.95f, 0.84f));
            CreateDecorPart(root, "Fridge Lower Left Corner", PrimitiveType.Sphere, new Vector3(-3.78f, -1.34f, 1.44f), new Vector3(0.12f, 0.12f, 0.1f), new Color(0.9f, 0.84f, 0.72f));
            CreateDecorPart(root, "Fridge Lower Right Corner", PrimitiveType.Sphere, new Vector3(-2.92f, -1.34f, 1.44f), new Vector3(0.12f, 0.12f, 0.1f), new Color(0.9f, 0.84f, 0.72f));
            CreateDecorPart(root, "Fridge Soft Shine", PrimitiveType.Cube, new Vector3(-3.66f, -0.2f, 1.26f), new Vector3(0.055f, 1.02f, 0.035f), new Color(1f, 0.98f, 0.88f));
            CreateDecorPart(root, "Fridge Door Split", PrimitiveType.Cube, new Vector3(-3.35f, -0.32f, 1.36f), new Vector3(0.76f, 0.035f, 0.06f), new Color(0.74f, 0.58f, 0.4f));
            CreateDecorPart(root, "Fridge Handle", PrimitiveType.Cylinder, new Vector3(-2.94f, -0.36f, 1.3f), new Vector3(0.04f, 0.28f, 0.04f), new Color(0.68f, 0.43f, 0.22f));
            CreateDecorPart(root, "Fridge Face Eye L", PrimitiveType.Sphere, new Vector3(-3.48f, -0.7f, 1.29f), new Vector3(0.048f, 0.048f, 0.032f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Face Eye R", PrimitiveType.Sphere, new Vector3(-3.22f, -0.7f, 1.29f), new Vector3(0.048f, 0.048f, 0.032f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Smile", PrimitiveType.Cube, new Vector3(-3.35f, -0.84f, 1.26f), new Vector3(0.16f, 0.025f, 0.025f), new Color(0.24f, 0.14f, 0.08f));
            CreateDecorPart(root, "Fridge Milk Memo", PrimitiveType.Cube, new Vector3(-3.12f, 0.02f, 1.27f), new Vector3(0.22f, 0.18f, 0.035f), new Color(1f, 0.76f, 0.34f));
            CreateDecorPart(root, "Fridge Star Magnet", PrimitiveType.Sphere, new Vector3(-3.5f, 0.05f, 1.26f), new Vector3(0.08f, 0.08f, 0.025f), new Color(1f, 0.76f, 0.22f));
            CreateDecorPart(root, "Fridge Blue Memo", PrimitiveType.Cube, new Vector3(-3.56f, -0.18f, 1.25f), new Vector3(0.18f, 0.14f, 0.03f), new Color(0.64f, 0.82f, 0.92f));
        }

        private static void CreateDioramaMilkShelfSet(Transform root)
        {
            CreateDecorPart(root, "MilkShelf Back", PrimitiveType.Cube, new Vector3(1.75f, 0f, 2.38f), new Vector3(1.65f, 1.18f, 0.16f), new Color(0.5f, 0.3f, 0.16f));
            CreateDecorPart(root, "MilkShelf Top", PrimitiveType.Cube, new Vector3(1.75f, 0.56f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));
            CreateDecorPart(root, "MilkShelf Middle", PrimitiveType.Cube, new Vector3(1.75f, 0.05f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));
            CreateDecorPart(root, "MilkShelf Bottom", PrimitiveType.Cube, new Vector3(1.75f, -0.48f, 2.08f), new Vector3(1.82f, 0.09f, 0.2f), new Color(0.68f, 0.42f, 0.22f));
            CreateDecorPart(root, "MilkShelf Left Upright", PrimitiveType.Cube, new Vector3(0.9f, 0.06f, 1.98f), new Vector3(0.08f, 1.28f, 0.12f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "MilkShelf Right Upright", PrimitiveType.Cube, new Vector3(2.6f, 0.06f, 1.98f), new Vector3(0.08f, 1.28f, 0.12f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "MilkShelf Bracket L", PrimitiveType.Cube, new Vector3(1.0f, -0.22f, 1.96f), new Vector3(0.1f, 0.5f, 0.08f), Quaternion.Euler(0f, 0f, -35f), new Color(0.52f, 0.3f, 0.15f));
            CreateDecorPart(root, "MilkShelf Bracket R", PrimitiveType.Cube, new Vector3(2.5f, -0.22f, 1.96f), new Vector3(0.1f, 0.5f, 0.08f), Quaternion.Euler(0f, 0f, 35f), new Color(0.52f, 0.3f, 0.15f));

            for (var i = 0; i < 4; i += 1)
            {
                CreateMilkBottle(root, $"MilkShelf Bottle Top {i + 1}", new Vector3(1.13f + i * 0.38f, 0.82f, 1.94f), 0.32f);
            }

            for (var i = 0; i < 3; i += 1)
            {
                CreateMilkBottle(root, $"MilkShelf Bottle Lower {i + 1}", new Vector3(1.32f + i * 0.42f, 0.3f, 1.94f), 0.28f);
            }

            CreateCheeseBlock(root, "Shelf Cheese Sample", new Vector3(2.44f, -0.2f, 1.88f), 0.22f);
            CreateDecorPart(root, "Shelf Vine Stem", PrimitiveType.Cube, new Vector3(2.66f, 0.9f, 1.98f), new Vector3(0.035f, 0.62f, 0.035f), Quaternion.Euler(0f, 0f, -12f), new Color(0.25f, 0.46f, 0.22f));
            for (var i = 0; i < 5; i += 1)
            {
                var y = 1.14f - i * 0.15f;
                var x = 2.62f + (i % 2 == 0 ? -0.08f : 0.08f);
                CreateDecorPart(root, $"Shelf Vine Leaf {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 1.9f), new Vector3(0.1f, 0.055f, 0.028f), new Color(0.34f, 0.63f, 0.34f));
            }
        }

        private static void CreateDioramaBlendingTableSet(Transform root)
        {
            CreateDecorPart(root, "BlendingTable Top", PrimitiveType.Cube, new Vector3(2.9f, -1.15f, 1.0f), new Vector3(1.25f, 0.18f, 0.58f), new Color(0.66f, 0.39f, 0.2f));
            CreateDecorPart(root, "BlendingTable Cloth", PrimitiveType.Cube, new Vector3(2.9f, -1.03f, 0.7f), new Vector3(1.36f, 0.09f, 0.12f), new Color(1f, 0.88f, 0.64f));
            CreateDecorPart(root, "BlendingTable Body", PrimitiveType.Cube, new Vector3(2.9f, -1.42f, 1.06f), new Vector3(1.18f, 0.62f, 0.42f), new Color(0.58f, 0.33f, 0.16f));
            CreateDecorPart(root, "BlendingTable Leg L", PrimitiveType.Cube, new Vector3(2.42f, -1.62f, 1.02f), new Vector3(0.09f, 0.76f, 0.09f), new Color(0.48f, 0.27f, 0.13f));
            CreateDecorPart(root, "BlendingTable Leg R", PrimitiveType.Cube, new Vector3(3.38f, -1.62f, 1.02f), new Vector3(0.09f, 0.76f, 0.09f), new Color(0.48f, 0.27f, 0.13f));
            for (var i = 0; i < 3; i += 1)
            {
                var x = 2.5f + i * 0.4f;
                CreateDecorPart(root, $"Blending Drawer {i + 1}", PrimitiveType.Cube, new Vector3(x, -1.4f, 0.78f), new Vector3(0.32f, 0.22f, 0.045f), new Color(0.7f, 0.43f, 0.22f));
                CreateDecorPart(root, $"Blending Drawer Pull {i + 1}", PrimitiveType.Sphere, new Vector3(x, -1.4f, 0.73f), new Vector3(0.04f, 0.04f, 0.022f), new Color(0.98f, 0.7f, 0.28f));
            }

            CreateDecorPart(root, "Blending Bowl", PrimitiveType.Sphere, new Vector3(2.72f, -0.92f, 0.66f), new Vector3(0.28f, 0.12f, 0.12f), new Color(0.82f, 0.94f, 0.98f));
            CreateDecorPart(root, "Blending Spoon", PrimitiveType.Cube, new Vector3(3.1f, -0.86f, 0.62f), new Vector3(0.45f, 0.035f, 0.03f), new Color(0.82f, 0.6f, 0.34f));
            CreateMilkBottle(root, "Blending Milk Bottle", new Vector3(3.32f, -0.78f, 0.58f), 0.34f);
            CreateDecorPart(root, "Blender Base", PrimitiveType.Cube, new Vector3(3.55f, -0.86f, 0.66f), new Vector3(0.24f, 0.18f, 0.12f), new Color(0.9f, 0.78f, 0.58f));
            CreateDecorPart(root, "Blender Jar", PrimitiveType.Capsule, new Vector3(3.55f, -0.62f, 0.64f), new Vector3(0.14f, 0.2f, 0.08f), new Color(0.78f, 0.9f, 0.98f));
            CreateDecorPart(root, "Blender Milk Fill", PrimitiveType.Sphere, new Vector3(3.55f, -0.68f, 0.58f), new Vector3(0.12f, 0.06f, 0.03f), new Color(0.98f, 0.94f, 0.78f));
        }

        private static void CreateDioramaChalkboardSet(Transform root)
        {
            CreateDecorPart(root, "Chalkboard", PrimitiveType.Cube, new Vector3(-2.52f, 1.15f, 2.54f), new Vector3(0.92f, 0.72f, 0.08f), new Color(0.15f, 0.25f, 0.2f));
            CreateDecorPart(root, "Chalkboard Frame Top", PrimitiveType.Cube, new Vector3(-2.52f, 1.55f, 2.47f), new Vector3(1.08f, 0.08f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Bottom", PrimitiveType.Cube, new Vector3(-2.52f, 0.75f, 2.47f), new Vector3(1.08f, 0.08f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Left", PrimitiveType.Cube, new Vector3(-3.06f, 1.15f, 2.47f), new Vector3(0.08f, 0.82f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateDecorPart(root, "Chalkboard Frame Right", PrimitiveType.Cube, new Vector3(-1.98f, 1.15f, 2.47f), new Vector3(0.08f, 0.82f, 0.06f), new Color(0.58f, 0.34f, 0.17f));
            CreateWorldLabel(root, "Chalkboard Text", "\uC6B0\uC720\uB294\n\uB9C8\uBC95", new Vector3(-2.52f, 1.16f, 2.39f), 0.075f, new Color(1f, 0.9f, 0.62f));
            CreateDecorPart(root, "Chalkboard Hanger L", PrimitiveType.Cube, new Vector3(-2.82f, 1.78f, 2.44f), new Vector3(0.03f, 0.42f, 0.03f), Quaternion.Euler(0f, 0f, -34f), new Color(0.64f, 0.42f, 0.2f));
            CreateDecorPart(root, "Chalkboard Hanger R", PrimitiveType.Cube, new Vector3(-2.22f, 1.78f, 2.44f), new Vector3(0.03f, 0.42f, 0.03f), Quaternion.Euler(0f, 0f, 34f), new Color(0.64f, 0.42f, 0.2f));
            CreateDecorPart(root, "Chalkboard Cheese Doodle", PrimitiveType.Sphere, new Vector3(-2.86f, 0.88f, 2.38f), new Vector3(0.08f, 0.06f, 0.018f), new Color(1f, 0.78f, 0.28f));
            CreateDecorPart(root, "Chalkboard Star Doodle", PrimitiveType.Sphere, new Vector3(-2.18f, 1.42f, 2.38f), new Vector3(0.045f, 0.045f, 0.016f), new Color(1f, 0.92f, 0.5f));
        }

        private static void CreateDioramaRug(Transform root)
        {
            CreateDecorPart(root, "Rug Base", PrimitiveType.Sphere, new Vector3(0f, -2.05f, 0.62f), new Vector3(1.92f, 0.13f, 0.82f), new Color(0.9f, 0.78f, 0.56f));
            CreateDecorPart(root, "Rug Soft Center", PrimitiveType.Sphere, new Vector3(0f, -2.0f, 0.52f), new Vector3(1.55f, 0.08f, 0.62f), new Color(1f, 0.9f, 0.68f));
            CreateDecorPart(root, "Rug Paw Center", PrimitiveType.Sphere, new Vector3(0f, -1.95f, 0.42f), new Vector3(0.32f, 0.04f, 0.08f), new Color(0.78f, 0.62f, 0.42f));
            CreateDecorPart(root, "Rug Paw Toe L", PrimitiveType.Sphere, new Vector3(-0.34f, -1.9f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
            CreateDecorPart(root, "Rug Paw Toe C", PrimitiveType.Sphere, new Vector3(0f, -1.86f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
            CreateDecorPart(root, "Rug Paw Toe R", PrimitiveType.Sphere, new Vector3(0.34f, -1.9f, 0.42f), new Vector3(0.13f, 0.035f, 0.06f), new Color(0.82f, 0.66f, 0.46f));
            for (var i = 0; i < 24; i += 1)
            {
                var angle = i / 24f * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * 1.78f;
                var z = 0.58f + Mathf.Sin(angle) * 0.72f;
                var width = 0.18f + (i % 3) * 0.018f;
                CreateDecorPart(root, $"Rug Tuft Rim {i + 1}", PrimitiveType.Sphere, new Vector3(x, -1.93f, z), new Vector3(width, 0.06f, 0.1f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f), new Color(0.96f, 0.86f, 0.66f));
            }

            for (var i = 0; i < 8; i += 1)
            {
                var x = -1.0f + i * 0.28f;
                CreateDecorPart(root, $"Rug Soft Stitch {i + 1}", PrimitiveType.Cube, new Vector3(x, -1.88f, 0.05f + (i % 2) * 0.08f), new Vector3(0.12f, 0.018f, 0.025f), Quaternion.Euler(0f, 18f, 0f), new Color(0.88f, 0.74f, 0.54f));
            }
        }

        private static void CreateDioramaCozyChair(Transform root)
        {
            CreateDecorPart(root, "CozyChair Back", PrimitiveType.Cube, new Vector3(-3.55f, -1.1f, 0.42f), new Vector3(0.92f, 0.8f, 0.34f), new Color(0.58f, 0.4f, 0.28f));
            CreateDecorPart(root, "CozyChair Seat", PrimitiveType.Cube, new Vector3(-3.55f, -1.62f, 0.12f), new Vector3(1.02f, 0.26f, 0.58f), new Color(0.72f, 0.52f, 0.36f));
            CreateDecorPart(root, "CozyChair Back Cushion", PrimitiveType.Sphere, new Vector3(-3.55f, -1.04f, 0.18f), new Vector3(0.78f, 0.42f, 0.12f), new Color(0.9f, 0.78f, 0.62f));
            CreateDecorPart(root, "CozyChair Seat Cushion", PrimitiveType.Sphere, new Vector3(-3.55f, -1.56f, -0.08f), new Vector3(0.86f, 0.18f, 0.26f), new Color(0.92f, 0.8f, 0.64f));
            CreateDecorPart(root, "CozyChair Arm L", PrimitiveType.Cube, new Vector3(-4.12f, -1.38f, 0.22f), new Vector3(0.16f, 0.52f, 0.48f), new Color(0.5f, 0.31f, 0.18f));
            CreateDecorPart(root, "CozyChair Arm R", PrimitiveType.Cube, new Vector3(-2.98f, -1.38f, 0.22f), new Vector3(0.16f, 0.52f, 0.48f), new Color(0.5f, 0.31f, 0.18f));
            CreateDecorPart(root, "CozyChair Arm L Round", PrimitiveType.Cylinder, new Vector3(-4.12f, -1.08f, 0.03f), new Vector3(0.08f, 0.34f, 0.08f), Quaternion.Euler(90f, 0f, 0f), new Color(0.64f, 0.42f, 0.23f));
            CreateDecorPart(root, "CozyChair Arm R Round", PrimitiveType.Cylinder, new Vector3(-2.98f, -1.08f, 0.03f), new Vector3(0.08f, 0.34f, 0.08f), Quaternion.Euler(90f, 0f, 0f), new Color(0.64f, 0.42f, 0.23f));
            CreateDecorPart(root, "CozyChair Butter Cushion", PrimitiveType.Cube, new Vector3(-3.55f, -1.22f, -0.04f), new Vector3(0.46f, 0.32f, 0.12f), new Color(1f, 0.72f, 0.28f));
            CreateDecorPart(root, "Butter Cushion Hole A", PrimitiveType.Sphere, new Vector3(-3.67f, -1.2f, -0.12f), new Vector3(0.05f, 0.045f, 0.018f), new Color(0.85f, 0.48f, 0.1f));
            CreateDecorPart(root, "Butter Cushion Hole B", PrimitiveType.Sphere, new Vector3(-3.45f, -1.26f, -0.12f), new Vector3(0.04f, 0.04f, 0.018f), new Color(0.85f, 0.48f, 0.1f));
            CreateDecorPart(root, "CozyChair Leg L", PrimitiveType.Cube, new Vector3(-3.96f, -1.88f, 0.2f), new Vector3(0.12f, 0.42f, 0.12f), new Color(0.48f, 0.28f, 0.14f));
            CreateDecorPart(root, "CozyChair Leg R", PrimitiveType.Cube, new Vector3(-3.14f, -1.88f, 0.2f), new Vector3(0.12f, 0.42f, 0.12f), new Color(0.48f, 0.28f, 0.14f));
        }

        private static void CreateDioramaLamps(Transform root)
        {
            CreateDecorPart(root, "Pendant Cord", PrimitiveType.Cube, new Vector3(0.2f, 2.55f, 1.55f), new Vector3(0.035f, 0.58f, 0.035f), new Color(0.34f, 0.2f, 0.1f));
            for (var i = 0; i < 4; i += 1)
            {
                CreateDecorPart(root, $"Pendant Chain Link {i + 1}", PrimitiveType.Cube, new Vector3(0.2f, 2.74f - i * 0.13f, 1.48f), new Vector3(0.05f, 0.07f, 0.025f), Quaternion.Euler(0f, 0f, i % 2 == 0 ? 45f : -45f), new Color(0.36f, 0.22f, 0.12f));
            }

            CreateDecorPart(root, "Pendant Warm Shade", PrimitiveType.Sphere, new Vector3(0.2f, 2.16f, 1.55f), new Vector3(0.36f, 0.2f, 0.24f), new Color(1f, 0.74f, 0.32f));
            for (var i = 0; i < 5; i += 1)
            {
                CreateDecorPart(root, $"Pendant Shade Scallop {i + 1}", PrimitiveType.Sphere, new Vector3(-0.08f + i * 0.14f, 2.04f, 1.36f), new Vector3(0.08f, 0.05f, 0.035f), new Color(1f, 0.86f, 0.46f));
            }

            CreateDecorPart(root, "Pendant Bulb", PrimitiveType.Sphere, new Vector3(0.2f, 1.98f, 1.42f), new Vector3(0.12f, 0.16f, 0.08f), new Color(1f, 0.96f, 0.72f));
            CreateDecorPart(root, "Pendant Warm Glow", PrimitiveType.Sphere, new Vector3(0.2f, 1.94f, 1.48f), new Vector3(0.54f, 0.22f, 0.28f), new Color(1f, 0.8f, 0.42f));
            CreateStarLamp(root, "Left Star Lamp", new Vector3(-3.86f, 1.68f, 2.34f), 0.24f);
            CreateStarLamp(root, "Window Hanging Star", new Vector3(-0.28f, 1.7f, 2.32f), 0.14f);
        }

        private static void CreateDioramaProps(Transform root)
        {
            CreateDecorPart(root, "Plant Pot", PrimitiveType.Cube, new Vector3(0.84f, -1.68f, 2.12f), new Vector3(0.34f, 0.24f, 0.28f), new Color(0.56f, 0.31f, 0.17f));
            CreateDecorPart(root, "Plant Leaf L", PrimitiveType.Sphere, new Vector3(0.68f, -1.38f, 2.04f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.32f, 0.58f, 0.32f));
            CreateDecorPart(root, "Plant Leaf R", PrimitiveType.Sphere, new Vector3(1.0f, -1.36f, 2.04f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.36f, 0.64f, 0.36f));
            CreateDecorPart(root, "Plant Leaf Tall A", PrimitiveType.Sphere, new Vector3(0.82f, -1.18f, 2.02f), new Vector3(0.12f, 0.24f, 0.06f), Quaternion.Euler(0f, 0f, -18f), new Color(0.28f, 0.55f, 0.28f));
            CreateDecorPart(root, "Plant Leaf Tall B", PrimitiveType.Sphere, new Vector3(0.98f, -1.18f, 2f), new Vector3(0.12f, 0.22f, 0.06f), Quaternion.Euler(0f, 0f, 22f), new Color(0.38f, 0.68f, 0.38f));
            CreateDecorPart(root, "Left Plant Pot", PrimitiveType.Cube, new Vector3(-4.18f, -1.64f, 1.68f), new Vector3(0.36f, 0.22f, 0.18f), new Color(0.58f, 0.33f, 0.18f));
            for (var i = 0; i < 6; i += 1)
            {
                var angle = -45f + i * 18f;
                var x = -4.18f + Mathf.Cos(angle * Mathf.Deg2Rad) * 0.18f;
                var y = -1.34f + i % 3 * 0.08f;
                CreateDecorPart(root, $"Left Plant Leaf {i + 1}", PrimitiveType.Sphere, new Vector3(x, y, 1.58f), new Vector3(0.22f, 0.1f, 0.05f), Quaternion.Euler(0f, 0f, angle), new Color(0.3f, 0.56f + i * 0.015f, 0.3f));
            }

            CreateDecorPart(root, "Wall Memo A", PrimitiveType.Cube, new Vector3(3.75f, 0.78f, 2.52f), new Vector3(0.28f, 0.22f, 0.035f), new Color(1f, 0.86f, 0.52f));
            CreateDecorPart(root, "Wall Memo B", PrimitiveType.Cube, new Vector3(3.42f, 0.42f, 2.52f), new Vector3(0.22f, 0.18f, 0.035f), new Color(0.78f, 0.92f, 1f));
            CreateDecorPart(root, "Wall Memo Pin A", PrimitiveType.Sphere, new Vector3(3.75f, 0.88f, 2.47f), new Vector3(0.025f, 0.025f, 0.012f), new Color(0.72f, 0.36f, 0.16f));
            CreateDecorPart(root, "Wall Memo Pin B", PrimitiveType.Sphere, new Vector3(3.42f, 0.5f, 2.47f), new Vector3(0.025f, 0.025f, 0.012f), new Color(0.72f, 0.36f, 0.16f));
            CreateMilkBottle(root, "Loose Milk Bottle", new Vector3(-0.96f, -1.66f, 0.12f), 0.3f);
            CreateCheeseBlock(root, "Foreground Cheese Cube", new Vector3(1.6f, -1.74f, -0.28f), 0.24f);
            CreateDecorPart(root, "Foreground Soft Milk Drop L", PrimitiveType.Sphere, new Vector3(-1.8f, -2.02f, -0.68f), new Vector3(0.22f, 0.045f, 0.08f), new Color(0.92f, 0.86f, 0.74f));
            CreateDecorPart(root, "Foreground Soft Milk Drop L Small", PrimitiveType.Sphere, new Vector3(-2.14f, -2f, -0.62f), new Vector3(0.11f, 0.03f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
            CreateDecorPart(root, "Foreground Soft Milk Drop R", PrimitiveType.Sphere, new Vector3(2.1f, -2.02f, -0.62f), new Vector3(0.26f, 0.05f, 0.09f), new Color(0.92f, 0.86f, 0.74f));
            CreateDecorPart(root, "Foreground Soft Milk Drop R Small", PrimitiveType.Sphere, new Vector3(2.48f, -2f, -0.58f), new Vector3(0.12f, 0.03f, 0.04f), new Color(0.94f, 0.9f, 0.78f));
        }

        private static void CreateReferenceMilkroomComposition(Transform root)
        {
            CreateRug(root);
            CreateWindow(root);
            CreateLeftFurniture(root);
            CreateRightFurniture(root);
            CreateBlendingTable(root);
            CreateShelfGroup(root);
            CreateHangingLights(root);
            CreateMilkroomForeground(root);
            CreateDecorPart(root, "Reference Warm Window Wash", PrimitiveType.Sphere, new Vector3(0f, 1.64f, 2.22f), new Vector3(3.4f, 1.6f, 0.06f), new Color(1f, 0.78f, 0.42f));
            CreateDecorPart(root, "Reference Floor Sun Patch", PrimitiveType.Cube, new Vector3(0.24f, -2.08f, 0.12f), new Vector3(2.55f, 0.022f, 0.2f), Quaternion.Euler(0f, 25f, 0f), new Color(1f, 0.75f, 0.34f));
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
            CreateWorldLabel(root, "Chalkboard Text", "\uC6B0\uC720\uB294\n\uB9C8\uBC95", new Vector3(-2.88f, 1.5f, 0.95f), 0.075f, new Color(1f, 0.9f, 0.62f));
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

            CreateDecorPart(bottleRoot, "Bottle Body", PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(size * 0.24f, size * 0.48f, size * 0.09f), new Color(0.84f, 0.94f, 0.98f));
            CreateDecorPart(bottleRoot, "Bottle Milk Fill", PrimitiveType.Capsule, new Vector3(0f, -size * 0.08f, -size * 0.012f), new Vector3(size * 0.2f, size * 0.34f, size * 0.07f), new Color(0.98f, 0.95f, 0.78f));
            CreateDecorPart(bottleRoot, "Bottle Neck", PrimitiveType.Cylinder, new Vector3(0f, size * 0.31f, -0.005f), new Vector3(size * 0.08f, size * 0.11f, size * 0.08f), new Color(0.86f, 0.95f, 1f));
            CreateDecorPart(bottleRoot, "Bottle Cap", PrimitiveType.Cube, new Vector3(0f, size * 0.43f, -0.02f), new Vector3(size * 0.18f, size * 0.07f, size * 0.055f), new Color(0.47f, 0.72f, 0.9f));
            CreateDecorPart(bottleRoot, "Bottle Label", PrimitiveType.Cube, new Vector3(0f, -size * 0.03f, -0.062f), new Vector3(size * 0.2f, size * 0.13f, size * 0.025f), new Color(1f, 0.86f, 0.56f));
            CreateDecorPart(bottleRoot, "Bottle Shine", PrimitiveType.Cube, new Vector3(-size * 0.08f, size * 0.08f, -size * 0.085f), new Vector3(size * 0.025f, size * 0.2f, size * 0.012f), new Color(1f, 1f, 0.94f));
            CreateWorldLabel(bottleRoot, "Bottle Milk Text", "\uC6B0\uC720", new Vector3(0f, -size * 0.03f, -size * 0.09f), size * 0.08f, new Color(0.28f, 0.56f, 0.76f));
            CreateDecorPart(bottleRoot, "Bottle Face Eye L", PrimitiveType.Sphere, new Vector3(-size * 0.05f, -size * 0.13f, -size * 0.09f), new Vector3(size * 0.018f, size * 0.018f, size * 0.008f), new Color(0.24f, 0.16f, 0.1f));
            CreateDecorPart(bottleRoot, "Bottle Face Eye R", PrimitiveType.Sphere, new Vector3(size * 0.05f, -size * 0.13f, -size * 0.09f), new Vector3(size * 0.018f, size * 0.018f, size * 0.008f), new Color(0.24f, 0.16f, 0.1f));
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
            return CreateDecorPart(parent, name, primitive, localPosition, localScale, Quaternion.identity, color);
        }

        private static Transform CreateDecorPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;

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
                PaintDecorRenderer(renderer, AdjustMilkroomDecorColor(name, color));
            }

            return part.transform;
        }

        private static Color AdjustMilkroomDecorColor(string objectName, Color color)
        {
            var adjusted = color;
            var isGlow = objectName.Contains("Glow") || objectName.Contains("Sun") || objectName.Contains("Bulb");
            var isGlass = objectName.Contains("Glass") || objectName.Contains("Bottle") || objectName.Contains("Window Sky");
            var isWood = objectName.Contains("Wood") || objectName.Contains("Floor") || objectName.Contains("Shelf") || objectName.Contains("Dresser") || objectName.Contains("Chair") || objectName.Contains("Table") || objectName.Contains("Frame");

            if (isGlow)
            {
                adjusted = Color.Lerp(adjusted, new Color(0.86f, 0.56f, 0.24f, color.a), 0.28f) * 0.68f;
            }
            else if (isGlass)
            {
                adjusted = Color.Lerp(adjusted, new Color(0.58f, 0.74f, 0.82f, color.a), 0.18f) * 0.82f;
            }
            else if (isWood)
            {
                adjusted = Color.Lerp(adjusted, new Color(0.45f, 0.25f, 0.12f, color.a), 0.18f) * 0.86f;
            }
            else if (Mathf.Max(color.r, color.g, color.b) > 0.9f)
            {
                adjusted = Color.Lerp(adjusted, new Color(0.88f, 0.78f, 0.58f, color.a), 0.22f) * 0.82f;
            }
            else
            {
                adjusted *= 0.88f;
            }

            adjusted.a = color.a;
            return adjusted;
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
                existing.transform.position = CheeseTamaRestingWorldPosition;
                existing.transform.localScale = Vector3.one;
                var existingController = GetOrCreateVisualController(existing);
                AlignCheeseTamaRestingPosition(existingController);
                return existingController;
            }

            var egg = new GameObject("CheeseTamaRoot");
            egg.transform.position = CheeseTamaRestingWorldPosition;
            egg.transform.localScale = Vector3.one;

            var controller = GetOrCreateVisualController(egg);
            AlignCheeseTamaRestingPosition(controller);
            return controller;
        }

        private const float RugPlacedHeight = 0.1f;
        private static readonly Vector3 CheeseTamaRestingWorldPosition = new Vector3(0f, -1.1f, 0.08f);

        private static void AlignCheeseTamaRestingPosition(CheeseTamaVisualController controller)
        {
            controller?.SetRestingWorldPosition(CheeseTamaRestingWorldPosition);
        }

        private static CheeseTamaVisualController GetOrCreateVisualController(GameObject target)
        {
            var controller = target.GetComponent<CheeseTamaVisualController>();
            if (controller == null)
            {
                controller = target.AddComponent<CheeseTamaVisualController>();
            }

            EnsureGeneratedCharacterModel(target, controller);
            return controller;
        }

        private const string CheeseTamaFallbackModelPrefabPath = "Assets/Characters/CheeseTama/CheeseTama_Model.prefab";
        private const string CheeseTamaEggPrefabPath = "Assets/Characters/CheeseTama/GrowthStages/CheeseTama_Egg.prefab";
        private const string CheeseTamaGrowthVisualSetPath = "Assets/_Project/Resources/CheeseTamaGrowthVisualSet.asset";
        private const float CheeseTamaModelYaw = 270f;
        private const float CheeseTamaModelScale = 1.7f;

        private static void EnsureGeneratedCharacterModel(GameObject root, CheeseTamaVisualController controller)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }

            var modelPrefab = ResolveEditorPreviewModelPrefab();
            if (modelPrefab == null)
            {
                return;
            }

            // Keep exactly one current growth-stage preview and discard legacy or duplicate rigs.
            var toRemove = new System.Collections.Generic.List<GameObject>();
            Transform modelTransform = null;
            foreach (Transform child in root.transform)
            {
                var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                var isCurrentPreview = child.name == "GeneratedModel" && source == modelPrefab;
                if (modelTransform == null && isCurrentPreview)
                {
                    modelTransform = child;
                    continue;
                }

                toRemove.Add(child.gameObject);
            }

            foreach (var g in toRemove)
            {
                Object.DestroyImmediate(g);
            }

            if (modelTransform == null)
            {
                var model = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(modelPrefab);
                model.name = "GeneratedModel";
                model.transform.SetParent(root.transform, false);
                modelTransform = model.transform;
            }

            modelTransform.localPosition = Vector3.zero;
            modelTransform.localRotation = Quaternion.Euler(0f, CheeseTamaModelYaw, 0f);
            modelTransform.localScale = Vector3.one * CheeseTamaModelScale;
            modelTransform.gameObject.SetActive(true);

            var so = new UnityEditor.SerializedObject(controller);
            so.FindProperty("modelPrefab").objectReferenceValue = modelPrefab;
            so.FindProperty("modelInstance").objectReferenceValue = modelTransform;
            so.FindProperty("modelYawDegrees").floatValue = CheeseTamaModelYaw;
            so.FindProperty("modelScale").floatValue = CheeseTamaModelScale;
            so.ApplyModifiedProperties();
#endif
        }

#if UNITY_EDITOR
        private static GameObject ResolveEditorPreviewModelPrefab()
        {
            var visualSet = UnityEditor.AssetDatabase.LoadAssetAtPath<CheeseTamaGrowthVisualSet>(CheeseTamaGrowthVisualSetPath);
            var previewSave = LoadEditorPreviewSave();
            var previewStage = CheeseTamaGrowthStageCatalog.Resolve(previewSave?.cheeseTama);
            var stagePrefab = visualSet != null ? visualSet.GetPrefab(previewStage) : null;
            if (stagePrefab != null)
            {
                return stagePrefab;
            }

            var eggPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CheeseTamaEggPrefabPath);
            return eggPrefab != null
                ? eggPrefab
                : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CheeseTamaFallbackModelPrefabPath);
        }
#endif

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
            ApplyRoundedImage(image);
            return panel;
        }

        private static GameObject GetOrCreateFullScreenOverlay(Transform parent, string name, Color color)
        {
            var existing = parent.Find(name);
            var overlay = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                overlay.transform.SetParent(parent, false);
            }

            var rect = overlay.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = overlay.AddComponent<RectTransform>();
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = overlay.GetComponent<Image>();
            if (image == null)
            {
                image = overlay.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = true;
            var group = overlay.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = overlay.AddComponent<CanvasGroup>();
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            return overlay;
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
                ApplyRoundedImage(image);
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

        private static RectTransform GetOrCreateCollectionScrollContent(Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var scrollView = GetOrCreatePanel(parent, "Collection Scroll View", anchoredPosition, size);
            if (scrollView.TryGetComponent(out Image scrollImage))
            {
                scrollImage.color = new Color(1f, 0.95f, 0.78f, 0.48f);
            }

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = scrollView.AddComponent<ScrollRect>();
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 28f;

            var viewport = GetOrCreatePanel(scrollView.transform, "Viewport", Vector2.zero, size);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.offsetMin = new Vector2(18f, 18f);
            viewportRect.offsetMax = new Vector2(-18f, -18f);

            if (viewport.TryGetComponent(out Image viewportImage))
            {
                viewportImage.color = new Color(1f, 0.98f, 0.9f, 0.35f);
            }

            var mask = viewport.GetComponent<Mask>();
            if (mask == null)
            {
                mask = viewport.AddComponent<Mask>();
            }

            mask.showMaskGraphic = false;

            var contentTransform = viewport.transform.Find("Collection Scroll Content");
            RectTransform contentRect;
            if (contentTransform != null && contentTransform.TryGetComponent(out contentRect))
            {
                ConfigureCollectionScrollContentRect(contentRect, size.y - 36f);
            }
            else
            {
                var contentObject = new GameObject("Collection Scroll Content");
                contentObject.transform.SetParent(viewport.transform, false);
                contentRect = contentObject.AddComponent<RectTransform>();
                ConfigureCollectionScrollContentRect(contentRect, size.y - 36f);
            }

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalNormalizedPosition = 1f;
            return contentRect;
        }

        private static void ConfigureCollectionScrollContentRect(RectTransform rect, float minimumHeight)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, Mathf.Max(360f, minimumHeight));
        }

        private static RectTransform GetOrCreateVerticalScrollContent(
            Transform parent,
            string scrollViewName,
            string contentName,
            Vector2 anchoredPosition,
            Vector2 size,
            float contentHeight,
            Color backgroundColor)
        {
            var scrollView = GetOrCreatePanel(parent, scrollViewName, anchoredPosition, size);
            if (scrollView.TryGetComponent(out Image scrollImage))
            {
                scrollImage.color = backgroundColor;
            }

            RemoveChildIfExists(scrollView.transform, $"{scrollViewName} Viewport");

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = scrollView.AddComponent<ScrollRect>();
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var viewportRect = scrollView.GetComponent<RectTransform>();
            RemoveComponentIfExists<Mask>(scrollView);
            if (scrollView.GetComponent<RectMask2D>() == null)
            {
                scrollView.AddComponent<RectMask2D>();
            }

            var contentTransform = scrollView.transform.Find(contentName);
            RectTransform contentRect;
            if (contentTransform != null && contentTransform.TryGetComponent(out contentRect))
            {
                ConfigureVerticalScrollContentRect(contentRect, contentHeight);
            }
            else
            {
                var contentObject = new GameObject(contentName);
                contentObject.transform.SetParent(scrollView.transform, false);
                contentRect = contentObject.AddComponent<RectTransform>();
                ConfigureVerticalScrollContentRect(contentRect, contentHeight);
            }

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalNormalizedPosition = 1f;
            return contentRect;
        }

        private static void ConfigureVerticalScrollContentRect(RectTransform rect, float contentHeight)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -12f);
            rect.sizeDelta = new Vector2(600f, Mathf.Max(120f, contentHeight));
        }

        private static Text GetOrCreateCollectionRecordText(Transform parent, string name, string text, int fontSize)
        {
            var label = GetOrCreateText(parent, name, text, fontSize, TextAnchor.UpperLeft, Vector2.zero, new Vector2(0f, 520f));
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 520f);
            ApplyCollectionRecordTextStyle(label);
            return label;
        }

        // Anchors a panel to the bottom-center of the screen so it stays pinned to the
        // bottom edge regardless of the game's aspect ratio. This keeps the care buttons,
        // stat values and status message from drifting over the character / milkroom.
        private static GameObject GetOrCreateBottomPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.TryGetComponent(out RectTransform rect))
                {
                    ConfigureBottomPanelRect(rect, anchoredPosition, size);
                }

                if (!existing.TryGetComponent(out Image image))
                {
                    image = existing.gameObject.AddComponent<Image>();
                }

                image.color = new Color(1f, 0.98f, 0.9f, 0.92f);
                ApplyRoundedImage(image);
                return existing.gameObject;
            }

            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var newRect = panel.AddComponent<RectTransform>();
            ConfigureBottomPanelRect(newRect, anchoredPosition, size);

            var newImage = panel.AddComponent<Image>();
            newImage.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            ApplyRoundedImage(newImage);
            return panel;
        }

        private static void ConfigureBottomPanelRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static GameObject GetOrCreateRightPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.TryGetComponent(out RectTransform rect))
                {
                    ConfigureRightPanelRect(rect, anchoredPosition, size);
                }

                if (!existing.TryGetComponent(out Image image))
                {
                    image = existing.gameObject.AddComponent<Image>();
                }

                image.color = new Color(1f, 0.98f, 0.9f, 0.86f);
                ApplyRoundedImage(image);
                return existing.gameObject;
            }

            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var newRect = panel.AddComponent<RectTransform>();
            ConfigureRightPanelRect(newRect, anchoredPosition, size);

            var newImage = panel.AddComponent<Image>();
            newImage.color = new Color(1f, 0.98f, 0.9f, 0.86f);
            ApplyRoundedImage(newImage);
            return panel;
        }

        private static void ConfigureRightPanelRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void RemoveChildIfExists(Transform parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name))
            {
                return;
            }

            var child = parent.Find(name);
            if (child == null)
            {
                return;
            }

            child.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                DestroyObjectSafely(child.gameObject);
            }
            else
            {
                DestroyObjectSafely(child.gameObject);
            }
        }

        private static void RemoveRootObjectIfExists(string name)
        {
            foreach (var rootObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (rootObject.name != name || rootObject.transform.parent != null || !rootObject.scene.IsValid())
                {
                    continue;
                }

                DestroyObjectSafely(rootObject);
                return;
            }
        }

        private static void RemoveComponentIfExists<T>(GameObject target) where T : Component
        {
            if (target == null || !target.TryGetComponent<T>(out var component))
            {
                return;
            }

            DestroyObjectSafely(component);
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
            ApplyRoundedImage(image);

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
                ApplyRoundedImage(image);
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

        private static Slider GetOrCreateSettingsSlider(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float minValue,
            float maxValue,
            bool wholeNumbers)
        {
            var existing = parent.Find(name);
            Slider slider;
            if (existing != null && existing.TryGetComponent(out slider))
            {
                ConfigureTopLeftRect(slider.GetComponent<RectTransform>(), anchoredPosition.x, -anchoredPosition.y, size.x, size.y);
            }
            else
            {
                var sliderObject = new GameObject(name);
                sliderObject.transform.SetParent(parent, false);
                sliderObject.AddComponent<RectTransform>();
                slider = sliderObject.AddComponent<Slider>();
                ConfigureTopLeftRect(slider.GetComponent<RectTransform>(), anchoredPosition.x, -anchoredPosition.y, size.x, size.y);
            }

            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = wholeNumbers;
            slider.direction = Slider.Direction.LeftToRight;

            var backgroundImage = GetOrCreateSettingsImage(slider.transform, "Background", new Color(1f, 0.93f, 0.68f, 0.95f));
            ConfigureStretchRect(backgroundImage.rectTransform, 0f, 0f, 7f);
            ApplyRoundedImage(backgroundImage);

            var fillArea = GetOrCreateRect(slider.transform, "Fill Area");
            ConfigureStretchRect(fillArea, 7f, 7f, 0f);

            var fillImage = GetOrCreateSettingsImage(fillArea, "Fill", new Color(1f, 0.58f, 0.12f, 1f));
            ConfigureStretchRect(fillImage.rectTransform, 0f, 0f, 8f);
            ApplyRoundedImage(fillImage);

            var handleArea = GetOrCreateRect(slider.transform, "Handle Slide Area");
            ConfigureStretchRect(handleArea, 7f, 7f, 0f);

            var handleImage = GetOrCreateSettingsImage(handleArea, "Handle", new Color(1f, 0.72f, 0.18f, 1f));
            ConfigureCenteredRect(handleImage.rectTransform, new Vector2(20f, 24f));
            ApplyRoundedImage(handleImage);

            slider.fillRect = fillImage.rectTransform;
            slider.handleRect = handleImage.rectTransform;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Toggle GetOrCreateSettingsToggle(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var existing = parent.Find(name);
            Toggle toggle;
            if (existing != null && existing.TryGetComponent(out toggle))
            {
                ConfigureTopLeftRect(toggle.GetComponent<RectTransform>(), anchoredPosition.x, -anchoredPosition.y, size.x, size.y);
            }
            else
            {
                var toggleObject = new GameObject(name);
                toggleObject.transform.SetParent(parent, false);
                toggleObject.AddComponent<RectTransform>();
                toggle = toggleObject.AddComponent<Toggle>();
                ConfigureTopLeftRect(toggle.GetComponent<RectTransform>(), anchoredPosition.x, -anchoredPosition.y, size.x, size.y);
            }

            var boxImage = GetOrCreateSettingsImage(toggle.transform, "Box", new Color(1f, 0.94f, 0.76f, 1f));
            ConfigureTopLeftRect(boxImage.rectTransform, 0f, 4f, 22f, 22f);
            ApplyRoundedImage(boxImage);

            var checkImage = GetOrCreateSettingsImage(boxImage.transform, "Checkmark", new Color(1f, 0.52f, 0.08f, 1f));
            ConfigureTopLeftRect(checkImage.rectTransform, 5f, 5f, 12f, 12f);
            ApplyRoundedImage(checkImage);

            var labelText = GetOrCreateText(toggle.transform, "Label", label, 14, TextAnchor.MiddleLeft, new Vector2(32f, 0f), new Vector2(size.x - 32f, size.y));
            labelText.color = new Color(0.24f, 0.16f, 0.08f);
            labelText.raycastTarget = false;

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.transition = Selectable.Transition.ColorTint;
            return toggle;
        }

        private static Image GetOrCreateSettingsImage(Transform parent, string name, Color color)
        {
            var existing = parent.Find(name);
            Image image;
            if (existing != null && existing.TryGetComponent(out image))
            {
                image.color = color;
                return image;
            }

            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            imageObject.AddComponent<RectTransform>();
            image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform GetOrCreateRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
            {
                return existingRect;
            }

            var rectObject = new GameObject(name);
            rectObject.transform.SetParent(parent, false);
            return rectObject.AddComponent<RectTransform>();
        }

        private static void ConfigureStretchRect(RectTransform rect, float left, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, -height * 0.5f);
            rect.offsetMax = new Vector2(-right, height * 0.5f);
        }

        private static void ConfigureCenteredRect(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
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
            return CreateButton(parent, name, label, anchoredPosition, new Vector2(136, 44));
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ConfigureButton(button, label, anchoredPosition, size);
            return button;
        }

        private static Button GetOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            return GetOrCreateButton(parent, name, label, anchoredPosition, new Vector2(136, 44));
        }

        private static Button GetOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                ConfigureButton(existingButton, label, anchoredPosition, size);
                return existingButton;
            }

            return CreateButton(parent, name, label, anchoredPosition, size);
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

        private static void ApplyMilkroomTopHudLayout(
            GameObject topBar,
            GameObject topMenu,
            Text nameText,
            Text levelText,
            Text sessionText,
            Text coinEconomyText,
            Text milkDropEconomyText,
            Text collectionFragmentEconomyText,
            Button topCollectionButton,
            Button topDecorateButton,
            Button settingsButton)
        {
            var topBarRightOffset = MilkroomSideMargin + MilkroomRightPanelWidth + TopHudGap;
            if (topBar != null && topBar.TryGetComponent(out RectTransform topBarRect))
            {
                ConfigureTopStretchRect(topBarRect, MilkroomSideMargin, topBarRightOffset, TopHudTop, TopHudHeight);
            }

            if (topMenu != null && topMenu.TryGetComponent(out RectTransform topMenuRect))
            {
                ConfigureTopRightRect(topMenuRect, MilkroomSideMargin, TopHudTop, MilkroomRightPanelWidth, TopHudHeight);
            }

            ConfigureTopLeftRect(nameText != null ? nameText.GetComponent<RectTransform>() : null, 24f, 17f, 280f, 48f);
            ConfigureTopLeftRect(levelText != null ? levelText.GetComponent<RectTransform>() : null, 330f, 17f, 190f, 48f);
            ConfigureTopStretchRect(sessionText != null ? sessionText.GetComponent<RectTransform>() : null, 548f, 598f, 10f, 64f);
            ConfigureTopRightRect(coinEconomyText != null ? coinEconomyText.GetComponent<RectTransform>() : null, 416f, 17f, 112f, 48f);
            ConfigureTopRightRect(milkDropEconomyText != null ? milkDropEconomyText.GetComponent<RectTransform>() : null, 202f, 17f, 152f, 48f);
            ConfigureTopRightRect(collectionFragmentEconomyText != null ? collectionFragmentEconomyText.GetComponent<RectTransform>() : null, 24f, 17f, 136f, 48f);
            ConfigureTopBarResourceIconLayout(topBar != null ? topBar.transform : null);

            var buttonWidth = (MilkroomRightPanelWidth - (TopMenuPadding * 2f) - (TopMenuButtonGap * 2f)) / 3f;
            ConfigureTopLeftRect(topCollectionButton != null ? topCollectionButton.GetComponent<RectTransform>() : null, TopMenuPadding, TopMenuButtonTop, buttonWidth, TopMenuButtonHeight);
            ConfigureTopLeftRect(topDecorateButton != null ? topDecorateButton.GetComponent<RectTransform>() : null, TopMenuPadding + buttonWidth + TopMenuButtonGap, TopMenuButtonTop, buttonWidth, TopMenuButtonHeight);
            ConfigureTopLeftRect(settingsButton != null ? settingsButton.GetComponent<RectTransform>() : null, TopMenuPadding + ((buttonWidth + TopMenuButtonGap) * 2f), TopMenuButtonTop, buttonWidth, TopMenuButtonHeight);
        }

        private static void ConfigureTopBarResourceIcons(Transform topBarTransform)
        {
            if (topBarTransform == null)
            {
                return;
            }

            ConfigureTopBarResourceIcon(topBarTransform, "Coin Economy Icon", "coin");
            ConfigureTopBarResourceIcon(topBarTransform, "Milk Drop Economy Icon", "milkdrop");
            ConfigureTopBarResourceIcon(topBarTransform, "Collection Fragment Economy Icon", "collectionpuzzle");
            ConfigureTopBarResourceIconLayout(topBarTransform);
        }

        private static void ConfigureTopBarResourceIcon(Transform parent, string name, string resourceName)
        {
            var iconTransform = parent.Find(name);
            if (iconTransform == null)
            {
                var iconObject = new GameObject(name);
                var iconRect = iconObject.AddComponent<RectTransform>();
                iconObject.transform.SetParent(parent, false);
                iconTransform = iconRect;
            }

            var iconImage = iconTransform.GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = iconTransform.gameObject.AddComponent<Image>();
            }

            iconImage.sprite = Resources.Load<Sprite>($"UI/TopBarIcons/{resourceName}");
            iconImage.enabled = iconImage.sprite != null;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        private static void ConfigureTopBarResourceIconLayout(Transform topBarTransform)
        {
            if (topBarTransform == null)
            {
                return;
            }

            ConfigureTopRightRect(topBarTransform.Find("Coin Economy Icon") as RectTransform, 548f, 27f, 28f, 28f);
            ConfigureTopRightRect(topBarTransform.Find("Milk Drop Economy Icon") as RectTransform, 370f, 27f, 28f, 28f);
            ConfigureTopRightRect(topBarTransform.Find("Collection Fragment Economy Icon") as RectTransform, 170f, 27f, 28f, 28f);
        }

        private static void ConfigureTopLeftRect(RectTransform rect, float left, float top, float width, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void ConfigureTopRightRect(RectTransform rect, float right, float top, float width, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void ConfigureTopStretchRect(RectTransform rect, float left, float right, float top, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController)
        {
            ConfigureCareButton(button, action, controller, visualController, (CookingPanelController)null);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController,
            CookingPanelController cookingPanelController)
        {
            button.onClick.RemoveAllListeners();
            var careButton = button.GetComponent<MilkroomCareButton>();
            if (careButton == null)
            {
                careButton = button.gameObject.AddComponent<MilkroomCareButton>();
            }

            careButton.Configure(action, controller, visualController, cookingPanelController);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController,
            MilkPanelController milkPanelController)
        {
            button.onClick.RemoveAllListeners();
            var careButton = button.GetComponent<MilkroomCareButton>();
            if (careButton == null)
            {
                careButton = button.gameObject.AddComponent<MilkroomCareButton>();
            }

            careButton.Configure(action, controller, visualController, milkPanelController);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController,
            SnackPanelController snackPanelController)
        {
            button.onClick.RemoveAllListeners();
            var careButton = button.GetComponent<MilkroomCareButton>();
            if (careButton == null)
            {
                careButton = button.gameObject.AddComponent<MilkroomCareButton>();
            }

            careButton.Configure(action, controller, visualController, snackPanelController);
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

        private static void ConfigureDebugButton(
            Button button,
            DebugAction action,
            MilkroomUIController controller,
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
            ConfigureButton(button, label, anchoredPosition, new Vector2(136, 44));
        }

        private static void ConfigureButton(Button button, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            ConfigureButtonVisuals(button, label, rect.sizeDelta);
        }

        private static void ConfigureButtonVisuals(Button button, string label, Vector2 size)
        {
            if (!button.TryGetComponent(out Image image))
            {
                image = button.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.96f, 0.78f, 0.35f);
            ApplyRoundedImage(image);
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

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null || !labelTransform.TryGetComponent(out Text labelText))
            {
                return;
            }

            labelText.text = label;
        }

        private static void SetButtonIcon(Button button, string iconId)
        {
            if (button == null)
            {
                return;
            }

            HideLegacyButtonIcons(button.transform);

            var iconTransform = button.transform.Find("Pictogram Icon");
            if (iconTransform == null)
            {
                var iconObject = new GameObject("Pictogram Icon");
                var iconRect = iconObject.AddComponent<RectTransform>();
                iconObject.transform.SetParent(button.transform, false);
                iconTransform = iconRect;
            }

            iconTransform.gameObject.SetActive(true);
            ClearButtonIconParts(iconTransform);

            var iconImage = iconTransform.GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = iconTransform.gameObject.AddComponent<Image>();
            }

            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.type = Image.Type.Simple;
            iconImage.color = Color.white;
            iconImage.sprite = LoadButtonIconSprite(iconId);

            if (iconImage.sprite == null)
            {
                iconImage.enabled = false;
                CreateButtonPictogram(iconTransform, iconId);
            }
            else
            {
                iconImage.enabled = true;
            }

            ApplyButtonIconLayout(button, iconTransform as RectTransform);
        }

        private static Sprite LoadButtonIconSprite(string iconId)
        {
            var resourceName = iconId switch
            {
                "collection" => "collection",
                "settings" => "settings",
                "milk" => "milk",
                "decorate" => "themes",
                "cook" => "cooking",
                "snack" => "snackbag",
                "play" => "playing",
                "clean" => "cleaning",
                "rest" => "resting",
                _ => string.Empty
            };

            return string.IsNullOrEmpty(resourceName)
                ? null
                : Resources.Load<Sprite>($"UI/ButtonIcons/{resourceName}");
        }

        private static void ApplyButtonIconLayout(Button button, RectTransform iconRect)
        {
            if (button == null || iconRect == null)
            {
                return;
            }

            var buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect == null)
            {
                return;
            }

            var isCompactTopMenuButton = buttonRect.sizeDelta.x <= 122f;
            var iconSize = isCompactTopMenuButton ? 38f : 42f;
            var leftPadding = isCompactTopMenuButton ? 7f : 8f;
            var labelGap = isCompactTopMenuButton ? 3f : 4f;
            var rightPadding = isCompactTopMenuButton ? 6f : 8f;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(leftPadding, 0f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null || !labelTransform.TryGetComponent(out Text label))
            {
                return;
            }

            var labelRect = label.GetComponent<RectTransform>();
            if (labelRect == null)
            {
                return;
            }

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.offsetMin = new Vector2(leftPadding + iconSize + labelGap, 4f);
            labelRect.offsetMax = new Vector2(-rightPadding, -4f);
            label.alignment = TextAnchor.MiddleCenter;
        }

        private static void HideLegacyButtonIcons(Transform buttonTransform)
        {
            if (buttonTransform == null)
            {
                return;
            }

            try
            {
                var legacyIcon = buttonTransform.Find("Icon");
                if (legacyIcon != null)
                {
                    legacyIcon.gameObject.SetActive(false);
                }
            }
            catch (MissingReferenceException)
            {
                // Legacy generated icon was already destroyed by a previous editor rebuild.
            }
        }

        private static void ClearButtonIconParts(Transform iconTransform)
        {
            for (var i = iconTransform.childCount - 1; i >= 0; i -= 1)
            {
                var child = iconTransform.GetChild(i);
                if (child != null && child.name.StartsWith("Icon Part"))
                {
                    DestroyObjectSafely(child.gameObject);
                }
            }
        }

        private static void CreateButtonPictogram(Transform iconTransform, string iconId)
        {
            var color = new Color(0.36f, 0.20f, 0.07f);
            switch (iconId)
            {
                case "collection":
                    CreateIconPart(iconTransform, "Icon Part Book Left Page", new Vector2(-5.5f, -1f), new Vector2(10f, 20f), color);
                    CreateIconPart(iconTransform, "Icon Part Book Right Page", new Vector2(5.5f, -1f), new Vector2(10f, 20f), color);
                    CreateIconPart(iconTransform, "Icon Part Book Spine", new Vector2(0f, -1f), new Vector2(2f, 21f), new Color(1f, 0.76f, 0.27f));
                    CreateIconPart(iconTransform, "Icon Part Book Top Fold", new Vector2(0f, 9f), new Vector2(19f, 2f), color);
                    CreateIconPart(iconTransform, "Icon Part Book Left Line 1", new Vector2(-5f, 3.5f), new Vector2(5f, 1.8f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Book Left Line 2", new Vector2(-5f, -2.5f), new Vector2(5f, 1.8f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Book Right Line 1", new Vector2(5f, 3.5f), new Vector2(5f, 1.8f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Book Right Line 2", new Vector2(5f, -2.5f), new Vector2(5f, 1.8f), new Color(1f, 0.86f, 0.48f));
                    break;
                case "decorate":
                    CreateIconPart(iconTransform, "Icon Part Brush Handle", new Vector2(-1f, -2f), new Vector2(4f, 22f), color, -32f);
                    CreateIconPart(iconTransform, "Icon Part Brush Tip", new Vector2(7f, 7f), new Vector2(9f, 6f), new Color(1f, 0.86f, 0.48f), -32f);
                    CreateIconPart(iconTransform, "Icon Part Spark 1", new Vector2(-8f, 8f), new Vector2(3f, 9f), color);
                    CreateIconPart(iconTransform, "Icon Part Spark 2", new Vector2(-8f, 8f), new Vector2(9f, 3f), color);
                    break;
                case "settings":
                    CreateIconPart(iconTransform, "Icon Part Gear Core", new Vector2(0f, 0f), new Vector2(13f, 13f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth N", new Vector2(0f, 11f), new Vector2(5f, 6f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth S", new Vector2(0f, -11f), new Vector2(5f, 6f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth W", new Vector2(-11f, 0f), new Vector2(6f, 5f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth E", new Vector2(11f, 0f), new Vector2(6f, 5f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth NW", new Vector2(-7.5f, 7.5f), new Vector2(5f, 5f), color, 45f);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth NE", new Vector2(7.5f, 7.5f), new Vector2(5f, 5f), color, -45f);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth SW", new Vector2(-7.5f, -7.5f), new Vector2(5f, 5f), color, -45f);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth SE", new Vector2(7.5f, -7.5f), new Vector2(5f, 5f), color, 45f);
                    CreateIconPart(iconTransform, "Icon Part Gear Hole", new Vector2(0f, 0f), new Vector2(5.5f, 5.5f), new Color(1f, 0.75f, 0.24f));
                    break;
                case "milk":
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Body", new Vector2(0f, -3f), new Vector2(16f, 20f), color);
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Roof Left", new Vector2(-4f, 9f), new Vector2(9f, 8f), color, -28f);
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Roof Right", new Vector2(4f, 9f), new Vector2(9f, 8f), color, 28f);
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Fold", new Vector2(0f, 7f), new Vector2(2f, 9f), new Color(1f, 0.76f, 0.27f));
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Label", new Vector2(0f, -4f), new Vector2(10f, 6f), new Color(1f, 0.89f, 0.56f));
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Stripe", new Vector2(0f, 0f), new Vector2(10f, 2f), new Color(1f, 0.76f, 0.27f));
                    break;
                case "cook":
                    CreateIconPart(iconTransform, "Icon Part Pot Body", new Vector2(0f, -3f), new Vector2(19f, 12f), color);
                    CreateIconPart(iconTransform, "Icon Part Pot Lid", new Vector2(0f, 5f), new Vector2(14f, 3f), color);
                    CreateIconPart(iconTransform, "Icon Part Pot Handle L", new Vector2(-12f, -3f), new Vector2(4f, 7f), color);
                    CreateIconPart(iconTransform, "Icon Part Pot Handle R", new Vector2(12f, -3f), new Vector2(4f, 7f), color);
                    CreateIconPart(iconTransform, "Icon Part Steam 1", new Vector2(-5f, 12f), new Vector2(3f, 8f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Steam 2", new Vector2(4f, 13f), new Vector2(3f, 7f), new Color(1f, 0.86f, 0.48f));
                    break;
                case "snack":
                    CreateIconPart(iconTransform, "Icon Part Bag Body", new Vector2(0f, -2f), new Vector2(18f, 19f), color);
                    CreateIconPart(iconTransform, "Icon Part Bag Fold", new Vector2(0f, 8f), new Vector2(14f, 4f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Snack Dot 1", new Vector2(-4f, -2f), new Vector2(3f, 3f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Snack Dot 2", new Vector2(4f, -2f), new Vector2(3f, 3f), new Color(1f, 0.86f, 0.48f));
                    break;
                case "play":
                    CreateIconPart(iconTransform, "Icon Part Basketball Border", Vector2.zero, new Vector2(22f, 22f), color, 0f, true);
                    CreateIconPart(iconTransform, "Icon Part Basketball Fill", Vector2.zero, new Vector2(17f, 17f), new Color(1f, 0.82f, 0.38f), 0f, true);
                    CreateIconPart(iconTransform, "Icon Part Basketball Horizontal", Vector2.zero, new Vector2(17f, 2f), color);
                    CreateIconPart(iconTransform, "Icon Part Basketball Vertical", Vector2.zero, new Vector2(2f, 17f), color);
                    CreateIconPart(iconTransform, "Icon Part Basketball Left Seam", new Vector2(-5f, 0f), new Vector2(2f, 16f), color, -18f);
                    CreateIconPart(iconTransform, "Icon Part Basketball Right Seam", new Vector2(5f, 0f), new Vector2(2f, 16f), color, 18f);
                    break;
                case "clean":
                    CreateIconPart(iconTransform, "Icon Part Spray Body", new Vector2(-2f, -4f), new Vector2(12f, 16f), color);
                    CreateIconPart(iconTransform, "Icon Part Spray Neck", new Vector2(1f, 6f), new Vector2(6f, 6f), color);
                    CreateIconPart(iconTransform, "Icon Part Spray Nozzle", new Vector2(7f, 10f), new Vector2(10f, 4f), color);
                    CreateIconPart(iconTransform, "Icon Part Spray Dot 1", new Vector2(12f, 3f), new Vector2(3f, 3f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Spray Dot 2", new Vector2(15f, -3f), new Vector2(3f, 3f), new Color(1f, 0.86f, 0.48f));
                    break;
                case "rest":
                    CreateIconPart(iconTransform, "Icon Part Bed Base", new Vector2(0f, -5f), new Vector2(22f, 7f), color);
                    CreateIconPart(iconTransform, "Icon Part Bed Back", new Vector2(-10f, 1f), new Vector2(4f, 14f), color);
                    CreateIconPart(iconTransform, "Icon Part Pillow", new Vector2(-3f, 2f), new Vector2(8f, 5f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Moon", new Vector2(8f, 7f), new Vector2(8f, 8f), color);
                    CreateIconPart(iconTransform, "Icon Part Moon Cut", new Vector2(11f, 8f), new Vector2(6f, 7f), new Color(1f, 0.75f, 0.24f));
                    break;
                default:
                    CreateIconPart(iconTransform, "Icon Part Default", Vector2.zero, new Vector2(16f, 16f), color);
                    break;
            }
        }

        private static void CreateIconPart(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color,
            float rotation = 0f,
            bool circle = false)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            var rect = part.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);

            var image = part.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (circle)
            {
                ApplyCircleImage(image);
            }
            else
            {
                ApplyRoundedImage(image);
            }
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

        private static void ApplyRecordSectionStyle(GameObject section, Color color)
        {
            if (section == null)
            {
                return;
            }

            section.transform.SetAsFirstSibling();
            if (section.TryGetComponent(out Image image))
            {
                image.color = color;
                image.raycastTarget = false;
                ApplyRoundedImage(image);
            }
        }

        private static void ApplyRecordLineStyle(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.supportRichText = true;
            label.fontStyle = FontStyle.Normal;
            label.color = new Color(0.25f, 0.17f, 0.09f);
            label.lineSpacing = 1.12f;
        }

        private static void ApplyCollectionRecordTextStyle(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.supportRichText = true;
            label.fontStyle = FontStyle.Normal;
            label.color = new Color(0.25f, 0.17f, 0.09f);
            label.lineSpacing = 1.18f;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void ApplyTopInfoTextStyle(Text label, int maxFontSize)
        {
            if (label == null)
            {
                return;
            }

            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.23f, 0.14f, 0.07f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(14, maxFontSize - 6);
            label.resizeTextMaxSize = maxFontSize;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ApplyTopSessionTextStyle(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.fontStyle = FontStyle.Normal;
            label.color = new Color(0.23f, 0.14f, 0.07f);
            label.fontSize = 15;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 15;
            label.lineSpacing = 1.36f;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ApplyTopMenuButtonStyle(Button button)
        {
            ApplyReadableButtonStyle(
                button,
                new Color(1f, 0.75f, 0.24f),
                new Color(1f, 0.86f, 0.39f),
                new Color(0.88f, 0.53f, 0.13f),
                new Color(1f, 0.86f, 0.39f),
                new Color(0.26f, 0.16f, 0.08f),
                20,
                15);
        }

        private static void ApplyCareButtonStyle(Button button)
        {
            ApplyReadableButtonStyle(
                button,
                new Color(1f, 0.75f, 0.24f),
                new Color(1f, 0.86f, 0.39f),
                new Color(0.88f, 0.53f, 0.13f),
                new Color(1f, 0.86f, 0.39f),
                new Color(0.26f, 0.16f, 0.08f),
                21,
                15);
        }

        private static void ApplyCollectionTabButtonStyle(params Button[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (var button in buttons)
            {
                ApplyReadableButtonStyle(
                    button,
                    new Color(1f, 0.9f, 0.62f, 0.88f),
                    new Color(1f, 0.84f, 0.36f),
                    new Color(0.88f, 0.53f, 0.13f),
                    new Color(1f, 0.74f, 0.24f),
                    new Color(0.26f, 0.16f, 0.08f),
                    17,
                    12);
            }
        }

        private static void ApplyCookingRecipeButtonStyle(params Button[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (var button in buttons)
            {
                ApplyReadableButtonStyle(
                    button,
                    new Color(1f, 0.87f, 0.54f, 0.96f),
                    new Color(1f, 0.92f, 0.66f, 1f),
                    new Color(0.95f, 0.68f, 0.28f, 1f),
                    new Color(1f, 0.78f, 0.30f, 1f),
                    new Color(0.18f, 0.10f, 0.04f),
                    16,
                    11);
            }
        }

        private static void ApplyReadableButtonStyle(
            Button button,
            Color normal,
            Color highlighted,
            Color pressed,
            Color selected,
            Color labelColor,
            int labelMaxFontSize,
            int labelMinFontSize)
        {
            if (button == null)
            {
                return;
            }

            if (!button.TryGetComponent(out Image image))
            {
                image = button.gameObject.AddComponent<Image>();
            }

            image.color = normal;
            ApplyRoundedImage(image);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = selected;
            colors.disabledColor = new Color(0.72f, 0.66f, 0.56f, 0.72f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null || !labelTransform.TryGetComponent(out Text label))
            {
                return;
            }

            var labelRect = label.GetComponent<RectTransform>();
            var buttonRect = button.GetComponent<RectTransform>();
            if (labelRect != null && buttonRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.offsetMin = new Vector2(10f, 4f);
                labelRect.offsetMax = new Vector2(-10f, -4f);
            }

            label.fontStyle = FontStyle.Bold;
            label.color = labelColor;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = labelMinFontSize;
            label.resizeTextMaxSize = labelMaxFontSize;
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
                ApplyRoundedImage(image);
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

        private static void ReapplyRoundedImages(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var images = root.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (ShouldUseRoundedRuntimeStyle(image))
                {
                    ApplyRoundedImage(image);
                }
            }
        }

        private static bool ShouldUseRoundedRuntimeStyle(Image image)
        {
            if (image == null)
            {
                return false;
            }

            var target = image.gameObject;
            if (target.GetComponent<Button>() != null)
            {
                return true;
            }

            var name = target.name;
            return name.Contains("Panel")
                || name.Contains("Bar")
                || name.Contains("Modal")
                || name.Contains("Dialog")
                || name.Contains("Menu")
                || name.Contains("View")
                || name.Contains("Row")
                || name.Contains("Section");
        }

        internal static void ApplyRoundedImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetRoundedUiSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
        }

        internal static void ApplyCircleImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetCircleUiSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.pixelsPerUnitMultiplier = 1f;
        }

        private static Sprite GetRoundedUiSprite()
        {
            if (roundedUiSprite != null)
            {
                return roundedUiSprite;
            }

            roundedUiTexture = new Texture2D(RoundedUiSpriteSize, RoundedUiSpriteSize, TextureFormat.RGBA32, false)
            {
                name = "CheeseTama Rounded UI Sprite",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < RoundedUiSpriteSize; y++)
            {
                for (var x = 0; x < RoundedUiSpriteSize; x++)
                {
                    var alpha = GetRoundedRectAlpha(x, y);
                    roundedUiTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            roundedUiTexture.Apply(false, false);

            var border = new Vector4(RoundedUiCornerRadius, RoundedUiCornerRadius, RoundedUiCornerRadius, RoundedUiCornerRadius);
            roundedUiSprite = Sprite.Create(
                roundedUiTexture,
                new Rect(0, 0, RoundedUiSpriteSize, RoundedUiSpriteSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            roundedUiSprite.name = "CheeseTama Rounded UI Sprite";
            roundedUiSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedUiSprite;
        }

        private static Sprite GetCircleUiSprite()
        {
            if (circleUiSprite != null)
            {
                return circleUiSprite;
            }

            circleUiTexture = new Texture2D(RoundedUiSpriteSize, RoundedUiSpriteSize, TextureFormat.RGBA32, false)
            {
                name = "CheeseTama Circle UI Sprite",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((RoundedUiSpriteSize - 1) * 0.5f, (RoundedUiSpriteSize - 1) * 0.5f);
            var radius = RoundedUiSpriteSize * 0.5f - 1f;
            for (var y = 0; y < RoundedUiSpriteSize; y++)
            {
                for (var x = 0; x < RoundedUiSpriteSize; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    circleUiTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            circleUiTexture.Apply(false, false);
            circleUiSprite = Sprite.Create(
                circleUiTexture,
                new Rect(0, 0, RoundedUiSpriteSize, RoundedUiSpriteSize),
                new Vector2(0.5f, 0.5f),
                100f);
            circleUiSprite.name = "CheeseTama Circle UI Sprite";
            circleUiSprite.hideFlags = HideFlags.HideAndDontSave;
            return circleUiSprite;
        }

        private static float GetRoundedRectAlpha(int x, int y)
        {
            var radius = RoundedUiCornerRadius;
            var size = RoundedUiSpriteSize;
            var centerX = Mathf.Clamp(x, radius, size - radius - 1);
            var centerY = Mathf.Clamp(y, radius, size - radius - 1);
            var distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
            return Mathf.Clamp01(radius + 0.5f - distance);
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
