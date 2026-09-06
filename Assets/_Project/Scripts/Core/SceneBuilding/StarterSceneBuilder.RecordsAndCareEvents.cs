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
        private static void EnsureLifeRecordsPanel(Transform canvasTransform, Transform journeyCard)
        {
            if (canvasTransform == null || journeyCard == null)
            {
                return;
            }

            var open = GetOrCreateTopLeftButton(
                journeyCard,
                "Life Records Open Button",
                "생활 기록",
                new Vector2(650f, -24f),
                new Vector2(204f, 52f));
            ApplyCareButtonStyle(open);

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                LifeRecordsPanelController.OverlayObjectName,
                new Color(0.055f, 0.045f, 0.035f, 0.84f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Life Records Card",
                Vector2.zero,
                new Vector2(1080f, 720f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(1080f, 720f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.87f, 1f);
                cardImage.raycastTarget = true;
            }

            MoveDirectChildIfExists(card.transform, journeyCard, "Mini Game Mastery Open Button");
            var masteryOpen = GetOrCreateTopLeftButton(
                journeyCard,
                "Mini Game Mastery Open Button",
                "메달 실력",
                new Vector2(868f, -24f),
                new Vector2(204f, 52f));
            ApplyCareButtonStyle(masteryOpen);

            var title = GetOrCreateText(
                card.transform,
                "Life Records Title Text",
                "생활 기록 앨범",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -24f),
                new Vector2(720f, 52f));
            title.fontStyle = FontStyle.Bold;

            var overviewPanel = GetOrCreatePanel(
                card.transform,
                "Life Records Overview Panel",
                new Vector2(48f, -96f),
                new Vector2(500f, 520f));
            var overviewScroll = EnsureVerticalScrollArea(
                overviewPanel.transform,
                "Life Records Overview Scroll View",
                "Viewport",
                "Content",
                new Vector2(16f, -16f),
                new Vector2(468f, 488f),
                out var overviewContent);
            MoveDirectChildIfExists(
                overviewPanel.transform,
                overviewContent,
                "Life Records Overview Text");
            var overview = GetOrCreateText(
                overviewContent,
                "Life Records Overview Text",
                string.Empty,
                18,
                TextAnchor.UpperLeft,
                Vector2.zero,
                new Vector2(0f, 456f));
            overview.supportRichText = true;
            overview.horizontalOverflow = HorizontalWrapMode.Wrap;
            overview.verticalOverflow = VerticalWrapMode.Overflow;
            overview.resizeTextForBestFit = false;
            ConfigureScrollableLayoutElement(overview, 456f);
            overviewScroll.verticalNormalizedPosition = 1f;

            var episodePanel = GetOrCreatePanel(
                card.transform,
                "Life Records Episode Panel",
                new Vector2(572f, -96f),
                new Vector2(460f, 520f));
            var position = GetOrCreateText(
                episodePanel.transform,
                "Life Records Episode Position Text",
                "완료 에피소드",
                17,
                TextAnchor.MiddleLeft,
                new Vector2(24f, -20f),
                new Vector2(412f, 36f));
            position.fontStyle = FontStyle.Bold;
            var episodeScroll = EnsureVerticalScrollArea(
                episodePanel.transform,
                "Life Records Episode Scroll View",
                "Viewport",
                "Content",
                new Vector2(16f, -64f),
                new Vector2(428f, 424f),
                out var episodeContent);
            MoveDirectChildIfExists(
                episodePanel.transform,
                episodeContent,
                "Life Records Episode Text");
            var episode = GetOrCreateText(
                episodeContent,
                "Life Records Episode Text",
                string.Empty,
                18,
                TextAnchor.UpperLeft,
                Vector2.zero,
                new Vector2(0f, 392f));
            episode.supportRichText = true;
            episode.horizontalOverflow = HorizontalWrapMode.Wrap;
            episode.verticalOverflow = VerticalWrapMode.Overflow;
            episode.resizeTextForBestFit = false;
            ConfigureScrollableLayoutElement(episode, 392f);
            episodeScroll.verticalNormalizedPosition = 1f;

            var previous = GetOrCreateTopLeftButton(
                card.transform,
                "Life Records Previous Button",
                "이전 기록",
                new Vector2(572f, -640f),
                new Vector2(150f, 48f));
            var next = GetOrCreateTopLeftButton(
                card.transform,
                "Life Records Next Button",
                "다음 기록",
                new Vector2(736f, -640f),
                new Vector2(150f, 48f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Life Records Close Button",
                "닫기",
                new Vector2(900f, -640f),
                new Vector2(132f, 48f));
            ApplyCareButtonStyle(previous);
            ApplyCareButtonStyle(next);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<LifeRecordsPanelController>()
                ?? canvasTransform.gameObject.AddComponent<LifeRecordsPanelController>();
            controller.Configure(
                overlay,
                title,
                overview,
                episode,
                position,
                previous,
                next,
                close,
                () => GameManager.Instance?.GetLifeRecordsSnapshot() ?? LifeRecordsSnapshot.Empty,
                null,
                CreateControlBlockingCallback(canvasTransform));
            open.onClick.RemoveAllListeners();
            open.onClick.AddListener(() => controller.Open());

            var masteryOverlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                MiniGameMasteryPanelController.OverlayObjectName,
                new Color(0.045f, 0.055f, 0.1f, 0.86f));
            var masteryCard = GetOrCreatePanel(
                masteryOverlay.transform,
                "Mini Game Mastery Card",
                Vector2.zero,
                new Vector2(840f, 640f));
            ConfigureCenteredRect(masteryCard.GetComponent<RectTransform>(), new Vector2(840f, 640f));
            if (masteryCard.TryGetComponent(out Image masteryCardImage))
            {
                masteryCardImage.color = new Color(0.94f, 0.97f, 1f, 1f);
                masteryCardImage.raycastTarget = true;
            }

            var masteryTitle = GetOrCreateText(
                masteryCard.transform,
                "Mini Game Mastery Title Text",
                "생활놀이 메달 · 실력판",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(70f, -34f),
                new Vector2(700f, 54f));
            masteryTitle.fontStyle = FontStyle.Bold;
            var masteryScroll = EnsureVerticalScrollArea(
                masteryCard.transform,
                "Mini Game Mastery Content Scroll View",
                "Viewport",
                "Content",
                new Vector2(82f, -104f),
                new Vector2(676f, 410f),
                out var masteryContent);
            MoveDirectChildIfExists(
                masteryCard.transform,
                masteryContent,
                "Mini Game Mastery Summary Text");
            MoveDirectChildIfExists(
                masteryCard.transform,
                masteryContent,
                "Mini Game Mastery Position Text");
            MoveDirectChildIfExists(
                masteryCard.transform,
                masteryContent,
                "Mini Game Mastery Detail Text");
            var masterySummary = GetOrCreateText(
                masteryContent,
                "Mini Game Mastery Summary Text",
                string.Empty,
                19,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(0f, 78f));
            masterySummary.horizontalOverflow = HorizontalWrapMode.Wrap;
            masterySummary.verticalOverflow = VerticalWrapMode.Overflow;
            masterySummary.resizeTextForBestFit = false;
            ConfigureScrollableLayoutElement(masterySummary, 78f);
            var masteryPosition = GetOrCreateText(
                masteryContent,
                "Mini Game Mastery Position Text",
                "생활놀이 실력",
                18,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                new Vector2(0f, 38f));
            masteryPosition.fontStyle = FontStyle.Bold;
            masteryPosition.horizontalOverflow = HorizontalWrapMode.Wrap;
            masteryPosition.verticalOverflow = VerticalWrapMode.Overflow;
            masteryPosition.resizeTextForBestFit = false;
            ConfigureScrollableLayoutElement(masteryPosition, 38f);
            var masteryDetail = GetOrCreateText(
                masteryContent,
                "Mini Game Mastery Detail Text",
                string.Empty,
                20,
                TextAnchor.UpperLeft,
                Vector2.zero,
                new Vector2(0f, 246f));
            masteryDetail.supportRichText = true;
            masteryDetail.horizontalOverflow = HorizontalWrapMode.Wrap;
            masteryDetail.verticalOverflow = VerticalWrapMode.Overflow;
            masteryDetail.resizeTextForBestFit = false;
            ConfigureScrollableLayoutElement(masteryDetail, 246f);
            masteryScroll.verticalNormalizedPosition = 1f;
            var masteryPrevious = GetOrCreateTopLeftButton(
                masteryCard.transform,
                "Mini Game Mastery Previous Button",
                "이전 놀이",
                new Vector2(100f, -536f),
                new Vector2(160f, 50f));
            var masteryNext = GetOrCreateTopLeftButton(
                masteryCard.transform,
                "Mini Game Mastery Next Button",
                "다음 놀이",
                new Vector2(278f, -536f),
                new Vector2(160f, 50f));
            var masteryClose = GetOrCreateTopLeftButton(
                masteryCard.transform,
                "Mini Game Mastery Close Button",
                "닫기",
                new Vector2(580f, -536f),
                new Vector2(160f, 50f));
            ApplyCareButtonStyle(masteryPrevious);
            ApplyCareButtonStyle(masteryNext);
            ApplyCareButtonStyle(masteryClose);

            var masteryController = canvasTransform.GetComponent<MiniGameMasteryPanelController>()
                ?? canvasTransform.gameObject.AddComponent<MiniGameMasteryPanelController>();
            masteryController.Configure(
                masteryOverlay,
                masteryOpen,
                masteryTitle,
                masterySummary,
                masteryDetail,
                masteryPosition,
                masteryPrevious,
                masteryNext,
                masteryClose,
                () => GameManager.Instance?.GetMiniGameMasterySnapshot()
                    ?? MiniGameMasteryBoardSnapshot.Empty,
                CreateControlBlockingCallback(canvasTransform));
            masteryController.ConfigureEntryNavigation(controller.Close);
            overlay.transform.SetAsLastSibling();
            masteryOverlay.transform.SetAsLastSibling();
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
                new Vector2(680f, 560f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(680f, 560f));
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
                new Vector2(584f, 250f));
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
                new Vector2(536f, 214f));
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
                new Vector2(48f, -418f),
                new Vector2(584f, 38f));
            rewardsText.fontStyle = FontStyle.Bold;
            rewardsText.color = new Color(0.72f, 0.34f, 0.08f);

            var confirmButton = GetOrCreateTopLeftButton(
                card.transform,
                "Return Summary Confirm Button",
                "확인",
                new Vector2(476f, -480f),
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
                "레벨 10 · 한 단계 더 자랐어요",
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
    }
}
