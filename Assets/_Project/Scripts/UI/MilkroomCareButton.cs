using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Gameplay.Care;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Milk;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public enum MilkroomCareAction
    {
        FeedMilk,
        FeedWarmMilk,
        FeedColdMilk,
        FeedNuttyMilk,
        FeedRichMilk,
        FeedFermentedMilk,
        FeedCoffeeMilk,
        Play,
        Clean,
        Rest,
        Save,
        Reload,
        Reset,
        WaitHour,
        FeedStarMilk,
        FeedSnack,
        CatchMilkDrops,
        Blend,
        OpenMilkPanel,
        OpenSnackPanel,
        SleepSchedule
    }

    [RequireComponent(typeof(Button))]
    public sealed class MilkroomCareButton : MonoBehaviour
    {
        [SerializeField] private MilkroomCareAction action;
        [SerializeField] private MilkroomUIController uiController;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private CookingPanelController cookingPanelController;
        [SerializeField] private MilkPanelController milkPanelController;
        [SerializeField] private SnackPanelController snackPanelController;

        private readonly CareActionSystem careActions = new CareActionSystem();
        private Button button;
        private CookingChoicePanelController cookingChoicePanelController;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button ??= GetComponent<Button>();
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Configure(
            MilkroomCareAction careAction,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            Configure(careAction, milkroomUi, cheeseTamaVisual, null, null, null);
        }

        public void Configure(
            MilkroomCareAction careAction,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController cheeseTamaVisual,
            CookingPanelController cookingPanel)
        {
            Configure(careAction, milkroomUi, cheeseTamaVisual, cookingPanel, null, null);
        }

        public void Configure(
            MilkroomCareAction careAction,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController cheeseTamaVisual,
            MilkPanelController milkPanel)
        {
            Configure(careAction, milkroomUi, cheeseTamaVisual, null, milkPanel, null);
        }

        public void Configure(
            MilkroomCareAction careAction,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController cheeseTamaVisual,
            SnackPanelController snackPanel)
        {
            Configure(careAction, milkroomUi, cheeseTamaVisual, null, null, snackPanel);
        }

        public void Configure(
            MilkroomCareAction careAction,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController cheeseTamaVisual,
            CookingPanelController cookingPanel,
            MilkPanelController milkPanel,
            SnackPanelController snackPanel)
        {
            action = careAction;
            uiController = milkroomUi;
            visualController = cheeseTamaVisual;
            cookingPanelController = cookingPanel;
            milkPanelController = milkPanel;
            snackPanelController = snackPanel;
            EnsureButtonListener();
        }

        private void EnsureButtonListener()
        {
            button ??= GetComponent<Button>();
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            ResolveReferences();

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            if (uiController == null)
            {
                Debug.LogWarning("밀크룸 UI 컨트롤러를 찾지 못했습니다.");
                return;
            }

            if (action == MilkroomCareAction.Save)
            {
                manager.SaveGame();
                Refresh("치즈타마 데이터를 저장했습니다.", manager, false);
                return;
            }

            if (action == MilkroomCareAction.Reload)
            {
                manager.ReloadGame();
                var reloadMessage = manager.LastTimeProgression.applied
                    ? manager.LastTimeProgression.ToSummary("비운 사이")
                    : "치즈타마 저장 데이터를 다시 불러왔습니다.";
                Refresh(reloadMessage, manager, false);
                return;
            }

            if (action == MilkroomCareAction.Reset)
            {
                manager.ResetGame();
                Refresh("치즈타마 저장 데이터를 초기화했습니다.", manager, false);
                return;
            }

            if (action == MilkroomCareAction.SleepSchedule)
            {
                CloseToolPanels();
                var sleepSchedule = Object.FindFirstObjectByType<SleepScheduleBridge>();
                if (sleepSchedule != null && sleepSchedule.Open())
                {
                    Refresh("수면 시간을 예약하거나 현재 수면을 확인해 주세요.", manager, false);
                }
                else
                {
                    Refresh("지금은 수면 예약을 열 수 없습니다.", manager, false);
                }

                return;
            }

            if (manager.IsSleepScheduleActive)
            {
                Refresh("치즈타마가 자는 중이에요. 수면 예약에서 먼저 깨워 주세요.", manager, false);
                return;
            }

            if (action == MilkroomCareAction.WaitHour)
            {
                var timeResult = manager.ApplyTimeSkipHours(1);
                manager.RegisterCareAction("wait_hour", timeResult.applied ? timeResult.hours : 1);
                var timeEvent = RegisterRandomEvent(manager);
                PersistAfterInteraction(manager);
                var timeEventUsesCard = !string.IsNullOrWhiteSpace(timeEvent.occurrenceId);
                Refresh(
                    timeResult.ToSummary("밀크룸에서"),
                    manager,
                    false,
                    timeEventUsesCard ? string.Empty : timeEvent.eventId,
                    timeEventUsesCard ? string.Empty : timeEvent.message);
                return;
            }

            if (action == MilkroomCareAction.CatchMilkDrops)
            {
                CloseToolPanels();
                var playChoice = Object.FindFirstObjectByType<PlayChoicePanelController>();
                if (playChoice != null && playChoice.Open())
                {
                    Refresh("오늘 함께할 놀이를 선택해 주세요.", manager, false);
                    return;
                }

                var miniGame = Object.FindFirstObjectByType<MilkDropMiniGameController>();
                if (miniGame != null && miniGame.Open())
                {
                    Refresh("30초 동안 떨어지는 우유방울을 받아 주세요.", manager, false);
                }
                else
                {
                    Refresh("우유방울 놀이를 지금 시작할 수 없습니다.", manager, false);
                }

                return;
            }

            if (action == MilkroomCareAction.Clean)
            {
                CloseToolPanels();
                var cleaningGame = Object.FindFirstObjectByType<CleaningMiniGameController>();
                if (cleaningGame != null && cleaningGame.Open())
                {
                    Refresh("24초 동안 나타나는 얼룩을 눌러 닦아 주세요.", manager, false);
                }
                else
                {
                    Refresh("청소 게임을 시작할 수 없습니다.", manager, false);
                }

                return;
            }

            if (action == MilkroomCareAction.Blend)
            {
                if (TryOpenCookingChoice())
                {
                    Refresh("요리와 우유 블렌딩 중 하나를 선택하세요.", manager, false);
                }
                else
                {
                    Refresh("지금은 만들기 선택창을 열 수 없습니다.", manager, false);
                }

                return;
            }

            if (action == MilkroomCareAction.OpenMilkPanel)
            {
                var milkPanel = ResolveMilkPanel();
                if (milkPanel != null)
                {
                    CloseToolPanelsExcept(MilkroomCareAction.OpenMilkPanel);
                    milkPanel.Open();
                    Refresh("먹일 우유를 선택하세요.", manager, false);
                    return;
                }

                Refresh("우유 패널을 찾지 못했습니다.", manager, false);
                return;
            }

            if (action == MilkroomCareAction.OpenSnackPanel)
            {
                var snackPanel = ResolveSnackPanel();
                if (snackPanel != null)
                {
                    CloseToolPanelsExcept(MilkroomCareAction.OpenSnackPanel);
                    snackPanel.Open();
                    Refresh("먹일 간식을 선택하세요.", manager, false);
                    return;
                }

                Refresh("간식 패널을 찾지 못했습니다.", manager, false);
                return;
            }

            var milkDefinition = GetMilkDefinition();
            if (milkDefinition != null && !manager.IsMilkUnlocked(milkDefinition.id))
            {
                Refresh($"{milkDefinition.displayName}는 아직 잠겨 있습니다.", manager, false);
                return;
            }

            if (action == MilkroomCareAction.Play
                || action == MilkroomCareAction.Clean
                || action == MilkroomCareAction.Rest)
            {
                CloseToolPanels();
            }

            var careResult = RunCareAction(manager, milkDefinition);
            var routineMessage = RegisterCareHistory(manager, careResult, milkDefinition);
            var discoveryMessage = RegisterCollectionDiscoveries(manager, careResult, milkDefinition);
            var eventResult = careResult.hatched || careResult.leveledUp
                ? CareEventResult.None()
                : RegisterRandomEvent(manager);
            var eventUsesCard = !string.IsNullOrWhiteSpace(eventResult.occurrenceId);
            var visualAction = GetVisualAction(careResult, milkDefinition);
            var hideMilkPanelDuringMotion = careResult.success && milkDefinition != null;
            PersistCareResult(manager, careResult);
            Refresh(
                CombineMessages(CombineMessages(careResult.message, routineMessage), discoveryMessage),
                manager,
                careResult.hatched,
                eventUsesCard ? string.Empty : eventResult.eventId,
                eventUsesCard ? string.Empty : eventResult.message,
                visualAction,
                hideMilkPanelDuringMotion);

        }

        private CareActionResult RunCareAction(GameManager manager, MilkDefinition milkDefinition)
        {
            careActions.ConfigureLateLevelGrowth(
                manager.CurrentSave?.lateLevelGrowth,
                manager.CurrentSave?.milkGrowth);
            if (milkDefinition != null)
            {
                return careActions.FeedMilk(manager.CurrentTama, milkDefinition);
            }

            return action switch
            {
                MilkroomCareAction.FeedSnack => careActions.FeedSnack(manager.CurrentTama),
                MilkroomCareAction.Play => careActions.Play(manager.CurrentTama),
                MilkroomCareAction.Clean => careActions.Clean(manager.CurrentTama),
                MilkroomCareAction.Rest => careActions.Rest(manager.CurrentTama),
                _ => new CareActionResult(false, false, "선택한 돌봄 행동이 없습니다.")
            };
        }

        private string RegisterCareHistory(GameManager manager, CareActionResult result, MilkDefinition milkDefinition)
        {
            if (!result.success)
            {
                return string.Empty;
            }

            var actionId = milkDefinition != null ? milkDefinition.actionId : GetCareActionId();
            if (!string.IsNullOrWhiteSpace(actionId))
            {
                manager.RegisterCareAction(actionId);
                return manager.RegisterDailyCareAction(actionId)
                    ? GameManager.DailyRoutineRewardMessage
                    : string.Empty;
            }

            return string.Empty;
        }

        private string GetCareActionId()
        {
            return action switch
            {
                MilkroomCareAction.FeedSnack => "feed_snack",
                MilkroomCareAction.Play => "play",
                MilkroomCareAction.Clean => "clean",
                MilkroomCareAction.Rest => "rest",
                _ => string.Empty
            };
        }

        private string RegisterCollectionDiscoveries(
            GameManager manager,
            CareActionResult result,
            MilkDefinition milkDefinition)
        {
            if (manager == null || !result.success)
            {
                return string.Empty;
            }

            var message = string.Empty;
            if (milkDefinition != null)
            {
                message = RegisterMilkProgress(manager, milkDefinition);
            }

            if (action == MilkroomCareAction.FeedSnack)
            {
                message = RegisterSnackDiscovery(manager);
            }

            if (result.hatched)
            {
                manager.RegisterCurrentEvolutionDiscovery();
            }

            return message;
        }

        private static string RegisterMilkProgress(GameManager manager, MilkDefinition milk)
        {
            var starWasUnlocked = manager.CurrentSave != null
                && manager.CurrentSave.unlocks != null
                && manager.CurrentSave.unlocks.starMilkUnlocked;
            var unlockedBefore = CaptureMainMilkUnlocks(manager);
            var previousGrowth = manager.FindMilkGrowth(milk.id);
            var previousLevel = previousGrowth?.growthLevel ?? 0;

            manager.RegisterMilkDiscovery(milk.id);
            var growth = manager.RegisterMilkGrowth(milk.id, milk.growthPoints);
            manager.RefreshDerivedCollectionRecords();

            var message = string.Empty;
            if (growth != null && growth.growthLevel > previousLevel)
            {
                message = $"{milk.displayName} 레벨 {growth.growthLevel} 달성.";
            }

            if (manager.LastMilkGrowthMilestoneReward != null
                && manager.LastMilkGrowthMilestoneReward.granted)
            {
                message = CombineMessages(message, manager.LastMilkGrowthMilestoneReward.message);
            }

            message = CombineMessages(message, RegisterNewMainMilkUnlocks(manager, unlockedBefore));

            var starNowUnlocked = manager.CurrentSave != null
                && manager.CurrentSave.unlocks != null
                && manager.CurrentSave.unlocks.starMilkUnlocked;
            if (!starWasUnlocked && starNowUnlocked)
            {
                manager.RegisterMilkDiscovery(MilkCatalog.StarMilkId);
                manager.RegisterEventDiscovery("star_milk_unlocked");
                message = CombineMessages(message, "별빛 알과 별빛 우유가 해금되었습니다.");
            }

            return message;
        }

        private static Dictionary<string, bool> CaptureMainMilkUnlocks(GameManager manager)
        {
            var states = new Dictionary<string, bool>();
            foreach (var milk in MilkCatalog.MainMilks)
            {
                if (milk != null)
                {
                    states[milk.id] = manager.IsMilkUnlocked(milk.id);
                }
            }

            return states;
        }

        private static string RegisterNewMainMilkUnlocks(GameManager manager, Dictionary<string, bool> unlockedBefore)
        {
            var message = string.Empty;
            foreach (var milk in MilkCatalog.MainMilks)
            {
                if (milk == null || milk.id == MilkCatalog.BasicMilkId)
                {
                    continue;
                }

                var wasUnlocked = unlockedBefore != null
                    && unlockedBefore.TryGetValue(milk.id, out var state)
                    && state;
                if (wasUnlocked || !manager.IsMilkUnlocked(milk.id))
                {
                    continue;
                }

                manager.RegisterMilkDiscovery(milk.id);
                manager.RegisterEventDiscovery($"{milk.id}_unlocked");
                message = CombineMessages(message, $"{milk.displayName}가 해금되었습니다.");
            }

            return message;
        }

        private static string RegisterSnackDiscovery(GameManager manager)
        {
            var message = manager.RegisterEventDiscovery("cheese_snack_fed")
                ? "치즈 간식 기록을 추가했습니다."
                : string.Empty;

            var tama = manager.CurrentTama;
            if (tama != null
                && tama.stats != null
                && tama.stats.cleanliness < 60
                && manager.RegisterEventDiscovery("crumbly_snack"))
            {
                message = CombineMessages(message, "부스러진 간식 순간을 기록했습니다.");
            }

            manager.RefreshDerivedCollectionRecords();
            return message;
        }

        private static CareEventResult RegisterRandomEvent(GameManager manager)
        {
            var eventResult = manager.TryRollCareEvent();
            if (!eventResult.occurred)
            {
                return eventResult;
            }

            manager.RegisterEventDiscovery(eventResult.eventId);
            return eventResult;
        }

        private static void PersistCareResult(GameManager manager, CareActionResult result)
        {
            if (!result.success)
            {
                return;
            }

            PersistAfterInteraction(manager);
        }

        private static void PersistAfterInteraction(GameManager manager)
        {
            manager.RefreshDerivedCollectionRecords();
            manager.SaveGame();
        }

        private static string CombineMessages(string primary, string secondary)
        {
            if (string.IsNullOrWhiteSpace(primary))
            {
                return secondary ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(secondary))
            {
                return primary;
            }

            return $"{primary} {secondary}";
        }

        private MilkDefinition GetMilkDefinition()
        {
            return action switch
            {
                MilkroomCareAction.FeedMilk => MilkCatalog.BasicMilk,
                MilkroomCareAction.FeedWarmMilk => MilkCatalog.WarmMilk,
                MilkroomCareAction.FeedColdMilk => MilkCatalog.ColdMilk,
                MilkroomCareAction.FeedNuttyMilk => MilkCatalog.NuttyMilk,
                MilkroomCareAction.FeedRichMilk => MilkCatalog.RichMilk,
                MilkroomCareAction.FeedFermentedMilk => MilkCatalog.FermentedMilk,
                MilkroomCareAction.FeedCoffeeMilk => MilkCatalog.CoffeeMilk,
                MilkroomCareAction.FeedStarMilk => MilkCatalog.StarMilk,
                _ => null
            };
        }

        private CheeseTamaVisualAction GetVisualAction(CareActionResult result, MilkDefinition milkDefinition)
        {
            if (result.hatched)
            {
                return CheeseTamaVisualAction.Hatch;
            }

            if (result.leveledUp)
            {
                return CheeseTamaVisualAction.LevelUp;
            }

            if (milkDefinition != null)
            {
                return CheeseTamaVisualAction.FeedMilk;
            }

            return action switch
            {
                MilkroomCareAction.FeedSnack => CheeseTamaVisualAction.FeedSnack,
                MilkroomCareAction.Play => CheeseTamaVisualAction.Play,
                MilkroomCareAction.Clean => CheeseTamaVisualAction.Clean,
                MilkroomCareAction.Rest => CheeseTamaVisualAction.Rest,
                _ => CheeseTamaVisualAction.Neutral
            };
        }

        private void Refresh(
            string message,
            GameManager manager,
            bool celebrate,
            string eventId = "",
            string eventMessage = "",
            CheeseTamaVisualAction visualAction = CheeseTamaVisualAction.Neutral,
            bool hideMilkPanelDuringMotion = false)
        {
            uiController.Bind(manager.CurrentSave);
            uiController.ShowMessage(message);
            uiController.ShowEventMessage(eventMessage);
            ResolveMilkPanel()?.Refresh();
            ResolveSnackPanel()?.Refresh();

            var visual = ResolveVisualController();
            if (visual != null)
            {
                visual.Bind(manager.CurrentTama);
                var shouldReact = celebrate
                    || !string.IsNullOrWhiteSpace(eventId)
                    || visualAction != CheeseTamaVisualAction.Neutral;
                if (!shouldReact)
                {
                    Debug.Log(message);
                    return;
                }

                if (string.IsNullOrWhiteSpace(eventId))
                {
                    visual.ReactAction(visualAction, celebrate);
                }
                else
                {
                    visual.ReactEvent(eventId, visualAction);
                }

                if (hideMilkPanelDuringMotion)
                {
                    ResolveMilkPanel()?.HideDuringReaction(visual);
                }
            }
            else
            {
                Debug.LogWarning("CheeseTama 비주얼 컨트롤러를 찾지 못했습니다.");
            }

            Debug.Log(message);
        }

        private void ResolveReferences()
        {
            if (uiController == null)
            {
                uiController = Object.FindFirstObjectByType<MilkroomUIController>();
            }

            if (visualController == null)
            {
                ResolveVisualController();
            }
        }

        private CheeseTamaVisualController ResolveVisualController()
        {
            if (visualController != null)
            {
                return visualController;
            }

            visualController = Object.FindFirstObjectByType<CheeseTamaVisualController>();
            if (visualController != null)
            {
                return visualController;
            }

            var eggObject = GameObject.Find("CheeseTama Egg Placeholder");
            if (eggObject == null)
            {
                return null;
            }

            visualController = eggObject.GetComponent<CheeseTamaVisualController>();
            if (visualController == null)
            {
                visualController = eggObject.AddComponent<CheeseTamaVisualController>();
            }

            return visualController;
        }

        private CookingPanelController ResolveCookingPanel()
        {
            if (cookingPanelController != null)
            {
                return cookingPanelController;
            }

            cookingPanelController = Object.FindFirstObjectByType<CookingPanelController>();
            return cookingPanelController;
        }

        private CookingChoicePanelController ResolveCookingChoicePanel()
        {
            if (cookingChoicePanelController != null)
            {
                return cookingChoicePanelController;
            }

            cookingChoicePanelController = Object.FindFirstObjectByType<CookingChoicePanelController>();
            return cookingChoicePanelController;
        }

        private bool TryOpenCookingChoice()
        {
            var cookingChoice = ResolveCookingChoicePanel();
            if (cookingChoice == null)
            {
                return false;
            }

            CloseToolPanels();
            return cookingChoice.Open();
        }

        private void CloseToolPanelsExcept(MilkroomCareAction activePanelAction)
        {
            if (activePanelAction != MilkroomCareAction.Blend)
            {
                ResolveCookingChoicePanel()?.Close();
                ResolveCookingPanel()?.Close();
            }

            if (activePanelAction != MilkroomCareAction.OpenMilkPanel)
            {
                ResolveMilkPanel()?.Close();
            }

            if (activePanelAction != MilkroomCareAction.OpenSnackPanel)
            {
                ResolveSnackPanel()?.Close();
            }
        }

        private void CloseToolPanels()
        {
            ResolveCookingChoicePanel()?.Close();
            ResolveCookingPanel()?.Close();
            ResolveMilkPanel()?.Close();
            ResolveSnackPanel()?.Close();
        }

        private MilkPanelController ResolveMilkPanel()
        {
            if (milkPanelController != null)
            {
                return milkPanelController;
            }

            milkPanelController = Object.FindFirstObjectByType<MilkPanelController>();
            return milkPanelController;
        }

        private SnackPanelController ResolveSnackPanel()
        {
            if (snackPanelController != null)
            {
                return snackPanelController;
            }

            snackPanelController = Object.FindFirstObjectByType<SnackPanelController>();
            return snackPanelController;
        }
    }
}
