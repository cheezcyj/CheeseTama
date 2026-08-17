using System;
using System.Collections.Generic;
using CheeseTama.Collections.HiddenCareers;
using CheeseTama.Core;
using CheeseTama.Gameplay.Bond;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Keeps the optional late-game views synchronized with the authoritative
    /// GameManager. Hidden-career rules stay behind GameManager's presentation
    /// boundary; this bridge only receives already-unlocked card view data.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LateGameFeatureBridge : MonoBehaviour
    {
        [SerializeField] private StarLegacyPanelController starLegacyPanel;
        [SerializeField] private BondStatusPanelController bondStatusPanel;
        [SerializeField] private HiddenCareerCardPanelController hiddenCareerPanel;
        [SerializeField] private BondReactionPresenter bondReactionPresenter;
        [SerializeField] private EmmentalConstellationPresenter constellationPresenter;
        [SerializeField] private CheeseTamaVisualController visualController;
        [SerializeField] private MilkroomUIController milkroomUi;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController bottomActionBarController;
        [SerializeField] private DevPanelController devPanelController;
        [SerializeField] private Button starEggButton;
        [SerializeField] private Text starLegacyStatusText;
        [SerializeField] private Button[] modalStateButtons;

        private GameManager boundManager;
        private bool configured;
        private bool controlsSuspended;
        private bool topMenuWasEnabled;
        private bool bottomBarWasEnabled;
        private bool devPanelWasEnabled;
        private bool milkroomUiWasEnabled;
        private bool starGenerationConfirmationPending;

        public GameManager BoundManager => boundManager;
        public bool IsGameplayBlocked => controlsSuspended;

        public void Configure(
            GameManager manager,
            StarLegacyPanelController starPanel,
            BondStatusPanelController bondPanel,
            HiddenCareerCardPanelController hiddenPanel,
            BondReactionPresenter reactionPresenter,
            EmmentalConstellationPresenter constellation,
            CheeseTamaVisualController visual,
            MilkroomUIController roomUi,
            TopMenuController topMenu,
            BottomActionBarController actionBar,
            DevPanelController developerPanel,
            Button selectStarEggButton,
            Text starStatusText,
            params Button[] modalButtons)
        {
            UnbindUiListeners();
            BindManager(null);
            RestoreControls();

            starLegacyPanel = starPanel;
            bondStatusPanel = bondPanel;
            hiddenCareerPanel = hiddenPanel;
            bondReactionPresenter = reactionPresenter;
            constellationPresenter = constellation;
            visualController = visual;
            milkroomUi = roomUi;
            topMenuController = topMenu;
            bottomActionBarController = actionBar;
            devPanelController = developerPanel;
            starEggButton = selectStarEggButton;
            starLegacyStatusText = starStatusText;
            modalStateButtons = modalButtons ?? Array.Empty<Button>();
            configured = starLegacyPanel != null
                && bondStatusPanel != null
                && hiddenCareerPanel != null;

            BindUiListeners();
            BindManager(manager);
            RefreshAll();
            RefreshGameplayBlock();
        }

        private void OnEnable()
        {
            if (!configured)
            {
                return;
            }

            BindUiListeners();
            BindManager(boundManager != null ? boundManager : GameManager.Instance);
            RefreshAll();
        }

        private void OnDisable()
        {
            UnbindUiListeners();
            BindManager(null);
            RestoreControls();
        }

        private void OnDestroy()
        {
            UnbindUiListeners();
            BindManager(null);
            RestoreControls();
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            if (boundManager == null && GameManager.Instance != null)
            {
                BindManager(GameManager.Instance);
                RefreshAll();
            }

            RefreshGameplayBlock();
        }

        /// <summary>
        /// Callback used by StarLegacyPanelController, which acquires the modal
        /// block before activating its overlay.
        /// </summary>
        public void SetStarPanelBlocking(bool blocked)
        {
            ApplyGameplayBlock(blocked || IsAnyLateGamePanelOpen());
        }

        public void RefreshAll()
        {
            var manager = boundManager;
            if (manager == null || manager.CurrentSave == null)
            {
                bondStatusPanel?.Bind(null);
                hiddenCareerPanel?.Bind(Array.Empty<HiddenCareerCardViewData>());
                starLegacyPanel?.Refresh();
                constellationPresenter?.SetVisible(false);
                RefreshStarEggButton(null);
                return;
            }

            bondStatusPanel?.Bind(manager.GetBondProfile());
            hiddenCareerPanel?.Bind(manager.GetVisibleHiddenCareerCards());
            starLegacyPanel?.Refresh();
            if (visualController != null)
            {
                visualController.Bind(manager.CurrentTama);
            }

            constellationPresenter?.Bind(manager.CurrentTama);
            RefreshStarEggButton(manager);
        }

        public static bool TryMapCareAction(
            string actionId,
            out BondInteraction interaction,
            out string subjectId)
        {
            interaction = BondInteraction.Ambient;
            subjectId = string.Empty;
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            var normalized = actionId.Trim();
            if (string.Equals(normalized, "pet", StringComparison.Ordinal))
            {
                interaction = BondInteraction.Pet;
                return true;
            }

            if (string.Equals(normalized, "play", StringComparison.Ordinal))
            {
                interaction = BondInteraction.Play;
                return true;
            }

            if (string.Equals(normalized, "clean", StringComparison.Ordinal))
            {
                interaction = BondInteraction.Clean;
                return true;
            }

            if (string.Equals(normalized, "rest", StringComparison.Ordinal))
            {
                interaction = BondInteraction.Rest;
                return true;
            }

            if (string.Equals(normalized, "cook", StringComparison.Ordinal)
                || string.Equals(normalized, "blend", StringComparison.Ordinal))
            {
                interaction = BondInteraction.Cook;
                return true;
            }

            if (!normalized.StartsWith("feed_", StringComparison.Ordinal))
            {
                return false;
            }

            interaction = BondInteraction.Feed;
            var milks = MilkCatalog.VisibleMilks;
            for (var index = 0; index < milks.Length; index += 1)
            {
                var milk = milks[index];
                if (milk != null
                    && string.Equals(milk.actionId, normalized, StringComparison.Ordinal))
                {
                    subjectId = milk.id;
                    break;
                }
            }

            return true;
        }

        private void BindManager(GameManager manager)
        {
            if (boundManager == manager)
            {
                return;
            }

            if (boundManager != null)
            {
                boundManager.SaveDataReplaced -= HandleSaveDataReplaced;
                boundManager.CareActionRegistered -= HandleCareActionRegistered;
                boundManager.ReturnSummaryAvailable -= HandleReturnSummaryAvailable;
                boundManager.EvolutionMilestoneAvailable -= HandleEvolutionMilestoneAvailable;
                boundManager.StarLegacyChanged -= HandleStarLegacyChanged;
                boundManager.HiddenCareerCardChanged -= HandleHiddenCareerCardChanged;
            }

            boundManager = manager;
            if (boundManager != null && isActiveAndEnabled)
            {
                boundManager.SaveDataReplaced += HandleSaveDataReplaced;
                boundManager.CareActionRegistered += HandleCareActionRegistered;
                boundManager.ReturnSummaryAvailable += HandleReturnSummaryAvailable;
                boundManager.EvolutionMilestoneAvailable += HandleEvolutionMilestoneAvailable;
                boundManager.StarLegacyChanged += HandleStarLegacyChanged;
                boundManager.HiddenCareerCardChanged += HandleHiddenCareerCardChanged;
            }
        }

        private void BindUiListeners()
        {
            if (starEggButton != null)
            {
                starEggButton.onClick.RemoveListener(HandleStarEggClicked);
                starEggButton.onClick.AddListener(HandleStarEggClicked);
            }

            if (modalStateButtons == null)
            {
                return;
            }

            for (var index = 0; index < modalStateButtons.Length; index += 1)
            {
                var button = modalStateButtons[index];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveListener(RefreshGameplayBlock);
                button.onClick.AddListener(RefreshGameplayBlock);
            }
        }

        private void UnbindUiListeners()
        {
            starEggButton?.onClick.RemoveListener(HandleStarEggClicked);
            if (modalStateButtons == null)
            {
                return;
            }

            for (var index = 0; index < modalStateButtons.Length; index += 1)
            {
                modalStateButtons[index]?.onClick.RemoveListener(RefreshGameplayBlock);
            }
        }

        private void HandleSaveDataReplaced()
        {
            RefreshAll();
        }

        private void HandleCareActionRegistered(string actionId)
        {
            RefreshAll();
            if (boundManager == null
                || !TryMapCareAction(actionId, out var interaction, out var subjectId))
            {
                return;
            }

            bondReactionPresenter?.Present(
                boundManager.GetBondReaction(interaction, subjectId),
                false,
                true);
        }

        private void HandleReturnSummaryAvailable(ReturnSummaryData summary)
        {
            if (boundManager == null || summary == null)
            {
                return;
            }

            bondReactionPresenter?.Present(
                boundManager.GetBondReaction(BondInteraction.Return),
                false,
                false);
        }

        private void HandleEvolutionMilestoneAvailable(EvolutionMilestoneData milestone)
        {
            RefreshAll();
        }

        private void HandleStarLegacyChanged()
        {
            RefreshAll();
        }

        private void HandleHiddenCareerCardChanged()
        {
            hiddenCareerPanel?.Bind(
                boundManager != null && boundManager.CurrentSave != null
                    ? boundManager.GetVisibleHiddenCareerCards()
                    : Array.Empty<HiddenCareerCardViewData>());
        }

        private void HandleStarEggClicked()
        {
            var manager = boundManager;
            if (manager == null)
            {
                return;
            }

            if (!starGenerationConfirmationPending)
            {
                starGenerationConfirmationPending = true;
                if (starLegacyStatusText != null)
                {
                    starLegacyStatusText.text =
                        "별빛 알과 새 세대를 시작할까요? 이름과 계정 기록은 유지되고, 현재 성장 진행은 새 알부터 시작합니다. 다시 누르면 시작합니다.";
                }

                return;
            }

            starGenerationConfirmationPending = false;
            var applied = manager.AdoptStarEgg();
            if (starLegacyStatusText != null)
            {
                starLegacyStatusText.text = applied
                    ? "새로운 별빛 알의 여정이 시작되었어요."
                    : "지금은 별빛 알의 여정을 시작할 수 없어요.";
            }

            RefreshAll();
        }

        private void RefreshStarEggButton(GameManager manager)
        {
            if (starEggButton == null)
            {
                return;
            }

            var snapshot = manager != null && manager.CurrentSave != null
                ? manager.GetStarLegacyViewModel()
                : StarLegacyPanelViewModel.Hidden();
            var visible = snapshot.visible
                && manager?.CurrentSave?.unlocks != null
                && manager.CurrentSave.unlocks.starEggUnlocked
                && manager.CurrentTama != null
                && !StarEggEmmentalEvolutionSystem.IsStarEggOrigin(manager.CurrentTama);
            starEggButton.gameObject.SetActive(visible);
            if (!visible)
            {
                starGenerationConfirmationPending = false;
            }
            starEggButton.interactable = visible;
        }

        private void RefreshGameplayBlock()
        {
            ApplyGameplayBlock(IsAnyLateGamePanelOpen());
        }

        private bool IsAnyLateGamePanelOpen()
        {
            return (starLegacyPanel != null && starLegacyPanel.IsBlockingGameplay)
                || (bondStatusPanel != null && bondStatusPanel.IsOpen)
                || (hiddenCareerPanel != null && hiddenCareerPanel.IsOpen);
        }

        private void ApplyGameplayBlock(bool blocked)
        {
            if (blocked == controlsSuspended)
            {
                return;
            }

            if (blocked)
            {
                topMenuWasEnabled = topMenuController != null && topMenuController.enabled;
                bottomBarWasEnabled = bottomActionBarController != null
                    && bottomActionBarController.enabled;
                devPanelWasEnabled = devPanelController != null && devPanelController.enabled;
                milkroomUiWasEnabled = milkroomUi != null && milkroomUi.enabled;
                if (topMenuController != null) topMenuController.enabled = false;
                if (bottomActionBarController != null) bottomActionBarController.enabled = false;
                if (devPanelController != null) devPanelController.enabled = false;
                if (milkroomUi != null) milkroomUi.enabled = false;
                controlsSuspended = true;
                return;
            }

            RestoreControls();
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null) topMenuController.enabled = topMenuWasEnabled;
            if (bottomActionBarController != null)
            {
                bottomActionBarController.enabled = bottomBarWasEnabled;
            }

            if (devPanelController != null) devPanelController.enabled = devPanelWasEnabled;
            if (milkroomUi != null) milkroomUi.enabled = milkroomUiWasEnabled;
            controlsSuspended = false;
        }
    }
}
