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
        private static void EnsureMilkroomStatGauges(
            Transform canvasTransform,
            MilkroomUIController controller)
        {
            var statBar = canvasTransform?.Find("Stat Bar");
            if (canvasTransform == null || controller == null)
            {
                return;
            }

            if (statBar != null)
            {
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

            var topBar = canvasTransform.Find("Top Status Bar");
            if (topBar != null)
            {
                controller.ConfigureLevelProgressGauge(GetOrCreateLevelProgressGauge(topBar));
                ConfigureTopBarIdentityLayout(topBar);
            }
        }

        private static Image GetOrCreateLevelProgressGauge(Transform topBar)
        {
            var track = GetOrCreatePanel(
                topBar,
                "Level Progress Gauge",
                new Vector2(TopIdentityProgressLeft, -TopIdentityGaugeTop),
                new Vector2(TopIdentityProgressPreferredWidth, TopIdentityGaugeHeight));
            var trackImage = track.GetComponent<Image>();
            if (trackImage != null)
            {
                ApplyRoundedImage(trackImage);
                trackImage.color = new Color(0.32f, 0.25f, 0.17f, 0.18f);
                trackImage.raycastTarget = false;
            }

            var fillTransform = track.transform.Find("Level Progress Fill");
            Image fill;
            if (fillTransform == null)
            {
                var fillObject = new GameObject(
                    "Level Progress Fill",
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
                new Vector2(24f, -740f),
                new Vector2(972f, 58f));
            if (utilityBar.TryGetComponent(out Image image))
            {
                image.color = new Color(1f, 0.96f, 0.8f, 0.9f);
                image.raycastTarget = false;
            }

            return utilityBar.transform;
        }

        private static void BuildMilkroomFeedbackPanels(
            Transform canvasTransform,
            out Text careTipText,
            out Text messageText,
            out Text eventMessageText)
        {
            var careTipPanel = GetOrCreatePanel(
                canvasTransform,
                "Care Tip Panel",
                new Vector2(24f, -528f),
                new Vector2(350f, 196f));
            if (careTipPanel.TryGetComponent(out Image careTipPanelImage))
            {
                careTipPanelImage.color = new Color(1f, 0.96f, 0.8f, 0.92f);
            }

            var careTipTitleText = GetOrCreateText(
                careTipPanel.transform,
                "Care Tip Title Text",
                "돌봄 팁",
                22,
                TextAnchor.MiddleLeft,
                new Vector2(22f, -18f),
                new Vector2(306f, 34f));
            careTipTitleText.fontStyle = FontStyle.Bold;
            careTipText = GetOrCreateText(
                careTipPanel.transform,
                "Care Tip Text",
                "배가 고프면 아래의 '우유주기'를 눌러 주세요.",
                20,
                TextAnchor.MiddleLeft,
                new Vector2(22f, -58f),
                new Vector2(306f, 130f));
            careTipText.color = new Color(0.28f, 0.18f, 0.08f);
            careTipText.resizeTextForBestFit = true;
            careTipText.resizeTextMinSize = 16;
            careTipText.resizeTextMaxSize = 20;
            careTipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            careTipText.verticalOverflow = VerticalWrapMode.Truncate;
            careTipText.lineSpacing = 1.08f;

            var messageBar = GetOrCreateBottomPanel(
                canvasTransform,
                "Message Bar",
                new Vector2(0f, 146f),
                new Vector2(696f, 144f));
            if (messageBar.TryGetComponent(out Image messageBarImage))
            {
                messageBarImage.color = new Color(1f, 0.93f, 0.68f, 0.98f);
            }

            messageText = GetOrCreateStatusExplanationText(messageBar.transform);
            ConfigureMultiLineFeedbackText(messageText, 17, 22);
            messageText.fontStyle = FontStyle.Bold;
            messageText.color = new Color(0.28f, 0.18f, 0.08f);
            ConfigureStatusExplanationScroll(messageBar, messageText);

            // 이벤트가 있을 때는 같은 큰 안내 칸을 덮어써 두 메시지가 서로 밀거나 겹치지 않게 한다.
            var eventMessageBar = GetOrCreateBottomPanel(
                canvasTransform,
                "Event Message Bar",
                new Vector2(0f, 146f),
                new Vector2(696f, 144f));
            if (eventMessageBar.TryGetComponent(out Image eventMessageBarImage))
            {
                eventMessageBarImage.color = new Color(0.86f, 0.92f, 1f, 0.98f);
            }

            eventMessageText = GetOrCreateText(
                eventMessageBar.transform,
                "Event Message Text",
                "새 소식이 생기면 여기에 알려 드려요.",
                20,
                TextAnchor.MiddleLeft,
                new Vector2(24f, -12f),
                new Vector2(648f, 120f));
            ConfigureMultiLineFeedbackText(eventMessageText, 16, 20);
            eventMessageText.fontStyle = FontStyle.Bold;
            eventMessageText.color = new Color(0.16f, 0.24f, 0.42f);
            eventMessageBar.SetActive(false);
        }

        private static void ConfigureCareTipInteraction(
            Transform canvasTransform,
            MilkroomUIController controller)
        {
            var careTipPanel = canvasTransform != null
                ? canvasTransform.Find("Care Tip Panel")
                : null;
            if (careTipPanel == null || controller == null)
            {
                return;
            }

            var panelImage = careTipPanel.GetComponent<Image>();
            var button = careTipPanel.GetComponent<Button>()
                ?? careTipPanel.gameObject.AddComponent<Button>();
            button.targetGraphic = panelImage;
            button.interactable = true;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(controller.AdvanceCareTip);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        private static void ConfigureMultiLineFeedbackText(Text label, int minimumSize, int maximumSize)
        {
            if (label == null)
            {
                return;
            }

            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = minimumSize;
            label.resizeTextMaxSize = maximumSize;
            label.lineSpacing = 1.04f;
        }

        private static void ConfigureStatusExplanationScroll(GameObject messageBar, Text label)
        {
            if (messageBar == null || label == null)
            {
                return;
            }

            var viewportRect = GetOrCreateRect(messageBar.transform, "Message Scroll View");
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            viewportRect.offsetMin = new Vector2(24f, 12f);
            viewportRect.offsetMax = new Vector2(-44f, -12f);
            var viewportImage = viewportRect.GetComponent<Image>()
                ?? viewportRect.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;
            var viewportMask = viewportRect.GetComponent<RectMask2D>()
                ?? viewportRect.gameObject.AddComponent<RectMask2D>();
            viewportMask.padding = Vector4.zero;

            var labelRect = label.rectTransform;
            labelRect.SetParent(viewportRect, false);
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(0f, 120f);
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = false;
            label.raycastTarget = false;

            var layoutElement = label.GetComponent<LayoutElement>()
                ?? label.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 120f;
            var contentSizeFitter = label.GetComponent<ContentSizeFitter>()
                ?? label.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbarRect = GetOrCreateRect(messageBar.transform, "Message Vertical Scrollbar");
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-34f, 16f);
            scrollbarRect.offsetMax = new Vector2(-24f, -16f);
            var scrollbarImage = scrollbarRect.GetComponent<Image>()
                ?? scrollbarRect.gameObject.AddComponent<Image>();
            scrollbarImage.color = new Color(0.56f, 0.36f, 0.12f, 0.20f);
            ApplyRoundedImage(scrollbarImage);

            var slidingArea = GetOrCreateRect(scrollbarRect, "Sliding Area");
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(2f, 2f);
            slidingArea.offsetMax = new Vector2(-2f, -2f);
            var handleRect = GetOrCreateRect(slidingArea, "Handle");
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImage = handleRect.GetComponent<Image>()
                ?? handleRect.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.94f, 0.54f, 0.12f, 0.94f);
            ApplyRoundedImage(handleImage);

            var scrollbar = scrollbarRect.GetComponent<Scrollbar>()
                ?? scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            var scrollRect = messageBar.GetComponent<ScrollRect>()
                ?? messageBar.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = labelRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 34f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalScrollbarSpacing = 8f;
        }

        private static void EnsureExistingStatusExplanationScroll(Transform canvasTransform)
        {
            var messageBarTransform = canvasTransform?.Find("Message Bar");
            if (messageBarTransform == null)
            {
                return;
            }

            var messageText = GetOrCreateStatusExplanationText(messageBarTransform);
            ConfigureStatusExplanationScroll(messageBarTransform.gameObject, messageText);
        }

        private static Text GetOrCreateStatusExplanationText(Transform messageBarTransform)
        {
            var nestedText = messageBarTransform
                ?.Find("Message Scroll View/Message Text")
                ?.GetComponent<Text>();
            if (nestedText != null)
            {
                ConfigureText(
                    nestedText,
                    "무엇을 해 줄까요? 아래에서 돌봄을 골라 주세요.",
                    22,
                    TextAnchor.MiddleLeft,
                    new Vector2(24f, -12f),
                    new Vector2(648f, 120f),
                    false);
                return nestedText;
            }

            return GetOrCreateText(
                messageBarTransform,
                "Message Text",
                "무엇을 해 줄까요? 아래에서 돌봄을 골라 주세요.",
                22,
                TextAnchor.MiddleLeft,
                new Vector2(24f, -12f),
                new Vector2(648f, 120f));
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

            if (TryResolveMilkroomUtilityGridIndex(name, out var gridIndex))
            {
                ConfigureTopLeftButton(
                    button,
                    label,
                    new Vector2(10f + gridIndex * 96f, -8f),
                    new Vector2(88f, 42f));
            }
            else
            {
                ConfigureTopLeftButton(button, label, anchoredPosition, size);
            }

            ReflowMilkroomUtilityButtons(utilityParent);
            return button;
        }

        private static void ReflowMilkroomUtilityButtons(Transform utilityParent)
        {
            if (utilityParent == null)
            {
                return;
            }

            var visible = new List<(int index, RectTransform rect)>();
            for (var childIndex = 0; childIndex < utilityParent.childCount; childIndex += 1)
            {
                var child = utilityParent.GetChild(childIndex) as RectTransform;
                if (child != null
                    && child.gameObject.activeSelf
                    && TryResolveMilkroomUtilityGridIndex(child.name, out var index))
                {
                    visible.Add((index, child));
                }
            }

            visible.Sort((left, right) => left.index.CompareTo(right.index));
            var visibleCount = visible.Count;
            var existingCollapseController = utilityParent.GetComponent<MilkroomUtilityBarController>();
            var displayedCount = existingCollapseController != null
                && existingCollapseController.IsCollapsed
                    ? 0
                    : visibleCount;
            var careTipPanel = utilityParent.parent?.Find("Care Tip Panel");
            var utilityTop = careTipPanel != null && !careTipPanel.gameObject.activeSelf
                ? 528f
                : 740f;
            const float toggleWidth = 42f;
            var utilityRect = utilityParent as RectTransform;
            ConfigureTopLeftRect(
                utilityRect,
                24f,
                utilityTop,
                16f + displayedCount * 88f + displayedCount * 8f + toggleWidth,
                58f);
            for (var index = 0; index < displayedCount; index += 1)
            {
                ConfigureTopLeftRect(
                    visible[index].rect,
                    10f + index * 96f,
                    8f,
                    88f,
                    42f);
            }

            var toggle = GetOrCreateTopLeftButton(
                utilityParent,
                MilkroomUtilityBarController.ToggleObjectName,
                "<",
                new Vector2(8f + displayedCount * 96f, -8f),
                new Vector2(toggleWidth, 42f));
            ApplyCareButtonStyle(toggle);
            var toggleLabel = toggle.transform.Find("Label")?.GetComponent<Text>();
            if (toggleLabel != null)
            {
                toggleLabel.alignment = TextAnchor.MiddleCenter;
                toggleLabel.fontSize = 22;
                toggleLabel.resizeTextMinSize = 18;
                toggleLabel.resizeTextMaxSize = 22;
            }

            var utilityButtons = new List<Button>(8);
            for (var index = 0; index < utilityParent.childCount; index += 1)
            {
                var child = utilityParent.GetChild(index);
                var button = child != null && TryResolveMilkroomUtilityGridIndex(child.name, out _)
                    ? child.GetComponent<Button>()
                    : null;
                if (button != null && button != toggle)
                {
                    utilityButtons.Add(button);
                }
            }

            var collapseController = existingCollapseController
                ?? utilityParent.gameObject.AddComponent<MilkroomUtilityBarController>();
            collapseController.Configure(
                toggle,
                utilityButtons,
                () => ReflowMilkroomUtilityButtons(utilityParent));
            utilityParent.parent?.GetComponent<ResponsiveCanvasRuntime>()?.RefreshLayout();
        }

        private static bool TryResolveMilkroomUtilityGridIndex(string name, out int index)
        {
            index = name switch
            {
                "Open Delivery Button" => 0,
                JourneyHubPanelController.OpenButtonObjectName => 1,
                "Open Npc Afterstory Button" => 2,
                "Open First Day Journey Button" => 3,
                "Open Milkroom Investigation Button" => 4,
                "Open Fantasy Powder Button" => 5,
                "Open Milkroom Mystery Button" => 6,
                "Open Dream Story Season Button" => 7,
                _ => -1
            };
            return index >= 0;
        }

        private static void ApplyMilkroomTopHudLayout(
            GameObject topBar,
            GameObject topMenu,
            Text nameText,
            Text levelText,
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

            var topBarTransform = topBar != null ? topBar.transform : null;
            if (topBarTransform != null)
            {
                var resourceLayout = ResolveTopResourceLayout(topBarTransform);
                ApplyTopBarResourceLayout(topBarTransform, resourceLayout);
                ApplyTopBarIdentityLayout(
                    topBarTransform,
                    resourceLayout.SessionRight);
            }

            var availableButtonWidth = MilkroomRightPanelWidth
                - (TopMenuPadding * 2f)
                - (TopMenuButtonGap * 2f);
            var shortButtonWidth = (availableButtonWidth - TopMenuDecorateButtonWidth) * 0.5f;
            var decorateButtonLeft = TopMenuPadding + shortButtonWidth + TopMenuButtonGap;
            var settingsButtonLeft = decorateButtonLeft + TopMenuDecorateButtonWidth + TopMenuButtonGap;
            ConfigureTopLeftRect(topCollectionButton != null ? topCollectionButton.GetComponent<RectTransform>() : null, TopMenuPadding, TopMenuButtonTop, shortButtonWidth, TopMenuButtonHeight);
            ConfigureTopLeftRect(topDecorateButton != null ? topDecorateButton.GetComponent<RectTransform>() : null, decorateButtonLeft, TopMenuButtonTop, TopMenuDecorateButtonWidth, TopMenuButtonHeight);
            ConfigureTopLeftRect(settingsButton != null ? settingsButton.GetComponent<RectTransform>() : null, settingsButtonLeft, TopMenuButtonTop, shortButtonWidth, TopMenuButtonHeight);
            ApplySharedTopMenuLabelSize(
                topCollectionButton,
                topDecorateButton,
                settingsButton);
        }

        private static void ApplySharedTopMenuLabelSize(params Button[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
            {
                return;
            }

            var labels = new Text[buttons.Length];
            for (var index = 0; index < buttons.Length; index += 1)
            {
                var button = buttons[index];
                var label = button != null
                    ? button.transform.Find("Label")?.GetComponent<Text>()
                    : null;
                if (label == null)
                {
                    return;
                }

                labels[index] = label;
                var profile = label.GetComponent<AccessibilityTextProfile>();
                if (profile == null)
                {
                    profile = label.gameObject.AddComponent<AccessibilityTextProfile>();
                    profile.Rebase(label);
                }
            }

            var textScale = GameSettingsSaveData.NormalizeTextScale(AccessibilityRuntime.TextScale);
            var sharedSize = Mathf.Approximately(textScale, GameSettingsSaveData.LargeTextScale)
                ? TopMenuLargeTextSharedFontSize
                : Mathf.Approximately(textScale, GameSettingsSaveData.MediumTextScale)
                    ? TopMenuMediumTextSharedFontSize
                    : TopMenuLabelMaxFontSize;
            var alreadyShared = true;
            for (var index = 0; alreadyShared && index < labels.Length; index += 1)
            {
                var label = labels[index];
                alreadyShared = label.resizeTextForBestFit
                    && label.resizeTextMinSize == sharedSize
                    && label.resizeTextMaxSize == sharedSize
                    && label.fontSize == sharedSize
                    && label.horizontalOverflow == HorizontalWrapMode.Overflow;
            }

            if (alreadyShared)
            {
                return;
            }

            for (var index = 0; index < labels.Length; index += 1)
            {
                var label = labels[index];
                label.fontSize = sharedSize;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = sharedSize;
                label.resizeTextMaxSize = sharedSize;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        internal static void RefreshMilkroomTopHudLayout(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return;
            }

            var topBar = canvasRoot.Find("Top Status Bar");
            var topMenu = canvasRoot.Find("Top Menu");
            if (topBar == null && topMenu == null)
            {
                return;
            }

            if (topMenu == null)
            {
                ConfigureTopBarIdentityLayout(topBar);
                return;
            }

            ApplyMilkroomTopHudLayout(
                topBar != null ? topBar.gameObject : null,
                topMenu != null ? topMenu.gameObject : null,
                topBar?.Find("Name Text")?.GetComponent<Text>(),
                topBar?.Find("Level Text")?.GetComponent<Text>(),
                topBar?.Find("Coin Economy Text")?.GetComponent<Text>(),
                topBar?.Find("Milk Drop Economy Text")?.GetComponent<Text>(),
                topBar?.Find("Collection Fragment Economy Text")?.GetComponent<Text>(),
                topMenu?.Find("Top Collection Button")?.GetComponent<Button>(),
                topMenu?.Find("Top Decorate Button")?.GetComponent<Button>(),
                topMenu?.Find("Settings Button")?.GetComponent<Button>());
        }

        private static void ConfigureTopBarIdentityLayout(Transform topBar)
        {
            if (topBar == null)
            {
                return;
            }

            var resourceLayout = ResolveTopResourceLayout(topBar);
            ApplyTopBarIdentityLayout(topBar, resourceLayout.SessionRight);
        }

        private static void ApplyTopBarIdentityLayout(Transform topBar, float sessionRight)
        {
            ResolveTopBarIdentityWidths(
                topBar,
                sessionRight,
                out var nameWidth,
                out var progressWidth);
            var progressLeft = TopIdentityLeft + nameWidth + TopIdentitySectionGap;
            var sessionLeft = progressLeft + progressWidth + TopSessionSectionGap;

            var nameRect = topBar.Find("Name Text") as RectTransform;
            ConfigureTopLeftRect(
                nameRect,
                TopIdentityLeft,
                TopIdentityNameTop,
                nameWidth,
                TopIdentityNameHeight);
            if (nameRect != null && nameRect.TryGetComponent(out Text nameLabel))
            {
                nameLabel.supportRichText = false;
            }

            var levelRect = topBar.Find("Level Text") as RectTransform;
            ConfigureTopLeftRect(
                levelRect,
                progressLeft,
                TopIdentityLevelTop,
                progressWidth,
                TopIdentityLevelHeight);
            if (levelRect != null && levelRect.TryGetComponent(out Text levelLabel))
            {
                levelLabel.fontSize = 14;
                levelLabel.supportRichText = false;
                ApplyTopInfoTextStyle(levelLabel, 14);
                AccessibilityRuntime.ApplyCurrent(levelLabel);
            }

            ConfigureTopLeftRect(
                topBar.Find("Level Progress Gauge") as RectTransform,
                progressLeft,
                TopIdentityGaugeTop,
                progressWidth,
                TopIdentityGaugeHeight);

            ConfigureTopStretchRect(
                topBar.Find("Session Text") as RectTransform,
                sessionLeft,
                sessionRight,
                TopSessionTop,
                TopSessionHeight);
        }

        private static void ResolveTopBarIdentityWidths(
            Transform topBar,
            float sessionRight,
            out float nameWidth,
            out float progressWidth)
        {
            nameWidth = TopIdentityPreferredWidth;
            progressWidth = TopIdentityProgressPreferredWidth;
            if (!(topBar is RectTransform topBarRect)
                || topBarRect.rect.width <= sessionRight + TopIdentityLeft)
            {
                return;
            }

            var availableWidth = topBarRect.rect.width
                - sessionRight
                - TopIdentityLeft
                - TopIdentitySectionGap
                - TopSessionSectionGap;
            var sessionMinimumWidth = TopSessionMinimumWidth;
            if (topBar.Find("Session Text") is RectTransform sessionRect
                && sessionRect.TryGetComponent(out Text sessionLabel))
            {
                sessionMinimumWidth = Mathf.Max(
                    sessionMinimumWidth,
                    sessionLabel.preferredWidth);
            }

            var minimumWidth = TopIdentityMinimumNameWidth
                + TopIdentityProgressMinimumWidth
                + sessionMinimumWidth;
            if (availableWidth >= TopIdentityPreferredWidth
                + TopIdentityProgressPreferredWidth
                + sessionMinimumWidth)
            {
                return;
            }

            if (availableWidth < minimumWidth)
            {
                var identityMinimumWidth = TopIdentityMinimumNameWidth
                    + TopIdentityProgressMinimumWidth;
                var identityAvailableWidth = Mathf.Max(
                    0f,
                    availableWidth - sessionMinimumWidth);
                var scale = Mathf.Clamp01(identityAvailableWidth / identityMinimumWidth);
                nameWidth = TopIdentityMinimumNameWidth * scale;
                progressWidth = TopIdentityProgressMinimumWidth * scale;
                return;
            }

            nameWidth = Mathf.Min(
                TopIdentityPreferredWidth,
                availableWidth - TopIdentityProgressMinimumWidth - sessionMinimumWidth);
            progressWidth = Mathf.Min(
                TopIdentityProgressPreferredWidth,
                availableWidth - nameWidth - sessionMinimumWidth);
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
            ConfigureTopBarResourceLayout(topBarTransform);
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

            var layout = ResolveTopResourceLayout(topBarTransform);
            ApplyTopBarResourceIconLayout(topBarTransform, layout);
        }

        private static void ApplyTopBarResourceIconLayout(
            Transform topBarTransform,
            TopResourceLayout layout)
        {
            ConfigureTopRightRect(
                topBarTransform.Find("Coin Economy Icon") as RectTransform,
                layout.CoinIconRight,
                27f,
                TopResourceIconSize,
                TopResourceIconSize);
            ConfigureTopRightRect(
                topBarTransform.Find("Milk Drop Economy Icon") as RectTransform,
                layout.MilkDropIconRight,
                27f,
                TopResourceIconSize,
                TopResourceIconSize);
            ConfigureTopRightRect(
                topBarTransform.Find("Collection Fragment Economy Icon") as RectTransform,
                layout.CollectionFragmentIconRight,
                27f,
                TopResourceIconSize,
                TopResourceIconSize);
        }

        private static void ConfigureTopBarResourceLayout(Transform topBarTransform)
        {
            if (topBarTransform == null)
            {
                return;
            }

            var layout = ResolveTopResourceLayout(topBarTransform);
            ApplyTopBarResourceLayout(topBarTransform, layout);
        }

        private static void ApplyTopBarResourceLayout(
            Transform topBarTransform,
            TopResourceLayout layout)
        {
            ConfigureTopRightRect(
                topBarTransform.Find("Coin Economy Text") as RectTransform,
                layout.CoinTextRight,
                17f,
                layout.CoinTextWidth,
                48f);
            ConfigureTopRightRect(
                topBarTransform.Find("Milk Drop Economy Text") as RectTransform,
                layout.MilkDropTextRight,
                17f,
                layout.MilkDropTextWidth,
                48f);
            ConfigureTopRightRect(
                topBarTransform.Find("Collection Fragment Economy Text") as RectTransform,
                layout.CollectionFragmentTextRight,
                17f,
                layout.CollectionFragmentTextWidth,
                48f);
            ApplyTopBarResourceIconLayout(topBarTransform, layout);
        }

        private static TopResourceLayout ResolveTopResourceLayout(Transform topBarTransform)
        {
            var coinLabel = topBarTransform != null
                ? topBarTransform.Find("Coin Economy Text")?.GetComponent<Text>()
                : null;
            var milkDropLabel = topBarTransform != null
                ? topBarTransform.Find("Milk Drop Economy Text")?.GetComponent<Text>()
                : null;
            var collectionFragmentLabel = topBarTransform != null
                ? topBarTransform.Find("Collection Fragment Economy Text")?.GetComponent<Text>()
                : null;

            var collectionFragmentTextWidth = ResolveTopResourceTextWidth(
                collectionFragmentLabel,
                TopCollectionFragmentResourceTextWidth);
            var collectionFragmentTextRight = TopResourceRightPadding;
            var collectionFragmentIconRight = collectionFragmentTextRight
                + collectionFragmentTextWidth
                + TopResourceIconLabelGap;

            var milkDropTextWidth = ResolveTopResourceTextWidth(
                milkDropLabel,
                TopMilkDropResourceTextWidth);
            var milkDropTextRight = collectionFragmentIconRight
                + TopResourceIconSize
                + TopResourceGroupGap;
            var milkDropIconRight = milkDropTextRight
                + milkDropTextWidth
                + TopResourceIconLabelGap;

            var coinTextWidth = ResolveTopResourceTextWidth(
                coinLabel,
                TopCoinResourceTextWidth);
            var coinTextRight = milkDropIconRight
                + TopResourceIconSize
                + TopResourceGroupGap;
            var coinIconRight = coinTextRight
                + coinTextWidth
                + TopResourceIconLabelGap;
            var sessionRight = coinIconRight
                + TopResourceIconSize
                + TopIdentitySectionGap;

            return new TopResourceLayout(
                coinTextRight,
                coinTextWidth,
                coinIconRight,
                milkDropTextRight,
                milkDropTextWidth,
                milkDropIconRight,
                collectionFragmentTextRight,
                collectionFragmentTextWidth,
                collectionFragmentIconRight,
                sessionRight);
        }

        private static float ResolveTopResourceTextWidth(Text label, float fallbackWidth)
        {
            if (label == null)
            {
                return fallbackWidth;
            }

            var preferredWidth = label.preferredWidth;
            if (float.IsNaN(preferredWidth)
                || float.IsInfinity(preferredWidth)
                || preferredWidth <= 0f)
            {
                return fallbackWidth;
            }

            return Mathf.Max(1f, Mathf.Ceil(preferredWidth));
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
    }
}
