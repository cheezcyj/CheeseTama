using System;
using System.Collections.Generic;
using CheeseTama.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class ResponsiveCanvasRuntime : MonoBehaviour
    {
        public const string LandscapePromptObjectName = "Mobile Landscape Prompt";
        private const float CollectionTitleTop = 82f;
        private const float CollectionHeaderWidth = 520f;
        private const float CollectionAuthoredTitleHeight = 44f;
        private const float CollectionAuthoredSubtitleHeight = 32f;
        private const float CollectionHeaderGap = 2f;
        private const float CollectionPanelHeaderGap = 18f;
        private const float CollectionAuthoredPanelHeight = 720f;
        private const float CollectionMinimumOuterGap = 18f;
        private const float CollectionBackButtonTopInset = 86f;
        private const float CollectionAuthoredBackButtonHeight = 50f;
        private const float CollectionMessageWidthBesideClaimButton = 1148f;
        private const float CollectionGlyphSafetyPadding = 2f;
        private const float MilkroomCareTipTop = 528f;
        private const float MilkroomLeftPanelGap = 16f;
        private const float MilkroomUtilityDefaultTop = 740f;
        private static readonly string[] MilkroomUtilityButtonNames =
        {
            "Open Delivery Button",
            JourneyHubPanelController.OpenButtonObjectName,
            "Open Npc Afterstory Button",
            "Open First Day Journey Button",
            "Open Milkroom Investigation Button",
            "Open Fantasy Powder Button",
            "Open Milkroom Mystery Button",
            "Open Dream Story Season Button"
        };

        private readonly Dictionary<RectTransform, SafeAreaInsets> appliedInsets =
            new Dictionary<RectTransform, SafeAreaInsets>();
        private readonly HashSet<RectTransform> safeAreaTargets = new HashSet<RectTransform>();
        private readonly List<RectTransform> staleTargets = new List<RectTransform>();
        private readonly List<Selectable> selectables = new List<Selectable>();
        private readonly List<RectTransform> activeMilkroomUtilityButtons =
            new List<RectTransform>(MilkroomUtilityButtonNames.Length);
        private readonly List<Rect> siblingSelectableBounds = new List<Rect>();
        private readonly Vector3[] rectWorldCorners = new Vector3[4];
        private readonly Dictionary<RectTransform, SettingsRectLayoutSnapshot> settingsRectLayouts =
            new Dictionary<RectTransform, SettingsRectLayoutSnapshot>();
        private readonly Dictionary<Text, SettingsTextLayoutSnapshot> settingsTextLayouts =
            new Dictionary<Text, SettingsTextLayoutSnapshot>();

        private Canvas targetCanvas;
        private CanvasScaler canvasScaler;
        private RectTransform landscapePrompt;
        private RectTransform cachedSettingsContent;
        private int cachedSettingsDirectChildCount = -1;
        private bool isBrowser;
        private bool touchPreferred;
        private bool configured;
        private float nextHierarchyRefreshAt;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private Rect lastSafeArea;
        private float lastCanvasScale = -1f;

        public ResponsiveUiProfile CurrentProfile { get; private set; }

        public void Configure(bool browser, bool preferTouch)
        {
            var configurationChanged = !configured
                || isBrowser != browser
                || touchPreferred != preferTouch;
            isBrowser = browser;
            touchPreferred = preferTouch;
            ResolveComponents();
            configured = true;
            if (configurationChanged)
            {
                ApplyCurrentLayout(forceHierarchyRefresh: true);
            }
        }

        public void ApplyLayout(
            int screenWidth,
            int screenHeight,
            Rect safeArea,
            float canvasScaleFactor,
            bool browser,
            bool preferTouch,
            bool forceHierarchyRefresh = true)
        {
            isBrowser = browser;
            touchPreferred = preferTouch;
            ResolveComponents();
            CurrentProfile = ResponsiveUiLayoutPolicy.Resolve(
                screenWidth,
                screenHeight,
                isBrowser,
                touchPreferred);
            ApplyScaleProfile();
            ApplyCollectionPageResponsiveLayout(screenHeight, canvasScaleFactor);
            ApplyBootIntroResponsiveLayout(screenWidth, screenHeight, canvasScaleFactor);
            ApplyMilkroomUtilityResponsiveLayout(screenWidth, screenHeight, canvasScaleFactor);
            ApplySettingsScrollResponsiveLayout(screenWidth, screenHeight, canvasScaleFactor);
            ApplyCookingChoiceResponsiveLayout(canvasScaleFactor);
            StarterSceneBuilder.RefreshMilkroomTopHudLayout(targetCanvas?.transform);
            EnsureLandscapePrompt(CurrentProfile.ShowLandscapePrompt);

            if (forceHierarchyRefresh)
            {
                var insets = SafeAreaInsets.FromScreenPixels(
                    safeArea,
                    screenWidth,
                    screenHeight,
                    canvasScaleFactor);
                ApplySafeArea(insets);
                ApplyAdaptiveTouchHitAreas(
                    CurrentProfile.PreferredTouchTargetPixels <= 0f
                        ? 0f
                        : CurrentProfile.PreferredTouchTargetPixels
                          / Mathf.Max(0.0001f, canvasScaleFactor));
            }
        }

        public void RefreshLayout()
        {
            ApplyCurrentLayout(forceHierarchyRefresh: true);
        }

        private void Awake()
        {
            ResolveComponents();
        }

        private void LateUpdate()
        {
            ApplyCurrentLayout(forceHierarchyRefresh: Time.unscaledTime >= nextHierarchyRefreshAt);
        }

        private void OnEnable()
        {
            if (configured)
            {
                ApplyCurrentLayout(forceHierarchyRefresh: true);
            }
        }

        private void OnDisable()
        {
            RestoreAppliedInsets();
        }

        private void ApplyCurrentLayout(bool forceHierarchyRefresh)
        {
            ResolveComponents();
            if (targetCanvas == null || !targetCanvas.isRootCanvas)
            {
                return;
            }

            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);
            var safeArea = Screen.safeArea;
            CurrentProfile = ResponsiveUiLayoutPolicy.Resolve(
                width,
                height,
                isBrowser,
                touchPreferred);
            ApplyScaleProfile();
            var scale = ResolveCanvasScaleFactor(width, height);
            var metricsChanged = width != lastScreenWidth
                || height != lastScreenHeight
                || safeArea != lastSafeArea
                || !Mathf.Approximately(scale, lastCanvasScale);
            ApplyCollectionPageResponsiveLayout(height, scale);
            ApplyBootIntroResponsiveLayout(width, height, scale);
            ApplyMilkroomUtilityResponsiveLayout(width, height, scale);
            ApplySettingsScrollResponsiveLayout(width, height, scale);
            ApplyCookingChoiceResponsiveLayout(scale);
            StarterSceneBuilder.RefreshMilkroomTopHudLayout(targetCanvas.transform);
            EnsureLandscapePrompt(CurrentProfile.ShowLandscapePrompt);

            if (metricsChanged || forceHierarchyRefresh)
            {
                ApplySafeArea(SafeAreaInsets.FromScreenPixels(safeArea, width, height, scale));
                ApplyAdaptiveTouchHitAreas(
                    CurrentProfile.PreferredTouchTargetPixels <= 0f
                        ? 0f
                        : CurrentProfile.PreferredTouchTargetPixels / scale);
                nextHierarchyRefreshAt = Time.unscaledTime + 0.75f;
                lastScreenWidth = width;
                lastScreenHeight = height;
                lastSafeArea = safeArea;
                lastCanvasScale = scale;
            }
        }

        private void ResolveComponents()
        {
            targetCanvas ??= GetComponent<Canvas>();
            canvasScaler ??= GetComponent<CanvasScaler>();
        }

        private void ApplyScaleProfile()
        {
            if (canvasScaler == null
                || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return;
            }

            if (!Mathf.Approximately(
                    canvasScaler.matchWidthOrHeight,
                    CurrentProfile.CanvasMatchWidthOrHeight))
            {
                canvasScaler.matchWidthOrHeight = CurrentProfile.CanvasMatchWidthOrHeight;
            }
        }

        private float ResolveCanvasScaleFactor(int screenWidth, int screenHeight)
        {
            var fallback = Mathf.Max(0.0001f, targetCanvas != null ? targetCanvas.scaleFactor : 1f);
            if (canvasScaler == null
                || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || canvasScaler.referenceResolution.x <= 0.0001f
                || canvasScaler.referenceResolution.y <= 0.0001f)
            {
                return fallback;
            }

            var widthScale = Mathf.Max(1, screenWidth) / canvasScaler.referenceResolution.x;
            var heightScale = Mathf.Max(1, screenHeight) / canvasScaler.referenceResolution.y;
            return canvasScaler.screenMatchMode switch
            {
                CanvasScaler.ScreenMatchMode.Expand => Mathf.Max(0.0001f, Mathf.Min(widthScale, heightScale)),
                CanvasScaler.ScreenMatchMode.Shrink => Mathf.Max(0.0001f, Mathf.Max(widthScale, heightScale)),
                _ => Mathf.Max(
                    0.0001f,
                    Mathf.Pow(
                        2f,
                        Mathf.Lerp(
                            Mathf.Log(Mathf.Max(0.0001f, widthScale), 2f),
                            Mathf.Log(Mathf.Max(0.0001f, heightScale), 2f),
                            canvasScaler.matchWidthOrHeight)))
            };
        }

        private void ApplyCollectionPageResponsiveLayout(
            int screenHeight,
            float canvasScaleFactor)
        {
            if (targetCanvas == null || targetCanvas.name != "Collection Canvas")
            {
                return;
            }

            var root = targetCanvas.transform as RectTransform;
            var title = root?.Find("Title Text") as RectTransform;
            var subtitle = root?.Find("Subtitle Text") as RectTransform;
            var panel = root?.Find("Collection Records Panel") as RectTransform;
            var scroll = panel?.Find("Collection Scroll View") as RectTransform;
            var message = panel?.Find("Collection Message Text") as RectTransform;
            if (root == null
                || title == null
                || subtitle == null
                || panel == null
                || scroll == null
                || message == null)
            {
                return;
            }

            var logicalHeight = Mathf.Max(1, screenHeight)
                / Mathf.Max(0.0001f, canvasScaleFactor);
            var minimumPanelTop = ApplyCollectionHeaderLayout(title, subtitle);
            ApplyCollectionRecordsPanelLayout(
                panel,
                scroll,
                message,
                logicalHeight,
                canvasScaleFactor,
                minimumPanelTop);
        }

        private static float ApplyCollectionHeaderLayout(
            RectTransform title,
            RectTransform subtitle)
        {
            var titleHeight = ResolveCollectionHeaderHeight(
                title.GetComponent<Text>(),
                CollectionAuthoredTitleHeight);
            title.anchoredPosition = new Vector2(0f, -CollectionTitleTop);
            title.sizeDelta = new Vector2(CollectionHeaderWidth, titleHeight);

            var subtitleTop = CollectionTitleTop + titleHeight + CollectionHeaderGap;
            var subtitleHeight = ResolveCollectionHeaderHeight(
                subtitle.GetComponent<Text>(),
                CollectionAuthoredSubtitleHeight);
            subtitle.anchoredPosition = new Vector2(0f, -subtitleTop);
            subtitle.sizeDelta = new Vector2(CollectionHeaderWidth, subtitleHeight);

            return subtitleTop + subtitleHeight + CollectionPanelHeaderGap;
        }

        private void ApplyCollectionRecordsPanelLayout(
            RectTransform panel,
            RectTransform scroll,
            RectTransform message,
            float logicalHeight,
            float canvasScaleFactor,
            float minimumPanelTop)
        {
            var centeredPanelTop = (logicalHeight - CollectionAuthoredPanelHeight) * 0.5f;
            var panelTop = Mathf.Max(minimumPanelTop, centeredPanelTop);
            var backButtonTop = logicalHeight - CollectionBackButtonTopInset;
            var preferredTouchTargetHeight = CurrentProfile.PreferredTouchTargetPixels <= 0f
                ? CollectionAuthoredBackButtonHeight
                : CurrentProfile.PreferredTouchTargetPixels
                  / Mathf.Max(0.0001f, canvasScaleFactor);
            var returnHitAreaTopExpansion = Mathf.Max(
                0f,
                (preferredTouchTargetHeight - CollectionAuthoredBackButtonHeight) * 0.5f);
            var minimumReturnGap = CollectionMinimumOuterGap + returnHitAreaTopExpansion;
            var panelHeight = Mathf.Max(
                1f,
                Mathf.Min(
                    CollectionAuthoredPanelHeight,
                    backButtonTop - minimumReturnGap - panelTop));

            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(
                0f,
                logicalHeight * 0.5f - panelTop - panelHeight * 0.5f);
            panel.sizeDelta = new Vector2(1500f, panelHeight);

            scroll.anchorMin = new Vector2(0f, 1f);
            scroll.anchorMax = new Vector2(0f, 1f);
            scroll.pivot = new Vector2(0f, 1f);
            scroll.anchoredPosition = new Vector2(24f, -134f);
            scroll.sizeDelta = new Vector2(
                1452f,
                Mathf.Max(1f, panelHeight - 218f));

            message.anchorMin = new Vector2(0f, 1f);
            message.anchorMax = new Vector2(0f, 1f);
            message.pivot = new Vector2(0f, 1f);
            message.anchoredPosition = new Vector2(
                30f,
                -(panelHeight - 62f));
            message.sizeDelta = new Vector2(CollectionMessageWidthBesideClaimButton, 52f);
        }

        private static float ResolveCollectionHeaderHeight(Text label, float authoredHeight)
        {
            if (label == null)
            {
                return authoredHeight;
            }

            var preferredHeight = label.preferredHeight;
            if (float.IsNaN(preferredHeight) || float.IsInfinity(preferredHeight))
            {
                return authoredHeight;
            }

            return Mathf.Max(
                authoredHeight,
                Mathf.Ceil(preferredHeight) + CollectionGlyphSafetyPadding);
        }

        private void ApplyBootIntroResponsiveLayout(
            int screenWidth,
            int screenHeight,
            float canvasScaleFactor)
        {
            if (targetCanvas == null)
            {
                return;
            }

            var root = targetCanvas.transform;
            var introCard = root.Find("Boot Intro Card") as RectTransform;
            if (introCard == null)
            {
                return;
            }

            var aspect = Mathf.Max(1, screenWidth) / (float)Mathf.Max(1, screenHeight);
            var wideCompact = CurrentProfile.Kind == ResponsiveUiProfileKind.CompactLandscape
                && aspect >= 1.9f;
            var densePhoneBoot = wideCompact
                && screenWidth < 1024
                && screenHeight <= 480;
            var spaciousBrowserBoot = isBrowser
                && CurrentProfile.Kind == ResponsiveUiProfileKind.WideLandscape;
            var canvasScale = Mathf.Max(0.0001f, canvasScaleFactor);
            var compactFontFloor = wideCompact
                ? Mathf.CeilToInt(13f / canvasScale)
                : 1;
            var startButtonBottom = densePhoneBoot
                ? 34f
                : wideCompact
                    ? 56f
                    : spaciousBrowserBoot ? 60f : 86f;
            var startButtonSize = densePhoneBoot
                ? new Vector2(500f, 90f)
                : wideCompact
                    ? new Vector2(560f, 110f)
                    : spaciousBrowserBoot
                        ? new Vector2(300f, 64f)
                        : new Vector2(420f, 88f);
            var minimumPhysicalStartGap = spaciousBrowserBoot ? 24f : 16f;
            var titleSize = densePhoneBoot
                ? new Vector2(1460f, 76f)
                : wideCompact
                    ? new Vector2(1460f, 84f)
                    : spaciousBrowserBoot
                        ? new Vector2(760f, 64f)
                        : new Vector2(960f, 84f);
            var subtitleSize = densePhoneBoot
                ? new Vector2(1600f, 42f)
                : wideCompact
                    ? new Vector2(1600f, 50f)
                    : spaciousBrowserBoot
                        ? new Vector2(900f, 40f)
                        : new Vector2(1100f, 50f);
            var titleFontSize = spaciousBrowserBoot ? 42 : 52;
            var titleMinimumFontSize = spaciousBrowserBoot ? 35 : 42;
            var subtitleFontSize = spaciousBrowserBoot ? 20 : 26;
            var subtitleMinimumFontSize = spaciousBrowserBoot ? 17 : 21;
            var startFontSize = spaciousBrowserBoot ? 20 : 26;
            var startMinimumFontSize = spaciousBrowserBoot ? 17 : 20;

            var introCardSize = densePhoneBoot
                ? new Vector2(1460f, 620f)
                : wideCompact
                    ? new Vector2(1460f, 730f)
                    : new Vector2(1200f, 640f);
            var rootRect = root as RectTransform;
            var logicalWidth = rootRect != null && rootRect.rect.width > 1f
                ? rootRect.rect.width
                : Mathf.Max(1, screenWidth) / canvasScale;
            var spaciousCardWidth = Mathf.Max(
                logicalWidth * 0.50f,
                720f / canvasScale);
            var desktopCardScale = spaciousBrowserBoot
                ? Mathf.Min(1f, spaciousCardWidth / introCardSize.x)
                : 1f;
            introCard.localScale = new Vector3(desktopCardScale, desktopCardScale, 1f);
            var visualCardHeight = introCardSize.y * desktopCardScale;
            var logicalHeight = Mathf.Max(
                visualCardHeight,
                rootRect != null && rootRect.rect.height > 1f
                    ? rootRect.rect.height
                    : Mathf.Max(1, screenHeight) / canvasScale);
            var introCardTop = (logicalHeight - visualCardHeight) * 0.5f;
            if (!wideCompact || densePhoneBoot)
            {
                var physicalScale = rootRect != null && rootRect.rect.height > 1f
                    ? Mathf.Max(1, screenHeight) / logicalHeight
                    : canvasScale;
                var maximumCardTop = logicalHeight
                    - visualCardHeight
                    - startButtonBottom
                    - startButtonSize.y
                    - minimumPhysicalStartGap / Mathf.Max(0.0001f, physicalScale);
                introCardTop = Mathf.Min(introCardTop, Mathf.Max(0f, maximumCardTop));
            }
            ConfigureBootTopCenteredRect(
                introCard,
                new Vector2(0f, -introCardTop),
                introCardSize);

            const float headerGap = 8f;
            var headerGroupHeight = titleSize.y + headerGap + subtitleSize.y;
            var headerTop = Mathf.Max(0f, (introCardTop - headerGroupHeight) * 0.5f);
            ConfigureBootTopCenteredRect(
                root.Find("Title Text") as RectTransform,
                new Vector2(0f, -headerTop),
                titleSize);
            ConfigureBootTopCenteredRect(
                root.Find("Subtitle Text") as RectTransform,
                new Vector2(0f, -(headerTop + titleSize.y + headerGap)),
                subtitleSize);

            ConfigureBootTopLeftRect(
                introCard.Find("Boot Intro Headline Text") as RectTransform,
                densePhoneBoot
                    ? new Vector2(50f, -18f)
                    : wideCompact ? new Vector2(50f, -24f) : new Vector2(50f, -34f),
                densePhoneBoot
                    ? new Vector2(1360f, 52f)
                    : wideCompact ? new Vector2(1360f, 72f) : new Vector2(1100f, 52f));
            ConfigureBootTopLeftRect(
                introCard.Find("Boot Intro Description Text") as RectTransform,
                densePhoneBoot
                    ? new Vector2(60f, -76f)
                    : wideCompact ? new Vector2(60f, -92f) : new Vector2(70f, -92f),
                densePhoneBoot
                    ? new Vector2(1340f, 90f)
                    : wideCompact ? new Vector2(1340f, 124f) : new Vector2(1060f, 88f));

            ConfigureBootFeatureCard(
                introCard,
                "Boot Care Feature Card",
                densePhoneBoot
                    ? new Vector2(40f, -170f)
                    : wideCompact ? new Vector2(40f, -230f) : new Vector2(50f, -210f),
                wideCompact,
                densePhoneBoot);
            ConfigureBootFeatureCard(
                introCard,
                "Boot Decorate Feature Card",
                densePhoneBoot
                    ? new Vector2(510f, -170f)
                    : wideCompact ? new Vector2(510f, -230f) : new Vector2(438f, -210f),
                wideCompact,
                densePhoneBoot);
            ConfigureBootFeatureCard(
                introCard,
                "Boot Story Feature Card",
                densePhoneBoot
                    ? new Vector2(980f, -170f)
                    : wideCompact ? new Vector2(980f, -230f) : new Vector2(826f, -210f),
                wideCompact,
                densePhoneBoot);

            var sessionPanel = introCard.Find("Boot Session Note Panel") as RectTransform;
            ConfigureBootTopLeftRect(
                sessionPanel,
                densePhoneBoot
                    ? new Vector2(40f, -400f)
                    : wideCompact ? new Vector2(40f, -506f) : new Vector2(50f, -430f),
                densePhoneBoot
                    ? new Vector2(1380f, 166f)
                    : wideCompact ? new Vector2(1380f, 198f) : new Vector2(1100f, 134f));
            ConfigureBootTopLeftRect(
                sessionPanel?.Find("Boot Session Title Text") as RectTransform,
                densePhoneBoot
                    ? new Vector2(30f, -8f)
                    : wideCompact ? new Vector2(30f, -10f) : new Vector2(28f, -18f),
                densePhoneBoot
                    ? new Vector2(1320f, 48f)
                    : wideCompact ? new Vector2(1320f, 60f) : new Vector2(1044f, 34f));
            ConfigureBootTopLeftRect(
                sessionPanel?.Find("Boot Session Body Text") as RectTransform,
                densePhoneBoot
                    ? new Vector2(30f, -56f)
                    : wideCompact ? new Vector2(30f, -70f) : new Vector2(28f, -56f),
                densePhoneBoot
                    ? new Vector2(1320f, 98f)
                    : wideCompact ? new Vector2(1320f, 116f) : new Vector2(1044f, 66f));

            var startButton = root.Find("Start Button") as RectTransform;
            if (startButton != null)
            {
                startButton.anchorMin = new Vector2(0.5f, 0f);
                startButton.anchorMax = new Vector2(0.5f, 0f);
                startButton.pivot = new Vector2(0.5f, 0f);
                startButton.anchoredPosition = new Vector2(0f, startButtonBottom);
                startButton.sizeDelta = startButtonSize;
                var startLabelRect = startButton.Find("Label") as RectTransform;
                if (startLabelRect != null)
                {
                    startLabelRect.anchorMin = Vector2.zero;
                    startLabelRect.anchorMax = Vector2.one;
                    startLabelRect.pivot = new Vector2(0.5f, 0.5f);
                    startLabelRect.anchoredPosition = Vector2.zero;
                    startLabelRect.sizeDelta = Vector2.zero;
                }
            }

            var accountButton = root.Find("Boot Account Entry Button") as RectTransform;
            if (accountButton != null)
            {
                accountButton.gameObject.SetActive(false);
            }

            ApplyBootResponsiveText(
                root.Find("Title Text")?.GetComponent<Text>(),
                titleFontSize,
                titleMinimumFontSize,
                titleFontSize,
                compactFontFloor);
            ApplyBootResponsiveText(
                root.Find("Subtitle Text")?.GetComponent<Text>(),
                subtitleFontSize,
                subtitleMinimumFontSize,
                subtitleFontSize,
                compactFontFloor);
            ApplyBootResponsiveText(introCard.Find("Boot Intro Headline Text")?.GetComponent<Text>(), 34, 28, 34, compactFontFloor);
            ApplyBootResponsiveText(introCard.Find("Boot Intro Description Text")?.GetComponent<Text>(), 24, 20, 24, compactFontFloor);
            ApplyBootResponsiveText(sessionPanel?.Find("Boot Session Title Text")?.GetComponent<Text>(), 22, 18, 22, compactFontFloor);
            ApplyBootResponsiveText(sessionPanel?.Find("Boot Session Body Text")?.GetComponent<Text>(), 19, 16, 19, compactFontFloor);
            ApplyBootResponsiveText(
                startButton?.Find("Label")?.GetComponent<Text>(),
                startFontSize,
                startMinimumFontSize,
                startFontSize,
                compactFontFloor);
            ApplyBootResponsiveText(accountButton?.Find("Label")?.GetComponent<Text>(), 24, 18, 24, compactFontFloor);

            foreach (var featureName in new[]
                     {
                         "Boot Care Feature Card",
                         "Boot Decorate Feature Card",
                         "Boot Story Feature Card"
                     })
            {
                var feature = introCard.Find(featureName);
                ApplyBootResponsiveText(feature?.Find("Step Badge/Step Text")?.GetComponent<Text>(), 22, 18, 22, compactFontFloor);
                ApplyBootResponsiveText(feature?.Find("Title Text")?.GetComponent<Text>(), 26, 21, 26, compactFontFloor);
                ApplyBootResponsiveText(feature?.Find("Body Text")?.GetComponent<Text>(), 20, 16, 20, compactFontFloor);
            }
        }

        private static void ConfigureBootFeatureCard(
            RectTransform introCard,
            string name,
            Vector2 position,
            bool wideCompact,
            bool densePhoneBoot)
        {
            var feature = introCard != null ? introCard.Find(name) as RectTransform : null;
            ConfigureBootTopLeftRect(
                feature,
                position,
                densePhoneBoot
                    ? new Vector2(440f, 216f)
                    : wideCompact ? new Vector2(440f, 250f) : new Vector2(324f, 180f));
            ConfigureBootTopLeftRect(
                feature?.Find("Step Badge") as RectTransform,
                densePhoneBoot
                    ? new Vector2(16f, -14f)
                    : new Vector2(18f, -16f),
                densePhoneBoot
                    ? new Vector2(54f, 54f)
                    : wideCompact ? new Vector2(60f, 60f) : new Vector2(44f, 44f));
            ConfigureBootTopLeftRect(
                feature?.Find("Step Badge/Step Text") as RectTransform,
                Vector2.zero,
                densePhoneBoot
                    ? new Vector2(54f, 54f)
                    : wideCompact ? new Vector2(60f, 60f) : new Vector2(44f, 44f));
            ConfigureBootTopLeftRect(
                feature?.Find("Title Text") as RectTransform,
                densePhoneBoot
                    ? new Vector2(80f, -14f)
                    : wideCompact ? new Vector2(88f, -18f) : new Vector2(72f, -18f),
                densePhoneBoot
                    ? new Vector2(336f, 58f)
                    : wideCompact ? new Vector2(328f, 64f) : new Vector2(230f, 40f));
            ConfigureBootTopLeftRect(
                feature?.Find("Body Text") as RectTransform,
                densePhoneBoot
                    ? new Vector2(24f, -72f)
                    : wideCompact ? new Vector2(24f, -92f) : new Vector2(22f, -60f),
                densePhoneBoot
                    ? new Vector2(392f, 140f)
                    : wideCompact ? new Vector2(392f, 148f) : new Vector2(280f, 106f));
        }

        private static void ApplyBootResponsiveText(
            Text label,
            int authoredFontSize,
            int authoredMinimum,
            int authoredMaximum,
            int compactFontFloor)
        {
            if (label == null)
            {
                return;
            }

            var textScale = CheeseTama.Save.GameSettingsSaveData.NormalizeTextScale(
                AccessibilityRuntime.TextScale);
            label.fontSize = Mathf.Max(
                compactFontFloor,
                Mathf.RoundToInt(authoredFontSize * textScale));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(
                compactFontFloor,
                Mathf.RoundToInt(authoredMinimum * textScale));
            label.resizeTextMaxSize = Mathf.Max(
                label.resizeTextMinSize,
                Mathf.RoundToInt(authoredMaximum * textScale));
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ConfigureBootTopCenteredRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureBootTopLeftRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void ApplyMilkroomUtilityResponsiveLayout(
            int screenWidth,
            int screenHeight,
            float canvasScaleFactor)
        {
            if (targetCanvas == null)
            {
                return;
            }

            var root = targetCanvas.transform;
            var utilityBar = root.Find("Milkroom Utility Bar") as RectTransform;
            if (utilityBar == null)
            {
                return;
            }

            var collapseController = utilityBar.GetComponent<MilkroomUtilityBarController>();
            var collapsed = collapseController != null && collapseController.IsCollapsed;
            var collapseToggle = utilityBar.Find(MilkroomUtilityBarController.ToggleObjectName)
                as RectTransform;
            activeMilkroomUtilityButtons.Clear();
            for (var index = 0; index < MilkroomUtilityButtonNames.Length; index += 1)
            {
                var button = utilityBar.Find(MilkroomUtilityButtonNames[index]) as RectTransform;
                if (button != null && button.gameObject.activeSelf)
                {
                    activeMilkroomUtilityButtons.Add(button);
                }
            }

            var activeButtonCount = activeMilkroomUtilityButtons.Count;
            var displayedButtonCount = collapsed ? 0 : activeButtonCount;

            var aspect = Mathf.Max(1, screenWidth) / (float)Mathf.Max(1, screenHeight);
            var wideCompact = CurrentProfile.Kind == ResponsiveUiProfileKind.CompactLandscape
                && aspect >= 1.9f;
            var densePhoneUtility = wideCompact
                && screenWidth < 1024
                && screenHeight <= 480;
            var canvasScale = Mathf.Max(0.0001f, canvasScaleFactor);
            var rootRect = root as RectTransform;
            var logicalWidth = rootRect != null && rootRect.rect.width > 1f
                ? rootRect.rect.width
                : Mathf.Max(1, screenWidth) / canvasScale;
            var logicalHeight = rootRect != null && rootRect.rect.height > 1f
                ? rootRect.rect.height
                : Mathf.Max(1, screenHeight) / canvasScale;
            var careTip = root.Find("Care Tip Panel") as RectTransform;
            var careTipVisible = careTip == null || careTip.gameObject.activeSelf;
            var careTipTitleText = careTip?.Find("Care Tip Title Text")?.GetComponent<Text>();
            var careTipBodyText = careTip?.Find("Care Tip Text")?.GetComponent<Text>();
            if (careTipTitleText != null)
            {
                careTipTitleText.alignment = TextAnchor.MiddleLeft;
            }

            if (careTipBodyText != null)
            {
                careTipBodyText.alignment = TextAnchor.MiddleLeft;
                careTipBodyText.lineSpacing = 1f;
            }

            var message = root.Find("Message Bar") as RectTransform;
            var eventMessage = root.Find("Event Message Bar") as RectTransform;
            var bottomAction = root.Find("Bottom Action Bar") as RectTransform;
            var bottomActionTop = bottomAction != null
                ? logicalHeight - (bottomAction.anchoredPosition.y + bottomAction.rect.height)
                : logicalHeight - 132f;
            // A 110% menu scale reduces the logical canvas height. In that case the authored
            // two-row utility block would cover the bottom care actions even on a wide screen.
            var shortWide = !wideCompact && bottomAction != null && bottomActionTop < 942f;

            if (wideCompact || shortWide)
            {
                var buttonWidth = wideCompact
                    ? Mathf.Ceil((densePhoneUtility ? 38f : 44f) / canvasScale)
                    : Mathf.Max(56f, Mathf.Ceil(42f / canvasScale));
                var buttonHeight = wideCompact
                    ? Mathf.Ceil((densePhoneUtility ? 26f : 30f) / canvasScale)
                    : Mathf.Max(38f, Mathf.Ceil(32f / canvasScale));
                var toggleWidth = wideCompact
                    ? Mathf.Ceil((densePhoneUtility ? 24f : 32f) / canvasScale)
                    : Mathf.Max(38f, Mathf.Ceil(32f / canvasScale));
                var toggleHeight = densePhoneUtility
                    ? Mathf.Ceil(24f / canvasScale)
                    : buttonHeight;
                var horizontalGap = wideCompact
                    ? Mathf.Ceil((densePhoneUtility ? 6f : 4f) / canvasScale)
                    : 6f;
                var toggleGap = wideCompact
                    ? Mathf.Ceil((densePhoneUtility ? 13f : 10f) / canvasScale)
                    : horizontalGap;
                var padding = wideCompact
                    ? Mathf.Ceil((densePhoneUtility ? 2f : 3f) / canvasScale)
                    : 6f;
                var utilityWidth = padding * 2f
                    + buttonWidth * displayedButtonCount
                    + toggleWidth
                    + horizontalGap * Mathf.Max(0, displayedButtonCount - 1)
                    + (displayedButtonCount > 0 ? toggleGap : 0f);
                var visibleControlHeight = displayedButtonCount > 0
                    ? Mathf.Max(buttonHeight, toggleHeight)
                    : toggleHeight;
                var utilityHeight = visibleControlHeight
                    + padding * 2f
                    + (wideCompact && !densePhoneUtility ? 8f : 10f);
                var leftPanelGap = wideCompact
                    ? Mathf.Ceil(8f / canvasScale)
                    : MilkroomLeftPanelGap;
                // Leave enough room for adaptive hit areas around the bottom action buttons.
                var utilityTop = Mathf.Max(
                    MilkroomUtilityDefaultTop
                        + Mathf.Max(0f, leftPanelGap - MilkroomLeftPanelGap),
                    bottomActionTop - utilityHeight - 24f);
                var careWidth = wideCompact ? 420f : 320f;
                var careTextInset = wideCompact ? 18f : 22f;
                var careTextWidth = careWidth - careTextInset * 2f;
                var careHeight = Mathf.Clamp(
                    utilityTop - MilkroomCareTipTop - leftPanelGap,
                    wideCompact ? 154f : 148f,
                    wideCompact ? 179f : 168f);
                utilityTop = MilkroomCareTipTop + careHeight + leftPanelGap;

                ConfigureBootTopLeftRect(
                    careTip,
                    new Vector2(24f, -MilkroomCareTipTop),
                    new Vector2(careWidth, careHeight));
                ConfigureBootTopLeftRect(
                    careTip?.Find("Care Tip Title Text") as RectTransform,
                    new Vector2(careTextInset, wideCompact ? -14f : -18f),
                    new Vector2(careTextWidth, wideCompact ? 34f : 38f));
                ConfigureBootTopLeftRect(
                    careTip?.Find("Care Tip Text") as RectTransform,
                    new Vector2(careTextInset, wideCompact ? -50f : -58f),
                    new Vector2(
                        careTextWidth,
                        Mathf.Max(wideCompact ? 98f : 88f, careHeight - (wideCompact ? 50f : 62f))));
                ConfigureBootTopLeftRect(
                    utilityBar,
                    new Vector2(24f, -(careTipVisible ? utilityTop : MilkroomCareTipTop)),
                    new Vector2(utilityWidth, utilityHeight));

                for (var index = 0; index < displayedButtonCount; index += 1)
                {
                    var button = activeMilkroomUtilityButtons[index];
                    ConfigureBootTopLeftRect(
                        button,
                        new Vector2(
                            padding + index * (buttonWidth + horizontalGap),
                            -(padding + 5f)),
                        new Vector2(buttonWidth, buttonHeight));
                }

                ConfigureBootTopLeftRect(
                    collapseToggle,
                    new Vector2(
                        padding
                            + displayedButtonCount * buttonWidth
                            + horizontalGap * Mathf.Max(0, displayedButtonCount - 1)
                            + (displayedButtonCount > 0 ? toggleGap : 0f),
                        -(padding + 5f + (visibleControlHeight - toggleHeight) * 0.5f)),
                    new Vector2(toggleWidth, toggleHeight));
                ApplyMilkroomUtilityTexts(
                    activeMilkroomUtilityButtons,
                    activeButtonCount,
                    collapseToggle,
                    canvasScale,
                    true,
                    wideCompact ? 11f : 13f);

                var messageGap = wideCompact ? Mathf.Ceil(12f / canvasScale) : 10f;
                var messageWidth = wideCompact
                    ? Mathf.Clamp(logicalWidth - 48f, 360f, 620f)
                    : Mathf.Clamp(logicalWidth - 48f, 360f, 696f);
                var messageTop = logicalHeight - 290f;
                var centeredMessageTop = messageTop;
                var centeredMessageLeft = (logicalWidth - messageWidth) * 0.5f;
                if (24f + utilityWidth > centeredMessageLeft)
                {
                    centeredMessageTop = Mathf.Max(0f, utilityTop - messageGap - 144f);
                }

                ConfigureBottomCenteredRect(
                    message,
                    new Vector2(0f, logicalHeight - centeredMessageTop - 144f),
                    new Vector2(messageWidth, 144f));
                ConfigureBottomCenteredRect(
                    eventMessage,
                    new Vector2(0f, logicalHeight - centeredMessageTop - 144f),
                    new Vector2(messageWidth, 144f));
                ConfigureBootTopLeftRect(
                    message?.Find("Message Text") as RectTransform,
                    new Vector2(24f, -12f),
                    new Vector2(Mathf.Max(312f, messageWidth - 48f), 120f));
                ConfigureBootTopLeftRect(
                    eventMessage?.Find("Event Message Text") as RectTransform,
                    new Vector2(24f, -12f),
                    new Vector2(Mathf.Max(312f, messageWidth - 48f), 120f));
                var compactTitleFloor = Mathf.CeilToInt((wideCompact ? 12f : 13f) / canvasScale);
                var compactBodyFloor = Mathf.CeilToInt((wideCompact ? 10.5f : 13f) / canvasScale);
                ApplyBootResponsiveText(
                    careTipTitleText,
                    wideCompact ? 22 : 18,
                    wideCompact ? 18 : 15,
                    wideCompact ? 22 : 18,
                    compactTitleFloor);
                ApplyBootResponsiveText(
                    careTipBodyText,
                    wideCompact ? 20 : 16,
                    wideCompact ? 16 : 14,
                    wideCompact ? 20 : 16,
                    compactBodyFloor);
                if (wideCompact && careTipBodyText != null)
                {
                    careTipBodyText.lineSpacing = 0.86f;
                }
                return;
            }

            ConfigureBootTopLeftRect(
                careTip,
                new Vector2(24f, -MilkroomCareTipTop),
                new Vector2(350f, 196f));
            ConfigureBootTopLeftRect(
                careTip?.Find("Care Tip Title Text") as RectTransform,
                new Vector2(22f, -18f),
                new Vector2(306f, 34f));
            ConfigureBootTopLeftRect(
                careTip?.Find("Care Tip Text") as RectTransform,
                new Vector2(22f, -58f),
                new Vector2(306f, 130f));
            const float defaultButtonWidth = 88f;
            const float defaultButtonHeight = 42f;
            const float defaultHorizontalGap = 8f;
            const float defaultPadding = 8f;
            const float defaultToggleWidth = 42f;
            var defaultUtilityWidth = defaultPadding * 2f
                + defaultButtonWidth * displayedButtonCount
                + defaultToggleWidth
                + defaultHorizontalGap * displayedButtonCount;
            ConfigureBootTopLeftRect(
                utilityBar,
                new Vector2(
                    24f,
                    -(careTipVisible ? MilkroomUtilityDefaultTop : MilkroomCareTipTop)),
                new Vector2(defaultUtilityWidth, 58f));
            for (var index = 0; index < displayedButtonCount; index += 1)
            {
                var button = activeMilkroomUtilityButtons[index];
                ConfigureBootTopLeftRect(
                    button,
                    new Vector2(defaultPadding + index * (defaultButtonWidth + defaultHorizontalGap), -defaultPadding),
                    new Vector2(defaultButtonWidth, defaultButtonHeight));
            }
            ConfigureBootTopLeftRect(
                collapseToggle,
                new Vector2(
                    defaultPadding + displayedButtonCount * (defaultButtonWidth + defaultHorizontalGap),
                    -defaultPadding),
                new Vector2(defaultToggleWidth, defaultButtonHeight));
            ApplyMilkroomUtilityTexts(
                activeMilkroomUtilityButtons,
                activeButtonCount,
                collapseToggle,
                canvasScale,
                false,
                1f);

            ConfigureBottomCenteredRect(message, new Vector2(0f, 146f), new Vector2(696f, 144f));
            ConfigureBottomCenteredRect(eventMessage, new Vector2(0f, 146f), new Vector2(696f, 144f));
            ConfigureBootTopLeftRect(
                message?.Find("Message Text") as RectTransform,
                new Vector2(24f, -12f),
                new Vector2(648f, 120f));
            ConfigureBootTopLeftRect(
                eventMessage?.Find("Event Message Text") as RectTransform,
                new Vector2(24f, -12f),
                new Vector2(648f, 120f));
            ApplyBootResponsiveText(
                careTipTitleText,
                22,
                18,
                22,
                1);
            ApplyBootResponsiveText(
                careTipBodyText,
                20,
                16,
                20,
                1);
        }

        private static void ApplyMilkroomUtilityTexts(
            IReadOnlyList<RectTransform> buttons,
            int displayedButtonCount,
            RectTransform collapseToggle,
            float canvasScale,
            bool compact,
            float compactPhysicalFontFloor)
        {
            var labels = new List<Text>(Mathf.Max(1, displayedButtonCount + 1));
            for (var index = 0; index < displayedButtonCount && index < buttons.Count; index += 1)
            {
                var label = buttons[index]?.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    labels.Add(label);
                }
            }

            var toggleLabel = collapseToggle?.Find("Label")?.GetComponent<Text>();
            if (toggleLabel != null)
            {
                labels.Add(toggleLabel);
            }

            if (labels.Count == 0)
            {
                return;
            }

            var baseFontSize = compact ? 14f : 16f;
            var textScale = CheeseTama.Save.GameSettingsSaveData.NormalizeTextScale(
                AccessibilityRuntime.TextScale);
            var authoredFloor = Mathf.Max(1, Mathf.RoundToInt(11f * textScale));
            var physicalFloor = compact
                ? Mathf.Max(
                    authoredFloor,
                    Mathf.CeilToInt(compactPhysicalFontFloor / canvasScale))
                : authoredFloor;
            var sharedFontSize = Mathf.Max(
                physicalFloor,
                Mathf.RoundToInt(baseFontSize * textScale));

            for (var index = 0; index < labels.Count; index += 1)
            {
                ConfigureMilkroomUtilityText(labels[index], compact, sharedFontSize, physicalFloor);
            }

            while (sharedFontSize > physicalFloor)
            {
                var allLabelsFit = true;
                for (var index = 0; index < labels.Count; index += 1)
                {
                    if (labels[index].preferredHeight
                        > labels[index].rectTransform.rect.height + 0.5f)
                    {
                        allLabelsFit = false;
                        break;
                    }
                }

                if (allLabelsFit)
                {
                    break;
                }

                sharedFontSize -= 1;
                for (var index = 0; index < labels.Count; index += 1)
                {
                    labels[index].fontSize = sharedFontSize;
                }
            }

            for (var index = 0; index < labels.Count; index += 1)
            {
                labels[index].fontSize = sharedFontSize;
                labels[index].resizeTextForBestFit = false;
                labels[index].resizeTextMinSize = sharedFontSize;
                labels[index].resizeTextMaxSize = sharedFontSize;
            }
        }

        private static void ConfigureMilkroomUtilityText(
            Text label,
            bool compact,
            int fontSize,
            int physicalFloor)
        {
            if (label == null)
            {
                return;
            }

            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            var verticalInset = compact ? 3f : 4f;
            labelRect.offsetMin = new Vector2(4f, verticalInset);
            labelRect.offsetMax = new Vector2(-4f, -verticalInset);

            var ownerRect = label.transform.parent as RectTransform;
            var squareButton = compact
                && ownerRect != null
                && ownerRect.rect.width <= ownerRect.rect.height * 1.55f;
            if (ownerRect != null)
            {
                if (ownerRect.name == "Open Delivery Button")
                {
                    var claimed = label.text != null
                        && label.text.Contains("받음");
                    label.text = CheeseStarDeliveryBridge.ResolveEntryLabel(
                        claimed,
                        squareButton);
                }
                else if (ownerRect.name == "Open Milkroom Mystery Button")
                {
                    label.text = squareButton ? "비밀\n서랍" : "비밀서랍";
                }
                else if (ownerRect.name == "Open Dream Story Season Button")
                {
                    label.text = squareButton ? "꿈\n이야기" : "꿈이야기";
                }
                else if (ownerRect.name == "Open First Day Journey Button")
                {
                    label.text = squareButton ? "첫날\n선물" : "첫날선물";
                }
                else if (ownerRect.name == "Open Journey Hub Button")
                {
                    label.text = "여정";
                }
            }

            label.fontSize = Mathf.Max(physicalFloor, fontSize);
            label.resizeTextForBestFit = false;
            label.resizeTextMinSize = label.fontSize;
            label.resizeTextMaxSize = label.fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.lineSpacing = 0.9f;
        }

        private void ApplyCookingChoiceResponsiveLayout(float canvasScaleFactor)
        {
            if (targetCanvas == null)
            {
                return;
            }

            var overlay = targetCanvas.transform.Find(CookingChoicePanelController.OverlayObjectName);
            var card = overlay?.Find("Cooking Choice Card") as RectTransform;
            if (card == null)
            {
                return;
            }

            var title = card.Find("Cooking Choice Title Text") as RectTransform;
            var help = card.Find("Cooking Choice Help Text") as RectTransform;
            var close = card.Find("Cooking Choice Close Button") as RectTransform;
            var cooking = card.Find("Cooking Choice Cooking Button") as RectTransform;
            var blending = card.Find("Cooking Choice Milk Blending Button") as RectTransform;
            var canvasScale = Mathf.Max(0.0001f, canvasScaleFactor);
            var compact = CurrentProfile.Kind == ResponsiveUiProfileKind.CompactLandscape
                && canvasScale < 0.75f;

            ConfigureMiddleCenteredRect(
                card,
                compact ? new Vector2(760f, 620f) : new Vector2(680f, 500f));
            ConfigureBootTopCenteredRect(
                title,
                compact ? new Vector2(0f, -30f) : new Vector2(0f, -34f),
                compact ? new Vector2(560f, 64f) : new Vector2(456f, 48f));
            ConfigureBootTopCenteredRect(
                help,
                compact ? new Vector2(0f, -108f) : new Vector2(0f, -92f),
                new Vector2(520f, compact ? 48f : 38f));
            ConfigureBootTopCenteredRect(
                close,
                compact ? new Vector2(0f, -480f) : new Vector2(0f, -430f),
                new Vector2(176f, 50f));
            ConfigureBootTopLeftRect(
                cooking,
                compact ? new Vector2(40f, -168f) : new Vector2(64f, -150f),
                compact ? new Vector2(680f, 112f) : new Vector2(552f, 96f));
            ConfigureBootTopLeftRect(
                blending,
                compact ? new Vector2(40f, -310f) : new Vector2(64f, -266f),
                compact ? new Vector2(680f, 112f) : new Vector2(552f, 96f));

            var textScale = CheeseTama.Save.GameSettingsSaveData.NormalizeTextScale(
                AccessibilityRuntime.TextScale);
            var compactFloor = compact ? Mathf.CeilToInt(13f / canvasScale) : 1;
            ApplyCookingChoiceText(
                title?.GetComponent<Text>(),
                30,
                textScale,
                compactFloor);
            ApplyCookingChoiceText(
                help?.GetComponent<Text>(),
                17,
                textScale,
                compactFloor);
            var closeLabel = close?.Find("Label")?.GetComponent<Text>();
            ApplyCookingChoiceText(
                closeLabel,
                16,
                textScale,
                compactFloor);
            if (closeLabel != null)
            {
                closeLabel.rectTransform.offsetMin = new Vector2(4f, 2f);
                closeLabel.rectTransform.offsetMax = new Vector2(-4f, -2f);
            }
            ApplyCookingChoiceText(
                cooking?.Find("Label")?.GetComponent<Text>(),
                21,
                textScale,
                compactFloor);

            var blendingLabel = blending?.Find("Label")?.GetComponent<Text>();
            if (blendingLabel != null)
            {
                var mainSize = Mathf.Max(
                    compactFloor,
                    Mathf.RoundToInt(21f * textScale));
                var detailSize = Mathf.Max(
                    compactFloor,
                    Mathf.RoundToInt(14f * textScale));
                blendingLabel.text = $"<size={mainSize}>우유 섞기</size>\n"
                    + $"<size={detailSize}>(가끔 특별한 음식이 나와요)</size>";
                blendingLabel.fontSize = mainSize;
                blendingLabel.resizeTextForBestFit = false;
                blendingLabel.alignment = TextAnchor.MiddleCenter;
                blendingLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                blendingLabel.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private static void ApplyCookingChoiceText(
            Text label,
            int authoredFontSize,
            float textScale,
            int minimumFontSize)
        {
            if (label == null)
            {
                return;
            }

            label.fontSize = Mathf.Max(
                minimumFontSize,
                Mathf.RoundToInt(authoredFontSize * textScale));
            label.resizeTextForBestFit = false;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ConfigureMiddleCenteredRect(RectTransform rect, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private void ApplySettingsScrollResponsiveLayout(
            int screenWidth,
            int screenHeight,
            float canvasScaleFactor)
        {
            if (targetCanvas == null)
            {
                return;
            }

            var content = targetCanvas.transform.Find(
                    "Settings Modal/Settings Scroll View/Viewport/Content")
                as RectTransform;
            if (content == null)
            {
                return;
            }

            // Older authored scenes can still contain the retired low-quality choice. Keep
            // those scenes readable until they are rebuilt, while saved Low values migrate to
            // Balanced through GraphicsQualityCatalog.
            var legacyLowQuality = content.Find("Graphics Quality Low Button");
            if (legacyLowQuality != null)
            {
                legacyLowQuality.gameObject.SetActive(false);
            }

            EnsureSettingsLayoutSnapshots(content);
            var useCompactSettingsLayout = CurrentProfile.Kind == ResponsiveUiProfileKind.CompactLandscape
                || (isBrowser
                    && touchPreferred
                    && CurrentProfile.Kind == ResponsiveUiProfileKind.WideLandscape);
            if (!useCompactSettingsLayout)
            {
                RestoreSettingsLayout();
                ApplyAuthoredSettingsImportSpacing(content);
                return;
            }

            var canvasScale = Mathf.Max(0.0001f, canvasScaleFactor);
            var aspect = Mathf.Max(1, screenWidth) / (float)Mathf.Max(1, screenHeight);
            var wideCompact = aspect >= 1.9f;
            // The CanvasScaler already contains the user's 90/100/110% preference. Remove
            // only the screen-fit component here so compact settings controls still grow or
            // shrink in the same ratio as the rest of the interface.
            var configuredUiScale = ResolveConfiguredUiScale();
            var screenFitScale = Mathf.Max(0.0001f, canvasScale / configuredUiScale);
            var viewport = content.parent as RectTransform;
            var contentWidth = viewport != null ? viewport.rect.width : 0f;
            if (contentWidth <= 1f && viewport != null)
            {
                contentWidth = viewport.sizeDelta.x;
            }

            if (contentWidth <= 1f)
            {
                contentWidth = 502f;
            }

            float ToLogicalPixels(float physicalPixels)
            {
                return Mathf.Ceil(physicalPixels / screenFitScale);
            }

            // Keep very short landscape visuals dense. AdaptiveTouchHitArea expands each
            // selectable to the 48px slot below without forcing the painted control to fill it.
            var padding = ToLogicalPixels(wideCompact ? 6f : 8f);
            var itemGap = ToLogicalPixels(wideCompact ? 4f : 6f);
            var controlGap = ToLogicalPixels(2f);
            var smallGap = ToLogicalPixels(wideCompact ? 3f : 4f);
            var sectionGap = ToLogicalPixels(wideCompact ? 12f : 18f);
            var titleHeight = ToLogicalPixels(wideCompact ? 22f : 26f);
            var lineHeight = ToLogicalPixels(wideCompact ? 24f : 28f);
            var dataStatusHeight = ToLogicalPixels(50f);
            var settingsStatusHeight = ToLogicalPixels(wideCompact ? 32f : 40f);
            var buttonHeight = ToLogicalPixels(wideCompact ? 30f : 36f);
            var toggleHeight = ToLogicalPixels(wideCompact ? 24f : 28f);
            var sliderHeight = ToLogicalPixels(wideCompact ? 18f : 22f);
            var touchSlotHeight = ToLogicalPixels(48f);
            var innerWidth = Mathf.Max(1f, contentWidth - padding * 2f);
            var columnWidth = Mathf.Max(
                touchSlotHeight,
                (innerWidth - itemGap) * 0.5f);
            var secondColumnLeft = padding + columnWidth + itemGap;

            float CenterInTouchSlot(float slotTop, float visualHeight)
            {
                return slotTop + Mathf.Max(0f, (touchSlotHeight - visualHeight) * 0.5f);
            }

            void Place(string objectName, float left, float top, float width, float height)
            {
                ConfigureSettingsTopLeftRect(content, objectName, left, top, width, height);
            }

            void PlaceButton(string objectName, float left, float slotTop, float width)
            {
                Place(
                    objectName,
                    left,
                    CenterInTouchSlot(slotTop, buttonHeight),
                    width,
                    buttonHeight);
            }

            void StretchInside(RectTransform rect, float left, float right)
            {
                if (rect == null)
                {
                    return;
                }

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(left, 0f);
                rect.offsetMax = new Vector2(-right, 0f);
            }

            void StretchCentered(
                RectTransform rect,
                float left,
                float right,
                float height)
            {
                if (rect == null)
                {
                    return;
                }

                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(left, -height * 0.5f);
                rect.offsetMax = new Vector2(-right, height * 0.5f);
            }

            void FitToggleChildren(string objectName)
            {
                var toggleRect = content.Find(objectName) as RectTransform;
                var boxRect = toggleRect?.Find("Box") as RectTransform;
                if (toggleRect == null || boxRect == null)
                {
                    return;
                }

                var boxSize = ToLogicalPixels(wideCompact ? 18f : 20f);
                var checkSize = ToLogicalPixels(wideCompact ? 10f : 12f);
                ConfigureBootTopLeftRect(
                    boxRect,
                    new Vector2(0f, -(toggleHeight - boxSize) * 0.5f),
                    new Vector2(boxSize, boxSize));
                ConfigureBootTopLeftRect(
                    boxRect.Find("Checkmark") as RectTransform,
                    new Vector2(
                        (boxSize - checkSize) * 0.5f,
                        -(boxSize - checkSize) * 0.5f),
                    new Vector2(checkSize, checkSize));
                StretchInside(
                    toggleRect.Find("Label") as RectTransform,
                    boxSize + ToLogicalPixels(8f),
                    0f);
            }

            void FitSliderChildren(string objectName)
            {
                var sliderRect = content.Find(objectName) as RectTransform;
                if (sliderRect == null)
                {
                    return;
                }

                var trackHeight = ToLogicalPixels(wideCompact ? 4f : 6f);
                var handleWidth = ToLogicalPixels(wideCompact ? 14f : 18f);
                var handleHeight = ToLogicalPixels(wideCompact ? 16f : 20f);
                var handleInset = handleWidth * 0.5f;
                StretchCentered(
                    sliderRect.Find("Background") as RectTransform,
                    0f,
                    0f,
                    trackHeight);
                var fillArea = sliderRect.Find("Fill Area") as RectTransform;
                StretchCentered(fillArea, handleInset, handleInset, trackHeight);
                StretchCentered(
                    fillArea?.Find("Fill") as RectTransform,
                    0f,
                    0f,
                    trackHeight);
                var handleArea = sliderRect.Find("Handle Slide Area") as RectTransform;
                StretchCentered(handleArea, handleInset, handleInset, handleHeight);
                var handle = handleArea?.Find("Handle") as RectTransform;
                if (handle != null)
                {
                    handle.anchorMin = new Vector2(0.5f, 0.5f);
                    handle.anchorMax = new Vector2(0.5f, 0.5f);
                    handle.pivot = new Vector2(0.5f, 0.5f);
                    handle.anchoredPosition = Vector2.zero;
                    handle.sizeDelta = new Vector2(handleWidth, handleHeight);
                }
            }

            void FitInputChildren(string objectName)
            {
                var inputRect = content.Find(objectName) as RectTransform;
                if (inputRect == null)
                {
                    return;
                }

                var inset = ToLogicalPixels(wideCompact ? 8f : 10f);
                StretchInside(inputRect.Find("Text") as RectTransform, inset, inset);
                StretchInside(inputRect.Find("Placeholder") as RectTransform, inset, inset);
            }

            void PlaceToggle(string objectName, float left, float slotTop, float width)
            {
                Place(
                    objectName,
                    left,
                    CenterInTouchSlot(slotTop, toggleHeight),
                    width,
                    toggleHeight);
                FitToggleChildren(objectName);
            }

            void PlaceSlider(string objectName, float left, float slotTop, float width)
            {
                Place(
                    objectName,
                    left,
                    CenterInTouchSlot(slotTop, sliderHeight),
                    width,
                    sliderHeight);
                FitSliderChildren(objectName);
            }

            var sectionTop = 0f;
            var cursor = sectionTop + padding;
            Place("Settings Data Title Text", padding, cursor, innerWidth, titleHeight);
            cursor += titleHeight + itemGap;
            Place("Data Status Text", padding, cursor, innerWidth, dataStatusHeight);
            cursor += dataStatusHeight + itemGap;
            PlaceButton("Export Save Button", padding, cursor, columnWidth);
            PlaceButton("Choose Import File Button", secondColumnLeft, cursor, columnWidth);
            cursor += touchSlotHeight + controlGap;
            PlaceButton("Open Reset Button", padding, cursor, columnWidth);
            PlaceButton("Open Cloud Save Button", secondColumnLeft, cursor, columnWidth);
            cursor += touchSlotHeight + controlGap;
            Place("Settings Last Saved Text", padding, cursor, innerWidth, lineHeight);
            cursor += lineHeight;
            var hasImportConfirmationRow = IsSettingsImportConfirmationVisible(content);
            if (hasImportConfirmationRow)
            {
                cursor += smallGap;
                PlaceButton("Import Save Confirmation Input", padding, cursor, columnWidth);
                FitInputChildren("Import Save Confirmation Input");
                PlaceButton("Confirm Import Save Button", secondColumnLeft, cursor, columnWidth);
                cursor += touchSlotHeight + padding;
            }
            else
            {
                cursor += padding;
            }
            var dataSectionBottom = cursor;
            Place(
                "Settings Data Section Background",
                0f,
                sectionTop,
                contentWidth,
                dataSectionBottom - sectionTop);

            sectionTop = dataSectionBottom + sectionGap;
            cursor = sectionTop + padding;
            Place("Settings Sound Title Text", padding, cursor, innerWidth, titleHeight);
            cursor += titleHeight + itemGap;

            var volumeLabelWidth = ToLogicalPixels(48f);
            var volumeValueWidth = ToLogicalPixels(34f);
            var volumeSliderLeft = padding + volumeLabelWidth + smallGap;
            var volumeValueLeft = contentWidth - padding - volumeValueWidth;
            var volumeSliderWidth = Mathf.Max(
                touchSlotHeight,
                volumeValueLeft - smallGap - volumeSliderLeft);
            var volumeTextTop = CenterInTouchSlot(cursor, lineHeight);
            Place("Master Volume Label Text", padding, volumeTextTop, volumeLabelWidth, lineHeight);
            PlaceSlider(
                "Master Volume Slider",
                volumeSliderLeft,
                cursor,
                volumeSliderWidth);
            Place("Master Volume Value Text", volumeValueLeft, volumeTextTop, volumeValueWidth, lineHeight);
            cursor += touchSlotHeight + controlGap;

            volumeTextTop = CenterInTouchSlot(cursor, lineHeight);
            Place("Music Volume Label Text", padding, volumeTextTop, volumeLabelWidth, lineHeight);
            PlaceSlider(
                "Music Volume Slider",
                volumeSliderLeft,
                cursor,
                volumeSliderWidth);
            Place("Music Volume Value Text", volumeValueLeft, volumeTextTop, volumeValueWidth, lineHeight);
            cursor += touchSlotHeight + controlGap;

            volumeTextTop = CenterInTouchSlot(cursor, lineHeight);
            Place("Effect Volume Label Text", padding, volumeTextTop, volumeLabelWidth, lineHeight);
            PlaceSlider(
                "Effect Volume Slider",
                volumeSliderLeft,
                cursor,
                volumeSliderWidth);
            Place("Effect Volume Value Text", volumeValueLeft, volumeTextTop, volumeValueWidth, lineHeight);
            cursor += touchSlotHeight + controlGap;
            PlaceToggle("Mute Audio Toggle", padding, cursor, innerWidth);
            cursor += touchSlotHeight + padding;
            var soundSectionBottom = cursor;
            Place(
                "Settings Sound Section Background",
                0f,
                sectionTop,
                contentWidth,
                soundSectionBottom - sectionTop);

            sectionTop = soundSectionBottom + sectionGap;
            cursor = sectionTop + padding;
            Place("Settings Display Title Text", padding, cursor, innerWidth, titleHeight);
            cursor += titleHeight + itemGap;
            PlaceToggle("Fullscreen Toggle", padding, cursor, innerWidth);
            cursor += touchSlotHeight + controlGap;
            PlaceButton("Viewport Zoom Out Settings Button", padding, cursor, columnWidth);
            PlaceButton("Viewport Zoom In Settings Button", secondColumnLeft, cursor, columnWidth);
            cursor += touchSlotHeight + controlGap;
            PlaceButton("Viewport Fit Settings Button", padding, cursor, innerWidth);
            cursor += touchSlotHeight + controlGap;

            var threeColumnWidth = Mathf.Max(
                touchSlotHeight,
                (innerWidth - itemGap * 2f) / 3f);
            var thirdColumnLeft = padding + (threeColumnWidth + itemGap) * 2f;
            var middleColumnLeft = padding + threeColumnWidth + itemGap;
            var groupLabelWidth = innerWidth;

            Place("UI Scale Label Text", padding, cursor, groupLabelWidth, lineHeight);
            cursor += lineHeight + smallGap;
            PlaceButton("UI Scale 90 Button", padding, cursor, threeColumnWidth);
            PlaceButton("UI Scale 100 Button", middleColumnLeft, cursor, threeColumnWidth);
            PlaceButton("UI Scale 110 Button", thirdColumnLeft, cursor, threeColumnWidth);
            cursor += touchSlotHeight + controlGap;

            Place("Accessibility Text Scale Label", padding, cursor, groupLabelWidth, lineHeight);
            cursor += lineHeight + smallGap;
            PlaceButton("Accessibility Text 100 Button", padding, cursor, threeColumnWidth);
            PlaceButton("Accessibility Text 125 Button", middleColumnLeft, cursor, threeColumnWidth);
            PlaceButton("Accessibility Text 140 Button", thirdColumnLeft, cursor, threeColumnWidth);
            cursor += touchSlotHeight + controlGap;
            PlaceToggle("High Contrast Toggle", padding, cursor, innerWidth);
            cursor += touchSlotHeight + controlGap;
            PlaceToggle("Reduce Motion Toggle", padding, cursor, innerWidth);
            cursor += touchSlotHeight + controlGap;

            Place("Frame Rate Label Text", padding, cursor, groupLabelWidth, lineHeight);
            cursor += lineHeight + smallGap;
            PlaceButton("Frame Rate 30 Button", padding, cursor, threeColumnWidth);
            PlaceButton("Frame Rate 60 Button", middleColumnLeft, cursor, threeColumnWidth);
            PlaceButton("Frame Rate 120 Button", thirdColumnLeft, cursor, threeColumnWidth);
            cursor += touchSlotHeight + controlGap;

            Place("Graphics Quality Label Text", padding, cursor, groupLabelWidth, lineHeight);
            cursor += lineHeight + smallGap;
            PlaceButton("Graphics Quality Balanced Button", padding, cursor, columnWidth);
            PlaceButton("Graphics Quality High Button", secondColumnLeft, cursor, columnWidth);
            cursor += touchSlotHeight + padding;
            var displaySectionBottom = cursor;
            Place(
                "Settings Display Section Background",
                0f,
                sectionTop,
                contentWidth,
                displaySectionBottom - sectionTop);

            sectionTop = displaySectionBottom + sectionGap;
            cursor = sectionTop + padding;
            Place("Settings Controls Title Text", padding, cursor, innerWidth, titleHeight);
            cursor += titleHeight + itemGap;
            PlaceToggle("Care Tip Toggle", padding, cursor, innerWidth);
            cursor += touchSlotHeight + controlGap;
            Place("Settings Status Text", padding, cursor, innerWidth, settingsStatusHeight);
            cursor += settingsStatusHeight + itemGap;
            PlaceButton("Open Input Bindings Button", padding, cursor, innerWidth);
            cursor += touchSlotHeight + controlGap;
            PlaceButton("Reset Settings Button", padding, cursor, innerWidth);
            cursor += touchSlotHeight + controlGap;
            PlaceButton("Game Home Button", padding, cursor, innerWidth);
            cursor += touchSlotHeight + padding;
            var controlsSectionBottom = cursor;
            Place(
                "Settings Controls Section Background",
                0f,
                sectionTop,
                contentWidth,
                controlsSectionBottom - sectionTop);

            content.sizeDelta = new Vector2(content.sizeDelta.x, controlsSectionBottom);
            var minimumFontPixelsAtSmallestMenuScale = wideCompact ? 10f : 12f;
            var compactFontFloor = Mathf.CeilToInt(
                minimumFontPixelsAtSmallestMenuScale
                / CheeseTama.Save.GameSettingsSaveData.MinUiScale
                / screenFitScale);
            ApplySettingsTextLayout(compactFontFloor, compact: true);
        }

        private float ResolveConfiguredUiScale()
        {
            if (canvasScaler == null || canvasScaler.referenceResolution.x <= 0.0001f)
            {
                return 1f;
            }

            return Mathf.Clamp(
                1920f / canvasScaler.referenceResolution.x,
                CheeseTama.Save.GameSettingsSaveData.MinUiScale,
                CheeseTama.Save.GameSettingsSaveData.MaxUiScale);
        }

        private static bool IsSettingsImportConfirmationVisible(RectTransform content)
        {
            if (content == null)
            {
                return false;
            }

            var input = content.Find("Import Save Confirmation Input");
            var confirm = content.Find("Confirm Import Save Button");
            return input != null && input.gameObject.activeSelf
                || confirm != null && confirm.gameObject.activeSelf;
        }

        private void ApplyAuthoredSettingsImportSpacing(RectTransform content)
        {
            if (content == null || IsSettingsImportConfirmationVisible(content))
            {
                return;
            }

            const float collapsedImportRowHeight = 42f;
            var dataBackground = content.Find("Settings Data Section Background") as RectTransform;
            if (dataBackground != null)
            {
                dataBackground.sizeDelta = new Vector2(
                    dataBackground.sizeDelta.x,
                    Mathf.Max(1f, dataBackground.sizeDelta.y - collapsedImportRowHeight));
            }

            for (var index = 0; index < content.childCount; index += 1)
            {
                if (content.GetChild(index) is not RectTransform child
                    || child == dataBackground
                    || child.name == "Import Save Confirmation Input"
                    || child.name == "Confirm Import Save Button"
                    || child.anchoredPosition.y > -280f)
                {
                    continue;
                }

                child.anchoredPosition += Vector2.up * collapsedImportRowHeight;
            }

            content.sizeDelta = new Vector2(
                content.sizeDelta.x,
                Mathf.Max(1f, content.sizeDelta.y - collapsedImportRowHeight));
        }

        private void EnsureSettingsLayoutSnapshots(RectTransform content)
        {
            if (cachedSettingsContent != content)
            {
                cachedSettingsContent = content;
                cachedSettingsDirectChildCount = -1;
                settingsRectLayouts.Clear();
                settingsTextLayouts.Clear();
            }

            if (cachedSettingsDirectChildCount == content.childCount
                && settingsRectLayouts.ContainsKey(content))
            {
                return;
            }

            CaptureSettingsRectLayout(content);
            var nestedRects = content.GetComponentsInChildren<RectTransform>(true);
            for (var index = 0; index < nestedRects.Length; index += 1)
            {
                CaptureSettingsRectLayout(nestedRects[index]);
            }

            for (var index = 0; index < content.childCount; index += 1)
            {
                var child = content.GetChild(index);
                if (child is RectTransform childRect)
                {
                    CaptureSettingsRectLayout(childRect);
                }

                var labels = child.GetComponentsInChildren<Text>(true);
                for (var labelIndex = 0; labelIndex < labels.Length; labelIndex += 1)
                {
                    var label = labels[labelIndex];
                    if (label != null && !settingsTextLayouts.ContainsKey(label))
                    {
                        settingsTextLayouts[label] = new SettingsTextLayoutSnapshot(label);
                    }
                }
            }

            cachedSettingsDirectChildCount = content.childCount;
        }

        private void CaptureSettingsRectLayout(RectTransform target)
        {
            if (target != null && !settingsRectLayouts.ContainsKey(target))
            {
                settingsRectLayouts[target] = new SettingsRectLayoutSnapshot(target);
            }
        }

        private void RestoreSettingsLayout()
        {
            var scrollPosition = cachedSettingsContent != null
                ? cachedSettingsContent.anchoredPosition
                : Vector2.zero;
            foreach (var pair in settingsRectLayouts)
            {
                if (pair.Key != null)
                {
                    pair.Value.Restore(pair.Key);
                }
            }

            if (cachedSettingsContent != null)
            {
                cachedSettingsContent.anchoredPosition = scrollPosition;
            }

            ApplySettingsTextLayout(1, compact: false);
        }

        private void ApplySettingsTextLayout(int physicalFontFloor, bool compact)
        {
            var textScale = CheeseTama.Save.GameSettingsSaveData.NormalizeTextScale(
                AccessibilityRuntime.TextScale);
            var scaledPhysicalFontFloor = Mathf.CeilToInt(physicalFontFloor * textScale);
            foreach (var pair in settingsTextLayouts)
            {
                var label = pair.Key;
                if (label == null)
                {
                    continue;
                }

                if (!compact)
                {
                    pair.Value.Restore(label, textScale);
                    continue;
                }

                label.fontSize = Mathf.Max(
                    scaledPhysicalFontFloor,
                    Mathf.RoundToInt(pair.Value.BaseFontSize * textScale));
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Max(
                    scaledPhysicalFontFloor,
                    Mathf.RoundToInt(pair.Value.BaseBestFitMinSize * textScale));
                label.resizeTextMaxSize = Mathf.Max(
                    label.resizeTextMinSize,
                    Mathf.RoundToInt(pair.Value.BaseBestFitMaxSize * textScale));
            }
        }

        private static void ConfigureSettingsTopLeftRect(
            RectTransform content,
            string objectName,
            float left,
            float top,
            float width,
            float height)
        {
            var rect = content?.Find(objectName) as RectTransform;
            ConfigureBootTopLeftRect(
                rect,
                new Vector2(left, -top),
                new Vector2(width, height));
        }

        private readonly struct SettingsRectLayoutSnapshot
        {
            private readonly Vector2 anchorMin;
            private readonly Vector2 anchorMax;
            private readonly Vector2 pivot;
            private readonly Vector2 anchoredPosition;
            private readonly Vector2 sizeDelta;

            public SettingsRectLayoutSnapshot(RectTransform target)
            {
                anchorMin = target.anchorMin;
                anchorMax = target.anchorMax;
                pivot = target.pivot;
                anchoredPosition = target.anchoredPosition;
                sizeDelta = target.sizeDelta;
            }

            public void Restore(RectTransform target)
            {
                target.anchorMin = anchorMin;
                target.anchorMax = anchorMax;
                target.pivot = pivot;
                target.anchoredPosition = anchoredPosition;
                target.sizeDelta = sizeDelta;
            }
        }

        private readonly struct SettingsTextLayoutSnapshot
        {
            private readonly bool resizeTextForBestFit;

            public int BaseFontSize { get; }
            public int BaseBestFitMinSize { get; }
            public int BaseBestFitMaxSize { get; }

            public SettingsTextLayoutSnapshot(Text target)
            {
                resizeTextForBestFit = target.resizeTextForBestFit;
                var accessibilityProfile = target.GetComponent<AccessibilityTextProfile>();
                var textScale = CheeseTama.Save.GameSettingsSaveData.NormalizeTextScale(
                    AccessibilityRuntime.TextScale);
                BaseFontSize = accessibilityProfile != null
                        && accessibilityProfile.BaseFontSize > 0
                    ? accessibilityProfile.BaseFontSize
                    : Mathf.Max(1, Mathf.RoundToInt(target.fontSize / textScale));
                BaseBestFitMinSize = accessibilityProfile != null
                        && accessibilityProfile.BaseBestFitMinSize > 0
                    ? accessibilityProfile.BaseBestFitMinSize
                    : Mathf.Max(1, Mathf.RoundToInt(target.resizeTextMinSize / textScale));
                BaseBestFitMaxSize = accessibilityProfile != null
                        && accessibilityProfile.BaseBestFitMaxSize > 0
                    ? accessibilityProfile.BaseBestFitMaxSize
                    : Mathf.Max(
                        BaseBestFitMinSize,
                        Mathf.RoundToInt(target.resizeTextMaxSize / textScale));
            }

            public void Restore(Text target, float textScale)
            {
                target.fontSize = Mathf.Max(1, Mathf.RoundToInt(BaseFontSize * textScale));
                target.resizeTextForBestFit = resizeTextForBestFit;
                target.resizeTextMinSize = Mathf.Max(
                    1,
                    Mathf.RoundToInt(BaseBestFitMinSize * textScale));
                target.resizeTextMaxSize = Mathf.Max(
                    target.resizeTextMinSize,
                    Mathf.RoundToInt(BaseBestFitMaxSize * textScale));
            }
        }

        private static void ConfigureBottomCenteredRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void ApplySafeArea(SafeAreaInsets currentInsets)
        {
            if (targetCanvas == null || !targetCanvas.isRootCanvas)
            {
                return;
            }

            safeAreaTargets.Clear();
            CollectSafeAreaTargets(targetCanvas.transform);
            foreach (var target in safeAreaTargets)
            {
                var previous = appliedInsets.TryGetValue(target, out var existing)
                    ? existing
                    : SafeAreaInsets.Zero;
                SafeAreaRectLayout.ApplyDelta(target, previous, currentInsets);
                appliedInsets[target] = currentInsets;
            }

            staleTargets.Clear();
            foreach (var pair in appliedInsets)
            {
                if (pair.Key == null || !safeAreaTargets.Contains(pair.Key))
                {
                    if (pair.Key != null)
                    {
                        SafeAreaRectLayout.ApplyDelta(pair.Key, pair.Value, SafeAreaInsets.Zero);
                    }

                    staleTargets.Add(pair.Key);
                }
            }

            for (var index = 0; index < staleTargets.Count; index += 1)
            {
                appliedInsets.Remove(staleTargets[index]);
            }
        }

        private void CollectSafeAreaTargets(Transform parent)
        {
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = parent.GetChild(index) as RectTransform;
                if (child == null || child == landscapePrompt)
                {
                    continue;
                }

                if (SafeAreaRectLayout.IsFullStretch(child))
                {
                    // Full-bleed backdrops keep covering the whole browser canvas. Their direct
                    // edge-aligned content is inset instead, avoiding a double safe-area offset.
                    CollectSafeAreaTargets(child);
                }
                else if (SafeAreaRectLayout.UsesScreenEdge(child))
                {
                    safeAreaTargets.Add(child);
                }
            }
        }

        private void ApplyAdaptiveTouchHitAreas(float preferredCanvasUnits)
        {
            if (targetCanvas == null)
            {
                return;
            }

            selectables.Clear();
            targetCanvas.GetComponentsInChildren(true, selectables);
            for (var index = 0; index < selectables.Count; index += 1)
            {
                var selectable = selectables[index];
                if (!IsEligibleTouchSelectable(selectable)
                    || selectable.transform == landscapePrompt)
                {
                    selectable?.GetComponent<AdaptiveTouchHitArea>()
                        ?.Configure(TouchHitAreaInsets.Zero);
                    continue;
                }

                var hitArea = selectable.GetComponent<AdaptiveTouchHitArea>();
                if (preferredCanvasUnits <= 0f)
                {
                    hitArea?.Configure(TouchHitAreaInsets.Zero);
                    continue;
                }

                if (hitArea == null)
                {
                    hitArea = selectable.gameObject.AddComponent<AdaptiveTouchHitArea>();
                }

                var targetRect = selectable.transform as RectTransform;
                if (targetRect == null)
                {
                    hitArea.Configure(TouchHitAreaInsets.Zero);
                    continue;
                }

                siblingSelectableBounds.Clear();
                for (var siblingIndex = 0; siblingIndex < selectables.Count; siblingIndex += 1)
                {
                    var sibling = selectables[siblingIndex];
                    if (ReferenceEquals(sibling, selectable)
                        || !IsEligibleTouchSelectable(sibling)
                        || sibling.transform.parent != selectable.transform.parent
                        || sibling.transform is not RectTransform siblingRect)
                    {
                        continue;
                    }

                    siblingSelectableBounds.Add(CalculateRectInParentSpace(siblingRect));
                }

                var expansion = TouchHitAreaLayout.Resolve(
                    CalculateRectInParentSpace(targetRect),
                    siblingSelectableBounds,
                    preferredCanvasUnits);
                hitArea.Configure(expansion);
            }
        }

        private static bool IsEligibleTouchSelectable(Selectable selectable)
        {
            if (selectable == null || !selectable.gameObject.activeSelf)
            {
                return false;
            }

            var visibility = selectable.GetComponent<CanvasGroup>();
            return visibility == null
                || (visibility.alpha > 0.001f
                    && visibility.interactable
                    && visibility.blocksRaycasts);
        }

        private Rect CalculateRectInParentSpace(RectTransform target)
        {
            target.GetWorldCorners(rectWorldCorners);
            var parent = target.parent;
            var first = parent != null
                ? parent.InverseTransformPoint(rectWorldCorners[0])
                : rectWorldCorners[0];
            var xMin = first.x;
            var xMax = first.x;
            var yMin = first.y;
            var yMax = first.y;
            for (var index = 1; index < rectWorldCorners.Length; index += 1)
            {
                var corner = parent != null
                    ? parent.InverseTransformPoint(rectWorldCorners[index])
                    : rectWorldCorners[index];
                xMin = Mathf.Min(xMin, corner.x);
                xMax = Mathf.Max(xMax, corner.x);
                yMin = Mathf.Min(yMin, corner.y);
                yMax = Mathf.Max(yMax, corner.y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void EnsureLandscapePrompt(bool visible)
        {
            if (landscapePrompt == null)
            {
                var existing = transform.Find(LandscapePromptObjectName) as RectTransform;
                landscapePrompt = existing != null ? existing : CreateLandscapePrompt();
            }

            if (landscapePrompt == null)
            {
                return;
            }

            landscapePrompt.gameObject.SetActive(visible);
            if (visible)
            {
                landscapePrompt.SetAsLastSibling();
            }
        }

        private RectTransform CreateLandscapePrompt()
        {
            var promptObject = new GameObject(
                LandscapePromptObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            promptObject.transform.SetParent(transform, false);
            var promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = Vector2.zero;
            promptRect.anchorMax = Vector2.one;
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;
            var background = promptObject.GetComponent<Image>();
            background.color = new Color(0.16f, 0.11f, 0.06f, 0.97f);
            background.raycastTarget = true;

            var labelObject = new GameObject(
                "Landscape Prompt Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(promptRect, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.08f, 0.15f);
            labelRect.anchorMax = new Vector2(0.92f, 0.85f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<Text>();
            label.text = "가로 화면으로 돌려 주세요\n치즈타마는 가로 화면에 맞춰져 있어요.";
            label.font = KoreanUiFontRuntime.GetDefaultFont();
            label.fontSize = 30;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.91f, 0.62f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return promptRect;
        }

        private void RestoreAppliedInsets()
        {
            foreach (var pair in appliedInsets)
            {
                if (pair.Key != null)
                {
                    SafeAreaRectLayout.ApplyDelta(pair.Key, pair.Value, SafeAreaInsets.Zero);
                }
            }

            appliedInsets.Clear();
        }
    }
}
