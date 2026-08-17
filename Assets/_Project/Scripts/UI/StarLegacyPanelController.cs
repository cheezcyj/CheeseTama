using System;
using System.Text;
using CheeseTama.Gameplay.Growth;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class StarLegacyPanelViewModel
    {
        public StarLegacyPanelViewModel(
            bool visible,
            bool evolved,
            bool canEvolve,
            string routeHint,
            int maturationProgress,
            int maturationRequired,
            int completedCycles,
            int pendingRewardCount,
            FinalMaturationReward nextReward)
        {
            this.visible = visible;
            this.evolved = evolved;
            this.canEvolve = canEvolve;
            this.routeHint = routeHint ?? string.Empty;
            this.maturationProgress = Math.Max(0, maturationProgress);
            this.maturationRequired = Math.Max(1, maturationRequired);
            this.completedCycles = Math.Max(0, completedCycles);
            this.pendingRewardCount = Math.Max(0, pendingRewardCount);
            this.nextReward = nextReward;
        }

        public bool visible { get; }
        public bool evolved { get; }
        public bool canEvolve { get; }
        public string routeHint { get; }
        public int maturationProgress { get; }
        public int maturationRequired { get; }
        public int completedCycles { get; }
        public int pendingRewardCount { get; }
        public FinalMaturationReward nextReward { get; }
        public bool canClaimReward => pendingRewardCount > 0;

        public static StarLegacyPanelViewModel Create(
            EmmentalEvolutionProgress evolution,
            FinalMaturationCycleSnapshot maturation)
        {
            return new StarLegacyPanelViewModel(
                evolution.visible,
                evolution.evolved,
                evolution.canEvolve,
                evolution.indirectHint,
                maturation.progress,
                maturation.requiredProgress,
                maturation.completedCycles,
                maturation.pendingRewardCount,
                maturation.nextReward);
        }

        public static StarLegacyPanelViewModel Hidden()
        {
            return new StarLegacyPanelViewModel(
                false,
                false,
                false,
                string.Empty,
                0,
                FinalMaturationCycleSystem.RequiredProgress,
                0,
                0,
                default);
        }
    }

    public sealed class StarLegacyPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Star Legacy Overlay";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text routeText;
        [SerializeField] private Slider maturationSlider;
        [SerializeField] private Text maturationText;
        [SerializeField] private Text pendingRewardText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button evolveButton;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;

        private Func<StarLegacyPanelViewModel> snapshotProvider;
        private Func<EmmentalEvolutionAttemptResult> evolveCommand;
        private Func<FinalMaturationClaimResult> claimCommand;
        private Action<bool> setGameplayBlocked;
        private StarLegacyPanelViewModel snapshot = StarLegacyPanelViewModel.Hidden();
        private GameObject previouslySelected;
        private bool configured;
        private bool listenersBound;
        private bool gameplayBlocked;

        public bool IsBlockingGameplay => panelRoot != null && panelRoot.activeSelf;
        public StarLegacyPanelViewModel CurrentSnapshot => snapshot;

        public void Configure(
            GameObject root,
            Text heading,
            Text routeLabel,
            Slider progressSlider,
            Text progressLabel,
            Text rewardLabel,
            Text resultLabel,
            Button evolveAction,
            Button claimAction,
            Button closeAction,
            Button openAction,
            Func<StarLegacyPanelViewModel> getSnapshot,
            Func<EmmentalEvolutionAttemptResult> tryEvolve,
            Func<FinalMaturationClaimResult> tryClaim,
            Action<bool> blockGameplay = null)
        {
            UnbindListeners();
            ReleaseGameplayBlock();
            panelRoot = root;
            titleText = heading;
            routeText = routeLabel;
            maturationSlider = progressSlider;
            maturationText = progressLabel;
            pendingRewardText = rewardLabel;
            statusText = resultLabel;
            evolveButton = evolveAction;
            claimButton = claimAction;
            closeButton = closeAction;
            openButton = openAction;
            snapshotProvider = getSnapshot;
            evolveCommand = tryEvolve;
            claimCommand = tryClaim;
            setGameplayBlocked = blockGameplay;
            configured = panelRoot != null
                && closeButton != null
                && openButton != null
                && snapshotProvider != null;
            SetPanelActive(false);
            BindListeners();
            Refresh();
        }

        private void OnEnable()
        {
            BindListeners();
            if (configured)
            {
                Refresh();
            }
        }

        private void OnDisable()
        {
            UnbindListeners();
            SetPanelActive(false);
            ReleaseGameplayBlock();
        }

        private void OnDestroy()
        {
            UnbindListeners();
            ReleaseGameplayBlock();
        }

        private void Update()
        {
            if (IsBlockingGameplay && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        public void Open()
        {
            if (!configured)
            {
                return;
            }

            Refresh();
            if (!snapshot.visible)
            {
                return;
            }

            previouslySelected = EventSystem.current?.currentSelectedGameObject;
            AcquireGameplayBlock();
            SetPanelActive(true);
            panelRoot.transform.SetAsLastSibling();
            var initialSelection = snapshot.canEvolve
                ? evolveButton
                : snapshot.canClaimReward
                    ? claimButton
                    : closeButton;
            if (initialSelection != null)
            {
                EventSystem.current?.SetSelectedGameObject(initialSelection.gameObject);
            }
        }

        public void Close()
        {
            SetPanelActive(false);
            ReleaseGameplayBlock();
            if (EventSystem.current != null
                && previouslySelected != null
                && previouslySelected.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(previouslySelected);
            }

            previouslySelected = null;
        }

        public void Refresh()
        {
            snapshot = snapshotProvider?.Invoke() ?? StarLegacyPanelViewModel.Hidden();
            SetActive(openButton?.gameObject, snapshot.visible);
            if (!snapshot.visible && IsBlockingGameplay)
            {
                Close();
            }

            SetText(titleText, snapshot.evolved ? "별자리 숙성" : "별빛 숙성");
            SetText(routeText, snapshot.routeHint);
            if (maturationSlider != null)
            {
                maturationSlider.minValue = 0f;
                maturationSlider.maxValue = snapshot.maturationRequired;
                maturationSlider.wholeNumbers = true;
                maturationSlider.value = Math.Min(
                    snapshot.maturationRequired,
                    snapshot.maturationProgress);
            }

            SetText(
                maturationText,
                $"최종형 숙성 {snapshot.maturationProgress}/{snapshot.maturationRequired}"
                    + $" · 완료 {snapshot.completedCycles}회");
            SetText(
                pendingRewardText,
                snapshot.pendingRewardCount > 0
                    ? $"받을 숙성 보상 {snapshot.pendingRewardCount}개\n{FormatReward(snapshot.nextReward)}"
                    : "받을 숙성 보상이 없습니다.");
            SetInteractable(evolveButton, snapshot.canEvolve && evolveCommand != null);
            SetInteractable(claimButton, snapshot.canClaimReward && claimCommand != null);
        }

        private void TryEvolve()
        {
            if (evolveCommand == null)
            {
                return;
            }

            var result = evolveCommand();
            SetText(statusText, FormatEvolutionResult(result));
            Refresh();
        }

        private void TryClaim()
        {
            if (claimCommand == null)
            {
                return;
            }

            var result = claimCommand();
            SetText(statusText, FormatClaimResult(result));
            Refresh();
        }

        private void BindListeners()
        {
            if (!configured || listenersBound)
            {
                return;
            }

            evolveButton?.onClick.AddListener(TryEvolve);
            claimButton?.onClick.AddListener(TryClaim);
            closeButton?.onClick.AddListener(Close);
            openButton?.onClick.AddListener(Open);
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            evolveButton?.onClick.RemoveListener(TryEvolve);
            claimButton?.onClick.RemoveListener(TryClaim);
            closeButton?.onClick.RemoveListener(Close);
            openButton?.onClick.RemoveListener(Open);
            listenersBound = false;
        }

        private void AcquireGameplayBlock()
        {
            if (gameplayBlocked)
            {
                return;
            }

            setGameplayBlocked?.Invoke(true);
            gameplayBlocked = true;
        }

        private void ReleaseGameplayBlock()
        {
            if (!gameplayBlocked)
            {
                return;
            }

            setGameplayBlocked?.Invoke(false);
            gameplayBlocked = false;
        }

        private void SetPanelActive(bool active)
        {
            if (panelRoot != null && panelRoot.activeSelf != active)
            {
                panelRoot.SetActive(active);
            }
        }

        private static string FormatReward(FinalMaturationReward reward)
        {
            var builder = new StringBuilder();
            AppendReward(builder, "코인", reward.milkCoins);
            AppendReward(builder, "우유방울", reward.milkDrops);
            AppendReward(builder, "별방울", reward.starDrops);
            AppendReward(builder, "환상가루", reward.fantasyPowder);
            return builder.Length == 0 ? "보상 정보 없음" : builder.ToString();
        }

        private static void AppendReward(StringBuilder builder, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(" · ");
            }

            builder.Append(label).Append(" +").Append(amount);
        }

        private static string FormatEvolutionResult(EmmentalEvolutionAttemptResult result)
        {
            return result.status switch
            {
                EmmentalEvolutionAttemptStatus.Applied => "일곱 개의 빛이 이어져 새로운 모습이 되었어요.",
                EmmentalEvolutionAttemptStatus.AlreadyApplied => "이미 반영된 진화입니다.",
                EmmentalEvolutionAttemptStatus.AlreadyEvolved => "이미 별자리 숙성에 도달했어요.",
                EmmentalEvolutionAttemptStatus.NotStarEggOrigin => "다른 알의 여정에서는 이 빛이 이어지지 않아요.",
                EmmentalEvolutionAttemptStatus.LevelTooLow => "아직 충분히 숙성되지 않았어요.",
                EmmentalEvolutionAttemptStatus.StarMilkSignalIncomplete
                    or EmmentalEvolutionAttemptStatus.FantasySignalIncomplete =>
                    "빛은 모였지만 아직 하나의 무늬가 되지 않았어요.",
                EmmentalEvolutionAttemptStatus.StarRouteLocked => "아직 별빛의 길이 열리지 않았어요.",
                _ => "지금은 새로운 모습으로 이어질 수 없어요."
            };
        }

        private static string FormatClaimResult(FinalMaturationClaimResult result)
        {
            return result.status switch
            {
                FinalMaturationClaimStatus.Applied => $"숙성 보상을 받았어요. {FormatReward(result.reward)}",
                FinalMaturationClaimStatus.AlreadyApplied => "이미 받은 숙성 보상입니다.",
                FinalMaturationClaimStatus.NoPendingReward => "아직 받을 숙성 보상이 없어요.",
                FinalMaturationClaimStatus.RewardCapacityFull => "재화 보관 공간을 먼저 정리해 주세요.",
                _ => "숙성 보상을 받을 수 없어요."
            };
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetInteractable(Selectable target, bool interactable)
        {
            if (target != null)
            {
                target.interactable = interactable;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
