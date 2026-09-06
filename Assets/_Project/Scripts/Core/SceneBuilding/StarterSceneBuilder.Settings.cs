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

        private static Transform EnsureSettingsScrollContent(
            Transform settingsTransform,
            out ScrollRect scrollRect)
        {
            const float viewportWidth = 502f;
            const float viewportHeight = 652f;
            const float contentHeight = 1058f;

            var scrollView = GetOrCreatePanel(
                settingsTransform,
                "Settings Scroll View",
                new Vector2(18f, -70f),
                new Vector2(524f, viewportHeight));
            var scrollViewImage = scrollView.GetComponent<Image>();
            scrollViewImage.color = Color.clear;
            scrollViewImage.raycastTarget = false;

            var viewport = GetOrCreatePanel(
                scrollView.transform,
                "Viewport",
                Vector2.zero,
                new Vector2(viewportWidth, viewportHeight));
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;
            var viewportMask = viewport.GetComponent<RectMask2D>()
                ?? viewport.AddComponent<RectMask2D>();
            viewportMask.padding = Vector4.zero;

            var contentRect = GetOrCreateRect(viewport.transform, "Content");
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, contentHeight);

            var legacyChildren = new List<Transform>();
            for (var index = 0; index < settingsTransform.childCount; index += 1)
            {
                var child = settingsTransform.GetChild(index);
                if (child == null
                    || child == scrollView.transform
                    || child.name == "Settings Title Text"
                    || child.name == "Close Settings Button"
                    || !IsScrollableSettingsChild(child.name))
                {
                    continue;
                }

                legacyChildren.Add(child);
            }

            foreach (var legacyChild in legacyChildren)
            {
                legacyChild.SetParent(contentRect, false);
            }

            var scrollbarRect = GetOrCreateRect(scrollView.transform, "Settings Vertical Scrollbar");
            ConfigureTopLeftRect(scrollbarRect, 508f, 8f, 10f, viewportHeight - 16f);
            var scrollbarImage = scrollbarRect.GetComponent<Image>()
                ?? scrollbarRect.gameObject.AddComponent<Image>();
            scrollbarImage.color = new Color(0.56f, 0.40f, 0.20f, 0.18f);
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
            handleImage.color = new Color(0.95f, 0.54f, 0.12f, 0.9f);
            ApplyRoundedImage(handleImage);

            var scrollbar = scrollbarRect.GetComponent<Scrollbar>()
                ?? scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            scrollRect = scrollView.GetComponent<ScrollRect>()
                ?? scrollView.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 42f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 4f;
            return contentRect;
        }

        private static bool IsScrollableSettingsChild(string name)
        {
            return name == "Export Save Button"
                || name == "Choose Import File Button"
                || name == "Open Reset Button"
                || name == "Open Cloud Save Button"
                || name == "Data Status Text"
                || name == "Settings Last Saved Text"
                || name == "Import Save Confirmation Input"
                || name == "Confirm Import Save Button"
                || name == "Mute Audio Toggle"
                || name == "Fullscreen Toggle"
                || name == "Care Tip Toggle"
                || name == "Accessibility Inline Title Text"
                || name == "Accessibility Text Scale Label"
                || name == "Accessibility Text 100 Button"
                || name == "Accessibility Text 125 Button"
                || name == "Accessibility Text 140 Button"
                || name == "High Contrast Toggle"
                || name == "Reduce Motion Toggle"
                || name == "Reset Settings Button"
                || name == "Settings Status Text"
                || name == "Open Input Bindings Button"
                || name == "Game Home Button"
                || name == "Settings Data Title Text"
                || name == "Settings Sound Title Text"
                || name == "Settings Display Title Text"
                || name == "Settings Controls Title Text"
                || name.StartsWith("Master Volume ")
                || name.StartsWith("Music Volume ")
                || name.StartsWith("Effect Volume ")
                || name.StartsWith("Viewport ")
                || name.StartsWith("UI Scale ")
                || name.StartsWith("Frame Rate ")
                || name.StartsWith("Graphics Quality ")
                || name.StartsWith("Settings Data Section ")
                || name.StartsWith("Settings Sound Section ")
                || name.StartsWith("Settings Display Section ")
                || name.StartsWith("Settings Controls Section ");
        }

        private static void ConfigureSettingsSectionBackground(
            Transform content,
            string name,
            Vector2 position,
            Vector2 size)
        {
            var panel = GetOrCreatePanel(content, name, position, size);
            var image = panel.GetComponent<Image>();
            image.color = new Color(1f, 0.91f, 0.66f, 0.34f);
            image.raycastTarget = false;
            panel.transform.SetAsFirstSibling();
        }

        private static void CenterSettingsButtonLabels(Transform settingsContent)
        {
            if (settingsContent == null)
            {
                return;
            }

            var buttons = settingsContent.GetComponentsInChildren<Button>(true);
            for (var index = 0; index < buttons.Length; index += 1)
            {
                var label = buttons[index] != null
                    ? buttons[index].transform.Find("Label")?.GetComponent<Text>()
                    : null;
                if (label == null)
                {
                    continue;
                }

                label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.offsetMin = new Vector2(8f, 2f);
                label.rectTransform.offsetMax = new Vector2(-8f, -2f);
            }
        }

        private static void ApplyCompactSettingsButtonTypography(Transform settingsContent)
        {
            if (settingsContent == null)
            {
                return;
            }

            var buttons = settingsContent.GetComponentsInChildren<Button>(true);
            for (var index = 0; index < buttons.Length; index += 1)
            {
                var label = buttons[index] != null
                    ? buttons[index].transform.Find("Label")?.GetComponent<Text>()
                    : null;
                if (label == null)
                {
                    continue;
                }

                label.fontSize = 13;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 13;
                label.lineSpacing = 0.92f;
                label.GetComponent<AccessibilityTextProfile>()?.Rebase(label);
            }
        }

        private static void ApplyCompactSettingsContentTypography(Transform settingsContent)
        {
            if (settingsContent == null)
            {
                return;
            }

            var labels = settingsContent.GetComponentsInChildren<Text>(true);
            for (var index = 0; index < labels.Length; index += 1)
            {
                var label = labels[index];
                if (label == null)
                {
                    continue;
                }

                label.fontSize = 13;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 13;
                label.GetComponent<AccessibilityTextProfile>()?.Rebase(label);
            }

            foreach (var titleName in new[]
                     {
                         "Settings Data Title Text",
                         "Settings Sound Title Text",
                         "Settings Display Title Text",
                         "Settings Controls Title Text"
                     })
            {
                var title = settingsContent.Find(titleName)?.GetComponent<Text>();
                if (title != null)
                {
                    title.fontStyle = FontStyle.Bold;
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
            var settingsModal = GetOrCreateRightPanel(
                canvasTransform,
                "Settings Modal",
                new Vector2(-44f, -116f),
                new Vector2(560f, 740f));
            if (settingsModal.TryGetComponent(out Image settingsImage))
            {
                settingsImage.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            }

            var settingsTransform = settingsModal.transform;
            var settingsTitle = GetOrCreateText(settingsTransform, "Settings Title Text", "설정", 16, TextAnchor.UpperLeft, new Vector2(28, -24), new Vector2(280, 34));
            settingsTitle.fontStyle = FontStyle.Bold;
            settingsTitle.resizeTextForBestFit = true;
            settingsTitle.resizeTextMinSize = 13;
            settingsTitle.resizeTextMaxSize = 16;
            settingsTitle.GetComponent<AccessibilityTextProfile>()?.Rebase(settingsTitle);
            var closeSettingsButton = GetOrCreateTopLeftButton(
                settingsTransform,
                "Close Settings Button",
                "닫기",
                new Vector2(424f, -18f),
                new Vector2(108f, 42f));
            var settingsContent = EnsureSettingsScrollContent(settingsTransform, out var settingsScrollRect);
            ConfigureSettingsSectionBackground(
                settingsContent,
                "Settings Data Section Background",
                new Vector2(4f, -4f),
                new Vector2(490f, 252f));
            ConfigureSettingsSectionBackground(
                settingsContent,
                "Settings Sound Section Background",
                new Vector2(4f, -280f),
                new Vector2(490f, 170f));
            ConfigureSettingsSectionBackground(
                settingsContent,
                "Settings Display Section Background",
                new Vector2(4f, -474f),
                new Vector2(490f, 320f));
            ConfigureSettingsSectionBackground(
                settingsContent,
                "Settings Controls Section Background",
                new Vector2(4f, -818f),
                new Vector2(490f, 236f));
            GetOrCreateText(settingsContent, "Settings Data Title Text", "데이터 관리", 16, TextAnchor.UpperLeft, new Vector2(20, -16), new Vector2(300, 28));
            GetOrCreateText(settingsContent, "Settings Sound Title Text", "소리", 16, TextAnchor.UpperLeft, new Vector2(20, -292), new Vector2(220, 28));
            GetOrCreateText(settingsContent, "Settings Display Title Text", "화면", 16, TextAnchor.UpperLeft, new Vector2(20, -486), new Vector2(140, 28));
            GetOrCreateText(settingsContent, "Settings Controls Title Text", "조작", 16, TextAnchor.UpperLeft, new Vector2(20, -830), new Vector2(220, 28));

            RemoveChildIfExists(settingsContent, "Manual Save Button");
            RemoveChildIfExists(settingsContent, "Manual Load Button");
            var exportSaveButton = GetOrCreateTopLeftButton(settingsContent, "Export Save Button", "백업 저장 만들기", new Vector2(20, -122), new Vector2(225, 36));
            var chooseImportFileButton = GetOrCreateTopLeftButton(settingsContent, "Choose Import File Button", "백업 저장 가져오기", new Vector2(253, -122), new Vector2(233, 36));
            var openResetButton = GetOrCreateTopLeftButton(settingsContent, "Open Reset Button", "처음부터 시작", new Vector2(20, -166), new Vector2(144, 36));
            ApplyDangerButtonStyle(openResetButton);
            var openCloudButton = GetOrCreateTopLeftButton(settingsContent, "Open Cloud Save Button", "온라인 저장", new Vector2(176, -166), new Vector2(128, 36));
            ApplyCareButtonStyle(openCloudButton);
#if UNITY_WEBGL && !UNITY_EDITOR
            openCloudButton.onClick.RemoveAllListeners();
            openCloudButton.gameObject.SetActive(false);
            var dataStatusMessage = RuntimePlatformCapabilities.BrowserLocalStorageNotice;
#else
            var dataStatusMessage = "돌봄 행동 후 자동 저장됩니다. 아래에서 백업을 만들거나 가져올 수 있습니다.";
#endif
            var dataStatusText = GetOrCreateText(settingsContent, "Data Status Text", dataStatusMessage, 13, TextAnchor.UpperLeft, new Vector2(20, -48), new Vector2(466, 68));
            ConfigureMultiLineFeedbackText(dataStatusText, 11, 13);
            settingsLastSavedText = GetOrCreateText(settingsContent, "Settings Last Saved Text", "<b>마지막 저장</b>  없음", 13, TextAnchor.MiddleLeft, new Vector2(316, -169), new Vector2(170, 30));
            settingsLastSavedText.supportRichText = true;
            settingsLastSavedText.color = new Color(0.34f, 0.22f, 0.1f);
            ConfigureMultiLineFeedbackText(settingsLastSavedText, 11, 13);
            var importConfirmationInput = GetOrCreateInputField(
                settingsContent,
                "Import Save Confirmation Input",
                SaveTransferImportSession.ConfirmationPhrase,
                new Vector2(20, -210),
                new Vector2(302, 36));
            var confirmImportButton = GetOrCreateTopLeftButton(
                settingsContent,
                "Confirm Import Save Button",
                "가져온 기록으로 바꾸기",
                new Vector2(332, -210),
                new Vector2(154, 36));
            ApplyDangerButtonStyle(confirmImportButton);

            GetOrCreateText(settingsContent, "Master Volume Label Text", "전체 볼륨", 13, TextAnchor.MiddleLeft, new Vector2(20, -324), new Vector2(96, 28));
            var masterVolumeSlider = GetOrCreateSettingsSlider(settingsContent, "Master Volume Slider", new Vector2(120, -328), new Vector2(268, 22), 0f, 1f, false);
            var masterVolumeValueText = GetOrCreateText(settingsContent, "Master Volume Value Text", "100%", 13, TextAnchor.MiddleRight, new Vector2(398, -322), new Vector2(76, 28));
            GetOrCreateText(settingsContent, "Music Volume Label Text", "배경음", 13, TextAnchor.MiddleLeft, new Vector2(20, -358), new Vector2(96, 28));
            var musicVolumeSlider = GetOrCreateSettingsSlider(settingsContent, "Music Volume Slider", new Vector2(120, -362), new Vector2(268, 22), 0f, 1f, false);
            var musicVolumeValueText = GetOrCreateText(settingsContent, "Music Volume Value Text", "100%", 13, TextAnchor.MiddleRight, new Vector2(398, -356), new Vector2(76, 28));
            GetOrCreateText(settingsContent, "Effect Volume Label Text", "효과음", 13, TextAnchor.MiddleLeft, new Vector2(20, -392), new Vector2(96, 28));
            var effectVolumeSlider = GetOrCreateSettingsSlider(settingsContent, "Effect Volume Slider", new Vector2(120, -396), new Vector2(268, 22), 0f, 1f, false);
            var effectVolumeValueText = GetOrCreateText(settingsContent, "Effect Volume Value Text", "100%", 13, TextAnchor.MiddleRight, new Vector2(398, -390), new Vector2(76, 28));
            var muteToggle = GetOrCreateSettingsToggle(settingsContent, "Mute Audio Toggle", "전체 음소거", new Vector2(20, -424), new Vector2(180, 26));
            ApplyCompactSettingsToggleTypography(muteToggle);

            var fullScreenToggle = GetOrCreateSettingsToggle(settingsContent, "Fullscreen Toggle", "전체화면", new Vector2(20, -534), new Vector2(140, 30));
            ApplyCompactSettingsToggleTypography(fullScreenToggle);
            var viewportZoomOutButton = GetOrCreateTopLeftButton(
                settingsContent,
                "Viewport Zoom Out Settings Button",
                MilkroomViewportNavigationController.ZoomOutLabel,
                new Vector2(170, -526),
                new Vector2(88, 36));
            var viewportZoomInButton = GetOrCreateTopLeftButton(
                settingsContent,
                "Viewport Zoom In Settings Button",
                MilkroomViewportNavigationController.ZoomInLabel,
                new Vector2(268, -526),
                new Vector2(88, 36));
            var viewportFitButton = GetOrCreateTopLeftButton(
                settingsContent,
                "Viewport Fit Settings Button",
                "화면 맞춤",
                new Vector2(366, -526),
                new Vector2(120, 36));
            ApplyCareButtonStyle(viewportZoomOutButton);
            ApplyCareButtonStyle(viewportZoomInButton);
            ApplyCareButtonStyle(viewportFitButton);
            GetOrCreateText(settingsContent, "UI Scale Label Text", "메뉴 크기", 13, TextAnchor.MiddleLeft, new Vector2(20, -576), new Vector2(96, 28));
            RemoveChildIfExists(settingsContent, "UI Scale Slider");
            var uiScale90Button = GetOrCreateTopLeftButton(settingsContent, "UI Scale 90 Button", "90", new Vector2(120, -570), new Vector2(72, 36));
            var uiScale100Button = GetOrCreateTopLeftButton(settingsContent, "UI Scale 100 Button", "100", new Vector2(202, -570), new Vector2(72, 36));
            var uiScale110Button = GetOrCreateTopLeftButton(settingsContent, "UI Scale 110 Button", "110", new Vector2(284, -570), new Vector2(72, 36));
            ApplyCollectionTabButtonStyle(uiScale90Button, uiScale100Button, uiScale110Button);
            RemoveChildIfExists(settingsContent, "UI Scale Value Text");
            Text uiScaleValueText = null;

            RemoveChildIfExists(settingsContent, "Accessibility Inline Title Text");
            GetOrCreateText(settingsContent, "Accessibility Text Scale Label", "글자 크기", 13, TextAnchor.MiddleLeft, new Vector2(20, -618), new Vector2(96, 28));
            var accessibilityText100Button = GetOrCreateTopLeftButton(settingsContent, "Accessibility Text 100 Button", "100", new Vector2(120, -612), new Vector2(72, 36));
            var accessibilityText125Button = GetOrCreateTopLeftButton(settingsContent, "Accessibility Text 125 Button", "125", new Vector2(202, -612), new Vector2(72, 36));
            var accessibilityText140Button = GetOrCreateTopLeftButton(settingsContent, "Accessibility Text 140 Button", "140", new Vector2(284, -612), new Vector2(72, 36));
            ApplyCollectionTabButtonStyle(accessibilityText100Button, accessibilityText125Button, accessibilityText140Button);
            var highContrastToggle = GetOrCreateSettingsToggle(settingsContent, "High Contrast Toggle", "색 차이 또렷하게", new Vector2(20, -656), new Vector2(218, 30));
            var reduceMotionToggle = GetOrCreateSettingsToggle(settingsContent, "Reduce Motion Toggle", "큰 움직임 줄이기", new Vector2(250, -656), new Vector2(218, 30));
            ApplyCompactSettingsToggleTypography(highContrastToggle);
            ApplyCompactSettingsToggleTypography(reduceMotionToggle);

            GetOrCreateText(settingsContent, "Frame Rate Label Text", "화면 부드러움", 13, TextAnchor.MiddleLeft, new Vector2(20, -702), new Vector2(96, 28));
            var frameRate30Button = GetOrCreateTopLeftButton(settingsContent, "Frame Rate 30 Button", "30", new Vector2(120, -696), new Vector2(72, 36));
            var frameRate60Button = GetOrCreateTopLeftButton(settingsContent, "Frame Rate 60 Button", "60", new Vector2(202, -696), new Vector2(72, 36));
            var frameRate120Button = GetOrCreateTopLeftButton(settingsContent, "Frame Rate 120 Button", "120", new Vector2(284, -696), new Vector2(72, 36));
            ApplyCollectionTabButtonStyle(frameRate30Button, frameRate60Button, frameRate120Button);
            RemoveChildIfExists(settingsContent, "Frame Rate Value Text");
            Text frameRateValueText = null;

            GetOrCreateText(settingsContent, "Graphics Quality Label Text", "그림 품질", 13, TextAnchor.MiddleLeft, new Vector2(20, -746), new Vector2(96, 28));
            RemoveChildIfExists(settingsContent, "Graphics Quality Low Button");
            Button qualityLowButton = null;
            var qualityBalancedButton = GetOrCreateTopLeftButton(settingsContent, "Graphics Quality Balanced Button", "균형", new Vector2(120, -740), new Vector2(112, 36));
            var qualityHighButton = GetOrCreateTopLeftButton(settingsContent, "Graphics Quality High Button", "고화질", new Vector2(244, -740), new Vector2(112, 36));
            ApplyCollectionTabButtonStyle(qualityBalancedButton, qualityHighButton);
            RemoveChildIfExists(settingsContent, "Graphics Quality Value Text");
            Text qualityValueText = null;

            var careTipToggle = GetOrCreateSettingsToggle(settingsContent, "Care Tip Toggle", "돌봄 팁 표시", new Vector2(20, -868), new Vector2(166, 30));
            ApplyCompactSettingsToggleTypography(careTipToggle);
            RemoveChildIfExists(settingsContent, "Accessibility Open Button");
            var resetSettingsButton = GetOrCreateTopLeftButton(settingsContent, "Reset Settings Button", "설정 초기화", new Vector2(20, -956), new Vector2(466, 36));
            ApplyCareButtonStyle(resetSettingsButton);
            var gameHomeButton = GetOrCreateTopLeftButton(
                settingsContent,
                "Game Home Button",
                "게임 홈으로 가기",
                new Vector2(20f, -1000f),
                new Vector2(466f, 36f));
            ApplyCareButtonStyle(gameHomeButton);
            ConfigureGameHomeButton(gameHomeButton);
            CenterSettingsButtonLabels(settingsContent);
            ApplyCompactSettingsButtonTypography(settingsContent);
            var settingsStatusText = GetOrCreateText(settingsContent, "Settings Status Text", "설정을 불러왔습니다.", 13, TextAnchor.MiddleLeft, new Vector2(200, -864), new Vector2(286, 38));
            settingsStatusText.color = new Color(0.38f, 0.28f, 0.17f);
            ConfigureMultiLineFeedbackText(settingsStatusText, 11, 13);
            ApplyCompactSettingsContentTypography(settingsContent);

            RemoveChildIfExists(settingsTransform, "Accessibility Panel");
            Text accessibilityTextScaleValue = null;

            var confirmRoot = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Confirm Reset Dialog",
                new Color(0.055f, 0.045f, 0.035f, 0.84f));
            var confirmCard = GetOrCreatePanel(
                confirmRoot.transform,
                "Confirm Reset Card",
                Vector2.zero,
                new Vector2(680, 640));
            ConfigureCenteredRect(confirmCard.GetComponent<RectTransform>(), new Vector2(680, 640));
            if (confirmCard.TryGetComponent(out Image confirmImage))
            {
                confirmImage.color = new Color(1f, 0.98f, 0.9f, 1f);
                confirmImage.raycastTarget = true;
            }

            MoveDirectChildIfExists(confirmRoot.transform, confirmCard.transform, "Confirm Reset Title Text");
            MoveDirectChildIfExists(confirmRoot.transform, confirmCard.transform, "Confirm Reset Message Text");
            MoveDirectChildIfExists(confirmRoot.transform, confirmCard.transform, "Reset Input Label Text");
            MoveDirectChildIfExists(confirmRoot.transform, confirmCard.transform, "Reset Input Field");
            MoveDirectChildIfExists(confirmRoot.transform, confirmCard.transform, "Confirm Reset Button");
            MoveDirectChildIfExists(confirmRoot.transform, confirmCard.transform, "Cancel Reset Button");
            var confirmTransform = confirmCard.transform;
            GetOrCreateText(confirmTransform, "Confirm Reset Title Text", "게임 기록 지우기", 22, TextAnchor.MiddleCenter, new Vector2(56, -22), new Vector2(568, 34));
            var confirmMessageText = GetOrCreateText(
                confirmTransform,
                "Confirm Reset Message Text",
                "지울 게임 기록을 골라 주세요.",
                15,
                TextAnchor.UpperLeft,
                new Vector2(24, -70),
                new Vector2(632, 280));
            ConfigureMultiLineFeedbackText(confirmMessageText, 12, 15);
            var careProgressResetButton = GetOrCreateTopLeftButton(
                confirmTransform,
                "Care Progress Reset Mode Button",
                "치즈타마만 새로 시작",
                new Vector2(24, -366),
                new Vector2(308, 48));
            var fullLocalResetButton = GetOrCreateTopLeftButton(
                confirmTransform,
                "Full Local Reset Mode Button",
                "현재 게임 기록 모두 지우기",
                new Vector2(348, -366),
                new Vector2(308, 48));
            ApplyCareButtonStyle(careProgressResetButton);
            ApplyDangerButtonStyle(fullLocalResetButton);
            GetOrCreateText(confirmTransform, "Reset Input Label Text", "아래에 나온 확인 말을 똑같이 적어 주세요.", 14, TextAnchor.MiddleLeft, new Vector2(24, -426), new Vector2(632, 24));
            var resetInput = GetOrCreateInputField(
                confirmTransform,
                "Reset Input Field",
                ProgressResetPolicy.CareProgressConfirmationPhrase,
                new Vector2(24, -460),
                new Vector2(632, 52));
            resetInput.contentType = InputField.ContentType.Standard;
            resetInput.inputType = InputField.InputType.Standard;
            resetInput.keyboardType = TouchScreenKeyboardType.Default;
            resetInput.lineType = InputField.LineType.SingleLine;
            resetInput.characterValidation = InputField.CharacterValidation.None;
            resetInput.characterLimit = 0;
            resetInput.shouldHideMobileInput = false;
            if (resetInput.textComponent != null)
            {
                resetInput.textComponent.alignment = TextAnchor.MiddleLeft;
            }

            if (resetInput.placeholder is Text resetPlaceholder)
            {
                resetPlaceholder.alignment = TextAnchor.MiddleLeft;
            }

            var resetImeBridge = resetInput.GetComponent<WebGlImeInputBridge>()
                ?? resetInput.gameObject.AddComponent<WebGlImeInputBridge>();
            resetImeBridge.Configure(resetInput);

            var confirmResetButton = GetOrCreateTopLeftButton(confirmTransform, "Confirm Reset Button", "선택한 기록 지우기", new Vector2(140, -536), new Vector2(260, 48));
            ApplyDangerButtonStyle(confirmResetButton);
            var cancelResetButton = GetOrCreateTopLeftButton(confirmTransform, "Cancel Reset Button", "취소", new Vector2(416, -536), new Vector2(124, 48));

            var confirmResetDialog = confirmRoot.GetComponent<ConfirmResetDialog>();
            if (confirmResetDialog == null)
            {
                confirmResetDialog = confirmRoot.AddComponent<ConfirmResetDialog>();
            }

            confirmResetDialog.Configure(
                confirmRoot,
                resetInput,
                confirmMessageText,
                careProgressResetButton,
                fullLocalResetButton,
                confirmResetButton,
                cancelResetButton,
                controller,
                visualController);
            confirmResetDialog.SetBlockingCallback(CreateControlBlockingCallback(canvasTransform));
#if UNITY_WEBGL && !UNITY_EDITOR
            var existingCloudOverlay = canvasTransform.Find(CloudSavePanelController.OverlayObjectName);
            if (existingCloudOverlay != null)
            {
                existingCloudOverlay.gameObject.SetActive(false);
            }
#else
            EnsureCloudSavePanel(canvasTransform, openCloudButton);
#endif

            var dataPanel = settingsModal.GetComponent<DataManagementPanelController>();
            if (dataPanel == null)
            {
                dataPanel = settingsModal.AddComponent<DataManagementPanelController>();
            }

            dataPanel.Configure(
                null,
                null,
                openResetButton,
                dataStatusText,
                confirmResetDialog,
                controller,
                visualController);
            dataPanel.ConfigureSaveTransfer(
                exportSaveButton,
                chooseImportFileButton,
                importConfirmationInput,
                confirmImportButton);

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
                qualityLowButton,
                qualityBalancedButton,
                qualityHighButton,
                careTipToggle,
                resetSettingsButton,
                masterVolumeValueText,
                musicVolumeValueText,
                effectVolumeValueText,
                uiScaleValueText,
                frameRateValueText,
                qualityValueText,
                settingsStatusText);

            var accessibilityController = settingsModal.GetComponent<AccessibilitySettingsPanelController>();
            if (accessibilityController == null)
            {
                accessibilityController = settingsModal.AddComponent<AccessibilitySettingsPanelController>();
            }

            accessibilityController.ConfigureInline(
                accessibilityText100Button,
                accessibilityText125Button,
                accessibilityText140Button,
                highContrastToggle,
                reduceMotionToggle,
                accessibilityTextScaleValue,
                settingsStatusText,
                canvasTransform);

            var settingsController = settingsModal.GetComponent<SettingsMenuController>();
            if (settingsController == null)
            {
                settingsController = settingsModal.AddComponent<SettingsMenuController>();
            }

            settingsController.Configure(
                settingsButton,
                closeSettingsButton,
                settingsModal,
                settingsScrollRect);

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

        private static void EnsureCloudSavePanel(Transform canvasTransform, Button openButton)
        {
            if (canvasTransform == null || openButton == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                CloudSavePanelController.OverlayObjectName,
                new Color(0.055f, 0.045f, 0.035f, 0.84f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Cloud Save Card",
                Vector2.zero,
                new Vector2(780f, 500f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(780f, 500f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.96f, 0.98f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Cloud Save Title Text",
                "온라인 저장",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(44f, -28f),
                new Vector2(520f, 48f));
            title.fontStyle = FontStyle.Bold;
            var provider = GetOrCreateText(
                card.transform,
                "Cloud Save Provider Text",
                "저장 위치 · 이 기기에만 저장 중",
                16,
                TextAnchor.MiddleLeft,
                new Vector2(44f, -88f),
                new Vector2(692f, 34f));
            var status = GetOrCreateText(
                card.transform,
                "Cloud Save Status Text",
                "온라인 저장 연결을 확인하고 있어요.",
                18,
                TextAnchor.UpperLeft,
                new Vector2(44f, -142f),
                new Vector2(692f, 130f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.verticalOverflow = VerticalWrapMode.Overflow;
            var confirmation = GetOrCreateInputField(
                card.transform,
                "Cloud Save Confirmation Input",
                CloudSavePanelController.RemoteConfirmationPhrase,
                new Vector2(44f, -298f),
                new Vector2(360f, 52f));
            var synchronize = GetOrCreateTopLeftButton(
                card.transform,
                "Cloud Save Synchronize Button",
                "저장 비교하기",
                new Vector2(44f, -398f),
                new Vector2(160f, 48f));
            var applyRemote = GetOrCreateTopLeftButton(
                card.transform,
                "Cloud Save Apply Remote Button",
                "클라우드 적용",
                new Vector2(220f, -398f),
                new Vector2(190f, 48f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Cloud Save Close Button",
                "닫기",
                new Vector2(576f, -398f),
                new Vector2(160f, 48f));
            ApplyCareButtonStyle(synchronize);
            ApplyDangerButtonStyle(applyRemote);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<CloudSavePanelController>()
                ?? canvasTransform.gameObject.AddComponent<CloudSavePanelController>();
            controller.Configure(
                overlay,
                provider,
                status,
                confirmation,
                synchronize,
                applyRemote,
                close,
                GameManager.Instance,
                null,
                CreateControlBlockingCallback(canvasTransform));
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(controller.Open);
            overlay.transform.SetAsLastSibling();
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

            var settingsContent = settingsModal.Find("Settings Scroll View/Viewport/Content")
                ?? settingsModal;

            var openButton = GetOrCreateTopLeftButton(
                settingsContent,
                "Open Input Bindings Button",
                "키 설정",
                new Vector2(20f, -912f),
                new Vector2(466f, 36f));
            ApplyCareButtonStyle(openButton);
            ApplyCompactSettingsButtonTypography(settingsContent);

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
    }
}
