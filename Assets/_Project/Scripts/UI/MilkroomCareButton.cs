using CheeseTama.Core;
using CheeseTama.Gameplay.Care;
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
        WaitHour
    }

    [RequireComponent(typeof(Button))]
    public sealed class MilkroomCareButton : MonoBehaviour
    {
        private const string BasicMilkId = "basic_milk";

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
                Debug.LogWarning("MilkroomCareButton could not find MilkroomUIController.");
                return;
            }

            if (action == MilkroomCareAction.Save)
            {
                manager.SaveGame();
                Refresh("Saved CheeseTama test data.", manager, false);
                return;
            }

            if (action == MilkroomCareAction.Reload)
            {
                manager.ReloadGame();
                var reloadMessage = manager.LastTimeProgression.applied
                    ? manager.LastTimeProgression.ToSummary("While away,")
                    : "Reloaded CheeseTama save data.";
                Refresh(reloadMessage, manager, false);
                return;
            }

            if (action == MilkroomCareAction.Reset)
            {
                manager.ResetGame();
                Refresh("Reset CheeseTama save data.", manager, false);
                return;
            }

            if (action == MilkroomCareAction.WaitHour)
            {
                var timeResult = manager.ApplyTimeSkipHours(1);
                Refresh(timeResult.ToSummary("In the milkroom,"), manager, false);
                return;
            }

            var careResult = RunCareAction(manager);
            var discoveryMessage = RegisterCollectionDiscoveries(manager, careResult);
            Refresh(CombineMessages(careResult.message, discoveryMessage), manager, careResult.hatched);
        }

        private CareActionResult RunCareAction(GameManager manager)
        {
            return action switch
            {
                MilkroomCareAction.FeedMilk => careActions.FeedMilk(manager.CurrentTama),
                MilkroomCareAction.Play => careActions.Play(manager.CurrentTama),
                MilkroomCareAction.Clean => careActions.Clean(manager.CurrentTama),
                MilkroomCareAction.Rest => careActions.Rest(manager.CurrentTama),
                _ => new CareActionResult(false, false, "No care action was selected.")
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
                var previousGrowth = manager.FindMilkGrowth(BasicMilkId);
                var previousLevel = previousGrowth?.growthLevel ?? 0;
                manager.RegisterMilkDiscovery(BasicMilkId);

                var growth = manager.RegisterMilkGrowth(BasicMilkId, 1);
                if (growth != null && growth.growthLevel > previousLevel)
                {
                    var eventId = $"{BasicMilkId}_growth_lv_{growth.growthLevel}";
                    manager.RegisterEventDiscovery(eventId);
                    message = $"Basic Milk reached Lv. {growth.growthLevel}.";
                }
            }

            if (result.hatched)
            {
                manager.RegisterCurrentEvolutionDiscovery();
            }

            return message;
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

        private void Refresh(string message, GameManager manager, bool celebrate)
        {
            uiController.Bind(manager.CurrentSave);
            uiController.ShowMessage(message);

            var visual = ResolveVisualController();
            if (visual != null)
            {
                visual.Bind(manager.CurrentTama);
                visual.React(celebrate);
            }
            else
            {
                Debug.LogWarning("MilkroomCareButton could not find CheeseTama visual controller.");
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
