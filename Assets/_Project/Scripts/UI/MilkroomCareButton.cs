using CheeseTama.Core;
using CheeseTama.Gameplay.Care;
using CheeseTama.Gameplay.Events;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public enum MilkroomCareAction
    {
        FeedMilk,
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
        Blend
    }

    [RequireComponent(typeof(Button))]
    public sealed class MilkroomCareButton : MonoBehaviour
    {
        private const string BasicMilkId = "basic_milk";
        private const string StarMilkId = "star_milk";
        private const int StarMilkUnlockBasicMilkLevel = 2;

        [SerializeField] private MilkroomCareAction action;
        [SerializeField] private MilkroomUIController uiController;
        [SerializeField] private CheeseTamaVisualController visualController;

        private readonly CareActionSystem careActions = new CareActionSystem();
        private Button button;

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
            action = careAction;
            uiController = milkroomUi;
            visualController = cheeseTamaVisual;
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

            if (action == MilkroomCareAction.WaitHour)
            {
                var timeResult = manager.ApplyTimeSkipHours(1);
                manager.RegisterCareAction("wait_hour", timeResult.applied ? timeResult.hours : 1);
                var timeEvent = RegisterRandomEvent(manager);
                PersistAfterInteraction(manager);
                Refresh(timeResult.ToSummary("밀크룸에서"), manager, false, timeEvent.eventId, timeEvent.message);
                return;
            }

            if (action == MilkroomCareAction.CatchMilkDrops)
            {
                var message = manager.PlayMilkDropCatch();
                PersistAfterInteraction(manager);
                Refresh(message, manager, false, "milk_drop_catch");
                return;
            }

            if (action == MilkroomCareAction.Blend)
            {
                manager.RegisterCareAction("blend");
                PersistAfterInteraction(manager);
                Refresh("조합 테이블이 따뜻하게 준비되고 있습니다.", manager, false, "happy_wiggle");
                return;
            }

            if (action == MilkroomCareAction.FeedStarMilk && !manager.IsMilkUnlocked(StarMilkId))
            {
                Refresh("별빛 우유는 잠겨 있습니다. 기본 우유를 레벨 2까지 올리세요.", manager, false);
                return;
            }

            var careResult = RunCareAction(manager);
            var routineMessage = RegisterCareHistory(manager, careResult);
            var discoveryMessage = RegisterCollectionDiscoveries(manager, careResult);
            var eventResult = careResult.hatched ? CareEventResult.None() : RegisterRandomEvent(manager);
            PersistCareResult(manager, careResult);
            Refresh(
                CombineMessages(CombineMessages(careResult.message, routineMessage), discoveryMessage),
                manager,
                careResult.hatched,
                eventResult.eventId,
                eventResult.message);
        }

        private CareActionResult RunCareAction(GameManager manager)
        {
            return action switch
            {
                MilkroomCareAction.FeedMilk => careActions.FeedMilk(manager.CurrentTama),
                MilkroomCareAction.FeedStarMilk => careActions.FeedStarMilk(manager.CurrentTama),
                MilkroomCareAction.FeedSnack => careActions.FeedSnack(manager.CurrentTama),
                MilkroomCareAction.Play => careActions.Play(manager.CurrentTama),
                MilkroomCareAction.Clean => careActions.Clean(manager.CurrentTama),
                MilkroomCareAction.Rest => careActions.Rest(manager.CurrentTama),
                _ => new CareActionResult(false, false, "선택된 돌봄 행동이 없습니다.")
            };
        }

        private string RegisterCareHistory(GameManager manager, CareActionResult result)
        {
            if (!result.success)
            {
                return string.Empty;
            }

            var actionId = GetCareActionId();
            if (!string.IsNullOrWhiteSpace(actionId))
            {
                manager.RegisterCareAction(actionId);
                return manager.RegisterDailyCareAction(actionId)
                    ? "오늘 돌봄 루틴을 완료했습니다."
                    : string.Empty;
            }

            return string.Empty;
        }

        private string GetCareActionId()
        {
            return action switch
            {
                MilkroomCareAction.FeedMilk => "feed_milk",
                MilkroomCareAction.FeedStarMilk => "feed_star_milk",
                MilkroomCareAction.FeedSnack => "feed_snack",
                MilkroomCareAction.Play => "play",
                MilkroomCareAction.Clean => "clean",
                MilkroomCareAction.Rest => "rest",
                _ => string.Empty
            };
        }

        private string RegisterCollectionDiscoveries(GameManager manager, CareActionResult result)
        {
            if (manager == null || !result.success)
            {
                return string.Empty;
            }

            var message = string.Empty;
            if (action == MilkroomCareAction.FeedMilk)
            {
                message = RegisterMilkProgress(manager, BasicMilkId, 1);
                var growth = manager.FindMilkGrowth(BasicMilkId);
                if (growth != null
                    && growth.growthLevel >= StarMilkUnlockBasicMilkLevel
                    && manager.UnlockStarMilk())
                {
                    manager.RegisterMilkDiscovery(StarMilkId);
                    manager.RegisterEventDiscovery("star_milk_unlocked");
                    message = CombineMessages(message, "별빛 우유가 해금되었습니다.");
                }
            }

            if (action == MilkroomCareAction.FeedStarMilk)
            {
                message = RegisterMilkProgress(manager, StarMilkId, 2);
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

        private static string RegisterMilkProgress(GameManager manager, string milkId, int growthPoints)
        {
            var previousGrowth = manager.FindMilkGrowth(milkId);
            var previousLevel = previousGrowth?.growthLevel ?? 0;
            manager.RegisterMilkDiscovery(milkId);

            var growth = manager.RegisterMilkGrowth(milkId, growthPoints);
            manager.RefreshDerivedCollectionRecords();
            if (growth == null || growth.growthLevel <= previousLevel)
            {
                return string.Empty;
            }

            return $"{FormatMilkName(milkId)} 레벨 {growth.growthLevel} 달성.";
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
                message = CombineMessages(message, "부스러지는 간식 순간을 기록했습니다.");
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

        private static string FormatMilkName(string milkId)
        {
            return milkId == StarMilkId ? "별빛 우유" : "기본 우유";
        }

        private void Refresh(string message, GameManager manager, bool celebrate, string eventId = "", string eventMessage = "")
        {
            uiController.Bind(manager.CurrentSave);
            uiController.ShowMessage(message);
            uiController.ShowEventMessage(eventMessage);

            var visual = ResolveVisualController();
            if (visual != null)
            {
                visual.Bind(manager.CurrentTama);
                if (string.IsNullOrWhiteSpace(eventId))
                {
                    visual.React(celebrate);
                }
                else
                {
                    visual.ReactEvent(eventId);
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
    }
}
