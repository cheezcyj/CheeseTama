using CheeseTama.Core;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class AccessibilitySettingsPanelController : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button textScale100Button;
        [SerializeField] private Button textScale125Button;
        [SerializeField] private Button textScale140Button;
        [SerializeField] private Toggle highContrastToggle;
        [SerializeField] private Toggle reduceMotionToggle;
        [SerializeField] private Text textScaleValueText;
        [SerializeField] private Text statusText;
        [SerializeField] private Transform applicationRoot;

        private bool isRefreshing;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Configure(
            Button open,
            Button close,
            GameObject root,
            Button textScale100,
            Button textScale125,
            Button textScale140,
            Toggle highContrast,
            Toggle reduceMotion,
            Text textScaleValue,
            Text status,
            Transform accessibilityApplicationRoot)
        {
            openButton = open;
            closeButton = close;
            panelRoot = root;
            textScale100Button = textScale100;
            textScale125Button = textScale125;
            textScale140Button = textScale140;
            highContrastToggle = highContrast;
            reduceMotionToggle = reduceMotion;
            textScaleValueText = textScaleValue;
            statusText = status;
            applicationRoot = accessibilityApplicationRoot;

            BindControls();
            RefreshFromSave("접근성 설정을 불러왔습니다.");
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            BindControls();
        }

        private void BindControls()
        {
            BindButton(openButton, Open);
            BindButton(closeButton, Close);
            BindTextScaleButton(textScale100Button, GameSettingsSaveData.DefaultTextScale);
            BindTextScaleButton(textScale125Button, GameSettingsSaveData.MediumTextScale);
            BindTextScaleButton(textScale140Button, GameSettingsSaveData.LargeTextScale);

            if (highContrastToggle != null)
            {
                highContrastToggle.onValueChanged.RemoveListener(SetHighContrast);
                highContrastToggle.onValueChanged.AddListener(SetHighContrast);
            }

            if (reduceMotionToggle != null)
            {
                reduceMotionToggle.onValueChanged.RemoveListener(SetReducedMotion);
                reduceMotionToggle.onValueChanged.AddListener(SetReducedMotion);
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void BindTextScaleButton(Button button, float value)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SetTextScale(value));
        }

        public void Open()
        {
            RefreshFromSave("접근성 설정을 불러왔습니다.");
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void SetTextScale(float value)
        {
            var settings = ResolveSettings();
            settings.textScale = GameSettingsSaveData.NormalizeTextScale(value);
            ApplyAndSave(settings, "글자 크기를 저장했습니다.");
        }

        public void SetHighContrast(bool value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = ResolveSettings();
            settings.highContrastUi = value;
            ApplyAndSave(settings, "고대비 표시를 저장했습니다.");
        }

        public void SetReducedMotion(bool value)
        {
            if (isRefreshing)
            {
                return;
            }

            var settings = ResolveSettings();
            settings.reduceMotion = value;
            ApplyAndSave(settings, "애니메이션 감소 설정을 저장했습니다.");
        }

        public void RefreshFromSave(string message = "접근성 설정을 불러왔습니다.")
        {
            var settings = ResolveSettings();
            settings.EnsureRuntimeDefaults();

            isRefreshing = true;
            highContrastToggle?.SetIsOnWithoutNotify(settings.highContrastUi);
            reduceMotionToggle?.SetIsOnWithoutNotify(settings.reduceMotion);
            isRefreshing = false;

            RefreshLabels(settings, message);
        }

        private void ApplyAndSave(GameSettingsSaveData settings, string message)
        {
            settings.EnsureRuntimeDefaults();
            AccessibilityRuntime.Apply(ResolveApplicationRoot(), settings);
            StarterSceneBuilder.EnsureCoreSystems().SaveGame();
            RefreshLabels(settings, message);
        }

        private void RefreshLabels(GameSettingsSaveData settings, string message)
        {
            if (textScaleValueText != null)
            {
                textScaleValueText.text = $"{Mathf.RoundToInt(settings.textScale * 100f)}%";
            }

            if (statusText != null)
            {
                statusText.text = message;
            }

            SetSelected(textScale100Button, Mathf.Approximately(settings.textScale, GameSettingsSaveData.DefaultTextScale));
            SetSelected(textScale125Button, Mathf.Approximately(settings.textScale, GameSettingsSaveData.MediumTextScale));
            SetSelected(textScale140Button, Mathf.Approximately(settings.textScale, GameSettingsSaveData.LargeTextScale));
        }

        private static void SetSelected(Button button, bool selected)
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

        private GameSettingsSaveData ResolveSettings()
        {
            var manager = StarterSceneBuilder.EnsureCoreSystems();
            if (manager.CurrentSave == null)
            {
                manager.LoadOrCreateGame();
            }

            manager.CurrentSave.EnsureRuntimeDefaults();
            return manager.CurrentSave.settings;
        }

        private Transform ResolveApplicationRoot()
        {
            if (applicationRoot != null)
            {
                return applicationRoot;
            }

            var canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform : transform.root;
        }
    }
}
