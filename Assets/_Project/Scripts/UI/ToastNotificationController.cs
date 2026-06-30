using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class ToastNotificationController : MonoBehaviour
    {
        [SerializeField] private Text toastText;
        [SerializeField] private float visibleUntil;

        public void Configure(Text label)
        {
            toastText = label;
            Hide();
        }

        public void Show(string message, float seconds = 2.5f)
        {
            if (toastText == null)
            {
                return;
            }

            toastText.text = message;
            toastText.gameObject.SetActive(true);
            visibleUntil = Time.unscaledTime + Mathf.Max(0.5f, seconds);
        }

        private void Update()
        {
            if (toastText != null && toastText.gameObject.activeSelf && Time.unscaledTime >= visibleUntil)
            {
                Hide();
            }
        }

        private void Hide()
        {
            if (toastText != null)
            {
                toastText.gameObject.SetActive(false);
            }
        }
    }
}
