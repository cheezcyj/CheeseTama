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
            RemoveChildIfExists(recordsTransform, "Event Records Tab Button");
            RemoveChildIfExists(recordsTransform, "Event Records Text");
            var milkTabButton = GetOrCreateTopLeftButton(recordsTransform, "Milk Records Tab Button", "발견", new Vector2(18, -18), new Vector2(212, 38));
            var evolutionTabButton = GetOrCreateTopLeftButton(recordsTransform, "Evolution Records Tab Button", "진화", new Vector2(240, -18), new Vector2(212, 38));
            var hiddenTabButton = GetOrCreateTopLeftButton(recordsTransform, "Hidden Records Tab Button", "특별", new Vector2(462, -18), new Vector2(212, 38));
            ApplyCollectionTabButtonStyle(milkTabButton, evolutionTabButton, hiddenTabButton);

            var milkText = GetOrCreateText(recordsTransform, "Milk Records Text", "발견 기록: 0", 16, TextAnchor.UpperLeft, new Vector2(18, -72), new Vector2(646, 350));
            var evolutionText = GetOrCreateText(recordsTransform, "Evolution Records Text", "진화 기록: 0", 16, TextAnchor.UpperLeft, new Vector2(18, -72), new Vector2(646, 350));
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
                null,
                hiddenText,
                messageText,
                milkTabButton,
                evolutionTabButton,
                null,
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
            var stateText = GetOrCreateText(overlayTransform, "Decorate Overlay State Text", "지금 방: 따뜻한 아침 · 별방울 0개", 15, TextAnchor.UpperLeft, new Vector2(28, -72), new Vector2(540, 38));
            ConfigureMultiLineFeedbackText(stateText, 13, 15);
            closeButton = GetOrCreateTopLeftButton(overlayTransform, "Close Decorate Button", "닫기", new Vector2(596, -20), new Vector2(116, 40));

            var previewPanel = GetOrCreatePanel(overlayTransform, "Decorate Preview Panel", new Vector2(24, -122), new Vector2(692, 432));
            if (previewPanel.TryGetComponent(out Image previewImage))
            {
                previewImage.color = new Color(1f, 0.94f, 0.78f, 0.42f);
            }

            var previewTransform = previewPanel.transform;
            var themeText = GetOrCreateText(previewTransform, "Decorate Theme Text", "따뜻한 아침 밀크룸", 18, TextAnchor.UpperLeft, new Vector2(22, -22), new Vector2(420, 32));
            var detailText = GetOrCreateText(previewTransform, "Decorate Theme Detail Text", "크림색 벽과 나무 의자에 포근한 아침빛이 들어와요.", 14, TextAnchor.UpperLeft, new Vector2(22, -60), new Vector2(620, 52));
            var lightingLabel = GetOrCreateText(previewTransform, "Decorate Slot A Text", "빛", 16, TextAnchor.UpperLeft, new Vector2(22, -122), new Vector2(100, 42));
            var lightingText = GetOrCreateText(previewTransform, "Decorate Slot A Value Text", "눈이 편안한 따뜻한 햇살", 14, TextAnchor.UpperLeft, new Vector2(122, -122), new Vector2(520, 42));
            var furnitureLabel = GetOrCreateText(previewTransform, "Decorate Slot B Text", "가구", 16, TextAnchor.UpperLeft, new Vector2(22, -174), new Vector2(100, 42));
            var furnitureText = GetOrCreateText(previewTransform, "Decorate Slot B Value Text", "의자와 냉장고는 그대로 두고 방 색만 바꿔요.", 14, TextAnchor.UpperLeft, new Vector2(122, -174), new Vector2(520, 42));
            var propsLabel = GetOrCreateText(previewTransform, "Decorate Slot C Text", "소품", 16, TextAnchor.UpperLeft, new Vector2(22, -226), new Vector2(100, 42));
            var propsText = GetOrCreateText(previewTransform, "Decorate Slot C Value Text", "놓아 둔 소품은 사라지지 않아요.", 14, TextAnchor.UpperLeft, new Vector2(122, -226), new Vector2(520, 42));
            foreach (var description in new[]
                     {
                         detailText, lightingLabel, lightingText,
                         furnitureLabel, furnitureText, propsLabel, propsText
                     })
            {
                ConfigureMultiLineFeedbackText(description, 12, description.fontSize);
            }

            var decorateHelp = GetOrCreateText(previewTransform, "Decorate Help Text", "별빛 방은 별빛 우유 이야기를 찾은 뒤, 별방울로 열 수 있어요. 한 번 열면 계속 쓸 수 있어요.", 14, TextAnchor.UpperLeft, new Vector2(22, -386), new Vector2(650, 42));
            ConfigureMultiLineFeedbackText(decorateHelp, 12, 14);

            var morningButton = GetOrCreateTopLeftButton(previewTransform, "Morning Theme Button", "아침", new Vector2(22, -280), new Vector2(148, 48));
            var eveningButton = GetOrCreateTopLeftButton(previewTransform, "Evening Theme Button", "오후", new Vector2(184, -280), new Vector2(148, 48));
            var nightButton = GetOrCreateTopLeftButton(previewTransform, "Night Theme Button", "밤", new Vector2(346, -280), new Vector2(148, 48));
            var rainyButton = GetOrCreateTopLeftButton(previewTransform, "Rainy Theme Button", "비", new Vector2(508, -280), new Vector2(148, 48));
            ApplyCollectionTabButtonStyle(morningButton, eveningButton, nightButton, rainyButton);
            var starlightButton = GetOrCreateTopLeftButton(previewTransform, "Starlight Theme Button", "별빛 · 별방울 3", new Vector2(22, -334), new Vector2(202, 48));
            var winterButton = GetOrCreateTopLeftButton(previewTransform, "Winter Theme Button", "겨울 · 별방울 2", new Vector2(238, -334), new Vector2(202, 48));
            var vintageButton = GetOrCreateTopLeftButton(previewTransform, "Vintage Theme Button", "빈티지 · 별방울 4", new Vector2(454, -334), new Vector2(202, 48));
            ApplyCollectionTabButtonStyle(starlightButton, winterButton, vintageButton);

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
                rainyButton,
                starlightButton,
                winterButton,
                vintageButton);

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
            basicMilkGrowthText = GetOrCreateText(listTransform, "Basic Milk Growth Text", "<b>구하기 쉬운 정도</b>  쉽게 만나요", 15, TextAnchor.UpperLeft, new Vector2(22, -54), new Vector2(580, 24));
            starMilkGrowthText = GetOrCreateText(listTransform, "Star Milk Growth Text", "<b>설명</b>\n<size=4> </size>\n우유를 선택해 주세요.\n\n<b>도움</b>  배부름 +25", 14, TextAnchor.UpperLeft, new Vector2(22, -86), new Vector2(580, 118));
            unlockText = GetOrCreateText(listTransform, "Unlock Text", "<b>사용 가능</b>  지금 바로 줄 수 있어요.", 15, TextAnchor.UpperLeft, new Vector2(22, -216), new Vector2(580, 30));
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

            var tipText = GetOrCreateText(panelTransform, "Milk Panel Tip Text", "버튼을 누르거나 우유병을 성장 기록 카드로 끌어 주세요.", 14, TextAnchor.UpperLeft, new Vector2(28, -544), new Vector2(440, 24));
            tipText.color = new Color(0.38f, 0.28f, 0.17f);

            var milkController = canvasTransform.GetComponent<MilkPanelController>();
            if (milkController == null)
            {
                milkController = canvasTransform.gameObject.AddComponent<MilkPanelController>();
            }

            for (var i = 0; i < feedButtons.Length; i++)
            {
                ConfigureCareButton(feedButtons[i], GetMilkCareAction(milks[i].id), controller, visualController, milkController);
                var feedButton = feedButtons[i];
                var dragTarget = feedButton.GetComponent<DirectManipulationInputTarget>()
                    ?? feedButton.gameObject.AddComponent<DirectManipulationInputTarget>();
                dragTarget.ConfigureMilkBottleDrag(
                    listPanel.GetComponent<RectTransform>(),
                    () =>
                    {
                        if (feedButton.isActiveAndEnabled && feedButton.interactable)
                        {
                            feedButton.onClick.Invoke();
                        }
                    },
                    progress => tipText.text = progress.IsActive
                        ? $"우유병 이동  {Mathf.RoundToInt(progress.NormalizedProgress * 100f)}%"
                        : "버튼을 누르거나 우유병을 성장 기록 카드로 끌어 주세요.",
                    () => panel.activeInHierarchy
                        && feedButton.isActiveAndEnabled
                        && feedButton.interactable);
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
            GetOrCreateText(
                panelTransform,
                "Cooking Panel Header Text",
                "요리",
                24,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -24f),
                new Vector2(440f, 36f),
                true);
            GetOrCreateText(
                panelTransform,
                "Cooking Panel Help Text",
                "재료를 고르고 CheeseTama에게 줄 작은 요리를 만듭니다.",
                14,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -64f),
                new Vector2(560f, 28f),
                true);
            ConfigureCookingPanelHeaderAlignment(panelTransform);

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

        private static void ConfigureCookingPanelHeaderAlignment(Transform panelTransform)
        {
            if (panelTransform == null)
            {
                return;
            }

            ConfigureCenteredCookingHeaderText(
                panelTransform.Find("Cooking Panel Header Text")?.GetComponent<Text>(),
                -24f,
                new Vector2(440f, 36f));
            ConfigureCenteredCookingHeaderText(
                panelTransform.Find("Cooking Panel Help Text")?.GetComponent<Text>(),
                -64f,
                new Vector2(560f, 28f));
        }

        private static void ConfigureCenteredCookingHeaderText(
            Text label,
            float anchoredY,
            Vector2 size)
        {
            if (label == null)
            {
                return;
            }

            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, anchoredY);
            rect.sizeDelta = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }

        private static void ConfigureTopCenteredCookingChoiceRect(
            RectTransform rect,
            float anchoredY,
            Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, anchoredY);
            rect.sizeDelta = size;
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
                detailTexts[i] = GetOrCreateText(rowTransform, $"Snack Row Detail Text {i}", $"{snack.description}\n도움: 배부름 +0", 13, TextAnchor.UpperLeft, new Vector2(18, -48), new Vector2(404, 56));
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

            for (var i = snacks.Length; i < 12; i++)
            {
                RemoveChildIfExists(scrollContent, $"Snack Inventory Row {i}");
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
    }
}
