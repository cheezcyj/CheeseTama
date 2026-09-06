using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect scrollRect;

        public void Configure(Button settingsButton, Button settingsCloseButton, GameObject settingsModalRoot)
        {
            Configure(settingsButton, settingsCloseButton, settingsModalRoot, null);
        }

        public void Configure(
            Button settingsButton,
            Button settingsCloseButton,
            GameObject settingsModalRoot,
            ScrollRect settingsScrollRect)
        {
            openButton = settingsButton;
            closeButton = settingsCloseButton;
            modalRoot = settingsModalRoot;
            scrollRect = settingsScrollRect;

            if (openButton != null)
            {
                openButton.onClick.RemoveListener(Open);
                openButton.onClick.AddListener(Open);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        public void Open()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Close()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }
    }
}
