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
                $"남은 시간  {Mathf.CeilToInt(MilkDropMiniGameRules.DurationSeconds)}초",
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
            dropTemplate.transition = Selectable.Transition.None;
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

            var basketObject = GetOrCreatePanel(
                playArea,
                "Milk Drop Basket",
                new Vector2(345f, -400f),
                new Vector2(170f, 54f));
            var basket = basketObject.GetComponent<RectTransform>();
            if (basketObject.TryGetComponent(out Image basketImage))
            {
                basketImage.color = Color.clear;
                basketImage.raycastTarget = false;
            }

            var bowlOutline = basketObject.GetComponent<Outline>()
                ?? basketObject.AddComponent<Outline>();
            bowlOutline.enabled = false;

            RemoveChildIfExists(basketObject.transform, "Basket Rim");
            RemoveChildIfExists(basketObject.transform, "Bowl Body");
            RemoveChildIfExists(basketObject.transform, "Bowl Rim");
            RemoveChildIfExists(basketObject.transform, "Bowl Inner");
            RemoveChildIfExists(basketObject.transform, "Bowl Highlight");
            RemoveChildIfExists(basketObject.transform, "Basket Label");
            var bowlArtwork = GetOrCreatePanel(
                basketObject.transform,
                "Milk Drop Bowl Artwork",
                new Vector2(0f, 30f),
                new Vector2(170f, 100f));
            if (bowlArtwork.TryGetComponent(out Image bowlArtworkImage))
            {
                bowlArtworkImage.sprite = Resources.Load<Sprite>(
                    "UI/MiniGames/milk_drop_bowl_v002");
                bowlArtworkImage.type = Image.Type.Simple;
                bowlArtworkImage.preserveAspect = true;
                bowlArtworkImage.color = Color.white;
                bowlArtworkImage.raycastTarget = false;
            }
            bowlArtwork.transform.SetAsLastSibling();

            var resultText = GetOrCreateText(
                card.transform,
                "Milk Drop Catch Result Text",
                "그릇을 좌우로 움직여 우유방울을 받아 보세요!",
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
                basket,
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
                $"남은 시간  {Mathf.CeilToInt(BouncyJumpMiniGameRules.SessionSeconds)}초",
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

            var legacyJumpButton = playArea.Find("Bouncy Jump Input Button");
            if (legacyJumpButton != null)
            {
                legacyJumpButton.SetParent(card.transform, false);
            }

            var jumpButton = GetOrCreateTopLeftButton(
                card.transform,
                "Bouncy Jump Input Button",
                "점프!",
                new Vector2(362f, -646f),
                new Vector2(216f, 60f));
            ApplyCareButtonStyle(jumpButton);

            var result = GetOrCreateText(
                card.transform,
                "Bouncy Jump Result Text",
                "빛나는 착지 구역과 겹칠 때 점프하세요!",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(58f, -556f),
                new Vector2(824f, 82f));
            result.horizontalOverflow = HorizontalWrapMode.Wrap;
            result.resizeTextForBestFit = true;
            result.resizeTextMinSize = 14;
            result.resizeTextMaxSize = 18;
            var cancel = GetOrCreateTopLeftButton(
                card.transform,
                "Bouncy Jump Cancel Button",
                "그만하기",
                new Vector2(718f, -646f),
                new Vector2(164f, 60f));
            var confirm = GetOrCreateTopLeftButton(
                card.transform,
                "Bouncy Jump Confirm Button",
                "확인",
                new Vector2(718f, -646f),
                new Vector2(164f, 60f));
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

        private static void EnsureBlueBallMiniGame(
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
                BlueBallMiniGameController.OverlayObjectName,
                new Color(0.035f, 0.08f, 0.17f, 0.82f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Blue Ball Mini Game Card",
                Vector2.zero,
                new Vector2(940f, 730f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(940f, 730f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.9f, 0.96f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Blue Ball Title Text",
                "파란 공 놀이",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(44f, -24f),
                new Vector2(852f, 50f));
            title.fontStyle = FontStyle.Bold;
            var time = GetOrCreateText(
                card.transform,
                "Blue Ball Time Text",
                $"남은 시간  {Mathf.CeilToInt(BlueBallMiniGameRules.SessionSeconds)}초",
                20,
                TextAnchor.MiddleLeft,
                new Vector2(58f, -82f),
                new Vector2(250f, 36f));
            var score = GetOrCreateText(
                card.transform,
                "Blue Ball Score Text",
                "점수  0 · 성공 0 · 놓침 0",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(270f, -82f),
                new Vector2(390f, 36f));
            var combo = GetOrCreateText(
                card.transform,
                "Blue Ball Combo Text",
                "콤보  -",
                20,
                TextAnchor.MiddleRight,
                new Vector2(674f, -82f),
                new Vector2(208f, 36f));

            var playAreaObject = GetOrCreatePanel(
                card.transform,
                "Blue Ball Play Area",
                new Vector2(58f, -132f),
                new Vector2(824f, 414f));
            var playArea = playAreaObject.GetComponent<RectTransform>();
            if (playAreaObject.TryGetComponent(out Image playAreaImage))
            {
                playAreaImage.color = new Color(0.55f, 0.78f, 1f, 0.52f);
                playAreaImage.raycastTarget = true;
            }

            if (playAreaObject.GetComponent<RectMask2D>() == null)
            {
                playAreaObject.AddComponent<RectMask2D>();
            }

            var ball = GetOrCreateButton(
                playArea,
                "Blue Ball Input Button",
                "●",
                Vector2.zero,
                new Vector2(78f, 78f));
            var ballRect = ball.GetComponent<RectTransform>();
            ballRect.anchorMin = new Vector2(0.5f, 0.5f);
            ballRect.anchorMax = new Vector2(0.5f, 0.5f);
            ballRect.pivot = new Vector2(0.5f, 0.5f);
            ballRect.sizeDelta = new Vector2(78f, 78f);
            if (ball.TryGetComponent(out Image ballImage))
            {
                ballImage.color = new Color(0.16f, 0.43f, 0.94f, 1f);
                ApplyCircleImage(ballImage);
            }

            var ballLabel = ball.transform.Find("Label")?.GetComponent<Text>();
            if (ballLabel != null)
            {
                ballLabel.color = Color.white;
                ballLabel.fontSize = 38;
            }

            var result = GetOrCreateText(
                card.transform,
                "Blue Ball Result Text",
                "파란 공을 빠르게 눌러 콤보를 이어 보세요!",
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
                "Blue Ball Cancel Button",
                "그만하기",
                new Vector2(718f, -594f),
                new Vector2(164f, 52f));
            var confirm = GetOrCreateTopLeftButton(
                card.transform,
                "Blue Ball Confirm Button",
                "확인",
                new Vector2(718f, -594f),
                new Vector2(164f, 52f));
            ApplyCareButtonStyle(cancel);
            ApplyCareButtonStyle(confirm);

            var controller = canvasTransform.GetComponent<BlueBallMiniGameController>()
                ?? canvasTransform.gameObject.AddComponent<BlueBallMiniGameController>();
            var manager = Application.isPlaying ? GameManager.Instance : null;
            controller.Configure(
                overlay,
                playArea,
                ball,
                time,
                score,
                combo,
                result,
                cancel,
                confirm,
                milkroomUi,
                visualController,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>(),
                manager != null ? manager.CompleteBlueBallMiniGame : null);
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
                new Vector2(820f, 450f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(820f, 450f));
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
                new Vector2(740f, 50f));
            title.fontStyle = FontStyle.Bold;
            var status = GetOrCreateText(
                card.transform,
                "Play Choice Status Text",
                "놀이를 선택해 주세요.",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(60f, -94f),
                new Vector2(700f, 66f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            var milkDrop = GetOrCreateTopLeftButton(
                card.transform,
                "Play Choice Milk Drop Button",
                $"우유방울 받기\n{Mathf.CeilToInt(MilkDropMiniGameRules.DurationSeconds)}초 반응 게임",
                new Vector2(42f, -184f),
                new Vector2(232f, 108f));
            var bouncy = GetOrCreateTopLeftButton(
                card.transform,
                "Play Choice Bouncy Jump Button",
                $"말랑 점프\n{Mathf.CeilToInt(BouncyJumpMiniGameRules.SessionSeconds)}초 타이밍 게임",
                new Vector2(294f, -184f),
                new Vector2(232f, 108f));
            var blueBall = GetOrCreateTopLeftButton(
                card.transform,
                "Play Choice Blue Ball Button",
                $"파란 공 놀이\n{Mathf.CeilToInt(BlueBallMiniGameRules.SessionSeconds)}초 콤보 게임",
                new Vector2(546f, -184f),
                new Vector2(232f, 108f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Play Choice Close Button",
                "닫기",
                new Vector2(322f, -350f),
                new Vector2(176f, 50f));
            ApplyCareButtonStyle(milkDrop);
            ApplyCareButtonStyle(bouncy);
            ApplyCareButtonStyle(blueBall);
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
                blueBall,
                close,
                canvasTransform.GetComponent<MilkDropMiniGameController>(),
                canvasTransform.GetComponent<BouncyJumpMiniGameController>(),
                canvasTransform.GetComponent<BlueBallMiniGameController>(),
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
            var timeText = GetOrCreateText(
                card.transform,
                "Cleaning Time Text",
                $"남은 시간  {Mathf.CeilToInt(CleaningMiniGameRules.DurationSeconds)}초",
                20,
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
                "얼룩 위에서 손가락이나 마우스를 좌우로 문질러 주세요.", 18, TextAnchor.MiddleCenter,
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
    }
}
