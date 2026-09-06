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
                2);
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
                new Vector2(520f, 54f));
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

            var lineageOpen = GetOrCreateTopLeftButton(
                starCard.transform,
                "Open Star Lineage Button",
                "지난 치즈타마",
                new Vector2(596f, -44f),
                new Vector2(128f, 40f));
            ApplyCareButtonStyle(lineageOpen);

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

            var lineageOverlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                StarLineagePanelController.OverlayObjectName,
                new Color(0.035f, 0.05f, 0.14f, 0.86f));
            var lineageCard = GetOrCreatePanel(
                lineageOverlay.transform,
                "Star Lineage Card",
                Vector2.zero,
                new Vector2(820f, 650f));
            ConfigureCenteredRect(lineageCard.GetComponent<RectTransform>(), new Vector2(820f, 650f));
            if (lineageCard.TryGetComponent(out Image lineageCardImage))
            {
                lineageCardImage.color = new Color(0.94f, 0.96f, 1f, 1f);
                lineageCardImage.raycastTarget = true;
            }

            var lineageTitle = GetOrCreateText(
                lineageCard.transform,
                "Star Lineage Title Text",
                "지난 치즈타마 기록",
                31,
                TextAnchor.MiddleCenter,
                new Vector2(64f, -38f),
                new Vector2(692f, 54f));
            lineageTitle.fontStyle = FontStyle.Bold;
            var lineageBody = GetOrCreateText(
                lineageCard.transform,
                "Star Lineage Body Text",
                string.Empty,
                20,
                TextAnchor.UpperLeft,
                new Vector2(90f, -118f),
                new Vector2(640f, 262f));
            lineageBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            lineageBody.verticalOverflow = VerticalWrapMode.Overflow;
            var lineageStatus = GetOrCreateText(
                lineageCard.transform,
                "Star Lineage Status Text",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                new Vector2(90f, -388f),
                new Vector2(640f, 52f));
            lineageStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            var lineagePrevious = GetOrCreateTopLeftButton(
                lineageCard.transform,
                "Star Lineage Previous Button",
                "이전 기록",
                new Vector2(90f, -452f),
                new Vector2(132f, 48f));
            var lineageNext = GetOrCreateTopLeftButton(
                lineageCard.transform,
                "Star Lineage Next Button",
                "다음 기록",
                new Vector2(236f, -452f),
                new Vector2(132f, 48f));
            var gentleTrait = GetOrCreateTopLeftButton(
                lineageCard.transform,
                "Star Lineage Gentle Trait Button",
                "다정한 손길",
                new Vector2(90f, -518f),
                new Vector2(190f, 54f));
            var maturationTrait = GetOrCreateTopLeftButton(
                lineageCard.transform,
                "Star Lineage Maturation Trait Button",
                "느긋한 숙성",
                new Vector2(298f, -518f),
                new Vector2(190f, 54f));
            var blendingTrait = GetOrCreateTopLeftButton(
                lineageCard.transform,
                "Star Lineage Blending Trait Button",
                "호기심 많은 향",
                new Vector2(506f, -518f),
                new Vector2(190f, 54f));
            var lineageClose = GetOrCreateTopLeftButton(
                lineageCard.transform,
                "Star Lineage Close Button",
                "닫기",
                new Vector2(614f, -586f),
                new Vector2(116f, 46f));
            ApplyCareButtonStyle(lineagePrevious);
            ApplyCareButtonStyle(lineageNext);
            ApplyCareButtonStyle(gentleTrait);
            ApplyCareButtonStyle(maturationTrait);
            ApplyCareButtonStyle(blendingTrait);
            ApplyCareButtonStyle(lineageClose);

            var lineageController = canvasTransform.GetComponent<StarLineagePanelController>()
                ?? canvasTransform.gameObject.AddComponent<StarLineagePanelController>();
            lineageController.Configure(
                lineageOverlay,
                lineageOpen,
                lineageClose,
                lineagePrevious,
                lineageNext,
                gentleTrait,
                maturationTrait,
                blendingTrait,
                lineageTitle,
                lineageBody,
                lineageStatus,
                () => GameManager.Instance?.GetStarLineageSnapshot(),
                traitId => GameManager.Instance != null
                    ? GameManager.Instance.TrySelectStarLineageTrait(traitId)
                    : default,
                lateBridge.SetStarPanelBlocking);
            lineageController.ConfigureEntryNavigation(starController.Close);
            lateBridge.ConfigureStarLineage(lineageController);

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
            lineageOverlay.transform.SetAsLastSibling();
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
                TextAnchor.MiddleCenter,
                new Vector2(80f, -122f),
                new Vector2(620f, 44f));
            var milk = GetOrCreateText(
                card.transform,
                "Growth Journey Milk Text",
                "주요 우유 완전 성장  0/7",
                24,
                TextAnchor.MiddleCenter,
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
                "레벨 33이 되고 주요 우유를 모두 키우면 별빛 이야기가 열려요.",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(80f, -368f),
                new Vector2(620f, 74f));
            unlock.horizontalOverflow = HorizontalWrapMode.Wrap;
            unlock.fontStyle = FontStyle.Bold;
            var actionRow = GetOrCreateRect(card.transform, "Growth Journey Actions");
            actionRow.anchorMin = new Vector2(0f, 1f);
            actionRow.anchorMax = new Vector2(0f, 1f);
            actionRow.pivot = new Vector2(0f, 1f);
            actionRow.anchoredPosition = new Vector2(180f, -468f);
            actionRow.sizeDelta = new Vector2(420f, 52f);
            var actionLayout = actionRow.GetComponent<HorizontalLayoutGroup>()
                ?? actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionLayout.padding = new RectOffset();
            actionLayout.spacing = 44f;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;

            var legacyLifeChapterOpen = card.transform.Find("Life Chapter Open Button");
            if (legacyLifeChapterOpen != null && legacyLifeChapterOpen.parent != actionRow)
            {
                legacyLifeChapterOpen.SetParent(actionRow, false);
            }

            var legacyClose = card.transform.Find("Growth Journey Close Button");
            if (legacyClose != null && legacyClose.parent != actionRow)
            {
                legacyClose.SetParent(actionRow, false);
            }

            var lifeChapterOpen = GetOrCreateTopLeftButton(
                actionRow,
                "Life Chapter Open Button",
                "생활 챕터",
                Vector2.zero,
                new Vector2(188f, 52f));
            var close = GetOrCreateTopLeftButton(
                actionRow,
                "Growth Journey Close Button",
                "확인",
                Vector2.zero,
                new Vector2(188f, 52f));
            foreach (var actionButton in new[] { lifeChapterOpen, close })
            {
                var layoutElement = actionButton.GetComponent<LayoutElement>()
                    ?? actionButton.gameObject.AddComponent<LayoutElement>();
                layoutElement.minWidth = 188f;
                layoutElement.preferredWidth = 188f;
                layoutElement.minHeight = 52f;
                layoutElement.preferredHeight = 52f;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;
            }

            ApplyCareButtonStyle(lifeChapterOpen);
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
                0);
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

            var chapterOverlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                LifeChapterPanelController.OverlayObjectName,
                new Color(0.08f, 0.055f, 0.035f, 0.84f));
            var chapterCard = GetOrCreatePanel(
                chapterOverlay.transform,
                "Life Chapter Card",
                Vector2.zero,
                new Vector2(900f, 700f));
            ConfigureCenteredRect(chapterCard.GetComponent<RectTransform>(), new Vector2(900f, 700f));
            if (chapterCard.TryGetComponent(out Image chapterCardImage))
            {
                chapterCardImage.color = new Color(1f, 0.965f, 0.88f, 1f);
                chapterCardImage.raycastTarget = true;
            }

            var chapterTitle = GetOrCreateText(
                chapterCard.transform,
                "Life Chapter Title Text",
                string.Empty,
                30,
                TextAnchor.MiddleCenter,
                new Vector2(70f, -30f),
                new Vector2(760f, 54f));
            chapterTitle.fontStyle = FontStyle.Bold;
            var chapterBody = GetOrCreateText(
                chapterCard.transform,
                "Life Chapter Body Text",
                string.Empty,
                20,
                TextAnchor.UpperLeft,
                new Vector2(90f, -96f),
                new Vector2(720f, 150f));
            chapterBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            chapterBody.verticalOverflow = VerticalWrapMode.Overflow;
            chapterBody.lineSpacing = 1.05f;
            var chapterProgress = GetOrCreateText(
                chapterCard.transform,
                "Life Chapter Progress Text",
                string.Empty,
                20,
                TextAnchor.MiddleLeft,
                new Vector2(90f, -260f),
                new Vector2(720f, 40f));
            chapterProgress.fontStyle = FontStyle.Bold;

            var objectiveTexts = new Text[LifeChapterCatalog.ObjectivesPerChapter];
            for (var index = 0; index < objectiveTexts.Length; index += 1)
            {
                objectiveTexts[index] = GetOrCreateText(
                    chapterCard.transform,
                    $"Life Chapter Objective {index + 1} Text",
                    string.Empty,
                    19,
                    TextAnchor.MiddleLeft,
                    new Vector2(110f, -314f - (index * 48f)),
                    new Vector2(680f, 38f));
            }

            var firstChoice = GetOrCreateTopLeftButton(
                chapterCard.transform,
                "Life Chapter First Choice Button",
                string.Empty,
                new Vector2(100f, -470f),
                new Vector2(320f, 62f));
            var secondChoice = GetOrCreateTopLeftButton(
                chapterCard.transform,
                "Life Chapter Second Choice Button",
                string.Empty,
                new Vector2(480f, -470f),
                new Vector2(320f, 62f));
            var chapterClose = GetOrCreateTopLeftButton(
                chapterCard.transform,
                "Life Chapter Close Button",
                "닫기",
                new Vector2(340f, -590f),
                new Vector2(220f, 56f));
            ApplyCareButtonStyle(firstChoice);
            ApplyCareButtonStyle(secondChoice);
            ApplyCareButtonStyle(chapterClose);

            var firstChoiceText = firstChoice.transform.Find("Label")?.GetComponent<Text>();
            var secondChoiceText = secondChoice.transform.Find("Label")?.GetComponent<Text>();
            var chapterController = canvasTransform.GetComponent<LifeChapterPanelController>()
                ?? canvasTransform.gameObject.AddComponent<LifeChapterPanelController>();
            chapterController.Configure(
                chapterOverlay,
                chapterTitle,
                chapterBody,
                chapterProgress,
                objectiveTexts,
                firstChoice,
                firstChoiceText,
                secondChoice,
                secondChoiceText,
                chapterClose,
                () => GameManager.Instance?.GetPendingLifeChapterSnapshot()
                    ?? LifeChapterSnapshot.CreateHidden(),
                (chapterId, choiceId) => GameManager.Instance?.TryChooseLifeChapter(
                    chapterId,
                    choiceId),
                CreateControlBlockingCallback(canvasTransform));
            chapterController.ConfigureEntryButton(lifeChapterOpen, controller.Close);

            var chapterBridge = canvasTransform.GetComponent<LifeChapterAvailabilityBridge>()
                ?? canvasTransform.gameObject.AddComponent<LifeChapterAvailabilityBridge>();
            chapterBridge.Configure(chapterController, Application.isPlaying ? GameManager.Instance : null);
            overlay.transform.SetAsLastSibling();
            chapterOverlay.transform.SetAsLastSibling();
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

            var legacyContentRect = card.transform.Find(
                "Memory Journal Viewport/Memory Journal Content") as RectTransform;
            var scroll = EnsureVerticalScrollArea(
                card.transform,
                "Memory Journal Viewport",
                "Memory Journal Clip Viewport",
                "Memory Journal Scroll Content",
                new Vector2(48f, -108f),
                new Vector2(804f, 500f),
                out var scrollContent);
            var viewportObject = scroll.gameObject;
            if (viewportObject.TryGetComponent(out Image viewportImage))
            {
                viewportImage.color = new Color(1f, 0.985f, 0.93f, 0.94f);
            }

            var legacyViewportMask = viewportObject.GetComponent<RectMask2D>();
            if (legacyViewportMask != null)
            {
                legacyViewportMask.enabled = false;
            }

            var scrollLayout = scrollContent.GetComponent<VerticalLayoutGroup>();
            if (scrollLayout != null)
            {
                scrollLayout.padding = new RectOffset(24, 24, 24, 24);
                scrollLayout.spacing = 0f;
                scrollLayout.childAlignment = TextAnchor.UpperLeft;
            }

            if (legacyContentRect != null && legacyContentRect.parent != scrollContent)
            {
                legacyContentRect.SetParent(scrollContent, false);
            }

            var contentRect = GetOrCreateRect(scrollContent, "Memory Journal Content");
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
            contentFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            ConfigureScrollableLayoutElement(entries, 1f);

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
                1);
            ApplyCareButtonStyle(open);

            var manager = Application.isPlaying ? GameManager.Instance : null;
            var controller = canvasTransform.GetComponent<MemoryJournalPanelController>()
                ?? canvasTransform.gameObject.AddComponent<MemoryJournalPanelController>();
            controller.Configure(
                overlay,
                title,
                unread,
                entries,
                scroll,
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
                    titleRect.anchoredPosition = new Vector2(22f, -18f);
                    titleRect.sizeDelta = new Vector2(306f, 34f);
                    if (titleRect.TryGetComponent(out Text titleText))
                    {
                        titleText.text = "돌봄 팁";
                        titleText.alignment = TextAnchor.MiddleLeft;
                    }
                }

                var bodyTransform = careTipPanel.Find("Care Tip Text");
                if (bodyTransform != null && bodyTransform.TryGetComponent(out Text bodyText))
                {
                    bodyText.alignment = TextAnchor.MiddleLeft;
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

        private static void EnsureJourneyHub(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                JourneyHubPanelController.OverlayObjectName,
                new Color(0.055f, 0.045f, 0.035f, 0.82f));
            var card = GetOrCreatePanel(
                overlay.transform,
                JourneyHubPanelController.CardObjectName,
                Vector2.zero,
                new Vector2(1120f, 760f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(1120f, 760f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.87f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Journey Hub Title Text",
                "다음 성장 목표",
                31,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -24f),
                new Vector2(360f, 52f));
            title.fontStyle = FontStyle.Bold;

            var tabs = new Button[5];
            var tabLabels = new[] { "목표", "주간", "관계", "앨범", "공방" };
            for (var index = 0; index < tabs.Length; index += 1)
            {
                tabs[index] = GetOrCreateTopLeftButton(
                    card.transform,
                    $"Journey Hub Tab {index}",
                    tabLabels[index],
                    new Vector2(48f + index * 200f, -86f),
                    new Vector2(184f, 48f));
                ApplyCareButtonStyle(tabs[index]);
            }

            var bodyContent = GetOrCreateVerticalScrollContent(
                card.transform,
                "Journey Hub Body Panel",
                "Journey Hub Body Scroll Content",
                new Vector2(48f, -150f),
                new Vector2(1024f, 464f),
                1200f,
                new Color(1f, 0.99f, 0.94f, 0.96f));
            var bodyPanel = bodyContent.parent.gameObject;
            if (bodyPanel.TryGetComponent(out Image bodyImage))
            {
                bodyImage.color = new Color(1f, 0.99f, 0.94f, 0.96f);
                bodyImage.raycastTarget = true;
            }

            MoveDirectChildIfExists(bodyPanel.transform, bodyContent, "Journey Hub Body Text");
            var body = GetOrCreateText(
                bodyContent,
                "Journey Hub Body Text",
                "여정 데이터를 준비하고 있어요.",
                18,
                TextAnchor.UpperLeft,
                new Vector2(12f, -8f),
                new Vector2(976f, 432f));
            body.supportRichText = true;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.lineSpacing = 1.05f;
            body.raycastTarget = false;
            ConfigureJourneyBodyScrollLayout(bodyContent, body, 464f);

            var status = GetOrCreateText(
                card.transform,
                "Journey Hub Status Text",
                string.Empty,
                17,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -620f),
                new Vector2(650f, 46f));
            status.color = new Color(0.64f, 0.34f, 0.09f);
            status.fontStyle = FontStyle.Bold;

            var previous = GetOrCreateTopLeftButton(
                card.transform,
                "Journey Hub Previous Button",
                "이전",
                new Vector2(48f, -676f),
                new Vector2(128f, 52f));
            var next = GetOrCreateTopLeftButton(
                card.transform,
                "Journey Hub Next Button",
                "다음",
                new Vector2(190f, -676f),
                new Vector2(128f, 52f));
            var primary = GetOrCreateTopLeftButton(
                card.transform,
                "Journey Hub Primary Button",
                "실행",
                new Vector2(748f, -676f),
                new Vector2(150f, 52f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Journey Hub Close Button",
                "닫기",
                new Vector2(922f, -676f),
                new Vector2(150f, 52f));
            ApplyCareButtonStyle(previous);
            ApplyCareButtonStyle(next);
            ApplyCareButtonStyle(primary);
            ApplyCareButtonStyle(close);

            var open = GetOrMoveUtilityButton(
                canvasTransform,
                null,
                JourneyHubPanelController.OpenButtonObjectName,
                "여정",
                new Vector2(112f, -48f),
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);

            var controller = canvasTransform.GetComponent<JourneyHubPanelController>()
                ?? canvasTransform.gameObject.AddComponent<JourneyHubPanelController>();
            controller.Configure(
                overlay,
                open,
                tabs,
                title,
                body,
                status,
                previous,
                next,
                primary,
                close,
                GameManager.Instance,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            var bodyScrollRect = bodyPanel.GetComponent<ScrollRect>();
            void ScrollJourneyBodyToTop()
            {
                if (bodyScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    bodyScrollRect.StopMovement();
                    if (bodyScrollRect.content != null)
                    {
                        var position = bodyScrollRect.content.anchoredPosition;
                        position.y = 0f;
                        bodyScrollRect.content.anchoredPosition = position;
                    }

                    bodyScrollRect.verticalNormalizedPosition = 1f;
                }
            }

            foreach (var tab in tabs)
            {
                tab.onClick.AddListener(ScrollJourneyBodyToTop);
            }

            previous.onClick.AddListener(ScrollJourneyBodyToTop);
            next.onClick.AddListener(ScrollJourneyBodyToTop);
            open.onClick.AddListener(ScrollJourneyBodyToTop);
            EnsureLifeRecordsPanel(canvasTransform, card.transform);
            EnsurePostgameResearchPanel(canvasTransform, card.transform);
            EnsureMilkroomMysteryPanel(canvasTransform);
            EnsureDreamStorySeasonPanel(canvasTransform);
            EnsureMilkroomInvestigation(canvasTransform);
            EnsureNpcAfterstoryPanel(canvasTransform);
            overlay.transform.SetAsLastSibling();
        }
    }
}
