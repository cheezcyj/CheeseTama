using System;
using System.Text;
using CheeseTama.Collections;
using CheeseTama.Core;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Guidance;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Gameplay.Weekly;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public enum JourneyHubTab
    {
        Goals = 0,
        Weekly = 1,
        Relationships = 2,
        Album = 3,
        Workshop = 4
    }

    public sealed class JourneyHubPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Journey Hub Overlay";
        public const string CardObjectName = "Journey Hub Card";
        public const string OpenButtonObjectName = "Open Journey Hub Button";

        private const string OpenButtonBadgeObjectName = "Journey Hub Attention Badge";
        private const string OpenButtonBadgeTextObjectName = "Journey Hub Attention Badge Text";
        private const string ControlAttentionBadgeObjectName = "Journey Hub Control Attention Badge";
        private const float AttentionRefreshIntervalSeconds = 30f;

        private static readonly string[] TabLabels =
        {
            "목표", "주간", "관계", "앨범", "공방"
        };

        [SerializeField] private GameObject overlay;
        [SerializeField] private Button openButton;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button closeButton;

        private GameManager manager;
        private TopMenuController topMenu;
        private BottomActionBarController actionBar;
        private DevPanelController devPanel;
        private int selectedTab;
        private int pageIndex;
        private string transientStatus = string.Empty;
        private bool subscribed;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool actionBarWasEnabled;
        private bool devPanelWasEnabled;
        private GameObject openButtonBadge;
        private Image openButtonBadgeImage;
        private string selectedGoalRouteId = string.Empty;
        private NpcRelationshipEpisodeSnapshot selectedRelationshipEpisode;
        private float nextAttentionRefreshAt;

        public bool IsOpen => overlay != null && overlay.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public JourneyHubTab SelectedTab => (JourneyHubTab)Mathf.Clamp(
            selectedTab,
            0,
            TabLabels.Length - 1);

        public static bool IsAnyOpen()
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<JourneyHubPanelController>();
            return controller != null && controller.IsOpen;
        }

        public void Configure(
            GameObject overlayRoot,
            Button opener,
            Button[] tabs,
            Text title,
            Text body,
            Text status,
            Button previous,
            Button next,
            Button primary,
            Button close,
            GameManager boundManager,
            TopMenuController menuController,
            BottomActionBarController actionBarController,
            DevPanelController developerPanelController)
        {
            Unsubscribe();
            overlay = overlayRoot;
            openButton = opener;
            tabButtons = tabs ?? Array.Empty<Button>();
            titleText = title;
            bodyText = body;
            statusText = status;
            previousButton = previous;
            nextButton = next;
            primaryButton = primary;
            closeButton = close;
            manager = boundManager;
            topMenu = menuController;
            actionBar = actionBarController;
            devPanel = developerPanelController;

            EnsureOpenButtonBadge();
            Bind(openButton, OpenAttention);
            for (var index = 0; index < tabButtons.Length; index += 1)
            {
                var captured = index;
                Bind(tabButtons[index], () => SelectTab(captured));
            }

            Bind(previousButton, HandlePreviousAction);
            Bind(nextButton, HandleNextAction);
            Bind(primaryButton, ExecutePrimaryAction);
            Bind(closeButton, Close);
            Close();
            Subscribe();
            Refresh();
            ScheduleAttentionRefresh();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshOpenButtonBadge();
            ScheduleAttentionRefresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreControls();
        }

        private void Update()
        {
            if (!Application.isPlaying || Time.unscaledTime < nextAttentionRefreshAt)
            {
                return;
            }

            manager = manager != null ? manager : GameManager.Instance;
            RefreshOpenButtonBadge();
            ScheduleAttentionRefresh();
        }

        public void Open()
        {
            if (overlay == null)
            {
                return;
            }

            manager = manager != null ? manager : GameManager.Instance;
            transientStatus = string.Empty;
            topMenu?.CloseAll();
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            SuspendControls();
            Refresh();
        }

        public void Open(JourneyHubTab tab)
        {
            selectedTab = Mathf.Clamp((int)tab, 0, TabLabels.Length - 1);
            pageIndex = 0;
            Open();
        }

        public void OpenAttention()
        {
            manager = manager != null ? manager : GameManager.Instance;
            var attention = ResolveAttentionState();
            Open(attention.HasAttention ? attention.PreferredTab : SelectedTab);
        }

        public void Close()
        {
            if (overlay != null)
            {
                overlay.SetActive(false);
            }

            RestoreControls();
        }

        public void SelectTab(int tabIndex)
        {
            selectedTab = Mathf.Clamp(tabIndex, 0, TabLabels.Length - 1);
            pageIndex = 0;
            transientStatus = string.Empty;
            Refresh();
        }

        public void SelectTab(JourneyHubTab tab)
        {
            SelectTab((int)tab);
        }

        public void Refresh()
        {
            manager = manager != null ? manager : GameManager.Instance;
            var attention = ResolveAttentionState();
            RefreshOpenButtonBadge(attention);
            for (var index = 0; index < tabButtons?.Length; index += 1)
            {
                SetButtonLabel(tabButtons[index], TabLabels[index]);
                UpdateTabVisual(tabButtons[index], index == selectedTab);
                SetButtonAttentionBadge(
                    tabButtons[index],
                    attention.IsTabActionable((JourneyHubTab)index));
            }

            switch (selectedTab)
            {
                case 0:
                    RenderGoals();
                    break;
                case 1:
                    RenderWeekly();
                    break;
                case 2:
                    RenderRelationships();
                    break;
                case 3:
                    RenderAlbum();
                    break;
                default:
                    RenderWorkshop();
                    break;
            }

            RefreshCurrentControlBadges(attention);

            if (statusText != null)
            {
                statusText.text = transientStatus;
            }
        }

        private void RenderGoals()
        {
            SetText(titleText, "다음 성장 목표");
            SetPaging(false, false);
            selectedGoalRouteId = string.Empty;
            SetPrimary("", false, false);
            var snapshot = manager?.GetNextActionGoalBoardSnapshot();
            if (snapshot == null || !snapshot.IsApplicable)
            {
                var level = manager?.CurrentTama?.level ?? 0;
                SetText(
                    bodyText,
                    level >= 33
                        ? NowAction("위의 [주간 돌봄], [관계], [앨범]에서 새 이야기를 골라 보세요.")
                            + "<b>레벨 33 성장 여정을 끝냈어요.</b>\n\n다른 여정에서 새로운 이야기를 이어갈 수 있어요."
                        : NowAction("아래 돌봄 버튼으로 우유를 주거나 함께 놀아 주세요.")
                            + "<b>레벨 30이 되면 새로운 성장 목표가 열려요.</b>\n\n지금은 치즈타마를 돌보며 천천히 자라게 해 주세요.");
                return;
            }

            var builder = new StringBuilder();
            AppendNowAction(
                builder,
                snapshot.IsReadyForLevelUp
                    ? "아래 돌봄 버튼을 한 번 더 눌러 다음 모습으로 자라게 해 주세요."
                    : "남은 목표를 보고 [바로 하기]를 눌러 보세요.");
            builder.AppendLine($"<b>레벨 {snapshot.CurrentLevel} → 레벨 {snapshot.TargetLevel}  {snapshot.ProgressPercent}%</b>");
            builder.AppendLine();
            if (snapshot.IsReadyForLevelUp)
            {
                builder.AppendLine("✓ 모든 성장 조건을 충족했어요. 다음 돌봄에서 성장할 수 있어요.");
            }
            else
            {
                for (var index = 0; index < snapshot.Goals.Count; index += 1)
                {
                    var goal = snapshot.Goals[index];
                    builder.AppendLine($"{UrgencyLabel(goal.Urgency)} <b>{goal.Title}</b>  {goal.ProgressPercent}%");
                    builder.AppendLine($"   {goal.MissingCondition}");
                }
            }

            if (snapshot.MissingConditions.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("<b>남은 조건</b>");
                for (var index = 0; index < snapshot.MissingConditions.Count; index += 1)
                {
                    builder.AppendLine($"• {snapshot.MissingConditions[index]}");
                }
            }

            SetText(bodyText, builder.ToString().TrimEnd());
            if (!snapshot.IsReadyForLevelUp
                && TryFindGoalRoute(snapshot, out selectedGoalRouteId))
            {
                SetPrimary("바로 하기", true, true);
            }
        }

        private void RenderWeekly()
        {
            SetText(titleText, "이번 주 돌봄 여정");
            SetPaging(false, false);
            var snapshot = manager?.GetWeeklyCareJourneySnapshot();
            if (snapshot == null)
            {
                SetText(bodyText, "주간 여정을 불러오지 못했어요.");
                SetPrimary("보상 받기", false, true);
                return;
            }

            var builder = new StringBuilder();
            AppendNowAction(
                builder,
                snapshot.CanClaimReward
                    ? "아래 [보상 받기]를 눌러 이번 주 선물을 받아요."
                    : "○ 표시가 있는 목표 하나를 골라 돌봐 주세요.");
            builder.AppendLine($"<b>{FormatWeekLabel(snapshot.WeekKey)} · 완료 {snapshot.CompletedObjectives}/5</b>");
            if (snapshot.WeekStatus == WeeklyCareWeekStatus.ClockRollback)
            {
                builder.AppendLine("<color=#A45120><b>기기 날짜가 저장된 주차보다 이전이에요.</b> 날짜와 시간을 확인하면 다시 기록하고 보상을 받을 수 있어요.</color>");
            }
            else if (snapshot.WeekStatus == WeeklyCareWeekStatus.MissingState)
            {
                builder.AppendLine("<color=#A45120><b>주간 여정 상태를 불러오지 못했어요.</b> 저장 데이터를 다시 불러와 주세요.</color>");
            }

            builder.AppendLine("다섯 목표 중 세 개를 완료하면 주간 선물을 받을 수 있어요.");
            builder.AppendLine();
            for (var index = 0; index < snapshot.Objectives.Count; index += 1)
            {
                var objective = snapshot.Objectives[index];
                if (objective.Definition == null)
                {
                    continue;
                }

                builder.AppendLine(
                    $"{(objective.Completed ? "✓" : "○")} <b>{objective.Definition.Title}</b>  "
                    + $"{objective.Progress}/{objective.Definition.Target}");
                builder.AppendLine($"   {objective.Definition.Description}");
            }

            builder.AppendLine();
            builder.AppendLine("보상  코인 60 · 우유방울 10 · 도감조각 3");
            SetText(bodyText, builder.ToString().TrimEnd());
            SetPrimary(
                snapshot.RewardClaimed ? "받기 완료" : "보상 받기",
                snapshot.CanClaimReward,
                true);
        }

        private void RenderRelationships()
        {
            SetText(titleText, "밀크룸 손님 관계");
            selectedRelationshipEpisode = ResolveEligibleRelationshipEpisode();
            var hasEpisode = selectedRelationshipEpisode.IsEligible
                && selectedRelationshipEpisode.Episode != null;
            var active = manager?.GetActiveNpcRelationshipQuest() ?? default;
            SetPaging(hasEpisode, hasEpisode);
            if (hasEpisode)
            {
                SetButtonLabel(previousButton, "선택 1");
                SetButtonLabel(nextButton, "선택 2");
            }

            var builder = new StringBuilder();
            AppendNowAction(
                builder,
                hasEpisode
                    ? "아래 [선택 1]과 [선택 2] 중 마음에 드는 답을 눌러요."
                    : active.CanDeliver
                        ? "필요한 것을 모았다면 아래 [선물 보내기]를 눌러요."
                        : "손님이 다시 올 때까지 치즈타마를 돌봐 주세요.");
            AppendRelationship(builder, NpcVisitSystem.MilkyDoctorId, "밀키 박사");
            AppendRelationship(builder, NpcVisitSystem.FermentationFairyId, "발효요정");
            AppendRelationship(builder, NpcVisitSystem.MilkCatId, "밀크냥");
            builder.AppendLine();

            if (hasEpisode)
            {
                var episode = selectedRelationshipEpisode.Episode;
                builder.AppendLine($"<color=#A45120><b>새 관계 이야기 · {episode.Title}</b></color>");
                builder.AppendLine(episode.Description);
                for (var index = 0; index < episode.Choices.Count; index += 1)
                {
                    builder.AppendLine($"{index + 1}. {episode.Choices[index].Label}");
                }

                builder.AppendLine("아래 ‘선택 1/2’로 답하면 선택 결과와 기념품이 추억일기에 저장돼요.");
                builder.AppendLine();
            }
            else
            {
                AppendNextRelationshipEpisodeHint(builder);
            }

            AppendKeepsakes(builder);

            if (active.Status == NpcQuestWindowStatus.None)
            {
                builder.AppendLine("<b>진행 중인 부탁이 없어요.</b>");
                builder.AppendLine("손님의 다음 방문 이야기를 완료하면 새 부탁을 받을 수 있어요.");
                SetPrimary("선물 보내기", false, true);
            }
            else if (active.Status == NpcQuestWindowStatus.UnknownQuest || active.Quest == null)
            {
                builder.AppendLine("<b>확인할 수 없는 부탁이에요.</b>");
                builder.AppendLine("저장 데이터를 다시 불러오거나 다음 손님 방문을 기다려 주세요.");
                SetPrimary("선물 보내기", false, true);
            }
            else
            {
                var heading = active.Status switch
                {
                    NpcQuestWindowStatus.ClockRollback => "시간 확인 필요",
                    NpcQuestWindowStatus.Expired => "기간 종료",
                    NpcQuestWindowStatus.Grace => "마지막 기회",
                    _ => "진행 중"
                };
                builder.AppendLine($"<b>{heading} · {active.Quest.Title}</b>");
                builder.AppendLine(active.Quest.Description);
                builder.AppendLine($"필요  {FormatQuestCost(active.Quest)}");
                builder.AppendLine($"보상  {FormatQuestReward(active.Quest)}");
                if (active.Status == NpcQuestWindowStatus.ClockRollback)
                {
                    builder.AppendLine("기기 날짜가 부탁 시작 시각보다 이전이에요. 날짜와 시간을 확인해 주세요.");
                }
                else if (active.Status == NpcQuestWindowStatus.Expired)
                {
                    builder.AppendLine($"{active.GraceEndsAt:yyyy-MM-dd}에 종료됐어요. 다음 방문에서 새 부탁을 받을 수 있어요.");
                }
                else
                {
                    builder.AppendLine(active.IsGrace
                        ? $"마지막 기회 · {active.GraceEndsAt:yyyy-MM-dd}까지"
                        : $"{active.ExpiresAt:yyyy-MM-dd}까지");
                }

                SetPrimary("선물 보내기", active.CanDeliver, true);
            }

            SetText(bodyText, builder.ToString().TrimEnd());
        }

        private void RenderAlbum()
        {
            SetText(titleText, "도감 세트 앨범");
            var snapshot = manager?.GetCollectionSetAlbumSnapshot();
            if (snapshot == null || snapshot.Sets.Count == 0)
            {
                SetText(bodyText, "공개된 세트가 아직 없어요.");
                SetPaging(false, false);
                SetPrimary("선물 받기", false, true);
                return;
            }

            pageIndex = Wrap(pageIndex, snapshot.Sets.Count);
            var set = snapshot.Sets[pageIndex];
            var builder = new StringBuilder();
            AppendNowAction(
                builder,
                set.CanClaimReward
                    ? "아래 [선물 받기]를 눌러 완성 선물을 받아요."
                    : "○ 표시 기록을 도감에서 찾아 세트를 완성해요.");
            builder.AppendLine($"<b>{pageIndex + 1}/{snapshot.Sets.Count} · {set.DisplayName}</b>");
            builder.AppendLine(set.Description);
            builder.AppendLine($"진행  {set.DiscoveredCount}/{set.RequiredCount}");
            for (var index = 0; index < set.Records.Count; index += 1)
            {
                var record = set.Records[index];
                var recordName = record.Discovered
                    ? CollectionUIController.FormatKnownRecordName(record.RecordId)
                    : "아직 찾지 못한 기록";
                builder.AppendLine($"{(record.Discovered ? "✓" : "○")} {FormatRecordCategory(record.Category)} · {recordName}");
            }

            builder.AppendLine();
            builder.AppendLine($"보상  코인 {set.Reward.Coins} · 우유방울 {set.Reward.MilkDrops} · 도감조각 {set.Reward.CollectionFragments}");

            var discoveries = manager?.GetAutonomousLifeDiscoverySnapshot();
            if (discoveries != null)
            {
                builder.AppendLine();
                builder.AppendLine($"<b>생활 순간 {discoveries.DiscoveredCount}/{discoveries.TotalCount}</b>");
                for (var index = 0; index < discoveries.Items.Count; index += 1)
                {
                    builder.Append(index > 0 ? " · " : string.Empty);
                    builder.Append(discoveries.Items[index].DisplayName);
                }
                builder.AppendLine();
            }

            var journal = manager?.GetRandomEventJournalSnapshot();
            if (journal != null)
            {
                builder.AppendLine();
                builder.AppendLine($"<b>이벤트 기록 {journal.TotalOccurrences}회</b>");
                var count = Math.Min(3, journal.Entries.Count);
                for (var index = 0; index < count; index += 1)
                {
                    var entry = journal.Entries[index];
                    builder.AppendLine($"• {entry.Title} ×{entry.TotalOccurrences} · {entry.LastOccurredDate}");
                }
            }

            SetText(bodyText, builder.ToString().TrimEnd());
            SetPaging(snapshot.Sets.Count > 1, snapshot.Sets.Count > 1);
            SetPrimary(
                set.RewardClaimed ? "받기 완료" : "선물 받기",
                set.CanClaimReward,
                true);
        }

        private void RenderWorkshop()
        {
            SetText(titleText, "밀크룸 장식 공방");
            var variants = DecorationWorkshopCatalog.All;
            if (variants == null || variants.Count == 0)
            {
                SetText(bodyText, "제작 가능한 장식 변형이 없어요.");
                SetPaging(false, false);
                SetPrimary("제작", false, true);
                return;
            }

            pageIndex = Wrap(pageIndex, variants.Count);
            var definition = variants[pageIndex];
            var quote = manager?.GetDecorationWorkshopQuote(definition.Id);
            var render = manager?.GetDecorationWorkshopRenderSnapshot();
            var selected = string.Equals(
                render?.Find(definition.Slot)?.VariantId,
                definition.Id,
                StringComparison.Ordinal);
            var owned = quote?.Status == DecorationWorkshopQuoteStatus.AlreadyOwned;

            var builder = new StringBuilder();
            AppendNowAction(
                builder,
                selected
                    ? "아래 [방에서 빼기]를 눌러 기본 장식으로 돌릴 수 있어요."
                    : owned
                        ? "아래 [방에 놓기]를 눌러 방을 꾸며 보세요."
                        : quote?.CanCraft == true
                            ? "아래 [만들기]를 눌러 장식을 만들어요."
                            : "돌봄을 하며 부족한 코인과 조각을 모아 주세요.");
            builder.AppendLine($"<b>{pageIndex + 1}/{variants.Count} · {definition.DisplayName}</b>");
            builder.AppendLine($"꾸밀 곳  {FormatSlot(definition.Slot)}");
            builder.AppendLine("이 장식으로 밀크룸 분위기를 바꿀 수 있어요.");
            builder.AppendLine();
            builder.AppendLine($"제작 비용  {DecorationWorkshopSystem.FormatCost(definition)}");
            builder.AppendLine(selected
                ? "현재 밀크룸에 적용 중이에요. 방에서 빼면 기본 장식으로 돌아가요."
                : owned
                    ? "제작 완료 · 밀크룸에 적용할 수 있어요."
                    : quote?.CanCraft == true
                        ? "지금 제작할 수 있어요."
                        : $"부족  코인 {quote?.MissingCoins ?? 0} · 우유방울 {quote?.MissingMilkDrops ?? 0} · 도감조각 {quote?.MissingCollectionFragments ?? 0}");
            SetText(bodyText, builder.ToString().TrimEnd());
            SetPaging(variants.Count > 1, variants.Count > 1);
            SetPrimary(
                selected ? "방에서 빼기" : owned ? "방에 놓기" : "만들기",
                selected || owned || quote?.CanCraft == true,
                true);
        }

        private void ExecutePrimaryAction()
        {
            if (manager == null)
            {
                transientStatus = "게임 데이터를 불러오지 못했어요.";
                Refresh();
                return;
            }

            switch (selectedTab)
            {
                case 0:
                ExecuteGoalRoute();
                break;
                case 1:
                var weekly = manager.TryClaimWeeklyCareJourneyReward($"weekly_ui_{Guid.NewGuid():N}");
                transientStatus = weekly.Applied ? "주간 돌봄 선물을 받았어요." : WeeklyFailure(weekly.Status);
                break;
                case 2:
                var delivery = manager.TryDeliverNpcRelationshipQuest($"npc_quest_ui_{Guid.NewGuid():N}");
                transientStatus = delivery.Applied ? "부탁을 전하고 관계가 더 가까워졌어요." : DeliveryFailure(delivery.Status);
                break;
                case 3:
                var sets = manager.GetCollectionSetAlbumSnapshot().Sets;
                if (sets.Count > 0)
                {
                    pageIndex = Wrap(pageIndex, sets.Count);
                    var album = manager.TryClaimCollectionSetAlbumReward(
                        sets[pageIndex].SetId,
                        $"album_ui_{Guid.NewGuid():N}");
                    transientStatus = album.Applied ? "세트 앨범 보상을 받았어요." : "아직 보상을 받을 수 없어요.";
                }
                break;
                case 4:
                ExecuteWorkshopAction();
                break;
            }

            Refresh();
        }

        private void ExecuteGoalRoute()
        {
            if (string.IsNullOrWhiteSpace(selectedGoalRouteId))
            {
                var snapshot = manager?.GetNextActionGoalBoardSnapshot();
                if (!TryFindGoalRoute(snapshot, out selectedGoalRouteId))
                {
                    transientStatus = "지금 바로 이동할 성장 행동이 없어요.";
                    return;
                }
            }

            if (string.Equals(
                    selectedGoalRouteId,
                    NextActionRouteIds.MilkGrowth,
                    StringComparison.Ordinal))
            {
                var milkPanel = GetComponent<MilkPanelController>();
                if (milkPanel == null)
                {
                    transientStatus = "우유 성장 화면을 열 수 없어요.";
                    return;
                }

                Close();
                milkPanel.Open();
                return;
            }

            if (string.Equals(
                    selectedGoalRouteId,
                    NextActionRouteIds.Care,
                    StringComparison.Ordinal))
            {
                Close();
                FocusFirstCareAction();
                return;
            }

            transientStatus = "연결되지 않은 성장 행동이에요.";
        }

        private void FocusFirstCareAction()
        {
            if (actionBar == null || EventSystem.current == null)
            {
                return;
            }

            var buttons = actionBar.GetComponentsInChildren<Button>(true);
            for (var index = 0; index < buttons.Length; index += 1)
            {
                var button = buttons[index];
                if (button != null && button.interactable && button.gameObject.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(button.gameObject);
                    return;
                }
            }
        }

        private void ExecuteWorkshopAction()
        {
            var variants = DecorationWorkshopCatalog.All;
            if (variants.Count == 0)
            {
                return;
            }

            pageIndex = Wrap(pageIndex, variants.Count);
            var definition = variants[pageIndex];
            var quote = manager.GetDecorationWorkshopQuote(definition.Id);
            if (quote.Status == DecorationWorkshopQuoteStatus.AlreadyOwned)
            {
                var render = manager.GetDecorationWorkshopRenderSnapshot();
                var isSelected = string.Equals(
                    render?.Find(definition.Slot)?.VariantId,
                    definition.Id,
                    StringComparison.Ordinal);
                var selected = manager.TrySelectDecorationWorkshopVariant(
                    definition.Slot,
                    isSelected ? string.Empty : definition.Id);
                transientStatus = isSelected
                    ? selected.Changed
                        ? "장식을 방에서 뺐어요."
                        : "이미 기본 장식으로 돌아가 있어요."
                    : selected.Changed
                        ? "밀크룸 장식에 적용했어요."
                        : "이미 적용 중이에요.";
                return;
            }

            var crafted = manager.TryCraftDecorationWorkshopVariant(
                definition.Id,
                $"workshop_ui_{Guid.NewGuid():N}");
            if (!crafted.Applied)
            {
                transientStatus = crafted.Status == DecorationWorkshopCraftStatus.InsufficientCurrency
                    ? "만드는 데 필요한 코인이나 재료가 부족해요."
                    : "지금은 제작할 수 없어요.";
                return;
            }

            manager.TrySelectDecorationWorkshopVariant(definition.Slot, definition.Id);
            transientStatus = "장식을 제작하고 바로 적용했어요.";
        }

        private void ChangePage(int direction)
        {
            pageIndex += direction;
            transientStatus = string.Empty;
            Refresh();
        }

        private void HandlePreviousAction()
        {
            if (SelectedTab == JourneyHubTab.Relationships
                && selectedRelationshipEpisode.IsEligible)
            {
                ExecuteRelationshipEpisodeChoice(0);
                return;
            }

            ChangePage(-1);
        }

        private void HandleNextAction()
        {
            if (SelectedTab == JourneyHubTab.Relationships
                && selectedRelationshipEpisode.IsEligible)
            {
                ExecuteRelationshipEpisodeChoice(1);
                return;
            }

            ChangePage(1);
        }

        private void ExecuteRelationshipEpisodeChoice(int choiceIndex)
        {
            var episode = selectedRelationshipEpisode.Episode;
            if (manager == null
                || episode == null
                || choiceIndex < 0
                || choiceIndex >= episode.Choices.Count)
            {
                transientStatus = "선택할 관계 이야기를 다시 확인해 주세요.";
                Refresh();
                return;
            }

            var result = manager.TryApplyNpcRelationshipEpisodeChoice(
                episode.Id,
                episode.Choices[choiceIndex].Id,
                $"npc_episode_ui_{Guid.NewGuid():N}");
            transientStatus = result.Applied
                ? $"{result.Choice.ResultMessage} · 기념품 ‘{FormatKeepsake(result.RewardKeepsakeId)}’을 받았어요."
                : EpisodeFailure(result.Status);
            Refresh();
        }

        private NpcRelationshipEpisodeSnapshot ResolveEligibleRelationshipEpisode()
        {
            var snapshots = manager?.GetNpcRelationshipEpisodeSnapshots();
            if (snapshots == null)
            {
                return default;
            }

            for (var index = 0; index < snapshots.Count; index += 1)
            {
                if (snapshots[index].IsEligible)
                {
                    return snapshots[index];
                }
            }

            return default;
        }

        private void AppendNextRelationshipEpisodeHint(StringBuilder builder)
        {
            var snapshots = manager?.GetNpcRelationshipEpisodeSnapshots();
            if (snapshots == null)
            {
                return;
            }

            for (var index = 0; index < snapshots.Count; index += 1)
            {
                var snapshot = snapshots[index];
                if (snapshot.Status != NpcRelationshipEpisodeSnapshotStatus.AffinityLocked
                    || snapshot.Episode == null)
                {
                    continue;
                }

                builder.AppendLine(
                    $"<b>다음 관계 이야기</b> · 친밀도 {snapshot.CurrentAffinity}/{snapshot.RequiredAffinity}");
                builder.AppendLine("손님의 방문과 부탁을 이어 가면 새로운 이야기가 열려요.");
                builder.AppendLine();
                return;
            }
        }

        private void AppendKeepsakes(StringBuilder builder)
        {
            var keepsakes = manager?.CurrentSave?.npcRelationshipEpisodes?.keepsakeIds;
            if (keepsakes == null || keepsakes.Count == 0)
            {
                return;
            }

            builder.Append("<b>관계 기념품</b>  ");
            for (var index = 0; index < keepsakes.Count; index += 1)
            {
                if (index > 0)
                {
                    builder.Append(" · ");
                }

                builder.Append(FormatKeepsake(keepsakes[index]));
            }

            builder.AppendLine();
            builder.AppendLine();
        }

        private void AppendRelationship(StringBuilder builder, string npcId, string displayName)
        {
            var relationship = manager?.GetNpcRelationshipSnapshot(npcId) ?? default;
            builder.AppendLine(
                $"<b>{displayName}</b> · {FormatTier(relationship.Tier)} · 친밀도 {relationship.Affinity}/99 · 방문 {relationship.Visits}회");
        }

        private void SetPaging(bool previousVisible, bool nextVisible)
        {
            if (SelectedTab != JourneyHubTab.Relationships)
            {
                SetButtonLabel(previousButton, "이전");
                SetButtonLabel(nextButton, "다음");
            }

            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(previousVisible);
                previousButton.interactable = previousVisible;
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(nextVisible);
                nextButton.interactable = nextVisible;
            }
        }

        private void SetPrimary(string label, bool interactable, bool visible)
        {
            if (primaryButton == null)
            {
                return;
            }

            primaryButton.gameObject.SetActive(visible);
            primaryButton.interactable = interactable;
            SetButtonLabel(primaryButton, label);
        }

        private void EnsureOpenButtonBadge()
        {
            if (openButton == null)
            {
                openButtonBadge = null;
                openButtonBadgeImage = null;
                return;
            }

            openButtonBadge = EnsureButtonAttentionBadge(
                openButton,
                OpenButtonBadgeObjectName,
                out openButtonBadgeImage);
            var existing = openButtonBadge != null ? openButtonBadge.transform : null;
            if (existing == null)
            {
                return;
            }

            var textTransform = existing.Find(OpenButtonBadgeTextObjectName);
            if (textTransform != null)
            {
                textTransform.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(textTransform.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(textTransform.gameObject);
                }
            }

            openButtonBadge.transform.SetAsLastSibling();
        }

        private void RefreshOpenButtonBadge()
        {
            RefreshOpenButtonBadge(ResolveAttentionState());
        }

        private void RefreshOpenButtonBadge(AttentionState attention)
        {
            EnsureOpenButtonBadge();
            if (openButtonBadge == null)
            {
                return;
            }

            openButtonBadge.SetActive(attention.HasAttention);
            if (!attention.HasAttention)
            {
                return;
            }

            if (openButtonBadgeImage != null)
            {
                openButtonBadgeImage.color = new Color(0.92f, 0.12f, 0.1f, 1f);
            }
        }

        private AttentionState ResolveAttentionState()
        {
            if (manager == null)
            {
                return AttentionState.None(SelectedTab);
            }

            var activeQuest = manager.GetActiveNpcRelationshipQuest();
            var isExpiring = activeQuest.Status == NpcQuestWindowStatus.Grace;
            var eligibleEpisode = ResolveEligibleRelationshipEpisode();
            var hasEpisode = eligibleEpisode.IsEligible && eligibleEpisode.Episode != null;
            var relationshipActionable = activeQuest.CanDeliver || hasEpisode || isExpiring;
            var goalsActionable = TryFindGoalRoute(
                manager.GetNextActionGoalBoardSnapshot(),
                out _);
            var weeklyClaimable = manager.GetWeeklyCareJourneySnapshot()?.CanClaimReward == true;
            var albumClaimable = false;
            var album = manager.GetCollectionSetAlbumSnapshot();
            if (album != null)
            {
                for (var index = 0; index < album.Sets.Count; index += 1)
                {
                    if (album.Sets[index]?.CanClaimReward == true)
                    {
                        albumClaimable = true;
                        break;
                    }
                }
            }

            var workshopActionable = false;
            var workshopVariants = DecorationWorkshopCatalog.All;
            for (var index = 0; index < workshopVariants.Count; index += 1)
            {
                if (IsWorkshopActionable(index))
                {
                    workshopActionable = true;
                    break;
                }
            }

            var preferredTab = relationshipActionable
                ? JourneyHubTab.Relationships
                : weeklyClaimable
                    ? JourneyHubTab.Weekly
                    : albumClaimable
                        ? JourneyHubTab.Album
                        : workshopActionable
                            ? JourneyHubTab.Workshop
                            : goalsActionable
                                ? JourneyHubTab.Goals
                                : SelectedTab;
            return new AttentionState(
                goalsActionable,
                weeklyClaimable,
                relationshipActionable,
                activeQuest.CanDeliver,
                hasEpisode,
                albumClaimable,
                workshopActionable,
                preferredTab);
        }

        private void RefreshCurrentControlBadges(AttentionState attention)
        {
            SetButtonAttentionBadge(previousButton, false);
            SetButtonAttentionBadge(nextButton, false);
            SetButtonAttentionBadge(primaryButton, false);

            switch (SelectedTab)
            {
                case JourneyHubTab.Goals:
                    SetButtonAttentionBadge(primaryButton, attention.GoalsActionable);
                    break;
                case JourneyHubTab.Weekly:
                    SetButtonAttentionBadge(primaryButton, attention.WeeklyActionable);
                    break;
                case JourneyHubTab.Relationships:
                    SetButtonAttentionBadge(previousButton, attention.HasRelationshipEpisode);
                    SetButtonAttentionBadge(nextButton, attention.HasRelationshipEpisode);
                    SetButtonAttentionBadge(primaryButton, attention.CanDeliverRelationshipQuest);
                    break;
                case JourneyHubTab.Album:
                    RefreshAlbumControlBadges();
                    break;
                case JourneyHubTab.Workshop:
                    RefreshWorkshopControlBadges();
                    break;
            }
        }

        private void RefreshAlbumControlBadges()
        {
            var album = manager?.GetCollectionSetAlbumSnapshot();
            var count = album?.Sets.Count ?? 0;
            if (count <= 0)
            {
                return;
            }

            pageIndex = Wrap(pageIndex, count);
            RefreshPagedControlBadges(
                count,
                index => album.Sets[index]?.CanClaimReward == true);
        }

        private void RefreshWorkshopControlBadges()
        {
            var count = DecorationWorkshopCatalog.All.Count;
            if (count <= 0)
            {
                return;
            }

            pageIndex = Wrap(pageIndex, count);
            RefreshPagedControlBadges(count, IsWorkshopActionable);
        }

        private void RefreshPagedControlBadges(int count, Func<int, bool> isActionable)
        {
            if (count <= 0 || isActionable == null)
            {
                return;
            }

            if (isActionable(pageIndex))
            {
                SetButtonAttentionBadge(primaryButton, true);
                return;
            }

            var previousDistance = int.MaxValue;
            var nextDistance = int.MaxValue;
            for (var distance = 1; distance < count; distance += 1)
            {
                if (previousDistance == int.MaxValue
                    && isActionable(Wrap(pageIndex - distance, count)))
                {
                    previousDistance = distance;
                }

                if (nextDistance == int.MaxValue
                    && isActionable(Wrap(pageIndex + distance, count)))
                {
                    nextDistance = distance;
                }

                if (previousDistance != int.MaxValue && nextDistance != int.MaxValue)
                {
                    break;
                }
            }

            if (previousDistance == int.MaxValue && nextDistance == int.MaxValue)
            {
                return;
            }

            SetButtonAttentionBadge(previousButton, previousDistance <= nextDistance);
            SetButtonAttentionBadge(nextButton, nextDistance <= previousDistance);
        }

        private bool IsWorkshopActionable(int index)
        {
            var variants = DecorationWorkshopCatalog.All;
            if (manager == null || index < 0 || index >= variants.Count)
            {
                return false;
            }

            var definition = variants[index];
            var quote = manager.GetDecorationWorkshopQuote(definition.Id);
            return quote?.CanCraft == true;
        }

        private static GameObject EnsureButtonAttentionBadge(
            Button button,
            string objectName,
            out Image badgeImage)
        {
            badgeImage = null;
            if (button == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var existing = button.transform.Find(objectName);
            if (existing == null)
            {
                var badgeObject = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                badgeObject.transform.SetParent(button.transform, false);
                existing = badgeObject.transform;
            }

            if (existing is RectTransform badgeRect)
            {
                badgeRect.anchorMin = Vector2.one;
                badgeRect.anchorMax = Vector2.one;
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.anchoredPosition = Vector2.zero;
                badgeRect.sizeDelta = new Vector2(16f, 16f);
            }

            badgeImage = existing.GetComponent<Image>()
                ?? existing.gameObject.AddComponent<Image>();
            badgeImage.color = new Color(0.92f, 0.12f, 0.1f, 1f);
            badgeImage.raycastTarget = false;
            StarterSceneBuilder.ApplyCircleImage(badgeImage);

            var outline = existing.GetComponent<Outline>()
                ?? existing.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.92f, 0.76f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            existing.SetAsLastSibling();
            return existing.gameObject;
        }

        private static void SetButtonAttentionBadge(Button button, bool visible)
        {
            var badge = EnsureButtonAttentionBadge(
                button,
                ControlAttentionBadgeObjectName,
                out _);
            if (badge != null)
            {
                badge.SetActive(visible
                    && button.gameObject.activeSelf
                    && button.interactable);
            }
        }

        private static void UpdateTabVisual(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var normal = selected
                ? new Color(1f, 0.74f, 0.24f, 1f)
                : new Color(1f, 0.9f, 0.62f, 0.88f);
            if (button.TryGetComponent(out Image image))
            {
                image.color = normal;
            }

            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = new Color(1f, 0.84f, 0.36f, 1f);
            colors.pressedColor = new Color(0.88f, 0.53f, 0.13f, 1f);
            colors.selectedColor = normal;
            button.colors = colors;

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                label.color = new Color(0.26f, 0.16f, 0.08f);
            }
        }

        private void ScheduleAttentionRefresh()
        {
            nextAttentionRefreshAt = Time.unscaledTime + AttentionRefreshIntervalSeconds;
        }

        private static bool TryFindGoalRoute(
            NextActionGoalBoardSnapshot snapshot,
            out string routeId)
        {
            routeId = string.Empty;
            if (snapshot == null || !snapshot.IsApplicable || snapshot.IsReadyForLevelUp)
            {
                return false;
            }

            for (var index = 0; index < snapshot.Goals.Count; index += 1)
            {
                var candidate = snapshot.Goals[index]?.DestinationRouteId;
                if (string.Equals(candidate, NextActionRouteIds.Care, StringComparison.Ordinal)
                    || string.Equals(candidate, NextActionRouteIds.MilkGrowth, StringComparison.Ordinal))
                {
                    routeId = candidate;
                    return true;
                }
            }

            return false;
        }

        private void Subscribe()
        {
            manager = manager != null ? manager : GameManager.Instance;
            if (subscribed || !isActiveAndEnabled || manager == null)
            {
                return;
            }

            manager.JourneyHubChanged += Refresh;
            manager.SaveDataReplaced += Refresh;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (manager != null)
            {
                manager.JourneyHubChanged -= Refresh;
                manager.SaveDataReplaced -= Refresh;
            }

            subscribed = false;
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            topMenuWasEnabled = topMenu != null && topMenu.enabled;
            actionBarWasEnabled = actionBar != null && actionBar.enabled;
            devPanelWasEnabled = devPanel != null && devPanel.enabled;
            if (topMenu != null) topMenu.enabled = false;
            if (actionBar != null) actionBar.enabled = false;
            if (devPanel != null) devPanel.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenu != null) topMenu.enabled = topMenuWasEnabled;
            if (actionBar != null) actionBar.enabled = actionBarWasEnabled;
            if (devPanel != null) devPanel.enabled = devPanelWasEnabled;
            controlsSuspended = false;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void SetButtonLabel(Button button, string value)
        {
            var label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            SetText(label, value);
        }

        private static int Wrap(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return ((value % count) + count) % count;
        }

        private static string UrgencyLabel(NextActionUrgency urgency)
        {
            return urgency switch
            {
                NextActionUrgency.Urgent => "[긴급]",
                NextActionUrgency.Today => "[오늘]",
                _ => "[장기]"
            };
        }

        private static string FormatTier(NpcRelationshipTier tier)
        {
            return tier switch
            {
                NpcRelationshipTier.Familiar => "낯익은 사이",
                NpcRelationshipTier.Friend => "친구",
                NpcRelationshipTier.TrustedFriend => "믿음직한 친구",
                _ => "첫인사"
            };
        }

        private static string FormatQuestCost(NpcRelationshipQuestDefinition quest)
        {
            var cost = quest.Cost;
            var parts = new StringBuilder();
            AppendPart(parts, "코인", cost.MilkCoins);
            AppendPart(parts, "우유방울", cost.MilkDrops);
            AppendPart(parts, "도감조각", cost.CollectionFragments);
            if (cost.SnackQuantity > 0)
            {
                if (parts.Length > 0) parts.Append(" · ");
                var snackName = SnackCatalog.Find(cost.SnackId)?.displayName ?? "간식";
                parts.Append($"{snackName} {cost.SnackQuantity}개");
            }
            return parts.Length > 0 ? parts.ToString() : "없음";
        }

        private static void AppendNowAction(StringBuilder builder, string action)
        {
            builder.Append(NowAction(action));
        }

        private static string NowAction(string action)
        {
            return $"<color=#7A3F08><b>지금 할 일</b>  {action}</color>\n\n";
        }

        private static string FormatWeekLabel(string weekKey)
        {
            return DateTime.TryParseExact(
                weekKey,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var startDate)
                ? $"{startDate.Month}월 {startDate.Day}일에 시작한 이번 주"
                : "이번 주";
        }

        private static string FormatQuestReward(NpcRelationshipQuestDefinition quest)
        {
            var reward = quest.Reward;
            var parts = new StringBuilder();
            AppendPart(parts, "코인", reward.MilkCoins);
            AppendPart(parts, "우유방울", reward.MilkDrops);
            AppendPart(parts, "도감조각", reward.CollectionFragments);
            AppendPart(parts, "친밀도", reward.Affinity);
            return parts.Length > 0 ? parts.ToString() : "없음";
        }

        private static void AppendPart(StringBuilder builder, string label, int value)
        {
            if (value <= 0)
            {
                return;
            }

            if (builder.Length > 0) builder.Append(" · ");
            builder.Append($"{label} {value}");
        }

        private static string FormatRecordCategory(CollectionSetAlbumRecordCategory category)
        {
            return category switch
            {
                CollectionSetAlbumRecordCategory.Milk => "우유",
                CollectionSetAlbumRecordCategory.Evolution => "진화",
                _ => "기록"
            };
        }

        private static string FormatSlot(DecorationSlot slot)
        {
            return slot switch
            {
                DecorationSlot.Wall => "벽",
                DecorationSlot.Floor => "바닥",
                DecorationSlot.Accent => "포인트",
                DecorationSlot.Window => "창가",
                DecorationSlot.Shelf => "선반",
                _ => "침대 곁"
            };
        }

        private static string FormatKeepsake(string keepsakeId)
        {
            return keepsakeId switch
            {
                NpcRelationshipKeepsakeIds.DoctorHealthNotebook => "건강 수첩",
                NpcRelationshipKeepsakeIds.DoctorSmallStethoscope => "작은 청진기",
                NpcRelationshipKeepsakeIds.FairyScentSachet => "향기 주머니",
                NpcRelationshipKeepsakeIds.FairyFermentationBell => "발효 종",
                NpcRelationshipKeepsakeIds.CatPawMap => "발자국 지도",
                NpcRelationshipKeepsakeIds.CatStarCompass => "별 나침반",
                _ => "소중한 기념품"
            };
        }

        private static string EpisodeFailure(NpcRelationshipEpisodeChoiceStatus status)
        {
            return status switch
            {
                NpcRelationshipEpisodeChoiceStatus.DuplicateReceipt => "이미 반영한 관계 이야기예요.",
                NpcRelationshipEpisodeChoiceStatus.AlreadyCompleted => "이미 마친 관계 이야기예요.",
                NpcRelationshipEpisodeChoiceStatus.AffinityLocked => "친밀도가 더 쌓인 뒤 선택할 수 있어요.",
                NpcRelationshipEpisodeChoiceStatus.PrerequisiteIncomplete => "앞선 관계 이야기를 먼저 완료해 주세요.",
                NpcRelationshipEpisodeChoiceStatus.StateCapacityFull => "기념품 보관 한도를 확인해 주세요.",
                _ => "지금은 이 관계 이야기를 진행할 수 없어요."
            };
        }

        private static string WeeklyFailure(WeeklyCareClaimStatus status)
        {
            return status switch
            {
                WeeklyCareClaimStatus.NotEnoughObjectives => "주간 목표 세 개를 먼저 완료해 주세요.",
                WeeklyCareClaimStatus.AlreadyClaimed => "이번 주 보상은 이미 받았어요.",
                WeeklyCareClaimStatus.RewardCapacityFull => "선물을 담을 공간이 부족해서 받을 수 없어요.",
                WeeklyCareClaimStatus.ClockRollback => "기기 시간이 이전 주로 돌아가 보상을 잠시 멈췄어요.",
                _ => "지금은 주간 보상을 받을 수 없어요."
            };
        }

        private static string DeliveryFailure(NpcQuestDeliveryStatus status)
        {
            return status switch
            {
                NpcQuestDeliveryStatus.InsufficientResources => "부탁에 필요한 코인이나 재료가 부족해요.",
                NpcQuestDeliveryStatus.Expired => "부탁의 유예 기간이 끝났어요.",
                NpcQuestDeliveryStatus.ClockRollback => "기기 시간이 되돌아가 납품을 잠시 멈췄어요.",
                NpcQuestDeliveryStatus.RewardCapacityFull => "보상 보관 한도 때문에 납품할 수 없어요.",
                _ => "지금 납품할 수 있는 부탁이 없어요."
            };
        }

        private readonly struct AttentionState
        {
            public AttentionState(
                bool goalsActionable,
                bool weeklyActionable,
                bool relationshipsActionable,
                bool canDeliverRelationshipQuest,
                bool hasRelationshipEpisode,
                bool albumActionable,
                bool workshopActionable,
                JourneyHubTab preferredTab)
            {
                GoalsActionable = goalsActionable;
                WeeklyActionable = weeklyActionable;
                RelationshipsActionable = relationshipsActionable;
                CanDeliverRelationshipQuest = canDeliverRelationshipQuest;
                HasRelationshipEpisode = hasRelationshipEpisode;
                AlbumActionable = albumActionable;
                WorkshopActionable = workshopActionable;
                PreferredTab = preferredTab;
            }

            public bool GoalsActionable { get; }
            public bool WeeklyActionable { get; }
            public bool RelationshipsActionable { get; }
            public bool CanDeliverRelationshipQuest { get; }
            public bool HasRelationshipEpisode { get; }
            public bool AlbumActionable { get; }
            public bool WorkshopActionable { get; }
            public bool HasAttention => GoalsActionable
                || WeeklyActionable
                || RelationshipsActionable
                || AlbumActionable
                || WorkshopActionable;
            public JourneyHubTab PreferredTab { get; }

            public bool IsTabActionable(JourneyHubTab tab)
            {
                return tab switch
                {
                    JourneyHubTab.Goals => GoalsActionable,
                    JourneyHubTab.Weekly => WeeklyActionable,
                    JourneyHubTab.Relationships => RelationshipsActionable,
                    JourneyHubTab.Album => AlbumActionable,
                    JourneyHubTab.Workshop => WorkshopActionable,
                    _ => false
                };
            }

            public static AttentionState None(JourneyHubTab currentTab)
            {
                return new AttentionState(
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    currentTab);
            }
        }
    }
}
