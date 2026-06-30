using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public enum DebugAction
    {
        SetHungry,
        SetSleepy,
        SetMessy,
        SetUnwell,
        SetCheerful,
        HatchNow,
        UnlockStarMilk,
        ResetSave,
        ForceEvent,
        AddSessionFiveMinutes
    }

    [RequireComponent(typeof(Button))]
    public sealed class DebugActionButton : MonoBehaviour
    {
        private const string StarMilkId = "star_milk";

        [SerializeField] private DebugAction action;
        [SerializeField] private DebugUIController uiController;
        [SerializeField] private CheeseTamaVisualController visualController;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            EnsureButtonListener();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Configure(
            DebugAction debugAction,
            DebugUIController debugUi,
            CheeseTamaVisualController cheeseTamaVisual)
        {
            action = debugAction;
            uiController = debugUi;
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

            if (action == DebugAction.ResetSave)
            {
                manager.ResetGame();
                Refresh("CheeseTama 저장 데이터를 초기화했습니다.", manager, false);
                return;
            }

            var tama = manager.CurrentTama;
            if (tama == null)
            {
                Refresh("CheeseTama 저장 데이터를 사용할 수 없습니다.", manager, false);
                return;
            }

            tama.EnsureRuntimeDefaults();
            var celebrate = false;
            var eventId = string.Empty;
            var message = ApplyAction(manager, tama, ref celebrate, ref eventId);

            manager.RefreshDerivedCollectionRecords();
            manager.SaveGame();
            Refresh(message, manager, celebrate, eventId);
        }

        private string ApplyAction(GameManager manager, CheeseTamaModel tama, ref bool celebrate, ref string eventId)
        {
            if (IsConditionPreset(action))
            {
                EnsureHatched(manager, tama);
                ApplyNeutralStats(tama);
            }

            switch (action)
            {
                case DebugAction.SetHungry:
                    tama.stats.hunger = 10;
                    tama.stats.mood = 45;
                    tama.stats.health = 90;
                    return "개발자 프리셋 적용: 배고픈 CheeseTama.";
                case DebugAction.SetSleepy:
                    tama.stats.sleepiness = 90;
                    tama.stats.mood = 45;
                    return "개발자 프리셋 적용: 졸린 CheeseTama.";
                case DebugAction.SetMessy:
                    tama.stats.cleanliness = 15;
                    tama.stats.mood = 50;
                    return "개발자 프리셋 적용: 지저분한 CheeseTama.";
                case DebugAction.SetUnwell:
                    tama.stats.health = 20;
                    tama.stats.hunger = 45;
                    tama.stats.cleanliness = 45;
                    return "개발자 프리셋 적용: 아픈 CheeseTama.";
                case DebugAction.SetCheerful:
                    tama.stats.mood = 95;
                    tama.stats.affection = 35;
                    tama.stats.hunger = 85;
                    return "개발자 프리셋 적용: 신난 CheeseTama.";
                case DebugAction.HatchNow:
                    EnsureHatched(manager, tama);
                    celebrate = true;
                    return "개발자 부화 적용: 말랑 CheeseTama가 깨어났습니다.";
                case DebugAction.UnlockStarMilk:
                    manager.UnlockStarMilk();
                    manager.RegisterMilkDiscovery(StarMilkId);
                    manager.RegisterEventDiscovery("star_milk_unlocked");
                    return "개발자 해금 적용: 별빛 우유를 사용할 수 있습니다.";
                case DebugAction.ForceEvent:
                    var eventResult = ForceCareEvent(manager);
                    eventId = eventResult.eventId;
                    return eventResult.message;
                case DebugAction.AddSessionFiveMinutes:
                    return manager.TickMilkroomPresence(300);
                default:
                    return "선택된 개발자 액션이 없습니다.";
            }
        }

        private static CareEventResult ForceCareEvent(GameManager manager)
        {
            var eventResult = manager.ForceCareEvent();
            if (!eventResult.occurred)
            {
                return new CareEventResult(false, string.Empty, "발생한 개발자 이벤트가 없습니다.");
            }

            manager.RegisterEventDiscovery(eventResult.eventId);
            return eventResult;
        }

        private static bool IsConditionPreset(DebugAction debugAction)
        {
            return debugAction == DebugAction.SetHungry
                || debugAction == DebugAction.SetSleepy
                || debugAction == DebugAction.SetMessy
                || debugAction == DebugAction.SetUnwell
                || debugAction == DebugAction.SetCheerful;
        }

        private static void ApplyNeutralStats(CheeseTamaModel tama)
        {
            tama.stats ??= StatBlock.CreateDefault();
            tama.stats.hunger = 80;
            tama.stats.mood = 70;
            tama.stats.cleanliness = 90;
            tama.stats.sleepiness = 20;
            tama.stats.health = 100;
            tama.stats.maturation = 0;
            tama.stats.affection = 10;
            tama.stats.milkSatisfaction = 50;
        }

        private static void EnsureHatched(GameManager manager, CheeseTamaModel tama)
        {
            if (!tama.isHatched)
            {
                tama.isHatched = true;
                tama.name = "말랑 CheeseTama";
                tama.form = "soft_cheesetama";
            }

            if (tama.level < HatchingSystem.HatchLevel)
            {
                tama.level = HatchingSystem.HatchLevel;
            }

            tama.levelProgress = 0;
            manager.RegisterCurrentEvolutionDiscovery();
        }

        private void Refresh(string message, GameManager manager, bool celebrate, string eventId = "")
        {
            uiController?.Bind(manager.CurrentSave);
            uiController?.ShowMessage(message);

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
                uiController = Object.FindFirstObjectByType<DebugUIController>();
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
