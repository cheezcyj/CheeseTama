using CheeseTama.Core;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Decorations;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class DecorateThemePanelController : MonoBehaviour
    {
        [SerializeField] private Text stateText;
        [SerializeField] private Text themeText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text lightingText;
        [SerializeField] private Text furnitureText;
        [SerializeField] private Text propsText;
        [SerializeField] private Button morningButton;
        [SerializeField] private Button eveningButton;
        [SerializeField] private Button nightButton;
        [SerializeField] private Button rainyButton;
        [SerializeField] private Button starlightButton;
        [SerializeField] private Button winterButton;
        [SerializeField] private Button vintageButton;

        private MilkroomThemeController themeController;
        private MilkroomLightingController lightingController;
        private MilkroomAmbientEventController ambientController;

        public void Configure(
            Text currentState,
            Text selectedTheme,
            Text selectedDetail,
            Text selectedLighting,
            Text selectedFurniture,
            Text selectedProps,
            Button morning,
            Button evening,
            Button night,
            Button rainy,
            Button starlight = null,
            Button winter = null,
            Button vintage = null)
        {
            stateText = currentState;
            themeText = selectedTheme;
            detailText = selectedDetail;
            lightingText = selectedLighting;
            furnitureText = selectedFurniture;
            propsText = selectedProps;
            morningButton = morning;
            eveningButton = evening;
            nightButton = night;
            rainyButton = rainy;
            starlightButton = starlight;
            winterButton = winter;
            vintageButton = vintage;

            BindButtons();
            RefreshFromSave();
        }

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            BindButtons();
            RefreshFromSave();
        }

        private void BindButtons()
        {
            BindButton(morningButton, MilkroomThemeController.MorningThemeId);
            BindButton(eveningButton, MilkroomThemeController.EveningThemeId);
            BindButton(nightButton, MilkroomThemeController.NightThemeId);
            BindButton(rainyButton, MilkroomThemeController.RainyThemeId);
            BindButton(starlightButton, MilkroomThemeController.StarlightThemeId);
            BindButton(winterButton, MilkroomThemeController.WinterThemeId);
            BindButton(vintageButton, MilkroomThemeController.VintageThemeId);
        }

        private void BindButton(Button button, string themeId)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectTheme(themeId));
        }

        private void SelectTheme(string themeId)
        {
            var definition = MilkroomThemeCatalog.Find(themeId);
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (definition == null || manager.CurrentSave == null)
            {
                RefreshFromSave("선택한 테마를 찾을 수 없습니다.");
                return;
            }

            manager.CurrentSave.EnsureRuntimeDefaults();
            var unlockSystem = new MilkroomThemeUnlockSystem();
            if (!unlockSystem.IsVisible(manager.CurrentSave, definition.Id))
            {
                RefreshFromSave("아직 이 테마의 흔적을 발견하지 못했습니다.");
                return;
            }

            if (!unlockSystem.IsOwned(manager.CurrentSave, definition.Id))
            {
                var unlockResult = manager.TryUnlockMilkroomTheme(definition.Id);
                if (!unlockResult.Succeeded)
                {
                    RefreshFromSave(unlockResult.Message);
                    return;
                }

                ApplyTheme(unlockResult.ThemeId);
                RefreshTexts(unlockResult.ThemeId, unlockResult.Message);
                RefreshButtonStates(manager.CurrentSave);
                return;
            }

            if (!manager.TrySelectMilkroomTheme(definition.Id))
            {
                RefreshFromSave("이 테마는 아직 선택할 수 없습니다.");
                return;
            }

            ApplyTheme(definition.Id);
            RefreshTexts(definition.Id);
            RefreshButtonStates(manager.CurrentSave);
        }

        public void RefreshFromSave()
        {
            RefreshFromSave(string.Empty);
        }

        private void RefreshFromSave(string statusMessage)
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            var themeId = MilkroomThemeController.MorningThemeId;
            if (manager.CurrentSave != null)
            {
                manager.CurrentSave.EnsureRuntimeDefaults();
                themeId = MilkroomThemeCatalog.Normalize(manager.CurrentSave.milkroomThemeId);
            }

            ApplyTheme(themeId);
            RefreshTexts(themeId, statusMessage);
            RefreshButtonStates(manager.CurrentSave);
        }

        private void ApplyTheme(string themeId)
        {
            CacheControllers();
            themeController?.ApplyTheme(themeId);
            lightingController?.ApplyTheme(themeId);
            ambientController?.SetTheme(themeId);
            Object.FindFirstObjectByType<DecorationRoomPresenter>()?.Refresh();
        }

        private void CacheControllers()
        {
            themeController ??= Object.FindFirstObjectByType<MilkroomThemeController>();
            lightingController ??= Object.FindFirstObjectByType<MilkroomLightingController>();
            ambientController ??= Object.FindFirstObjectByType<MilkroomAmbientEventController>();
        }

        private void RefreshTexts(string themeId, string statusMessage = "")
        {
            var definition = MilkroomThemeCatalog.Find(themeId)
                ?? MilkroomThemeCatalog.Find(MilkroomThemeController.MorningThemeId);
            var starDrops = GameManager.Instance?.CurrentSave?.economy?.starDrops ?? 0;
            var statusPrefix = string.IsNullOrWhiteSpace(statusMessage)
                ? string.Empty
                : $"{statusMessage}  ";
            SetText(stateText, $"{statusPrefix}현재 테마: {definition.DisplayName} · 별방울 {starDrops}");
            SetText(themeText, definition.DisplayName);
            SetText(detailText, definition.Detail);
            SetText(lightingText, definition.LightingDetail);
            SetText(furnitureText, "가구 배치와 소품 재질은 유지됩니다.");
            SetText(propsText, definition.PropsDetail);
        }

        private void RefreshButtonStates(CheeseTama.Save.CheeseTamaSaveData saveData)
        {
            var unlockSystem = new MilkroomThemeUnlockSystem();
            RefreshButton(morningButton, MilkroomThemeController.MorningThemeId, saveData, unlockSystem);
            RefreshButton(eveningButton, MilkroomThemeController.EveningThemeId, saveData, unlockSystem);
            RefreshButton(nightButton, MilkroomThemeController.NightThemeId, saveData, unlockSystem);
            RefreshButton(rainyButton, MilkroomThemeController.RainyThemeId, saveData, unlockSystem);
            RefreshButton(starlightButton, MilkroomThemeController.StarlightThemeId, saveData, unlockSystem);
            RefreshButton(winterButton, MilkroomThemeController.WinterThemeId, saveData, unlockSystem);
            RefreshButton(vintageButton, MilkroomThemeController.VintageThemeId, saveData, unlockSystem);
        }

        private static void RefreshButton(
            Button button,
            string themeId,
            CheeseTama.Save.CheeseTamaSaveData saveData,
            MilkroomThemeUnlockSystem unlockSystem)
        {
            if (button == null)
            {
                return;
            }

            var definition = MilkroomThemeCatalog.Find(themeId);
            var visible = definition != null && unlockSystem.IsVisible(saveData, themeId);
            button.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            button.interactable = saveData != null;
            var owned = unlockSystem.IsOwned(saveData, themeId);
            var label = owned
                ? definition.ShortName
                : $"{definition.ShortName} · 별방울 {definition.StarDropCost}";
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

    }
}
