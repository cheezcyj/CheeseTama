using CheeseTama.Core;
using CheeseTama.Environment;
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
            Button rainy)
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
            var normalizedThemeId = NormalizeThemeId(themeId);
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager.CurrentSave != null)
            {
                manager.CurrentSave.EnsureRuntimeDefaults();
                manager.CurrentSave.milkroomThemeId = normalizedThemeId;
                manager.SaveGame();
            }

            ApplyTheme(normalizedThemeId);
            RefreshTexts(normalizedThemeId);
        }

        public void RefreshFromSave()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            var themeId = MilkroomThemeController.MorningThemeId;
            if (manager.CurrentSave != null)
            {
                manager.CurrentSave.EnsureRuntimeDefaults();
                themeId = NormalizeThemeId(manager.CurrentSave.milkroomThemeId);
            }

            ApplyTheme(themeId);
            RefreshTexts(themeId);
        }

        private void ApplyTheme(string themeId)
        {
            CacheControllers();
            themeController?.ApplyTheme(themeId);
            lightingController?.ApplyTheme(themeId);
            ambientController?.SetTheme(themeId);
        }

        private void CacheControllers()
        {
            themeController ??= Object.FindFirstObjectByType<MilkroomThemeController>();
            lightingController ??= Object.FindFirstObjectByType<MilkroomLightingController>();
            ambientController ??= Object.FindFirstObjectByType<MilkroomAmbientEventController>();
        }

        private void RefreshTexts(string themeId)
        {
            var displayName = GetThemeName(themeId);
            SetText(stateText, $"현재 테마: {displayName}");
            SetText(themeText, displayName);
            SetText(detailText, GetThemeDetail(themeId));
            SetText(lightingText, GetLightingDetail(themeId));
            SetText(furnitureText, "GLB 소품 재질 유지 / 벽과 바닥 팔레트만 전환");
            SetText(propsText, GetPropsDetail(themeId));
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private static string NormalizeThemeId(string themeId)
        {
            return themeId switch
            {
                MilkroomThemeController.EveningThemeId => MilkroomThemeController.EveningThemeId,
                MilkroomThemeController.NightThemeId => MilkroomThemeController.NightThemeId,
                MilkroomThemeController.RainyThemeId => MilkroomThemeController.RainyThemeId,
                _ => MilkroomThemeController.MorningThemeId
            };
        }

        private static string GetThemeName(string themeId)
        {
            return themeId switch
            {
                MilkroomThemeController.EveningThemeId => "따뜻한 오후 밀크룸",
                MilkroomThemeController.NightThemeId => "별빛 밤 밀크룸",
                MilkroomThemeController.RainyThemeId => "비 오는 밀크룸",
                _ => "따뜻한 아침 밀크룸"
            };
        }

        private static string GetThemeDetail(string themeId)
        {
            return themeId switch
            {
                MilkroomThemeController.EveningThemeId => "노을빛 벽 / 따뜻한 그림자 / 창가의 주황빛",
                MilkroomThemeController.NightThemeId => "차분한 밤색 벽 / 푸른 창빛 / 별빛 포인트",
                MilkroomThemeController.RainyThemeId => "흐린 벽색 / 차분한 바닥 / 창밖 빗방울 분위기",
                _ => "크림색 벽 / 정돈된 바닥 / 포근한 아침빛"
            };
        }

        private static string GetLightingDetail(string themeId)
        {
            return themeId switch
            {
                MilkroomThemeController.EveningThemeId => "노을빛 키라이트 + 낮은 림라이트",
                MilkroomThemeController.NightThemeId => "부드러운 푸른 주변광 + 낮은 조도",
                MilkroomThemeController.RainyThemeId => "흐린 하늘빛 필라이트 + 따뜻한 실내등",
                _ => "따뜻한 햇살 + 부드러운 림라이트"
            };
        }

        private static string GetPropsDetail(string themeId)
        {
            return themeId switch
            {
                MilkroomThemeController.EveningThemeId => "오후 빛줄기 표시",
                MilkroomThemeController.NightThemeId => "별빛 표시",
                MilkroomThemeController.RainyThemeId => "빗줄기 표시",
                _ => "기본 소품 배치 유지"
            };
        }
    }
}
