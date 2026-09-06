using System;
using System.Text;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.NewGameSetup;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class StarLineagePanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Star Lineage Overlay";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button gentleTraitButton;
        [SerializeField] private Button maturationTraitButton;
        [SerializeField] private Button blendingTraitButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;

        private Func<StarLineageSnapshot> snapshotProvider;
        private Func<string, StarLineageSelectionResult> selectionCommand;
        private Action<bool> blockingChanged;
        private Action beforeEntryOpen;
        private StarLineageSnapshot snapshot;
        private int recordIndex;
        private bool listenersBound;
        private bool gameplayBlocked;
        private GameObject previouslySelected;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public StarLineageSnapshot CurrentSnapshot => snapshot;

        public void Configure(
            GameObject root,
            Button entryButton,
            Button dismissButton,
            Button previousRecordButton,
            Button nextRecordButton,
            Button gentleButton,
            Button maturationButton,
            Button blendingButton,
            Text heading,
            Text body,
            Text result,
            Func<StarLineageSnapshot> getSnapshot,
            Func<string, StarLineageSelectionResult> selectTrait,
            Action<bool> onBlockingChanged = null)
        {
            UnbindListeners();
            ReleaseGameplayBlock();
            panelRoot = root;
            openButton = entryButton;
            closeButton = dismissButton;
            previousButton = previousRecordButton;
            nextButton = nextRecordButton;
            gentleTraitButton = gentleButton;
            maturationTraitButton = maturationButton;
            blendingTraitButton = blendingButton;
            titleText = heading;
            bodyText = body;
            statusText = result;
            snapshotProvider = getSnapshot;
            selectionCommand = selectTrait;
            blockingChanged = onBlockingChanged;
            recordIndex = 0;
            SetPanelActive(false);
            BindListeners();
            Refresh();
        }

        public void ConfigureEntryNavigation(Action onBeforeOpen)
        {
            beforeEntryOpen = onBeforeOpen;
        }

        private void OnEnable()
        {
            BindListeners();
            Refresh();
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
            if (IsOpen && GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        public void Open()
        {
            Refresh();
            if (snapshot?.Visible != true)
            {
                return;
            }

            previouslySelected = EventSystem.current?.currentSelectedGameObject;
            AcquireGameplayBlock();
            SetPanelActive(true);
            panelRoot.transform.SetAsLastSibling();
            var initial = snapshot.RequiresTraitSelection ? gentleTraitButton : closeButton;
            if (initial != null)
            {
                EventSystem.current?.SetSelectedGameObject(initial.gameObject);
            }
        }

        public void OpenFromEntry()
        {
            if (!IsOpen)
            {
                beforeEntryOpen?.Invoke();
            }

            Open();
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
            snapshot = snapshotProvider?.Invoke();
            var visible = snapshot?.Visible == true;
            SetActive(openButton?.gameObject, visible);
            if (!visible)
            {
                if (IsOpen)
                {
                    Close();
                }

                SetText(titleText, "지난 치즈타마 기록");
                SetText(bodyText, "새 세대로 이어진 기록이 아직 없습니다.");
                SetTraitButtons(false);
                SetRecordNavigation(false, false);
                return;
            }

            recordIndex = Math.Max(0, Math.Min(recordIndex, snapshot.Records.Count - 1));
            SetText(titleText, snapshot.RequiresTraitSelection
                ? $"{snapshot.PendingGenerationNumber}세대가 이어받을 도움 고르기"
                : "지난 치즈타마 기록");
            SetText(bodyText, snapshot.RequiresTraitSelection
                ? BuildSelectionText(snapshot)
                : BuildRecordText(snapshot));
            SetTraitButtons(snapshot.RequiresTraitSelection && selectionCommand != null);
            SetRecordNavigation(
                !snapshot.RequiresTraitSelection && recordIndex > 0,
                !snapshot.RequiresTraitSelection && recordIndex + 1 < snapshot.Records.Count);
        }

        private void SelectGentle()
        {
            SelectTrait(StarLineageTraitIds.GentleRecovery);
        }

        private void SelectMaturation()
        {
            SelectTrait(StarLineageTraitIds.PatientMaturation);
        }

        private void SelectBlending()
        {
            SelectTrait(StarLineageTraitIds.CuriousBlending);
        }

        private void SelectTrait(string traitId)
        {
            if (selectionCommand == null || snapshot?.RequiresTraitSelection != true)
            {
                return;
            }

            var result = selectionCommand(traitId);
            SetText(statusText, FormatSelectionResult(result));
            Refresh();
        }

        private void ShowPrevious()
        {
            recordIndex = Math.Max(0, recordIndex - 1);
            Refresh();
        }

        private void ShowNext()
        {
            if (snapshot != null)
            {
                recordIndex = Math.Min(snapshot.Records.Count - 1, recordIndex + 1);
            }

            Refresh();
        }

        private static string BuildSelectionText(StarLineageSnapshot value)
        {
            var builder = new StringBuilder();
            builder.Append("이전 치즈타마의 좋은 점 하나를 지금 치즈타마에게 이어 주세요. ")
                .Append("도움은 하나만 고를 수 있고, 다음 세대에서 다시 고를 수 있어요.");
            for (var index = 0; index < value.Traits.Count; index += 1)
            {
                var trait = value.Traits[index];
                builder.Append("\n\n").Append(trait.DisplayName).Append(" · ").Append(trait.Detail);
            }

            return builder.ToString();
        }

        private string BuildRecordText(StarLineageSnapshot value)
        {
            var builder = new StringBuilder();
            var active = value.ActiveTrait;
            builder.Append(active != null
                ? $"지금 이어받은 도움 · {active.DisplayName}\n{active.Detail}"
                : "아직 이어받은 도움이 없어요.");
            if (value.Records.Count == 0)
            {
                return builder.ToString();
            }

            var record = value.Records[recordIndex];
            builder.Append("\n\n")
                .Append(recordIndex + 1).Append('/').Append(value.Records.Count)
                .Append(" · ").Append(record.NextGenerationNumber).Append("세대로 이어진 기록")
                .Append("\n").Append(string.IsNullOrEmpty(record.DisplayName) ? "치즈타마" : record.DisplayName)
                .Append(" · 모습 ").Append(ResolveFormDisplayName(record))
                .Append("\n가장 좋아한 우유 ").Append(ResolveMilkDisplayName(record.PrimaryMilkId))
                .Append(" · 돌본 방법 ").Append(ResolveCareStyleDisplayName(record.CareStyleId));
            return builder.ToString();
        }

        private static string ResolveFormDisplayName(StarLineageRecordSnapshot record)
        {
            var evolutionId = string.IsNullOrWhiteSpace(record?.EvolutionId)
                ? record?.FormId
                : record.EvolutionId;
            var normal = EvolutionSystem.FindNormalEvolution(evolutionId);
            if (normal != null && !string.IsNullOrWhiteSpace(normal.DisplayName))
            {
                return normal.DisplayName;
            }

            if (string.Equals(
                    evolutionId,
                    StarEggEmmentalEvolutionSystem.EmmentalEvolutionId,
                    StringComparison.Ordinal))
            {
                return StarEggEmmentalEvolutionSystem.Profile.DisplayName;
            }

            return evolutionId switch
            {
                "egg" => "치즈타마 알",
                "soft_cheesetama" => "부화 치즈타마",
                _ => "알 수 없는 모습"
            };
        }

        private static string ResolveMilkDisplayName(string milkId)
        {
            var milk = MilkCatalog.Find(milkId);
            return string.IsNullOrWhiteSpace(milk?.displayName)
                ? "알 수 없는 우유"
                : milk.displayName;
        }

        private static string ResolveCareStyleDisplayName(string careStyleId)
        {
            return careStyleId switch
            {
                NewGameSetupCatalog.BalancedTraitId => "골고루 돌보기",
                NewGameSetupCatalog.LivelyTraitId => "신나게 놀아주기",
                NewGameSetupCatalog.ExpressiveTraitId => "마음을 잘 살피기",
                NewGameSetupCatalog.CalmTraitId => "차분하게 돌보기",
                NewGameSetupCatalog.FocusedTraitId => "꾸준히 돌보기",
                "gentle" => "다정하게 돌보기",
                "patient" => "천천히 돌보기",
                _ => "알 수 없는 돌봄 방법"
            };
        }

        private static string FormatSelectionResult(StarLineageSelectionResult result)
        {
            return result.Status switch
            {
                StarLineageSelectionStatus.Applied => "이어받을 도움을 골랐어요.",
                StarLineageSelectionStatus.NoPendingSelection => "지금 선택할 새 세대가 없습니다.",
                StarLineageSelectionStatus.UnknownTrait => "알 수 없는 도움입니다.",
                StarLineageSelectionStatus.DuplicateReceipt => "이미 반영한 선택입니다.",
                StarLineageSelectionStatus.CapacityFull => "이어진 기록이 가득 차 선택을 저장할 수 없어요.",
                _ => "이어받을 도움을 고를 수 없어요."
            };
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            openButton?.onClick.AddListener(OpenFromEntry);
            closeButton?.onClick.AddListener(Close);
            previousButton?.onClick.AddListener(ShowPrevious);
            nextButton?.onClick.AddListener(ShowNext);
            gentleTraitButton?.onClick.AddListener(SelectGentle);
            maturationTraitButton?.onClick.AddListener(SelectMaturation);
            blendingTraitButton?.onClick.AddListener(SelectBlending);
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            openButton?.onClick.RemoveListener(OpenFromEntry);
            closeButton?.onClick.RemoveListener(Close);
            previousButton?.onClick.RemoveListener(ShowPrevious);
            nextButton?.onClick.RemoveListener(ShowNext);
            gentleTraitButton?.onClick.RemoveListener(SelectGentle);
            maturationTraitButton?.onClick.RemoveListener(SelectMaturation);
            blendingTraitButton?.onClick.RemoveListener(SelectBlending);
            listenersBound = false;
        }

        private void AcquireGameplayBlock()
        {
            if (gameplayBlocked)
            {
                return;
            }

            blockingChanged?.Invoke(true);
            gameplayBlocked = true;
        }

        private void ReleaseGameplayBlock()
        {
            if (!gameplayBlocked)
            {
                return;
            }

            blockingChanged?.Invoke(false);
            gameplayBlocked = false;
        }

        private void SetTraitButtons(bool active)
        {
            SetActive(gentleTraitButton?.gameObject, active);
            SetActive(maturationTraitButton?.gameObject, active);
            SetActive(blendingTraitButton?.gameObject, active);
            SetInteractable(gentleTraitButton, active);
            SetInteractable(maturationTraitButton, active);
            SetInteractable(blendingTraitButton, active);
        }

        private void SetRecordNavigation(bool previous, bool next)
        {
            SetActive(previousButton?.gameObject, snapshot?.Records.Count > 1 && snapshot.RequiresTraitSelection == false);
            SetActive(nextButton?.gameObject, snapshot?.Records.Count > 1 && snapshot.RequiresTraitSelection == false);
            SetInteractable(previousButton, previous);
            SetInteractable(nextButton, next);
        }

        private void SetPanelActive(bool active)
        {
            if (panelRoot != null && panelRoot.activeSelf != active)
            {
                panelRoot.SetActive(active);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetInteractable(Selectable target, bool active)
        {
            if (target != null)
            {
                target.interactable = active;
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
