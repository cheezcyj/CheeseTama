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
                var result = manager.ApplyTimeSkipHours(1);
                Refresh(result.ToSummary("In the milkroom,"), manager, false);
                return;
            }

            var result = RunCareAction(manager);
            Refresh(result.message, manager, result.hatched);
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

        private void Refresh(string message, GameManager manager, bool celebrate)
        {
            uiController.Bind(manager.CurrentTama);
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
