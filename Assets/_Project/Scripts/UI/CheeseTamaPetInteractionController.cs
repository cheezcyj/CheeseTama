using CheeseTama.Core;
using CheeseTama.Gameplay.Care;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTama.UI
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CheeseTamaPetInteractionController : MonoBehaviour
    {
        private const float MinimumStrokeScreenRatio = 0.035f;
        private const float MinimumStrokeSeconds = 0.12f;
        private const float InteractionCooldownSeconds = 2.5f;

        private static readonly string[] BlockingUiNames =
        {
            "CheeseTama Name Dialog",
            "Settings Modal",
            "Confirm Reset Dialog",
            "Decorate Overlay",
            "Milk Panel",
            "Cooking Panel",
            "Snack Panel",
            "Dev Panel",
            "Return Summary Overlay",
            "Growth Achievement Overlay",
            "Milk Drop Catch Overlay",
            "Care Event Overlay"
            ,"Cleaning Mini Game Overlay"
            ,"Evolution Achievement Overlay"
            ,"Decoration Shop Overlay"
            ,"New Game Setup Overlay"
            ,"Growth Journey Overlay"
            ,"Play Choice Overlay"
            ,"Bouncy Jump Overlay"
            ,FirstDayJourneyController.OverlayObjectName
            ,"Cheese Star Delivery Overlay"
            ,"Memory Journal Overlay"
            ,"Fantasy Powder Overlay"
            ,SaveRecoveryNoticeController.OverlayObjectName
            ,CheeseTamaProfileMenuController.OverlayObjectName
            ,"Star Legacy Overlay"
            ,"Bond Status Overlay"
            ,"Hidden Career Card Overlay"
            ,InputBindingsPanelController.OverlayObjectName
            ,"Milk Blending Overlay"
            ,CookingChoicePanelController.OverlayObjectName
            ,NpcVisitCardController.OverlayObjectName
            ,JourneyHubPanelController.OverlayObjectName
            ,SleepSchedulePanelController.OverlayObjectName
        };

        [SerializeField] private BoxCollider interactionCollider;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private Transform uiRoot;

        private readonly CareActionSystem careActions = new CareActionSystem();
        private Transform observedModel;
        private Camera interactionCamera;
        private bool strokeActive;
        private Vector2 previousPointerPosition;
        private float accumulatedStrokeDistance;
        private float strokeStartedAt;
        private float cooldownUntil;

        public void Configure(
            MilkroomUIController uiController,
            CheeseTamaVisualController tamaVisual,
            Transform canvasRoot)
        {
            milkroomUi = uiController;
            visualController = tamaVisual != null ? tamaVisual : GetComponent<CheeseTamaVisualController>();
            uiRoot = canvasRoot;
            interactionCollider ??= GetComponent<BoxCollider>();
            interactionCollider.isTrigger = true;
            RefreshInteractionBounds(true);
        }

        private void Awake()
        {
            interactionCollider ??= GetComponent<BoxCollider>();
            visualController ??= GetComponent<CheeseTamaVisualController>();
            interactionCollider.isTrigger = true;
            RefreshInteractionBounds(true);
        }

        private void OnDisable()
        {
            CancelStroke();
        }

        private void Update()
        {
            RefreshInteractionBounds(false);

            if (Input.GetMouseButtonDown(0))
            {
                BeginStroke();
            }

            if (!strokeActive)
            {
                return;
            }

            if (IsGameplayBlocked() || !Input.GetMouseButton(0))
            {
                if (Input.GetMouseButtonUp(0))
                {
                    FinishStroke();
                }
                else
                {
                    CancelStroke();
                }

                return;
            }

            var currentPointer = (Vector2)Input.mousePosition;
            if (!IsPointerOverCollider(currentPointer))
            {
                CancelStroke();
                return;
            }

            accumulatedStrokeDistance += Vector2.Distance(previousPointerPosition, currentPointer)
                / Mathf.Max(1f, Screen.height);
            previousPointerPosition = currentPointer;

            if (Input.GetMouseButtonUp(0))
            {
                FinishStroke();
            }
        }

        private void BeginStroke()
        {
            if (Time.unscaledTime < cooldownUntil || IsGameplayBlocked())
            {
                return;
            }

            var pointerPosition = (Vector2)Input.mousePosition;
            if (!IsPointerOverCollider(pointerPosition))
            {
                return;
            }

            strokeActive = true;
            previousPointerPosition = pointerPosition;
            accumulatedStrokeDistance = 0f;
            strokeStartedAt = Time.unscaledTime;
        }

        private void FinishStroke()
        {
            if (!strokeActive)
            {
                return;
            }

            var elapsed = Time.unscaledTime - strokeStartedAt;
            var completed = !IsGameplayBlocked()
                && elapsed >= MinimumStrokeSeconds
                && accumulatedStrokeDistance >= MinimumStrokeScreenRatio;
            CancelStroke();
            if (completed)
            {
                CommitPet();
            }
        }

        private void CommitPet()
        {
            var manager = GameManager.Instance;
            if (manager == null || manager.CurrentTama == null)
            {
                return;
            }

            careActions.ConfigureLateLevelGrowth(
                manager.CurrentSave?.lateLevelGrowth,
                manager.CurrentSave?.milkGrowth);
            var result = careActions.Pet(manager.CurrentTama);
            if (!result.success)
            {
                return;
            }

            cooldownUntil = Time.unscaledTime + InteractionCooldownSeconds;
            manager.RegisterCareAction("pet");
            manager.RefreshDerivedCollectionRecords();
            manager.SaveGame();

            milkroomUi?.Bind(manager.CurrentSave);
            milkroomUi?.ShowMessage(result.message);
            visualController?.Bind(manager.CurrentTama);
            if (visualController != null)
            {
                visualController.ReactAction(
                    result.hatched
                        ? CheeseTamaVisualAction.Hatch
                        : result.leveledUp
                            ? CheeseTamaVisualAction.LevelUp
                            : CheeseTamaVisualAction.Pet,
                    result.hatched);
            }
        }

        private bool IsGameplayBlocked()
        {
            if (!Application.isPlaying)
            {
                return true;
            }

            if (GameManager.Instance?.IsSleepScheduleActive == true)
            {
                return true;
            }

            var onboarding = uiRoot != null
                ? uiRoot.GetComponent<FirstMeetingOnboardingController>()
                : null;
            if (onboarding != null && onboarding.IsBlockingGameplay)
            {
                return true;
            }

            var returnSummary = uiRoot != null
                ? uiRoot.GetComponent<ReturnSummaryController>()
                : null;
            if (returnSummary != null && returnSummary.IsBlockingGameplay)
            {
                return true;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            if (uiRoot == null)
            {
                return false;
            }

            foreach (var blockerName in BlockingUiNames)
            {
                var blocker = uiRoot.Find(blockerName);
                if (blocker != null && blocker.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPointerOverCollider(Vector2 screenPosition)
        {
            if (interactionCollider == null)
            {
                return false;
            }

            interactionCamera ??= Camera.main;
            if (interactionCamera == null)
            {
                return false;
            }

            var ray = interactionCamera.ScreenPointToRay(screenPosition);
            return interactionCollider.Raycast(ray, out _, 1000f);
        }

        private void RefreshInteractionBounds(bool force)
        {
            interactionCollider ??= GetComponent<BoxCollider>();
            visualController ??= GetComponent<CheeseTamaVisualController>();
            var model = visualController != null ? visualController.ModelInstance : null;
            if (!force && observedModel == model)
            {
                return;
            }

            observedModel = model;
            if (model == null)
            {
                interactionCollider.center = new Vector3(0f, 0.35f, 0f);
                interactionCollider.size = new Vector3(1.4f, 1.9f, 0.9f);
                return;
            }

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var worldBounds = new Bounds();
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                interactionCollider.center = new Vector3(0f, 0.35f, 0f);
                interactionCollider.size = new Vector3(1.4f, 1.9f, 0.9f);
                return;
            }

            interactionCollider.center = transform.InverseTransformPoint(worldBounds.center);
            var scale = transform.lossyScale;
            interactionCollider.size = new Vector3(
                worldBounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)) + 0.18f,
                worldBounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)) + 0.18f,
                Mathf.Max(0.5f, worldBounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)) + 0.18f));
        }

        private void CancelStroke()
        {
            strokeActive = false;
            accumulatedStrokeDistance = 0f;
        }
    }
}
