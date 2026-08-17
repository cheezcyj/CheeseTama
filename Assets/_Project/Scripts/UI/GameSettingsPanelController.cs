using CheeseTama.Core;
using CheeseTama.Save;
using CheeseTama.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class GameSettingsPanelController : MonoBehaviour
    {
        private static readonly Vector2 BaseReferenceResolution = new Vector2(1920f, 1080f);

        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider effectVolumeSlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Toggle fullScreenToggle;
        [SerializeField] private Toggle careTipToggle;
        [SerializeField] private Button uiScale90Button;
        [SerializeField] private Button uiScale100Button;
        [SerializeField] private Button uiScale110Button;
        [SerializeField] private Button frameRate30Button;
        [SerializeField] private Button frameRate60Button;
        [SerializeField] private Button frameRate120Button;
        [SerializeField] private Button resetSettingsButton;
        [SerializeField] private Text masterVolumeValueText;
        [SerializeField] private Text musicVolumeValueText;
        [SerializeField] private Text effectVolumeValueText;
        [SerializeField] private Text uiScaleValueText;
        [SerializeField] private Text frameRateValueText;
        [SerializeField] private Text statusText;

        private bool isRefreshing;

        public void Configure(
            Slider masterVolume,
            Toggle mute,
            Toggle fullScreen,
            Button uiScale90,
            Button uiScale100,
            Button uiScale110,
            Button frameRate30,
            Button frameRate60,
            Button frameRate120,
            Toggle careTip,
            Button resetSettings,
            Text masterVolumeValue,
            Text uiScaleValue,
            Text frameRateValue,
            Text settingsStatus)
        {
            Configure(
                masterVolume,
                null,
                null,
                mute,
                fullScreen,
                uiScale90,
                uiScale100,
                uiScale110,
                frameRate30,
                frameRate60,
                frameRate120,
                careTip,
                resetSettings,
                masterVolumeValue,
                null,
                null,
                uiScaleValue,
                frameRateValue,
                settingsStatus);
        }

        public void Configure(
            Slider masterVolume,
            Slider musicVolume,
            Slider effectVolume,
            Toggle mute,
            Toggle fullScreen,
            Button uiScale90,
            Button uiScale100,
            Button uiScale110,
            Button frameRate30,
            Button frameRate60,
            Button frameRate120,
            Toggle careTip,
            Button resetSettings,
            Text masterVolumeValue,
            Text musicVolumeValue,
            Text effectVolumeValue,
            Text uiScaleValue,
            Text frameRateValue,
            Text settingsStatus)
        {
            masterVolumeSlider = masterVolume;
            musicVolumeSlider = musicVolume;
            effectVolumeSlider = effectVolume;
            muteToggle = mute;
            fullScreenToggle = fullScreen;
            uiScale90Button = uiScale90;
            uiScale100Button = uiScale100;
            uiScale110Button = uiScale110;
            frameRate30Button = frameRate30;
            frameRate60Button = frameRate60;
            frameRate120Button = frameRate120;
            careTipToggle = careTip;
            resetSettingsButton = resetSettings;
            masterVolumeValueText = masterVolumeValue;
            musicVolumeValueText = musicVolumeValue;
            effectVolumeValueText = effectVolumeValue;
            uiScaleValueText = uiScaleValue;
            frameRateValueText = frameRateValue;
            statusText = settingsStatus;

            BindControls();
            RefreshFromSave(true);
        }

        private void OnEnable()
        {
            BindControls();
            RefreshFromSave(true);
        }

        private void BindControls()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            }

            if (effectVolumeSlider != null)
            {
                effectVolumeSlider.onValueChanged.RemoveListener(SetEffectVolume);
                effectVolumeSlider.onValueChanged.AddListener(SetEffectVolume);
            }

            if (muteToggle != null)
            {
                muteToggle.onValueChanged.RemoveListener(SetMuted);
                muteToggle.onValueChanged.AddListener(SetMuted);
            }

            if (fullScreenToggle != null)
            {
                fullScreenToggle.onValueChanged.RemoveListener(SetFullScreen);
                fullScreenToggle.onValueChanged.AddListener(SetFullScreen);
            }

            if (careTipToggle != null)
            {
                careTipToggle.onValueChanged.RemoveListener(SetCareTipsVisible);
                careTipToggle.onValueChanged.AddListener(SetCareTipsVisible);
            }

            BindUiScaleButton(uiScale90Button, 0.9f);
            BindUiScaleButton(uiScale100Button, 1f);
            BindUiScaleButton(uiScale110Button, 1.1f);
            BindFrameRateButton(frameRate30Button, 30);
            BindFrameRateButton(frameRate60Button, 60);
            BindFrameRateButton(frameRate120Button, 120);

            if (resetSettingsButton != null)
            {
                resetSettingsButton.onClick.RemoveListener(ResetSettings);
                resetSettingsButton.onClick.AddListener(ResetSettings);
            }
        }

        private void BindUiScaleButton(Button button, float uiScale)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SetUiScale(uiScale));
        }

        private void BindFrameRateButton(Button button, int frameRate)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SetFrameRate(frameRate));
        }

        public void RefreshFromSave(bool applySettings)
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            var settings = GetSettings(manager);
            if (settings == null)
            {
                return;
            }

            isRefreshing = true;
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
            }


            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
            }

            if (effectVolumeSlider != null)
            {
                effectVolumeSlider.SetValueWithoutNotify(settings.effectVolume);
            }

            if (muteToggle != null)
            {
                muteToggle.SetIsOnWithoutNotify(settings.muteAudio);
            }

            if (fullScreenToggle != null)
            {
                fullScreenToggle.SetIsOnWithoutNotify(settings.fullScreen);
            }

            if (careTipToggle != null)
            {
                careTipToggle.SetIsOnWithoutNotify(settings.showCareTips);
            }

            isRefreshing = false;

            if (applySettings)
            {
                ApplySettings(settings);
            }

            RefreshLabels(settings, "설정을 불러왔습니다.");
        }

        private void SetMasterVolume(float value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = GetSettings(StarterSceneBuilder.EnsureCoreSystems());
            settings.masterVolume = Mathf.Clamp01(value);
            ApplyAndSave(settings, "소리 설정을 저장했습니다.");
        }

        private void SetMusicVolume(float value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = GetSettings(StarterSceneBuilder.EnsureCoreSystems());
            settings.musicVolume = Mathf.Clamp01(value);
            ApplyAndSave(settings, "배경음 볼륨을 저장했습니다.");
        }

        private void SetEffectVolume(float value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = GetSettings(StarterSceneBuilder.EnsureCoreSystems());
            settings.effectVolume = Mathf.Clamp01(value);
            ApplyAndSave(settings, "효과음 볼륨을 저장했습니다.");
        }

        private void SetUiScale(float value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = GetSettings(StarterSceneBuilder.EnsureCoreSystems());
            settings.uiScale = Mathf.Clamp(value, GameSettingsSaveData.MinUiScale, GameSettingsSaveData.MaxUiScale);
            ApplyAndSave(settings, "화면 설정을 저장했습니다.");
        }

        private void SetMuted(bool value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = GetSettings(StarterSceneBuilder.EnsureCoreSystems());
            settings.muteAudio = value;
            ApplyAndSave(settings, "소리 설정을 저장했습니다.");
        }

        private void SetFullScreen(bool value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = GetSettings(StarterSceneBuilder.EnsureCoreSystems());
            settings.fullScreen = value;
            ApplyAndSave(settings, value ? "전체화면으로 전환했습니다." : "창모드로 전환했습니다.");
        }

        private void SetCareTipsVisible(bool value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = GetSettings(StarterSceneBuilder.EnsureCoreSystems());
            settings.showCareTips = value;
            ApplyAndSave(settings, "조작 설정을 저장했습니다.");
        }

        private void SetFrameRate(int frameRate)
        {
            var settings = GetSettings(StarterSceneBuilder.EnsureCoreSystems());
            settings.targetFrameRate = frameRate;
            ApplyAndSave(settings, "화면 설정을 저장했습니다.");
        }

        private void ResetSettings()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            manager.CurrentSave.EnsureRuntimeDefaults();
            manager.CurrentSave.settings = GameSettingsSaveData.CreateDefault();
            ApplyAndSave(manager.CurrentSave.settings, "설정을 기본값으로 돌렸습니다.");
            RefreshFromSave(false);
        }

        private void ApplyAndSave(GameSettingsSaveData settings, string message)
        {
            settings.EnsureRuntimeDefaults();
            ApplySettings(settings);
            StarterSceneBuilder.EnsureCoreSystems().SaveGame();
            RefreshLabels(settings, message);
        }

        private void ApplySettings(GameSettingsSaveData settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.EnsureRuntimeDefaults();
            AudioListener.volume = settings.muteAudio ? 0f : settings.masterVolume;
            CheeseTamaAudioController.Instance?.ApplyVolumeSettings(settings);
            ApplyScreenMode(settings.fullScreen);
            Application.targetFrameRate = settings.targetFrameRate;
            ApplyUiScale(settings.uiScale);
            ApplyCareTipVisibility(settings.showCareTips);
        }

        private static void ApplyScreenMode(bool fullScreen)
        {
            var width = Screen.width > 0 ? Screen.width : Screen.currentResolution.width;
            var height = Screen.height > 0 ? Screen.height : Screen.currentResolution.height;
            var mode = fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.fullScreenMode = mode;
            Screen.SetResolution(width, height, mode);
            Screen.fullScreen = fullScreen;
        }

        private void ApplyUiScale(float uiScale)
        {
            var scale = Mathf.Clamp(uiScale, GameSettingsSaveData.MinUiScale, GameSettingsSaveData.MaxUiScale);
            var referenceResolution = BaseReferenceResolution / scale;
            var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (var scaler in scalers)
            {
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    continue;
                }

                scaler.referenceResolution = referenceResolution;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void ApplyCareTipVisibility(bool showCareTips)
        {
            var canvasTransform = transform.parent;
            var careTipPanel = canvasTransform != null ? canvasTransform.Find("Care Tip Panel") : null;
            if (careTipPanel != null)
            {
                careTipPanel.gameObject.SetActive(showCareTips);
            }
        }

        private void RefreshLabels(GameSettingsSaveData settings, string message)
        {
            SetText(masterVolumeValueText, settings.muteAudio ? "음소거" : $"{Mathf.RoundToInt(settings.masterVolume * 100f)}%");
            SetText(musicVolumeValueText, $"{Mathf.RoundToInt(settings.musicVolume * 100f)}%");
            SetText(effectVolumeValueText, $"{Mathf.RoundToInt(settings.effectVolume * 100f)}%");
            SetText(uiScaleValueText, $"{Mathf.RoundToInt(settings.uiScale * 100f)}%");
            SetText(frameRateValueText, $"{settings.targetFrameRate} FPS · {(settings.fullScreen ? "전체" : "창")}");
            SetText(statusText, message);

            SetOptionButtonSelected(uiScale90Button, Mathf.Approximately(settings.uiScale, 0.9f));
            SetOptionButtonSelected(uiScale100Button, Mathf.Approximately(settings.uiScale, 1f));
            SetOptionButtonSelected(uiScale110Button, Mathf.Approximately(settings.uiScale, 1.1f));
            SetFrameRateButtonSelected(frameRate30Button, settings.targetFrameRate == 30);
            SetFrameRateButtonSelected(frameRate60Button, settings.targetFrameRate == 60);
            SetFrameRateButtonSelected(frameRate120Button, settings.targetFrameRate == 120);
        }

        private static void SetFrameRateButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            SetOptionButtonSelected(button, selected);
        }

        private static void SetOptionButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = selected ? new Color(1f, 0.74f, 0.24f) : new Color(1f, 0.86f, 0.46f);
            colors.highlightedColor = selected ? new Color(1f, 0.8f, 0.32f) : new Color(1f, 0.9f, 0.58f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private static GameSettingsSaveData GetSettings(GameManager manager)
        {
            if (manager == null || manager.CurrentSave == null)
            {
                return GameSettingsSaveData.CreateDefault();
            }

            manager.CurrentSave.EnsureRuntimeDefaults();
            return manager.CurrentSave.settings;
        }
    }
}
