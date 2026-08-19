using System;
using CheeseTama.Collections;
using CheeseTama.Collections.HiddenCareers;
using CheeseTama.Core;
using CheeseTama.Data;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class CollectionUIController : MonoBehaviour
    {
        private const float CardRootMinimumHeight = 446f;
        private const float CardGridWidth = 1300f;
        private const float CardWidth = 640f;
        private const float CardFullWidth = 1300f;
        private const float CardHeight = 104f;
        private const float EmptyCardHeight = 116f;
        private const float CardGap = 20f;
        private const float CardRowGap = 14f;
        private const float CardTopOffset = 8f;
        private const string LegacyClaimAllButtonName = "Claim All Collection Fragments Button";
        private const string MilkClaimAllButtonName = "Claim All Milk Collection Fragments Button";
        private const string EvolutionClaimAllButtonName = "Claim All Evolution Collection Fragments Button";
        private const string EventClaimAllButtonName = "Claim All Event Collection Fragments Button";
        private const string HiddenClaimAllButtonName = "Claim All Hidden Collection Fragments Button";
        private const string RewardNotificationBadgeName = "Collection Reward Notification Badge";

        [SerializeField] private Text milkText;
        [SerializeField] private Text evolutionText;
        [SerializeField] private Text eventText;
        [SerializeField] private Text hiddenText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text recordHeaderText;
        [SerializeField] private Button milkTabButton;
        [SerializeField] private Button evolutionTabButton;
        [SerializeField] private Button eventTabButton;
        [SerializeField] private Button hiddenTabButton;
        [SerializeField] private CheeseTamaGrowthVisualSet growthVisualSet;

        private readonly HiddenCollectionSystem hiddenCollectionSystem = new HiddenCollectionSystem();
        private readonly CollectionSystem collectionSystem = new CollectionSystem();
        private CollectionRecordTab activeTab = CollectionRecordTab.Milk;
        private bool tabsEnabled;
        private bool cardLayoutEnabled;
        private bool runtimeInitialized;
        private RectTransform milkCardRoot;
        private RectTransform evolutionCardRoot;
        private RectTransform eventCardRoot;
        private RectTransform hiddenCardRoot;
        private int milkCardCount;
        private int evolutionCardCount;
        private int eventCardCount;
        private int hiddenCardCount;
        private Button milkClaimAllFragmentsButton;
        private Button evolutionClaimAllFragmentsButton;
        private Button eventClaimAllFragmentsButton;
        private Button hiddenClaimAllFragmentsButton;
        private bool fragmentRewardCapacityAvailable;

        private void OnEnable()
        {
            InitializeRuntimeView(false);
        }

        private void Start()
        {
            InitializeRuntimeView(true);
        }

        private void Update()
        {
            if (Application.isPlaying && milkText != null && !HasCardRoots())
            {
                InitializeRuntimeView(true);
            }
        }

        private void InitializeRuntimeView(bool force)
        {
            if (!Application.isPlaying || milkText == null)
            {
                return;
            }

            if (runtimeInitialized && !force)
            {
                return;
            }

            runtimeInitialized = true;
            EnsureGrowthVisualSet();
            ConfigureTabButtons();
            EnsureCategoryClaimAllFragmentButtons();
            var manager = GameManager.Instance;
            if (manager != null)
            {
                manager.RefreshDerivedCollectionRecords();
                Bind(manager.CurrentSave);
                return;
            }

            EnsureCardRoots();
            ShowTab(activeTab);
        }

        private bool HasCardRoots()
        {
            var content = GetCollectionScrollContent();
            return content != null
                && content.Find("Milk Records Card Root") != null
                && content.Find("Evolution Records Card Root") != null
                && content.Find("Event Records Card Root") != null
                && content.Find("Hidden Records Card Root") != null;
        }

        public void Configure(
            Text milkLabel,
            Text evolutionLabel,
            Text eventLabel,
            Text hiddenLabel,
            Text messageLabel)
        {
            Configure(milkLabel, evolutionLabel, eventLabel, hiddenLabel, messageLabel, null, null, null, null, null);
        }

        public void Configure(
            Text milkLabel,
            Text evolutionLabel,
            Text eventLabel,
            Text hiddenLabel,
            Text messageLabel,
            Button milkTab,
            Button evolutionTab,
            Button eventTab,
            Button hiddenTab)
        {
            Configure(milkLabel, evolutionLabel, eventLabel, hiddenLabel, messageLabel, null, milkTab, evolutionTab, eventTab, hiddenTab);
        }

        public void Configure(
            Text milkLabel,
            Text evolutionLabel,
            Text eventLabel,
            Text hiddenLabel,
            Text messageLabel,
            Text headerLabel,
            Button milkTab,
            Button evolutionTab,
            Button eventTab,
            Button hiddenTab)
        {
            milkText = milkLabel;
            evolutionText = evolutionLabel;
            eventText = eventLabel;
            hiddenText = hiddenLabel;
            messageText = messageLabel;
            recordHeaderText = headerLabel;
            milkTabButton = milkTab;
            evolutionTabButton = evolutionTab;
            eventTabButton = eventTab;
            hiddenTabButton = hiddenTab;
            tabsEnabled = milkTabButton != null
                && evolutionTabButton != null
                && eventTabButton != null
                && hiddenTabButton != null;

            EnsureCardRoots();
            ConfigureTabButtons();
            EnsureCategoryClaimAllFragmentButtons();
            ShowTab(activeTab);
        }

        public void Bind(CheeseTamaSaveData saveData)
        {
            EnsureCategoryClaimAllFragmentButtons();
            if (saveData == null)
            {
                fragmentRewardCapacityAvailable = false;
                SetText(milkText, FormatEmptyRecordList("우유 기록"));
                SetText(evolutionText, FormatEmptyRecordList("진화 기록"));
                SetText(eventText, FormatEmptyRecordList("이벤트 기록"));
                SetText(hiddenText, FormatEmptyRecordList("특별 기록"));
                SetText(messageText, "도감 데이터를 불러오지 못했습니다.");
                RefreshCategoryClaimAllFragmentButtons(null);
                RefreshRewardNotificationBadges(null);
                RefreshCardLayout(null);
                ShowTab(activeTab);
                ApplyCurrentAccessibility();
                return;
            }

            saveData.EnsureRuntimeDefaults();
            var careerBenefits = new HiddenCareerCardSystem().GetBenefitSet(
                saveData.collections);
            fragmentRewardCapacityAvailable = saveData.economy.collectionFragments < int.MaxValue;
            RefreshCategoryClaimAllFragmentButtons(saveData);
            RefreshRewardNotificationBadges(saveData.collections, fragmentRewardCapacityAvailable);
            SetText(milkText, FormatRecordList("우유 기록", saveData.collections.milk, FormatKnownRecordName, careerBenefits));
            SetText(evolutionText, FormatRecordList("진화 기록", saveData.collections.evolution, FormatKnownRecordName, careerBenefits));
            SetText(eventText, FormatRecordList("이벤트 기록", saveData.collections.events, FormatKnownRecordName, careerBenefits));
            SetText(hiddenText, FormatHiddenRecordList(saveData.collections.hiddenUnlockedOnly, careerBenefits));
            SetText(messageText, "발견한 기록만 표시됩니다. 밀크룸에서 돌봄을 이어가면 새 기록이 추가됩니다.");
            RefreshCardLayout(saveData);
            ShowTab(activeTab);
            AccessibilityRuntime.Apply(transform, saveData.settings);
        }

        private void ApplyCurrentAccessibility()
        {
            var labels = transform.GetComponentsInChildren<Text>(true);
            for (var index = 0; index < labels.Length; index += 1)
            {
                AccessibilityRuntime.ApplyCurrent(labels[index]);
            }
        }

        public HiddenCollectionDefinition[] GetVisibleHiddenCards(
            HiddenCollectionDefinition[] definitions,
            CollectionSaveData collections)
        {
            var visible = hiddenCollectionSystem.GetVisibleUnlockedCards(definitions, collections);
            var result = new HiddenCollectionDefinition[visible.Count];
            for (var i = 0; i < visible.Count; i++)
            {
                result[i] = visible[i];
            }

            return result;
        }

        private static string FormatRecordList(
            string title,
            System.Collections.Generic.List<string> records,
            System.Func<string, string> formatter,
            HiddenCareerBenefitSet careerBenefits = default)
        {
            if (records == null || records.Count == 0)
            {
                return FormatEmptyRecordList(title);
            }

            var builder = new StringBuilder();
            AppendHeader(builder, title, records.Count);

            for (var i = 0; i < records.Count; i++)
            {
                var label = formatter != null ? formatter(records[i]) : records[i];
                AppendRecordItem(
                    builder,
                    i + 1,
                    label,
                    careerBenefits.BuildCollectionInterpretation(records[i]));
            }

            return builder.ToString();
        }

        private static string FormatHiddenRecordList(
            System.Collections.Generic.List<HiddenCollectionSaveEntry> records,
            HiddenCareerBenefitSet careerBenefits = default)
        {
            if (records == null || records.Count == 0)
            {
                return FormatEmptyRecordList("특별 기록");
            }

            var builder = new StringBuilder();
            AppendHeader(builder, "특별 기록", records.Count);

            for (var i = 0; i < records.Count; i++)
            {
                var entry = records[i];
                if (entry == null)
                {
                    AppendRecordItem(builder, i + 1, "알 수 없음");
                    continue;
                }

                AppendRecordItem(
                    builder,
                    i + 1,
                    FormatHiddenRecordName(entry.id),
                    AppendDetail(
                        $"획득일 {FormatIso(entry.acquiredAtIso)}",
                        careerBenefits.BuildCollectionInterpretation(entry.id),
                        careerBenefits.BuildDeepLoreSignal(entry.id)));
            }

            return builder.ToString();
        }

        private static string FormatEmptyRecordList(string title)
        {
            return $"<b>{title}</b>  <size=15>0개 발견</size>\n\n<size=16><color=#6E533A>아직 발견한 기록이 없습니다.</color></size>";
        }

        private void RefreshCardLayout(CheeseTamaSaveData saveData)
        {
            EnsureCardRoots();
            if (!cardLayoutEnabled)
            {
                return;
            }

            if (saveData == null || saveData.collections == null)
            {
                milkCardCount = 0;
                evolutionCardCount = 0;
                eventCardCount = 0;
                hiddenCardCount = 0;
                BuildCollectionCards(milkCardRoot, "우유 기록", new List<CollectionCardData>(), "아직 발견한 우유 기록이 없습니다.");
                BuildCollectionCards(evolutionCardRoot, "진화 기록", new List<CollectionCardData>(), "아직 발견한 진화 기록이 없습니다.");
                BuildCollectionCards(eventCardRoot, "이벤트 기록", new List<CollectionCardData>(), "아직 발견한 이벤트 기록이 없습니다.");
                BuildCollectionCards(hiddenCardRoot, "특별 기록", new List<CollectionCardData>(), "아직 발견한 특별 기록이 없습니다.");
                UpdateFixedHeader(activeTab);
                return;
            }

            var collections = saveData.collections;
            var careerBenefits = new HiddenCareerCardSystem().GetBenefitSet(collections);
            var milkCards = CreateRecordCards(
                collections,
                CollectionRecordCategory.Milk,
                collections.milk,
                FormatKnownRecordName,
                FormatKnownRecordDetail,
                careerBenefits);
            var evolutionCards = CreateRecordCards(
                collections,
                CollectionRecordCategory.Evolution,
                collections.evolution,
                FormatKnownRecordName,
                FormatKnownRecordDetail,
                careerBenefits);
            var eventCards = CreateRecordCards(
                collections,
                CollectionRecordCategory.Event,
                collections.events,
                FormatKnownRecordName,
                FormatKnownRecordDetail,
                careerBenefits);
            var hiddenCards = CreateHiddenRecordCards(
                collections,
                collections.hiddenUnlockedOnly,
                careerBenefits);
            milkCardCount = milkCards.Count;
            evolutionCardCount = evolutionCards.Count;
            eventCardCount = eventCards.Count;
            hiddenCardCount = hiddenCards.Count;

            BuildCollectionCards(
                milkCardRoot,
                "우유 기록",
                milkCards,
                "우유를 먹이고 성장시키면 이곳에 기록됩니다.");
            BuildCollectionCards(
                evolutionCardRoot,
                "진화 기록",
                evolutionCards,
                "부화와 성장 기록이 생기면 이곳에 표시됩니다.");
            BuildCollectionCards(
                eventCardRoot,
                "이벤트 기록",
                eventCards,
                "돌봄, 요리, 체류 이벤트가 생기면 이곳에 표시됩니다.");
            BuildCollectionCards(
                hiddenCardRoot,
                "특별 기록",
                hiddenCards,
                "특별 기록은 발견한 카드만 표시됩니다.");
            UpdateFixedHeader(activeTab);
        }

        private void EnsureCardRoots()
        {
            var content = GetCollectionScrollContent();
            if (content == null)
            {
                cardLayoutEnabled = false;
                return;
            }

            milkCardRoot = GetOrCreateCardRoot(content, "Milk Records Card Root");
            evolutionCardRoot = GetOrCreateCardRoot(content, "Evolution Records Card Root");
            eventCardRoot = GetOrCreateCardRoot(content, "Event Records Card Root");
            hiddenCardRoot = GetOrCreateCardRoot(content, "Hidden Records Card Root");
            cardLayoutEnabled = true;
        }

        private RectTransform GetCollectionScrollContent()
        {
            if (milkText == null || milkText.rectTransform == null)
            {
                return null;
            }

            var content = milkText.rectTransform.parent as RectTransform;
            return content != null && content.name == "Collection Scroll Content" ? content : null;
        }

        private static RectTransform GetOrCreateCardRoot(RectTransform content, string name)
        {
            var existing = content.Find(name);
            RectTransform rect;
            if (existing != null && existing.TryGetComponent(out rect))
            {
                ConfigureCardRootRect(rect, CardRootMinimumHeight);
                return rect;
            }

            var rootObject = new GameObject(name);
            rootObject.transform.SetParent(content, false);
            rect = rootObject.AddComponent<RectTransform>();
            ConfigureCardRootRect(rect, CardRootMinimumHeight);
            return rect;
        }

        private static void ConfigureCardRootRect(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, Mathf.Max(CardRootMinimumHeight, height));
        }

        private List<CollectionCardData> CreateRecordCards(
            CollectionSaveData collections,
            CollectionRecordCategory rewardCategory,
            IList<string> records,
            System.Func<string, string> titleFormatter,
            System.Func<string, string> detailFormatter,
            HiddenCareerBenefitSet careerBenefits = default)
        {
            var cards = new List<CollectionCardData>();
            if (records == null)
            {
                return cards;
            }

            for (var i = 0; i < records.Count; i++)
            {
                var id = records[i];
                Sprite thumbnail = null;
                if (CheeseTamaGrowthStageCatalog.TryGetByRecordId(id, out var growthStage))
                {
                    EnsureGrowthVisualSet();
                    thumbnail = growthVisualSet != null
                        ? growthVisualSet.GetThumbnail(growthStage.Stage)
                        : null;
                }

                var detail = detailFormatter != null
                    ? detailFormatter(id)
                    : string.Empty;
                cards.Add(new CollectionCardData(
                    FormatRecordCategory(id),
                    titleFormatter != null ? titleFormatter(id) : id,
                    AppendDetail(
                        detail,
                        careerBenefits.BuildCollectionInterpretation(id)),
                    thumbnail,
                    rewardCategory,
                    id,
                    collectionSystem.IsFragmentRewardClaimed(collections, rewardCategory, id)));
            }

            return cards;
        }

        private List<CollectionCardData> CreateHiddenRecordCards(
            CollectionSaveData collections,
            IList<HiddenCollectionSaveEntry> records,
            HiddenCareerBenefitSet careerBenefits = default)
        {
            var cards = new List<CollectionCardData>();
            if (records == null)
            {
                return cards;
            }

            for (var i = 0; i < records.Count; i++)
            {
                var entry = records[i];
                if (entry == null)
                {
                    cards.Add(new CollectionCardData("특별", "알 수 없음", "획득 정보가 없습니다."));
                    continue;
                }

                cards.Add(new CollectionCardData(
                    "특별",
                    FormatHiddenRecordName(entry.id),
                    AppendDetail(
                        $"획득일 {FormatIso(entry.acquiredAtIso)}",
                        careerBenefits.BuildCollectionInterpretation(entry.id),
                        careerBenefits.BuildDeepLoreSignal(entry.id)),
                    null,
                    CollectionRecordCategory.Hidden,
                    entry.id,
                    collectionSystem.IsFragmentRewardClaimed(
                        collections,
                        CollectionRecordCategory.Hidden,
                        entry.id)));
            }

            return cards;
        }

        private void BuildCollectionCards(
            RectTransform root,
            string title,
            IList<CollectionCardData> cards,
            string emptyMessage)
        {
            if (root == null)
            {
                return;
            }

            ClearChildren(root);

            var totalHeight = CardTopOffset;
            if (cards == null || cards.Count == 0)
            {
                CreateCard(root, 0, new CollectionCardData("대기", "아직 발견한 기록이 없습니다.", emptyMessage), 0f, -CardTopOffset, CardFullWidth, EmptyCardHeight);
                totalHeight += EmptyCardHeight;
            }
            else
            {
                var cardHeight = CalculateCardHeight(cards);
                for (var i = 0; i < cards.Count; i++)
                {
                    var column = i % 2;
                    var row = i / 2;
                    var x = column * (CardWidth + CardGap);
                    var y = -CardTopOffset - (row * (cardHeight + CardRowGap));
                    CreateCard(root, i + 1, cards[i], x, y, CardWidth, cardHeight);
                }

                var rows = Mathf.CeilToInt(cards.Count / 2f);
                totalHeight += rows * cardHeight + Mathf.Max(0, rows - 1) * CardRowGap;
            }

            ConfigureCardRootRect(root, totalHeight + 26f);
        }

        private static float CalculateCardHeight(IList<CollectionCardData> cards)
        {
            var maximumAdditionalLines = 0;
            if (cards != null)
            {
                for (var index = 0; index < cards.Count; index += 1)
                {
                    var detail = cards[index].detail;
                    if (string.IsNullOrEmpty(detail))
                    {
                        continue;
                    }

                    var additionalLines = 0;
                    for (var characterIndex = 0; characterIndex < detail.Length; characterIndex += 1)
                    {
                        if (detail[characterIndex] == '\n')
                        {
                            additionalLines += 1;
                        }
                    }

                    maximumAdditionalLines = Mathf.Max(
                        maximumAdditionalLines,
                        additionalLines);
                }
            }

            return CardHeight + (maximumAdditionalLines * 34f);
        }

        private static void ClearChildren(RectTransform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void CreateCard(
            RectTransform root,
            int index,
            CollectionCardData data,
            float x,
            float y,
            float width,
            float height)
        {
            var cardObject = new GameObject(index > 0 ? $"Collection Record Card {index:00}" : "Collection Empty Card");
            cardObject.transform.SetParent(root, false);
            var rect = cardObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);

            var image = cardObject.AddComponent<Image>();
            image.color = index > 0
                ? new Color(1f, 0.96f, 0.82f, 0.92f)
                : new Color(1f, 0.92f, 0.68f, 0.78f);
            image.raycastTarget = index > 0;

            var outline = cardObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.66f, 0.45f, 0.2f, 0.22f);
            outline.effectDistance = new Vector2(1f, -1f);

            var badgeText = index > 0 ? index.ToString("00") : "--";
            var badge = CreateText(rect, "Index Text", badgeText, 16, TextAnchor.MiddleCenter);
            ConfigureChildTextRect(badge.rectTransform, new Vector2(16f, -14f), new Vector2(42f, 28f));
            badge.color = new Color(0.3f, 0.18f, 0.08f);
            badge.fontStyle = FontStyle.Bold;

            var category = CreateText(rect, "Category Text", string.IsNullOrWhiteSpace(data.category) ? "기록" : data.category, 14, TextAnchor.MiddleLeft);
            ConfigureChildTextRect(category.rectTransform, new Vector2(72f, -14f), new Vector2(118f, 28f));
            category.color = new Color(0.55f, 0.34f, 0.12f);

            var title = CreateText(rect, "Title Text", string.IsNullOrWhiteSpace(data.title) ? "알 수 없음" : data.title, 18, TextAnchor.MiddleLeft);
            ConfigureChildTextRect(title.rectTransform, new Vector2(190f, -13f), new Vector2(width - 214f, 30f));
            title.color = new Color(0.25f, 0.17f, 0.09f);
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 14;
            title.resizeTextMaxSize = 18;

            var detail = CreateText(rect, "Detail Text", string.IsNullOrWhiteSpace(data.detail) ? "발견한 기록입니다." : data.detail, 14, TextAnchor.UpperLeft);
            var detailWidth = data.HasFragmentReward ? width - 302f : width - 100f;
            ConfigureChildTextRect(detail.rectTransform, new Vector2(72f, -50f), new Vector2(detailWidth, height - 58f));
            detail.color = new Color(0.42f, 0.3f, 0.18f);
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            detail.verticalOverflow = VerticalWrapMode.Truncate;
            detail.lineSpacing = 1.08f;

            if (data.thumbnail != null)
            {
                var thumbnailObject = new GameObject("Growth Thumbnail");
                thumbnailObject.transform.SetParent(rect, false);
                var thumbnailRect = thumbnailObject.AddComponent<RectTransform>();
                ConfigureChildTextRect(thumbnailRect, new Vector2(10f, -43f), new Vector2(58f, 58f));
                var thumbnailImage = thumbnailObject.AddComponent<Image>();
                thumbnailImage.sprite = data.thumbnail;
                thumbnailImage.preserveAspect = true;
                thumbnailImage.raycastTarget = false;
            }

            if (data.HasFragmentReward)
            {
                CreateFragmentRewardButton(rect, data, width);
            }

            if (index > 0)
            {
                var detailsTarget = cardObject.AddComponent<ItemDetailsInputTarget>();
                detailsTarget.Configure(_ => ShowCardDetails(data));
            }
        }

        private void ShowCardDetails(CollectionCardData data)
        {
            if (messageText == null)
            {
                return;
            }

            var category = string.IsNullOrWhiteSpace(data.category) ? "기록" : data.category;
            var title = string.IsNullOrWhiteSpace(data.title) ? "알 수 없음" : data.title;
            var detail = string.IsNullOrWhiteSpace(data.detail) ? "발견한 기록입니다." : data.detail;
            AccessibilityRuntime.SetTextAndApply(
                messageText,
                $"{category} · {title}\n{detail}");
        }

        private void CreateFragmentRewardButton(
            RectTransform parent,
            CollectionCardData data,
            float cardWidth)
        {
            var buttonObject = new GameObject("Collection Fragment Claim Button");
            buttonObject.transform.SetParent(parent, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            ConfigureChildTextRect(buttonRect, new Vector2(cardWidth - 200f, -57f), new Vector2(186f, 34f));

            var background = buttonObject.AddComponent<Image>();
            background.color = data.fragmentRewardClaimed
                ? new Color(0.78f, 0.72f, 0.62f, 0.9f)
                : new Color(0.96f, 0.67f, 0.18f, 0.96f);
            StarterSceneBuilder.ApplyRoundedImage(background);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.interactable = !data.fragmentRewardClaimed && fragmentRewardCapacityAvailable;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.75f);
            colors.pressedColor = new Color(0.9f, 0.78f, 0.54f);
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.8f);
            button.colors = colors;

            var labelText = data.fragmentRewardClaimed
                ? "도감조각 받기 완료"
                : fragmentRewardCapacityAvailable
                    ? "도감조각 받기"
                    : "도감조각 보관 한도 도달";
            var label = CreateText(
                buttonRect,
                "Label",
                labelText,
                14,
                TextAnchor.MiddleCenter);
            ConfigureChildTextRect(label.rectTransform, Vector2.zero, buttonRect.sizeDelta);
            label.color = data.fragmentRewardClaimed
                ? new Color(0.38f, 0.34f, 0.28f)
                : new Color(0.28f, 0.16f, 0.05f);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 14;

            var rewardCategory = data.rewardCategory;
            var recordId = data.recordId;
            button.onClick.AddListener(() =>
            {
                button.interactable = false;
                ClaimCollectionFragmentReward(button, rewardCategory, recordId);
            });
        }

        private void EnsureCategoryClaimAllFragmentButtons()
        {
            if (recordHeaderText == null || recordHeaderText.transform.parent == null)
            {
                return;
            }

            var parent = recordHeaderText.transform.parent;
            var legacyButton = parent.Find(LegacyClaimAllButtonName);
            var existingMilkButton = parent.Find(MilkClaimAllButtonName);
            if (legacyButton != null && existingMilkButton == null)
            {
                legacyButton.name = MilkClaimAllButtonName;
            }
            else if (legacyButton != null)
            {
                legacyButton.gameObject.SetActive(false);
            }

            milkClaimAllFragmentsButton = EnsureCategoryClaimAllFragmentButton(
                parent,
                MilkClaimAllButtonName,
                CollectionRecordCategory.Milk);
            evolutionClaimAllFragmentsButton = EnsureCategoryClaimAllFragmentButton(
                parent,
                EvolutionClaimAllButtonName,
                CollectionRecordCategory.Evolution);
            eventClaimAllFragmentsButton = EnsureCategoryClaimAllFragmentButton(
                parent,
                EventClaimAllButtonName,
                CollectionRecordCategory.Event);
            hiddenClaimAllFragmentsButton = EnsureCategoryClaimAllFragmentButton(
                parent,
                HiddenClaimAllButtonName,
                CollectionRecordCategory.Hidden);
            UpdateCategoryClaimAllButtonVisibility();
        }

        private Button EnsureCategoryClaimAllFragmentButton(
            Transform parent,
            string buttonName,
            CollectionRecordCategory category)
        {
            var existing = parent.Find(buttonName);
            GameObject buttonObject;
            if (existing != null)
            {
                buttonObject = existing.gameObject;
            }
            else
            {
                buttonObject = new GameObject(buttonName);
                buttonObject.transform.SetParent(parent, false);
            }

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            if (buttonRect == null)
            {
                buttonRect = buttonObject.AddComponent<RectTransform>();
            }

            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-48f, 30f);
            buttonRect.sizeDelta = new Vector2(254f, 40f);

            var background = buttonObject.GetComponent<Image>();
            if (background == null)
            {
                background = buttonObject.AddComponent<Image>();
            }

            background.color = new Color(0.96f, 0.67f, 0.18f, 0.98f);
            StarterSceneBuilder.ApplyRoundedImage(background);

            var button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.75f);
            colors.pressedColor = new Color(0.9f, 0.78f, 0.54f);
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.8f);
            button.colors = colors;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ClaimCategoryCollectionFragmentRewards(category));

            var labelTransform = buttonObject.transform.Find("Label");
            Text label;
            if (labelTransform != null && labelTransform.TryGetComponent(out label))
            {
                ConfigureStretchRect(label.rectTransform);
            }
            else
            {
                label = CreateText(buttonRect, "Label", "도감조각 모두 받기", 14, TextAnchor.MiddleCenter);
                ConfigureStretchRect(label.rectTransform);
            }

            label.color = new Color(0.28f, 0.16f, 0.05f);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 14;
            buttonObject.transform.SetAsLastSibling();
            return button;
        }

        private void ClaimCategoryCollectionFragmentRewards(CollectionRecordCategory category)
        {
            var sourceButton = GetCategoryClaimAllButton(category);
            var manager = GameManager.Instance;
            if (manager == null)
            {
                if (sourceButton != null)
                {
                    sourceButton.interactable = true;
                }

                return;
            }

            if (sourceButton != null)
            {
                sourceButton.interactable = false;
            }

            manager.ClaimCollectionFragmentRewards(category);
            Bind(manager.CurrentSave);
        }

        private void RefreshCategoryClaimAllFragmentButtons(CheeseTamaSaveData saveData)
        {
            EnsureCategoryClaimAllFragmentButtons();
            RefreshCategoryClaimAllFragmentButton(
                milkClaimAllFragmentsButton,
                saveData,
                CollectionRecordCategory.Milk);
            RefreshCategoryClaimAllFragmentButton(
                evolutionClaimAllFragmentsButton,
                saveData,
                CollectionRecordCategory.Evolution);
            RefreshCategoryClaimAllFragmentButton(
                eventClaimAllFragmentsButton,
                saveData,
                CollectionRecordCategory.Event);
            RefreshCategoryClaimAllFragmentButton(
                hiddenClaimAllFragmentsButton,
                saveData,
                CollectionRecordCategory.Hidden);
            UpdateCategoryClaimAllButtonVisibility();
        }

        private void RefreshCategoryClaimAllFragmentButton(
            Button button,
            CheeseTamaSaveData saveData,
            CollectionRecordCategory category)
        {
            if (button == null)
            {
                return;
            }

            var labelTransform = button.transform.Find("Label");
            var label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
            var collections = saveData != null ? saveData.collections : null;
            var unclaimedCount = collectionSystem.CountUnclaimedFragmentRewards(collections, category);
            var remainingCapacity = saveData != null && saveData.economy != null
                ? (long)int.MaxValue - saveData.economy.collectionFragments
                : 0L;
            var claimableCount = remainingCapacity > 0L
                ? (int)System.Math.Min(unclaimedCount, remainingCapacity)
                : 0;
            button.interactable = claimableCount > 0;
            if (label == null)
            {
                return;
            }

            if (claimableCount > 0)
            {
                label.text = $"도감조각 모두 받기 ({claimableCount})";
            }
            else if (unclaimedCount > 0)
            {
                label.text = "도감조각 보관 한도 도달";
            }
            else
            {
                label.text = HasDiscoveredRecord(collections, category)
                    ? "도감조각 모두 받기 완료"
                    : "받을 도감조각 없음";
            }

            label.color = claimableCount > 0
                ? new Color(0.28f, 0.16f, 0.05f)
                : new Color(0.38f, 0.34f, 0.28f);
        }

        private static bool HasDiscoveredRecord(
            CollectionSaveData collections,
            CollectionRecordCategory category)
        {
            if (collections == null)
            {
                return false;
            }

            collections.EnsureRuntimeDefaults();
            if (category == CollectionRecordCategory.Milk)
            {
                return collections.milk.Count > 0;
            }

            if (category == CollectionRecordCategory.Evolution)
            {
                return collections.evolution.Count > 0;
            }

            if (category == CollectionRecordCategory.Event)
            {
                return collections.events.Count > 0;
            }

            if (category != CollectionRecordCategory.Hidden)
            {
                return false;
            }

            foreach (var entry in collections.hiddenUnlockedOnly)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.id))
                {
                    return true;
                }
            }

            return false;
        }

        private Button GetCategoryClaimAllButton(CollectionRecordCategory category)
        {
            if (category == CollectionRecordCategory.Evolution)
            {
                return evolutionClaimAllFragmentsButton;
            }

            if (category == CollectionRecordCategory.Event)
            {
                return eventClaimAllFragmentsButton;
            }

            if (category == CollectionRecordCategory.Hidden)
            {
                return hiddenClaimAllFragmentsButton;
            }

            return milkClaimAllFragmentsButton;
        }

        private void UpdateCategoryClaimAllButtonVisibility()
        {
            SetClaimAllButtonVisible(milkClaimAllFragmentsButton, activeTab == CollectionRecordTab.Milk);
            SetClaimAllButtonVisible(evolutionClaimAllFragmentsButton, activeTab == CollectionRecordTab.Evolution);
            SetClaimAllButtonVisible(eventClaimAllFragmentsButton, activeTab == CollectionRecordTab.Event);
            SetClaimAllButtonVisible(hiddenClaimAllFragmentsButton, activeTab == CollectionRecordTab.Hidden);
        }

        private static void SetClaimAllButtonVisible(Button button, bool visible)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            if (visible)
            {
                button.transform.SetAsLastSibling();
            }
        }

        private void RefreshRewardNotificationBadges(
            CollectionSaveData collections,
            bool hasFragmentCapacity = false)
        {
            SetRewardNotificationBadge(
                milkTabButton,
                hasFragmentCapacity
                    && collectionSystem.CountUnclaimedFragmentRewards(collections, CollectionRecordCategory.Milk) > 0);
            SetRewardNotificationBadge(
                evolutionTabButton,
                hasFragmentCapacity
                    && collectionSystem.CountUnclaimedFragmentRewards(collections, CollectionRecordCategory.Evolution) > 0);
            SetRewardNotificationBadge(
                eventTabButton,
                hasFragmentCapacity
                    && collectionSystem.CountUnclaimedFragmentRewards(collections, CollectionRecordCategory.Event) > 0);
            SetRewardNotificationBadge(
                hiddenTabButton,
                hasFragmentCapacity
                    && collectionSystem.CountUnclaimedFragmentRewards(collections, CollectionRecordCategory.Hidden) > 0);
        }

        private static void SetRewardNotificationBadge(Button button, bool visible)
        {
            if (button == null)
            {
                return;
            }

            var badgeTransform = button.transform.Find(RewardNotificationBadgeName);
            GameObject badgeObject;
            if (badgeTransform != null)
            {
                badgeObject = badgeTransform.gameObject;
            }
            else
            {
                badgeObject = new GameObject(RewardNotificationBadgeName);
                badgeObject.transform.SetParent(button.transform, false);
            }

            var badgeRect = badgeObject.GetComponent<RectTransform>();
            if (badgeRect == null)
            {
                badgeRect = badgeObject.AddComponent<RectTransform>();
            }

            badgeRect.anchorMin = Vector2.one;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = new Vector2(16f, 16f);

            var badgeImage = badgeObject.GetComponent<Image>();
            if (badgeImage == null)
            {
                badgeImage = badgeObject.AddComponent<Image>();
            }

            badgeImage.color = new Color(0.92f, 0.12f, 0.1f, 1f);
            badgeImage.raycastTarget = false;
            StarterSceneBuilder.ApplyCircleImage(badgeImage);

            var outline = badgeObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = badgeObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(1f, 0.92f, 0.76f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            badgeObject.transform.SetAsLastSibling();
            badgeObject.SetActive(visible);
        }

        private void ClaimCollectionFragmentReward(
            Button sourceButton,
            CollectionRecordCategory category,
            string recordId)
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                if (sourceButton != null)
                {
                    sourceButton.interactable = true;
                }

                return;
            }

            manager.TryClaimCollectionFragmentReward(category, recordId);
            Bind(manager.CurrentSave);
        }

        private void EnsureGrowthVisualSet()
        {
            if (growthVisualSet == null)
            {
                growthVisualSet = Resources.Load<CheeseTamaGrowthVisualSet>("CheeseTamaGrowthVisualSet");
            }
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<RectTransform>();
            var label = textObject.AddComponent<Text>();
            label.text = text;
            label.font = KoreanUiFontRuntime.GetDefaultFont();
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.supportRichText = true;
            label.raycastTarget = false;
            return label;
        }

        private static void ConfigureChildTextRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void ConfigureStretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AppendHeader(StringBuilder builder, string title, int count)
        {
            builder
                .Append("<b>")
                .Append(title)
                .Append("</b>  <size=15>")
                .Append(count)
                .Append("개 발견</size>")
                .AppendLine()
                .AppendLine();
        }

        private static void AppendRecordItem(StringBuilder builder, int index, string title, string detail = null)
        {
            if (index > 1)
            {
                builder.AppendLine().AppendLine();
            }

            builder
                .Append("<b>")
                .Append(index.ToString("00"))
                .Append("</b>  ")
                .Append(string.IsNullOrWhiteSpace(title) ? "알 수 없음" : title);

            if (!string.IsNullOrWhiteSpace(detail))
            {
                builder
                    .AppendLine()
                    .Append("<size=15><color=#6E533A>")
                    .Append(detail)
                    .Append("</color></size>");
            }
        }

        private static string AppendDetail(string primary, params string[] additions)
        {
            var builder = new StringBuilder(primary ?? string.Empty);
            if (additions == null)
            {
                return builder.ToString();
            }

            for (var index = 0; index < additions.Length; index += 1)
            {
                if (string.IsNullOrWhiteSpace(additions[index]))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(additions[index]);
            }

            return builder.ToString();
        }

        private static string FormatKnownRecordName(string id)
        {
            var masteryRecord = MilkBlendingCatalog.FindMasteryResearchRecord(id);
            if (masteryRecord != null)
            {
                return masteryRecord.title;
            }

            var seasonalRecord = SeasonalCareEventCatalog.Find(id);
            if (seasonalRecord != null)
            {
                return seasonalRecord.CollectionTitle;
            }

            id = NormalizeKnownRecordId(id);
            if (CheeseTamaGrowthStageCatalog.TryGetByRecordId(id, out var growthStage))
            {
                return growthStage.DisplayName;
            }

            var normalEvolution = EvolutionSystem.FindNormalEvolution(id);
            if (normalEvolution != null)
            {
                return normalEvolution.DisplayName;
            }

            var milkDefinition = MilkCatalog.Find(id);
            if (milkDefinition != null)
            {
                return milkDefinition.displayName;
            }

            foreach (var milk in MilkCatalog.MainMilks)
            {
                if (milk != null && id == $"{milk.id}_unlocked")
                {
                    return $"{milk.displayName} 해금";
                }
            }

            if (id == "basic_milk")
            {
                return "기본 우유";
            }

            if (id == "star_milk")
            {
                return "별빛 우유";
            }

            if (id == "star_milk_unlocked")
            {
                return "별빛 우유 해금";
            }

            if (id == "cheese_snack_fed")
            {
                return "치즈 간식 맛봄";
            }

            if (id == "crumbly_snack")
            {
                return "부스러지는 간식";
            }

            if (id == "care_total_5")
            {
                return "돌봄 5회";
            }

            if (id == "care_total_15")
            {
                return "돌봄 15회";
            }

            if (id == "milk_feeds_5")
            {
                return "우유 5회";
            }

            if (id == "star_milk_feeds_3")
            {
                return "별빛 우유 3회";
            }

            if (id == "snacks_fed_3")
            {
                return "간식 3회";
            }

            if (id == "play_sessions_3")
            {
                return "놀이 3회";
            }

            if (id == "pet_first")
            {
                return "첫 쓰다듬기";
            }

            if (id == "pet_sessions_10")
            {
                return "쓰다듬기 10회";
            }

            if (id == "cleanings_3")
            {
                return "청소 3회";
            }

            if (id == "rests_3")
            {
                return "휴식 3회";
            }

            if (id == "wait_hours_3")
            {
                return "3시간 경과";
            }

            if (id == "daily_routine_complete")
            {
                return "일일 루틴 완료";
            }

            if (id == "daily_routine_3")
            {
                return "일일 루틴 3회";
            }

            if (id == "session_5m")
            {
                return "5분 체류";
            }

            if (id == "session_10m")
            {
                return "10분 체류";
            }

            if (id == "session_20m")
            {
                return "20분 체류";
            }

            if (id == "session_30m")
            {
                return "30분 체류";
            }

            if (id == "daily_presence_10m")
            {
                return "오늘 밀크룸 10분";
            }

            if (id == "daily_presence_30m")
            {
                return "오늘 밀크룸 30분";
            }

            if (id == "milk_drop_catch")
            {
                return "우유 방울 획득";
            }

            if (id == "milk_drop_catch_5")
            {
                return "우유 방울 5회 획득";
            }

            if (id == "milk_drop_catch_10")
            {
                return "우유 방울 10회 획득";
            }

            if (id == "quiet_hum")
            {
                return "조용한 밀크룸 울림";
            }

            if (id == "small_fever")
            {
                return "작은 열기";
            }

            if (id == "hungry_peep")
            {
                return "배고픈 소리";
            }

            if (id == "dusty_corner")
            {
                return "먼지 낀 구석";
            }

            if (id == "sleepy_yawn")
            {
                return "졸린 하품";
            }

            if (id == "happy_wiggle")
            {
                return "기쁜 흔들림";
            }

            if (id == "milk_aversion")
            {
                return "우유가 질린 날";
            }

            if (id == "overfull")
            {
                return "배가 너무 부른 날";
            }

            if (id == "body_chill")
            {
                return "몸이 떨린 밤";
            }

            if (id == "fermented_aftertaste")
            {
                return "발효 뒷맛이 남은 날";
            }

            if (id == "sleep_rhythm_disruption")
            {
                return "수면 리듬이 흐트러진 밤";
            }

            if (id == "recipe_warm_milk_soup")
            {
                return "따뜻한 우유 수프";
            }

            if (id == "recipe_soft_snack_dough")
            {
                return "말랑 간식 반죽";
            }

            if (id == "recipe_cold_milk_pudding")
            {
                return "차가운 우유 푸딩";
            }

            if (id == "recipe_nutty_cheese_cracker")
            {
                return "고소한 치즈 크래커";
            }

            if (id == "recipe_rich_milk_risotto")
            {
                return "진한 밀크 리조또";
            }

            if (id == "recipe_fermented_yogurt_bowl")
            {
                return "발효우유 요거트볼";
            }

            if (id == "recipe_coffee_milk_jelly")
            {
                return "커피우유 젤리";
            }

            if (id == "recipe_cream_soup")
            {
                return "크림 수프";
            }

            if (id == "recipe_star_cream")
            {
                return "별빛 크림";
            }

            foreach (var milk in MilkCatalog.VisibleMilks)
            {
                if (milk == null)
                {
                    continue;
                }

                var growthPrefix = $"{milk.id}_growth_lv_";
                if (!string.IsNullOrWhiteSpace(id) && id.StartsWith(growthPrefix))
                {
                    return $"{milk.displayName} 레벨 {id.Substring(growthPrefix.Length)} 달성";
                }
            }

            foreach (var milk in MilkCatalog.VisibleMilks)
            {
                if (milk == null)
                {
                    continue;
                }

                var rewardPrefix = $"milk_growth_reward_{milk.id}_lv_";
                if (!string.IsNullOrWhiteSpace(id) && id.StartsWith(rewardPrefix))
                {
                    return $"{milk.displayName} 특별 성장 Lv.{id.Substring(rewardPrefix.Length)}";
                }
            }

            const string BasicMilkGrowthPrefix = "basic_milk_growth_lv_";
            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith(BasicMilkGrowthPrefix))
            {
                return $"기본 우유 레벨 {id.Substring(BasicMilkGrowthPrefix.Length)} 달성";
            }

            const string StarMilkGrowthPrefix = "star_milk_growth_lv_";
            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith(StarMilkGrowthPrefix))
            {
                return $"별빛 우유 레벨 {id.Substring(StarMilkGrowthPrefix.Length)} 달성";
            }

            return string.IsNullOrWhiteSpace(id) ? "알 수 없음" : id;
        }

        private static string FormatRecordCategory(string id)
        {
            if (MilkBlendingCatalog.FindMasteryResearchRecord(id) != null)
            {
                return "연구";
            }

            if (SeasonalCareEventCatalog.Find(id) != null)
            {
                return "계절";
            }

            id = NormalizeKnownRecordId(id);
            if (CheeseTamaGrowthStageCatalog.TryGetByRecordId(id, out _))
            {
                return "성장";
            }

            if (EvolutionSystem.FindNormalEvolution(id) != null)
            {
                return "진화";
            }

            if (MilkCatalog.Find(id) != null || id == "basic_milk" || id == "star_milk")
            {
                return "우유";
            }

            if (!string.IsNullOrWhiteSpace(id)
                && (id.Contains("_growth_lv_")
                    || id.StartsWith("milk_growth_reward_")
                    || id.EndsWith("_unlocked")))
            {
                return "성장";
            }

            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith("recipe_"))
            {
                return "요리";
            }

            if (!string.IsNullOrWhiteSpace(id) && (id.StartsWith("daily_") || id.Contains("routine")))
            {
                return "루틴";
            }

            if (!string.IsNullOrWhiteSpace(id)
                && (id.StartsWith("session_")
                    || id.StartsWith("daily_presence_")
                    || id.StartsWith("wait_")
                    || id.Contains("milk_drop")))
            {
                return "체류";
            }

            if (!string.IsNullOrWhiteSpace(id)
                && (id.Contains("snack")
                    || id.Contains("feed")
                    || id.Contains("play")
                    || id.Contains("clean")
                    || id.Contains("rest")))
            {
                return "돌봄";
            }

            return "이벤트";
        }

        private static string FormatKnownRecordDetail(string id)
        {
            var masteryRecord = MilkBlendingCatalog.FindMasteryResearchRecord(id);
            if (masteryRecord != null)
            {
                return masteryRecord.detail;
            }

            var seasonalRecord = SeasonalCareEventCatalog.Find(id);
            if (seasonalRecord != null)
            {
                return seasonalRecord.CollectionDetail;
            }

            var milkBlendRecord = IsMilkBlendRecordId(id);
            id = NormalizeKnownRecordId(id);
            if (CheeseTamaGrowthStageCatalog.TryGetByRecordId(id, out var growthStage))
            {
                return growthStage.Description;
            }

            var normalEvolution = EvolutionSystem.FindNormalEvolution(id);
            if (normalEvolution != null)
            {
                return normalEvolution.Description;
            }

            var milkDefinition = MilkCatalog.Find(id);
            if (milkDefinition != null)
            {
                return milkDefinition.description;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return "기록 정보를 확인할 수 없습니다.";
            }

            if (id.Contains("_growth_lv_"))
            {
                return "우유를 반복해서 돌본 성장 기록입니다.";
            }

            if (id.StartsWith("milk_growth_reward_"))
            {
                return "우유의 성장 단계에서 얻은 특별 보상 기록입니다.";
            }

            if (id.EndsWith("_unlocked"))
            {
                return "새 돌봄 선택지가 열렸습니다.";
            }

            if (id.StartsWith("recipe_"))
            {
                return milkBlendRecord
                    ? "우유 블렌딩에서 발견한 음식 기록입니다."
                    : "요리 패널에서 만든 레시피 기록입니다.";
            }

            if (id.StartsWith("session_") || id.StartsWith("daily_presence_") || id.StartsWith("wait_"))
            {
                return "밀크룸에 머문 시간이 쌓여 등록된 기록입니다.";
            }

            if (id.Contains("milk_drop"))
            {
                return "밀크룸에서 우유방울을 획득한 기록입니다.";
            }

            if (id.Contains("routine"))
            {
                return "오늘 돌봄 루틴을 이어간 기록입니다.";
            }

            if (id.Contains("snack"))
            {
                return "간식을 먹이거나 간식 반응으로 등록된 기록입니다.";
            }

            if (id == "milk_aversion")
            {
                return "같은 우유를 반복해 질렸지만, 다른 우유를 맛보며 회복할 수 있는 먹이 상태입니다.";
            }

            if (id == "overfull")
            {
                return "먹이를 너무 많이 먹었을 때 생기며, 시간 경과나 가벼운 놀이로 회복하는 상태입니다.";
            }

            if (id == "body_chill")
            {
                return "밤에 차가운 우유를 먹어 생기며, 따뜻한 우유·휴식·시간 경과로 회복합니다.";
            }

            if (id == "fermented_aftertaste")
            {
                return "발효 우유나 요거트 계열을 먹어 생기며, 청소와 시간 경과로 옅어집니다.";
            }

            if (id == "sleep_rhythm_disruption")
            {
                return "밤에 커피우유를 먹어 생기며, 휴식과 시간 경과로 회복합니다.";
            }

            if (id.Contains("play") || id.Contains("pet") || id.Contains("clean") || id.Contains("rest"))
            {
                return "직접 돌봄 행동을 반복해 등록된 기록입니다.";
            }

            return "밀크룸 돌봄 중 발견한 짧은 순간입니다.";
        }

        private static string NormalizeKnownRecordId(string id)
        {
            const string MilkBlendRecordPrefix = "milk_blend_";
            if (IsMilkBlendRecordId(id))
            {
                return id.Substring(MilkBlendRecordPrefix.Length);
            }

            return id;
        }

        private static bool IsMilkBlendRecordId(string id)
        {
            return !string.IsNullOrWhiteSpace(id)
                && id.StartsWith("milk_blend_", StringComparison.Ordinal);
        }

        private static string FormatHiddenRecordName(string id)
        {
            var hiddenCareer = HiddenCareerCardCatalog.Find(id);
            if (hiddenCareer != null)
            {
                return hiddenCareer.DisplayName;
            }

            if (id == "first_soft_hatch")
            {
                return "첫 말랑 부화";
            }

            if (id == "star_milk_keeper")
            {
                return "별빛 우유 지킴이";
            }

            if (id == "milkroom_listener")
            {
                return "밀크룸 청취자";
            }

            if (id == "first_snack_bite")
            {
                return "첫 간식 한입";
            }

            if (id == "gentle_caretaker")
            {
                return "다정한 돌봄이";
            }

            if (id == "tidy_keeper")
            {
                return "깔끔한 관리인";
            }

            if (id == "playful_friend")
            {
                return "장난스러운 친구";
            }

            if (id == "warm_balance")
            {
                return "따뜻한 균형";
            }

            if (id == "daily_regular")
            {
                return "꾸준한 일과";
            }

            if (id == "patient_guest")
            {
                return "느긋한 밀크룸 손님";
            }

            if (id == "drop_listener")
            {
                return "방울 청취자";
            }

            return string.IsNullOrWhiteSpace(id) ? "알 수 없음" : id;
        }

        private static string FormatIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
            {
                return "알 수 없음";
            }

            return iso.Length > 10 ? iso.Substring(0, 10) : iso;
        }

        private void ConfigureTabButtons()
        {
            ConfigureTabButton(milkTabButton, CollectionRecordTab.Milk);
            ConfigureTabButton(evolutionTabButton, CollectionRecordTab.Evolution);
            ConfigureTabButton(eventTabButton, CollectionRecordTab.Event);
            ConfigureTabButton(hiddenTabButton, CollectionRecordTab.Hidden);
        }

        private void ConfigureTabButton(Button button, CollectionRecordTab tab)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowTab(tab));
        }

        private void ShowTab(CollectionRecordTab tab)
        {
            activeTab = tab;
            UpdateCategoryClaimAllButtonVisibility();
            if (cardLayoutEnabled)
            {
                SetTextVisible(milkText, false);
                SetTextVisible(evolutionText, false);
                SetTextVisible(eventText, false);
                SetTextVisible(hiddenText, false);
                SetCardRootVisible(milkCardRoot, tab == CollectionRecordTab.Milk);
                SetCardRootVisible(evolutionCardRoot, tab == CollectionRecordTab.Evolution);
                SetCardRootVisible(eventCardRoot, tab == CollectionRecordTab.Event);
                SetCardRootVisible(hiddenCardRoot, tab == CollectionRecordTab.Hidden);
                UpdateFixedHeader(tab);
                ResizeScrollContent(GetActiveCardRoot(tab));
                UpdateTabVisuals();
                return;
            }

            if (!tabsEnabled)
            {
                SetTextVisible(milkText, true);
                SetTextVisible(evolutionText, true);
                SetTextVisible(eventText, true);
                SetTextVisible(hiddenText, true);
                ResizeScrollContent(milkText);
                return;
            }

            SetTextVisible(milkText, tab == CollectionRecordTab.Milk);
            SetTextVisible(evolutionText, tab == CollectionRecordTab.Evolution);
            SetTextVisible(eventText, tab == CollectionRecordTab.Event);
            SetTextVisible(hiddenText, tab == CollectionRecordTab.Hidden);
            UpdateFixedHeader(tab);
            ResizeScrollContent(GetActiveText(tab));
            UpdateTabVisuals();
        }

        private void UpdateFixedHeader(CollectionRecordTab tab)
        {
            if (recordHeaderText == null)
            {
                return;
            }

            recordHeaderText.supportRichText = true;
            recordHeaderText.color = new Color(0.25f, 0.17f, 0.09f);
            recordHeaderText.alignment = TextAnchor.MiddleLeft;
            recordHeaderText.text = $"<b>{GetTabTitle(tab)}</b>  <size=15>{GetTabCount(tab)}개 발견</size>";
        }

        private static string GetTabTitle(CollectionRecordTab tab)
        {
            if (tab == CollectionRecordTab.Evolution)
            {
                return "진화 기록";
            }

            if (tab == CollectionRecordTab.Event)
            {
                return "이벤트 기록";
            }

            if (tab == CollectionRecordTab.Hidden)
            {
                return "특별 기록";
            }

            return "우유 기록";
        }

        private int GetTabCount(CollectionRecordTab tab)
        {
            if (tab == CollectionRecordTab.Evolution)
            {
                return evolutionCardCount;
            }

            if (tab == CollectionRecordTab.Event)
            {
                return eventCardCount;
            }

            if (tab == CollectionRecordTab.Hidden)
            {
                return hiddenCardCount;
            }

            return milkCardCount;
        }

        private RectTransform GetActiveCardRoot(CollectionRecordTab tab)
        {
            if (tab == CollectionRecordTab.Evolution)
            {
                return evolutionCardRoot;
            }

            if (tab == CollectionRecordTab.Event)
            {
                return eventCardRoot;
            }

            if (tab == CollectionRecordTab.Hidden)
            {
                return hiddenCardRoot;
            }

            return milkCardRoot;
        }

        private Text GetActiveText(CollectionRecordTab tab)
        {
            if (tab == CollectionRecordTab.Evolution)
            {
                return evolutionText;
            }

            if (tab == CollectionRecordTab.Event)
            {
                return eventText;
            }

            if (tab == CollectionRecordTab.Hidden)
            {
                return hiddenText;
            }

            return milkText;
        }

        private void UpdateTabVisuals()
        {
            UpdateTabVisual(milkTabButton, activeTab == CollectionRecordTab.Milk);
            UpdateTabVisual(evolutionTabButton, activeTab == CollectionRecordTab.Evolution);
            UpdateTabVisual(eventTabButton, activeTab == CollectionRecordTab.Event);
            UpdateTabVisual(hiddenTabButton, activeTab == CollectionRecordTab.Hidden);
        }

        private static void UpdateTabVisual(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            if (button.TryGetComponent(out Image image))
            {
                image.color = selected
                    ? new Color(1f, 0.74f, 0.24f, 1f)
                    : new Color(1f, 0.9f, 0.62f, 0.88f);
            }

            var colors = button.colors;
            colors.normalColor = selected
                ? new Color(1f, 0.74f, 0.24f, 1f)
                : new Color(1f, 0.9f, 0.62f, 0.88f);
            colors.highlightedColor = new Color(1f, 0.84f, 0.36f, 1f);
            colors.pressedColor = new Color(0.88f, 0.53f, 0.13f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform != null && labelTransform.TryGetComponent(out Text label))
            {
                label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                label.color = new Color(0.26f, 0.16f, 0.08f);
            }
        }

        private static void SetTextVisible(Text target, bool visible)
        {
            if (target != null)
            {
                target.gameObject.SetActive(visible);
            }
        }

        private static void SetCardRootVisible(RectTransform target, bool visible)
        {
            if (target != null)
            {
                target.gameObject.SetActive(visible);
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.supportRichText = true;
                target.horizontalOverflow = HorizontalWrapMode.Wrap;
                target.verticalOverflow = VerticalWrapMode.Overflow;
                target.lineSpacing = 1.18f;
                target.text = value;
                ResizeScrollContent(target);
            }
        }

        private static void ResizeScrollContent(Text target)
        {
            if (target == null || target.rectTransform == null)
            {
                return;
            }

            var content = target.rectTransform.parent as RectTransform;
            if (content == null || content.name != "Collection Scroll Content")
            {
                return;
            }

            var viewport = content.parent as RectTransform;
            var viewportHeight = viewport != null ? viewport.rect.height : 0f;
            var height = Mathf.Max(360f, viewportHeight, target.preferredHeight + 32f);
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);

            var textRect = target.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(0f, height);

            var scrollView = viewport != null && viewport.parent != null
                ? viewport.parent.GetComponent<ScrollRect>()
                : null;
            if (scrollView != null)
            {
                scrollView.verticalNormalizedPosition = 1f;
            }
        }

        private static void ResizeScrollContent(RectTransform activeRoot)
        {
            if (activeRoot == null)
            {
                return;
            }

            var content = activeRoot.parent as RectTransform;
            if (content == null || content.name != "Collection Scroll Content")
            {
                return;
            }

            var viewport = content.parent as RectTransform;
            var viewportHeight = viewport != null ? viewport.rect.height : 0f;
            var height = Mathf.Max(CardRootMinimumHeight, viewportHeight, activeRoot.sizeDelta.y + 32f);
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);

            var scrollView = viewport != null && viewport.parent != null
                ? viewport.parent.GetComponent<ScrollRect>()
                : null;
            if (scrollView != null)
            {
                scrollView.verticalNormalizedPosition = 1f;
            }
        }

        private enum CollectionRecordTab
        {
            Milk,
            Evolution,
            Event,
            Hidden
        }

        private readonly struct CollectionCardData
        {
            public readonly string category;
            public readonly string title;
            public readonly string detail;
            public readonly Sprite thumbnail;
            public readonly CollectionRecordCategory rewardCategory;
            public readonly string recordId;
            public readonly bool fragmentRewardClaimed;

            public bool HasFragmentReward => !string.IsNullOrWhiteSpace(recordId);

            public CollectionCardData(
                string category,
                string title,
                string detail,
                Sprite thumbnail = null,
                CollectionRecordCategory rewardCategory = CollectionRecordCategory.Milk,
                string recordId = null,
                bool fragmentRewardClaimed = false)
            {
                this.category = category;
                this.title = title;
                this.detail = detail;
                this.thumbnail = thumbnail;
                this.rewardCategory = rewardCategory;
                this.recordId = recordId;
                this.fragmentRewardClaimed = fragmentRewardClaimed;
            }
        }
    }
}
