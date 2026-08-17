using System;
using CheeseTama.Gameplay.NpcVisits;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class NpcVisitCardController : MonoBehaviour
    {
        public const string OverlayObjectName = "Npc Visit Overlay";

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text portraitLabel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text roleLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Text relationshipLabel;
        [SerializeField] private Button firstChoiceButton;
        [SerializeField] private Text firstChoiceLabel;
        [SerializeField] private Button secondChoiceButton;
        [SerializeField] private Text secondChoiceLabel;
        [SerializeField] private Button laterButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TopMenuController topMenuController;
        [SerializeField] private BottomActionBarController actionBarController;
        [SerializeField] private DevPanelController developerPanelController;

        private NpcVisitOffer displayed;
        private Func<string, string, NpcVisitResolutionResult> resolveChoice;
        private Action later;
        private bool showingResult;
        private bool controlsSuspended;
        private bool previousTopEnabled;
        private bool previousBottomEnabled;
        private bool previousDevEnabled;
        private GameObject previousSelected;

        public bool IsBlockingGameplay => overlayRoot != null && overlayRoot.activeSelf;
        public string DisplayedOccurrenceId => displayed?.OccurrenceId ?? string.Empty;

        public void Configure(
            GameObject root,
            Text portrait,
            Text title,
            Text role,
            Text message,
            Text relationship,
            Button firstChoice,
            Text firstChoiceText,
            Button secondChoice,
            Text secondChoiceText,
            Button laterAction,
            Button confirmAction,
            TopMenuController menuController = null,
            BottomActionBarController bottomController = null,
            DevPanelController devController = null)
        {
            overlayRoot = root;
            portraitLabel = portrait;
            titleLabel = title;
            roleLabel = role;
            messageLabel = message;
            relationshipLabel = relationship;
            firstChoiceButton = firstChoice;
            firstChoiceLabel = firstChoiceText;
            secondChoiceButton = secondChoice;
            secondChoiceLabel = secondChoiceText;
            laterButton = laterAction;
            confirmButton = confirmAction;
            topMenuController = menuController;
            actionBarController = bottomController;
            developerPanelController = devController;
            WireButtons();
            CloseInternal(false);
        }

        public bool Show(
            NpcVisitOffer offer,
            Func<string, string, NpcVisitResolutionResult> resolver,
            Action onLater = null)
        {
            if (offer == null || !offer.HasOffer || overlayRoot == null || offer.Visitor.Choices.Count < 2)
            {
                return false;
            }

            displayed = offer;
            resolveChoice = resolver;
            later = onLater;
            showingResult = false;
            SetText(titleLabel, offer.Title);
            SetText(portraitLabel, offer.Title);
            SetText(roleLabel, offer.Role);
            SetText(messageLabel, offer.Message);
            SetText(relationshipLabel, $"이야기 {offer.StoryStep + 1}/3");
            SetText(firstChoiceLabel, offer.Visitor.Choices[0].Label);
            SetText(secondChoiceLabel, offer.Visitor.Choices[1].Label);
            SetChoiceState(true);
            SetActive(laterButton, true);
            SetActive(confirmButton, false);
            previousSelected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            SuspendControls();
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            Select(firstChoiceButton);
            return true;
        }

        public void ChooseFirst()
        {
            Resolve(0);
        }

        public void ChooseSecond()
        {
            Resolve(1);
        }

        public void Later()
        {
            if (!IsBlockingGameplay || showingResult)
            {
                return;
            }

            later?.Invoke();
            CloseInternal(true);
        }

        public void Confirm()
        {
            if (IsBlockingGameplay && showingResult)
            {
                CloseInternal(true);
            }
        }

        private void Update()
        {
            if (!IsBlockingGameplay || !CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                return;
            }

            if (showingResult)
            {
                Confirm();
            }
            else
            {
                Later();
            }
        }

        private void Resolve(int choiceIndex)
        {
            if (!IsBlockingGameplay
                || showingResult
                || displayed?.Visitor == null
                || choiceIndex < 0
                || choiceIndex >= displayed.Visitor.Choices.Count)
            {
                return;
            }

            var choice = displayed.Visitor.Choices[choiceIndex];
            var result = resolveChoice?.Invoke(displayed.OccurrenceId, choice.Id);
            if (result == null || !result.Applied)
            {
                SetText(messageLabel, "방문 결과를 저장하지 못했어요. 잠시 뒤 다시 시도해 주세요.");
                return;
            }

            showingResult = true;
            SetText(messageLabel, result.Message);
            SetText(relationshipLabel, $"관계 이야기 {result.RelationshipLevel + 1}/3");
            SetChoiceState(false);
            SetActive(laterButton, false);
            SetActive(confirmButton, true);
            Select(confirmButton);
        }

        private void WireButtons()
        {
            Bind(firstChoiceButton, ChooseFirst);
            Bind(secondChoiceButton, ChooseSecond);
            Bind(laterButton, Later);
            Bind(confirmButton, Confirm);
        }

        private void CloseInternal(bool restoreSelection)
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            RestoreControls();
            if (restoreSelection && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(previousSelected);
            }

            previousSelected = null;
            displayed = null;
            resolveChoice = null;
            later = null;
            showingResult = false;
        }

        private void SuspendControls()
        {
            if (controlsSuspended)
            {
                return;
            }

            previousTopEnabled = topMenuController != null && topMenuController.enabled;
            previousBottomEnabled = actionBarController != null && actionBarController.enabled;
            previousDevEnabled = developerPanelController != null && developerPanelController.enabled;
            if (topMenuController != null) topMenuController.enabled = false;
            if (actionBarController != null) actionBarController.enabled = false;
            if (developerPanelController != null) developerPanelController.enabled = false;
            controlsSuspended = true;
        }

        private void RestoreControls()
        {
            if (!controlsSuspended)
            {
                return;
            }

            if (topMenuController != null) topMenuController.enabled = previousTopEnabled;
            if (actionBarController != null) actionBarController.enabled = previousBottomEnabled;
            if (developerPanelController != null) developerPanelController.enabled = previousDevEnabled;
            controlsSuspended = false;
        }

        private void OnDisable()
        {
            RestoreControls();
        }

        private void SetChoiceState(bool active)
        {
            SetActive(firstChoiceButton, active);
            SetActive(secondChoiceButton, active);
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

        private static void SetActive(Button button, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static void Select(Button button)
        {
            if (button != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }
    }
}
