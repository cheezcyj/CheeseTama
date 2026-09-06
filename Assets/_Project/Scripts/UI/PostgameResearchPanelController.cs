using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.Research;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class PostgameResearchPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject overlay;
        [SerializeField] private Button openButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button closeButton;

        private Func<PostgameResearchSnapshot> snapshotProvider;
        private Func<string, PostgameResearchUnlockResult> unlockCommand;
        private Func<string, bool> selectProfileCommand;
        private Action<bool> blockingChanged;
        private readonly PostgameResearchProfileEffectSystem profileEffectSystem =
            new PostgameResearchProfileEffectSystem();
        private GameManager eventSource;
        private bool eventsBound;
        private int selectedIndex;
        private string transientStatus = string.Empty;

        public bool IsOpen => overlay != null && overlay.activeSelf;

        public void Configure(
            GameObject overlayRoot,
            Button opener,
            Text title,
            Text body,
            Text status,
            Button previous,
            Button next,
            Button unlock,
            Button close,
            Func<PostgameResearchSnapshot> getSnapshot,
            Func<string, PostgameResearchUnlockResult> tryUnlock,
            Action<bool> setBlocking = null,
            GameManager manager = null,
            Func<string, bool> trySelectProfile = null)
        {
            UnbindEvents();
            overlay = overlayRoot;
            openButton = opener;
            titleText = title;
            bodyText = body;
            statusText = status;
            previousButton = previous;
            nextButton = next;
            unlockButton = unlock;
            closeButton = close;
            snapshotProvider = getSnapshot;
            unlockCommand = tryUnlock;
            selectProfileCommand = trySelectProfile;
            blockingChanged = setBlocking;
            eventSource = manager;

            Bind(openButton, Open);
            Bind(previousButton, Previous);
            Bind(nextButton, Next);
            Bind(unlockButton, UnlockSelected);
            Bind(closeButton, Close);
            BindEvents();
            Close();
            Refresh();
        }

        private void OnEnable()
        {
            BindEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindEvents();
            blockingChanged?.Invoke(false);
        }

        private void Update()
        {
            if (IsOpen
                && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(
                    CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        public void Open()
        {
            var snapshot = snapshotProvider?.Invoke();
            if (overlay == null || snapshot == null || !snapshot.Available)
            {
                transientStatus = "레벨 33이 되면 최고 레벨 연구가 열려요.";
                Refresh();
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, Math.Max(0, snapshot.Nodes.Count - 1));
            transientStatus = string.Empty;
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            blockingChanged?.Invoke(true);
            Refresh();
            EventSystem.current?.SetSelectedGameObject(unlockButton?.gameObject);
        }

        public void Close()
        {
            if (overlay != null)
            {
                overlay.SetActive(false);
            }

            blockingChanged?.Invoke(false);
        }

        public void Refresh()
        {
            var snapshot = snapshotProvider?.Invoke();
            var available = snapshot != null && snapshot.Available;
            if (openButton != null)
            {
                openButton.gameObject.SetActive(available);
            }

            if (!available)
            {
                SetText(titleText, "최고 레벨 연구");
                SetText(bodyText, "최고 레벨 뒤에도 돌보고 놀면 연구 점수를 모을 수 있어요.");
                SetText(statusText, transientStatus);
                SetInteractable(previousButton, false);
                SetInteractable(nextButton, false);
                SetInteractable(unlockButton, false);
                return;
            }

            SetText(
                titleText,
                $"최고 레벨 연구 · 연구 점수 {snapshot.AvailablePoints}/{snapshot.EarnedPoints}");
            if (snapshot.Nodes.Count == 0)
            {
                SetText(bodyText, "아직 볼 수 있는 연구가 없어요.");
                SetInteractable(previousButton, false);
                SetInteractable(nextButton, false);
                SetInteractable(unlockButton, false);
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, snapshot.Nodes.Count - 1);
            var node = snapshot.Nodes[selectedIndex];
            var definition = node.Definition;
            var active = node.Unlocked
                && string.Equals(
                    snapshot.ActiveProfileId,
                    definition.ProfileId,
                    StringComparison.Ordinal);
            var state = node.Unlocked
                ? active ? "도움 사용 중" : "연구 완료"
                : !node.PrerequisiteMet
                    ? "앞의 연구 먼저"
                    : !node.Affordable
                        ? "연구 점수 부족"
                        : "연구할 수 있어요";
            var effectPreview = node.Unlocked || node.CanUnlock
                ? profileEffectSystem.BuildPreview(definition)
                : default;
            var effectText = effectPreview.Available
                ? $"\n\n<b>{SimplifyResearchCopy(effectPreview.EffectTitle)}</b>\n"
                    + SimplifyResearchCopy(effectPreview.EffectDescription)
                : string.Empty;
            SetText(
                bodyText,
                $"<b>{SimplifyResearchCopy(definition.Title)}</b>  ({selectedIndex + 1}/{snapshot.Nodes.Count})\n"
                + $"필요한 연구 점수 {definition.Cost} · {state}\n\n"
                + SimplifyResearchCopy(definition.Detail)
                + effectText);
            SetText(statusText, transientStatus);
            SetInteractable(previousButton, selectedIndex > 0);
            SetInteractable(nextButton, selectedIndex + 1 < snapshot.Nodes.Count);
            SetInteractable(unlockButton, node.CanUnlock || (node.Unlocked && !active));
            SetButtonLabel(
                unlockButton,
                node.Unlocked
                    ? active ? "사용 중" : "도움 사용"
                    : "연구 열기");
        }

        private void Previous()
        {
            selectedIndex = Math.Max(0, selectedIndex - 1);
            transientStatus = string.Empty;
            Refresh();
        }

        private void Next()
        {
            var snapshot = snapshotProvider?.Invoke();
            selectedIndex = Math.Min(Math.Max(0, (snapshot?.Nodes.Count ?? 1) - 1), selectedIndex + 1);
            transientStatus = string.Empty;
            Refresh();
        }

        private void UnlockSelected()
        {
            var snapshot = snapshotProvider?.Invoke();
            if (snapshot == null || selectedIndex < 0 || selectedIndex >= snapshot.Nodes.Count)
            {
                return;
            }

            var node = snapshot.Nodes[selectedIndex];
            if (node.Unlocked)
            {
                var selected = selectProfileCommand?.Invoke(node.Definition.ProfileId) == true;
                transientStatus = selected
                    ? "이 연구의 도움을 사용하고 있어요."
                    : "이미 사용 중이거나 지금은 고를 수 없어요.";
                Refresh();
                return;
            }

            var result = unlockCommand?.Invoke(node.Definition.Id);
            transientStatus = FormatResult(result);
            Refresh();
        }

        internal static string SimplifyResearchCopy(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value
                    .Replace(
                        "최종 숙성 행동 진행량이 1 증가합니다.",
                        "최종 숙성 점수가 1 늘어나요.")
                    .Replace(
                        "특별 블렌딩 결과 확률이 2%p 증가합니다.",
                        "우유를 섞을 때 특별한 음식이 나올 확률이 2% 높아져요.")
                    .Replace(
                        "NPC 승급 에피소드의 호감도 증가량이 1 늘어납니다.",
                        "손님 이야기에서 얻는 친밀도가 1 늘어나요.")
                    .Replace(
                        "기존 별빛과 도감조각 보상이 각각 1 증가합니다.",
                        "별빛과 도감 조각 보상이 각각 1 늘어나요.")
                    .Replace("Lv.33", "레벨 33")
                    .Replace("반복 돌봄", "계속한 돌봄")
                    .Replace("반복 성장 연구", "최고 레벨 연구")
                    .Replace("블렌딩 상관표", "우유 섞기 비교표")
                    .Replace("블렌딩 상관 보정", "우유 섞기 도움")
                    .Replace("특별 블렌딩 결과 확률", "우유를 섞을 때 특별한 음식이 나올 확률")
                    .Replace("재료 숙련", "재료를 사용한 기록")
                    .Replace("블렌딩", "우유 섞기")
                    .Replace("NPC 승급 에피소드", "손님 이야기")
                    .Replace("NPC 에피소드", "손님 이야기")
                    .Replace("호감도 증가량", "친밀도")
                    .Replace("성숙 순환", "여러 번 자란 기록")
                    .Replace("최종 숙성 행동 진행량", "최종 숙성 점수")
                    .Replace("보정", "도움")
                    .Replace("%p 증가합니다.", "%만큼 높아져요.")
                    .Replace("증가합니다.", "늘어나요.")
                    .Replace("남깁니다.", "남겨요.")
                    .Replace("엽니다.", "열어요.")
                    .Replace("완성합니다.", "완성해요.");
        }

        private static string FormatResult(PostgameResearchUnlockResult result)
        {
            if (result == null)
            {
                return "연구 정보를 확인하지 못했어요.";
            }

            return result.Status switch
            {
                PostgameResearchUnlockStatus.Applied => "새 연구를 열었어요.",
                PostgameResearchUnlockStatus.AlreadyApplied => "이미 열린 연구예요.",
                PostgameResearchUnlockStatus.AlreadyUnlocked => "이미 열린 연구예요.",
                PostgameResearchUnlockStatus.MissingPrerequisite => "앞의 연구를 먼저 열어 주세요.",
                PostgameResearchUnlockStatus.InsufficientPoints => "연구 점수가 부족해요.",
                PostgameResearchUnlockStatus.NotFinalLevel => "레벨 33이 되면 연구할 수 있어요.",
                _ => "지금은 이 연구를 열 수 없어요."
            };
        }

        private void BindEvents()
        {
            if (eventSource == null || eventsBound)
            {
                return;
            }

            eventSource.JourneyHubChanged += HandleStateChanged;
            eventSource.SaveDataReplaced += HandleStateChanged;
            eventSource.PostgameResearchChanged += HandleResearchChanged;
            eventSource.CareActionRegistered += HandleCareActionRegistered;
            eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (eventSource == null || !eventsBound)
            {
                eventsBound = false;
                return;
            }

            eventSource.JourneyHubChanged -= HandleStateChanged;
            eventSource.SaveDataReplaced -= HandleStateChanged;
            eventSource.PostgameResearchChanged -= HandleResearchChanged;
            eventSource.CareActionRegistered -= HandleCareActionRegistered;
            eventsBound = false;
        }

        private void HandleStateChanged()
        {
            Refresh();
        }

        private void HandleResearchChanged(PostgameResearchUnlockResult _)
        {
            Refresh();
        }

        private void HandleCareActionRegistered(string _)
        {
            Refresh();
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

        private static void SetInteractable(Button button, bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button != null)
            {
                SetText(button.GetComponentInChildren<Text>(true), value);
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
