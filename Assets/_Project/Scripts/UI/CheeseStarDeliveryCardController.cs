using System;
using System.Globalization;
using System.Text;
using CheeseTama.Gameplay.Deliveries;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class CheeseStarDeliveryCardController : MonoBehaviour
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text streakText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text noteText;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button laterButton;

        private Action claimRequested;
        private Action laterRequested;
        private GameObject previouslySelected;
        private bool buttonsBound;
        private bool requestInFlight;

        public bool IsConfigured => overlayRoot != null
            && titleText != null
            && streakText != null
            && rewardText != null
            && claimButton != null
            && laterButton != null;

        public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;

        public bool IsBlockingGameplay => IsVisible;

        public void Configure(
            GameObject root,
            Text titleLabel,
            Text streakLabel,
            Text rewardLabel,
            Text noteLabel,
            Button receiveButton,
            Button postponeButton)
        {
            UnbindButtons();
            overlayRoot = root;
            titleText = titleLabel;
            streakText = streakLabel;
            rewardText = rewardLabel;
            noteText = noteLabel;
            claimButton = receiveButton;
            laterButton = postponeButton;
            ClearCallbacks();
            SetOverlayActive(false);
            BindButtons();
        }

        public bool Show(
            CheeseStarDeliveryOffer offer,
            Action onClaimRequested,
            Action onLaterRequested)
        {
            if (!IsConfigured || offer == null || !offer.CanClaim)
            {
                return false;
            }

            BindButtons();
            claimRequested = onClaimRequested;
            laterRequested = onLaterRequested;
            requestInFlight = false;
            Populate(offer);
            SetInteractionEnabled(true);

            var eventSystem = EventSystem.current;
            previouslySelected = eventSystem != null
                ? eventSystem.currentSelectedGameObject
                : null;

            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            Select(claimButton);
            return true;
        }

        public void Hide()
        {
            var selectionToRestore = previouslySelected;
            ClearCallbacks();
            SetOverlayActive(false);

            if (selectionToRestore != null
                && selectionToRestore.activeInHierarchy
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectionToRestore);
            }
        }

        public void SetInteractionEnabled(bool enabled)
        {
            requestInFlight = !enabled;
            if (claimButton != null)
            {
                claimButton.interactable = enabled;
            }

            if (laterButton != null)
            {
                laterButton.interactable = enabled;
            }

            if (enabled && IsVisible)
            {
                Select(claimButton);
            }
        }

        private void OnEnable()
        {
            BindButtons();
        }

        private void OnDisable()
        {
            UnbindButtons();
            ClearCallbacks();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Update()
        {
            if (!IsVisible || requestInFlight)
            {
                return;
            }

            if (CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                HandleLaterRequested();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                HandleClaimRequested();
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow)
                || Input.GetKeyDown(KeyCode.UpArrow))
            {
                Select(claimButton);
                return;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow)
                || Input.GetKeyDown(KeyCode.DownArrow))
            {
                Select(laterButton);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                var selected = EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject
                    : null;
                Select(selected == claimButton.gameObject ? laterButton : claimButton);
            }
        }

        private void HandleClaimRequested()
        {
            if (!IsVisible || requestInFlight)
            {
                return;
            }

            requestInFlight = true;
            if (claimButton != null)
            {
                claimButton.interactable = false;
            }

            if (laterButton != null)
            {
                laterButton.interactable = false;
            }

            claimRequested?.Invoke();
        }

        private void HandleLaterRequested()
        {
            if (!IsVisible || requestInFlight)
            {
                return;
            }

            var callback = laterRequested;
            Hide();
            callback?.Invoke();
        }

        private void Populate(CheeseStarDeliveryOffer offer)
        {
            SetText(
                titleText,
                offer.RevealStarRoute ? "치즈별 배달" : "오늘의 배달");
            SetText(
                streakText,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "연속 {0}일째 · 보상 {1}일차",
                    offer.StreakDay,
                    offer.RewardCycleDay));
            SetText(rewardText, BuildRewardText(offer.Reward));
            SetText(noteText, BuildNoteText(offer));
        }

        private static string BuildRewardText(CheeseStarDeliveryReward reward)
        {
            var builder = new StringBuilder();
            AppendReward(builder, "우유 코인", reward.MilkCoins);
            AppendReward(builder, "우유방울", reward.MilkDrops);
            AppendReward(builder, "별방울", reward.StarDrops);
            AppendReward(builder, "환상가루", reward.FantasyPowder);
            return builder.ToString();
        }

        private static string BuildNoteText(CheeseStarDeliveryOffer offer)
        {
            return offer.BonusKind switch
            {
                CheeseStarDeliveryBonusKind.DayThree =>
                    "3일째 보너스가 함께 도착했어요.",
                CheeseStarDeliveryBonusKind.DaySeven when offer.RevealStarRoute =>
                    "7일째 특별 보너스가 함께 도착했어요.",
                CheeseStarDeliveryBonusKind.DaySeven =>
                    "7일째 포근한 보너스가 함께 도착했어요.",
                _ => "오늘 찾아온 기본 선물이에요. 나중에 받아도 오늘 안에는 사라지지 않아요."
            };
        }

        private static void AppendReward(StringBuilder builder, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(label).Append(" +").Append(amount);
        }

        private void BindButtons()
        {
            if (buttonsBound || claimButton == null || laterButton == null)
            {
                return;
            }

            claimButton.onClick.AddListener(HandleClaimRequested);
            laterButton.onClick.AddListener(HandleLaterRequested);
            buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (!buttonsBound)
            {
                return;
            }

            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(HandleClaimRequested);
            }

            if (laterButton != null)
            {
                laterButton.onClick.RemoveListener(HandleLaterRequested);
            }

            buttonsBound = false;
        }

        private void ClearCallbacks()
        {
            claimRequested = null;
            laterRequested = null;
            previouslySelected = null;
            requestInFlight = false;
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
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
            if (button != null
                && button.interactable
                && button.gameObject.activeInHierarchy
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }
    }
}
