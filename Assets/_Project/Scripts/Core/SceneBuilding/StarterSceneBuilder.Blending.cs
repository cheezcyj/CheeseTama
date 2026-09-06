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
                "우유 섞기 실험",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(42f, -26f),
                new Vector2(430f, 48f));
            title.fontStyle = FontStyle.Bold;
            var balance = GetOrCreateText(
                card.transform,
                "Milk Blending Balance Text",
                "코인 0 · 우유방울 0 · 도감 조각 0",
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
                    new Vector2(214f, 60f));
                ApplyCareButtonStyle(button);
                var nameLabel = button.transform.Find("Label")?.GetComponent<Text>();
                if (nameLabel != null)
                {
                    nameLabel.alignment = TextAnchor.MiddleLeft;
                    nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
                    nameLabel.rectTransform.anchorMin = new Vector2(0f, 0.46f);
                    nameLabel.rectTransform.anchorMax = Vector2.one;
                    nameLabel.rectTransform.offsetMin = new Vector2(14f, 2f);
                    nameLabel.rectTransform.offsetMax = new Vector2(-14f, -2f);
                }

                var stateLabel = GetOrCreateText(
                    button.transform,
                    "Option State Text",
                    "사용 가능",
                    12,
                    TextAnchor.MiddleLeft,
                    Vector2.zero,
                    Vector2.zero);
                stateLabel.rectTransform.anchorMin = Vector2.zero;
                stateLabel.rectTransform.anchorMax = new Vector2(1f, 0.46f);
                stateLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                stateLabel.rectTransform.offsetMin = new Vector2(14f, 3f);
                stateLabel.rectTransform.offsetMax = new Vector2(-14f, -1f);
                stateLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                stateLabel.verticalOverflow = VerticalWrapMode.Truncate;
                stateLabel.resizeTextForBestFit = true;
                stateLabel.resizeTextMinSize = 10;
                stateLabel.resizeTextMaxSize = 12;
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
                    new Vector2(214f, 60f));
                ApplyCareButtonStyle(button);
                var nameLabel = button.transform.Find("Label")?.GetComponent<Text>();
                if (nameLabel != null)
                {
                    nameLabel.alignment = TextAnchor.MiddleLeft;
                    nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
                    nameLabel.rectTransform.anchorMin = new Vector2(0f, 0.46f);
                    nameLabel.rectTransform.anchorMax = Vector2.one;
                    nameLabel.rectTransform.offsetMin = new Vector2(14f, 2f);
                    nameLabel.rectTransform.offsetMax = new Vector2(-14f, -2f);
                }

                var stateLabel = GetOrCreateText(
                    button.transform,
                    "Option State Text",
                    "사용 0회",
                    12,
                    TextAnchor.MiddleLeft,
                    Vector2.zero,
                    Vector2.zero);
                stateLabel.rectTransform.anchorMin = Vector2.zero;
                stateLabel.rectTransform.anchorMax = new Vector2(1f, 0.46f);
                stateLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                stateLabel.rectTransform.offsetMin = new Vector2(14f, 3f);
                stateLabel.rectTransform.offsetMax = new Vector2(-14f, -1f);
                stateLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                stateLabel.verticalOverflow = VerticalWrapMode.Truncate;
                stateLabel.resizeTextForBestFit = true;
                stateLabel.resizeTextMinSize = 10;
                stateLabel.resizeTextMaxSize = 12;
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
                16,
                TextAnchor.UpperLeft,
                new Vector2(20f, -16f),
                new Vector2(344f, 182f));
            detail.supportRichText = true;
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            detail.verticalOverflow = VerticalWrapMode.Truncate;
            detail.resizeTextForBestFit = true;
            detail.resizeTextMinSize = 13;
            detail.resizeTextMaxSize = 16;
            RemoveChildIfExists(detailPanel.transform, "Milk Blending Result Text");

            var detailDivider = GetOrCreatePanel(
                detailPanel.transform,
                "Milk Blending Detail Divider",
                new Vector2(374f, -18f),
                new Vector2(2f, 178f));
            if (detailDivider.TryGetComponent(out Image detailDividerImage))
            {
                detailDividerImage.color = new Color(0.61f, 0.43f, 0.22f, 0.18f);
                detailDividerImage.raycastTarget = false;
            }

            var stirDivider = GetOrCreatePanel(
                detailPanel.transform,
                "Milk Blending Stir Divider",
                new Vector2(798f, -18f),
                new Vector2(2f, 178f));
            if (stirDivider.TryGetComponent(out Image stirDividerImage))
            {
                stirDividerImage.color = new Color(0.61f, 0.43f, 0.22f, 0.18f);
                stirDividerImage.raycastTarget = false;
            }

            Text CreateResultSummaryText(
                string objectName,
                string initialText,
                Vector2 position,
                Vector2 size)
            {
                var label = GetOrCreateText(
                    detailPanel.transform,
                    objectName,
                    initialText,
                    16,
                    TextAnchor.MiddleCenter,
                    position,
                    size);
                label.supportRichText = true;
                label.fontStyle = FontStyle.Normal;
                label.color = new Color(0.28f, 0.18f, 0.08f);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 16;
                label.lineSpacing = 0.92f;
                return label;
            }

            var discoveredResultText = CreateResultSummaryText(
                "Milk Blending Discovered Result Text",
                "<b>발견한 결과</b>\n???",
                new Vector2(388f, -20f),
                new Vector2(396f, 44f));
            var requiredCostText = CreateResultSummaryText(
                "Milk Blending Required Cost Text",
                "<b>필요한 것</b>\n선택해 주세요",
                new Vector2(388f, -72f),
                new Vector2(396f, 44f));
            var blendCountText = CreateResultSummaryText(
                "Milk Blending Count Text",
                "<b>섞은 횟수</b>\n0회",
                new Vector2(388f, -124f),
                new Vector2(192f, 58f));
            var specialResultText = CreateResultSummaryText(
                "Milk Blending Special Result Text",
                "<b>특별 결과</b>\n없음",
                new Vector2(592f, -124f),
                new Vector2(192f, 58f));

            var stirCup = GetOrCreatePanel(
                detailPanel.transform,
                "Milk Blending Stir Cup",
                new Vector2(812f, -7f),
                new Vector2(200f, 200f));
            if (stirCup.TryGetComponent(out Image stirCupImage))
            {
                ApplyCircleImage(stirCupImage);
                stirCupImage.color = new Color(1f, 0.98f, 0.89f, 0.9f);
                stirCupImage.raycastTarget = true;
            }

            var stirTrack = GetOrCreatePanel(
                stirCup.transform,
                "Milk Blending Stir Track",
                new Vector2(8f, -8f),
                new Vector2(184f, 184f));
            if (stirTrack.TryGetComponent(out Image stirTrackImage))
            {
                ApplyRingImage(stirTrackImage);
                stirTrackImage.color = new Color(0.61f, 0.43f, 0.22f, 0.3f);
                stirTrackImage.raycastTarget = false;
            }

            var stirProgress = GetOrCreatePanel(
                stirCup.transform,
                "Milk Blending Stir Progress",
                new Vector2(8f, -8f),
                new Vector2(184f, 184f));
            var stirProgressImage = stirProgress.GetComponent<Image>();
            if (stirProgressImage != null)
            {
                ApplyRingImage(stirProgressImage);
                stirProgressImage.type = Image.Type.Filled;
                stirProgressImage.fillMethod = Image.FillMethod.Radial360;
                stirProgressImage.fillOrigin = (int)Image.Origin360.Top;
                stirProgressImage.fillClockwise = true;
                stirProgressImage.fillAmount = 0f;
                stirProgressImage.color = new Color(0.96f, 0.52f, 0.10f, 0.52f);
                stirProgressImage.raycastTarget = false;
            }

            RemoveChildIfExists(stirCup.transform, "Milk Blending Stir Center");
            RemoveChildIfExists(stirCup.transform, "Milk Blending Stir Bowl");
            RemoveChildIfExists(stirCup.transform, "Milk Blending Stir Bowl Inner");
            RemoveChildIfExists(stirCup.transform, "Milk Blending Stir Spoon");
            var stirArtwork = GetOrCreatePanel(
                stirCup.transform,
                "Milk Blending Stir Artwork",
                new Vector2(26f, -6f),
                new Vector2(148f, 144f));
            if (stirArtwork.TryGetComponent(out Image stirArtworkImage))
            {
                stirArtworkImage.sprite = Resources.Load<Sprite>(
                    "UI/MiniGames/milk_mixing_bowl_v001");
                stirArtworkImage.type = Image.Type.Simple;
                stirArtworkImage.preserveAspect = true;
                stirArtworkImage.color = Color.white;
                stirArtworkImage.raycastTarget = false;
            }

            var stirCupLabel = GetOrCreateText(
                stirCup.transform,
                "Milk Blending Stir Cup Label",
                $"손가락으로 원을 그려요\n완료 보상 +{MilkBlendingSystem.ManualStirMilkCoinReward} 코인",
                15,
                TextAnchor.MiddleCenter,
                new Vector2(10f, -152f),
                new Vector2(180f, 42f));
            stirCupLabel.fontStyle = FontStyle.Bold;
            stirCupLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            stirCupLabel.verticalOverflow = VerticalWrapMode.Truncate;
            stirCupLabel.lineSpacing = 0.9f;
            stirCupLabel.raycastTarget = false;
            stirTrack.transform.SetAsFirstSibling();
            stirProgress.transform.SetSiblingIndex(1);
            stirArtwork.transform.SetSiblingIndex(2);
            stirCupLabel.transform.SetAsLastSibling();
            var status = GetOrCreateText(
                card.transform,
                "Milk Blending Status Text",
                string.Empty,
                15,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -660f),
                new Vector2(780f, 52f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.verticalOverflow = VerticalWrapMode.Truncate;
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 13;
            status.resizeTextMaxSize = 15;
            var blend = GetOrCreateTopLeftButton(
                card.transform,
                "Execute Milk Blending Button",
                "바로 섞기",
                new Vector2(884f, -660f),
                new Vector2(194f, 54f));
            ApplyCareButtonStyle(blend);

            MilkBlendResult ExecuteBlend(
                string milkId,
                string ingredientId,
                bool manualStirCompleted)
            {
                var manager = GameManager.Instance;
                var result = manager?.TryBlendMilk(
                    milkId,
                    ingredientId,
                    manualStirCompleted);
                if (result != null && result.applied)
                {
                    milkroomUi?.Bind(manager.CurrentSave);
                    visualController?.Bind(manager.CurrentTama);
                    visualController?.ReactAction(CheeseTamaVisualAction.Cook);
                }

                return result;
            }

            var panelController = canvasTransform.GetComponent<MilkBlendingPanelController>()
                ?? canvasTransform.gameObject.AddComponent<MilkBlendingPanelController>();
            panelController.Configure(
                overlay,
                balance,
                detail,
                null,
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
                (milkId, ingredientId) => ExecuteBlend(milkId, ingredientId, false),
                null,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>(),
                (milkId, ingredientId) => ExecuteBlend(milkId, ingredientId, true),
                stirProgressImage,
                stirCupLabel,
                discoveredResultText,
                requiredCostText,
                blendCountText,
                specialResultText);
            var stirTarget = stirCup.GetComponent<DirectManipulationInputTarget>()
                ?? stirCup.AddComponent<DirectManipulationInputTarget>();
            stirTarget.ConfigureCircularStir(
                stirCup.GetComponent<RectTransform>(),
                panelController.BlendSelectedFromStir,
                panelController.HandleStirProgress,
                () => panelController.IsOpen,
                requiredDegrees: 300f,
                minimumRadiusPixels: 22f,
                maximumRadiusPixels: 78f);
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
                new Vector2(680f, 500f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(680f, 500f));
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
                new Vector2(0f, -34f),
                new Vector2(456f, 48f),
                true);
            title.fontStyle = FontStyle.Bold;
            var closeButton = GetOrCreateTopLeftButton(
                card.transform,
                "Cooking Choice Close Button",
                "닫기",
                new Vector2(0f, -430f),
                new Vector2(176f, 50f));
            ConfigureTopCenteredCookingChoiceRect(
                closeButton.GetComponent<RectTransform>(),
                -430f,
                new Vector2(176f, 50f));
            var help = GetOrCreateText(
                card.transform,
                "Cooking Choice Help Text",
                "만들 방법을 골라 주세요.",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -92f),
                new Vector2(520f, 38f),
                true);
            ConfigureCenteredCookingHeaderText(title, -34f, new Vector2(456f, 48f));
            ConfigureCenteredCookingHeaderText(help, -92f, new Vector2(520f, 38f));

            var cookingButton = GetOrCreateTopLeftButton(
                card.transform,
                "Cooking Choice Cooking Button",
                "요리하기",
                new Vector2(64f, -150f),
                new Vector2(552f, 96f));
            var milkBlendingButton = GetOrCreateTopLeftButton(
                card.transform,
                "Cooking Choice Milk Blending Button",
                "<size=21>우유 섞기</size>\n<size=14>(가끔 특별한 음식이 나와요)</size>",
                new Vector2(64f, -266f),
                new Vector2(552f, 96f));
            ApplyCareButtonStyle(cookingButton);
            ApplyCareButtonStyle(milkBlendingButton);
            ApplyCareButtonStyle(closeButton);
            var closeLabel = closeButton.transform.Find("Label")?.GetComponent<Text>();
            if (closeLabel != null)
            {
                closeLabel.alignment = TextAnchor.MiddleCenter;
                closeLabel.fontSize = 16;
                closeLabel.resizeTextMinSize = 13;
                closeLabel.resizeTextMaxSize = 16;
            }
            cookingButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = closeButton,
                selectOnDown = milkBlendingButton
            };
            milkBlendingButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = cookingButton,
                selectOnDown = closeButton
            };
            closeButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = milkBlendingButton,
                selectOnDown = cookingButton
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
                canvasTransform.GetComponent<DevPanelController>(),
                closeButton);
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureDirectRestAction(
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
                    MilkroomCareAction.Rest,
                    milkroomUi,
                    visualController);
                SetButtonLabel(sleepButton, "휴식하기");
                ApplyCareButtonStyle(sleepButton);
                SetButtonIcon(sleepButton, "rest");
            }

            // Retire the former scheduled-sleep surface without deleting its
            // persisted history contract. Existing scenes are rebound through
            // this method, so legacy UI/components are cleaned up idempotently.
            RemoveChildIfExists(
                canvasTransform,
                SleepSchedulePanelController.OverlayObjectName);
            RemoveComponentIfExists<SleepScheduleBridge>(canvasTransform.gameObject);
            RemoveComponentIfExists<SleepSchedulePanelController>(canvasTransform.gameObject);
        }
    }
}
