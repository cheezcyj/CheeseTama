using System.Collections.Generic;
using CheeseTama.Audio;
using CheeseTama.Data;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Records;
using CheeseTama.Gameplay.Reset;
using CheeseTama.Gameplay.Story;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Platform;
using CheeseTama.Platform.Accounts;
using CheeseTama.Save;
using CheeseTama.UI;
using CheeseTama.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheeseTama.Core
{
    public static partial class StarterSceneBuilder
    {
        private const int RoundedUiSpriteSize = 32;
        private const int RoundedUiCornerRadius = 8;
        private const int RingUiSpriteSize = 64;
        private const float RingUiThickness = 4f;
        private const float MilkroomSideMargin = 24f;
        private const float MilkroomRightPanelWidth = 360f;
        private const float TopHudTop = 16f;
        private const float TopHudHeight = 82f;
        private const float TopHudGap = 18f;
        private const float TopMenuPadding = 7f;
        private const float TopMenuButtonGap = 6f;
        private const float TopMenuDecorateButtonWidth = 128f;
        private const int TopMenuLabelMinFontSize = 15;
        private const int TopMenuLabelMaxFontSize = 20;
        private const int TopMenuMediumTextSharedFontSize = 23;
        private const int TopMenuLargeTextSharedFontSize = 24;
        private const float TopMenuButtonTop = 14f;
        private const float TopMenuButtonHeight = 54f;
        private const float TopProfileLeft = 18f;
        private const float TopProfileSize = 56f;
        private const float TopIdentitySectionGap = 14f;
        private const float TopSessionSectionGap = 32f;
        private const float TopIdentityLeft = 92f;
        private const float TopIdentityPreferredWidth = 212f;
        private const float TopIdentityMinimumNameWidth = 190f;
        private const float TopIdentityNameHeight = 32f;
        private const float TopIdentityNameTop = (TopHudHeight - TopIdentityNameHeight) * 0.5f;
        private const float TopIdentityProgressLeft = TopIdentityLeft + TopIdentityPreferredWidth + TopIdentitySectionGap;
        private const float TopIdentityProgressPreferredWidth = 212f;
        private const float TopIdentityProgressMinimumWidth = 140f;
        private const float TopIdentityLevelHeight = 32f;
        private const float TopIdentityGaugeGap = 6f;
        private const float TopIdentityGaugeHeight = 8f;
        private const float TopIdentityGrowthHeight = TopIdentityLevelHeight + TopIdentityGaugeGap + TopIdentityGaugeHeight;
        private const float TopIdentityLevelTop = (TopHudHeight - TopIdentityGrowthHeight) * 0.5f;
        private const float TopIdentityGaugeTop = TopIdentityLevelTop + TopIdentityLevelHeight + TopIdentityGaugeGap;
        private const float TopSessionLeft = TopIdentityProgressLeft + TopIdentityProgressPreferredWidth + TopSessionSectionGap;
        private const float TopSessionMinimumWidth = 170f;
        private const float TopResourceRightPadding = 24f;
        private const float TopResourceGroupGap = 16f;
        private const float TopResourceIconLabelGap = 8f;
        private const float TopResourceIconSize = 28f;
        private const float TopCoinResourceTextWidth = 100f;
        private const float TopMilkDropResourceTextWidth = 132f;
        private const float TopCollectionFragmentResourceTextWidth = 120f;
        private const float TopSessionHeight = 64f;
        private const float TopSessionTop = (TopHudHeight - TopSessionHeight) * 0.5f;
        private const string TopLocalAccountButtonName = "Top Local Account Button";
        private const string BootLocalAccountButtonName = "Boot Account Entry Button";
        private const string ContinueAsGuestButtonName = "Continue As Guest Button";
        private const string RecordDetailVerticalGap = "\n";

        private readonly struct TopResourceLayout
        {
            public TopResourceLayout(
                float coinTextRight,
                float coinTextWidth,
                float coinIconRight,
                float milkDropTextRight,
                float milkDropTextWidth,
                float milkDropIconRight,
                float collectionFragmentTextRight,
                float collectionFragmentTextWidth,
                float collectionFragmentIconRight,
                float sessionRight)
            {
                CoinTextRight = coinTextRight;
                CoinTextWidth = coinTextWidth;
                CoinIconRight = coinIconRight;
                MilkDropTextRight = milkDropTextRight;
                MilkDropTextWidth = milkDropTextWidth;
                MilkDropIconRight = milkDropIconRight;
                CollectionFragmentTextRight = collectionFragmentTextRight;
                CollectionFragmentTextWidth = collectionFragmentTextWidth;
                CollectionFragmentIconRight = collectionFragmentIconRight;
                SessionRight = sessionRight;
            }

            public float CoinTextRight { get; }
            public float CoinTextWidth { get; }
            public float CoinIconRight { get; }
            public float MilkDropTextRight { get; }
            public float MilkDropTextWidth { get; }
            public float MilkDropIconRight { get; }
            public float CollectionFragmentTextRight { get; }
            public float CollectionFragmentTextWidth { get; }
            public float CollectionFragmentIconRight { get; }
            public float SessionRight { get; }
        }

        private static readonly string[] MilkroomPropBlockingSurfaceNames =
        {
            NewGameSetupController.OverlayObjectName,
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Evolution Achievement Overlay",
            "Care Event Overlay",
            CleaningMiniGameController.OverlayObjectName,
            "Milk Drop Catch Overlay",
            BouncyJumpMiniGameController.OverlayObjectName,
            BlueBallMiniGameController.OverlayObjectName,
            PlayChoicePanelController.OverlayObjectName,
            GrowthJourneyController.OverlayObjectName,
            LifeChapterPanelController.OverlayObjectName,
            "Decoration Shop Overlay",
            DecorationPlacementOverlayController.OverlayObjectName,
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Confirm Reset Dialog",
            "Collection Overlay",
            "Decorate Overlay",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel",
            FirstDayJourneyController.OverlayObjectName,
            "Cheese Star Delivery Overlay",
            "Memory Journal Overlay",
            "Fantasy Powder Overlay",
            SaveRecoveryNoticeController.OverlayObjectName,
            CheeseTamaProfileMenuController.OverlayObjectName,
            AuthPanelController.OverlayObjectName,
            InputBindingsPanelController.OverlayObjectName,
            LifeRecordsPanelController.OverlayObjectName,
            MiniGameMasteryPanelController.OverlayObjectName,
            "Postgame Research Overlay",
            MilkroomMysteryPanelController.OverlayObjectName,
            DreamStorySeasonPanelController.OverlayObjectName,
            "Milkroom Investigation Mode",
            "Npc Afterstory Overlay",
            CloudSavePanelController.OverlayObjectName,
            "Milk Blending Overlay",
            NpcVisitCardController.OverlayObjectName,
            JourneyHubPanelController.OverlayObjectName,
            SleepSchedulePanelController.OverlayObjectName,
            StarLegacyPanelController.OverlayObjectName,
            StarLineagePanelController.OverlayObjectName,
            "Bond Status Overlay",
            "Hidden Career Card Overlay"
        };
        private static readonly Vector2 MilkroomToolPanelPosition = new Vector2(420f, -244f);
        private static readonly Vector2 MilkroomToolPanelSize = new Vector2(680f, 590f);

        private static Texture2D roundedUiTexture;
        private static Sprite roundedUiSprite;
        private static Texture2D circleUiTexture;
        private static Sprite circleUiSprite;
        private static Texture2D ringUiTexture;
        private static Sprite ringUiSprite;

        private static bool IsRuntimeSessionActive()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
            return Application.isPlaying;
#endif
        }

        public static GameManager EnsureCoreSystems()
        {
            LocalAccountRuntime.EnsureInitialized();
            if (GameManager.Instance != null)
            {
                if (IsRuntimeSessionActive() && GameManager.Instance.CurrentSave == null)
                {
                    GameManager.Instance.LoadOrCreateGame();
                }

                EnsureAudioController(GameManager.Instance);
                return GameManager.Instance;
            }

            var existing = Object.FindFirstObjectByType<GameManager>();
            if (existing != null)
            {
                if (IsRuntimeSessionActive() && existing.CurrentSave == null)
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
            if (IsRuntimeSessionActive() && manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

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

            var manager = EnsureCoreSystems();
            GameSettingsPanelController.ApplyUiScaleToLoadedCanvases(
                manager?.CurrentSave?.settings?.uiScale ?? 1f);
            Object.FindFirstObjectByType<MobileWebUiCoordinator>()?.RefreshLoadedCanvases();
        }

        public static bool TryBindExistingSceneForRuntime(string sceneName)
        {
            if (!IsRuntimeSessionActive())
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

            KeepOverlayCanvasAtSceneRoot(canvas.transform);
            RemoveLegacyMilkroomPanelObjects(canvas.transform);
            ReapplyRoundedImages(canvas.transform);
            EnsureExistingStatusExplanationScroll(canvas.transform);
            ConfigureCookingPanelHeaderAlignment(canvas.transform.Find("Cooking Panel"));
            ConfigureCareTipInteraction(canvas.transform, controller);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            RemoveChildIfExists(canvas.transform, "Dev Panel");
            RemoveChildIfExists(canvas.transform, "Dev Mode Toggle Button");
#endif

            var manager = EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            EnsureMilkroomBackground();
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
                BuildDecorateOverlay(canvas.transform, out _);
            }
            else
            {
                return false;
            }

            Object.FindFirstObjectByType<MilkPanelController>()?.Close();
            Object.FindFirstObjectByType<CookingPanelController>()?.Close();
            BuildSnackPanel(canvas.transform, controller, visualController)?.Close();
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
            EnsureBlueBallMiniGame(canvas.transform, controller, visualController);
            EnsurePlayChoicePanel(canvas.transform);
            EnsureCleaningMiniGame(canvas.transform, controller, visualController);
            EnsureCareEventCard(canvas.transform, visualController);
            EnsureNpcVisitCard(canvas.transform);
            EnsureDirectRestAction(canvas.transform, controller, visualController);
            EnsureDecorationShop(canvas.transform);
            EnsureDecorationRoomPresenter();
            EnsureDecorationPlacement(canvas.transform);
            EnsureMilkroomViewportNavigation(canvas.transform);
            EnsureMilkroomAtmosphere(canvas.transform, manager.CurrentTama);
            EnsureMilkroomSeasonalLayer(canvas.transform, manager);
            var petInteraction = EnsureCheeseTamaPetInteraction(
                canvas.transform,
                controller,
                visualController);
            EnsureCheeseTamaSpeechBubble(canvas.transform, visualController);
            EnsureAutonomousLife(canvas.transform, visualController);
            RemoveNormalEvolutionVisualAccents(visualController);
            EnsureLateGameFeatures(canvas.transform, controller, visualController);
            EnsureCheeseStarDelivery(canvas.transform);
            EnsureMemoryJournal(canvas.transform);
            EnsureFantasyPowderHiddenRecipes(canvas.transform);
            EnsureFirstDayJourney(canvas.transform);
            EnsureJourneyHub(canvas.transform);
            EnsureCheeseTamaProfileMenu(canvas.transform);
            EnsureLocalAccountPanel(canvas.transform, manager, controller, visualController);
            if (!EnsureInputBindingsPanel(canvas.transform))
            {
                return false;
            }
            EnsureMilkroomPropInteractions(canvas.transform, petInteraction);
            EnsureUiButtonSounds(canvas.transform);
            EnsureAccessibleInputScopes(canvas.transform);
            AccessibilityRuntime.Apply(canvas.transform, manager.CurrentSave.settings);
            RefreshMilkroomTopHudLayout(canvas.transform);
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
            EnsureAccessibleInputScopes(canvas.transform);
            AccessibilityRuntime.Apply(canvas.transform, manager.CurrentSave.settings);

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
        private static readonly System.Type EditorTestRunnerApi = System.Type.GetType(
            "UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
        private static readonly System.Reflection.MethodInfo EditorTestRunActive = EditorTestRunnerApi?.GetMethod(
            "IsRunActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        public static bool DeferAutomaticEditorPreview => Application.isBatchMode
            || UnityEditor.BuildPipeline.isBuildingPlayer
            || (EditorTestRunnerApi != null && (EditorTestRunActive == null
                || (bool)EditorTestRunActive.Invoke(null, null)));

        public static bool SyncEditorScenePreview()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                || DeferAutomaticEditorPreview)
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
            var previewSave = LoadEditorPreviewSave() ?? new CheeseTamaSaveData();
            previewSave.EnsureRuntimeDefaults();

            if (scene.name == SceneNames.Milkroom)
            {
                var canvas = GameObject.Find("Milkroom Canvas");
                SyncEditorUiPreview(canvas != null ? canvas.transform : null, previewSave);
            }
            else
            {
                Object.FindFirstObjectByType<DebugUIController>()?.Bind(previewSave);
                var canvas = GameObject.Find("Debug Canvas");
                SyncEditorUiPreview(canvas != null ? canvas.transform : null, previewSave);
            }

            var themeId = string.IsNullOrWhiteSpace(previewSave.milkroomThemeId)
                ? MilkroomThemeController.MorningThemeId
                : previewSave.milkroomThemeId;
            Object.FindFirstObjectByType<MilkroomThemeController>()?.ApplyTheme(themeId);
            Object.FindFirstObjectByType<MilkroomLightingController>()?.ApplyTheme(themeId);
            Object.FindFirstObjectByType<MilkroomAmbientEventController>()?.SetTheme(themeId);
            return true;
        }

        /// <summary>Refreshes presentation only; does not initialize gameplay, save, or advance time.</summary>
        public static void SyncEditorUiPreview(Transform canvasTransform, CheeseTamaSaveData previewSave)
        {
            if (canvasTransform == null || previewSave == null || IsRuntimeSessionActive())
            {
                return;
            }

            previewSave.EnsureRuntimeDefaults();
            KeepOverlayCanvasAtSceneRoot(canvasTransform);
            var controller = canvasTransform.GetComponent<MilkroomUIController>();
            if (controller != null)
            {
                RemoveLegacyMilkroomPanelObjects(canvasTransform);
                ReapplyRoundedImages(canvasTransform);
                EnsureExistingStatusExplanationScroll(canvasTransform);
                ConfigureCookingPanelHeaderAlignment(canvasTransform.Find("Cooking Panel"));
                EnsureMilkroomStatGauges(canvasTransform, controller);
                controller.Bind(previewSave);
            }

            GameSettingsPanelController.ApplyUiScaleToCanvas(
                canvasTransform.GetComponent<CanvasScaler>(), previewSave.settings.uiScale);
            var careTip = canvasTransform.Find("Care Tip Panel");
            if (careTip != null) careTip.gameObject.SetActive(previewSave.settings.showCareTips);

            var font = KoreanUiFontRuntime.GetDefaultFont();
            foreach (var label in canvasTransform.GetComponentsInChildren<Text>(true))
            {
                if (font != null && label.font != font) label.font = font;
            }
            AccessibilityRuntime.Apply(canvasTransform, previewSave.settings);
            var responsive = canvasTransform.GetComponent<ResponsiveCanvasRuntime>();
            if (responsive != null)
            {
                responsive.Configure(Application.platform == RuntimePlatform.WebGLPlayer,
                    Application.isMobilePlatform || Input.touchSupported);
                responsive.RefreshLayout();
            }
            RefreshMilkroomTopHudLayout(canvasTransform);
            // Exiting Play Mode discards the transient CanvasRenderer geometry.
            // Refresh the existing graphics instead of rebuilding the whole scene.
            foreach (var graphic in canvasTransform.GetComponentsInChildren<Graphic>(true))
                if (graphic.isActiveAndEnabled) graphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();
        }

        private static CheeseTamaSaveData LoadEditorPreviewSave()
        {
            // Automatic editor presentation must not consume another test's isolated
            // save or change global accessibility settings while a suite is running.
            if (DeferAutomaticEditorPreview) return null;
            var saveManager = Object.FindFirstObjectByType<SaveManager>();
            return ReadEditorPreviewSave(saveManager);
        }

        public static CheeseTamaSaveData ReadEditorPreviewSave(SaveManager saveManager)
        {
            // Use the same account/isolated path resolver as Play Mode, without Load(),
            // recovery, migration writes, or creating a CoreSystems instance.
            var savePath = saveManager != null ? saveManager.SaveFilePath
                : System.IO.Path.Combine(Application.persistentDataPath, "cheesetama_save.json");
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
            var manager = EnsureCoreSystems();
            EnsureCamera("Boot Camera");
            EnsureEventSystem();
            var canvas = EnsureCanvas("Boot Canvas");
            EnsureBootIntro(canvas.transform, IsLocalAccountSignedIn(manager));
            var startButton = GetOrCreateButton(
                canvas.transform,
                "Start Button",
                "입장하기",
                new Vector2(0f, 86f),
                new Vector2(420f, 88f));
            ApplyCareButtonStyle(startButton);
            var startLabel = startButton.GetComponentInChildren<Text>(true);
            if (startLabel != null)
            {
                startLabel.fontSize = 26;
                startLabel.resizeTextForBestFit = true;
                startLabel.resizeTextMinSize = 20;
                startLabel.resizeTextMaxSize = 26;
            }
            startButton.onClick.RemoveAllListeners();
            var legacyNavigation = startButton.GetComponent<SceneNavigationButton>();
            if (legacyNavigation != null)
            {
                legacyNavigation.enabled = false;
            }
            EnsureBootLocalAccountFlow(canvas.transform, manager);
            EnsureAccessibleInputScopes(canvas.transform);
            AccessibilityRuntime.Apply(
                canvas.transform,
                ResolveSceneDisplaySave(manager)?.settings);
        }

        private static void EnsureBootIntro(Transform canvasTransform, bool signedIn)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var title = GetOrCreateText(
                canvasTransform,
                "Title Text",
                "CheeseTama",
                52,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -39f),
                new Vector2(960f, 84f),
                true);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.46f, 0.24f, 0.07f, 1f);
            ConfigureBootIntroText(title, 42, 52, TextAnchor.MiddleCenter);

            var subtitle = GetOrCreateText(
                canvasTransform,
                "Subtitle Text",
                "작은 치즈 친구와 오늘을 시작해요",
                26,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -131f),
                new Vector2(1100f, 50f),
                true);
            ConfigureBootIntroText(subtitle, 21, 26, TextAnchor.MiddleCenter);

            var card = GetOrCreatePanel(
                canvasTransform,
                "Boot Intro Card",
                Vector2.zero,
                new Vector2(1200f, 640f));
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 1f);
            cardRect.anchorMax = new Vector2(0.5f, 1f);
            cardRect.pivot = new Vector2(0.5f, 1f);
            // Keep the feature card itself vertically centered on the authored home canvas.
            // The entry CTA remains bottom-fixed and therefore never competes with the card.
            cardRect.anchoredPosition = new Vector2(0f, -220f);
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.975f, 0.86f, 0.97f);
                cardImage.raycastTarget = false;
            }

            var headline = GetOrCreateText(
                card.transform,
                "Boot Intro Headline Text",
                "치즈타마와 무엇을 할 수 있을까요?",
                34,
                TextAnchor.MiddleCenter,
                new Vector2(50f, -34f),
                new Vector2(1100f, 52f));
            headline.fontStyle = FontStyle.Bold;
            ConfigureBootIntroText(headline, 28, 34, TextAnchor.MiddleCenter);

            var description = GetOrCreateText(
                card.transform,
                "Boot Intro Description Text",
                "우유를 주고, 놀아 주고, 밀크룸을 꾸며 보세요.\n돌볼수록 새로운 모습과 이야기를 만날 수 있어요.",
                24,
                TextAnchor.MiddleCenter,
                new Vector2(70f, -92f),
                new Vector2(1060f, 88f));
            ConfigureBootIntroText(description, 20, 24, TextAnchor.MiddleCenter);

            CreateBootFeatureCard(
                card.transform,
                "Boot Care Feature Card",
                new Vector2(50f, -210f),
                "1",
                "잘 돌봐요",
                "배고프면 우유를 주고,\n졸리면 편하게 쉬게 해요.",
                new Color(1f, 0.88f, 0.58f, 0.96f));
            CreateBootFeatureCard(
                card.transform,
                "Boot Decorate Feature Card",
                new Vector2(438f, -210f),
                "2",
                "내 방처럼 꾸며요",
                "마음에 드는 소품을 놓고\n밀크룸을 예쁘게 바꿔요.",
                new Color(0.83f, 0.94f, 0.72f, 0.96f));
            CreateBootFeatureCard(
                card.transform,
                "Boot Story Feature Card",
                new Vector2(826f, -210f),
                "3",
                "새 이야기를 찾아요",
                "손님을 만나고,\n숨은 기록을 하나씩 찾아요.",
                new Color(0.78f, 0.9f, 1f, 0.96f));

            var sessionPanel = GetOrCreatePanel(
                card.transform,
                "Boot Session Note Panel",
                new Vector2(50f, -430f),
                new Vector2(1100f, 134f));
            if (sessionPanel.TryGetComponent(out Image sessionImage))
            {
                sessionImage.color = new Color(1f, 0.93f, 0.67f, 0.78f);
                sessionImage.raycastTarget = false;
            }

            var sessionTitle = GetOrCreateText(
                sessionPanel.transform,
                "Boot Session Title Text",
                string.Empty,
                22,
                TextAnchor.MiddleCenter,
                new Vector2(28f, -18f),
                new Vector2(1044f, 34f));
            sessionTitle.fontStyle = FontStyle.Bold;
            ConfigureBootIntroText(sessionTitle, 18, 22, TextAnchor.MiddleCenter);

            var sessionBody = GetOrCreateText(
                sessionPanel.transform,
                "Boot Session Body Text",
                string.Empty,
                19,
                TextAnchor.MiddleCenter,
                new Vector2(28f, -56f),
                new Vector2(1044f, 66f));
            ConfigureBootIntroText(sessionBody, 16, 19, TextAnchor.MiddleCenter);
            ApplyBootIntroSessionCopy(canvasTransform, signedIn);
        }

        private static void CreateBootFeatureCard(
            Transform parent,
            string name,
            Vector2 position,
            string stepLabel,
            string titleText,
            string bodyText,
            Color background)
        {
            var card = GetOrCreatePanel(parent, name, position, new Vector2(324f, 180f));
            if (card.TryGetComponent(out Image image))
            {
                image.color = background;
                image.raycastTarget = false;
            }

            var badge = GetOrCreatePanel(
                card.transform,
                "Step Badge",
                new Vector2(18f, -16f),
                new Vector2(44f, 44f));
            if (badge.TryGetComponent(out Image badgeImage))
            {
                badgeImage.color = new Color(1f, 0.68f, 0.16f, 1f);
                badgeImage.raycastTarget = false;
            }

            var badgeText = GetOrCreateText(
                badge.transform,
                "Step Text",
                stepLabel,
                22,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(44f, 44f));
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.color = new Color(0.28f, 0.15f, 0.05f, 1f);
            ConfigureBootIntroText(badgeText, 18, 22, TextAnchor.MiddleCenter);

            var title = GetOrCreateText(
                card.transform,
                "Title Text",
                titleText,
                26,
                TextAnchor.MiddleCenter,
                new Vector2(64f, -18f),
                new Vector2(238f, 40f));
            title.fontStyle = FontStyle.Bold;
            ConfigureBootIntroText(title, 21, 26, TextAnchor.MiddleCenter);

            var body = GetOrCreateText(
                card.transform,
                "Body Text",
                bodyText,
                20,
                TextAnchor.MiddleCenter,
                new Vector2(22f, -60f),
                new Vector2(280f, 106f));
            ConfigureBootIntroText(body, 16, 20, TextAnchor.MiddleCenter);
        }

        private static void ConfigureBootIntroText(
            Text text,
            int minimumSize,
            int maximumSize,
            TextAnchor alignment)
        {
            if (text == null)
            {
                return;
            }

            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimumSize;
            text.resizeTextMaxSize = maximumSize;
            text.lineSpacing = 1.16f;
            text.GetComponent<AccessibilityTextProfile>()?.Rebase(text);
        }

        private static void ApplyBootIntroSessionCopy(Transform canvasTransform, bool signedIn)
        {
            var sessionPanel = canvasTransform != null
                ? canvasTransform.Find("Boot Intro Card/Boot Session Note Panel")
                : null;
            var title = sessionPanel != null
                ? sessionPanel.Find("Boot Session Title Text")?.GetComponent<Text>()
                : null;
            var body = sessionPanel != null
                ? sessionPanel.Find("Boot Session Body Text")?.GetComponent<Text>()
                : null;

            AccessibilityRuntime.SetTextAndApply(
                title,
                signedIn ? "계정으로 이어서 해요" : "시작 방법을 골라요");
            AccessibilityRuntime.SetTextAndApply(
                body,
                signedIn
                    ? "로그인한 계정의 게임 기록을 불러왔어요. 준비되면 바로 입장해 주세요."
                    : "먼저 홈을 둘러보세요. 입장하기를 누르면 로그인하거나 게스트로 시작할 수 있어요.");
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
            var nameText = GetOrCreateText(topBarTransform, "Name Text", "CheeseTama", 28, TextAnchor.MiddleLeft, new Vector2(TopIdentityLeft, -TopIdentityNameTop), new Vector2(TopIdentityPreferredWidth, TopIdentityNameHeight));
            var levelText = GetOrCreateText(topBarTransform, "Level Text", "레벨 1 · 성장 0%", 14, TextAnchor.MiddleLeft, new Vector2(TopIdentityProgressLeft, -TopIdentityLevelTop), new Vector2(TopIdentityProgressPreferredWidth, TopIdentityLevelHeight));
            levelText.supportRichText = false;
            var sessionText = GetOrCreateText(topBarTransform, "Session Text", "지금 함께한 시간 00:00\n오늘 함께한 시간 00:00", 15, TextAnchor.MiddleLeft, new Vector2(TopSessionLeft, -TopSessionTop), new Vector2(300, TopSessionHeight));
            RemoveChildIfExists(topBarTransform, "Economy Text");
            var coinEconomyText = GetOrCreateText(topBarTransform, "Coin Economy Text", "코인 0", 17, TextAnchor.MiddleLeft, new Vector2(0, -17), new Vector2(TopCoinResourceTextWidth, 48));
            var milkDropEconomyText = GetOrCreateText(topBarTransform, "Milk Drop Economy Text", "우유방울 0", 17, TextAnchor.MiddleLeft, new Vector2(0, -17), new Vector2(TopMilkDropResourceTextWidth, 48));
            var collectionFragmentEconomyText = GetOrCreateText(topBarTransform, "Collection Fragment Economy Text", "도감조각 0", 17, TextAnchor.MiddleLeft, new Vector2(0, -17), new Vector2(TopCollectionFragmentResourceTextWidth, 48));
            ApplyTopInfoTextStyle(nameText, 28);
            ApplyTopInfoTextStyle(levelText, 14);
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
            SetButtonIcon(topCollectionButton, "collection");
            SetButtonIcon(topDecorateButton, "decorate");
            SetButtonIcon(settingsButton, "settings");
            ApplyMilkroomTopHudLayout(
                topBar,
                topMenu,
                nameText,
                levelText,
                coinEconomyText,
                milkDropEconomyText,
                collectionFragmentEconomyText,
                topCollectionButton,
                topDecorateButton,
                settingsButton);

            var panel = GetOrCreateRightPanel(canvas.transform, "Status Panel", new Vector2(-24, -116), new Vector2(360, 556));
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
            var careSummarySection = GetOrCreatePanel(panelTransform, "Record Care Summary Section", new Vector2(12, -296), new Vector2(336, 96));
            ApplyRecordSectionStyle(careSummarySection, recordPrimaryColor);
            var dailyRoutineSection = GetOrCreatePanel(panelTransform, "Record Daily Routine Section", new Vector2(12, -404), new Vector2(336, 140));
            ApplyRecordSectionStyle(dailyRoutineSection, recordSecondaryColor);

            var detailTitleText = GetOrCreateText(panelTransform, "Detail Title Text", "밀크룸 기록", 22, TextAnchor.UpperLeft, new Vector2(22, -20), new Vector2(316, 34));
            detailTitleText.fontStyle = FontStyle.Bold;
            var formText = GetOrCreateText(identitySection.transform, "Form Text", "<b>형태</b>  알", 17, TextAnchor.MiddleLeft, new Vector2(10, -10), new Vector2(316, 30));
            var conditionText = GetOrCreateText(identitySection.transform, "Condition Text", "<b>상태</b>  따뜻함", 17, TextAnchor.MiddleLeft, new Vector2(10, -48), new Vector2(316, 30));
            var affectionText = GetOrCreateText(growthSection.transform, "Affection Text", "<b>애정</b>  10", 17, TextAnchor.MiddleLeft, new Vector2(10, -10), new Vector2(316, 30));
            var maturationText = GetOrCreateText(growthSection.transform, "Maturation Text", "<b>성장</b>  0", 17, TextAnchor.MiddleLeft, new Vector2(10, -48), new Vector2(316, 30));
            var hatchProgressText = GetOrCreateText(growthSection.transform, "Hatch Progress Text", "<b>부화 진행</b>  0%", 17, TextAnchor.MiddleLeft, new Vector2(10, -86), new Vector2(316, 30));
            var careSummaryText = GetOrCreateRecordText(careSummarySection.transform, "Care Summary Text", "<b>돌봄 누적</b>  0회" + RecordDetailVerticalGap + "놀이 0  청소 0  휴식 0", 16, TextAnchor.MiddleLeft, new Vector2(316, 60));
            var dailyRoutineText = GetOrCreateRecordText(dailyRoutineSection.transform, "Daily Routine Text", "<b>오늘 루틴</b>" + RecordDetailVerticalGap + "먹기 0/3  요리 0/2\n놀이 0/3  청소 0/2  휴식 0/2\n<size=14>완료 보상  코인 20 · 우유방울 5 · 도감조각 1</size>", 16, TextAnchor.MiddleLeft, new Vector2(316, 104));
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
            var hungerText = GetOrCreateText(statBarTransform, "Hunger Text", "배부름  80/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -72), new Vector2(306, 30));
            var moodText = GetOrCreateText(statBarTransform, "Mood Text", "기분  70/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -132), new Vector2(306, 30));
            var cleanlinessText = GetOrCreateText(statBarTransform, "Cleanliness Text", "깨끗함  90/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -192), new Vector2(306, 30));
            var sleepinessText = GetOrCreateText(statBarTransform, "Sleepiness Text", "졸림  20/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -252), new Vector2(306, 30));
            var healthText = GetOrCreateText(statBarTransform, "Health Text", "건강  100/100", 20, TextAnchor.MiddleLeft, new Vector2(22, -312), new Vector2(306, 30));

            BuildMilkroomFeedbackPanels(
                canvas.transform,
                out var careTipText,
                out var messageText,
                out var eventMessageText);

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
            ConfigureCareTipInteraction(canvas.transform, controller);
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
            ConfigureCareButton(sleepButton, MilkroomCareAction.Rest, controller, visualController);

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
            EnsureBlueBallMiniGame(canvas.transform, controller, visualController);
            EnsurePlayChoicePanel(canvas.transform);
            EnsureCleaningMiniGame(canvas.transform, controller, visualController);
            EnsureCareEventCard(canvas.transform, visualController);
            EnsureNpcVisitCard(canvas.transform);
            EnsureDirectRestAction(canvas.transform, controller, visualController);
            EnsureDecorationShop(canvas.transform);
            EnsureDecorationRoomPresenter();
            EnsureDecorationPlacement(canvas.transform);
            EnsureMilkroomViewportNavigation(canvas.transform);
            EnsureMilkroomAtmosphere(canvas.transform, manager.CurrentTama);
            EnsureMilkroomSeasonalLayer(canvas.transform, manager);
            var petInteraction = EnsureCheeseTamaPetInteraction(
                canvas.transform,
                controller,
                visualController);
            EnsureCheeseTamaSpeechBubble(canvas.transform, visualController);
            EnsureAutonomousLife(canvas.transform, visualController);
            RemoveNormalEvolutionVisualAccents(visualController);
            EnsureLateGameFeatures(canvas.transform, controller, visualController);
            EnsureCheeseStarDelivery(canvas.transform);
            EnsureMemoryJournal(canvas.transform);
            EnsureFantasyPowderHiddenRecipes(canvas.transform);
            EnsureFirstDayJourney(canvas.transform);
            EnsureJourneyHub(canvas.transform);
            EnsureCheeseTamaProfileMenu(canvas.transform);
            EnsureLocalAccountPanel(canvas.transform, manager, controller, visualController);
            EnsureInputBindingsPanel(canvas.transform);
            EnsureMilkroomPropInteractions(canvas.transform, petInteraction);
            EnsureUiButtonSounds(canvas.transform);
            EnsureAccessibleInputScopes(canvas.transform);
            AccessibilityRuntime.Apply(
                canvas.transform,
                ResolveSceneDisplaySave(manager)?.settings);
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

            var panel = GetOrCreateCollectionRecordsPanel(canvas.transform);

            var panelTransform = panel.transform;
            RemoveChildIfExists(panelTransform, "Milk Records Text");
            RemoveChildIfExists(panelTransform, "Evolution Records Text");
            RemoveChildIfExists(panelTransform, "Event Records Text");
            RemoveChildIfExists(panelTransform, "Hidden Records Text");
            RemoveChildIfExists(panelTransform, "Event Records Tab Button");

            var milkTabButton = GetOrCreateTopLeftButton(panelTransform, "Milk Records Tab Button", "발견", new Vector2(24, -24), new Vector2(476, 52));
            var evolutionTabButton = GetOrCreateTopLeftButton(panelTransform, "Evolution Records Tab Button", "진화", new Vector2(512, -24), new Vector2(476, 52));
            var hiddenTabButton = GetOrCreateTopLeftButton(panelTransform, "Hidden Records Tab Button", "특별", new Vector2(1000, -24), new Vector2(476, 52));
            ApplyCollectionTabButtonStyle(milkTabButton, evolutionTabButton, hiddenTabButton);

            var recordHeaderText = GetOrCreateText(panelTransform, "Collection Records Header Text", "<b>발견 기록</b>  <size=15>0개 발견</size>", 20, TextAnchor.MiddleLeft, new Vector2(30, -88), new Vector2(1440, 34));
            recordHeaderText.supportRichText = true;
            recordHeaderText.color = new Color(0.25f, 0.17f, 0.09f);

            var scrollContent = GetOrCreateCollectionScrollContent(panelTransform, new Vector2(24, -134), new Vector2(1452, 502));
            RemoveChildIfExists(scrollContent, "Event Records Text");
            RemoveChildIfExists(scrollContent, "Discovery Records Card Root");
            RemoveChildIfExists(scrollContent, "Milk Records Card Root");
            RemoveChildIfExists(scrollContent, "Evolution Records Card Root");
            RemoveChildIfExists(scrollContent, "Event Records Card Root");
            RemoveChildIfExists(scrollContent, "Hidden Records Card Root");
            var milkText = GetOrCreateCollectionRecordText(scrollContent, "Milk Records Text", "발견 기록: 0", 18);
            var evolutionText = GetOrCreateCollectionRecordText(scrollContent, "Evolution Records Text", "진화 기록: 0", 18);
            var hiddenText = GetOrCreateCollectionRecordText(scrollContent, "Hidden Records Text", "특별 기록: 0", 18);
            var messageText = GetOrCreateText(panelTransform, "Collection Message Text", "찾은 기록만 보여요. 밀크룸에서 계속 돌보면 새로운 기록이 생겨요.", 15, TextAnchor.MiddleLeft, new Vector2(30, -658), new Vector2(1148, 52));
            messageText.color = new Color(0.38f, 0.28f, 0.17f);
            ConfigureMultiLineFeedbackText(messageText, 13, 15);

            controller.Configure(
                milkText,
                evolutionText,
                null,
                hiddenText,
                messageText,
                recordHeaderText,
                milkTabButton,
                evolutionTabButton,
                null,
                hiddenTabButton);
            controller.Bind(ResolveSceneDisplaySave(manager));

            var backButton = GetOrCreateButton(canvas.transform, "Milkroom Button", "밀크룸", new Vector2(0, 36), new Vector2(164, 50));
            var backLabel = backButton.GetComponentInChildren<Text>(true);
            if (backLabel != null)
            {
                backLabel.fontStyle = FontStyle.Bold;
            }

            ConfigureNavigationButton(backButton, SceneNames.Milkroom, false);
            EnsureAccessibleInputScopes(canvas.transform);
            AccessibilityRuntime.Apply(
                canvas.transform,
                ResolveSceneDisplaySave(manager)?.settings);
        }

        private static GameObject GetOrCreateCollectionRecordsPanel(Transform canvasTransform)
        {
            var panel = GetOrCreatePanel(
                canvasTransform,
                "Collection Records Panel",
                Vector2.zero,
                new Vector2(1500f, 720f));
            ConfigureCenteredRect(panel.GetComponent<RectTransform>(), new Vector2(1500f, 720f));
            if (panel.TryGetComponent(out Image panelImage))
            {
                panelImage.color = new Color(1f, 0.98f, 0.9f, 0.96f);
            }

            return panel;
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

            var unlockStarButton = GetOrCreateButton(canvas.transform, "Unlock Star Preset Button", "별빛 사용 가능", new Vector2(-70, 36));
            ConfigureDebugButton(unlockStarButton, DebugAction.UnlockStarMilk, controller, visualController);

            var resetButton = GetOrCreateButton(canvas.transform, "Debug Reset Button", "초기화", new Vector2(70, 36));
            ConfigureDebugButton(resetButton, DebugAction.ResetSave, controller, visualController);

            var forceEventButton = GetOrCreateButton(canvas.transform, "Force Event Button", "이벤트 발생", new Vector2(210, 36));
            ConfigureDebugButton(forceEventButton, DebugAction.ForceEvent, controller, visualController);

            var stayButton = GetOrCreateButton(canvas.transform, "Stay Five Minutes Button", "5분 체류", new Vector2(350, 96));
            ConfigureDebugButton(stayButton, DebugAction.AddSessionFiveMinutes, controller, visualController);

            var milkroomButton = GetOrCreateButton(canvas.transform, "Milkroom Button", "밀크룸", new Vector2(350, 36));
            ConfigureNavigationButton(milkroomButton, SceneNames.Milkroom, true);
            EnsureAccessibleInputScopes(canvas.transform);
            AccessibilityRuntime.Apply(
                canvas.transform,
                ResolveSceneDisplaySave(manager)?.settings);
        }

        private const float RugPlacedHeight = 0.06f;
        // The active growth model extends about 0.9 world units below its motion-root pivot.
        // The thin rug's flat center is 0.05451m above the floor (-2.13m).
        // The growth model's local -0.53m floor is scaled by 1.7, giving -0.901m.
        private static readonly Vector3 CheeseTamaRestingWorldPosition = new Vector3(0f, -1.1745f, 0.08f);

        private const string CheeseTamaEggPrefabPath = "Assets/Characters/CheeseTama/GrowthStages/CheeseTama_Egg.prefab";
        private const string CheeseTamaGrowthVisualSetPath = "Assets/_Project/Resources/CheeseTamaGrowthVisualSet.asset";
        private const float CheeseTamaModelYaw = 270f;
        private const float CheeseTamaModelScale = 1.7f;

    }
}
