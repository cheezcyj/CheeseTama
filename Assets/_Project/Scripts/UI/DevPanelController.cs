using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class DevPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button toggleButton;

        public void Configure(GameObject root)
        {
            Configure(root, null);
        }

        public void Configure(GameObject root, Button button)
        {
            UnbindToggleButton();
            panelRoot = root;
            toggleButton = button;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(true);
            }

            if (isActiveAndEnabled)
            {
                BindToggleButton();
            }

            RefreshToggleButtonLabel();
#else
            HideDevelopmentUi();
#endif
        }

        private void OnEnable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(true);
            }

            BindToggleButton();
            RefreshToggleButtonLabel();
#else
            HideDevelopmentUi();
#endif
        }

        private void OnDisable()
        {
            UnbindToggleButton();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (panelRoot != null && Input.GetKeyDown(KeyCode.F12))
            {
                TogglePanel();
            }
#endif
        }

        private void TogglePanel()
        {
            if (panelRoot == null)
            {
                return;
            }

            panelRoot.SetActive(!panelRoot.activeSelf);
            RefreshToggleButtonLabel();
        }

        private void BindToggleButton()
        {
            UnbindToggleButton();
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(TogglePanel);
            }
        }

        private void UnbindToggleButton()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(TogglePanel);
            }
        }

        private void RefreshToggleButtonLabel()
        {
            if (toggleButton == null)
            {
                return;
            }

            var label = toggleButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = panelRoot != null && panelRoot.activeSelf
                    ? "개발자 닫기"
                    : "개발자 모드";
            }
        }

        private void HideDevelopmentUi()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(false);
            }

            enabled = false;
        }
    }
}
