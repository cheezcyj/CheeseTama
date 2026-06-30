using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        public void Configure(Button settingsButton, Button settingsCloseButton, GameObject settingsModalRoot)
        {
            openButton = settingsButton;
            closeButton = settingsCloseButton;
            modalRoot = settingsModalRoot;

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
