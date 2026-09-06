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

            EnsureCheeseTamaProfileMenuShell(canvasTransform);

            Button FindButton(string path)
            {
                var target = canvasTransform.Find(path);
                return target != null ? target.GetComponent<Button>() : null;
            }

            var settingsModal = canvasTransform.Find("Settings Modal")?.gameObject;
            var replayButton = GetOrMoveProfileEntryButton(
                canvasTransform,
                settingsModal != null ? settingsModal.transform : null,
                "Replay First Meeting Button",
                "튜토리얼 다시 보기",
                4);
            ApplyCareButtonStyle(replayButton);

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
                "건너뛴 뒤에도 프로필 메뉴에서 튜토리얼을 다시 볼 수 있습니다.",
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
                visualController,
                () => canvasTransform.GetComponent<CheeseTamaProfileMenuController>()
                    ?.CloseForChildNavigation());
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
                new Vector2(980f, 800f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(980f, 800f));
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
                TextAnchor.MiddleCenter, new Vector2(74f, -126f), new Vector2(832f, 72f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            var selection = GetOrCreateText(card.transform, "New Game Setup Selection Text", "아직 선택하지 않음", 17,
                TextAnchor.MiddleCenter, new Vector2(74f, -204f), new Vector2(832f, 124f));
            var status = GetOrCreateText(card.transform, "New Game Setup Status Text", string.Empty, 15,
                TextAnchor.MiddleCenter, new Vector2(74f, -656f), new Vector2(832f, 52f));
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
                    var x = 122f + column * 250f + (row == 1 ? 125f : 0f);
                    var y = -340f - row * 146f;
                    var button = GetOrCreateTopLeftButton(
                        parent,
                        $"{prefix} Option Button {index}",
                        choices[index].DisplayName,
                        new Vector2(x, y),
                        new Vector2(236f, 138f));
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

            var back = GetOrCreateTopLeftButton(card.transform, "New Game Setup Back Button", "이전", new Vector2(62f, -720f), new Vector2(118f, 48f));
            var skip = GetOrCreateTopLeftButton(card.transform, "New Game Setup Skip Button", "건너뛰기", new Vector2(620f, -720f), new Vector2(126f, 48f));
            var primary = GetOrCreateTopLeftButton(card.transform, "New Game Setup Primary Button", "다음", new Vector2(762f, -720f), new Vector2(156f, 48f));
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
    }
}
