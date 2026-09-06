using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class UiButtonSound : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button ??= GetComponent<Button>();
            button.onClick.RemoveListener(PlayClick);
            button.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(PlayClick);
        }

        private static void PlayClick()
        {
            CheeseTamaAudioController.Instance?.PlayUiClick();
        }
    }
}
